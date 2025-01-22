using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Data;


namespace ArchiveShowDocs
{
    public class DatabaseService
    {
        private readonly DatabaseManager _databaseManager;
        public List<TreeNodeModel> nodeList;

        // Constructor que recibe el DatabaseManager con la conexión persistente
        public DatabaseService(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        }

        /// <summary>
        /// Carga los datos para el TreeView desde la base de datos.
        /// </summary>
        /// <returns>Lista de nodos del árbol.</returns>
        public List<TreeNodeModel> LoadTreeData(DataTable documentTable)
        {
            nodeList = new List<TreeNodeModel>();

            try
            {
                // Asegurarse de que la conexión persistente está activa
                _databaseManager.EnsurePersistentConnection();
                Console.WriteLine("Conexión persistente asegurada.");

                // Usar la conexión persistente del DatabaseManager
                // Generamos los nodos del treeView
                using (SqlCommand command = new SqlCommand("EXEC DATD..User_Archives_GetList", _databaseManager._persistentConnection))
                {
                    Console.WriteLine("Ejecutando procedimiento almacenado: User_Archives_GetList");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("No se encontraron filas en el resultado del procedimiento.");
                        }

                        while (reader.Read())
                        {
                            Console.WriteLine($"User_Archives_GetList - Leyendo fila: {reader["f_name"]}");

                            nodeList.Add(new TreeNodeModel
                            {
                                Table = reader["f_table"].ToString(),
                                OwnerId = Convert.ToInt32(reader["f_owner"]),
                                Key = Convert.ToInt32(reader["f_key"]),
                                Name = reader["f_name"].ToString(),
                                ImageId = Convert.ToInt32(reader["f_imageid"]),
                                Id = reader["f_id"].ToString(),
                                ParentId = reader["f_parentid"].ToString(),
                                Rank = Convert.ToInt32(reader["f_rank"]),
                                HasChildren = true // Marcar como nodo que puede tener hijos
                            });
                        }
                    }
                }

                /*
                // Añadir documentos como nodos del TreeView
                foreach (DataRow row in documentTable.Rows)
                {
                    // Verificar si "class_id" no es nulo y no es igual a 0
                    if (
                            (row["class_id"] == DBNull.Value || Convert.ToInt32(row["class_id"]) == 0) ||
                            (row["graphid"] == DBNull.Value || Convert.ToInt32(row["graphid"]) == 0)
                        )
                    {
                        Console.WriteLine($"Fila ignorada: class_id={row["class_id"]}, doc_id={row["doc_id"]}");
                        continue; // Saltar esta fila
                    }
                    
                    Console.WriteLine($"GetDocumentList - Leyendo fila: doc_id={row["doc_id"]}, designatio={row["designatio"]}, {row["name"]}");

                    nodeList.Add(new TreeNodeModel
                    {
                        Table = "DOCUMENT",
                        OwnerId = Convert.ToInt32(row["class_id"]),
                        Key = Convert.ToInt32(row["doc_id"]),
                        Name = row["name"].ToString(),
                        ImageId = Convert.ToInt32(row["graphid"]), // Usar imagen del tipo de documento
                        Id = $"DOCUMENT_{row["doc_id"]}",
                        ParentId = $"CLASSFOLD_{row["class_id"]}",
                        Rank = row["f_rank"] != DBNull.Value ? Convert.ToInt32(row["f_rank"]) : 0
                    });
                 }
                */
            }
            catch (SqlException sqlEx)
            {
                StringBuilder errorDetails = new StringBuilder();
                foreach (SqlError error in sqlEx.Errors)
                {
                    errorDetails.AppendLine($"Error: {error.Number}, Mensaje: {error.Message}, Línea: {error.LineNumber}");
                }
                Console.WriteLine($"Error SQL: {errorDetails}");
                MessageBox.Show($"Error al cargar los datos del árbol: {errorDetails}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar los datos del árbol: {ex.Message}");
                // Opcional: manejar el error o lanzar una excepción
            }

            return nodeList;
        }

        // Cargar subnodos de una carpeta
        public List<TreeNodeModel> LoadChildNodes(string parentId)
        {
            return nodeList.Where(n => n.ParentId == parentId).ToList();
        }

        // Obtener documentos relacionados con un nodo
        public List<TreeNodeModel> GetDocumentsForNode(string parentId, DataTable documentTable)
        {
            return (from row in documentTable.AsEnumerable()
                    where $"CLASSFOLD_{row["class_id"]}" == parentId
                    select new TreeNodeModel
                    {
                        Table = "DOCUMENT",
                        OwnerId = row.Field<int>("class_id"),
                        Key = row.Field<int>("doc_id"),
                        Name = row.Field<string>("designatio") + " [" + row.Field<string>("name") + "]",
                        ImageId = row.Field<int>("graphid"),
                        Id = $"DOCUMENT_{row.Field<int>("doc_id")}",
                        ParentId = parentId,
                        Rank = row["f_rank"] != DBNull.Value ? Convert.ToInt32(row["f_rank"]) : 0,
                        HasChildren = false // Marcar como nodo que no puede tener hijos
                    }).ToList();
        }

        /// <summary>
        /// Carga los datos de las imagines para el TreeView desde la base de datos.
        /// </summary>
        /// <returns>Lista de las imagines para los nodos del árbol.</returns>
        public Dictionary<int, Image> LoadImageList(IEnumerable<int> imageKeys)
        {
            var imageList = new Dictionary<int, Image>();

            try
            {
                // Asegurar conexión persistente
                _databaseManager.EnsurePersistentConnection();

                // Crear un parámetro para los f_keys necesarios
                string keysParameter = string.Join(",", imageKeys);

                if (string.IsNullOrEmpty(keysParameter))
                {
                    MessageBox.Show("No se encontraron claves de imágenes para cargar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return imageList;
                }

                // Consulta SQL para obtener solo las imágenes necesarias
                string query = $"SELECT f_key, f_blob FROM DBASE..uea_blobs WHERE f_key IN ({keysParameter})";

                // Consulta para obtener imágenes
                using (SqlCommand command = new SqlCommand(query, _databaseManager._persistentConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int key = reader.GetInt32(0);

                            Console.WriteLine($"Procesar la imagen: graphid={key}");

                            // Obtener el blob
                            byte[] blob = !reader.IsDBNull(1) ? (byte[])reader["f_blob"] : null;

                            // Procesar la imagen y asignarla al diccionario
                            imageList[key] = ProcessImage(blob, key);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                StringBuilder errorDetails = new StringBuilder();
                foreach (SqlError error in sqlEx.Errors)
                {
                    errorDetails.AppendLine($"Error: {error.Number}, Mensaje: {error.Message}, Línea: {error.LineNumber}");
                }
                Console.WriteLine($"Error SQL: {errorDetails}");
                MessageBox.Show($"Error al cargar los datos del árbol: {errorDetails}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de imágenes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return imageList;
        }

        /// <summary>
        /// Procesa una imagen desde un blob de datos.
        /// </summary>
        /// <param name="blob">El arreglo de bytes que representa la imagen.</param>
        /// <param name="key">La clave asociada con la imagen.</param>
        /// <returns>La imagen procesada o la imagen predeterminada si ocurre un error.</returns>
        private Image ProcessImage(byte[] blob, int key)
        {
            if (blob == null || blob.Length == 0)
            {
                Console.WriteLine($"Blob nulo o vacío para la clave {key}. Usando 'nodoc'.");
                blob = LoadDefaultNodocImage(); // Cargar la imagen predeterminada si aún no se ha cargado
            }

            try
            {
                // Validar si el blob puede ser convertido a una imagen
                using (var imageStream = new MemoryStream(blob))
                {
                    Console.WriteLine($"Clave: {key}, Longitud del blob: {blob.Length}");

                    // Verificar si el formato del flujo es válido
                    if (imageStream.Length == 0)
                    {
                        Console.WriteLine($"El flujo de la clave {key} está vacío. Usando 'nodoc'.");
                        return Image.FromStream(new MemoryStream(LoadDefaultNodocImage()));
                    }

                    return Image.FromStream(imageStream);
                }
            }
            catch (ArgumentException argEx)
            {
                Console.WriteLine($"Error de argumento al procesar la imagen para la clave {key}: {argEx.Message}");
            }
            catch (System.Runtime.InteropServices.ExternalException extEx)
            {
                Console.WriteLine($"Error externo al procesar la imagen para la clave {key}: {extEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al procesar la imagen para la clave {key}: {ex.Message}");
            }

            // Si algo falla, devolver la imagen predeterminada
            return Image.FromStream(new MemoryStream(LoadDefaultNodocImage()));
        }


        /// <summary>
        /// Carga la imagen predeterminada "nodoc".
        /// </summary>
        /// <returns>La imagen predeterminada "nodoc".</returns>
        private byte[] LoadDefaultNodocImage()
        {
            string nodocQuery = "SELECT f_blob FROM DBASE..uea_blobs WHERE f_key = 216";

            // Obtener el blob de la base de datos
            byte[] nodocBlob = null;

            try
            {
                // Asegurar conexión persistente
                _databaseManager.EnsurePersistentConnection();

                using (SqlCommand nodocCommand = new SqlCommand(nodocQuery, _databaseManager._persistentConnection))
                {
                    nodocBlob = nodocCommand.ExecuteScalar() as byte[];
                }

                return nodocBlob;
            }
            catch (SqlException sqlEx)
            {
                StringBuilder errorDetails = new StringBuilder();
                foreach (SqlError error in sqlEx.Errors)
                {
                    errorDetails.AppendLine($"Error: {error.Number}, Mensaje: {error.Message}, Línea: {error.LineNumber}");
                }
                Console.WriteLine($"Error SQL: {errorDetails}");
                MessageBox.Show($"Error al cargar los datos del árbol: {errorDetails}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return nodocBlob;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la imagen predeterminada 'nodoc': {ex.Message}");
                throw new InvalidOperationException("No se pudo cargar la imagen predeterminada 'nodoc'.", ex);
            }
        }


        /// <summary>
        /// Carga los datos de los documentos como el contenido para las carpetas de TreeView desde la base de datos.
        /// </summary>
        /// <returns>Tabla de los documentos como el contenido para los nodos del árbol.</returns>
        public DataTable LoadDocumentList()
        {
            try
            {
                // Asegurar que la conexión persistente está abierta
                _databaseManager.EnsurePersistentConnection();

                using (SqlCommand command = new SqlCommand("DATD..GetDocumentList", _databaseManager._persistentConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable documentTable = new DataTable();
                        adapter.Fill(documentTable);
                        return documentTable;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                StringBuilder errorDetails = new StringBuilder();
                foreach (SqlError error in sqlEx.Errors)
                {
                    errorDetails.AppendLine($"Error: {error.Number}, Mensaje: {error.Message}, Línea: {error.LineNumber}");
                }
                Console.WriteLine($"Error SQL: {errorDetails}");
                MessageBox.Show($"Error al cargar los datos del árbol: {errorDetails}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de documentos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Carga los datos del documento indicado antes de mostrarlo.
        /// </summary>
        /// <returns>Almacena los datos del documento indicado en DocumentModel.</returns>
        public DocumentModel GetDocumentDetails(int docId)
        {
            try
            {
                _databaseManager.EnsurePersistentConnection();

                // Consulta para obtener los detalles del documento
                string query = $@"
                    SELECT a.doc_id, a.designatio, a.name, b.dir_name, c.flname, c.filebody
                    FROM DATD..doclist a
                    JOIN DATD..dc b ON a.wrk_dir_id = b.dirkey_id
                    JOIN DDOC..docums1 c ON a.doc_id = c.file_id
                    WHERE a.doc_id = @docId";

                using (SqlCommand command = new SqlCommand(query, _databaseManager._persistentConnection))
                {
                    command.Parameters.AddWithValue("@docId", docId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DocumentModel
                            {
                                DocId = reader.GetInt32(0),
                                Designation = reader.GetString(1),
                                Name = reader.GetString(2),
                                DirectoryPath = reader.GetString(3),
                                FileName = reader.GetString(4),
                                FileBody = reader["filebody"] as byte[]
                            };
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                StringBuilder errorDetails = new StringBuilder();
                foreach (SqlError error in sqlEx.Errors)
                {
                    errorDetails.AppendLine($"Error: {error.Number}, Mensaje: {error.Message}, Línea: {error.LineNumber}");
                }
                Console.WriteLine($"Error SQL: {errorDetails}");
                MessageBox.Show($"Error al cargar los datos del árbol: {errorDetails}", "Error SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener detalles del documento: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Forzar la carga del documento.
        /// </summary>
        /// <returns>Almacena los datos del documento indicado en DocumentModel.</returns>
        public DocumentModel ForceReloadDocument(int docId)
        {
            // Implementa la lógica para obtener el documento directamente de la BD
            return GetDocumentDetails(docId);
        }

    }
}

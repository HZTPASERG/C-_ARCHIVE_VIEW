using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Drawing;
using System.IO;


namespace ArchiveShowDocs
{
    public class DatabaseService
    {
        private readonly DatabaseManager _databaseManager;

        // Constructor que recibe el DatabaseManager con la conexión persistente
        public DatabaseService(DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        }

        /// <summary>
        /// Carga los datos para el TreeView desde la base de datos.
        /// </summary>
        /// <returns>Lista de nodos del árbol.</returns>
        public List<TreeNodeModel> LoadTreeData()
        {
            List<TreeNodeModel> nodes = new List<TreeNodeModel>();

            try
            {
                // Asegurarse de que la conexión persistente está activa
                _databaseManager.EnsurePersistentConnection();
                Console.WriteLine("Conexión persistente asegurada.");

                // Usar la conexión persistente del DatabaseManager
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
                            Console.WriteLine($"Leyendo fila: {reader["f_name"]}");

                            nodes.Add(new TreeNodeModel
                            {
                                Table = reader["f_table"].ToString(),
                                OwnerId = Convert.ToInt32(reader["f_owner"]),
                                Key = Convert.ToInt32(reader["f_key"]),
                                Name = reader["f_name"].ToString(),
                                ImageId = Convert.ToInt32(reader["f_imageid"]),
                                Id = reader["f_id"].ToString(),
                                ParentId = reader["f_parentid"].ToString(),
                                Rank = Convert.ToInt32(reader["f_rank"])
                            });
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
                Console.WriteLine($"Error al cargar los datos del árbol: {ex.Message}");
                // Opcional: manejar el error o lanzar una excepción
            }

            return nodes;
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

                            // Validar que el blob no sea nulo y contenga datos
                            if (!reader.IsDBNull(1))
                            {
                                byte[] blob = (byte[])reader["f_blob"];

                                // Verificar que el blob no esté vacío
                                if (blob.Length > 0)
                                {
                                    try
                                    {
                                        using (var ms = new MemoryStream(blob))
                                        {
                                            Image image = Image.FromStream(ms);
                                            imageList[key] = image;
                                        }
                                    }
                                    catch (Exception imgEx)
                                    {
                                        // Log de errores relacionados con la imagen específica
                                        Console.WriteLine($"Error al procesar la imagen para la clave {key}: {imgEx.Message}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Blob nulo para la clave {key}");
                            }
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

    }
}

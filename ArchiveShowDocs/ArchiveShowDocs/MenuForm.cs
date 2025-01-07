using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ArchiveShowDocs
{
    public partial class MenuForm : Form
    {
        private MainApp _mainApp;
        private DatabaseService _databaseService;
        private ImageList _imageList;
        public DataTable documentTable;

        public MenuForm(MainApp mainApp)
        {
            InitializeComponent();
            _mainApp = mainApp ?? throw new ArgumentNullException(nameof(mainApp));

            // Inicializar el servicio de base de datos con la cadena de conexión
            _databaseService = new DatabaseService(_mainApp.DatabaseManagerPersistent);

            // Configurar TreeView para un estilo más moderno
            ConfigureTreeView();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Cargar los documentos en la tabla "documentTable"
            documentTable = _databaseService.LoadDocumentList(); // Carga todos los documentos

            // Llama al método para cargar los datos
            LoadTreeView();
        }

        private void LoadTreeView()
        {
            try
            {
                // Cargar los datos desde la base de datos
                List<TreeNodeModel> nodes = _databaseService.LoadTreeData(documentTable);

                if (nodes == null || nodes.Count == 0)
                {
                    MessageBox.Show("No se encontraron datos para el TreeView.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Obtener las claves únicas de las imágenes desde los nodos cargados
                var imageKeys = nodes
                    .Select(n => n.ImageId) // Asumiendo que `ImageId` es el identificador de la imagen en la BD
                    .Distinct()
                    .ToList();

                if (imageKeys.Count == 0)
                {
                    MessageBox.Show("No se encontraron claves de imágenes asociadas con los nodos del TreeView.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Cargar imágenes desde la base de datos usando las claves únicas
                // Después de cargar los datos del árbol, cargamos las imágenes y configúramoslas en el TreeView
                var images = _databaseService.LoadImageList(imageKeys);
                InitializeImageList(images);

                // Rellenar el TreeView con los datos
                PopulateTreeView(nodes);

                // Manejar el evento BeforeExpand para cargar subnodos y documentos dinámicamente
                treeView.BeforeExpand += TreeView_BeforeExpand;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el TreeView: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateTreeView(List<TreeNodeModel> nodes)
        {
            treeView.Nodes.Clear(); // Limpiar nodos existentes

            // Encontrar los nodos raíz (ParentId == "ROOT_0")
            var rootNodes = nodes.Where(n => n.ParentId == "ROOT_0").ToList();

            foreach (var rootNode in rootNodes)
            {
                TreeNode treeNode = new TreeNode
                {
                    Text = rootNode.Name,
                    Tag = rootNode.Id,
                    ImageKey = rootNode.ImageId.ToString(), // Asignar imagen por clave
                    SelectedImageKey = rootNode.ImageId.ToString()
                };

                // AddChildNodes(treeNode, rootNode.Id, nodes);

                // Si el nodo tiene hijos, agregar un placeholder
                if (rootNode.HasChildren)
                {
                    treeNode.Nodes.Add(new TreeNode("Cargando..."));
                }

                treeView.Nodes.Add(treeNode);
            }
        }

        private void AddChildNodes(TreeNode parentNode, string parentId, List<TreeNodeModel> nodes)
        {
            var childNodes = nodes.Where(n => n.ParentId == parentId).ToList();

            foreach (var childNode in childNodes)
            {
                TreeNode treeNode = new TreeNode
                {
                    Text = childNode.Name,
                    Tag = childNode.Id,
                    ImageKey = childNode.ImageId.ToString(), // Asignar imagen por clave
                    SelectedImageKey = childNode.ImageId.ToString()
                };

                AddChildNodes(treeNode, childNode.Id, nodes);
                parentNode.Nodes.Add(treeNode);
            }
        }

        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode parentNode = e.Node;
            string parentId = parentNode.Tag.ToString();

            // Limpiar subnodos si ya se cargaron previamente
            if (parentNode.Nodes.Count == 1 && parentNode.Nodes[0].Text == "Cargando...")
            {
                parentNode.Nodes.Clear();

                // Cargar los nodos hijos y documentos relacionados
                var childNodes = _databaseService.LoadChildNodes(parentId); // Nodos hijos
                var documents = _databaseService.GetDocumentsForNode(parentId, documentTable); // Documentos

                // Obtener claves de imágenes de los documentos y cargar dinámicamente
                var imageKeys = documents.Select(d => d.ImageId).Distinct().ToList();
                if (imageKeys.Count > 0)
                {
                    var newImages = _databaseService.LoadImageList(imageKeys);
                    foreach (var kvp in newImages)
                    {
                        if (!_imageList.Images.ContainsKey(kvp.Key.ToString()))
                        {
                            try
                            {
                                _imageList.Images.Add(kvp.Key.ToString(), kvp.Value);
                                Console.WriteLine($"Imagen añadida dinámicamente para la clave: {kvp.Key}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error al agregar la imagen para la clave {kvp.Key}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Imagen con clave {kvp.Key} ya existe en el ImageList.");
                        }
                    }
                }

                // Agregar nodos hijos al nodo expandido
                foreach (var childNode in childNodes)
                {
                    TreeNode childTreeNode = new TreeNode
                    {
                        Text = childNode.Name,
                        Tag = childNode.Id,
                        ImageKey = childNode.ImageId.ToString(),
                        SelectedImageKey = childNode.ImageId.ToString()
                    };

                    // Si el nodo puede tener subnodos, añadir un placeholder "Cargando..."
                    if (childNode.HasChildren)
                    {
                        childTreeNode.Nodes.Add(new TreeNode("Cargando..."));
                    }

                    parentNode.Nodes.Add(childTreeNode);
                }

                // Agregar documentos como subnodos
                foreach (var doc in documents)
                {
                    TreeNode documentNode = new TreeNode
                    {
                        Text = doc.Name,
                        Tag = doc.Id,
                        ImageKey = doc.ImageId.ToString(),
                        SelectedImageKey = doc.ImageId.ToString()
                    };

                    parentNode.Nodes.Add(documentNode);
                }
            }
        }


        // Añade una ImageList para asociar las imágenes con los nodos del TreeView
        private void InitializeImageList(Dictionary<int, Image> images)
        {
            try
            {
                //  Limpiar cualquier instancia previa
                if (_imageList != null)
                {
                    _imageList.Dispose();
                    _imageList = null;
                }

                _imageList = new ImageList();
                _imageList.ImageSize = new Size(32, 32);

                Console.WriteLine($"Total de imágenes a procesar: {images.Count}");

                foreach (var kvp in images)
                {
                    if (kvp.Value == null)
                    {
                        Console.WriteLine($"Imagen nula para la clave: {kvp.Key}");
                        continue;
                    }

                    try
                    {
                        /*
                        if (!IsImageValid(kvp.Value))
                        {
                            Console.WriteLine($"Imagen inválida para la clave: {kvp.Key}");
                            continue;
                        }
                        */

                        _imageList.Images.Add(kvp.Key.ToString(), ResizeImage(kvp.Value, _imageList.ImageSize));
                        // _imageList.Images.Add(kvp.Key.ToString(), kvp.Value);
                        Console.WriteLine($"Imagen añadida para la clave: {kvp.Key}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al añadir la imagen para la clave {kvp.Key}: {ex.Message}");
                    }
                }

                treeView.ImageList = _imageList;
                Console.WriteLine("ImageList inicializado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar el ImageList: {ex.Message}");
                MessageBox.Show($"Error al inicializar el ImageList: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Establece un tamaño predeterminado para la imagen
        private Image ResizeImage(Image img, Size size)
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return bmp;
        }

        // Validar la imagen
        private bool IsImageValid(Image img)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, img.RawFormat);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        // Sobrescribir OnShown
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Actualizar la duración del inicio de la aplicación
            _mainApp.StartAppEnd();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Confirmar si el usuario realmente quiere salir
            var result = MessageBox.Show(
                "¿Está seguro de que desea cerrar la aplicación?",
                "Confirmar salida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Cierra la sesión actual de SQL Server y libera recursos
                _mainApp.EndApp();

                // Cierra la aplicación
                Application.Exit();
            }

        }

        // Usar el evento Paint del formulario para dibujar un rectángulo sobre el área del divisor
        // para colorar el Split en color Blue
        private void splitContainer1_Paint(object sender, PaintEventArgs e)
        {
            // Obtener las dimensiones del divisor
            int splitterWidth = splitContainer1.SplitterWidth;
            int splitterPosition = splitContainer1.SplitterDistance;

            // Crear un pincel con el color deseado
            using (Brush brush = new SolidBrush(Color.Blue)) // Cambia el color aquí
            {
                // Dibujar un rectángulo sobre el divisor
                if (splitContainer1.Orientation == Orientation.Vertical)
                {
                    e.Graphics.FillRectangle(brush, splitterPosition, 0, splitterWidth, splitContainer1.Height);
                }
                else
                {
                    e.Graphics.FillRectangle(brush, 0, splitterPosition, splitContainer1.Width, splitterWidth);
                }
            }
        }

        // Configurar la vista de treeView para que se vea más agradable y moderno
        private void ConfigureTreeView()
        {
            // Configurar las propiedades básicas del TreeView
            treeView.ItemHeight = 36; // Altura de los elementos
            treeView.ShowLines = true; // Mostrar líneas entre nodos
            treeView.HideSelection = false; // Mostrar selección incluso cuando el TreeView pierde el foco
            treeView.BackColor = Color.WhiteSmoke; // Fondo moderno
            treeView.ForeColor = Color.DarkSlateGray; // Color del texto

            // Estilo de fuente ajustado para un aspecto más limpio
            treeView.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // Configuración del ImageList (asumiendo que ya está inicializado)
            if (_imageList != null)
            {
                _imageList.ImageSize = new Size(32, 32); // Asegúrate de que las imágenes tengan el tamaño correcto
                treeView.ImageList = _imageList;
            }

            // Configuración adicional si es necesario
        }
    }
}

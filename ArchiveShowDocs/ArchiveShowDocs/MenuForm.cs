using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveShowDocs
{
    public partial class MenuForm : Form
    {
        private MainApp _mainApp;
        private DatabaseService _databaseService;
        private ImageList _imageList;

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
            // Llama al método para cargar los datos
            LoadTreeView();
        }

        private void LoadTreeView()
        {
            try
            {
                // Cargar los datos desde la base de datos
                List<TreeNodeModel> nodes = _databaseService.LoadTreeData();

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

                AddChildNodes(treeNode, rootNode.Id, nodes);
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

        // Añade una ImageList para asociar las imágenes con los nodos del TreeView
        private void InitializeImageList(Dictionary<int, Image> images)
        {
            try
            {
                _imageList = new ImageList();
                foreach (var kvp in images)
                {
                    _imageList.Images.Add(kvp.Key.ToString(), kvp.Value);
                }

                treeView.ImageList = _imageList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar el ImageList: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            treeView.ItemHeight = 24; // Altura de los elementos
            treeView.ShowLines = true; // Mostrar líneas entre nodos
            treeView.HideSelection = false; // Mostrar selección incluso cuando el TreeView pierde el foco
            treeView.BackColor = Color.WhiteSmoke; // Fondo moderno
            treeView.ForeColor = Color.DarkSlateGray; // Color del texto

            // Estilo de fuente ajustado para un aspecto más limpio
            treeView.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // Configuración del ImageList (asumiendo que ya está inicializado)
            if (_imageList != null)
            {
                treeView.ImageList = _imageList;
            }

            // Configuración adicional si es necesario
        }
    }
}

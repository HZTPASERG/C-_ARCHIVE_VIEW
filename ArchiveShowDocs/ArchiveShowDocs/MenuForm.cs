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

        public MenuForm(MainApp mainApp)
        {
            InitializeComponent();
            _mainApp = mainApp ?? throw new ArgumentNullException(nameof(mainApp));
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
    }
}

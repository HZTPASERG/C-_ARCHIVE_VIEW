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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Cierra la aplicación
            Application.Exit();
        }
    }
}

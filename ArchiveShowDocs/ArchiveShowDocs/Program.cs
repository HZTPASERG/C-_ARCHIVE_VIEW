using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveShowDocs
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Crear instancia principal de la aplicación
            MainApp app = new MainApp();

            // Iniciar la aplicación
            if (app.StartApp())
            {
                Application.Run(new MenuForm(app)); // Abrir formulario principal
            }
        }
    }
}

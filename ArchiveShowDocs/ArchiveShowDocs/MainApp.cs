using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ArchiveShowDocs
{
    public class MainApp
    {
        public string UserName { get; private set; }
        public string UserRole { get; private set; }
        public int SessionId { get; private set; }
        public DatabaseManager Database { get; private set; }
        public UserCatalog UserCatalog { get; private set; }

        public MainApp()
        {
            Database = new DatabaseManager();
        }

        /// <summary>
        /// Inicia la aplicación con la validación del usuario.
        /// </summary>
        public bool StartApp()
        {
            // Mostrar formulario de inicio de sesión
            using (LoginForm loginForm = new LoginForm(Database))
            {
                if (loginForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return false; // Usuario canceló el inicio de sesión
                }

                // Obtener datos del usuario desde el formulario
                UserName = loginForm.Username;
                UserRole = loginForm.UserRole;

                // Configurar directorio del usuario
                UserCatalog = new UserCatalog(AppConstants.DirectoryOfConfig, UserName);

                if (!UserCatalog.EnsureUserCatalog())
                {
                    System.Windows.Forms.MessageBox.Show(
                        "El directorio del usuario está en uso o no se pudo configurar.",
                        "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error
                    );
                    return false;
                }

                // Crear sesión en la base de datos
                SessionId = Database.StartSession(UserName);
                if (SessionId <= 0)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "No se pudo iniciar la sesión en la base de datos.",
                        "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error
                    );
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Termina la sesión del usuario y cierra la aplicación.
        /// </summary>
        public void EndApp()
        {
            Database.EndSession(SessionId);
            UserCatalog.ReleaseLock();
        }
    }
}

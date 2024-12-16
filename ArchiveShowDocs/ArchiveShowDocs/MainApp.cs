using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace ArchiveShowDocs
{
    public class MainApp
    {
        public int UserId { get; private set; }
        public string UserName { get; private set; }
        public string UserRole { get; private set; }
        public string UserFullName { get; private set; }
        public int SessionId { get; private set; }
        public DatabaseManager Database { get; private set; }
        public UserCatalog UserCatalog { get; private set; }
        public string LocalPath { get; private set; }

        public MainApp()
        {
            Database = new DatabaseManager();
            LocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.DirectoryOfConfig);
            Debug.WriteLine("LocalPath: " + LocalPath);
        }

        /// <summary>
        /// Inicia la aplicación con la validación del usuario.
        /// </summary>
        public bool StartApp()
        {
            // Cerrar cualquier conexión previa con el servidor
            Database.EndSession(UserId);

            // Configurar el título de la ventana principal
            string oldWindowTitle = Application.ProductName;
            string newWindowTitle = AppConstants.NameMainWindow;

            // Configurar el manejador global de errores
            Application.ThreadException += (sender, e) =>
            {
                HandleError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                HandleError((Exception)e.ExceptionObject);
            };

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
                UserId = loginForm.UserId; // Asegúrate de que LoginForm tenga una propiedad UserId para almacenar este valor
                UserFullName = loginForm.UserFullName;

                newWindowTitle = newWindowTitle + " | Користувач: " + UserFullName;

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
                SessionId = Database.StartSession(UserId);
                if (SessionId <= 0)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "No se pudo iniciar la sesión en la base de datos.",
                        "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error
                    );
                    return false;
                }
            }

            // Verificar si otra instancia de la aplicación ya está en ejecución
            if (IsApplicationAlreadyRunning())
            {
                MessageBox.Show(
                    "¡La aplicación ya está en ejecución!",
                    newWindowTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return false;
            }

            // Crear y mostrar el formulario principal después del inicio de sesión
            MenuForm mainForm = new MenuForm(this);
            mainForm.Text = newWindowTitle; // Configurar el título aquí
            Application.Run(mainForm); // Iniciar el formulario principal

            return true;
        }

        /// <summary>
        /// Termina la sesión del usuario y cierra la aplicación.
        /// </summary>
        public void EndApp()
        {
            Database.EndSession(UserId);
            UserCatalog.ReleaseLock();
        }

        /// <summary>
        /// Manejador global de errores.
        /// </summary>
        private void HandleError(Exception ex)
        {
            MessageBox.Show(
                $"Ocurrió un error inesperado:\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        /// <summary>
        /// Verifica si otra instancia de la aplicación ya está en ejecución.
        /// </summary>
        private bool IsApplicationAlreadyRunning()
        {
            string lockFilePath = Path.Combine(LocalPath, AppConstants.LockFileName);
            Debug.WriteLine("lockFilePath: " + lockFilePath);
            try
            {
                // Intentar abrir el archivo de bloqueo en modo exclusivo
                using (FileStream fs = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    // Si se puede abrir el archivo, no hay otra instancia en ejecución
                    return false;
                }
            }
            catch (IOException)
            {
                // Si se lanza una excepción, significa que el archivo está bloqueado por otra instancia
                return true;
            }
        }
    }
}

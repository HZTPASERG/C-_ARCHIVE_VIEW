﻿using System;
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
        public string EncodedPassword { get; private set; }
        public string UserRole { get; private set; }
        public string UserFullName { get; private set; }
        public int SessionId { get; private set; }
        public DatabaseManager DatabaseManagerTemp { get; private set; }
        public DatabaseManager DatabaseManagerPersistent { get; private set; }
        public UserCatalog UserCatalog { get; private set; }
        public string LocalPath { get; private set; }
        public string WindowTitle { get; private set; }

        public MainApp()
        {
            WindowTitle = AppConstants.NameMainWindow.Replace("\"", "\"\""); ;
            DatabaseManagerTemp = new DatabaseManager();
            bool connectionOpened = DatabaseManagerTemp.OpenTemporaryConnection(
                AppConstants.SqlServer, 
                AppConstants.DatabaseName, 
                AppConstants.SqlLogin, 
                AppConstants.LoginPassword, 
                ""
            );
            if (!connectionOpened)
            {
                throw new InvalidOperationException("No se pudo establecer la conexión temporal.");
            }
            LocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.DirectoryOfConfig);
            Debug.WriteLine("LocalPath: " + LocalPath);
        }

        /// <summary>
        /// Inicia la aplicación con la validación del usuario.
        /// </summary>
        public bool StartApp()
        {
            // Configurar el título de la ventana principal
            string oldWindowTitle = Application.ProductName;
            //string newWindowTitle = AppConstants.NameMainWindow;

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
            using (LoginForm loginForm = new LoginForm(DatabaseManagerTemp))
            {
                if (loginForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return false; // Usuario canceló el inicio de sesión
                }

                // Obtener datos del usuario desde el formulario
                UserName = loginForm.Username;
                EncodedPassword = loginForm.EncodedPassword;
                UserRole = loginForm.UserRole;
                UserId = loginForm.UserId; // Asegúrate de que LoginForm tenga una propiedad UserId para almacenar este valor
                UserFullName = loginForm.UserFullName;

                WindowTitle = WindowTitle + " | Користувач: " + UserFullName;

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

                DatabaseManagerPersistent = new DatabaseManager(AppConstants.SqlServer, AppConstants.DatabaseName, AppConstants.SqlLoginAdmin, AppConstants.AdminPassword, WindowTitle);

                // Crear sesión en la base de datos
                SessionId = DatabaseManagerPersistent.StartSession(UserId);
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
                    WindowTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return false;
            }

            // Crear y mostrar el formulario principal después del inicio de sesión
            MenuForm mainForm = new MenuForm(this);
            mainForm.Text = WindowTitle; // Configurar el título aquí
            Application.Run(mainForm); // Iniciar el formulario principal

            // Liberar los recursos asociados a la conexión temporal
            DatabaseManagerTemp.DisposeTemp();

            return true;
        }

        /// <summary>
        /// Termina la sesión del usuario y cierra la aplicación.
        /// </summary>
        public void EndApp()
        {
            DatabaseManagerPersistent.EndSession(SessionId);
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

        /// <summary>
        /// Invoque al método UpdateStartAppDuration de DatabaseManager para actualizar el campo StartApp_Duration.
        /// </summary>
        public void StartAppEnd()
        {
            if (SessionId > 0)
            {
                bool success = DatabaseManagerPersistent.UpdateStartAppDuration(SessionId);
                if (!success)
                {
                    Console.WriteLine("No se pudo actualizar la duración del inicio de la aplicación en la base de datos.");
                }
            }
        }

    }
}

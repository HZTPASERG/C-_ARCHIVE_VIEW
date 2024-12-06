using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveShowDocs
{
    public class UserCatalog
    {
        private string _basePath;       // Ruta base para los directorios de usuarios
        private string _userLogin;      // Nombre de usuario
        private string _userPath;       // Ruta completa del directorio del usuario
        private string _lockFile;       // Archivo de bloqueo

        public UserCatalog(string basePath, string userLogin)
        {
            _basePath = basePath;
            _userLogin = userLogin;
            _userPath = Path.Combine(_basePath, _userLogin, "ARCHIV_USER"); // Ruta: basePath/userLogin/ARCHIV_USER
            _lockFile = Path.Combine(_userPath, $"{_userLogin}.lock");      // Archivo de bloqueo: userPath/userLogin.lock
        }

        public bool EnsureUserCatalog()
        {
            try
            {
                // 1. Crear el directorio del usuario si no existe
                if (!Directory.Exists(_userPath))
                {
                    Directory.CreateDirectory(_userPath);
                }

                // 2. Verificar si existe el archivo de bloqueo
                if (File.Exists(_lockFile))
                {
                    // Intentar abrir el archivo en modo exclusivo
                    using (FileStream fs = new FileStream(_lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // Archivo de bloqueo disponible
                    }
                }
                else
                {
                    // Crear el archivo de bloqueo si no existe
                    using (FileStream fs = new FileStream(_lockFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                    {
                        // Archivo de bloqueo creado
                    }
                }

                // 3. Verificar si existen los archivos de configuración y copiarlos si es necesario
                string defaultConfigFile = Path.Combine(_basePath, "DefaultConfig.ini");
                string userConfigFile = Path.Combine(_userPath, "Config.ini");

                if (!File.Exists(userConfigFile) && File.Exists(defaultConfigFile))
                {
                    File.Copy(defaultConfigFile, userConfigFile);
                }

                return true; // El directorio está disponible y listo
            }
            catch (IOException)
            {
                // Ocurre si otro proceso ya tiene el archivo de bloqueo abierto
                Console.WriteLine("El directorio del usuario está en uso por otra sesión.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar el directorio del usuario: {ex.Message}");
                return false;
            }
        }

        public void ReleaseLock()
        {
            try
            {
                // Eliminar el archivo de bloqueo al finalizar la sesión
                if (File.Exists(_lockFile))
                {
                    File.Delete(_lockFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al liberar el archivo de bloqueo: {ex.Message}");
            }
        }
    }
}

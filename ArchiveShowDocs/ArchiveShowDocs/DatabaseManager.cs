using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArchiveShowDocs
{
    public class DatabaseManager
    {
        private SqlConnection _connection;

        // Constructor sin parámetros
        public DatabaseManager()
        {
            // Opcional: Establecer una configuración predeterminada
            // Por ejemplo, puedes llamar a Connect con valores predeterminados aquí si lo necesitas.
        }

        // Constructor con parámetros para inicializar la conexión directamente
        public DatabaseManager(string server, string database, string username, string password)
        {
            string appName = $"{AppConstants.NameMainWindow} | Користувач: {username}";
            Connect(server, database, username, password, appName);
        }

        public bool Connect(string server, string database, string username, string password, string appName)
        {
            try
            {
                string connectionString = $"Server={server};Database={database};User Id={username};Password={password};Application Name={appName};";
                _connection = new SqlConnection(connectionString);
                _connection.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ValidateUser(string username, string password, out int userId, out string role, out bool pwdChangeRequired, out string fullName)
        {
            userId = 0;
            fullName = "";
            role = string.Empty;
            pwdChangeRequired = false;

            string query = "EXEC DATD..CurUser @username, @password";
            
            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fullName = reader["fullname"].ToString();
                            userId = Convert.ToInt32(reader["user_id"]);
                            if (userId == 0)
                            {
                                return false;
                            }

                            role = reader["role"].ToString();
                            pwdChangeRequired = reader["cfg_data"].ToString().Contains("CHANGEPWDONLOGIN=-1");
                            
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        public int StartSession(int UserId)
        {
            try
            {
                EnsureConnection();

                SqlCommand command = new SqlCommand("EXEC DATD..Admin_Session_ON @USER_ID", _connection);
                command.Parameters.AddWithValue("@USER_ID", UserId);
                
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar sesión: {ex.Message}");
                return -1;
            }
        }

        public void EndSession(int UserId)
        {
            if (_connection == null)
            {
                Console.WriteLine("La conexión a la base de datos no está inicializada.");
                return;
            }

            try
            {
                EnsureConnection();

                SqlCommand command = new SqlCommand("EXEC DATD..Admin_Session_OFF @USER_ID", _connection);
                command.Parameters.AddWithValue("@USER_ID", UserId);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al terminar sesión: {ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }

        private void EnsureConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("La conexión a la base de datos no está inicializada.");
            }

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }
        }

    }
}

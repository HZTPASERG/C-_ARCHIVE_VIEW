using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ArchiveShowDocs
{
    public class DatabaseManager
    {
        private SqlConnection _connection;

        public DatabaseManager()
        {
            // Configurar conexión inicial
            _connection = new SqlConnection("Server=.;Database=Master;User Id=sa;Password=yourpassword;");
        }

        public bool ValidateUser(string username, string password, out string role)
        {
            role = string.Empty;

            try
            {
                _connection.Open();
                SqlCommand command = new SqlCommand("EXEC DATD..CurUser @Username, @Password", _connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        role = reader["role"].ToString();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar usuario: {ex.Message}");
            }
            finally
            {
                _connection.Close();
            }

            return false;
        }

        public int StartSession(string username)
        {
            try
            {
                _connection.Open();
                SqlCommand command = new SqlCommand("EXEC DATD..Admin_Session_ON @Username", _connection);
                command.Parameters.AddWithValue("@Username", username);

                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar sesión: {ex.Message}");
                return -1;
            }
            finally
            {
                _connection.Close();
            }
        }

        public void EndSession(int sessionId)
        {
            try
            {
                _connection.Open();
                SqlCommand command = new SqlCommand("EXEC DATD..Admin_Session_OFF @SessionId", _connection);
                command.Parameters.AddWithValue("@SessionId", sessionId);

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
    }
}

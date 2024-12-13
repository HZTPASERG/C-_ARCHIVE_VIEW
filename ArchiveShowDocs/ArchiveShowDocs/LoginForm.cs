using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace ArchiveShowDocs
{
    public partial class LoginForm : Form
    {
        private readonly DatabaseManager _database;
        private int _loginAttempts;

        public int UserId { get; private set; }
        public string Username { get; private set; }
        public string UserRole { get; private set; }
        public bool PasswordChangeRequired { get; private set; }

        public LoginForm(DatabaseManager database)
        {
            InitializeComponent();
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _loginAttempts = AppConstants.LoginAttemptLimit;
            LoadPreviousUsername();
        }

        private void LoadPreviousUsername()
        {
            string iniFilePath = Path.Combine(AppConstants.DirectoryOfConfig, AppConstants.SqlIniFile);
            Username = IniFileHelper.ReadFromIniFile("Options", "LastLoginUser", iniFilePath);
            txtUsername.Text = Username;
            if (!string.IsNullOrEmpty(Username))
            {
                txtPassword.Focus();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "Por favor, ingrese su usuario y contraseña.";
                return;
            }

            string encodedPassword = PasswordHelper.EncodePassword(password);

            // Crear conexión temporal a la BD
            if (!_database.Connect(AppConstants.SqlServer, AppConstants.DatabaseName, AppConstants.SqlLogin, AppConstants.LoginPassword))
            {
                lblMessage.Text = "Error al conectar con el servidor.";
                return;
            }

            // Validar usuario
            if (!_database.ValidateUser(Username, encodedPassword, out int id, out string role, out bool pwdChangeRequired))
            {
                _loginAttempts--;
                lblMessage.Text = $"Usuario o contraseña incorrectos. Intentos restantes: {_loginAttempts}";

                if (_loginAttempts <= 0)
                {
                    MessageBox.Show("Se han agotado los intentos de inicio de sesión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
                return;
            }

            UserId = id;
            UserRole = role;
            PasswordChangeRequired = pwdChangeRequired;

            // Guardar el último usuario en el archivo INI
            string iniFilePath = Path.Combine(AppConstants.DirectoryOfConfig, AppConstants.SqlIniFile);
            IniFileHelper.SaveToIniFile("Options", "LastLoginUser", Username, iniFilePath);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
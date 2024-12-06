using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArchiveShowDocs
{
    public partial class LoginForm : Form
    {
        private readonly DatabaseManager _database;

        public string Username { get; private set; }
        public string UserRole { get; private set; }

        public LoginForm(DatabaseManager database)
        {
            InitializeComponent();
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }


        private void label1_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "Por favor, ingrese su usuario y contraseña.";
                return;
            }

            // Validar credenciales en la base de datos
            if (!_database.ValidateUser(username, password, out string role))
            {
                lblMessage.Text = "Usuario o contraseña incorrectos.";
                return;
            }

            // Credenciales válidas
            Username = username;
            UserRole = role;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
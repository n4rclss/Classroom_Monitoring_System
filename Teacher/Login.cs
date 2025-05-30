using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher
{
    public partial class Login : Form
    {
        private NetworkManager.NetworkManager _netManager;
        private AuthService.AuthService _authService;

        public Login(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;
            _authService = new AuthService.AuthService(netManager);
            //this.FormClosing += Login_FormClosing;
        }

        //private void Login_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    MessageBox.Show("Exiting...");
        //    Application.Exit();
        //
        //}
        private void Btn_login_Click(object sender, EventArgs e)
        {
            string username = textBox_username.Text;
            string password = textBox_password.Text;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var loginResult = _authService.Login(username, password);
                loginResult.Wait();

                if (loginResult.Result)
                {
                    MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Proceed to the next form or functionality  
                    var dashboard = new TeacherDashboard(_netManager);
                    dashboard.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Login failed. Please check your credentials.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during login: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

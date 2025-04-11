using Mysqlx.Session;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.Server.Database.Controller;
using App.Server.Database;

namespace Server.Forms
{
    public partial class LoginForm: Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private bool CheckInput(string teacherId, string password)
        {
            // check input format
            return true;
        }

        private bool AuthenticateUser(string teacherId, string password)
        {
            try
            {
                // Assuming you have a method to authenticate the user
                Teachers teachers = new Teachers(new DbHelper());
                bool result = teachers.Authenticate(Convert.ToInt32(teacherId), password);
                if (result)
                {
                    // User authenticated successfully
                    return true;
                }
                else
                {
                    // Authentication failed
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private void btn_Login_Click(object sender, EventArgs e)
        {
            string teacherId = textBox_ID.Text;
            string password = textBox_Password.Text;

            if (string.IsNullOrEmpty(teacherId) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both Teacher ID and Password.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!CheckInput(teacherId, password))
            {
                MessageBox.Show("Invalid Teacher ID or Password.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };

            if (AuthenticateUser(teacherId, password))
            {
                Form dashboard = new Dashboard();
                dashboard.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Login failed. Please try again.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

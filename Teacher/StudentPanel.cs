using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;

namespace Teacher.StudentPanel
{
    public class StudentPanel
    {
        public string studentName { get; set; }

        public string username { get; set; }
        public string mssv { get; set; }

        private NetworkManager.NetworkManager _netManager;

        public StudentPanel(NetworkManager.NetworkManager netManager, string username, string studentName, string mssv)
        {
            _netManager = netManager;
            this.studentName = studentName;
            this.username = username;
            this.mssv = mssv;
        }

        public Panel CreateStudentPanel()
        {
            Panel studentPanel = new Panel
            {
                Width = 360,
                Height = 120,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(230, 240, 255),
                BorderStyle = BorderStyle.FixedSingle
            };


            Label nameLabel = new Label
            {
                Text = studentName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = false,
                Width = 130,
                Height = 22,
                Top = 5,
                Left = 5,
                ForeColor = Color.DarkBlue
            };

            Label usernameLabel = new Label
            {
                Text = $"Username: {username}",
                Font = new Font("Segoe UI", 11),
                AutoSize = false,
                Width = 130,
                Height = 20,
                Top = 30,
                Left = 5,
                ForeColor = Color.DimGray
            };

            Label mssvLabel = new Label
            {
                Text = $"MSSV: {mssv}",
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Width = 130,
                Height = 20,
                Top = 50,
                Left = 5
            };

            
            Button btnDelete = new Button();
            btnDelete.Text = "Xóa";
            btnDelete.Size = new Size(80, 25);
            btnDelete.Location = new Point(10, 85);
            btnDelete.Click += (s, e) =>
            {
                // Xóa học sinh
                MessageBox.Show($"Đã xóa học sinh {studentName}");
            };

            
            Button btnWatch = new Button();
            btnWatch.Text = "Quan sát màn hình";
            btnWatch.Size = new Size(120, 25);
            btnWatch.Location = new Point(100, 85);
            btnWatch.Click += (s, e) =>
            {
                if (Globals.roomID.Length > 0)
                {
                    Streaming cap = new Streaming(_netManager, this.username);
                    cap.Show();
                }
                else
                {
                    MessageBox.Show("You have to create room first");
                }

            };

            
            Button btnAlert = new Button();
            btnAlert.Text = "Gửi tin nhắn";
            btnAlert.Size = new Size(100, 25);
            btnAlert.Location = new Point(230, 85);
            btnAlert.Click += (s, e) =>
            {
                // Gửi tin nhắn alert đến học sinh
                MessageBox.Show($"Đã gửi tin nhắn đến học sinh {studentName}");
            };

            studentPanel.Controls.Add(nameLabel);
            studentPanel.Controls.Add(usernameLabel);
            studentPanel.Controls.Add(mssvLabel);
            studentPanel.Controls.Add(btnDelete);
            studentPanel.Controls.Add(btnWatch);
            studentPanel.Controls.Add(btnAlert);


            return studentPanel;
        }
    }
}

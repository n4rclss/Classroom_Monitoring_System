using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher
{
    public partial class TeacherDashboard : Form
    {
        private NetworkManager.NetworkManager _netManager;
        public TeacherDashboard(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;
        }


        // REFRESH HANDLER
        private async void refresh_btt_Click(object sender, EventArgs e)
        {
            await refresh();
        }
        
        private Panel CreateStudentPanel(string username, string studentName, string mssv)
        {
            Panel studentPanel = new Panel
            {
                Width = 140,
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

          

            studentPanel.Controls.Add(nameLabel);
            studentPanel.Controls.Add(usernameLabel);
            studentPanel.Controls.Add(mssvLabel);
        

            return studentPanel;
        }

        public async Task<bool> refresh()
        {
            try
            {
                if (Room_ID_tb == null || Room_ID_tb.Text.Length == 0)
                    MessageBox.Show("ID room must be filled!");

                var create_room_message = new Teacher.MessageModel.Refresh
                {
                    room_id = Room_ID_tb.Text.ToString(),
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(create_room_message);
                if (string.IsNullOrEmpty(responseData))
                {
                    return false; // No response from server
                }
                JsonDocument doc = JsonDocument.Parse(responseData);
                var root = doc.RootElement;

                var status = root.GetProperty("status").GetString();
                var message = root.GetProperty("message");

                switch (status)
                {
                    case "success":
                        MessageBox.Show(message.ToString());
                        if (message.TryGetProperty("participants", out JsonElement participantsElement))
                        {
                            List<string> participants = new List<string>();
                            statusPanel.Controls.Clear(); // Clear old data before adding new ones
                           
                            foreach (JsonElement participant in participantsElement.EnumerateArray())
                            {
                                MessageBox.Show(participant.ToString());
                                string username = participant.GetProperty("username").GetString();
                                string studentName = participant.GetProperty("student_name").GetString();
                                string mssv = participant.GetProperty("mssv").GetString();

                                Panel studentBlock = CreateStudentPanel(username, studentName, mssv);
                                statusPanel.Controls.Add(studentBlock);
                            }


                        }

                        return true;

                    case "error":
                        MessageBox.Show("Error: " + message);
                        return false;

                    default:
                        MessageBox.Show("Unknown response type.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false; // Return false in case of any exception
        }







        private void Send_message_to_all_btt_Click(object sender, EventArgs e)
        {
            if (Globals.roomID.Length > 0)
            {
                Notify sendForm = new Notify(_netManager);
                sendForm.Show();
            }
            else
            {
                MessageBox.Show("You have to create room first");
            }

        }

        private async void Create_Click(object sender, EventArgs e)
        {
            await Create_room();
        }


        public async Task<bool> Create_room()
        {
            try
            {
                var create_room_message = new Teacher.MessageModel.Create_room
                {
                    room_id = Room_ID_tb.Text.ToString(),
                    teacher = Globals.UsernameGlobal,
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(create_room_message);
                if (string.IsNullOrEmpty(responseData))
                {
                    return false; // No response from server
                }
                JsonDocument doc = JsonDocument.Parse(responseData);
                var status = doc.RootElement.GetProperty("status").GetString();
                var message = doc.RootElement.GetProperty("message").GetString();
                /* Handle message types */
                switch (status)
                {
                    case "success":
                        {
                            Globals.roomID = Room_ID_tb.Text.ToString();
                            MessageBox.Show(message);
                            return true;
                        }
                    case "error":
                        return false;
                    default:
                        // Handle unknown message type
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false; // Return false in case of any exception
        }

        private async void TeacherDashboard_FormClosed(object sender, FormClosedEventArgs e)
        {
            await Log_out();
        }

        public async Task<bool> Log_out()
        {
            try
            {
                var create_room_message = new Teacher.MessageModel.Log_out
                {
                    room_id = Room_ID_tb.Text.ToString(),
                    teacher = Globals.UsernameGlobal,
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(create_room_message);
                if (string.IsNullOrEmpty(responseData))
                {
                    return false; // No response from server
                }
                JsonDocument doc = JsonDocument.Parse(responseData);
                var status = doc.RootElement.GetProperty("status").GetString();
                var message = doc.RootElement.GetProperty("message").GetString();
                /* Handle message types */
                switch (status)
                {
                    case "success":
                        {
                            MessageBox.Show(message);
                            return true;
                        }
                    case "error":
                        return false;
                    default:
                        // Handle unknown message type
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false; // Return false in case of any exception
        }
    }
}

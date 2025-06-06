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
using _StudentPanel = Teacher.StudentPanel.StudentPanel;


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
                                //MessageBox.Show(participant.ToString());
                                string username = participant.GetProperty("username").GetString();
                                string studentName = participant.GetProperty("student_name").GetString();
                                string mssv = participant.GetProperty("mssv").GetString();

                                // Khúc này để tạo panel cho từng học sinh
                                _StudentPanel studentPanel = new _StudentPanel(_netManager, username, studentName, mssv);
                                Panel panel = studentPanel.CreateStudentPanel();
                                statusPanel.Controls.Add(panel);
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

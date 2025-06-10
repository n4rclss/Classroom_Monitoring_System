using System;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Student;

namespace Student
{
    public partial class Dashboard : Form
    {
        private NetworkManager.NetworkManager _netManager;

        public Dashboard(NetworkManager.NetworkManager netManger)
        {
            InitializeComponent();
            _netManager = netManger;
            this.DisconnectBtn.Enabled = false;
            //this.FormClosing += Client_FormClosing;
            _netManager.OnMessageReceived = UpdateLogBox;

            // Update status label
            UpdateStatus(false);
        }

        private void UpdateStatus(bool isConnected)
        {
            if (isConnected)
            {
                statusLabel.Text = "Status: Connected";
                statusLabel.ForeColor = Color.FromArgb(39, 174, 96); // Green color
                ConnectBtn.Enabled = false;
                DisconnectBtn.Enabled = true;
            }
            else
            {
                statusLabel.Text = "Status: Not connected";
                statusLabel.ForeColor = Color.FromArgb(41, 128, 185); // Blue color
                ConnectBtn.Enabled = true;
                DisconnectBtn.Enabled = false;
            }
        }

        private void UpdateLogBox(string message)
        {
            if (logBox_tb.InvokeRequired)
            {
                logBox_tb.Invoke(new Action(() =>
                {
                    logBox_tb.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}" + Environment.NewLine);
                    logBox_tb.SelectionStart = logBox_tb.TextLength;
                    logBox_tb.ScrollToCaret();
                }));
            }
            else
            {
                logBox_tb.AppendText($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}" + Environment.NewLine);
                logBox_tb.SelectionStart = logBox_tb.TextLength;
                logBox_tb.ScrollToCaret();
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DisconnectBtn.Enabled)
            {
                e.Cancel = true;
                MessageBox.Show("Please disconnect before closing the application.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //HANDLE CONNECT
        private async void ConnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate input fields
                if (string.IsNullOrWhiteSpace(usernameBox.Text))
                {
                    MessageBox.Show("Please enter your name.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    usernameBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(mssvBox.Text))
                {
                    MessageBox.Show("Please enter your student ID.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    mssvBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(roomidBox.Text))
                {
                    MessageBox.Show("Please enter a Room ID.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    roomidBox.Focus();
                    return;
                }

                // Show connecting status
                ConnectBtn.Text = "Connecting...";
                ConnectBtn.Enabled = false;

                if (await Join_room())
                {
                    UpdateStatus(true);
                    await Listen_passive();
                }
                else
                {
                    ConnectBtn.Text = "Connect";
                    ConnectBtn.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                ConnectBtn.Text = "Connect";
                ConnectBtn.Enabled = true;
                logBox_tb.AppendText("Error: " + ex.Message + "\r\n");
                UpdateStatus(false);
            }
        }

        public async Task<bool> Join_room()
        {
            try
            {
                var create_room_message = new Student.MessageModel.Join_room
                {
                    room_id = roomidBox.Text,
                    mssv = mssvBox.Text,
                    student_name = usernameBox.Text,
                    Username = Globals.UsernameGlobal,
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    UpdateLogBox("Connecting to server...");
                }

                string responseData = await _netManager.ProcessSendMessage(create_room_message);
                if (string.IsNullOrEmpty(responseData))
                {
                    UpdateLogBox("No response from server.");
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
                            UpdateLogBox($"Connected: {message}");
                            MessageBox.Show(message);
                            return true;
                        }
                    case "error":
                        {
                            UpdateLogBox($"Error: {message}");
                            MessageBox.Show(message);
                            return false;
                        }
                    default:
                        // Handle unknown message type
                        UpdateLogBox("Unknown response type from server.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                UpdateLogBox($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Listen_passive()
        {
            try
            {
                await _netManager.ListeningPassivelyForever();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                UpdateLogBox($"Listening error: {ex.Message}");
                UpdateStatus(false);
                return false;
            }
            return true;
        }

        private async void DisconnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Show disconnecting status
                DisconnectBtn.Text = "Disconnecting...";
                DisconnectBtn.Enabled = false;

                // Here you would add your disconnect logic
                UpdateLogBox("Disconnecting from server...");

                // Set the UI to disconnected state
                await Task.Delay(500); // Simulate disconnection process
                UpdateStatus(false);
                DisconnectBtn.Text = "Disconnect";
                UpdateLogBox("Disconnected from server.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Client Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisconnectBtn.Text = "Disconnect";
                DisconnectBtn.Enabled = true;
            }
        }
    }
}

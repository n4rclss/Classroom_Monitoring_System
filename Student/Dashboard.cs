using System;
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
        }
        private void UpdateLogBox(string message)
        {
            if (logBox_tb.InvokeRequired)
            {
                logBox_tb.Invoke(new Action(() =>
                {
                    logBox_tb.AppendText(message + Environment.NewLine);
                }));
            }
            else
            {
                logBox_tb.AppendText(message + Environment.NewLine);
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
                if (await Join_room())
                    await Listen_passive();

            }
            catch (Exception ex)
            {
                logBox_tb.AppendText("Error: " + ex.Message + "\r\n");
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
                        {
                            MessageBox.Show(message);
                            return false;
                        }
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

        public async Task<bool> Listen_passive()
        {
            try
            {
                await _netManager.ListeningPassivelyForever();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
            return true;
        }













        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
           
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Client Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

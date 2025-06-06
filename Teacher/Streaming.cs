using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


namespace Teacher
{

    public partial class Streaming : Form
    {
        private NetworkManager.NetworkManager _netManager;
        private string _studentUsername;
        public Streaming(NetworkManager.NetworkManager netManager, string username)
        {
            InitializeComponent();
            _netManager = netManager;
            _studentUsername = username;  // Store it
   
        }


        private void ShowRdpViewer(string invitationString)
        {

            var viewerForm = new Form();
            viewerForm.Text = "RDP Viewer";
            viewerForm.Size = new Size(800, 600);

            var rdpViewer = new AxRDPCOMAPILib.AxRDPViewer();
            rdpViewer.Dock = DockStyle.Fill;

            viewerForm.Controls.Add(rdpViewer);
            viewerForm.Show();

            rdpViewer.Connect(invitationString, "", "");
        }


        public async Task<bool> Capture_remote_screen()
        {
            try
            {
                MessageBox.Show("Capture_remote_screen from teacher");
                var capture_screen_message = new Teacher.MessageModel.Streaming_message
                {
                    target_username = _studentUsername, 

                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(capture_screen_message);
                if (string.IsNullOrEmpty(responseData))
                {
                    return false; // No response from server
                }

                JsonDocument doc = JsonDocument.Parse(responseData);
                var status = doc.RootElement.GetProperty("status").GetString();
                var message = doc.RootElement.GetProperty("message").GetString();
                if (status == "success")
                {

                    string response_Invitation = await _netManager.ListenResponsesAsync(_netManager._cts.Token);
                    doc = JsonDocument.Parse(response_Invitation);
                    var Invitation = doc.RootElement.GetProperty("image_data").GetString();
                    MessageBox.Show($"Message from teacher after receving response capture screen {Invitation.Length.ToString()}");
                    //MessageBox.Show("Token length received from teacher: ",Invitation.Length.ToString());
                    //ShowRdpViewer(Invitation);
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false; // Return false in case of any exception
        }


     

        private void Capture_screen_FormClosed(object sender, FormClosedEventArgs e)
        {
            //axRDPViewer1.Disconnect();
        }

        private void Capture_screen_Load(object sender, EventArgs e)
        {

        }

        private async void Streaming_Load(object sender, EventArgs e)
        {
            await Capture_remote_screen();

        }
    }
}

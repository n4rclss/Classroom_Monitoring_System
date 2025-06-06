using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Teacher
{

    public partial class Streaming : Form
    {
        private NetworkManager.NetworkManager _netManager;
        private string _student_username;
        public  Streaming(NetworkManager.NetworkManager netManager,string student_username)
        {
            InitializeComponent();
            _netManager = netManager;
            _student_username = student_username;
            this.Load += Streaming_Load; // defer the async method to the Load event
        }


        private async void Streaming_Load(object sender, EventArgs e)
        {
            await Capture_remote_screen();
        }

        private void ShowRdpViewer(string invitationString)
        {
            Thread staThread = new Thread(() =>
            {
                var viewerForm = new Form
                {
                    Text = "RDP Viewer",
                    Size = new Size(800, 600)
                };

                var rdpViewer = new AxRDPCOMAPILib.AxRDPViewer();

                ((ISupportInitialize)rdpViewer).BeginInit();
                viewerForm.Controls.Add(rdpViewer);
                rdpViewer.Dock = DockStyle.Fill;
                ((ISupportInitialize)rdpViewer).EndInit();

                viewerForm.Shown += (s, e) =>
                {
                    try
                    {
                        rdpViewer.Connect(invitationString, "", "");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"RDP Connect error: {ex.Message}");
                    }
                };

                Application.Run(viewerForm);
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
        }




        public async Task<bool> Capture_remote_screen()
        {
            try
            {
                var capture_screen_message = new Teacher.MessageModel.Streaming_message
                {
                    target_username = _student_username,
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
                    this.Invoke((MethodInvoker)(() => ShowRdpViewer(Invitation)));

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

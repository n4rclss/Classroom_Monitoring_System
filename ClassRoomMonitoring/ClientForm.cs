using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using RDPCOMAPILib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ClassRoomMonitoring
{

    public partial class ClientForm : Form
    {

        
        RDPSession RDPx = new RDPSession();
        private TcpClient tcpClient = null;
        private NetworkStream networkStream;
        private StreamWriter streamWriter;
        private StreamReader streamReader;

        public class AlertForm : Form
        {
            private Label messageLabel;
            public AlertForm(string message)
            {
                messageLabel = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(messageLabel);
                Size = new Size(300, 150); // Adjust the size as needed
                StartPosition = FormStartPosition.CenterScreen;
            }
        }


        private void Incoming(object Guest)
        {
            IRDPSRAPIAttendee MyGuest = (IRDPSRAPIAttendee)Guest;
            MyGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE;
        }

        public ClientForm()
        {
            InitializeComponent();
            this.DisconnectBtn.Enabled = false;
            this.FormClosing += Client_FormClosing;
        }
        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DisconnectBtn.Enabled)
            {
                e.Cancel = true;
                MessageBox.Show("Please disconnect before closing the application.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Serving listen to "sendmessage" from server 
        private void ListenForServerMessages()
        {

            try
            {
                while (tcpClient.Connected)
                {
                    string serverMessage = streamReader.ReadLine();
                    if (!string.IsNullOrEmpty(serverMessage))
                    {
                        // Update UI safely since this is a background thread
                        this.Invoke((MethodInvoker)delegate {
                            AlertForm alert = new AlertForm(serverMessage);
                            alert.ShowDialog(); // Show as a modal dialog
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate {
                    logBox.AppendText("Error reading from server: " + ex.Message + "\r\n");
                });
            }
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            RDPx.OnAttendeeConnected += Incoming;
            // this will trigger event handler whenever a connection appears.
            RDPx.Open();
            // RDP will act like a asynchronous under the hood with its own thread.
            // Start background listener
           
            ConnectBtn.Enabled = false;
            DisconnectBtn.Enabled = true;
            usernameBox.Enabled = false;
            MssvBox.Enabled = false;

            try
            {
                if (!isValid(ipBox.Text, portBox.Text))
                {
                    logBox.AppendText("Invalid IP or Port.\r\n");
                    return;
                }

                if (string.IsNullOrEmpty(usernameBox.Text))
                {
                    logBox.AppendText("Please enter a username.\r\n");
                    return;
                }

                if (string.IsNullOrEmpty(MssvBox.Text))
                {
                    logBox.AppendText("Please enter your student ID (MSSV).\r\n");
                    return;
                }

                string ip = ipBox.Text;
                string port = portBox.Text;

                tcpClient = new TcpClient(ip, int.Parse(port));

                Task.Run(() => ListenForServerMessages());
                // start background listener :vv aiming for listening to "send message" function in server .

                networkStream = tcpClient.GetStream();
                streamWriter = new StreamWriter(networkStream, Encoding.UTF8);
                streamReader = new StreamReader(networkStream, Encoding.UTF8);

                IRDPSRAPIInvitation Invitation = RDPx.Invitations.CreateInvitation("StudentConnection", "group_name", "", 1);
                // 3rd arg is password for connect, 4th is the number of connection can be accepted, 1st and 2nd does not matter much.

                // Ghép tên + MSSV gửi lên server + token invitation
                string userInfo = $"{usernameBox.Text.Trim()}|{MssvBox.Text.Trim()}|{Invitation.ConnectionString}";
                streamWriter.WriteLine(userInfo);
                streamWriter.Flush();

                logBox.AppendText("Connected to server!\r\n");
            }
            catch (Exception ex)
            {
                logBox.AppendText("Error: " + ex.Message + "\r\n");
            }
        }

        private bool isValid(string ip, string port)
        {
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("IP cannot be empty.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrEmpty(port))
            {
                MessageBox.Show("Port cannot be empty.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Invalid IP address format.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!int.TryParse(port, out int portNumber) || portNumber < 0 || portNumber > 65535)
            {
                MessageBox.Show("Port must be a number between 0 and 65535.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            DisconnectBtn.Enabled = false;
            ConnectBtn.Enabled = true;
            usernameBox.Enabled = true;
            MssvBox.Enabled = true;

            try
            {
                if (streamWriter != null)
                {
                    streamWriter.WriteLine("disconnected");
                    streamWriter.Flush();
                }

                streamWriter?.Close();
                streamReader?.Close();
                networkStream?.Close();
                tcpClient?.Close();

                logBox.AppendText("Disconnected from server.\r\n");
                ConnectBtn.Enabled = true;
                usernameBox.Enabled = true;
                DisconnectBtn.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Client Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.IO;
using RDPCOMAPILib;

namespace ClassRoomMonitoring
{
    public partial class ServerForm : Form
    {
        private Socket listenerSocket;
        private Thread listenThread;
        private bool isListening = false;

        public ServerForm()
        {
            InitializeComponent();
            this.DisconnectBtn.Enabled = false;
            this.FormClosing += ServerForm_FormClosing;
        }

        // Xử lý sự kiện khi form đang đóng
        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DisconnectBtn.Enabled)
            {
                e.Cancel = true;
                MessageBox.Show("Please disconnect before closing the application.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Xử lý sự kiện khi nhấn nút "Listen"
        private void ListenBtn_Click(object sender, EventArgs e)
        {
            ListenBtn.Enabled = false;
            DisconnectBtn.Enabled = true;
            StartListen(sender, e);
        }

        // Bắt đầu lắng nghe kết nối từ client
        private void StartListen(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            isListening = true;
            if (!isValid(ipBox.Text, portBox.Text))
            {
                ListenBtn.Enabled = true;
                return;
            }
            listenThread = new Thread(StartServer);
            listenThread.Start();
        }

        // Send message to client
        private void SendMessage(ClientInfo client)
        {
            using (Form inputForm = new Form())
            {
                inputForm.Text = $"Send Message to {client.DisplayName}";
                inputForm.Size = new Size(400, 200);
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.StartPosition = FormStartPosition.CenterParent;

                Label label = new Label() { Text = "Enter message:", AutoSize = true, Location = new Point(10, 20) };
                TextBox inputBox = new TextBox() { Location = new Point(10, 50), Width = 360 };
                Button sendBtn = new Button() { Text = "Send", DialogResult = DialogResult.OK, Location = new Point(220, 100), Width = 70 };
                Button cancelBtn = new Button() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(300, 100), Width = 70 };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(inputBox);
                inputForm.Controls.Add(sendBtn);
                inputForm.Controls.Add(cancelBtn);
                inputForm.AcceptButton = sendBtn;
                inputForm.CancelButton = cancelBtn;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string messageToSend = inputBox.Text.Trim();
                    if (!string.IsNullOrEmpty(messageToSend) && client.Writer != null)
                    {
                        try
                        {
                            client.Writer.WriteLine($"[Server Message]: {messageToSend}");
                            textBox.AppendText($"Message sent to {client.DisplayName}: {messageToSend}\r\n");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }


        //show student screen 
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



        // Kiểm tra tính hợp lệ của IP và Port
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

        // Khởi động server và lắng nghe kết nối từ client
        private void StartServer()
        {
            try
            {
                listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                string ip = ipBox.Text;
                string port = portBox.Text;
                IPEndPoint ipepServer = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                listenerSocket.Bind(ipepServer);
                listenerSocket.Listen(10);

                textBox.AppendText($"Server started on {ip}:{port}\r\n");

                while (isListening)
                {
                    try
                    {
                        Socket clientSocket = listenerSocket.Accept();
                        Thread clientThread = new Thread(HandleClient);
                        clientThread.Start(clientSocket);
                    }
                    catch (SocketException ex)
                    {
                        if (!isListening)
                        {
                            break;
                        }
                        else
                        {
                            textBox.AppendText($"Socket error: {ex.Message}\r\n");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                ListenBtn.Enabled = true;
                MessageBox.Show($"Socket Error: {ex.Message}", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listenerSocket?.Close();
            }
        }

        private ConcurrentDictionary<Socket, ClientInfo> connectedClients = new ConcurrentDictionary<Socket, ClientInfo>();

        // Xử lý kết nối từ client
        private void HandleClient(object obj)
        {
            Socket clientSocket = (Socket)obj;
            ClientInfo clientInfo = null;

            try
            {
                NetworkStream networkStream = new NetworkStream(clientSocket);
                StreamReader reader = new StreamReader(networkStream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };
               
                string userInfo = reader.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(userInfo))
                {
                    string username = "Unknown";
                    string mssv = "N/A";
                    string Invitation = "";

                    if (userInfo.Contains("|"))
                    {
                        string[] parts = userInfo.Split('|');
                        if (parts.Length == 3)
                        {
                            username = parts[0].Trim();
                            mssv = parts[1].Trim();
                            Invitation = parts[2].Trim();
                        }
                    }

                    string ipAddress = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                    clientInfo = new ClientInfo(username, mssv, ipAddress, Invitation);
                    clientInfo.Writer = writer;
                    connectedClients.TryAdd(clientSocket, clientInfo);

                    UpdateClientList();
                    textBox.AppendText($"New client connected: {clientInfo.DisplayName}\r\n");
                }

                while (clientSocket.Connected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    if (message.ToLower() == "disconnected")
                    {
                        textBox.AppendText($"Client {clientInfo?.DisplayName} requested to disconnect.\r\n");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                textBox.AppendText($"Client error: {ex.Message}\r\n");
            }
            finally
            {
                if (clientInfo != null)
                {
                    connectedClients.TryRemove(clientSocket, out _);
                    UpdateClientList();
                }

                textBox.AppendText($"Client disconnected: {clientInfo?.DisplayName}\r\n");
                clientSocket.Close();
            }
        }



        // Cập nhật giao diện danh sách client
        private void UpdateClientUI()
        {
            if (flowPanel.InvokeRequired)
            {
                flowPanel.Invoke(new Action(UpdateClientUI));
                return;
            }

            flowPanel.Controls.Clear();

            foreach (var client in connectedClients.Values)
            {
                // Tạo một panel cho mỗi client
                Panel clientPanel = new Panel();
                clientPanel.BorderStyle = BorderStyle.FixedSingle;
                clientPanel.Width = 350;
                clientPanel.Height = 130;
                clientPanel.Margin = new Padding(5);

                // Label: Thông tin chi tiết
                Label infoLabel = new Label();
                infoLabel.Text = $"Tên: {client.Username}\n" +
                                 $"MSSV: {client.Mssv}\n" +
                                 $"IP: {client.IPAddress}\n" +
                                 $"Kết nối lúc: {client.ConnectedAt:HH:mm:ss}";
                infoLabel.AutoSize = true;
                infoLabel.Location = new Point(10, 10);

                // Button "Ngắt"
                Button btnDisconnect = new Button();
                btnDisconnect.Text = "Ngắt";
                btnDisconnect.Size = new Size(80, 25);
                btnDisconnect.Location = new Point(10, 85);

                // Button "Quan sát màn hình"
                Button btnWatch = new Button();
                btnWatch.Text = "Quan sát màn hình";
                btnWatch.Size = new Size(120, 25);
                btnWatch.Location = new Point(100, 85);
                btnWatch.Click += (s, e) =>
                {
                    ShowRdpViewer(client.Invitation);
                };

                // Button "Gửi message alert học sinh"
                Button btnSendMessage = new Button();
                btnSendMessage.Text = "Gửi tin nhắn";
                btnSendMessage.Size = new Size(100, 25);
                btnSendMessage.Location = new Point(230, 85);
                btnSendMessage.Click += (s, e) =>
                {
                    SendMessage(client);
                };


                // Add controls to panel
                clientPanel.Controls.Add(infoLabel);
                clientPanel.Controls.Add(btnDisconnect);
                clientPanel.Controls.Add(btnWatch);
                clientPanel.Controls.Add(btnSendMessage);

                // Add panel to FlowLayoutPanel
                flowPanel.Controls.Add(clientPanel);
            }
        }




        // Cập nhật danh sách client trong giao diện
        private void UpdateClientList()
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action(UpdateClientList));
            }
            else
            {
                textBox.Clear();
                textBox.AppendText("Connected Clients:\r\n");
                foreach (var client in connectedClients.Values)
                {
                    textBox.AppendText($"{client.DisplayName} - {client.IPAddress}\r\n");
                }

                UpdateClientUI();
            }
        }

        // Xử lý sự kiện khi nhấn nút "Disconnect"
        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            textBox.AppendText("Server stopped.\r\n");
            ListenBtn.Enabled = true;
            DisconnectBtn.Enabled = false;
            isListening = false;
            listenerSocket?.Close();
            listenThread?.Join();
        }
    }


    // Lớp lưu trữ thông tin client
    public class ClientInfo
    {
        public string Username { get; set; }
        public string Mssv { get; set; }
        public string DisplayName => $"{Username} ({Mssv})";
        public string IPAddress { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string Invitation { get; set; }
        public StreamWriter Writer { get; set; }



        public ClientInfo(string username, string mssv, string ipAddress, string Invitation_token)
        {
            Username = username;
            Mssv = mssv;
            IPAddress = ipAddress;
            ConnectedAt = DateTime.Now;
            Invitation = Invitation_token;
        }

        public override string ToString()
        {
            return $"{DisplayName} - {IPAddress} - Connected at {ConnectedAt:HH:mm:ss}";
        }
    }
}

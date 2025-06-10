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
        private bool isDragging = false;
        private Point dragStartPosition;
        private bool isRedIndicator = true;

        public Streaming(NetworkManager.NetworkManager netManager, string student_username)
        {
            InitializeComponent();
            _netManager = netManager;
            _student_username = student_username;

            // Add styling and dragging functionality
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;

            // Make form draggable from header panel
            this.headerPanel.MouseDown += HeaderPanel_MouseDown;
            this.headerPanel.MouseMove += HeaderPanel_MouseMove;
            this.headerPanel.MouseUp += HeaderPanel_MouseUp;

            // Start the animation for the status indicator
            animationTimer.Start();

            // Update streaming status with student name
            label1.Text = $"Streaming {_student_username}'s screen";
        }

        #region Form Dragging Support
        private void HeaderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPosition = new Point(e.X, e.Y);
            }
        }

        private void HeaderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.PointToScreen(new Point(e.X, e.Y));
                newLocation.Offset(-dragStartPosition.X, -dragStartPosition.Y);
                this.Location = newLocation;
            }
        }

        private void HeaderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
        #endregion

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Blink the recording indicator
        private void animationTimer_Tick(object sender, EventArgs e)
        {
            if (isRedIndicator)
            {
                statusIndicator.BackColor = Color.FromArgb(231, 76, 60); // Red
                isRedIndicator = false;
            }
            else
            {
                statusIndicator.BackColor = Color.FromArgb(155, 89, 182); // Purple
                isRedIndicator = true;
            }
        }

        // The timer for any other timing needs
        private void timer1_Tick(object sender, EventArgs e)
        {
            // This can be used for updates or refreshes if needed
        }

        // Your original functionality preserved
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
                    Text = $"Viewing {_student_username}'s Screen",
                    Size = new Size(1024, 768),
                    StartPosition = FormStartPosition.CenterScreen
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

                    // Update the UI to show we're connected successfully
                    this.Invoke((MethodInvoker)(() =>
                    {
                        label1.Text = $"Connected to {_student_username}";
                        statusIndicator.BackColor = Color.FromArgb(46, 204, 113); // Success green
                    }));

                    this.Invoke((MethodInvoker)(() => ShowRdpViewer(Invitation)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                // Update UI to show error
                this.Invoke((MethodInvoker)(() =>
                {
                    label1.Text = "Connection failed";
                    statusIndicator.BackColor = Color.Red;
                }));
            }
            return false; // Return false in case of any exception
        }

        // Close on escape key
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

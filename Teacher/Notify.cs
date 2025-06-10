using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher
{
    public partial class Notify : Form
    {
        private NetworkManager.NetworkManager _netManager;
        private bool isDragging = false;
        private Point dragStartPosition;

        public Notify(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;

            // Make form draggable
            this.headerPanel.MouseDown += HeaderPanel_MouseDown;
            this.headerPanel.MouseMove += HeaderPanel_MouseMove;
            this.headerPanel.MouseUp += HeaderPanel_MouseUp;

            // Update recipient label with room ID
            if (!string.IsNullOrEmpty(Globals.roomID))
            {
                recipientLabel.Text = $"To: All students in Room {Globals.roomID}";
            }

            // Set focus to text area
            this.Shown += (s, e) => Send_to_all_tb.Focus();
        }

        #region Draggable Form Functionality
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

        // Original notification functionality preserved
        public async Task<bool> Notify_message()
        {
            try
            {
                // Display sending indicator
                button1.Text = "Sending...";
                button1.Enabled = false;

                var Notify_message_ = new Teacher.MessageModel.Notify
                {
                    room_id = Globals.roomID,
                    noti_message = Send_to_all_tb.Text,
                };

                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(Notify_message_);
                if (string.IsNullOrEmpty(responseData))
                {
                    // Reset button state
                    button1.Text = "Send";
                    button1.Enabled = true;
                    return false;
                }

                JObject doc = JObject.Parse(responseData);
                var status = (string)doc["status"];
                var message = (string)doc["message"];

                // Reset button state
                button1.Text = "Send";
                button1.Enabled = true;

                switch (status)
                {
                    case "success":
                        MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                        return true;
                    case "error":
                        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                // Reset button state in case of exception
                button1.Text = "Send";
                button1.Enabled = true;

                MessageBox.Show("An error occurred while sending the notification: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private async void Send_to_all_btt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Send_to_all_tb.Text))
            {
                MessageBox.Show("Please enter a message before sending.",
                    "Empty Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await Notify_message();
        }

        // Make form movable with Escape key
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

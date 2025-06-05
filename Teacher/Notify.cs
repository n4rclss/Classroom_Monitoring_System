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
    public partial class Notify : Form
    {
        private NetworkManager.NetworkManager _netManager;
        public Notify(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;
        }

        public async Task<bool> Notify_message()
        {
            try
            {
                var Notify_message_ = new Teacher.MessageModel.Notify
                {
                    room_id = Globals.roomID,
                    noti_message = Send_to_all_tb.Text,
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(Notify_message_);
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

        private async void Send_to_all_btt_Click(object sender, EventArgs e)
        {
            await Notify_message();
        }
    }
}

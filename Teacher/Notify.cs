using Newtonsoft.Json.Linq;
using System;
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

                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync();
                    MessageBox.Show("Might be something wrong there?");
                }

                string responseData = await _netManager.ProcessSendMessage(Notify_message_);
                if (string.IsNullOrEmpty(responseData))
                {
                    return false;
                }

                JObject doc = JObject.Parse(responseData);
                var status = (string)doc["status"];
                var message = (string)doc["message"];

                switch (status)
                {
                    case "success":
                        MessageBox.Show(message);
                        return true;
                    case "error":
                        return false;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false;
        }


        private async void Send_to_all_btt_Click(object sender, EventArgs e)
        {
            await Notify_message();
        }
    }
}

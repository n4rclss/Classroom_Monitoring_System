using System.Threading.Tasks;
using Student;
using System.Text.Json;
using System.Windows.Forms;
using System.Linq.Expressions;
using System;

namespace Student.AuthService
{
    public class AuthService
    {
        private NetworkManager.NetworkManager _netManager;

        public AuthService(NetworkManager.NetworkManager netManager)
        {
            _netManager = netManager;
        }
        public async Task<bool> Login(string username, string password)
        {
            try
            {
                var loginMessage = new Student.MessageModel.LoginMessage
                {
                    Username = username,
                    Password = password
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync().ConfigureAwait(false);
                }

                string responseData = await _netManager.ProcessSendMessage(loginMessage).ConfigureAwait(false);
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
                        return true;
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
    }
}

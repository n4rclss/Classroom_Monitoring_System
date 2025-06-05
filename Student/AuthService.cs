using System.Threading.Tasks;
using Student; 
using System.Text.Json;
using System.Windows.Forms;
using System;

namespace Student.AuthService // Keep namespace for consistency
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
                var loginMessage = new MessageModel.LoginMessage
                {
                    Username = username,
                    Password = password,
                    Type = "login",
                    Role = "student" // Explicitly set role for student client
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    await _netManager.ConnectAsync().ConfigureAwait(false);
                }

                string responseData = await _netManager.ProcessSendMessage(loginMessage).ConfigureAwait(false);
                if (string.IsNullOrEmpty(responseData))
                {
                    Console.WriteLine("No response received from server");
                    return false; // No response from server
                }

                try
                {
                    // Deserialize using the LoginResponse model
                    var response = JsonSerializer.Deserialize<MessageModel.LoginResponse>(responseData);
                    Console.WriteLine($"Login response: {response.Status} - {response.Message}");
                    
                    return response.Status == "success";
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing response: {ex.Message}");
                    Console.WriteLine($"Raw response: {responseData}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            return false; // Return false in case of any exception
        }
    }
}

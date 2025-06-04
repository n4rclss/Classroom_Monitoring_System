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
    public partial class Send_to_all : Form
    {
        private NetworkManager.NetworkManager _netManager;
        public Send_to_all(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;
        }

        public async Task<bool> Send_message_to_all()
        {
            // *** ADD CHECK HERE ***
            if (string.IsNullOrEmpty(Globals.roomID))
            {
                MessageBox.Show("No active room selected. Please create a room in the dashboard first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false; // Prevent sending message
            }
            // *** END CHECK ***

            if (string.IsNullOrWhiteSpace(Send_to_all_tb.Text))
            {
                MessageBox.Show("Message cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false; // Prevent sending empty message
            }

            try
            {
                var broadcast_message = new Teacher.MessageModel.Send_message_to_all // Renamed variable for clarity
                {
                    room_id = Globals.roomID, // Use the checked Globals.roomID
                    message_to_all = Send_to_all_tb.Text,
                };

                // Check if netManager is connected
                if (!_netManager.IsConnected)
                {
                    // Consider if reconnecting here is the best strategy, maybe just show error?
                    // For now, keep original logic but add logging/message
                    MessageBox.Show("Network connection lost. Attempting to reconnect...", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try
                    {
                        await _netManager.ConnectAsync();
                        if (!_netManager.IsConnected)
                        {
                            MessageBox.Show("Failed to reconnect. Please check your connection and try again.", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    catch (Exception reconEx)
                    {
                        MessageBox.Show($"Failed to reconnect: {reconEx.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                string responseData = await _netManager.ProcessSendMessage(broadcast_message); // Use renamed variable
                if (string.IsNullOrEmpty(responseData))
                {
                    MessageBox.Show("No response received from the server.", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false; // No response from server
                }

                // Safely parse JSON
                try
                {
                    JsonDocument doc = JsonDocument.Parse(responseData);
                    var status = doc.RootElement.GetProperty("status").GetString();
                    var message = doc.RootElement.GetProperty("message").GetString();

                    switch (status)
                    {
                        case "success":
                            {
                                MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // Optionally close the form after successful send?
                                // this.Close();
                                return true;
                            }
                        case "error":
                            MessageBox.Show($"Server error: {message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        default:
                            MessageBox.Show($"Unknown server response status: {status}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                    }
                }
                catch (JsonException jsonEx)
                {
                    MessageBox.Show($"Failed to parse server response: {jsonEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"JSON Parsing Error: {jsonEx}");
                    return false;
                }
                catch (KeyNotFoundException keyEx)
                {
                    MessageBox.Show($"Server response missing expected field: {keyEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"JSON Key Error: {keyEx}");
                    return false;
                }
            }
            catch (InvalidOperationException opEx) // Catch specific network errors if possible
            {
                MessageBox.Show($"Network operation failed: {opEx.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Network Error: {opEx}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Error in Send_message_to_all: {ex}");
            }
            return false; // Return false in case of any exception
        }

        private async void Send_to_all_btt_Click(object sender, EventArgs e)
        {
            await Send_message_to_all();
        }
    }
}

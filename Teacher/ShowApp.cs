using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Teacher;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Teacher
{
    public partial class ShowApp : Form
    {
        // Remove userProcesses list as we get data from server now
        // private List<Process> userProcesses = new List<Process>(); 
        private ListViewItemComparer comparer = null;
        private NetworkManager.NetworkManager _netManager;
        private string _studentUsername;

        public ShowApp(NetworkManager.NetworkManager networkManager, string username)
        {
            InitializeComponent();
            comparer = new ListViewItemComparer();
            comparer.ColumnIndex = 0;
            listView1.ListViewItemSorter = comparer;
            _netManager = networkManager;
            _studentUsername = username; 
            // Subscribe to network events if NetworkManager provides them for incoming messages
            // Example: _netManager.OnReturnAppReceived += HandleReturnAppReceived;
            // For now, we will use a request/response pattern as in the original code
        }

        // Method to handle receiving the app list (if using event-based approach)
        // private void HandleReturnAppReceived(MessageModel.RunningAppMessage appMessage)
        // {
        //     // Ensure UI updates are on the UI thread
        //     if (this.InvokeRequired)
        //     {
        //         this.Invoke(new Action(() => DisplayReceivedApps(appMessage.app_data)));
        //     }
        //     else
        //     {
        //         DisplayReceivedApps(appMessage.app_data);
        //     }
        // }

        // Method to populate the ListView
        private void DisplayReceivedApps(List<MessageModel.ProcessInfo> apps)
        {
            if (apps == null) return;

            listView1.BeginUpdate(); // Prevent flicker during update
            listView1.Items.Clear();

            foreach (var appInfo in apps)
            {
                // Use MainWindowTitle if available, otherwise ProcessName
                string windowTitle = string.IsNullOrWhiteSpace(appInfo.main_window_title) ? "(No Title)" : appInfo.main_window_title;
                string[] row = new string[] { appInfo.process_name, windowTitle };
                listView1.Items.Add(new ListViewItem(row));
            }
            listView1.EndUpdate();
            totalLabel.Text = apps.Count.ToString(); // Assuming totalLabel exists
        }


        public async Task RequestAndDisplayRunningApps()
        {
            string responseJson = null;
            try
            {
                var request_app_packet = new MessageModel.RequestRunningApps
                {
                    target_username = _studentUsername,
                };

                if (!_netManager.IsConnected)
                {
                    // Attempt to reconnect or show error
                    MessageBox.Show("Not connected to the server.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; 
                }

                responseJson = await _netManager.ProcessSendMessage(request_app_packet);
                
                if (string.IsNullOrEmpty(responseJson))
                {
                    MessageBox.Show("No response received from the server after requesting app list.", "Timeout/Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; 
                }

                // --- Parse the response --- 
                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    if (doc.RootElement.TryGetProperty("type", out JsonElement typeElement) && typeElement.GetString() == "return_app")
                    {
                        if (doc.RootElement.TryGetProperty("app_data", out JsonElement appDataElement) && appDataElement.ValueKind == JsonValueKind.Array)
                        {
                            List<MessageModel.ProcessInfo> receivedApps = new List<MessageModel.ProcessInfo>();
                            foreach (JsonElement appElement in appDataElement.EnumerateArray())
                            {
                                try
                                {
                                    // Deserialize each element in the array
                                    MessageModel.ProcessInfo appInfo = JsonSerializer.Deserialize<MessageModel.ProcessInfo>(appElement.GetRawText());
                                    if (appInfo != null)
                                    {
                                        receivedApps.Add(appInfo);
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    Debug.WriteLine($"Error deserializing app element: {jsonEx.Message}");
                                    // Optionally log or show a partial error
                                }
                            }
                            
                            // Display the successfully parsed apps
                            DisplayReceivedApps(receivedApps);
                            MessageBox.Show($"Received and displayed {receivedApps.Count} running applications.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); // Confirmation message
                        }
                        else
                        {
                             MessageBox.Show("Received response, but \"app_data\" is missing or not an array.", "Invalid Response", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement) && statusElement.GetString() == "error")
                    {
                        // Handle potential error response from the initial request
                        string errorMessage = doc.RootElement.TryGetProperty("message", out JsonElement msgElement) ? msgElement.GetString() : "Unknown error";
                        MessageBox.Show($"Server returned an error: {errorMessage}", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        // Handle unexpected response type
                         MessageBox.Show($"Received unexpected response type from server: {responseJson}", "Unexpected Response", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                 MessageBox.Show($"Error parsing server response: {jsonEx.Message}\nResponse: {responseJson ?? "null"}", "JSON Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error requesting/processing running applications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ShowApp_Load(object sender, EventArgs e)
        {
            // Request and display apps when the form loads
            await RequestAndDisplayRunningApps(); 
        }
        
        // Filter TextBox TextChanged (Keep existing functionality if needed)
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
           // Implement filtering based on displayed apps if required
           // Example: FilterDisplayedApps(toolStripTextBox1.Text);
        }

        // Column Click for Sorting (Keep existing functionality)
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            comparer.ColumnIndex = e.Column;
            comparer.SortDirection = comparer.SortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            listView1.Sort();
        }
    }
}


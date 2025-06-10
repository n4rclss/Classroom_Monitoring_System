using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        // Keep your existing variables
        private ListViewItemComparer comparer = null;
        private NetworkManager.NetworkManager _netManager;
        private string _studentUsername;
        private bool isDragging = false;
        private Point dragStartPosition;

        public ShowApp(NetworkManager.NetworkManager networkManager, string username)
        {
            InitializeComponent();
            comparer = new ListViewItemComparer();
            comparer.ColumnIndex = 0;
            listView1.ListViewItemSorter = comparer;
            _netManager = networkManager;
            _studentUsername = username;

            // Update title to show which student's applications we're viewing
            titleLabel.Text = $"Applications - {_studentUsername}";
        }

        // Method to populate the ListView - preserved exactly as in your original code
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

            // Apply filter if there's text in the filter box
            if (!string.IsNullOrEmpty(filterTextBox.Text))
            {
                FilterListView(filterTextBox.Text);
            }
        }

        // Your original request and display method - preserved fully
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

        // Filter functionality - updated to work with TextBox instead of ToolStripTextBox
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            FilterListView(filterTextBox.Text);
        }

        // Added filter implementation
        // Replace your current FilterListView method with this implementation
        private void FilterListView(string filterText)
        {
            // Save currently selected item if any
            int selectedIndex = -1;
            if (listView1.SelectedItems.Count > 0)
                selectedIndex = listView1.SelectedItems[0].Index;

            // Clear and re-add only matching items
            listView1.BeginUpdate();

            // Store all items first
            List<ListViewItem> allItems = new List<ListViewItem>();
            foreach (ListViewItem item in listView1.Items)
            {
                allItems.Add((ListViewItem)item.Clone());
            }

            listView1.Items.Clear();

            if (string.IsNullOrEmpty(filterText))
            {
                // Add all items back if no filter
                foreach (ListViewItem item in allItems)
                {
                    listView1.Items.Add(item);
                }
            }
            else
            {
                filterText = filterText.ToLower();

                // Only add matching items
                foreach (ListViewItem item in allItems)
                {
                    bool match = false;

                    // Check process name (column 0)
                    if (item.SubItems[0].Text.ToLower().Contains(filterText))
                        match = true;

                    // Check window title (column 1)
                    if (item.SubItems[1].Text.ToLower().Contains(filterText))
                        match = true;

                    if (match)
                        listView1.Items.Add(item);
                }
            }

            listView1.EndUpdate();

            // Update total count
            totalLabel.Text = listView1.Items.Count.ToString();

            // Try to restore selection
            if (selectedIndex >= 0 && selectedIndex < listView1.Items.Count)
                listView1.Items[selectedIndex].Selected = true;
        }


        // Column Click for Sorting - preserved from your original code
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            comparer.ColumnIndex = e.Column;
            comparer.SortDirection = comparer.SortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            listView1.Sort();
        }

        // Added for the new UI - close button
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Added for the new UI - refresh button
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await RequestAndDisplayRunningApps();
        }

        // Added for the new UI - window dragging
        private void headerPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPosition = new Point(e.X, e.Y);
            }
        }

        private void headerPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.PointToScreen(new Point(e.X, e.Y));
                newLocation.Offset(-dragStartPosition.X, -dragStartPosition.Y);
                this.Location = newLocation;
            }
        }

        private void headerPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        // Close with escape key
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

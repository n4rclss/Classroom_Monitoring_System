
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Teacher
{
    public partial class ShowApp : Form
    {
        private List<Process> userProcesses = new List<Process>();
        private ListViewItemComparer comparer = null;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        public ShowApp()
        {
            InitializeComponent();
            comparer = new ListViewItemComparer();
            comparer.ColumnIndex = 0;
            listView1.ListViewItemSorter = comparer;
        }

        private void ShowApp_Load(object sender, EventArgs e)
        {
            RefreshAndDisplayProcesses();
        }

        private bool HasVisibleWindow(int processId)
        {
            bool hasVisible = false;
            EnumWindows((hWnd, lParam) =>
            {
                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);

                if (windowPid == processId && IsWindowVisible(hWnd))
                {
                    hasVisible = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return hasVisible;
        }


        private void UpdateUserProcessList()
        {
            userProcesses.Clear();
            Process[] allProcesses = Process.GetProcesses();
            IntPtr shellWindowHandle = GetShellWindow(); // Get handle for the Shell desktop window

            foreach (Process p in allProcesses)
            {
                // Basic check: Skip idle process, ensure MainWindowHandle is not the Shell itself (often 0 anyway)
                if (p.Id == 0 || p.MainWindowHandle == shellWindowHandle) continue;

                try
                {
                    //Filter
                    if (HasVisibleWindow(p.Id))
                    {
                        if (!string.IsNullOrWhiteSpace(p.MainWindowTitle) || p.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                        {
                            userProcesses.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing process {p.Id} ({p.ProcessName}): {ex.Message}");
                }
            }
        }

        private void RefreshAndDisplayProcesses(string keyword = null)
        {
            try
            {
                UpdateUserProcessList(); // Get the latest list using refined filtering

                listView1.BeginUpdate(); // Prevent flicker during update
                listView1.Items.Clear();

                List<Process> processesToDisplay = string.IsNullOrWhiteSpace(keyword)
                    ? userProcesses
                    : userProcesses.Where(p =>
                        (p.ProcessName?.ToLower().Contains(keyword.ToLower()) ?? false) ||
                        (p.MainWindowTitle?.ToLower().Contains(keyword.ToLower()) ?? false)
                      ).ToList();

                foreach (Process p in processesToDisplay)
                {
                    // Use MainWindowTitle if available, otherwise ProcessName
                    string windowTitle = string.IsNullOrWhiteSpace(p.MainWindowTitle) ? "(No Title)" : p.MainWindowTitle;
                    string[] row = new string[] { p.ProcessName, windowTitle };
                    listView1.Items.Add(new ListViewItem(row));
                }
                listView1.EndUpdate();
                totalLabel.Text = processesToDisplay.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing process list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Refresh Button Click
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            RefreshAndDisplayProcesses(toolStripTextBox1.Text); // Refresh with current filter
        }

        // Filter TextBox TextChanged
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            RefreshAndDisplayProcesses(toolStripTextBox1.Text);
        }

        // Column Click for Sorting
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            comparer.ColumnIndex = e.Column;
            comparer.SortDirection = comparer.SortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            listView1.Sort();
        }
    }
}


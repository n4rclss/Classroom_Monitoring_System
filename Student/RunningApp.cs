using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Student.MessageModel; // Added to access RunningAppMessage.ProcessInfo

namespace Student
{
    public class RunningApp
    {
        private List<Process> userProcesses = new List<Process>();
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

        // Removed internal ProcessInfo class as we'll use the one from MessageModel

        private bool HasVisibleWindow(int processId)
        {
            bool hasVisible = false;
            EnumWindows((hWnd, lParam) =>
            {
                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);

                if (windowPid == processId && IsWindowVisible(hWnd))
                {
                    // Check if the window has a title - helps filter out background processes
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        hasVisible = true;
                        return false; // Stop enumeration
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);
            return hasVisible;
        }

        private void UpdateUserProcessList()
        {
            userProcesses.Clear();
            Process[] allProcesses = Process.GetProcesses();
            IntPtr shellWindowHandle = GetShellWindow(); // Handle for the desktop window

            foreach (Process p in allProcesses)
            {
                // Skip idle process, system process, and the shell window itself
                if (p.Id <= 4 || p.MainWindowHandle == IntPtr.Zero || p.MainWindowHandle == shellWindowHandle) continue;

                try
                {
                    // Check if the process has a visible window with a title
                    if (IsWindowVisible(p.MainWindowHandle) && !string.IsNullOrEmpty(p.MainWindowTitle))
                    {
                         userProcesses.Add(p);
                    }
                    // Fallback check using EnumWindows for processes without a main window handle readily available
                    else if (HasVisibleWindow(p.Id))
                    {
                         userProcesses.Add(p);
                    }
                }
                catch (Exception ex)
                {
                    // Log errors for processes that might be inaccessible
                    Console.WriteLine($"Error accessing process {p.Id} ({p.ProcessName}): {ex.Message}");
                }
            }
            // Ensure uniqueness just in case the two methods added the same process
            userProcesses = userProcesses.GroupBy(proc => proc.Id).Select(g => g.First()).ToList();
        }

        // Method to get the list of running applications information
        public List<RunningAppMessage.ProcessInfo> GetRunningAppsInfo()
        {
            UpdateUserProcessList();

            var processInfos = userProcesses.Select(p => new RunningAppMessage.ProcessInfo
            {
                ProcessName = p.ProcessName,
                MainWindowTitle = p.MainWindowTitle ?? "N/A" // Use "N/A" if title is null
            }).ToList();

            return processInfos;
        }

        public void ShowRunningAppsJson()
        {
            UpdateUserProcessList();

            var processInfos = userProcesses.Select(p => new ProcessInfo
            {
                ProcessName = p.ProcessName,
                MainWindowTitle = p.MainWindowTitle
            }).ToList();

            string json = JsonConvert.SerializeObject(processInfos, Formatting.Indented);

            // Create a new form to display the JSON
            Form jsonForm = new Form();
            jsonForm.Text = "Running Apps (JSON)";
            jsonForm.Width = 800;
            jsonForm.Height = 600;
            jsonForm.StartPosition = FormStartPosition.CenterParent;

            // Add a TextBox with the JSON content
            TextBox textBox = new TextBox();
            textBox.Multiline = true;
            textBox.ScrollBars = ScrollBars.Both;
            textBox.Dock = DockStyle.Fill;
            textBox.Text = json;
            textBox.Font = new System.Drawing.Font("Consolas", 9.75f); // Monospaced font for better readability
            textBox.SelectAll(); // Automatically select all text for easy copying

            // Add a Copy button
            Button copyButton = new Button();
            copyButton.Text = "Copy to Clipboard";
            copyButton.Dock = DockStyle.Bottom;
            copyButton.Click += (sender, e) =>
            {
                Clipboard.SetText(textBox.Text);
                MessageBox.Show("JSON copied to clipboard!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Add controls to the form
            jsonForm.Controls.Add(textBox);
            jsonForm.Controls.Add(copyButton);

            // Show the form as a dialog
            jsonForm.ShowDialog();
        }
        private class ProcessInfo
        {
            public string ProcessName { get; set; }
            public string MainWindowTitle { get; set; }
        }
    }
}

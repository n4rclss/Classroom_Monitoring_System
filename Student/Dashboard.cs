using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Student;

namespace Student
{

    public partial class Dashboard : Form
    {
        private NetworkManager.NetworkManager _netManager;
        public Dashboard(NetworkManager.NetworkManager netManger)
        {
            InitializeComponent();
            _netManager = netManger;
            this.DisconnectBtn.Enabled = false;
            //this.FormClosing += Client_FormClosing;
            _netManager.OnMessageReceived = UpdateLogBox;
        }
        private void UpdateLogBox(string message)
        {
            if (logBox_tb.InvokeRequired)
            {
                logBox_tb.Invoke(new Action(() =>
                {
                    logBox_tb.AppendText(message + Environment.NewLine);
                }));
            }
            else
            {
                logBox_tb.AppendText(message + Environment.NewLine);
            }
        }

        public async Task<bool> Listen_passive()
        {
            try
            {
                await _netManager.ListeningPassivelyForever();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
            return true;
        }




        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DisconnectBtn.Enabled)
            {
                e.Cancel = true;
                MessageBox.Show("Please disconnect before closing the application.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private async void ConnectBtn_Click(object sender, EventArgs e)
        {

            
        }
        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
           
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Client Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

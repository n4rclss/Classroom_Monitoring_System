using System;
using System.Windows.Forms;

//using RDPCOMAPILib;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Teacher
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
        }
        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DisconnectBtn.Enabled)
            {
                e.Cancel = true;
                MessageBox.Show("Please disconnect before closing the application.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void ConnectBtn_Click(object sender, EventArgs e)
        {

            try
            {
                
            }
            catch (Exception ex)
            {
                logBox.AppendText("Error: " + ex.Message + "\r\n");
            }
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

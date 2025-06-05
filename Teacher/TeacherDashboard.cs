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
    public partial class TeacherDashboard : Form
    {
        private NetworkManager.NetworkManager _netManager;
        public TeacherDashboard(NetworkManager.NetworkManager netManager)
        {
            InitializeComponent();
            _netManager = netManager;
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void refresh_btt_Click(object sender, EventArgs e)
        {

        }

        private void Send_message_to_all_btt_Click(object sender, EventArgs e)
        {
            
        }
    }
}

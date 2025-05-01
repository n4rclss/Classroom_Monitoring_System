using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassRoomMonitoring
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void serverBtn_Click(object sender, EventArgs e)
        {
            ServerForm server = new ServerForm();
            server.Show();
        }

        private void clientBtn_Click(object sender, EventArgs e)
        {
            ClientForm client = new ClientForm();
            client.Show();
        }
    }
}

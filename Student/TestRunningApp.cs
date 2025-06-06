using System;
using System.Windows.Forms;

namespace Student
{
    public partial class TestRunningApp : Form
    {
        private RunningApp runningApp;

        public TestRunningApp()
        {
            InitializeComponent();
            runningApp = new RunningApp();
        }

        private void btnShowJson_Click(object sender, EventArgs e)
        {
            runningApp.ShowRunningAppsJson();
        }
    }
}

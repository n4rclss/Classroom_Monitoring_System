using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Student;

namespace Student
{
    public static class Globals
    {
        public static string UsernameGlobal;
    }
    internal static class Program
    {
        /// <summary>  
        /// The main entry point for the application.  
        /// </summary>  
        [STAThread]

        static async Task Main()
        {
            NetworkManager.NetworkManager netManager = new NetworkManager.NetworkManager();
            await netManager.ConnectAsync();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login(netManager));
            //Application.Run(new TestRunningApp());
        }
    }
}

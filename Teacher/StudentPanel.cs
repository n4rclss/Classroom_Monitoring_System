using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher
{
    public class StudentPanel
    {
        public string studentName { get; set; }

        public string username { get; set; }
        public string mssv { get; set; }

        public StudentPanel(string studentName, string username, string mssv)
        {
            this.studentName = studentName;
            this.username = username;
            this.mssv = mssv;
        }

        public Panel CreateStudentPanel()
        {
            Panel studentPanel = new Panel
            {
                Width = 360,
                Height = 120,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(230, 240, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            return studentPanel;
        }
    }
}

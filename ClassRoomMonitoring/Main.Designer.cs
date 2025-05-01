using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ClassRoomMonitoring
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            serverBtn = new Button();
            clientBtn = new Button();
            SuspendLayout();
            // 
            // serverBtn
            // 
            serverBtn.Location = new Point(40, 58);
            serverBtn.Name = "serverBtn";
            serverBtn.Size = new Size(123, 67);
            serverBtn.TabIndex = 0;
            serverBtn.Text = "Server";
            serverBtn.UseVisualStyleBackColor = true;
            serverBtn.Click += serverBtn_Click;
            // 
            // clientBtn
            // 
            clientBtn.Location = new Point(344, 58);
            clientBtn.Name = "clientBtn";
            clientBtn.Size = new Size(123, 67);
            clientBtn.TabIndex = 1;
            clientBtn.Text = "Client";
            clientBtn.UseVisualStyleBackColor = true;
            clientBtn.Click += this.clientBtn_Click;
            // 
            // menu
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(512, 196);
            Controls.Add(clientBtn);
            Controls.Add(serverBtn);
            Name = "menu";
            Text = "Menu";
            ResumeLayout(false);
        }

        #endregion

        private Button serverBtn;
        private Button clientBtn;
    }
}

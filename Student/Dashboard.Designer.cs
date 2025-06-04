namespace Teacher
{
    partial class Dashboard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.roomidBox = new System.Windows.Forms.TextBox();
            this.ConnectBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.usernameBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.logBox_tb = new System.Windows.Forms.TextBox();
            this.DisconnectBtn = new System.Windows.Forms.Button();
            this.mssvBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // roomidBox
            // 
            this.roomidBox.Location = new System.Drawing.Point(62, 206);
            this.roomidBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.roomidBox.Name = "roomidBox";
            this.roomidBox.Size = new System.Drawing.Size(163, 26);
            this.roomidBox.TabIndex = 3;
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.Location = new System.Drawing.Point(673, 45);
            this.ConnectBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(146, 42);
            this.ConnectBtn.TabIndex = 5;
            this.ConnectBtn.Text = "Kết nối";
            this.ConnectBtn.UseVisualStyleBackColor = true;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(58, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Tên của bạn ";
            // 
            // usernameBox
            // 
            this.usernameBox.Location = new System.Drawing.Point(62, 45);
            this.usernameBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(275, 26);
            this.usernameBox.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(58, 182);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 20);
            this.label2.TabIndex = 9;
            this.label2.Text = "Room ID";
            // 
            // logBox_tb
            // 
            this.logBox_tb.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.logBox_tb.Location = new System.Drawing.Point(446, 115);
            this.logBox_tb.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.logBox_tb.Multiline = true;
            this.logBox_tb.Name = "logBox_tb";
            this.logBox_tb.ReadOnly = true;
            this.logBox_tb.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox_tb.Size = new System.Drawing.Size(373, 315);
            this.logBox_tb.TabIndex = 11;
            // 
            // DisconnectBtn
            // 
            this.DisconnectBtn.Location = new System.Drawing.Point(446, 45);
            this.DisconnectBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DisconnectBtn.Name = "DisconnectBtn";
            this.DisconnectBtn.Size = new System.Drawing.Size(146, 42);
            this.DisconnectBtn.TabIndex = 12;
            this.DisconnectBtn.Text = "Ngắt kết nối ";
            this.DisconnectBtn.UseVisualStyleBackColor = true;
            this.DisconnectBtn.Click += new System.EventHandler(this.DisconnectBtn_Click);
            // 
            // mssvBox
            // 
            this.mssvBox.Location = new System.Drawing.Point(62, 115);
            this.mssvBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.mssvBox.Name = "mssvBox";
            this.mssvBox.Size = new System.Drawing.Size(275, 26);
            this.mssvBox.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(58, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(121, 20);
            this.label4.TabIndex = 14;
            this.label4.Text = "Mã số sinh viên ";
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 562);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.mssvBox);
            this.Controls.Add(this.DisconnectBtn);
            this.Controls.Add(this.logBox_tb);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.usernameBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ConnectBtn);
            this.Controls.Add(this.roomidBox);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Dashboard";
            this.Text = "ClientForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox roomidBox;
        private System.Windows.Forms.Button ConnectBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox usernameBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox logBox_tb;
        private System.Windows.Forms.Button DisconnectBtn;
        private System.Windows.Forms.TextBox mssvBox;
        private System.Windows.Forms.Label label4;
    }
}
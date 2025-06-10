namespace Student
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
            this.headerPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.logPanel = new System.Windows.Forms.Panel();
            this.logHeaderLabel = new System.Windows.Forms.Label();
            this.headerPanel.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.logPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // roomidBox
            // 
            this.roomidBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.roomidBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.roomidBox.Location = new System.Drawing.Point(20, 205);
            this.roomidBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.roomidBox.Name = "roomidBox";
            this.roomidBox.Size = new System.Drawing.Size(360, 30);
            this.roomidBox.TabIndex = 3;
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.ConnectBtn.FlatAppearance.BorderSize = 0;
            this.ConnectBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ConnectBtn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConnectBtn.ForeColor = System.Drawing.Color.White;
            this.ConnectBtn.Location = new System.Drawing.Point(200, 260);
            this.ConnectBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(180, 42);
            this.ConnectBtn.TabIndex = 5;
            this.ConnectBtn.Text = "Connect";
            this.ConnectBtn.UseVisualStyleBackColor = false;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label1.Location = new System.Drawing.Point(20, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 23);
            this.label1.TabIndex = 7;
            this.label1.Text = "Your Name:";
            // 
            // usernameBox
            // 
            this.usernameBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.usernameBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.usernameBox.Location = new System.Drawing.Point(20, 45);
            this.usernameBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(360, 30);
            this.usernameBox.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label2.Location = new System.Drawing.Point(20, 180);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 23);
            this.label2.TabIndex = 9;
            this.label2.Text = "Room ID:";
            // 
            // logBox_tb
            // 
            this.logBox_tb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox_tb.BackColor = System.Drawing.Color.White;
            this.logBox_tb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logBox_tb.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logBox_tb.Location = new System.Drawing.Point(20, 50);
            this.logBox_tb.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.logBox_tb.Multiline = true;
            this.logBox_tb.Name = "logBox_tb";
            this.logBox_tb.ReadOnly = true;
            this.logBox_tb.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox_tb.Size = new System.Drawing.Size(460, 430);
            this.logBox_tb.TabIndex = 11;
            // 
            // DisconnectBtn
            // 
            this.DisconnectBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.DisconnectBtn.FlatAppearance.BorderSize = 0;
            this.DisconnectBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DisconnectBtn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DisconnectBtn.ForeColor = System.Drawing.Color.White;
            this.DisconnectBtn.Location = new System.Drawing.Point(20, 260);
            this.DisconnectBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DisconnectBtn.Name = "DisconnectBtn";
            this.DisconnectBtn.Size = new System.Drawing.Size(170, 42);
            this.DisconnectBtn.TabIndex = 12;
            this.DisconnectBtn.Text = "Disconnect";
            this.DisconnectBtn.UseVisualStyleBackColor = false;
            this.DisconnectBtn.Click += new System.EventHandler(this.DisconnectBtn_Click);
            // 
            // mssvBox
            // 
            this.mssvBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mssvBox.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mssvBox.Location = new System.Drawing.Point(20, 125);
            this.mssvBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.mssvBox.Name = "mssvBox";
            this.mssvBox.Size = new System.Drawing.Size(360, 30);
            this.mssvBox.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label4.Location = new System.Drawing.Point(20, 100);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 23);
            this.label4.TabIndex = 14;
            this.label4.Text = "Student ID:";
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(900, 60);
            this.headerPanel.TabIndex = 15;
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(15, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(264, 37);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Student Dashboard";
            // 
            // leftPanel
            // 
            this.leftPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.leftPanel.Controls.Add(this.statusPanel);
            this.leftPanel.Controls.Add(this.label1);
            this.leftPanel.Controls.Add(this.usernameBox);
            this.leftPanel.Controls.Add(this.label4);
            this.leftPanel.Controls.Add(this.mssvBox);
            this.leftPanel.Controls.Add(this.label2);
            this.leftPanel.Controls.Add(this.roomidBox);
            this.leftPanel.Controls.Add(this.ConnectBtn);
            this.leftPanel.Controls.Add(this.DisconnectBtn);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Location = new System.Drawing.Point(0, 60);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Size = new System.Drawing.Size(400, 502);
            this.leftPanel.TabIndex = 16;
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Location = new System.Drawing.Point(20, 320);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(360, 150);
            this.statusPanel.TabIndex = 15;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.statusLabel.Location = new System.Drawing.Point(10, 10);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(187, 23);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Status: Not connected";
            // 
            // logPanel
            // 
            this.logPanel.BackColor = System.Drawing.Color.White;
            this.logPanel.Controls.Add(this.logHeaderLabel);
            this.logPanel.Controls.Add(this.logBox_tb);
            this.logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logPanel.Location = new System.Drawing.Point(400, 60);
            this.logPanel.Name = "logPanel";
            this.logPanel.Padding = new System.Windows.Forms.Padding(20);
            this.logPanel.Size = new System.Drawing.Size(500, 502);
            this.logPanel.TabIndex = 17;
            // 
            // logHeaderLabel
            // 
            this.logHeaderLabel.AutoSize = true;
            this.logHeaderLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logHeaderLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.logHeaderLabel.Location = new System.Drawing.Point(20, 15);
            this.logHeaderLabel.Name = "logHeaderLabel";
            this.logHeaderLabel.Size = new System.Drawing.Size(199, 28);
            this.logHeaderLabel.TabIndex = 12;
            this.logHeaderLabel.Text = "Activity Monitoring";
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 562);
            this.Controls.Add(this.logPanel);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.headerPanel);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Dashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Student Dashboard";
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.logPanel.ResumeLayout(false);
            this.logPanel.PerformLayout();
            this.ResumeLayout(false);

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
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Panel logPanel;
        private System.Windows.Forms.Label logHeaderLabel;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label statusLabel;
    }
}

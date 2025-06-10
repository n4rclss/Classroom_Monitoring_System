namespace Teacher
{
    partial class TeacherDashboard
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
            this.label1 = new System.Windows.Forms.Label();
            this.Room_ID_tb = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.refresh_btt = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.Send_message_to_all_btt = new System.Windows.Forms.Button();
            this.statusPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.label1.Location = new System.Drawing.Point(23, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Room ID:";
            // 
            // Room_ID_tb
            // 
            this.Room_ID_tb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Room_ID_tb.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Room_ID_tb.Location = new System.Drawing.Point(23, 65);
            this.Room_ID_tb.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Room_ID_tb.Name = "Room_ID_tb";
            this.Room_ID_tb.Size = new System.Drawing.Size(254, 30);
            this.Room_ID_tb.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(23, 115);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(254, 45);
            this.button1.TabIndex = 2;
            this.button1.Text = "Create Room";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.Create_Click);
            // 
            // refresh_btt
            // 
            this.refresh_btt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.refresh_btt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.refresh_btt.FlatAppearance.BorderSize = 0;
            this.refresh_btt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.refresh_btt.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refresh_btt.ForeColor = System.Drawing.Color.White;
            this.refresh_btt.Location = new System.Drawing.Point(1032, 635);
            this.refresh_btt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.refresh_btt.Name = "refresh_btt";
            this.refresh_btt.Size = new System.Drawing.Size(150, 45);
            this.refresh_btt.TabIndex = 4;
            this.refresh_btt.Text = "Refresh Users";
            this.refresh_btt.UseVisualStyleBackColor = false;
            this.refresh_btt.Click += new System.EventHandler(this.refresh_btt_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.label2.Location = new System.Drawing.Point(325, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(149, 31);
            this.label2.TabIndex = 6;
            this.label2.Text = "Online Users";
            // 
            // Send_message_to_all_btt
            // 
            this.Send_message_to_all_btt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(156)))), ((int)(((byte)(18)))));
            this.Send_message_to_all_btt.FlatAppearance.BorderSize = 0;
            this.Send_message_to_all_btt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Send_message_to_all_btt.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Send_message_to_all_btt.ForeColor = System.Drawing.Color.White;
            this.Send_message_to_all_btt.Location = new System.Drawing.Point(23, 180);
            this.Send_message_to_all_btt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Send_message_to_all_btt.Name = "Send_message_to_all_btt";
            this.Send_message_to_all_btt.Size = new System.Drawing.Size(254, 45);
            this.Send_message_to_all_btt.TabIndex = 7;
            this.Send_message_to_all_btt.Text = "Send Message To All";
            this.Send_message_to_all_btt.UseVisualStyleBackColor = false;
            this.Send_message_to_all_btt.Click += new System.EventHandler(this.Send_message_to_all_btt_Click);
            // 
            // statusPanel
            // 
            this.statusPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusPanel.AutoScroll = true;
            this.statusPanel.BackColor = System.Drawing.Color.White;
            this.statusPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.statusPanel.Location = new System.Drawing.Point(325, 130);
            this.statusPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(857, 489);
            this.statusPanel.TabIndex = 8;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.panel1.Controls.Add(this.labelTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1200, 70);
            this.panel1.TabIndex = 9;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(18, 14);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(286, 41);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Teacher Dashboard";
            // 
            // leftPanel
            // 
            this.leftPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(242)))), ((int)(((byte)(248)))));
            this.leftPanel.Controls.Add(this.label1);
            this.leftPanel.Controls.Add(this.Room_ID_tb);
            this.leftPanel.Controls.Add(this.button1);
            this.leftPanel.Controls.Add(this.Send_message_to_all_btt);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Location = new System.Drawing.Point(0, 70);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Padding = new System.Windows.Forms.Padding(20);
            this.leftPanel.Size = new System.Drawing.Size(300, 630);
            this.leftPanel.TabIndex = 10;
            // 
            // TeacherDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.refresh_btt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "TeacherDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Teacher Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TeacherDashboard_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox Room_ID_tb;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button refresh_btt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button Send_message_to_all_btt;
        private System.Windows.Forms.FlowLayoutPanel statusPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Panel leftPanel;
    }
}

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
            this.statusPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(21, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Room ID";
            // 
            // Room_ID_tb
            // 
            this.Room_ID_tb.Location = new System.Drawing.Point(114, 21);
            this.Room_ID_tb.Multiline = true;
            this.Room_ID_tb.Name = "Room_ID_tb";
            this.Room_ID_tb.Size = new System.Drawing.Size(117, 43);
            this.Room_ID_tb.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.Blue;
            this.button1.FlatAppearance.BorderSize = 3;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.button1.Location = new System.Drawing.Point(26, 80);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(236, 61);
            this.button1.TabIndex = 2;
            this.button1.Text = "Create";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Create_Click);
            // 
            // refresh_btt
            // 
            this.refresh_btt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.refresh_btt.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.refresh_btt.Location = new System.Drawing.Point(631, 373);
            this.refresh_btt.Name = "refresh_btt";
            this.refresh_btt.Size = new System.Drawing.Size(143, 56);
            this.refresh_btt.TabIndex = 4;
            this.refresh_btt.Text = "Refresh user";
            this.refresh_btt.UseVisualStyleBackColor = true;
            this.refresh_btt.Click += new System.EventHandler(this.refresh_btt_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.label2.Location = new System.Drawing.Point(479, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(153, 29);
            this.label2.TabIndex = 6;
            this.label2.Text = "Online Users";
            // 
            // Send_message_to_all_btt
            // 
            this.Send_message_to_all_btt.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.Send_message_to_all_btt.FlatAppearance.BorderSize = 3;
            this.Send_message_to_all_btt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Send_message_to_all_btt.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.Send_message_to_all_btt.Location = new System.Drawing.Point(26, 176);
            this.Send_message_to_all_btt.Name = "Send_message_to_all_btt";
            this.Send_message_to_all_btt.Size = new System.Drawing.Size(236, 58);
            this.Send_message_to_all_btt.TabIndex = 7;
            this.Send_message_to_all_btt.Text = "Send message to all";
            this.Send_message_to_all_btt.UseVisualStyleBackColor = true;
            this.Send_message_to_all_btt.Click += new System.EventHandler(this.Send_message_to_all_btt_Click);
            // 
            // statusPanel
            // 
            this.statusPanel.AutoScroll = true;
            this.statusPanel.Location = new System.Drawing.Point(310, 59);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(464, 293);
            this.statusPanel.TabIndex = 8;
            // 
            // TeacherDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.Send_message_to_all_btt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.refresh_btt);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Room_ID_tb);
            this.Controls.Add(this.label1);
            this.Name = "TeacherDashboard";
            this.Text = "TeacherDashboard";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TeacherDashboard_FormClosed);
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
        private System.Windows.Forms.Panel statusPanel;
    }
}
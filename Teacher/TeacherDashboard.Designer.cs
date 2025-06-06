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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(19, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Room ID";
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));           // 
            // Room_ID_tb
            // 
            this.Room_ID_tb.Location = new System.Drawing.Point(101, 17);
            this.Room_ID_tb.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Room_ID_tb.Multiline = true;
            this.Room_ID_tb.Name = "Room_ID_tb";
            this.Room_ID_tb.Size = new System.Drawing.Size(104, 35);
            this.Room_ID_tb.TabIndex = 1;
            this.Room_ID_tb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));            // 
            // button1
            // 
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.Blue;
            this.button1.FlatAppearance.BorderSize = 3;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.button1.Location = new System.Drawing.Point(23, 64);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(210, 49);
            this.button1.TabIndex = 2;
            this.button1.Text = "Create";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left))); this.button1.Click += new System.EventHandler(this.Create_Click);
            // 
            // refresh_btt
            // 
            this.refresh_btt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.refresh_btt.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.refresh_btt.Location = new System.Drawing.Point(763, 368);
            this.refresh_btt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.refresh_btt.Name = "refresh_btt";
            this.refresh_btt.Size = new System.Drawing.Size(127, 45);
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
            this.label2.Location = new System.Drawing.Point(628, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 25);
            this.label2.TabIndex = 6;
            this.label2.Text = "Online Users";
            // 
            // Send_message_to_all_btt
            // 
            this.Send_message_to_all_btt.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.Send_message_to_all_btt.FlatAppearance.BorderSize = 3;
            this.Send_message_to_all_btt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Send_message_to_all_btt.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.Send_message_to_all_btt.Location = new System.Drawing.Point(23, 141);
            this.Send_message_to_all_btt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Send_message_to_all_btt.Name = "Send_message_to_all_btt";
            this.Send_message_to_all_btt.Size = new System.Drawing.Size(210, 46);
            this.Send_message_to_all_btt.TabIndex = 7;
            this.Send_message_to_all_btt.Text = "Send message to all";
            this.Send_message_to_all_btt.UseVisualStyleBackColor = true;
            this.Send_message_to_all_btt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left))); this.Send_message_to_all_btt.Click += new System.EventHandler(this.Send_message_to_all_btt_Click);
            // 
            // statusPanel
            // 
            this.statusPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
    | System.Windows.Forms.AnchorStyles.Left)
    | System.Windows.Forms.AnchorStyles.Right)));
            this.statusPanel.AutoScroll = true; this.statusPanel.Location = new System.Drawing.Point(269, 52);
            this.statusPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(621, 298);
            this.statusPanel.TabIndex = 8;
            // 
            // TeacherDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 422);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.Send_message_to_all_btt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.refresh_btt);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Room_ID_tb);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
        private System.Windows.Forms.FlowLayoutPanel statusPanel;
    }
}
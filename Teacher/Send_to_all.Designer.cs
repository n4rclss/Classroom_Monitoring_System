namespace Teacher
{
    partial class Send_to_all
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
            this.Send_to_all_tb = new System.Windows.Forms.TextBox();
            this.Send_to_all_btt = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F);
            this.label1.Location = new System.Drawing.Point(320, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(135, 36);
            this.label1.TabIndex = 0;
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.Text = "Message";
            // 
            // Send_to_all_tb
            // 
            this.Send_to_all_tb.Location = new System.Drawing.Point(81, 92);
            this.Send_to_all_tb.Multiline = true;
            this.Send_to_all_tb.Name = "Send_to_all_tb";
            this.Send_to_all_tb.Size = new System.Drawing.Size(632, 241);
            this.Send_to_all_tb.TabIndex = 1;
            this.Send_to_all_tb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // Send_to_all_btt
            // 
            this.Send_to_all_btt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Send_to_all_btt.Location = new System.Drawing.Point(298, 356);
            this.Send_to_all_btt.Name = "Send_to_all_btt";
            this.Send_to_all_btt.Size = new System.Drawing.Size(157, 59);
            this.Send_to_all_btt.TabIndex = 2;
            this.Send_to_all_btt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Send_to_all_btt.Text = "Send";
            this.Send_to_all_btt.UseVisualStyleBackColor = true;
            this.Send_to_all_btt.Click += new System.EventHandler(this.Send_to_all_btt_Click);
            // 
            // Send_to_all
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Send_to_all_btt);
            this.Controls.Add(this.Send_to_all_tb);
            this.Controls.Add(this.label1);
            this.Name = "Send_to_all";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox Send_to_all_tb;
        private System.Windows.Forms.Button Send_to_all_btt;
    }
}
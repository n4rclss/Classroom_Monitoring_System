namespace Student
{
    partial class TestRunningApp
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
            this.btnShowJson = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnShowJson
            // 
            this.btnShowJson.Location = new System.Drawing.Point(259, 172);
            this.btnShowJson.Name = "btnShowJson";
            this.btnShowJson.Size = new System.Drawing.Size(284, 90);
            this.btnShowJson.TabIndex = 0;
            this.btnShowJson.Text = "button1";
            this.btnShowJson.UseVisualStyleBackColor = true;
            this.btnShowJson.Click += new System.EventHandler(this.btnShowJson_Click);
            // 
            // TestRunningApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnShowJson);
            this.Name = "TestRunningApp";
            this.Text = "TestRunningApp";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnShowJson;
    }
}
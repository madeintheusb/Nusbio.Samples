namespace MadeInTheUSB.ManagedDesktop
{
    partial class Form1
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
            this.butLamp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // butLamp
            // 
            this.butLamp.Location = new System.Drawing.Point(63, 12);
            this.butLamp.Name = "butLamp";
            this.butLamp.Size = new System.Drawing.Size(60, 23);
            this.butLamp.TabIndex = 0;
            this.butLamp.Text = "Lamp Off";
            this.butLamp.UseVisualStyleBackColor = true;
            this.butLamp.Click += new System.EventHandler(this.butLamp_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(179, 46);
            this.Controls.Add(this.butLamp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "Nusbio - Desktop Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butLamp;
    }
}


namespace NusbioAutoDetect
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
            this.components = new System.ComponentModel.Container();
            this.tmrDetectNusbio = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.butUsbDiskOn = new System.Windows.Forms.Button();
            this.butUsbDiskOff = new System.Windows.Forms.Button();
            this.tmrActivity = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tmrDetectNusbio
            // 
            this.tmrDetectNusbio.Interval = 500;
            this.tmrDetectNusbio.Tick += new System.EventHandler(this.tmrDetectNusbio_Tick);
            // 
            // label1
            // 
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(275, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "...";
            // 
            // butUsbDiskOn
            // 
            this.butUsbDiskOn.Location = new System.Drawing.Point(12, 46);
            this.butUsbDiskOn.Name = "butUsbDiskOn";
            this.butUsbDiskOn.Size = new System.Drawing.Size(98, 23);
            this.butUsbDiskOn.TabIndex = 1;
            this.butUsbDiskOn.Text = "USB Disk On";
            this.butUsbDiskOn.UseVisualStyleBackColor = true;
            this.butUsbDiskOn.Click += new System.EventHandler(this.butUsbDiskOn_Click);
            // 
            // butUsbDiskOff
            // 
            this.butUsbDiskOff.Location = new System.Drawing.Point(189, 46);
            this.butUsbDiskOff.Name = "butUsbDiskOff";
            this.butUsbDiskOff.Size = new System.Drawing.Size(92, 23);
            this.butUsbDiskOff.TabIndex = 2;
            this.butUsbDiskOff.Text = "USB Disk Off";
            this.butUsbDiskOff.UseVisualStyleBackColor = true;
            this.butUsbDiskOff.Click += new System.EventHandler(this.button2_Click);
            // 
            // tmrActivity
            // 
            this.tmrActivity.Enabled = true;
            this.tmrActivity.Interval = 2000;
            this.tmrActivity.Tick += new System.EventHandler(this.tmrActivity_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.ClientSize = new System.Drawing.Size(293, 81);
            this.Controls.Add(this.butUsbDiskOff);
            this.Controls.Add(this.butUsbDiskOn);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Nusbio - USB Disk Controller";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer tmrDetectNusbio;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button butUsbDiskOn;
        private System.Windows.Forms.Button butUsbDiskOff;
        private System.Windows.Forms.Timer tmrActivity;
    }
}


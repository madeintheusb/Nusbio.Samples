using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MadeInTheUSB;
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.Component;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;


namespace MadeInTheUSB.ManagedDesktop
{
    public partial class Form1 : Form
    {
        Nusbio _nusbio;
        public Form1()
        {
            InitializeComponent();
        }
        private void SetMessage(string m)
        {
            this.Text = m;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width;
            this.Top  = Screen.PrimaryScreen.WorkingArea.Height - this.Height;

            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                this.SetMessage("Nusbio not detected");
            }
            else
            {
                // Turn all gpio off/low
                _nusbio = new Nusbio(serialNumber);
            }
            this.UpdateLampButtonText();
        }

        private void butLamp_Click(object sender, EventArgs e)
        {
            _nusbio[NusbioGpio.Gpio0].DigitalWrite(!_nusbio[NusbioGpio.Gpio0].State);
            this.UpdateLampButtonText();
        }

        private void UpdateLampButtonText()
        {
            this.butLamp.Text = string.Format("Lamp {0}", _nusbio[NusbioGpio.Gpio0].State ? "On" : "Off");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._nusbio.Close();
        }
    }
}

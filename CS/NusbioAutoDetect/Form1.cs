using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MadeInTheUSB;

namespace NusbioAutoDetect
{
    public partial class Form1 : Form
    {
        private enum NusbioEvent
        {
            Connected,
            Deconnected,
            Unknown
        };
        private const int WM_DEVICECHANGE = 0x0219;
        private Nusbio _nusbio;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Notify("Waiting for Nusbio");
        }

        private void Notify(string message)
        {
            this.label1.Text = message;
            this.label1.Refresh();
        }

        private NusbioEvent DetectNusbio()
        {
            if (_nusbio == null)
            {
                try
                {
                    this.Notify("Waiting new USB Device to initialize");
                    Thread.Sleep(5*1000);
                    this.Notify("Searching for Nusbio");
                    var serialNumber = Nusbio.Detect(retryCount: 0);
                    if (serialNumber != null)
                    {
                        this.Notify("Nusbio found");
                        _nusbio = new Nusbio(serialNumber);
                        _nusbio[NusbioGpio.Gpio0].AsLed.ReverseSet();
                        return NusbioEvent.Connected;
                    }
                    else
                    {
                        this.Notify("Nusbio not found");
                    }
                }
                catch (System.Exception ex)
                {
                    this.Notify(ex.Message);
                }
            }
            else
            {
                var serialNumber = Nusbio.Detect();
                if (_nusbio.SerialNumber == serialNumber)
                {
                    // Nusbio still there
                }
                else
                {
                    this.Notify("Nusbio unplugged");
                    _nusbio = null;
                }
            }
            return NusbioEvent.Unknown;
        }

        public void Nusbio_WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                tmrDetectNusbio.Enabled = true;
                switch (m.WParam.ToInt32())
                {
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            Nusbio_WndProc(ref m);
        }

        private void tmrDetectNusbio_Tick(object sender, EventArgs e)
        {
            tmrDetectNusbio.Enabled = false;
            var r = DetectNusbio();
        }

        private void tmrAnimate_Tick(object sender, EventArgs e)
        {
            if (_nusbio != null)
            {
                _nusbio[NusbioGpio.Gpio0].AsLed.ReverseSet();
                _nusbio[NusbioGpio.Gpio4].AsLed.ReverseSet();
            }
        }
    }
}

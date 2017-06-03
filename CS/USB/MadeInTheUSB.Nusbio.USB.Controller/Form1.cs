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
using System.Runtime.InteropServices;
using System.Management;
using System.IO;
using RemoveDriveByLetter;

namespace NusbioAutoDetect
{
    public partial class Form1 : Form
    {
        static bool FindUSBDisk(string driveLetter)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d =>
            d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed ))
            {
                System.Diagnostics.Debug.WriteLine(drive.Name + " " + drive.IsReady.ToString());
                if(drive.Name == string.Format("{0}\\", driveLetter.ToString().ToUpperInvariant())) {
                    return true;
                }
            }
            return false;
        }

        bool _usbDeviceConnected = false;
        private string USBDiskLetter = "E:";

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

        NusbioGpio gpio = NusbioGpio.Gpio5;

        private void USB_Device_Off(bool justTurnGpioOff = false)
        {
            Notify("Disconnecting USB Disk");
            _usbDeviceConnected = false;

            if (!justTurnGpioOff)
            {
                System.Threading.Thread.Sleep(2000);
                // Eject the usb device first
                if (!RemoveDriveTools.RemoveDrive(USBDiskLetter))
                {
                    MessageBox.Show(this, string.Format("Cannot eject drive {0}", USBDiskLetter), "Eject Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            _nusbio[gpio].High();
            Notify ("USB Disk Off");
        }

        private void USB_Device_On()
        {
            _nusbio[gpio].Low();
            Notify("USB Disk On");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Notify("Waiting for Nusbio");
            if (DetectNusbio() == NusbioEvent.Connected)
            {
                USB_Device_Off(justTurnGpioOff: true);
            }
            else Environment.Exit(1);
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
                    var serialNumber = Nusbio.Detect(retryCount: 0);
                    if (serialNumber != null)
                    {
                        this.Notify("Nusbio found");
                        _nusbio = new Nusbio(serialNumber, initGpioAsInput: false);
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
            //System.Diagnostics.Debug.WriteLine("Nusbio_WndProc:{0}", m.Msg);
            //if (m.Msg == WM_DEVICECHANGE)
            //{
            //    tmrDetectNusbio.Enabled = true;
            //    switch (m.WParam.ToInt32())
            //    {
            //    }
            //}
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            Nusbio_WndProc(ref m);
        }
        

        private void tmrDetectNusbio_Tick(object sender, EventArgs e)
        {
            if(FindUSBDisk(USBDiskLetter))
            {
                Notify(string.Format("USB Disk '{0}' found", USBDiskLetter));
                tmrDetectNusbio.Enabled = false;
                _usbDeviceConnected = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Notify("no disk");
            USB_Device_Off();
        }

        private void butUsbDiskOn_Click(object sender, EventArgs e)
        {
            USB_Device_On();
            tmrDetectNusbio.Enabled = true;
        }

        private void tmrActivity_Tick(object sender, EventArgs e)
        {
            if(_usbDeviceConnected)
            {
                var fileName = DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss") + ".txt";
                var path     = Path.Combine(USBDiskLetter+@"\", "USB_DISK");
                if(!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                fileName = Path.Combine(path, fileName);
                System.IO.File.WriteAllText(fileName, Environment.TickCount.ToString());
            }
        }
    }
}

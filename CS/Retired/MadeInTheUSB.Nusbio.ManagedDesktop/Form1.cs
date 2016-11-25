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
using MadeInTheUSB.Component;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.Display;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Diagnostics;
using System.Globalization;

namespace MadeInTheUSB.ManagedDesktop
{
    public partial class Form1 : Form
    {
        private const bool mqttOn = true;

        Nusbio _nusbio;
        MqttClient _mqttClient;

        const string MQTT_FredOfficeChannelID = "BD06C5EBA5F0";
        const string mqttServer               = "test.mosquitto.org";
        static string MQTT_Channel
        {
            get { return string.Format("{0}/office/lamp", MQTT_FredOfficeChannelID); }
        }

        void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            var topic   = e.Topic;
            var time    = DateTime.Now.ToString("hh:mm:ss");
            var msg     = string.Empty;

            if (topic.EndsWith("office/lamp"))
            {
                msg = string.Format("[{0}] mqtt msg, lamp {1}", time, message);
                this.Lamp(message == "on");
            }
            else
            {
                msg = string.Format("Unknown topic:{0}, message:{1}", topic, message);
            }
            Debug.WriteLine(msg);
            this.StatusLabel1.Text = msg;
        }

        private void SubscribeToMqttNotifications()
        {
            _mqttClient = new MqttClient(mqttServer);
            _mqttClient.MqttMsgPublishReceived += MqttMsgPublishReceived;
            _mqttClient.Connect(Guid.NewGuid().ToString());
            _mqttClient.Subscribe(new string[] { MQTT_Channel }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
            this.StatusLabel1.Text = string.Format("Listening to mqtt {0}", mqttServer);
        }

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
            this.StatusLabel1.Text = "Ready...";

            if (mqttOn)
            {
                SubscribeToMqttNotifications();
            }
        }

        private void butLamp_Click(object sender, EventArgs e)
        {
            _nusbio[NusbioGpio.Gpio0].DigitalWrite(!_nusbio[NusbioGpio.Gpio0].State);
            this.UpdateLampButtonText();
        }

        private void Lamp(bool on)
        {
            if(this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    this.Lamp(on);
                }));
            }
            else
            {
                _nusbio[NusbioGpio.Gpio0].DigitalWrite(on);
                this.UpdateLampButtonText();
            }
        }

        private void UpdateLampButtonText()
        {
            this.butLamp.Text = string.Format("Lamp {0}", !_nusbio[NusbioGpio.Gpio0].State ? "On" : "Off");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(_mqttClient != null)
            {
                _mqttClient.Disconnect();
                _mqttClient = null;
            }
            this._nusbio.Close();
        }
    }
}

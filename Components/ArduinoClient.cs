using System;
using System.Collections.Generic;
using System.Text;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// Based on Kevin Darrah video - https://www.youtube.com/watch?v=3Rs3SJBsiYE
    /// 1-Wire tutorial http://www.maximintegrated.com/en/products/1-wire/flash/overview/
    /// </summary>
    class ArduinoClient
    {
        private NusbioGpio _gpio;
        private Nusbio          _nusbio;

        public bool Communicating;

        public ArduinoClient(Nusbio nusbio, NusbioGpio gpio)
        {
            this._gpio              = gpio;
            this._nusbio           = nusbio;
        }

        private void High()
        {
            Console.Write("H");
            this._nusbio.GPIOS[_gpio].DigitalWrite(PinState.High);
        }

        private void Low()
        {
            Console.Write("L");
            this._nusbio.GPIOS[_gpio].DigitalWrite(PinState.Low);
        }

        public bool SendByte(byte b)
        {
            int i;

            Console.WriteLine("Send fake high low");
  
            for(i=0; i<5; i++)
            {
                this.High();
                TimePeriod.Sleep(500);
                this.Low();
                TimePeriod.Sleep(500);
            }

            //Console.WriteLine("Send 3000 high");
            //this.High();
            //TimePeriod.__Sleep(3000);
            //Console.WriteLine("Send 500 low");
            //this.Low();
            //TimePeriod.__Sleep(500);
    
            //Console.WriteLine("Send bit now");
            
            //for(i=0; i<8; i++)
            //{
            //    if ((b & i) == i)
            //        this.High();
            //    else
            //        this.Low();                    

            //    TimePeriod.__Sleep(500);

            //    if ((b & i) == i)
            //        this.Low();                    
            //    else                    
            //        this.High();

            //    TimePeriod.__Sleep(500);
            //}
            this.Low();
            return true;
        }

        public bool SendBytebu(byte b)
        {
            int i;
  
            for(i=0; i<20; i++)
            {
                this.High();
                TimePeriod.__SleepMicro(500);
                this.Low();
                TimePeriod.__SleepMicro(500);
            }

            this.High();
            TimePeriod.__SleepMicro(3000);
            
            this.Low();
            TimePeriod.__SleepMicro(500);
    
            for(i=0; i<8; i++)
            {
                if ((b & i) == i)
                    this.High();
                else
                    this.Low();                    

                TimePeriod.__SleepMicro(500);

                if ((b & i) == i)
                    this.Low();                    
                else                    
                    this.High();

                TimePeriod.__SleepMicro(500);
            }
            this.Low();
            return true;
        }

        public bool SendString(string s)
        {
            using (var tp = new TimePeriod(1))
            {
                Console.Clear();
                Console.WriteLine(s);
                //s += "#";
                byte[] byteArray = Encoding.ASCII.GetBytes(s);
                foreach (var c in byteArray)
                {
                    if (!SendByte(c))
                        return false;
                }
                return true;
            }
        }
    }
}

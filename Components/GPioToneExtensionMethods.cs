/*
    Copyright (C) 2015 MadeInTheUSB LLC

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
    associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial 
    portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;

namespace MadeInTheUSB
{
    public static class GPioExtensionMethods
    {
        [DllImport("winmm.dll", EntryPoint="timeBeginPeriod", SetLastError=true)]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint="timeEndPeriod", SetLastError=true)]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        public static void Tone(this GpioApi gpio, int frequency, int duration)
        {
            gpio.DigitalWrite(PinState.Low);
            var waitTimeMicroS = (int) (1000*1000.0/frequency);
            var sw = Stopwatch.StartNew();
            using (var tp = new TimePeriod(waitTimeMicroS))
            {
                while (sw.ElapsedMilliseconds < duration)
                {
                    gpio.DigitalWrite(PinState.High);
                    tp.SleepMicro(waitTimeMicroS);
                    gpio.DigitalWrite(PinState.Low);
                    tp.SleepMicro(waitTimeMicroS);
                }
            }
            sw.Stop();
        }

        public static void ToneEx(this GpioApi gpio, int frequency, int duration)
        {
            gpio.DigitalWrite(PinState.Low);
            var waitTimeMicroS = (1000.0*1000.0/frequency);
            var sw = Stopwatch.StartNew();
            using (var tp = new TimePeriod((int)waitTimeMicroS))
            {
                while (sw.ElapsedMilliseconds < duration)
                {
                    gpio.DigitalWrite(PinState.High);
                    TimePeriod.Sleep((int)(waitTimeMicroS/1000.0));
                    gpio.DigitalWrite(PinState.Low);
                    TimePeriod.Sleep((int)(waitTimeMicroS/1000.0));
                }
            }
            sw.Stop();
        }


        public static void WasteTime(int duration)
        {
            uint a = 0, b = 0, c = 0;
            for (uint i = 0; i < duration; i++)
            {
                a++;
                b++;
                c++;
                if (a%b == c)
                {
                    a = 0;
                }
            }

        }

        public static void Tone2(this GpioApi gpio, int rate, int duration)
        {
            uint i = 0;
            //gpio.DigitalWrite(false);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < duration)
            {
                var wait = rate*1000*Math.Sin(i);
                //var wait = rate*1000*Math.Cos(i);
                //var wait = Math.Abs(rate * 1000 * Math.Cos(i) * Math.Sin(i));
                //var wait = Math.Abs(rate*1000*Math.Tan(i));
                //Console.Write("{0} ", (int) wait);
                gpio.DigitalWrite(PinState.High);
                WasteTime((int)wait);
                gpio.DigitalWrite(PinState.Low);
                WasteTime((int)wait);
                i++;
            }
            sw.Stop();
        }
    }
}


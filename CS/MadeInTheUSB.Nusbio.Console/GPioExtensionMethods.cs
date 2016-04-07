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
using System.Text;
using System.Threading.Tasks;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB
{
    public static class GPioExtensionMethods
    {
        /// <summary>
        /// http://en.wikipedia.org/wiki/Pulse-width_modulation
        /// </summary>
        /// <param name="gpio"></param>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public static void PWM(this GpioApi gpio, int on, int off, int duration)
        {            
            gpio.DigitalWrite(PinState.Low);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < duration)
            {
                gpio.DigitalWrite(PinState.High );
                //Gpio.SleepMicro(on*1000);
                System.Threading.Thread.Sleep(on);
                gpio.DigitalWrite(PinState.Low);
                //Gpio.SleepMicro(off*1000);
                System.Threading.Thread.Sleep(off);
            }
            sw.Stop();
        }        
    }
}

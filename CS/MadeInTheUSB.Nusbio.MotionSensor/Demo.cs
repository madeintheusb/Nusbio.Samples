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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.Sensor;

/*
 * Pyroelectric Infrared PIR Motion Sensor Detector Module
 * http://www.amazon.com/gp/product/B008AESDSY/ref=oh_aui_detailpage_o01_s00?ie=UTF8&psc=1
 */

namespace MotionSensorPIRConsole
{
    class Demo
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio Initializing");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            var motionSensorGpio = 0;
            var red              = NusbioGpio.Gpio1;
            var green            = NusbioGpio.Gpio2;

            using (var nusbio = new Nusbio(serialNumber))
            {
                var motionSensor = new DigitalMotionSensorPIR(nusbio, motionSensorGpio, 3);
                
                var redLed       = nusbio.GPIOS[red].AsLed;   // Blink fast for 3 seconds when motion is detected
                var greenLed     = nusbio.GPIOS[green].AsLed; // Blink every 1/2 second just to tell system is running and is ok
                greenLed.SetBlinkMode(500);

                Cls(nusbio);
                while (nusbio.Loop())
                {
                    var motionType = motionSensor.MotionDetected();
                    if (motionType == DigitalMotionSensorPIR.MotionDetectedType.MotionDetected)
                    {
                        ConsoleEx.Write(0, 8, string.Format("[{0}] MotionSensor:{1,-20}", DateTime.Now, motionType), ConsoleColor.DarkCyan);
                        redLed.SetBlinkMode(200);
                    }

                    else if (motionType == DigitalMotionSensorPIR.MotionDetectedType.None)
                    {
                        ConsoleEx.Write(0, 8, string.Format("[{0}] MotionSensor:{1,-20}", DateTime.Now, motionType), ConsoleColor.DarkCyan);
                        redLed.SetBlinkModeOff();
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) break;
                    }
                }
            }
            Console.Clear();
        }
    }
}


  //ConsoleEx.Write(0, 7, string.Format("[{0}] detectionModeOn:{1,-4}", DateTime.Now, detectionModeOn), ConsoleColor.DarkCyan);
                    //if (nusbio.GetButtonUpState())
                    //{
                    //    detectionModeOn = !detectionModeOn;
                    //    nusbio.LEDS[red].SetBlinkModeOff();
                    //    if (detectionModeOn)
                    //        nusbio.LEDS[green].SetBlinkMode(500);
                    //    else
                    //        nusbio.LEDS[green].SetBlinkModeOff();
                    //}

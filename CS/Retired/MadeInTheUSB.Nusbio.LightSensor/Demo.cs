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

using MadeInTheUSB.GPIO;
using MadeInTheUSB.Sensor;

namespace LightSensorConsole
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
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            var lightSensor = NusbioGpio.Gpio1; // Input

            using (var nusbio = new Nusbio(serialNumber)) // , inputGpios: new List<NusbioGpio>() {lightSensor}
            {
                var photocellResistor = new LightSensorWithCapacitor(nusbio, lightSensor);

                photocellResistor.AddCalibarationValue ("Bright"      , 00,  10)
                                  .AddCalibarationValue("OfficeDay"   , 10,  50)
                                  .AddCalibarationValue("OfficeNight" , 50, 100)
                                  .AddCalibarationValue("Darkish"     , 100, 300)
                                  .AddCalibarationValue("CompleteDark", 300, LightSensorWithCapacitor.TimeOutMaxValue);
                
                Cls(nusbio);
                while(nusbio.Loop(2000))
                {
                    ConsoleEx.Write(0, 9, string.Format("[{0}] Value:{1,-4} Calibarated:{2,-15}", 
                        DateTime.Now, 
                        photocellResistor.Value, 
                        photocellResistor.CalibratedValue), 
                        ConsoleColor.DarkCyan);

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

/*

                    int reading = 0;
                    nusbio.GPIOS[lightSensor].DigitalWrite(false);
                    nusbio.ResetGpios(inputGpios: new List<NusbioGpio>() {lightSensor});
                    while (nusbio.GPIOS[lightSensor].DigitalRead() == PinState.Low)
                    {
                        reading++;
                        if (reading >= 30000)
                            break;
                    }
                    nusbio.ResetGpios();
                    ConsoleEx.Write(0, 8, string.Format("[{0}] Reading:{1,-10}", DateTime.Now, reading), ConsoleColor.DarkCyan);*/

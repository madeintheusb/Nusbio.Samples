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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.Components;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;
using MadeInTheUSB.Sensor;

namespace LightSensorConsole
{
    /// <summary>
    /// This demo write and read the SPI EEPROM 25AA256 (32k byte of data).
    /// The bytes can be accessed one at the time or per page of 64 bytes.
    /// To test the code we write the 32k byte of data one page at the time.
    /// To test the code we read the 32k byte of data one page at the time or in batch of 8 to 16 pages.
    /// All 64 bytes page have values : 0..64
    /// Except page 2 which all value are 129
    /// Except page 3 which all value are 170
    /// </summary>
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
            //ConsoleEx.WriteMenu(-1, 4, "");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);

                var tc77 = new TC77(nusbio,
                    NusbioGpio.Gpio0,
                    NusbioGpio.Gpio1,
                    NusbioGpio.Gpio2,
                    NusbioGpio.Gpio4
                    );

                var secondTimeout = new TimeOut(1000);
                
                while(nusbio.Loop())
                {
                    if (secondTimeout.IsTimeOut())
                    {
                        ConsoleEx.WriteLine(0, 3,
                            string.Format("{0:0.00}C {1:0.00}F",tc77.GetTemperature(), tc77.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit))
                            , ConsoleColor.DarkCyan
                        );
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                            
                        if (k == ConsoleKey.W)
                        {
                        }

                        if (k == ConsoleKey.Q) break;

                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}

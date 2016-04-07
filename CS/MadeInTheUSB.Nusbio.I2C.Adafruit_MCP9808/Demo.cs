/*
   Copyright (C) 2015 MadeInTheUSB LLC

   The MIT License (MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

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
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;

namespace LightSensorConsole
{
    class Demo
    {
        private static MCP9808_TemperatureSensor _MCP9808_TemperatureSensor;

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
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");

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

            var clockPin                       = NusbioGpio.None;
            var dataOutPin                     = NusbioGpio.None;
            var useAdafruitI2CAdapterForNusbio = true;

            if (useAdafruitI2CAdapterForNusbio)
            {
                clockPin   = NusbioGpio.Gpio1; // Clock should be zero, but on the Adafruit MCP9808 SCL and SDA are inversed compared to the Adafruit LED matrix
                dataOutPin = NusbioGpio.Gpio0; 
            }
            else
            {
                clockPin = NusbioGpio.Gpio6; // White, Arduino A5
                dataOutPin = NusbioGpio.Gpio5; // Green, Arduino A4
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                _MCP9808_TemperatureSensor = new MCP9808_TemperatureSensor(nusbio, dataOutPin, clockPin);
                if (!_MCP9808_TemperatureSensor.Begin())
                {
                    Console.WriteLine("MCP9808 not detected on I2C bus. Hit any key to retry");
                    var kk = Console.ReadKey();
                    if (!_MCP9808_TemperatureSensor.Begin()) 
                        return;
                }

                Cls(nusbio);

                var everySecond = new TimeOut(1000);

                while(nusbio.Loop())
                {
                    if (everySecond.IsTimeOut())
                    {
                        double celsius = 0;
                        for (var i = 0; i < 3; i++)
                        {
                            celsius = _MCP9808_TemperatureSensor.GetTemperature(TemperatureType.Celsius);
                        }

                        ConsoleEx.WriteLine(1, 2, 
                            string.Format("Temperature Celsius:{0:000.00}, Fahrenheit:{1:000.00}, Kelvin:{2:00000.00}", 
                            celsius,
                            _MCP9808_TemperatureSensor.CelsiusToFahrenheit(celsius),
                            _MCP9808_TemperatureSensor.CelsiusToKelvin(celsius)
                            ), ConsoleColor.Cyan);
                    }
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.T)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.D0)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
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


//#define INCLUDE_TemperatureSensorMCP0908_InDemo
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
 
    Written by FT for MadeInTheUSB
    MIT license, all text above must be included in any redistribution
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
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;

namespace LCDConsole
{
    class Demo
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Demo).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 4, "A)PI demo  Custom cH)ar demo  Nusbio R)ocks  P)erformance Test  Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void PerformanceTest(LiquidCrystal_I2C_PCF8574 lc)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Performance Test");
            
            ConsoleEx.WriteLine(0, 3, "Running test...", ConsoleColor.Gray);

            var testCount = 4;

            var sw = StopWatch.StartNew();
            for (var i = 0; i < testCount; i++)
            {
                lc.Clear();
                lc.Print(0, 0, "01234567890123456789");
                lc.Print(0, 1, "01234567890123456789");
                if (lc.NumLines > 2)
                {
                    lc.Print(0, 2, "01234567890123456789");
                    lc.Print(0, 3, "01234567890123456789");
                }
            }
            sw.Stop();
            ConsoleEx.WriteLine(0, 4, 
                string.Format("Total time:{0} ms, Time to write one 16 chars line:{1} ms", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds/lc.NumLines/testCount),
                ConsoleColor.Gray);

            ConsoleEx.WriteMenu(-1, 1, "Q)uit");
            var k = Console.ReadKey();
        }


        private static MCP9808_TemperatureSensor InitializeI2CAdafruitTemperatureSensorMCP0908_ClockGpio1_DataGpio0(Nusbio nusbio)
        {
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
                clockPin   = NusbioGpio.Gpio6; // White, Arduino A5
                dataOutPin = NusbioGpio.Gpio5; // Green, Arduino A4
            }
            var mcp9808TemperatureSensor = new MCP9808_TemperatureSensor(nusbio, dataOutPin, clockPin);
            if (!mcp9808TemperatureSensor.Begin())
            {
                Console.WriteLine("MCP9808 not detected on I2C bus. Hit any key to retry");
                var kk = Console.ReadKey();
                if (!mcp9808TemperatureSensor.Begin()) 
                    return null;
            }
            return mcp9808TemperatureSensor;
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

            // The PCF8574 has limited speed
            Nusbio.BaudRate = LiquidCrystal_I2C_PCF8574.MAX_BAUD_RATE;

            using (var nusbio = new Nusbio(serialNumber))
            {
                Console.WriteLine("LCD i2c Initialization");
              
                // I2C LCD directly plugged into Nusbio
                var sda       = NusbioGpio.Gpio7;
                var scl       = NusbioGpio.Gpio6;

                if (nusbio.Type == NusbioType.NusbioType1_Light)
                {
                    sda = NusbioGpio.Gpio7; // Directly connected into Nusbio
                    scl = NusbioGpio.Gpio6;
                }

                var maxColumn = 16;
                var maxRow    = 2;

                var lcdI2C = LiquidCrystal_I2C_PCF8574.Detect(nusbio, maxColumn, maxRow, sda, scl);
                if(lcdI2C == null)
                {
                    Console.WriteLine("Hit any key to continue");
                    Console.ReadKey();
                    return;
                }

                lcdI2C.Backlight();
                lcdI2C.Print(0, 0, "Hi!");

                /// This temperature sensor is used to also demo how to connect
                /// multiple devices directly into Nusbio by using the Nsubio Expander Extension
                /// https://squareup.com/market/madeintheusb-dot-net/ex-expander
                MCP9808_TemperatureSensor _MCP9808_TemperatureSensor = null;
                #if INCLUDE_TemperatureSensorMCP0908_InDemo
                    _MCP9808_TemperatureSensor = InitializeI2CAdafruitTemperatureSensorMCP0908_ClockGpio1_DataGpio0(nusbio);
                #endif
                var timeOut = new TimeOut(1000);

                Cls(nusbio);

                while(nusbio.Loop())
                {
                    if (_MCP9808_TemperatureSensor != null && timeOut.IsTimeOut())
                    {
                        lcdI2C.Print(0, 0, DateTime.Now.ToString("T"));
                        lcdI2C.Print(0, 1, "Temp {0:00}C {1:00}F", _MCP9808_TemperatureSensor.GetTemperature(TemperatureType.Celsius), _MCP9808_TemperatureSensor.GetTemperature(TemperatureType.Fahrenheit));
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q)
                        {
                            nusbio.ExitLoop();
                        }
                        if (k == ConsoleKey.P)
                        {
                            PerformanceTest(lcdI2C);
                            lcdI2C.Clear();
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                            lcdI2C.Clear();
                        }
                        if (k == ConsoleKey.A)
                        {
                            LiquidCrystalDemo.ApiDemo(lcdI2C);
                            lcdI2C.Clear();
                        }
                        if (k == ConsoleKey.R)
                        {
                            LiquidCrystalDemo.NusbioRocks(lcdI2C, 333);
                            lcdI2C.Clear();
                        }
                        if (k == ConsoleKey.H)
                        {
                            LiquidCrystalDemo.CustomCharDemo(lcdI2C);
                        }
                        if (k == ConsoleKey.T)
                        {
                            LiquidCrystalDemo.ProgressBarDemo(lcdI2C);
                            LiquidCrystalDemo.NusbioRocksOrWhat(lcdI2C);
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}


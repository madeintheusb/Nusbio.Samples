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

namespace MadeInTheUSB
{
    class Demo
    {
        private static MCP4725_12BitDac _dac;

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

            ConsoleEx.TitleBar(4, "0) Reset  T)est  Q)uit", ConsoleColor.White, ConsoleColor.DarkBlue);

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        private static void Reset0()
        {
            _dac.SetVoltage(0, false);
        }

        private static void Test()
        {
            try { 
                int waitTime = 1600;
                int step = 400;
                Console.Clear();

                _dac.SetVoltage(0, false);
                _dac.SetVoltage(MCP4725_12BitDac.MAX_DIGITAL_VALUE/4*2, false);
                _dac.SetVoltage(MCP4725_12BitDac.MAX_DIGITAL_VALUE/4*3, false);
                _dac.SetVoltage(MCP4725_12BitDac.MAX_DIGITAL_VALUE, false);

                int dv;
                for (dv = 0; dv < MCP4725_12BitDac.MAX_DIGITAL_VALUE; dv += step)
                {
                    Console.WriteLine(string.Format("Digital Value:{0:00000}, Expected Voltage:{1:0.00}", dv, _dac.ComputeVoltage(dv, 5)));
                    _dac.SetVoltage(dv, false);
                    TimePeriod.Sleep(waitTime);
                    if(Console.KeyAvailable)
                        return;
                }
                for (dv = MCP4725_12BitDac.MAX_DIGITAL_VALUE; dv > 0; dv -= step)
                {
                    Console.WriteLine(string.Format("Digital Value:{0:00000}, Expected Voltage:{1:0.00}", dv, _dac.ComputeVoltage(dv, 5)));
                    _dac.SetVoltage(dv, false);
                    TimePeriod.Sleep(waitTime);
                    if(Console.KeyAvailable)
                        return;
                }
            }
            finally
            {
                _dac.SetVoltage(0, false);
            }
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            //var serialNumber = "LD2Ub9pAg";
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }
            
            var clockPin           = NusbioGpio.Gpio0; // White, Arduino A5
            var dataOutPin         = NusbioGpio.Gpio1; // Green, Arduino A4
            var dataInPin          = NusbioGpio.Gpio2; // Orange
            
       
            byte DAC_I2C_ADDR = 0x62;  

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                Cls(nusbio);

                 _dac = new   MCP4725_12BitDac(nusbio, dataOutPin, clockPin, DAC_I2C_ADDR);
                Reset0();

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.T)
                        {
                            Test();
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.D0)
                        {
                            Reset0();
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


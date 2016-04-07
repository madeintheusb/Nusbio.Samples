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
        private static _4Digits7Segments _4digits;

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

            ConsoleEx.WriteMenu(-1, 3, "1) Demo 1  2) Demo 2  3) Demo 3  4) Demo 4");
            ConsoleEx.WriteMenu(-1, 5, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        
        public static void DemoScrollNumber()
        {
            _4digits.Clear(true);

            for (var i = 0; i < 10; i++) { 

                _4digits.Clear();
                _4digits.GotoX(i % 4);
                _4digits.Write(i, false, false);
                _4digits.WriteDisplay();
                TimePeriod.Sleep(330);
            }
            TimePeriod.Sleep(330);
        }

        public static void Demo1()
        {
            _4digits.Clear(true);

            _4digits.GotoX(0);

            _4digits.Write((byte)(((byte)'0') + 1), refresh: true);
            _4digits.Write((byte)(((byte)'0') + 0), refresh: true);
            _4digits.Write((byte)(((byte)'0') + 3), refresh: true);
            _4digits.Write((byte)(((byte)'0') + 2), refresh: true);
            

            for(var i = 0; i < 10; i++) { 

                _4digits.GotoX(0);

                for(var c = 0; c < 4; c++) { 

                    var cc = ((byte)'0') + i;
                    _4digits.Write((byte)cc, refresh: false);
                }
                _4digits.WriteDisplay();
                TimePeriod.Sleep(250);
            }
        }

         public static void Demo1To10000()
        {
            _4digits.Clear(true);

            for(var i = 0; i <= 10000; i += 11) {  

                _4digits.Clear();
                _4digits.Write(i, refresh: true);
                TimePeriod.Sleep(64);

                if (Console.KeyAvailable) { 
                    Console.ReadKey(true);
                    break;
                }
            }
        }

        public static void Demo1To100()
        {
            _4digits.Clear(true);

            for(var i = 0; i <= 100; i += 1) {  

                _4digits.Clear();
                _4digits.Write(i, refresh: true);
                TimePeriod.Sleep(64);
                if (Console.KeyAvailable) { 
                    Console.ReadKey(true);
                    break;
                }
            }
        }

        public static void DoubleDemo()
        {
            _4digits.Clear(true);

            for(double i = 0; i < 9999; i += 128.128) {  

                _4digits.Write(i, refresh: true);
                TimePeriod.Sleep(128);
                if (Console.KeyAvailable) { 
                    Console.ReadKey(true);
                    break;
                }
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
                        
            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                Cls(nusbio);

                var _4DIGITS7SEGMENTS_ADDR = 0x70 + 2;
                var clockPin               = NusbioGpio.Gpio0; // White
                var dataOutPin             = NusbioGpio.Gpio1; // Green
                _4digits                   = new _4Digits7Segments(nusbio, dataOutPin, clockPin);
                _4digits.Begin(_4DIGITS7SEGMENTS_ADDR);
                _4digits.Clear(true);
                
                var oneSecondTimeOut = new TimeOut(1000);

                while(nusbio.Loop())
                {
                    if(oneSecondTimeOut.IsTimeOut()) { // Make the colon blink at a 1 second rate
                        
                        var t = DateTime.Now;

                        _4digits.Clear();
                        _4digits.Write(string.Format("{0}{1}", t.Minute.ToString("00"), t.Second.ToString("00")));

                        //if(twoSecondTimeOut.Counter % 2 == 0) 
                        //    _4digits.Write(string.Format("{0}{1}", t.Minute.ToString("00"), t.Second.ToString("00")));
                        //else
                        //    _4digits.Write(string.Format("{0}{1}", t.Hour.ToString("00"), t.Minute.ToString("00")));

                        _4digits.WriteDisplay();
                        _4digits.DrawColon(!_4digits.ColonOn);
                    }

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.D1)
                        {
                            Demo1();
                        }
                        if (k == ConsoleKey.D2)
                        {
                            Demo1To100();
                        }
                        if (k == ConsoleKey.D3)
                        {
                            DemoScrollNumber();
                        }
                        if (k == ConsoleKey.D4)
                        {
                            Demo1To10000();
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                            _4digits.Clear(true);
                        }
                        if (k == ConsoleKey.Q) {
                            _4digits.Clear(true);
                            break;
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}


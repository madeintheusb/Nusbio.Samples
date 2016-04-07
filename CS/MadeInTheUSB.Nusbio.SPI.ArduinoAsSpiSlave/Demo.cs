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
using MadeInTheUSB.spi;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;

namespace TestConsole
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
            ConsoleEx.WriteMenu(-1, 4, "Test: I)N Gpio+Analog port in  O)UT Gpio out");
            ConsoleEx.WriteMenu(-1, 5, "N)eoPixel  P)erformance  Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static int ArduinoApi(SPIEngine spiEngine, string cmd)
        {
            int v1 = -1, v2 = -1;
            var buffer = ASCIIEncoding.ASCII.GetBytes(cmd+"\n");
            var r = spiEngine.Transfer(buffer.ToList());

            // Send dummy byte 0 to read answer byte 0
            var r2 = spiEngine.Transfer(new List<byte>() { 0 }); // dummy
            if (r2.Succeeded)
                v1 = r2.ReadBuffer[0];

            // Send dummy byte 1 to read answer byte 1
            var r3 = spiEngine.Transfer(new List<byte>() { 1 }); // dummy
            if (r3.Succeeded)
                v2 = r3.ReadBuffer[0];

            return (v2 << 8) + v1; 
        }

        public static void TestGpioOut(ArduinoUnoSPISlave a)
        {
            Console.Clear();
            var goOn = true;

            if (!a.SetPinMode(8, PinMode.Output)) throw new ApplicationException();
            if (!a.SetPinMode(9, PinMode.Output)) throw new ApplicationException();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 25; i++)
            {
                a.DigitalWrite(8, PinState.High);
                a.DigitalWrite(9, PinState.Low);
                a.DigitalWrite(8, PinState.Low);
                a.DigitalWrite(9, PinState.High);
            }
            sw.Stop();
            Console.WriteLine("Total:{0}ms, DigitalWrite:{1}ms",
                sw.ElapsedMilliseconds, sw.ElapsedMilliseconds/ (25*4)
                );

            while (goOn)
            {
                ConsoleEx.WriteLine(0, 2, string.Format("DigitalWrite({0}):{1}              ", 8, PinState.High), ConsoleColor.Cyan);
                a.DigitalWrite(8, PinState.High);
                ConsoleEx.WriteLine(0, 3, string.Format("DigitalWrite({0}):{1}              ", 9, PinState.Low), ConsoleColor.Cyan);
                a.DigitalWrite(9, PinState.Low);
                Thread.Sleep(250);
                
                ConsoleEx.WriteLine(0, 2, string.Format("DigitalWrite({0}):{1}              ", 8, PinState.Low), ConsoleColor.Cyan);
                a.DigitalWrite(8, PinState.Low);
                ConsoleEx.WriteLine(0, 3, string.Format("DigitalWrite({0}):{1}              ", 9, PinState.High), ConsoleColor.Cyan);
                a.DigitalWrite(9, PinState.High);
                Thread.Sleep(250);

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        goOn = false;
                        break;
                    }
                }
                //Thread.Sleep(100);
            }
            Console.WriteLine("Done - hit space");
        }

        private static List<byte> ShiftList(List<byte> vals)
        {
            var f = vals[0];
            vals.RemoveAt(0);
            vals.Add(f);
            return vals;
        }

        public static void TestNeoPixel(ArduinoUnoSPISlave a)
        {
            Console.Clear();
            var goOn = true;

            const int MAX_LED = 7;
            a.NeoPixelInit(0, 3, MAX_LED);

            var colors = new List<byte>()
            {
                200, 000, 000, // red
                000, 200, 000, // green
                000, 000, 200, // blue
                200, 000, 000, // red
                000, 200, 000, // green
                000, 000, 200, // blue
                064, 064, 064, // white
            };
            //colors = new List<byte>()
            //{
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //    200, 000, 000, // red
            //};

            while (goOn)
            {
                a.NeoPixelSet(0, 32, 0, MAX_LED, colors.ToArray());
                Thread.Sleep(150);
                colors = ShiftList(ShiftList(ShiftList(colors)));

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        goOn = false;
                        break;
                    }
                }
            }
            Console.WriteLine("Done - hit space");
        }
        

        public static void TestGpioAnalogPortIn(ArduinoUnoSPISlave a)
        {
            Console.Clear();
            var goOn = true;

            while (goOn)
            {
                for (var g = a.MinGpioIndex; g <= a.MaxGpioIndex; g++)
                {
                    ConsoleEx.WriteLine(0, g+2, string.Format("digitalRead({0}):{1}              ", g, a.DigitalRead(g)), ConsoleColor.Cyan);
                }
                for (var g = a.MinAnalogIndex; g <= a.MaxAnalogIndex; g++)
                {
                    ConsoleEx.WriteLine(0, g+2+a.MaxGpioIndex, string.Format("analogRead({0}):{1}              ", g, a.AnalogRead(g)), ConsoleColor.Cyan);
                }
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        goOn = false;
                        break;
                    }
                }
                //Thread.Sleep(100);
            }
            Console.WriteLine("Done - hit space");
            var kk = Console.ReadKey();
        }

        public static void TestPerformance(ArduinoUnoSPISlave a)
        {
            Console.Clear();

            var goOn       = true;
            var dataString = "".PadLeft(ArduinoUnoSPISlave.MAX_BUFFER_TRANSFERT_SIZE-1, 'A');
            var buffer     = new List<byte>();
            buffer.AddRange(ASCIIEncoding.ASCII.GetBytes(dataString));
            var maxTests = 100;
            while (goOn)
            {
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < maxTests; i++)
                {
                    Console.Write(".");
                    if (!a.SendBuffer(buffer)) 
                        throw new ApplicationException("SendBuffer failed");
                }
                sw.Stop();
                Console.WriteLine("");
                Console.WriteLine("Total byte:{0}, Total time:{1}, {2:0.00} Kb/s",
                    ArduinoUnoSPISlave.MAX_BUFFER_TRANSFERT_SIZE*maxTests,
                    sw.ElapsedMilliseconds,
                    1000.0*ArduinoUnoSPISlave.MAX_BUFFER_TRANSFERT_SIZE*maxTests/1024.0/sw.ElapsedMilliseconds
                    );
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        goOn = false;
                        break;
                    }
                }
            }
            Console.WriteLine("Done - hit space");
            var kk = Console.ReadKey();
        }

        public static void Run(string[] args)
        {
            ushort a = 512 + 1;
            byte b1 = (byte)(a >> 8);
            byte b2 = (byte)(a & 0xFF);

            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            // { 200, 300, 9600, 38400, 57600, 76800, 115200, 230400 };
            Nusbio.BaudRate = 230400;

            using (var nusbio = new Nusbio(serialNumber))
            {
                var  arduinoUno = new ArduinoUnoSPISlave(ArduinoUnoSPISlave.ArduinoType.ArduinoUno, nusbio,
                    selectGpio: NusbioGpio.Gpio3,
                    mosiGpio:   NusbioGpio.Gpio1,
                    misoGpio:   NusbioGpio.Gpio2,
                    clockGpio:  NusbioGpio.Gpio0
                    );
                // Arduino Uno SPI Pin
                //  CS      10
                //  MOSI    11
                //  MISO    12
                //  CLOCK   13  
                //var spiEngine = new SPIEngine(nusbio,
                //    selectGpio: NusbioGpio.Gpio3,
                //    mosiGpio: NusbioGpio.Gpio1,
                //    misoGpio: NusbioGpio.Gpio2,
                //    clockGpio: NusbioGpio.Gpio0
                //    );

                Cls(nusbio);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q)
                        {
                            nusbio.ExitLoop();
                        }
                        if (k == ConsoleKey.P)
                        {
                            TestPerformance(arduinoUno);
                        }
                        if (k == ConsoleKey.I)
                        {
                            TestGpioAnalogPortIn(arduinoUno);
                        }
                        if (k == ConsoleKey.N)
                        {
                            TestNeoPixel(arduinoUno);
                        }
                        if (k == ConsoleKey.O)
                        {
                            TestGpioOut(arduinoUno);
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}


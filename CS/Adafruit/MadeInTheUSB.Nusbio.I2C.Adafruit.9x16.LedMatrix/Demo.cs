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
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.Display;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;
using MadeInTheUSB.WinUtil;

namespace MadeInTheUSB
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

            ConsoleEx.WriteMenu(-1, 5, "B)ar scroll demo  I)ntensisty scroll demo  inT)ensisty demo 2  L)andscape demo");
            ConsoleEx.WriteMenu(-1, 7, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);

            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        private static bool BarScrollDemo1(IS31FL3731 ledMatrix16x9)
        {
            var doubleBufferIndex = 1;
            ledMatrix16x9.Clear(doubleBufferIndex);
            ledMatrix16x9.SelectFrame(doubleBufferIndex);

            while (true)
            {
                for (var x = 0; x < ledMatrix16x9.Width; x++)
                {
                    ledMatrix16x9.Clear(doubleBufferIndex, false);

                    for (int y = 0; y < ledMatrix16x9.Height; y++)
                    {
                        ledMatrix16x9.SetLedPwm(x, y, 32, doubleBufferIndex);
                    }
                    var bytePerSecond = ledMatrix16x9.UpdateDisplay(doubleBufferIndex);
                    ConsoleEx.WriteLine(0, 1, string.Format("x:{0:000}, {1:0.00} K byte/sec sent", x, bytePerSecond/1024.0), ConsoleColor.Cyan);

                    doubleBufferIndex = doubleBufferIndex == 1 ? 0 : 1;
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) return false;
                    Thread.Sleep(10);
                }
            }
            return true;
        }

        static bool FadeInFadeout(IS31FL3731 ledMatrix16x9)
        {
            var maxIntensisty = 32;
            var nextFrameIndex = 1;
            for (var incr = 1; incr < maxIntensisty; incr++)
            {
                ledMatrix16x9.SelectFrame(nextFrameIndex);
                for (int x = 0; x < ledMatrix16x9.Width; x++)
                    for (int y = 0; y < ledMatrix16x9.Height; y++)
                        ledMatrix16x9.SetLedPwm(x, y, incr, nextFrameIndex);
                ledMatrix16x9.UpdateDisplay(nextFrameIndex);
                nextFrameIndex = nextFrameIndex == 1 ? 0 : 1;
                if (incr > 128)
                    break;
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) return false;
            }

            Thread.Sleep(100);

            for (var incr = maxIntensisty; incr > 1; incr--)
            {
                ledMatrix16x9.SelectFrame(nextFrameIndex);
                for (int x = 0; x < ledMatrix16x9.Width; x++)
                    for (int y = 0; y < ledMatrix16x9.Height; y++)
                        ledMatrix16x9.SetLedPwm(x, y, incr, nextFrameIndex);
                ledMatrix16x9.UpdateDisplay(nextFrameIndex);
                nextFrameIndex = nextFrameIndex == 1 ? 0 : 1;
                if (incr > 128)
                    break;
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) return false;
            }

            Thread.Sleep(200);

            return true;
        }

        static void BarScrollDemo(IS31FL3731 ledMatrix16x9)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Bar Scroll Demo", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 5, "Q)uit");
            var quit  = false;
            ledMatrix16x9.Clear();
            while (!quit)
            {
                if(!BarScrollDemo1(ledMatrix16x9)) quit = true;
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q)
                        quit = true;
                }       
            }
        }

        static void IntensistyScrollingDemo(IS31FL3731 ledMatrix16x9)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Intensisty Scrolling Demo", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(0, 2, "Q)uit");
            var sweep = new List<int>() {
                1, 2, 3, 4, 6, 8, 10, 15, 20, 30, 40, 60, 60, 40, 30, 20, 15, 10, 8, 6, 4, 3, 2, 1
            };

            var quit              = false;
            var incr              = 0;
            var doubleBufferIndex = 1;
            ledMatrix16x9.Clear();
            int modulo = 24;

            while (!quit)
            {
                for (int y=0; y<ledMatrix16x9.Height; y++) {

                    ConsoleEx.Write(0, y+4, string.Format("{0:00} - ", y), ConsoleColor.Cyan);

                    for (int x = 0; x < ledMatrix16x9.Width; x++ ) {

                        var intensity = sweep[(x + y + incr) % modulo];
                        ledMatrix16x9.DrawPixel(x, y, intensity);
                        Console.Write("{0:000} ", intensity);
                    }
                }
                var bytePerSecond = ledMatrix16x9.UpdateDisplay(doubleBufferIndex);
                ConsoleEx.WriteLine(0, 15, string.Format("{0:0.00} K byte/sec sent", bytePerSecond/1024.0), ConsoleColor.Cyan);
                doubleBufferIndex = doubleBufferIndex == 1 ? 0 : 1;
                incr++;

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q)
                        quit = true;
                }       
            }
        }

        static void IntensistyScrollingDemo2(IS31FL3731 ledMatrix16x9)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Intensisty Demo", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(0, 2, "Q)uit");
            var sweep = new List<int>() {
                1, 2, 8, 16, 24, 32, 44, 64, 80, 96, 120, 128, 120, 96, 80, 64, 44, 32, 24, 16, 8, 4, 2, 1
            };

            var quit              = false;
            var incr              = 1;

            var moduleStepIndex = 0;
            var moduleSteps = new List<int>() { 2, 4, 6, 8, 10, 12, 16, 18, 20, 22 };
            moduleSteps = new List<int>() { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };

            var doubleBufferIndex = 1;
            ledMatrix16x9.Clear();

            while (!quit)
            {
                for (int y = 0; y < ledMatrix16x9.Height; y++)
                {
                    ConsoleEx.Write(0, y + 4, string.Format("{0:00} - ", y), ConsoleColor.Cyan);

                    for (int x = 0; x < ledMatrix16x9.Width; x++)
                    {
                        var intensity = sweep[(x + y + incr) % moduleSteps[moduleStepIndex]];
                        ledMatrix16x9.DrawPixel(x, y, intensity);
                        Console.Write("{0:000} ", intensity);

                        moduleStepIndex++;
                        if (moduleStepIndex >= moduleSteps.Count)
                            moduleStepIndex = 0;
                    }
                }
                var bytePerSecond = ledMatrix16x9.UpdateDisplay(doubleBufferIndex);
                ConsoleEx.WriteLine(0, 15, string.Format("{0:0.00} K byte/sec sent", bytePerSecond / 1024.0), ConsoleColor.Cyan);
                doubleBufferIndex = doubleBufferIndex == 1 ? 0 : 1;
                incr++;
                Thread.Sleep(50);

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q)
                        quit = true;
                }
            }
        }

        private static void LandscapeDemo(IS31FL3731 ledMatrix16x9)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Random Landscape Demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit  F)ull speed");
            var landscape = new LandscapeIS31FL3731(ledMatrix16x9);

            var speed = 32;
            ledMatrix16x9.SelectFrame(0);
            ledMatrix16x9.Clear();
            var quit = false;
            var fullSpeed = false;

            while (!quit)
            {
                var bytePerSecond = landscape.Redraw();
                ConsoleEx.WriteLine(0, 15, string.Format("{0:0.00} K byte/sec sent", bytePerSecond / 1024.0), ConsoleColor.Cyan);

                ConsoleEx.WriteLine(0, 4, landscape.ToString(), ConsoleColor.Cyan);
                if (!fullSpeed)
                    Thread.Sleep(speed);

                if (Console.KeyAvailable)
                {
                    switch (Console.ReadKey(true).Key)
                    {
                        case ConsoleKey.Q: quit = true; break;
                        case ConsoleKey.F:
                            fullSpeed = !fullSpeed; break;
                    }
                }
            }
        }



        static void IntensistyDemo(IS31FL3731 ledMatrix16x9)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Intensisty Demo", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(0, 2, "Q)uit");
            var quit = false;
            var doubleBufferIndex = 1;

            // 8 intensities for each row
            // After 160, the intensity remain the same
            var intensities = new List<int>() { 2, 8, 16, 32, 32 + 12, 64, 96, 96 + 24, 96 + 32 };
            var intensityIndex = 0;

            ledMatrix16x9.Clear();

            while (!quit)
            {
                intensityIndex = 0;
                for (int y = 0; y < ledMatrix16x9.Height; y++)
                {
                    ConsoleEx.Write(0, y + 4, string.Format("{0:00} - ", y), ConsoleColor.Cyan);
                    for (int x = 0; x < ledMatrix16x9.Width; x++)
                    {
                        ledMatrix16x9.DrawPixel(x, y, intensities[intensityIndex]);
                        Console.Write("{0:000} ", intensities[intensityIndex]);
                    }
                    if (++intensityIndex >= intensities.Count)
                        intensityIndex = 0;
                }
                var bytePerSecond = ledMatrix16x9.UpdateDisplay(doubleBufferIndex);
                ConsoleEx.WriteLine(0, 15, string.Format("{0:0.00} K byte/sec sent", bytePerSecond / 1024.0), ConsoleColor.Cyan);
                doubleBufferIndex = doubleBufferIndex == 1 ? 0 : 1;

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q)
                        quit = true;
                }
            }
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
            
            var sclPin             = NusbioGpio.Gpio0; // White
            var sdaPin             = NusbioGpio.Gpio1; // Green

            // With the Nusbio Adafruit I2C Adapter we need to reverse sda and scl
            // So we can plug the IS31FL3731 in the adpater
            var nusbioAdafruitI2CAdadpater = !true;
            if (nusbioAdafruitI2CAdadpater) 
            {
                sclPin = NusbioGpio.Gpio1;
                sdaPin = NusbioGpio.Gpio0; 
            }

            // The chip IS31FL3731 is I2C 400 000 Hz
            // http://www.issi.com/WW/pdf/31FL3731.pdf
            // Nusbio default is 912 600 baud which is 
            Nusbio.BaudRate = IS31FL3731.MAX_BAUD_RATE/8;

            using (var nusbio = new Nusbio(serialNumber))
            {
                var ledMatrix16x9 = new IS31FL3731(nusbio, sdaPin, sclPin);
                if (!ledMatrix16x9.Begin())
                {
                    Console.WriteLine("Led matrix not detected");
                    return;
                }

                Cls(nusbio);
                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.B)
                        {
                            BarScrollDemo(ledMatrix16x9);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.I)
                        {
                            IntensistyScrollingDemo(ledMatrix16x9);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.T)
                        {
                            IntensistyDemo(ledMatrix16x9);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.L)
                        {
                            LandscapeDemo(ledMatrix16x9);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q)
                        {
                            nusbio.ExitLoop();
                        }
                    }
                }
            }
            Console.Clear();
        }
    }
}


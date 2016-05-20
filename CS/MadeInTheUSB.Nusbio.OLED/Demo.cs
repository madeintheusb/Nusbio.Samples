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
            ConsoleEx.WriteMenu(-1, 4, "C)ircle demo  F)ractal circle demo  T)ext demo");
            ConsoleEx.WriteMenu(-1, 5, "R)ectangle demo  P)erformance rectangle demo  I)nit");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void RectangleDemo(OLED _oledDisplay, int wait)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Rectangle demo");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            if (wait > 0)
                _oledDisplay.DrawWindow("Rectangle Demo", "Slow Speed");
            else
                _oledDisplay.DrawWindow("Rectangle Demo", "Full Speed");
            TimePeriod.Sleep(1000*2);

            var sw = Stopwatch.StartNew();

            for (var j = 1; j < 64; j++)
            {
                _oledDisplay.Clear(refresh: false);
                for (var i = 0; i < _oledDisplay.Height; i += j)
                {
                    _oledDisplay.DrawRect(i, i, _oledDisplay.Width - (i*2), _oledDisplay.Height - (i*2), true);
                    Console.WriteLine("Rectangle {0:000},{1:000} {2:000},{3:000}", i, i, _oledDisplay.Width - (i*2), _oledDisplay.Height - (i*2));
                }
                try {
                    // With Nusbio v 1.0A - FT232RL based this may throw
                    _oledDisplay.WriteDisplay();
                }
                catch { }
                if(wait > 0)
                    TimePeriod.Sleep(wait);
            }
            sw.Stop();
            _oledDisplay.DrawWindow("Rectangle demo", "The End.");
            Console.WriteLine("Execution Time:{0}. Hit space to continue", sw.ElapsedMilliseconds);
            var k = Console.ReadKey();
        }

        public static void DrawCircleFractal(OLED _oledDisplay, int x, int y, int r)
        {
            _oledDisplay.DrawCircle(x, y, r, true);
            _oledDisplay.DrawCircle(x+r, y, r/2, true);
            _oledDisplay.DrawCircle(x-r, y, r/2, true);
            _oledDisplay.WriteDisplay();
        }

        public static void CircleFractalDemo(OLED _oledDisplay, bool clearScreen)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Circle fractal demo");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");
            var rnd = new Random();
            var wait = 00;

            var sw = Stopwatch.StartNew();

            _oledDisplay.Clear(refresh: true);
            var r = 16;
            var x = _oledDisplay.Width/2-1;
            var y = _oledDisplay.Height/2-1;
            var maxStep = 16;
                
            for (var i = 0; i < maxStep; i += r/4)
            {
                if (clearScreen) _oledDisplay.Clear(false);
                DrawCircleFractal(_oledDisplay, x+i, y-i, r);
                //oledDisplay.WriteDisplay();
            }
            for (var i = 0; i < maxStep; i += r/4)
            {
                if (clearScreen) _oledDisplay.Clear(false);
                DrawCircleFractal(_oledDisplay, x-i, y+i, r);
                //oledDisplay.WriteDisplay();
            }
            for (var i = 0; i < maxStep; i += r/4)
            {
                if (clearScreen) _oledDisplay.Clear(false);
                DrawCircleFractal(_oledDisplay, x+i, y+i, r);
                //oledDisplay.WriteDisplay();
            }
            for (var i = 0; i < maxStep; i += r/4)
            {
                if (clearScreen) _oledDisplay.Clear(false);
                DrawCircleFractal(_oledDisplay, x-i, y-i, r);
                //oledDisplay.WriteDisplay();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public static void CircleDemo(OLED _oledDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Circle Demo");
            ConsoleEx.WriteMenu(0, 1, "Q)uit");
            var rnd = new Random();

            _oledDisplay.Clear(refresh: true);
            for (var i = 0; i < 16; i += 1)
            {
                var r = rnd.Next(2, 16);
                var x = rnd.Next(r+1, _oledDisplay.Width-r);
                var y = rnd.Next(r+1, _oledDisplay.Height-r);
                _oledDisplay.DrawCircle(x, y, r, true);
                _oledDisplay.WriteDisplay();

                Console.WriteLine("Circle {0:000},{1:000} r:{2:000}", x,y, r);
                TimePeriod.Sleep(125);
            }
        }

        public static void ExperimentDemo(OLED oledDisplay)
        {
            Console.Clear();
            
            ConsoleEx.TitleBar(0, "Text demo");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            //oledDisplay.Clear(refresh:true);
            //Thread.Sleep(1000);
            oledDisplay.Clear(true);
            for (var y = 0; y < 16; y += 1)
            {
                for (var x = 0; x < 128; x += 1)
                {
                    oledDisplay.SetPixel(x, y, true);
                    if (y > oledDisplay.Height - 24)
                        y = 0;
                }
                oledDisplay.WriteDisplay();
            }
        }

        public static void TextDemo(OLED oledDisplay)
        {
            Console.Clear();
            
            ConsoleEx.TitleBar(0, "Text demo");
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");

            oledDisplay.DrawWindow(" NUSBIO ");

            var messages = new List<string>()
            {
                "Hello World",
                "Hello World.",
                "Hello World..",
                "Hello World...",
                "Written in C#",
                "or VB.net",
                "Could be done in F#",
                "PowerShell? Maybe!",
            };
            
            var messageIndex = 0;

            var goOn = true;
            while (goOn)
            {
                oledDisplay.WriteString(-1, 6*8, DateTime.Now.ToString());
                oledDisplay.WriteString(-1, 3*8, messages[messageIndex]);
                oledDisplay.WriteDisplay();
                oledDisplay.WriteString(-1, 3*8, messages[messageIndex], clearText: true);
                messageIndex++;
                if (messageIndex == messages.Count)
                    messageIndex = 0;

                ConsoleEx.WriteLine(0, 2, "Message Displayed:", ConsoleColor.Cyan);
                ConsoleEx.WriteLine(19, 2, messages[messageIndex].PadRight(64), ConsoleColor.Yellow);

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey().Key;
                    if(k == ConsoleKey.Q)  goOn = false;
                }
                Thread.Sleep(900);
            }
            oledDisplay.Clear(refresh:true);
        }


        private static OLED InitializeOLED(Nusbio nusbio)
        {
            OLED oledDisplay = OLED_SSD1306.Create_128x64_09Inch_DirectlyIntoNusbio(nusbio);
            // Gpio7 is set to low to be the ground
            //oledDisplay = OLED_SH1106.Create_128x64_13Inch_DirectlyIntoNusbio(nusbio);
            oledDisplay.Debug = false;
            oledDisplay.Begin();
            return oledDisplay;
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
            //Nusbio.BaudRate = 9600;

            using (var nusbio = new Nusbio(serialNumber))
            {
                Console.WriteLine("OLED Display Initialization");

                OLED oledDisplay = InitializeOLED(nusbio);
                oledDisplay.DrawWindow(" NUSBIO ", "Ready...");
                Cls(nusbio);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.I)
                        {
                            oledDisplay = InitializeOLED(nusbio);
                        }
                        if (k == ConsoleKey.Q)
                        {
                            oledDisplay.Clear(true);
                            nusbio.ExitLoop();
                        }
                        if (k == ConsoleKey.R)
                        {
                            RectangleDemo(oledDisplay, 80);
                            RectangleDemo(oledDisplay, 0);
                        }
                        if (k == ConsoleKey.P)
                        {
                            RectangleDemo(oledDisplay, 0);
                        }
                        if (k == ConsoleKey.T)
                        {
                            TextDemo(oledDisplay);
                        }
                        if (k == ConsoleKey.C)
                        {
                            CircleDemo(oledDisplay);
                        }
                        if (k == ConsoleKey.F)
                        {
                            CircleFractalDemo(oledDisplay, true);
                            CircleFractalDemo(oledDisplay, false);
                        }
                        if (k == ConsoleKey.H)
                        {
                            Cls(nusbio);
                            oledDisplay.Clear();
                        }
                        Cls(nusbio);
                        oledDisplay.DrawWindow(" NUSBIO ", "Ready...");
                    }
                }
            }
            Console.Clear();
            
        }
    }
}




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
    
    This program controls up to 2 Adafruit 8x8 LED matrix with backpack small or large
  
        https://learn.adafruit.com/adafruit-led-backpack/overview
 
            Small:
                Adafruit Mini 0.8" 8x8 LED Matrix w/I2C Backpack - Yellow-Green https://www.adafruit.com/product/872
                Adafruit Small 1.2" 8x8 LED Matrix w/I2C Backpack - Red https://www.adafruit.com/product/1049
 
            Large:
  
            Small 1.2" 8x8 Ultra Bright Square Amber LED Matrix + Backpack https://www.adafruit.com/products/1854
            Small 1.2" 8x8 Bright Square Pure Green LED Matrix + Backpack https://www.adafruit.com/products/1856
  
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
        private static LEDBackpack             _ledMatrix00;
        private static LEDBackpack             _ledMatrix01;
        private static LEDBackpack             _ledMatrix02;
        private static LEDBackpack             _ledMatrix03;
        private static MultiLEDBackpackManager _multiLEDBackpackManager;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        private static List<string> smileBmp = new List<string>()
        {
            "B00111100",
            "B01000010",
            "B10100101",
            "B10000001",
            "B10100101",
            "B10011001",
            "B01000010",
            "B00111100",
        };

        private static List<string> neutralBmp = new List<string>()
        {
            "B00111100",
            "B01000010",
            "B10100101",
            "B10000001",
            "B10111101",
            "B10000001",
            "B01000010",
            "B00111100",
        };

        private static List<string> frownbmp = new List<string>()
        { 
            "B00111100",
            "B01000010",
            "B10100101",
            "B10000001",
            "B10011001",
            "B10100101",
            "B01000010",
            "B00111100",
        };

        static void DisplayImage()
        {
            int MAX_REPEAT = 5;
            int wait       = 400;

            ConsoleEx.Bar(0, 5, "DrawBitmap Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            for (byte rpt = 0; rpt <= MAX_REPEAT; rpt++)
            {
                var images = new List<List<string>> {neutralBmp, smileBmp, neutralBmp, frownbmp};
                foreach (var image in images)
                {
                    _multiLEDBackpackManager.Clear(refresh: false);
                    _multiLEDBackpackManager.DrawBitmap(0, 0, BitUtil.ParseBinary(image), 8, 8, 1);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait);
                }
            }
        }

        static void Animate()
        {
            int wait       = 100;
            int waitPixelDemo = 10;
            int maxRepeat = 5;

            DrawRoundRectDemo(wait, maxRepeat);

            _multiLEDBackpackManager.SetRotation(0);
            DrawPixelDemo(maxRepeat, waitPixelDemo);

            _multiLEDBackpackManager.SetRotation(1);
            DrawPixelDemo(maxRepeat, waitPixelDemo);

            _multiLEDBackpackManager.SetRotation(2);
            DrawPixelDemo(maxRepeat, waitPixelDemo);

            _multiLEDBackpackManager.SetRotation(3);
            DrawPixelDemo(maxRepeat, waitPixelDemo);

            SetDefaultOrientations();
            BrightnessDemo(maxRepeat);
            SetBrightnesses();

            DrawCircleDemo(wait);
            DrawRectDemo(maxRepeat, wait);
        }

        private static void DrawRectDemo(int MAX_REPEAT, int wait)
        {
            ConsoleEx.Bar(0, 5, "DrawRect Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            _multiLEDBackpackManager.Clear();

            for (byte rpt = 0; rpt <= MAX_REPEAT; rpt += 3)
            {
                _multiLEDBackpackManager.Clear();
                var y = 0;
                while (y <= 4)
                {
                    _multiLEDBackpackManager.DrawRect(y, y, 8 - (y*2), 8 - (y*2), true);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait);
                    y += 1;
                }
                TimePeriod.Sleep(wait);
                y = 4;
                while (y >= 1)
                {
                    _multiLEDBackpackManager.DrawRect(y, y, 8 - (y*2), 8 - (y*2), false);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait);
                    y -= 1;
                }
            }
            _multiLEDBackpackManager.Clear(true);
        }

        private class Coordinate
        {
            public Int16 X, Y;
        }

        private static void DrawCircleDemo(int wait)
        {
            ConsoleEx.Bar(0, 5, "DrawCircle Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            _multiLEDBackpackManager.Clear();

            var circleLocations = new List<Coordinate>()
            {
                new Coordinate { X = 4, Y = 4},
                new Coordinate { X = 3, Y = 3},
                new Coordinate { X = 5, Y = 5},
                new Coordinate { X = 2, Y = 2},
            };

            foreach (var circleLocation in circleLocations)
            {
                for (byte ray = 0; ray <= 4; ray++)
                {
                    _multiLEDBackpackManager.Clear();
                    _multiLEDBackpackManager.DrawCircle(circleLocation.X, circleLocation.Y, ray, 1);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait*2);
                }
            }
        }

        private static void BrightnessDemo(int maxRepeat)
        {
            ConsoleEx.Bar(0, 5, "Brightness Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            _multiLEDBackpackManager.AnimateSetBrightness(maxRepeat-2);
            _multiLEDBackpackManager.Clear(true);
        }

        private static void DrawPixelDemo(int maxRepeat, int wait = 13)
        {
            maxRepeat = 4;
            ConsoleEx.Bar(0, 5, "DrawPixel Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            for (byte rpt = 0; rpt < maxRepeat; rpt += 2)
            {
                _multiLEDBackpackManager.Clear();
                TimePeriod.Sleep(wait*20);
                for (var r = 0; r < _ledMatrix00.Height; r++)
                {
                    for (var c = 0; c < _ledMatrix00.Width; c++)
                    {
                        _multiLEDBackpackManager.DrawPixel(r, c, true);
                        if ((true)||(c%2 != 0)) // Reduce the number of refresh and improve speed
                        {
                            _multiLEDBackpackManager.WriteDisplay();
                            TimePeriod.Sleep(wait);
                        }
                    }
                }
            }
        }

        private static void DrawRoundRectDemo(int wait, int maxRepeat)
        {
            ConsoleEx.Bar(0, 5, "DrawRoundRect Demo", ConsoleColor.Yellow, ConsoleColor.Red);

            for (byte rpt = 0; rpt <= maxRepeat; rpt += 2)
            {
                _multiLEDBackpackManager.Clear(true);
                var yy = 0;
                while (yy <= 3)
                {
                    _multiLEDBackpackManager.DrawRoundRect(yy, yy, 8 - (yy*2), 8 - (yy*2), 2, 1);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait);
                    yy += 1;
                }
                TimePeriod.Sleep(wait);
                yy = 2;
                while (yy >= 0)
                {
                    _multiLEDBackpackManager.DrawRoundRect(yy, yy, 8 - (yy*2), 8 - (yy*2), 2, 0);
                    _multiLEDBackpackManager.WriteDisplay();
                    TimePeriod.Sleep(wait);
                    yy -= 1;
                }
                _multiLEDBackpackManager.Clear(true);
                TimePeriod.Sleep(wait);
            }
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.TitleBar(4, "A)nimate I)mage R)eset  Q)uit", ConsoleColor.White, ConsoleColor.DarkBlue);

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static bool InitLEDMatrixes(Nusbio nusbio)
        {
            // Use the Nusbio's Adafruit I2C Adapter to plug up to 2 Adafruit LED Matrix
            // into Nusbio. You can also use a bread board.

            var clockPin           = NusbioGpio.Gpio0; // White
            var dataOutPin         = NusbioGpio.Gpio1; // Green

            // The first Led matrix must have the default I2C id which is 0x70
            // The second one if plugged must have the is 0x71 (A0 Shorted)
            // Or change the I2C Id

            // BackPack address A0, A1, A2, (Carefull the label are inversed)
            // None soldered    = 0x70
            // A0 Shorted       = 0x70 + 1 = 0x71
            // A1 Shorted       = 0x70 + 2 = 0x72
            // A2 Shorted       = 0x70 + 4 = 0x74
            // A0+A1 Shorted    = 0x70 + 2 + 1 = 0x73
            // A0+A2 Shorted    = 0x70 + 4 + 1 = 0x75

            _multiLEDBackpackManager = new MultiLEDBackpackManager();
            _multiLEDBackpackManager.Clear();

            const byte LED_MATRIX_00_I2C_ADDR = 0x70;
            _ledMatrix00 = ConsoleEx.WaitOnComponentToBePlugged<LEDBackpack>("LED Matrix", () => {
                    return _multiLEDBackpackManager.Add(8, 8, nusbio, dataOutPin, clockPin, LED_MATRIX_00_I2C_ADDR);
            });
            if(_ledMatrix00 == null)
                return false;

            const byte LED_MATRIX_01_I2C_ADDR = 0x71;
            _ledMatrix01 = _multiLEDBackpackManager.Add(8, 8, nusbio, dataOutPin, clockPin, LED_MATRIX_01_I2C_ADDR);

            const byte LED_MATRIX_02_I2C_ADDR = 0x72;
            _ledMatrix02 = _multiLEDBackpackManager.Add(8, 8, nusbio, dataOutPin, clockPin, LED_MATRIX_02_I2C_ADDR);

            const byte LED_MATRIX_03_I2C_ADDR = 0x73;
            _ledMatrix03 = _multiLEDBackpackManager.Add(8, 8, nusbio, dataOutPin, clockPin, LED_MATRIX_03_I2C_ADDR);
            
            SetDefaultOrientations();
            SetBrightnesses();

            return true;
        }

        private static void SetBrightnesses()
        {
            _ledMatrix00.SetBrightness(4);
            if(_ledMatrix01 != null)
                _ledMatrix01.SetBrightness(4);
            if(_ledMatrix02 != null)
                _ledMatrix02.SetBrightness(1);
            if(_ledMatrix03 != null)
                _ledMatrix03.SetBrightness(10);

            // Adafruit -- show and tell demo
            _ledMatrix00.SetBrightness(1);
            if(_ledMatrix01 != null)
                _ledMatrix01.SetBrightness(1);
        }

        private static void SetDefaultOrientations()
        {
            // You may need to change the rotaton depending on how the Adafruit I2C devices are plugged
            // into Nusbio, your board or your breadboard.
            _ledMatrix00.SetRotation(0);
            if(_ledMatrix01 != null)
                _ledMatrix01.SetRotation(2);
            if(_ledMatrix02 != null)
                _ledMatrix02.SetRotation(0);
            if(_ledMatrix03 != null)
                _ledMatrix03.SetRotation(0);

            

            // Adafruit -- show and tell demo
            _ledMatrix00.SetRotation(2);
            if(_ledMatrix01 != null)
                _ledMatrix01.SetRotation(0);
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

            //Nusbio.BaudRate = 9600/8;

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                if(!InitLEDMatrixes(nusbio)) return;

                Cls(nusbio);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.L)
                        {
                            for(var l=0; l<1000; l++)
                                Animate();
                        }
                        if (k == ConsoleKey.A)
                        {
                            Animate();
                        }
                        if (k == ConsoleKey.I)
                        {
                            //_ledMatrix00.SetRotation(2);
                            //_ledMatrix01.SetRotation(0);
                            DisplayImage();
                        }
                        if (k == ConsoleKey.F)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.R)
                        {
                            InitLEDMatrixes(nusbio);
                        }
                        if (k == ConsoleKey.Q) break;
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                            _ledMatrix00.Clear(true);
                        }
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}


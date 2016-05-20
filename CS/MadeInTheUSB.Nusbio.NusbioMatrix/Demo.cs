// Un comment this symbol if you have 4 8x8 LED matrix chained together
#define DEMO_WITH_4_8x8_LED_MATRIX_CHAINED

// Comment this symbol if you want to plug directly the 8x8 LED matrix into Nusbio -- Not recommended
// We recommend to use a bread board or our Nusbio 8x8 LED matrix adapter that is free
#define NUSBIO_8x8_LED_MATRIX_ADAPTER

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
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.Component;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;

namespace NusbioMatrixNS
{
    class Demo
    {
        private const int DEFAULT_BRIGTHNESS_DEMO = MAX7219.MAX_BRITGHNESS / 2;
        private const int ConsoleUserStatusRow = 10;
        
        private class Coordinate
        {
            public Int16 X, Y;
        }

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

            ConsoleEx.WriteMenu(-1, 4, "0) Animation demo  1) Images demo");
            ConsoleEx.WriteMenu(-1, 5, "P)erformance test  L)andscape demo  A)xis demo");           
            ConsoleEx.WriteMenu(-1, 6, " T)ext demo  R)otate demo  B)rigthness demo");
            ConsoleEx.WriteMenu(-1, 7, " C)lear All  Q)uit I)nit Devices");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
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

        private static List<string> Square00Bmp = new List<string>()
        {
            "B00000000",
            "B00000000",
            "B00000000",
            "B00000000",
            "B00000000",
            "B00000000",
            "B00000000",
            "B00000000",
        };

        private static List<string> Square01Bmp = new List<string>()
        {
            "B11111111",
            "B10000001",
            "B10000001",
            "B10000001",
            "B10000001",
            "B10000001",
            "B10000001",
            "B11111111",
        };

        private static List<string> Square02Bmp = new List<string>()
        {
            "B11111111",
            "B10000001",
            "B10111101",
            "B10100101",
            "B10100101",
            "B10111101",
            "B10000001",
            "B11111111",
        };

        private static List<string> Square03Bmp = new List<string>()
        {
            "B11111111",
            "B10000001",
            "B10111101",
            "B10110101",
            "B10101101",
            "B10111101",
            "B10000001",
            "B11111111",
        };

         private static List<string> Square04Bmp = new List<string>()
        {
            "B11111111",
            "B10000001",
            "B10111101",
            "B10101101",
            "B10110101",
            "B10111101",
            "B10000001",
            "B11111111",
        };

         private static List<string> Square05Bmp = new List<string>()
        {
            "B11111111",
            "B10000001",
            "B10111101",
            "B10111101",
            "B10111101",
            "B10111101",
            "B10000001",
            "B11111111",
        };

        private static List<string> Square06Bmp = new List<string>()
        {
            "B11111111",
            "B11111111",
            "B11111111",
            "B11111111",
            "B11111111",
            "B11111111",
            "B11111111",
            "B11111111",
        };

        static void  PerformanceTest (NusbioMatrix matrix, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Performance Test");
            ConsoleEx.WriteLine(0, 2, "Draw images as fast as possible", ConsoleColor.Cyan);
            ConsoleEx.WriteMenu(0, 3, "Q)uit");

            int maxRepeat             = 16;
            matrix.CurrentDeviceIndex = deviceIndex;

            var images = new List<List<string>> {
                Square00Bmp, Square02Bmp 
            };
            
            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawBitmap Demo - Slow mode first", ConsoleColor.Yellow, ConsoleColor.Red);
            ConsoleEx.Gotoxy(0, ConsoleUserStatusRow+1);
            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                foreach (var image in images)
                {
                    matrix.Clear(deviceIndex, refresh:false);
                    matrix.DrawBitmap(0, 0, image, 8, 8, 1);
                    matrix.WriteDisplay(deviceIndex);
                    Console.Write(".");
                    Thread.Sleep(200);
                }
            }

            maxRepeat             = 96;

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawBitmap Demo - Fast mode first", ConsoleColor.Yellow, ConsoleColor.Red);
            ConsoleEx.Gotoxy(0, ConsoleUserStatusRow+1);
            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                foreach (var image in images)
                {
                    matrix.Clear(deviceIndex, refresh:false);
                    matrix.DrawBitmap(0, 0, image, 8, 8, 1);
                    matrix.WriteDisplay(deviceIndex);
                    Console.Write(".");
                }
            }
        }

        static void ScrollDemo(NusbioMatrix matrix, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Scroll Demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit");

            matrix.Clear(all:true, refresh:true);

            for (var d = 0; d < matrix.DeviceCount; d++)
            {
                for (var x = 0; x < matrix.Width; x++)
                {
                    matrix.SetLed(d, x, 0, true);
                    matrix.SetLed(d, x, 7, true);
                    matrix.SetLed(d, 0, x, true);
                }
            }
            matrix.WriteDisplay(all:true);
            Thread.Sleep(1000);

            for (var z = 0; z < 8*3; z++)
            {
                
                matrix.ScrollPixelLeftDevices(3, 0);
                matrix.WriteDisplay(all: true);
            }
        }

        static void DisplayImageSequence(NusbioMatrix matrix, string title, int deviceIndex, int maxRepeat, int wait, List<List<string>> images)
        {
            matrix.CurrentDeviceIndex = deviceIndex;
            Console.Clear();
            ConsoleEx.TitleBar(0, title);
            ConsoleEx.WriteMenu(0, 2, "Q)uit");

            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                foreach (var image in images)
                {
                    matrix.Clear(deviceIndex, refresh: false);
                    matrix.DrawBitmap(0, 0, image, 8, 8, 1);
                    matrix.CopyToAll(deviceIndex, refreshAll: true);
                    TimePeriod.Sleep(wait);

                    if (Console.KeyAvailable)
                        if (Console.ReadKey().Key == ConsoleKey.Q) return;
                }
            }
            matrix.Clear(deviceIndex, refresh: true);
        }

        static void DisplaySquareImage1(NusbioMatrix matrix, int deviceIndex)
        {
            var images = new List<List<string>>
            {
                Square00Bmp, Square01Bmp, Square02Bmp,

                Square03Bmp, Square04Bmp, Square05Bmp, Square04Bmp, Square03Bmp,
                Square04Bmp, Square05Bmp, Square04Bmp, Square03Bmp,

                Square06Bmp, 
                Square01Bmp, Square00Bmp, Square01Bmp, Square00Bmp, Square01Bmp, 
            };
            DisplayImageSequence(matrix, "Display Images Demo", deviceIndex, 2, 200, images);
        }

        static void DisplaySquareImage2(NusbioMatrix matrix, int deviceIndex)
        {
            var images = new List<List<string>>
            {
                Square03Bmp, Square04Bmp, Square05Bmp,
            };
            DisplayImageSequence(matrix, "Display Images Demo 2", deviceIndex, 8, 250, images);
        }

        static void DisplayImage(NusbioMatrix matrix)
        {
            int MAX_REPEAT = 3;
            int wait       = 400;

            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawBitmap Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            for (byte rpt = 0; rpt < MAX_REPEAT; rpt++)
            {
                var images = new List<List<string>> {neutralBmp, smileBmp, neutralBmp, frownbmp};
                foreach (var image in images)
                {
                    matrix.Clear(refresh:false);
                    matrix.DrawBitmap(0, 0, BitUtil.ParseBinary(image), 8, 8, 1);
                    matrix.WriteDisplay();
                    TimePeriod.Sleep(wait);
                }
            }
            matrix.Clear();
        }

        private static void DrawRoundRectDemo(NusbioMatrix matrix, int wait, int maxRepeat, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Draw Round Rectangle Demo");

            matrix.CurrentDeviceIndex = deviceIndex;

            for (byte rpt = 0; rpt <= maxRepeat; rpt += 2)
            {
                matrix.Clear(deviceIndex);
                var yy = 0;
                while (yy <= 3)
                {
                    matrix.DrawRoundRect(yy, yy, 8 - (yy*2), 8 - (yy*2), 2, 1);
                    matrix.CopyToAll(deviceIndex, true);
                    TimePeriod.Sleep(wait);
                    yy += 1;
                }
                TimePeriod.Sleep(wait);
                yy = 2;
                while (yy >= 0)
                {
                    matrix.DrawRoundRect(yy, yy, 8 - (yy*2), 8 - (yy*2), 2, 0);
                    matrix.CopyToAll(deviceIndex, true);
                    TimePeriod.Sleep(wait);
                    yy -= 1;
                }
                matrix.Clear(deviceIndex);
                matrix.CopyToAll(deviceIndex, true);
                TimePeriod.Sleep(wait);
            }
        }

        private static void DrawAllMatrixOnePixelAtTheTimeDemo(NusbioMatrix matrix, int deviceIndex, int waitAfterClear = 350, int maxRepeat = 4)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Draw one pixel at the time demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit");
            ConsoleEx.WriteLine(0, ConsoleUserStatusRow + 1, "".PadLeft(80), ConsoleColor.Black);
            ConsoleEx.Gotoxy(0, ConsoleUserStatusRow+1);

            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                matrix.Clear(deviceIndex, refresh: true);
                TimePeriod.Sleep(waitAfterClear);
                for (var r = 0; r < matrix.Height; r++)
                {
                    for (var c = 0; c < matrix.Width; c++)
                    {
                        matrix.CurrentDeviceIndex = deviceIndex;
                        matrix.DrawPixel(r, c, true);
                        // Only refresh the row when we light up an led
                        // This is 8 time faster than a full refresh
                        matrix.WriteRow(deviceIndex, r);
                        Console.Write('.');
                    }
                }
            }
        }

        private static void ScrollText(NusbioMatrix matrix, int deviceIndex = 0)
        {
            var quit      = false;
            var speed     = 10;
            var text      = "Hello World!   ";

            if (matrix.DeviceCount == 1 && matrix.MAX7218Wiring == NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OrigineBottomRightCorner)
                speed = speed*3;

            while (!quit)
            {
                Console.Clear();
                ConsoleEx.TitleBar(0, "Scroll Text");
                ConsoleEx.WriteMenu(0, 2, string.Format("Q)uit  F)aster  S)lower   Speed:{0:000}", speed));

                matrix.Clear(all: true);
                matrix.WriteDisplay(all: true);

                for (var ci=0; ci<text.Length; ci++)
                {
                    var c = text[ci];

                    ConsoleEx.WriteMenu(ci, 4, c.ToString());

                    matrix.WriteChar(deviceIndex, c); // See property matrix.MAX7218Wiring for more info
                    matrix.WriteDisplay(all: true);

                    if (speed > 0)
                    {
                        Thread.Sleep(speed);
                        // Provide a better animation
                        if (matrix.DeviceCount == 1 && matrix.MAX7218Wiring == NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OrigineBottomRightCorner)
                            Thread.Sleep(speed * 12);
                    }

                    for (var i = 0; i < MAX7219.MATRIX_ROW_SIZE; i++)
                    {
                        matrix.ScrollPixelLeftDevices(matrix.DeviceCount - 1, 0, 1);
                        matrix.WriteDisplay(all: true);

                        // Do not wait when we scrolled the last pixel, we will wait when we display the new character
                        if(i < MAX7219.MATRIX_ROW_SIZE-1) 
                            if(speed > 0) Thread.Sleep(speed);

                        if (Console.KeyAvailable)
                        {
                            switch (Console.ReadKey().Key)
                            {
                                case ConsoleKey.Q: quit = true; i = 100; ci = 10000; break;
                                case ConsoleKey.S: speed += 10; break;
                                case ConsoleKey.F: speed -= 10; if (speed < 0) speed = 0; break;
                            }
                            ConsoleEx.WriteMenu(0, 2, string.Format("Q)uit  F)aster  S)lower   Speed:{0:000}", speed));
                        }
                    }
                }
            }
        }

        private static void LandscapeDemo(NusbioMatrix matrix, int deviceIndex = 0)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Random Landscape Demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit  F)ull speed");
            var landscape = new NusbioLandscapeMatrix(matrix, 0);

            var speed = 250-(matrix.DeviceCount * 25); // slower speed if we have 1 device rather than 4
            if(matrix.DeviceCount == 1)
                speed = 150;

            matrix.Clear(all: true);
            var quit = false;
            var fullSpeed = false;

            while (!quit)
            {
                landscape.Redraw();

                ConsoleEx.WriteLine(0, 4, landscape.ToString(), ConsoleColor.Cyan);
                if(!fullSpeed)
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

        private static void RotateMatrix(NusbioMatrix matrix, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Rotate Demo");
            ConsoleEx.WriteMenu(0, 2, "Rotate:  L)eft  R)ight  Q)uit");

            matrix.Clear(deviceIndex);
            matrix.CurrentDeviceIndex = deviceIndex;
            matrix.DrawLine(0, 0, 0, matrix.Height, true);
            matrix.DrawLine(7, 0, 7, matrix.Height, true);
            matrix.DrawLine(0, 2, matrix.Width, 2, true);
            matrix.WriteDisplay(deviceIndex);

            while (true)
            {
                var k = Console.ReadKey(true).Key;
                switch (k)
                {
                    case ConsoleKey.Q :return; break;
                    case ConsoleKey.L : matrix.RotateLeft(deviceIndex); break;
                    case ConsoleKey.R: matrix.RotateRight(deviceIndex); break;
                }
                matrix.WriteDisplay(deviceIndex);
            }
        }

        private static void DrawAxis(NusbioMatrix matrix, int deviceIndex)
        {
            ConsoleEx.Bar(0, ConsoleUserStatusRow, "Draw Axis Demo", ConsoleColor.Yellow, ConsoleColor.Red);

            Console.Clear();
            ConsoleEx.TitleBar(0, "Draw Axis Demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit");


            matrix.Clear(deviceIndex);
            matrix.CurrentDeviceIndex = deviceIndex;

            matrix.Clear(deviceIndex);
            matrix.CurrentDeviceIndex = deviceIndex;
            matrix.DrawLine(0, 0, matrix.Width, 0, true);
            matrix.DrawLine(0, 0, 0, matrix.Height, true);
            matrix.WriteDisplay(deviceIndex);

            for (var i = 0; i < matrix.Width; i++)
            {
                matrix.SetLed(deviceIndex, i, i, true, true);
            }
            var k = Console.ReadKey();
        }

        private static void DrawOnePixelAllOverTheMatrixDemo(NusbioMatrix matrix, int deviceIndex, int waitAfterClear = 350, int maxRepeat = 4)
        {
            ConsoleEx.Bar(0, ConsoleUserStatusRow, "DrawPixel Demo", ConsoleColor.Yellow, ConsoleColor.Red);

            for (byte rpt = 0; rpt < maxRepeat; rpt++)
            {
                for (var r = 0; r < matrix.Height; r++)
                {
                    for (var c = 0; c < matrix.Width; c++)
                    {
                        matrix.Clear(deviceIndex);
                        matrix.CurrentDeviceIndex = deviceIndex;
                        matrix.DrawPixel(r, c, true);


                        // Only refresh the row when we light up an led
                        // This is 8 time faster than a full refresh
                        matrix.WriteRow(deviceIndex, r);
                        Thread.Sleep(32);
                    }
                }
            }
            matrix.Clear(deviceIndex);
        }

        private static void BrightnessDemo(NusbioMatrix matrix, int maxRepeat, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Brightness Demo");

            matrix.Clear(deviceIndex);
            matrix.CurrentDeviceIndex = deviceIndex;

            var y = 0;
            for (y=0; y < matrix.Height; y++)
            {
                matrix.DrawLine(0, y, matrix.Width, y, true);
                matrix.WriteDisplay(deviceIndex);
            }
            matrix.AnimateSetBrightness(maxRepeat-2, deviceIndex: deviceIndex);
            matrix.Clear(deviceIndex);
        }
        
        private static void DrawRectDemo(NusbioMatrix matrix, int MAX_REPEAT, int wait, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Draw Rectangle Demo");
            ConsoleEx.WriteMenu(0, 2, "Q)uit");

            matrix.Clear(deviceIndex);
            matrix.CopyToAll(deviceIndex, refreshAll: true);
            matrix.CurrentDeviceIndex = deviceIndex;

            for (byte rpt = 0; rpt < MAX_REPEAT; rpt += 3)
            {
                matrix.Clear();
                var y = 0;
                while (y <= 4)
                {
                    matrix.DrawRect(y, y, 8 - (y*2), 8 - (y*2), true);
                    matrix.CopyToAll(deviceIndex, refreshAll: true);
                    TimePeriod.Sleep(wait);
                    y += 1;
                }
                TimePeriod.Sleep(wait);
                y = 4;
                while (y >= 1)
                {
                    matrix.DrawRect(y, y, 8 - (y*2), 8 - (y*2), false);
                    matrix.CopyToAll(deviceIndex, refreshAll: true);
                    TimePeriod.Sleep(wait);
                    y -= 1;
                }
            }
            matrix.Clear(deviceIndex);
        }

        private static void DrawCircleDemo(NusbioMatrix matrix, int wait, int deviceIndex)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "DrawCircle Demo");

            matrix.CurrentDeviceIndex = deviceIndex;
            matrix.Clear(deviceIndex);
            matrix.CopyToAll(deviceIndex, refreshAll: true);

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
                    matrix.Clear(deviceIndex);
                    matrix.DrawCircle(circleLocation.X, circleLocation.Y, ray, 1);
                    matrix.CopyToAll(deviceIndex, refreshAll: true);
                    TimePeriod.Sleep(wait);
                }
            }
        }

        /*
        static void MultiMatrixDemo(NusbioMatrix matrix)
        {
            ConsoleEx.Bar(0, ConsoleUserStatusRow, "Multi Matrix Demo", ConsoleColor.Yellow, ConsoleColor.Red);
            matrix.Clear(0);
            matrix.Clear(1);

            var pw = new List<byte>() {1, 2, 4, 8, 16, 32, 64, 128};

            for (var r = 0; r < 8; r++) { 
                for (var c = 0; c < 8; c++)
                {
                    matrix.SpiTransferBuffer(new List<byte>(){
                        (byte)(r+1),
                        pw[c],
                        (byte)(r+1),
                        pw[8-c-1],
                    }, software: true);
                    Thread.Sleep(50);
                }
            }
        }*/

        static void Animate(NusbioMatrix matrix, int deviceIndex)
        {
            int wait       = 100;
            int maxRepeat = 5;

            matrix.CurrentDeviceIndex = deviceIndex;

            DrawRoundRectDemo(matrix, wait, maxRepeat, deviceIndex);

            //matrix.SetRotation(0);
            DrawAllMatrixOnePixelAtTheTimeDemo(matrix, deviceIndex);

            //matrix.SetRotation(1);
            //DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);

            //matrix.SetRotation(2);
            //DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);

            //matrix.SetRotation(3);
            //DrawAllMatrixOnePixelAtTheTimeDemo(matrix, maxRepeat);
            
            SetDefaultOrientations(matrix);
            BrightnessDemo(matrix, maxRepeat, deviceIndex);
            SetBrightnesses(matrix);

            DrawCircleDemo(matrix, wait, deviceIndex);
            DrawRectDemo(matrix, maxRepeat, wait, deviceIndex);

            matrix.CurrentDeviceIndex = 0;
        }

        private static void SetDefaultOrientations(NusbioMatrix matrix)
        {
            matrix.SetRotation(0);
        }

        static void test1(NusbioMatrix matrix)
        {
            matrix.Clear(0);
            Thread.Sleep(500);
            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 8; c++)
                    matrix.SetLed(0, r , c, true, true); // WriteDisplay for every pixel
            Thread.Sleep(500);

            matrix.Clear(0);
            Thread.Sleep(500);
            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 8; c++)
                    matrix.SetLed(0, r , c, true, c==7); // WriteDisplay for every row
            Thread.Sleep(500);

            matrix.Clear(0);
            Thread.Sleep(500);
            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 8; c++)
                    matrix.SetLed(0, r , c, true, false);
            matrix.WriteDisplay(); // WriteDisplay only once
            Thread.Sleep(500);
        }

        private static void SetBrightnesses(NusbioMatrix matrix)
        {
            var brightness = DEFAULT_BRIGTHNESS_DEMO;
            if (matrix.DeviceCount > 1)
                brightness /= 2;
            
            for(var deviceIndex = 0; deviceIndex < matrix.DeviceCount; deviceIndex++)
                matrix.SetBrightness(brightness, deviceIndex);
        }

        private static NusbioMatrix InitializeMatrix(
            Nusbio nusbio, 
            NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX origin, int matrixChainedCount)
        {

            #if NUSBIO_8x8_LED_MATRIX_ADAPTER
                var matrix = NusbioMatrix.Initialize(nusbio,
                    selectGpio   : NusbioGpio.Gpio3,
                    mosiGpio     : NusbioGpio.Gpio1,
                    clockGpio    : NusbioGpio.Gpio0,
                    gndGpio      : NusbioGpio.None,
                    MAX7218Wiring: origin,
                    deviceCount  : matrixChainedCount); // If you have MAX7219 LED Matrix chained together increase the number
            #else
            // How to plug the 8x8 LED Matrix directly into Nusbio
            // -----------------------------------------------------------------------
            // NUSBIO                          : GND VCC  7   6  5   4  3  2  1  0
            // 8x8 LED Matrix MAX7219 base     :     VCC GND DIN CS CLK
            // Gpio 7 act as ground so we can plug directly the 8x8 led matrix
            var matrix = NusbioMatrix.Initialize(nusbio,
                selectGpio   : NusbioGpio.Gpio5,
                mosiGpio     : NusbioGpio.Gpio6,
                clockGpio    : NusbioGpio.Gpio4,
                gndGpio      : NusbioGpio.Gpio7,
                MAX7218Wiring: origin,
                deviceCount  : matrixChainedCount); // If you have MAX7219 LED Matrix chained together increase the number
            #endif

            SetBrightnesses(matrix);

            return matrix;
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

            #if DEMO_WITH_4_8x8_LED_MATRIX_CHAINED
                var matrixChainedCount = 4;
                var origin = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OrigineUpperLeftCorner; // Different Wiring for 4 8x8 LED Matrix sold by MadeInTheUSB
            #else
                var matrixChainedCount = 1;
                var origin = NusbioMatrix.MAX7219_WIRING_TO_8x8_LED_MATRIX.OrigineBottomRightCorner;
            #endif


            using (var nusbio = new Nusbio(serialNumber))
            {
                var matrix = InitializeMatrix(nusbio, origin, matrixChainedCount);

                Cls(nusbio);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        
                        if (k == ConsoleKey.C)
                            matrix.Clear(all:true, refresh:true);

                        if (k == ConsoleKey.L)
                            LandscapeDemo(matrix);

                        if (k == ConsoleKey.D0) 
                            Animate(matrix, 0);

                        if (k == ConsoleKey.D1) 
                            DisplaySquareImage1(matrix, 0);

                        if (k == ConsoleKey.D2)
                            DisplaySquareImage2(matrix, 0);

                        if (k == ConsoleKey.A)
                            DrawAxis(matrix, 0);

                        if (k == ConsoleKey.R)
                            RotateMatrix(matrix, 0);

                        if (k == ConsoleKey.S)
                            ScrollDemo(matrix, 0);

                        if (k == ConsoleKey.B)
                        {
                            BrightnessDemo(matrix, 5, 0);
                            SetBrightnesses(matrix);
                        }

                        if (k == ConsoleKey.P)
                            PerformanceTest(matrix, 0); // Speed test

                        if (k == ConsoleKey.T)
                            ScrollText(matrix);

                        if (k == ConsoleKey.I)
                            matrix = InitializeMatrix(nusbio, origin, matrixChainedCount);

                        if (k == ConsoleKey.Q) 
                            break;

                        Cls(nusbio);
                        matrix.Clear(all: true, refresh: true);
                    }
                }
            }
            Console.Clear();
        }
    }
}


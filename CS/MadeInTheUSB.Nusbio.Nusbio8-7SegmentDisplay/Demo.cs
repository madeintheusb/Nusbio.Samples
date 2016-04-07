#define PLUGGED_INTO_NUSBIO
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
        private const int DEFAULT_BRIGTHNESS_DEMO = 5;
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

            ConsoleEx.WriteMenu(-1, 4, "T)est 7 segment  C)ounters demo  L)etter demo");
            ConsoleEx.WriteMenu(-1, 5, "1) Play with segments 1  2) Play with segments 2");
            ConsoleEx.WriteMenu(-1, 6, " C)lear All  Q)uit I)nit Devices");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }
        
        public static void SetAllSegmentWithValues(NusbioSevenSegmentDisplay sevenSegmentDisplay, List<int> bitValues, int wait)
        {
            foreach (var bitValue in bitValues)
                SetAllSegmentWithValue(sevenSegmentDisplay, bitValue, wait);
        }

        public static void SetAllSegmentWithValue(NusbioSevenSegmentDisplay sevenSegmentDisplay, int bitValue, int wait)
        {
            for (var i = 0; i < sevenSegmentDisplay.SevenSegmentCount; i++)
            {
                sevenSegmentDisplay.SetDigitDataByte(0, i, bitValue, false);
            }
            //sevenSegmentDisplay.Clear(refresh:true);
            //var buffer = new List<byte>();
            //for (var i = 0; i < sevenSegmentDisplay.SevenSegmentCount; i++)
            //    buffer.Add((byte)bitValue);
            //sevenSegmentDisplay.SetDigitDataByte(0, buffer, false); 
            if (wait > 0)
                TimePeriod.Sleep(wait);
        }


        public static void LetterDemo(NusbioSevenSegmentDisplay sevenSegmentDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Letters Demo");
            ConsoleEx.WriteMenu(0, 1, " Q)uit");

            int wait = 300;
            bool go = true;

            sevenSegmentDisplay.Clear(0, refresh: true);
            sevenSegmentDisplay.WriteLetter(0, 7, "HELLO");
            TimePeriod.Sleep(1000);

            while (go)
            {
                sevenSegmentDisplay.Clear(0, refresh: true);
                foreach (var d in NusbioSevenSegmentDisplay.Letters)
                {
                    if (d.Value > 0)
                    {
                        ConsoleEx.WriteLine(0, 3, string.Format("Letter:{0}", d.Key), ConsoleColor.Cyan);
                        SetAllSegmentWithValue(sevenSegmentDisplay, d.Value, wait);
                        TimePeriod.Sleep(600);
                    }
                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            go = false;
                            break;
                        }
                    }
                }
            }
        }

        public static void PlayWithSegment1(NusbioSevenSegmentDisplay sevenSegmentDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Play With Segments 1");
            ConsoleEx.WriteMenu(0, 1, " Q)uit");

            int wait = 300;

            while (true)
            {
                sevenSegmentDisplay.Clear(0, refresh: true);
                // Define sequence of bit or segment in the seven segment to light up
                // Bit Definition
                //   64
                //   _
                // 2| |32
                // 1 -
                // 4| |16
                //   -
                //   8 
                var values = new List<int>();
                values.Add(1 + 2 + 4 + 8 + 16 + 32 + 64);
                values.Add(values[values.Count-1] & ~1);

                SetAllSegmentWithValues(sevenSegmentDisplay, values, wait);

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                        break;
                }
            }
        }

        public static void PlayWithSegment2(NusbioSevenSegmentDisplay sevenSegmentDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Play With Segments 2");
            ConsoleEx.WriteMenu(0, 1, " Q)uit");
            int wait = 300;

            // Define sequence of bit or segment in the seven segment to light up
            // Bit Definition
            //   64
            //   _
            // 2| |32
            // 1 -
            // 4| |16
            //   -
            //   8 
            var values = new List<int>();
            values.Add(8);
            values.Add(values[values.Count-1] | 1);
            values.Add(values[values.Count-1] | 64);
            values.Add(values[values.Count-1] & ~64);
            values.Add(values[values.Count-1] & ~1);
            values.Add(values[values.Count-1] & ~8);

            while (true)
            {
                sevenSegmentDisplay.Clear(0, refresh: true);
                SetAllSegmentWithValues(sevenSegmentDisplay, values, wait);
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                        break;
                }
            }
        }

        public static void CounterDemo(NusbioSevenSegmentDisplay sevenSegmentDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Counter Demo");
            ConsoleEx.WriteMenu(0, 1, " Q)uit");

            sevenSegmentDisplay.Clear(0, refresh:true);

            var counter1 = 000.0;
            var counter2 = 1000.0;
            var format = "0.0";
            while (counter1 < 200.0)
            {
                ConsoleEx.WriteLine(0, 3, string.Format("{0} {1}", counter2.ToString(format), counter1.ToString(format)).PadRight(16), ConsoleColor.White);
                sevenSegmentDisplay.DisplayNumber(0, counter1, format, 0);
                sevenSegmentDisplay.DisplayNumber(0, counter2, format, 4);
                TimePeriod.Sleep(150);
                counter1 = counter1 + 0.1;
                counter2 = counter2 - 0.1;
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                        break;
                }
            }
        }

        public static void TestSevenSegmentDisplay(NusbioSevenSegmentDisplay sevenSegmentDisplay)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Test Seven Segments Display");
            ConsoleEx.WriteMenu(0, 1, " Q)uit");

            sevenSegmentDisplay.Clear(0, refresh:true);
            for (var i = 0; i < sevenSegmentDisplay.SevenSegmentCount; i++)
            {
                Console.WriteLine("7-Segment:{0}, Digit:{1}", i, i);
                sevenSegmentDisplay.SetDigit(0, i, i, false);
                TimePeriod.Sleep(250);
            }

            var pwrs = new List<int>() { 1, 2, 4, 8, 16, 32, 64, 128 };
            foreach(var pwr in pwrs)
            {
                sevenSegmentDisplay.Clear(0, refresh: true);
                Console.WriteLine("Bit set:{0}", pwr);
                for (var i = 0; i < sevenSegmentDisplay.SevenSegmentCount; i++)
                {
                    sevenSegmentDisplay.SetDigitDataByte(0, i, pwr, !true);
                    TimePeriod.Sleep(100);
                }
            }
        }

        public static NusbioSevenSegmentDisplay Initialize8SevenSegmentDisplays(Nusbio nusbio)
        {
            // Using a breadboard and some wires
            var selectGpio = NusbioGpio.Gpio2;
            var mosiGpio   = NusbioGpio.Gpio1;
            var clockGpio  = NusbioGpio.Gpio0;

            // How to plug the 8 7Segment display directly into Nusbio
            // -----------------------------------------------------------------------
            // NUSBIO                          : GND VCC  7   6  5   4  3  2  1  0
            // 8x8 LED Matrix MAX7219 base     :     VCC GND DIN CS CLK
            // Gpio 7 act as ground 
            #if PLUGGED_INTO_NUSBIO
                nusbio[NusbioGpio.Gpio7].Low(); // <- GROUND
                mosiGpio   = NusbioGpio.Gpio6;
                selectGpio = NusbioGpio.Gpio5;
                clockGpio  = NusbioGpio.Gpio4;
            #endif

            var sevenSegmentDisplay = NusbioSevenSegmentDisplay.Initialize(
                nusbio, 
                8, // 8 digits/7Segments on the device
                selectGpio,
                mosiGpio,
                clockGpio,
                deviceCount : 1 // 1 device with 8 7Segmens == 1 MAX7219
            );

            return sevenSegmentDisplay;
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

            var matrixChainedCount = 1;

            using (var nusbio = new Nusbio(serialNumber))
            {
                var sevenSegmentDisplay = Initialize8SevenSegmentDisplays(nusbio);

                Cls(nusbio);

                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.L)
                            LetterDemo(sevenSegmentDisplay);

                        if (k == ConsoleKey.T)
                            TestSevenSegmentDisplay(sevenSegmentDisplay);

                        if (k == ConsoleKey.C)
                            CounterDemo(sevenSegmentDisplay);

                        if (k == ConsoleKey.D1)
                            PlayWithSegment1(sevenSegmentDisplay);

                        if (k == ConsoleKey.D2)
                            PlayWithSegment2(sevenSegmentDisplay);

                        if (k == ConsoleKey.C)
                            sevenSegmentDisplay.Clear(all:true, refresh:true);

                        if (k == ConsoleKey.I)
                            sevenSegmentDisplay = Initialize8SevenSegmentDisplays(nusbio);

                        if (k == ConsoleKey.Q) 
                            break;

                        Cls(nusbio);
                        sevenSegmentDisplay.Clear(0, true);
                    }
                }
            }
            Console.Clear();
        }
    }
}


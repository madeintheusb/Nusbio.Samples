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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.Buttons;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.Components;

namespace ButtonConsole
{
    class Demo
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteMenu(0, 3, "F1) Test Segment  F2) Test Digit");
            
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }


        private static void SevenSegmentDisplaySequence(Nusbio nusbio, List<TwoNusbioGpio> seq, int repeat)
        {
            for (var r = 0; r < repeat; r++)
            {
                var wait = 180;
                var state = true;
                for (var i = 0; i < seq.Count; i++)
                {
                    var g = seq[i];
                    nusbio[g.g1].DigitalWrite(state);
                    nusbio[g.g2].DigitalWrite(state);
                    if (Console.KeyAvailable) break;
                    Thread.Sleep(wait);

                    if (i + 1 < seq.Count)
                    {
                        var g2 = seq[i + 1];
                        nusbio[g2.g1].DigitalWrite(state);
                        nusbio[g2.g2].DigitalWrite(state);
                        Thread.Sleep(wait);
                    }

                    nusbio[g.g1].DigitalWrite(!state);
                    nusbio[g.g2].DigitalWrite(!state);
                    Thread.Sleep(wait);
                    foreach (var gg in seq)
                    {
                        nusbio[gg.g1].DigitalWrite(PinState.Low);
                        nusbio[gg.g2].DigitalWrite(PinState.Low);
                    }
                }
                Thread.Sleep(wait);
            }
        }

        private static void SevenSegmentDisplaySequenceAll(Nusbio nusbio, List<NusbioGpio> seq)
        {
            var wait = 120;
            var seqReversed = new List<NusbioGpio>();
            seqReversed.AddRange(seq);
            seqReversed.Reverse();

            foreach (var g in seq)
            {
                nusbio[g].DigitalWrite(PinState.High);
                if (Console.KeyAvailable) break;
                Thread.Sleep(wait);
            }
            Thread.Sleep(wait * 2);
            foreach (var g in seqReversed)
            {
                nusbio[g].DigitalWrite(PinState.Low);
                if (Console.KeyAvailable) break;
                Thread.Sleep(wait);
            }
            Thread.Sleep(wait * 2);
        }

        private static void SevenSegmentDisplaySequenceOne(Nusbio nusbio, List<NusbioGpio> seq)
        {
            var wait = 170;
            var seqReversed = new List<NusbioGpio>();
            seqReversed.AddRange(seq);
            seqReversed.Reverse();
            seqReversed.RemoveAt(0);
            seq.AddRange(seqReversed);

            foreach (var g in seq)
            {
                foreach (var gg in seq) nusbio[gg].DigitalWrite(PinState.Low);
                nusbio[g].DigitalWrite(PinState.High);
                if (Console.KeyAvailable) break;
                Thread.Sleep(wait);
            }
            //Thread.Sleep(wait);
        }

        private class TwoNusbioGpio
        {
            public NusbioGpio g1, g2;
        }

        private static void SevenSegmentDisplayDemo(Nusbio nusbio)
        {
            var title = "7 Segment Display Demo";
            Console.Clear();
            ConsoleEx.TitleBar(0, title, ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");
            ConsoleEx.Gotoxy(0, 4);

            var anim1 = new List<NusbioGpio>() {
                NusbioGpio.Gpio1,NusbioGpio.Gpio2,NusbioGpio.Gpio3,NusbioGpio.Gpio4, NusbioGpio.Gpio5,NusbioGpio.Gpio6,NusbioGpio.Gpio7,
            };
            var anim2 = new List<NusbioGpio>() {
                NusbioGpio.Gpio3,NusbioGpio.Gpio2, NusbioGpio.Gpio1,NusbioGpio.Gpio6,NusbioGpio.Gpio5,NusbioGpio.Gpio4, NusbioGpio.Gpio7
            };
            var anim3 = new List<TwoNusbioGpio>() {
                new TwoNusbioGpio { g1 = NusbioGpio.Gpio2, g2 = NusbioGpio.Gpio5 },
                new TwoNusbioGpio { g1 = NusbioGpio.Gpio7, g2 = NusbioGpio.Gpio7 },
            };
            while (true)
            {
                SevenSegmentDisplaySequenceAll(nusbio, anim1);
                SevenSegmentDisplaySequenceAll(nusbio, anim2);
                SevenSegmentDisplaySequence(nusbio, anim3, 4);
                SevenSegmentDisplaySequenceOne(nusbio, anim1);
                if (Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.Attention)
                    break;
            }
        }


        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio Initializing");
            var serialNumber = Nusbio.Detect();
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("Nusbio not detected");
                return;
            }

            var halfSecondTimeOut = new TimeOut(500);

            using (var nusbio = new Nusbio(serialNumber))
            {
                var _7Seg = new SevenSegmentDirect(nusbio);
                Cls(nusbio);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.F1)
                            _7Seg.Test0();
                        if (k == ConsoleKey.F2)
                            _7Seg.TestDigit();

                        if (k == ConsoleKey.F5)
                            SevenSegmentDisplayDemo(nusbio);
                        if (k == ConsoleKey.Q) break;
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}




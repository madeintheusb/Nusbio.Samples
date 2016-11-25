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
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Components;

namespace LedConsole
{
    class Demo
    {
        
        private static void ReverseLed(Led led)
        {
            led.ReverseSet();
        }

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
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            
            ConsoleEx.WriteMenu(-1, 2, "0) 1) 2) 3) Animations   S)croll Animation");
            ConsoleEx.WriteMenu(-1, 3, "Q)uit");
        }

        public static void SimpleAnimation(BiColorLedStrip biColorLeds, List<int> initialStateIndex)
        {
            Console.Clear();
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");

            biColorLeds.AllOff();
            var biColorLedStateIndex = new List<int>() { 0, 0, 0, 0 };

            biColorLeds[0].StateIndex = initialStateIndex[0];
            biColorLeds[1].StateIndex = initialStateIndex[1];
            biColorLeds[2].StateIndex = initialStateIndex[2];
            biColorLeds[3].StateIndex = initialStateIndex[3];

            while (true) { 

                biColorLeds.Set(incState: true, firstStateIndex: 1);
                MadeInTheUSB.WinUtil.TimePeriod.Sleep(350*2);

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q) break;
                }
            }
            biColorLeds.AllOff();
        }

        private class Pattern { 
            public BiColorLed.BiColorLedState BackgroundColor;
            public BiColorLed.BiColorLedState ForegroundColor;
        }

        public static void ScrollAnimation(BiColorLedStrip biColorLeds)
        {
            Console.Clear();
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");
            int waitTime = 240;
            biColorLeds.AllOff();
            var currentLed   = 0;
            var patternIndex = 0;

            var patterns = new Dictionary<int, Pattern>() {

                { 0, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Green,  ForegroundColor = BiColorLed.BiColorLedState.Red    } },
                { 1, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Green,  ForegroundColor = BiColorLed.BiColorLedState.Yellow } },
                { 2, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Red,    ForegroundColor = BiColorLed.BiColorLedState.Green  } },
                { 3, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Red,    ForegroundColor = BiColorLed.BiColorLedState.Yellow } },
                { 4, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Yellow, ForegroundColor = BiColorLed.BiColorLedState.Red    } },
                { 5, new Pattern() { BackgroundColor = BiColorLed.BiColorLedState.Yellow, ForegroundColor = BiColorLed.BiColorLedState.Green  } },
            };

            while (true) {
                
                var p = patterns[patternIndex];

                if (currentLed == 0) // When we start a scroll line sequence first let set all the 4 leds with the background color
                {
                    for (var i = 0; i < biColorLeds.Count; i++)
                    {

                        biColorLeds[i].Set(p.BackgroundColor);
                    }
                    MadeInTheUSB.WinUtil.TimePeriod.Sleep(waitTime);
                }

                for (var i = 0; i < biColorLeds.Count; i++) {

                    if (i == currentLed)
                    {
                        biColorLeds[currentLed].Set(p.ForegroundColor);
                    }
                    else
                    {
                        biColorLeds[i].Set(p.BackgroundColor);
                    }
                }

                currentLed += 1;
                if (currentLed >= biColorLeds.Count)
                {
                    currentLed = 0;
                    patternIndex += 1;
                    if(patternIndex >= patterns.Count)
                        patternIndex = 0;
                }

                MadeInTheUSB.WinUtil.TimePeriod.Sleep(waitTime);
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q) break;
                }
            }
            biColorLeds.AllOff();
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

            using (var nusbio = new Nusbio(serialNumber))
            {
                var biColorLedStrip = new BiColorLedStrip(
                    new List<BiColorLed>() {
                        new BiColorLed(nusbio, NusbioGpio.Gpio0, NusbioGpio.Gpio1),
                        new BiColorLed(nusbio, NusbioGpio.Gpio2, NusbioGpio.Gpio3),
                        new BiColorLed(nusbio, NusbioGpio.Gpio4, NusbioGpio.Gpio5),
                        new BiColorLed(nusbio, NusbioGpio.Gpio6, NusbioGpio.Gpio7)
                    }
                );

                Cls(nusbio);

                var stateIndex0 = 0;

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) 
                            break;
                        if (k == ConsoleKey.D0)  
                            SimpleAnimation(biColorLedStrip, new List<int>() { 0, 0, 0, 0 });
                        if (k == ConsoleKey.D1)  
                            SimpleAnimation(biColorLedStrip, new List<int>() { 0, 1, 2, 3 });
                        if (k == ConsoleKey.D2)  
                            SimpleAnimation(biColorLedStrip, new List<int>() { 0, 1, 0, 1 });
                        if (k == ConsoleKey.D3)  
                            SimpleAnimation(biColorLedStrip, new List<int>() { 0, 3, 0, 3 });

                        if (k == ConsoleKey.S)  
                            ScrollAnimation(biColorLedStrip);

                        if (k == ConsoleKey.O) 
                            nusbio.SetAllGpioOutputState(PinState.Low);
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }

    }
}




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

namespace LCDConsole
{
    class Demo
    {
        static MCP4131 _mcp4131_10k;
        static MCP4131 _mcp4132_100k;
        static int waitTime = 100; // 20
        static int demoStep = 2;
        
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

            ConsoleEx.TitleBar(4, "0) 10k Pot,  1) 100k Pot,  O)ther Api test ", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(5, "O)ther Api test L)ED test on 10k Pot", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(6, "Q)uit", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        private static bool DisplayPotentiometerInfoAndCheckForCancel(MCP41X1_Base ldp, bool tryReadValue = false, int exWaitTime = -1)
        {
            Console.WriteLine(ldp.ToString());
            if(exWaitTime == -1)
                TimePeriod.Sleep(waitTime * 10);
            else 
                if (exWaitTime >0) TimePeriod.Sleep(exWaitTime);

            if (tryReadValue)
            {
                int val = ldp.Get();
            }
            if (Console.KeyAvailable)  { 
                var k = Console.ReadKey();
                return true;
            }
            else return false;
        }

        static void OtherApiTests(MCP41X1_Base dp)
        {
            var quit = false;
            Console.Clear();
            try { 
                dp.Set(0);
                for (var i = 0; i <= dp.MaxDigitalValue; i += demoStep)
                {
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp)) { quit = true; break;}
                    dp.Increment(demoStep);
                }

                if(quit)
                    return;

                dp.Set(dp.MaxDigitalValue);
                DisplayPotentiometerInfoAndCheckForCancel(dp);

                for (var i = dp.MaxDigitalValue; i > 0; i -= demoStep)
                {
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp)) { quit = true; break;}
                    dp.Decrement(demoStep);
                }
            }
            finally
            {
                dp.Set(0);
            }
        }

        static void DigitalPotRangeForLed(MCP41X1_Base dp)
        {
            Console.Clear();
            var quit = false;

            var ledThreadHold17Volt = 0;

            dp.Set(0);

            int wait = 16;
            
            while (!quit) 
            { 
                for (var i = ledThreadHold17Volt; i <= dp.MaxDigitalValue; i += 1)
                {
                    if (!dp.Set(i).Succeeded)
                        Console.WriteLine("Communication error");
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) { quit = true; break; } 
                }
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) { quit = true; break; } 

                //_mcp4131_10k.Set(dp.MaxDigitalValue);
                //TimePeriod.Sleep(waitTime * 10);

                //for (var i = 0; i < 5; i++) { 
                //    dp.Set(0);
                //    TimePeriod.Sleep(waitTime*2);
                //    _mcp4131_10k.Set(dp.MaxDigitalValue);
                //    TimePeriod.Sleep(waitTime*3);
                //}

                for (var i = dp.MaxDigitalValue; i > ledThreadHold17Volt; i -= 1)
                {
                    if (!dp.Set(i).Succeeded)
                        Console.WriteLine("Communication error");
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) { quit = true; break; } 
                }
                //TimePeriod.Sleep(waitTime*10);
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) { quit = true; break; } 
            }
            var k = Console.ReadKey();
            dp.Set(0);
        }

        static void TestDigitalPotRange(MCP41X1_Base dp, bool tryReadValue = false)
        {
            Console.Clear();
            for (var i = dp.MinDigitalValue; i <= (dp.MaxDigitalValue); i += demoStep)
            {
                if (!dp.Set(i).Succeeded)
                    Console.WriteLine("Communication error");
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, tryReadValue)) 
                    break;
                if(dp.Amp > 1.4)
                    break;
            }
            dp.Set(0);
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

            using (var nusbio = new Nusbio(serialNumber))
            {
                Cls(nusbio);
                /*
                MCP4131
                    CS(SELECT g1)    [    ] VDD(POWER)
                    SCK(CLOCK g0)    [    ] P0B(POTENTIOMETER-GND)
                    SDI/SDO(MOSI g2) [    ] P0W(OUTPUT)
                    VSS(GND)         [    ] P0A(VCC)
                */
                _mcp4131_10k = new  MCP4131(nusbio, 
                    NusbioGpio.Gpio1, // Select
                    NusbioGpio.Gpio2, // Mosi
                    NusbioGpio.None,  // Miso // USE THE SAME FOR NOW
                    NusbioGpio.Gpio0  // Clock                    
                    );
                _mcp4131_10k.Begin();

                //nusbio.SetPinMode(NusbioGpio.Gpio3, PinMode.Input);

                //_mcp4132_100k = new  MCP4132 (nusbio, 
                //    NusbioGpio.Gpio3, // Select
                //    NusbioGpio.Gpio2, // Mosi
                //    NusbioGpio.Gpio0 // Clock                    
                //    );
                //_mcp4132_100k.Begin();
                
                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.D0)
                        {
                            TestDigitalPotRange(_mcp4131_10k, !true);
                        }
                        if (k == ConsoleKey.D1)
                        {
                            TestDigitalPotRange(_mcp4132_100k);
                        }
                        if (k == ConsoleKey.L)
                        {
                            DigitalPotRangeForLed(_mcp4131_10k);
                        }
                        if (k == ConsoleKey.O)
                        {
                            OtherApiTests(_mcp4131_10k);
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q) {
                            
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


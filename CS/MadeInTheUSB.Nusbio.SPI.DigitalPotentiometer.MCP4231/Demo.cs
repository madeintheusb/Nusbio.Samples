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
using MadeInTheUSB.Components;

namespace DigitalPotentiometerSample
{
    class Demo
    {
        static BiColorLedMCP4231Manager _mcp4231_10k;
        static int                      _waitTime = 100; // 20
        static int                      _demoStep = 5;
        
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

            ConsoleEx.TitleBar(4, "0) Loop through 10k Pot,  O)ther Api test ", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(5, "B)iColor Led driven with 2 potentiometer (MCP4231)", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(6, "Q)uit", ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        private static bool DisplayPotentiometerInfoAndCheckForCancel(MCP41X1_Base ldp, bool tryReadValue = false, int exWaitTime = -1)
        {
            Console.WriteLine(ldp.ToString());
            if(exWaitTime == -1)
                TimePeriod.Sleep(_waitTime * 1);
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
            try 
            { 
                var expectedValue = 0;
                dp.Set(0);
                for (var i = 0; i <= dp.MaxDigitalValue; i += _demoStep)
                {
                    expectedValue += _demoStep;
                    if(expectedValue > dp.MaxDigitalValue) 
                        expectedValue = dp.MaxDigitalValue;
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp)) { quit = true; break;}
                    dp.Increment(_demoStep);
                    var v = dp.Get();
                    if(expectedValue != v)
                        Console.WriteLine("Method Get() did not return expected result");
                }

                if(quit)
                    return;

                dp.Set(dp.MaxDigitalValue);
                DisplayPotentiometerInfoAndCheckForCancel(dp);

                expectedValue = dp.MaxDigitalValue;

                for (var i = dp.MaxDigitalValue; i > 0; i -= _demoStep)
                {
                    expectedValue -= _demoStep;
                    if(expectedValue < dp.MinDigitalValue)
                        expectedValue = dp.MinDigitalValue;
                    if(DisplayPotentiometerInfoAndCheckForCancel(dp)) { quit = true; break;}
                    dp.Decrement(_demoStep);
                    var v = dp.Get();
                    if(expectedValue != v)
                        Console.WriteLine("Method Get() did not return expected result");
                }
            }
            finally
            {
                dp.Set(0);
            }
        }

        static bool AnimateOneLed(MCP4231 dp, MCP41X1_Base.ADDRESS pot0, MCP41X1_Base.ADDRESS? pot1, int wait)
        {
            int startStep = 38;
            int step = 4;
            for (var i = startStep; i <= dp.MaxDigitalValue; i += step)
            {
                if (!dp.Set(i, pot0).Succeeded) Console.WriteLine("Communication error");
                if(pot1.HasValue)
                    if (!dp.Set(i, pot1.Value).Succeeded) Console.WriteLine("Communication error");
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) 
                    return true; // Quit
            }
            
            for (var i = dp.MaxDigitalValue; i > startStep; i -= step)
            {
                if (!dp.Set(i, pot0).Succeeded) Console.WriteLine("Communication error");
                if(pot1.HasValue)
                    if (!dp.Set(i, pot1.Value).Succeeded) Console.WriteLine("Communication error");
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, exWaitTime:wait)) 
                    return true; // Quit
            }
            return false;
        }
                
        static void TestDigitalPotRange(MCP41X1_Base dp, bool tryReadValue = false)
        {
            Console.Clear();
            for (var i = dp.MinDigitalValue; i <= dp.MaxDigitalValue; i += _demoStep)
            {
                if (!dp.Set(i).Succeeded)
                    Console.WriteLine("Communication error");
                if(DisplayPotentiometerInfoAndCheckForCancel(dp, tryReadValue)) 
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
                MCP4231
                    CS(SELECT g1)          [    ] VDD(POWER)
                    SCK(CLOCK g0)          [    ] SDO(MISO g3)
                    SDI/SDO(MOSI g2)       [    ] SHDN
                    VSS(GND)               [    ] WP
                    P0B(POTENTIOMETER-GND) [    ] P0B(POTENTIOMETER-GND)
                    P0W(OUTPUT)            [    ] P0W(OUTPUT)
                    P0A(VCC)               [    ] P0A(VCC)
                    
                */
                _mcp4231_10k = new  BiColorLedMCP4231Manager(nusbio, 
                    NusbioGpio.Gpio1, // Select
                    NusbioGpio.Gpio2, // Mosi
                    NusbioGpio.Gpio3, // Miso // USE THE SAME FOR NOW
                    NusbioGpio.Gpio0  // Clock                    
                    );
                _mcp4231_10k.Begin();
                
                while(nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.D0)
                        {
                            TestDigitalPotRange(_mcp4231_10k, !true);
                        }
                        if (k == ConsoleKey.B)
                        {
                            _mcp4231_10k.Animate();
                        }
                        if (k == ConsoleKey.O)
                        {
                            OtherApiTests(_mcp4231_10k);
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


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
using MadeInTheUSB.Components;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;

namespace LedConsole
{
    class Demo
    {
        static MCP23008 _mcp;
         
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
            //ConsoleEx.WriteMenu(-1, 2, "Gpio/Led: 0) 1) 2) 3) 4) 5) 6) 7) On/Off");
            ConsoleEx.WriteMenu(-1, 10, "O)ff All Q)uit");
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

            var clockPin         = NusbioGpio.Gpio0;
            var dataOutPin       = NusbioGpio.Gpio1;
            byte MCP23008EP_ADDR = 0x20;

            using (var nusbio = new Nusbio(serialNumber))
            {
                // Add 8 new gpios named Gpio9 to Gpio16
                _mcp = new MCP23008(nusbio, dataOutPin, clockPin, gpioStartIndex:9);
                _mcp.Begin(MCP23008EP_ADDR);

                _mcp.SetPinMode(NusbioGpioEx.Gpio9,  PinMode.Output);
                _mcp.SetPinMode(NusbioGpioEx.Gpio10, PinMode.Output);
                _mcp.SetPinMode(NusbioGpioEx.Gpio11, PinMode.Output);
                _mcp.SetPinMode(NusbioGpioEx.Gpio12, PinMode.Output);
                _mcp.SetPinMode(NusbioGpioEx.Gpio13, PinMode.Output);
                _mcp.SetPinMode(NusbioGpioEx.Gpio14, PinMode.Input);
                _mcp.SetPinMode(NusbioGpioEx.Gpio15, PinMode.Input );
                _mcp.SetPinMode(NusbioGpioEx.Gpio16, PinMode.Input );

                Cls(nusbio);

                while (nusbio.Loop())
                {
                    _mcp.GPIOS[NusbioGpioEx.Gpio9].DigitalWrite(PinState.High);
                    _mcp.GPIOS[NusbioGpioEx.Gpio10].DigitalWrite(PinState.High);
                    TimePeriod.Sleep(200);

                    _mcp.GPIOS[NusbioGpioEx.Gpio9].DigitalWrite(PinState.Low);
                    _mcp.GPIOS[NusbioGpioEx.Gpio10].DigitalWrite(PinState.Low);
                    TimePeriod.Sleep(200);

                    ConsoleEx.Write(0, 4, string.Format("[{0}] Input14 detected:{1},{2} ", DateTime.Now, 
                        _mcp.GPIOS[NusbioGpioEx.Gpio14].DigitalRead(),
                        _mcp.DigitalRead(14)
                        ), ConsoleColor.Cyan);

                    ConsoleEx.Write(0, 5, string.Format("[{0}] Input15 detected:{1},{2} ", DateTime.Now, 
                        _mcp.GPIOS[NusbioGpioEx.Gpio15].DigitalRead(),
                        _mcp.DigitalRead(15)
                        ), ConsoleColor.Cyan);

                    ConsoleEx.Write(0, 6, string.Format("[{0}] Input16 detected:{1},{2} ", DateTime.Now, 
                        _mcp.GPIOS[NusbioGpioEx.Gpio16].DigitalRead(),
                        _mcp.DigitalRead(16)
                        ), ConsoleColor.Cyan);                    

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.Q) break;
                        if (k == ConsoleKey.C) Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }

    }
}


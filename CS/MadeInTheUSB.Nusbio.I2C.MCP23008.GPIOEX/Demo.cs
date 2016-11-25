#define GPIO_15_AS_INPUT
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
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;
using MadeInTheUSB.WinUtil;

namespace LightSensorConsole
{
    class Demo
    {
        //private static EEPROM_24LC256 _eeprom;
        static MCP23008 _mcp;

        //private const byte NEW_WRITTEN_VALUE_1 = 64+32+16+8+4+2+1;
        private const byte NEW_WRITTEN_VALUE_1 = 128+1;
        private const byte NEW_WRITTEN_VALUE_2 = 170;

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
            ConsoleEx.WriteMenu(-1, 5, "Q)uit");
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
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
            byte MCP23008_I2C_ADDR = 0x20; // Microship MCP 23008 = 8 gpios

            // Gpio 0 and 1 are reserved for I2C Bus
            var standardOutputGpios = new List<NusbioGpio>() {
                    NusbioGpio.Gpio2,
                    NusbioGpio.Gpio3,
                    NusbioGpio.Gpio4,
                    NusbioGpio.Gpio5,
                    NusbioGpio.Gpio6,
                    NusbioGpio.Gpio7
                };
            var extendedOutputGpios = new List<string>() {
                    NusbioGpioEx.Gpio8 ,
                    NusbioGpioEx.Gpio9 ,
                    NusbioGpioEx.Gpio10,
                    NusbioGpioEx.Gpio11,
                    NusbioGpioEx.Gpio12,
                    NusbioGpioEx.Gpio13,
                    NusbioGpioEx.Gpio14,
#if !GPIO_15_AS_INPUT 
                 NusbioGpioEx.Gpio15,// Configured as input
#endif
            };

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                _mcp = new MCP23008(nusbio, sdaPin, sclPin);
                _mcp.Begin(MCP23008_I2C_ADDR);

                Cls(nusbio);

                #if GPIO_15_AS_INPUT
                var inputGpio = NusbioGpioEx.Gpio15;
                _mcp.SetPinMode(inputGpio, PinMode.InputPullUp);
                #endif

                while (nusbio.Loop())
                {
                    #if GPIO_15_AS_INPUT
                    if (_mcp.GPIOS[inputGpio].DigitalRead() == PinState.High)
                        ConsoleEx.Write(0, 2, string.Format("[{0}] Button Down", DateTime.Now), ConsoleColor.Cyan);
                    else
                        ConsoleEx.Write(0, 2, string.Format("[{0}] Button Up     ", DateTime.Now), ConsoleColor.Cyan);
                    #endif  

                    foreach (var eg in extendedOutputGpios)
                    {
                        _mcp.GPIOS[eg].State = !_mcp.GPIOS[eg].State;
                        _mcp.GPIOS[eg].DigitalWrite(_mcp.GPIOS[eg].State);
                    }
                    //foreach (var sg in standardOutputGpios)
                    //{
                    //    nusbio.GPIOS[sg].State = !nusbio.GPIOS[sg].State;
                    //    nusbio.GPIOS[sg].DigitalWrite(nusbio.GPIOS[sg].State);
                    //}
                    TimePeriod.Sleep(500);

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.A)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q)
                        {
                            _mcp.AllOff();
                            break;
                        }
                    }
                }
            }
            Console.Clear();
        }
    }
}

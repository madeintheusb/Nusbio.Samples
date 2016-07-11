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
        private static EEPROM_24LC256 _eeprom;
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

        static void WriteEEPromPerPage(int numberOfPageToRead)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();
            var refBuffer = new List<Byte>();

            for (var x = 0; x < EEPROM_24LC256.PAGE_SIZE; x++)
            {
                refBuffer.Add((byte)(NEW_WRITTEN_VALUE_1 + x));
            }

            for (var p = 2; p < numberOfPageToRead; p++)
            {
                if(p % 10 == 0)
                    Console.WriteLine("Writing page {0}", p);

                var r = _eeprom.WritePage(p*EEPROM_24LC256.PAGE_SIZE, refBuffer.ToArray());
                if (!r)
                {
                    Console.WriteLine("WriteBuffer failure");
                }
                break;
            }
            t.Stop();
            Console.WriteLine("{0} error(s), Time:{1}", totalErrorCount, t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void WriteEEPromBytePerByte(int maxPage)
        {
            Console.Clear();
            var totalErrorCount = 0;
            Console.WriteLine("Write first 64 byte one by one");
            var t = Stopwatch.StartNew();
            var p = 2;
            Console.WriteLine("Writing page [{0}]", p);

            for (var i = 0; i < EEPROM_24LCXXX.PAGE_SIZE; i++)
            {
                //Console.WriteLine("Reading byte [{0}]", i);
                var addr     = (p * EEPROM_24LC256.PAGE_SIZE ) + i;
                var b = _eeprom.WriteByte(addr, (byte)(NEW_WRITTEN_VALUE_1+i));
                //Console.WriteLine(String.Format("[{0}] = {1}", i, b));
            }
            Console.WriteLine("{0} error(s), Time:{1}", totalErrorCount, t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void ReadEEPromBytePerByte(int maxPage)
        {
            Console.Clear();
            var totalErrorCount = 0;
            Console.WriteLine("Reading first 64 byte one by one");
            var t = Stopwatch.StartNew();

            for (var p = 0; p < maxPage; p++) { 

                Console.WriteLine("Reading page [{0}]", p);

                for (var i = 0; i < EEPROM_24LC256.PAGE_SIZE; i++)
                {
                    //Console.WriteLine("Reading byte [{0}]", i);
                    var addr     = (p * EEPROM_24LC256.PAGE_SIZE ) + i;
                    var b        = _eeprom.ReadByte(addr);
                    var expected = i;
                    //if (p == 0 && i == 0) expected = 33;
                    //if (p == 1 && i == 0) expected = 32;
                    //if (p == 1 && i == 1) expected = 0;
                    //if (p == 1 && i == 2) expected = 0;

                    if (b != expected)
                    {
                        Console.WriteLine("Failed  [{0}] = {1}, expected {2}", addr, b, i);
                        totalErrorCount++;
                    }
                    //Console.WriteLine(String.Format("[{0}] = {1}", i, b));
                }
            }
            Console.WriteLine("{0} error(s), Time:{1}", totalErrorCount, t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void ReadEEPromPerPage(int numberOfPageToRead, Nusbio nusbio)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();
            byte [] buf;

            //nusbio.SetBaudRate(230400*8);

            for (var p = 0; p < numberOfPageToRead; p++)
            {
                if(p % 50 == 0 || p<5)
                    Console.WriteLine("Reading page {0}", p);

                var r = _eeprom.ReadPage(p*EEPROM_24LC256.PAGE_SIZE, EEPROM_24LC256.PAGE_SIZE);
                if (r.Succeeded)
                {
                    buf = r.Buffer;
                    for (var i = 0; i < EEPROM_24LC256.PAGE_SIZE; i++)
                    {
                        var expected = i;
                        if (p == 2)
                            expected = NEW_WRITTEN_VALUE_1;
                        if (p == 3)
                            expected = NEW_WRITTEN_VALUE_2;

                        if (buf[i] != expected)
                        {
                            Console.WriteLine("Failed Page:{0} [{1}] = {2}, expected {3}", p, i, buf[i], expected);
                            totalErrorCount++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ReadBuffer failure");
                }
            }
            t.Stop();
            Console.WriteLine("{0} error(s), Time:{1}", totalErrorCount, t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteMenu(-1, 5, "0) Read 10 pages A)ll read all pages");
            ConsoleEx.WriteMenu(-1, 7, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);

            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-4, string.Format("EEPROM:{0}",  _eeprom.ToString()), ConsoleColor.Black, ConsoleColor.DarkCyan);
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
            byte EEPROM_I2C_ADDR   = 0x50; // Microship 24LC256 = 32k
            byte MCP23008_I2C_ADDR = 0x20; // Microship MCP 23008 = 8 gpios

            using (var nusbio = new Nusbio(serialNumber)) // , 
            {
                _eeprom = new EEPROM_24LC256(nusbio, sdaPin, sclPin);
                _eeprom.Begin(EEPROM_I2C_ADDR);
                _mcp = new MCP23008(nusbio, sdaPin, sclPin, gpioStartIndex:9);
                _mcp.Begin(MCP23008_I2C_ADDR);

                Cls(nusbio);

                var standardsGpios = new List<NusbioGpio>() {
                    NusbioGpio.Gpio2,
                    NusbioGpio.Gpio3,
                    NusbioGpio.Gpio4,
                    NusbioGpio.Gpio5,
                    NusbioGpio.Gpio6,
                    NusbioGpio.Gpio7
                };
                var extendedGpios = new List<string>() {
                    NusbioGpioEx.Gpio9 , 
                    NusbioGpioEx.Gpio10, 
                    NusbioGpioEx.Gpio11, 
                    NusbioGpioEx.Gpio12, 
                    NusbioGpioEx.Gpio13, 
                    NusbioGpioEx.Gpio14, 
                    NusbioGpioEx.Gpio15, 
                    NusbioGpioEx.Gpio16
                };

                var g = NusbioGpioEx.Gpio16;
                _mcp.SetPinMode(g, PinMode.Input);

                while(nusbio.Loop())
                {
                    //if (_mcp.GPIOS[g].DigitalRead() == PinState.High)
                    //{
                    //    ConsoleEx.Write(0, 10, string.Format("[{0}] Button Down", DateTime.Now), ConsoleColor.Cyan);
                    //}
                    //else
                    //{
                    //    ConsoleEx.Write(0, 10, string.Format("[{0}] Button Up     ", DateTime.Now), ConsoleColor.Cyan);
                    //}

                    foreach (var eg in extendedGpios) { 

                        _mcp.GPIOS[eg].State = !_mcp.GPIOS[eg].State;
                        _mcp.GPIOS[eg].DigitalWrite(_mcp.GPIOS[eg].State);
                    }
                    foreach (var sg in standardsGpios) { 

                        nusbio.GPIOS[sg].State = !nusbio.GPIOS[sg].State;
                        nusbio.GPIOS[sg].DigitalWrite(nusbio.GPIOS[sg].State);
                    }
                    TimePeriod.Sleep(300);

                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.D0)
                         {
                            ReadEEPromPerPage(10, nusbio);
                            Cls(nusbio);
                        }
                        
                        if (k == ConsoleKey.A)
                        {
                            ReadEEPromPerPage(512, nusbio);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q) {
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

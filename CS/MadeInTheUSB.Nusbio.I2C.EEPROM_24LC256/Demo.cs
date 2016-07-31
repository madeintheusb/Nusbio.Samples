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

namespace LightSensorConsole
{
    /// <summary>
    /// This demo write and read the I2C EEPROM 24L256 (32k byte of data).
    /// The bytes can be accessed one at the time or per page of 64 bytes.
    /// To test the code we write the 32k byte of data one page at the time.
    /// To test the code we read the 32k byte of data one page at the time or in batch of 8 to 16 pages.
    /// All 64 bytes page have values : 0..64
    /// Except page 2 which all value are 129
    /// Except page 3 which all value are 170
    /// </summary>
    class Demo
    {
        private static EEPROM_24LC256 _eeprom;
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

        static void WriteEEPROMPage(int numberOfPageToRead)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();

            for (var p = 0; p < numberOfPageToRead; p++)
            {
                var refBuffer = new List<Byte>();
                for (var x = 0; x < EEPROM_24LC256.PAGE_SIZE; x++)
                {
                    if(p == 2)
                        refBuffer.Add((byte)NEW_WRITTEN_VALUE_1);
                    else if(p == 3)
                        refBuffer.Add((byte)NEW_WRITTEN_VALUE_2);
                    else 
                        refBuffer.Add((byte)x);
                }

                if(p % 10 == 0)
                    Console.WriteLine("Writing page {0}", p);

                var r = _eeprom.WritePage(p*EEPROM_24LC256.PAGE_SIZE, refBuffer.ToArray());
                if (!r)
                {
                    Console.WriteLine("WriteBuffer failure");
                }
            }
            t.Stop();
            Console.WriteLine("{0} error(s), Time:{1}", totalErrorCount, t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void WriteEEPROMByte(int maxPage)
        {
            Console.Clear();
            var totalErrorCount = 0;
            Console.WriteLine("Write first 64 byte one by one");
            var t = Stopwatch.StartNew();
            var p = 2;
            Console.WriteLine("Writing page [{0}]", p);

            for (var i = 0; i < EEPROM_24LC256.PAGE_SIZE; i++)
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

        static void ReadEEPROMByte(int maxPage)
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
                    if (p == 2)
                        expected = NEW_WRITTEN_VALUE_1;
                    if (p == 3)
                        expected = NEW_WRITTEN_VALUE_2;


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

        static void ReadAndVerifyEEPROMPage(int numberOfPageToRead)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();
            byte [] buf;

            for (var pageIndex = 0; pageIndex < numberOfPageToRead; pageIndex++)
            {
                if(pageIndex % 50 == 0 || pageIndex < 5)
                    Console.WriteLine("Reading page {0}", pageIndex);

                var r = _eeprom.ReadPage(pageIndex*EEPROM_24LC256.PAGE_SIZE, EEPROM_24LC256.PAGE_SIZE);
                if (r.Succeeded)
                {
                    VerifyPage(r.Buffer, pageIndex, 0);
                }
                else
                {
                    Console.WriteLine("ReadBuffer failure");
                }
            }
            t.Stop();
            
            Console.WriteLine("{0} error(s), Time:{1}, {2:0.00} kb/s", 
                totalErrorCount, 
                t.ElapsedMilliseconds,
                _eeprom.MaxByte*1.0/t.ElapsedMilliseconds
                );
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static bool VerifyPage(byte [] buf, int pageIndex, int batchIndex) {

            int totalErrorCount = 0;
            for (var i = 0; i < EEPROM_24LC256.PAGE_SIZE; i++)
            {
                var expected = i;
                if (pageIndex == 2)
                    expected = NEW_WRITTEN_VALUE_1;
                if (pageIndex == 3)
                    expected = NEW_WRITTEN_VALUE_2;

                if (buf[i] != expected)
                {
                    Console.WriteLine("Failed Page:{0} [{1}] = {2}, expected {3}", pageIndex, i, buf[i], expected);
                    totalErrorCount += 1;
                }
            }
            return totalErrorCount == 0;
        }
        
        static void ReadAndVerifyEEPROMPageInBatch(int numberOfPageToRead)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t               = Stopwatch.StartNew();
            var batchSize       = 64;
            var pageIndex       = 0;
            byte[] buf;

            for (var batchIndex = 0; batchIndex < numberOfPageToRead; batchIndex += batchSize)
            {
                Console.WriteLine("Reading page {0}", batchIndex);

                var r = _eeprom.ReadPage(batchIndex * EEPROM_24LC256.PAGE_SIZE, EEPROM_24LC256.PAGE_SIZE * batchSize);
                if (r.Succeeded)
                {
                    for (var b = 0; b < batchSize; b++)
                    {
                        buf = r.GetPage(b, EEPROM_24LC256.PAGE_SIZE);
                        VerifyPage(buf, pageIndex, batchIndex);
                        pageIndex += 1;
                    }
                }
                else
                {
                    Console.WriteLine("ReadBuffer failure");
                }
            }
            t.Stop();

            Console.WriteLine("{0} error(s), Time:{1}, {2:0.00} kb/s",
                totalErrorCount,
                t.ElapsedMilliseconds,
                _eeprom.MaxByte * 1.0 / t.ElapsedMilliseconds
                );
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteMenu(-1, 4, "R)ead 10 pages   Read A)ll 32k   Read all 32k in B)atch mode   W)rite 32k");
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
            
            var clockPin = NusbioGpio.Gpio0;
            var dataPin  = NusbioGpio.Gpio1;

            using (var nusbio = new Nusbio(serialNumber)) 
            {
                Cls(nusbio);

                var mask = nusbio.GetGpioMask();

                byte EEPROM1_WR = 80; // 0xA0;

                _eeprom = new EEPROM_24LC256(nusbio, dataPin, clockPin);
                _eeprom.Begin(EEPROM1_WR);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                            
                        if (k == ConsoleKey.W)
                        {
                            var a = ConsoleEx.Question(23, "Write 32k now Y)es, N)o", new List<char>()  { 'Y', 'N' });
                            if (a == 'Y') {
                                WriteEEPROMPage(512);
                            }
                        }

                        if (k == ConsoleKey.R)
                        {
                            ReadAndVerifyEEPROMPage(10);
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.A)
                        {
                            ReadAndVerifyEEPROMPage(512);
                        }
                        if (k == ConsoleKey.B)
                        {
                            ReadAndVerifyEEPROMPageInBatch(512);
                        }
                        if (k == ConsoleKey.Q) break;

                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}

//var i2c = new I2CEngine(nusbio, NusbioGpio.Gpio1, NusbioGpio.Gpio0, EEPROM1_WR);
//var addr = 0;
//var buffer = new byte[64];
//if (i2c.WriteBuffer(new byte[2] { (byte)(addr >> 8), (byte)(addr & 0xFF) }))
//{
//    var r = i2c.ReadBuffer(64, buffer);
//}
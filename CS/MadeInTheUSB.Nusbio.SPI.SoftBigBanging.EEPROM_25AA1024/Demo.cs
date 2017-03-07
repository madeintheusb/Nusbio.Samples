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
    WHETHER IN AN ACTION OF CONTRACT, TO    RT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
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
using MadeInTheUSB.Components;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;

namespace MadeInTheUSB
{
    /// <summary>
    /// This demo write and read the SPI EEPROM 25AA1024 (128k byte of data).
    /// The bytes can be accessed one at the time or per page of 256 bytes.
    /// To test the code we write the 128k byte of data one page at the time.
    /// To test the code we read the 128k byte of data one page at the time or in batch of 8 pages.
    /// All 64 bytes page have values : 0..256
    /// Except page 2 which all value are 129
    /// Except page 3 which all value are 170
    /// </summary>
    class Demo
    {
        private static EEPROM_25AA1024 _eeprom;
        private const byte NEW_WRITTEN_VALUE_1 = 128 + 1;
        private const byte NEW_WRITTEN_VALUE_2 = 170;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }

        static void WriteEEPROMPage(int numberOfPageToRead, int valueToWrite = -1)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Writing EEPROM");
            ConsoleEx.WriteMenu(0, 1, "");

            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();

            for (var p = 0; p < numberOfPageToRead; p++)
            {
                var refBuffer = new List<Byte>();
                for (var x = 0; x < _eeprom.PAGE_SIZE; x++)
                {
                    if (valueToWrite != -1)
                    {
                        refBuffer.Add((byte)valueToWrite);
                    }
                    else
                    {
                        if (p == 2)
                            refBuffer.Add((byte)NEW_WRITTEN_VALUE_1);
                        else if (p == 3)
                            refBuffer.Add((byte)NEW_WRITTEN_VALUE_2);
                        else
                            refBuffer.Add((byte)x);
                    }
                }

                if (p % 10 == 0)
                    Console.WriteLine("Writing page {0}", p);

                var r = _eeprom.WritePage(p * _eeprom.PAGE_SIZE, refBuffer.ToArray());
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

            for (var i = 0; i < _eeprom.PAGE_SIZE; i++)
            {
                //Console.WriteLine("Reading byte [{0}]", i);
                var addr = (p * _eeprom.PAGE_SIZE) + i;
                var b = _eeprom.WriteByte(addr, (byte)(NEW_WRITTEN_VALUE_1 + i));
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

            for (var p = 0; p < maxPage; p++)
            {

                Console.WriteLine("Reading page [{0}]", p);

                for (var i = 0; i < _eeprom.PAGE_SIZE; i++)
                {
                    //Console.WriteLine("Reading byte [{0}]", i);
                    var addr = (p * _eeprom.PAGE_SIZE) + i;
                    var b = _eeprom.ReadByte(addr);
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

        static void ReadAndVerifyEEPROMPage(int numberOfPageToRead, int expectedSingleValue = -1)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, string.Format("Performance test reading all {0}k no batch standard SPI code", _eeprom.MaxKByte));
            ConsoleEx.Gotoxy(0, 2);

            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();
            byte[] buf;

            for (var p = 0; p < numberOfPageToRead; p++)
            {
                if (p % 50 == 0 || p < 5)
                    Console.WriteLine("Reading page {0}", p);

                // The method ReadPage, use the default SPI method to transfer data
                var r = _eeprom.ReadPage(p * _eeprom.PAGE_SIZE, _eeprom.PAGE_SIZE);
                if (r.Succeeded)
                {
                    buf = r.Buffer;
                    for (var i = 0; i < _eeprom.PAGE_SIZE; i++)
                    {
                        var expected = i;
                        if (p == 2)
                            expected = NEW_WRITTEN_VALUE_1;
                        if (p == 3)
                            expected = NEW_WRITTEN_VALUE_2;

                        if (expectedSingleValue != -1)
                            expected = expectedSingleValue;

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
            Console.WriteLine("{0} error(s), Data:{1}kb, Time:{2}, {3:0.00} kb/s",
                totalErrorCount,
                _eeprom.PAGE_SIZE * numberOfPageToRead, t.ElapsedMilliseconds, _eeprom.MaxByte * 1.0 / t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void ReadAndVerifyEEPROMPage_BatchRead(int numberOfPageToRead, int expectedSingleValue = -1)
        {
            Console.Clear();

            var batchPageSize = 128; // Transfer batch of 32page x 256byte = 8k

            ConsoleEx.TitleBar(0, string.Format("Performance test reading all {0}k in {1} pages batch mode", _eeprom.MaxKByte, batchPageSize));
            ConsoleEx.Gotoxy(0, 2);

            var t = Stopwatch.StartNew();
            EEPROM_25AA1024_UnitTests.PerformanceTest_ReadPage(_eeprom, batchPageSize,
                (pageIndex, maxPage) => ConsoleEx.WriteLine(0, 3, string.Format("{0:000} % done", 1.0 * pageIndex / maxPage * 100.0), ConsoleColor.Yellow));
            t.Stop();

            Console.WriteLine("Data:{0}kb, Time:{1}ms, {2:0.00} kb/s", _eeprom.MaxKByte, t.ElapsedMilliseconds, _eeprom.MaxByte * 1.0 / t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void ReadAndVerifyEEPROMOnyByteAtTheTime(int numberOfPageToRead)
        {
            Console.Clear();
            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();
            byte[] buf;

            for (var p = 0; p < numberOfPageToRead; p++)
            {
                if (p % 50 == 0 || p < 5)
                    Console.WriteLine("Reading page {0}", p);

                for (var bx = 0; bx < _eeprom.PAGE_SIZE; bx++)
                {
                    var b = _eeprom.ReadByte((p * _eeprom.PAGE_SIZE) + bx);
                    if (b != -1)
                    {
                        var expected = bx;
                        if (p == 2)
                            expected = NEW_WRITTEN_VALUE_1;
                        if (p == 3)
                            expected = NEW_WRITTEN_VALUE_2;

                        if (b != expected)
                        {
                            Console.WriteLine("Failed Page:{0} [{1}] = {2}, expected {3}", p, bx, b, expected);
                            totalErrorCount++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Read Byte failure");
                    }
                }
            }
            t.Stop();
            Console.WriteLine("{0} error(s), Time:{1}, {2:0.00} kb/s", totalErrorCount, t.ElapsedMilliseconds, _eeprom.MaxByte * 1.0 / t.ElapsedMilliseconds);
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteMenu(-1, 4, "R)ead 10 pages   read A)ll 128k   read all B)yte mode 128k");
            ConsoleEx.WriteMenu(-1, 5, "W)rite 128k");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");

            Nusbio.ActivateFastMode();

            var serialNumber = Nusbio.Detect();

            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }

            using (var nusbio = new Nusbio(serialNumber))
            {
                //_eeprom = new EEPROM_25AA1024(
                //   nusbio: nusbio,
                //   clockPin: NusbioGpio.Gpio4,
                //   mosiPin: NusbioGpio.Gpio5,
                //   misoPin: NusbioGpio.Gpio2,
                //   selectPin: NusbioGpio.Gpio6
                //   );


                _eeprom = new EEPROM_25AA1024(
                    nusbio: nusbio,
                    clockPin: NusbioGpio.Gpio0,
                    mosiPin: NusbioGpio.Gpio1,
                    misoPin: NusbioGpio.Gpio2,
                    selectPin: NusbioGpio.Gpio3
                    );

                _eeprom.Begin();

                // Noticed that we cannot detect if the EEPROM is connected or
                // not based on SPI command. The SPI command does not fail.
                // But return a buffer with garbage mostly all byte set to 255
                // So based on the expected data of the first page we could 
                // detect of the EEPROM is connected or not
                var r = _eeprom.ReadPage(0, _eeprom.PAGE_SIZE * 4);
                if (r.Succeeded)
                {
                    if(r.Buffer[0] == 255)
                    {
                        Console.WriteLine("SPI EEPROM 25AA1024 not detected, hit enter to continue");
                        Console.ReadLine();
                    }
                }

                Cls(nusbio);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.W)
                        {
                            var a = ConsoleEx.Question(23, string.Format("Execute Write/Read/Write Test {0}k now Y)es, N)o", _eeprom.MaxKByte), new List<char>() { 'Y', 'N' });
                            if (a == 'Y')
                            {

                                Console.Clear(); ConsoleEx.TitleBar(0, "Writing EEPROM");

                                var testValue = 1 + 4 + 16 + 64;
                                _eeprom.WriteAll(0, EEPROM_25AA1024.MakeBuffer((byte)testValue, _eeprom.MaxByte), (pageIndex, maxPage) => ConsoleEx.WriteLine(0, 2, string.Format("{0:000} % done", 1.0 * pageIndex / maxPage * 100.0), ConsoleColor.Yellow));

                                Console.Clear(); ConsoleEx.TitleBar(0, "Reading EEPROM");
                                var dataRead = _eeprom.ReadAll(16, (pageIndex, maxPage) => ConsoleEx.WriteLine(0, 2, string.Format("{0:000} % done", 1.0 * pageIndex / maxPage * 100.0), ConsoleColor.Yellow));
                                for (var z = 0; z < _eeprom.MaxByte; z++)
                                {
                                    if (dataRead[z] != testValue)
                                        throw new ArgumentException("Writing/Reading error");
                                }
                                WriteEEPROMPage(512);
                                ReadAndVerifyEEPROMPage_BatchRead(512);
                            }
                        }

                        if (k == ConsoleKey.R)
                        {
                            ReadAndVerifyEEPROMPage(10);
                            Cls(nusbio);
                        }

                        if (k == ConsoleKey.A)
                        {
                            ReadAndVerifyEEPROMPage_BatchRead(512);
                            ReadAndVerifyEEPROMPage(512);
                        }
                        if (k == ConsoleKey.B)
                        {
                            // This mode is slow, only read the first 64 pages out of 512
                            ReadAndVerifyEEPROMOnyByteAtTheTime(64);
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
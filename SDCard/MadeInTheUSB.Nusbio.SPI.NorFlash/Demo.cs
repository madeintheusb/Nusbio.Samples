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
using MadeInTheUSB.FLASH;
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
        private static NOR_FLASH_S25FL164K norFlash;

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

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 4, "R)ead all  W)rite All");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");
            ConsoleEx.WriteLine(0, 10, norFlash.GetFlashInfo(), ConsoleColor.Cyan);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        static void WriteFlashMemory(int valueToWrite = -1)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Writing Flash Memory");
            ConsoleEx.WriteLine(0, 2, string.Format("Max Sector {0}, Sector Size {1}k, Page Size:{2}b, Total Byte:{3}", 
                norFlash.MaxSector, 
                norFlash.SectorSize,
                norFlash.PAGE_SIZE,
                norFlash.MaxByte
                ), ConsoleColor.Yellow);

            var totalErrorCount = 0;
            var t = Stopwatch.StartNew();

            var str4kBuffer = Encoding.ASCII.GetBytes(Get4kString()).ToList();
            var str4kBufferReversed = Encoding.ASCII.GetBytes(Get4kString()).ToList();
            str4kBufferReversed.Reverse();

            for (var  p = 0; p < norFlash.MaxSector; p++)
            {
                if (p % 4 == 0)
                    Console.WriteLine("Writing Sector {0}", p);

                var bufferToWrite = str4kBuffer;
                if(p == 1 || p == 3)
                    bufferToWrite = str4kBufferReversed;

                var r = norFlash.Write4kSector(p, bufferToWrite);
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

        static void ReadFlashMemory()
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Read Flash Memory");
            ConsoleEx.WriteLine(0, 2, string.Format("Max Sector {0}, Sector Size {1}k, Page Size:{2}b, Total Byte:{3}", norFlash.MaxSector,norFlash.SectorSize,norFlash.PAGE_SIZE,norFlash.MaxByte), ConsoleColor.Yellow);

            var totalErrorCount = 0;
            var t               = Stopwatch.StartNew();
            var strData         = Get4kString();
            var totalRead       = 0;

            var sectorCountToRead = norFlash.MaxSector;
            //sectorCountToRead = 40;

            for (var p = 0; p < sectorCountToRead; p++) // norFlash.MaxSector
            {
                if (p % 4 == 0)
                    Console.WriteLine("Reading Sector {0}", p);

                var r = norFlash.ReadSector(p, norFlash.SectorSize, optimize: false);
                if (r.Succeeded)
                {
                    totalRead += r.Buffer.Length;

                    if (p == 1 || p == 3)
                    {
                        var lBuffer = r.Buffer.ToList();
                        lBuffer.Reverse();
                        r.Buffer = lBuffer.ToArray();
                    }

                    var strData2 = Encoding.ASCII.GetString(r.Buffer);
                    if (strData != strData2) 
                    {
                        Console.WriteLine("Read data compare failed");
                        totalErrorCount++;
                    }
                }
                else
                {
                    Console.WriteLine("Read data operation failed");
                    totalErrorCount++;
                }
            }
            t.Stop();
            Console.WriteLine("{0} error(s), Time:{1}, {2} K byte, {3} K byte/S", 
                totalErrorCount, 
                t.ElapsedMilliseconds,
                totalRead,
                totalRead / (t.ElapsedMilliseconds / 1000.0) / 1024
                );
            Console.WriteLine("Hit enter key");
            Console.ReadLine();
        }

        public static string Get4kString()
        {
            var s = "";
            for (var i = 0; i < 1024; i++)
                s += "ABCD";
            return s;
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

            // For Flash we always read a 4k sectors, which in hardware accelrated
            // bit banging will requires 4096 bytes * 8 * 2 = 64k.
            // 64k does not fit in the FTDI buffer which 64k-1.
            // So even with a buffer of 48k or 65k we still need 2 USB operations
            // 2 transfer 4k.
            Nusbio.ActivateFastMode(32*1024);

            using (var nusbio = new Nusbio(serialNumber))
            {
                norFlash = new NOR_FLASH_S25FL164K (
                    nusbio   : nusbio,
                    clockPin : NusbioGpio.Gpio0,
                    mosiPin  : NusbioGpio.Gpio1,
                    misoPin  : NusbioGpio.Gpio2,
                    selectPin: NusbioGpio.Gpio3
                    );

                norFlash.Begin();
                norFlash.ReadInfo();

                var wr = norFlash.Write4kSector(0, Encoding.ASCII.GetBytes(Get4kString()).ToList());
                var dataRead = norFlash.ReadSector(0, Get4kString().Length);
                var strData2 = Encoding.ASCII.GetString(dataRead.Buffer);
                if(Get4kString() != strData2) throw new ArgumentException();

                ////var wr = norFlash.Write4kSector(1, Encoding.ASCII.GetBytes(Get4kString()).ToList());
                //dataRead = norFlash.ReadPageS(1, Get4kString().Length);
                //strData2 = Encoding.ASCII.GetString(dataRead.Buffer);
                //if (Get4kString() != strData2) throw new ArgumentException();

                ////var wr = norFlash.Write4kSector(2, Encoding.ASCII.GetBytes(Get4kString()).ToList());
                //dataRead = norFlash.ReadPageS(6, Get4kString().Length);
                //strData2 = Encoding.ASCII.GetString(dataRead.Buffer);
                //if (Get4kString() != strData2) throw new ArgumentException();

                ////var wr = norFlash.Write4kSector(3, Encoding.ASCII.GetBytes(Get4kString()).ToList());
                //dataRead = norFlash.ReadPageS(7, Get4kString().Length);
                //strData2 = Encoding.ASCII.GetString(dataRead.Buffer);
                //if (Get4kString() != strData2) throw new ArgumentException();

               
                Cls(nusbio);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.W)
                        {
                            WriteFlashMemory();
                        }

                        if (k == ConsoleKey.R)
                        {
                            Cls(nusbio);
                            ReadFlashMemory();
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
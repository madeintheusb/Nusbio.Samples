//#define SD_RAW_SDHC
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

/*
  REFERENCES
     FatFs - Generic FAT File System Module
     http://elm-chan.org/fsw/ff/00index_e.html
 
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
using MadeInTheUSB.spi;

using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using uint32_t = System.UInt32;
using int32_t = System.Int32;

//using abs = System.Math

namespace MadeInTheUSB.SdCardDemo
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

        static void Cls(Nusbio nusbio, MICRO_SD_CARD mSdCard)
        {
            Console.Clear();
            
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);

            ConsoleEx.WriteMenu(-1, 4, "E)xport data  P)erformance  B)uild file0000.txt");
            ConsoleEx.WriteMenu(-1, 5, "I)nit Card  W)rite test");
            ConsoleEx.WriteMenu(-1, 6, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);

            ConsoleEx.WriteLine(0, 6, mSdCard.ToString(), ConsoleColor.Yellow);
        }
        
        public  static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();

            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }

            Nusbio.BaudRate = Nusbio.FastestBaudRate/2;
            Nusbio.ActivateFastMode(65536-64);

            using (var nusbio = new Nusbio(serialNumber))
            {
                var mSdCard = new MICRO_SD_CARD_TEST(
                    nusbio: nusbio,
                    clockPin: NusbioGpio.Gpio0,
                    mosiPin: NusbioGpio.Gpio1,
                    misoPin: NusbioGpio.Gpio2,
                    selectPin: NusbioGpio.Gpio3
                    );

                if (!mSdCard.Begin())
                {
                    Console.Clear();
                    Console.WriteLine("SD Card not found, un plug nusbio and re plug it");
                    Console.WriteLine("Hit any key to continue");
                    var k0 = Console.ReadKey();
                    return;
                }

                Cls(nusbio, mSdCard);

                while (nusbio.Loop())
                {
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;

                        if (k == ConsoleKey.E)
                            mSdCard.ExportData("Samsung.16Gb");

                        if (k == ConsoleKey.P)
                            mSdCard.PerformanceReadingFile0000Txt();

                        if (k == ConsoleKey.W)
                        {
                            //var sector = mSdCard.PerformanceWritingFile0000Txt('H'); //'A'
                            var sector = 0;
                            if (sector == -1)
                            {
                                Console.WriteLine("Write operation failed");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("Write operation succeeded");
                                Console.ReadKey();
                                //var sector = 15114240; // /2

                                //if (mSdCard.Manufacturer == CID_INFO.SD_CARD_MANUFACTURER.Samsung)
                                //    sector = 15645696; // /2 Samsung
                                //else
                                //    sector = 15114240; // /2

                                if (mSdCard.Manufacturer == CID_INFO.SD_CARD_MANUFACTURER.Samsung)
                                    sector = 15645696; //6258278;// 10430464; // /3 Samsung
                                else
                                    sector = 6045696; // /5    - 10076160; // /3

                                ReInitCard(mSdCard);

                                Console.Clear();

                                for (var i = 0; i < 10; i++)
                                {
                                    Console.WriteLine("**** PerformanceReadingFile0000Txt iteration:{0} ****", i);
                                    if (!mSdCard.PerformanceReadingFile0000Txt(sector))
                                    {
                                        Console.WriteLine("FAILED");
                                        Console.ReadLine();
                                    }
                                }
                                Console.ReadKey();
                            }
                        }

                        if (k == ConsoleKey.B)
                        {
                            var file0000Sectors = new List<int32_t>() { 16704, 16705, 16706, 16707, 16708 };
                            if(mSdCard.Manufacturer == CID_INFO.SD_CARD_MANUFACTURER.Samsung )
                                file0000Sectors = new List<int32_t>() { 16448, 16449, 16450, 16451, 16452 };
                            mSdCard.BuildFile(@"c:\FILE0000.txt", 2082, file0000Sectors);
                            //mSdCard.BuildFileContinousSector(@"c:\FILE0000.txt.txt", 2082, 16704, 4);
                        }
                        if (k == ConsoleKey.I)
                        {
                            ReInitCard(mSdCard);
                        }
                        if (k == ConsoleKey.Q)
                            break;

                        Cls(nusbio, mSdCard);
                    }
                }
            }
            Console.Clear();
        }

        private static void ReInitCard(MICRO_SD_CARD_TEST mSdCard)
        {
            Console.Clear();
            if (mSdCard.Begin())
                Console.WriteLine("Re initialization succeeded");
            else
                Console.WriteLine("Re initialization failed");
            Console.WriteLine("Hit any key to continue");
            var k0 = Console.ReadKey();
        }
    }
}

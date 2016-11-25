/*
   Copyright (C) 2015, 2016 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

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
  
    MIT license, all text above must be included in any redistribution
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MadeInTheUSB.EEPROM;

namespace MadeInTheUSB.Components
{
    public class EEPROM_25AA1024_UnitTests
    {
        public static List<byte> GetTestBuffer(int time = 1, int pageSize = 64)
        {
            var l = new List<byte>();
            for (var t = 0; t < time; t++)
            {
                for (var i = 0; i < pageSize; i++)
                    l.Add((byte) i);
            }
            return l;
        }

        public static List<byte> GetBuffer(byte value, int time = 1, int pageSize = 64)
        {
            var l = new List<byte>();
            for (var t = 0; t < time; t++)
            {
                for (var i = 0; i < pageSize; i++)
                    l.Add((byte) value);
            }
            return l;
        }

        const int REF_VALUE_2              = (128+1); // 10000001
        const int REF_VALUE_3              = (170);   // 10101010

        public static void UnitTest_ReadPage(EEPROM_25AA1024 eeprom)
        {
            Console.WriteLine("UnitTest_ReadPage Start, BufferSize:{0}", eeprom.PAGE_SIZE);
            var dataBuffer = new List<byte>();

            for (int p = 0; p < eeprom.MaxPage; p++)
            {
                dataBuffer.Clear();
                int addr = p* eeprom.PAGE_SIZE;
                var r = eeprom.ReadPageOptimized(p * eeprom.PAGE_SIZE, eeprom.PAGE_SIZE);
                if (r.Succeeded)
                {
                    int errorCount = 0;
                    for (int i = 0; i < eeprom.PAGE_SIZE; i++)
                    {
                        byte b = dataBuffer[i];
                        int expected = i;
                        if (p == 2)
                            expected = REF_VALUE_2;
                        if (p == 3)
                            expected = REF_VALUE_3;

                        if (b != expected)
                        {
                            errorCount++;
                            Console.WriteLine("Error page:{0}, baseAddress:{1}, actual:{2}, expected:{3}", p, (addr + i), b, expected);
                        }
                        else
                        {
                            if (p < 5)
                            {
                                //Console.WriteLine("Ok page:{0}, baseAddress:{1}, actual:{2}, expected:{3}", p, (addr + i), b, expected);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed ReadPages");
                }
            }
            Console.WriteLine("UnitTest_ReadPage End");
        }

        public static void PerformanceTest_ReadPage(
            EEPROM_25AA1024 eeprom,
            int pageBatchCounter = 8, // Beyond 8 performance do not improve
            Action<int, int> notifyReadPage = null
            )
        {
            var errorCount = 0;

            Console.WriteLine("PerformanceTest_ReadPage Start - pageBatchCounter:{0}, TransferBufferSize:{1}", pageBatchCounter, pageBatchCounter * eeprom.PAGE_SIZE);

            for (int p = 0; p < eeprom.MaxPage/pageBatchCounter; p++)
            {
                int addr = p * eeprom.PAGE_SIZE;

                if (notifyReadPage != null)
                    notifyReadPage(p*pageBatchCounter, eeprom.MaxPage);

                var r = eeprom.ReadPageOptimized(addr, eeprom.PAGE_SIZE * pageBatchCounter);
                if (r.Succeeded)
                {
                    for(var pbc = 0; pbc < pageBatchCounter; pbc++)
                    {
                        for (var x = 0; x < eeprom.PAGE_SIZE ; x++)
                        {
                            var b            = r.Buffer[(pbc*eeprom.PAGE_SIZE)+x];
                            var physicalPage = p + pbc;
                            var expected     = x;
                            if (physicalPage == 2)
                                expected = REF_VALUE_2;
                            if (physicalPage == 3)
                                expected = REF_VALUE_3;

                            if (b != expected)
                            {
                                errorCount++;
                                Console.WriteLine("Error page:{0}, baseAddress:{1}, actual:{2}, expected:{3}", p, (addr + pbc), b, expected);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed ReadPages");
                }
            }
            Console.WriteLine("PerformanceTest_ReadPage End");
        }
    }
}

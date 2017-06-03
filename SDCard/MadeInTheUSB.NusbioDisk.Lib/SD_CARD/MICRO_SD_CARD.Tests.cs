#define SD_RAW_SDHC
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

namespace MadeInTheUSB
{
    /// <summary>
    /// C# source code based on web site 
    /// http://www.roland-riegel.de/sd-reader/index.html
    /// and associated source code 
    /// </summary>
    public partial class MICRO_SD_CARD_TEST : MICRO_SD_CARD
    {

#if NUSBIO2
        public MICRO_SD_CARD_TEST(): base()
        {
        }
#else

        public MICRO_SD_CARD_TEST(Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin) : base(nusbio, clockPin, mosiPin, misoPin, selectPin)
        {
        }
#endif


        public bool BuildFile(string fileText, long fileSize, List<int> sectors)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("Reading file {0}", fileText);
                System.IO.File.Delete(fileText);
                var buffer = new List<byte>();

                long blockCount = (fileSize / MAX_BLOCK) + 1L;

                //var r5 = ReadBlocks(sectors[0], (Int32)blockCount, true);
                //var str0 = System.Text.Encoding.Default.GetString(r5.Buffer.Take((int)fileSize).ToArray());

                foreach (var sector in sectors)
                {
                    Console.WriteLine(string.Format("Sector:{0}", sector));
                    var r = ReadContiguousSectorsForAFile_SingleBlockMode(sector, MAX_BLOCK);
                    
                    if (r.Succeeded)
                    {
                        //var str = System.Text.Encoding.Default.GetString(r.Buffer.ToArray());
                        buffer.AddRange(r.Buffer);
                    }
                    else
                    {
                        return false;
                    }
                }
                var buffer2 = buffer.Take((int)fileSize);
                var str2 = System.Text.Encoding.Default.GetString(buffer2.ToArray());
                System.IO.File.WriteAllBytes(fileText, buffer2.ToArray());
            }
            catch (System.Exception ex)
            {
                return false;
            }
            return true;
        }


        public bool BuildFileContinousSector(string fileText, long fileSize, int sector, int sectorCount)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("Reading file {0}", fileText);
                System.IO.File.Delete(fileText);
                var buffer = new List<byte>();
                Console.WriteLine(string.Format("Sector:{0}, Count:{1}", sector, sectorCount));
                var r = ReadContiguousSectorsForAFile_SingleBlockMode(sector, MAX_BLOCK * sectorCount);
                if (r.Succeeded)
                {
                    buffer.AddRange(r.Buffer);
                }
                else
                {
                    return false;
                }
                var buffer2 = buffer.Take((int)fileSize);
                System.IO.File.WriteAllBytes(fileText, buffer2.ToArray());
            }
            catch (System.Exception ex)
            {
                return false;
            }
            finally
            {
                CS_Unselect();
            }
            return true;
        }

        public bool ExportData(string sdCardName)
        {
            try
            {
                Console.Clear();
                var sdCardSizeInByte = 16 * 1024 * 1024;
                var _512BlockCount = sdCardSizeInByte / MAX_BLOCK;

                var sectorToRead = new Dictionary<int, string>() {
                    {0, "root" },
                    {8192,"Fat 1" },
                    {8193,"Fat 2" },
                    {8194,"Fat 2" },
                    {16384 ,"Directory" },
                    {16704 ,"File000.txt s0" },
                    {16705 ,"File000.txt s1" },
                    {16706 ,"File000.txt s2" },
                    {16707 ,"File000.txt s3" },
                    {16708 ,"File000.txt s4" },
                    {16768 ,"File001.txt" },
                    {16448 ,"File002.txt" },
                    {16832 ,"File003.txt" }
                };

                
                sectorToRead.Clear();
                for (var s = 17000; s < 50000; s++)
                {
                    sectorToRead.Add(s, s.ToString());
                }

                var sw = Stopwatch.StartNew();
                int swByteCount = 0;

                string OutputFilename = string.Format(@"c:\sdcard{0}.txt", sdCardName);
                System.IO.File.Delete(OutputFilename);

                //for (var b = 0; b < _512BlockCount; b++)
                foreach (var k in sectorToRead)
                {
                    var b = k.Key;
                    var sb = new StringBuilder();
                    var s0 = string.Format("Sector:{0}, {1}", b, k.Value);
                    Console.WriteLine(s0);

                    bool mustInsertSectorNumberInDump = true;
                    bool sectorNumberInsertedInDump = false;

                    var r = ReadContiguousSectorsForAFile_SingleBlockMode(b, MAX_BLOCK);
                    if (r.Succeeded)
                    {
                        swByteCount += MAX_BLOCK;
                        for (var i = 0; i < MAX_BLOCK; i++)
                        {
                            if (r.Buffer[i] != 0)
                            {
                                sectorNumberInsertedInDump = InsertSectorInDumpIfNeeded(sb, s0, mustInsertSectorNumberInDump, sectorNumberInsertedInDump);
                                var s = string.Format("[{0:000}]{1:000}:{2} ", i, r.Buffer[i], (char)(r.Buffer[i]));
                                if (r.Buffer[i] >= 32) // char 7 bell create issue displaying
                                {
                                    Console.Write(s);
                                }
                                sb.Append(s);
                            }
                        }
                        if (sectorNumberInsertedInDump)
                        {
                            sb.AppendLine();
                        }
                        var asciiFound = false;
                        for (var i = 0; i < MAX_BLOCK; i++)
                        {
                            if ((r.Buffer[i] >= 32 && r.Buffer[i] <= 127) || (r.Buffer[i] == 13 || r.Buffer[i] == 10))
                            {
                                sectorNumberInsertedInDump = InsertSectorInDumpIfNeeded(sb, s0, mustInsertSectorNumberInDump, sectorNumberInsertedInDump);
                                asciiFound = true;
                                var s = ((char)(r.Buffer[i])).ToString();
                                sb.Append(s);
                            }
                        }
                        if (asciiFound)
                            sb.AppendLine();

                        if(sb.ToString().Length>0)
                            System.IO.File.AppendAllText(OutputFilename, sb.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Failed");
                    }
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }
            finally
            {
                CS_Unselect();
            }
            return true;
        }

        private static bool InsertSectorInDumpIfNeeded(StringBuilder sb, string s0, bool mustInsertSectorNumberInDump, bool sectorNumberInsertedInDump)
        {
            if (mustInsertSectorNumberInDump == true && sectorNumberInsertedInDump == false)
            {
                sectorNumberInsertedInDump = true;
                sb.AppendLine().Append(s0).AppendLine();
            }

            return sectorNumberInsertedInDump;
        }

        const string FILE000_EXPECTED_BASE = @"{
0FILE0000
1FILE0000
2FILE0000
3FILE0000
4FILE0000
5FILE0000
6FILE0000
7FILE0000
8FILE0000
9FILE0000
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
0FILE0000
1FILE0000
2FILE0000
3FILE0000
4FILE0000
5FILE0000
6FILE0000
7FILE0000
8FILE0000
9FILE0000
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
}
{
0FILE0000
1FILE0000
2FILE0000
3FILE0000
4FILE0000
5FILE0000
6FILE0000
7FILE0000
8FILE0000
9FILE0000
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
0FILE0000
1FILE0000
2FILE0000
3FILE0000
4FILE0000
5FILE0000
6FILE0000
7FILE0000
8FILE0000
9FILE0000
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
}
";

        const string FILE000_EXPECTED = FILE000_EXPECTED_BASE + FILE000_EXPECTED_BASE + FILE000_EXPECTED_BASE;

        public int PerformanceWritingFile0000Txt(char? charToWrite = null)
        {
            var sector = this.MaxSector / 2; // Use the the middle of the SD CARD
            try
            {
                Console.Clear();
                var strData = FILE000_EXPECTED;
                if(charToWrite.HasValue)
                {
                    strData = "".PadLeft(FILE000_EXPECTED.Length, charToWrite.Value);
                }

                var buffer           = MakeBufferMultipleOf(ASCIIEncoding.ASCII.GetBytes(strData).ToList(), MAX_BLOCK);
                var sw               = Stopwatch.StartNew();
                var r                = WriteSectors_ContiguousSector_MultiBlockMode(sector, buffer, true);
                sw.Stop();
                var totalByteWritten = buffer.Count;
                var duration         = sw.ElapsedMilliseconds / 1000.0;
                var bytePerSecond    = totalByteWritten / duration;

                Console.WriteLine(
                    "{0} K byte written, {1:0.000} M Byte written, duration:{2:0.0} S, {3:0.0} Byte/S, {4:0.0} KByte/S",
                    totalByteWritten / 1024.0,
                    totalByteWritten / 1024.0 / 1024.0,
                    duration,
                    totalByteWritten / duration,
                    totalByteWritten / 1024.0 / duration
                    );

                if (!r.Succeeded)
                    return -1;
            }
            catch (System.Exception ex)
            {
                return -1;
            }
            finally
            {
                CS_Unselect();
            }
            return sector;
        }

        public bool PerformanceReadingFile0000Txt(int fileStartSector = 16704, int fileLen = 6252)
        {
            try
            {
                int errorCount              = 0;
                int dataError               = 0;
                var sw = Stopwatch.StartNew();

                //CS_Select();

                var s0 = string.Format("Read file StartSector:{0}, Len:{1}", fileStartSector, fileLen);
                Console.WriteLine(s0);

                var r = ReadSectors_ContiguousSector_MultiBlockMode(fileStartSector, fileLen, select: true);

                long blockCount = (fileLen / MAX_BLOCK) + 1L;

                var str = System.Text.Encoding.ASCII.GetString(r.Buffer.Take(fileLen).ToArray());
                    
                if (!r.Succeeded)
                    errorCount++;
                if (str != FILE000_EXPECTED)
                {
                    Console.WriteLine("READ:{0}", str);
                    dataError++;
                }

                //CS_Unselect();

                sw.Stop();
                var duration = sw.ElapsedMilliseconds / 1000.0;
                var bytePerSecond = fileLen / duration;

                Console.WriteLine("{0} ErrorCount, {1} DataError", errorCount, dataError);
                Console.WriteLine(
                    "{0} K byte read, {1:0.000} M Byte read, duration:{2:0.0} S, {3:0.0} Byte/S, {4:0.0} KByte/S",
                    fileLen / 1024.0,
                    fileLen / 1024.0 / 1024.0, 
                    duration,
                    fileLen / duration,
                    fileLen / 1024.0 / duration
                    );
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
            finally
            {
                CS_Unselect();
            }
            return true;
        }


    }
}
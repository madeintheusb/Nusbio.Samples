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
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.EEPROM;

namespace MadeInTheUSB
{
    /// <summary>
    /// Method with dual implementation for Nusbio1 and Nusbio2
    /// </summary>
    public partial class MICRO_SD_CARD : MICRO_SD_CARD_Base
    {

#if NUSBIO2
#else
        Nusbio _nusbio;
        SPIEngine _spi;
#endif
        
        public EEPROM_BUFFER abstract_ReadPageOptimized(int addr, bool readFirstDataStartBlock,
            bool readCRC, int len, int blockCountToRead)
        {
#if NUSBIO2
            return ReadPageOptimized_nusbio2(addr, readFirstDataStartBlock, readCRC, len, blockCountToRead);
#else
            return ReadPageOptimized(addr, readFirstDataStartBlock, readCRC, len, blockCountToRead);
#endif
        }

        public SPIResult SPITransfer(List<byte> bytes, bool select = true, bool optimizeDataLine = false)
        {
#if NUSBIO2
            var r             = new SPIResult();
            var tmpReadBuffer = new byte[bytes.Count];
            var ok            = Nusbio2NAL.SPI_Helper_SingleReadWrite(bytes.ToArray(), tmpReadBuffer, bytes.Count, select:select);
            if (ok)
            {
                r.Buffer    = tmpReadBuffer.ToList();
                r.Succeeded = true;
            }
            return r;
#else
            return this._spi.Transfer(bytes, select: select, optimizeDataLine: optimizeDataLine);
#endif
        }

        public void CS_Select()
        {
#if NUSBIO2
            Nusbio2NAL.SPI_SELECT();
#else
            this._spi.Select();
#endif
        }
        public void CS_Unselect()
        {
#if NUSBIO2
            Nusbio2NAL.SPI_UNSELECT();
#else
            this._spi.Unselect();
#endif
        }

        public EEPROM_BUFFER ReadPageOptimized_nusbio2(int addr, bool readFirstDataStartBlock, bool readCRC, int len, int blockCountToRead)
        {
            var eb = new EEPROM_BUFFER();

            List<byte> ReadPageOptimized_SendDataBuffer = null;

            var dataLen = len;
            var readDataStartBlockDefaultSize = 2;
            var crcBlockDefaultSize = 2;

            if (readFirstDataStartBlock)
                len += readDataStartBlockDefaultSize * blockCountToRead;
            else
                len += readDataStartBlockDefaultSize * (blockCountToRead - 1);

            if (readCRC)
                len += crcBlockDefaultSize * blockCountToRead;

            if (ReadPageOptimized_SendDataBuffer == null || ReadPageOptimized_SendDataBuffer.Count != len)
            {
                ReadPageOptimized_SendDataBuffer = new List<byte>();
                for (var i = 0; i < len; i++)
                    ReadPageOptimized_SendDataBuffer.Add(0xFF);
            }

            try
            {
                var swReadPageOptimized_SendReadData = Stopwatch.StartNew();

                var peb2 = this.SPITransfer(ReadPageOptimized_SendDataBuffer, select: false);
                if(!peb2.Succeeded)
                    return eb;

                swReadPageOptimized_SendReadData.Stop();
                Console.WriteLine("swReadPageOptimized_SendReadData:{0}", swReadPageOptimized_SendReadData.ElapsedMilliseconds);

                if (peb2.Succeeded)
                {
                    var tmpBuf2 = new List<byte>();
                    var tmpBuf = peb2.Buffer.ToList();
                    for (var b = 0; b < blockCountToRead; b++)
                    {
                        if ((readFirstDataStartBlock && b == 0) || (b > 0))
                        {
                            if (tmpBuf[1] != DATA_START_BLOCK)
                                return eb; // Failed
                            tmpBuf = tmpBuf.Skip(readDataStartBlockDefaultSize).ToList();
                        }
                        tmpBuf2.AddRange(tmpBuf.Take(MAX_BLOCK).ToList());
                        tmpBuf = tmpBuf.Skip(MAX_BLOCK).ToList();

                        if (readCRC)
                            tmpBuf = tmpBuf.Skip(crcBlockDefaultSize).ToList();
                    }
                    eb.Buffer = tmpBuf2.ToArray();
                    eb.Succeeded = true;
                }
                else
                    return eb; // failed
            }
            finally
            {
            }
            return eb;
        }

#if !NUSBIO2

        List<byte> ReadPageOptimized_SendDataBuffer = null;
        /// <summary>
        /// Read pages using an optimized way to transfer page
        /// Transfer around 20Kb/s.
        /// 
        /// About optimizing transfer with Nusbio
        /// 
        /// There are 2 optimizations
        /// 1) - We send 64 0 from the master to the slave to force the slave to send the data
        /// Since we know it all 0 the data line need to be set only once. We therefore save 1 cycle out of 3.
        /// The cycle saved are used to request more data in the limited 2024 cycles buffer that we hace in one
        /// USB Transaction
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        /// DATA_START_BLOCK
        public EEPROM_BUFFER ReadPageOptimized(int addr, bool readFirstDataStartBlock,
            bool readCRC, int len, int blockCountToRead)
        {
            var eb = new EEPROM_BUFFER();
            var nusbio = this._spi.Nusbio;
            var spi = this._spi;

            var dataLen = len;
            var readDataStartBlockDefaultSize = 2;
            var crcBlockDefaultSize = 2;

            if (readFirstDataStartBlock)
                len += readDataStartBlockDefaultSize * blockCountToRead;
            else
                len += readDataStartBlockDefaultSize * (blockCountToRead - 1);

            if (readCRC)
                len += crcBlockDefaultSize * blockCountToRead;

            if (ReadPageOptimized_SendDataBuffer == null || ReadPageOptimized_SendDataBuffer.Count != len)
            {
                ReadPageOptimized_SendDataBuffer = new List<byte>();
                for (var i = 0; i < len; i++)
                    ReadPageOptimized_SendDataBuffer.Add(0xFF);
            }

            try
            {
                var byteBitBanged = 0;
                var spiSeq = new GpioSequence(nusbio.GetGpioMask(), nusbio.GetTransferBufferSize());
                // Convert the 3 Command Bytes + the 64 0 Bytes into a bit banging buffer
                // The 64 bytes part is optimized since all the value are 0 we just need to set
                // the data line once
                for (var bx = 0; bx < ReadPageOptimized_SendDataBuffer.Count; bx++)
                {
                    for (byte bit = (1 << 7); bit > 0; bit >>= 1) // MSB - Most significant bit first
                    {
                        spiSeq.ClockBit(nusbio[spi.ClockGpio], nusbio[spi.MosiGpio], WinUtil.BitUtil.IsSet(ReadPageOptimized_SendDataBuffer[bx], bit), compactData: true);
                    }
                    byteBitBanged++;

                    if (spiSeq.IsSpaceAvailable(8 * 2)) // If we only have left space to compute 1 byte or less
                        throw new NotImplementedException();
                }
                var swReadPageOptimized_SendReadData = Stopwatch.StartNew();
                var peb2 = EEPROM_25AAXXX_BASE.ReadPageOptimized_SendReadData(spiSeq, -1, byteBitBanged, this._spi);
                swReadPageOptimized_SendReadData.Stop();
                Console.WriteLine("swReadPageOptimized_SendReadData:{0}", swReadPageOptimized_SendReadData.ElapsedMilliseconds);

                if (peb2.Succeeded)
                {
                    var tmpBuf2 = new List<byte>();
                    var tmpBuf = peb2.Buffer.ToList();
                    for (var b = 0; b < blockCountToRead; b++)
                    {
                        if ((readFirstDataStartBlock && b == 0) || (b > 0))
                        {
                            if (tmpBuf[1] != DATA_START_BLOCK)
                                return eb; // Failed
                            tmpBuf = tmpBuf.Skip(readDataStartBlockDefaultSize).ToList();
                        }
                        tmpBuf2.AddRange(tmpBuf.Take(MAX_BLOCK).ToList());
                        tmpBuf = tmpBuf.Skip(MAX_BLOCK).ToList();

                        if (readCRC)
                            tmpBuf = tmpBuf.Skip(crcBlockDefaultSize).ToList();
                    }
                    eb.Buffer = tmpBuf2.ToArray();
                    eb.Succeeded = true;
                }
                else
                    return eb; // failed
            }
            finally
            {
            }
            return eb;
        }
#endif
    }
}
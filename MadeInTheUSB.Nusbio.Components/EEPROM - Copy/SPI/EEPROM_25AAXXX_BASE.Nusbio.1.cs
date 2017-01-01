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
using MadeInTheUSB.GPIO;
using MadeInTheUSB.spi;

namespace MadeInTheUSB.EEPROM
{
#if NUSBIO2
#else
    public partial class EEPROM_25AAXXX_BASE : EEPROM_BASE_CLASS
    {

        public List<byte> ReadAll(
            int pageBatchCounter = 16, // Beyond 16 page batch performance do not improve
            Action<int, int> notifyReadPage = null
            )
        {
            var buffer = new List<byte>();
            for (int p = 0; p < this.MaxPage / pageBatchCounter; p++)
            {
                int addr = p * this.PAGE_SIZE;

                if (notifyReadPage != null)
                    notifyReadPage(p * pageBatchCounter, this.MaxPage);

                var r = this.ReadPageOptimized(addr, this.PAGE_SIZE * pageBatchCounter);
                if (r.Succeeded)
                    buffer.AddRange(r.Buffer);
                else
                    return null;
            }
            return buffer;
        }


        private EEPROM_BUFFER ReadPageOptimized_SendReadData(GpioSequence spiSeq, int startByteToSkip, int byteToRead)
        {
            var nusbio = this._spi.Nusbio;
            var eb = new EEPROM_BUFFER();
            if (spiSeq.Send(nusbio))
            {
                var inBuffer = spiSeq.GetInputBuffer(nusbio).ToList();

                if (startByteToSkip == 0)
                {
                    inBuffer.RemoveAt(0); // Extra byte from somewhere
                }

                if (startByteToSkip > 0)
                {
                    var offSetToSkip = SPIEngine.BYTES_PER_BIT * 8 * startByteToSkip; // Skip the first 3 bytes, bitbanging the command and 16 bit address
                    inBuffer = inBuffer.GetRange(offSetToSkip, inBuffer.Count - offSetToSkip);
                }

                var buffer = __spi_buf_r(byteToRead, inBuffer);

                for (var i = 0; i < startByteToSkip; i++) // Remove the last startByteToSkip because they were not yet sent
                    buffer.RemoveAt(buffer.Count - 1);

                eb.Buffer = buffer.ToArray();
                eb.Succeeded = true;
            }
            return eb;
        }

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
        public EEPROM_BUFFER ReadPageOptimized(int addr, int len = -1)
        {
            if (len == -1)
                len = this.PAGE_SIZE;

            var eb = new EEPROM_BUFFER();
            //int byteSent           = 0;
            var nusbio = this._spi.Nusbio;
            var spi = this._spi;
            var spiBufferCmdAddr = this.GetEepromApiReadBuffer(addr);
            var spiBufferDummyData = this.GetEepromApiDataBuffer(len);
            var buffer = new List<byte>();
            buffer.AddRange(spiBufferCmdAddr);   // Command + 16 bit Address
            buffer.AddRange(spiBufferDummyData); // Dummy data to be sent which will force the EEPROM to send the right data back
            var startByteToSkip = spiBufferCmdAddr.Length;
            var finalBuffer = new List<byte>();

            this._spi.Select(); // Set select now so it is part of the bit banging sequence

            try
            {
                var byteBitBanged = 0;
                var spiSeq = new GpioSequence(nusbio.GetGpioMask(), nusbio.GetTransferBufferSize());
                // Convert the 3 Command Bytes + the 64 0 Bytes into a bit banging buffer
                // The 64 bytes part is optimized since all the value are 0 we just need to set
                // the data line once
                for (var bx = 0; bx < buffer.Count; bx++)
                {
                    for (byte bit = (1 << 7); bit > 0; bit >>= 1) // MSB - Most significant bit first
                    {
                        spiSeq.ClockBit(nusbio[spi.ClockGpio], nusbio[spi.MosiGpio], WinUtil.BitUtil.IsSet(buffer[bx], bit),
                            compactData: (bx >= spiBufferCmdAddr.Length) // For simplicity do not compact the first 3 commands byte, so we know exactly where the data start after the first 3 bytes
                            );
                    }
                    byteBitBanged++;

                    if (spiSeq.IsSpaceAvailable(8 * 2)) // If we only have left space to compute 1 byte or less
                    {
                        var peb = ReadPageOptimized_SendReadData(spiSeq, startByteToSkip, byteBitBanged);
                        if (peb.Succeeded)
                        {
                            finalBuffer.AddRange(peb.Buffer);
                            spiSeq = new GpioSequence(nusbio.GetGpioMask(), nusbio.GetTransferBufferSize());
                            startByteToSkip = 0; // We skipped it, let's forget about it
                            byteBitBanged = 0;
                        }
                        else
                            return eb; // failed
                    }
                }
                var peb2 = ReadPageOptimized_SendReadData(spiSeq, startByteToSkip, byteBitBanged);
                if (peb2.Succeeded)
                {
                    finalBuffer.AddRange(peb2.Buffer);
                    eb.Buffer = finalBuffer.ToArray();
                    eb.Succeeded = true;
                }
                else
                    return eb; // failed
            }
            finally
            {
                this._spi.Unselect();
            }
            return eb;
        }

        private List<byte> __spi_buf_r(int s, List<byte> outputBuf)
        {
            try
            {
                int j = 0;
                int pos = 0;
                List<byte> b = new List<byte>();
                var misoBit = (this._spi.Nusbio[this._spi.MisoGpio] as Gpio).Bit;
                j = 1;

                for (pos = 0; pos < s; pos++)
                {
                    Byte v = 0;
                    Byte bit;

                    if (this._spi.BitOrder == BitOrder.MSBFIRST)
                    {
                        for (bit = (1 << 7); bit > 0; bit >>= 1)
                        {
                            if (j < outputBuf.Count)
                            {
                                if (WinUtil.BitUtil.IsSet(outputBuf[j], misoBit)) //if (buf[j++] & PIN_FMISO)
                                    v |= bit;
                            }
                            j += 2;
                        }
                    }
                    else throw new NotImplementedException();
                    b.Add(v);
                }
                return b;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                if (Debugger.IsAttached)
                    Debugger.Break();
                return null;
            }
        }
    }
#endif
}
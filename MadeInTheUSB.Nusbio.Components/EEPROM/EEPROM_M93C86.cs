/*
   Copyright (C) 2015, 2016, 2017 MadeInTheUSB LLC
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

namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// m93c86 is a 2k byte spi cheap eeprom
    /// http://www.mouser.com/ds/2/389/m93c46-w-955034.pdf
    ///     CS(PD-1k)[] [] VCC
    ///     SCK      [] [] Not used
    ///     MOSI     [] [] ORG Leave unconnected for 16kbit orf
    ///     MISO     [] [] GND
    /// </summary>
    public class EEPROM_M93C86 : EEPROM_25AAXXX_BASE
    {

#if NUSBIO2
        public EEPROM_M93C86() : base(16)
        {
        }
#else
        public EEPROM_M93C86(Nusbio nusbio, 
            NusbioGpio clockPin, 
            NusbioGpio mosiPin, 
            NusbioGpio misoPin, 
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, 16, 
                debug, 
                chipSelectActiveLow:false // << important
                )
        {
            var b = this.MaxByte;
            var p = this.MaxPage;
            this.SetWriteRegisterEnable();
            //this.SetWriteRegisterDisable();
        }
#endif

    
        public override bool Is3BytesAddress
        {
            get { return false; }
        }

        public override int PAGE_SIZE
        {
            get{ return 256; }
        }

        protected override bool SetWriteRegisterEnable()
        {
            var r = this.SpiTransfer( new List<byte>(){ 0x98 /*0b10011000*/, 00 } );
            return r.Succeeded;
        }

        protected override bool SetWriteRegisterDisable()
        {
            var r = this.SpiTransfer(new List<byte>() { 0x80 /*0b10000000*/, 00 });
            return r.Succeeded;
        }
        /// <summary>
        /// http://www.mouser.com/ds/2/389/m93c46-w-955034.pdf
        /// based on the data sheet in ORG: 8 byte (low)
        /// writing one byte require 22 Clock Cycle
        /// 1 start bit, 2 bit-opCode, 11-Addr == 2 Clock cycle
        /// We send 24 bits the first 2 bits are 00
        /// I do not think that this EEPROM support write in bulk mode
        /// It does support Read in bulk
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public virtual bool WritePage(int addr, byte[] buffer)
        {
            int dt1, dt2, ans;

            for (var num = 0; num < buffer.Length; num++)
            {
                //dt1 = 0b00101000 | ((adrs & 0b0000011100000000) >> 8) ;
                //dt2 = (adrs & 0b0000000011111111) ;
                dt1           = 0x28 | ((addr & 0x700) >> 8);
                dt2           = (addr & 0xFF);
                var spiBuffer = new List<byte>() { (byte)dt1, (byte)dt2, buffer[num] };
                var writeR    = this.SpiTransfer(spiBuffer);
                var readR     = this.SpiTransfer(new List<byte>() { 0 });
                ans           = readR.Buffer[0];
                addr += 1;
            }
            return true;
        }

        /*
            Write enable 
                    10011000  
            Write Disable
                    10000000        
            Write
            //dt1 = 00101000 | ((adrs & 0000011100000000) >> 8) ;
            //dt2 = (adrs & 0000000011111111) ;
            Read
            //dt1 = 00110000 | ((adrs & 0000011100000000) >> 8) ;
            //dt2 = (adrs & 0b0000000011111111) ;            
        */

        public override EEPROM_BUFFER ReadPage(int addr, int len = -1)
        {
            if (len == -1)
                len = PAGE_SIZE;

            var eb = new EEPROM_BUFFER();
            int dt1, dt2;

            //dt1 = 0b00110000 | ((adrs & 0b0000011100000000) >> 8) ;
            //dt2 = (adrs & 0b0000000011111111) ;
            dt1 = 0x30 | ((addr & 0x700) >> 8);
            dt2 = (addr & 0xFF);

            var spiBufferWrite = new List<byte>() { (byte)dt1, (byte)dt2 };
            var spiBufferRead = GetEepromApiDataBuffer(len);
            var buffer = new List<byte>();
            buffer.AddRange(spiBufferWrite);
            buffer.AddRange(spiBufferRead);

            var r = this.SpiTransfer(buffer);

            if (r.Succeeded)
            {
                eb.Succeeded = true;
                eb.Buffer = r.Buffer.GetRange(spiBufferWrite.Count, r.Buffer.Count - spiBufferWrite.Count).ToArray();
            }
            return eb;
        }

        public bool WriteAll(int addr, List<byte> buffer, Action<int, int> notifyWritePage = null)
        {
            if (buffer.Count%this.PAGE_SIZE != 0)
                throw new ArgumentException(string.Format("Buffer length must be a multiple of {0}", this.PAGE_SIZE));

            var cAddr = addr;
            var pageToWrite = buffer.Count/this.PAGE_SIZE;
            for (var p = 0; p < pageToWrite; p++)
            {
                if(notifyWritePage != null)
                    notifyWritePage(p, pageToWrite);

                var tmpBuffer = buffer.GetRange(cAddr, this.PAGE_SIZE);
                if(!this.WritePage(cAddr, tmpBuffer.ToArray()))
                    return false;
                cAddr += this.PAGE_SIZE;
            }
            return true;
        }
    }
}
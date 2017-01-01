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
    public partial class EEPROM_25AAXXX_BASE : EEPROM_BASE_CLASS
    {
        const int WRSR   = 1; // Write Status Register Instruction 
        const int WRITE  = 2;
        const int READ   = 3;
        const int WRDI   = 4; // Write Disable
        const int RDSR   = 5; // Read Status Register Instruction 
        const int WREN   = 6; // Write Enable

        private int _waitTimeAfterWriteOperation = 5; // milli second

        //public int MaxPage
        //{
        //    get
        //    {
        //        return this.MaxByte / this.PAGE_SIZE;
        //    }
        //}

        public virtual bool Is3BytesAddress
        {
            get { return false; }
        }

        public override int PAGE_SIZE
        {
            get{ return 64; }
        }

#if NUSBIO2
        public EEPROM_25AAXXX_BASE(
            int kBit, 
            bool debug = false)
        {
            this._kBit = kBit;
        }
#else
        public EEPROM_25AAXXX_BASE(Nusbio nusbio, 
            NusbioGpio clockPin, 
            NusbioGpio mosiPin, 
            NusbioGpio misoPin, 
            NusbioGpio selectPin, 
            int kBit, 
            bool debug = false)
        {
            this._kBit = kBit;
            this._spi = new SPIEngine(nusbio, selectPin, mosiPin, misoPin, clockPin, debug: debug);
        }
#endif

        public bool Begin()
        {
            return true;
        }

        protected byte[] GetEepromApiReadBuffer(int address)
        {
            if(this.Is3BytesAddress)
                return new byte[] { READ,  (byte)(address >> 16), (byte)((address >> 8) & 0xff), (byte)(address & 0xff)};
            else
                return new byte[] { READ,  (byte)(address >> 8), (byte)address};
        }

        protected byte[] GetEepromApiWriteBuffer(int address, List<byte> data = null)
        {
            List<byte> buffer;

            if(this.Is3BytesAddress)
                buffer = new List<byte>() { WRITE,  (byte)(address >> 16), (byte)((address >> 8) & 0xff), (byte)(address & 0xff)};
            else 
                buffer = new List<byte>() { WRITE,  (byte)(address >> 8), (byte)address};

            if(data != null)
                buffer.AddRange(data);
            return buffer.ToArray();
        }

        protected byte[] GetEepromApiDataBuffer(int count)
        {
            // Buffer contains 0. Value does not matter. all we need is to send some clock to the slave to read the value
            return new byte[count]; 
        }

        protected bool SetWriteRegisterEnable()
        {
            return this.SendCommand(WREN);
        }

        protected bool SetWriteRegisterDisable()
        {
            return this.SendCommand(WRDI);
        }
        
        public override bool WritePage(int addr, byte [] buffer)
        {
            this.SetWriteRegisterEnable();

            var spiBufferWrite = GetEepromApiWriteBuffer(addr, buffer.ToList());
            //var r = this._spi.Transfer(spiBufferWrite.ToList());
            var r = this.SpiTransfer(spiBufferWrite.ToList());

            this.SetWriteRegisterDisable();

            return r.Succeeded;
        }

        public override bool WriteByte(int addr, byte value)
        {
            return WritePage(addr, (new List<byte>() {value}).ToArray());
        }

        public override int ReadByte(int addr)
        {
            var buf = ReadPage(addr, 1);
            if (buf.Succeeded)
                return buf.Buffer[0];
            else
                return -1;
        }

        public override EEPROM_BUFFER ReadPage(int addr, int len = -1)
        {
            if (len == -1)
                len = PAGE_SIZE;

            var eb             = new EEPROM_BUFFER();
            var spiBufferWrite = GetEepromApiReadBuffer(addr);
            var spiBufferRead  = GetEepromApiDataBuffer(len);
            var buffer         = new List<byte>();
            buffer.AddRange(spiBufferWrite);
            buffer.AddRange(spiBufferRead);

            var r = this.SpiTransfer(buffer);

            if (r.Succeeded)
            {
                eb.Succeeded = true;
                eb.Buffer = r.Buffer.GetRange(spiBufferWrite.Length, r.Buffer.Count - spiBufferWrite.Length).ToArray();
            }
            return eb;
        }
              
        public SPIResult SpiTransfer(List<byte> bytes, bool select = true, bool optimizeDataLine = false)
        {
#if NUSBIO2
#else
            return this._spi.Transfer(bytes, select, optimizeDataLine);
#endif
        }

        protected bool SendCommand(byte cmd)
        {
#if NUSBIO2
#else
            return this._spi.Transfer(new byte[] { cmd }.ToList()).Succeeded;
#endif
        }

    }
}
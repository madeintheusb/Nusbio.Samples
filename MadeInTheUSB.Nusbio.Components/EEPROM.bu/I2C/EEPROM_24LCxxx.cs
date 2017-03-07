#define OPTIMIZE_I2C_CALL
/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB
 
   Support of the Microship EEPROM 24LCXXX series
 
   About data transfert speed:
   ===========================
   Data transfert rate from the EEPROM to .NET is around 5.4k byte per second.
   6 seconds to transfert 32k. 
 
   Here we are talking about transfering data from the EEPROM to the memory of the .NET program
   using the I2C protocol. When using SPI protocol Nusbio can transfert out at the speed of 6.8k/sec.
   
   The reason why is that though Nusbio use the hardware acceleration of the FT232RL,
   There is only a 384 byte buffer used for communication between .NET and the chip,
   to use hardware acceleration. For every buffer that we send we get a 1 milli second
   latency mostlty due the USB communication protocol. Though the baud rate set is to
   1843200, we lose 1ms every time we send a buffer.
        See video https://www.youtube.com/watch?v=XJ48zJwrZI0
   A better solution will come with a future version of Nusbio that will use a different chip.
   
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

using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

#if NUSBIO2
using MadeInTheUSB.Nusbio2.Console;
#endif

namespace MadeInTheUSB.EEPROM
{
    public class EEPROM_24LCXXX : EEPROM_BASE_CLASS
    {
        public const int DEFAULT_I2C_ADDR = 0x50;       // Microship 24LC256 = 32k
        private int _waitTimeAfterWriteOperation = 5;   // milli second


#if !NUSBIO2
        public EEPROM_24LCXXX(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int kBit, int waitTimeAfterWriteOperation = 5, bool debug = false)
        {
            this._waitTimeAfterWriteOperation = waitTimeAfterWriteOperation;
            this._kBit = kBit;
            this._i2c = new i2c.I2CEngine(nusbio, sdaPin, sclPin, 0, debug);
        }
#else
        public EEPROM_24LCXXX(int kBit, int waitTimeAfterWriteOperation = 5, bool debug = false)
        {
            this._waitTimeAfterWriteOperation = waitTimeAfterWriteOperation;
            this._kBit = kBit;
        }
#endif

        public bool Begin(byte _addr = DEFAULT_I2C_ADDR)
        {
            if (this.DeviceId == 0)
                this.DeviceId = (byte)(_addr);

#if !NUSBIO2
            this._i2c.DeviceId = (byte)this.DeviceId;
#endif

            return true;
        }

        public override bool WritePage(int addr, byte[] buffer)
        {
#if !NUSBIO2
            var v = this._i2c.Send16BitsAddressAndBuffer(addr, buffer.Length, buffer);
            // EEPROM need a wait time after a write operation
            if (this._waitTimeAfterWriteOperation > 0)
                Thread.Sleep(this._waitTimeAfterWriteOperation);
            return v;
#else

            var outBuffer = new List<byte>() { (byte)(addr >> 8), (byte)(addr & 0xFF) };
            outBuffer.AddRange(buffer);
            var r = Nusbio2NAL.__I2C_Helper_Write(base.DeviceId, outBuffer.ToArray()) == 1;

            if (this._waitTimeAfterWriteOperation > 0)
                System.Threading.Thread.Sleep(this._waitTimeAfterWriteOperation);

            return true;
#endif
        }
        public override bool WriteByte(int addr, byte value)
        {
#if !NUSBIO2
            var v = this._i2c.Send16BitAddressAnd1Byte(addr, value);
            // EEPROM need a wait time after a write operation
            if (this._waitTimeAfterWriteOperation > 0)
                Thread.Sleep(this._waitTimeAfterWriteOperation);
            return v;
#else
            return true;
#endif
        }

        public override int ReadByte(int addr)
        {
#if !NUBSIO2
            return 0;    
#else
            return this._i2c.Send16BitsAddressAndRead1Byte((System.Int16)addr);
#endif
        }

        public override EEPROM_BUFFER ReadPage(int addr, int len = EEPROM_BASE_CLASS.DEFAULT_PAGE_SIZE)
        {
            var r = new EEPROM_BUFFER(len);
#if NUSBIO2
            var inBuffer = new List<byte>() { (byte)(addr >> 8), (byte)(addr & 0xFF) };
            r.Buffer = new byte[len]; // Must pre allocate the buffer for now
            r.Succeeded = Nusbio2NAL.__I2C_Helper_WriteRead(base.DeviceId, inBuffer.ToArray(), r.Buffer) == 1;
            return r;
#else

#if OPTIMIZE_I2C_CALL

            // This method is faster because the I2C write and read operations are
            // combined in one USB buffer
            r.Succeeded = this._i2c.Send16BitsAddressAndReadBuffer(addr, len, r.Buffer);
#else
                // Slower method because we have one USB operation for the I2C write
                // and one USB operation for the I2C read
                // The transfer of the data per say is the same
                var tmpArray = new byte[2];
                if (this._i2c.WriteBuffer(new byte[2] { (byte)(addr >> 8), (byte)(addr & 0xFF) }))
                {
                    r.Succeeded = this._i2c.ReadBuffer(len, r.Buffer);
                }
#endif
#endif
            return r;
        }
    }
}

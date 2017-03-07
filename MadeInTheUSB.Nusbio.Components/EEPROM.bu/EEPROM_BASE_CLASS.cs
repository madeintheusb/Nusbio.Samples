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

using System.Collections.Generic;
using MadeInTheUSB.i2c;
using MadeInTheUSB.spi;

namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// This is a base class used for the I2C and SPI EEPROM.
    /// All Microship EEPRROM
    /// </summary>
    public abstract class EEPROM_BASE_CLASS
    {
        /// <summary>
        /// Not all EEPROM have a page size of 64, but we chose this a the default value
        /// </summary>
        public const int DEFAULT_PAGE_SIZE = 64;

        public virtual int PAGE_SIZE
        {
            get { return DEFAULT_PAGE_SIZE; }
        }

        protected int _kBit; // 256kBit = 32k

        public int DeviceId;

#if NUSBIO2

#else
        protected I2CEngine _i2c;
        protected SPIEngine _spi;
#endif

        abstract public bool WritePage(int addr, byte [] buffer);
        abstract public bool WriteByte(int addr, byte value);
        abstract public int  ReadByte(int addr);
        abstract public EEPROM_BUFFER ReadPage(int addr, int len = EEPROM_BASE_CLASS.DEFAULT_PAGE_SIZE);

        public EEPROM_BASE_CLASS() { }

        public string Name
        {
            get
            {
                return string.Format("Microship 24LC{0}", this._kBit);
            }
        }

        public int MaxBit
        {
            get
            {
                return this._kBit * 1024;
            }
        }

        public int MaxByte
        {
            get
            {
                return MaxBit / 8;
            }
        }

        public int MaxKByte
        {
            get { return MaxByte/1024; }
        }

        public virtual int MaxPage
        {
            get
            {
                return this.MaxByte / PAGE_SIZE;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, Bit:{1}, Byte:{2}, Page:{3}", this.Name, this.MaxBit, this.MaxByte, this.MaxPage);
        }

        public static List<byte> MakeBuffer(byte b, int len)
        {
            var l = new List<byte>();
            for (var i = 0; i < len; i++)
                l.Add(b);
            return l;
        }
    }
}
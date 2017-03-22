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
    /// http://www.mouser.com/Search/ProductDetail.aspx?R=S25FL164K0XMFI011virtualkey66850000virtualkey797-25FL164K0XMFI011
    ///  Handling Cypress Flash NOR Memorty S25fl164k = 64M bit = 1M byte
    ///  https://raw.githubusercontent.com/avrxml/asf/master/common/components/memory/qspi_flash/s25fl1xx/s25fl1xx.c
    /// </summary>
    public class FLASH_S25FL1xxK : EEPROM_25AAXXX_BASE
    {
        /** Device is protected, operation cannot be carried out. */
        protected const int S25FL1XX_ERROR_PROTECTED = 1;
        /** Device is busy executing a command. */
        protected const int S25FL1XX_ERROR_BUSY = 2;
        /** There was a problem while trying to program page data. */
        protected const int S25FL1XX_ERROR_PROGRAM = 3;
        /** There was an SPI communication error. */
        protected const int S25FL1XX_ERROR_SPI = 4;

        /** Device ready/busy status bit. */
        protected const int S25FL1XX_STATUS_RDYBSY = (1 << 0);
        /** Device is ready. */
        protected const int S25FL1XX_STATUS_RDYBSY_READY = (0 << 0);
        /** Device is busy with internal operations. */
        protected const int S25FL1XX_STATUS_RDYBSY_BUSY = (1 << 0);
        /** Write enable latch status bit. */
        protected const int S25FL1XX_STATUS_WEL = (1 << 1);
        /** Device is not write enabled. */
        protected const int S25FL1XX_STATUS_WEL_DISABLED = (0 << 1);
        /** Device is write enabled. */
        protected const int S25FL1XX_STATUS_WEL_ENABLED = (1 << 1);
        /** Software protection status bit-field. */
        protected const int S25FL1XX_STATUS_SWP = (3 << 2);
        /** All sectors are software protected. */
        protected const int S25FL1XX_STATUS_SWP_PROTALL = (3 << 2);
        /** Some sectors are software protected. */
        protected const int S25FL1XX_STATUS_SWP_PROTSOME = (1 << 2);
        /** No sector is software protected. */
        protected const int S25FL1XX_STATUS_SWP_PROTNONE = (0 << 2);
        /** Write protect pin status bit. */
        protected const int S25FL1XX_STATUS_WPP = (1 << 4);
        /** Write protect signal is not asserted. */
        protected const int S25FL1XX_STATUS_WPP_NOTASSERTED = (0 << 4);
        /** Write protect signal is asserted. */
        protected const int S25FL1XX_STATUS_WPP_ASSERTED = (1 << 4);
        /** Erase/program error bit. */
        protected const int S25FL1XX_STATUS_EPE = (1 << 5);
        /** Erase or program operation was successful. */
        protected const int S25FL1XX_STATUS_EPE_SUCCESS = (0 << 5);
        /** Erase or program error detected. */
        protected const int S25FL1XX_STATUS_EPE_ERROR = (1 << 5);
        /** Sector protection registers locked bit. */
        protected const int S25FL1XX_STATUS_SPRL = (1 << 7);
        /** Sector protection registers are unlocked. */
        protected const int S25FL1XX_STATUS_SPRL_UNLOCKED = (0 << 7);
        /** Sector protection registers are locked. */
        protected const int S25FL1XX_STATUS_SPRL_LOCKED = (1 << 7);
        /** Quad enable bit */
        protected const int S25FL1XX_STATUS_QUAD_ENABLE = (1 << 1);
        /** Quad enable bit */
        protected const int S25FL1XX_STATUS_WRAP_ENABLE = (0 << 4);

        /** Latency control bits */
        protected const int S25FL1XX_STATUS_LATENCY_CTRL = (0xF << 0);
        protected const int S25FL1XX_STATUS_WRAP_BYTE = (1 << 5);
        protected const int S25FL1XX_BLOCK_PROTECT_Msk = (7 << 2);
        protected const int S25FL1XX_TOP_BTM_PROTECT_Msk = (1 << 5);
        protected const int S25FL1XX_SEC_PROTECT_Msk = (1 << 6);
        protected const int S25FL1XX_CHIP_PROTECT_Msk = (0x1F << 2);

        /** Sequential program mode command code 1. */
        protected const int S25FL1XX_SEQUENTIAL_PROGRAM_1 = 0xAD;
        /** Sequential program mode command code 2. */
        protected const int S25FL1XX_SEQUENTIAL_PROGRAM_2 = 0xAF;
        /** Protect sector command code. */
        protected const int S25FL1XX_PROTECT_SECTOR = 0x36;
        /** Unprotected sector command code. */
        protected const int S25FL1XX_UNPROTECT_SECTOR = 0x39;
        /** Read sector protection registers command code. */
        protected const int S25FL1XX_READ_SECTOR_PROT = 0x3C;
        /** Resume from deep power-down command code. */
        protected const int S25FL1XX_SOFT_RESET_ENABLE = 0x66;
        /** Resume from deep power-down command code. */
        protected const int S25FL1XX_SOFT_RESET = 0x99;
        /** Read status register command code. */
        protected const int S25FL1XX_READ_STATUS_1 = 0x05;
        /** Read status register command code. */
        protected const int S25FL1XX_READ_STATUS_2 = 0x35;
        /** Read status register command code. */
        protected const int S25FL1XX_READ_STATUS_3 = 0x33;
        /** Write enable command code. */
        protected const int S25FL1XX_WRITE_ENABLE = 0x06;
        /** Write Enable for Volatile Status Register. */
        protected const int S25FL1XX_WRITE_ENABLE_FOR_VOLATILE_STATUS = 0x50;
        /** Write disable command code. */
        protected const int S25FL1XX_WRITE_DISABLE = 0x04;
        /** Write status register command code. */
        protected const int S25FL1XX_WRITE_STATUS = 0x01;
        /** Resume from deep power-down command code. */
        protected const int S25FL1XX_WRAP_ENABLE = 0x77;
        /** Byte/page program command code. */
        protected const int S25FL1XX_BYTE_PAGE_PROGRAM = 0x02;
        /** Block erase command code (4K block). */
        protected const int S25FL1XX_BLOCK_ERASE_4K = 0x20;
        /** Block erase command code (32K block). */
        protected const int S25FL1XX_BLOCK_ERASE_32K = 0x52;
        /** Block erase command code (64K block). */
        protected const int S25FL1XX_BLOCK_ERASE_64K = 0xD8;
        /** Chip erase command code 1. */
        protected const int S25FL1XX_CHIP_ERASE_1 = 0x60;
        /** Chip erase command code 2. */
        protected const int S25FL1XX_CHIP_ERASE_2 = 0xC7;
        /** Read array (low frequency) command code. */
        protected const int S25FL1XX_READ_ARRAY_LF = 0x03;
        /** Read array command code. */
        protected const int S25FL1XX_READ_ARRAY = 0x0B;
        /** Fast Read array  command code. */
        protected const int S25FL1XX_READ_ARRAY_DUAL = 0x3B;
        /** Fast Read array  command code. */
        protected const int S25FL1XX_READ_ARRAY_QUAD = 0x6B;
        /** Fast Read array  command code. */
        protected const int S25FL1XX_READ_ARRAY_DUAL_IO = 0xBB;
        /** Fast Read array  command code. */
        protected const int S25FL1XX_READ_ARRAY_QUAD_IO = 0xEB;
        /** Deep power-down command code. */
        protected const int S25FL1XX_DEEP_PDOWN = 0xB9;
        /** Resume from deep power-down command code. */
        protected const int S25FL1XX_RES_DEEP_PDOWN = 0xAB;
        /** Manufacturer/ Device ID command code. */
        protected const int S25FL1XX_MANUFACTURER_DEVICE_ID = 0x90;
        /** Read manufacturer and device ID command code. */
        protected const int S25FL1XX_READ_JEDEC_ID = 0x9F;
        /** Continuous Read Mode Reset command code. */
        protected const int S25FL1XX_CONT_MODE_RESET = 0xFF;

        /** QSPI Flash Manufacturer JEDEC ID */
        protected const int S25FL1XX_ATMEL_SPI_FLASH = 0x1F;
        protected const int S25FL1XX_ST_SPI_FLASH = 0x20;
        protected const int S25FL1XX_WINBOND_SPI_FLASH = 0xEF;
        protected const int S25FL1XX_MACRONIX_SPI_FLASH = 0xC2;
        protected const int S25FL1XX_SST_SPI_FLASH = 0xBF;

        public enum block_size
        {
            S25FL1XX_SIZE_4K = 0,
            S25FL1XX_SIZE_8K,
            S25FL1XX_SIZE_16K,
            S25FL1XX_SIZE_32K,
            S25FL1XX_SIZE_64K,
            S25FL1XX_SIZE_128K,
            S25FL1XX_SIZE_256K,
            S25FL1XX_SIZE_512K,
            S25FL1XX_SIZE_1M,
            S25FL1XX_SIZE_2M
        };


#if NUSBIO2
        public FLASH_S25FL1xxK() : base(16)
        {
        }
#else
        public FLASH_S25FL1xxK(Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, 1024 * 64,
                debug
                )
        {
            var b = this.MaxByte;
            var p = this.MaxPage;
            base._spi.Unselect();
            this.SetWriteRegisterEnable();
            
            //this.SetWriteRegisterDisable();
        }
#endif

        public int GetRegister1() {

            var buffer = new List<byte>() { S25FL1XX_READ_STATUS_1 };
            var wr = this.SpiTransfer(buffer);
            var rr = this.SpiTransfer(new List<byte>() { 0, 0 });
            if(rr.Succeeded)
            {
                return rr.Buffer[0];
            }
            return -1;
        }


        public override int PAGE_SIZE
        {
            get { return 256; }
        }

        protected override bool SetWriteRegisterEnable()
        {
            var r = this.SpiTransfer(new List<byte>() { 0x98 /*0b10011000*/, 00 });
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
                dt1 = 0x28 | ((addr & 0x700) >> 8);
                dt2 = (addr & 0xFF);
                var spiBuffer = new List<byte>() { (byte)dt1, (byte)dt2, buffer[num] };
                var writeR = this.SpiTransfer(spiBuffer);
                var readR = this.SpiTransfer(new List<byte>() { 0 });
                ans = readR.Buffer[0];
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
            if (buffer.Count % this.PAGE_SIZE != 0)
                throw new ArgumentException(string.Format("Buffer length must be a multiple of {0}", this.PAGE_SIZE));

            var cAddr = addr;
            var pageToWrite = buffer.Count / this.PAGE_SIZE;
            for (var p = 0; p < pageToWrite; p++)
            {
                if (notifyWritePage != null)
                    notifyWritePage(p, pageToWrite);

                var tmpBuffer = buffer.GetRange(cAddr, this.PAGE_SIZE);
                if (!this.WritePage(cAddr, tmpBuffer.ToArray()))
                    return false;
                cAddr += this.PAGE_SIZE;
            }
            return true;
        }
    }
}
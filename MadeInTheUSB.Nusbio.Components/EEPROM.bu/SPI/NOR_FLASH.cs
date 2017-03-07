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

using MadeInTheUSB.spi;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.EEPROM;

namespace MadeInTheUSB.FLASH
{
    /// <summary>
    /// 
    /// The chip S25FL164K is a NOR FLASH 8 M byte of data.
    /// Sector size is 4k, though there is also in write mode a concept of page
    /// of 256 byte.
    /// Nusbio SPI in optimized mode can do 26 K byte/S with the S25FL164K api.
    /// so it will take 315 second (8*1024/26) about 5 minutes to read 8 M byte of data.
    /// 
    /// With Nusbio 2, we could acheive 4.7 M byte/S at 40Mhz.
    /// 
    /// http://www.mouser.com/ds/2/100/002-00497_S25FL116K_S25FL132K_S25FL164K_16_Mbit_2_-933056.pdf
    /// https://github.com/BleepLabs/S25FLx/blob/master/S25FLx.cpp
    /// </summary>
    public class NOR_FLASH_S25FL164K : EEPROM_25AAXXX_BASE
    {
        const int MAX_TRY = 10;

        const int NOR_FLASH_S25FL164K_kBit = 64 * 1024;  // 64 Mbit = 8 Mbyte

        //public const int WREN        = 0x06;/* Write Enable */
        //public const int WRDI        = 0x04;/* Write Disable */ 
        //public const int RDSR        = 0x05;/* Read Status Register */
        //public const int WRSR        = 0x01;/* Write Status Register */
        //public const int READ        = 0x03;/* Read Data Bytes  */
        public const int FAST_READ   = 0x0b;/* Read Data Bytes at Higher Speed //Not used as as the 328 isn't fast enough  */
        public const int PP          = 0x02;/* Page Program  */
        public const int SE          = 0x20;/* Sector Erase (4k)  */
        public const int BE          = 0x20;/* Block Erase (64k)  */
        public const int CE          = 0xc7;/* Erase entire chip  */
        public const int DP          = 0xb9;/* Deep Power-down  */
        public const int RES         = 0xab;/* Release Power-down, return Device ID */
        public const int RDID        = 0x9F; /* Read Manufacture ID, memory type ID, capacity ID */

        public const int SPIFLASH_WRITEENABLE      = 0x06; // write enable
        public const int SPIFLASH_WRITEDISABLE     = 0x04; // write disable
        public const int SPIFLASH_BLOCKERASE_4K    = 0x20; // erase one 4K block of flash memory
        public const int SPIFLASH_BLOCKERASE_32K   = 0x52; // erase one 32K block of flash memory
        public const int SPIFLASH_BLOCKERASE_64K   = 0xD8; // erase one 64K block of flash memory
        public const int SPIFLASH_CHIPERASE        = 0x60; // chip erase (may take several seconds depending on size)
        public const int SPIFLASH_STATUSREAD_REG_1 = 0x05; // read status register
        public const int SPIFLASH_STATUSREAD_REG_2 = 0x35; // read status register
        public const int SPIFLASH_STATUSREAD_REG_3 = 0x33; // read status register
        public const int SPIFLASH_STATUSWRITE      = 0x01; // write status register
        public const int SPIFLASH_ARRAYREAD        = 0x0B; // read array (fast, need to add 1 dummy byte after 3 address bytes)
        public const int SPIFLASH_ARRAYREADLOWFREQ = 0x03; // read array (low frequency)
        public const int SPIFLASH_SLEEP            = 0xB9; // deep power down
        public const int SPIFLASH_WAKE             = 0xAB; // deep power wake up
        public const int SPIFLASH_BYTEPAGEPROGRAM  = 0x02; // write (1 to 256bytes)
        public const int SPIFLASH_IDREAD           = 0x9F; // read JEDEC manufacturer and device ID (2 bytes, specific bytes for each manufacturer and device)
                                                           // Example for Atmel-Adesto 4Mbit AT25DF041A: 0x1F44 (page 27: http://www.adestotech.com/sites/default/files/datasheets/doc3668.pdf)
                                                           // Example for Winbond 4Mbit W25X40CL: 0xEF30 (page 14: http://www.winbond.com/NR/rdonlyres/6E25084C-0BFE-4B25-903D-AE10221A0929/0/W25X40CL.pdf)
        public const int SPIFLASH_MACREAD          = 0x4B; // read unique ID number (MAC)

        public List<byte> UNIQUEID = new List<uint8_t>();


        public enum Manufacturers {
            Cypress = 1
        };


#if NUSBIO2
        public NOR_FLASH_S25FL164K() : base(NOR_FLASH_S25FL164K_kBit)
        {
        }
#else
        public NOR_FLASH_S25FL164K(
            Nusbio nusbio, 
            NusbioGpio clockPin, 
            NusbioGpio mosiPin, 
            NusbioGpio misoPin, 
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, NOR_FLASH_S25FL164K_kBit, debug)
        {
            var b = this.MaxByte;
            var p = this.MaxPage;
            this._spi.Unselect();
        }
#endif

        public Manufacturers Manufacturer { get { return (Manufacturers)ManufacturerID; } }
        public int ManufacturerID;
        public int Capacity;
        public int DeviceType;
        public int FlashDeviceID;
        public int SectorSize = 4*1024;
        public int MaxSector { get { return this.MaxByte / this.SectorSize; } }

        public string GetFlashInfo()
        {
            var b = new System.Text.StringBuilder();

            b.AppendFormat("Manufacturer:{0} ({1}), FlashType:{2}, ", Manufacturer, ManufacturerID, this.FlashType);
            b.AppendFormat("MemoryType:{0} ", DeviceType).AppendLine();
            b.AppendFormat("Capacity: Code:{0}, {1} M byte ", Capacity, this.MaxKByte).AppendLine();
            b.AppendFormat("SectorSize:{0}, ", SectorSize);
            b.AppendFormat("MaxSector:{0} ", MaxSector);

            return b.ToString();
        }

        public enum CYPRESS_S25FLXXX
        {
            Undefined = 0,
            S25FL116K = 0x15,
            S25FL132K = 0x16,
            S25FL164K = 0x17, // Based on Capacity number
        }

        public CYPRESS_S25FLXXX FlashType = CYPRESS_S25FLXXX.Undefined;

        public bool ReadInfo()
        {
            var buffer = new List<byte>() { 0x9F, 0, 0, 0 };
            var r = this.SpiTransfer(buffer);

           this.ManufacturerID = r.Buffer[1];
           this.DeviceType     = r.Buffer[2];
           this.Capacity       = r.Buffer[3];
           this.FlashType      = (CYPRESS_S25FLXXX)this.Capacity;

            if (Capacity == 0)
                return false;

            WaitForOperation();

            buffer = new List<byte>() { 0x90, 0, 0, 0, 0, 0 };
            r = this.SpiTransfer(buffer);
            this.FlashDeviceID = r.Buffer[5];
            if (r.Buffer[4] != this.ManufacturerID)
                throw new ApplicationException("ManufacturerID does not match in api 0x90");

            WaitForOperation();

            var r0 = ReadStatusRegister1();
            var r1 = ReadStatusRegister2();
            var r2 = ReadStatusRegister3();

            return true;
        }

        ///// Send a command to the flash chip, pass TRUE for isWrite when its a write command
        //void command(uint8_t cmd)
        //{
        //    this._spi.Select();
        //    var r = this._spi.Transfer(new List<byte>() { cmd }, select: false);
        //}

        //bool readUniqueId()
        //{
        //    command(SPIFLASH_MACREAD);
        //    SPIResult r;
                        
        //    r = this._spi.Transfer(new List<byte>() { 0 }, select: false);
        //    r = this._spi.Transfer(new List<byte>() { 0 }, select: false);
        //    r = this._spi.Transfer(new List<byte>() { 0 }, select: false);
        //    r = this._spi.Transfer(new List<byte>() { 0 }, select: false);
        //    for (uint8_t i = 0; i < 8; i++)
        //    {
        //        var r0 = this._spi.Transfer(new List<byte>() { 0 }, select: false);
        //        if (!r0.Succeeded) return false;
        //        UNIQUEID.Add(r0.Buffer[0]);
        //    }
        //    this._spi.Unselect();
        //    return true;
        //}

        // Erase an entire 4k sector the location is in.
        // For example "erase_4k(300);" will erase everything from 0-3999. 
        //
        // All erase commands take time. No other actions can be preformed
        // while the chip is errasing except for reading the register
        public bool Erase4K(int loc)
        {
            if ((loc % (SectorSize)) != 0)
                throw new ArgumentException(string.Format("Address {0} must be a multiple of {1}", loc, this.SectorSize));
            var b = EraseSector(loc, SPIFLASH_BLOCKERASE_4K);
            this.WaitForOperation(10, "!");
            return b;
        }

        public bool Erase64k(int loc)
        {
            if ((loc % (64*1024)) != 0)
                throw new ArgumentException(string.Format("Address {0} must be a multiple of {1}", loc, this.SectorSize));
            var b = EraseSector(loc, SPIFLASH_BLOCKERASE_64K);
            this.WaitForOperation(100);
            return b;
        }

        private bool EraseSector(int loc, byte sectorSizeCommand)
        {
            WaitForOperation();
            base.SetWriteRegisterEnable();

            var buffer = new List<byte>() { sectorSizeCommand, (byte)(loc >> 16), (byte)(loc >> 8), (byte)(loc & 0xFF) };
            var r = this.SpiTransfer(buffer);

            WaitForOperation();

            base.SetWriteRegisterDisable();

            return r.Succeeded;
        }

        private bool Busy()
        {
            return (ReadStatusRegister1() & 1) == 1;
        }

        int ReadStatusRegister1()
        {
            return ReadStatusRegister(SPIFLASH_STATUSREAD_REG_1);
        }

        int ReadStatusRegister2()
        {
            return ReadStatusRegister(SPIFLASH_STATUSREAD_REG_2);
        }

        int ReadStatusRegister3()
        {
            return ReadStatusRegister(SPIFLASH_STATUSREAD_REG_3);
        }

        int ReadStatusRegister(byte reg)
        {
            var r0 = this.SpiTransfer(new List<byte>() { reg, 0 });
            uint8_t status = r0.Buffer[1];
            return status;
        }

        private void WaitForOperation(int wait = 10, string t = "~")
        {
            var tryCounter = 0;
            Thread.Sleep(wait/10);
            while (true)
            {
                if (!this.Busy()) return;
                Thread.Sleep(wait);
                if (tryCounter++ >= MAX_TRY)
                    throw new ApplicationException("Waiting for operation timeout");
                //Console.Write(t);
            }
        }

        public override bool Is3BytesAddress
        {
            get { return true; }
        }

        public override int PAGE_SIZE
        {
            get{ return 256; }
        }

        public EEPROM_BUFFER ReadPageOptimized(int addr, int len = -1)
        {
            if (len == -1)
                len = this.PAGE_SIZE;

            var eb                 = new EEPROM_BUFFER();
            var nusbio             = this._spi.Nusbio;
            var spi                = this._spi;
            var spiBufferCmdAddr   = this.GetEepromApiReadBuffer(addr);
            var spiBufferDummyData = this.GetEepromApiDataBuffer(len);
            var buffer             = new List<byte>();

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
                        var peb = ReadPageOptimized_SendReadData(spiSeq, startByteToSkip, byteBitBanged, this._spi);
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
                var peb2 = ReadPageOptimized_SendReadData(spiSeq, startByteToSkip, byteBitBanged, this._spi);
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

        public EEPROM_BUFFER ReadSector(int sector4kStart, int len, bool optimize = false)
        {
            sector4kStart = sector4kStart * this.SectorSize;

#if !NUSBIO2 // With Nusbio2 with high speed SPI by default
            if (optimize) 
                return ReadPageOptimized(sector4kStart, len);
#endif

            var rb        = new EEPROM_BUFFER();
            var tmpBuffer = new List<byte>() { READ, (byte)(sector4kStart >> 16), (byte)(sector4kStart >> 8), (byte)(sector4kStart & 0xFF) };
            var buffer    = base.GetEepromApiDataBuffer((int)len);
            tmpBuffer.AddRange(buffer);
            var r = this.SpiTransfer(tmpBuffer);

            if (!r.Succeeded)
                return rb;

            rb.Buffer = r.Buffer.Skip(4).ToArray();
            rb.Succeeded = r.Succeeded;

            return rb;
        }

        //public EEPROM_BUFFER ReadSector_NotOptimized(int sector4kStart, int len)
        //{
        //    sector4kStart = sector4kStart * this.SectorSize;
        //    var rb        = new EEPROM_BUFFER();
        //    var tmpBuffer = new List<byte>() { READ, (byte)(sector4kStart >> 16), (byte)(sector4kStart >> 8), (byte)(sector4kStart & 0xFF) };
        //    var buffer    = base.GetEepromApiDataBuffer((int)len);
        //    tmpBuffer.AddRange(buffer);
        //    var r         = this._spi.Transfer(tmpBuffer);

        //    if (!r.Succeeded)
        //        return rb;

        //    rb.Buffer = r.Buffer.Skip(4).ToArray();
        //    rb.Succeeded = r.Succeeded;

        //    return rb;
        //}

        public bool Write4kSector(int sector4kStart, List<byte> buffer, bool erase = true)
        {
            sector4kStart = sector4kStart * this.SectorSize;
            if (erase)
                this.Erase4K(sector4kStart);

            var addr    = sector4kStart;
            int written = 0;

            for (var b = 0; b < buffer.Count; b += this.PAGE_SIZE)
            {
                base.SetWriteRegisterEnable();

                var tmpBuffer = new List<byte>() { PP, (byte)(addr >> 16), (byte)(addr >> 8), (byte)(addr & 0xFF) };
                tmpBuffer.AddRange(buffer.Skip(written).Take(this.PAGE_SIZE));
                var r   = this.SpiTransfer(tmpBuffer);

                if (!r.Succeeded)
                    return false;

                written += this.PAGE_SIZE;
                addr    += this.PAGE_SIZE;
            }
            this.WaitForOperation();
            //base.SetWriteRegisterEnable();
            return true;
        }

        private SPIResult SpiTransfer(List<byte> buffer)
        {
#if NUSBIO2
#else
            return this._spi.Transfer(buffer);
#endif
        }
    }
}
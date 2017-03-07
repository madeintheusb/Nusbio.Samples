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

    SD CARD REFERENCES
    https://www.sdcard.org/downloads/pls/archive/index.html
    https://www.sd-3c.com/
    http://elm-chan.org/docs/mmc/mmc_e.html

     FatFs - Generic FAT File System Module
     http://elm-chan.org/fsw/ff/00index_e.html
     https://luckyresistor.me/cat-protector/software/sdcard-3/

 
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

//using abs = System.Math

namespace MadeInTheUSB
{
    /// <summary>
    /// https://www.sdcard.org/developers/index.html
    /// C# source code based on web site 
    /// http://www.roland-riegel.de/sd-reader/index.html
    /// and associated source code 
    /// Possible better version https://github.com/greiman/SdFat-beta/blob/master/SdFat/src/SdCard/SdSpiCard.cpp
    /// </summary>
    public partial class MICRO_SD_CARD : MICRO_SD_CARD_Base
    {
        public CID_INFO CIDInfo      = new CID_INFO();
        public CSD_INFO_V2 CSDInfo   = new CSD_INFO_V2();
        public SpeedClass SpeedClass = SpeedClass.Undefined;
        public OCR_Register OCRRegister;
        public Int64 FullSizeInByte = 0;

        private R7 IF_COND_CMD8_R7;
        
        public  CID_INFO.SD_CARD_MANUFACTURER Manufacturer
        {
            get
            {
                return this.CIDInfo.GetManufacturer();
            }
        }

        public Int32 SectorSize
        {
            get
            {
                if (CSDInfo == null)
                    return -1;
                return CSDInfo.MaxReadSectorSize;
            }
        }

        public Int32 MaxSector
        {
            get
            {
                return (Int32)(this.FullSizeInByte / this.SectorSize);
            }
        }

        public Int64 FullSizeInMB
        {
            get
            {
                return FullSizeInByte / 1024 / 1024;
            }
        }

        public Int64 FullSizeInGB
        {
            get
            {
                return FullSizeInByte / 1024 / 1024 / 1024;
            }
        }

        [Flags]
        public enum SdCardSpecType
        {
            Spec1   = 1 << SD_RAW_SPEC_1,
            Spec2   = 1 << SD_RAW_SPEC_2,
            SpecHC  = 1 << SD_RAW_SPEC_SDHC,
            Unknown = 1024
        }

        public SdCardSpecType SdCardType = MICRO_SD_CARD.SdCardSpecType.Spec1;

        //public int sd_raw_card_type = 0;


#if NUSBIO2
        public MICRO_SD_CARD()
        {
            CS_Unselect();
            this.SdCardType = SdCardSpecType.Unknown;
        }
#else
    public MICRO_SD_CARD(Nusbio nusbio,
            NusbioGpio clockPin,
            NusbioGpio mosiPin,
            NusbioGpio misoPin,
            NusbioGpio selectPin)
        {
            this._spi = new SPIEngine(nusbio, selectPin, mosiPin, misoPin, clockPin);
            this._nusbio = nusbio;
            CS_Unselect();
            this.SdCardType = SdCardSpecType.Unknown;
        }
#endif

        private bool initCard_CheckIfSdCardHC()
        {
            if (this.IsSpec2Card)
            {
                var r = cardCommand(CMD_READ_OCR_58, 0, 5);
                var ocrRegister = new OCR_Register(r.Buffer);
                if (!ocrRegister.Validate())
                    return false;
                if (!ocrRegister.Extract())
                    return false;

                if ((r.Buffer[1] & 0x40) == 0x40) // Also (r1.Buffer[0] & 0xC0) == 0xC0)
                {
                    //sd_raw_card_type |= (1 << SD_RAW_SPEC_SDHC);
                    SdCardType |= (SdCardSpecType)((1 << SD_RAW_SPEC_SDHC));
                }
                MICRO_SD_CARD.InternalAssert((int)(SdCardSpecType.Spec2|SdCardSpecType.SpecHC), (int)SdCardType);
            }
            return true;
        }

        private bool initCard_SetBlockSize()
        {
            int size = MAX_BLOCK;
            var r = cardCommand(CMD_SET_BLOCKLEN_16, size, 1);
            return IsR1Flag(r.Buffer[0], R1.READY_STATE);
        }

        private bool initCard_GO_IDLE_STATE()
        {
            var tryIndex = 0;
            while (true)
            {
                var r = cardCommand(CMD_GO_IDLE_STATE_CMD0, 0, 1);
                if (!r.Succeeded) return false;
                if (r.Buffer[0] == (int)R1.IDLE_STATE)
                    break;

                R1 r1 = (R1)r.Buffer[0];
                Debug.WriteLine("Go Idle State R1:{0}", r1);

                if (tryIndex++ >= MAX_TRY)
                    return false;
            }
            return true;
        }

        private bool initCard_GetCardType_IF_COND_CMD8()
        {
            const byte Cmd8_ExpectedPattern = 0xAA;
            var tryIndex                    = 0;
            var done                        = false;

            while (!done)
            {
                for (var z = 0; z < 2; z++)
                {
                    var r = cardCommand(CMD_SEND_IF_COND_CMD8, 0x100 /* 2.7V - 3.6V */ | Cmd8_ExpectedPattern, 5);
                    if (!r.Succeeded && (z == 0 || z == 1))
                        continue;

                    if (tryIndex++ >= MAX_TRY)
                        return false;

                    if (IsR1Flag(r.Buffer[0], R1.ILLEGAL_COMMAND))
                    {
                        //sd_raw_card_type |= (1 << SD_RAW_SPEC_1);
                        SdCardType = (SdCardSpecType)((1 << SD_RAW_SPEC_1));
                        done = true;
                        break;
                    }
                    else
                    {
                        //this.SdCardType = SdCardSpecType.Spec2;
                        //sd_raw_card_type |= (1 << SD_RAW_SPEC_2);
                        this.SdCardType = (SdCardSpecType)(1 << SD_RAW_SPEC_2);

                        this.IF_COND_CMD8_R7 = new R7(r.Buffer, Cmd8_ExpectedPattern);
                        if (!this.IF_COND_CMD8_R7.Validate())
                            return false;
                        if (this.IF_COND_CMD8_R7.Extract())
                        {
                            done = true;
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private bool initCard_SEND_OP_COND__ACMD41()
        {
            int tryCount = 0;
            var arg      = 0X40000000;
            while (true)
            {
                if (tryCount++ >= MAX_TRY)
                    return false;
                var r = sendCard_ACMD(CMD_SD_SEND_OP_COND_CMD41, arg, expectedState: R1.READY_STATE); // CMD55 + ACMD41
                if (!r.Succeeded)
                    return false;
                if (IsR1Flag(r.Buffer[0], R1.READY_STATE))
                {
                    var r3 = new R3(r.Buffer);
                    if (r3.Validate())
                        return true;
                    else
                        return false;
                }
                Thread.Sleep(1);
            }            
        }

        public SD_STATUS ReadCardStatus()
        {
            var done = false;
            while (!done)
            {
                var r = sendCard_ACMD(CMD_SEND_STATUS, 0, 1, expectedState: R1.READY_STATE);
                if (!r.Succeeded) return null;
                if (IsR1Flag(r.Buffer[0], R1.READY_STATE))
                {
                    done = true;
                }
                var rr = WaitForDataStartBlockAndReadBuffer(64);
                var sdStatus = new SD_STATUS(rr.Buffer);
                if (!sdStatus.Extract())
                    return null;
                return sdStatus;

            }
            return null;
        }

        public static bool MultiCall(Func<bool> f)
        {
            int count = 0;
            while (count < MAX_TRY)
            {
                var b = f();
                if (b)
                    return true;
                count += 1;
            }
            return false;
        }
        
        public bool Initialize()
        {
            try
            {
                if (!MultiCall(ActivateSPIMode)) return false;

                CS_Select();

                if (!MultiCall(initCard_GO_IDLE_STATE)) return false;
                if (!MultiCall(initCard_GetCardType_IF_COND_CMD8)) return false;
                if (!MultiCall(initCard_SEND_OP_COND__ACMD41)) return false;
                if (!MultiCall(initCard_CheckIfSdCardHC)) return false;
                if (!MultiCall(initCard_SetBlockSize)) return false;

                CleanPendingData();
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

        SPIResult sendCard_ACMD(uint8_t cmd, int arg, int expectedLed = 1, R1 expectedState = R1.IDLE_STATE)
        {
            var tryCounter = 0;
            while ((tryCounter++) < MAX_TRY)
            {
                var r = cardCommand(CMD_APP_55, 0, 1);
                if (!r.Succeeded)
                    return r;
                var r1 = R1ToString(r.Buffer[0]);

                // In the case of the ACMD41 the state will change from idle to ready
                // so here it will be idle, for the next cardCommand() call, the state
                // will become ready.
                // For ACMD CMD_SEND_STATUS CMD13, the current state is aready ready
                // So we expect the call to CMD55 to return ready
                if (!(IsR1Flag(r.Buffer[0], R1.IDLE_STATE)|| IsR1Flag(r.Buffer[0], R1.READY_STATE)))
                {
                    r.Succeeded = false;
                    return r;
                }
                var rr = cardCommand(cmd, arg, expectedLed);
                if (!rr.Succeeded)
                    return rr;

                if (IsR1Flag(r.Buffer[0], R1.READY_STATE))
                    return rr;

                Thread.Sleep(1);
            }
            return new SPIResult();// Failed
        }

        private bool IsHCCard
        {
            get
            {
                return (this.SdCardType & SdCardSpecType.SpecHC) == SdCardSpecType.SpecHC;
            }
        }
        private bool IsSpec1Card
        {
            get
            {
                return (this.SdCardType & SdCardSpecType.Spec1) == SdCardSpecType.Spec1;
            }
        }
        private bool IsSpec2Card
        {
            get
            {
                return (this.SdCardType & SdCardSpecType.Spec2) == SdCardSpecType.Spec2;
            }
        }

        protected SPIResult WriteBuffer_SingleBlock(int sectorAddr, List<byte> buffer, bool select = true)
        {
            try
            {
                int offset  = 0;
                int written = 0;
                var r       = new SPIResult();

                int wait = 0;

                if (select)
                    CS_Select();

                while (written < buffer.Count)
                {
                    Console.WriteLine("Writing Sector:{0}", sectorAddr);

                    var rr = cardCommand(CMD_WRITE_SINGLE_BLOCK_CMD24, sectorAddr, 1, select: false);
                    if (!rr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                        return r.Failed();

                    var crc       = 0XFFFF;
                    var bufToSend = new List<byte>();

                    var sendDataR = sd_raw_send_byte((byte)DATA_START_BLOCK);
                    if (!sendDataR.Succeeded)
                        return sendDataR;

                    for (var x = 0; x < MAX_BLOCK; x++) 
                        bufToSend.Add(buffer[offset + x]);

                    r = SPITransfer(bufToSend, select: false, optimizeDataLine: false);
                    if (!r.Succeeded)
                        return r;

                    sendDataR  = sd_raw_send_byte((byte)(crc >> 8));
                    sendDataR  = sd_raw_send_byte((byte)(crc & 0XFF));
                    var status = sd_raw_send_byte(0xff);
                    if ((status.Val0 & DATA_RES_MASK) != DATA_RES_ACCEPTED)
                    {
                        return status.Failed();
                    }
                    this.GetRidOfCrc();

                    written    += MAX_BLOCK;
                    offset     += MAX_BLOCK;
                    sectorAddr += 1;

                    if (!WaitUntilBusy()) 
                        return r.Failed();

                    //if (select) CS_Unselect();
                }
                r.Succeeded = true;
                return r;
            }
            finally
            {
                if (select) CS_Unselect();
            }
        }

        /// <summary>
        /// https://www.cl.cam.ac.uk/teaching/1112/P31/code/diskio.c
        /// </summary>
        /// <param name="sectorAddr"></param>
        /// <param name="buffer"></param>
        /// <param name="select"></param>
        /// <returns></returns>
        protected SPIResult WriteSectors_ContiguousSector_MultiBlockMode(int sectorAddr, List<byte> buffer, bool select = true)
        {
            try
            {
                int offset      = 0;
                int written     = 0;
                var crc         = 0XFFFF;
                var r           = new SPIResult();
                int wait        = 0;
                var sectorCount = buffer.Count / MAX_BLOCK;

                if(sectorCount* MAX_BLOCK != buffer.Count)
                    throw new ArgumentException(string.Format("Buffer length {0} must be a multiple of {1}", buffer.Count, MAX_BLOCK));

                Console.WriteLine("Writing Sector:{0}", sectorAddr);

                if (select)
                    CS_Select();

                var er = cardCommand(SD_CARD_ERROR_ACMD23, sectorCount, 1, select: false); // send pre-erase count - Sd2Card.cpp
                if (!er.Succeeded)
                    return r.Failed();

                //WaitUntilBusy();

                var rr = cardCommand(CMD_WRITE_MULTIPLE_BLOCK_CMD25, sectorAddr, 1, select: false);
                if (!rr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                    return r.Failed();

                while (written < buffer.Count)
                {
                    var bufToSend = new List<byte>();
                    bufToSend.Add((byte)WRITE_MULTIPLE_TOKEN);
                    for (var x = 0; x < MAX_BLOCK; x++)
                        bufToSend.Add(buffer[offset + x]);

                    bufToSend.Add((byte)(crc >> 8));
                    bufToSend.Add((byte)(crc & 0XFF));
                    bufToSend.Add(0xff); // Read response
                    bufToSend.Add(0xff); // Get rid of crc answer 0
                    bufToSend.Add(0xff); // Get rid of crc answer 0

                    r = SPITransfer(bufToSend, select: false, optimizeDataLine: false);
                    if (!r.Succeeded)
                        return r;

                    var status = r.Buffer[r.Buffer.Count - 2/*crc answer*/ - 1];
                    if ((status & DATA_RES_MASK) != DATA_RES_ACCEPTED)
                    {
                        return r.Failed();
                    }

                    written += MAX_BLOCK;
                    offset  += MAX_BLOCK;

                    if (!WaitUntilBusy())
                        return r.Failed();
                }
                var stopTranR = sd_raw_send_byte(STOP_TRAN_TOKEN);

                if (!WaitUntilBusy())
                    return r.Failed();

                //var rrr = cardCommand(CMD_STOP_TRANSMISSION_CMD12, sectorAddr, 1, select: true);
                //if (!rrr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                //    return r.Failed();

                r.Succeeded = true;
                return r;
            }
            finally
            {
                if (select) CS_Unselect();
            }
        }

        protected SPIResult ReadContiguousSectorsForAFile_SingleBlockMode(int sectorAddr, int length, bool select = true)
        {
            try
            {
                var readableLen = MAX_BLOCK; // We can only transfer 512 byte in one SPI operation
                var r = new SPIResult();

                while (length > 0)
                {
                    if (select)
                        CS_Select();

                    var rr = cardCommand(CMD_READ_SINGLE_BLOCK_CMD17, sectorAddr, 1, select: false);
                    if (!rr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                        return r.Failed();

                    if (!GetPendingData(DATA_START_BLOCK))
                        return rr.Failed();

                    var eBuf   = abstract_ReadPageOptimized(-1, false, false, MAX_BLOCK, 1);
                    var tmpBuf = eBuf.Buffer.Take(eBuf.Buffer.Length);
                    r.Buffer.AddRange(tmpBuf);

                    var crc = 0xFFFF;
                    var sendDataR = sd_raw_send_byte((byte)(crc >> 8));
                    sendDataR     = sd_raw_send_byte((byte)(crc & 0XFF));

                    length -= readableLen;
                    sectorAddr += 1;

                    if (select)
                        CS_Unselect();
                }
                r.Succeeded = length == 0;
                return r;
            }
            finally
            {
                if (select) CS_Unselect();
            }
        }

        protected SPIResult ReadSectors_ContiguousSector_MultiBlockMode(int sectorAddr, int len, bool select = true)
        {
            var sectorToRead = (len / MAX_BLOCK) + 1;
            var lenInSector  = sectorToRead * MAX_BLOCK;
            try
            {
                if (select) CS_Select();
                var r  = new SPIResult();
                var rr = cardCommand(CMD_READ_MULTIPLE_BLOCK_CMD18, sectorAddr, 1, select: false);
                if (!rr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                    return r.Failed();

                var blockReadCount              = 0;
                var blockCountToReadInOneUsbOp  = 1;
                var blockLenToReadInOneUsbOp    = 0;

                while (lenInSector > 0)
                {
                    // TODO: Change this for NUSBIO2
                    // Try to read 7,6,5,4,3,2,1,1 512 byte block in the same usb op if we can
                    // This must fit in 57k byte of bit banging data for Nusbio1
                    if (lenInSector >= MAX_BLOCK * 7)
                        blockCountToReadInOneUsbOp = 7;
                    else if (lenInSector >= MAX_BLOCK * 6)
                        blockCountToReadInOneUsbOp = 6;
                    else if (lenInSector >= MAX_BLOCK * 5)
                        blockCountToReadInOneUsbOp = 5;
                    else if (lenInSector >= MAX_BLOCK * 4)
                        blockCountToReadInOneUsbOp = 4;
                    else if (lenInSector >= MAX_BLOCK * 3)
                        blockCountToReadInOneUsbOp = 3;
                    else if (lenInSector >= MAX_BLOCK * 2)
                        blockCountToReadInOneUsbOp = 2;
                    else
                        blockCountToReadInOneUsbOp = 1;

                    blockLenToReadInOneUsbOp = MAX_BLOCK * blockCountToReadInOneUsbOp;

                    EEPROM_BUFFER eBuf = null;

                    if (blockReadCount == 0)
                    {
                        if (!GetPendingData(DATA_START_BLOCK))
                            return rr.Failed();
                        eBuf = abstract_ReadPageOptimized(-1, readFirstDataStartBlock:false, readCRC: true, len: blockLenToReadInOneUsbOp, blockCountToRead:blockCountToReadInOneUsbOp);
                    }
                    else
                    {
                        eBuf = abstract_ReadPageOptimized(-1, readFirstDataStartBlock:true, readCRC: true, len: blockLenToReadInOneUsbOp, blockCountToRead: blockCountToReadInOneUsbOp);
                    }

                    r.Buffer.AddRange(eBuf.Buffer);

                    lenInSector     -= blockLenToReadInOneUsbOp;
                    blockReadCount  += blockCountToReadInOneUsbOp;
                }

                r.Succeeded = lenInSector == 0;

                var rrr = cardCommand(CMD_STOP_TRANSMISSION_CMD12, sectorAddr, 1, select: true);
                if (!rrr.Succeeded || !IsR1Flag(rr.Val0, R1.READY_STATE))
                    return r.Failed();

                this.CleanPendingData();

                if(r.Succeeded)
                {
                    r.Buffer = r.Buffer.Take(len).ToList(); // Remove the multiple of sector unit
                }

                return r;
            }
            finally
            {
                if (select) CS_Unselect();
            }
        }

        internal bool sdCardGoIdleState()
        {
            var sdCardFoundInIdleState = false;
            for (int i = 0; i < MAX_TRY; ++i)
            {
                var val = sd_raw_send_command_val(CMD_GO_IDLE_STATE_CMD0, 0);
                if (val == R1_IDLE_STATE)
                {
                    sdCardFoundInIdleState = true;
                    break;
                }
                if (i == 0x1ff)
                {
                    return false;
                }
                Thread.Sleep(i + 1);
            }
            if (!sdCardFoundInIdleState)
                return false;
            return true;
        }

        //public bool __DetermineIfSdCardSpec1Or2()
        //{
        //    //sd_raw_card_type = 0;

        //    // http://electronics.stackexchange.com/questions/77417/what-is-the-correct-command-sequence-for-microsd-card-initialization-in-spi

        //    SPIResult response = null;
        //    response = sd_raw_send_command(
        //        CMD_SEND_IF_COND_CMD8,
        //        0x100 /* 2.7V - 3.6V */ |
        //        0xaa /* test pattern */
        //    );

        //    // Assume Spec 1 first
        //    //sd_raw_card_type |= (1 << SD_RAW_SPEC_1);
        //    SdCardType = (SdCardSpecType)((1 << SD_RAW_SPEC_1));

        //    // If bit is not set then it is not a spec 1
        //    if ((response.Succeeded) && ((response.Buffer[0] & (1 << R1_ILL_COMMAND_toShift)) == 0))
        //    {
        //        sd_raw_rec_byte();
        //        sd_raw_rec_byte();
        //        var r = sd_raw_rec_byte();
        //        if (!r.Succeeded) return false;
        //        if ((r.Buffer[0] & 0x01) == 0)
        //            return false; /* card operation voltage range doesn't match */
        //        var r1 = sd_raw_rec_byte();
        //        if (!r.Succeeded)
        //            return false;
        //        if (r1.Buffer[0] != 0xaa)
        //        {
        //            ///_errorCode = SD_CARD_ERROR_CMD8;
        //            return false; /* wrong test pattern */
        //        }

        //        /* card conforms to SD 2 card specification */
        //        //sd_raw_card_type |= (1 << SD_RAW_SPEC_2);
        //        SdCardType = (SdCardSpecType)((1 << SD_RAW_SPEC_2));
        //    }
        //    return true;
        //}

        //private bool _WaitForSDCardToBeReady_CMD55_CMD41()
        //{
        //    var ready = false;
        //    SPIResult response = null;
        //    for (uint16_t i = 0; i < 10; i++) /* wait for card to get ready */
        //    {
        //        //var mask = (1 << SD_RAW_SPEC_1) | (1 << SD_RAW_SPEC_2);
        //        //var v = sd_raw_card_type & (mask);
        //        //if ((v & mask) > 0) // SPEC1 OR SPEC 2
        //        if(this.SdCardType == SdCardSpecType.Spec1 || this.SdCardType == SdCardSpecType.Spec2)
        //        {
        //            int arg = 0;
        //            //mask = (1 << SD_RAW_SPEC_2);
        //            //v = sd_raw_card_type & (mask);
        //            //if ((v & mask) == mask)
        //            if (this.IsSpec2Card)
        //                arg = 0x40000000;
        //            sd_raw_send_command(CMD_APP_55, 0);

        //            // CMD41
        //            response = sd_raw_send_command(CMD_SD_SEND_OP_COND_CMD41, arg); // _errorCode = SD_CARD_ERROR_ACMD41;
        //        }
        //        else
        //        {
        //            response = sd_raw_send_command(CMD_SEND_OP_COND, 0);
        //        }

        //        if ((response.Succeeded) && ((response.Buffer[0] & (1 << R1_IDLE_STATE)) == 0))
        //        {
        //            ready = true;
        //            break;
        //        }

        //        if (i == 0x7fff)
        //        {
        //            CS_Unselect();
        //            return false;
        //        }
        //    }
        //    return ready;
        //}

        private bool ActivateSPIMode()
        {
            CS_Unselect();
            Thread.Sleep(10);
            for (var i = 0; i < 10; i++)
            {
                sd_raw_rec_byte(0xFF); // Here CS is un selected or HIGH
                Thread.Sleep(10);
            }
            Thread.Sleep(10);
            CS_Select();
            Thread.Sleep(10);
            return true;
        }

        //bool sd_raw_send_commandEx(int command, int arg, int expectedVal = 0, bool waitFor0xFE = true)
        //{
        //    var r = sd_raw_send_command(command, arg);

        //    if (!r.Succeeded)
        //        return false;

        //    if (r.Buffer[0] == expectedVal)
        //    {
        //        if (waitFor0xFE)
        //            return WaitForDataStartBlock();
        //        else
        //            return true;
        //    }
        //    else return false;
        //}

        int sd_raw_send_command_val(int command, int arg, bool waitFor0xFE = false)
        {
            var r = sd_raw_send_command(command, arg);
            if (!r.Succeeded) return -1;
            int val = r.Buffer[0];
            if (waitFor0xFE)
                if (WaitForDataStartBlock())
                    return val;
                else
                    return -2;
            else
                return val;
        }
        
        SPIResult cardCommand(int command, int arg, int expectedLen, bool select = true)
        {
            var response = new SPIResult();
            try
            {
                if (select)
                {
                    CS_Select();
                    Thread.Sleep(1); // wait up to 300 ms if busy -- https://github.com/adafruit/SD/blob/master/utility/Sd2Card.cpp
                }

                var buffer = new List<byte>()
                {
                    (byte)(0x40 | command),
                    (byte)((arg >> 24) & 0xff),
                    (byte)((arg >> 16) & 0xff),
                    (byte)((arg >> 8) & 0xff),
                    (byte)((arg >> 0) & 0xff),
                    (byte)0,
                };

                switch (command)
                {
                    case CMD_GO_IDLE_STATE_CMD0:
                        buffer[buffer.Count - 1] = 0x95;
                        break;
                    case CMD_SEND_IF_COND_CMD8:
                        buffer[buffer.Count - 1] = 0x87;
                        break;
                    case CMD_SEND_STATUS:
                        buffer[buffer.Count - 1] = 0x95;
                        break;
                    default:
                        buffer[buffer.Count - 1] = 0xff;
                        break;
                }
                response = SPITransfer(buffer, select: false);
                if (!response.Succeeded)
                    return response;

                if(command == CMD_STOP_TRANSMISSION_CMD12) /* Skip a stuff byte when stop reading */
                {
                    sd_raw_rec_byte(0xFF);
                }
                
                SPIResult r0 = null;
                var ok = false;
                var i = 0;

                while ((i++) < 255)
                {
                    r0 = sd_raw_rec_byte(0xFF); // Here CS is un selected or HIGH
                    if (!r0.Succeeded)
                        return new SPIResult();
                    if ((r0.Buffer[0] & 0x80) != 0x80)
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                    return new SPIResult();

                if (expectedLen == 1)
                {
                    return new SPIResult() { Succeeded = true, Buffer = new List<uint8_t>() { r0.Buffer[0] } };
                }

                var buffer2 = new List<byte>() { r0.Buffer[0] }; // Prepare buffer to read expectedLen buffer
                for (var e = 0; e < expectedLen - 1; e++)
                {
                    var r1 = sd_raw_rec_byte(0xFF);
                    if (!r1.Succeeded)
                        return new SPIResult();
                    buffer2.Add(r1.Buffer[0]);
                }

                var rrr = new SPIResult() { Succeeded = true, Buffer = buffer2 };
                return rrr;
            }
            finally
            {
                if (select)
                    CS_Unselect();
            }
        }

        SPIResult sd_raw_send_command(int command, int arg)
        {
            try
            {                
                var response = new SPIResult();
                var buffer = new List<byte>()
                {
                    (byte)(0x40 | command),
                    (byte)((arg >> 24) & 0xff),
                    (byte)((arg >> 16) & 0xff),
                    (byte)((arg >> 8) & 0xff),
                    (byte)((arg >> 0) & 0xff),
                    (byte)0, // CRC
                };

                switch (command)
                {
                    case CMD_GO_IDLE_STATE_CMD0:
                        buffer[buffer.Count - 1] = 0x95;
                        break;
                    case CMD_SEND_IF_COND_CMD8:
                        buffer[buffer.Count - 1] = 0x87;
                        break;
                    default:
                        buffer[buffer.Count - 1] = 0xff;
                        break;
                }

                buffer.Add(0xFF);
                buffer.Add(0xFF);

                response = SPITransfer(buffer, select: false);
                if (!response.Succeeded)
                {
                    return response;
                }

                var len = response.Buffer.Count;
                if (response.Buffer[len - 2] == 255 && response.Buffer[len - 1] != 255)
                {
                    var response2 = new SPIResult();
                    response2.Buffer = new List<uint8_t>() { response.Buffer[len - 1] };
                    response2.Succeeded = true;
                    return response2;
                }

                var ok = false;
                /* receive response */
                for (uint8_t i = 0; i < 10; ++i)
                {
                    response = sd_raw_rec_byte();
                    if (response.Succeeded && response.Buffer[0] != 0xff)
                    {
                        response.Buffer = new List<uint8_t>() { response.Buffer[0] };
                        ok = true;
                        break;
                    }
                }
                response.Succeeded = ok;
                return response;
            }
            finally
            {
                
            }
        }

        SPIResult sd_raw_send_command_old(int command, int arg)
        {
            SPIResult response = new SPIResult();

            /* wait some clock cycles */
            sd_raw_rec_byte();

            /* send command via SPI */
            sd_raw_send_byte(0x40 | command);
            sd_raw_send_byte((arg >> 24) & 0xff);
            sd_raw_send_byte((arg >> 16) & 0xff);
            sd_raw_send_byte((arg >> 8) & 0xff);
            sd_raw_send_byte((arg >> 0) & 0xff);

            switch (command)
            {
                case CMD_GO_IDLE_STATE_CMD0:
                    sd_raw_send_byte(0x95);
                    break;
                case CMD_SEND_IF_COND_CMD8:
                    sd_raw_send_byte(0x87);
                    break;
                default:
                    sd_raw_send_byte(0xff);
                    break;
            }
            var ok = false;
            /* receive response */
            for (uint8_t i = 0; i < 10; ++i)
            {
                response = sd_raw_rec_byte();
                if (response.Succeeded && response.Buffer[0] != 0xff)
                {
                    ok = true;
                    break;
                }
            }
            response.Succeeded = ok;
            return response;
        }

        bool WaitUntilBusy()
        {
            return CleanPendingData();
        }

        bool CleanPendingData(byte sendVal = 0xFF, int wait = -1, byte expectedVal = 0xFF)
        {
            var maxTry = 100;
            int count = 0;
            while ((count++) < maxTry)
            {
                var a = SPITransfer(new List<byte>() { sendVal }, select: false);
                if (a.Val0 == expectedVal)
                    return true;
                if (wait != -1)
                    Thread.Sleep(wait);
            }
            return false;
        }

        bool GetPendingData(byte val, int maxByte = 64, byte sendVal = 0xFF)
        {
            int count = 0;
            while ((count++) < maxByte)
            {
                var a = SPITransfer(new List<byte>() { sendVal }, select: false);
                if (a.Val0 == val)
                    return true;
            }
            return false;
        }
        
        SPIResult sd_raw_rec_byte(byte val = 0xFF)
        {
            var a = SPITransfer(new List<byte>() { val }, select: false);
            return a;
        }

        SPIResult sd_raw_rec_byte_count(int count, byte val = 0xFF, bool select = false, bool expect0xFE = false)
        {
            var b = new List<byte>();
            if (expect0xFE)
            {
                b.Add(0xFF);
                b.Add(0xFF);
            }

            for (var i = 0; i < count; i++)
                b.Add(val);

            var r = SPITransfer(b, select);

            if (expect0xFE)
            {
                if (r.Buffer[1] == 0xFE)
                {
                    r.Buffer = r.Buffer.Skip(2).ToList();
                }
                else
                {
                    r.Succeeded = false;
                    return r;
                }
            }
            return r;
        }

        SPIResult sd_raw_send_byte(int b)
        {
            //SPDR = b;
            ///* wait for byte to be shifted out */
            //while (!(SPSR & (1 << SPIF))) ;
            //SPSR &= ~(1 << SPIF);
            var a = SPITransfer(new List<byte>() { (byte)b }, select: false);
            return a;
        }

        public bool Begin(int reserved = 0)
        {
            if (Initialize())
            {
                if (sd_raw_get_info(CIDInfo, CSDInfo))
                {
                    CleanPendingData();
                    //var cardStatus = this.ReadCardStatus();
                    //if (cardStatus != null)
                    //{
                    //    this.SpeedClass = cardStatus.SpeedClass;
                    //    return true;
                    //}
                    //else
                        return true;
                }
            }
            if(reserved == 0)
            {
                Thread.Sleep(10);
                return Begin(reserved + 1);
            }
            else return false;
        }

        private bool GetRidOfCrc()
        {
            var v0 = sd_raw_rec_byte();
            var v1 = sd_raw_rec_byte();
            var timeOutCounter = 0;
            while (true) // TODO: Add a timeout
            {
                var r0 = sd_raw_rec_byte();
                if (!r0.Succeeded)
                    return false;
                if (r0.Buffer[0] == 0xFF)
                    break;
                if (++timeOutCounter > 32)
                    return false;
            }
            return true;
        }

        private bool WaitForDataStartBlock()
        {
            var timeOutCounter = 0;
            while (true) // TODO: Add a timeout
            {
                var r0 = sd_raw_rec_byte();
                if (!r0.Succeeded)
                    return false;
                if (r0.Buffer[0] == DATA_START_BLOCK)
                    break;
                if (++timeOutCounter > 32)
                    return false;
            }
            return true;
        }

        List<byte> sd_raw_read_buffer(int count, bool expectDataStartBlock = false)
        {
            var buffer = new List<byte>();

            // Expect 2 extra byte
            if(expectDataStartBlock) 
            {
                buffer.Add(0xFF);
                buffer.Add(0xFF);
            }

            for (int i = 0; i < count; ++i)
                buffer.Add(0xFF);

            if (expectDataStartBlock) // CRC
            {
                buffer.Add(0xFF);
                buffer.Add(0xFF);
            }

            var r = SPITransfer(buffer, select: false);
            if (!r.Succeeded)
                return null;

            if(expectDataStartBlock)
            {
                if(r.Buffer[1] == DATA_START_BLOCK)
                {
                    r.Buffer = r.Buffer.Skip(2).ToList(); // Skip DATA_START_BLOCK
                    //r.Buffer = r.Buffer.Take(r.Buffer.Count-2).ToList(); // Get rid of CRC
                    return r.Buffer;
                }
                else
                    return null;
            }
            else
                return r.Buffer;
        }

        internal static void InternalAssert(int expected, int actual)
        {
            if (expected != actual)
            {
                throw new ApplicationException("Internal Assert");
            }
        }

        private SPIResult WaitForDataStartBlockAndReadBuffer(int count, bool select = true, bool clearCRC = true)
        {
            var r = new SPIResult();
            try
            {
                if(select)
                    CS_Select();

                if (this.WaitForDataStartBlock())
                {
                    r.Buffer = sd_raw_read_buffer(count);
                    r.Succeeded = true;
                    if(clearCRC)
                    {
                        r.Succeeded = this.CleanPendingData();
                    }
                }
            }
            finally
            {
                if (select)
                    CS_Unselect();
            }
            return r;
        }

        public bool sd_raw_get_info(CID_INFO cidInfo, CSD_INFO_V2 csdInfo)
        {
            try
            {
                var timeOutCounter = 0;

                CS_Select();

                while (true)
                {
                    var rr = cardCommand(CMD_SEND_CID, 0, 1, select: false);
                    if (IsR1Flag(rr.Buffer[0], R1.READY_STATE)|| IsR1Flag(rr.Buffer[0], R1.IDLE_STATE))
                        break;
                    else
                        if (++timeOutCounter > 10) return false;
                }

                var spiR = WaitForDataStartBlockAndReadBuffer(18, select: false);
                if (!spiR.Succeeded)
                    return false;

                CS_Unselect();

                var r = new BitUtil.BitRprArrayToStringResult { BitArrayDef = BitUtil.BitRprArrayToString(spiR.Buffer.ToArray()) };

                r = BitUtil.BitRprArrayGet(r, 8);
                cidInfo.manufacturer = r.ValueAsByte;
                var manu = cidInfo.GetManufacturer();

                r = BitUtil.BitRprArrayGetAsciiString(r, 2);
                cidInfo.OEM = r.StringValue;

                r = BitUtil.BitRprArrayGetAsciiString(r, 5);
                cidInfo.Product = r.StringValue;

                r = BitUtil.BitRprArrayGet(r, 8);
                cidInfo.revision = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 16);
                var v0 = r.Value;
                r = BitUtil.BitRprArrayGet(r, 16);
                var v1 = r.Value;
                cidInfo.serial = (UInt32)((v1 << 16) + v0);
                //cidInfo.serial = BitConverter.ToUInt32((new List<byte> { buffer[9], buffer[10], buffer[11], buffer[12] }).ToArray(), 0);

                r = BitUtil.BitRprArrayGet(r, 8);
                cidInfo.manufacturing_year = (r.ValueAsByte << 4)+2000;
                r = BitUtil.BitRprArrayGet(r, 8);
                cidInfo.manufacturing_month = (byte)(r.ValueAsByte & 0x0f);
                
                //r = BitUtil.BitRprArrayGet(r, 8);
                //cidInfo.manufacturing_year = (byte)(r.ValueAsByte << 4);
                //r = BitUtil.BitRprArrayGet(r, 8);
                //cidInfo.manufacturing_year |= (r.ValueAsByte >> 4) + 2000;
                //r = BitUtil.BitRprArrayGet(r, 8);
                //cidInfo.manufacturing_month = (byte)(r.ValueAsByte & 0x0f);

                timeOutCounter = 0;
                while (true)
                {
                    var rr = cardCommand(CMD_SEND_CSD, 0, 1);
                    if (IsR1Flag(rr.Buffer[0], R1.READY_STATE))
                        break;
                    else
                        if (++timeOutCounter > 10) return false;
                }
                //if (!sd_raw_send_commandEx(CMD_SEND_CSD, 0, waitFor0xFE: true)) return false;
                spiR = WaitForDataStartBlockAndReadBuffer(15);

                /*
01000000 ver+res 
00001110 tac 14
00000000 nasc 0
00110010 trabs speed 50
01011011 - 0101 ccc 12 bit
1001 - read_bl_len =9
00000000
0000000001110011010011110111111110000000000010100100000000000000                
                 */

                var bits = BitUtil.BitRprArrayToString(spiR.Buffer.ToArray());
                r = BitUtil.BitRprArrayGet(bits, 2, 6); // [0]:2
                csdInfo.csd_ver = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 6);// [0]:6
                csdInfo.reserved1 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 8);// [1]:8
                csdInfo.taac = r.ValueAsByte;
                InternalAssert(0x0E, csdInfo.taac);

                r = BitUtil.BitRprArrayGet(r, 8);// [2]:8
                csdInfo.nsac = r.ValueAsByte;
                InternalAssert(0x00, csdInfo.nsac);

                r = BitUtil.BitRprArrayGet(r, 8);// [3]:8
                csdInfo.tran_speed = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 12);// [4]:8+[5:4]  // 0x32(50) to 5Ah(90)
                csdInfo.ccc_high = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 4, 4);// [5]:4
                csdInfo.read_bl_len = r.ValueAsByte;
                InternalAssert(0x09, csdInfo.read_bl_len);
                csdInfo.MaxReadSectorSize = (int)Math.Pow(2, csdInfo.read_bl_len);

                r = BitUtil.BitRprArrayGet(r, 1);// [6]:1
                csdInfo.read_bl_partial = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1);// [6]:1
                csdInfo.write_blk_misalign = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1);// [6]:1
                csdInfo.read_blk_misalign = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1);// [6]:1
                csdInfo.dsr_imp = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 6);// [6]:1
                csdInfo.reserved2 = r.ValueAsByte;

                //// byte 6
                //csdInfo.c_size_mid = buffer[8];
                ////  http://www.hjreggel.net/cardspeed/special-sd.html
                //// byte 9
                //csdInfo.c_size_low = buffer[9];

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 22);// [6]:1
                csdInfo.c_size = r.Value;
                this.FullSizeInByte = (Int64)((csdInfo.c_size + 1) * Math.Pow(2, 19));

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);// [6]:1
                csdInfo.reserved3 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1, 7);// [6]:1
                csdInfo.erase_blk_en = r.ValueAsByte;
                InternalAssert(0x01, csdInfo.erase_blk_en);

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 7, 1);// [10,11]:7
                csdInfo.sector_size = r.ValueAsByte;
                InternalAssert(0x7F, csdInfo.sector_size);

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 7);// [11,12]:7 may need to be shifted by 1 >>
                csdInfo.wp_grp_size = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);// [12]:1 may need to be shifted by 7 >>
                csdInfo.wp_grp_enable = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 2);// [6]:2
                csdInfo.reserved4 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 3, autoRightShift8: true);// [6]:2 Shift by 5(8-3) because the data is 3bits
                csdInfo.r2w_factor = r.ValueAsByte; // Should be 2
                InternalAssert(2, csdInfo.r2w_factor);

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 4, 4);
                csdInfo.write_bl_len = r.ValueAsByte; // Should be 9
                InternalAssert(0x09, csdInfo.write_bl_len);
                csdInfo.MaxWriteSectorSize = (int)Math.Pow(2, csdInfo.write_bl_len);

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);
                csdInfo.write_partial = r.ValueAsByte; // Should be >> 7 to get a 1 instead of 128

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 5);
                csdInfo.reserved5 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);
                csdInfo.file_format_grp = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);
                cidInfo.flag_copy = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);
                cidInfo.flag_write_protect = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 1);
                cidInfo.flag_write_protect_temp = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r.BitArrayDef, 2);
                cidInfo.format = r.ValueAsByte;

                return true;
            }
            finally
            {
            }
        }

        protected List<byte> MakeBufferMultipleOf(List<byte> buffer1, int multipleOf, int val = 0)
        {
            // Buffer1 size must be a multiple of 512
            var buffer1SizeExpected = ((buffer1.Count / multipleOf) + 1) * multipleOf;
            while (buffer1.Count < buffer1SizeExpected)
                buffer1.Add(0);
            return buffer1;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Manufacturer:{0}, ", CIDInfo.GetManufacturer());
            sb.AppendFormat("OEM:{0}, ", CIDInfo.OEM);
            sb.AppendFormat("Product:{0}, ", CIDInfo.Product);
            sb.AppendFormat("Revision:{0}, ", CIDInfo.revision).AppendLine();
            sb.AppendFormat("Card Type:[{0}], Class:{1}, ", this.SdCardType, this.SpeedClass).AppendLine();
            sb.AppendFormat("Serial:{0}, ", CIDInfo.serial).AppendLine();
            sb.AppendFormat("Year:{0}/{1}, ", CIDInfo.manufacturing_year, CIDInfo.manufacturing_month).AppendLine();
            sb.AppendFormat("Capacity:{0} KByte, {1} MByte, {2} GByte ", this.FullSizeInByte, this.FullSizeInMB, this.FullSizeInGB).AppendLine();
            sb.AppendFormat("Sector: {0} Max", this.MaxSector).AppendLine();
            sb.AppendFormat("Sector Size Read:{0}, Max Write:{1}", CSDInfo.MaxReadSectorSize, CSDInfo.MaxWriteSectorSize).AppendLine();
            return sb.ToString();
        }
    }
}

//int a = 0xF0, b = 0xFF;
//int c = ~a & b;



//SPIResult cardCommand(int command, int arg, int expectedLen, int expectedValue = -1)
//  {
//      var response = new SPIResult();
//      try
//      {
//          this._spi.Select();
//          Thread.Sleep(1); // wait up to 300 ms if busy -- https://github.com/adafruit/SD/blob/master/utility/Sd2Card.cpp

//          var buffer = new List<byte>()
//          {
//              (byte)(0x40 | command),
//              (byte)((arg >> 24) & 0xff),
//              (byte)((arg >> 16) & 0xff),
//              (byte)((arg >> 8) & 0xff),
//              (byte)((arg >> 0) & 0xff),
//              (byte)0,
//          };

//          switch (command)
//          {
//              case CMD_GO_IDLE_STATE_CMD0:
//                  buffer[buffer.Count - 1] = 0x95;
//                  break;
//              case CMD_SEND_IF_COND_CMD8:
//                  buffer[buffer.Count - 1] = 0x87;
//                  break;
//              default:
//                  buffer[buffer.Count - 1] = 0xff;
//                  break;
//          }
//          response = SPITransfer(buffer, select: false);
//          if (!response.Succeeded)
//              return response;

//          var buffer2 = new List<byte>(); // Prepare buffer to read expectedLen buffer
//          for (var e = 0; e < expectedLen; e++)
//              buffer2.Add(0xFF);

//          var timeOut = 0;
//          while (timeOut < 2)
//          {
//              timeOut += 1;
//              response = this._spi.Transfer(buffer2, select: false);
//              if (!response.Succeeded)
//                  return response;

//              if (expectedValue == -1)
//              {
//                  return response; // No data verifucation
//              }
//              else
//              {
//                  for (var e = 0; e < expectedLen; e++)
//                  {
//                      if (response.Buffer[e] == expectedValue)
//                      {
//                          response.Value = response.Buffer[e];
//                          return response;
//                      }
//                  }
//              }
//          }
//          response.Succeeded = false;

//          /*
//           response = this._spi.Transfer(buffer, select: false);
//                          if (!response.Succeeded)
//                              return response;

//                          var buffer2 = new List<byte>() { 0xFF, 0xFF };

//                          for (var rp = 0; rp < 4; rp++)
//                          {
//                              response = this._spi.Transfer(buffer2, select: false);
//                              if (!response.Succeeded)
//                                  return response;

//                              var len = response.Buffer.Count;
//                              if (response.Buffer[len - 2] == 255 && response.Buffer[len - 1] != 255)
//                              {
//                                  response.Buffer = new List<uint8_t>() { response.Buffer[len - 1] };
//                                  response.Succeeded = true;
//                                  return response;
//                              }
//                          }
//          */
//      }
//      finally
//      {
//          this._spi.Unselect();
//      }
//      return response;
//  }



//List<byte> sd_raw_read_buffer_old(int count)
//{
//    var buffer = new List<byte>();
//    for (int i = 0; i < count; ++i)
//    {
//        var r0 = sd_raw_rec_byte();
//        if (!r0.Succeeded)
//            return null;
//        buffer.Add(r0.Buffer[0]);
//    }
//    return buffer;
//}
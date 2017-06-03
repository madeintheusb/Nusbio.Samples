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
using System.Runtime.InteropServices;
using MadeInTheUSB.WinUtil;

//using abs = System.Math

namespace MadeInTheUSB
{

    public class CSD_INFO_V2
    {
        // byte 0
        public byte reserved1 /*: 6bit*/;
        public byte csd_ver; /*: 2*/
        
        // byte 1
        public uint8_t taac;
        // byte 2
        public uint8_t nsac;
        // byte 3
        public uint8_t tran_speed;
        // byte 4
        public uint8_t ccc_high;
        // byte 5
        public uint8_t read_bl_len; //: 4;
        public uint8_t ccc_low; //: 4;

        public int MaxReadSectorSize = -1; // 2 power read_bl_len
        public long c_size = -1;
        public int MaxWriteSectorSize = -1; // 2 power read_bl_len

        // byte 6
        public uint8_t reserved2; // : 4;
        public uint8_t dsr_imp; // : 1;
        public uint8_t read_blk_misalign; // :1;
        public uint8_t write_blk_misalign; // : 1;
        public uint8_t read_bl_partial; // : 1;
        // byte 7
        public uint8_t reserved3; // : 2;
        public uint8_t c_size_high; // : 6;
        // byte 8
        public uint8_t c_size_mid; //;
        // byte 9
        public uint8_t c_size_low; //
        // byte 10
        public uint8_t sector_size; //: [10]:7;
        public uint8_t sector_size_high; //: 6;
        public uint8_t erase_blk_en; // : 1;
        public uint8_t reserved4; // : 1;
        // byte 11
        public uint8_t wp_grp_size; // : 7;
        public uint8_t sector_size_low; // : 1;
        // byte 12
        public uint8_t write_bl_len; // : 2;
        public uint8_t r2w_factor; // : 3;
        public uint8_t reserved5; // : 2;
        public uint8_t wp_grp_enable; // : 1;
        // byte 13
        public uint8_t reserved6; // : 5;
        public uint8_t write_partial; // : 1;
        //public uint8_t write_bl_len_low; // : 2;
        // byte 14
        public uint8_t reserved7; //: 2;
        public uint8_t file_format; // : 2;
        public uint8_t tmp_write_protect; // : 1;
        public uint8_t perm_write_protect; // : 1;
        public uint8_t copy; // : 1;
        public uint8_t file_format_grp; // : 1;
        // byte 15
        public uint8_t always1; // : 1;
        public uint8_t crc; // : 7;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 0)]
    public class CID_INFO
    {
        public static List<string> VDD_MIN = new List<string> { "0.5mA", "1mA", "5mA", "10mA", "25mA", "35mA", "60mA", "100mA" };
        public static List<string> VDD_MAX = new List<string> { "1mA", "5mA", "10mA", "25mA", "35mA", "45mA", "80mA", "200mA" };
        
        public enum SD_CARD_MANUFACTURER : byte {
            Kingston = 2,
            Sandisk  = 3,
            Samsung  = 27,
            Sony     = 130,
        }

        public SD_CARD_MANUFACTURER GetManufacturer()
        {
            return (SD_CARD_MANUFACTURER)manufacturer;
        }

        /**
         * A manufacturer code globally assigned by the SD card organization.
         */
        public uint8_t manufacturer;
        /**
         * A string describing the card's OEM or content, globally assigned by the SD card organization.
         */
        public string OEM;

        /**
         * A product name.
         */
        public string Product;
       
        /**
         * The card's revision, coded in packed BCD.
         *
         * For example, the revision value \c 0x32 means "3.2".
         */
        public uint8_t revision;
        /**
         * A serial number assigned by the manufacturer.
         */
        public uint32_t serial;
        /**
         * The year of manufacturing.
         *
         * A value of zero means year 2000.
         */
        public int manufacturing_year;
        /**
         * The month of manufacturing.
         */
        public uint8_t manufacturing_month;
        /**
         * The card's total capacity in bytes.
         */
        public /*offset_t*/ulong capacity;
        /**
         * Defines wether the card's content is original or copied.
         *
         * A value of \c 0 means original, \c 1 means copied.
         */
        public uint8_t flag_copy;
        /**
         * Defines wether the card's content is write-protected.
         *
         * \note This is an internal flag and does not represent the
         *       state of the card's mechanical write-protect switch.
         */
        public uint8_t flag_write_protect;
        /**
         * Defines wether the card's content is temporarily write-protected.
         *
         * \note This is an internal flag and does not represent the
         *       state of the card's mechanical write-protect switch.
         */
        public uint8_t flag_write_protect_temp;
        /**
         * The card's data layout.
         *
         * See the \c SD_RAW_FORMAT_* constants for details.
         *
         * \note This value is not guaranteed to match reality.
         */
        public uint8_t format;


        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Manufacturer:{0}, ", GetManufacturer());
            sb.AppendFormat("OEM:{0}, ", this.OEM);
            sb.AppendFormat("Product:{0}, ", this.Product).AppendLine();
            sb.AppendFormat("Revision:{0}, ", revision);
            sb.AppendFormat("Serial:{0}, ", serial).AppendLine();
            sb.AppendFormat("Year:{0}/{1}, ", manufacturing_year, manufacturing_month).AppendLine();
            sb.AppendFormat("Capacity:{0} K Byte, {1} M Byte, {2} G Byte ", capacity/1024, capacity / 1024 / 1024, capacity / 1024 / 1024 / 1024);
            return sb.ToString();
        }
    };

    public class MICRO_SD_CARD_Base
    {
        protected const int MAX_TRY       = 6;
        protected const int MAX_BLOCK     = 512;
        protected const int CRC_BYTE_SIZE = 2;
        /**
         * The card's layout is harddisk-like, which means it contains
         * a master boot record with a partition table.
         */
        protected const int SD_RAW_FORMAT_HARDDISK = 0;
        /**
         * The card contains a single filesystem and no partition table.
         */
        protected const int SD_RAW_FORMAT_SUPERFLOPPY = 1;
        /**
         * The card's layout follows the Universal File Format.
         */
        protected const int SD_RAW_FORMAT_UNIVERSAL = 2;
        /**
         * The card's layout is unknown.
         */
        protected const int SD_RAW_FORMAT_UNKNOWN = 3;

        /* commands available in SPI mode */

        /* CMD0: response R1 */
        protected const int CMD_GO_IDLE_STATE_CMD0 = 0x00;
        /* CMD1: response R1 */
        protected const int CMD_SEND_OP_COND = 0x01;
        /* CMD8: response R7 */
        protected const int CMD_SEND_IF_COND_CMD8 = 0x08;
        /* CMD9: response R1 */
        protected const int CMD_SEND_CSD = 0x09;
        /* CMD10: response R1 */
        protected const int CMD_SEND_CID = 0x0a;
        /* CMD12: response R1 */
        protected const int CMD_STOP_TRANSMISSION_CMD12 = 0x0c;
        /* CMD13: response R2 */
        protected const int CMD_SEND_STATUS = 0x0d;
        /* CMD16: arg0[31:0]: block length, response R1 */
        protected const int CMD_SET_BLOCKLEN_16 = 0x10;
        /* CMD17: arg0[31:0]: data address, response R1 */
        protected const int CMD_READ_SINGLE_BLOCK_CMD17 = 0x11;
        /* CMD18: arg0[31:0]: data address, response R1 */
        protected const int CMD_READ_MULTIPLE_BLOCK_CMD18 = 0x12;
        /* CMD24: arg0[31:0]: data address, response R1 */
        protected const int CMD_WRITE_SINGLE_BLOCK_CMD24 = 0x18;
        /* CMD25: arg0[31:0]: data address, response R1 */
        protected const int CMD_WRITE_MULTIPLE_BLOCK_CMD25 = 0x19;
        /* CMD27: response R1 */
        protected const int CMD_PROGRAM_CSD = 0x1b;
        /* CMD28: arg0[31:0]: data address, response R1b */
        protected const int CMD_SET_WRITE_PROT = 0x1c;
        /* CMD29: arg0[31:0]: data address, response R1b */
        protected const int CMD_CLR_WRITE_PROT = 0x1d;
        /* CMD30: arg0[31:0]: write protect data address, response R1 */
        protected const int CMD_SEND_WRITE_PROT = 0x1e;
        /* CMD32: arg0[31:0]: data address, response R1 */
        protected const int CMD_TAG_SECTOR_START = 0x20;
        /* CMD33: arg0[31:0]: data address, response R1 */
        protected const int CMD_TAG_SECTOR_END = 0x21;
        /* CMD34: arg0[31:0]: data address, response R1 */
        protected const int CMD_UNTAG_SECTOR = 0x22;
        /* CMD35: arg0[31:0]: data address, response R1 */
        protected const int CMD_TAG_ERASE_GROUP_START = 0x23;
        /* CMD36: arg0[31:0]: data address, response R1 */
        protected const int CMD_TAG_ERASE_GROUP_END = 0x24;
        /* CMD37: arg0[31:0]: data address, response R1 */
        protected const int CMD_UNTAG_ERASE_GROUP = 0x25;
        /* CMD38: arg0[31:0]: stuff bits, response R1b */
        protected const int CMD_ERASE = 0x26;
        /* ACMD41: arg0[31:0]: OCR contents, response R1 */
        protected const int CMD_SD_SEND_OP_COND_CMD41 = 0x29;
        /* CMD42: arg0[31:0]: stuff bits, response R1b */
        protected const int CMD_LOCK_UNLOCK = 0x2a;
        /* CMD55: arg0[31:0]: stuff bits, response R1 */
        protected const int CMD_APP_55 = 0x37;
        /* CMD58: arg0[31:0]: stuff bits, response R3 */
        protected const int CMD_READ_OCR_58 = 0x3a;
        /* CMD59: arg0[31:1]: stuff bits, arg0[0:0]: crc option, response R1 */
        protected const int CMD_CRC_ON_OFF = 0x3b;

        /** SET_WR_BLK_ERASE_COUNT failed */
        //protected const int SD_CARD_ERROR_ACMD23 = 0X07;

        [Flags]
        public enum R1 : int
        {
            READY_STATE     = 0, // Remark is not a bit per say
            IDLE_STATE      = 1, //1 << 0,
            ERASE_RESET     = 2, //1 << 1,
            ILLEGAL_COMMAND = 4, //1 << 2,
            COM_CRC_ERR     = 8, //1 << 3,
            ERASE_SEQ_ERR   = 16,//1 << 4,
            ADDR_ERR        = 32,//1 << 5,
            PARAM_ERR       = 64 //1 << 6,
        }

        public static List<R1> R1_LIST = new List<R1>() {

            R1.IDLE_STATE,
            R1.ERASE_RESET,
            R1.ILLEGAL_COMMAND,
            R1.COM_CRC_ERR,
            R1.ERASE_SEQ_ERR ,
            R1.ADDR_ERR,
            R1.PARAM_ERR,
        };

        ///* command responses */
        ///* R1: size 1 byte   */
        //protected const int R1_IDLE_STATE = 0; // These value must be use as 1 << x to find the bit
        //protected const int R1_ERASE_RESET = 1;
        protected const int R1_ILL_COMMAND_toShift = 2;
        //protected const int R1_COM_CRC_ERR = 3;
        //protected const int R1_ERASE_SEQ_ERR = 4;
        //protected const int R1_ADDR_ERR = 5;
        //protected const int R1_PARAM_ERR = 6;

        ////// R1 status bits
        ////#define STATUS_READY					    0x00
        ////#define STATUS_IN_IDLE					0x01
        ////#define STATUS_ERASE_RESET				0x02
        ////#define STATUS_ILLEGAL_COMMAND			0x04
        ////#define STATUS_CRC_ERROR				    0x08
        ////#define STATUS_ERASE_SEQ_ERROR			0x10
        ////#define STATUS_ADDRESS_ERROR			    0x20
        ////#define STATUS_PARAMETER_ERROR			0x40
        ////#define STATUS_START_BLOCK				0xFE


        public enum R2 : ushort
        {
            R2_CARD_LOCKED = 1 << 0,
            R2_WP_ERASE_SKIP = 1 << 1,
            R2_ERR = 1 << 2,
            R2_CARD_CC_ERR = 1 << 3,
            R2_CARD_ECC_FAIL = 1 << 4,
            R2_WP_VIOLATION = 1 << 5,
            R2_INVAL_ERASE_PARAM = 1 << 6,

            R2_OUT_OF_RANGE = 1 << 7, // << same
            R2_CSD_OVERWRITE = 1 << 7, // << same

            R2_IDLE_STATE = 1 << 8,
            R2_ERASE_RESET = 1 << 9,
            R2_ILL_COMMAND = 1 << 10,
            R2_COM_CRC_ERR = 1 << 11,
            R2_ERASE_SEQ_ERR = 1 << 12,
            R2_ADDR_ERR = 1 << 13,
            R2_PARAM_ERR = 1 << 14
        }

        ///* R3: size 5 bytes */
        //protected const UInt64 R3_OCR_MASK = (0xffffffffUL);
        //protected const int R3_IDLE_STATE = (R1_IDLE_STATE + 32);
        //protected const int R3_ERASE_RESET = (R1_ERASE_RESET + 32);
        //protected const int R3_ILL_COMMAND = (R1_ILL_COMMAND + 32);
        //protected const int R3_COM_CRC_ERR = (R1_COM_CRC_ERR + 32);
        //protected const int R3_ERASE_SEQ_ERR = (R1_ERASE_SEQ_ERR + 32);
        //protected const int R3_ADDR_ERR = (R1_ADDR_ERR + 32);
        //protected const int R3_PARAM_ERR = (R1_PARAM_ERR + 32);
        /* Data Response: size 1 byte */
        protected const int DR_STATUS_MASK = 0x0e;
        protected const int DR_STATUS_ACCEPTED = 0x05;
        protected const int DR_STATUS_CRC_ERR = 0x0a;
        protected const int DR_STATUS_WRITE_ERR = 0x0c;

        /* status bits for card types */
        protected const int SD_RAW_SPEC_1 = 0;
        protected const int SD_RAW_SPEC_2 = 1;
        protected const int SD_RAW_SPEC_SDHC = 2;
        //------------------------------------------------------------------------------
        // SD card errors
        /** timeout error for command CMD0 */
        protected const int SD_CARD_ERROR_CMD0 = 0X1;
        /** CMD8 was not accepted - not a valid SD card*/
        protected const int SD_CARD_ERROR_CMD8 = 0X2;
        /** card returned an error response for CMD17 (read block) */
        protected const int SD_CARD_ERROR_CMD17 = 0X3;
        /** card returned an error response for CMD24 (write block) */
        protected const int SD_CARD_ERROR_CMD24 = 0X4;
        /**  WRITE_MULTIPLE_BLOCKS command failed */
        protected const int SD_CARD_ERROR_CMD25 = 0X05;
        /** card returned an error response for CMD58 (read OCR) */
        protected const int SD_CARD_ERROR_CMD58 = 0X06;
        /** SET_WR_BLK_ERASE_COUNT failed */
        protected const int SD_CARD_ERROR_ACMD23 = 0X07;
        /** card's ACMD41 initialization process timeout */
        protected const int SD_CARD_ERROR_ACMD41 = 0X08;
        /** card returned a bad CSR version field */
        protected const int SD_CARD_ERROR_BAD_CSD = 0X09;
        /** erase block group command failed */
        protected const int SD_CARD_ERROR_ERASE = 0X0A;
        /** card not capable of single block erase */
        protected const int SD_CARD_ERROR_ERASE_SINGLE_BLOCK = 0X0B;
        /** Erase sequence timed out */
        protected const int SD_CARD_ERROR_ERASE_TIMEOUT = 0X0C;
        /** card returned an error token instead of read data */
        protected const int SD_CARD_ERROR_READ = 0X0D;
        /** read CID or CSD failed */
        protected const int SD_CARD_ERROR_READ_REG = 0X0E;
        /** timeout while waiting for start of read data */
        protected const int SD_CARD_ERROR_READ_TIMEOUT = 0X0F;
        /** card did not accept STOP_TRAN_TOKEN */
        protected const int SD_CARD_ERROR_STOP_TRAN = 0X10;
        /** card returned an error token as a response to a write operation */
        protected const int SD_CARD_ERROR_WRITE = 0X11;
        /** attempt to write protected block zero */
        protected const int SD_CARD_ERROR_WRITE_BLOCK_ZERO = 0X12;
        /** card did not go ready for a multiple block write */
        protected const int SD_CARD_ERROR_WRITE_MULTIPLE = 0X13;
        /** card returned an error to a CMD13 status check after a write */
        protected const int SD_CARD_ERROR_WRITE_PROGRAMMING = 0X14;
        /** timeout occurred during write programming */
        protected const int SD_CARD_ERROR_WRITE_TIMEOUT = 0X15;
        /** incorrect rate selected */
        protected const int SD_CARD_ERROR_SCK_RATE = 0X16;

        //------------------------------------------------------------------------------
        /** status for card in the ready state */
        protected const int R1_READY_STATE = 0X00;
        /** status for card in the idle state */
        protected const int R1_IDLE_STATE = 0X01;
        /** status bit for illegal command */
        protected const int R1_ILLEGAL_COMMAND = 0X04;
        /** start data token for read or write single block*/
        protected const int DATA_START_BLOCK = 0xFE;
        /** stop token for write multiple blocks*/
        protected const int STOP_TRAN_TOKEN = 0xFD;
        /** start data token for write multiple blocks*/
        protected const int WRITE_MULTIPLE_TOKEN = 0xFC;
        /** mask for data response tokens after a write block operation */
        protected const int DATA_RES_MASK = 0X1F;
        /** write data accepted token */
        protected const int DATA_RES_ACCEPTED = 0X05;

        protected static string R1ToString(int val)
        {
            var s = new StringBuilder();
            s.AppendFormat("R1 {0}: ", val);
            foreach (var r in R1_LIST)
            {
                if (IsR1Flag(val, r))
                    s.AppendFormat("{0} ", r.ToString());
            }
            return s.ToString();
        }

        protected static bool IsR1Flag(int val, R1 flag)
        {
            if (flag == R1.READY_STATE)// Ready state is not a bit
                return val == (int)R1.READY_STATE;

            return (val & (int)flag) == (int)flag;
        }

        public class R7
        {
            private List<byte> _buffer5Bytes;
            byte _expectedEchoBack;
            public R7(List<byte> Buffer5Bytes, byte expectedEchoBack)
            {
                _buffer5Bytes = Buffer5Bytes;
                _expectedEchoBack = expectedEchoBack;
            }
            public bool Validate()
            {
                if (IsR1Flag(_buffer5Bytes[0], R1.ILLEGAL_COMMAND))
                    return false;
                if (_buffer5Bytes[4] != _expectedEchoBack)
                    return false;
                return true;
            }


            public byte VoltageAccepted;
            public byte CommandVersion;
            public ushort ReservedBits;
            public R1 r1;

            public bool Extract()
            {
                var bits = BitUtil.BitRprArrayToString(_buffer5Bytes.ToArray());
                var r = BitUtil.BitRprArrayGet(bits, 8);
                this.r1 = (R1)r.Value;

                r = BitUtil.BitRprArrayGet(r, 4);
                this.CommandVersion = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 16);
                this.ReservedBits = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 4);
                this.VoltageAccepted = r.ValueAsByte;
                MICRO_SD_CARD.InternalAssert(16, this.VoltageAccepted);

                r = BitUtil.BitRprArrayGet(r, 8);
                if (_expectedEchoBack != r.ValueAsByte)
                    return false;

                return true;
            }
        }


        public class R3
        {
            private List<byte> _buffer5Bytes;
            public R3(List<byte> Buffer5Bytes)
            {
                _buffer5Bytes = Buffer5Bytes;
            }
            public bool Validate()
            {
                if (IsR1Flag(_buffer5Bytes[0], R1.ILLEGAL_COMMAND))
                    return false;
                return true;
            }


            public bool Extract()
            {
                var bits = BitUtil.BitRprArrayToString(_buffer5Bytes.ToArray());
                var r = BitUtil.BitRprArrayGet(bits, 8);

                return true;
            }
        }

        public class OCR_Register
        {
            private List<byte> _buffer5Bytes;
            public OCR_Register(List<byte> Buffer5Bytes)
            {
                _buffer5Bytes = Buffer5Bytes;
            }
            public bool Validate()
            {
                if (IsR1Flag(_buffer5Bytes[0], R1.READY_STATE))
                    return true;
                if (IsR1Flag(_buffer5Bytes[0], R1.IDLE_STATE))
                    return true;
                return false;
            }

            public bool Extract()
            {
                var bits = BitUtil.BitRprArrayToString(_buffer5Bytes.Skip(1).ToArray());

                var r = BitUtil.BitRprArrayGet(bits, 4, autoRightShift8: true);
                var reserved0__0_3 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 3, autoRightShift8: true);
                var reserved1__4_6 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var reserved2__7_LowVoltage = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 7, autoRightShift8: true);
                var reserved3__8_14_7 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_15 = r.ValueAsByte;
                MICRO_SD_CARD.InternalAssert(1, voltage_15);

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_16 = r.ValueAsByte;
                MICRO_SD_CARD.InternalAssert(1, voltage_16);

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_17 = r.ValueAsByte;
                

                 r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_18 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_19 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_20 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_21 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_22 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_23 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var voltage_24_SwitchingTo1_8V_Accepted_S18A = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 5, autoRightShift8: true);
                var reserved4__25_29_5 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var CardCapacityStatus_CCS_30 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var CardPowerUpStatusBit = r.ValueAsByte;

                var Busy = CardPowerUpStatusBit == 0;
                var Ready = CardPowerUpStatusBit == 1;

                return true;
            }
        }

        public enum SpeedClass
        {
            Class0 = 0,
            Class2 = 1,
            Class4 = 2,
            Class6 = 3,
            Reserved = 4, // 4..255 reserved
            Undefined = 1024,
        };

        /// <summary>
        /// See SD Spec Part 1 - Phy Layer Simplified Spec - v 2.0 - 2006/9/25
        /// Page 65, Section 4.10.2
        /// </summary>
        public class SD_STATUS
        {
            private List<byte> _buffer64Bytes; // 64 x 8 = 512 bits of data 

            public SpeedClass SpeedClass;

            public SD_STATUS(List<byte> buffer64Bytes)
            {
                _buffer64Bytes = buffer64Bytes;
            }
            public bool Extract()
            {
                var bits = BitUtil.BitRprArrayToString(_buffer64Bytes.ToArray());

                var r = BitUtil.BitRprArrayGet(bits, 2, autoRightShift8: true); //
                var DAT_BUS_WIDTH = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 1, autoRightShift8: true);
                var SECURED_MODE = r.ValueAsByte == 1;  // expected false

                r = BitUtil.BitRprArrayGet(r, 13, autoRightShift8: true);
                var reserved0 = r.ValueAsByte;

                r = BitUtil.BitRprArrayGet(r, 16);
                var SD_CARD_TYPE = r.Value;

                r = BitUtil.BitRprArrayGet(r, 32);
                var SIZE_OF_PROTECTED_AREA = r.Value;

                r = BitUtil.BitRprArrayGet(r, 8);
                this.SpeedClass = (SpeedClass)r.Value;

                return true;
            }
        }
    }
}
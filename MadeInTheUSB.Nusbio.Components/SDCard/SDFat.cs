using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    public class SDFat
    {
        // flags for ls()
        /** ls() flag to print modify date */
        public const uint8_t  LS_DATE = 1;
        /** ls() flag to print file size */
        public const uint8_t  LS_SIZE = 2;
        /** ls() flag for recursive list of subdirectories */
        public const uint8_t  LS_R    = 4;

        // use the gnu style oflag in open()
        /** open() oflag for reading */
        public const uint8_t  O_READ    = 0X01;
        /** open() oflag - same as O_READ */
        public const uint8_t  O_RDONLY  = O_READ;
        /** open() oflag for write */
        public const uint8_t  O_WRITE   = 0X02;
        /** open() oflag - same as O_WRITE */
        public const uint8_t  O_WRONLY  = O_WRITE;
        /** open() oflag for reading and writing */
        public const uint8_t  O_RDWR    = (O_READ | O_WRITE);
        /** open() oflag mask for access modes */
        public const uint8_t  O_ACCMODE = (O_READ | O_WRITE);
        /** The file offset shall be set to the end of the file prior to each write. */
        public const uint8_t  O_APPEND  = 0X04;
        /** synchronous writes - call sync() after each write */
        public const uint8_t  O_SYNC    = 0X08;
        /** create the file if nonexistent */
        public const uint8_t  O_CREAT   = 0X10;
        /** If O_CREAT and O_EXCL are set, open() shall fail if the file exists */
        public const uint8_t  O_EXCL    = 0X20;
        /** truncate the file to zero length */
        public const uint8_t  O_TRUNC   = 0X40;

        // flags for timestamp
        /** set the file's last access date */
        public const uint8_t  T_ACCESS              = 1;
        /** set the file's creation date and time */
        public const uint8_t  T_CREATE              = 2;
        /** Set the file's write date and time */
        public const uint8_t  T_WRITE               = 4;
        // values for type_
        /** This SdFile has not been opened. */
        public const uint8_t  FAT_FILE_TYPE_CLOSED  = 0;
        /** SdFile for a file */
        public const uint8_t  FAT_FILE_TYPE_NORMAL  = 1;
        /** SdFile for a FAT16 root directory */
        public const uint8_t  FAT_FILE_TYPE_ROOT16  = 2;
        /** SdFile for a FAT32 root directory */
        public const uint8_t  FAT_FILE_TYPE_ROOT32  = 3;
        /** SdFile for a subdirectory */
        public const uint8_t  FAT_FILE_TYPE_SUBDIR  = 4;
        /** Test value for directory type */
        public const uint8_t  FAT_FILE_TYPE_MIN_DIR = FAT_FILE_TYPE_ROOT16;

        /** date field for FAT directory entry */
        public static uint16_t FAT_DATE(uint16_t year, uint8_t month, uint8_t day) {

            return (uint16_t)((year - 1980) << 9 | month << 5 | day);
        }

        /** year part of FAT directory date field */
        public static uint16_t FAT_YEAR(uint16_t fatDate) {

            return (uint16_t)(1980 + (fatDate >> 9));
        }

        /** month part of FAT directory date field */
        public static uint8_t FAT_MONTH(uint16_t fatDate) {

            return (uint8_t)((fatDate >> 5) & 0XF);
        }

        /** day part of FAT directory date field */
        public static uint8_t FAT_DAY(uint16_t fatDate) {

            return (uint8_t)(fatDate & 0X1F);
        }

        /** time field for FAT directory entry */
        public static uint16_t FAT_TIME(uint8_t hour, uint8_t minute, uint8_t second) {

            return (uint16_t)(hour << 11 | minute << 5 | second >> 1);
        }

        /** hour part of FAT directory time field */
        public static uint8_t FAT_HOUR(uint16_t fatTime) {

            return (uint8_t)(fatTime >> 11);
        }
        /** minute part of FAT directory time field */
        public static uint8_t FAT_MINUTE(uint16_t fatTime) {

            return(uint8_t)((fatTime >> 5) & 0X3F);
        }

        /** second part of FAT directory time field */
        public static uint8_t FAT_SECOND(uint16_t fatTime) {

          return (uint8_t)(2*(fatTime & 0X1F));
        }

        /** Default date for file timestamps is 1 Jan 2000 */
        public const uint16_t FAT_DEFAULT_DATE = ((2000 - 1980) << 9) | (1 << 5) | 1;

        /** Default time for file timestamp is 1 am */
        public const uint16_t FAT_DEFAULT_TIME = (1 << 11);

        /// <summary>
        /// 
        /// This is only used to remind user that there are 2 kind of sd card reader
        /// 
        /// - 3.3 Volt, in this case Nusbio hardware (Jumper) must configure to 3.3V.
        ///          This hardware configuration is only supported by Nusbio 1.0+.
        /// 
        /// 5 Volt, in this case Nusbio can remain a 5 volts device        
        /// </summary>
        public enum SDCardType
        {
            /// <summary>
            /// Only support 3.3 Volts. 
            /// NO voltage regulator for power and NO signal level shifter on the SD Card board
            /// Supported by Nusbio when Nusbio hardware is configured as a 3.3 Volts device.
            /// </summary>
            _3VoltsOnly,

            /// <summary>
            // Support 3.3 or 5 Volts for power, but only support 3.3 volts for the GPIO 
            /// connection (clock, miso, mosi,...)
            /// One voltage regulator for power and NO signal level shifter on the SD Card board.
            /// Supported by Nusbio when Nusbio hardware is configured as a 3.3 Volts device.
            /// </summary>
            _5Or3VoltsForPower_3VoltsOnlyForGpio,

            /// <summary>
            /// SD card reader with a Voltage regulator for power and a Signal level shifter
            /// </summary>
            _5VoltsCompatible,
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using unsigned = System.UInt16;
using uint8_t = System.Byte;
using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    public class FATStruct
    {
        //------------------------------------------------------------------------------
        /** Value for byte 510 of boot block or MBR */
        public const uint8_t  BOOTSIG0 = 0X55;
        /** Value for byte 511 of boot block or MBR */
        public const uint8_t BOOTSIG1 = 0XAA;

        public const int part_t_size = 1000;

        //------------------------------------------------------------------------------
        // End Of Chain values for FAT entries
        /** FAT16 end of chain value used by Microsoft. */
        public const uint16_t  FAT16EOC = 0XFFFF;
        /** Minimum value for FAT16 EOC.  Use to test for EOC. */
        public const uint16_t  FAT16EOC_MIN = 0XFFF8;
        /** FAT32 end of chain value used by Microsoft. */
        public const uint32_t  FAT32EOC = 0X0FFFFFFF;
        /** Minimum value for FAT32 EOC.  Use to test for EOC. */
        public const uint32_t  FAT32EOC_MIN = 0X0FFFFFF8;
        /** Mask a for FAT32 entry. Entries are 28 bits. */
        public const uint32_t  FAT32MASK = 0X0FFFFFFF;
    }

    //------------------------------------------------------------------------------
    /**
     * \struct partitionTable
     * \brief MBR partition table entry
     *
     * A partition table entry for a MBR formatted storage device.
     * The MBR partition table has four entries.
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct part_t /*partitionTable*/ {
        /**
        * Boot Indicator . Indicates whether the volume is the active
        * partition.  Legal values include: 0X00. Do not use for booting.
        * 0X80 Active partition.
        */
        uint8_t  boot;
        /**
        * Head part of Cylinder-head-sector address of the first block in
        * the partition. Legal values are 0-255. Only used in old PC BIOS.
        */
        uint8_t  beginHead;
        /**
        * Sector part of Cylinder-head-sector address of the first block in
        * the partition. Legal values are 1-63. Only used in old PC BIOS.
        */
        unsigned beginSector;/*6*/
        /** High bits cylinder for first block in partition. */
        unsigned beginCylinderHigh; /*2*/
        /**
        * Combine beginCylinderLow with beginCylinderHigh. Legal values
        * are 0-1023.  Only used in old PC BIOS.
        */
        uint8_t  beginCylinderLow;
        /**
        * Partition type. See defines that Begin with PART_TYPE_ for
        * some Microsoft partition types.
        */
        uint8_t  type;
        /**
        * head part of cylinder-head-sector address of the last sector in the
        * partition.  Legal values are 0-255. Only used in old PC BIOS.
        */
        uint8_t  endHead;
        /**
        * Sector part of cylinder-head-sector address of the last sector in
        * the partition.  Legal values are 1-63. Only used in old PC BIOS.
        */
        unsigned endSector; /*6*/
        /** High bits of end cylinder */
        unsigned endCylinderHigh; /*2*/
        /**
        * Combine endCylinderLow with endCylinderHigh. Legal values
        * are 0-1023.  Only used in old PC BIOS.
        */
        uint8_t  endCylinderLow;
        /** Logical block address of the first block in the partition. */
        uint32_t firstSector;
        /** Length of the partition, in blocks. */
        uint32_t totalSectors;

        public part_t(bool init = true)
        {
            boot              = 0;
            beginHead         = 0;
            beginSector       = 0;
            beginCylinderLow  = 0;
            type              = 0;
            endHead           = 0;
            endSector         = 0;
            endCylinderLow    = 0;
            firstSector       = 0;
            totalSectors      = 0;

            beginSector       = 6;
            beginCylinderHigh = 2;
            endSector         = 6;
            endCylinderHigh   = 2;
        }
    }/* __attribute__((packed))*/;

    

    /** Type name for partitionTable */
    //typedef struct partitionTable part_t;
    
    //------------------------------------------------------------------------------
    /**
     * \struct masterBootRecord
     *
     * \brief Master Boot Record
     *
     * The first block of a storage device that is formatted with a MBR.
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct /*masterBootRecord*/mbr_t {

        /** Code Area for master boot program. */
        public fixed uint8_t codeArea[440];
        /** Optional WindowsNT disk signature. May contain more boot code. */
        public uint32_t diskSignature;
        /** Usually zero but may be more boot code. */
        public uint16_t usuallyZero;

        /** Partition tables. */
        //public fixed part_t part[4];
        // Fred: Since allocating a array of 4 part_t is not possible
        public part_t part0;
        public part_t part1;
        public part_t part2;
        public part_t part3;
        
        
        /** First MBR signature byte. Must be 0X55 */
        public uint8_t  mbrSig0;
        /** Second MBR signature byte. Must be 0XAA */
        public uint8_t  mbrSig1;
    }/* __attribute__((packed))*/;
    /** Type name for masterBootRecord */
    //typedef struct masterBootRecord mbr_t;
}
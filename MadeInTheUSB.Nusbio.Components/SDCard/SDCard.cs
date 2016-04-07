//#define ALLOW_DEPRECATED_FUNCTIONS 
/*
   Copyright (C) 2015 MadeInTheUSB LLC
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
 
    Based on the SD Arduino library
*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
using uint32_t = System.UInt32;

using dir_t= System.Byte;
using mbr_t= System.Byte;
using fbs_t= System.Byte;

using MadeInTheUSB.spi;

namespace MadeInTheUSB
{
    public class Sd2Card
    {
    }

    public class SdVolume
    {
        // value for action argument in cacheRawBlock to indicate read from cache
        public const uint8_t CACHE_FOR_READ = 0;
        // value for action argument in cacheRawBlock to indicate cache dirty
        public const uint8_t CACHE_FOR_WRITE = 1;

        static cache_t cacheBuffer_;        // 512 byte cache for device blocks
        static uint32_t cacheBlockNumber_;  // Logical number of block in the cache
        static Sd2Card sdCard_;            // Sd2Card object for cache
        static uint8_t cacheDirty_;         // cacheFlush() will write block if true
        static uint32_t cacheMirrorBlock_;  // block number for mirror FAT

        uint32_t allocSearchStart_;   // start cluster for alloc search
        uint8_t blocksPerCluster_;    // cluster size in blocks
        uint32_t blocksPerFat_;       // FAT size in blocks
        uint32_t clusterCount_;       // clusters in one FAT
        uint8_t clusterSizeShift_;    // shift to convert cluster count to block count
        uint32_t dataStartBlock_;     // first data block number
        uint8_t fatCount_;            // number of FATs on volume
        uint32_t fatStartBlock_;      // start block for first FAT
        uint8_t fatType_;             // volume type (12, 16, OR 32)
        uint16_t rootDirEntryCount_;  // number of entries in FAT16 root dir
        uint32_t rootDirStart_;       // root start block for FAT16, cluster for FAT32

        static unsafe uint8_t [] cacheClear() {

            // TODO:call -> cacheFlush();

            cacheBlockNumber_ = 0XFFFFFFFF;

            // This how to copy the 512 byte buffer cacheBuffer_.data into a list
            // to return it. NOT TESTED!
            var l = new List<uint8_t>();
            fixed (byte* pData0 = cacheBuffer_.data)
            {
                byte* pData = pData0;
                for (var i = 0; i < 512; i++)
                {
                    l.Add(*pData);
                    pData++;
                }
            }
            //return cacheBuffer_.data;
            return l.ToArray();
        }
       /**
        * Initialize a FAT volume.  Try partition one first then try super
        * floppy format.
        *
        * \param[in] dev The Sd2Card where the volume is located.
        *
        * \return The value one, true, is returned for success and
        * the value zero, false, is returned for failure.  Reasons for
        * failure include not finding a valid partition, not finding a valid
        * FAT file system or an I/O error.
        */
        bool init(Sd2Card dev) { return init(dev, 1) ? true : init(dev, 0);}

        bool init(Sd2Card dev, uint8_t part)
        {
            return true; // TODO: finish
        }

        // inline functions that return volume info
        /** \return The volume's cluster size in blocks. */
        uint8_t blocksPerCluster()  {return blocksPerCluster_;}
        /** \return The number of blocks in one FAT. */
        uint32_t blocksPerFat()   {return blocksPerFat_;}
        /** \return The total number of clusters in the volume. */
        uint32_t clusterCount()  {return clusterCount_;}
        /** \return The shift count required to multiply by blocksPerCluster. */
        uint8_t clusterSizeShift()  {return clusterSizeShift_;}
        /** \return The logical block number for the start of file data. */
        uint32_t dataStartBlock()  {return dataStartBlock_;}
        /** \return The number of FAT structures on the volume. */
        uint8_t fatCount()  {return fatCount_;}
        /** \return The logical block number for the start of the first FAT. */
        uint32_t fatStartBlock()  {return fatStartBlock_;}
        /** \return The FAT type of the volume. Values are 12, 16 or 32. */
        uint8_t fatType()  {return fatType_;}
        /** \return The number of entries in the root directory for FAT16 volumes. */
        uint32_t rootDirEntryCount()  {return rootDirEntryCount_;}
        /** \return The logical block number for the start of the root directory
        on FAT16 volumes or the first cluster number on FAT32 volumes. */
        uint32_t rootDirStart()  {return rootDirStart_;}
        /** return a pointer to the Sd2Card object for this volume */
        static Sd2Card sdCard() {return sdCard_;}
  
        uint8_t blockOfCluster(uint32_t position)  
        {
            return (uint8_t)((position >> 9) & (blocksPerCluster_ - 1));
        }
        uint32_t clusterStartBlock(uint32_t cluster)  
        {
            return dataStartBlock_ + ((cluster - 2) << clusterSizeShift_);
        }
        uint32_t blockNumber(uint32_t cluster, uint32_t position)  {
            return clusterStartBlock(cluster) + blockOfCluster(position);
        }
        static void cacheSetDirty() {cacheDirty_ |= CACHE_FOR_WRITE;}

        //TODO: get real imp
        private uint8_t fatPut(uint32_t cluster, uint32_t value)
        {
            return 0;
        }

        uint8_t fatPutEOC(uint32_t cluster) {
            return fatPut(cluster, 0x0FFFFFFF);
        }
        bool isEOC(uint32_t cluster)  {
            return  cluster >= (fatType_ == 16 ? FATStruct.FAT16EOC_MIN : FATStruct.FAT32EOC_MIN);
        }
        uint8_t readBlock(uint32_t block, ref uint8_t  dst) {
            //TODO: IMP return sdCard.readBlock(block, dst);
            return 0;
        }
        unsafe uint8_t readData(uint32_t block, uint16_t offset,uint16_t count, uint8_t* dst) {
            //TODO: IMP return sdCard.readData(block, offset, count, dst);
            return 0;
        }
        uint8_t writeBlock(uint32_t block, ref uint8_t dst) {
            //TODO: IMP return sdCard.writeBlock(block, dst);
            return 0;
        }
    }

    public class SdFile
    {
        // private data
        /*uint8_t*/ int flags_;         // See above for definition of flags_ bits
        uint8_t   type_;          // type of file see above for values
        uint32_t  curCluster_;    // cluster for current file position
        uint32_t  curPosition_;   // current file position in bytes from beginning
        uint32_t  dirBlock_;      // SD block that contains directory entry for file
        uint8_t   dirIndex_;      // index of entry in dirBlock 0 <= dirIndex_ <= 0XF
        uint32_t  fileSize_;      // file size in bytes
        uint32_t  firstCluster_;  // first cluster of file
        SdVolume vol_;           // volume where file is located

        void clearUnbufferedRead() {
            flags_ &= ~F_FILE_UNBUFFERED_READ;
        }
        /** \return The current cluster number for a file or directory. */
        uint32_t curCluster()  {return curCluster_;}
        /** \return The current position for a file or directory. */
        uint32_t curPosition()  {return curPosition_;}

        /**
        * Set the date/time callback function
        *
        * \param[in] dateTime The user's call back function.  The callback
        * function is of the form:
        *
        * \code
        * void dateTime(uint16_t* date, uint16_t* time) {
        *   uint16_t year;
        *   uint8_t month, day, hour, minute, second;
        *
        *   // User gets date and time from GPS or real-time clock here
        *
        *   // return date using FAT_DATE macro to format fields
        *   *date = FAT_DATE(year, month, day);
        *
        *   // return time using FAT_TIME macro to format fields
        *   *time = FAT_TIME(hour, minute, second);
        * }
        * \endcode
        *
        * Sets the function that is called when a file is created or when
        * a file's directory entry is modified by sync(). All timestamps,
        * access, creation, and modify, are set when a file is created.
        * sync() maintains the last access date and last modify date/time.
        *
        * See the timestamp() function.
        */
        public delegate  void DateTimeCallbackDelegate (uint16_t date, uint16_t time);
        static void dateTimeCallback(DateTimeCallbackDelegate dateTime) {
            dateTime_ = dateTime;
        }
        static void dateTimeCallbackCancel() {
            // use explicit zero since NULL is not defined for Sanguino
            dateTime_ = null;
        }
        private static DateTimeCallbackDelegate dateTime_;

        /** \return Address of the block that contains this file's directory. */
        uint32_t dirBlock()  {return dirBlock_;}
        /** \return Index of this file's directory in the block dirBlock. */
        uint8_t dirIndex()  {return dirIndex_;}
            
        /** \return The total number of bytes in a file or directory. */
        uint32_t fileSize()  {return fileSize_;}
        /** \return The first cluster number for a file or directory. */
        uint32_t firstCluster()  {return firstCluster_;}
        /** \return True if this is a SdFile for a directory else false. */
        bool isDir()  {return type_ >= SDFat.FAT_FILE_TYPE_MIN_DIR;}
        /** \return True if this is a SdFile for a file else false. */
        bool isFile()  {return type_ == SDFat.FAT_FILE_TYPE_NORMAL;}
        /** \return True if this is a SdFile for an open file/directory else false. */
        bool isOpen()  {return type_ != SDFat.FAT_FILE_TYPE_CLOSED;}
        /** \return True if this is a SdFile for a subdirectory else false. */
        bool isSubDir()  {return type_ == SDFat.FAT_FILE_TYPE_SUBDIR;}
        /** \return True if this is a SdFile for the root directory. */
        bool isRoot()  {
            return type_ == SDFat.FAT_FILE_TYPE_ROOT16 || type_ == SDFat.FAT_FILE_TYPE_ROOT32;
        }
        int16_t read(byte[] buf, uint16_t nbyte)
        {
            // TODO: TO BE IMPLEMENTED
            return 0;
        }
        int16_t read()
        {
            uint8_t[] b = new uint8_t[1];
            return (int16_t)(read(b, 1) == 1 ? b[0] : -1);
        }
        /** Set the file's current position to zero. */
        void rewind() {
            curPosition_ = curCluster_ = 0;
        }
        /** Set the files position to current position + \a pos. See seekSet(). */
        uint8_t seekCur(uint32_t pos) {
            //TODO: IMP return seekSet(curPosition_ + pos);
            return 0;
        }
        /**
        * Use unbuffered reads to access this file.  Used with Wave
        * Shield ISR.  Used with Sd2Card::partialBlockRead() in WaveRP.
        *
        * Not recommended for normal applications.
        */
        void setUnbufferedRead() {
            if (isFile()) flags_ |= F_FILE_UNBUFFERED_READ;
        }
        uint8_t type()  {return type_;}
        uint8_t unbufferedRead()  {
            return (uint8_t)(flags_ & F_FILE_UNBUFFERED_READ);
        }
        #if ALLOW_DEPRECATED_FUNCTIONS
        
        #endif
         // bits defined in flags_
        // should be 0XF
        private const uint8_t F_OFLAG = SDFat.O_ACCMODE | SDFat.O_APPEND | SDFat.O_SYNC;
        // available bits
        const  uint8_t  F_UNUSED = 0X30;
        // use unbuffered SD read
        const  uint8_t  F_FILE_UNBUFFERED_READ = 0X40;
        // sync of directory entry required
        const  uint8_t  F_FILE_DIR_DIRTY = 0X80;   
        
        // make sure F_OFLAG is ok
        #if ((F_UNUSED || F_FILE_UNBUFFERED_READ || F_FILE_DIR_DIRTY) && F_OFLAG)
        #error flags_ bits conflict
        #endif  // flags_ bits
    }

    //==============================================================================
    // SdVolume class
    /**
     * \brief Cache for an SD data block
     */
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct cache_t { 
        
        // THIS IS A UNION ALL FIELD POINT TO THE SAME DATA BLOCK
        // SEE THIS SAMPLE TO DIRECT MEMORY ACCESS 
        // -- https://msdn.microsoft.com/en-us/library/zycewsya.aspx
        // -- https://msdn.microsoft.com/en-us/library/28k1s2k6.aspx

        /** Used to access cached file data blocks. */
        [FieldOffset(0)] 
        public fixed uint8_t data[512];
        /** Used to access cached FAT16 entries. */
        [FieldOffset(0)] 
        public fixed uint16_t fat16[256];
        /** Used to access cached FAT32 entries. */
        [FieldOffset(0)] 
        public fixed uint32_t fat32 [128];
        /** Used to access cached directory entries. */
        [FieldOffset(0)] 
        public fixed dir_t    dir[16];
        /** Used to access a cached MasterBoot Record. */
        [FieldOffset(0)] 
        public mbr_t    mbr;
        /** Used to access to a cached FAT boot sector. */
        [FieldOffset(0)] 
        public fbs_t    fbs;
    }

    public class File
    {
        /// <summary>
        /// Limited to 13 chars
        /// </summary>
        private string Name;
        private SdFile _sdFile;
        public int Position;
        public int Size;

        public File(SdFile sddFile, string name)
        {
            
        }
        public File()
        {
            
        }
    };

    /// <summary>
    /// 
    /// </summary>
    public class SDCard
    {
        private readonly SPIEngine _spiEngine;

        public uint8_t FILE_READ = SDFat.O_READ;
        public uint8_t FILE_WRITE = SDFat.O_READ | SDFat.O_WRITE | SDFat.O_CREAT;
        
        public SDCard(
            Nusbio nusbio,
            SDFat.SDCardType sdCardType,
            NusbioGpio selectGpio,
            NusbioGpio mosiGpio,
            NusbioGpio misoGpio,
            NusbioGpio clockGpio)
        {
            this._spiEngine = new SPIEngine(nusbio, selectGpio, mosiGpio, misoGpio, clockGpio);
        }

        public void Begin()
        {
            _spiEngine.Begin();
        }
    }
}
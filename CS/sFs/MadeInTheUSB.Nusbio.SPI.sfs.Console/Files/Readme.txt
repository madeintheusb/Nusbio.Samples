MadeInTheUSB.sFs - small Files system

A very simple file system that can be implemented in C# based on 
- 128k bytes SPI EEPROM  (Page size or sector size is 256 bytes)
    Read performance 28 Kbyte/S
    Write performance 12 Kbyte/S
- Later 16 or 32 Mb NOR FLASH and 16 GB SD Card.

Key ideas:
---------

1) The files stored on the disk with the MadeInTheUSB.sFs file system are
protected from the Windows OS and any process running on the PC including:
trojan, virus, malware, ransomware program. The .NET application contains its
own file system and HAL (Hardware abstraction layer) based on the SPI communication
protocol and the USB device Nusbio v1 or v2 (www.Nusbio.net).

2) The data is encrypted on the disk (EEPROM). Though the data may be decrypted in
memory. The encryption password is for now just a .NET string, later it should be changed into a SecureString.

3) In v1 only SPI EEPROM are supported using Nusbio v1 (see www.Nusbio.net).
In v2 support for 16Mb or more NOR FLASH memory with read performance up to 3 Mb/S using Nusbio v2. 
In v3 support for 16 GB SD Card with read performance up to 1 Mb/S Nusbio v2.

Architecture:
- The first 8 pages (8x256) 2k byte are reserved to store the FAT
    The first 5 bytes are reserved for: Magic 1byte + NumberOfFile 4bytes (see method SerializeFAT())
    The metadata for a file is 28 bytes
    The total number of file that can be stored is 42. (2048-5)/48.
- The rest of the pages are used for file storage.
- No folder only one root
- File name length is limited to 16 ascii characters. File name support 
  european or asian characters but the size may be less than 16
- File max size 2Gb
- Once a file is marked as deleted the used space is not recovered
- Data is always encrypted
- A storage compaction feature can get rid of the unused space
- The concept of page or sector is the same and is for now 256 bytes.
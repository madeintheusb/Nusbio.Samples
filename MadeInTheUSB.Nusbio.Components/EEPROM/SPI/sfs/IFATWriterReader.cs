using System;
using System.Security.Cryptography.X509Certificates;

namespace MadeInTheUSB.sFs
{
    public interface IFATWriterReader
    {
        bool WriteFAT(int fatIndex, int sector, byte [] buffer);
        byte[] LoadFAT(int fatIndex, int sector, uint ulen);

        bool WriteFile(int sector, byte [] buffer);
        byte [] ReadFile(int sector, UInt32 len);
        
        bool ResetAfterWriteOperation();

        Int64 DiskMaxByte { get;  set; }
        Int64 SectorSize  { get; set;}
        Int64 SectorCount { get; }

        bool WriteFATDetails(int fatIndex, byte[] buffer);
        byte[] LoadFatDetails(int fatIndex, int len);
    }
}

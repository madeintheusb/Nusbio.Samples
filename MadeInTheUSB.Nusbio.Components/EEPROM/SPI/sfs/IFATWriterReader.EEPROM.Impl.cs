using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace MadeInTheUSB.sFs
{
    public class FATWriterReaderEEPROMImpl : EEPROM.EEPROM_25AA1024, IFATWriterReader
    {
#if NUSBIO2
        // Implementation for Nusbio v2 is not implemented yet
        public FATWriterReaderEEPROMImpl() : base(1024)
        {
        }
#else
        // Implementation for USB device Nusbio v1
        public FATWriterReaderEEPROMImpl(Nusbio nusbio, NusbioGpio clockPin, NusbioGpio mosiPin, NusbioGpio misoPin, NusbioGpio selectPin, bool debug = false) 
            : base(nusbio, clockPin, mosiPin, misoPin, selectPin, debug)
        {
        }
#endif

        long IFATWriterReader.DiskMaxByte
        {
            get { return base.MaxByte; }
            set { throw new NotImplementedException(); }
        }

        long IFATWriterReader.SectorCount
        {
            get { return base.MaxPage; }
        }

        long IFATWriterReader.SectorSize
        {
            get { return this.PAGE_SIZE; }
            set { throw new NotImplementedException(); }
        }
        byte[] IFATWriterReader.LoadFAT(int fatIndex, int sector, uint len)
        {
            return ((IFATWriterReader)this).ReadFile(sector, len);
        }
        bool IFATWriterReader.WriteFAT(int fatIndex, int sector, byte[] buffer)
        {
            return ((IFATWriterReader)this).WriteFile(sector, buffer);
        }
        byte[] IFATWriterReader.ReadFile(int sector, uint len)
        {
            var r = base.ReadPage(sector, (int)len);
            if (r.Succeeded)
                return r.Buffer;
            return null;
        }
        bool IFATWriterReader.ResetAfterWriteOperation()
        {
            return true;
        }
        bool IFATWriterReader.WriteFile(int sector, byte[] buffer)
        {
            return WriteAll(sector, buffer.ToList());
        }
        bool IFATWriterReader.WriteFATDetails(int fatIndex, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        byte[] IFATWriterReader.LoadFatDetails(int fatIndex, int len)
        {
            throw new NotImplementedException();
        }
    }

}

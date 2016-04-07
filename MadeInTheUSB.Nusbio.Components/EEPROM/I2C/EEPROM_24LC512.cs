namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// EEPROM_24LC256
    /// Microship 32k EEPROM
    /// 512*1024/8 == 65536 = 64k
    /// </summary>
    public class EEPROM_24LC512 : EEPROM_24LCXXX
    {
        public EEPROM_24LC512(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int waitTimeAfterWriteOperation = 5, bool debug = false)
            : base(nusbio, sdaPin, sclPin, 512, waitTimeAfterWriteOperation, debug)
        {
            
        }
    }
}
namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// EEPROM_24LC256
    /// Microship 32k EEPROM
    /// 256*1024/8 == 32768 = 32k
    /// </summary>
    public class EEPROM_24LC256 : EEPROM_24LCXXX
    {
        public EEPROM_24LC256(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int waitTimeAfterWriteOperation = 5, bool debug = false)
            : base(nusbio, sdaPin, sclPin, 256, waitTimeAfterWriteOperation, debug)
        {
            
        }
    }
}
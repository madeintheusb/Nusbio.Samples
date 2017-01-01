namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// EEPROM_24LC256
    /// Microship 32k EEPROM
    /// 256*1024/8 == 32768 = 32k
    /// </summary>
    public class EEPROM_24LC256 : EEPROM_24LCXXX
    {
        public const byte NEW_WRITTEN_VALUE_1 = 128 + 1;
        public const byte NEW_WRITTEN_VALUE_2 = 170;

        public EEPROM_24LC256(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int waitTimeAfterWriteOperation = 5, bool debug = false)
            : base(nusbio, sdaPin, sclPin, 256, waitTimeAfterWriteOperation, debug)
        {

        }
        
        public static bool VerifyPage(EEPROM_BUFFER r, int pageIndex)
        {
            int totalErrorCount = 0;
            for (var i = 0; i < EEPROM_24LCXXX.DEFAULT_PAGE_SIZE; i++)
            {
                var expected = i;
                if (pageIndex == 2)
                    expected = NEW_WRITTEN_VALUE_1;
                if (pageIndex == 3)
                    expected = NEW_WRITTEN_VALUE_2;

                if (r.Buffer[i] != expected)
                {
                    System.Console.WriteLine("Failed Page:{0} [{1}] = {2}, expected {3}", 
                        pageIndex, i, r.Buffer[i], expected);
                    totalErrorCount += 1;
                }
            }
            return totalErrorCount == 0;
        }
    }
}
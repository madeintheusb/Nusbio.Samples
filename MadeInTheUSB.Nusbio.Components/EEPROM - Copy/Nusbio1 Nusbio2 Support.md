Nusbio v1 and v2 support strategy
=================================

# EEPROM
There is a common base class for all SPI and I2C EEPROM
supported named EEPROM_BASE_CLASS

The EEPROM supported are
	I2C: Family if EEPROM_24LC256(32Kb) and smaller
	SPI: Family if EEPROM_25AA256(32Kb), EEPROM_25AA1024(128Kb)


EEPROM_BUFFER

## I2C
	For I2C there is a second base class EEPROM_24LCXXX which implement
	most of the code
		public bool Begin(byte _addr = DEFAULT_I2C_ADDR)
		public override bool WriteByte(int addr, byte value)
		public override int ReadByte(int addr)
		public override bool WritePage(int addr, byte[] buffer)
		public override EEPROM_BUFFER ReadPage(int addr, int len = EEPROM_BASE_CLASS.DEFAULT_PAGE_SIZE)

	To support Nusbio2 we use the compilation symbol NUSBIO2 in the 4
	ReadXXXX and WriteXXXX Method.

The class EEPROM_24LC512, EEPROM_24LC256 only define the size of the 
EEPROM     

## SPI
	For SPI there is a second base class EEPROM_25AAXXX_BASE which implement
	most of the code

	To support Nusbio2 we use the compilation symbol NUSBIO2 in the 4
	
        public SPIResult SpiTransfer(List<byte> bytes, bool select = true, bool optimizeDataLine = false);
        protected bool SendCommand(byte cmd);
	
The class EEPROM_25AA1024(128Kb) and EEPROM_25AA256(32Kb) only
implement the minimum.

Remark EEPROM_25AA1024 is a 3 byte address and a page size of 256 byte.
Remark EEPROM_25AA256 is a 2 byte address and a page size of 64 byte.

Nusbio 1 SPI Optimization: the file The EEPROM_25AAXXX_BASE.Nusbio.1.cs,
contains specific method to optimize the SPI protocol, which do not apply 
for Nusbio2


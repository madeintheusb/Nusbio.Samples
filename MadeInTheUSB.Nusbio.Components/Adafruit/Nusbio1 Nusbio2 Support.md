Nusbio v1 and v2 support strategy
=================================

For support of Adafruit devices with Nusbio1 and 2
we try to keep as much as common code.

Adafruit_GFX.cs was designed to be generic by the creator and does not
change.

The class LEDBackpack now implement the interface
	
	interface Ii2cOut
    {
        bool i2c_Send1ByteCommand(byte c);
        bool i2c_Send2ByteCommand(byte c0, byte c1);
        bool i2c_WriteBuffer(byte addr, byte[] buffer);
	}

To talk the I2C Driver.

We use the compilation symbol NUSBIO2 to compile the right implementation
for Nusbio 1 or 2.

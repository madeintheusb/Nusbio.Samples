/*
    Written by FT for MadeInTheUSB
    Copyright (C) 2015 MadeInTheUSB LLC

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
 
    This code is based from
 
    Adafruit - MCP4725 Breakout Board - 12-Bit DAC w/I2C Interface
    For the Nusbio
    https://www.adafruit.com/products/935
    https://learn.adafruit.com/mcp4725-12-bit-dac-tutorial/using-with-arduino
  
    SparkFun I2C DAC Breakout - MCP4725
    For the Nusbio
    https://www.sparkfun.com/products/12918?_ga=1.257062152.2055457765.1416957430
    https://learn.sparkfun.com/tutorials/mcp4725-digital-to-analog-converter-hookup-guide

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;

namespace MadeInTheUSB.Adafruit
{
    public class MCP4725_12BitDac
    {
        const byte MCP4726_CMD_WRITEDAC       = 0x40; // Writes data to the DAC
        const byte MCP4726_CMD_WRITEDACEEPROM = 0x60; // Writes data to the DAC and the EEPROM (persisting the assigned value after reset)
        public const int MAX_DIGITAL_VALUE    = 4095;

        private I2CEngine _i2c;

        public MCP4725_12BitDac(Nusbio nusbio, NusbioGpio sdaOutPin,  NusbioGpio sclPin, byte deviceId, int waitTimeAfterWriteOperation = 5, bool debug = false)
        {
            this._i2c = new I2CEngine(nusbio, sdaOutPin, sclPin, deviceId, debug);
        }

        public void Begin(byte deviceAddress)
        {
            this._i2c.DeviceId = deviceAddress;
        }

        public bool SetVoltage(UInt16 output, bool writeEEPROM)
        {
            byte e  = writeEEPROM ? MCP4726_CMD_WRITEDACEEPROM : MCP4726_CMD_WRITEDAC;

            //byte o0 = (byte)(output/16); // One way to do it
            //byte o1 = (byte) ((output%16) << 4);

            byte o0 = (byte)(output >> 4); // Another way
            byte o1 = (byte)((output & 15) << 4);

            return this._i2c.Send3BytesCommand(e, o0, o1);
        }

        public double ComputeVoltage(int digitalValue, double referenceVoltage)
        {
            return referenceVoltage / MAX_DIGITAL_VALUE * digitalValue;
        }

        public bool SetVoltage(int output, bool writeEEPROM)
        {
            return this.SetVoltage((UInt16) output, writeEEPROM);
        }
    }
}


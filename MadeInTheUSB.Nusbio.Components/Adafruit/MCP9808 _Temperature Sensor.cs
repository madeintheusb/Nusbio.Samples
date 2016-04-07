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
  
    Adafruit_MCP9808 - library - https://github.com/adafruit/Adafruit_MCP9808_Library
  
    See Adafruit: 
        - MCP9808 High Accuracy I2C Temperature Sensor Breakout Board - https://www.adafruit.com/product/1782
        - Adafruit MCP9808 Precision I2C Temperature Sensor Guide - https://learn.adafruit.com/adafruit-mcp9808-precision-i2c-temperature-sensor-guide/overview
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;

using int16_t  = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t  = System.Byte;


namespace MadeInTheUSB
{
    public enum TemperatureType
    {
        Celsius,
        Fahrenheit,
        Kelvin
    }

    public class MCP9808_TemperatureSensor
    {
        public const int MCP9808_I2CADDR_DEFAULT        = 0x18;
        public const int MCP9808_REG_CONFIG             = 0x01;

        public const int MCP9808_REG_CONFIG_SHUTDOWN    = 0x0100;
        public const int MCP9808_REG_CONFIG_CRITLOCKED  = 0x0080;
        public const int MCP9808_REG_CONFIG_WINLOCKED   = 0x0040;
        public const int MCP9808_REG_CONFIG_INTCLR      = 0x0020;
        public const int MCP9808_REG_CONFIG_ALERTSTAT   = 0x0010;
        public const int MCP9808_REG_CONFIG_ALERTCTRL   = 0x0008;
        public const int MCP9808_REG_CONFIG_ALERTSEL    = 0x0002;
        public const int MCP9808_REG_CONFIG_ALERTPOL    = 0x0002;
        public const int MCP9808_REG_CONFIG_ALERTMODE   = 0x0001;

        public const int MCP9808_REG_UPPER_TEMP         = 0x02;
        public const int MCP9808_REG_LOWER_TEMP         = 0x03;
        public const int MCP9808_REG_CRIT_TEMP          = 0x04;
        public const int MCP9808_REG_AMBIENT_TEMP       = 0x05;

        public const int MCP9808_REG_MANUF_ID           = 0x06;
        public const int MCP9808_REG_MANUF_ID_ANSWER    = 0x0054;

        public const int MCP9808_REG_DEVICE_ID          = 0x07;
        public const int MCP9808_REG_DEVICE_ID_ANSWER   = 0x0400;
        

        public const double CELCIUS_TO_KELVIN = 274.15;

       

        private Nusbio _nusbio; 
        private I2CEngine _i2c;

        public MCP9808_TemperatureSensor(Nusbio nusbio, NusbioGpio sdaOutPin,  NusbioGpio sclPin, byte deviceId = MCP9808_I2CADDR_DEFAULT, int waitTimeAfterWriteOperation = 5, bool debug = false)
        {
            this._i2c = new I2CEngine(nusbio, sdaOutPin, sclPin, deviceId, debug);
            this._nusbio = nusbio;
        }

        public bool Begin(byte deviceAddress = MCP9808_I2CADDR_DEFAULT)
        {
            try
            {
                this._i2c.DeviceId = deviceAddress;
                if (read16(MCP9808_REG_MANUF_ID) != MCP9808_REG_MANUF_ID_ANSWER) return false;
                if (read16(MCP9808_REG_DEVICE_ID) != MCP9808_REG_DEVICE_ID_ANSWER) return false;
                return true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        public double GetTemperature(TemperatureType type = TemperatureType.Celsius)
        {
            //System.Threading.Thread.Sleep(40);

            uint16_t t = read16(MCP9808_REG_AMBIENT_TEMP);
            double temp = t & 0x0FFF;
            temp /= 16.0;
            if ((t & 0x1000) == 0x1000) temp -= 256;

            switch (type)
            {
                case TemperatureType.Celsius: return temp;
                case TemperatureType.Fahrenheit: return CelsiusToFahrenheit(temp);
                case TemperatureType.Kelvin: return temp*CELCIUS_TO_KELVIN;
                default:
                    throw new ArgumentException();
            }
        }

        public double CelsiusToFahrenheit(double v)
        {
            return (v * 9.0 / 5.0) + 32.0;
        }

        public double CelsiusToKelvin(double v)
        {
            return v * CELCIUS_TO_KELVIN;
        }

        private uint16_t read16(uint8_t reg)
        {
            // this._nusbio.SetBaudRate(9600);

            var result = this._i2c.Send1ByteRead2Bytes(reg);

            // this._nusbio.SetBaudRate(Nusbio.BaudRate);

            if (result.Succeeded)
            {
                // Calling the MCP9808 too fast disrupt the chip.
                // Slowing down the baudrate does not change anything
                // Tested with Nusbio abd NusbioWithClock, Only Sleep(40) seems to work
                System.Threading.Thread.Sleep(40);
                return (uint16_t) result.value;
            }
            else
                throw new ArgumentException();
        }
    }
}


/*
 
   MCP23008 and MCP230XX I2C Support for the Nusbio
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB
  
   MIT License (MIT)
  
   Datasheet: http://ww1.microchip.com/downloads/en/DeviceDoc/21919b.pdf
 
        MCP23008 - I2C
        MCP23S08 - SPI
  
  For more info in the MCP23008 see
     - Expanding the number of I/O lines using Microchip MCP23008 - http://embedded-lab.com/blog/?p=2834
 
   Copyright (C) 2015 MadeInTheUSB LLC
    
   This code is based from Adafruit-MCP23008-library
   https://www.adafruit.com/product/593
   https://github.com/adafruit/Adafruit-MCP23008-library

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
  
    MIT license, all text above must be included in any redistribution
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;

using int16_t = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t = System.Byte;

namespace MadeInTheUSB
{
    /// <summary>
    /// MCP23008 implementation
    /// http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP23008
    /// http://ww1.microchip.com/downloads/en/DeviceDoc/21919e.pdf
    /// http://www.mouser.com/ProductDetail/Microchip-Technology/MCP23008-E-P/?qs=sGAEpiMZZMvcAs5GUBtMdVgPLCS8TUZv
    /// </summary>
    public class MCP23008 : MCP230XX_Base,  IDigitalWriteRead
    {
        protected override int GetMaxGPIO()
        {
            return 8;
        }

        public MCP23008(Nusbio nusbio, NusbioGpio sdaPin, NusbioGpio sclPin, int gpioStartIndex = 8)
            : base(nusbio, sdaPin, sclPin)
        {
            this._gpioStartIndex = (byte)gpioStartIndex;

            for (var i = 0; i < this.GetMaxGPIO(); i++)
            {
                base.AddGPIO(MCP230XX_Base.BuildGpioName(this.GpioStartIndex + i), PinMode.Output, true);
            }
        }

        //public void SetGpioMask(byte mask)
        //{
        //    SetGPIOMask(mask);
        //}

        public byte ComputeMask(byte mask, int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            var mcpIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpIndex > 7) 
                return mask;
            
            int gpio = mask;

            // set the pin and direction
            if (d == GPIO.PinState.High)
            {
                gpio |= (byte)(1 << mcpIndex);
            }
            else
            {
                gpio &= (byte)(~(1 << mcpIndex));
            }
            return (byte)gpio;
        }

        public override void DigitalWrite(int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            var gpio = VerifyGpioIndex(gpioIndex);
            if (gpio.Mode != PinMode.Output)
                throw new ArgumentException(string.Format("GPIO {0} is not configured as ouput", gpioIndex));
            
            this.SetGpioMask(ComputeMask(GetGpioMask(), gpioIndex, d));
        }

        public override void SetPullUp(int gpioIndex, MadeInTheUSB.GPIO.PinState d)
        {
            var mcpGpioIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpGpioIndex > 7)
                throw new InvalidGpioOperationException(string.Format("Invalid gpio index:{0}", gpioIndex));

            int gppu = this._i2c.Send1ByteRead1Byte(MCP230XX_GPPU);
            if (gppu == -1)
                throw new InvalidGpioOperationException(string.Format("Command MCP230XX_GPPU({0}) failed gpio index:{1}", MCP230XX_GPPU, gpioIndex));

            if (d == GPIO.PinState.High)
            { // set the pin and direction
                gppu |= (byte)(1 << mcpGpioIndex);
            }
            else
            {
                gppu &= (byte)(~(1 << mcpGpioIndex));
            }
            // write the new GPIO
            this._i2c.Send2BytesCommand(MCP230XX_GPPU, (byte)gppu);
        }

        public GpioPublicApiBase VerifyGpioIndex (int gpioIndex)
        {
            var gpio = this.GPIOS.Values.FirstOrDefault(g => g.Name == "Gpio" + gpioIndex);
            if (gpio == null)
                throw new ArgumentException(string.Format("Cannot find GPIO {0}", gpioIndex));
            return gpio;
        }

        public override PinState DigitalRead(int gpioIndex)
        {
            var gpio = VerifyGpioIndex(gpioIndex);
            if (gpio.Mode == PinMode.Output)
                throw new ArgumentException(string.Format("GPIO {0} is not configured as input", gpioIndex));

            var mcpIndex = gpioIndex - this.GpioStartIndex;

            // only 8 bits!
            if (mcpIndex > 7)
                return PinState.Unknown;

            var mask = GetGpioMask();
            var maskB = WinUtil.BitUtil.BitRpr(mask);
            // read the current GPIO
            var v = (byte)((mask >> mcpIndex) & 0x1);

            return v == 0 ? MadeInTheUSB.GPIO.PinState.Low : MadeInTheUSB.GPIO.PinState.High;
        }
    }
}


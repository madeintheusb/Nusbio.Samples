/*
   Copyright (C) 2015, 2016 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

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
using MadeInTheUSB.Sensor;
using MadeInTheUSB.spi;

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// TC77 SPI temperature sensor +- 1 degre C
    /// 12 bit value, 1 bit for sign
    /// Datasheet: http://ww1.microchip.com/downloads/en/devicedoc/20092a.pdf
    /// 
    /// SPI TC77 Temperature Sensor (MOSI is not used, we only read data from the sensor)
    ///    1 MISO      [] [] 5 VDD VCC
    ///    2 SCK       [] [] 6 CS
    ///    3 NC        [] [] 7 NC
    ///    4 GND/VSS   [] [] 8 NC
    /// 
    /// Nusbio Recommended wiring
    /// Clock   Gpio0
    /// Mosi    Gpio1
    /// Miso    Gpio2
    /// CS      Gpio3
    /// 
    /// The TC77 is one of those SPI component that has MOSI and MISO connected to the
    /// same pin. 
    /// 
    /// 
    /// </summary>
    public class TC77 : AnalogTemperatureSensor // Just inherited to inherit some methods
    {
        public SPIEngine _spi;

        public TC77(Nusbio nusbio, 
            NusbioGpio clockGpio, 
            NusbioGpio mosiGpio, 
            NusbioGpio misoGpio,
            NusbioGpio selectGpio,
            bool debug = false) : base(nusbio)
        {
            this._spi = new SPIEngine(nusbio, selectGpio, mosiGpio, misoGpio, clockGpio, NusbioGpio.None, false);
        }

        public void Begin()
        {

        }

        public override double GetTemperature(TemperatureType type = TemperatureType.Celsius)
        {
            this._celsiusValue = __GetTemperature();
            switch (type)
            {
                case TemperatureType.Celsius: return this._celsiusValue;
                case TemperatureType.Fahrenheit: return base.CelsiusToFahrenheit(this._celsiusValue);
                case TemperatureType.Kelvin:     return base.CelsiusToKelvin(this._celsiusValue);
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// http://www.microchip.com/wwwproducts/en/TC77
        /// ±1°C Accuracy from +25°C to +65°C
        /// ±2°C Accuracy from -40°C to +85°C
        /// Testing at +21°C the sensor return +23°C which ±2°C Accuracy 
        /// that is ok according the datasheet, but is not great in term of
        /// accuracy, specially in Fahrenheit. 
        /// Set this value to 0.059 to approximate a better temperature.
        /// TODO: Re test when temperature is > +25°C
        /// </summary>
        public double CelciusConverter = 0.0625; // 0.0625 0.059

        /// <summary>
        /// // http://www.kerrywong.com/2012/10/10/mcp2210-library-spi-example-using-tc77/
        /// </summary>
        /// <returns></returns>
        private double __GetTemperature()
        {
            var r       = this._spi.Transfer(new List<byte>() { 0, 0 }); // Just transfert 16 bits of 0 to get the temperature
            int tempVal = 0;
            int sign    = r.Buffer[0] & 0x80;
 
            // 13 bit 2's complement left aligned (last three bits are all 1's)
            if (sign == 0)
                tempVal = (r.Buffer[0] << 8 | r.Buffer[1]) >> 3;
            else
                tempVal = (((r.Buffer[0] & 0x7f) << 8 | r.Buffer[1]) >> 3) - 4096;
 
            double tempC = tempVal * CelciusConverter;

            return tempC;

            //// https://github.com/MajenkoLibraries/TC77/blob/master/TC77.cpp
            //byte bh = r.ReadBuffer[0];
            //byte bl = r.ReadBuffer[1];
            //var tbin = (bh << 8) | bl;
            //tbin /= 8; // Important to divide, not shift, since it then preserves the sign
            //var temp = (float)tbin * 0.0625;
        }
    }
}
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
namespace MadeInTheUSB.EEPROM
{
    /// <summary>
    /// SPI EEPROM 25AA256 23Kb
    /// EEPROM_25AA256
    ///     CS      [] [] VCC
    ///     MISO/SO [] [] HOLD (Set HIGH while CLOCK is low)
    ///     WP(HIGH)[] [] SCK
    ///     GND     [] [] MOSI/SI
    /// 
    /// Nusbio Recommended wiring
    /// Clock   Gpio0
    /// Mosi    Gpio1
    /// Miso    Gpio2
    /// CS      Gpio3
    /// 
    /// CosmosOS
    /// Check out the fat test kernel in the solution for file system samples
    /// </summary>
    public class EEPROM_25AA256  : EEPROM_25AAXXX_BASE
    {
        /// <summary>
        /// SPI Constructor
        /// </summary>
        /// <param name="nusbio"></param>
        /// <param name="clockPin"></param>
        /// <param name="mosiPin"></param>
        /// <param name="misoPin"></param>
        /// <param name="selectPin"></param>
        /// <param name="kBit"></param>
        /// <param name="debug"></param>
        public EEPROM_25AA256(Nusbio nusbio, 
            NusbioGpio clockPin, 
            NusbioGpio mosiPin, 
            NusbioGpio misoPin, 
            NusbioGpio selectPin,
            bool debug = false) : base(nusbio, clockPin, mosiPin, misoPin, selectPin, 256, debug)
        {
            
        }
    }
}
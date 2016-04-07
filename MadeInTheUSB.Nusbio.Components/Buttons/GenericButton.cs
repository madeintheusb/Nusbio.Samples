/*
   Copyright (C) 2015 MadeInTheUSB LLC
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
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Buttons
{
    /// <summary>
    /// The GenericButton class works with Nusbio gpios and also an gpio expander added to Nusbio see component
    /// The Button class only works with Nusbio gpios and provide extra feature.
    /// </summary>
    public class GenericButton
    {
        IDigitalWriteRead _digitalWriteRead;
        int _gpioIndex;

        public bool _inverse = false;

        public GenericButton(IDigitalWriteRead digitalWriteRead, int gpioIndex, bool inverse = false)
        {
            this._gpioIndex         = gpioIndex;
            this._digitalWriteRead  = digitalWriteRead;
            this._inverse           = inverse;
            this._digitalWriteRead.SetPinMode(gpioIndex, PinMode.Input);
        }

        public PinState GetButtonState(bool debounced = true)
        {
            return this._digitalWriteRead.DigitalRead(this._gpioIndex);
        }

        public bool IsDown()
        {
            return this.final(this.GetButtonState() == PinState.High);
        }

        private bool final(bool state)
        {
            if (this._inverse)
                return !state;
            else
                return state;
        }
    }
}

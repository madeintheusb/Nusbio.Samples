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
    /// This button class only works with Nusbio gpios and provide extra feature.
    /// For a button that can works with Nusbio gpios and also an gpio expander added to Nusbio see component
    /// GenericButton
    /// </summary>
    public class Button
    {
        private NusbioGpio _gpioPin;
        private Nusbio     _nusbio;
        public string Name;
        
        public bool Inverse = false;

        public enum ButtonState
        {
            Up = 0,
            Down = 1,
            On = 1,
            Off = 0
        };

        
        public Button(Nusbio nusbio, NusbioGpio gpioPin, string name = null, bool inverse = false)
        {
            this.Name     = name;
            this._gpioPin = gpioPin;
            this._nusbio  = nusbio;
            this.Inverse  = inverse;
            this._nusbio.SetPinMode(gpioPin, PinMode.Input);
            
        }

        public PinState GetButtonState()
        {
            return _nusbio.GPIOS[_gpioPin].DigitalRead();
        }

        public ButtonState GetState()
        {
            if (GetButtonState() == PinState.High)
            {
                return !this.Inverse ? ButtonState.Down : ButtonState.Up;
            }
            else
            {
                return !this.Inverse ? ButtonState.Up : ButtonState.Down;
            }
        }

        public bool GetButtonUpState(bool incCounter = true)
        {
            var state = _nusbio.GPIOS[_gpioPin].DigitalReadDebounced();
            return this.Final(state == PinState.Low);
        }

        public bool WaitUntilReleased(int timeOut = 4)
        {
            var butttonReleaseTimeOut = new TimeOut(4 * 1000);
            while (true)
            { // Wait for the button to be released
                if (butttonReleaseTimeOut.IsTimeOut())
                    return false;
                if (!this.GetButtonDownState())
                    return true;
                System.Threading.Thread.Sleep(10);
            }
        }

    public bool GetButtonDownState(bool incCounter = true)
        {            
            var state = _nusbio.GPIOS[_gpioPin].DigitalReadDebounced();            
            return this.Final(state == PinState.High);
        }

        private bool Final(bool state)
        {
            if (this.Inverse)
                return !state;
            else
                return state;
        }
    }
}

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

namespace MadeInTheUSB.Sensor
{
    /// <summary>
    /// Relay.
    /// On is LOW, Off is HIGH
    /// </summary>
    public class Relay
    {
        private int               _gpioPin;
        private IDigitalWriteRead _digitalWriteRead;
        private PinState          _state;

        public Relay(IDigitalWriteRead _digitalWriteRead, int gpioPin)
        {
            this._gpioPin           = gpioPin;
            this._digitalWriteRead  = _digitalWriteRead;
            this._state             = PinState.Unknown;
            this.TurnOff();
        }

        public bool IsOn
        {
            get { return this._state == PinState.Low; }
        }

        public Relay Reverse()
        {
            if(this.IsOn)
                this.TurnOff();
            else
                this.TurnOn();
            return this;
        }

        public Relay TurnOn()
        {
            return this.OnOff(true);
        }

        public Relay TurnOff()
        {
            return this.OnOff(false);
        }

        public Relay OnOff(bool on)
        {
            this._state = on ? PinState.Low : PinState.High;
            this._digitalWriteRead.DigitalWrite(this._gpioPin, this._state);
            return this;
        }
    }
}

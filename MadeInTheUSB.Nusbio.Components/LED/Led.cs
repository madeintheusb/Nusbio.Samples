/*
   Copyright (C) 2015 MadeInTheUSB LLC
   Written by FT for MadeInTheUSB

   The MIT License (MIT)
 * 
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

namespace MadeInTheUSB.Components
{
    /// <summary>
    /// SunFlower Relay as reverse HIGH is off, LOW is ON
    /// </summary>
    public class Led : GpioLedPublicApi 
    {
        private NusbioGpio _gpioPin;
        private Nusbio     _nusbio;

        public Led(Nusbio nusbio, NusbioGpio gpioPin)
        {
            this._gpioPin           = gpioPin;
            this._nusbio           = nusbio;
        }

        public void High()
        {
            DigitalWrite(PinState.High);
        }

        public void Low()
        {
            DigitalWrite(PinState.Low);
        }

        private GpioApi G
        {
            get { return this._nusbio.GPIOS[this._gpioPin]; }
        }

        public GpioLedPublicApi ReverseBlinkMode(int rate)
        {
            return G.AsLed.ReverseBlinkMode(rate);
        }

        public GpioLedPublicApi SetBlinkMode(int blinkDuration, int doubleBlinkDuration = 0)
        {
            return G.AsLed.SetBlinkMode(blinkDuration, doubleBlinkDuration);
        }

        public GpioLedPublicApi SetBlinkModeOff()
        {
            return G.AsLed.SetBlinkModeOff();
        }

        public GpioLedPublicApi BlinkSynchronous(int onDuration, int offDuration, int repeat)
        {
            return G.AsLed.BlinkSynchronous(onDuration, offDuration, repeat);
        }

        public GpioLedPublicApi ReverseSet()
        {
            return G.AsLed.ReverseSet();
        }

        public void On()
        {
            this.Set(true);
        }

        public void Off()
        {
            this.Set(false);
        }

        public GpioLedPublicApi Set(bool on)
        {
            return G.AsLed.Set(on);
        }

        public ExecutionModeEnum ExecutionMode
        {
            get { return G.AsLed.ExecutionMode; }
        }

        public bool State
        {
            get { return G.AsLed.State; }
            set { G.AsLed.State = value; }
        }

        public PinState PinState
        {
            get { return G.AsLed.PinState; }
            set { G.AsLed.PinState = value; }
        }

        public string Name
        {
            get { return G.AsLed.Name; }
            set { G.AsLed.Name = value; }
        }

        public PinMode Mode
        {
            get { return G.AsLed.Mode; }
            set { G.AsLed.Mode = value; }
        }

        public void DigitalWrite(PinState on)
        {
            G.AsLed.DigitalWrite(on);
        }

        public void DigitalWrite(bool high)
        {
            this.DigitalWrite(high ? PinState.High : PinState.Low);
        }

        public PinState DigitalRead()
        {
            throw new NotImplementedException();
        }
    }
}

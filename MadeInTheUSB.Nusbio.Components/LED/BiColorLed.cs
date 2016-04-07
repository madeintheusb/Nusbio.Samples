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
using System.Collections.Generic;

namespace MadeInTheUSB.Components
{
    /// <summary>
    /// 
    /// </summary>
    public class BiColorLedStrip : List<BiColorLed> {

        List<int> _biColorLedStateIndex;

        public BiColorLedStrip(List<BiColorLed> biColorLeds)
        {
            _biColorLedStateIndex = new List<int>();
            foreach(var l in biColorLeds)
            {
                this.Add(l);
                _biColorLedStateIndex.Add(0);
            }
        }
        public void AllOff()
        {
            this.ForEach(l => l.AllOff());
        }
        public void ReverseSet()
        {
            this.ForEach(l => l.ReverseSet());
        }
        public void Set(bool incState = false, int firstStateIndex = 0)
        {
            this.ForEach(l => l.Set(l.State, incState, firstStateIndex));
        }
        public void Set(MadeInTheUSB.Components.BiColorLed.BiColorLedState state, bool incState = false, int firstStateIndex = 0)
        {
            this.ForEach(l => l.Set(state, incState, firstStateIndex));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class BiColorLed
    {
        public enum BiColorLedState : byte
        {
            Off     = 0,     // No light
            Red     = 1,     // Red
            Green   = 2,     // Green
            Yellow  = 3,     // Red+Green = Yellow
        }

        private const int MAX_STATE = (int)BiColorLedState.Yellow;

        public Led Led0;
        public Led Led1;
        public BiColorLedState State;

        public int StateIndex 
        {
            get {
                return (int)this.State;
            }
            set
            {
                this.State = (BiColorLedState)value;
            }
        }
        
        public BiColorLed(Nusbio nusbio, NusbioGpio led0GpioPin, NusbioGpio led1GpioPin)
        {
            this.Led0 = new Led(nusbio, led0GpioPin);
            this.Led1 = new Led(nusbio, led1GpioPin);
            this.State = BiColorLedState.Off;
            this.AllOff();
        }

        public void AllOff()
        {
            this.Led0.Low();
            this.Led1.Low();
            this.State = BiColorLedState.Off;
        }

        public void Mixed()
        {
            this.Led0.High();
            this.Led1.High();
            this.State = BiColorLedState.Yellow;
        }

        public BiColorLedState Set(BiColorLedState state, bool incState = false, int firstStateIndex = 0)
        {
            if (incState)
            {
                var ix = this.StateIndex + 1;
                if(ix > MAX_STATE)
                    ix = firstStateIndex;
                this.StateIndex = ix;
                state = this.State;
            }

            switch (state)
            {
                case BiColorLedState.Off   : this.Set(false, false); break;
                case BiColorLedState.Red : this.Set(true , false); break;
                case BiColorLedState.Green : this.Set(false, true);  break;
                case BiColorLedState.Yellow  : this.Set(true , true);  break;
                default                    : this.Set(false, false); break;
            }
           
            return state;
        }

        public void Set(bool led0State, bool led1State)
        {
            Led0.DigitalWrite(led0State);
            Led1.DigitalWrite(led1State);

            if(led0State && led1State)
                this.State = BiColorLedState.Yellow;
            else if(!led0State && !led1State)
                this.State = BiColorLedState.Off;
            else if(led0State && !led1State)
                this.State = BiColorLedState.Red;
            else if(!led0State && led1State)
                this.State = BiColorLedState.Green;
        }

        public void ReverseSet()
        {
            if (Led0.State)
                this.Set(false, true);
            else
                this.Set(true, false);
        }
    }
}

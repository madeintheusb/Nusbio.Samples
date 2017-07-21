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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Components
{
    /// <summary>
    /// SunFlower Relay as reverse HIGH is off, LOW is ON
    /// </summary>
    public class SevenSegmentDirect  
    {
        private Nusbio _nusbio;

        public enum Segments
        {
            A,  B,  C,  D,  E,  F,  G,  DP
        };

        public class GpioInfo
        {
            public NusbioGpio gpio;
            public bool State;

            public override string ToString()
            {
                return string.Format("[{0}] {1}", gpio, State);
            }
        }

        /// <summary>
        /// http://www.electronicshub.org/wp-content/uploads/2015/02/common-cathode-truth-table.jpg
        /// </summary>
        Dictionary<int, List<Segments>> _digitDefinitions = new Dictionary<int, List<Segments>>()
        {
            { 0, new List<Segments>() { Segments.A, Segments.B, Segments.C, Segments.D, Segments.E, Segments.F } },
            { 1, new List<Segments>() { Segments.B, Segments.C } },
            { 2, new List<Segments>() { Segments.A, Segments.B, Segments.D, Segments.E, Segments.G, } },
            { 3, new List<Segments>() { Segments.A, Segments.B, Segments.C, Segments.D, Segments.G, } },
            { 4, new List<Segments>() { Segments.B, Segments.C, Segments.F, Segments.G } },
            { 5, new List<Segments>() { Segments.A, Segments.C, Segments.D, Segments.F, Segments.G } },
            { 6, new List<Segments>() { Segments.A, Segments.C, Segments.D, Segments.E, Segments.F, Segments.G } },
            { 7, new List<Segments>() { Segments.A, Segments.B, Segments.C, } },
            { 8, new List<Segments>() { Segments.A, Segments.B, Segments.C, Segments.D, Segments.E, Segments.F, Segments.G } },
            { 9, new List<Segments>() { Segments.A, Segments.B, Segments.C, Segments.D, Segments.F, Segments.G } },
        };

        public Dictionary<Segments, GpioInfo> SegmentToGpioMap = new Dictionary<Segments, GpioInfo>() {

            { Segments.A,   new GpioInfo { gpio = NusbioGpio.Gpio0  }},
            { Segments.B,   new GpioInfo { gpio = NusbioGpio.Gpio1  }},
            { Segments.C,   new GpioInfo { gpio = NusbioGpio.Gpio2  }},
            { Segments.D,   new GpioInfo { gpio = NusbioGpio.Gpio3  }},
            { Segments.E,   new GpioInfo { gpio = NusbioGpio.Gpio4  }},
            { Segments.F,   new GpioInfo { gpio = NusbioGpio.Gpio5  }},
            { Segments.G,   new GpioInfo { gpio = NusbioGpio.Gpio6  }},
            { Segments.DP,  new GpioInfo { gpio = NusbioGpio.Gpio7  }},
        };
            
        public SevenSegmentDirect(Nusbio nusbio)
        {
            this._nusbio           = nusbio;
            this.AllOff(clearDot: true);
        }

        public void SetSegmentState(Segments s, bool state)
        {
            _nusbio[SegmentToGpioMap[s].gpio].DigitalWrite(state);
            SegmentToGpioMap[s].State = state;
        }

        public void AllOff(bool clearDot = false)
        {
            foreach (var g in SegmentToGpioMap)
            {
                if (g.Key != Segments.DP || clearDot == true)
                {
                    g.Value.State = false;
                    _nusbio[g.Value.gpio].Low();
                }
            }
        }

        public void DrawDigit(int digit)
        {
            this.AllOff();
            if (_digitDefinitions.ContainsKey(digit))
            {                
                var dd = _digitDefinitions[digit];

                foreach(var s in dd) // TODO: Could be optimized with _nusbio.SetGpioMask()
                {
                    this.SetSegmentState(s, true);
                }
            }
            else throw new ArgumentException();
        }

        public void InverseDot()
        {
            SegmentToGpioMap[Segments.DP].State = !SegmentToGpioMap[Segments.DP].State;
            SetSegmentState(Segments.DP, SegmentToGpioMap[Segments.DP].State);
        }
    }
}

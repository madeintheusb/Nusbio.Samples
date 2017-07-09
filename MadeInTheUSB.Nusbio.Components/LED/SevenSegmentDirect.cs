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

        private class GpioInfo
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

        Dictionary<Segments, GpioInfo> _segmentToGpioMap = new Dictionary<Segments, GpioInfo>() {

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

        public void Test0()
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Segments Test", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");
            ConsoleEx.Gotoxy(0, 4);

            var done = false;
            while (!done)
            {
                this.AllOff();
                for(var i=0; i < _segmentToGpioMap.Count; i++)
                {
                    var s = _segmentToGpioMap.Keys.ToList()[i];
                    ConsoleEx.WriteLine(0, 5, string.Format("Segment {0:0}", s.ToString().PadRight(2, ' ')), ConsoleColor.Cyan);
                    SetSegmentState(s, true);
                    if (WaitForKeyQ())
                    {
                        done = true; break;
                    }
                }
            }
        }
        
        private bool WaitForKeyQ()
        {
            Thread.Sleep(200);
            if (Console.KeyAvailable)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                    return true;
            }
            return false;
        }

        public void TestDigit()
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, "Digit Test", ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 2, "Q)uit");
            ConsoleEx.Gotoxy(0, 4);

            this.AllOff();
            var done = false;
            while (!done)
            {
                for (var i = 0; i < _digitDefinitions.Count; i++)
                {
                    ConsoleEx.WriteLine(0, 5, string.Format("Digit {0}", i), ConsoleColor.Blue);
                    this.DrawDigit(i);
                    Thread.Sleep(1000);
                    if (Console.KeyAvailable)
                        if (Console.ReadKey().Key == ConsoleKey.Q)
                        {
                            done = true;
                            break;
                        }
                }
            }
        }

        public void SetSegmentState(Segments s, bool state)
        {
            _nusbio[_segmentToGpioMap[s].gpio].DigitalWrite(state);
            _segmentToGpioMap[s].State = state;
        }

        public void AllOff(bool clearDot = false)
        {
            foreach (var g in _segmentToGpioMap)
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
                foreach(var s in dd)
                {
                    this.SetSegmentState(s, true);
                }
                this.InverseDot();
            }
            else throw new ArgumentException();
        }

        public void InverseDot()
        {
            _segmentToGpioMap[Segments.DP].State = !_segmentToGpioMap[Segments.DP].State;
            SetSegmentState(Segments.DP, _segmentToGpioMap[Segments.DP].State);
        }
    }
}

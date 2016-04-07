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
using MadeInTheUSB.WinUtil;

namespace MadeInTheUSB.Components
{
    /// <summary>
    /// Manager class to control a bi color LED using the MCP4231 digital potentiometer
    /// Note that the green LED is much brighter. One 100 Ohm resistor is used to connect the negative lead of the LED to ground.
    /// Requires class \MadeInTheUSB\Nusbio.Samples\Components\MCP413x.cs.
    /// </summary>
    public class BiColorLedMCP4231Manager : MCP4231
    {
        MCP41X1_Base.ADDRESS Red   = MCP41X1_Base.ADDRESS.POT_0;
        MCP41X1_Base.ADDRESS Green = MCP41X1_Base.ADDRESS.POT_1;
        MCP41X1_Base.ADDRESS None  = MCP41X1_Base.ADDRESS.UNDEFINED;

        // Define for each LED inside the BiColor LED, a list of 9 steps to send to the potentiometer
        // to increase the intensity of the internal LED.
        private Dictionary<MCP41X1_Base.ADDRESS, List<int>> _stepsDefinition = new Dictionary<ADDRESS,List<int>>() { 
            { 
                MCP41X1_Base.ADDRESS.POT_0, // Red
                new List<int>() { 40, 48+2, 56+2, 64, 72, 80, 88, 96, 104, 112, 122 }
            },
            { 
                MCP41X1_Base.ADDRESS.POT_1, // Green
                new List<int>() { 52, 58, 64, 70, 76, 82, 88, 94, 100, 106, 113 }
            },
        };

        public BiColorLedMCP4231Manager(Nusbio nusbio, NusbioGpio selectGpio, NusbioGpio mosiGpio, NusbioGpio misoGpio, NusbioGpio clockGpio, bool debug = false)
        : base(nusbio, selectGpio, mosiGpio, misoGpio, clockGpio, debug)
        {
            base.MaxResistance   = 10000; // 10k Ohm
        }

        public enum AnimationDsl { 
            FadeIn,
            FadeOut,
            FadeInFadeOut,
            Off,
            Max,
            BlinkWithOtherColor,
            Blink,
            HalfMax,
            Pause,
        }

        private bool Animate(MCP41X1_Base.ADDRESS pot, AnimationDsl animationType, int wait, int count = 1)
        {
            var steps = new List<int>();

            if (animationType == AnimationDsl.FadeInFadeOut) { 

                Animate(pot, AnimationDsl.FadeIn, wait);
                Animate(pot, AnimationDsl.Pause, wait);
                Animate(pot, AnimationDsl.FadeOut, wait);
            }
            else if (animationType == AnimationDsl.FadeIn) { 

                steps = _stepsDefinition[pot];
            }
            else if (animationType == AnimationDsl.FadeOut) { 

                steps = new List<int>(_stepsDefinition[pot]);
                steps.Reverse();
            }
            else if (animationType == AnimationDsl.Pause) { 
                
                TimePeriod.Sleep(wait*2);
                return true;
            }
            else if (animationType == AnimationDsl.Off) { 

                this.Set(0, pot);
                return true;
            }
            else if (animationType == AnimationDsl.Max) { 

                steps = _stepsDefinition[pot];
                this.Set(steps[steps.Count-1], pot);
                return true;
            }
            else if (animationType == AnimationDsl.HalfMax) { 

                steps = _stepsDefinition[pot];
                this.Set(steps[steps.Count/2], pot);
                return true;
            }
            else if (animationType == AnimationDsl.Blink) { 

                MCP41X1_Base.ADDRESS Led1 = pot;
                MCP41X1_Base.ADDRESS Led2 = Led1 == Red ? Green : Red;

                for (var c = 0; c < count; c++) { 
                    Animate(Led1, AnimationDsl.Off, wait);
                    TimePeriod.Sleep(wait);
                    Animate(Led1, AnimationDsl.Max, wait);
                    TimePeriod.Sleep(wait);
                }
                return true;
            }
            else if (animationType == AnimationDsl.BlinkWithOtherColor) { 

                MCP41X1_Base.ADDRESS Led1 = pot;
                MCP41X1_Base.ADDRESS Led2 = Led1 == Red ? Green : Red;

                for (var c = 0; c < count; c++) { 
                    Animate(Led1, AnimationDsl.Off, wait);
                    Animate(Led2, AnimationDsl.Max, wait);
                    TimePeriod.Sleep(wait);
                    Animate(Led2, AnimationDsl.Off, wait);
                    Animate(Led1, AnimationDsl.Max, wait);
                    TimePeriod.Sleep(wait);
                }
                return true;
            }
            else throw new ArgumentException();

            foreach (var s in steps)
            {
                if (!this.Set(s, pot).Succeeded) 
                    Console.WriteLine("Communication error");
                TimePeriod.Sleep(wait);
            }
            return true;
        }

        public void Animate()
        {
            int wait = 60;
            Console.Clear();

            Console.WriteLine("Animation 1 -- Fade in Fade out one LED at the time");
            Animate(this.Red  , AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Red  , AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Green, AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Green, AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Red  , AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Green, AnimationDsl.FadeInFadeOut, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.None,  AnimationDsl.Pause, wait*5);

            Console.WriteLine("Animation 2 -- Fade in in red Fade out in green");
            Animate(this.Red  , AnimationDsl.FadeIn, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Green , AnimationDsl.Max, wait);
            Animate(this.Red  , AnimationDsl.Off, wait);
            Animate(this.Green , AnimationDsl.FadeOut, wait);
            Animate(this.Green, AnimationDsl.FadeIn, wait);
            Animate(this.None,  AnimationDsl.Pause, wait);
            Animate(this.Red  , AnimationDsl.Max, wait);
            Animate(this.Green, AnimationDsl.Off, wait);
            Animate(this.Red  , AnimationDsl.FadeOut, wait);

            Console.WriteLine("Animation 2 -- Fade in, Blink wand fade out");
            Animate(this.Red   , AnimationDsl.FadeIn, wait);
            Animate(this.Red   , AnimationDsl.Blink, wait*5, 3);
            Animate(this.Red   , AnimationDsl.FadeOut, wait);
            Animate(this.Green , AnimationDsl.FadeIn, wait);
            Animate(this.Green , AnimationDsl.Blink, wait*5, 3);
            Animate(this.Green , AnimationDsl.FadeOut, wait);
             
            Console.WriteLine("Animation 2 -- Fade in, Blink with other color and fade out");
            Animate(this.Red   , AnimationDsl.FadeIn, wait);
            Animate(this.Red   , AnimationDsl.BlinkWithOtherColor, wait*5, 5);
            Animate(this.Red   , AnimationDsl.FadeOut, wait);
            Animate(this.Green , AnimationDsl.FadeIn, wait);
            Animate(this.Green , AnimationDsl.BlinkWithOtherColor, wait*5, 5);
            Animate(this.Green , AnimationDsl.FadeOut, wait);
        }
    }

}

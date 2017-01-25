/*
   Copyright (C) 2017 MadeInTheUSB LLC
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
    public class JoyStick
    {
        Nusbio _nusbio;
        MCP3008 _adc;

        MadeInTheUSB.Sensor.AnalogSensor XSensor, YSensor, ButSensor;

        public static int ZeroAdValue = 511;
        public static int CalibrationOffset = 4;

        public JoyStick(Nusbio nusbio, MCP3008 adc, int xAdPort, int yAdPort, int buttonAdPort)
        {
            this._nusbio   = nusbio;
            this._adc      = adc;
            this.XSensor   = new Sensor.AnalogSensor(nusbio, xAdPort);
            this.YSensor   = new Sensor.AnalogSensor(nusbio, yAdPort);
            this.ButSensor = new Sensor.AnalogSensor(nusbio, buttonAdPort);
        }

        public string XDir
        {
            get
            {
                if (X == 0) return "";
                if (X < 0) return "Down";
                    return "Up";
            }
        }
        public int X
        {
            get
            {
                this.XSensor.SetAnalogValue(this._adc.Read(this.XSensor.ADPort));
                return Adjust(((int)this.XSensor.AnalogValue)- ZeroAdValue);
            }
        }

        private int Adjust(int v)
        {
            var vv = v / CalibrationOffset;
            return vv * CalibrationOffset;
        }

        public string YDir
        {
            get
            {
                if (Y == 0) return "";
                if (Y < 0) return "Right";
                return "Left";
            }
        }
        public int Y
        {
            get
            {
                this.YSensor.SetAnalogValue(this._adc.Read(this.YSensor.ADPort));
                return Adjust(((int)this.YSensor.AnalogValue)- ZeroAdValue);
            }
        }
        public bool ButtonPressed
        {
            get
            {
                this.ButSensor.SetAnalogValue(this._adc.Read(this.ButSensor.ADPort));
                return this.ButSensor.AnalogValue == 0;
            }
        }
    }
}

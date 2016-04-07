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
using System.Collections.Generic;
using MadeInTheUSB;
using MadeInTheUSB.GPIO;

namespace MadeInTheUSB.Sensor
{
    public class AnalogLightSensor : AnalogSensor
    {

        public enum LightSensorType
        {
            Unknown,
            /// <summary>
            /// 3 x 3 millimeter light sensor 
            /// Light resistance at 10lux 45-145K
            /// Dark resistance at 0 lux/min 5M
            /// </summary>
            CdsPhotoCell_3mm_45k_140k,
            /// <summary>
            /// Adafruit product: https://www.adafruit.com/products/161
            /// Adafruit tutorial:https://learn.adafruit.com/photocells
            /// </summary>
            CdsPhotoCell_5mm_5k_200k,
        }

        private double _value;

        private List<PhotocellResistorCalibration> _calibrationValues = new List<PhotocellResistorCalibration>();

        public string CalibratedValue
        {
            get
            {
                if (_calibrationValues.Count > 0)
                {
                    foreach (var v in _calibrationValues)
                    {
                        if (this._value >= v.StartValue && this._value < v.EndValue)
                            return v.Name;
                    }
                    return "";
                }
                else return this._value.ToString();
            }
        }

        public override void SetAnalogValue(double value)
        {
            base.SetAnalogValue(value);
            this._value = value;
        }
        
        public AnalogLightSensor(Nusbio nusbio) : base(nusbio)
        {
        }

        public AnalogLightSensor AddCalibarationValue(string name, int startValue, int endValue)
        {
            this._calibrationValues.Add(new PhotocellResistorCalibration()
            {
                Name = name, StartValue = startValue, EndValue = endValue
            });
            return this;
        }

        public class PhotocellResistorCalibration
        {
            public string Name;
            public double StartValue, EndValue;
        }
    }
}

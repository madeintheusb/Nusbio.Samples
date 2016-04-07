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
    /// <summary>
    /// How to use a Photocell CDS or light sensor with the Nusbio.
    /// The sensor act as a resistor if there is a lot of light the light sensor will act as a
    /// 10k resistor, if there is no light te light sensor will as a 200k resistor.
    /// If you can read the voltage, you know the lighting.
    /// 
    /// See AdaFruit tutorial for more info on the sensor https://learn.adafruit.com/photocells/overview
    /// 
    /// The Nusbio like the Raspberry PI does not offer Analog to digigal input port unlike the Arduino.
    /// There is no way to read the voltage coming out of the sensor.
    /// But what we can do is use a Capacitor (Reservoir) and count "how long it takes" to fill it.
    /// From that we can define a table of values
    /// 
    /// Wiring:
    /// 
    ///                     [--]       [--]
    ///                     [--]       |  |
    ///    LDEVICE.Gnd -->  |  | <-|-> |  | <-- LDEVICE.Vcc
    ///                     Cap    |   LightSensor
    ///                         LDEVICE.Gpio0 (Input mode)
    ///
    /// Capacitor:
    /// 
    /// We tested with a 47uF capacitor and the CdS photoresistor from Adafruit.
    /// and we noticed that we can only trigger acquisition only every 2 seconds to get a
    /// accurate and stable value. This is mostly due to the fact that the capacitor need to be
    /// fully decharged before we can retry.
    /// Also due to the speed of execution of the code, do not use a smaller capacitor. 47uF to 100uF is advised.
    /// 
    /// </summary>
    public class LightSensorWithCapacitor
    {
        public const int TimeOutMaxValue = 16384;
        public const int ErrorValue = -1;
 
        private NusbioGpio _gpio;
        private Nusbio          _nusbio;
        private TimeOut         _timeOut;

        private int _value = -1;

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

        public int Value
        {
            get
            {
                if (this._timeOut.IsTimeOut() || this._value == -1)
                    AcquireSample();
                return _value;
            }
        }

        private void AcquireSample()
        {
            var error      = false;
            var _tickCount = Environment.TickCount;

            _nusbio.GPIOS[_gpio].DigitalWrite(PinState.Low);
            _nusbio.SetPinMode(_gpio, PinMode.Input);

            while (_nusbio.GPIOS[_gpio].DigitalRead() == PinState.Low)
            {
                _value++;
                if (_value >= TimeOutMaxValue)
                {
                    this._value = ErrorValue;
                    error       = true;
                    break;
                }
            }
            if (!error)
            {
                this._value = Environment.TickCount - _tickCount;
            }
            _nusbio.SetPinMode(_gpio, PinMode.Output);
        }

        public LightSensorWithCapacitor(Nusbio nusbio, NusbioGpio gpio, int sampleFrequencySeconds = 2000)
        {
            _timeOut = new TimeOut(sampleFrequencySeconds);
            this._gpio              = gpio;
            this._nusbio           = nusbio;
        }

        public LightSensorWithCapacitor AddCalibarationValue(string name, int startValue, int endValue)
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
            public int StartValue, EndValue;
        }
    }
}

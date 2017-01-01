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

namespace MadeInTheUSB.Buttons
{
    public class KeypadPressedInfo
    {
        public int Col, Row;
        public char Key;
    }
    /// <summary>
    /// This button class only works with Nusbio gpios and provide extra feature.
    /// For a button that can works with Nusbio gpios and also an gpio expander added to Nusbio see component
    /// GenericButton
    /// </summary>
    public class Keypad
    {
        private List<NusbioGpio> _gpioRow;
        private List<NusbioGpio> _gpioCol;
        private List<List<char>> _keys;
        private Nusbio     _nusbio;
        
        public bool Inverse = false;

        public Keypad(Nusbio nusbio,
            List<NusbioGpio> gpioRow,
            List<NusbioGpio> gpioCol,
            List<List<char>> keys
            )
        {
            this._nusbio = nusbio;
            this._gpioCol = gpioCol;
            this._gpioRow = gpioRow;
            this._keys = keys;
            this.Init();
        }

public KeypadPressedInfo Check()
{
    KeypadPressedInfo rr = null;
    var found            = false;
    for (var c = 0; c < this._gpioCol.Count; c++)
    {
        _nusbio.GPIOS[this._gpioCol[c]].Low();
        for (var r = 0; r < this._gpioRow.Count; r++)
        {
            var pressed = _nusbio.GPIOS[this._gpioRow[r]].DigitalRead() == PinState.Low;
            if (pressed)
            {
                rr = new KeypadPressedInfo {
                    Row = r, Col = c, Key = this._keys[r][c]
                };
                break;
            }
        }
        _nusbio.GPIOS[this._gpioCol[c]].High();
        if (rr!=null)
            break;
    }
    return rr;
}

        private void Init()
        {
            for (var r = 0; r < this._gpioRow.Count; r++)
            {
                this._nusbio.SetPinMode(this._gpioRow[r], PinMode.Input);
            }
            for (var c = 0; c < this._gpioCol.Count; c++)
            {
                this._nusbio.SetPinMode(this._gpioCol[c], PinMode.Output);
                _nusbio.GPIOS[this._gpioCol[c]].High();
            }
        }
    }
}

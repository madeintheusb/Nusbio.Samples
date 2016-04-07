/*
    
   Copyright (C) 2015 MadeInTheUSB LLC
   Ported to C# and Nusbio by FT for MadeInTheUSB

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
 
   Based from:
 
        This is a "Fast SH1106 Library". It is designed to be used with
        128x64 OLED displays, driven by the SH1106 controller.
        This library uses hardware SPI of your Arduino microcontroller,
        and does not supprt 'software SPI' mode.
 
        Written by Arthur Liberman (aka 'The Coolest'). http://www.alcpu.com
        Special thanks goes out to 'robtillaart' for his help with debugging
        and optimization.

        BSD license, check license.txt for more information.
        All text above must be included in any redistribution.
 
   Also based 
       on stanleyhuangyc/MultiLCD
       https://github.com/stanleyhuangyc/MultiLCD/tree/master/MicroLCD
       officeboy/sh1106
       https://github.com/officeboy/sh1106/blob/master/firmware/sh1106.cpp
*/

using System;
using System.Diagnostics;
using MadeInTheUSB;
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.GPIO;
using System.Linq;
using MadeInTheUSB.spi;
using int16_t  = System.Int16; // Nice C# feature allowing to use same Arduino/C type
using uint16_t = System.UInt16;
using uint8_t  = System.Byte;
using size_t   = System.Int16;
using MadeInTheUSB.WinUtil;
using System.Collections.Generic;

namespace MadeInTheUSB.Display
{
    /// <summary>
    /// SSD1306 - https://www.adafruit.com/datasheets/SSD1306.pdf
    /// </summary>
    public class OLED_SH1106 : OLED
    {
        public OLED_SH1106(Nusbio nusbio, int width, int height, NusbioGpio clock, NusbioGpio mosi, NusbioGpio select, NusbioGpio dc, NusbioGpio reset, bool debug = false) :
            base(nusbio, width, height, clock, mosi, @select, dc, reset, OledDriver.SH1106)
        {

        }

        public static OLED Create_128x64_13Inch_DirectlyIntoNusbio(Nusbio nusbio)
        {
            nusbio[NusbioGpio.Gpio7].Low(); // Play the role of GND
            var oledDisplay = new OLED_SH1106(nusbio,
                128, 64,
                clock :NusbioGpio.Gpio4,
                mosi  :NusbioGpio.Gpio5, 
                select:NusbioGpio.Gpio3,
                dc    :NusbioGpio.Gpio2, 
                reset :NusbioGpio.Gpio1,
                debug : !true);
            return oledDisplay;
        }

        public override void Begin(bool invert = false, uint8_t contrast = 128, uint8_t Vpp = 0)
        {
            var externalVCC = false;

            this._spiEngine.Nusbio[this._spiEngine.DCGpio].Low();

            // LCD init section:
            Vpp &= 0x03;

            this.SendCommand(OLED_API_DISPLAYOFF);                    /* AE display off                          */
            this.SendCommand(OLED_API_SH1106_SETLOWCOLUMN);           /* set lower column address             */
            this.SendCommand(OLED_API_SETHIGHCOLUMN);                 /* set higher column address            */
            this.SendCommand(OLED_API_SETSTARTLINE);                  /* set display start line               */
            this.SendCommand(OLED_API_SH1106_PAGE_ADDR);              /* set page address                     */
            this.SendCommand(contrast);                               /* 128                                  */
            this.SendCommand(OLED_API_SH1106_SET_SEGMENT_REMAP);      /* set segment remap                    */
            this.SendCommand((uint8_t) (invert ? 0xA7 : 0xA6));       /* normal / reverse                     */
            this.SendCommand(OLED_API_SETMULTIPLEX, this.Height - 1); /* multiplex ratio                      */

            this.SendCommand(0xAD);                                   /* set charge pump enable               */
            this.SendCommand(0x8B);                                   /* external VCC                         */
            this.SendCommand(0x30 | Vpp);                             /* 0X30---0X33  set VPP   9V liangdu!!!!*/
            this.SendCommand(OLED_API_COMSCANDEC);                    /* Com scan direction                   */
            this.SendCommand(OLED_API_SETDISPLAYOFFSET, 0x00);        /* set display offset - no offset */
            this.SendCommand(OLED_API_SETDISPLAYCLOCKDIV, 0x80);      /* set osc division                     */
            this.SendCommand(OLED_API_SETPRECHARGE);                  /* set pre-charge period                */

            if (externalVCC)
                this.SendCommand(0x22);                               /* 0x22                                 */
            else
                this.SendCommand(0x1F);                               /* 0x22                                 */

            this.SendCommand(OLED_API_SETCOMPINS, 0x10);              /* set COM pins                         */
            this.SendCommand(OLED_API_SETVCOMDETECT, 0x40);           /* set vcomh                            */
            this.SendCommand(OLED_API_DISPLAYON);                     /* display ON                           */

            this.Clear(true);

            // WriteDisplay Optimized: False does not work right first time called
            // when we have pixel. Can't understand why. This seems to fix the problem
            this.Fill(true, false);
            this.Fill(true, false);
            this.Clear(true);
        }
    }
}
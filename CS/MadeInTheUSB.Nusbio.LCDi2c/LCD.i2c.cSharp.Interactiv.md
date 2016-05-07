
#r "C:\DVT\MadeInTheUSB\Nusbio.Samples.TRUNK\Components\bin\MadeInTheUSB.Nusbio.Components.dll"
#r "C:\DVT\MadeInTheUSB\Nusbio.Samples.TRUNK\Components\bin\MadeInTheUSB.Nusbio.Lib.dll"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadeInTheUSB;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.WinUtil;
using MadeInTheUSB.Display;

MadeInTheUSB.Devices.Initialize();

var nusbio = new Nusbio(Nusbio.Detect());
              
var sda        = NusbioGpio.Gpio7; // Directly connected into Nusbio
var scl        = NusbioGpio.Gpio6;
var maxColumn  = 16;
var maxRow     = 2;
var lcdI2cId   = 0x27;
var lcdI2C     = new LiquidCrystal_I2C_PCF8574(nusbio, sda, scl, maxColumn, maxRow, deviceId: lcdI2cId);
lcdI2C.Begin(maxColumn, maxRow);
lcdI2C.Backlight();

lcdI2C.Print(0, 0, "Hi!");



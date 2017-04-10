/*
	http://code.visualstudio.com/docs/?dv=win
	http://dailydotnettips.com/2016/01/14/executing-c-scripts-from-command-line-or-c-interactive-windows-in-visual-studio/
*/



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

Console.WriteLine("Nusbio demo with C# Interactive mode");
Console.WriteLine("Detecting Nusbio USB Device...");

MadeInTheUSB.Devices.Initialize();
var nusbio = new Nusbio(Nusbio.Detect());
var on     = true;
var off    = false;

Console.WriteLine("Ready...");




nusbio.GPIOS[NusbioGpio.Gpio7].DigitalWrite(on);

nusbio.GPIOS[NusbioGpio.Gpio0].DigitalWrite(on);
nusbio.GPIOS[NusbioGpio.Gpio1].DigitalWrite(on);
nusbio.GPIOS[NusbioGpio.Gpio2].DigitalWrite(on);
nusbio.GPIOS[NusbioGpio.Gpio3].DigitalWrite(on);
nusbio.GPIOS[NusbioGpio.Gpio4].DigitalWrite(on);
nusbio.GPIOS[NusbioGpio.Gpio5].DigitalWrite(on);

for(var g = 0; g < 7; g++) nusbio[g].DigitalWrite(off);

for (var t = 0; t < 8; t++)
{
    for (var g = 0; g < 7; g++)
    {
        nusbio[g].DigitalWrite(on);
        Thread.Sleep(64);
    }
    for (var g = 6; g >= 0; g--)
    {
        nusbio[g].DigitalWrite(off);
        Thread.Sleep(64);
    }
}
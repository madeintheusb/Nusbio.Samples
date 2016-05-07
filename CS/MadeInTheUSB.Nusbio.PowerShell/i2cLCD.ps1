<#
	Nusbio Demo Using PowerShell

    Copyright (C) 2015 MadeInTheUSB.net

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
    associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial 
    portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 #>
 
Param(
  [Alias("a")]   [string] $action = "" # edit to edit sourcecode
) 

if(($action.ToLowerInvariant() -eq "sourcecode") -or ($action.ToLowerInvariant() -eq "sc")) {

    powershell_ise.exe "i2cLCD.ps1,..\..\Components\Nusbio.psm1"
    Exit 0
}

function UpdateStatus() {

    Write-Host (Nusbio_GetGpioState $nusbio) -ForegroundColor DarkGreen
}

function Help() {

    Cls
    Write-Host "Nusbio" -ForegroundColor Cyan -NoNewline

    $m = " - SerialNumber:{0}, Description:{1} " -f  $serialNumber, $nusbio.Description
    Write-Host $m -ForegroundColor DarkCyan

    Write-Host "" -ForegroundColor Green

    $m = "C)ear Q)uit"
    Write-Host $m -ForegroundColor Green
}

cls

$nusbio_psm1 = "..\..\Components\Nusbio.psm1"
Import-Module ($nusbio_psm1) -Force

$serialNumber = [MadeInTheUSB.nusbio]::Detect()
if($serialNumber -eq $null) {
    Write-Error "Nusbio not detected"
    Exit 1
}

pUsing ($nusbio = New-Object MadeInTheUSB.Nusbio($serialNumber)) {

	Help
	$key = ""

    $everySecond   = New-Object MadeInTheUSB.TimeOut(1000) 
    $sda           = $Gpio7
    $scl           = $Gpio6
    $maxColumn     = 16
    $maxRow        = 2
    $lcdI2cId      = 0x27
    $lcdI2C        = New-Object MadeInTheUSB.Display.LiquidCrystal_I2C_PCF8574($nusbio, $sda, $scl, $maxColumn, $maxRow, $lcdI2cId)
    
    $lcdI2C.Init(16, 2)
    $lcdI2C.Backlight()
    $lcdI2C.Print(0, 0, "Hi from ")
    $lcdI2C.Print(0, 1, "PowerShell")

	while ($nusbio.Loop()) {

        if($everySecond.IsTimeOut()) {
            "."
            $lcdI2C.Print(0, 0, [System.DateTime]::Now.ToString("T"));
        }
		if([console]::KeyAvailable) {

			$key = [console]::readkey("noecho").Key
			switch -Regex ($key.ToString()) {

				"q" { $nusbio.ExitLoop() }
				"c" { Help }
			}
		}
	}
}
Write-Host "Done"



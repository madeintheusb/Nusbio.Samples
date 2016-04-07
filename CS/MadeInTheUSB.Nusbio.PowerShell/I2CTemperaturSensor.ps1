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

if($action.ToLowerInvariant() -eq "sourcecode") {

    powershell_ise.exe "I2CTemperaturSensor.ps1,..\..\Components\Nusbio.psm1"
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

$nusbio_psm1 = "..\..\Components\Nusbio.psm1"
Import-Module ($nusbio_psm1) -Force

$serialNumber = [MadeInTheUSB.nusbio]::Detect()
if($serialNumber -eq $null) {
    Write-Error "Nusbio not detected"
    Exit 1
}

pUsing ($nusbio = New-Object MadeInTheUSB.Nusbio($serialNumber)) {
	
    $everySecond                = New-Object MadeInTheUSB.TimeOut(1000) 
    $clockPin                   = $Gpio6
    $dataOutPin                 = $Gpio5    
    $_MCP9808_TemperatureSensor = New-Object MadeInTheUSB.MCP9808_TemperatureSensor($nusbio, $dataOutPin, $clockPin)
		
    if (!$_MCP9808_TemperatureSensor.Begin()) {
        Write-Error  "MCP9808 not detected on I2C bus"
        Exit 1
    }

	Help
	$key = ""
    
	while ($nusbio.Loop()) {

        if($everySecond.IsTimeOut()) {

			$celsiusValue = $_MCP9808_TemperatureSensor.GetTemperature($Celsius)
			Write-Host ("Temperature Celsius:{0:00.00}, Fahrenheit:{1:000.00}, Kelvin:{2:00000.00}" -f  $celsiusValue, $_MCP9808_TemperatureSensor.CelsiusToFahrenheit($celsiusValue), $_MCP9808_TemperatureSensor.CelsiusToKelvin($celsiusValue))
				
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


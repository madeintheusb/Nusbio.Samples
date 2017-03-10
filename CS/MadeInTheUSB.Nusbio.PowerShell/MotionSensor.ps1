<#
	Nusbio Demo Using PowerShell

    Copyright (C) 2017 MadeInTheUSB.net

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

cls

if($action.ToLowerInvariant() -eq "sourcecode")
{
    powershell_ise.exe "MotionSensor.ps1"
    Exit 0
}


# Update $MadeInTheUSB_Nusbio_Lib_dll in psm1 module
$nusbio_psm1 = "..\..\Components\Nusbio.psm1"
Import-Module ($nusbio_psm1) -Force

$serialNumber = [MadeInTheUSB.nusbio]::Detect()
if($serialNumber -eq $null) {
    Write-Error "Nusbio not detected"
    Exit 1
}
else {
    Start-Sleep -s 2
    Cls
    "Nusbio serialNumber:$serialNumber detected"
}

$WaitingForMotionMsg = "Waiting for motion..."

pUsing ($nusbio = New-Object MadeInTheUSB.Nusbio($serialNumber)) {
     
    nusbio_AddGpioProperties $nusbio
    $nusbio.SetPinMode($Gpio7, [MadeInTheUSB.GPIO.PinMode]::Input)
    
    "(Q)uit"
    $WaitingForMotionMsg
        
    while ($nusbio.Loop()) {

        $motionDetected =  $nusbio.GetGpio($Gpio7.ToString()).DigitalReadDebounced()
        if($motionDetected -eq $High) 
        {
            "Motion detected $([System.Datetime]::Now)`r`n"
            Start-Sleep -s 6 # PIR motion sensor has a 5 second timeout
            $WaitingForMotionMsg
        }

        if([console]::KeyAvailable) {

            $key = [console]::readkey("noecho").Key
            switch -Regex ($key.ToString()) {

                "q" { $nusbio.ExitLoop() }
            }
        }
    }               
}
Write-Host "Done"


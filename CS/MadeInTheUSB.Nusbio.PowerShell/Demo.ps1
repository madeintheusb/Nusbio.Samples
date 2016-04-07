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

    powershell_ise.exe "Demo.ps1,ReplDemo.ps1,..\..\Components\Nusbio.psm1"
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

    $m = "Gpio: 0) 1) 2) 3) 4) 5) 6) 7) -  C)lear  A)ll off  Q)uit"
    Write-Host $m -ForegroundColor Green
}

# Update $MadeInTheUSB_Nusbio_Lib_dll in psm1 module
$nusbio_psm1 = "..\..\Components\Nusbio.psm1"
Import-Module ($nusbio_psm1) -Force

$serialNumber = [MadeInTheUSB.nusbio]::Detect()
if($serialNumber -eq $null) {
    Write-Error "Nusbio not detected"
    Exit 1
}

pUsing ($nusbio = New-Object MadeInTheUSB.Nusbio($serialNumber)) {
    
    nusbio_AddGpioProperties $nusbio
    nusbio_RegisterWebServerUrlEvent $nusbio { param($s) Write-Host ("HTTP:" + $s) }
    Help
    $key = ""
    
    while ($nusbio.Loop()) {

        if([console]::KeyAvailable) {

            $key = [console]::readkey("noecho").Key
            #"key $key"

            switch -Regex ($key.ToString()) {

                "q" { $nusbio.ExitLoop() }
                "c" { Help }
                "a" {  
                        $nusbio.SetAllGpioOutputState($Low) 
                        UpdateStatus
                    }
                # 1"w" { [System.Diagnostics.Process]::Start($nusbio.GetWebServerUrl()) }
                "\d" {
                    $gpioIndex = 0 + $key.ToString().Substring(1) # Parse as int
                    if($gpioIndex -ge 0 -and $gpioIndex -lt 8) {
                        $nusbio.GetGpio("Gpio" + $gpioIndex).ReverseSet() | Out-Null
                        UpdateStatus
                    }
                }
            }
        }
    }               
}
Write-Host "Done"


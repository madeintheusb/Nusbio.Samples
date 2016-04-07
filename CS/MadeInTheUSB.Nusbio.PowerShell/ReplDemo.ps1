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
  [Alias("o")]   [string] $operation = "" # edit to edit sourcecode
) 

if($operation.ToLowerInvariant() -eq "sourcecode") {

    powershell_ise.exe "ReplDemo.ps1,..\..\Components\Nusbio.psm1"
    Exit 0
}

$Nusbio_psm1 = "..\..\Components\Nusbio.psm1"
Import-Module ($Nusbio_psm1) -Force

"Nusbio Initializing"
$serialNumber = [MadeInTheUSB.Nusbio]::Detect()
if($serialNumber -eq $null) {
    Write-Error "Nusbio not detected"
    Exit 1
}

Function prompt {"PS>"}

$global:nusbio = New-Object MadeInTheUSB.Nusbio($serialNumber, 0, $NUSBIO_WEB_SERVER_LISTENING_PORT)
Nusbio_RegisterWebServerUrlEvent $global:nusbio { param($s) Write-Host ("HTTP:" + $s) }
Nusbio_Help

<#

Function prompt {"PS>"}

$indexes = 0..7


 foreach($x in $indexes) { $nusbio[$x].High() }
 foreach($x in 0,2,4,6) { $nusbio[$x].ReverseSet() | out-null }
 foreach($x in $indexes) { $nusbio[$x].ReverseSet() | out-null }
 foreach($i in 0..49) { 
     foreach($x in $indexes) { 
         $nusbio[$x].ReverseSet() | out-null 
     } 
     Start-Sleep -m 150 
 }

#>




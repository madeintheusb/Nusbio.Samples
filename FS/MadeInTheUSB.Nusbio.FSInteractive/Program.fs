// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open System
open System.Threading
#r @"D:\DVT\MadeInTheUSB\Nusbio.Samples\Components\bin\MadeInTheUSB.Nusbio.Lib.dll"
open MadeInTheUSB
MadeInTheUSB.Devices.Initialize();
let serialNumber = Nusbio.Detect()
let nusbio = new Nusbio(serialNumber)


[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    let sampleInteger = 176
    
    

    0 // return an integer exit code

//
// Turning on and off LEDs with Nusbio and F#
//
open System
open System.Threading
#r @"D:\DVT\MadeInTheUSB\Nusbio.Samples\Components\bin\MadeInTheUSB.Nusbio.Lib.dll"
open MadeInTheUSB
MadeInTheUSB.Devices.Initialize();

module NusbioInteractive = 
    let serialNumber = Nusbio.Detect()
    let nusbio       = new Nusbio(serialNumber)
    let gpios        = [0..7]
    
    gpios |> List.iter(fun(x) -> nusbio.GetGpio(x).High())

    gpios |> List.iter(fun(x) -> nusbio.GetGpio(x).Low())
    
    [0..2..7] |> List.iter(fun(x) -> nusbio.GetGpio(x).High())
    
    gpios |> List.iter(fun(x) -> nusbio.GetGpio(x).AsLed.ReverseSet() |> ignore)
       
    
    let wait = 250
    let leds = [ (0,4); (1,5); (2,6); (3,7); (2,6); (1,5);] // 

    let blinkTuple (l1 : int, l2 : int, on : bool, wait : int) =
        nusbio.GetGpio(l1).DigitalWrite(on)
        nusbio.GetGpio(l2).DigitalWrite(on)
        Thread.Sleep(wait)


    leds |> List.iter(fun((l1,l2)) -> 
            blinkTuple(l1, l2, true , wait)
            blinkTuple(l1, l2, false, wait)
        )
        

    [0..5] |> List.map(fun(x) -> 
        leds |> List.iter(fun((l1,l2)) -> 
            blinkTuple(l1, l2, true, wait)
            blinkTuple(l1, l2, false, wait)
        )
    )

    nusbio.Close()





























    [0..7] |> List.iter(fun(x) -> Console.WriteLine(x))

    let gpios = [0..7] |> List.map(nusbio.GetGpio)
    gpios |> Seq.iter(fun(g) -> g.High())

    [0..7] |> List.iter(fun(x) -> Console.WriteLine(x))
    [0..7] |> List.map(Console.WriteLine)
    
    blinkInPair(1)
        
    
    leds |> Seq.iter(fun(x) -> Console.WriteLine(x.[0]) )

    let people = [ ("Joe", 55); ("John", 32); ("Jane", 24); ("Jimmy", 42) ];;
    [for (name, age) in people when age < 30 -> name ];;
    [for (name, age) in people -> name ];;



    [for l in leds -> [ for ll in l -> Console.WriteLine(l)]]

    leds |> Seq.iter(fun(x) -> printfn )
    

     (fst >> String.uppercase) ("Hello world", 123);;


    nusbio.Close()


    let concat (x : string) y = x + y;;
    concat "Hello, " "World!";;


    let square x = x * x
    let numbers = [1 .. 10]
    let squares = List.map square numbers
    let squares = numbers |> List.map(Console.WriteLine)
    printfn "N^2 = %A" squares


    List.iter (printfn "%i") [ 0..7 ]

    let n = 1000
    let arr = Array.init n (fun _ -> n)
    
    let rec buildList n acc i = if i = n then acc else buildList n (0::acc) (i + 1)
    let lst = buildList n [] 0

    lst |> Seq.iter(fun(x) -> Console.WriteLine(x))
    
    let doNothing x =
        printfn (x.ToString())

    let incr x = x + 1

    #time

    arr |> Array.iter doNothing         // this takes 14ms
    arr |> Seq.iter doNothing           // this takes 74ms


    lst |> List.iter doNothing          // this takes 19ms
    lst |> Seq.iter doNothing           // this takes 88ms

//    let blinkAllLed(nusbio : Nusbio, wait : int) = 
//        let gpioIndexes  = [0..7]
//        [for i in gpioIndexes -> nusbio.GetGpio(i).High()]
//        Thread.Sleep(wait)
//        [for i in gpioIndexes -> nusbio.GetGpio(i).Low()]
//        Thread.Sleep(wait)
        
        // http://dungpa.github.io/fsharp-cheatsheet/
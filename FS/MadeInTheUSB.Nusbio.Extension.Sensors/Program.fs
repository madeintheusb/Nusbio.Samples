open System
open System.Threading
open MadeInTheUSB
open MadeInTheUSB.Sensor
open MadeInTheUSB.GPIO

let Show(x) =
    Console.WriteLine(sprintf "%40A" x)

let Pause(unit) =
    Console.ReadKey()

let CalibrateLightSensor(lightSensor : AnalogLightSensor) =
    lightSensor.AddCalibarationValue("Dark"             , 0, 119)
    lightSensor.AddCalibarationValue("Office Night"     , 120, 160)
    lightSensor.AddCalibarationValue("Office Day"       , 161, 200)
    lightSensor.AddCalibarationValue("Outdoor Sun Light", 201, 1024)
    lightSensor

let Cls(nusbio : Nusbio) = 
    Console.Clear()
    ConsoleEx.TitleBar(0, "Nusbio - F# - Sensor Extension", ConsoleColor.Yellow, ConsoleColor.DarkBlue)
    ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
    ConsoleEx.WriteMenu(-1, 4, "Q)uit");

let Demo(unit) =

    let serialNumber = Nusbio.Detect()
    use nusbio = new Nusbio(serialNumber)
    Cls(nusbio)
    
    let ledGpio = NusbioGpio.Gpio5 // The LED on the sensor extension is linked to gpio 5
    
    // Mcp3008 8 analog to digital converter - SPI Config
    let ad = MCP3008(nusbio, selectGpio = NusbioGpio.Gpio3, mosiGpio = NusbioGpio.Gpio1, misoGpio = NusbioGpio.Gpio2, clockGpio = NusbioGpio.Gpio0)
    ad.Begin()
    let motionSensorAnalogPort      = 0
    let buttonSensorAnalogPort      = 1
    let lightSensorAnalogPort       = 2

    let analogMotionSensor = AnalogMotionSensor(nusbio, 3)
    analogMotionSensor.Begin()

    let button = AnalogButton(nusbio);

    let lightSensor = CalibrateLightSensor(AnalogLightSensor(nusbio))
    lightSensor.Begin()

    // TC77 Temperature Sensor SPI
    let tc77 = TC77(nusbio, clockGpio = NusbioGpio.Gpio0, mosiGpio = NusbioGpio.Gpio1, misoGpio = NusbioGpio.Gpio2, selectGpio = NusbioGpio.Gpio4)
    tc77.Begin()

    let halfSeconds = TimeOut(500)

    while nusbio.Loop() do

        if (halfSeconds.IsTimeOut()) then
            if nusbio.GetGpio(5).State then nusbio.GetGpio(5).Low() else nusbio.GetGpio(5).High()

        ConsoleEx.WriteLine(0, 2, String.Format("{0,-15}", DateTime.Now), ConsoleColor.Cyan);
        
        lightSensor.SetAnalogValue(ad.Read(lightSensorAnalogPort))
        ConsoleEx.WriteLine(0, 4, String.Format("Light Sensor       : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})       ", 
            lightSensor.CalibratedValue.PadRight(18), lightSensor.AnalogValue, lightSensor.Voltage), ConsoleColor.Cyan)

        analogMotionSensor.SetAnalogValue(ad.Read(motionSensorAnalogPort))
        let motionType = analogMotionSensor.MotionDetected()
        if (motionType = DigitalMotionSensorPIR.MotionDetectedType.MotionDetected || motionType = DigitalMotionSensorPIR.MotionDetectedType.None) then
            ConsoleEx.Write(0, 6, String.Format("Motion Sensor      : {0,-18} (ADValue:{1:000.000}, Volt:{2:0.00})    ", motionType, analogMotionSensor.AnalogValue, analogMotionSensor.Voltage), ConsoleColor.Cyan)

        ConsoleEx.WriteLine(0, 8, String.Format("Temperature Sensor : {0:0.00}C {1:0.00}F    ",tc77.GetTemperature(), tc77.GetTemperature(AnalogTemperatureSensor.TemperatureType.Fahrenheit)), ConsoleColor.Cyan)

        button.SetAnalogValue(ad.Read(buttonSensorAnalogPort))
        ConsoleEx.WriteLine(0, 10, String.Format("Button             : {0,-18} [{1:0000}, {2:0.00}V]   ",  
            (if button.Down then "Down" else "Up"), button.AnalogValue, button.Voltage), ConsoleColor.Cyan)
            
        if (Console.KeyAvailable) then
            let k = Console.ReadKey(true).Key
            if (k = ConsoleKey.Q) then
                nusbio.ExitLoop()
    Pause()

[<EntryPoint>]
let main argv = 
    MadeInTheUSB.Devices.Initialize();
    Demo()
    0 // return an integer exit code


 
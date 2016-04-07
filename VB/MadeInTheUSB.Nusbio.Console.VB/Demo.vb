'
'    Copyright (C) 2015 MadeInTheUSB LLC
'
'    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
'    associated documentation files (the "Software"), to deal in the Software without restriction, 
'    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
'    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
'    furnished to do so, subject to the following conditions:
'
'    The above copyright notice and this permission notice shall be included in all copies or substantial 
'    portions of the Software.
'
'    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
'    LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
'    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
'    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
'    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
'

Imports System.Reflection
Imports System.Drawing
Imports DynamicSugar
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.Components.APA
Imports MadeInTheUSB.WinUtil
Imports System.Text
Imports System.Threading
' http://converter.telerik.com/

Module Demo

    Function GetAssemblyProduct() As String
        Return My.Application.Info.ProductName
    End Function
    
    Private Sub AnimateBlocking2(nusbio As Nusbio)

        Dim maxGpio = 8

        Dim gpiosSequence = DS.List(NusbioGpio.Gpio0, NusbioGpio.Gpio1, NusbioGpio.Gpio3, NusbioGpio.Gpio2, NusbioGpio.Gpio4, NusbioGpio.Gpio5, _
            NusbioGpio.Gpio7, NusbioGpio.Gpio6)

        Dim max As Integer = 36
        Dim min As Integer = 4
        Dim [step] As Integer = 2
        Dim delay As Integer = min
        Dim [on] As Boolean = True

        While True

            If Console.KeyAvailable Then
                Dim k = Console.ReadKey(True)
                Exit While
            End If

            For i = 0 To maxGpio - 1

                nusbio(gpiosSequence(i)).DigitalWrite(PinState.High)
                TimePeriod.Sleep(delay)
                nusbio(gpiosSequence(i)).DigitalWrite(PinState.Low)
                TimePeriod.Sleep(delay)
                delay += If([on], ([step]), (-[step]))
                If delay > max Then
                    [on] = False
                End If
                If delay < min Then
                    [on] = True
                End If
                If delay < 0 Then
                    delay = 0

                End If
            Next
        End While
    End Sub

    Private Function AnimateNonBlocking(nusbio As Nusbio) As Boolean

        If nusbio.IsAsynchronousSequencerOn Then
            nusbio.CancelAsynchronousSequencer()
            Return False
        Else
            nusbio.StartAsynchronousSequencer(200, seq:=DS.List(NusbioGpio.Gpio0, NusbioGpio.Gpio1, NusbioGpio.Gpio2, NusbioGpio.Gpio3, NusbioGpio.Gpio4, NusbioGpio.Gpio5, _
                NusbioGpio.Gpio6, NusbioGpio.Gpio7, NusbioGpio.Gpio7, NusbioGpio.Gpio6, NusbioGpio.Gpio5, NusbioGpio.Gpio4, _
                NusbioGpio.Gpio3, NusbioGpio.Gpio2, NusbioGpio.Gpio1, NusbioGpio.Gpio0))
            Return True
        End If
    End Function

    Private Sub ReverseGpioLed3State(led As NusbioGpio, nusbio As Nusbio)

        If nusbio.GPIOS(led).AsLed.ExecutionMode = ExecutionModeEnum.Blinking Then
            nusbio.GPIOS(led).AsLed.SetBlinkModeOff()
        Else
            nusbio.GPIOS(led).AsLed.ReverseSet()
        End If
    End Sub

    Private Sub AnimateBlocking(nusbio As Nusbio)

        Dim maxRepeat = 3
        Dim maxGpio = 8
        Dim wait = 300

        For i = 0 To maxRepeat - 1
            For g = 0 To maxGpio - 1
                nusbio(g).DigitalWrite(PinState.High)
            Next
            Thread.Sleep(wait)
            For g = 0 To maxGpio - 1
                nusbio(g).DigitalWrite(PinState.Low)
            Next
            Thread.Sleep(wait)
        Next
        For i = 0 To maxRepeat - 1
            For g = 0 To maxGpio - 1
                nusbio(g).AsLed.[Set](True)
            Next
            Thread.Sleep(wait)
            For g = 0 To maxGpio - 1
                nusbio(g).AsLed.[Set](False)
            Next
            Thread.Sleep(wait)
        Next
    End Sub

    Private Sub ReverseLed3State(led As NusbioGpio, nusbio As Nusbio)

        If nusbio.GPIOS(led).AsLed.ExecutionMode = ExecutionModeEnum.Blinking Then
            nusbio.GPIOS(led).AsLed.SetBlinkModeOff()
        Else
            nusbio.GPIOS(led).AsLed.ReverseSet()
        End If
    End Sub

    Private Sub ReverseGpio(gpio As NusbioGpio, nusbio As Nusbio)

        If nusbio.GPIOS(gpio).Mode = PinMode.Output Then
            nusbio.GPIOS(gpio).State = Not nusbio.GPIOS(gpio).State
            nusbio.GPIOS(gpio).DigitalWrite(If(nusbio.GPIOS(gpio).State, PinState.High, PinState.Low))
        End If
    End Sub

    Private Sub ShowNusbioState(nusbio As Nusbio)

        Dim b = New StringBuilder(100)
        b.AppendFormat("Gpios ")

        For Each g In nusbio.GPIOS
            If g.Value.AsLed.ExecutionMode = ExecutionModeEnum.Blinking Then
                b.AppendFormat("{0}:Blinking, ", g.Value.Name)
            Else
                b.AppendFormat("{0}:{1}, ", g.Value.Name.Substring(4), If(g.Value.State, "High", "Low"))
            End If
        Next
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 7, b.ToString().RemoveLastChar().RemoveLastChar(), ConsoleColor.Cyan, ConsoleColor.DarkCyan)

        Dim maskString = "Gpios Mask:{0} - {1}".FormatString(nusbio.GetGpioMask().ToString("000"), nusbio.GetGpioMaskAsBinary())
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 8, maskString, ConsoleColor.Cyan, ConsoleColor.DarkCyan)

        ConsoleEx.Gotoxy(0, 24)
    End Sub


    Private Sub NusbioUrlEvent(message As String)

        ConsoleEx.Bar(0, 15, "Http Request:{0}".FormatString(message), ConsoleColor.Cyan, ConsoleColor.DarkCyan)
    End Sub

    Private Sub Cls(nusbio As Nusbio)

        Console.Clear()

        ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)

        ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", Nusbio.SerialNumber, Nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 4, "Web Server listening at {0}".FormatString(Nusbio.GetWebServerUrl()), ConsoleColor.Black, ConsoleColor.DarkCyan)
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 5, "Machine Time Capabilities {0}".FormatString(TimePeriod.GetTimeCapabilityInfo()), ConsoleColor.Black, ConsoleColor.DarkCyan)

        ShowNusbioState(Nusbio)
        NusbioUrlEvent("")

        ConsoleEx.WriteMenu(-1, 2, "Gpios: 0) 1) 2) 3) 4) 5) 6) 7) [Shift:Blink Mode]")
        ConsoleEx.WriteMenu(-1, 4, "F1) Blocking Animation  F2) Non Blocking Animation  F3) Blocking Animation 2")
        ConsoleEx.WriteMenu(-1, 6, "Q)uit  A)ll off  W)eb UI")
    End Sub


    Public Sub Run()

        Console.WriteLine("Nusbio Initializing")
        Dim serialNumber = Nusbio.Detect()
        If serialNumber Is Nothing Then
            ' Detect the first Nusbio available
            Console.WriteLine("Nusbio not detected")
            Return
        End If

        Dim halfSecondTimeOut = New TimeOut(500)

        Using nusbioD = New Nusbio(serialNumber:=serialNumber, webServerPort:=1964)

            AddHandler nusbioD.UrlEvent, AddressOf NusbioUrlEvent

            Cls(nusbioD)
            While nusbioD.Loop()
                If Console.KeyAvailable Then
                    Dim kk = Console.ReadKey(True)
                    Dim blinkMode = kk.Modifiers = ConsoleModifiers.Shift
                    Dim key = kk.Key

                    If key = ConsoleKey.Q Then
                        Exit While
                    End If
                    If key = ConsoleKey.C Then
                        Cls(nusbioD)
                    End If

                    If nusbioD.IsAsynchronousSequencerOn Then
                        ' If background sequencer for animation is on then turn it off if we receive any key
                        nusbioD.CancelAsynchronousSequencer()
                        Continue While
                    End If

                    If blinkMode Then
                        If key = ConsoleKey.D0 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio0).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D1 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio1).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D2 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio2).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D3 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio3).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D4 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio4).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D5 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio5).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D6 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio6).AsLed.SetBlinkMode(1000, 80)
                        End If
                        If key = ConsoleKey.D7 Then
                            nusbioD.GPIOS(NusbioGpio.Gpio7).AsLed.SetBlinkMode(1000, 80)
                        End If
                    Else
                        If key = ConsoleKey.F1 Then
                            AnimateBlocking(nusbioD)
                        End If
                        If key = ConsoleKey.F2 Then
                            AnimateNonBlocking(nusbioD)
                        End If
                        If key = ConsoleKey.F3 Then
                            AnimateBlocking2(nusbioD)
                        End If

                        If key = ConsoleKey.D0 Then
                            ReverseGpio(NusbioGpio.Gpio0, nusbioD)
                        End If
                        If key = ConsoleKey.D1 Then
                            ReverseGpio(NusbioGpio.Gpio1, nusbioD)
                        End If
                        If key = ConsoleKey.D2 Then
                            ReverseGpio(NusbioGpio.Gpio2, nusbioD)
                        End If
                        If key = ConsoleKey.D3 Then
                            ReverseGpio(NusbioGpio.Gpio3, nusbioD)
                        End If
                        If key = ConsoleKey.D4 Then
                            ReverseGpio(NusbioGpio.Gpio4, nusbioD)
                        End If
                        If key = ConsoleKey.D5 Then
                            ReverseGpio(NusbioGpio.Gpio5, nusbioD)
                        End If
                        If key = ConsoleKey.D6 Then
                            ReverseGpio(NusbioGpio.Gpio6, nusbioD)
                        End If
                        If key = ConsoleKey.D7 Then
                            ReverseGpio(NusbioGpio.Gpio7, nusbioD)
                        End If

                        If key = ConsoleKey.A Then
                            nusbioD.SetAllGpioOutputState(PinState.Low)
                        End If
                        If key = ConsoleKey.W Then
                            System.Diagnostics.Process.Start(nusbioD.GetWebServerUrl())
                        End If
                    End If
                    ShowNusbioState(nusbioD)
                Else
                    If halfSecondTimeOut.IsTimeOut() Then
                        ShowNusbioState(nusbioD)
                    End If
                End If
            End While
        End Using
        Console.Clear()
    End Sub


End Module

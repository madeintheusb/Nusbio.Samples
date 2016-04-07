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
Imports MadeInTheUSB.Components
Imports MadeInTheUSB.Components.APA
' http://converter.telerik.com/

Module Demo

    Function GetAssemblyProduct() As String
        Return My.Application.Info.ProductName
    End Function
    
    Sub Cls(nusbio As Nusbio)

        Console.Clear()
        ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 5, "R)ainbow Demo   B)rightness Demo   S)equence Demo   RainboW) Demo + 4 LED")
        ConsoleEx.WriteMenu(-1, 9, "Q)uit")
        ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", Nusbio.SerialNumber, Nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
    End Sub


    Private TargetColors As String = "Blue" & vbCr & vbLf & "BlueViolet" & vbCr & vbLf & "Brown" & vbCr & vbLf & "Chartreuse" & vbCr & vbLf & "Chocolate" & vbCr & vbLf & "CornflowerBlue" & vbCr & vbLf & "Crimson" & vbCr & vbLf & "Cyan" & vbCr & vbLf & "DarkOrange" & vbCr & vbLf & "DarkOrchid" & vbCr & vbLf & "DarkRed" & vbCr & vbLf & "DarkTurquoise" & vbCr & vbLf & "DarkViolet" & vbCr & vbLf & "DarkBlue" & vbCr & vbLf & "DarkCyan" & vbCr & vbLf & "DarkGoldenrod" & vbCr & vbLf & "DarkGreen" & vbCr & vbLf & "DarkMagenta" & vbCr & vbLf & "DeepPink" & vbCr & vbLf & "DeepSkyBlue" & vbCr & vbLf & "DodgerBlue" & vbCr & vbLf & "Firebrick" & vbCr & vbLf & "ForestGreen" & vbCr & vbLf & "Fuchsia" & vbCr & vbLf & "Gold" & vbCr & vbLf & "Green" & vbCr & vbLf & "Indigo" & vbCr & vbLf & "LawnGreen" & vbCr & vbLf & "LightSeaGreen" & vbCr & vbLf & "Lime" & vbCr & vbLf & "Maroon" & vbCr & vbLf & "MediumBlue" & vbCr & vbLf & "MediumSpringGreen" & vbCr & vbLf & "MediumVioletRed" & vbCr & vbLf & "MidnightBlue" & vbCr & vbLf & "Navy" & vbCr & vbLf & "Olive" & vbCr & vbLf & "Orange" & vbCr & vbLf & "OrangeRed" & vbCr & vbLf & "Purple" & vbCr & vbLf & "Red" & vbCr & vbLf & "RoyalBlue" & vbCr & vbLf & "SeaGreen" & vbCr & vbLf & "SpringGreen" & vbCr & vbLf & "Teal" & vbCr & vbLf & "Turquoise" & vbCr & vbLf & "Yellow" & vbCr & vbLf


    Public Sub BrigthnessDemo(ledStrip0 As APA102LEDStrip, ledStrip1 As APA102LEDStrip)

        Console.Clear()
        ConsoleEx.TitleBar(0, "Brightness Demo", ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 3, "Q)uit")

        ledStrip0.AllOff()
        ledStrip1.AllOff()
        Dim bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(","c).ToList()
        Dim wait = 15
        Dim b = 0

        While Not Console.KeyAvailable

            For Each sBColor In bkColors

                Dim bkColor = APA102LEDStrip.DrawingColors(sBColor)

                For b = 1 To APA102LEDStrip.MAX_BRIGHTNESS Step 2
                    ConsoleEx.Write(1, 2, String.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan)
                    ledStrip0.SetColor(b, bkColor).Show()
                    ledStrip1.SetColor(b, bkColor).Show().Wait(wait)
                Next

                If Console.KeyAvailable Then
                    Exit For
                End If
                ledStrip0.Wait(wait * 7) ' Wait when the fade in is done

                For b = APA102LEDStrip.MAX_BRIGHTNESS To 0 Step -2
                    ConsoleEx.Write(1, 2, String.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan)
                    ledStrip0.SetColor(b, bkColor).Show()
                    ledStrip1.SetColor(b, bkColor).Show().Wait(wait)
                Next

                If Console.KeyAvailable Then
                    Exit For
                End If
                ledStrip0.Wait(wait * 10) ' Wait when the fade out is done
            Next
        End While
        ledStrip0.AllOff()
        ledStrip1.AllOff()
        Dim k = Console.ReadKey(True).Key
    End Sub

    Public Sub ColorsSequence(ledStripe0 As APA102LEDStrip, ledStripe1 As APA102LEDStrip)

        Dim wait = 200
        Dim quit = False
        ledStripe0.Brightness = 7
        ledStripe0.AllOff()

        Console.Clear()
        ConsoleEx.TitleBar(0, "Color Sequence Demo", ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 4, "Q)uit")

        Dim bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(","c).ToList()

        While Not quit
            For Each sBColor In bkColors
                If String.IsNullOrEmpty(sBColor.Trim()) Then
                    Continue For
                End If

                Dim bkColor = APA102LEDStrip.DrawingColors(sBColor)
                ConsoleEx.Gotoxy(1, 2)
                ConsoleEx.WriteLine(String.Format("Background Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan)

                ledStripe0.Reset().AddRGBSequence(True, ledStripe0.Brightness, ledStripe0.MaxLed, bkColor).Show()
                ledStripe1.Reset().AddRGBSequence(True, ledStripe1.Brightness, ledStripe0.MaxLed, bkColor).Show().Wait(wait)

                If Console.KeyAvailable Then
                    quit = True
                    Exit For
                End If
            Next
        End While
        ledStripe0.AllOff()
        Dim k = Console.ReadKey(True).Key
    End Sub

    Sub RainbowDemo(ledStripe As APA102LEDStrip, jStep As Integer, Optional ledStripe2 As APA102LEDStrip = Nothing)

        Console.Clear()
        ConsoleEx.TitleBar(0, "Rainbow Demo", ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 4, "Q)uit")

        Dim wait = 10
        Dim quit = False
        Dim j = 0, i = 0, j2 = 0
        Dim maxStep = 256
        ledStripe.AllOff()
        ledStripe2.AllOff()

        While (Not quit)

            For j = 0 To maxStep Step jStep

                ledStripe.Reset()
                ledStripe2.Reset()

                For i = 0 To ledStripe.MaxLed
                    ledStripe.AddRGBSequence(False, 10, RGBHelper.Wheel(CInt(((i * 256 / ledStripe.MaxLed) + j))))
                Next
                For i = 0 To ledStripe2.MaxLed
                    ledStripe2.AddRGBSequence(False, 10, RGBHelper.Wheel(CInt(((i * 256 / ledStripe.MaxLed) + j))))
                Next
                For Each bkColor In ledStripe.LedColors
                    ConsoleEx.Gotoxy(1, 2)
                    ConsoleEx.WriteLine(String.Format("Color:{0}, Html:{1}, Dec:{2}", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan)
                Next

                ledStripe.Show()
                ledStripe2.Show().Wait(wait)

                If (Console.KeyAvailable) Then
                    Dim k = Console.ReadKey(True).Key
                    If (k = ConsoleKey.Q) Then
                        quit = True
                        Exit While
                    End If
                End If
            Next
        End While
        ledStripe.AllOff()
        ledStripe2.AllOff()
    End Sub

    Public Sub RgbRainbowPlusLedOnGpio0To3Demo(nusbio As Nusbio, ledStripe As APA102LEDStrip, jStep As Integer, Optional ledStripe2 As APA102LEDStrip = Nothing)
        Console.Clear()
        ConsoleEx.TitleBar(0, "Rainbow Demo", ConsoleColor.White, ConsoleColor.DarkBlue)
        ConsoleEx.WriteMenu(-1, 4, "Q)uit")

        ' Allow to loop through the 4 available gpios on the RgbLedAdpater
        ' and turn on and off 4 leds connected to gpio 0 to 3
        Dim availableGpiosOnRgbLedAdapters = New List(Of NusbioGpio)() From { _
            NusbioGpio.Gpio0, _
            NusbioGpio.Gpio1, _
            NusbioGpio.Gpio2, _
            NusbioGpio.Gpio3 _
        }
        Dim availableGpiosOnRgbLedAdaptersIndex = 0

        Const wait As Integer = 10
        Dim quit = False
        Const rgbLedIntensisty As Integer = 30
        Dim i = 0
        ledStripe.AllOff()
        ledStripe2.AllOff()

        While Not quit
            nusbio(availableGpiosOnRgbLedAdapters(availableGpiosOnRgbLedAdaptersIndex)).AsLed.[Set](True)

            ' Turn on/off one of the 4 standard one color led connected to gpio 0..3
            If availableGpiosOnRgbLedAdaptersIndex > 0 Then
                ' Turn off the current led
                nusbio(availableGpiosOnRgbLedAdapters(availableGpiosOnRgbLedAdaptersIndex - 1)).AsLed.[Set](False)
            ElseIf availableGpiosOnRgbLedAdaptersIndex = 0 Then
                nusbio(availableGpiosOnRgbLedAdapters(availableGpiosOnRgbLedAdapters.Count - 1)).AsLed.[Set](False)
            End If

            availableGpiosOnRgbLedAdaptersIndex += 1
            If availableGpiosOnRgbLedAdaptersIndex = availableGpiosOnRgbLedAdapters.Count Then
                availableGpiosOnRgbLedAdaptersIndex = 0
            End If

            ' Animate the 2 RGB LED connected to gpio 4,5 and 6,7 usin SPI like protocol
            Dim j = 0

            While j < 256

                ledStripe.Reset()
                ledStripe2.Reset()

                For i = 0 To ledStripe.MaxLed - 1
                    ledStripe.AddRGBSequence(False, rgbLedIntensisty, RGBHelper.Wheel(((i * 256 / ledStripe.MaxLed) + j)))
                Next
                For i = 0 To ledStripe2.MaxLed - 1
                    ledStripe2.AddRGBSequence(False, rgbLedIntensisty, RGBHelper.Wheel(((i * 256 / ledStripe.MaxLed) + j)))
                Next

                For Each bkColor In ledStripe.LedColors
                    ConsoleEx.Gotoxy(1, 2)
                    ConsoleEx.WriteLine([String].Format("Color:{0}, Html:{1}, Dec:{2}", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)), ConsoleColor.DarkCyan)
                Next

                ledStripe.Show()
                ledStripe2.Show().Wait(wait)

                If Console.KeyAvailable Then
                    Dim k = Console.ReadKey(True).Key
                    If k = ConsoleKey.Q Then
                        quit = True
                        Exit While
                    End If
                End If
                j += jStep
            End While
        End While
        ledStripe.AllOff()
        ledStripe2.AllOff()
    End Sub

    Public Sub Run()

        Console.WriteLine("Nusbio initialization")
        Dim serialNumber As String = Nusbio.Detect()
        If (serialNumber Is Nothing) Then ' Detect the first Nusbio available

            Console.WriteLine("nusbio not detected")
            Return
        End If
        Using nusbio As New Nusbio(serialNumber)

            Cls(nusbio)
            Dim ledStrip0 As APA102LEDStrip = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbio, APA102LEDStrip.Extensions.LedPerMeter._1Led, APA102LEDStrip.Extensions.StripIndex._0, 1)
            Dim ledStrip1 As APA102LEDStrip = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbio, APA102LEDStrip.Extensions.LedPerMeter._1Led, APA102LEDStrip.Extensions.StripIndex._1, 1)

            While (nusbio.Loop())

                If (Console.KeyAvailable) Then

                    Dim k = Console.ReadKey(True).Key
                    If (k = ConsoleKey.Q) Then Exit While

                    If (k = ConsoleKey.B) Then BrigthnessDemo(ledStrip0, ledStrip1)
                    If (k = ConsoleKey.R) Then RainbowDemo(ledStrip0, 2, ledStrip1)
                    If (k = ConsoleKey.W) Then RgbRainbowPlusLedOnGpio0To3Demo(nusbio, ledStrip0, 2, ledStrip1)
                    If (k = ConsoleKey.S) Then ColorsSequence(ledStrip0, ledStrip1)

                    Cls(nusbio)
                End If
            End While
        End Using
    End Sub
End Module

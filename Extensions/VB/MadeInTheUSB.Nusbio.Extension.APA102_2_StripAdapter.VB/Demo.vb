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

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports MadeInTheUSB
Imports MadeInTheUSB.i2c
Imports MadeInTheUSB.GPIO
Imports MadeInTheUSB.WinUtil
Imports MadeInTheUSB.Components
Imports System.Drawing
Imports MadeInTheUSB.Components.APA

Namespace APA102_RGB_LED

    Class Demo

        Private Shared Function GetAssemblyProduct() As String

            Dim currentAssem As Assembly = GetType(Demo).Assembly
            Dim attribs As Object() = currentAssem.GetCustomAttributes(GetType(AssemblyProductAttribute), True)
            If attribs.Length > 0 Then
                Return DirectCast(attribs(0), AssemblyProductAttribute).Product
            End If
            Return Nothing
        End Function

        Private Shared Function AskForStripType() As Char

            Console.Clear()

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            Dim r = ConsoleEx.Question(ConsoleEx.WindowHeight - 3, "Strip Type?    3)0 LED/Meter   6)0 LED/Meter  I) do not know", New List(Of [Char])() From {"3"c, "6"c, "I"c})
            Return r
        End Function

        Private Shared Sub Cls(nusbio__1 As Nusbio)

            Console.Clear()

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue)
            ConsoleEx.WriteMenu(-1, 5, "B)rightness Demo   R)GB Demo   S)croll Demo   SP)eed Demo")
            ConsoleEx.WriteMenu(-1, 7, "A)mp Test   RainboW) Demo   L)ine Demo   AlT)ernate Line Demo")
            ConsoleEx.WriteMenu(-1, 9, "Q)uit")
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight - 2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue)
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight - 3, String.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio__1.SerialNumber, nusbio__1.Description), ConsoleColor.Black, ConsoleColor.DarkCyan)
        End Sub


        Private Shared TargetColors As String = "Blue" & vbCr & vbLf & "BlueViolet" & vbCr & vbLf & "Brown" & vbCr & vbLf & "Chartreuse" & vbCr & vbLf & "Chocolate" & vbCr & vbLf & "CornflowerBlue" & vbCr & vbLf & "Crimson" & vbCr & vbLf & "Cyan" & vbCr & vbLf & "DarkOrange" & vbCr & vbLf & "DarkOrchid" & vbCr & vbLf & "DarkRed" & vbCr & vbLf & "DarkTurquoise" & vbCr & vbLf & "DarkViolet" & vbCr & vbLf & "DarkBlue" & vbCr & vbLf & "DarkCyan" & vbCr & vbLf & "DarkGoldenrod" & vbCr & vbLf & "DarkGreen" & vbCr & vbLf & "DarkMagenta" & vbCr & vbLf & "DeepPink" & vbCr & vbLf & "DeepSkyBlue" & vbCr & vbLf & "DodgerBlue" & vbCr & vbLf & "Firebrick" & vbCr & vbLf & "ForestGreen" & vbCr & vbLf & "Fuchsia" & vbCr & vbLf & "Gold" & vbCr & vbLf & "Green" & vbCr & vbLf & "Indigo" & vbCr & vbLf & "LawnGreen" & vbCr & vbLf & "LightSeaGreen" & vbCr & vbLf & "Lime" & vbCr & vbLf & "Maroon" & vbCr & vbLf & "MediumBlue" & vbCr & vbLf & "MediumSpringGreen" & vbCr & vbLf & "MediumVioletRed" & vbCr & vbLf & "MidnightBlue" & vbCr & vbLf & "Navy" & vbCr & vbLf & "Olive" & vbCr & vbLf & "Orange" & vbCr & vbLf & "OrangeRed" & vbCr & vbLf & "Purple" & vbCr & vbLf & "Red" & vbCr & vbLf & "RoyalBlue" & vbCr & vbLf & "SeaGreen" & vbCr & vbLf & "SpringGreen" & vbCr & vbLf & "Teal" & vbCr & vbLf & "Turquoise" & vbCr & vbLf & "Yellow" & vbCr & vbLf

        Private Shared Function GetWaitTimeUnit(ledStrip As APA102LEDStrip) As Integer

            Dim wait = CInt((80.0 / (2 * ledStrip.MaxLed)) * 10)
            Return wait
        End Function

        Public Shared Sub ScrollDemo(ledStrip As APA102LEDStrip)

            Dim wait = GetWaitTimeUnit(ledStrip)
            Dim quit = False
            ledStrip.Brightness = 16
            ledStrip.AllOff()

            Console.Clear()
            ConsoleEx.TitleBar(0, "Scroll Demo")
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            ConsoleEx.WriteMenu(-1, 3, "")

            Dim bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(","c).ToList()

            While Not quit

                For Each sBColor In bkColors

                    If String.IsNullOrEmpty(sBColor.Trim()) Then
                        Continue For
                    End If

                    Dim bkColor = APA102LEDStrip.DrawingColors(sBColor)
                    bkColor = APA102LEDStrip.ToBrighter(bkColor, -65)

                    Console.WriteLine([String].Format("Background Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)))
                    Dim fgColor = APA102LEDStrip.ToBrighter(bkColor, 20)

                    ledStrip.AddRGBSequence(True, 4, ledStrip.MaxLed - 1, bkColor)
                    ledStrip.InsertRGBSequence(0, 15, fgColor)
                    ledStrip.ShowAndShiftRightAllSequence(wait)

                    If Console.KeyAvailable Then
                        quit = True
                        Exit For
                    End If
                Next
            End While
            ledStrip.AllOff()
            Dim k = Console.ReadKey(True).Key
        End Sub

        Public Shared Sub SpeedTest(ledStrip As APA102LEDStrip)

            ledStrip.Brightness = 16
            ledStrip.AllOff()
            Console.Clear()
            Console.WriteLine("Running test...")

            Dim bkColor = Color.Red
            Dim sw = System.Diagnostics.Stopwatch.StartNew()
            Dim testCount = 1000

            ' This loop set the strip 500 x 2 == 1000 times
            For t As Integer = 0 To (testCount / 2) - 1
                ' Light up in red the 60 led strips
                ledStrip.Reset()
                For l As Integer = 0 To ledStrip.MaxLed - 1
                    ledStrip.AddRGBSequence(False, 7, bkColor)
                Next
                ledStrip.Show()

                ' Turn it off the 60 led strips
                ledStrip.Reset()
                For l As Integer = 0 To ledStrip.MaxLed - 1
                    ledStrip.AddRGBSequence(False, 7, Color.Black)
                Next
                ledStrip.Show()
            Next

            sw.Stop()
            Dim bytePerSeconds = ledStrip.MaxLed * 4 * testCount / (sw.ElapsedMilliseconds / 1000)

            Console.WriteLine("test Duration:{0}, BytePerSecond:{1}, NumberOfLedTurnOnOrOff:{2}", sw.ElapsedMilliseconds, bytePerSeconds, ledStrip.MaxLed * testCount)
            Dim k = Console.ReadKey(True).Key

            ledStrip.AllOff()
            k = Console.ReadKey(True).Key
        End Sub



        Public Shared Sub AlternateLineDemo(ledStrip As APA102LEDStrip)

            Dim wait = GetWaitTimeUnit(ledStrip)
            If ledStrip.MaxLed <= 10 Then
                wait *= 3
            End If

            Dim quit = False
            ledStrip.Brightness = 16
            ledStrip.AllOff()

            Console.Clear()
            ConsoleEx.TitleBar(0, "Alternate Line Demo")
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            ConsoleEx.WriteMenu(-1, 3, "")

            Dim bkColors = TargetColors.Replace(Environment.NewLine, ",").Split(","c).ToList()

            While Not quit

                For Each sBColor In bkColors

                    If String.IsNullOrEmpty(sBColor.Trim()) Then
                        Continue For
                    End If

                    Dim bkColor = APA102LEDStrip.ToBrighter(APA102LEDStrip.DrawingColors(sBColor), -75)

                    Console.WriteLine([String].Format("Background Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)))

                    Dim fgColor = APA102LEDStrip.ToBrighter(bkColor, 40)

                    ledStrip.Reset()
                    For l As Integer = 0 To ledStrip.MaxLed - 1 Step 2
                        ledStrip.AddRGBSequence(False, 4, bkColor)
                        ledStrip.AddRGBSequence(False, 6, fgColor)
                    Next

                    For i As Integer = 0 To ledStrip.MaxLed * 3 - 1 Step 4
                        ledStrip.Show().ShiftRightSequence().Wait(wait)
                        If Console.KeyAvailable Then
                            Exit For
                        End If
                    Next

                    If Console.KeyAvailable Then
                        quit = True
                        Exit For
                    End If
                Next
            End While
            ledStrip.AllOff()
            Dim k = Console.ReadKey(True).Key
        End Sub


        Public Shared Sub RainbowDemo(ledStrip As APA102LEDStrip, jStep As Integer, Optional ledStrip2 As APA102LEDStrip = Nothing)

            Console.Clear()
            ConsoleEx.TitleBar(0, "Rainbow Demo")
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            ConsoleEx.WriteMenu(-1, 3, "")

            Dim brigthness As Integer = 6
            Dim wait As Integer = GetWaitTimeUnit(ledStrip) / 2
            Dim quit = False
            ledStrip.AllOff()

            If ledStrip2 IsNot Nothing Then

                ledStrip2.AllOff()
            End If

            While Not quit

                Dim j = 0

                While j < 256

                    ConsoleEx.Gotoxy(0, 4)
                    ledStrip.Reset()
                    If ledStrip2 IsNot Nothing Then
                        ledStrip2.Reset()
                    End If

                    For i As Integer = 0 To ledStrip.MaxLed - 1
                        ledStrip.AddRGBSequence(False, brigthness, RGBHelper.Wheel(((i * 256 / ledStrip.MaxLed) + j)))
                    Next

                    If ledStrip2 IsNot Nothing Then
                        For i As Integer = 0 To ledStrip2.MaxLed - 1
                            ledStrip2.AddRGBSequence(False, brigthness, RGBHelper.Wheel(((i * 256 / ledStrip.MaxLed) + j)))
                        Next
                    End If

                    For Each bkColor As Color In ledStrip.LedColors
                        Console.WriteLine([String].Format("Color:{0}, Html:{1}, Dec:{2}", bkColor.Name, APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)))
                    Next

                    If ledStrip2 IsNot Nothing Then
                        ledStrip.Show()
                        ledStrip2.Show().Wait(wait)
                    Else
                        ledStrip.Show().Wait(wait)
                    End If

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
            ledStrip.AllOff()
            If ledStrip2 IsNot Nothing Then
                ledStrip2.AllOff()
            End If
        End Sub


        Public Shared Sub MultiShades(ledStrip As APA102LEDStrip)

            Dim wait As Integer = 0
            Dim quit = False
            ledStrip.AllOff()

            While Not quit

                For j As Integer = 0 To 255

                    Console.Clear()
                    ledStrip.Reset()

                    For i As Integer = 0 To ledStrip.MaxLed - 1
                        ledStrip.AddRGBSequence(False, 10, RGBHelper.Wheel(((i + j))))
                    Next

                    For Each bkColor In ledStrip.LedColors
                        Console.WriteLine([String].Format("Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)))
                    Next

                    ledStrip.Show().Wait(wait)

                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key
                        If k = ConsoleKey.Q Then
                            quit = True
                            Exit For
                        End If
                    End If
                Next
            End While
            ledStrip.AllOff()
        End Sub


        Public Shared Sub RGBDemo(ledStrip As APA102LEDStrip)

            Dim wait As Integer = GetWaitTimeUnit(ledStrip)
            Dim waitStep As Integer = 10
            Dim maxWait As Integer = 200
            Dim quit = False
            Dim userMessage = "Speed:{0}. Use Left and Right keys to change the speed"
            ledStrip.Brightness = 22
            ledStrip.AllOff()

            Console.Clear()
            ConsoleEx.TitleBar(0, "RGB Demo")
            ConsoleEx.WriteMenu(-1, 4, "Q)uit")
            ConsoleEx.WriteLine(0, 2, String.Format(userMessage, wait), ConsoleColor.DarkGray)

            While Not quit

                ledStrip.AddRGBSequence(True, 2, ledStrip.MaxLed - 1, Color.Blue)
                ledStrip.InsertRGBSequence(0, 14, Color.Red)
                ledStrip.ShowAndShiftRightAllSequence(wait)

                If Not Console.KeyAvailable Then

                    ledStrip.AddRGBSequence(True, 3, ledStrip.MaxLed - 1, Color.Green)
                    ledStrip.InsertRGBSequence(0, 16, Color.Red)
                    ledStrip.ShowAndShiftRightAllSequence(wait)
                End If

                If Console.KeyAvailable Then

                    While Console.KeyAvailable

                        Dim k = Console.ReadKey(True).Key
                        If k = ConsoleKey.Q Then
                            quit = True
                        End If
                        If k = ConsoleKey.RightArrow Then
                            wait += waitStep
                            If wait > maxWait Then
                                wait = maxWait
                            End If
                        End If
                        If k = ConsoleKey.LeftArrow Then
                            wait -= waitStep
                            If wait < 0 Then
                                wait = 0
                            End If
                        End If
                    End While
                    ConsoleEx.WriteLine(0, 2, String.Format(userMessage, wait), ConsoleColor.DarkGray)
                End If
            End While
            ledStrip.AllOff()
        End Sub

        Public Shared Sub LineDemo(ledStrip As APA102LEDStrip)

            Dim wait As Integer = If(ledStrip.MaxLed <= 10, 55, 0)
            Dim quit = False
            ledStrip.AllOff()

            Console.Clear()
            ConsoleEx.TitleBar(0, "Line Demo")
            ConsoleEx.WriteMenu(-1, 2, "Q)uit")
            ConsoleEx.WriteMenu(-1, 3, "")

            While Not quit

                Dim j = 0

                For i As Integer = 0 To ledStrip.MaxLed - 1
                    ' Remark: there should be a faster way to draw the line, by first setting all the led
                    ' to black and only resetting the one in color. Once we light up all the led, we would
                    ' turn them all off and re start... Todo, totry.
                    Dim bkColor = RGBHelper.Wheel(((i * 256 / ledStrip.MaxLed) + j))
                    ledStrip.AddRGBSequence(True, 2, i + 1, bkColor)
                    If System.Threading.Interlocked.Increment(j) >= 256 Then
                        j = 0
                    End If
                    While Not ledStrip.IsFull
                        ledStrip.AddRGBSequence(False, 2, Color.Black)
                    End While

                    ledStrip.Show().Wait(wait)

                    Console.WriteLine([String].Format("Color:{0}, Html:{1}, Dec:{2}", bkColor.Name.PadRight(16), APA102LEDStrip.ToHexValue(bkColor), APA102LEDStrip.ToDecValue(bkColor)))

                    If Console.KeyAvailable Then

                        While Console.KeyAvailable

                            Dim k = Console.ReadKey(True).Key
                            If k = ConsoleKey.Q Then
                                quit = True
                                Exit While
                            End If
                        End While
                    End If
                Next
                ledStrip.Wait(wait * 3).AllOff()
            End While
            ledStrip.AllOff()
        End Sub

        Public Shared Sub BrigthnessDemo(ledStrip As APA102LEDStrip)

            Dim maxBrightness As Integer = APA102LEDStrip.MAX_BRIGHTNESS / 2
            Dim wait As Integer = GetWaitTimeUnit(ledStrip) / 2
            Dim [step] As Integer = 1
            ledStrip.AllOff()
            Console.Clear()
            ConsoleEx.WriteMenu(-1, 3, "Q)uit")

            While Not Console.KeyAvailable

                Dim b = 1

                While b <= maxBrightness

                    ledStrip.Reset()
                    For l As Integer = 0 To ledStrip.MaxLed - 1
                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Red, b)
                        End If
                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Green, b)
                        End If
                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Blue, b)
                        End If
                    Next
                    ConsoleEx.Write(0, 0, String.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan)
                    ledStrip.Show().Wait(wait)
                    b += [step]
                End While

                ledStrip.Wait(wait * 10)
                b = maxBrightness

                While b >= 1

                    ledStrip.Reset()

                    For l As Integer = 0 To ledStrip.MaxLed - 1

                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Red, b)
                        End If
                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Green, b)
                        End If
                        If Not ledStrip.IsFull Then
                            ledStrip.AddRGB(Color.Blue, b)
                        End If
                    Next
                    ConsoleEx.Write(0, 0, String.Format("Brightness {0:00}", b), ConsoleColor.DarkCyan)
                    ledStrip.Show().Wait(wait)
                    b -= [step]
                End While
                ledStrip.Wait(wait * 10)
                If Console.KeyAvailable Then
                    Exit While
                End If
            End While
            ledStrip.AllOff()
            Dim k = Console.ReadKey(True).Key
        End Sub


        ''' <summary>
        ''' 
        ''' *** ATTENTION ***
        ''' 
        ''' WHEN CONTROLLING AN APA LED STRIP WITH NUSBIO YOU MUST KNOW THE AMP CONSUMPTION.
        ''' 
        ''' USB DEVICE ARE LIMITED TO 500 MILLI AMP.
        ''' 
        ''' AN LED IN GENERAL CONSUMES FROM 20 TO 25 MILLI AMP. AN RGB LED CONSUMES 3 TIMES 
        ''' MORE IF THE RED, GREEN AND BLUE ARE SET TO THE 255, 255, 255 WHICH IS WHITE
        ''' AT THE MAXIMUN INTENSISTY WHICH IS 31.
        ''' 
        ''' YOU MUST KNOW WHAT IS THE MAXIMUN CONSUMPTION OF YOUR APA 102 RGB LEB STRIP WHEN THE 
        ''' RGB IS SET TO WHITE, WHITE, WHITE AND THE BRIGTHNESS IS AT THE MAXIMUM.
        ''' 
        '''    -------------------------------------------------------------------------------
        '''    --- NEVER GO OVER 300 MILLI AMP IF THE LED STRIP IS POWERED FROM THE NUSBIO ---
        '''    -------------------------------------------------------------------------------
        ''' 
        '''         POWER ONLY A LED STRIP OF 5 LED WHEN DIRECTLY PLUGGED INTO NUSBIO.
        ''' 
        ''' THE FUNCTION AmpTest() WILL LIGHT UP THE FIRST LED OF THE STRIP AT MAXIMUM BRIGHTNESS.
        ''' USE A MULTI METER TO WATCH THE AMP COMSUMPTION.
        ''' 
        ''' IF YOU WANT TO POWER MORE THAN 5 LEDS, THERE ARE 2 SOLUTIONS:
        ''' 
        ''' (1) ONLY FOR 6 to 10 LEDs. ADD BETWEEN NUSBIO VCC AND THE STRIP 5V PIN A 47 OHM RESISTORS.
        ''' YOU WILL LOOSE SOME BRIGTHNESS, BUT IT IS SIMPLER. THE RESISTOR LIMIT THE CURRENT THAT
        ''' CAN BE USED FROM THE USB.
        ''' 
        ''' (2) USE A SECOND SOURCE OF POWER LIKE:
        ''' 
        '''  - A 5 VOLTS 1 AMPS ADAPTERS TO POWER A 30 LED STRIP
        '''  - A 5 VOLTS 2 AMPS ADAPTERS TO POWER A 60 LED STRIP
        '''  
        ''' ~~~ ATTENTION ~~~
        ''' 
        '''     WHEN USING A SECOND SOURCE OF POWER IN THE SAME BREADBOARD OR PCB, ~ NEVER ~ 
        '''     CONNECT THE POSISTIVE OF THE SECOND SOURCE OF POWER WITH THE NUSBIO VCC.
        ''' 
        ''' SEE OUR WEB SITE 'LED STRIP TUTORIAL' FOR MORE INFO.
        ''' 
        ''' </summary>
        ''' <param name="ledStrip"></param>
        Public Shared Sub AmpTest(ledStrip As APA102LEDStrip)

            Dim wait As Integer = 37
            ledStrip.Brightness = 22
            Dim quit = False
            ledStrip.AllOff()

            Console.Clear()

            ' Set the first LED of the strip to max brithness and all other led will be off
            ledStrip.Reset()
            ledStrip.AddRGBSequence(True, APA102LEDStrip.MAX_BRIGHTNESS, Color.White)
            While Not ledStrip.IsFull
                ' Color black does not consume current
                ledStrip.AddRGBSequence(False, 1, Color.Black)
            End While
            ledStrip.Show().Wait(wait)

            Console.WriteLine("Measure the AMP consumption - Hit enter to continue")
            Console.ReadLine()

            '''///10 LED all white, minimun intensisty
            Dim b = 1
            Console.WriteLine("Brigthness {0}", b)
            ledStrip.AllOff()
            ledStrip.Reset()
            While Not ledStrip.IsFull
                ledStrip.AddRGBSequence(False, b, Color.White)
            End While
            ledStrip.Show().Wait(wait)

            Console.WriteLine("Measure the AMP consumption - Hit enter to continue")
            Console.ReadLine()

            ledStrip.AllOff()
        End Sub

        Public Shared Sub Run()

            Console.WriteLine("Nusbio initialization")
            Dim serialNumber = Nusbio.Detect()
            If serialNumber Is Nothing Then
                ' Detect the first Nusbio available
                Console.WriteLine("nusbio not detected")
                Return
            End If
            Console.Clear()

            Using nusbioDevice = New Nusbio(serialNumber)

                ' 30 led per meter strip
                Dim ledStrip1 As APA102LEDStrip = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbioDevice, APA102LEDStrip.Extensions.LedPerMeter._30LedPerMeter, APA102LEDStrip.Extensions.StripIndex._0, 10)
                ' 60 led per meter strip

                If AskForStripType() = "6"c Then
                    ledStrip1 = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbioDevice, APA102LEDStrip.Extensions.LedPerMeter._60LedPerMeter, APA102LEDStrip.Extensions.StripIndex._0, 60)
                End If

                ledStrip1.AllOff()
                Cls(nusbioDevice)

                ' For more information about the Nusbio APA102 2 Strip Adapter to control up to 2 strips 
                ' with 10 RGB LED on each strip powered from Nusbio. See following url
                ' http://www.madeintheusb.net/TutorialExtension/Index#Apa102RgbLedStrip

                Dim ledStrip2 As APA102LEDStrip = APA102LEDStrip.Extensions.TwoStripAdapter.Init(nusbioDevice, APA102LEDStrip.Extensions.LedPerMeter._30LedPerMeter, APA102LEDStrip.Extensions.StripIndex._1, 10)
                If ledStrip2 IsNot Nothing Then
                    ledStrip2.AllOff()
                End If

                While nusbioDevice.Loop()

                    If Console.KeyAvailable Then
                        Dim k = Console.ReadKey(True).Key
                        If k = ConsoleKey.Q Then
                            Exit While
                        End If

                        If k = ConsoleKey.R Then
                            RGBDemo(ledStrip1)
                        End If

                        If k = ConsoleKey.B Then
                            BrigthnessDemo(ledStrip1)
                        End If

                        If k = ConsoleKey.A Then
                            AmpTest(ledStrip1)
                        End If

                        If k = ConsoleKey.W Then
                            RainbowDemo(ledStrip1, 10, ledStrip2)
                        End If

                        If k = ConsoleKey.D1 Then
                            RainbowDemo(ledStrip1, 1)
                        End If

                        If k = ConsoleKey.M Then
                            MultiShades(ledStrip1)
                        End If

                        If k = ConsoleKey.S Then
                            ScrollDemo(ledStrip1)
                        End If

                        If k = ConsoleKey.T Then
                            AlternateLineDemo(ledStrip1)
                        End If

                        If k = ConsoleKey.P Then
                            SpeedTest(ledStrip1)
                        End If

                        If k = ConsoleKey.L Then
                            LineDemo(ledStrip1)
                        End If

                        Cls(nusbioDevice)
                    End If
                End While
            End Using
            Console.Clear()
        End Sub
    End Class
End Namespace



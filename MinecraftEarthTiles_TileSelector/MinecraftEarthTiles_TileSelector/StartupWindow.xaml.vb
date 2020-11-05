Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms

Public Class StartupWindow

    Public Shared Property MySettings As New Settings
    Public Shared Property MySelection As New Selection
    Public Shared Property MyGeneration As New List(Of Generation)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim LocalSettings As Settings
        If File.Exists(My.Application.Info.DirectoryPath & "\settings.xml") Then
            Try
                LocalSettings = CustomXmlSerialiser.GetXMLSettings(My.Application.Info.DirectoryPath & "\settings.xml")
                If Not Directory.Exists(LocalSettings.PathToScriptsFolder) Then
                    MySettings.PathToScriptsFolder = ""
                End If
                If Not File.Exists(LocalSettings.PathToWorldPainterFolder) Then
                    MySettings.PathToWorldPainterFolder = ""
                End If
                If Not Directory.Exists(LocalSettings.PathToQGIS) Then
                    MySettings.PathToQGIS = ""
                End If
                If Not File.Exists(LocalSettings.PathToMagick) Then
                    MySettings.PathToMagick = ""
                End If
                MySettings = LocalSettings
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
        Dim LocalSelection As New Selection
        If File.Exists(My.Application.Info.DirectoryPath & "\tiles.xml") Then
            Try
                LocalSelection = CustomXmlSerialiser.GetXMLSelection(My.Application.Info.DirectoryPath & "\tiles.xml")
                MySelection = LocalSelection
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
        Check()
    End Sub

#Region "Menu"

    Private Sub Exit_Click(sender As Object, e As RoutedEventArgs)
        End
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "MyResources/MinecraftEarthTiles.chm")
        Process.Start("https://earthtiles.motfe.net/")
    End Sub

#End Region

#Region "Config Buttons"

    Private Sub Btn_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim SettingsWindow As New SettingsWindow
        SettingsWindow.ShowDialog()
        Check()
    End Sub

    Private Sub Btn_Selection_Click(sender As Object, e As RoutedEventArgs)
        Dim SelectionWindow As New SelectionWindow
        SelectionWindow.ShowDialog()
        Check()
    End Sub

    Private Sub Btn_Generation_Click(sender As Object, e As RoutedEventArgs)
        ''Btn_osmbatExport_Click(sender, e)
        ''Btn_QgisExport_Click(sender, e)
        ''Btn_tartool_Click(sender, e)
        ''Btn_gdalExport_Click(sender, e)
        ''Btn_magick_Export_Click(sender, e)
        ''Btn_WPScriptExport_Click(sender, e)
        ''Btn_CombineExport_Click(sender, e)
        ''Btn_CleanUpExport_Click(sender, e)
        Dim TilesList As List(Of String) = MySelection.TilesList
        If TilesList.Count = 0 Then
            Throw New System.Exception("No Tiles selected.")
        End If
        TilesList.Sort()
        MyGeneration = New List(Of Generation)
        MyGeneration.Add(New Generation("Convert pbf"))
        For Each Tile In TilesList
            MyGeneration.Add(New Generation(Tile))
        Next
        MyGeneration.Add(New Generation("Combining"))
        MyGeneration.Add(New Generation("Cleanup"))

        Dim GenerationWindow As New GenerationWindow
        GenerationWindow.ShowDialog()
    End Sub

#End Region

#Region "Export Buttons"

    Private Sub Btn_osmbatExport_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim TilesList As List(Of String) = MySelection.TilesList
            If TilesList.Count = 0 Then
                Throw New System.Exception("No Tiles selected.")
            End If
            TilesList.Sort()

            If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            If MySettings.geofabrik = True Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & MySettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\osm"" mkdir """ & MySettings.PathToScriptsFolder & "\osm""")
                ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\image_exports"" mkdir """ & MySettings.PathToScriptsFolder & "\image_exports""")

                ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmconvert.exe"" """ & MySettings.PathToPBF & """ -o=""" & MySettings.PathToScriptsFolder & "\osm\unfiltered.o5m""")

                Dim filter As String = "water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR natural=wetland OR wetland=swamp OR natural=glacier OR natural=volcano OR natural=beach OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub OR landuse=forest OR landuse=bare_rock OR natural=scree OR natural=shingle"

                If MySettings.highways Then
                    filter &= " OR highway=motorway OR highway=trunk"
                End If

                If MySettings.streets Then
                    filter &= " OR highway=primary OR highway=secondary OR highway=tertiary"
                End If

                If MySettings.small_streets Then
                    filter &= " OR highway=residential"
                End If

                If MySettings.riversBoolean Then
                    filter &= " OR waterway=river OR waterway=canal OR water=river OR waterway=riverbank"
                End If

                If MySettings.streams Then
                    filter &= " OR waterway=stream"
                End If

                If MySettings.farms Then
                    filter &= " OR landuse=farmland OR landuse=vineyard"
                End If

                If MySettings.meadows Then
                    filter &= " OR landuse=meadow"
                End If

                If MySettings.quarrys Then
                    filter &= " OR landuse=quarry"
                End If

                If MySettings.aerodrome Then
                    If CType(MySettings.TilesPerMap, Int16) = 1 And CType(MySettings.BlocksPerTile, Int16) >= 1024 Then
                        filter &= " OR aeroway=aerodrome AND iata="
                    Else
                        filter &= " OR aeroway=launchpad"
                    End If
                End If

                If MySettings.buildings Then
                    filter &= " OR landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"
                End If

                If MySettings.bordersBoolean = True And MySettings.borders = "2020" Then
                    filter &= " OR boundary=administrative AND admin_level=2"
                End If

                ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\unfiltered.o5m"" --verbose --keep=""" & filter & """ -o=""" & MySettings.PathToScriptsFolder & "\osm\output.o5m""")
                ScriptBatchFile.Close()

                For Each Tile In TilesList

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                    End If

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                    ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                    ScriptBatchFile.WriteLine()
                    ScriptBatchFile.Close()

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                    ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

                    Dim LatiDir = Tile.Substring(0, 1)
                    Dim LatiNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                    Dim LongDir = Tile.Substring(3, 1)
                    Dim LongNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(4, 3), LongNumber)
                    Dim borderW As Double = 0
                    Dim borderS As Double = 0
                    Dim borderE As Double = 0
                    Dim borderN As Double = 0
                    If LatiDir = "N" Then
                        borderS = LatiNumber - CType(MySettings.TilesPerMap, Int16) + 1 - 0.01
                        borderN = LatiNumber + 1.01
                    Else
                        borderS = (-1 * LatiNumber) - CType(MySettings.TilesPerMap, Int16) + 1 - 0.01
                        borderN = (-1 * LatiNumber) + 1.01
                    End If
                    If LongDir = "E" Then
                        borderW = LongNumber - 0.01
                        borderE = LongNumber + CType(MySettings.TilesPerMap, Int16) + 0.01
                    Else
                        borderW = (-1 * LongNumber) - 0.01
                        borderE = (-1 * LongNumber) + CType(MySettings.TilesPerMap, Int16) + 0.01
                    End If

                    If borderN > 90 Then
                        borderN = 90
                    End If

                    If borderS < -90 Then
                        borderS = -90
                    End If

                    If borderE > 180 Then
                        borderE = 180
                    End If

                    If borderW < -180 Then
                        borderW = -180
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmconvert.exe"" """ & MySettings.PathToScriptsFolder & "\osm\output.o5m"" -b=" & borderW.ToString("##0.##", New CultureInfo("en-US")) & "," & borderS.ToString("#0.##", New CultureInfo("en-US")) & "," & (borderE).ToString("##0.##", New CultureInfo("en-US")) & "," & (borderN).ToString("#0.##", New CultureInfo("en-US")) & " -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --complete-ways --complete-multipolygons --complete-boundaries --drop-version --verbose")

                    If MySettings.highways Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                    End If

                    If MySettings.streets Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                    End If

                    If MySettings.small_streets Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR waterway=riverbank OR waterway=canal OR waterway=canal"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                    If MySettings.riversBoolean Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                    End If

                    If MySettings.streams Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=stream"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland"" --drop=""wetland=swamp"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\wetland.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland AND wetland=swamp"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\swamp.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")

                    If MySettings.farms Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                    End If

                    If MySettings.meadows Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                    End If

                    If MySettings.quarrys Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                    End If

                    If MySettings.aerodrome Then
                        If CType(MySettings.TilesPerMap, Int16) = 1 And CType(MySettings.BlocksPerTile, Int16) >= 1024 Then
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                        Else
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                        End If
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")
                    If MySettings.buildings Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                    End If

                    If MySettings.bordersBoolean = True And MySettings.borders = "2020" Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("del """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")

                    ScriptBatchFile.Close()

                Next

            Else

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & MySettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("")
                ScriptBatchFile.Close()

                For Each Tile In TilesList

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                    End If

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                    ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                    ScriptBatchFile.WriteLine()
                    ScriptBatchFile.Close()

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                    ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

                    If Not MySettings.Proxy = "" Then
                        ScriptBatchFile.WriteLine("set http_proxy=" & MySettings.Proxy)
                    End If

                    Dim LatiDir = Tile.Substring(0, 1)
                    Dim LatiNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                    Dim LongDir = Tile.Substring(3, 1)
                    Dim LongNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(4, 3), LongNumber)
                    Dim borderW As Int32 = 0
                    Dim borderS As Int32 = 0
                    Dim borderE As Int32 = 0
                    Dim borderN As Int32 = 0
                    If LatiDir = "N" Then
                        borderS = LatiNumber
                        borderN = LatiNumber + 1
                    Else
                        borderS = -1 * LatiNumber
                        borderN = -1 * LatiNumber + 1
                    End If
                    If LongDir = "E" Then
                        borderW = LongNumber
                        borderE = LongNumber + 1
                    Else
                        borderW = -1 * LongNumber
                        borderE = -1 * LongNumber + 1
                    End If

                    ScriptBatchFile.WriteLine(": Download_" & Tile)
                    ScriptBatchFile.WriteLine("@echo on")

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\wget.exe"" -O """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" ""http://overpass-api.de/api/interpreter?data=(node(" & borderS.ToString & "," & borderW.ToString & "," & borderN.ToString & "," & borderE.ToString & ");<;>;);out;""")
                    ScriptBatchFile.WriteLine("@echo off")
                    ScriptBatchFile.WriteLine("FOR /F  "" usebackq "" %%A IN ('" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm') DO set size=%%~zA")
                    ScriptBatchFile.WriteLine("if %size% lss 1000 (")
                    ScriptBatchFile.WriteLine("	goto :UserInputNeeded_" & Tile)
                    ScriptBatchFile.WriteLine(") else (")
                    ScriptBatchFile.WriteLine("	goto :Continue_" & Tile)
                    ScriptBatchFile.WriteLine(")")
                    ScriptBatchFile.WriteLine(":UserInputNeeded_" & Tile)
                    ScriptBatchFile.WriteLine("set /p E=OSM file seems incomplete. Do you want to re-download the file? [Y/N]:")
                    ScriptBatchFile.WriteLine("if /I '%E%'=='Y' goto :Download_" & Tile)
                    ScriptBatchFile.WriteLine("if /I '%E%'=='N' goto :Continue_" & Tile)
                    ScriptBatchFile.WriteLine(":Continue_" & Tile)
                    ScriptBatchFile.WriteLine("@echo on")

                    If MySettings.highways Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                    End If

                    If MySettings.streets Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """"" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"""" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                    End If

                    If MySettings.small_streets Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR waterway=riverbank OR waterway=canal OR waterway=canal"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                    If MySettings.riversBoolean Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                    End If

                    If MySettings.streams Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=stream"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland"" --drop=""wetland=swamp"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\wetland.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland And wetland=swamp"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\swamp.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")

                    If MySettings.farms Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                    End If

                    If MySettings.meadows Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                    End If

                    If MySettings.quarrys Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                    End If

                    If MySettings.quarrys Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                    End If

                    If MySettings.aerodrome Then
                        If CType(MySettings.TilesPerMap, Int16) = 1 And CType(MySettings.BlocksPerTile, Int16) >= 1024 Then
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")

                        Else
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                        End If
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")

                    If MySettings.buildings Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                    End If

                    If MySettings.bordersBoolean = True And MySettings.borders = "2020" Then
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                    Else
                        ScriptBatchFile.WriteLine("xcopy /y """ & MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                    End If

                    ScriptBatchFile.WriteLine("del """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")

                    ScriptBatchFile.Close()

                Next

            End If
        Catch ex As Exception
            MsgBox("File '1-osmconvert.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_QgisExport_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim TilesList As List(Of String) = MySelection.TilesList
            If TilesList.Count = 0 Then
                Throw New System.Exception("No Tiles selected.")
            End If
            TilesList.Sort()

            If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\python\")) Then
                System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\python\")
            End If

            If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            For Each Tile In TilesList

                Dim pythonFile As System.IO.StreamWriter
                pythonFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\python\" & Tile.ToString & "_repair.py", False, System.Text.Encoding.ASCII)
                pythonFile.WriteLine("import os" & Environment.NewLine &
                                    "from PyQt5.QtCore import *" & Environment.NewLine &
                                    "import processing")

                Dim PathReversedSlash = Replace(MySettings.PathToScriptsFolder, "\", "/")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/urban.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/urban.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""broadleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/broadleaved.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""needleleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/needleleaved.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/forest.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/mixedforest.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/beach.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/beach.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/grass.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/grass.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/farmland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/farmland.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/meadow.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/meadow.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/quarry.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/quarry.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/water.osm|layername=multipolygons|subset=\""natural\"" = \'water\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/water.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/glacier.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/glacier.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/wetland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/wetland.shp'})")
                pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/swamp.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile.ToString & "/swamp.shp'})")
                pythonFile.WriteLine("os.kill(os.getpid(), 9)")
                pythonFile.Close()

                Dim pythonFile2 As System.IO.StreamWriter
                pythonFile2 = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\python\" & Tile.ToString & ".py", False, System.Text.Encoding.ASCII)
                pythonFile2.WriteLine("import os" & Environment.NewLine &
                                    "from PyQt5.QtCore import *" & Environment.NewLine &
                                    Environment.NewLine &
                                    "path = '" & MySettings.PathToScriptsFolder.Replace("\", "/") & "/'" & Environment.NewLine &
                                    "tile = '" & Tile & "'" & Environment.NewLine &
                                    "scale = " & MySettings.BlocksPerTile & Environment.NewLine &
                                    "TilesPerMap = " & MySettings.TilesPerMap & Environment.NewLine &
                                    "rivers = '" & MySettings.rivers & "'" & Environment.NewLine &
                                    "borders = " & MySettings.bordersBoolean & Environment.NewLine &
                                    "borderyear = '" & MySettings.borders & "'" & Environment.NewLine &
                                    Environment.NewLine)

                'Dim QgisBasescript As String = ""
                'If My.Computer.FileSystem.FileExists(My.Application.Info.DirectoryPath & "/qgis.txt") Then
                'Dim fileReader As String
                'fileReader = My.Computer.FileSystem.ReadAllText(My.Application.Info.DirectoryPath & "/qgis.txt")
                'QgisBasescript = fileReader
                'Else
                'QgisBasescript = My.Resources.ResourceManager.GetString("basescript")
                'Dim objWriter As New System.IO.StreamWriter(My.Application.Info.DirectoryPath & "/qgis.txt")
                'objWriter.Write(QgisBasescript)
                'objWriter.Close()
                'End If

                pythonFile2.WriteLine(My.Resources.ResourceManager.GetString("basescript"))
                pythonFile2.WriteLine("os.kill(os.getpid(), 9)")
                pythonFile2.Close()
            Next

            For Each Tile In TilesList

                If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                Dim ScriptBatchFile As System.IO.StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-log-qgis.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-qgis.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-qgis.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\image_exports\"" mkdir """ & MySettings.PathToScriptsFolder & "\image_exports\""")
                ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & """ mkdir """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & """")
                ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap"" mkdir """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")
                'first, repair multipolygons
                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --code """ & MySettings.PathToScriptsFolder & "\python\" & Tile & "_repair.py""")
                'next, copy all files
                ScriptBatchFile.WriteLine("xcopy """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & """ """ & MySettings.PathToScriptsFolder & "\QGIS\OsmData"" /Y")
                'open project and run script
                If MySettings.bathymetry Then
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --project """ & MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz"" --code """ & MySettings.PathToScriptsFolder & "\python\" & Tile & ".py""")
                Else
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --project """ & MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles_no-bathymetry.qgz"" --code """ & MySettings.PathToScriptsFolder & "\python\" & Tile & ".py""")
                End If

                ScriptBatchFile.Close()
            Next

        Catch ex As Exception
            MsgBox("File '2-qgis.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_tartool_Click(sender As Object, e As RoutedEventArgs)
        Try
            If CType(MySettings.TilesPerMap, Int16) = 1 Then
                Dim TilesList As List(Of String) = MySelection.TilesList
                If TilesList.Count = 0 Then
                    Throw New System.Exception("No Tiles selected.")
                End If
                TilesList.Sort()

                If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                    System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
                End If

                Dim ScriptBatchFile As System.IO.StreamWriter

                For Each Tile In TilesList

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                    End If

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-log-tartool.bat", False, System.Text.Encoding.ASCII)
                    ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                    ScriptBatchFile.WriteLine()
                    ScriptBatchFile.Close()

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat", False, System.Text.Encoding.ASCII)
                    If Not MySettings.Proxy = "" Then
                        ScriptBatchFile.WriteLine("set ftp_proxy=" & MySettings.Proxy)
                    End If

                    Dim LatiDir = Tile.Substring(0, 1)
                    Dim LatiNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                    Dim LongDir = Tile.Substring(3, 1)
                    Dim LongNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(4, 3), LongNumber)

                    Dim NewTile As String = LatiDir & LatiNumber.ToString("00") & LongDir & LongNumber.ToString("000")

                    Dim TilesOneMoreDigit = LatiDir
                    If LatiNumber < 10 Then
                        TilesOneMoreDigit = TilesOneMoreDigit & "00" & LatiNumber
                    Else
                        TilesOneMoreDigit = TilesOneMoreDigit & "0" & LatiNumber
                    End If
                    TilesOneMoreDigit &= LongDir
                    If LongNumber < 10 Then
                        TilesOneMoreDigit = TilesOneMoreDigit & "00" & LongNumber
                    ElseIf LongNumber < 100 Then
                        TilesOneMoreDigit = TilesOneMoreDigit & "0" & LongNumber
                    Else
                        TilesOneMoreDigit &= LongNumber
                    End If
                    Dim TilesRounded As String = TilesOneMoreDigit
                    If LatiDir = "N" Then
                        Dim LatiRounded As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(3, 1), LatiRounded)
                        If LatiRounded = 6 Or LatiRounded = 7 Or LatiRounded = 8 Or LatiRounded = 9 Then
                            TilesRounded = TilesRounded.Remove(3, 1)
                            TilesRounded = TilesRounded.Insert(3, "5")
                        ElseIf LatiRounded = 1 Or LatiRounded = 2 Or LatiRounded = 3 Or LatiRounded = 4 Then
                            TilesRounded = TilesRounded.Remove(3, 1)
                            TilesRounded = TilesRounded.Insert(3, "0")
                        End If
                    Else
                        Dim LatiRounded1 As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(3, 1), LatiRounded1)
                        Dim LatiRounded10 As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(2, 1), LatiRounded10)
                        If LatiRounded1 = 6 Or LatiRounded1 = 7 Or LatiRounded1 = 8 Or LatiRounded1 = 9 Then
                            TilesRounded = TilesRounded.Remove(3, 1)
                            TilesRounded = TilesRounded.Insert(3, "0")
                            TilesRounded = TilesRounded.Remove(2, 1)
                            TilesRounded = TilesRounded.Insert(2, (LatiRounded10 + 1).ToString)
                        ElseIf LatiRounded1 = 1 Or LatiRounded1 = 2 Or LatiRounded1 = 3 Or LatiRounded1 = 4 Then
                            TilesRounded = TilesRounded.Remove(3, 1)
                            TilesRounded = TilesRounded.Insert(3, "5")
                        End If
                    End If
                    If LongDir = "E" Then
                        Dim LongRounded As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(7, 1), LongRounded)
                        If LongRounded = 5 Or LongRounded = 6 Or LongRounded = 7 Or LongRounded = 8 Or LongRounded = 9 Then
                            TilesRounded = TilesRounded.Remove(7, 1)
                            TilesRounded = TilesRounded.Insert(7, "5")
                        Else
                            TilesRounded = TilesRounded.Remove(7, 1)
                            TilesRounded = TilesRounded.Insert(7, "0")
                        End If
                    Else
                        Dim LongRounded1 As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(7, 1), LongRounded1)
                        Dim LongRounded10 As Int32 = 0
                        Int32.TryParse(TilesOneMoreDigit.Substring(6, 1), LongRounded10)
                        If LongRounded10 = 9 And LongRounded1 >= 6 Then
                            TilesRounded = TilesRounded.Remove(7, 1)
                            TilesRounded = TilesRounded.Insert(7, "0")
                            TilesRounded = TilesRounded.Remove(6, 1)
                            TilesRounded = TilesRounded.Insert(6, "0")
                            TilesRounded = TilesRounded.Remove(5, 1)
                            TilesRounded = TilesRounded.Insert(5, "1")
                        ElseIf LongRounded1 = 6 Or LongRounded1 = 7 Or LongRounded1 = 8 Or LongRounded1 = 9 Then
                            TilesRounded = TilesRounded.Remove(7, 1)
                            TilesRounded = TilesRounded.Insert(7, "0")
                            TilesRounded = TilesRounded.Remove(6, 1)
                            TilesRounded = TilesRounded.Insert(6, (LongRounded10 + 1).ToString)
                        ElseIf LongRounded1 = 1 Or LongRounded1 = 2 Or LongRounded1 = 3 Or LongRounded1 = 4 Then
                            TilesRounded = TilesRounded.Remove(7, 1)
                            TilesRounded = TilesRounded.Insert(7, "5")
                        End If
                    End If
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\wget.exe"" -O """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" ""ftp://ftp.eorc.jaxa.jp/pub/ALOS/ext1/AW3D30/release_v1903/" & TilesRounded & "/" & TilesOneMoreDigit & ".tar.gz""")
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToScriptsFolder & "\TarTool.exe"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")

                    ScriptBatchFile.Close()
                Next
            End If
        Catch ex As Exception
            MsgBox("File '3-tartool.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_gdalExport_Click(sender As Object, e As RoutedEventArgs)
        If CType(MySettings.TilesPerMap, Int16) = 1 Then
            If MySettings.PathToQGIS = "" Then
                MsgBox("Please enter the path to the 'gdal_translator.exe' file")
            Else
                Try
                    Dim TilesList As List(Of String) = MySelection.TilesList
                    If TilesList.Count = 0 Then
                        Throw New System.Exception("No Tiles selected.")
                    End If
                    TilesList.Sort()

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
                    End If

                    Dim ScriptBatchFile As System.IO.StreamWriter
                    For Each Tile In TilesList

                        If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                            System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                        End If

                        ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-log-gdal.bat", False, System.Text.Encoding.ASCII)
                        ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                        ScriptBatchFile.WriteLine()
                        ScriptBatchFile.Close()

                        ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat", False, System.Text.Encoding.ASCII)

                        Dim LatiDir = Tile.Substring(0, 1)
                        Dim LatiNumber As Int32 = 0
                        Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                        Dim LongDir = Tile.Substring(3, 1)
                        Dim LongNumber As Int32 = 0
                        Int32.TryParse(Tile.Substring(4, 3), LongNumber)

                        Dim TilesOneMoreDigit = LatiDir
                        If LatiNumber < 10 Then
                            TilesOneMoreDigit = TilesOneMoreDigit & "00" & LatiNumber
                        Else
                            TilesOneMoreDigit = TilesOneMoreDigit & "0" & LatiNumber
                        End If
                        TilesOneMoreDigit &= LongDir
                        If LongNumber < 10 Then
                            TilesOneMoreDigit = TilesOneMoreDigit & "00" & LongNumber
                        ElseIf LongNumber < 100 Then
                            TilesOneMoreDigit = TilesOneMoreDigit & "0" & LongNumber
                        Else
                            TilesOneMoreDigit &= LongNumber
                        End If

                        If CType(MySettings.TilesPerMap, Int16) > 1 Then
                            Dim CombinedString As String = ""
                            CombinedString &= "@python3 """ & MySettings.PathToQGIS & "/apps/Python37/Scripts/gdal_merge.py"" -o """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"""
                            For indexLati As Int32 = 0 To CType(MySettings.TilesPerMap, Int16) - 1
                                For indexLongi As Int32 = 0 To CType(MySettings.TilesPerMap, Int16) - 1
                                    Dim LatiDirTemp = Tile.Substring(0, 1)
                                    Dim LatiNumberTemp As Int32 = 0
                                    Int32.TryParse(Tile.Substring(1, 2), LatiNumberTemp)
                                    Dim LongDirTemp = Tile.Substring(3, 1)
                                    Dim LongNumberTemp As Int32 = 0
                                    Int32.TryParse(Tile.Substring(4, 3), LongNumberTemp)

                                    If LatiDir = "N" Then
                                        LatiNumberTemp -= indexLati
                                    Else
                                        LatiNumberTemp += indexLati
                                    End If

                                    If LongDir = "W" Then
                                        LongNumberTemp -= indexLongi
                                    Else
                                        LongNumberTemp += indexLongi
                                    End If

                                    Dim TilesOneMoreDigitTemp = LatiDir
                                    If LatiNumberTemp < 10 Then
                                        TilesOneMoreDigitTemp = TilesOneMoreDigitTemp & "00" & LatiNumberTemp
                                    Else
                                        TilesOneMoreDigitTemp = TilesOneMoreDigitTemp & "0" & LatiNumberTemp
                                    End If
                                    TilesOneMoreDigitTemp &= LongDir
                                    If LongNumberTemp < 10 Then
                                        TilesOneMoreDigitTemp = TilesOneMoreDigitTemp & "00" & LongNumberTemp
                                    ElseIf LongNumber < 100 Then
                                        TilesOneMoreDigitTemp = TilesOneMoreDigitTemp & "0" & LongNumberTemp
                                    Else
                                        TilesOneMoreDigitTemp &= LongNumberTemp
                                    End If

                                    CombinedString &= " """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigitTemp & "\" & TilesOneMoreDigitTemp & "_AVE_DSM.tif"""

                                Next
                            Next
                            ScriptBatchFile.WriteLine(CombinedString)
                        End If

                        Select Case MySettings.VerticalScale
                            Case "100"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -6200 6547300 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                            Case "50"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -3100 3273650 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                            Case "33"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -2046 2160609 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                            Case "25"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1550 1636825 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                            Case "10"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -620 654730‬ 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                            Case "5"
                                ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -310 327365 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                        End Select
                        'ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -512 15872‬ 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdaldem.exe"" slope """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" -s 111120 -compute_edges")
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale 0 90 0 65535 """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_slope.png""")

                        ScriptBatchFile.Close()
                    Next
                Catch ex As Exception
                    MsgBox("File '4-gdal.bat' could not be saved\n" & ex.Message)
                End Try
            End If
        End If
    End Sub

    Private Sub Btn_magick_Export_Click(sender As Object, e As RoutedEventArgs)
        If MySettings.PathToMagick = "" Then
            MsgBox("Please enter the path to the 'magick.exe' file")
        Else
            Try
                Dim TilesList As List(Of String) = MySelection.TilesList
                If TilesList.Count = 0 Then
                    Throw New System.Exception("No Tiles selected.")
                End If
                TilesList.Sort()

                If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                    System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
                End If

                For Each Tile In TilesList

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                    End If

                    Dim ScriptBatchFile As System.IO.StreamWriter

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-log-magick.bat", False, System.Text.Encoding.ASCII)
                    ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                    ScriptBatchFile.WriteLine()
                    ScriptBatchFile.Close()

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat", False, System.Text.Encoding.ASCII)

                    Dim NumberOfResize = "( +clone -resize 50%% )"
                    Dim NumberOfResizeWater = "( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                    Dim TilesSize As Double = CType(MySettings.BlocksPerTile, Double)
                    While TilesSize >= 2
                        NumberOfResize &= " ( +clone -resize 50%% )"
                        NumberOfResizeWater &= " ( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                        TilesSize /= 2
                    End While

                    Dim NewTile As String = Tile

                    If CType(MySettings.TilesPerMap, Int16) = 1 Then

                        If MySettings.Heightmap_Error_Correction = True Then
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png"" -transparent black -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png"" " & NumberOfResize & " -layers RemoveDups -filter Gaussian -resize " & MySettings.BlocksPerTile & "x" & MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png"" -compose over -composite -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert -negate """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water.png"" -threshold 50%% -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert -negate """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river.png"" -threshold 50%% -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river_mask.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ composite -gravity center """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river_mask.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert -negate """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png"" -alpha off """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png"" -transparent white -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png"" -compose over -composite -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png"" -transparent black -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png"" -morphology Dilate Gaussian " & NumberOfResizeWater & " -layers RemoveDups -filter Gaussian -resize " & MySettings.BlocksPerTile & "x" & MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png""")
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png"" -compose over -composite -depth 16 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
                        Else
                            ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
                        End If
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png"" -blur 5 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
                    Else
                        ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png"" -blur 5 """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png""")
                    End If

                    ScriptBatchFile.WriteLine("""" & MySettings.PathToMagick & """ convert """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain.png"" -dither None -remap """ & MySettings.PathToScriptsFolder & "\wpscript\terrain\" & MySettings.Terrain & ".png"" """ & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain_reduced_colors.png""")
                    ScriptBatchFile.Close()
                Next
            Catch ex As Exception
                MsgBox("File '5-magick.bat' could not be saved\n" & ex.Message)
            End Try
        End If
    End Sub

    Private Sub Btn_WPScriptExport_Click(sender As Object, e As RoutedEventArgs)
        If MySettings.PathToWorldPainterFolder = "" Then
            MsgBox("Please enter the path to the 'wpscript.exe' file")
        Else
            Try
                Dim TilesList As List(Of String) = MySelection.TilesList
                If TilesList.Count = 0 Then
                    Throw New System.Exception("No Tiles selected.")
                End If
                TilesList.Sort()

                If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                    System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
                End If

                Dim ScriptBatchFile As System.IO.StreamWriter
                For Each Tile In TilesList

                    If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                        System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                    End If

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-log-wpscript.bat", False, System.Text.Encoding.ASCII)
                    ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                    ScriptBatchFile.WriteLine()
                    ScriptBatchFile.Close()

                    ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat", False, System.Text.Encoding.ASCII)

                    ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\wpscript\backups"" mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\backups""")
                    ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\wpscript\exports"" mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\exports""")
                    ScriptBatchFile.WriteLine("if not exist """ & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files"" mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")

                    Dim LatiDir = Tile.Substring(0, 1)
                    Dim LatiNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                    Dim LongDir = Tile.Substring(3, 1)
                    Dim LongNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(4, 3), LongNumber)
                    Dim ReplacedString = LatiDir & " " & LatiNumber.ToString & " " & LongDir & " " & LongNumber.ToString
                    Dim MapVersionShort As String = "1-12"
                    Select Case MySettings.MapVersion
                        Case "1.12"
                            MapVersionShort = "1-12"
                        Case "1.12 with Cubic Chunks"
                            MapVersionShort = "1-12-cc"
                        Case "1.16+"
                            MapVersionShort = "1-16"
                    End Select
                    ScriptBatchFile.WriteLine("""" & MySettings.PathToWorldPainterFolder & """ """ & MySettings.PathToScriptsFolder & "\wpscript.js"" """ & MySettings.PathToScriptsFolder.Replace("\", "/") & "/"" " & ReplacedString & " " & MySettings.BlocksPerTile & " " & MySettings.TilesPerMap & " " & MySettings.VerticalScale & " " & MySettings.highways.ToString & " " & MySettings.streets.ToString & " " & MySettings.small_streets.ToString & " " & MySettings.buildings.ToString & " " & MySettings.borders & " " & MySettings.farms.ToString & " " & MySettings.meadows.ToString & " " & MySettings.quarrys.ToString & " " & MySettings.aerodrome.ToString & " " & MySettings.mobSpawner.ToString & " " & MySettings.animalSpawner.ToString & " " & MySettings.rivers.ToString & " " & MySettings.streams.ToString & " " & MapVersionShort)

                    ScriptBatchFile.Close()
                Next
            Catch ex As Exception
                MsgBox("File '6-wpscript.bat' could not be saved\n" & ex.Message)
            End Try
        End If
    End Sub

    Private Sub Btn_CombineExport_Click(sender As Object, e As RoutedEventArgs)
        Try

            If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If MySelection.SpawnTile = "" Then
                MsgBox("Please select a SpawnTile")
            Else
                Dim ScriptBatchFile As System.IO.StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\7-combine.bat"" >> """ & MySettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\7-combine.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("If Not exist """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & """ mkdir """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & Chr(34))
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & Chr(34))
                ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & Chr(34))
                ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & "\region""")
                ScriptBatchFile.WriteLine("copy """ & MySettings.PathToScriptsFolder & "\wpscript\exports\" & MySelection.SpawnTile & "\level.dat"" """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & Chr(34))
                ScriptBatchFile.WriteLine("copy """ & MySettings.PathToScriptsFolder & "\wpscript\exports\" & MySelection.SpawnTile & "\session.lock"" """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & Chr(34))
                ScriptBatchFile.WriteLine("pushd """ & MySettings.PathToScriptsFolder & "\wpscript\exports\""")
                If MySettings.PathToExport = "" Then
                    ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y ""%%i"" """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & "\region\""")
                Else
                    ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y ""%%i"" """ & MySettings.PathToExport & "\" & MySettings.WorldName & "\region\""")
                End If
                ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y ""%%i"" """ & MySettings.PathToScriptsFolder & "\" & MySettings.WorldName & "\region\""")
                ScriptBatchFile.WriteLine("popd")
                ScriptBatchFile.Close()
            End If
        Catch ex As Exception
            MsgBox("File '7-combine.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_CleanUpExport_Click(sender As Object, e As RoutedEventArgs)
        Try

            Dim TilesList As List(Of String) = MySelection.TilesList
            If TilesList.Count = 0 Then
                Throw New System.Exception("No Tiles selected.")
            End If
            TilesList.Sort()

            If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            For Each Tile In TilesList

                If (Not System.IO.Directory.Exists(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat"" >> """ & MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & Chr(34))
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))
                ScriptBatchFile.Close()
            Next

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & MySettings.PathToScriptsFolder & "\8-cleanup.bat"" >> """ & MySettings.PathToScriptsFolder & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\image_exports""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\image_exports""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & MySettings.PathToScriptsFolder & "\batchfiles""")
            ScriptBatchFile.WriteLine("mkdir """ & MySettings.PathToScriptsFolder & "\batchfiles""")
            ScriptBatchFile.Close()
        Catch ex As Exception
            MsgBox("File '8-cleanup.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

#End Region
    Public Sub Check()
        lbl_Setting_Status.Content = "Status: incomplete"
        lbl_Selection_Numbers.Content = "Tiles selected: " & MySelection.TilesList.Count
        btn_AllExport.IsEnabled = False

        If Not MySettings.PathToMagick = "" _
            And Not MySettings.PathToQGIS = "" _
            And Not MySettings.PathToScriptsFolder = "" _
            And Not MySettings.PathToWorldPainterFolder = "" _
            And ((Not MySettings.PathToPBF = "" And MySettings.geofabrik = True And MySettings.geofabrikalreadygenerated = False) Or (MySettings.geofabrik = False) Or (MySettings.geofabrik = True And MySettings.geofabrikalreadygenerated = True)) Then
            lbl_Setting_Status.Content = "Status: complete"
            If Not MySelection.SpawnTile = "" And MySelection.TilesList.Count > 0 Then
                btn_AllExport.IsEnabled = True
            End If
        End If

        Dim worldSize As Double = 0.0
        Dim worldSizeString As String = ""
        Try
            worldSize = 4.1 * ((CType(MySettings.BlocksPerTile, Int32) / 512) ^ 2) * CType(MySelection.TilesList.Count, Int32)
            Select Case worldSize
                Case < 1
                    worldSizeString = worldSize.ToString & " KB"
                Case < 1000
                    worldSizeString = Math.Round(worldSize / 1, 2).ToString & " MB"
                Case < 1000000
                    worldSizeString = Math.Round(worldSize / 1000, 2).ToString & " GB"
                Case < 1000000000
                    worldSizeString = Math.Round(worldSize / 1000000, 2).ToString & " TB"
                Case < 1000000000000
                    worldSizeString = Math.Round(worldSize / 1000000000, 2).ToString & " PB"
            End Select
            lbl_World_Size_Content.Content = worldSizeString
        Catch ex As Exception
            worldSizeString = "0 KB"
        End Try

    End Sub

End Class

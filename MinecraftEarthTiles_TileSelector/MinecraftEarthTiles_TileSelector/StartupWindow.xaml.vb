Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms

Public Class StartupWindow

    Public Shared MySettings As New Settings
    Public Shared MySelection As New Selection

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
        Help.ShowHelp(Nothing, "MyResources/MinecraftEarthTiles.chm")
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

#End Region

#Region "Export Buttons"

    Private Sub Btn_osmbatExport_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim TilesList As List(Of String) = MySelection.TilesList
            If TilesList.Count = 0 Then
                Throw New System.Exception("No Tiles selected.")
            End If
            TilesList.Sort()
            Dim OsmScriptBatchFile As System.IO.StreamWriter
            OsmScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
            For Each Tile In TilesList
                OsmScriptBatchFile.WriteLine("if not exist " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34) & " mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))
            Next
            If MySettings.geofabrik = True Then
                OsmScriptBatchFile.WriteLine("osmconvert osm/download.osm.pbf -o=osm/output.o5m")

                For Each Tile In TilesList
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
                        borderS = LatiNumber - CType(MySettings.TilesPerMap, Int16) + 1 - 0.5
                        borderN = LatiNumber + 1.5
                    Else
                        borderS = (-1 * LatiNumber) - CType(MySettings.TilesPerMap, Int16) + 1 - 0.5
                        borderN = (-1 * LatiNumber) + 1.5
                    End If
                    If LongDir = "E" Then
                        borderW = LongNumber - 0.5
                        borderE = LongNumber + CType(MySettings.TilesPerMap, Int16) + 0.5
                    Else
                        borderW = (-1 * LongNumber) - 0.5
                        borderE = (-1 * LongNumber) + CType(MySettings.TilesPerMap, Int16) + 0.5
                    End If
                    OsmScriptBatchFile.WriteLine("osmconvert osm/output.o5m -b=" & borderW.ToString("###.#", New CultureInfo("en-US")) & "," & borderS.ToString("##.#", New CultureInfo("en-US")) & "," & (borderE).ToString("###.#", New CultureInfo("en-US")) & "," & (borderN).ToString("##.#", New CultureInfo("en-US")) & " -o=osm/" & Tile & "/output.osm")
                    OsmScriptBatchFile.WriteLine("SET folder=osm/" & Tile)

                    If MySettings.highways Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=motorway Or highway=trunk" & Chr(34) & " -o=%folder%/highway.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*" & Chr(34))
                    End If

                    If MySettings.streets Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=primary Or highway=secondary" & Chr(34) & " -o=%folder%/big_road.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=tertiary" & Chr(34) & " -o=%folder%/middle_road.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=residential" & Chr(34) & " -o=%folder%/small_road.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "waterway=river Or waterway=canal Or natural=water And water=river" & Chr(34) & " -o=%folder%/river.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "water=lake Or water=reservoir Or natural=water Or landuse=reservoir Or waterway=riverbank Or waterway=canal Or water=river" & Chr(34) & " -o=%folder%/water.osm")

                    If MySettings.streams Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "waterway=stream" & Chr(34) & " -o=%folder%/stream.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=wetland" & Chr(34) & " --drop=" & Chr(34) & "wetland=swamp" & Chr(34) & " -o=%folder%/wetland.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=wetland And wetland=swamp" & Chr(34) & " -o=%folder%/swamp.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=glacier" & Chr(34) & " -o=%folder%/glacier.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=volcano" & Chr(34) & " -o=%folder%/volcano.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=beach" & Chr(34) & " -o=%folder%/beach.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=grassland Or natural=fell Or natural=heath Or natural=scrub" & Chr(34) & " -o=%folder%/grass.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=forest" & Chr(34) & " -o=%folder%/forest.osm")

                    If MySettings.farms Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=farmland" & Chr(34) & " -o=%folder%/farmland.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=vineyard" & Chr(34) & " -o=%folder%/vineyard.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*" & Chr(34))
                    End If

                    If MySettings.meadows Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=meadow" & Chr(34) & " -o=%folder%/meadow.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*" & Chr(34))
                    End If

                    If MySettings.quarrys Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=quarry" & Chr(34) & " -o=%folder%/quarry.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=bare_rock Or natural=scree Or natural=shingle" & Chr(34) & " -o=%folder%/bare_rock.osm")

                    If MySettings.borders Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "boundary=administrative And admin_level=2" & Chr(34) & " --drop=" & Chr(34) & "natural=coastline Or admin_level=3 Or admin_level=4 Or admin_level=5 Or admin_level=6 Or admin_level=7 Or admin_level=8 Or admin_level=9 Or admin_level=10 Or admin_level=11" & Chr(34) & " -o=%folder%/border.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*" & Chr(34))
                    End If

                    If MySettings.buildings Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=commercial Or landuse=construction Or landuse=industrial Or landuse=residential Or landuse=retail" & Chr(34) & " -o=%folder%/residential.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\residential.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm" & Chr(34) & "")

                Next
            Else
                If Not MySettings.Proxy = "" Then
                    OsmScriptBatchFile.WriteLine("set http_proxy=" & MySettings.Proxy)
                End If
                For Each Tile In TilesList
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
                    OsmScriptBatchFile.WriteLine(":Download_" & Tile)
                    OsmScriptBatchFile.WriteLine("@echo on")
                    OsmScriptBatchFile.WriteLine("wget -O osm/" & Tile & "/output.osm " & Chr(34) & "http://overpass-api.de/api/interpreter?data=(node(" & borderS.ToString & "," & borderW.ToString & "," & borderN.ToString & "," & borderE.ToString & ");<;>;);out;" & Chr(34) & "")
                    OsmScriptBatchFile.WriteLine("@echo off")
                    OsmScriptBatchFile.WriteLine("FOR /F  " & Chr(34) & " usebackq " & Chr(34) & " %%A IN ('" & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm') DO set size=%%~zA")
                    OsmScriptBatchFile.WriteLine("if %size% lss 1000 (")
                    OsmScriptBatchFile.WriteLine("	goto :UserInputNeeded_" & Tile)
                    OsmScriptBatchFile.WriteLine(") else (")
                    OsmScriptBatchFile.WriteLine("	goto :Continue_" & Tile)
                    OsmScriptBatchFile.WriteLine(")")
                    OsmScriptBatchFile.WriteLine(":UserInputNeeded_" & Tile)
                    OsmScriptBatchFile.WriteLine("set /p E=OSM file seems incomplete. Do you want to re-download the file? [Y/N]:")
                    OsmScriptBatchFile.WriteLine("if /I '%E%'=='Y' goto :Download_" & Tile)
                    OsmScriptBatchFile.WriteLine("if /I '%E%'=='N' goto :Continue_" & Tile)
                    OsmScriptBatchFile.WriteLine(":Continue_" & Tile)
                    OsmScriptBatchFile.WriteLine("@echo on")
                    OsmScriptBatchFile.WriteLine("SET folder=osm/" & Tile)

                    If MySettings.highways Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=motorway Or highway=trunk" & Chr(34) & " -o=%folder%/highway.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*" & Chr(34))
                    End If

                    If MySettings.streets Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=primary Or highway=secondary" & Chr(34) & " -o=%folder%/big_road.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=tertiary" & Chr(34) & " -o=%folder%/middle_road.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "highway=residential" & Chr(34) & " -o=%folder%/small_road.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "waterway=river Or waterway=canal Or natural=water And water=river" & Chr(34) & " -o=%folder%/river.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "water=lake Or water=reservoir Or natural=water Or landuse=reservoir Or waterway=riverbank Or waterway=canal Or water=river" & Chr(34) & " -o=%folder%/water.osm")

                    If MySettings.streams Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "waterway=stream" & Chr(34) & " -o=%folder%/stream.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=wetland" & Chr(34) & " --drop=" & Chr(34) & "wetland=swamp" & Chr(34) & " -o=%folder%/wetland.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=wetland And wetland=swamp" & Chr(34) & " -o=%folder%/swamp.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=glacier" & Chr(34) & " -o=%folder%/glacier.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=volcano" & Chr(34) & " -o=%folder%/volcano.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=beach" & Chr(34) & " -o=%folder%/beach.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "natural=grassland Or natural=fell Or natural=heath Or natural=scrub" & Chr(34) & " -o=%folder%/grass.osm")
                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=forest" & Chr(34) & " -o=%folder%/forest.osm")

                    If MySettings.farms Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=farmland" & Chr(34) & " -o=%folder%/farmland.osm")
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=vineyard" & Chr(34) & " -o=%folder%/vineyard.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*" & Chr(34))
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*" & Chr(34))
                    End If

                    If MySettings.meadows Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=meadow" & Chr(34) & " -o=%folder%/meadow.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*" & Chr(34))
                    End If

                    If MySettings.quarrys Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=quarry" & Chr(34) & " -o=%folder%/quarry.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=bare_rock Or natural=scree Or natural=shingle" & Chr(34) & " -o=%folder%/bare_rock.osm")

                    If MySettings.borders Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "boundary=administrative And admin_level=2" & Chr(34) & " --drop=" & Chr(34) & "natural=coastline Or admin_level=3 Or admin_level=4 Or admin_level=5 Or admin_level=6 Or admin_level=7 Or admin_level=8 Or admin_level=9 Or admin_level=10 Or admin_level=11" & Chr(34) & " -o=%folder%/border.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*" & Chr(34))
                    End If

                    If MySettings.buildings Then
                        OsmScriptBatchFile.WriteLine("osmfilter %folder%/output.osm --verbose --keep=" & Chr(34) & "landuse=commercial Or landuse=construction Or landuse=industrial Or landuse=residential Or landuse=retail" & Chr(34) & " -o=%folder%/residential.osm")
                    Else
                        OsmScriptBatchFile.WriteLine("xcopy /y " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\empty.osm" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\residential.osm*" & Chr(34))
                    End If

                    OsmScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm" & Chr(34) & "")

                Next
            End If

            OsmScriptBatchFile.Close()
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

            For Each Tile In TilesList
                Dim pythonFile As System.IO.StreamWriter
                pythonFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\python\" & Tile.ToString & ".py", False, System.Text.Encoding.ASCII)
                pythonFile.WriteLine("import os" & Environment.NewLine &
                                    "from PyQt5.QtCore import *" & Environment.NewLine &
                                    Environment.NewLine &
                                    "path = '" & MySettings.PathToScriptsFolder.Replace("\", "/") & "/'" & Environment.NewLine &
                                    "tile = '" & Tile & "'" & Environment.NewLine &
                                    "scale = " & MySettings.BlocksPerTile & Environment.NewLine &
                                    "TilesPerMap = " & MySettings.TilesPerMap & Environment.NewLine &
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

                pythonFile.WriteLine(My.Resources.ResourceManager.GetString("basescript"))
                pythonFile.WriteLine("os.kill(os.getpid(), 9)")
                pythonFile.Close()
            Next

            Dim ScriptBatchFile As System.IO.StreamWriter
            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\2-qgis.bat", False, System.Text.Encoding.ASCII)
            For Each Tile In TilesList
                ScriptBatchFile.WriteLine("if not exist " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & Chr(34) & " mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & Chr(34))
                ScriptBatchFile.WriteLine("if not exist " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap" & Chr(34) & " mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap" & Chr(34))
                ScriptBatchFile.WriteLine("xcopy " & Chr(34) & MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\OsmData" & Chr(34) & " /Y")
                ScriptBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\qgis-bin.exe" & Chr(34) & " --project " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz" & Chr(34) & " --code " & Chr(34) & MySettings.PathToScriptsFolder & "\python\" & Tile & ".py" & Chr(34))
            Next
            ScriptBatchFile.Close()
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
                Dim GdalBatchFile As System.IO.StreamWriter
                GdalBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\3-tartool.bat", False, System.Text.Encoding.ASCII)
                If Not MySettings.Proxy = "" Then
                    GdalBatchFile.WriteLine("set ftp_proxy=" & MySettings.Proxy)
                End If
                For Each Tile In TilesList

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
                    GdalBatchFile.WriteLine("wget -O image_exports/" & Tile & "/" & NewTile & ".tar.gz " & Chr(34) & "ftp://ftp.eorc.jaxa.jp/pub/ALOS/ext1/AW3D30/release_v1903/" & TilesRounded & "/" & TilesOneMoreDigit & ".tar.gz" & Chr(34))
                    GdalBatchFile.WriteLine("TarTool image_exports/" & Tile & "/" & NewTile & ".tar.gz image_exports/" & Tile & "/heightmap")

                Next
                GdalBatchFile.Close()
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
                    Dim GdalBatchFile As System.IO.StreamWriter
                    GdalBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\4-gdal.bat", False, System.Text.Encoding.ASCII)
                    For Each Tile In TilesList

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
                            CombinedString &= "@python3 " & Chr(34) & MySettings.PathToQGIS & "/apps/Python37/Scripts/gdal_merge.py" & Chr(34) & " -o " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34)
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

                                    CombinedString &= " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigitTemp & "\" & TilesOneMoreDigitTemp & "_AVE_DSM.tif" & Chr(34)

                                Next
                            Next
                            GdalBatchFile.WriteLine(CombinedString)
                        End If

                        Select Case MySettings.VerticalScale
                            Case "100"
                                GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -6200 6547300 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                            Case "50"
                                GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -3100 3273650 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                            Case "25"
                                GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1550 1636825 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                            Case "10"
                                GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -620 654730‬ 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                            Case "5"
                                GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -310 327365 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                        End Select
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -512 15872‬ 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png" & Chr(34))
                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & Chr(34) & "\bin\gdaldem.exe" & " slope " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif" & Chr(34) & " -s 111120 -compute_edges")
                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToQGIS & "\bin\gdal_translate.exe" & Chr(34) & " -a_nodata none -outsize " & MySettings.BlocksPerTile & " " & MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale 0 90 0 65535 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_slope.png" & Chr(34))

                    Next
                    GdalBatchFile.Close()
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
                Dim GdalBatchFile As System.IO.StreamWriter
                GdalBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\5-magick.bat", False, System.Text.Encoding.ASCII)

                Dim NumberOfResize = "( +clone -resize 50%% )"
                Dim NumberOfResizeWater = "( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                Dim TilesSize As Double = CType(MySettings.BlocksPerTile, Double)
                While TilesSize >= 2
                    NumberOfResize &= " ( +clone -resize 50%% )"
                    NumberOfResizeWater &= " ( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                    TilesSize /= 2
                End While
                For Each Tile In TilesList

                    Dim NewTile As String = Tile

                    If CType(MySettings.TilesPerMap, Int16) = 1 Then

                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain.png" & Chr(34) & " -dither None -remap " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\terrain\" & MySettings.Terrain & ".png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain_reduced_colors.png" & Chr(34))
                        If MySettings.Heightmap_Error_Correction = True Then
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png" & Chr(34) & " -transparent black -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png" & Chr(34) & " -channel A -morphology EdgeIn Diamond -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png" & Chr(34) & " " & NumberOfResize & " -layers RemoveDups -filter Gaussian -resize " & MySettings.BlocksPerTile & "x" & MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png" & Chr(34) & " -compose over -composite -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water.png" & Chr(34) & " -threshold 5%% -alpha off -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png" & Chr(34) & " -transparent white -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png" & Chr(34) & " -compose over -composite -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png" & Chr(34) & " -transparent black -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png" & Chr(34) & " -channel A -morphology EdgeIn Diamond -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png" & Chr(34) & " -morphology Dilate Gaussian " & NumberOfResizeWater & " -layers RemoveDups -filter Gaussian -resize " & MySettings.BlocksPerTile & "x" & MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png" & Chr(34))
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png" & Chr(34) & " -compose over -composite -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34))
                        Else
                            GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34))
                        End If
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_bathymetry.png" & Chr(34) & " -transparent black -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_bathymetry_transparent.png" & Chr(34))
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_bathymetry_transparent.png" & Chr(34) & " -compose over -composite -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png" & Chr(34))
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_bathymetry.png" & Chr(34) & " -evaluate divide 256 -depth 16 -alpha off " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_bathymetry.png" & Chr(34))
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_bathymetry.png" & Chr(34) & " -transparent black -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_bathymetry_transparent.png" & Chr(34))
                        'GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_bathymetry_transparent.png" & Chr(34) & " -compose over -composite -depth 16 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34))
                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34) & " -blur 5 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34))

                    Else
                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain.png" & Chr(34) & " -dither None -remap " & Chr(34) & MySettings.PathToScriptsFolder & "\QGIS\terrain\" & MySettings.Terrain & ".png" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain_reduced_colors.png" & Chr(34))
                        GdalBatchFile.WriteLine(Chr(34) & MySettings.PathToMagick & Chr(34) & " convert " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34) & " -blur 5 " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png" & Chr(34))

                    End If

                Next
                GdalBatchFile.Close()
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
                Dim ScriptBatchFile As System.IO.StreamWriter
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\6-wpscript.bat", False, System.Text.Encoding.ASCII)
                For Each Tile In TilesList
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
                        Case "1.14+"
                            MapVersionShort = "1-14"
                    End Select
                    ScriptBatchFile.WriteLine(Chr(34) & MySettings.PathToWorldPainterFolder & Chr(34) & " wpscript.js " & Chr(34) & MySettings.PathToScriptsFolder.Replace("\", "/") & "/" & Chr(34) & " " & ReplacedString & " " & MySettings.BlocksPerTile & " " & MySettings.TilesPerMap & " " & MySettings.VerticalScale & " " & MySettings.highways.ToString & " " & MySettings.streets.ToString & " " & MySettings.buildings.ToString & " " & MySettings.borders.ToString & " " & MySettings.farms.ToString & " " & MySettings.meadows.ToString & " " & MySettings.quarrys.ToString & " " & MySettings.streets.ToString & " " & MapVersionShort)
                Next
                ScriptBatchFile.Close()
            Catch ex As Exception
                MsgBox("File '6-wpscript.bat' could not be saved\n" & ex.Message)
            End Try
        End If
    End Sub

    Private Sub Btn_CombineExport_Click(sender As Object, e As RoutedEventArgs)
        Try
            If MySelection.SpawnTile = "" Then
                MsgBox("Please select a SpawnTile")
            Else
                Dim ScriptBatchFile As System.IO.StreamWriter
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\7-combine.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("If Not exist " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34) & " mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34))
                ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34))
                ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34))
                ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\world\region" & Chr(34))
                ScriptBatchFile.WriteLine("copy " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\exports\" & MySelection.SpawnTile & "\level.dat" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34))
                ScriptBatchFile.WriteLine("copy " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\exports\" & MySelection.SpawnTile & "\session.lock" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\world" & Chr(34))
                ScriptBatchFile.WriteLine("pushd " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\exports\" & Chr(34))
                ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y " & Chr(34) & "%%i" & Chr(34) & " " & Chr(34) & MySettings.PathToScriptsFolder & "\world\region\" & Chr(34))
                ScriptBatchFile.WriteLine("popd")
                ScriptBatchFile.Close()
            End If
        Catch ex As Exception
            MsgBox("File '7-combine.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_CleanUpExport_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim ScriptBatchFile As System.IO.StreamWriter
            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\image_exports" & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\osm" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\osm" & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\backups" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\backups" & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files" & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\exports" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\wpscript\exports" & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S " & Chr(34) & MySettings.PathToScriptsFolder & "\python" & Chr(34))
            ScriptBatchFile.WriteLine("mkdir " & Chr(34) & MySettings.PathToScriptsFolder & "\python" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\osmscript.txt" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\qgis.txt" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\0-all.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\1-osmconvert.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\2-qgis.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\3-tartool.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\4-gdal.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\5-magick.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\6-wpscript.bat" & Chr(34))
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\7-combine.bat" & Chr(34))
            'it's important to delete this cleanup file last
            ScriptBatchFile.WriteLine("del " & Chr(34) & MySettings.PathToScriptsFolder & "\8-cleanup.bat" & Chr(34))
            ScriptBatchFile.Close()
        Catch ex As Exception
            MsgBox("File '8-cleanup.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

    Private Sub Btn_AllExport_Click(sender As Object, e As RoutedEventArgs)
        Btn_osmbatExport_Click(sender, e)
        Btn_QgisExport_Click(sender, e)
        Btn_tartool_Click(sender, e)
        Btn_gdalExport_Click(sender, e)
        Btn_magick_Export_Click(sender, e)
        Btn_WPScriptExport_Click(sender, e)
        Btn_CombineExport_Click(sender, e)
        Btn_CleanUpExport_Click(sender, e)
        Try
            Dim ScriptBatchFile As System.IO.StreamWriter
            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(MySettings.PathToScriptsFolder & "\0-all.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("@echo Started %date% %time%")
            ScriptBatchFile.WriteLine("TITLE Convert OSM data")
            ScriptBatchFile.WriteLine("Call 1-osmconvert.bat")
            ScriptBatchFile.WriteLine("TITLE Export images using QGIS")
            ScriptBatchFile.WriteLine("Call 2-qgis.bat")
            ScriptBatchFile.WriteLine("TITLE Download heightmap")
            ScriptBatchFile.WriteLine("Call 3-tartool.bat")
            ScriptBatchFile.WriteLine("TITLE Convert heightmaps")
            ScriptBatchFile.WriteLine("Call 4-gdal.bat")
            ScriptBatchFile.WriteLine("TITLE Convert a lot of images")
            ScriptBatchFile.WriteLine("Call 5-magick.bat")
            ScriptBatchFile.WriteLine("TITLE Generating worlds with WorldPainter")
            ScriptBatchFile.WriteLine("Call 6-wpscript.bat")
            ScriptBatchFile.WriteLine("TITLE Combine world")
            ScriptBatchFile.WriteLine("Call 7-combine.bat")
            ScriptBatchFile.WriteLine("TITLE Finished at %date% %time%")
            ScriptBatchFile.WriteLine("@echo Completed at %date% %time%")
            ScriptBatchFile.WriteLine("PAUSE")
            ScriptBatchFile.Close()
        Catch ex As Exception
            MsgBox("File '0-all.bat' could not be saved\n" & ex.Message)
        End Try
    End Sub

#End Region
    Public Sub Check()
        lbl_Setting_Status.Content = "Status: incomplete"
        lbl_Selection_Numbers.Content = "Tiles selected: " & MySelection.TilesList.Count
        btn_osmbatExport.IsEnabled = False
        btn_QgisExport.IsEnabled = False
        btn_tartool.IsEnabled = False
        btn_gdalExport.IsEnabled = False
        btn_Magick_Export.IsEnabled = False
        btn_WPScriptExport.IsEnabled = False
        btn_CombineExport.IsEnabled = False
        btn_CleanUpExport.IsEnabled = False
        btn_AllExport.IsEnabled = False

        If Not MySettings.PathToMagick = "" And Not MySettings.PathToQGIS = "" And Not MySettings.PathToScriptsFolder = "" And Not MySettings.PathToWorldPainterFolder = "" Then
            lbl_Setting_Status.Content = "Status: complete"
            If Not MySelection.SpawnTile = "" And MySelection.TilesList.Count > 0 Then
                btn_osmbatExport.IsEnabled = True
                btn_QgisExport.IsEnabled = True
                btn_tartool.IsEnabled = True
                btn_gdalExport.IsEnabled = True
                btn_Magick_Export.IsEnabled = True
                btn_WPScriptExport.IsEnabled = True
                btn_CombineExport.IsEnabled = True
                btn_CleanUpExport.IsEnabled = True
                btn_AllExport.IsEnabled = True
            End If
        End If
    End Sub

End Class

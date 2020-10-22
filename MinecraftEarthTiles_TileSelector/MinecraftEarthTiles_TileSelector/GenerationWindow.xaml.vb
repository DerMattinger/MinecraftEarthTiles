Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Windows.Threading

Public Class GenerationWindow

    Private Property stateQGIS As Boolean = False
    Private Property keepRunning As Boolean = True

    Private neuerthread As System.Threading.Thread
    Private cts As CancellationTokenSource = New CancellationTokenSource
    Private myOptions As ParallelOptions = New ParallelOptions()

    Private tilesReady As Int32 = 0
    Private maxTiles As Int32 = StartupWindow.MySelection.TilesList.Count + 3

    Private Sub Window_Loaded() Handles MyBase.Loaded
        InitializeComponent()
        dgr_Tiles.ItemsSource = StartupWindow.MyGeneration

        keepRunning = True

        myOptions.MaxDegreeOfParallelism = CType(StartupWindow.MySettings.NumberOfCores, Int16)
        myOptions.CancellationToken = cts.Token
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earthtiles.motfe.net/")
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub Start_Click(sender As Object, e As RoutedEventArgs)
        keepRunning = True
        btn_Start_Selection.IsEnabled = False
        neuerthread = New System.Threading.Thread(AddressOf Me.Tile_Generation)
        neuerthread.Start()
    End Sub

    Private Sub Stop_Click(sender As Object, e As RoutedEventArgs)
        Try
            cts.Cancel()
        Catch ex As Exception
        End Try
        Try
            If Not neuerthread Is Nothing Then
                neuerthread.Abort()
            End If
        Catch ex As Exception
        End Try
        keepRunning = False
        Close()
    End Sub

    Private Sub Tile_Generation()

        Dim currentProcess As New System.Diagnostics.Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If StartupWindow.MySettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        If keepRunning Then
            For Each Tile In StartupWindow.MyGeneration
                Try
                    If Tile.TileName = "Convert pbf" Then

                        Dispatcher.Invoke(Sub()
                                              Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                          End Sub)

                        Dim filesList As New List(Of String) From {
                            StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm",
                            StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz",
                            StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz.cfg"
                        }
                        If StartupWindow.MySettings.bathymetry Then
                            filesList.Add(StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles_no-bathymetry.qgz")
                            filesList.Add(StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\TifFiles\bathymetry.tif")
                        End If

                        For Each myFile In filesList
                            If keepRunning And Not My.Computer.FileSystem.FileExists(myFile) Then
                                Tile.Comment = "Error: '" & myFile & "' not found"
                                keepRunning = False
                            End If
                        Next

                        If (StartupWindow.MySettings.geofabrikalreadygenerated = False And StartupWindow.MySettings.geofabrik = True) Or (StartupWindow.MySettings.geofabrikalreadygenerated = True And StartupWindow.MySettings.geofabrik = True And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\osm\output.o5m")) Then

                            If Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToPBF) Then
                                Tile.Comment = "Error: *.pbf file not found"
                                keepRunning = False
                            End If

                            If keepRunning Then

                                If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat") Then
                                    currentProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat"
                                    currentProcess = System.Diagnostics.Process.Start(currentProcessInfo)
                                    Tile.Comment = "Converting OSM data"
                                    currentProcess.WaitForExit()
                                    currentProcess.Close()
                                    Tile.GenerationProgress = 100
                                    tilesReady += 1
                                    Dispatcher.Invoke(Sub()
                                                          Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                                      End Sub)
                                    Tile.Comment = "Finished"
                                Else
                                    Tile.GenerationProgress = 100
                                    Tile.Comment = "1-osmconvert.bat not found"
                                End If

                                If Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\osm\unfiltered.o5m") Then
                                    Tile.Comment = "Error: Could not convert *.pbf file"
                                    keepRunning = False
                                End If

                                If Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\osm\output.o5m") Then
                                    Tile.Comment = "Error: Could not filter *.o5m file"
                                    keepRunning = False
                                End If

                            End If

                        Else

                            Tile.GenerationProgress = 100
                            tilesReady += 1
                            Dispatcher.Invoke(Sub()
                                                  Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                              End Sub)
                            Tile.Comment = "Skipped"

                        End If

                    End If

                Catch ex As Exception
                    Tile.Comment = ex.Message
                End Try
            Next
        End If

        Parallel.ForEach(StartupWindow.MyGeneration,
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then
                                Try

                                    Dim currentParallelProcess As New System.Diagnostics.Process
                                    Dim currentParallelProcessInfo As New ProcessStartInfo

                                    If StartupWindow.MySettings.cmdVisibility = False Then
                                        currentParallelProcessInfo.CreateNoWindow = True
                                        currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                        currentParallelProcessInfo.UseShellExecute = False
                                    End If

                                    If StartupWindow.MySettings.geofabrik = False Then
                                        currentParallelProcessInfo.CreateNoWindow = False
                                        currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
                                        currentParallelProcessInfo.UseShellExecute = True
                                    End If

                                    If keepRunning Then
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat") Then
                                            Tile.Comment = "Converting OSM data"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            currentParallelProcess.WaitForExit()
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 15
                                        Else
                                            Tile.GenerationProgress = 15
                                            Tile.Comment = Tile.TileName & "\1-osmconvert.bat not found"
                                        End If
                                    End If

                                    If StartupWindow.MySettings.cmdVisibility = False Then
                                        currentParallelProcessInfo.CreateNoWindow = True
                                        currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                        currentParallelProcessInfo.UseShellExecute = False
                                    End If

                                    Dim filesList As New List(Of String) From {
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\beach.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\big_road.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\border.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\farmland.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\forest.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\glacier.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\grass.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\highway.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\meadow.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\middle_road.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\quarry.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\river.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\small_road.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\stream.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\swamp.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\urban.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\vineyard.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\volcano.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\water.osm",
                                        StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\wetland.osm"
                                    }

                                    For Each myFile In filesList
                                        If keepRunning And Not My.Computer.FileSystem.FileExists(myFile) Then
                                            Tile.Comment = "Error: '" & myFile & "' not found"
                                            keepRunning = False
                                        End If
                                    Next

                                    Dim timer As Int16 = CType(Math.Ceiling(Rnd() * 30) + 1, Int16)
                                    Threading.Thread.Sleep(timer)

                                    While stateQGIS = True
                                        Tile.Comment = "Waiting for QGIS"
                                        Threading.Thread.Sleep(timer)
                                    End While

                                    If keepRunning Then
                                        stateQGIS = True
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-log-qgis.bat") Then
                                            Tile.Comment = "Creating images with QGIS"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-log-qgis.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            currentParallelProcess.WaitForExit()
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 30
                                        Else
                                            Tile.GenerationProgress = 30
                                            Tile.Comment = Tile.TileName & "\2-qgis.bat not found"
                                        End If
                                        stateQGIS = False
                                    End If

                                    If Directory.Exists(Path.GetTempPath) Then
                                        For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*QGIS")
                                            File.Delete(_file)
                                        Next
                                    End If

                                    If Directory.Exists(Path.GetTempPath) Then
                                        For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*osm")
                                            File.Delete(_file)
                                        Next
                                    End If

                                    If keepRunning And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & ".png") Then
                                        Tile.Comment = "Error: " & Tile.TileName & ".png not found"
                                        keepRunning = False
                                    End If

                                    If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 Then

                                        If keepRunning Then
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat") Then
                                                Tile.Comment = "Downloading heightmap"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                currentParallelProcess.WaitForExit()
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 45
                                            Else
                                                Tile.GenerationProgress = 45
                                                Tile.Comment = Tile.TileName & "\3-tartool.bat not found"
                                            End If
                                        End If

                                        If keepRunning Then
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat") Then
                                                Tile.Comment = "Process heightmap"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                currentParallelProcess.WaitForExit()
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 60
                                            Else
                                                Tile.GenerationProgress = 60
                                                Tile.Comment = Tile.TileName & "\4-gdal.bat not found"
                                            End If
                                        End If

                                    End If

                                    If keepRunning Then
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat") Then
                                            Tile.Comment = "Processing images"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            currentParallelProcess.WaitForExit()
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 75
                                        Else
                                            Tile.GenerationProgress = 75
                                            Tile.Comment = Tile.TileName & "\5-magick.bat not found"
                                        End If
                                    End If

                                    If keepRunning And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & "_terrain_reduced_colors.png") Then
                                        Tile.Comment = "Error: " & Tile.TileName & "_terrain_reduced_colors.png not found"
                                        keepRunning = False
                                    End If

                                    If keepRunning Then
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat") Then
                                            Tile.Comment = "Creating world with WorldPainter"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            currentParallelProcess.WaitForExit()
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 90
                                        Else
                                            Tile.GenerationProgress = 90
                                            Tile.Comment = Tile.TileName & "\6-wpscript.bat not found"
                                        End If

                                    End If

                                    If keepRunning And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & Tile.TileName & "\level.dat") Then
                                        Tile.Comment = "Error: " & Tile.TileName & "\level.dat not found"
                                        keepRunning = False
                                    End If

                                    If keepRunning Then
                                        If StartupWindow.MySettings.KeepTemporaryFiles = False Then
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat") Then
                                                Tile.Comment = "Cleaning up my mess"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                currentParallelProcess.WaitForExit()
                                                currentParallelProcess.Close()
                                            End If
                                        End If
                                        Tile.GenerationProgress = 100
                                        tilesReady += 1
                                        Dispatcher.Invoke(Sub()
                                                              Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                                          End Sub)
                                        Tile.Comment = "Finished"
                                    End If

                                Catch ex As Exception
                                    Tile.Comment = ex.Message
                                End Try

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In StartupWindow.MyGeneration
                If Tile.TileName = "Combining" Then
                    If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat") Then
                        Tile.Comment = "Combining world"
                        currentProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat"
                        currentProcess = System.Diagnostics.Process.Start(currentProcessInfo)
                        currentProcess.WaitForExit()
                        currentProcess.Close()
                        Tile.GenerationProgress = 100
                        tilesReady += 1
                        Dispatcher.Invoke(Sub()
                                              Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                          End Sub)
                        Tile.Comment = "Finished"
                    Else
                        Tile.GenerationProgress = 90
                        Tile.Comment = "7-combine.bat not found"
                    End If
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In StartupWindow.MyGeneration
                If Tile.TileName = "Cleanup" Then
                    If StartupWindow.MySettings.KeepTemporaryFiles = False Then
                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\8-log-cleanup.bat") Then

                            Tile.Comment = "Cleaning up my mess"
                            currentProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\8-log-cleanup.bat"
                            currentProcess = System.Diagnostics.Process.Start(currentProcessInfo)
                            currentProcess.WaitForExit()
                            currentProcess.Close()
                            Tile.GenerationProgress = 100
                            tilesReady += 1
                            Dispatcher.Invoke(Sub()
                                                  Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                              End Sub)
                            Tile.Comment = "Finished"
                            MsgBox("Generation of world '" & StartupWindow.MySettings.WorldName & "' completed")
                        Else
                            Tile.GenerationProgress = 100
                            Tile.Comment = "8-cleanup.bat not found"
                        End If
                    Else
                        Tile.GenerationProgress = 100
                        tilesReady += 1
                        Dispatcher.Invoke(Sub()
                                              Title = "Tile Generation - " & Math.Round((tilesReady / maxTiles) * 100, 1) & "%"
                                          End Sub)
                        Tile.Comment = "Skipped"
                        MsgBox("Generation of world '" & StartupWindow.MySettings.WorldName & "' completed")
                    End If
                End If
            Next
        End If

    End Sub

End Class

Imports System.IO
Imports System.Threading
Imports System.Windows.Threading
Imports System.Globalization

Public Class GenerationWindow

    Private Property stateQGIS As Boolean = False
    Private Property keepRunning As Boolean = True

    Private neuerthread As System.Threading.Thread
    Private cts As CancellationTokenSource = New CancellationTokenSource
    Private myOptions As ParallelOptions = New ParallelOptions()

    Private processList As List(Of System.Diagnostics.Process) = New List(Of System.Diagnostics.Process)

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
        keepRunning = False
        Close()
    End Sub

    Private Sub Start_Click(sender As Object, e As RoutedEventArgs)
        keepRunning = True
        btn_Start_Selection.IsEnabled = False
        neuerthread = New System.Threading.Thread(AddressOf Me.Tile_Generation)
        neuerthread.Start()
    End Sub

    Private Sub Stop_Click(sender As Object, e As RoutedEventArgs)
        keepRunning = False
        Try
            cts.Cancel()
        Catch ex As Exception
        End Try
        For Each SingleProcess In processList
            SingleProcess.Close()
        Next
        Try
            If Not neuerthread Is Nothing Then
                neuerthread.Abort()
            End If
        Catch ex As Exception
        End Try
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

                        CleanUpFinalExport()
                        If (StartupWindow.MySettings.geofabrikalreadygenerated = False And StartupWindow.MySettings.geofabrik = True) Or (StartupWindow.MySettings.geofabrikalreadygenerated = True And StartupWindow.MySettings.geofabrik = True And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\osm\output.o5m")) Then

                            If Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToPBF) Then
                                Tile.Comment = "Error: *.pbf file not found"
                                keepRunning = False
                            End If

                            If keepRunning Then

                                osmbatExportPrepare()
                                If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat") Then
                                    currentProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat"
                                    currentProcess = System.Diagnostics.Process.Start(currentProcessInfo)
                                    Tile.Comment = "Converting OSM data"
                                    processList.Add(currentProcess)
                                    currentProcess.WaitForExit()
                                    processList.Remove(currentProcess)
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

                                    Dim keepRunningLocal As Boolean = True

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

                                    If keepRunning And keepRunningLocal Then
                                        osmbatExport(Tile.TileName)

                                        If StartupWindow.MySettings.geofabrik = True Then
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat") Then
                                                Tile.Comment = "Converting OSM data"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                processList.Add(currentParallelProcess)
                                                currentParallelProcess.WaitForExit()
                                                processList.Remove(currentParallelProcess)
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 15
                                            Else
                                                Tile.GenerationProgress = 15
                                                Tile.Comment = Tile.TileName & "\1-osmconvert.bat not found"
                                            End If
                                        Else
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-osmconvert.bat") Then
                                                Tile.Comment = "Converting OSM data"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-osmconvert.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                processList.Add(currentParallelProcess)
                                                currentParallelProcess.WaitForExit()
                                                processList.Remove(currentParallelProcess)
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 15
                                            Else
                                                Tile.GenerationProgress = 15
                                                Tile.Comment = Tile.TileName & "\1-osmconvert.bat not found"
                                            End If
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
                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(myFile) Then
                                            Tile.Comment = "Error: '" & myFile & "' not found"
                                            If StartupWindow.MySettings.continueGeneration Then
                                                keepRunningLocal = False
                                            Else
                                                keepRunning = False
                                            End If
                                        End If
                                    Next

                                    Dim timer As Int16 = CType(Math.Ceiling(Rnd() * 30) + 1, Int16)
                                    Threading.Thread.Sleep(timer)

                                    While stateQGIS = True
                                        Tile.Comment = "Waiting for QGIS"
                                        Threading.Thread.Sleep(timer)
                                    End While

                                    If keepRunning And keepRunningLocal Then
                                        stateQGIS = True
                                        QgisExport(Tile.TileName)
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-log-qgis.bat") Then
                                            Tile.Comment = "Creating images with QGIS"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-log-qgis.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            processList.Add(currentParallelProcess)
                                            currentParallelProcess.WaitForExit()
                                            processList.Remove(currentParallelProcess)
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 30
                                        Else
                                            Tile.GenerationProgress = 30
                                            Tile.Comment = Tile.TileName & "\2-qgis.bat not found"
                                        End If
                                        stateQGIS = False
                                    End If

                                    If Directory.Exists(Path.GetTempPath) Then
                                        For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*QGIS*")
                                            Try
                                                File.Delete(_file)
                                            Catch ex As Exception
                                            End Try
                                        Next
                                    End If

                                    If Directory.Exists(Path.GetTempPath) Then
                                        For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*osm*")
                                            Try
                                                File.Delete(_file)
                                            Catch ex As Exception
                                            End Try
                                        Next
                                    End If

                                    If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & ".png") Then
                                        Tile.Comment = "Error: " & Tile.TileName & ".png not found"
                                        If StartupWindow.MySettings.continueGeneration Then
                                            keepRunningLocal = False
                                        Else
                                            keepRunning = False
                                        End If
                                    End If

                                    If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 Then

                                        If keepRunning And keepRunningLocal Then
                                            tartoolExport(Tile.TileName)
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat") Then
                                                Tile.Comment = "Downloading heightmap"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                processList.Add(currentParallelProcess)
                                                currentParallelProcess.WaitForExit()
                                                processList.Remove(currentParallelProcess)
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 45
                                            Else
                                                Tile.GenerationProgress = 45
                                                Tile.Comment = Tile.TileName & "\3-tartool.bat not found"
                                            End If
                                        End If

                                        If keepRunning And keepRunningLocal Then
                                            gdalExport(Tile.TileName)
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat") Then
                                                Tile.Comment = "Process heightmap"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                processList.Add(currentParallelProcess)
                                                currentParallelProcess.WaitForExit()
                                                processList.Remove(currentParallelProcess)
                                                currentParallelProcess.Close()
                                                Tile.GenerationProgress = 60
                                            Else
                                                Tile.GenerationProgress = 60
                                                Tile.Comment = Tile.TileName & "\4-gdal.bat not found"
                                            End If
                                        End If

                                    End If

                                    If keepRunning And keepRunningLocal Then
                                        imagemagickExport(Tile.TileName)
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat") Then
                                            Tile.Comment = "Processing images"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            processList.Add(currentParallelProcess)
                                            currentParallelProcess.WaitForExit()
                                            processList.Remove(currentParallelProcess)
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 75
                                        Else
                                            Tile.GenerationProgress = 75
                                            Tile.Comment = Tile.TileName & "\5-magick.bat not found"
                                        End If
                                    End If

                                    If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & "_terrain_reduced_colors.png") Then
                                        Tile.Comment = "Error: " & Tile.TileName & "_terrain_reduced_colors.png not found"
                                        If StartupWindow.MySettings.continueGeneration Then
                                            keepRunningLocal = False
                                        Else
                                            keepRunning = False
                                        End If
                                    End If

                                    If keepRunning And keepRunningLocal Then
                                        wpScriptExport(Tile.TileName)
                                        If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat") Then
                                            Tile.Comment = "Creating world with WorldPainter"
                                            currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat"
                                            currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                            processList.Add(currentParallelProcess)
                                            currentParallelProcess.WaitForExit()
                                            processList.Remove(currentParallelProcess)
                                            currentParallelProcess.Close()
                                            Tile.GenerationProgress = 90
                                        Else
                                            Tile.GenerationProgress = 90
                                            Tile.Comment = Tile.TileName & "\6-wpscript.bat not found"
                                        End If

                                    End If

                                    If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & Tile.TileName & "\level.dat") Then
                                        Tile.Comment = "Error: " & Tile.TileName & "\level.dat not found"
                                        If StartupWindow.MySettings.continueGeneration Then
                                            keepRunningLocal = False
                                        Else
                                            keepRunning = False
                                        End If
                                    End If

                                    If keepRunning And keepRunningLocal Then
                                        If StartupWindow.MySettings.KeepTemporaryFiles = False Then
                                            CleanUpExport(Tile.TileName)
                                            If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat") Then
                                                Tile.Comment = "Cleaning up my mess"
                                                currentParallelProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat"
                                                currentParallelProcess = System.Diagnostics.Process.Start(currentParallelProcessInfo)
                                                processList.Add(currentParallelProcess)
                                                currentParallelProcess.WaitForExit()
                                                processList.Remove(currentParallelProcess)
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
                    combineExport()
                    If My.Computer.FileSystem.FileExists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat") Then
                        Tile.Comment = "Combining world"
                        currentProcessInfo.FileName = StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat"
                        currentProcess = System.Diagnostics.Process.Start(currentProcessInfo)
                        processList.Add(currentProcess)
                        currentProcess.WaitForExit()
                        processList.Remove(currentProcess)
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
                            processList.Add(currentProcess)
                            currentProcess.WaitForExit()
                            processList.Remove(currentProcess)
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


#Region "Create Batch Files"

    Private Sub osmbatExportPrepare()
        Try
            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\python\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\python\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            If StartupWindow.MySettings.geofabrik = True Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & StartupWindow.MySettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm""")
                ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports""")

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmconvert.exe"" """ & StartupWindow.MySettings.PathToPBF & """ -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\unfiltered.o5m""")

                Dim filter As String = "water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR natural=wetland OR wetland=swamp OR natural=glacier OR natural=volcano OR natural=beach OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub OR landuse=forest OR landuse=bare_rock OR natural=scree OR natural=shingle"

                If StartupWindow.MySettings.highways Then
                    filter &= " OR highway=motorway OR highway=trunk"
                End If

                If StartupWindow.MySettings.streets Then
                    filter &= " OR highway=primary OR highway=secondary OR highway=tertiary"
                End If

                If StartupWindow.MySettings.small_streets Then
                    filter &= " OR highway=residential"
                End If

                If StartupWindow.MySettings.riversBoolean Then
                    filter &= " OR waterway=river OR waterway=canal OR water=river OR waterway=riverbank"
                End If

                If StartupWindow.MySettings.streams Then
                    filter &= " OR waterway=stream"
                End If

                If StartupWindow.MySettings.farms Then
                    filter &= " OR landuse=farmland OR landuse=vineyard"
                End If

                If StartupWindow.MySettings.meadows Then
                    filter &= " OR landuse=meadow"
                End If

                If StartupWindow.MySettings.quarrys Then
                    filter &= " OR landuse=quarry"
                End If

                If StartupWindow.MySettings.aerodrome Then
                    If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 And CType(StartupWindow.MySettings.BlocksPerTile, Int16) >= 1024 Then
                        filter &= " OR aeroway=aerodrome AND iata="
                    Else
                        filter &= " OR aeroway=launchpad"
                    End If
                End If

                If StartupWindow.MySettings.buildings Then
                    filter &= " OR landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"
                End If

                If StartupWindow.MySettings.bordersBoolean = True And StartupWindow.MySettings.borders = "2020" Then
                    filter &= " OR boundary=administrative AND admin_level=2"
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\unfiltered.o5m"" --verbose --keep=""" & filter & """ -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\output.o5m""")
                ScriptBatchFile.Close()

            Else

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & StartupWindow.MySettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("")
                ScriptBatchFile.Close()

            End If

        Catch ex As Exception
            Throw New Exception("File '1-osmconvert.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub osmbatExport(Tile As String)
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            If StartupWindow.MySettings.geofabrik = True Then


                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

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
                    borderS = LatiNumber - CType(StartupWindow.MySettings.TilesPerMap, Int16) + 1 - 0.01
                    borderN = LatiNumber + 1.01
                Else
                    borderS = (-1 * LatiNumber) - CType(StartupWindow.MySettings.TilesPerMap, Int16) + 1 - 0.01
                    borderN = (-1 * LatiNumber) + 1.01
                End If
                If LongDir = "E" Then
                    borderW = LongNumber - 0.01
                    borderE = LongNumber + CType(StartupWindow.MySettings.TilesPerMap, Int16) + 0.01
                Else
                    borderW = (-1 * LongNumber) - 0.01
                    borderE = (-1 * LongNumber) + CType(StartupWindow.MySettings.TilesPerMap, Int16) + 0.01
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

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmconvert.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\output.o5m"" -b=" & borderW.ToString("##0.##", New CultureInfo("en-US")) & "," & borderS.ToString("#0.##", New CultureInfo("en-US")) & "," & (borderE).ToString("##0.##", New CultureInfo("en-US")) & "," & (borderN).ToString("#0.##", New CultureInfo("en-US")) & " -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --complete-ways --complete-multipolygons --complete-boundaries --drop-version --verbose")

                If StartupWindow.MySettings.highways Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                End If

                If StartupWindow.MySettings.streets Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                End If

                If StartupWindow.MySettings.small_streets Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR waterway=riverbank OR waterway=canal OR waterway=canal"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                If StartupWindow.MySettings.riversBoolean Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                End If

                If StartupWindow.MySettings.streams Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=stream"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland"" --drop=""wetland=swamp"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\wetland.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland AND wetland=swamp"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\swamp.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")

                If StartupWindow.MySettings.farms Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                End If

                If StartupWindow.MySettings.meadows Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                End If

                If StartupWindow.MySettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                End If

                If StartupWindow.MySettings.aerodrome Then
                    If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 And CType(StartupWindow.MySettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    Else
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")
                If StartupWindow.MySettings.buildings Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                End If

                If StartupWindow.MySettings.bordersBoolean = True And StartupWindow.MySettings.borders = "2020" Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                End If

                ScriptBatchFile.WriteLine("del """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")

                ScriptBatchFile.Close()

            Else

                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

                If Not StartupWindow.MySettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine("set http_proxy=" & StartupWindow.MySettings.Proxy)
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

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\wget.exe"" -O """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" ""http://overpass-api.de/api/interpreter?data=(node(" & borderS.ToString & "," & borderW.ToString & "," & borderN.ToString & "," & borderE.ToString & ");<;>;);out;""")
                ScriptBatchFile.WriteLine("@echo off")
                ScriptBatchFile.WriteLine("FOR /F  "" usebackq "" %%A IN ('" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm') DO set size=%%~zA")
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

                If StartupWindow.MySettings.highways Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                End If

                If StartupWindow.MySettings.streets Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """"" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"""" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                End If

                If StartupWindow.MySettings.small_streets Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR waterway=riverbank OR waterway=canal OR waterway=canal"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                If StartupWindow.MySettings.riversBoolean Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                End If

                If StartupWindow.MySettings.streams Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=stream"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland"" --drop=""wetland=swamp"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\wetland.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=wetland And wetland=swamp"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\swamp.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")

                If StartupWindow.MySettings.farms Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                End If

                If StartupWindow.MySettings.meadows Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                End If

                If StartupWindow.MySettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                End If

                If StartupWindow.MySettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                If StartupWindow.MySettings.aerodrome Then
                    If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 And CType(StartupWindow.MySettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")

                    Else
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")

                If StartupWindow.MySettings.buildings Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                End If

                If StartupWindow.MySettings.bordersBoolean = True And StartupWindow.MySettings.borders = "2020" Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\osmfilter.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                Else
                    ScriptBatchFile.WriteLine("xcopy /y """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                End If

                ScriptBatchFile.WriteLine("del """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")

                ScriptBatchFile.Close()


            End If
        Catch ex As Exception
            Throw New Exception("File '1-osmconvert.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub QgisExport(Tile As String)
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\python\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\python\")
            End If

            Dim pythonFile As System.IO.StreamWriter
            pythonFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\python\" & Tile & "_repair.py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine("import os" & Environment.NewLine &
                                "from PyQt5.QtCore import *" & Environment.NewLine &
                                "import processing")

            Dim PathReversedSlash = Replace(StartupWindow.MySettings.PathToScriptsFolder, "\", "/")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/urban.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/urban.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""broadleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/broadleaved.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""needleleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/needleleaved.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/mixedforest.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/beach.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/beach.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/grass.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/grass.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/farmland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/farmland.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/meadow.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/meadow.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/quarry.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/quarry.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/water.osm|layername=multipolygons|subset=\""natural\"" = \'water\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/water.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/glacier.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/glacier.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/wetland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/wetland.shp'})")
            pythonFile.WriteLine("processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/swamp.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/swamp.shp'})")
            pythonFile.WriteLine("os.kill(os.getpid(), 9)")
            pythonFile.Close()

            Dim pythonFile2 As System.IO.StreamWriter
            pythonFile2 = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\python\" & Tile & ".py", False, System.Text.Encoding.ASCII)
            pythonFile2.WriteLine("import os" & Environment.NewLine &
                                "from PyQt5.QtCore import *" & Environment.NewLine &
                                Environment.NewLine &
                                "path = '" & StartupWindow.MySettings.PathToScriptsFolder.Replace("\", "/") & "/'" & Environment.NewLine &
                                "tile = '" & Tile & "'" & Environment.NewLine &
                                "scale = " & StartupWindow.MySettings.BlocksPerTile & Environment.NewLine &
                                "TilesPerMap = " & StartupWindow.MySettings.TilesPerMap & Environment.NewLine &
                                "rivers = '" & StartupWindow.MySettings.rivers & "'" & Environment.NewLine &
                                "borders = " & StartupWindow.MySettings.bordersBoolean & Environment.NewLine &
                                "borderyear = '" & StartupWindow.MySettings.borders & "'" & Environment.NewLine &
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


            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-log-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-qgis.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-qgis.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\""")
            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & """ mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & """")
            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")
            'first, repair multipolygons
            ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --code """ & StartupWindow.MySettings.PathToScriptsFolder & "\python\" & Tile & "_repair.py""")
            'next, copy all files
            ScriptBatchFile.WriteLine("xcopy """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & """ """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\OsmData"" /Y")
            'open project and run script
            If StartupWindow.MySettings.bathymetry Then
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --project """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz"" --code """ & StartupWindow.MySettings.PathToScriptsFolder & "\python\" & Tile & ".py""")
            Else
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\qgis-bin.exe"" --project """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles_no-bathymetry.qgz"" --code """ & StartupWindow.MySettings.PathToScriptsFolder & "\python\" & Tile & ".py""")
            End If

            ScriptBatchFile.Close()

        Catch ex As Exception
            Throw New Exception("File '2-qgis.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub tartoolExport(Tile As String)
        Try
            If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 Then

                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
                End If

                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                Dim ScriptBatchFile As System.IO.StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-log-tartool.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat", False, System.Text.Encoding.ASCII)
                If Not StartupWindow.MySettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine("set ftp_proxy=" & StartupWindow.MySettings.Proxy)
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
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\wget.exe"" -O """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" ""ftp://ftp.eorc.jaxa.jp/pub/ALOS/ext1/AW3D30/release_v1903/" & TilesRounded & "/" & TilesOneMoreDigit & ".tar.gz""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToScriptsFolder & "\TarTool.exe"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")

                ScriptBatchFile.Close()

            End If
        Catch ex As Exception
            Throw New Exception("File '3-tartool.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub gdalExport(Tile As String)
        If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 Then
            Try

                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
                End If

                If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                    System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                Dim ScriptBatchFile As System.IO.StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-log-gdal.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat", False, System.Text.Encoding.ASCII)

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

                If CType(StartupWindow.MySettings.TilesPerMap, Int16) > 1 Then
                    Dim CombinedString As String = ""
                    CombinedString &= "@python3 """ & StartupWindow.MySettings.PathToQGIS & "/apps/Python37/Scripts/gdal_merge.py"" -o """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"""
                    For indexLati As Int32 = 0 To CType(StartupWindow.MySettings.TilesPerMap, Int16) - 1
                        For indexLongi As Int32 = 0 To CType(StartupWindow.MySettings.TilesPerMap, Int16) - 1
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

                            CombinedString &= " """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigitTemp & "\" & TilesOneMoreDigitTemp & "_AVE_DSM.tif"""

                        Next
                    Next
                    ScriptBatchFile.WriteLine(CombinedString)
                End If

                Select Case StartupWindow.MySettings.VerticalScale
                    Case "100"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -6200 6547300 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "50"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -3100 3273650 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "33"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -2046 2160609 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "25"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1550 1636825 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "10"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -620 654730‬ 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "5"
                        ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -310 327365 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                End Select
                'ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -512 15872‬ 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdaldem.exe"" slope """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" -s 111120 -compute_edges")
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale 0 90 0 65535 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_slope.png""")

                ScriptBatchFile.Close()

            Catch ex As Exception
                Throw New Exception("File '4-gdal.bat' could not be saved. " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub imagemagickExport(Tile As String)
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-log-magick.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat", False, System.Text.Encoding.ASCII)

            Dim NumberOfResize = "( +clone -resize 50%% )"
            Dim NumberOfResizeWater = "( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
            Dim TilesSize As Double = CType(StartupWindow.MySettings.BlocksPerTile, Double)
            While TilesSize >= 2
                NumberOfResize &= " ( +clone -resize 50%% )"
                NumberOfResizeWater &= " ( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                TilesSize /= 2
            End While

            Dim NewTile As String = Tile

            If CType(StartupWindow.MySettings.TilesPerMap, Int16) = 1 Then

                If StartupWindow.MySettings.Heightmap_Error_Correction = True Then
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png"" -transparent black -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_edges.png"" " & NumberOfResize & " -layers RemoveDups -filter Gaussian -resize " & StartupWindow.MySettings.BlocksPerTile & "x" & StartupWindow.MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_invalid_filled.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_removed_invalid.png"" -compose over -composite -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert -negate """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water.png"" -threshold 50%% -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert -negate """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river.png"" -threshold 50%% -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river_mask.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ composite -gravity center """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_river_mask.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert -negate """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png"" -alpha off """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_combined_mask.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_water_mask.png"" -transparent white -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_unsmoothed.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_transparent.png"" -compose over -composite -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_blacked.png"" -transparent black -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_edges.png"" -morphology Dilate Gaussian " & NumberOfResizeWater & " -layers RemoveDups -filter Gaussian -resize " & StartupWindow.MySettings.BlocksPerTile & "x" & StartupWindow.MySettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png""")
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_filled.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_water_removed.png"" -compose over -composite -depth 16 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
                Else
                    ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & "_exported.png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
                End If
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png"" -blur 5 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\heightmap\" & NewTile & ".png""")
            Else
                ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png"" -blur 5 """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & ".png""")
            End If

            ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToMagick & """ convert """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain.png"" -dither None -remap """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\terrain\" & StartupWindow.MySettings.Terrain & ".png"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & NewTile & "\" & NewTile & "_terrain_reduced_colors.png""")
            ScriptBatchFile.Close()

        Catch ex As Exception
            Throw New Exception("File '5-magick.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub wpScriptExport(Tile As String)
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-log-wpscript.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\backups"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("if not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")

            Dim LatiDir = Tile.Substring(0, 1)
            Dim LatiNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
            Dim LongDir = Tile.Substring(3, 1)
            Dim LongNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(4, 3), LongNumber)
            Dim ReplacedString = LatiDir & " " & LatiNumber.ToString & " " & LongDir & " " & LongNumber.ToString
            Dim MapVersionShort As String = "1-12"
            Select Case StartupWindow.MySettings.MapVersion
                Case "1.12"
                    MapVersionShort = "1-12"
                Case "1.12 with Cubic Chunks"
                    MapVersionShort = "1-12-cc"
                Case "1.16+"
                    MapVersionShort = "1-16"
            End Select
            ScriptBatchFile.WriteLine("""" & StartupWindow.MySettings.PathToWorldPainterFolder & """ """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript.js"" """ & StartupWindow.MySettings.PathToScriptsFolder.Replace("\", "/") & "/"" " & ReplacedString & " " & StartupWindow.MySettings.BlocksPerTile & " " & StartupWindow.MySettings.TilesPerMap & " " & StartupWindow.MySettings.VerticalScale & " " & StartupWindow.MySettings.highways.ToString & " " & StartupWindow.MySettings.streets.ToString & " " & StartupWindow.MySettings.small_streets.ToString & " " & StartupWindow.MySettings.buildings.ToString & " " & StartupWindow.MySettings.ores.ToString & " " & StartupWindow.MySettings.farms.ToString & " " & StartupWindow.MySettings.meadows.ToString & " " & StartupWindow.MySettings.quarrys.ToString & " " & StartupWindow.MySettings.aerodrome.ToString & " " & StartupWindow.MySettings.mobSpawner.ToString & " " & StartupWindow.MySettings.animalSpawner.ToString & " " & StartupWindow.MySettings.riversBoolean.ToString & " " & StartupWindow.MySettings.streams.ToString & " " & MapVersionShort)

            ScriptBatchFile.Close()
        Catch ex As Exception
            Throw New Exception("File '6-wpscript.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub CleanUpExport(Tile As String)
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports\" & Tile & Chr(34))
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))
            ScriptBatchFile.Close()

        Catch ex As Exception
            Throw New Exception("File '8-cleanup.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub combineExport()
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-combine.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\7-combine.bat", False, System.Text.Encoding.ASCII)

            If StartupWindow.MySettings.PathToExport = "" Then
                ScriptBatchFile.WriteLine("If Not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & """ mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("If Not exist """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & "\region"" mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & "\region""")
                ScriptBatchFile.WriteLine("copy """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & StartupWindow.MySelection.SpawnTile & "\level.dat"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("copy """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & StartupWindow.MySelection.SpawnTile & "\session.lock"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("pushd """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\""")
                ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y ""%%i"" """ & StartupWindow.MySettings.PathToScriptsFolder & "\" & StartupWindow.MySettings.WorldName & "\region\""")
                ScriptBatchFile.WriteLine("popd")
            Else
                ScriptBatchFile.WriteLine("If Not exist """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & """ mkdir """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("If Not exist """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & "\region"" mkdir """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & "\region""")
                ScriptBatchFile.WriteLine("copy """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & StartupWindow.MySelection.SpawnTile & "\level.dat"" """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("copy """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\" & StartupWindow.MySelection.SpawnTile & "\session.lock"" """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & """")
                ScriptBatchFile.WriteLine("pushd """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports\""")
                ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  xcopy /Y ""%%i"" """ & StartupWindow.MySettings.PathToExport & "\" & StartupWindow.MySettings.WorldName & "\region\""")
                ScriptBatchFile.WriteLine("popd")
            End If
            ScriptBatchFile.Close()

        Catch ex As Exception
            Throw New Exception("File '7-combine.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Private Sub CleanUpFinalExport()
        Try

            If (Not System.IO.Directory.Exists(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")) Then
                System.IO.Directory.CreateDirectory(StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles\")
            End If

            Dim ScriptBatchFile As System.IO.StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & StartupWindow.MySettings.PathToScriptsFolder & "\8-cleanup.bat"" >> """ & StartupWindow.MySettings.PathToScriptsFolder & "\log.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(StartupWindow.MySettings.PathToScriptsFolder & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\image_exports""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles""")
            ScriptBatchFile.WriteLine("mkdir """ & StartupWindow.MySettings.PathToScriptsFolder & "\batchfiles""")
            ScriptBatchFile.Close()
        Catch ex As Exception
            Throw New Exception("File '8-cleanup.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

#End Region

End Class

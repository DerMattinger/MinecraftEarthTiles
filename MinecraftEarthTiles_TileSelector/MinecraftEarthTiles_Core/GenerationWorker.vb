Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.DirectoryServices.AccountManagement
Imports System.Globalization
Imports System.IO
Imports System.Net.Mail
Imports System.Threading

Public Class GenerationWorker
    Implements INotifyPropertyChanged

    Private stateQGIS As Boolean = False
    Private stateWorldPainter As Boolean = False
    Public keepRunning As Boolean = True

    Public pause As Boolean = False

    Public neuerthread As Thread
    Public cts As CancellationTokenSource = New CancellationTokenSource
    Private myOptions As ParallelOptions = New ParallelOptions()

    Public processList As List(Of Process) = New List(Of Process)

    Public tilesReady As Int32 = 0
    Public maxTiles As Int32 = (ClassWorker.MySelection.TilesList.Count + 3) - ClassWorker.MySelection.VoidTiles

    Public startTime As DateTime = DateTime.Now

    Public Property hoursLeft As Int32 = 0
    Public Property minutesLeft As Int32 = 0
    Public Property hoursDone As Int32 = 0
    Public Property minutesDone As Int32 = 0

    Private _LatestMessage As String = ""

    Public Property LatestMessage As String
        Get
            Return _LatestMessage
        End Get
        Set(ByVal value As String)
            If Not _LatestMessage = value Then
                _LatestMessage = value
                OnPropertyChanged("LatestMessage")
            End If
        End Set
    End Property

    Private _RefreshPreview As String = ""

    Public Property RefreshPreview As String
        Get
            Return _RefreshPreview
        End Get
        Set(ByVal value As String)
            _RefreshPreview = value
            OnPropertyChanged("RefreshPreview")
        End Set
    End Property

    Private _GenerationComplete = False

    Public Property GenerationComplete As Boolean
        Get
            Return _GenerationComplete
        End Get
        Set(ByVal value As Boolean)
            _GenerationComplete = value
            OnPropertyChanged("GenerationComplete")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(ByVal propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property MyGeneration As New List(Of Generation)

    Public Sub New()
        myOptions.MaxDegreeOfParallelism = CType(ClassWorker.MyTilesSettings.NumberOfCores, Int16)
        myOptions.CancellationToken = cts.Token
        keepRunning = True
    End Sub

    Public Sub CreateGeneration()
        If ClassWorker.MySelection.TilesList.Count = 0 Then
            Throw New Exception("No Tiles selected.")
        End If
        MyGeneration = New List(Of Generation)
        MyGeneration.Add(New Generation("Convert pbf"))
        For Each Tile In ClassWorker.MySelection.TilesList
            MyGeneration.Add(New Generation(Tile))
        Next
        MyGeneration.Add(New Generation("Combining"))
        MyGeneration.Add(New Generation("Cleanup"))
    End Sub

    Public Sub CombineOnly_Generation()

        WriteLog("Starting Combining Only.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Convert pbf" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Tile.GenerationProgress = 100
                                tilesReady += 1
                                CalculateDuration()
                                Tile.Comment = "Skipped"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    CombineBatchExport()
                    CombineGeneration(Tile, currentProcess, currentProcessInfo)
                    If Not ClassWorker.MyTilesSettings.PathToMinutor = "" And ClassWorker.MyTilesSettings.minutor = True Then
                        MinutorFinalRenderExort(Tile.TileName)
                        MinutorFinalGeneration(Tile, currentProcess, currentProcessInfo)
                    End If
                    Tile.GenerationProgress = 100
                    Tile.Comment = "Finished"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Finished()
                End If
            Next
        End If

    End Sub

    Public Sub CleanupOnly_Generation()

        WriteLog("Starting Cleanup Only.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Convert pbf" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Dim keepRunningLocal As Boolean = True

                                Dim currentParallelProcess As New Process
                                Dim currentParallelProcessInfo As New ProcessStartInfo

                                If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                    currentParallelProcessInfo.CreateNoWindow = True
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                    currentParallelProcessInfo.UseShellExecute = False
                                End If

                                If Not Tile.TileName.Contains("x") Then

                                    Try
                                        If keepRunning And keepRunningLocal Then
                                            CleanupBatchExport(Tile.TileName)
                                            CleanupGeneration(Tile, currentProcess, currentProcessInfo)
                                            ClassWorker.MySelection.TilesList.Remove(Tile.TileName)
                                        End If

                                        tilesReady += 1

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                End If

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    CleanupFinalBatchExport()
                    CleanUpFinalGeneration(Tile, currentProcess, currentProcessInfo)
                    Finished()
                End If
            Next
        End If

    End Sub

    Public Sub OsmOnly_Generation()

        WriteLog("Starting OSM only generation.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                Try
                    If Tile.TileName = "Convert pbf" Then
                        OsmbatExportPrepare()
                        OsmbatGenerationPrepare(Tile, currentProcess, currentProcessInfo)
                    End If

                Catch ex As Exception
                    WriteLog(ex.Message)
                    Tile.Comment = ex.Message
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End Try
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Dim keepRunningLocal As Boolean = True

                                Dim currentParallelProcess As New Process
                                Dim currentParallelProcessInfo As New ProcessStartInfo

                                If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                    currentParallelProcessInfo.CreateNoWindow = True
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                    currentParallelProcessInfo.UseShellExecute = False
                                End If

                                If Not Tile.TileName.Contains("x") Then

                                    Try

                                        If ClassWorker.MyWorldSettings.geofabrik = True And ClassWorker.MyTilesSettings.reUseOsmFiles = False And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                            If Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m") Then
                                                Tile.Comment = "Error: Could not find output.o5m file"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                keepRunning = False
                                            End If
                                        End If

                                        If ClassWorker.MyWorldSettings.geofabrik = False Then
                                            currentParallelProcessInfo.CreateNoWindow = False
                                            currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
                                            currentParallelProcessInfo.UseShellExecute = True
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal Then
                                            OsmbatBatchExport(Tile.TileName)
                                            OsmbatGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        End If

                                        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                            currentParallelProcessInfo.CreateNoWindow = True
                                            currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                            currentParallelProcessInfo.UseShellExecute = False
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer As Int32 = r.Next(1, 10000)

                                        Threading.Thread.Sleep(timer)

                                        While stateQGIS = True
                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While
                                            Tile.Comment = "Waiting for QGIS"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(timer)
                                        End While
                                        stateQGIS = True
                                        QgisRepairBatchExport(Tile.TileName)
                                        QgisRepairGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        stateQGIS = False

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\beach.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\beach.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\broadleaved.dbf") = True And My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\mixedforest.dbf") = True And My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\needleleaved.dbf") = True Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\forest.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\farmland.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\farmland.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\glacier.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\glacier.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\grass.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\grass.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\meadow.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\meadow.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\quarry.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\quarry.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\urban.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\urban.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\water.dbf") Then
                                            Try
                                                File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\water.osm")
                                            Catch ex As Exception
                                                WriteLog(ex.Message)
                                            End Try
                                        End If

                                        tilesReady += 1

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                Else

                                    Tile.GenerationProgress = 100
                                    CalculateDuration()
                                    Tile.Comment = "Skipped"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment

                                End If

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Finished()
                End If
            Next
        End If

    End Sub

    Public Sub ImagesOnly_Generation()

        WriteLog("Starting Images only generation.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        Dim projectName As String = ""
        If ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles.qgz"
        ElseIf Not ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_bathymetry.qgz"
        ElseIf ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_terrain.qgz"
        ElseIf Not ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_terrain_no_bathymetry.qgz"
        Else
            projectName = "MinecraftEarthTiles.qgz"
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Convert pbf" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Dim keepRunningLocal As Boolean = True

                                Dim currentParallelProcess As New Process
                                Dim currentParallelProcessInfo As New ProcessStartInfo

                                If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                    currentParallelProcessInfo.CreateNoWindow = True
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                    currentParallelProcessInfo.UseShellExecute = False
                                End If

                                If Not Tile.TileName.Contains("x") Then

                                    Try

                                        Dim filesList As New List(Of String) From {
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\big_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\border.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\highway.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\middle_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\river.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\small_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\stream.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\vineyard.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\volcano.osm"
                                            }

                                        Dim missingOSM As Integer = 0
                                        For Each myFile In filesList
                                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(myFile) Then
                                                missingOSM += 1
                                            End If
                                        Next
                                        If missingOSM > 1 Then
                                            Tile.Comment = $"Error: {missingOSM} osm files missing"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer As Int32 = r.Next(1, 10000)

                                        Threading.Thread.Sleep(timer)

                                        While stateQGIS = True
                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While
                                            Tile.Comment = "Waiting for QGIS"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(timer)
                                        End While
                                        stateQGIS = True
                                        QgisBatchExport(Tile.TileName)
                                        QgisGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        stateQGIS = False

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & ".png") Then
                                            Tile.Comment = "Error: " & Tile.TileName & ".png not found"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            If keepRunning And keepRunningLocal Then
                                                TartoolBatchExport(Tile.TileName)
                                                TartoolGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            If keepRunning And keepRunningLocal Then
                                                GdalBatchExport(Tile.TileName)
                                                GdalGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If

                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal Then
                                            ImageMagickBatchExport(Tile.TileName)
                                            ImageMagickGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        End If

                                        tilesReady += 1

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                Else

                                    Tile.GenerationProgress = 100
                                    CalculateDuration()
                                    Tile.Comment = "Skipped"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment

                                End If

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Finished()
                End If
            Next
        End If

    End Sub

    Public Sub WorldPainterOnly_Generation()

        WriteLog("Starting WorldPainter only generation.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Convert pbf" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Dim keepRunningLocal As Boolean = True

                                Dim currentParallelProcess As New Process
                                Dim currentParallelProcessInfo As New ProcessStartInfo

                                If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                    currentParallelProcessInfo.CreateNoWindow = True
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                    currentParallelProcessInfo.UseShellExecute = False
                                End If

                                If Tile.TileName.Contains("x") Then

                                    Try

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal Then
                                            VoidScriptBatchExport(Tile.TileName)
                                            VoidScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        End If
                                        Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer As Int32 = r.Next(1, 10000)
                                        Threading.Thread.Sleep(timer)

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                Else

                                    Try

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & "_terrain_reduced_colors.png") Then
                                            Tile.Comment = "Error: " & Tile.TileName & "_terrain_reduced_colors.png not found"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        Dim r2 As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer2 As Int32 = r2.Next(1, 10000)

                                        Threading.Thread.Sleep(timer2)

                                        While stateWorldPainter = True
                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While
                                            Tile.Comment = "Waiting for WorldPainter"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(timer2)
                                        End While

                                        If keepRunning And keepRunningLocal Then

                                            If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                                stateWorldPainter = True
                                            End If
                                            WpScriptBatchExport(Tile.TileName)
                                            WpScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                                stateWorldPainter = False
                                            End If

                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        Dim Foldername As String = ""
                                        If ClassWorker.MySelection.SpawnTile = Tile.TileName Then
                                            Foldername = ClassWorker.MyWorldSettings.WorldName
                                        Else
                                            Foldername = Tile.TileName
                                        End If

                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & Foldername & "\level.dat") Then
                                            Tile.Comment = "Error: " & Tile.TileName & "\level.dat not found"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If Not ClassWorker.MyTilesSettings.PathToMinutor = "" Then
                                            If keepRunning And keepRunningLocal Then
                                                MinutorRenderExort(Tile.TileName)
                                                MinutorGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If
                                        End If

                                        tilesReady += 1

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                End If

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    Tile.GenerationProgress = 100
                    tilesReady += 1
                    CalculateDuration()
                    Tile.Comment = "Skipped"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Finished()
                End If
            Next
        End If

    End Sub

    Public Sub Tile_Generation()

        WriteLog("Starting Complete Generation.")

        keepRunning = True

        Dim currentProcess As New Process
        Dim currentProcessInfo As New ProcessStartInfo

        currentProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
        If ClassWorker.MyTilesSettings.cmdVisibility = False Then
            currentProcessInfo.CreateNoWindow = True
            currentProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            currentProcessInfo.UseShellExecute = False
        End If

        Dim projectName As String = ""
        If ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles.qgz"
        ElseIf Not ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_bathymetry.qgz"
        ElseIf ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_terrain.qgz"
        ElseIf Not ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            projectName = "MinecraftEarthTiles_no_terrain_no_bathymetry.qgz"
        Else
            projectName = "MinecraftEarthTiles.qgz"
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                Try
                    If Tile.TileName = "Convert pbf" Then
                        OsmbatExportPrepare()
                        OsmbatGenerationPrepare(Tile, currentProcess, currentProcessInfo)
                    End If

                Catch ex As Exception
                    Tile.Comment = ex.Message
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End Try
            Next
        End If

        Parallel.ForEach(MyGeneration.AsParallel().AsOrdered(),
                        myOptions,
                        Sub(Tile)
                            If Not Tile.TileName = "Convert pbf" And Not Tile.TileName = "Combining" And Not Tile.TileName = "Cleanup" Then

                                Dim keepRunningLocal As Boolean = True

                                Dim currentParallelProcess As New Process
                                Dim currentParallelProcessInfo As New ProcessStartInfo

                                If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                    currentParallelProcessInfo.CreateNoWindow = True
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                    currentParallelProcessInfo.UseShellExecute = False
                                End If

                                If Tile.TileName.Contains("x") Then

                                    Try

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal Then
                                            VoidScriptBatchExport(Tile.TileName)
                                            VoidScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        End If
                                        Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer As Int32 = r.Next(1, 10000)
                                        Threading.Thread.Sleep(timer)

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                Else

                                    Try

                                        Dim reUseFilesList As New List(Of String) From {
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\big_road.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\border.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\highway.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\middle_road.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\river.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\small_road.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\stream.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\vineyard.osm",
                                            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\volcano.osm"
                                        }

                                        For Each myFile In reUseFilesList
                                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(myFile) And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                                ClassWorker.MyTilesSettings.reUseOsmFiles = False
                                            End If
                                        Next

                                        If ClassWorker.MyWorldSettings.geofabrik = True And ClassWorker.MyTilesSettings.reUseOsmFiles = False And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                            If Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m") Then
                                                Tile.Comment = "Error: Could not find output.o5m file"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                keepRunning = False
                                            End If
                                        End If

                                        If ClassWorker.MyTilesSettings.reUseOsmFiles = False And ClassWorker.MyTilesSettings.reUseImageFiles = False Then

                                            If ClassWorker.MyWorldSettings.geofabrik = False Then
                                                currentParallelProcessInfo.CreateNoWindow = False
                                                currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
                                                currentParallelProcessInfo.UseShellExecute = True
                                            End If

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            If keepRunning And keepRunningLocal Then
                                                OsmbatBatchExport(Tile.TileName)
                                                OsmbatGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If

                                            If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                                currentParallelProcessInfo.CreateNoWindow = True
                                                currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                                currentParallelProcessInfo.UseShellExecute = False
                                            End If

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                            Dim timer As Int32 = r.Next(1, 10000)

                                            Threading.Thread.Sleep(timer)

                                            While stateQGIS = True
                                                While pause = True
                                                    Tile.Comment = "Pause"
                                                    WriteLog(Tile.Comment, Tile.TileName)
                                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                    Threading.Thread.Sleep(10000)
                                                End While
                                                Tile.Comment = "Waiting for QGIS"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(timer)
                                            End While
                                            stateQGIS = True
                                            QgisRepairBatchExport(Tile.TileName)
                                            QgisRepairGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            stateQGIS = False

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\beach.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\beach.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\broadleaved.dbf") = True And My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\mixedforest.dbf") = True And My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\needleleaved.dbf") = True Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\forest.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\farmland.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\farmland.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\glacier.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\glacier.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\grass.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\grass.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\meadow.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\meadow.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\quarry.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\quarry.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\urban.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\urban.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\water.dbf") Then
                                                Try
                                                    File.Delete(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\water.osm")
                                                Catch ex As Exception
                                                    WriteLog(ex.Message)
                                                End Try
                                            End If

                                        End If

                                        If (ClassWorker.MyTilesSettings.reUseImageFiles = False) Then

                                            Dim filesList As New List(Of String) From {
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\big_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\border.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\highway.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\middle_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\river.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\small_road.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\stream.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\vineyard.osm",
                                                ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile.TileName & "\volcano.osm"
                                            }

                                            Dim missingOSM As Integer = 0
                                            For Each myFile In filesList
                                                If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(myFile) Then
                                                    missingOSM += 1
                                                End If
                                            Next
                                            If missingOSM > 1 Then
                                                Tile.Comment = $"Error: {missingOSM} osm files missing"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                    keepRunning = False
                                                End If
                                                keepRunningLocal = False
                                            End If

                                            Dim r As Random = New Random(Tile.TileName.GetHashCode())
                                            Dim timer As Int32 = r.Next(1, 10000)

                                            Threading.Thread.Sleep(timer)

                                            While stateQGIS = True
                                                While pause = True
                                                    Tile.Comment = "Pause"
                                                    WriteLog(Tile.Comment, Tile.TileName)
                                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                    Threading.Thread.Sleep(10000)
                                                End While
                                                Tile.Comment = "Waiting for QGIS"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(timer)
                                            End While
                                            stateQGIS = True
                                            QgisBatchExport(Tile.TileName)
                                            QgisGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            stateQGIS = False

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & ".png") Then
                                                Tile.Comment = "Error: " & Tile.TileName & ".png not found"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                    keepRunning = False
                                                End If
                                                keepRunningLocal = False
                                            End If

                                            If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then

                                                While pause = True
                                                    Tile.Comment = "Pause"
                                                    WriteLog(Tile.Comment, Tile.TileName)
                                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                    Threading.Thread.Sleep(10000)
                                                End While

                                                If keepRunning And keepRunningLocal Then
                                                    TartoolBatchExport(Tile.TileName)
                                                    TartoolGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                                End If

                                                While pause = True
                                                    Tile.Comment = "Pause"
                                                    WriteLog(Tile.Comment, Tile.TileName)
                                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                    Threading.Thread.Sleep(10000)
                                                End While

                                                If keepRunning And keepRunningLocal Then
                                                    GdalBatchExport(Tile.TileName)
                                                    GdalGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                                End If

                                            End If

                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While

                                            If keepRunning And keepRunningLocal Then
                                                ImageMagickBatchExport(Tile.TileName)
                                                ImageMagickGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If

                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile.TileName & "\" & Tile.TileName & "_terrain_reduced_colors.png") Then
                                            Tile.Comment = "Error: " & Tile.TileName & "_terrain_reduced_colors.png not found"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        Dim r2 As Random = New Random(Tile.TileName.GetHashCode())
                                        Dim timer2 As Int32 = r2.Next(1, 10000)

                                        Threading.Thread.Sleep(timer2)

                                        While stateWorldPainter = True
                                            While pause = True
                                                Tile.Comment = "Pause"
                                                WriteLog(Tile.Comment, Tile.TileName)
                                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                                Threading.Thread.Sleep(10000)
                                            End While
                                            Tile.Comment = "Waiting for WorldPainter"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(timer2)
                                        End While

                                        If keepRunning And keepRunningLocal Then

                                            If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                                stateWorldPainter = True
                                            End If
                                            WpScriptBatchExport(Tile.TileName)
                                            WpScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                                stateWorldPainter = False
                                            End If

                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        Dim Foldername As String = ""
                                        If ClassWorker.MySelection.SpawnTile = Tile.TileName Then
                                            Foldername = ClassWorker.MyWorldSettings.WorldName
                                        Else
                                            Foldername = Tile.TileName
                                        End If

                                        If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & Foldername & "\level.dat") Then
                                            Tile.Comment = "Error: " & Tile.TileName & "\level.dat not found"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                                keepRunning = False
                                            End If
                                            keepRunningLocal = False
                                        End If

                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While

                                        If Not ClassWorker.MyTilesSettings.PathToMinutor = "" And ClassWorker.MyTilesSettings.minutor = True Then
                                            If keepRunning And keepRunningLocal Then
                                                MinutorRenderExort(Tile.TileName)
                                                MinutorGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                            End If
                                        End If

                                        If keepRunning And keepRunningLocal Then
                                            CleanupBatchExport(Tile.TileName)
                                            CleanupGeneration(Tile, currentParallelProcess, currentParallelProcessInfo)
                                            RefreshPreview = Tile.TileName
                                            ClassWorker.MySelection.TilesList.Remove(Tile.TileName)
                                        End If

                                        'only on real Tiles, not void Tiles
                                        tilesReady += 1

                                        SaveCurrentState()

                                        Tile.GenerationProgress = 100
                                        CalculateDuration()
                                        Tile.Comment = "Finished"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment

                                    Catch ex As Exception
                                        Tile.Comment = ex.Message
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    End Try

                                End If

                            End If

                        End Sub)

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Combining" Then
                    CombineBatchExport()
                    CombineGeneration(Tile, currentProcess, currentProcessInfo)
                    If Not ClassWorker.MyTilesSettings.PathToMinutor = "" And ClassWorker.MyTilesSettings.minutor = True Then
                        MinutorFinalRenderExort(Tile.TileName)
                        MinutorFinalGeneration(Tile, currentProcess, currentProcessInfo)
                    End If
                    Tile.GenerationProgress = 100
                    Tile.Comment = "Finished"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                End If
            Next
        End If

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Cleanup" Then
                    CleanupFinalBatchExport()
                    CleanUpFinalGeneration(Tile, currentProcess, currentProcessInfo)
                    Tile.GenerationProgress = 100
                    Tile.Comment = "Finished"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Finished()
                End If
            Next
        End If

    End Sub

    Private Sub SaveCurrentState()
        CustomXmlSerialiser.SaveXML(ClassWorker.MyTilesSettings.PathToScriptsFolder & "/selection-left.xml", ClassWorker.MySelection)
    End Sub


#Region "Generate World"

    Public Sub OsmbatGenerationPrepare(ByRef Tile As Generation, ByRef currentProcess As Process, ByRef currentProcessInfo As ProcessStartInfo)

        CalculateDuration()

        Dim filesList As New List(Of String) From {
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\MinecraftEarthTiles.qgz",
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmconvert.exe",
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe",
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"
        }
        If ClassWorker.MyWorldSettings.bathymetry Then
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\bathymetry.tif")
        End If

        If ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.A1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.A2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.A3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.A4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.B1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.B2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.B3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.B4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.C1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.C2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.C3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.C4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.D1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.D2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.D3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.D4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.E1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.E2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.E3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.E4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.F1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.F2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.F3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.F4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.G1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.G2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.G3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.G4.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.H1.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.H2.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.H3.tif")
            filesList.Add(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\TifFiles\terrain\TrueMarble.250m.21600x21600.H4.tif")
        End If

        For Each myFile In filesList
            If keepRunning And Not My.Computer.FileSystem.FileExists(myFile) Then
                Tile.Comment = "Error: '" & myFile & "' not found"
                WriteLog(Tile.Comment)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                keepRunning = False
            End If
        Next

        If keepRunning Then

            If ClassWorker.MyTilesSettings.reUseOsmFiles = True Or ClassWorker.MyTilesSettings.reUseImageFiles = True Then

                Tile.GenerationProgress = 100
                tilesReady += 1
                CalculateDuration()
                Tile.Comment = "Skipped"
                WriteLog(Tile.Comment)
                LatestMessage = Tile.TileName & ": " & Tile.Comment

            ElseIf (ClassWorker.MyTilesSettings.reUsePbfFile = False And ClassWorker.MyWorldSettings.geofabrik = True) Or (ClassWorker.MyTilesSettings.reUsePbfFile = True And ClassWorker.MyWorldSettings.geofabrik = True And Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m")) Then

                If Not My.Computer.FileSystem.FileExists(ClassWorker.MyWorldSettings.PathToPBF) Then
                    Tile.Comment = "Error: *.pbf file not found"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    keepRunning = False
                End If

                While pause = True
                    Tile.Comment = "Pause"
                    WriteLog(Tile.Comment)
                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Threading.Thread.Sleep(10000)
                End While

                If keepRunning Then

                    OsmbatExportPrepare()
                    If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat") Then
                        currentProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat"
                        currentProcess = Process.Start(currentProcessInfo)
                        Tile.Comment = "Converting OSM data"
                        WriteLog(Tile.Comment)
                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                        processList.Add(currentProcess)
                        currentProcess.WaitForExit()
                        processList.Remove(currentProcess)
                        currentProcess.Close()
                        Tile.GenerationProgress = 100
                        tilesReady += 1
                        CalculateDuration()
                        Tile.Comment = "Finished"
                        WriteLog(Tile.Comment)
                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                    Else
                        Tile.GenerationProgress = 100
                        Tile.Comment = "1-osmconvert.bat not found"
                        WriteLog(Tile.Comment)
                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                    End If

                    If Not My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m") Then
                        Tile.Comment = "Error: Could not create output.o5m file"
                        WriteLog(Tile.Comment)
                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                        keepRunning = False
                    End If

                End If

            Else

                Tile.GenerationProgress = 100
                tilesReady += 1
                CalculateDuration()
                Tile.Comment = "Skipped"
                WriteLog(Tile.Comment)
                LatestMessage = Tile.TileName & ": " & Tile.Comment

            End If

        End If
    End Sub

    Public Sub OsmbatGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If ClassWorker.MyWorldSettings.geofabrik = True Then
            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat") Then
                Tile.Comment = "Converting OSM data"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-log-osmconvert.bat"
                currentParallelProcess = Process.Start(currentParallelProcessInfo)
                processList.Add(currentParallelProcess)
                currentParallelProcess.WaitForExit()
                processList.Remove(currentParallelProcess)
                currentParallelProcess.Close()
                Tile.GenerationProgress = 15
            Else
                Tile.GenerationProgress = 15
                Tile.Comment = Tile.TileName & "\1-osmconvert.bat not found"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
            End If
        Else
            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-osmconvert.bat") Then
                Tile.Comment = "Converting OSM data"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\1-osmconvert.bat"
                currentParallelProcess = Process.Start(currentParallelProcessInfo)
                processList.Add(currentParallelProcess)
                currentParallelProcess.WaitForExit()
                processList.Remove(currentParallelProcess)
                currentParallelProcess.Close()
                Tile.GenerationProgress = 15
            Else
                Tile.GenerationProgress = 15
                Tile.Comment = Tile.TileName & "\1-osmconvert.bat not found"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
            End If
        End If
    End Sub

    Public Sub QgisRepairGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-1-log-qgis.bat") Then

            If File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db") Then
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db")
            End If

            Tile.Comment = "Repairing OSM with QGIS"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-1-log-qgis.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            If My.Computer.FileSystem.FileExists(Environment.SpecialFolder.ApplicationData & "\EarthTilesPythonError") Then
                Tile.GenerationProgress = 30
                Tile.Comment = "Error in the python environment"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
                File.Delete(Environment.SpecialFolder.ApplicationData & "\EarthTilesPythonError")
            End If
            Tile.GenerationProgress = 33
        Else
            Tile.GenerationProgress = 33
            Tile.Comment = Tile.TileName & "\2-1-qgis.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub QgisGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db") Then
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db")
        End If

        'Try
        'My.Computer.FileSystem.WriteAllText(ClassWorker.MyTilesSettings.PathToQGIS & "\apps\qgis\python\qgis\utils.py", My.Resources.utils, False)
        'Catch ex As Exception 
        'WriteLog(ex.Message)
        '
        'End Try

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-2-log-qgis.bat") Then

            If File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db") Then
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\QGIS\QGIS3\profiles\default\qgis.db")
            End If

            Tile.Comment = "Creating images with QGIS"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-2-log-qgis.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            If My.Computer.FileSystem.FileExists(Environment.SpecialFolder.ApplicationData & "\EarthTilesPythonError") Then
                Tile.GenerationProgress = 30
                Tile.Comment = "Error in the python environment"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
                File.Delete(Environment.SpecialFolder.ApplicationData & "\EarthTilesPythonError")
            End If
            Tile.GenerationProgress = 30
        Else
            Tile.GenerationProgress = 30
            Tile.Comment = Tile.TileName & "\2-qgis.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If

        If Directory.Exists(Path.GetTempPath) Then
            For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*QGIS*")
                Try
                    File.Delete(_file)
                Catch ex As Exception
                    WriteLog(ex.Message)
                End Try
            Next
        End If

        If Directory.Exists(Path.GetTempPath) Then
            For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*osm*")
                Try
                    File.Delete(_file)
                Catch ex As Exception
                    WriteLog(ex.Message)
                End Try
            Next
        End If

        'Try
        'My.Computer.FileSystem.WriteAllText(ClassWorker.MyTilesSettings.PathToQGIS & "\apps\qgis\python\qgis\utils.py", My.Resources.utils_orig, False)
        'Catch ex As Exception 
        'WriteLog(ex.Message)
        '
        'End Try
    End Sub

    Public Sub TartoolGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat") Then
            Tile.Comment = "Downloading heightmap"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\3-log-tartool.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 45
        Else
            Tile.GenerationProgress = 45
            Tile.Comment = Tile.TileName & "\3-tartool.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub GdalGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat") Then
            Tile.Comment = "Processing heightmap"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\4-log-gdal.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 60
        Else
            Tile.GenerationProgress = 60
            Tile.Comment = Tile.TileName & "\4-gdal.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub ImageMagickGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat") Then
            Tile.Comment = "Processing images"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\5-log-magick.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 75
        Else
            Tile.GenerationProgress = 75
            Tile.Comment = Tile.TileName & "\5-magick.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub WpScriptGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat") Then
            Tile.Comment = "Creating world with WorldPainter"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-wpscript.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 90
        Else
            Tile.GenerationProgress = 90
            Tile.Comment = Tile.TileName & "\6-wpscript.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub VoidScriptGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-voidscript.bat") Then
            Tile.Comment = "Creating world with WorldPainter"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-log-voidscript.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 100
            Tile.Comment = "Finished"
        Else
            Tile.GenerationProgress = 100
            Tile.Comment = Tile.TileName & "\6-voidscript.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub MinutorGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-1-log-minutors.bat") Then
            Tile.Comment = "Creating Preview Image"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-1-log-minutors.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 95
        Else
            Tile.GenerationProgress = 95
            Tile.Comment = Tile.TileName & "\6-1-minutors.bat not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub CleanupGeneration(ByRef Tile As Generation, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat") Then
            Tile.Comment = "Cleaning up my mess"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\8-log-cleanup.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
        End If
    End Sub

    Public Sub CombineGeneration(ByRef Tile As Generation, ByRef currentProcess As Process, ByRef currentProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat") Then
            Tile.Comment = "Combining world"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat"
            currentProcess = Process.Start(currentProcessInfo)
            processList.Add(currentProcess)
            currentProcess.WaitForExit()
            processList.Remove(currentProcess)
            currentProcess.Close()
            Tile.GenerationProgress = 50
            tilesReady += 1
            CalculateDuration()
        Else
            Tile.GenerationProgress = 90
            Tile.Comment = "7-combine.bat not found"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            keepRunning = False
        End If
    End Sub

    Public Sub MinutorFinalGeneration(ByRef Tile As Generation, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-1-log-minutors.bat") Then
            Tile.Comment = "Creating Preview Image"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-1-log-minutors.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 100
        Else
            Tile.GenerationProgress = 90
            Tile.Comment = Tile.TileName & "\7-1-minutors.bat not found"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            keepRunning = False
        End If
    End Sub

    Public Sub CleanUpFinalGeneration(ByRef Tile As Generation, ByRef currentProcess As Process, ByRef currentProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\8-log-cleanup.bat") Then
            Tile.Comment = "Cleaning up my mess"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\8-log-cleanup.bat"
            currentProcess = Process.Start(currentProcessInfo)
            processList.Add(currentProcess)
            currentProcess.WaitForExit()
            processList.Remove(currentProcess)
            currentProcess.Close()
            Tile.GenerationProgress = 100
            tilesReady += 1
            CalculateDuration()
        Else
            Tile.GenerationProgress = 100
            Tile.Comment = "8-cleanup.bat not found"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            keepRunning = False
        End If
    End Sub

    Public Sub Finished()
        If ClassWorker.MyTilesSettings.alertAfterFinish Then
            Try
                Dim localMail As New MailAddress(ClassWorker.MyTilesSettings.alertMail)
                Dim Smtp_Server As New SmtpClient
                Dim e_mail As New MailMessage()
                Smtp_Server.UseDefaultCredentials = False
                Smtp_Server.Credentials = New Net.NetworkCredential("web132034p2", "M1n3Cr4ftT!l3s")
                Smtp_Server.Port = 25
                Smtp_Server.EnableSsl = True
                Smtp_Server.Host = "rex16.flatbooster.com"
                Smtp_Server.Timeout = 60000

                e_mail = New MailMessage()
                e_mail.From = New MailAddress("alert@earth.motfe.net", "Earth Tiles")
                e_mail.To.Add(localMail.Address)
                e_mail.Subject = $"Generation of world ""{ClassWorker.MyWorldSettings.WorldName}"" complete"
                e_mail.IsBodyHtml = False

                Dim minutesDoneDiff As Double = DateDiff(DateInterval.Minute, DateTime.Now, startTime)
                Dim hoursDone As Int32 = CType(Fix(Math.Abs(minutesDoneDiff) / 60), Int32)
                Dim minutesDone As Int32 = CType(Math.Abs(minutesDoneDiff) Mod 60, Int32)

                e_mail.Body = $"The generation took ""{hoursDone} h and {minutesDone} min"" in total.
                                                    {vbNewLine}The message was sent from ""{Environment.MachineName}"" using the windows account ""{UserPrincipal.Current.Name} ({UserPrincipal.Current.EmailAddress})"".
                                                    {vbNewLine}This message was generated automatically using the Earth Tiles Software.
                                                    {vbNewLine}Please don't reply to this address."

                Smtp_Server.Send(e_mail)
            Catch ex As Exception
                WriteLog(ex.Message)
                MsgBox(ex.Message)
            End Try
            GenerationComplete = True
        End If
    End Sub

#End Region

#Region "Create Batch Files"

    Public Sub OsmbatExportPrepare()
        Try
            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            If ClassWorker.MyWorldSettings.geofabrik = True Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-general.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm""")
                ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports""")

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmconvert.exe"" """ & ClassWorker.MyWorldSettings.PathToPBF & """ -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\unfiltered.o5m""")

                Dim filter As String = "water=lake OR water=reservoir OR natural=water OR landuse=reservoir OR natural=glacier OR natural=volcano OR natural=beach OR landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub OR landuse=forest OR landuse=bare_rock OR natural=scree OR natural=shingle"

                If ClassWorker.MyWorldSettings.highways Then
                    filter &= " OR highway=motorway OR highway=trunk"
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    filter &= " OR highway=primary OR highway=secondary OR highway=tertiary"
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    filter &= " OR highway=residential"
                End If

                If ClassWorker.MyWorldSettings.riversBoolean Then
                    filter &= " OR waterway=river OR waterway=canal OR water=river OR waterway=riverbank"
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    filter &= " OR waterway=river OR water=river OR waterway=stream"
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    filter &= " OR landuse=farmland OR landuse=vineyard"
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    filter &= " OR landuse=meadow"
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    filter &= " OR landuse=quarry"
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        filter &= " OR aeroway=aerodrome AND iata="
                    Else
                        filter &= " OR aeroway=launchpad"
                    End If
                End If

                If ClassWorker.MyWorldSettings.buildings Then
                    filter &= " OR landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    filter &= " OR boundary=administrative AND admin_level=2"
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\unfiltered.o5m"" --verbose --keep=""" & filter & """ -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m""")
                ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\unfiltered.o5m""")
                ScriptBatchFile.Close()

            Else

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat"" > """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine("PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            End If

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '1-osmconvert.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub OsmbatBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            If ClassWorker.MyWorldSettings.geofabrik = True Then


                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" > """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

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
                    borderS = LatiNumber - CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 1 - 0.01
                    borderN = LatiNumber + 1.01
                Else
                    borderS = (-1 * LatiNumber) - CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 1 - 0.01
                    borderN = (-1 * LatiNumber) + 1.01
                End If
                If LongDir = "E" Then
                    borderW = LongNumber - 0.01
                    borderE = LongNumber + CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 0.01
                Else
                    borderW = (-1 * LongNumber) - 0.01
                    borderE = (-1 * LongNumber) + CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 0.01
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

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmconvert.exe""  """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m"" -b=" & borderW.ToString("##0.##", New CultureInfo("en-US")) & "," & borderS.ToString("#0.##", New CultureInfo("en-US")) & "," & (borderE).ToString("##0.##", New CultureInfo("en-US")) & "," & (borderN).ToString("#0.##", New CultureInfo("en-US")) & " -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --complete-ways --complete-multipolygons --complete-boundaries --drop-version --verbose")

                If ClassWorker.MyWorldSettings.highways Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe""  """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe""  """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe""  """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                If ClassWorker.MyWorldSettings.riversBoolean Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=riverbank OR waterway=canal"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=stream"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")

                If ClassWorker.MyWorldSettings.forests Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm*""")
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm*""")
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    Else
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")
                If ClassWorker.MyWorldSettings.buildings Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                End If

                ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine("PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            Else

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\1-osmconvert.bat", False, System.Text.Encoding.ASCII)

                ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & """ mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))

                If Not ClassWorker.MyTilesSettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine("set http_proxy=" & ClassWorker.MyTilesSettings.Proxy)
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

                Dim overpassURL As String = "http://overpass-api.de"
                If Not ClassWorker.MyWorldSettings.OverpassURL = "" Then
                    overpassURL = ClassWorker.MyWorldSettings.OverpassURL
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wget.exe"" -O """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" """ & overpassURL & "/api/interpreter?data=(node(" & borderS.ToString & "," & borderW.ToString & "," & borderN.ToString & "," & borderE.ToString & ");<;>;);out;""")
                ScriptBatchFile.WriteLine("@echo off")
                ScriptBatchFile.WriteLine("FOR /F  "" usebackq "" %%A IN ('" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm') DO set size=%%~zA")
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

                If ClassWorker.MyWorldSettings.highways Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\highway.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=tertiary"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\big_road.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\middle_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""highway=residential"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\small_road.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\water.osm""")

                If ClassWorker.MyWorldSettings.riversBoolean Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=riverbank OR waterway=canalg"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\river.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=stream"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=glacier"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.osm""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\volcano.osm""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""natural=beach"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.osm""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")

                If ClassWorker.MyWorldSettings.forests Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=forest"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\forest.osm*""")
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=farmland"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\vineyard.osm*""")
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=meadow"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.osm*""")
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.osm*""")
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=quarry"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.osm*""")
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")

                    Else
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\bare_rock.osm""")

                If ClassWorker.MyWorldSettings.buildings Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.osm*""")
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm""")
                Else
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\empty.osm"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\border.osm*""")
                End If

                ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine("PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()


            End If
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '1-osmconvert.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub QgisRepairBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim pythonFile As StreamWriter
            pythonFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\" & Tile & "_repair.py", False, System.Text.Encoding.ASCII)
            Dim PathReversedSlash = Replace(ClassWorker.MyTilesSettings.PathToScriptsFolder, "\", "/")
            pythonFile.WriteLine("try:" & Environment.NewLine &
                                vbTab & "import os" & Environment.NewLine &
                                vbTab & "from PyQt5.QtCore import *" & Environment.NewLine &
                                vbTab & "import processing" & Environment.NewLine)
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/urban.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/urban.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""broadleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/broadleaved.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""needleleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/needleleaved.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/forest.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/mixedforest.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/beach.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/beach.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/grass.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/grass.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/farmland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/farmland.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/meadow.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/meadow.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/quarry.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/quarry.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/water.osm|layername=multipolygons|subset=\""natural\"" = \'water\'','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/water.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/glacier.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/glacier.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/wetland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/wetland.shp'})")
            pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/" & Tile & "/swamp.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/" & Tile & "/swamp.shp'})")
            pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
            pythonFile.WriteLine("except:" & Environment.NewLine &
                                vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
                                vbTab & vbTab & "pass" & Environment.NewLine &
                                vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
            pythonFile.Close()


            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As StreamWriter

            Dim qgisExecutableName As String = ""
            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToQGIS & "\bin\qgis-bin.exe") Then
                qgisExecutableName = "qgis-bin.exe"
            ElseIf My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToQGIS & "\bin\qgis-ltr-bin.exe") Then
                qgisExecutableName = "qgis-ltr-bin.exe"
            Else
                qgisExecutableName = "qgis-bin.exe"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-1-log-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-1-qgis.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-1-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\" & qgisExecutableName & """ --noversioncheck --nologo --noplugins --code """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\" & Tile & "_repair.py""")

            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\urban.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\broadleaved.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\needleleaved.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\mixedforest.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\beach.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\grass.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\farmland.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\meadow.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\quarry.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\water.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\glacier.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\wetland.dbf""")
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\swamp.dbf""")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '2-1-qgis.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub QgisBatchExport(Tile As String)
        Try

            Dim projectName As String = ""
            If ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
                projectName = "MinecraftEarthTiles.qgz"
            ElseIf Not ClassWorker.MyWorldSettings.bathymetry And ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
                projectName = "MinecraftEarthTiles_no_bathymetry.qgz"
            ElseIf ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
                projectName = "MinecraftEarthTiles_no_terrain.qgz"
            ElseIf Not ClassWorker.MyWorldSettings.bathymetry And Not ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
                projectName = "MinecraftEarthTiles_no_terrain_no_bathymetry.qgz"
            Else
                projectName = "MinecraftEarthTiles.qgz"
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim citySize As String = "villages_5-000-000"

            If CalcScale() > 1000 Then
                '1:1500 - 1:6000
                citySize = "villages_5-000-000"
            ElseIf CalcScale() > 750 Then
                '1:1000
                citySize = "villages_2-500-000"
            ElseIf CalcScale() > 500 Then
                '1:750
                citySize = "villages_1-500-000"
            Else
                '1:500 - maybe at some time
                citySize = "villages_500-000"
            End If

            Dim terrainSource As String = "backupterrain"

            If ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (high res)" Then
                terrainSource = "TrueMarble"
            ElseIf ClassWorker.MyWorldSettings.TerrainSource = "Offline Terrain (low res)" Then
                terrainSource = "backupterrain"
            ElseIf ClassWorker.MyWorldSettings.TerrainSource = "Arcgis" Then
                terrainSource = "terrain_arcgis"
            ElseIf ClassWorker.MyWorldSettings.TerrainSource = "Google" Then
                terrainSource = "terrain_google"
            ElseIf ClassWorker.MyWorldSettings.TerrainSource = "Bing" Then
                terrainSource = "terrain_bing"
            Else
                terrainSource = "backupterrain"
            End If

            If CalcScale() > 6000 Then
                terrainSource = "backupterrain"
            End If

            Dim rivers As String = "majorFew"
            Select Case ClassWorker.MyWorldSettings.rivers
                Case "All (small)"
                    rivers = "small"
                Case "All (medium)"
                    rivers = "medium"
                Case "All (large)"
                    rivers = "large"
                Case "Major"
                    rivers = "majorFew"
                Case "Major + Minor"
                    rivers = "majorMany"
            End Select
            Dim customLayerString As String = "["
            ClassWorker.MyWorldSettings.custom_layers = Trim(ClassWorker.MyWorldSettings.custom_layers)
            Dim customLayers As List(Of String) = ClassWorker.MyWorldSettings.custom_layers.Split(New Char() {" "c}).ToList
            For Each customLayer In customLayers
                If Not customLayer = "" Then
                    If customLayer = customLayers.Last Then
                        customLayerString &= "'" & Trim(customLayer) & "'"
                    Else
                        customLayerString &= "'" & Trim(customLayer) & "', "
                    End If
                End If
            Next
            customLayerString &= "]"
            Dim pythonFile As StreamWriter
            pythonFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\" & Tile & ".py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine("try:" & Environment.NewLine &
                                  vbTab & "import os" & Environment.NewLine &
                                  vbTab & "from PyQt5.QtCore import *" & Environment.NewLine &
                                  vbTab & Environment.NewLine &
                                  vbTab & "path = '" & ClassWorker.MyTilesSettings.PathToScriptsFolder.Replace("\", "/") & "/'" & Environment.NewLine &
                                  vbTab & "tile = '" & Tile & "'" & Environment.NewLine &
                                  vbTab & "spawntile = '" & ClassWorker.MySelection.SpawnTile & "'" & Environment.NewLine &
                                  vbTab & "scale = " & ClassWorker.MyWorldSettings.BlocksPerTile & Environment.NewLine &
                                  vbTab & "verticleScale = " & ClassWorker.MyWorldSettings.VerticalScale & Environment.NewLine &
                                  vbTab & "TilesPerMap = " & ClassWorker.MyWorldSettings.TilesPerMap & Environment.NewLine &
                                  vbTab & "rivers = '" & rivers & "'" & Environment.NewLine &
                                  vbTab & "TerrainSource = '" & terrainSource & "'" & Environment.NewLine &
                                  vbTab & "citySize = '" & citySize & "'" & Environment.NewLine &
                                  vbTab & "borders = '" & ClassWorker.MyWorldSettings.bordersBoolean & "'" & Environment.NewLine &
                                  vbTab & "borderyear = '" & ClassWorker.MyWorldSettings.borders & "'" & Environment.NewLine &
                                  vbTab & "vanillaGeneration = '" & ClassWorker.MyWorldSettings.vanillaPopulation & "'" & Environment.NewLine &
                                  vbTab & "customLayers = " & customLayerString)
            pythonFile.WriteLine(My.Resources.ResourceManager.GetString("basescript"))
            pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
            pythonFile.WriteLine("except:" & Environment.NewLine &
                                vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
                                vbTab & vbTab & "pass" & Environment.NewLine &
                                vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
            pythonFile.Close()

            If (Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")) Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            Dim ScriptBatchFile As StreamWriter

            Dim qgisExecutableName As String = ""
            If My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToQGIS & "\bin\qgis-bin.exe") Then
                qgisExecutableName = "qgis-bin.exe"
            ElseIf My.Computer.FileSystem.FileExists(ClassWorker.MyTilesSettings.PathToQGIS & "\bin\qgis-ltr-bin.exe") Then
                qgisExecutableName = "qgis-ltr-bin.exe"
            Else
                qgisExecutableName = "qgis-bin.exe"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-2-log-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-2-qgis.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\2-2-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("if Not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\""")
            ScriptBatchFile.WriteLine("if Not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & """ mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & """")
            ScriptBatchFile.WriteLine("if Not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")

            ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & """ """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\OsmData"" /Y")
            'open project and run script

            ScriptBatchFile.WriteLine("timeout /t 3")
            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\" & qgisExecutableName & """ --noversioncheck --nologo --noplugins --project """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\" & projectName & """ --code """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python\" & Tile & ".py""")
            ScriptBatchFile.WriteLine("timeout /t 3")
            ScriptBatchFile.WriteLine("START taskkill /f /im python.exe")
            ScriptBatchFile.WriteLine("timeout /t 3")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '2-2-qgis.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub TartoolBatchExport(Tile As String)
        Try
            If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
                End If

                Dim ScriptBatchFile As StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-log-tartool.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\3-tartool.bat", False, System.Text.Encoding.ASCII)
                If Not ClassWorker.MyTilesSettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine("set ftp_proxy=" & ClassWorker.MyTilesSettings.Proxy)
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
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wget.exe"" -O """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" ""ftp://ftp.eorc.jaxa.jp/pub/ALOS/ext1/AW3D30/release_v1903/" & TilesRounded & "/" & TilesOneMoreDigit & ".tar.gz""")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\TarTool.exe"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".tar.gz"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine("PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            End If
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '3-tartool.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub GdalBatchExport(Tile As String)
        If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then
            Try

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
                End If

                If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                    Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
                End If

                Dim ScriptBatchFile As StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-log-gdal.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\4-gdal.bat", False, System.Text.Encoding.ASCII)

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

                If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) > 1 Then
                    Dim CombinedString As String = ""
                    CombinedString &= "@python3 """ & ClassWorker.MyTilesSettings.PathToQGIS & "/apps/Python37/Scripts/gdal_merge.py"" -o """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"""
                    For indexLati As Int32 = 0 To CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) - 1
                        For indexLongi As Int32 = 0 To CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) - 1
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

                            CombinedString &= " """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigitTemp & "\" & TilesOneMoreDigitTemp & "_AVE_DSM.tif"""

                        Next
                    Next
                    ScriptBatchFile.WriteLine(CombinedString)
                End If
                Select Case ClassWorker.MyWorldSettings.VerticalScale
                    Case "200"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 50 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "100"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 100 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "75"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 133 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "50"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 200 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "35"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 285 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "25"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 400 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "10"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 1000 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                    Case "5"
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale -1152 8848 0 2000 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png""")
                End Select
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdaldem.exe"" slope """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" -s 111120 -compute_edges")
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToQGIS & "\bin\gdal_translate.exe"" -a_nodata none -outsize " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " -Of PNG -ot UInt16 -scale 0 90 0 65535 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & TilesOneMoreDigit & "\" & TilesOneMoreDigit & "_AVE_DSM_slope.tif"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_slope.png""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine("PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()
            Catch ex As Exception
                WriteLog(ex.Message)
                Throw New Exception("File '4-gdal.bat' could not be saved. " & ex.Message)
            End Try
        End If
    End Sub

    Public Sub ImageMagickBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-log-magick.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\5-magick.bat", False, System.Text.Encoding.ASCII)

            Dim NumberOfResize = "( +clone -resize 50%% )"
            Dim NumberOfResizeWater = "( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
            Dim TilesSize As Double = CType(ClassWorker.MyWorldSettings.BlocksPerTile, Double)
            While TilesSize >= 2
                NumberOfResize &= " ( +clone -resize 50%% )"
                NumberOfResizeWater &= " ( +clone -filter Gaussian -resize 50%% -morphology Dilate Gaussian )"
                TilesSize /= 2
            End While

            Dim NewTile As String = ""

            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            ScriptBatchFile.WriteLine("timeout /t 7")

            If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then
                If ClassWorker.MyWorldSettings.Heightmap_Error_Correction = True Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png"" -transparent black -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_removed_invalid.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_removed_invalid.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_edges.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_edges.png"" " & NumberOfResize & " -layers RemoveDups -filter Gaussian -resize " & ClassWorker.MyWorldSettings.BlocksPerTile & "x" & ClassWorker.MyWorldSettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_invalid_filled.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_invalid_filled.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_removed_invalid.png"" -compose over -composite -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_unsmoothed.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert -negate """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water.png"" -threshold 20%% -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_mask.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert -negate """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river.png"" -threshold 20%% -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river_mask.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ composite -gravity center """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_mask.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river_mask.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_transparent.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_unsmoothed.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_transparent.png"" -compose over -composite -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_blacked.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_blacked.png"" -transparent black -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_removed.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_removed.png"" -channel A -morphology EdgeIn Diamond -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_edges.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_edges.png"" -morphology Dilate Gaussian " & NumberOfResizeWater & " -layers RemoveDups -filter Gaussian -resize " & ClassWorker.MyWorldSettings.BlocksPerTile & "x" & ClassWorker.MyWorldSettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_filled.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png"" -level 0.002%%,100.002%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_filled.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_water_removed.png"" -compose over -composite -depth 16 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & ".png""")
                Else
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & "_exported.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & ".png""")
                End If
            Else
                If ClassWorker.MyWorldSettings.Heightmap_Error_Correction = True Then
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_unsmoothed.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert -negate """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water.png"" -threshold 20%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_mask.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert -negate """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river.png"" -threshold 20%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river_mask.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ composite -gravity center """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_mask.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_river_mask.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_transparent.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_unsmoothed.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_transparent.png"" -compose over -composite """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_blacked.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_blacked.png"" -transparent white """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_removed.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_removed.png"" -channel A -morphology EdgeIn Diamond """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_edges.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_edges.png"" -morphology Dilate Gaussian " & NumberOfResizeWater & " -layers RemoveDups -filter Gaussian -resize " & ClassWorker.MyWorldSettings.BlocksPerTile & "x" & ClassWorker.MyWorldSettings.BlocksPerTile & "! -reverse -background None -flatten -alpha off """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png"" -level 0.391%%,100.391%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png""")
                    ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_filled.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_water_removed.png"" -compose over -composite """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png""")
                End If
            End If
            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png"" -blur 5 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png""")

            Dim scale As Double = Math.Round(36768000 / ((CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) * (360 / CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16)))), 1)

            If scale < 25 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png"" -sample 3.125%% -magnify -magnify -magnify -magnify -magnify -define png:color-type=6 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png""")
            ElseIf scale < 50 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png"" -sample 6.25%% -magnify -magnify -magnify -magnify -define png:color-type=6 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png""")
            ElseIf scale < 100 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png"" -sample 12.5%% -magnify -magnify -magnify -define png:color-type=6 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png""")
            ElseIf scale < 200 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png"" -sample 25%% -magnify -magnify -define png:color-type=6 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png""")
            ElseIf scale < 500 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png"" -sample 50%% -magnify -define png:color-type=6 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_climate.png""")
            End If

            If scale < 50 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 1.5625%% -magnify -magnify -magnify -magnify -magnify -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            ElseIf scale < 100 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 3.125%% -magnify -magnify -magnify -magnify -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            ElseIf scale < 200 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 6.25%% -magnify -magnify -magnify -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            ElseIf scale < 500 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 12.5%% -magnify -magnify -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            ElseIf scale < 1000 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 25%% -magnify -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            ElseIf scale < 2000 Then
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png"" -sample 50%% -magnify """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_ocean_temp.png""")
            End If

            If Not ClassWorker.MyWorldSettings.terrainModifier = 0 Then
                Select Case ClassWorker.MyWorldSettings.terrainModifier
                    Case -2
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain.png"" -channel R -level -15%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png"" -channel G -level -5%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                    Case -1
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain.png"" -channel R -level -5%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png"" -channel G -level -2%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                    Case 1
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain.png"" -channel G -level -2%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png"" -channel R -level -0%%,100%%,0.9 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                    Case 2
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain.png"" -channel G -level -5%%,100%% """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                        ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png"" -channel R -level 0%%,100%%,0.8 """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png""")
                End Select
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_modified.png"" -dither None -remap """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\terrain\" & ClassWorker.MyWorldSettings.Terrain & ".png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_reduced_colors.png""")
            Else
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMagick & """ convert """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain.png"" -dither None -remap """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\terrain\" & ClassWorker.MyWorldSettings.Terrain & ".png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & "_terrain_reduced_colors.png""")
            End If

            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '5-magick.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub WpScriptBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-log-wpscript.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-wpscript.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")

            Dim LatiDir = Tile.Substring(0, 1)
            Dim LatiNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
            Dim LongDir = Tile.Substring(3, 1)
            Dim LongNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(4, 3), LongNumber)
            Dim ReplacedString = LatiDir & " " & LatiNumber.ToString & " " & LongDir & " " & LongNumber.ToString
            Dim MapVersionShort As String = "1-19"
            Select Case ClassWorker.MyWorldSettings.MapVersion
                Case "1.12"
                    MapVersionShort = "1-12"
                Case "1.16"
                    MapVersionShort = "1-16"
                Case "1.17"
                    MapVersionShort = "1-17"
                Case "1.18"
                    MapVersionShort = "1-18"
                Case "1.19"
                    MapVersionShort = "1-19"
            End Select

            Dim NewTile As String = ""
            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            Dim NewBiomeSource As String = "koeppen"
            Select Case ClassWorker.MyWorldSettings.biomeSource
                Case "Terrestrial Ecoregions (WWF)"
                    NewBiomeSource = "ecoregions"
                Case "Köppen Climate Classification"
                    NewBiomeSource = "koeppen"
            End Select
            If Tile = ClassWorker.MySelection.SpawnTile Then
                ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & NewTile & ".png*""")
                If ClassWorker.MyWorldSettings.TilesPerMap = "1" Then
                    ScriptBatchFile.WriteLine("""C:\Windows\system32\xcopy.exe"" /y """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & Tile & ".png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\heightmap\" & NewTile & ".png*""")
                Else
                End If
            End If
            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToWorldPainterFolder & """ """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript.js"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder.Replace("\", "/") & "/"" " & ReplacedString & " " & ClassWorker.MyWorldSettings.BlocksPerTile & " " & ClassWorker.MyWorldSettings.TilesPerMap & " " & ClassWorker.MyWorldSettings.VerticalScale & " " & ClassWorker.MyWorldSettings.bordersBoolean.ToString & " " & ClassWorker.MyWorldSettings.highways.ToString & " " & ClassWorker.MyWorldSettings.streets.ToString & " " & ClassWorker.MyWorldSettings.small_streets.ToString & " " & ClassWorker.MyWorldSettings.buildings.ToString & " " & ClassWorker.MyWorldSettings.ores.ToString & " " & ClassWorker.MyWorldSettings.netherite.ToString & " " & ClassWorker.MyWorldSettings.farms.ToString & " " & ClassWorker.MyWorldSettings.meadows.ToString & " " & ClassWorker.MyWorldSettings.quarrys.ToString & " " & ClassWorker.MyWorldSettings.aerodrome.ToString & " " & ClassWorker.MyWorldSettings.mobSpawner.ToString & " " & ClassWorker.MyWorldSettings.animalSpawner.ToString & " " & ClassWorker.MyWorldSettings.riversBoolean.ToString & " " & ClassWorker.MyWorldSettings.streams.ToString & " " & ClassWorker.MyWorldSettings.volcanos.ToString & " " & ClassWorker.MyWorldSettings.shrubs.ToString & " " & ClassWorker.MyWorldSettings.crops.ToString & " " & MapVersionShort & " " & ClassWorker.MyWorldSettings.mapOffset & " " & ClassWorker.MyWorldSettings.vanillaPopulation & " " & NewTile & " " & NewBiomeSource & " " & ClassWorker.MyWorldSettings.oreModifier.ToString & " " & ClassWorker.MyWorldSettings.mod_BOP & " " & ClassWorker.MyWorldSettings.mod_BYG & " " & ClassWorker.MyWorldSettings.mod_Terralith & " " & ClassWorker.MyWorldSettings.mod_Create)
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '6-wpscript.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub VoidScriptBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-log-voidscript.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-voidscript.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-voidscript.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports""")
            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & """ mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & """")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\void.png"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & "\" & Tile & ".png""")


            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("if not exist """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files"" mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")

            Dim TileArray() As String = Split(Tile, "x")
            Dim longitude As Int32 = CType(TileArray(0), Integer)
            Dim latitude As Int32 = CType(TileArray(1), Integer)

            Dim MapVersionShort As String = "1-12"
            Select Case ClassWorker.MyWorldSettings.MapVersion
                Case "1.12"
                    MapVersionShort = "1-12"
                Case "1.16"
                    MapVersionShort = "1-16"
                Case "1.17"
                    MapVersionShort = "1-17"
                Case "1.18"
                    MapVersionShort = "1-18"
                Case "1.19"
                    MapVersionShort = "1-19"
            End Select

            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToWorldPainterFolder & """ """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\voidscript.js"" """ & ClassWorker.MyTilesSettings.PathToScriptsFolder.Replace("\", "/") & "/"" ""x" & latitude & """ ""x" & longitude & """ """ & MapVersionShort & """ ""x" & Tile & """ """ & ClassWorker.MyWorldSettings.VerticalScale & """")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '6-voidscript.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub MinutorRenderExort(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-1-log-minutors.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-1-minutors.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            Dim NewTile As String = ""
            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            Dim depth As String = "255"
            If ClassWorker.MyTilesSettings.version = "1.18" Or ClassWorker.MyWorldSettings.MapVersion = "1.19" Then
                depth = "319"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\6-1-minutors.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMinutor & """ --world """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & NewTile & """ --depth " & depth & " --savepng """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\" & Tile & ".png""")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '6-1-minutors.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub CleanupBatchExport(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-" & Tile & ".txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)

            If ClassWorker.MyTilesSettings.keepImageFiles = False And (Not ClassWorker.MySelection.SpawnTile = Tile) Then
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports\" & Tile & """")
            End If

            If ClassWorker.MyTilesSettings.keepOsmFiles = False Then
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & Chr(34))
            End If
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '8-cleanup.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub CombineBatchExport()
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-log-combine.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-combine.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-combine.bat", False, System.Text.Encoding.ASCII)

            Dim exportPath As String = ""
            If ClassWorker.MyTilesSettings.PathToExport = "" Then
                exportPath = ClassWorker.MyTilesSettings.PathToScriptsFolder
            Else
                exportPath = ClassWorker.MyTilesSettings.PathToExport
            End If

            ScriptBatchFile.WriteLine("If Not exist """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """ mkdir """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("If Not exist """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\region"" mkdir """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\region""")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & ClassWorker.MyWorldSettings.WorldName & "\level.dat"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")

            If Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & ClassWorker.MyWorldSettings.WorldName & "\datapacks\") Then
                ScriptBatchFile.WriteLine("If Not exist """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\datapacks"" mkdir """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\datapacks""")
                ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & ClassWorker.MyWorldSettings.WorldName & "\datapacks\worldpainter.zip"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\datapacks""")
            End If

            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\settings.xml"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\selection.xml"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & ClassWorker.MyWorldSettings.WorldName & "\session.lock"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("pushd """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\""")
            ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  ""C:\Windows\system32\xcopy.exe"" /Y ""%%i"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\region\""")
            ScriptBatchFile.WriteLine("popd")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '7-combine.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub MinutorFinalRenderExort(Tile As String)
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile & "\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-1-log-minutors.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-1-minutors.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            Dim NewTile As String = ""
            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\7-1-minutors.bat", False, System.Text.Encoding.ASCII)
            If Not ClassWorker.MyTilesSettings.PathToMinutor = "" And ClassWorker.MyTilesSettings.minutor = True Then
                Dim depth As String = "255"
                If ClassWorker.MyTilesSettings.version = "1.18" Or ClassWorker.MyWorldSettings.MapVersion = "1.19" Then
                    depth = "319"
                End If
                ScriptBatchFile.WriteLine("""" & ClassWorker.MyTilesSettings.PathToMinutor & """ --world """ & ClassWorker.MyTilesSettings.PathToExport & "\" & ClassWorker.MyWorldSettings.WorldName & """ --depth " & depth & " --savepng """ & ClassWorker.MyTilesSettings.PathToExport & "\" & ClassWorker.MyWorldSettings.WorldName & "\" & ClassWorker.MyWorldSettings.WorldName & ".png""")
            End If

            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '7-1-minutors.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

    Public Sub CleanupFinalBatchExport()
        Try

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\render\")
            End If

            If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
                Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
            End If

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine("CALL """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\8-cleanup.bat"" >> """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\8-cleanup.bat", False, System.Text.Encoding.ASCII)

            ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups""")
            ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\backups""")
            If ClassWorker.MyTilesSettings.keepWorldPainterFiles = False Then
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
                ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\worldpainter_files""")
            End If
            ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\unfiltered.o5m""")
            If ClassWorker.MyTilesSettings.keepPbfFile = False Then
                ScriptBatchFile.WriteLine("del """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\output.o5m""")
            End If
            If ClassWorker.MyTilesSettings.keepImageFiles = False Then
                ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports""")
                ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\image_exports""")
            End If
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\python""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\QGIS\OsmData""")
            ScriptBatchFile.WriteLine("rmdir /Q /S """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles""")
            ScriptBatchFile.WriteLine("mkdir """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles""")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine("PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception("File '8-cleanup.bat' could not be saved. " & ex.Message)
        End Try
    End Sub

#End Region

    Public Sub CalculateDuration()
        If tilesReady > 0 Then
            Dim percentReady As Double = Math.Round((tilesReady / maxTiles) * 100, 2)
            Dim minutesLeftDiff As Double = DateDiff(DateInterval.Minute, DateTime.Now, startTime) / (tilesReady / maxTiles) * (1 - (tilesReady / maxTiles))
            Dim minutesDoneDiff As Double = DateDiff(DateInterval.Minute, DateTime.Now, startTime)
            hoursLeft = CType(Fix(Math.Abs(minutesLeftDiff) / 60), Int32)
            minutesLeft = CType(Math.Abs(minutesLeftDiff) Mod 60, Int32)
            hoursDone = CType(Fix(Math.Abs(minutesDoneDiff) / 60), Int32)
            minutesDone = CType(Math.Abs(minutesDoneDiff) Mod 60, Int32)
        End If
    End Sub

    Private Function CalcScale() As Double
        Return 36768000 / (CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int32) * (360 / CType(ClassWorker.MyWorldSettings.TilesPerMap, Int32)))
    End Function

    Public Function WriteLog(Message As String, Optional Tile As String = "") As Boolean
        Dim path As String = ""
        If Not Directory.Exists(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\") Then
            Directory.CreateDirectory(ClassWorker.MyTilesSettings.PathToScriptsFolder & "\logs\")
        End If
        If Tile = "" Then
            path = $"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\log-general.txt"
        Else
            path = $"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\log-{Tile}.txt"
        End If
        Try
            Using sw As StreamWriter = File.AppendText(path)
                sw.WriteLine(Message)
            End Using
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class

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
    Private stateOverpass As Boolean = False
    Public keepRunning As Boolean = True

    Public pause As Boolean = False

    Public neuerthread As Thread
    Public cts As CancellationTokenSource = New CancellationTokenSource
    Private myOptions As ParallelOptions = New ParallelOptions()

    Public processList As List(Of Process) = New List(Of Process)

    Public tilesReady As Int32 = 0
    Public maxTiles As Int32 = (ClassWorker.MySelection.TilesList.Count + 3) - ClassWorker.MySelection.VoidTiles

    Public startTime As DateTime = DateTime.Now

    Private Enum BatchFileName
        OsmbatGenerationPrepare
        OsmbatGeneration
        QgisRepairGeneration
        QgisGeneration
        TartoolGeneration
        GdalGeneration
        ImageMagickGeneration
        WpScriptGeneration
        VoidScriptGeneration
        MinutorGeneration
        CleanupGeneration
        CombineGeneration
        MinutorFinalGeneration
        CleanupFinalGeneration
    End Enum

    Public Property hoursLeft As Int32 = 0
    Public Property minutesLeft As Int32 = 0
    Public Property hoursDone As Int32 = 0
    Public Property minutesDone As Int32 = 0

    Private _LatestTile As String = ""
    Public Property LatestTile As String
        Get
            Return _LatestTile
        End Get
        Set(ByVal value As String)
            If Not _LatestTile = value Then
                _LatestTile = value
                OnPropertyChanged("LatestTile")
            End If
        End Set
    End Property

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
        CreateFilesAndFolders()
    End Sub

    Public Sub CreateGeneration()
        If ClassWorker.MySelection.TilesList.Count = 0 Then
            Throw New Exception($"No Tiles selected.")
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName

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

                            If keepRunning And keepRunningLocal Then
                                tilesReady += 1
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                        LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName

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

                            If Not ClassWorker.MyWorldSettings.generateFullEarth Then

                                If ClassWorker.MyWorldSettings.geofabrik = True And ClassWorker.MyTilesSettings.reUseOsmFiles = False And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                    If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToTempOSM}\osm\output.o5m") Then
                                        Tile.Comment = "Error: Could Not find output.o5m file"
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

                                Dim r0 As Random = New Random(Tile.TileName.GetHashCode())
                                Dim timer0 As Int32 = r0.Next(1, 10000)

                                Threading.Thread.Sleep(timer0)

                                While stateOverpass = True
                                    While pause = True
                                        Tile.Comment = "Pause"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                        Threading.Thread.Sleep(10000)
                                    End While
                                    Tile.Comment = "Waiting for Overpass-API"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    Threading.Thread.Sleep(timer0)
                                End While

                                If keepRunning And keepRunningLocal Then
                                    If ClassWorker.MyWorldSettings.geofabrik = False Then
                                        stateOverpass = True
                                    End If
                                    If keepRunning And keepRunningLocal Then
                                        OsmbatBatchExport(Tile.TileName)
                                        OsmbatGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                    End If
                                    If ClassWorker.MyWorldSettings.geofabrik = False Then
                                        stateOverpass = False
                                    End If

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

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\beach.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\beach.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\broadleaved.dbf") = True And My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\mixedforest.dbf") = True And My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\needleleaved.dbf") = True Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\forest.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\farmland.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\farmland.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\glacier.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\glacier.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\grass.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\grass.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\meadow.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\meadow.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\quarry.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\quarry.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\urban.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\urban.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\water.dbf") Then
                                    Try
                                        File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\water.osm")
                                    Catch ex As Exception
                                        WriteLog(ex.Message)
                                    End Try
                                End If

                                If keepRunning And keepRunningLocal Then
                                    tilesReady += 1
                                    SaveCurrentState()
                                    Tile.GenerationProgress = 100
                                    CalculateDuration()
                                    Tile.Comment = "Finished"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                End If

                            Else

                                    Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Skipped"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment

                            End If
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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

        If keepRunning Then
            For Each Tile In MyGeneration
                If Tile.TileName = "Convert pbf" Then
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName

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
                            Dim filesList As New List(Of String)
                            If ClassWorker.MyWorldSettings.generateFullEarth Then
                                filesList = New List(Of String) From {
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\aerodrome.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\bare_rock.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\big_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\border.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\stateborder.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\highway.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\middle_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\river.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\small_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\stream.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\vineyard.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\volcano.osm"
                                }
                            Else
                                filesList = New List(Of String) From {
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\big_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\border.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stateborder.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\highway.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\middle_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\river.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\small_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stream.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\vineyard.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\volcano.osm"
                                }
                            End If

                            Dim missingOSM As Integer = 0
                            For Each myFile In filesList
                                If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{myFile}") Then
                                    missingOSM += 1
                                End If
                            Next
                            If missingOSM > 1 Then
                                Tile.Comment = $"Error {missingOSM} osm files missing"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
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

                            While pause = True
                                Tile.Comment = "Pause"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                Threading.Thread.Sleep(10000)
                            End While

                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\image_exports\{Tile.TileName}\heightmap\{Tile.TileName}_exported.png") Then
                                Tile.Comment = "Error " & Tile.TileName & " Heightmap Not found"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
                            End If

                            If keepRunning And keepRunningLocal Then
                                ImageMagickBatchExport(Tile.TileName)
                                ImageMagickGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                            End If

                            If keepRunning And keepRunningLocal Then
                                tilesReady += 1
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName

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

                            If keepRunning And keepRunningLocal Then
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

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

                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\image_exports\{Tile.TileName}\{Tile.TileName}_terrain_reduced_colors.png") Then
                                Tile.Comment = "Error " & Tile.TileName & "_terrain_reduced_colors.png Not found"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
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

                            Dim Foldername As String = ""
                            If ClassWorker.MySelection.SpawnTile = Tile.TileName Then
                                Foldername = ClassWorker.MyWorldSettings.WorldName
                            Else
                                Foldername = Tile.TileName
                            End If

                            If keepRunning And keepRunningLocal Then

                                If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                    stateWorldPainter = True
                                End If
                                WpScriptBatchExport(Tile.TileName, False)
                                WpScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                    stateWorldPainter = False
                                End If

                                If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\wpscript\exports\{Foldername}\level.dat") Then
                                    currentParallelProcessInfo.CreateNoWindow = False
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
                                    currentParallelProcessInfo.UseShellExecute = True
                                    If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                        stateWorldPainter = True
                                    End If
                                    WpScriptBatchExport(Tile.TileName, True)
                                    WpScriptDebugGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                    If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                        stateWorldPainter = False
                                    End If
                                    If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                        currentParallelProcessInfo.CreateNoWindow = True
                                        currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                        currentParallelProcessInfo.UseShellExecute = False
                                    End If
                                End If

                            End If

                            While pause = True
                                Tile.Comment = "Pause"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                Threading.Thread.Sleep(10000)
                            End While

                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\wpscript\exports\{Foldername}\level.dat") Then
                                Tile.Comment = "Error " & Tile.TileName & "\level.dat Not found"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
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

                            If keepRunning And keepRunningLocal Then
                                tilesReady += 1
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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

        If keepRunning Then
            For Each Tile In MyGeneration
                Try
                    If Tile.TileName = "Convert pbf" Then
                        LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName

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

                            If keepRunning And keepRunningLocal Then
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

                        Catch ex As Exception
                            WriteLog(ex.Message)
                            Tile.Comment = ex.Message
                            WriteLog(Tile.Comment, Tile.TileName)
                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                        End Try

                    Else

                        Try

                            If Not ClassWorker.MyWorldSettings.generateFullEarth Then

                                Dim reUseFilesList As New List(Of String) From {
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\big_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\border.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stateborder.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\highway.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\middle_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\river.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\small_road.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stream.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\vineyard.osm",
                                    ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\volcano.osm"
                                }

                                For Each myFile In reUseFilesList
                                    If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{myFile}") And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                        ClassWorker.MyTilesSettings.reUseOsmFiles = False
                                    End If
                                Next

                                If ClassWorker.MyWorldSettings.geofabrik = True And ClassWorker.MyTilesSettings.reUseOsmFiles = False And ClassWorker.MyTilesSettings.reUseImageFiles = False Then
                                    If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToTempOSM}\osm\output.o5m") Then
                                        Tile.Comment = "Error Could Not find output.o5m file"
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

                                    Dim r0 As Random = New Random(Tile.TileName.GetHashCode())
                                    Dim timer0 As Int32 = r0.Next(1, 10000)

                                    Threading.Thread.Sleep(timer0)

                                    While stateOverpass = True
                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While
                                        Tile.Comment = "Waiting for Overpass-API"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                        Threading.Thread.Sleep(timer0)
                                    End While

                                    If keepRunning And keepRunningLocal Then
                                        If ClassWorker.MyWorldSettings.geofabrik = False Then
                                            stateOverpass = True
                                        End If
                                        If keepRunning And keepRunningLocal Then
                                            OsmbatBatchExport(Tile.TileName)
                                            OsmbatGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                        End If
                                        If ClassWorker.MyWorldSettings.geofabrik = False Then
                                            stateOverpass = False
                                        End If

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

                                    Dim r1 As Random = New Random(Tile.TileName.GetHashCode())
                                    Dim timer1 As Int32 = r1.Next(1, 10000)

                                    Threading.Thread.Sleep(timer1)

                                    While stateQGIS = True
                                        While pause = True
                                            Tile.Comment = "Pause"
                                            WriteLog(Tile.Comment, Tile.TileName)
                                            LatestMessage = Tile.TileName & ": " & Tile.Comment
                                            Threading.Thread.Sleep(10000)
                                        End While
                                        Tile.Comment = "Waiting For QGIS"
                                        WriteLog(Tile.Comment, Tile.TileName)
                                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                                        Threading.Thread.Sleep(timer1)
                                    End While
                                    stateQGIS = True
                                    QgisRepairBatchExport(Tile.TileName)
                                    QgisRepairGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                    stateQGIS = False

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\beach.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\beach.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\broadleaved.dbf") = True And My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\mixedforest.dbf") = True And My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\needleleaved.dbf") = True Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\forest.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\farmland.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\farmland.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\glacier.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\glacier.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\grass.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\grass.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\meadow.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\meadow.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\quarry.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\quarry.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\urban.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\urban.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\water.dbf") Then
                                        Try
                                            File.Delete($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\osm\{Tile.TileName}\water.osm")
                                        Catch ex As Exception
                                            WriteLog(ex.Message)
                                        End Try
                                    End If

                                End If
                            End If

                            If (ClassWorker.MyTilesSettings.reUseImageFiles = False) Then

                                Dim filesList As New List(Of String)
                                If ClassWorker.MyWorldSettings.generateFullEarth Then
                                    filesList = New List(Of String) From {
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\aerodrome.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\bare_rock.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\big_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\border.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\stateborder.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\highway.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\middle_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\river.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\small_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\stream.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\vineyard.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\all\volcano.osm"
                                     }
                                Else
                                    filesList = New List(Of String) From {
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\aerodrome.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\bare_rock.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\big_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\border.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stateborder.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\highway.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\middle_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\river.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\small_road.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\stream.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\vineyard.osm",
                                         ClassWorker.MyTilesSettings.PathToTempOSM & "\osm\" & Tile.TileName & "\volcano.osm"
                                     }
                                End If

                                Dim missingOSM As Integer = 0
                                For Each myFile In filesList
                                    If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{myFile}") Then
                                        missingOSM += 1
                                    End If
                                Next
                                If missingOSM > 1 Then
                                    Tile.Comment = $"Error {missingOSM} osm files missing"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                        keepRunning = False
                                    End If
                                    keepRunningLocal = False
                                    Return
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

                                If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\image_exports\{Tile.TileName}\heightmap\{Tile.TileName}_exported.png") Then
                                    Tile.Comment = "Error " & Tile.TileName & " Heightmap Not found"
                                    WriteLog(Tile.Comment, Tile.TileName)
                                    LatestMessage = Tile.TileName & ": " & Tile.Comment
                                    If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                        keepRunning = False
                                    End If
                                    keepRunningLocal = False
                                    Return
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

                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\image_exports\{Tile.TileName}\{Tile.TileName}_terrain_reduced_colors.png") Then
                                Tile.Comment = "Error " & Tile.TileName & "_terrain_reduced_colors.png Not found"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
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

                            Dim Foldername As String = ""
                            If ClassWorker.MySelection.SpawnTile = Tile.TileName Then
                                Foldername = ClassWorker.MyWorldSettings.WorldName
                            Else
                                Foldername = Tile.TileName
                            End If

                            If keepRunning And keepRunningLocal Then

                                If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                    stateWorldPainter = True
                                End If
                                WpScriptBatchExport(Tile.TileName, False)
                                WpScriptGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                    stateWorldPainter = False
                                End If

                                If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\wpscript\exports\{Foldername}\level.dat") Then
                                    currentParallelProcessInfo.CreateNoWindow = False
                                    currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Minimized
                                    currentParallelProcessInfo.UseShellExecute = True
                                    If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                        stateWorldPainter = True
                                    End If
                                    WpScriptBatchExport(Tile.TileName, True)
                                    WpScriptDebugGeneration(Tile, keepRunningLocal, currentParallelProcess, currentParallelProcessInfo)
                                    If ClassWorker.MyTilesSettings.ParallelWorldPainterGenerations = False Then
                                        stateWorldPainter = False
                                    End If
                                    If ClassWorker.MyTilesSettings.cmdVisibility = False Then
                                        currentParallelProcessInfo.CreateNoWindow = True
                                        currentParallelProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
                                        currentParallelProcessInfo.UseShellExecute = False
                                    End If
                                End If

                            End If

                            While pause = True
                                Tile.Comment = "Pause"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                Threading.Thread.Sleep(10000)
                            End While

                            If keepRunning And keepRunningLocal And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\wpscript\exports\{Foldername}\level.dat") Then
                                Tile.Comment = "Error " & Tile.TileName & "\level.dat Not found"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                                    keepRunning = False
                                End If
                                keepRunningLocal = False
                                Return
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

                            If keepRunning And keepRunningLocal Then
                                tilesReady += 1
                                SaveCurrentState()
                                Tile.GenerationProgress = 100
                                CalculateDuration()
                                Tile.Comment = "Finished"
                                WriteLog(Tile.Comment, Tile.TileName)
                                LatestMessage = Tile.TileName & ": " & Tile.Comment
                            End If

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
                    LatestTile = Tile.TileName
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
                    LatestTile = Tile.TileName
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
        CustomXmlSerialiser.SaveXML($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}/selection-left.xml", ClassWorker.MySelection)
    End Sub


#Region "Generate World"

    Public Sub OsmbatGenerationPrepare(ByRef Tile As Generation, ByRef currentProcess As Process, ByRef currentProcessInfo As ProcessStartInfo)

        CalculateDuration()

        Dim filesList As New List(Of String) From {
            ClassWorker.MyTilesSettings.PathToQGISProject & "\MinecraftEarthTiles.qgz",
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmconvert.exe",
            ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osmfilter.exe",
            ClassWorker.MyTilesSettings.PathToQGISProject & "\empty.osm"
        }
        If ClassWorker.MyWorldSettings.bathymetry Then
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectBathymetryAddon}\TifFiles\bathymetry.tif")
        End If

        If ClassWorker.MyWorldSettings.TerrainSource = "High Quality Offline Terrain (Addon)" Then
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.A1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.A2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.A3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.A4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.B1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.B2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.B3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.B4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.C1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.C2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.C3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.C4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.D1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.D2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.D3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.D4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.E1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.E2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.E3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.E4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.F1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.F2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.F3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.F4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.G1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.G2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.G3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.G4.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.H1.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.H2.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.H3.tif")
            filesList.Add($"{ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}\TifFiles\terrain\TrueMarble.250m.21600x21600.H4.tif")
        End If

        For Each myFile In filesList
            If Not My.Computer.FileSystem.FileExists($"{myFile}") Then
                Tile.Comment = $"Error '{myFile}' not found"
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

            ElseIf (ClassWorker.MyTilesSettings.reUsePbfFile = False And ClassWorker.MyWorldSettings.geofabrik = True) Or (ClassWorker.MyTilesSettings.reUsePbfFile = True And ClassWorker.MyWorldSettings.geofabrik = True And Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToTempOSM}\osm\output.o5m")) Then

                If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyWorldSettings.PathToPBF}") Then
                    Tile.Comment = "Error: *.pbf file Not found"
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
                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\1-log-osmconvert.bat") Then
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
                        Tile.Comment = "1-osmconvert.bat Not found"
                        WriteLog(Tile.Comment)
                        LatestMessage = Tile.TileName & ": " & Tile.Comment
                    End If

                    If Not My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToTempOSM}\osm\output.o5m") Then
                        Tile.Comment = "Error Could Not create output.o5m file"
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
            If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\1-log-osmconvert.bat") Then
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
                Tile.Comment = Tile.TileName & "\1-osmconvert.bat Not found"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
            End If
        Else
            If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\1-osmconvert.bat") Then
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
                Tile.Comment = Tile.TileName & "\1-osmconvert.bat Not found"
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

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\2-1-log-qgis.bat") Then

            If System.IO.File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db") Then
                System.IO.File.Delete($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db")
            End If

            Tile.Comment = "Repairing OSM With QGIS"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-1-log-qgis.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            If My.Computer.FileSystem.FileExists($"{Environment.SpecialFolder.ApplicationData}\EarthTilesPythonError") Then
                Tile.GenerationProgress = 30
                Tile.Comment = "Error In the python environment"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
                System.IO.File.Delete($"{Environment.SpecialFolder.ApplicationData}\EarthTilesPythonError")
            End If
            Tile.GenerationProgress = 33
        Else
            Tile.GenerationProgress = 33
            Tile.Comment = Tile.TileName & "\2-1-qgis.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub QgisGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)
        If System.IO.File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db") Then
            System.IO.File.Delete($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db")
        End If

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\2-2-log-qgis.bat") Then

            If System.IO.File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db") Then
                System.IO.File.Delete($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\QGIS\QGIS3\profiles\Default\qgis.db")
            End If

            Tile.Comment = "Creating images With QGIS"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\2-2-log-qgis.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            If My.Computer.FileSystem.FileExists($"{Environment.SpecialFolder.ApplicationData}\EarthTilesPythonError") Then
                Tile.GenerationProgress = 30
                Tile.Comment = "Error In the python environment"
                WriteLog(Tile.Comment, Tile.TileName)
                LatestMessage = Tile.TileName & ": " & Tile.Comment
                If Not ClassWorker.MyTilesSettings.continueGeneration Then
                    keepRunning = False
                End If
                keepRunningLocal = False
                System.IO.File.Delete($"{Environment.SpecialFolder.ApplicationData}\EarthTilesPythonError")
            End If
            Tile.GenerationProgress = 30
        Else
            Tile.GenerationProgress = 30
            Tile.Comment = Tile.TileName & "\2-qgis.bat Not found"
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
                    System.IO.File.Delete($"{_file}")
                Catch ex As Exception
                    WriteLog(ex.Message)
                End Try
            Next
        End If

        If Directory.Exists(Path.GetTempPath) Then
            For Each _file As String In Directory.GetFiles(Path.GetTempPath, "*osm*")
                Try
                    System.IO.File.Delete($"{_file}")
                Catch ex As Exception
                    WriteLog(ex.Message)
                End Try
            Next
        End If

        'Try
        'My.Computer.FileSystem.WriteAllText(ClassWorker.MyTilesSettings.PathToQGIS}\apps\qgis\python\qgis\utils.py", My.Resources.utils_orig, False)
        'Catch ex As Exception 
        'WriteLog(ex.Message)
        '
        'End Try
    End Sub

    Public Sub TartoolGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\3-log-tartool.bat") Then
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
            Tile.Comment = Tile.TileName & "\3-tartool.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub GdalGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\4-log-gdal.bat") Then
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
            Tile.Comment = Tile.TileName & "\4-gdal.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub ImageMagickGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\5-log-magick.bat") Then
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
            Tile.Comment = Tile.TileName & "\5-magick.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub WpScriptGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\6-log-wpscript.bat") Then
            Tile.Comment = "Creating world With WorldPainter"
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
            Tile.Comment = Tile.TileName & "\6-wpscript.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub WpScriptDebugGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\6-wpscript-debug.bat") Then
            Tile.Comment = "Debugging WorldPainter - Check the CMD output"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            currentParallelProcessInfo.FileName = ClassWorker.MyTilesSettings.PathToScriptsFolder & "\batchfiles\" & Tile.TileName & "\6-wpscript-debug.bat"
            currentParallelProcess = Process.Start(currentParallelProcessInfo)
            processList.Add(currentParallelProcess)
            currentParallelProcess.WaitForExit()
            processList.Remove(currentParallelProcess)
            currentParallelProcess.Close()
            Tile.GenerationProgress = 90
        Else
            Tile.GenerationProgress = 90
            Tile.Comment = Tile.TileName & "\6-wpscript-debug.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub VoidScriptGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\6-log-voidscript.bat") Then
            Tile.Comment = "Creating world With WorldPainter"
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
            Tile.Comment = Tile.TileName & "\6-voidscript.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub MinutorGeneration(ByRef Tile As Generation, ByRef keepRunningLocal As Boolean, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\6-1-log-minutors.bat") Then
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
            Tile.Comment = Tile.TileName & "\6-1-minutors.bat Not found"
            WriteLog(Tile.Comment, Tile.TileName)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            If Not ClassWorker.MyTilesSettings.continueGeneration Then
                keepRunning = False
            End If
            keepRunningLocal = False
        End If
    End Sub

    Public Sub CleanupGeneration(ByRef Tile As Generation, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile.TileName}\8-log-cleanup.bat") Then
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

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-log-combine.bat") Then
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
            Tile.Comment = "7-combine.bat Not found"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            keepRunning = False
        End If
    End Sub

    Public Sub MinutorFinalGeneration(ByRef Tile As Generation, ByRef currentParallelProcess As Process, ByRef currentParallelProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-1-log-minutors.bat") Then
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
            Tile.Comment = Tile.TileName & "\7-1-minutors.bat Not found"
            WriteLog(Tile.Comment)
            LatestMessage = Tile.TileName & ": " & Tile.Comment
            keepRunning = False
        End If
    End Sub

    Public Sub CleanUpFinalGeneration(ByRef Tile As Generation, ByRef currentProcess As Process, ByRef currentProcessInfo As ProcessStartInfo)

        If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\8-log-cleanup.bat") Then
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
            Tile.Comment = "8-cleanup.bat Not found"
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
                e_mail.Subject = $"Generation Of world ""{ClassWorker.MyWorldSettings.WorldName}"" complete"
                e_mail.IsBodyHtml = False

                Dim minutesDoneDiff As Double = DateDiff(DateInterval.Minute, DateTime.Now, startTime)
                Dim hoursDone As Int32 = CType(Fix(Math.Abs(minutesDoneDiff) / 60), Int32)
                Dim minutesDone As Int32 = CType(Math.Abs(minutesDoneDiff) Mod 60, Int32)

                e_mail.Body = $"The generation took ""{hoursDone} h And {minutesDone} min"" In total.
                        {vbNewLine}The message was sent from ""{Environment.MachineName}"" using the windows account ""{UserPrincipal.Current.Name} ({UserPrincipal.Current.EmailAddress})"".
                        {vbNewLine}This message was generated automatically using the Earth Tiles Software.
                        {vbNewLine}Please don't reply to this address."

                Smtp_Server.Send(e_mail)
            Catch ex As Exception
                WriteLog(ex.Message)
                MsgBox(ex.Message)
            End Try
            keepRunning = False
            GenerationComplete = True
        End If
    End Sub

#End Region

#Region "Create Batch Files"

    Public Sub OsmbatExportPrepare()
        Try
            CreateFilesAndFolders()

            Dim ScriptBatchFile As StreamWriter

            If ClassWorker.MyWorldSettings.geofabrik = True Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\1-osmconvert.bat"" > ""%PathToScriptsFolder%\logs\log-general.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                WriteTilesVariablesToBatch(ScriptBatchFile, "Convert pbf")
                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGenerationPrepare, "Before")

                ScriptBatchFile.WriteLine($"if Not exist ""%PathToTempOSM%\osm"" mkdir ""%PathToTempOSM%\osm""")
                ScriptBatchFile.WriteLine($"if Not exist ""%PathToScriptsFolder%\image_exports"" mkdir ""%PathToScriptsFolder%\image_exports""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmconvert.exe"" ""%PathToPBF%"" -o=""%PathToTempOSM%\osm\unfiltered.o5m""")

                Dim filter As String = "natural=glacier Or natural=volcano Or natural=beach Or landuse=grass Or natural=grassland Or natural=fell Or natural=heath Or natural=scrub Or landuse=forest Or landuse=bare_rock Or natural=scree Or natural=shingle Or natural=wetland"

                If ClassWorker.MyWorldSettings.waterBodies = "All" Then
                    filter &= " Or water=lake Or water=reservoir Or natural=water Or landuse=reservoir"
                End If

                If ClassWorker.MyWorldSettings.highways Then
                    filter &= " Or highway=motorway Or highway=trunk"
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    filter &= " Or highway=primary Or highway=secondary Or highway=tertiary"
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    filter &= " Or highway=residential"
                End If

                If ClassWorker.MyWorldSettings.riversBoolean And Not (ClassWorker.MyWorldSettings.rivers = "Major" Or ClassWorker.MyWorldSettings.rivers = "Major + Minor") Then
                    filter &= " Or waterway=river Or waterway=canal Or water=river Or waterway=riverbank"
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    filter &= " Or waterway=river Or water=river Or waterway=stream"
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    filter &= " Or landuse=farmland Or landuse=vineyard"
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    filter &= " Or landuse=meadow"
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    filter &= " Or landuse=quarry"
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        filter &= " Or aeroway=aerodrome And iata="
                    Else
                        filter &= " Or aeroway=launchpad"
                    End If
                End If

                If ClassWorker.MyWorldSettings.buildings Then
                    filter &= " Or landuse=commercial Or landuse=construction Or landuse=industrial Or landuse=residential Or landuse=retail"
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    filter &= " Or boundary=administrative And admin_level=2"
                End If

                If ClassWorker.MyWorldSettings.stateBorders = True Then
                    filter &= " Or boundary=administrative And admin_level=3 Or boundary=administrative And admin_level=4"
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\unfiltered.o5m"" --verbose --keep=""{filter}"" -o=""%PathToTempOSM%\osm\output.o5m""")
                ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\unfiltered.o5m""")

                If ClassWorker.MyWorldSettings.generateFullEarth Then

                    ScriptBatchFile.WriteLine($"if Not exist ""%PathToTempOSM%\osm\all"" mkdir ""%PathToTempOSM%\osm\all""")

                    'OSM CONVERT

                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmconvert.exe""  ""%PathToTempOSM%\osm\output.o5m"" -o=""%PathToTempOSM%\osm\all\output.osm"" --drop-version --verbose")

                    If ClassWorker.MyWorldSettings.highways Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""%PathToTempOSM%\osm\all\highway.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\highway.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.streets Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""%PathToTempOSM%\osm\all\big_road.osm""")
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""highway=tertiary"" -o=""%PathToTempOSM%\osm\all\middle_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\big_road.osm*""")
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\middle_road.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.small_streets Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""highway=residential"" -o=""%PathToTempOSM%\osm\all\small_road.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\small_road.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.waterBodies = "All" Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir"" -o=""%PathToTempOSM%\osm\all\water.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\water.osm*""")
                    End If

                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""natural=wetland"" -o=""%PathToTempOSM%\osm\all\wetland.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""natural=wetland AND wetland=swamp"" -o=""%PathToTempOSM%\osm\all\swamp.osm""")

                    If ClassWorker.MyWorldSettings.riversBoolean And Not (ClassWorker.MyWorldSettings.rivers = "Major" Or ClassWorker.MyWorldSettings.rivers = "Major + Minor") Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=riverbank OR waterway=canal"" -o=""%PathToTempOSM%\osm\all\river.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\river.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.streams Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=stream"" -o=""%PathToTempOSM%\osm\all\stream.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\stream.osm*""")
                    End If

                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""natural=glacier"" -o=""%PathToTempOSM%\osm\all\glacier.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""%PathToTempOSM%\osm\all\volcano.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""natural=beach"" -o=""%PathToTempOSM%\osm\all\beach.osm""")

                    If ClassWorker.MyWorldSettings.forests Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=forest"" -o=""%PathToTempOSM%\osm\all\forest.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\forest.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.farms Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=farmland"" -o=""%PathToTempOSM%\osm\all\farmland.osm""")
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""%PathToTempOSM%\osm\all\vineyard.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\farmland.osm*""")
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\vineyard.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.meadows Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=meadow"" -o=""%PathToTempOSM%\osm\all\meadow.osm""")
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""%PathToTempOSM%\osm\all\grass.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\meadow.osm*""")
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\grass.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.quarrys Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=quarry"" -o=""%PathToTempOSM%\osm\all\quarry.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\quarry.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.aerodrome Then
                        If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                            ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""%PathToTempOSM%\osm\all\aerodrome.osm""")
                        Else
                            ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""%PathToTempOSM%\osm\all\aerodrome.osm""")
                        End If
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\aerodrome.osm*""")
                    End If

                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""%PathToTempOSM%\osm\all\bare_rock.osm""")
                    If ClassWorker.MyWorldSettings.buildings Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""%PathToTempOSM%\osm\all\urban.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\urban.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\all\border.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\border.osm*""")
                    End If

                    If ClassWorker.MyWorldSettings.stateBorders = True Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\all\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=3 OR boundary=administrative AND admin_level=4"" --drop=""natural=coastline OR admin_level=2 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\all\stateborder.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\all\stateborder.osm*""")
                    End If

                    WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGeneration, "After")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\output.osm""")

                    'QGIS REPAIR

                    Dim pythonFile As StreamWriter
                    pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\all_repair.py", False, System.Text.Encoding.ASCII)
                    Dim PathReversedSlash = Replace(ClassWorker.MyTilesSettings.PathToTempOSM, "\", "/")
                    pythonFile.WriteLine("try:" & Environment.NewLine &
                                vbTab & "import os" & Environment.NewLine &
                                vbTab & "from PyQt5.QtCore import *" & Environment.NewLine &
                                vbTab & "import processing" & Environment.NewLine)
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/urban.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/urban.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""broadleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/all/broadleaved.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/forest.osm|layername=multipolygons|subset=\""other_tags\"" = \'\""leaf_type\""=>\""needleleaved\""\'','OUTPUT':'" & PathReversedSlash & "/osm/all/needleleaved.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/forest.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/mixedforest.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/beach.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/beach.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/grass.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/grass.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/farmland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/farmland.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/meadow.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/meadow.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/quarry.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/quarry.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/water.osm|layername=multipolygons|subset=\""natural\"" = \'water\'','OUTPUT':'" & PathReversedSlash & "/osm/all/water.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/glacier.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/glacier.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/wetland.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/wetland.shp'})")
                    pythonFile.WriteLine(vbTab & "processing.run(""native:fixgeometries"", {'INPUT':'" & PathReversedSlash & "/osm/all/swamp.osm|layername=multipolygons','OUTPUT':'" & PathReversedSlash & "/osm/all/swamp.shp'})")
                    pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
                    pythonFile.WriteLine("except:" & Environment.NewLine &
                                vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
                                vbTab & vbTab & "pass" & Environment.NewLine &
                                vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
                    pythonFile.Close()

                    Dim qgisExecutableName As String = ""
                    If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-bin.exe") Then
                        qgisExecutableName = "qgis-bin.exe"
                    ElseIf My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-ltr-bin.exe") Then
                        qgisExecutableName = "qgis-ltr-bin.exe"
                    Else
                        qgisExecutableName = "qgis-bin.exe"
                    End If

                    ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\all_repair.py""")

                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\urban.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\broadleaved.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\needleleaved.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\mixedforest.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\beach.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\grass.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\farmland.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\meadow.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\quarry.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\water.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\glacier.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\wetland.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\swamp.dbf""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\urban.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\forest.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\beach.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\grass.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\farmland.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\meadow.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\quarry.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\water.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\glacier.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\wetland.osm""")
                    ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\all\swamp.osm""")

                    ScriptBatchFile.WriteLine()

                End If

                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine($"PAUSE")
                End If
                ScriptBatchFile.Close()

            Else

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\1-osmconvert.bat"" > ""%PathToScriptsFolder%\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                WriteTilesVariablesToBatch(ScriptBatchFile, "Convert pbf")

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGenerationPrepare, "After")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine($"PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            End If

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '1-osmconvert.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub OsmbatBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            If ClassWorker.MyWorldSettings.geofabrik = True Then

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
                ScriptBatchFile.WriteLine($"set Tile={Tile}")
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\1-osmconvert.bat"" > ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGeneration, "Before")

                ScriptBatchFile.WriteLine($"if not exist ""%PathToTempOSM%\osm\%Tile%"" mkdir ""%PathToTempOSM%\osm\%Tile%""")

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

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmconvert.exe""  ""%PathToTempOSM%\osm\output.o5m"" -b={borderW.ToString("##0.##", New CultureInfo("en-US"))},{borderS.ToString("#0.##", New CultureInfo("en-US"))},{borderE.ToString("##0.##", New CultureInfo("en-US"))},{borderN.ToString("#0.##", New CultureInfo("en-US"))} -o=""%PathToTempOSM%\osm\%Tile%\output.osm"" --complete-ways --complete-multipolygons --complete-boundaries --drop-version --verbose")

                If ClassWorker.MyWorldSettings.highways Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""%PathToTempOSM%\osm\%Tile%\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\highway.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""%PathToTempOSM%\osm\%Tile%\big_road.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe""  ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=tertiary"" -o=""%PathToTempOSM%\osm\%Tile%\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\big_road.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\middle_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=residential"" -o=""%PathToTempOSM%\osm\%Tile%\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\small_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.waterBodies = "All" Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir"" -o=""%PathToTempOSM%\osm\%Tile%\water.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\water.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=wetland"" -o=""%PathToTempOSM%\osm\%Tile%\wetland.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=wetland AND wetland=swamp"" -o=""%PathToTempOSM%\osm\%Tile%\swamp.osm""")

                If ClassWorker.MyWorldSettings.riversBoolean And Not (ClassWorker.MyWorldSettings.rivers = "Major" Or ClassWorker.MyWorldSettings.rivers = "Major + Minor") Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=riverbank OR waterway=canal"" -o=""%PathToTempOSM%\osm\%Tile%\river.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\river.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=stream"" -o=""%PathToTempOSM%\osm\%Tile%\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=glacier"" -o=""%PathToTempOSM%\osm\%Tile%\glacier.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""%PathToTempOSM%\osm\%Tile%\volcano.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=beach"" -o=""%PathToTempOSM%\osm\%Tile%\beach.osm""")

                If ClassWorker.MyWorldSettings.forests Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=forest"" -o=""%PathToTempOSM%\osm\%Tile%\forest.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\forest.osm*""")
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=farmland"" -o=""%PathToTempOSM%\osm\%Tile%\farmland.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""%PathToTempOSM%\osm\%Tile%\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\farmland.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\vineyard.osm*""")
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=meadow"" -o=""%PathToTempOSM%\osm\%Tile%\meadow.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""%PathToTempOSM%\osm\%Tile%\grass.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\meadow.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\grass.osm*""")
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=quarry"" -o=""%PathToTempOSM%\osm\%Tile%\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\quarry.osm*""")
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""%PathToTempOSM%\osm\%Tile%\aerodrome.osm""")
                    Else
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""%PathToTempOSM%\osm\%Tile%\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""%PathToTempOSM%\osm\%Tile%\bare_rock.osm""")
                If ClassWorker.MyWorldSettings.buildings Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""%PathToTempOSM%\osm\%Tile%\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\urban.osm*""")
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\%Tile%\border.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\border.osm*""")
                End If

                If ClassWorker.MyWorldSettings.stateBorders = True Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=3 OR boundary=administrative AND admin_level=4"" --drop=""natural=coastline OR admin_level=2 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\%Tile%\stateborder.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\stateborder.osm*""")
                End If

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGeneration, "After")
                ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\output.osm""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine($"PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            Else

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\1-log-osmconvert.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
                ScriptBatchFile.WriteLine($"set Tile={Tile}")
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\1-osmconvert.bat"" >> ""%PathToScriptsFolder%\batchfiles\%Tile%\log.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

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

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\1-osmconvert.bat", False, System.Text.Encoding.ASCII)
                WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
                ScriptBatchFile.WriteLine($"set overpassBorder={borderS.ToString("#0.##", New CultureInfo("en-US"))},{borderW.ToString("##0.##", New CultureInfo("en-US"))},{borderN.ToString("#0.##", New CultureInfo("en-US"))},{borderE.ToString("##0.##", New CultureInfo("en-US"))}")
                If Not ClassWorker.MyTilesSettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine($"set http_proxy={ClassWorker.MyTilesSettings.Proxy}")
                End If

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGeneration, "Before")

                ScriptBatchFile.WriteLine($"if not exist ""%PathToTempOSM%\osm\%Tile%"" mkdir ""%PathToTempOSM%\osm\{Tile & Chr(34)}")

                ScriptBatchFile.WriteLine($": Download_{Tile}")
                ScriptBatchFile.WriteLine($"@echo on")

                Dim overpassURL As String = "http://overpass-api.de"
                If Not ClassWorker.MyWorldSettings.OverpassURL = "" Then
                    overpassURL = ClassWorker.MyWorldSettings.OverpassURL
                End If

                If ClassWorker.MyTilesSettings.warnOnOverpass Then

                    ScriptBatchFile.WriteLine("""%PathToScriptsFolder%\wget.exe"" -O ""%PathToTempOSM%\osm\" & Tile & "\output.osm"" """ & overpassURL & "/api/interpreter?data=[maxsize:2000000000];(node(%overpassBorder%);<;>;);out;""")
                    ScriptBatchFile.WriteLine("@echo off")
                    ScriptBatchFile.WriteLine("FOR /F  "" usebackq "" %%A IN ('" & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\osm\" & Tile & "\output.osm') DO set size=%%~zA")
                    ScriptBatchFile.WriteLine("if %size% lss 500 (")
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

                Else

                    ScriptBatchFile.WriteLine("""%PathToScriptsFolder%\wget.exe"" -O ""%PathToTempOSM%\osm\" & Tile & "\output.osm"" """ & overpassURL & "/api/interpreter?data=[maxsize:2000000000];(node(%overpassBorder%);<;>;);out;""")

                End If

                If ClassWorker.MyWorldSettings.highways Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=motorway OR highway=trunk"" -o=""%PathToTempOSM%\osm\%Tile%\highway.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\highway.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streets Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=primary OR highway=secondary"" -o=""%PathToTempOSM%\osm\%Tile%\big_road.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=tertiary"" -o=""%PathToTempOSM%\osm\%Tile%\middle_road.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\big_road.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\middle_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.small_streets Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""highway=residential"" -o=""%PathToTempOSM%\osm\%Tile%\small_road.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\small_road.osm*""")
                End If

                If ClassWorker.MyWorldSettings.waterBodies = "All" Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""water=lake OR water=reservoir OR natural=water OR landuse=reservoir"" -o=""%PathToTempOSM%\osm\%Tile%\water.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\water.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=wetland"" -o=""%PathToTempOSM%\osm\%Tile%\wetland.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=wetland AND wetland=swamp"" -o=""%PathToTempOSM%\osm\%Tile%\swamp.osm""")

                If ClassWorker.MyWorldSettings.riversBoolean And Not (ClassWorker.MyWorldSettings.rivers = "Major" Or ClassWorker.MyWorldSettings.rivers = "Major + Minor") Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=riverbank OR waterway=canal"" -o=""%PathToTempOSM%\osm\%Tile%\river.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\river.osm*""")
                End If

                If ClassWorker.MyWorldSettings.streams Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""waterway=river OR water=river OR waterway=stream"" -o=""%PathToTempOSM%\osm\%Tile%\stream.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\stream.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=glacier"" -o=""%PathToTempOSM%\osm\%Tile%\glacier.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=volcano AND volcano:status=active"" -o=""%PathToTempOSM%\osm\%Tile%\volcano.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""natural=beach"" -o=""%PathToTempOSM%\osm\%Tile%\beach.osm""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""%PathToTempOSM%\osm\%Tile%\grass.osm""")

                If ClassWorker.MyWorldSettings.forests Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=forest"" -o=""%PathToTempOSM%\osm\%Tile%\forest.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\forest.osm*""")
                End If

                If ClassWorker.MyWorldSettings.farms Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=farmland"" -o=""%PathToTempOSM%\osm\%Tile%\farmland.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=vineyard"" -o=""%PathToTempOSM%\osm\%Tile%\vineyard.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\farmland.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\vineyard.osm*""")
                End If

                If ClassWorker.MyWorldSettings.meadows Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=meadow"" -o=""%PathToTempOSM%\osm\%Tile%\meadow.osm""")
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=grass OR natural=grassland OR natural=fell OR natural=heath OR natural=scrub"" -o=""%PathToTempOSM%\osm\%Tile%\grass.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\meadow.osm*""")
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\grass.osm*""")
                End If

                If ClassWorker.MyWorldSettings.quarrys Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=quarry"" -o=""%PathToTempOSM%\osm\%Tile%\quarry.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\quarry.osm*""")
                End If

                If ClassWorker.MyWorldSettings.aerodrome Then
                    If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 And CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) >= 1024 Then
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""aeroway=aerodrome AND iata="" -o=""%PathToTempOSM%\osm\%Tile%\aerodrome.osm""")

                    Else
                        ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""aeroway=launchpad"" -o=""%PathToTempOSM%\osm\%Tile%\aerodrome.osm""")
                    End If
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\aerodrome.osm*""")
                End If

                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=bare_rock OR natural=scree OR natural=shingle"" -o=""%PathToTempOSM%\osm\%Tile%\bare_rock.osm""")

                If ClassWorker.MyWorldSettings.buildings Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""landuse=commercial OR landuse=construction OR landuse=industrial OR landuse=residential OR landuse=retail"" -o=""%PathToTempOSM%\osm\%Tile%\urban.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\urban.osm*""")
                End If

                If ClassWorker.MyWorldSettings.bordersBoolean = True And ClassWorker.MyWorldSettings.borders = "Current" Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=2"" --drop=""natural=coastline OR admin_level=3 OR admin_level=4 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\%Tile%\border.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\border.osm*""")
                End If

                If ClassWorker.MyWorldSettings.stateBorders = True Then
                    ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\osmfilter.exe"" ""%PathToTempOSM%\osm\%Tile%\output.osm"" --verbose --keep=""boundary=administrative AND admin_level=3 OR boundary=administrative AND admin_level=4"" --drop=""natural=coastline OR admin_level=2 OR admin_level=5 OR admin_level=6 OR admin_level=7 OR admin_level=8 OR admin_level=9 OR admin_level=10 OR admin_level=11"" -o=""%PathToTempOSM%\osm\%Tile%\stateborder.osm""")
                Else
                    ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToQGISProject%\empty.osm"" ""%PathToTempOSM%\osm\%Tile%\stateborder.osm*""")
                End If

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.OsmbatGeneration, "After")
                ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\output.osm""")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine($"PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()


            End If
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '1-osmconvert.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub QgisRepairBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim pythonFile As StreamWriter
            pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_repair.py", False, System.Text.Encoding.ASCII)
            Dim PathReversedSlash = Replace(ClassWorker.MyTilesSettings.PathToTempOSM, "\", "/")
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

            Dim ScriptBatchFile As StreamWriter

            Dim qgisExecutableName As String = ""
            If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-bin.exe") Then
                qgisExecutableName = "qgis-bin.exe"
            ElseIf My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-ltr-bin.exe") Then
                qgisExecutableName = "qgis-ltr-bin.exe"
            Else
                qgisExecutableName = "qgis-bin.exe"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\2-1-log-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\2-1-qgis.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\2-1-qgis.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.QgisRepairGeneration, "Before")

            ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_repair.py""")

            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\urban.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\broadleaved.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\needleleaved.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\mixedforest.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\beach.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\grass.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\farmland.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\meadow.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\quarry.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\water.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\glacier.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\wetland.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\swamp.dbf""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\urban.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\forest.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\beach.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\grass.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\farmland.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\meadow.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\quarry.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\water.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\glacier.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\wetland.osm""")
            ScriptBatchFile.WriteLine($"del ""%PathToTempOSM%\osm\%Tile%\swamp.osm""")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.QgisRepairGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '2-1-qgis.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub QgisBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

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

            If ClassWorker.MyWorldSettings.TerrainSource = "Low Quality Offline Terrain" Then
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
            Dim waterBodies As String = "water_lakes"
            Select Case ClassWorker.MyWorldSettings.rivers
                Case "All"
                    rivers = "water"
                Case "Major"
                    rivers = "water_lakes"
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
            pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}.py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine($"try:" & Environment.NewLine &
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
            vbTab & "waterBodies = '" & waterBodies & "'" & Environment.NewLine &
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

            pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_bathymetry.py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine($"try:" & Environment.NewLine &
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
            vbTab & "waterBodies = '" & waterBodies & "'" & Environment.NewLine &
            vbTab & "TerrainSource = '" & terrainSource & "'" & Environment.NewLine &
            vbTab & "citySize = '" & citySize & "'" & Environment.NewLine &
            vbTab & "borders = '" & ClassWorker.MyWorldSettings.bordersBoolean & "'" & Environment.NewLine &
            vbTab & "borderyear = '" & ClassWorker.MyWorldSettings.borders & "'" & Environment.NewLine &
            vbTab & "vanillaGeneration = '" & ClassWorker.MyWorldSettings.vanillaPopulation & "'" & Environment.NewLine &
            vbTab & "customLayers = " & customLayerString)
            pythonFile.WriteLine(My.Resources.ResourceManager.GetString("basescript_bathymetry"))
            pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
            pythonFile.WriteLine("except:" & Environment.NewLine &
            vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
            vbTab & vbTab & "pass" & Environment.NewLine &
            vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
            pythonFile.Close()

            pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_terrain.py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine($"try:" & Environment.NewLine &
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
            vbTab & "waterBodies = '" & waterBodies & "'" & Environment.NewLine &
            vbTab & "TerrainSource = '" & terrainSource & "'" & Environment.NewLine &
            vbTab & "citySize = '" & citySize & "'" & Environment.NewLine &
            vbTab & "borders = '" & ClassWorker.MyWorldSettings.bordersBoolean & "'" & Environment.NewLine &
            vbTab & "borderyear = '" & ClassWorker.MyWorldSettings.borders & "'" & Environment.NewLine &
            vbTab & "vanillaGeneration = '" & ClassWorker.MyWorldSettings.vanillaPopulation & "'" & Environment.NewLine &
            vbTab & "customLayers = " & customLayerString)
            pythonFile.WriteLine(My.Resources.ResourceManager.GetString("basescript_terrain"))
            pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
            pythonFile.WriteLine("except:" & Environment.NewLine &
            vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
            vbTab & vbTab & "pass" & Environment.NewLine &
            vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
            pythonFile.Close()

            pythonFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_heightmap.py", False, System.Text.Encoding.ASCII)
            pythonFile.WriteLine($"try:" & Environment.NewLine &
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
            vbTab & "waterBodies = '" & waterBodies & "'" & Environment.NewLine &
            vbTab & "TerrainSource = '" & terrainSource & "'" & Environment.NewLine &
            vbTab & "citySize = '" & citySize & "'" & Environment.NewLine &
            vbTab & "borders = '" & ClassWorker.MyWorldSettings.bordersBoolean & "'" & Environment.NewLine &
            vbTab & "borderyear = '" & ClassWorker.MyWorldSettings.borders & "'" & Environment.NewLine &
            vbTab & "vanillaGeneration = '" & ClassWorker.MyWorldSettings.vanillaPopulation & "'" & Environment.NewLine &
            vbTab & "customLayers = " & customLayerString)
            pythonFile.WriteLine(My.Resources.ResourceManager.GetString("basescript_heightmap"))
            pythonFile.WriteLine(vbTab & "os.kill(os.getpid(), 9)")
            pythonFile.WriteLine("except:" & Environment.NewLine &
            vbTab & "with open(os.path.join(os.getenv('APPDATA') + '/', 'EarthTilesPythonError'), 'w') as fp:" & Environment.NewLine &
            vbTab & vbTab & "pass" & Environment.NewLine &
            vbTab & "os.kill(os.getpid(), 9)" & Environment.NewLine)
            pythonFile.Close()

            Dim ScriptBatchFile As StreamWriter

            Dim qgisExecutableName As String = ""
            If My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-bin.exe") Then
                qgisExecutableName = "qgis-bin.exe"
            ElseIf My.Computer.FileSystem.FileExists($"{ClassWorker.MyTilesSettings.PathToQGIS}\bin\qgis-ltr-bin.exe") Then
                qgisExecutableName = "qgis-ltr-bin.exe"
            Else
                qgisExecutableName = "qgis-bin.exe"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\2-2-log-qgis.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\2-2-qgis.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\2-2-qgis.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.QgisGeneration, "Before")

            ScriptBatchFile.WriteLine($"if Not exist ""%PathToScriptsFolder%\image_exports\"" mkdir ""%PathToScriptsFolder%\image_exports\""")
            ScriptBatchFile.WriteLine($"if Not exist ""%PathToScriptsFolder%\image_exports\%Tile%"" mkdir ""%PathToScriptsFolder%\image_exports\%Tile%""")
            ScriptBatchFile.WriteLine($"if Not exist ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap"" mkdir ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap""")

            If Not ClassWorker.MyWorldSettings.generateFullEarth Then
                ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" ""%PathToTempOSM%\osm\%Tile%"" ""%PathToQGISProject%\OsmData"" /Y")
            Else
                ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" ""%PathToTempOSM%\osm\all"" ""%PathToQGISProject%\OsmData"" /Y")
            End If
            'open project and run script

            ScriptBatchFile.WriteLine($"timeout /t 3")
            ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --project ""%PathToQGISProject%\MinecraftEarthTiles.qgz"" --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}.py""")
            ScriptBatchFile.WriteLine($"timeout /t 3")
            ScriptBatchFile.WriteLine($"START taskkill /f /im python.exe")
            If ClassWorker.MyWorldSettings.bathymetry Then
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --project ""%PathToQGISBathymetry%\MinecraftEarthTiles_Bathymetry.qgz"" --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_bathymetry.py""")
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"START taskkill /f /im python.exe")
            End If
            If ClassWorker.MyWorldSettings.TerrainSource = "High Quality Offline Terrain (Addon)" Then
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --project ""%PathToQGISTerrain%\MinecraftEarthTiles_Terrain.qgz"" --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_terrain.py""")
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"START taskkill /f /im python.exe")
            End If
            If ClassWorker.MyWorldSettings.offlineHeightmap Then
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\{qgisExecutableName}"" --noversioncheck --nologo --noplugins --project ""%PathToQGISHeightmap%\MinecraftEarthTiles_Heightmap.qgz"" --code ""{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\{Tile}_heightmap.py""")
                ScriptBatchFile.WriteLine($"timeout /t 3")
                ScriptBatchFile.WriteLine($"START taskkill /f /im python.exe")
            End If
            ScriptBatchFile.WriteLine($"timeout /t 3")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.QgisGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '2-2-qgis.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub TartoolBatchExport(Tile As String)
        Try
            If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) = 1 Then

                CreateFilesAndFolders(Tile)

                Dim ScriptBatchFile As StreamWriter

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\3-log-tartool.bat", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
                ScriptBatchFile.WriteLine($"set Tile={Tile}")
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\3-tartool.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\3-tartool.bat", False, System.Text.Encoding.ASCII)
                WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
                If Not ClassWorker.MyTilesSettings.Proxy = "" Then
                    ScriptBatchFile.WriteLine($"set ftp_proxy={ClassWorker.MyTilesSettings.Proxy}")
                End If

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.TartoolGeneration, "Before")

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
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\wget.exe"" -O ""%PathToScriptsFolder%\image_exports\%Tile%\{NewTile}.tar.gz"" ""ftp://ftp.eorc.jaxa.jp/pub/ALOS/ext1/AW3D30/release_v1903/{TilesRounded}/{TilesOneMoreDigit}.tar.gz""")
                ScriptBatchFile.WriteLine($"""%PathToScriptsFolder%\TarTool.exe"" ""%PathToScriptsFolder%\image_exports\%Tile%\{NewTile}.tar.gz"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap""")

                WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.TartoolGeneration, "After")
                If ClassWorker.MyTilesSettings.cmdPause Then
                    ScriptBatchFile.WriteLine($"PAUSE")
                End If
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()

            End If
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '3-tartool.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub GdalBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\4-log-gdal.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\4-gdal.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\4-gdal.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.GdalGeneration, "Before")

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

            Dim borderW As Double = 0
            Dim borderS As Double = 0
            Dim borderE As Double = 0
            Dim borderN As Double = 0
            If LatiDir = "N" Then
                borderS = LatiNumber - CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 1
                borderN = LatiNumber + 1
            Else
                borderS = (-1 * LatiNumber) - CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) + 1
                borderN = (-1 * LatiNumber)
            End If
            If LongDir = "E" Then
                borderW = LongNumber
                borderE = LongNumber + CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16)
            Else
                borderW = (-1 * LongNumber)
                borderE = (-1 * LongNumber) + CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16)
            End If

            If CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16) > 1 Then

                If ClassWorker.MyWorldSettings.offlineHeightmap Then
                    ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdal_translate.exe"" -a_nodata none -outsize {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.BlocksPerTile} -projwin {borderW} {borderN} {borderE} {borderS} -Of PNG -ot UInt16 -scale -1152 8848 0 65535 ""%PathToQGISHeightmap%\TifFiles\HQheightmap.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png""")
                Else
                    ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdal_translate.exe"" -a_nodata none -outsize {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.BlocksPerTile} -projwin {borderW} {borderN} {borderE} {borderS} -Of PNG -ot UInt16 -scale -1152 8848 0 65535 ""%PathToQGISProject%\TifFiles\heightmap.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png""")
                End If
            Else
                'Backup for ocean Tiles
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdal_translate.exe"" -a_nodata none -outsize {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.BlocksPerTile} -projwin {borderW} {borderN} {borderE} {borderS} -Of PNG -ot UInt16 -scale -1152 8848 0 65535 ""%PathToQGISProject%\TifFiles\heightmap.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png""")
                'JAXA Heightmap, if available
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdal_translate.exe"" -a_nodata none -outsize {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.BlocksPerTile} -Of PNG -ot UInt16 -scale -1152 8848 0 65535 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\{TilesOneMoreDigit}\{TilesOneMoreDigit}_AVE_DSM.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png""")
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdaldem.exe"" slope ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\{TilesOneMoreDigit}\{TilesOneMoreDigit}_AVE_DSM.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\{TilesOneMoreDigit}\{TilesOneMoreDigit}_AVE_DSM_slope.tif"" -s 111120 -compute_edges")
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToQGIS}\bin\gdal_translate.exe"" -a_nodata none -outsize {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.BlocksPerTile} -Of PNG -ot UInt16 -scale 0 90 0 65535 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\{TilesOneMoreDigit}\{TilesOneMoreDigit}_AVE_DSM_slope.tif"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_slope.png""")
            End If

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.GdalGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '4-gdal.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub ImageMagickBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\5-log-magick.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\5-magick.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\5-magick.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.ImageMagickGeneration, "Before")

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

            ScriptBatchFile.WriteLine($"timeout /t 7")

            If ClassWorker.MyWorldSettings.Heightmap_Error_Correction Then
                'prepare water images
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_temp.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_temp.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_temp.png"" -draw ""point 1,1"" -fill black ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_temp.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_temp.png"" -draw ""point 1,1"" -fill black ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_temp.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert -negate ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_temp.png"" -threshold 1%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_mask.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert -negate ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_temp.png"" -threshold 1%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_mask.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_mask.png"" -transparent black ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_mask.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_mask.png"" -transparent black ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_mask.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" composite -gravity center ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_mask.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_river_mask.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_transparent.png""")

                'fill holes in the heightmap (JAXA)
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png"" -transparent black -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_removed_invalid.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_removed_invalid.png"" -channel A -morphology EdgeIn Diamond -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_edges.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_edges.png"" {NumberOfResize} -layers RemoveDups -filter Gaussian -resize %BlocksPerTile%x%BlocksPerTile%! -reverse -background None -flatten -alpha off -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_invalid_filled.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_invalid_filled.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_removed_invalid.png"" -compose over -composite -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_unsmoothed.png""")

                'Fixing rivers in 16 bit heightmap
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_unsmoothed.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_water_transparent.png"" -compose over -composite -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_blacked.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_blacked.png"" -transparent white -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_removed.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_removed.png"" -channel A -morphology EdgeIn Diamond -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_edges.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_edges.png"" ( +clone -channel A -morphology EdgeIn Diamond +channel +write sparse-color:""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%vf.txt"" -sparse-color Voronoi ""@%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%vf.txt""  -alpha off -depth 16 ) -compose DstOver -composite ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_filled.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_filled.png"" -level 0.002%%,100.002%% ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_filled.png""")
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_filled.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_water_removed.png"" -compose over -composite -depth 16 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png""")
            Else
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%_exported.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png""")
            End If
            ''ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png"" -blur 5 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png""")
            ''ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png"" -blur 5 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png""")
            ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png"" -morphology Convolve Gaussian:0x2 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png""")
            ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png"" -morphology Convolve Gaussian:0x2 ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png""")

            Dim scale As Double = Math.Round(36768000 / ((CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int16) * (360 / CType(ClassWorker.MyWorldSettings.TilesPerMap, Int16)))), 1)

            If scale < 25 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png"" -sample 3.125%% -magnify -magnify -magnify -magnify -magnify -define png:color-type=6 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png""")
            ElseIf scale < 50 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png"" -sample 6.25%% -magnify -magnify -magnify -magnify -define png:color-type=6 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png""")
            ElseIf scale < 100 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png"" -sample 12.5%% -magnify -magnify -magnify -define png:color-type=6 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png""")
            ElseIf scale < 200 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png"" -sample 25%% -magnify -magnify -define png:color-type=6 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png""")
            ElseIf scale < 500 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png"" -sample 50%% -magnify -define png:color-type=6 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_climate.png""")
            End If

            If scale < 50 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 1.5625%% -magnify -magnify -magnify -magnify -magnify -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            ElseIf scale < 100 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 3.125%% -magnify -magnify -magnify -magnify -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            ElseIf scale < 200 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 6.25%% -magnify -magnify -magnify -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            ElseIf scale < 500 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 12.5%% -magnify -magnify -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            ElseIf scale < 1000 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 25%% -magnify -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            ElseIf scale < 2000 Then
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png"" -sample 50%% -magnify ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_ocean_temp.png""")
            End If

            If Not ClassWorker.MyWorldSettings.terrainModifier = 0 Then
                Select Case ClassWorker.MyWorldSettings.terrainModifier
                    Case -2
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain.png"" -channel R -level -15%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png"" -channel G -level -5%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                    Case -1
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain.png"" -channel R -level -5%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png"" -channel G -level -2%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                    Case 1
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain.png"" -channel G -level -2%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png"" -channel R -level -0%%,100%%,0.9 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                    Case 2
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain.png"" -channel G -level -5%%,100%% ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                        ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png"" -channel R -level 0%%,100%%,0.8 ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png""")
                End Select
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_modified.png"" -dither None -remap ""%PathToScriptsFolder%\wpscript\terrain\{ClassWorker.MyWorldSettings.Terrain}.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_reduced_colors.png""")
            Else
                ScriptBatchFile.WriteLine($"""%PathToMagick%"" convert ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain.png"" -dither None -remap ""%PathToScriptsFolder%\wpscript\terrain\{ClassWorker.MyWorldSettings.Terrain}.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%_terrain_reduced_colors.png""")
            End If

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.ImageMagickGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '5-magick.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub WpScriptBatchExport(Tile As String, PauseForDebugging As Boolean)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            If PauseForDebugging Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-log-wpscript-debug.bat", False, System.Text.Encoding.ASCII)
            Else
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-log-wpscript.bat", False, System.Text.Encoding.ASCII)
            End If
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            If PauseForDebugging Then
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\6-wpscript-debug.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            Else
                ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\6-wpscript.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            If PauseForDebugging Then
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-wpscript-debug.bat", False, System.Text.Encoding.ASCII)

            Else
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-wpscript.bat", False, System.Text.Encoding.ASCII)
            End If
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.WpScriptGeneration, "Before")

            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\backups"" mkdir ""%PathToScriptsFolder%\wpscript\backups""")
            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\exports"" mkdir ""%PathToScriptsFolder%\wpscript\exports""")
            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\worldpainter_files"" mkdir ""%PathToScriptsFolder%\wpscript\worldpainter_files""")

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
                Case "1.20"
                    MapVersionShort = "1-20"
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
                ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\{NewTile}.png*""")
                ScriptBatchFile.WriteLine($"""C:\Windows\system32\xcopy.exe"" /y ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\%Tile%.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\heightmap\{NewTile}.png*""")
            End If
            ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToWorldPainterFolder}"" ""%PathToScriptsFolder%\wpscript.js"" ""{ClassWorker.MyTilesSettings.PathToScriptsFolder.Replace("\", "/")}/"" {ReplacedString} {ClassWorker.MyWorldSettings.BlocksPerTile} {ClassWorker.MyWorldSettings.TilesPerMap} {ClassWorker.MyWorldSettings.VerticalScale} {ClassWorker.MyWorldSettings.bordersBoolean} {ClassWorker.MyWorldSettings.stateBorders} {ClassWorker.MyWorldSettings.highways} {ClassWorker.MyWorldSettings.streets} {ClassWorker.MyWorldSettings.small_streets} {ClassWorker.MyWorldSettings.buildings} {ClassWorker.MyWorldSettings.ores} {ClassWorker.MyWorldSettings.netherite} {ClassWorker.MyWorldSettings.farms} {ClassWorker.MyWorldSettings.meadows} {ClassWorker.MyWorldSettings.quarrys} {ClassWorker.MyWorldSettings.aerodrome} {ClassWorker.MyWorldSettings.mobSpawner} {ClassWorker.MyWorldSettings.animalSpawner} {ClassWorker.MyWorldSettings.riversBoolean} {ClassWorker.MyWorldSettings.streams} {ClassWorker.MyWorldSettings.volcanos} {ClassWorker.MyWorldSettings.shrubs} {ClassWorker.MyWorldSettings.crops} {MapVersionShort} {ClassWorker.MyWorldSettings.mapOffset} ""{ClassWorker.MyWorldSettings.lowerBuildLimit}"" ""{ClassWorker.MyWorldSettings.upperBuildLimit}"" {ClassWorker.MyWorldSettings.vanillaPopulation} {NewTile} {NewBiomeSource} {ClassWorker.MyWorldSettings.oreModifier} {ClassWorker.MyWorldSettings.mod_BOP} {ClassWorker.MyWorldSettings.mod_BYG} {ClassWorker.MyWorldSettings.mod_Terralith} {ClassWorker.MyWorldSettings.mod_WilliamWythers} {ClassWorker.MyWorldSettings.mod_Create}")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.WpScriptGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Or PauseForDebugging Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File 6-wpscript.bat could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub VoidScriptBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-log-voidscript.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\6-voidscript.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-voidscript.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.VoidScriptGeneration, "Before")

            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\image_exports"" mkdir ""%PathToScriptsFolder%\image_exports""")
            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\image_exports\%Tile%"" mkdir ""%PathToScriptsFolder%\image_exports\%Tile%""")
            ScriptBatchFile.WriteLine($"copy ""%PathToScriptsFolder%\wpscript\void.png"" ""%PathToScriptsFolder%\image_exports\%Tile%\%Tile%.png""")


            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\backups"" mkdir ""%PathToScriptsFolder%\wpscript\backups""")
            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\exports"" mkdir ""%PathToScriptsFolder%\wpscript\exports""")
            ScriptBatchFile.WriteLine($"if not exist ""%PathToScriptsFolder%\wpscript\worldpainter_files"" mkdir ""%PathToScriptsFolder%\wpscript\worldpainter_files""")

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

            ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToWorldPainterFolder}"" ""%PathToScriptsFolder%\voidscript.js"" ""{ClassWorker.MyTilesSettings.PathToScriptsFolder.Replace("\", "/")}/"" ""x{latitude}"" ""x{longitude}"" ""{MapVersionShort}"" ""x%Tile%"" ""{ClassWorker.MyWorldSettings.VerticalScale}"" ""{ClassWorker.MyWorldSettings.lowerBuildLimit}"" ""{ClassWorker.MyWorldSettings.upperBuildLimit}""")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.VoidScriptGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '6-voidscript.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub MinutorRenderExort(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-1-log-minutors.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\6-1-minutors.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            Dim NewTile As String = ""
            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            Dim depth As String = "255"
            If ClassWorker.MyTilesSettings.version = "1.18" Or ClassWorker.MyWorldSettings.MapVersion = "1.19" Or ClassWorker.MyTilesSettings.version = "1.20" Then
                depth = "319"
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\6-1-minutors.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.MinutorGeneration, "Before")

            ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToMinutor}"" --world ""%PathToScriptsFolder%\wpscript\exports\{NewTile}"" --depth {depth} --savepng ""%PathToScriptsFolder%\render\%Tile%.png""")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.MinutorGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '6-1-minutors.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub CleanupBatchExport(Tile As String)
        Try

            CreateFilesAndFolders(Tile)

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"set Tile={Tile}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\%Tile%\8-cleanup.bat"" >> ""%PathToScriptsFolder%\logs\log-%Tile%.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\8-cleanup.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, Tile)
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CleanupGeneration, "Before")

            If ClassWorker.MyTilesSettings.keepImageFiles = False And (Not ClassWorker.MySelection.SpawnTile = Tile) Then
                ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\image_exports\%Tile%""")
            End If

            If ClassWorker.MyTilesSettings.keepOsmFiles = False Then
                ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\osm\{Tile & Chr(34)}")
            End If

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CleanupGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '8-cleanup.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub CombineBatchExport()
        Try

            CreateFilesAndFolders()

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-log-combine.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\7-combine.bat"" >> ""%PathToScriptsFolder%\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-combine.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, "Combine")
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CombineGeneration, "Before")

            Dim exportPath As String = ""
            If ClassWorker.MyTilesSettings.PathToExport = "" Then
                exportPath = ClassWorker.MyTilesSettings.PathToScriptsFolder
            Else
                exportPath = ClassWorker.MyTilesSettings.PathToExport
            End If

            ScriptBatchFile.WriteLine($"If Not exist ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}"" mkdir ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}""")
            ScriptBatchFile.WriteLine($"If Not exist ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}\region"" mkdir ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}\region""")
            ScriptBatchFile.WriteLine($"copy ""%PathToScriptsFolder%\wpscript\exports\{ClassWorker.MyWorldSettings.WorldName}\level.dat"" ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}""")

            If Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\wpscript\exports\{ClassWorker.MyWorldSettings.WorldName}\datapacks\") Then
                ScriptBatchFile.WriteLine($"If Not exist ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}\datapacks"" mkdir ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}\datapacks""")
                ScriptBatchFile.WriteLine($"copy ""%PathToScriptsFolder%\wpscript\exports\{ClassWorker.MyWorldSettings.WorldName}\datapacks\worldpainter.zip"" ""{exportPath}\{ClassWorker.MyWorldSettings.WorldName}\datapacks""")
            End If

            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\settings.xml"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\selection.xml"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("copy """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\" & ClassWorker.MyWorldSettings.WorldName & "\session.lock"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & """")
            ScriptBatchFile.WriteLine("pushd """ & ClassWorker.MyTilesSettings.PathToScriptsFolder & "\wpscript\exports\""")
            ScriptBatchFile.WriteLine("For /r %%i in (*.mca) do  ""C:\Windows\system32\xcopy.exe"" /Y ""%%i"" """ & exportPath & "\" & ClassWorker.MyWorldSettings.WorldName & "\region\""")
            ScriptBatchFile.WriteLine("popd")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CombineGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '7-combine.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub MinutorFinalRenderExort(Tile As String)
        Try

            CreateFilesAndFolders()

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-1-log-minutors.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\batchfiles\7-1-minutors.bat"" >> ""%PathToScriptsFolder%\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            Dim NewTile As String = ""
            If Tile = ClassWorker.MySelection.SpawnTile Then
                NewTile = ClassWorker.MyWorldSettings.WorldName
            Else
                NewTile = Tile
            End If

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\7-1-minutors.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, "Minutor")
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.MinutorFinalGeneration, "Before")

            If Not ClassWorker.MyTilesSettings.PathToMinutor = "" And ClassWorker.MyTilesSettings.minutor = True Then
                Dim depth As String = "255"
                If ClassWorker.MyTilesSettings.version = "1.18" Or ClassWorker.MyWorldSettings.MapVersion = "1.19" Then
                    depth = "319"
                End If
                ScriptBatchFile.WriteLine($"""{ClassWorker.MyTilesSettings.PathToMinutor}"" --world ""{ClassWorker.MyTilesSettings.PathToExport}\{ClassWorker.MyWorldSettings.WorldName}"" --depth {depth} --savepng ""{ClassWorker.MyTilesSettings.PathToExport}\{ClassWorker.MyWorldSettings.WorldName}\{ClassWorker.MyWorldSettings.WorldName}.png""")
            End If

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.MinutorFinalGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '7-1-minutors.bat' could not be saved. {ex.Message}")
        End Try
    End Sub

    Public Sub CleanupFinalBatchExport()
        Try

            CreateFilesAndFolders()

            Dim ScriptBatchFile As StreamWriter

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\8-log-cleanup.bat", False, System.Text.Encoding.ASCII)
            ScriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
            ScriptBatchFile.WriteLine($"CALL ""%PathToScriptsFolder%\8-cleanup.bat"" >> ""%PathToScriptsFolder%\logs\log-general.txt""")
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()

            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\8-cleanup.bat", False, System.Text.Encoding.ASCII)
            WriteTilesVariablesToBatch(ScriptBatchFile, "Cleanup")
            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CleanupFinalGeneration, "Before")

            ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\wpscript\backups""")
            ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\wpscript\backups""")
            If ClassWorker.MyTilesSettings.keepWorldPainterFiles = False Then
                ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\wpscript\worldpainter_files""")
                ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\wpscript\worldpainter_files""")
            End If
            ScriptBatchFile.WriteLine($"del ""%PathToScriptsFolder%\osm\unfiltered.o5m""")
            If ClassWorker.MyTilesSettings.keepPbfFile = False Then
                ScriptBatchFile.WriteLine($"del ""%PathToScriptsFolder%\osm\output.o5m""")
            End If
            If ClassWorker.MyTilesSettings.keepImageFiles = False Then
                ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\image_exports""")
                ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\image_exports""")
            End If
            ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\wpscript\exports""")
            ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\wpscript\exports""")
            ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\python""")
            ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\python""")
            ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToQGISProject%\OsmData""")
            ScriptBatchFile.WriteLine($"mkdir ""%PathToQGISProject%\OsmData""")
            ScriptBatchFile.WriteLine($"rmdir /Q /S ""%PathToScriptsFolder%\batchfiles""")
            ScriptBatchFile.WriteLine($"mkdir ""%PathToScriptsFolder%\batchfiles""")

            WriteCustomTextToBatch(ScriptBatchFile, BatchFileName.CleanupFinalGeneration, "After")
            If ClassWorker.MyTilesSettings.cmdPause Then
                ScriptBatchFile.WriteLine($"PAUSE")
            End If
            ScriptBatchFile.WriteLine()
            ScriptBatchFile.Close()
        Catch ex As Exception
            WriteLog(ex.Message)
            Throw New Exception($"File '8-cleanup.bat' could not be saved. {ex.Message}")
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

    Private Sub WriteTilesVariablesToBatch(scriptBatchFile As StreamWriter, Tile As String)

        Dim NewTile As String = ""
        If Tile = ClassWorker.MySelection.SpawnTile Then
            NewTile = ClassWorker.MyWorldSettings.WorldName
        Else
            NewTile = Tile
        End If

        scriptBatchFile.WriteLine()
        scriptBatchFile.WriteLine($"set Tile={Tile}")
        scriptBatchFile.WriteLine($"set WorldPainterExportName={NewTile}")
        scriptBatchFile.WriteLine($"set PathToScriptsFolder={ClassWorker.MyTilesSettings.PathToScriptsFolder}")
        scriptBatchFile.WriteLine($"set PathToWorldPainterFolder={ClassWorker.MyTilesSettings.PathToWorldPainterFolder}")
        scriptBatchFile.WriteLine($"set PathToTempOSM={ClassWorker.MyTilesSettings.PathToTempOSM}")
        scriptBatchFile.WriteLine($"set PathToQGIS={ClassWorker.MyTilesSettings.PathToQGIS}")
        scriptBatchFile.WriteLine($"set PathToQGISProject={ClassWorker.MyTilesSettings.PathToQGISProject}")
        scriptBatchFile.WriteLine($"set PathToQGISBathymetry={ClassWorker.MyTilesSettings.PathToQGISProjectBathymetryAddon}")
        scriptBatchFile.WriteLine($"set PathToQGISTerrain={ClassWorker.MyTilesSettings.PathToQGISProjectTerrainAddon}")
        scriptBatchFile.WriteLine($"set PathToQGISHeightmap={ClassWorker.MyTilesSettings.PathToQGISProjectHeightmapAddon}")
        scriptBatchFile.WriteLine($"set PathToMagick={ClassWorker.MyTilesSettings.PathToMagick}")
        scriptBatchFile.WriteLine($"set PathToMinutor={ClassWorker.MyTilesSettings.PathToMinutor}")
        scriptBatchFile.WriteLine($"set PathToExport={ClassWorker.MyTilesSettings.PathToExport}")
        scriptBatchFile.WriteLine($"set Proxy={ClassWorker.MyTilesSettings.Proxy}")
        scriptBatchFile.WriteLine($"set PathToPBF={ClassWorker.MyWorldSettings.PathToPBF}")
        scriptBatchFile.WriteLine($"set OverpassURL={ClassWorker.MyWorldSettings.OverpassURL}")
        scriptBatchFile.WriteLine($"set WorldName={ClassWorker.MyWorldSettings.WorldName}")
        scriptBatchFile.WriteLine($"set BlocksPerTile={ClassWorker.MyWorldSettings.BlocksPerTile}")
        scriptBatchFile.WriteLine($"set TilesPerMap={ClassWorker.MyWorldSettings.TilesPerMap}")
        scriptBatchFile.WriteLine($"set VerticalScale={ClassWorker.MyWorldSettings.VerticalScale}")
        scriptBatchFile.WriteLine($"set Terrain={ClassWorker.MyWorldSettings.Terrain}")
        scriptBatchFile.WriteLine($"set Heightmap_Error_Correction={ClassWorker.MyWorldSettings.Heightmap_Error_Correction}")
        scriptBatchFile.WriteLine($"set bordersBoolean={ClassWorker.MyWorldSettings.bordersBoolean}")
        scriptBatchFile.WriteLine($"set borders={ClassWorker.MyWorldSettings.borders}")
        scriptBatchFile.WriteLine($"set geofabrik={ClassWorker.MyWorldSettings.geofabrik}")
        scriptBatchFile.WriteLine($"set bathymetry={ClassWorker.MyWorldSettings.bathymetry}")
        scriptBatchFile.WriteLine($"set TerrainSource={ClassWorker.MyWorldSettings.TerrainSource}")
        scriptBatchFile.WriteLine($"set biomeSource={ClassWorker.MyWorldSettings.biomeSource}")
        scriptBatchFile.WriteLine($"set highways={ClassWorker.MyWorldSettings.highways}")
        scriptBatchFile.WriteLine($"set streets={ClassWorker.MyWorldSettings.streets}")
        scriptBatchFile.WriteLine($"set small_streets={ClassWorker.MyWorldSettings.small_streets}")
        scriptBatchFile.WriteLine($"set buildings={ClassWorker.MyWorldSettings.buildings}")
        scriptBatchFile.WriteLine($"set ores={ClassWorker.MyWorldSettings.ores}")
        scriptBatchFile.WriteLine($"set netherite={ClassWorker.MyWorldSettings.netherite}")
        scriptBatchFile.WriteLine($"set farms={ClassWorker.MyWorldSettings.farms}")
        scriptBatchFile.WriteLine($"set meadows={ClassWorker.MyWorldSettings.meadows}")
        scriptBatchFile.WriteLine($"set quarrys={ClassWorker.MyWorldSettings.quarrys}")
        scriptBatchFile.WriteLine($"set forests={ClassWorker.MyWorldSettings.forests}")
        scriptBatchFile.WriteLine($"set aerodrome={ClassWorker.MyWorldSettings.aerodrome}")
        scriptBatchFile.WriteLine($"set mobSpawner={ClassWorker.MyWorldSettings.mobSpawner}")
        scriptBatchFile.WriteLine($"set animalSpawner={ClassWorker.MyWorldSettings.animalSpawner}")
        scriptBatchFile.WriteLine($"set riversBoolean={ClassWorker.MyWorldSettings.riversBoolean}")
        scriptBatchFile.WriteLine($"set rivers={ClassWorker.MyWorldSettings.rivers}")
        scriptBatchFile.WriteLine($"set streams={ClassWorker.MyWorldSettings.streams}")
        scriptBatchFile.WriteLine($"set volcanos={ClassWorker.MyWorldSettings.volcanos}")
        scriptBatchFile.WriteLine($"set shrubs={ClassWorker.MyWorldSettings.shrubs}")
        scriptBatchFile.WriteLine($"set crops={ClassWorker.MyWorldSettings.crops}")
        scriptBatchFile.WriteLine($"set MapVersion={ClassWorker.MyWorldSettings.MapVersion}")
        scriptBatchFile.WriteLine($"set mapOffset={ClassWorker.MyWorldSettings.mapOffset}")
        scriptBatchFile.WriteLine($"set vanillaPopulation={ClassWorker.MyWorldSettings.vanillaPopulation}")
        scriptBatchFile.WriteLine($"set mod_BOP={ClassWorker.MyWorldSettings.mod_BOP}")
        scriptBatchFile.WriteLine($"set mod_BYG={ClassWorker.MyWorldSettings.mod_BYG}")
        scriptBatchFile.WriteLine($"set mod_Terralith={ClassWorker.MyWorldSettings.mod_Terralith}")
        scriptBatchFile.WriteLine($"set mod_williamWythers={ClassWorker.MyWorldSettings.mod_WilliamWythers}")
        scriptBatchFile.WriteLine($"set mod_Create={ClassWorker.MyWorldSettings.mod_Create}")
        scriptBatchFile.WriteLine($"set custom_layers={ClassWorker.MyWorldSettings.custom_layers}")
        scriptBatchFile.WriteLine()
    End Sub

    Private Sub WriteCustomTextToBatch(scriptBatchFile As StreamWriter, BatchFileName As BatchFileName, Position As String)
        If File.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\{Position}{BatchFileName.ToString()}.txt") Then
            scriptBatchFile.WriteLine()
            Dim fileReader As System.IO.StreamReader = System.IO.File.OpenText($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\{Position}{BatchFileName.ToString()}.txt")
            Dim sInputLine As String = fileReader.ReadLine()
            Do Until sInputLine Is Nothing
                scriptBatchFile.WriteLine(sInputLine)
                sInputLine = fileReader.ReadLine()
            Loop
            fileReader.Close()
            scriptBatchFile.WriteLine()
        End If
    End Sub

    Private Sub CreateFilesAndFolders(Optional Tile As String = "")

        If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\") Then
            Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\")
        End If

        If Not Tile = "" Then
            If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\") Then
                Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\batchfiles\{Tile}\")
            End If
        End If

        If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\render\") Then
            Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\render\")
        End If

        If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\") Then
            Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\python\")
        End If

        If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\") Then
            Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\")
        End If
        If Not Directory.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\") Then
            Directory.CreateDirectory($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\")
        End If

        Dim custombatchFiles As New List(Of String) From {
"BeforeOsmbatGenerationPrepare",
"AfterOsmbatGenerationPrepare",
"BeforeOsmbatGeneration",
"AfterOsmbatGeneration",
"BeforeQgisRepairGeneration",
"AfterQgisRepairGeneration",
"BeforeQgisGeneration",
"AfterQgisGeneration",
"BeforeTartoolGeneration",
"AfterTartoolGeneration",
"BeforeGdalGeneration",
"AfterGdalGeneration",
"BeforeImageMagickGeneration",
"AfterImageMagickGeneration",
"BeforeWpScripütGeneration",
"AfterWpScripütGeneration",
"BeforeVoidScriptGeneration",
"AfterVoidScriptGeneration",
"BeforeMinutorGeneration",
"AfterMinutorGeneration",
"BeforeCleanupGeneration",
"AfterCleanupGeneration",
"BeforeCombineGeneration",
"AfterCombineGeneration",
"BeforeMinutorFinalGeneration",
"AfterMinutorFinalGeneration",
"BeforeCleanupFinalGeneration",
"AfterCleanupFinalGeneration"
}
        For Each custombatchFile In custombatchFiles
            If Not File.Exists($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\{custombatchFile}.txt") Then
                Dim ScriptBatchFile As StreamWriter
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter($"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\custombatchfiles\{custombatchFile}.txt", False, System.Text.Encoding.ASCII)
                ScriptBatchFile.WriteLine("REM In this part you can manipulate the batch scripts by using the files in the ""custombatchfiles"" folder.")
                ScriptBatchFile.WriteLine("REM You can use all variables from the settings. Take a look at the generated batch files to see all names for the variables.")
                ScriptBatchFile.WriteLine()
                ScriptBatchFile.Close()
            End If
        Next

    End Sub

    Private Function CalcScale() As Double
        Return 36768000 / (CType(ClassWorker.MyWorldSettings.BlocksPerTile, Int32) * (360 / CType(ClassWorker.MyWorldSettings.TilesPerMap, Int32)))
    End Function

    Public Function WriteLog(Message As String, Optional Tile As String = "") As Boolean
        Dim path As String = ""
        If Tile = "" Then
            path = $"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\log-general.txt"
        Else
            path = $"{ClassWorker.MyTilesSettings.PathToScriptsFolder}\logs\log-{Tile}.txt"
        End If
        Try
            Using sw As StreamWriter = System.IO.File.AppendText(path)
                sw.WriteLine(Message)
            End Using
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class

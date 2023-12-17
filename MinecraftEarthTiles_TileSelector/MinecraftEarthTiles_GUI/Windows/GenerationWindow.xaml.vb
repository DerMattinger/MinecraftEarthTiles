Imports System.Globalization
Imports System.IO
Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.IO.Compression
Imports System.Windows.Forms
Imports MinecraftEarthTiles_Core
Imports System.Collections.ObjectModel
Imports System.ComponentModel

Public Class GenerationWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Public preview_Windows As PreviewWindow = Nothing

    Private MyGenerationWorker As GenerationWorker

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitializeComponent()
        MyGenerationWorker = New GenerationWorker
        MyGenerationWorker.keepRunning = False
        AddHandler MyGenerationWorker.PropertyChanged, New PropertyChangedEventHandler(AddressOf PropertyChanged)
        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        If Not ClassWorker.GetTilesSettings.PathToMinutor = "" Then
            Try
                For Each deleteFile In Directory.GetFiles("render", "*.*", SearchOption.TopDirectoryOnly)
                    File.Delete(deleteFile)
                Next
            Catch ex As Exception
            End Try
            If 36768000 / (CType(ClassWorker.GetWorldSettings.BlocksPerTile, Int16) * (360 / CType(ClassWorker.GetWorldSettings.TilesPerMap, Int16))) >= 450 Then
                btn_preview.IsEnabled = True
            End If
        End If
        ApplyTheme()
    End Sub

    Private Sub ApplyTheme()
        Select Case ClassWorker.GetTilesSettings.Theme
            Case "Dark"
                Resources.MergedDictionaries.Clear()
                Dim localThem As New BundledTheme
                localThem.BaseTheme = BaseTheme.Dark
                localThem.PrimaryColor = PrimaryColor.Grey
                localThem.SecondaryColor = SecondaryColor.Amber
                Resources.MergedDictionaries.Add(localThem)
                Dim customButtons As New ResourceDictionary
                customButtons.Source = New Uri("CustomButtons.xaml", UriKind.Relative)
                Resources.MergedDictionaries.Add(customButtons)
                DwmSetWindowAttribute(New System.Windows.Interop.WindowInteropHelper(Me).Handle, 20, True, Runtime.InteropServices.Marshal.SizeOf(True))
            Case "Light"
                Resources.MergedDictionaries.Clear()
                Dim localThem As New BundledTheme
                localThem.BaseTheme = BaseTheme.Light
                localThem.PrimaryColor = PrimaryColor.Grey
                localThem.SecondaryColor = SecondaryColor.Amber
                Resources.MergedDictionaries.Add(localThem)
                Dim customButtons As New ResourceDictionary
                customButtons.Source = New Uri("CustomButtons.xaml", UriKind.Relative)
                Resources.MergedDictionaries.Add(customButtons)
                DwmSetWindowAttribute(New System.Windows.Interop.WindowInteropHelper(Me).Handle, 20, False, Runtime.InteropServices.Marshal.SizeOf(True))
        End Select
    End Sub

    Private Sub window_closing()
        If preview_Windows IsNot Nothing Then
            preview_Windows.Close()
        End If
    End Sub

    Public Sub CalculateDuration()
        If MyGenerationWorker.tilesReady > 0 Then
            Dim percentReady As Double = Math.Round((MyGenerationWorker.tilesReady / MyGenerationWorker.maxTiles) * 100, 2)
            Title = "Tile Generation - " & percentReady & "%"
            lbl_Estimated_Duration.Content = "Time left: " & MyGenerationWorker.hoursLeft & " h and " & MyGenerationWorker.minutesLeft & " min"
            lbl_elapsed_time.Content = "Running: " & MyGenerationWorker.hoursDone & " h and " & MyGenerationWorker.minutesDone & " min"
        End If
    End Sub

    Private Sub PropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs)
        Select Case e.PropertyName
            Case "LatestTile"
                Dispatcher.Invoke(Sub()
                                      If ClassWorker.GetTilesSettings.autoScroll = True Then
                                          Dim items As List(Of Generation) = MyGenerationWorker.MyGeneration
                                          For Each item In items
                                              If item.TileName = MyGenerationWorker.LatestTile Then
                                                  dgr_Tiles.SelectedItem = item
                                                  dgr_Tiles.UpdateLayout()
                                                  dgr_Tiles.ScrollIntoView(dgr_Tiles.SelectedItem)
                                              End If
                                          Next
                                      End If
                                  End Sub)
            Case "LatestMessage"
                Dispatcher.Invoke(Sub()
                                      CalculateDuration()
                                  End Sub)
            Case "RefreshPreview"
                Dispatcher.Invoke(Sub()
                                      If preview_Windows IsNot Nothing Then
                                          preview_Windows.refresh(MyGenerationWorker.RefreshPreview)
                                      End If
                                  End Sub)
            Case "GenerationComplete"
                If MyGenerationWorker.GenerationComplete = True Then
                    If ClassWorker.GetTilesSettings.closeAfterFinish Then
                        End
                    Else
                        Dispatcher.Invoke(Sub()
                                              Dim MessageBox As New MessageBoxWindow("Generation for '" & ClassWorker.GetWorldSettings.WorldName & "' completed")
                                              MessageBox.ShowDialog()
                                              Close()
                                          End Sub)
                    End If
                End If
        End Select
    End Sub

#Region "Menu"

    Private Sub preview_Click(sender As Object, e As RoutedEventArgs)
        preview_Windows = New PreviewWindow()
        preview_Windows.Show()
    End Sub

    Private Sub DebugZip_Click(sender As Object, e As RoutedEventArgs)

        Dim SaveSettingsFileDialog As New SaveFileDialog With {
            .FileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") & "_Debug.zip",
            .Filter = "ZIP Files (.zip)|*.zip|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If SaveSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                ClassWorker.CreateDebugZip(SaveSettingsFileDialog.FileName)
                Dim MessageBox As New MessageBoxWindow("Debug Zip saved.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub BatchFiles_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("BatchFiles_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.CleanupFinalBatchExport()
        MyGenerationWorker.OsmbatExportPrepare()
        For Each Tile In ClassWorker.GetSelection.TilesList
            If Tile.Contains("x") Then
                MyGenerationWorker.VoidScriptBatchExport(Tile)
            Else
                MyGenerationWorker.OsmbatBatchExport(Tile)
                MyGenerationWorker.QgisRepairBatchExport(Tile)
                MyGenerationWorker.QgisBatchExport(Tile)
                MyGenerationWorker.TartoolBatchExport(Tile)
                MyGenerationWorker.GdalBatchExport(Tile)
                MyGenerationWorker.ImageMagickBatchExport(Tile)
                MyGenerationWorker.WpScriptBatchExport(Tile, False)
                MyGenerationWorker.WpScriptBatchExport(Tile, True)
                MyGenerationWorker.MinutorRenderExort(Tile)
                MyGenerationWorker.CleanupBatchExport(Tile)
            End If
        Next
        MyGenerationWorker.CombineBatchExport()
    End Sub

    Private Sub OsmOnly_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("OsmOnly_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        For Each Tile In MyGenerationWorker.MyGeneration
            Tile.GenerationProgress = 0
            Tile.Comment = ""
        Next
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.OsmOnly_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub ImagesOnly_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("ImagesOnly_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.ImagesOnly_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub WorldPainterOnly_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("WorldPainterOnly_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.WorldPainterOnly_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub CombineOnly_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("CombineOnly_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.CombineOnly_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub CleanupOnly_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("CleanupOnly_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.CleanupOnly_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earthtiles.motfe.net/")
    End Sub

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("Close_Click")

        MyGenerationWorker.keepRunning = False
        Close()
    End Sub

#End Region

    Private Sub Start_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("Start_Click")

        MyGenerationWorker.CreateGeneration()
        dgr_Tiles.ItemsSource = MyGenerationWorker.MyGeneration
        MyGenerationWorker.tilesReady = 0
        MyGenerationWorker.startTime = DateTime.Now
        MyGenerationWorker.keepRunning = True
        btn_Start.IsEnabled = False
        mnu_Start.IsEnabled = False
        btn_Stop.IsEnabled = True
        MyGenerationWorker.neuerthread = New Thread(AddressOf MyGenerationWorker.Tile_Generation)
        MyGenerationWorker.neuerthread.Start()
    End Sub

    Private Sub Stop_Click(sender As Object, e As RoutedEventArgs)

        MyGenerationWorker.WriteLog("Stop_Click")

        MyGenerationWorker.keepRunning = False
        Try
            MyGenerationWorker.cts.Cancel()
        Catch ex As Exception
            MyGenerationWorker.WriteLog(ex.Message)
            MsgBox(ex.Message)
        End Try
        For Each SingleProcess In MyGenerationWorker.processList
            SingleProcess.Kill()
        Next
        Try
            If Not MyGenerationWorker.neuerthread Is Nothing Then
                MyGenerationWorker.neuerthread.Abort()
            End If
        Catch ex As Exception
            MyGenerationWorker.WriteLog(ex.Message)
            MsgBox(ex.Message)
        End Try

        If ClassWorker.GetTilesSettings.processKilling Then
            For Each singleProcess In Process.GetProcesses()
                If singleProcess.ProcessName = "wget" Or singleProcess.ProcessName = "osmfilter" Or singleProcess.ProcessName = "osmconvert" Or singleProcess.ProcessName = "qgis-bin" Or singleProcess.ProcessName = "magick" Or singleProcess.ProcessName = "wpscript" Then
                    Try
                        MyGenerationWorker.WriteLog($"{singleProcess.ProcessName}.exe killed")
                        singleProcess.Kill()
                    Catch ex As Exception
                        MyGenerationWorker.WriteLog(ex.Message)
                        MsgBox(ex.Message)
                    End Try
                End If
            Next
        End If

        Close()
    End Sub

    Private Sub FormClosing(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        If MyGenerationWorker.keepRunning = True Then
            Dim result As DialogResult = MessageBox.Show("There is a generation running. Do you really want to close this window and stop the generation?", "Close Generation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = Forms.DialogResult.Yes Or result = Forms.DialogResult.Cancel Then
                e.Cancel = False
                MyGenerationWorker.WriteLog("Stop_Click")

                MyGenerationWorker.keepRunning = False
                Try
                    MyGenerationWorker.cts.Cancel()
                Catch ex As Exception
                    MyGenerationWorker.WriteLog(ex.Message)
                    MsgBox(ex.Message)
                End Try
                For Each SingleProcess In MyGenerationWorker.processList
                    SingleProcess.Kill()
                Next
                Try
                    If Not MyGenerationWorker.neuerthread Is Nothing Then
                        MyGenerationWorker.neuerthread.Abort()
                    End If
                Catch ex As Exception
                    MyGenerationWorker.WriteLog(ex.Message)
                    MsgBox(ex.Message)
                End Try

                If ClassWorker.GetTilesSettings.processKilling Then
                    For Each singleProcess In Process.GetProcesses()
                        If singleProcess.ProcessName = "wget" Or singleProcess.ProcessName = "osmfilter" Or singleProcess.ProcessName = "osmconvert" Or singleProcess.ProcessName = "qgis-bin" Or singleProcess.ProcessName = "magick" Or singleProcess.ProcessName = "wpscript" Then
                            Try
                                MyGenerationWorker.WriteLog($"{singleProcess.ProcessName}.exe killed")
                                singleProcess.Kill()
                            Catch ex As Exception
                                MyGenerationWorker.WriteLog(ex.Message)
                                MsgBox(ex.Message)
                            End Try
                        End If
                    Next
                End If
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub ckb_pause_Checked(sender As Object, e As RoutedEventArgs) Handles ckb_pause.Checked, ckb_pause.Unchecked
        If ckb_pause.IsChecked = True Then
            MyGenerationWorker.WriteLog("Pause Generation")
            MyGenerationWorker.pause = True
        ElseIf ckb_pause.IsChecked = False Then
            MyGenerationWorker.WriteLog("Continue Generation")
            MyGenerationWorker.pause = False
        End If
    End Sub

    Private Sub Log_Click(sender As Object, e As RoutedEventArgs)
        Dim logfile As String = CType(sender, Controls.Button).Tag.ToString
        If logfile = "Convert pbf" Or logfile = "Combining" Or logfile = "Cleanup" Then
            If My.Computer.FileSystem.FileExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\logs\log-general.txt") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\logs\log-general.txt")
            End If
        Else
            If My.Computer.FileSystem.FileExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\logs\log-" & logfile & ".txt") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\logs\log-" & logfile & ".txt")
            End If
        End If
    End Sub

    Private Sub Folder_OSM_Click(sender As Object, e As RoutedEventArgs)
        Dim TileName As String = CType(sender, Controls.Button).Tag.ToString
        If TileName = "Convert pbf" Or TileName = "Combining" Or TileName = "Cleanup" Then
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToTempOSM & "\osm\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToTempOSM & "\osm\")
            End If
        Else
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToTempOSM & "\osm\" & TileName & "\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToTempOSM & "\osm\" & TileName & "\")
            End If
        End If
    End Sub

    Private Sub Folder_Images_Click(sender As Object, e As RoutedEventArgs)
        Dim TileName As String = CType(sender, Controls.Button).Tag.ToString
        If TileName = "Convert pbf" Or TileName = "Combining" Or TileName = "Cleanup" Then
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\image_exports\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\image_exports\")
            End If
        Else
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\image_exports\" & TileName & "\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\image_exports\" & TileName & "\")
            End If
        End If
    End Sub

    Private Sub Folder_Batch_Click(sender As Object, e As RoutedEventArgs)
        Dim TileName As String = CType(sender, Controls.Button).Tag.ToString
        If TileName = "Convert pbf" Or TileName = "Combining" Or TileName = "Cleanup" Then
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\batchfiles\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\batchfiles\")
            End If
        Else
            If My.Computer.FileSystem.DirectoryExists(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\batchfiles\" & TileName & "\") Then
                Process.Start(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\batchfiles\" & TileName & "\")
            End If
        End If
    End Sub


End Class

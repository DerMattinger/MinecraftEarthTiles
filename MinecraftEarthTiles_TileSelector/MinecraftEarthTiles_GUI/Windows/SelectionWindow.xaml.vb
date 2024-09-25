Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports MinecraftEarthTiles_Core
Imports System.Runtime.InteropServices

Public Class SelectionWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Private lastCenterPositionOnTarget As Point?
    Private lastMousePositionOnTarget As Point?
    Private lastDragPoint As Point?

    Private TilesVoidList As New List(Of String)

    Public Sub New()
        InitializeComponent()
        AddHandler ScrollViewer.ScrollChanged, AddressOf OnScrollViewerScrollChanged
        AddHandler ScrollViewer.MouseRightButtonUp, AddressOf OnMouseRightButtonUpCustom
        AddHandler ScrollViewer.PreviewMouseRightButtonUp, AddressOf OnMouseRightButtonUpCustom
        AddHandler ScrollViewer.PreviewMouseWheel, AddressOf OnPreviewMouseWheelCustom
        AddHandler ScrollViewer.PreviewMouseRightButtonDown, AddressOf OnMouseRightButtonDownCustom
        AddHandler ScrollViewer.MouseMove, AddressOf OnMouseMoveCustom
        AddHandler zsl_ZoomSlider.ValueChanged, AddressOf OnSliderValueChanged
    End Sub

    Private Sub OnMouseMoveCustom(ByVal sender As Object, ByVal e As MouseEventArgs)
        If lastDragPoint.HasValue Then
            Dim posNow As Point = e.GetPosition(ScrollViewer)
            Dim dX As Double = posNow.X - lastDragPoint.Value.X
            Dim dY As Double = posNow.Y - lastDragPoint.Value.Y
            lastDragPoint = posNow
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset - dX)
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - dY)
        End If
    End Sub

    Private Sub OnMouseRightButtonDownCustom(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
        Dim mousePos = e.GetPosition(ScrollViewer)

        If mousePos.X <= ScrollViewer.ViewportWidth AndAlso mousePos.Y < ScrollViewer.ViewportHeight Then 'make sure we still can use the scrollbars
            ScrollViewer.Cursor = Cursors.SizeAll
            lastDragPoint = mousePos
            Mouse.Capture(ScrollViewer)
        End If
    End Sub

    Private Sub OnPreviewMouseWheelCustom(ByVal sender As Object, ByVal e As MouseWheelEventArgs)
        lastMousePositionOnTarget = Mouse.GetPosition(Tiles)

        If e.Delta > 0 Then
            zsl_ZoomSlider.Value += 0.5
        End If

        If e.Delta < 0 Then
            zsl_ZoomSlider.Value -= 0.5
        End If

        e.Handled = True
    End Sub

    Private Sub OnMouseRightButtonUpCustom(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
        ScrollViewer.Cursor = Cursors.Arrow
        ScrollViewer.ReleaseMouseCapture()
        lastDragPoint = Nothing
    End Sub

    Private Sub OnSliderValueChanged(ByVal sender As Object, ByVal e As RoutedPropertyChangedEventArgs(Of Double))
        scaleTransform.ScaleX = e.NewValue
        scaleTransform.ScaleY = e.NewValue
        Dim centerOfViewport = New Point(ScrollViewer.ViewportWidth / 2, ScrollViewer.ViewportHeight / 2)
        lastCenterPositionOnTarget = ScrollViewer.TranslatePoint(centerOfViewport, Tiles)
    End Sub

    Private Sub OnScrollViewerScrollChanged(ByVal sender As Object, ByVal e As ScrollChangedEventArgs)
        If e.ExtentHeightChange <> 0 OrElse e.ExtentWidthChange <> 0 Then
            Dim targetBefore As Point? = Nothing
            Dim targetNow As Point? = Nothing

            If Not lastMousePositionOnTarget.HasValue Then

                If lastCenterPositionOnTarget.HasValue Then
                    Dim centerOfViewport = New Point(ScrollViewer.ViewportWidth / 2, ScrollViewer.ViewportHeight / 2)
                    Dim centerOfTargetNow As Point = ScrollViewer.TranslatePoint(centerOfViewport, Tiles)
                    targetBefore = lastCenterPositionOnTarget
                    targetNow = centerOfTargetNow
                End If
            Else
                targetBefore = lastMousePositionOnTarget
                targetNow = Mouse.GetPosition(Tiles)
                lastMousePositionOnTarget = Nothing
            End If

            If targetBefore.HasValue Then
                Dim dXInTargetPixels As Double = targetNow.Value.X - targetBefore.Value.X
                Dim dYInTargetPixels As Double = targetNow.Value.Y - targetBefore.Value.Y
                Dim multiplicatorX As Double = e.ExtentWidth / Tiles.Width
                Dim multiplicatorY As Double = e.ExtentHeight / Tiles.Height
                Dim newOffsetX As Double = ScrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX
                Dim newOffsetY As Double = ScrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY

                If Double.IsNaN(newOffsetX) OrElse Double.IsNaN(newOffsetY) Then
                    Return
                End If

                ScrollViewer.ScrollToHorizontalOffset(newOffsetX)
                ScrollViewer.ScrollToVerticalOffset(newOffsetY)
            End If
        End If
    End Sub

    Dim lastChecked As String = ""

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        For longitude As Integer = -89 To 90 Step CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
            For latitude As Integer = -180 To 179 Step CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
                Dim chb As New CheckBox With {
                    .IsChecked = False,
                    .Height = (4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)),
                    .Width = (4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)),
                    .Style = CType(TryFindResource("EarthTilesCheckboxStyle"), Style),
                    .HorizontalAlignment = HorizontalAlignment.Left,
                    .VerticalAlignment = VerticalAlignment.Top
                }

                'Für nicht durch 90 Teilbare TilesPerMap
                'If (longitude = -89 Or longitude = 90) And CType(StartupWindow.MyWorldSettings.TilesPerMap, Int16) = 4 Then
                'chb.Height = 2 * CType(StartupWindow.MyWorldSettings.TilesPerMap, Int32)
                'End If
                'If (latitude = -180 Or latitude = 179) And CType(StartupWindow.MyWorldSettings.TilesPerMap, Int16) = 4 Then
                'chb.Width = 2 * CType(StartupWindow.MyWorldSettings.TilesPerMap, Int32)
                'End If

                AddHandler chb.Click, AddressOf Chb_CheckedChanged
                Dim TilesMargin As Thickness
                TilesMargin.Left = (latitude * 4) + 720
                TilesMargin.Top = (longitude * 4) + 356
                chb.Margin = TilesMargin

                Dim latitudeDirection As String = ""
                If (latitude < 0) Then
                    latitudeDirection = "W"
                Else
                    latitudeDirection = "E"
                End If

                Dim longitudeDirection As String = ""
                If (longitude < 1) Then
                    longitudeDirection = "N"
                Else
                    longitudeDirection = "S"
                End If

                Dim latitudeTemp As Integer = latitude
                If (latitudeTemp < 0) Then
                    latitudeTemp *= -1
                End If

                Dim longitudeTemp As Integer = longitude
                If (longitudeTemp < 0) Then
                    longitudeTemp *= -1
                End If

                Dim latitudeNumber As String = ""
                If (latitudeTemp < 10) Then
                    latitudeNumber = "00" & latitudeTemp.ToString
                ElseIf (latitudeTemp < 100) Then
                    latitudeNumber = "0" & latitudeTemp.ToString
                Else
                    latitudeNumber = latitudeTemp.ToString
                End If

                Dim longitudeNumber As String = ""
                If (longitudeTemp < 10) Then
                    longitudeNumber = "0" & longitudeTemp.ToString
                Else
                    longitudeNumber = longitudeTemp.ToString
                End If

                chb.Name = longitudeDirection & longitudeNumber & latitudeDirection & latitudeNumber
                chb.ToolTip = longitudeDirection & longitudeNumber & latitudeDirection & latitudeNumber

                Tiles.Children.Add(chb)

            Next
        Next
        ApplyTheme()
        Selection_To_GUI(ClassWorker.GetSelection)
        CalculateTiles()
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

#Region "Menu"

    Private Sub Reset_Selection_Click(sender As Object, e As RoutedEventArgs)
        Dim MySelection As New Selection
        Selection_To_GUI(MySelection)
    End Sub

    Private Sub Load_Selection_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalSelection As New Selection
        Dim LoadSettingsFileDialog As New Forms.OpenFileDialog With {
            .FileName = "custom_selection.xml",
            .Filter = "Exe Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If LoadSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                LocalSelection = ClassWorker.LoadSelectionFromFile(LoadSettingsFileDialog.FileName)
                Selection_To_GUI(LocalSelection)
                Dim MessageBox As New MessageBoxWindow("Selection loaded from file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Save_Selection_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalSelection As Selection = GUI_To_Selection()
        Dim SaveSelectionFileDialog As New Forms.SaveFileDialog With {
            .FileName = "custom_selection.xml",
            .Filter = "Exe Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If SaveSelectionFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                CustomXmlSerialiser.SaveXML(SaveSelectionFileDialog.FileName, LocalSelection)
                Dim MessageBox As New MessageBoxWindow("Selection saved to file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earth.motfe.net/")
    End Sub

#End Region

#Region "Save/Cancel"

    Private Sub Save_Close_Click(sender As Object, e As RoutedEventArgs)
        Save_Click(sender, e)
        Close_Click(sender, e)
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            ClassWorker.SetSelection(GUI_To_Selection())
            Try
                ClassWorker.SaveSelectionToFile(ClassWorker.GetSelection, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/selection.xml")
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        Catch ex As Exception
            Dim MessageBox As New MessageBoxWindow(ex.Message)
            MessageBox.ShowDialog()
        End Try
    End Sub

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

#End Region

#Region "GUI"

    Public Sub Selection_To_GUI(MySelection As MinecraftEarthTiles_Core.Selection)
        cbb_Spawn_Tile.Items.Clear()
        If Not MySelection.TilesList Is Nothing Then
            For Each Tile In MySelection.TilesList
                If Not Tile.Contains("x") Then
                    cbb_Spawn_Tile.Items.Add(Tile)
                    If Tile = MySelection.SpawnTile Then
                        cbb_Spawn_Tile.Text = MySelection.SpawnTile
                    End If
                End If
            Next
            For Each Checkbox In Me.Tiles.Children.OfType(Of CheckBox)
                If MySelection.TilesList.Contains(Checkbox.Name) Then
                    Checkbox.IsChecked = True
                Else
                    Checkbox.IsChecked = False
                End If
            Next
        End If
        If MySelection.VoidBarrier = "0" Or MySelection.VoidBarrier = "512" Or MySelection.VoidBarrier = "1024" Or MySelection.VoidBarrier = "1536" Or MySelection.VoidBarrier = "2048" Then
            cbb_Void_Barrier.SelectedValue = MySelection.VoidBarrier
            cbb_Void_Barrier.Text = MySelection.VoidBarrier
        End If
        Calculate_Void_Tiles(Nothing, Nothing)
    End Sub

    Private Function GUI_To_Selection() As MinecraftEarthTiles_Core.Selection
        Dim LocalSelection As New MinecraftEarthTiles_Core.Selection
        Dim TilesList = (
            From T In Me.Tiles.Children.OfType(Of CheckBox)()
            Where T.IsChecked Select T.Name
        ).ToList
        TilesList.Sort()
        LocalSelection.TilesList.Clear()
        LocalSelection.TilesList.AddRange(TilesList)
        LocalSelection.FullTileList.Clear()
        LocalSelection.FullTileList.AddRange(TilesList)
        LocalSelection.TilesList.AddRange(TilesVoidList)
        LocalSelection.SpawnTile = cbb_Spawn_Tile.Text
        LocalSelection.VoidBarrier = cbb_Void_Barrier.Text
        LocalSelection.VoidTiles = TilesVoidList.Count
        Return LocalSelection
    End Function

    Private Sub Chb_CheckedChanged(sender As Object, e As EventArgs)
        Dim chb As CheckBox = DirectCast(sender, CheckBox) 'Use DirectCast to cast the sender into a checkbox
        Dim currentSpawnTile As String = ""

        If My.Computer.Keyboard.ShiftKeyDown Then
            If Not lastChecked = "" Then
                Dim lastCheckedLatiDir = lastChecked.Substring(0, 1)
                Dim lastCheckedLatiNumber As Int32 = 0
                Int32.TryParse(lastChecked.Substring(1, 2), lastCheckedLatiNumber)
                Dim lastCheckedLongDir = lastChecked.Substring(3, 1)
                Dim lastCheckedLongNumber As Int32 = 0
                Int32.TryParse(lastChecked.Substring(4, 3), lastCheckedLongNumber)
                If lastCheckedLatiDir = "N" Then
                    lastCheckedLatiNumber *= -1
                End If
                If lastCheckedLongDir = "W" Then
                    lastCheckedLongNumber *= -1
                End If
                Dim currentChecked As String = chb.Name
                Dim currentCheckedLatiDir = currentChecked.Substring(0, 1)
                Dim currentCheckedLatiNumber As Int32 = 0
                Int32.TryParse(currentChecked.Substring(1, 2), currentCheckedLatiNumber)
                Dim currentCheckedLongDir = currentChecked.Substring(3, 1)
                Dim currentCheckedLongNumber As Int32 = 0
                Int32.TryParse(currentChecked.Substring(4, 3), currentCheckedLongNumber)
                If currentCheckedLatiDir = "N" Then
                    currentCheckedLatiNumber *= -1
                End If
                If currentCheckedLongDir = "W" Then
                    currentCheckedLongNumber *= -1
                End If

                Dim smallerLati As Int32 = 0
                Dim biggerLati As Int32 = 0
                Dim smallerLong As Int32 = 0
                Dim biggerLong As Int32 = 0

                If lastCheckedLatiNumber < currentCheckedLatiNumber Then
                    smallerLati = lastCheckedLatiNumber
                    biggerLati = currentCheckedLatiNumber
                Else
                    smallerLati = currentCheckedLatiNumber
                    biggerLati = lastCheckedLatiNumber
                End If

                If lastCheckedLongNumber < currentCheckedLongNumber Then
                    smallerLong = lastCheckedLongNumber
                    biggerLong = currentCheckedLongNumber
                Else
                    smallerLong = currentCheckedLongNumber
                    biggerLong = lastCheckedLongNumber
                End If

                Dim NewTilesList As List(Of String) = New List(Of String)
                For LatiFor As Int32 = smallerLati To biggerLati Step 1
                    For LongFor As Int32 = smallerLong To biggerLong Step 1
                        Dim LatiForDir As String = ""
                        Dim LatiForNum As Int32 = 0
                        If (LatiFor < 1) Then
                            LatiForDir = "N"
                            LatiForNum = LatiFor * -1
                        Else
                            LatiForDir = "S"
                            LatiForNum = LatiFor
                        End If
                        Dim LongForDir As String = ""
                        Dim LongForNum As Int32 = 0
                        If (LongFor < 0) Then
                            LongForDir = "W"
                            LongForNum = LongFor * -1
                        Else
                            LongForDir = "E"
                            LongForNum = LongFor
                        End If

                        Dim longitudeNumber As String = ""
                        If (LongForNum < 10) Then
                            longitudeNumber = "00" & LongForNum.ToString
                        ElseIf (LongForNum < 100) Then
                            longitudeNumber = "0" & LongForNum.ToString
                        Else
                            longitudeNumber = LongForNum.ToString
                        End If

                        Dim latitudeNumber As String = ""
                        If (LatiForNum < 10) Then
                            latitudeNumber = "0" & LatiForNum.ToString
                        Else
                            latitudeNumber = LatiForNum.ToString
                        End If

                        NewTilesList.Add(LatiForDir & latitudeNumber & LongForDir & longitudeNumber)
                    Next
                Next
                For Each Checkbox In Me.Tiles.Children.OfType(Of CheckBox)
                    If NewTilesList.Contains(Checkbox.Name) Then
                        Checkbox.IsChecked = True
                    End If
                Next

            End If
        Else
            lastChecked = chb.Name
        End If

        currentSpawnTile = cbb_Spawn_Tile.Text
        cbb_Spawn_Tile.Items.Clear()

        Dim TilesList =
        (
            From T In Me.Tiles.Children.OfType(Of CheckBox)()
            Where T.IsChecked Select T.Name
        ).ToList
        For Each Tile In TilesList
            If Not Tile.Contains("x") Then
                cbb_Spawn_Tile.Items.Add(Tile)
                If Tile = currentSpawnTile Then
                    cbb_Spawn_Tile.Text = Tile
                End If
                If TilesList.Count = 1 Then
                    cbb_Spawn_Tile.Text = Tile
                End If
            End If
        Next

        CalculateTiles()
    End Sub

    Public Sub Change_Background(sender As Object, e As EventArgs)
        Dim MyURI As Uri
        Select Case cbb_Background_Image.Text
            Case "Terrain"
                MyURI = New Uri("pack://application:,,,/MyResources/terrain.jpg")
            Case "Borders"
                MyURI = New Uri("pack://application:,,,/MyResources/borders.png")
            Case "Scale 1:33"
                MyURI = New Uri("pack://application:,,,/MyResources/scale33.png")
            Case "Scale 1:25"
                MyURI = New Uri("pack://application:,,,/MyResources/scale25.png")
            Case "Scale 1:10"
                MyURI = New Uri("pack://application:,,,/MyResources/scale10.png")
            Case "Scale 1:5"
                MyURI = New Uri("pack://application:,,,/MyResources/scale5.png")
            Case Else
                MyURI = New Uri("pack://application:,,,/MyResources/terrain.jpg")
        End Select
        img_Background.Source = New BitmapImage(MyURI)
    End Sub

    Public Sub Calculate_Void_Tiles(sender As Object, e As EventArgs)

        Dim NumberOfVoidBarrier As Int32 = CType(cbb_Void_Barrier.Text, Integer)

        Dim mapWidth As Int32 = CType(180 / CType(ClassWorker.GetWorldSettings.TilesPerMap, Integer) * CType(ClassWorker.GetWorldSettings.BlocksPerTile, Integer), Integer)
        Dim mapHeight As Int32 = CType(90 / CType(ClassWorker.GetWorldSettings.TilesPerMap, Integer) * CType(ClassWorker.GetWorldSettings.BlocksPerTile, Integer), Integer)

        Select Case cbb_Void_Barrier.Text
            Case "0"
                TilesVoidList.Clear()
            Case Else

                Dim VoidTiles As Integer = CType(CType(cbb_Void_Barrier.Text, Integer) / 512, Integer)

                ''Void im Norden
                For latitude As Integer = (mapHeight + NumberOfVoidBarrier) * -1 To (mapHeight + 512) * -1 Step 512
                    For longitude As Integer = (mapWidth + NumberOfVoidBarrier) * -1 To mapWidth Step 512
                        TilesVoidList.Add(longitude & "x" & latitude)
                    Next
                Next

                ''Viod im Süden
                For latitude As Integer = mapHeight To mapHeight + NumberOfVoidBarrier - 512 Step 512
                    For longitude As Integer = (mapWidth + NumberOfVoidBarrier) * -1 To mapWidth Step 512
                        TilesVoidList.Add(longitude & "x" & latitude)
                    Next
                Next

                ''Viod im Westen
                For longitude As Integer = (mapWidth + NumberOfVoidBarrier) * -1 To (mapWidth + 512) * -1 Step 512
                    For latitude As Integer = (mapHeight + NumberOfVoidBarrier) * -1 To mapHeight + NumberOfVoidBarrier - 512 Step 512
                        TilesVoidList.Add(longitude & "x" & latitude)
                    Next
                Next

                ''Viod im Osten
                For longitude As Integer = mapWidth To mapWidth + NumberOfVoidBarrier - 512 Step 512
                    For latitude As Integer = (mapHeight + NumberOfVoidBarrier) * -1 To mapHeight + NumberOfVoidBarrier - 512 Step 512
                        TilesVoidList.Add(longitude & "x" & latitude)
                    Next
                Next

                TilesVoidList = TilesVoidList.Distinct().ToList
                TilesVoidList.Sort()

        End Select

    End Sub

    Public Sub CalculateTiles()
        Dim TilesList = (
            From T In Me.Tiles.Children.OfType(Of CheckBox)()
            Where T.IsChecked Select T.Name
        ).ToList
        Dim xMin As Integer = 0
        Dim yMin As Integer = 0
        Dim xMax As Integer = 0
        Dim yMax As Integer = 0
        Dim TilesPerMap As Int32 = CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
        Dim blocksPerTile As Int32 = CType(ClassWorker.GetWorldSettings.BlocksPerTile, Int32)

        For Each Tile In TilesList
            Dim LatiDir = Tile.Substring(0, 1)
            Dim LatiNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
            Dim LongDir = Tile.Substring(3, 1)
            Dim LongNumber As Int32 = 0
            Int32.TryParse(Tile.Substring(4, 3), LongNumber)

            Dim xMinNew As Integer = 0
            Dim yMinNew As Integer = 0
            Dim xMaxNew As Integer = 0
            Dim yMaxNew As Integer = 0

            If LatiDir = "N" Then
                yMinNew = CType((((LatiNumber + 1) * blocksPerTile / TilesPerMap) + 1) * -1, Int32)
                yMaxNew = CType((((LatiNumber + 1) * blocksPerTile / TilesPerMap) - blocksPerTile + 1) * -1, Int32)
            Else
                yMinNew = CType(((LatiNumber - 1) * blocksPerTile / TilesPerMap), Int32)
                yMaxNew = CType(((LatiNumber - 1) * blocksPerTile / TilesPerMap) + blocksPerTile, Int32)
            End If
            If LongDir = "E" Then
                xMinNew = CType(((LongNumber) * blocksPerTile / TilesPerMap), Int32)
                xMaxNew = CType(((LongNumber) * blocksPerTile / TilesPerMap) + blocksPerTile, Int32)
            Else
                xMinNew = CType(((LongNumber * blocksPerTile / TilesPerMap) + 1) * -1, Int32)
                xMaxNew = CType(((LongNumber * blocksPerTile / TilesPerMap) - blocksPerTile + 1) * -1, Int32)
            End If

            If Tile = TilesList.First Then
                xMin = xMinNew
                xMax = xMaxNew
                yMin = yMinNew
                yMax = yMaxNew
            Else
                If xMinNew < xMin Then
                    xMin = xMinNew
                End If
                If xMaxNew > xMax Then
                    xMax = xMaxNew
                End If
                If yMinNew < yMin Then
                    yMin = yMinNew
                End If
                If yMaxNew > yMax Then
                    yMax = yMaxNew
                End If
            End If

        Next

        txb_Corners.Text = xMin & " " & yMin & " " & xMax & " " & yMax
        btn_Save_Selection.IsEnabled = True
        btnSaveClose.IsEnabled = True
        btnSave.IsEnabled = True
        txb_toManyTiles.Text = ""

    End Sub

#End Region

End Class

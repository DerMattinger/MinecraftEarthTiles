Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports MinecraftEarthTiles_Core
Imports System.Runtime.InteropServices

Public Class PreviewWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Private lastCenterPositionOnTarget As Point?
    Private lastMousePositionOnTarget As Point?
    Private lastDragPoint As Point?

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

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        For longitude As Integer = -89 To 90 Step CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
            For latitude As Integer = -180 To 179 Step CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)


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

                Dim tile As String = longitudeDirection & longitudeNumber & latitudeDirection & latitudeNumber

                Dim gpi As GeneratedImagePreview

                If ClassWorker.GetSelection.FullTileList.Contains(tile) Then
                    gpi = New GeneratedImagePreview(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\render\" & tile & ".png", 4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32), True)
                    gpi.ToolTip = ClassWorker.GetTilesSettings.PathToScriptsFolder & "\render\" & longitudeDirection & longitudeNumber & latitudeDirection & latitudeNumber & ".png"
                    gpi.Name = tile
                Else
                    gpi = New GeneratedImagePreview(ClassWorker.GetTilesSettings.PathToScriptsFolder & "\render\" & tile & ".png", 4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32), False)
                End If
                gpi.Height = 4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
                gpi.Width = 4 * CType(ClassWorker.GetWorldSettings.TilesPerMap, Int32)
                gpi.HorizontalAlignment = HorizontalAlignment.Left
                gpi.VerticalAlignment = VerticalAlignment.Top

                Dim TilesMargin As Thickness
                TilesMargin.Left = (latitude * 4) + 720
                TilesMargin.Top = (longitude * 4) + 356
                gpi.Margin = TilesMargin



                Tiles.Children.Add(gpi)
                ApplyTheme()
            Next
        Next
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

    Public Sub refresh(tileName As String)
        For Each child In Tiles.Children
            If child.GetType = GetType(GeneratedImagePreview) Then
                Dim gip = CType(child, GeneratedImagePreview)
                If gip.Name = tileName Then
                    gip.refresh()
                End If
            End If
        Next
    End Sub

#Region "Menu"

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        For Each child In Tiles.Children
            If child.GetType = GetType(GeneratedImagePreview) Then
                Dim gip = CType(child, GeneratedImagePreview)
                gip.Dispose()
            End If
        Next
        Tiles.Children.Clear()
        Close()
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earth.motfe.net/")
    End Sub

#End Region

End Class

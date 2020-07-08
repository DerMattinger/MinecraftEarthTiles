Imports System.IO
Imports System.Windows.Forms

Public Class SettingsWindow

#Region "Menu"

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Settings_To_GUI(StartupWindow.MySettings)
    End Sub

    Private Sub Reset_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim ResetSettings As New Settings
        Settings_To_GUI(ResetSettings)
    End Sub

    Private Sub Load_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalSettings As New Settings
        Dim LoadSettingsFileDialog As New OpenFileDialog With {
            .FileName = "settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If LoadSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                LocalSettings = CustomXmlSerialiser.GetXMLSettings(LoadSettingsFileDialog.FileName)
                If Not Directory.Exists(LocalSettings.PathToScriptsFolder) Then
                    LocalSettings.PathToScriptsFolder = ""
                End If
                If Not File.Exists(LocalSettings.PathToWorldPainterFolder) Then
                    LocalSettings.PathToWorldPainterFolder = ""
                End If
                If Not Directory.Exists(LocalSettings.PathToQGIS) Then
                    LocalSettings.PathToQGIS = ""
                End If
                If Not File.Exists(LocalSettings.PathToMagick) Then
                    LocalSettings.PathToMagick = ""
                End If
                MsgBox("Settings loaded from file.")
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
        Settings_To_GUI(LocalSettings)
    End Sub

    Private Sub Save_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalSettings As Settings = GUI_To_Settings()
        Dim SaveSettingsFileDialog As New SaveFileDialog With {
            .FileName = "settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If SaveSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                CustomXmlSerialiser.SaveXML(SaveSettingsFileDialog.FileName, LocalSettings)
                MsgBox("Settings saved to file.")
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        Help.ShowHelp(Nothing, "Help/Settings.chm")
    End Sub

#End Region

#Region "Path Dialogs"

    Private Sub Btn_PathToScriptFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New Forms.FolderBrowserDialog With {
            .SelectedPath = My.Application.Info.DirectoryPath
        }
        If MyFolderBrowserDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txb_PathToScriptsFolder.Text = MyFolderBrowserDialog.SelectedPath
        End If
    End Sub

    Private Sub Btn_PathToWorldPainter_Click(sender As Object, e As RoutedEventArgs)
        Dim WorldPainterScriptFileDialog As New OpenFileDialog With {
            .FileName = "wpscript.exe",
            .Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If WorldPainterScriptFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            txb_PathToWorldPainterFile.Text = WorldPainterScriptFileDialog.FileName
        End If
    End Sub

    Private Sub Btn_PathToQGIS_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New Forms.FolderBrowserDialog With {
            .SelectedPath = My.Application.Info.DirectoryPath
        }
        If MyFolderBrowserDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txb_PathToQGIS.Text = MyFolderBrowserDialog.SelectedPath
        End If
    End Sub

    Private Sub Btn_PathToMagick_Click(sender As Object, e As RoutedEventArgs)
        Dim MagickFileDialog As New OpenFileDialog With {
            .FileName = "magick.exe",
            .Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If MagickFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            txb_PathToMagick.Text = MagickFileDialog.FileName
        End If
    End Sub

#End Region

#Region "Save/Cancel"

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            If Not StartupWindow.MySettings.TilesPerMap = cbb_TilesperMap.Text Then
                StartupWindow.MySelection = New Selection
            End If
            StartupWindow.MySettings = GUI_To_Settings()
            Close()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

#End Region

#Region "GUI"

    Private Sub Settings_To_GUI(Settings As Settings)

        txb_PathToScriptsFolder.Text = Settings.PathToScriptsFolder
        txb_PathToWorldPainterFile.Text = Settings.PathToWorldPainterFolder
        txb_PathToQGIS.Text = Settings.PathToQGIS
        txb_PathToMagick.Text = Settings.PathToMagick

        If Settings.BlocksPerTile = "512" Or Settings.BlocksPerTile = "1024" Or Settings.BlocksPerTile = "2048" Or Settings.BlocksPerTile = "3072" Or Settings.BlocksPerTile = "4096" Or Settings.BlocksPerTile = "10240" Then
            cbb_BlocksPerTile.SelectedValue = Settings.BlocksPerTile
            cbb_BlocksPerTile.Text = Settings.BlocksPerTile
        End If

        If Settings.VerticalScale = "5" Or Settings.VerticalScale = "10" Or Settings.VerticalScale = "25" Or Settings.VerticalScale = "50" Or Settings.VerticalScale = "100" Or Settings.VerticalScale = "200" Then
            cbb_VerticalScale.SelectedValue = Settings.VerticalScale
            cbb_VerticalScale.Text = Settings.VerticalScale
        End If

        If Settings.Terrain = "Standard" Or Settings.Terrain = "Custom" Then
            cbb_TerrainMapping.SelectedValue = Settings.Terrain
            cbb_TerrainMapping.Text = Settings.Terrain
        End If

        If Settings.TilesPerMap = "1" Or Settings.TilesPerMap = "2" Or Settings.TilesPerMap = "3" Or Settings.TilesPerMap = "5" Or Settings.TilesPerMap = "10" Or Settings.TilesPerMap = "15" Or Settings.TilesPerMap = "30" Then
            cbb_TilesperMap.SelectedValue = Settings.TilesPerMap
            cbb_TilesperMap.Text = Settings.TilesPerMap
        End If

        If Settings.MapVersion = "1.12" Or Settings.MapVersion = "1.12 with Cubic Chunks" Or Settings.MapVersion = "1.14+" Then
            cbb_TerrainMapping.SelectedValue = Settings.Terrain
            cbb_TerrainMapping.Text = Settings.Terrain
        End If

        If Settings.Heightmap_Error_Correction = True Then
            chb_Heightmap_Error_Correction.IsChecked = True
        ElseIf Settings.Heightmap_Error_Correction = False Then
            chb_Heightmap_Error_Correction.IsChecked = False
        End If

        If Settings.geofabrik = True Then
            chb_geofabrik.IsChecked = True
        ElseIf Settings.geofabrik = False Then
            chb_geofabrik.IsChecked = False
        End If

        If Settings.highways = True Then
            chb_highways.IsChecked = True
        ElseIf Settings.highways = False Then
            chb_highways.IsChecked = False
        End If

        If Settings.streets = True Then
            chb_streets.IsChecked = True
        ElseIf Settings.streets = False Then
            chb_streets.IsChecked = False
        End If

        If Settings.buildings = True Then
            chb_buildings.IsChecked = True
        ElseIf Settings.buildings = False Then
            chb_buildings.IsChecked = False
        End If

        If Settings.borders = True Then
            chb_borders.IsChecked = True
        ElseIf Settings.borders = False Then
            chb_borders.IsChecked = False
        End If

        If Settings.farms = True Then
            chb_farms.IsChecked = True
        ElseIf Settings.farms = False Then
            chb_farms.IsChecked = False
        End If

        If Settings.meadows = True Then
            chb_meadows.IsChecked = True
        ElseIf Settings.meadows = False Then
            chb_meadows.IsChecked = False
        End If

        If Settings.quarrys = True Then
            chb_quarrys.IsChecked = True
        ElseIf Settings.quarrys = False Then
            chb_quarrys.IsChecked = False
        End If

        If Settings.streams = True Then
            chb_streams.IsChecked = True
        ElseIf Settings.streams = False Then
            chb_streams.IsChecked = False
        End If

        txb_Proxy.Text = Settings.Proxy

        Dim ScaleCalc As Int16 = 0
        Select Case Settings.BlocksPerTile
            Case "512"
                ScaleCalc = 500
            Case "1024"
                ScaleCalc = 1000
            Case "2048"
                ScaleCalc = 2000
            Case "3072"
                ScaleCalc = 3000
            Case "4096"
                ScaleCalc = 4000
            Case "10240"
                ScaleCalc = 10000
        End Select
        lbl_Scale_Quantity.Content = "1:" & (Math.Round(100000 / (ScaleCalc / CType(Settings.TilesPerMap, Int16)))).ToString

    End Sub

    Private Function GUI_To_Settings() As Settings
        Dim LocalSettings As New Settings
        If Directory.Exists(txb_PathToScriptsFolder.Text) Then
            LocalSettings.PathToScriptsFolder = txb_PathToScriptsFolder.Text
        End If
        If File.Exists(txb_PathToWorldPainterFile.Text) Then
            LocalSettings.PathToWorldPainterFolder = txb_PathToWorldPainterFile.Text
        End If
        If Directory.Exists(txb_PathToQGIS.Text) Then
            LocalSettings.PathToQGIS = txb_PathToQGIS.Text
        End If
        If File.Exists(txb_PathToMagick.Text) Then
            LocalSettings.PathToMagick = txb_PathToMagick.Text
        End If
        If cbb_BlocksPerTile.Text = "512" Or cbb_BlocksPerTile.Text = "1024" Or cbb_BlocksPerTile.Text = "2048" Or cbb_BlocksPerTile.Text = "3072" Or cbb_BlocksPerTile.Text = "4096" Or cbb_BlocksPerTile.Text = "10240" Then
            LocalSettings.BlocksPerTile = cbb_BlocksPerTile.Text
        End If
        If cbb_VerticalScale.Text = "200" Or cbb_VerticalScale.Text = "100" Or cbb_VerticalScale.Text = "50" Or cbb_VerticalScale.Text = "25" Or cbb_VerticalScale.Text = "10" Or cbb_VerticalScale.Text = "5" Then
            LocalSettings.VerticalScale = cbb_VerticalScale.Text
        End If
        If cbb_TilesperMap.Text = "1" Or cbb_TilesperMap.Text = "2" Or cbb_TilesperMap.Text = "3" Or cbb_TilesperMap.Text = "5" Or cbb_TilesperMap.Text = "10" Or cbb_TilesperMap.Text = "15" Or cbb_TilesperMap.Text = "30" Then
            LocalSettings.TilesPerMap = cbb_TilesperMap.Text
        End If
        If cbb_TerrainMapping.Text = "Default" Or cbb_TerrainMapping.Text = "Custom" Then
            LocalSettings.Terrain = cbb_TerrainMapping.Text
        End If
        If cbb_MapVersion.Text = "1.12" Or cbb_MapVersion.Text = "1.12 with Cubic Chunks" Or cbb_MapVersion.Text = "1.14+" Then
            LocalSettings.MapVersion = cbb_MapVersion.Text
        End If

        LocalSettings.Heightmap_Error_Correction = CBool(chb_Heightmap_Error_Correction.IsChecked)

        LocalSettings.geofabrik = CBool(chb_geofabrik.IsChecked)
        LocalSettings.streets = CBool(chb_streets.IsChecked)
        LocalSettings.buildings = CBool(chb_buildings.IsChecked)
        LocalSettings.borders = CBool(chb_borders.IsChecked)
        LocalSettings.farms = CBool(chb_farms.IsChecked)
        LocalSettings.meadows = CBool(chb_meadows.IsChecked)
        LocalSettings.quarrys = CBool(chb_quarrys.IsChecked)
        LocalSettings.streams = CBool(chb_streams.IsChecked)

        LocalSettings.Proxy = txb_Proxy.Text

        Return LocalSettings
    End Function

    Private Sub Calculate_Scale(sender As Object, e As EventArgs)
        If Not cbb_BlocksPerTile Is Nothing And Not cbb_TilesperMap Is Nothing Then
            If Not cbb_BlocksPerTile.Text = "" And Not cbb_TilesperMap.Text = "" Then
                Dim ScaleCalc As Int16 = 0
                Select Case cbb_BlocksPerTile.Text
                    Case "512"
                        ScaleCalc = 500
                    Case "1024"
                        ScaleCalc = 1000
                    Case "2048"
                        ScaleCalc = 2000
                    Case "3072"
                        ScaleCalc = 3000
                    Case "4096"
                        ScaleCalc = 4000
                    Case "10240"
                        ScaleCalc = 10000
                End Select
                lbl_Scale_Quantity.Content = "1:" & (Math.Round(100000 / (ScaleCalc / CType(cbb_TilesperMap.Text, Int16)))).ToString
            End If
        End If
    End Sub

#End Region

End Class

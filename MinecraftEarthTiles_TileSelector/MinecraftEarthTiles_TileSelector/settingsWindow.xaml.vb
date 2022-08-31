Imports System.IO
Imports System.Text.RegularExpressions
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
                Settings_To_GUI(LocalSettings)
                MsgBox("Settings loaded from file.")
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub

    Private Sub Save_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalSettings As Settings = GUI_To_Settings()
        Dim SaveSettingsFileDialog As New SaveFileDialog With {
            .FileName = "custom_settings.xml",
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
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earth.motfe.net/tiles-settings/")
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
            If My.Computer.FileSystem.FileExists(MyFolderBrowserDialog.SelectedPath & "\bin\qgis-bin.exe") Or My.Computer.FileSystem.FileExists(MyFolderBrowserDialog.SelectedPath & "\bin\qgis-ltr-bin.exe") Then
                txb_PathToQGIS.Text = MyFolderBrowserDialog.SelectedPath
            Else
                MsgBox("QGIS not found. Maybe it is in another folder!")
            End If
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

    Private Sub Btn_PathToPBF_Click(sender As Object, e As RoutedEventArgs)
        Dim PBFFileDialog As New OpenFileDialog With {
            .Filter = "pbf Files (.pbf)|*.pbf|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If PBFFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            txb_PathToPBF.Text = PBFFileDialog.FileName
            chb_geofabrik.IsChecked = True
        End If
    End Sub

    Private Sub Btn_PathToExport_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New Forms.FolderBrowserDialog With {
            .SelectedPath = My.Application.Info.DirectoryPath
        }
        If MyFolderBrowserDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txb_PathToExport.Text = MyFolderBrowserDialog.SelectedPath
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
            Try
                CustomXmlSerialiser.SaveXML(StartupWindow.MySettings.PathToScriptsFolder & "/settings.xml", StartupWindow.MySettings)
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
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
        txb_PathToPBF.Text = Settings.PathToPBF
        txb_PathToExport.Text = Settings.PathToExport

        If Not Settings.WorldName = "" Then
            txb_worldName.Text = RemoveIllegalFileNameChars(Settings.WorldName)
        Else
            txb_worldName.Text = "world"
        End If

        If CType(Settings.BlocksPerTile, Int16) Mod 512 = 0.0 Then
            cbb_BlocksPerTile.SelectedValue = Settings.BlocksPerTile
            cbb_BlocksPerTile.Text = Settings.BlocksPerTile
        End If

        If Settings.VerticalScale = "5" Or Settings.VerticalScale = "10" Or Settings.VerticalScale = "25" Or Settings.VerticalScale = "35" Or Settings.VerticalScale = "50" Or Settings.VerticalScale = "75" Or Settings.VerticalScale = "100" Or Settings.VerticalScale = "200" Then
            cbb_VerticalScale.SelectedValue = Settings.VerticalScale
            cbb_VerticalScale.Text = Settings.VerticalScale
            If Not Settings.TilesPerMap = "1" And (Settings.VerticalScale = "5" Or Settings.VerticalScale = "10" Or Settings.VerticalScale = "25") Then
                cbb_VerticalScale.SelectedValue = "35"
                cbb_VerticalScale.Text = "35"
            End If
        End If

        If Settings.Terrain = "Standard" Or Settings.Terrain = "Custom" Then
            cbb_TerrainMapping.SelectedValue = Settings.Terrain
            cbb_TerrainMapping.Text = Settings.Terrain
        End If

        If 90 Mod CType(Settings.TilesPerMap, Int16) = 0.0 Then
            cbb_TilesPerMap.SelectedValue = Settings.TilesPerMap
            cbb_TilesPerMap.Text = Settings.TilesPerMap
        End If

        If Settings.MapVersion = "1.12" Or Settings.MapVersion = "1.16" Or Settings.MapVersion = "1.17" Or Settings.MapVersion = "1.18" Or Settings.MapVersion = "1.19" Then
            cbb_MapVersion.SelectedValue = Settings.MapVersion
            cbb_MapVersion.Text = Settings.MapVersion
        End If

        If Settings.MapVersion = "1.12" Or Settings.MapVersion = "1.18" Or Settings.MapVersion = "1.19" Then
            chb_VanillaPopulation.IsEnabled = True
        Else
            chb_VanillaPopulation.IsEnabled = False
        End If

        chb_Borders.IsChecked = Settings.bordersBoolean

        If Settings.borders = "2000bc" Or Settings.borders = "1000bc" Or Settings.borders = "500bc" Or Settings.borders = "323bc" Or Settings.borders = "200bc" Or Settings.borders = "1bc" Or Settings.borders = "400" Or Settings.borders = "600" Or Settings.borders = "800" Or Settings.borders = "1000" Or Settings.borders = "1279" Or Settings.borders = "1482" Or Settings.borders = "1530" Or Settings.borders = "1650" Or Settings.borders = "1715" Or Settings.borders = "1783" Or Settings.borders = "1815" Or Settings.borders = "1880" Or Settings.borders = "1914" Or Settings.borders = "1920" Or Settings.borders = "1938" Or Settings.borders = "1945" Or Settings.borders = "1994" Or Settings.borders = "Current" Then
            cbb_Borders.SelectedValue = Settings.borders
            cbb_Borders.Text = Settings.borders
        End If

        chb_HeightmapErrorCorrection.IsChecked = Settings.Heightmap_Error_Correction

        chb_geofabrik.IsChecked = Settings.geofabrik

        chb_Bathymetry.IsChecked = Settings.bathymetry

        If Settings.TerrainSource = "Offline Terrain (high res)" Or Settings.TerrainSource = "Offline Terrain (low res)" Or Settings.TerrainSource = "Arcgis" Or Settings.TerrainSource = "Google" Or Settings.TerrainSource = "Bing" Then
            cbb_TerrainSource.SelectedValue = Settings.TerrainSource
            cbb_TerrainSource.Text = Settings.TerrainSource
        End If

        If Settings.biomeSource = "Köppen Climate Classification" Or Settings.biomeSource = "Terrestrial Ecoregions (WWF)" Then
            cbb_BiomeSource.SelectedValue = Settings.biomeSource
            cbb_BiomeSource.Text = Settings.biomeSource
        End If

        chb_highways.IsChecked = Settings.highways

        chb_streets.IsChecked = Settings.streets

        chb_small_streets.IsChecked = Settings.small_streets

        chb_buildings.IsChecked = Settings.buildings

        chb_ores.IsChecked = Settings.ores

        chb_netherrite.IsChecked = Settings.netherite

        chb_farms.IsChecked = Settings.farms

        chb_meadows.IsChecked = Settings.meadows

        chb_quarrys.IsChecked = Settings.quarrys

        chb_forest.IsChecked = Settings.forests

        chb_aerodrome.IsChecked = Settings.aerodrome

        chb_mob_spawner.IsChecked = Settings.mobSpawner

        chb_animal_spawner.IsChecked = Settings.animalSpawner

        chb_rivers.IsChecked = Settings.riversBoolean

        If Settings.rivers = "small" Or Settings.rivers = "medium" Or Settings.rivers = "large" Or Settings.rivers = "major" Then
            cbb_Rivers.SelectedValue = Settings.rivers
            cbb_Rivers.Text = Settings.rivers
        End If

        If Settings.MapVersion = "1.12" Or Settings.MapVersion = "1.18" Or Settings.MapVersion = "1.19" Then
            chb_VanillaPopulation.IsChecked = Settings.vanillaPopulation
        Else
            chb_VanillaPopulation.IsChecked = False
        End If

        chb_streams.IsChecked = Settings.streams

        chb_volcanos.IsChecked = Settings.volcanos
        chb_shrubs.IsChecked = Settings.shrubs
        chb_crops.IsChecked = Settings.crops

        If CType(Settings.NumberOfCores, Int32) <= 16 Then
            cbb_NumberOfCores.SelectedValue = Settings.NumberOfCores
            cbb_NumberOfCores.Text = Settings.NumberOfCores
        End If

        chb_ParallelWorldPainterGenerations.IsChecked = Settings.ParallelWorldPainterGenerations

        chb_keepPbfFiles.IsChecked = Settings.keepPbfFile
        chb_reUsePbfFiles.IsChecked = Settings.reUsePbfFile
        chb_keepOsmFiles.IsChecked = Settings.keepOsmFiles
        chb_reUseOsmFiles.IsChecked = Settings.reUseOsmFiles
        chb_keepImages.IsChecked = Settings.keepImageFiles
        chb_reUseImages.IsChecked = Settings.reUseImageFiles
        chb_keepWorldPainter.IsChecked = Settings.keepWorldPainterFiles

        chb_CmdVisibility.IsChecked = Settings.cmdVisibility

        chb_continue.IsChecked = Settings.continueGeneration

        txb_Proxy.Text = Settings.Proxy

        If Settings.mapOffset = "-1" Or Settings.mapOffset = "0" Or Settings.mapOffset = "1" Then
            cbb_MapOffset.SelectedValue = Settings.mapOffset
            cbb_MapOffset.Text = Settings.mapOffset
        End If

        txb_OsmURL.Text = Settings.OverpassURL

        Calculate_Scale()
        ReUseChanged()

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
        If File.Exists(txb_PathToPBF.Text) Then
            LocalSettings.PathToPBF = txb_PathToPBF.Text
        End If
        If Directory.Exists(txb_PathToExport.Text) Then
            LocalSettings.PathToExport = txb_PathToExport.Text
        End If
        If Not txb_WorldName.Text = "" Then
            LocalSettings.WorldName = RemoveIllegalFileNameChars(txb_WorldName.Text)
            txb_WorldName.Text = RemoveIllegalFileNameChars(txb_WorldName.Text)
        End If
        If CType(cbb_BlocksPerTile.Text, Int16) Mod 512 = 0.0 Then
            LocalSettings.BlocksPerTile = cbb_BlocksPerTile.Text
        End If
        If cbb_VerticalScale.Text = "200" Or cbb_VerticalScale.Text = "100" Or cbb_VerticalScale.Text = "75" Or cbb_VerticalScale.Text = "50" Or cbb_VerticalScale.Text = "35" Or cbb_VerticalScale.Text = "25" Or cbb_VerticalScale.Text = "10" Or cbb_VerticalScale.Text = "5" Then
            LocalSettings.VerticalScale = cbb_VerticalScale.Text
        End If
        If 90 Mod CType(cbb_TilesperMap.Text, Int16) = 0.0 Then
            LocalSettings.TilesPerMap = cbb_TilesperMap.Text
        End If
        If cbb_TerrainMapping.Text = "Default" Or cbb_TerrainMapping.Text = "Custom" Then
            LocalSettings.Terrain = cbb_TerrainMapping.Text
        End If
        If cbb_MapVersion.Text = "1.12" Or cbb_MapVersion.Text = "1.16" Or cbb_MapVersion.Text = "1.17" Or cbb_MapVersion.Text = "1.18" Or cbb_MapVersion.Text = "1.19" Then
            LocalSettings.MapVersion = cbb_MapVersion.Text
        End If

        LocalSettings.bordersBoolean = CBool(chb_Borders.IsChecked)

        If cbb_Borders.Text = "2000bc" Or cbb_Borders.Text = "1000bc" Or cbb_Borders.Text = "500bc" Or cbb_Borders.Text = "323bc" Or cbb_Borders.Text = "200bc" Or cbb_Borders.Text = "1bc" Or cbb_Borders.Text = "400" Or cbb_Borders.Text = "600" Or cbb_Borders.Text = "800" Or cbb_Borders.Text = "1000" Or cbb_Borders.Text = "1279" Or cbb_Borders.Text = "1482" Or cbb_Borders.Text = "1530" Or cbb_Borders.Text = "1650" Or cbb_Borders.Text = "1715" Or cbb_Borders.Text = "1783" Or cbb_Borders.Text = "1815" Or cbb_Borders.Text = "1880" Or cbb_Borders.Text = "1914" Or cbb_Borders.Text = "1920" Or cbb_Borders.Text = "1938" Or cbb_Borders.Text = "1945" Or cbb_Borders.Text = "1994" Or cbb_Borders.Text = "Current" Then
            LocalSettings.borders = cbb_Borders.Text
        End If

        LocalSettings.Heightmap_Error_Correction = CBool(chb_HeightmapErrorCorrection.IsChecked)

        LocalSettings.geofabrik = CBool(chb_geofabrik.IsChecked)
        LocalSettings.bathymetry = CBool(chb_bathymetry.IsChecked)

        If cbb_TerrainSource.Text = "Offline Terrain (high res)" Or cbb_TerrainSource.Text = "Offline Terrain (low res)" Or cbb_TerrainSource.Text = "Arcgis" Or cbb_TerrainSource.Text = "Google" Or cbb_TerrainSource.Text = "Bing" Then
            LocalSettings.TerrainSource = cbb_TerrainSource.Text
        End If

        If cbb_BiomeSource.Text = "Terrestrial Ecoregions (WWF)" Or cbb_BiomeSource.Text = "Köppen Climate Classification" Then
            LocalSettings.biomeSource = cbb_BiomeSource.Text
        End If

        LocalSettings.highways = CBool(chb_highways.IsChecked)
        LocalSettings.streets = CBool(chb_streets.IsChecked)
        LocalSettings.buildings = CBool(chb_buildings.IsChecked)
        LocalSettings.ores = CBool(chb_ores.IsChecked)
        LocalSettings.netherite = CBool(chb_netherrite.IsChecked)
        LocalSettings.small_streets = CBool(chb_small_streets.IsChecked)
        LocalSettings.farms = CBool(chb_farms.IsChecked)
        LocalSettings.meadows = CBool(chb_meadows.IsChecked)
        LocalSettings.quarrys = CBool(chb_quarrys.IsChecked)
        LocalSettings.forests = CBool(chb_forest.IsChecked)
        LocalSettings.aerodrome = CBool(chb_aerodrome.IsChecked)
        LocalSettings.mobSpawner = CBool(chb_mob_spawner.IsChecked)
        LocalSettings.animalSpawner = CBool(chb_animal_spawner.IsChecked)
        LocalSettings.riversBoolean = CBool(chb_rivers.IsChecked)
        LocalSettings.streams = CBool(chb_streams.IsChecked)
        LocalSettings.volcanos = CBool(chb_volcanos.IsChecked)
        LocalSettings.shrubs = CBool(chb_shrubs.IsChecked)
        LocalSettings.crops = CBool(chb_crops.IsChecked)

        If cbb_Rivers.Text = "small" Or cbb_Rivers.Text = "medium" Or cbb_Rivers.Text = "large" Or cbb_Rivers.Text = "major" Then
            LocalSettings.rivers = cbb_Rivers.Text
        End If

        LocalSettings.vanillaPopulation = CBool(chb_VanillaPopulation.IsChecked)

        If CType(cbb_NumberOfCores.Text, Int32) <= 16 Then
            LocalSettings.NumberOfCores = cbb_NumberOfCores.Text
        End If

        LocalSettings.ParallelWorldPainterGenerations = CBool(chb_ParallelWorldPainterGenerations.IsChecked)

        LocalSettings.keepPbfFile = CBool(chb_keepPbfFiles.IsChecked)
        LocalSettings.reUsePbfFile = CBool(chb_reUsePbfFiles.IsChecked)
        LocalSettings.keepOsmFiles = CBool(chb_keepOsmFiles.IsChecked)
        LocalSettings.reUseOsmFiles = CBool(chb_reUseOsmFiles.IsChecked)
        LocalSettings.keepImageFiles = CBool(chb_keepImages.IsChecked)
        LocalSettings.reUseImageFiles = CBool(chb_reUseImages.IsChecked)
        LocalSettings.keepWorldPainterFiles = CBool(chb_keepWorldPainter.IsChecked)

        LocalSettings.cmdVisibility = CBool(chb_CmdVisibility.IsChecked)
        LocalSettings.continueGeneration = CBool(chb_continue.IsChecked)

        LocalSettings.Proxy = txb_Proxy.Text

        If cbb_MapOffset.Text = "-1" Or cbb_MapOffset.Text = "0" Or cbb_MapOffset.Text = "1" Then
            LocalSettings.mapOffset = cbb_MapOffset.Text
        End If

        LocalSettings.OverpassURL = txb_OsmURL.Text

        Return LocalSettings
    End Function

    Private Sub Calculate_Scale()
        If Not cbb_BlocksPerTile Is Nothing And Not cbb_TilesperMap Is Nothing Then
            If Not cbb_BlocksPerTile.Text = "" And Not cbb_TilesperMap.Text = "" Then
                Dim ScaleCalc As Double = (CType(cbb_BlocksPerTile.Text, Int16)) / 1.024

                lbl_ScaleQuantity.Content = "1 : " & (Math.Round(40075000 / ((CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesPerMap.Text, Int16)))), 1)).ToString
                lbl_ScaleRoundedQuantity.Content = "1 : " & (Math.Round(36768000 / ((CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesPerMap.Text, Int16)))), 1)).ToString

                If 36768000 / (CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesperMap.Text, Int16))) >= 500 Then
                    chb_small_streets.IsEnabled = False
                    chb_small_streets.IsChecked = False
                    chb_farms.IsEnabled = False
                    chb_farms.IsChecked = False
                    chb_meadows.IsEnabled = False
                    chb_meadows.IsChecked = False
                    chb_quarrys.IsEnabled = False
                    chb_quarrys.IsChecked = False
                    chb_forest.IsEnabled = False
                    chb_forest.IsChecked = False
                    chb_streams.IsEnabled = False
                    chb_streams.IsChecked = False
                    lbl_Warning.Content = "Some features are only available on larger scales."
                Else
                    chb_small_streets.IsEnabled = True
                    chb_farms.IsEnabled = True
                    chb_meadows.IsEnabled = True
                    chb_quarrys.IsEnabled = True
                    chb_forest.IsEnabled = True
                    chb_streams.IsEnabled = True
                    lbl_Warning.Content = ""
                End If
            End If
        End If

        If Not cbb_TilesPerMap.Text = "1" Then
            For Each item As ComboBoxItem In cbb_VerticalScale.Items
                If item.Content.ToString = "5" Or item.Content.ToString = "10" Or item.Content.ToString = "25" Then
                    item.IsEnabled = False
                End If
            Next
        Else
            For Each item As ComboBoxItem In cbb_VerticalScale.Items
                item.IsEnabled = True
            Next
        End If
    End Sub

    Public Shared Function RemoveIllegalFileNameChars(input As String, Optional replacement As String = "_") As String
        Dim regexSearch = New String(Path.GetInvalidFileNameChars()) & New String(Path.GetInvalidPathChars())
        Dim r = New Regex(String.Format("[{0}]", Regex.Escape(regexSearch)))
        Dim returnString As String = ""
        returnString = r.Replace(input, replacement)
        returnString = returnString.Replace(" ", replacement)
        Return returnString
    End Function

    Private Sub ReUseChanged()
        If chb_reUsePbfFiles.IsChecked = True Or chb_reUseOsmFiles.IsChecked = True Or chb_reUseImages.IsChecked = True Then
            lbl_reuse_warning.Visibility = Visibility.Visible
        Else
            lbl_reuse_warning.Visibility = Visibility.Hidden
        End If
    End Sub

    Private Sub cbb_MapVersion_DropDownClosed(sender As Object, e As EventArgs) Handles cbb_MapVersion.DropDownClosed
        If cbb_MapVersion.Text = "1.12" Or cbb_MapVersion.Text = "1.18" Or cbb_MapVersion.Text = "1.19" Then
            chb_VanillaPopulation.IsEnabled = True
        Else
            chb_VanillaPopulation.IsEnabled = False
            chb_VanillaPopulation.IsChecked = False
        End If
    End Sub

#End Region

End Class

Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports System.Runtime.InteropServices
Imports System.Net.Mail
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports MinecraftEarthTiles_Core

Public Class SettingsWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        ApplyTheme()
        Settings_To_GUI(ClassWorker.GetWorldSettings, ClassWorker.GetTilesSettings)
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

    Private Sub Reset_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim ResetWorldSettings As New Settings
        Dim ResetTilesSettings As New TilesSettings
        Settings_To_GUI(ResetWorldSettings, ResetTilesSettings)
    End Sub

    Private Sub Load_World_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalWorldSettings As New Settings
        Dim LoadSettingsFileDialog As New OpenFileDialog With {
            .FileName = "settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If LoadSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                LocalWorldSettings = ClassWorker.LoadWorldSettingsFromFile(LoadSettingsFileDialog.FileName)
                Settings_To_GUI(LocalWorldSettings, GUI_To_TilesSettings)
                Dim MessageBox As New MessageBoxWindow("World Settings loaded from file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow("Error while Loading XML file. " & ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Load_Tiles_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalTilesSettings As New TilesSettings
        Dim LoadSettingsFileDialog As New OpenFileDialog With {
            .FileName = "tiles_settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If LoadSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                LocalTilesSettings = ClassWorker.LoadTilesSettingsFromFile(LoadSettingsFileDialog.FileName)
                Settings_To_GUI(GUI_To_WorldSettings, LocalTilesSettings)
                Dim MessageBox As New MessageBoxWindow("Tiles Settings loaded from file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow("Error while Loading XML file. " & ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Save_World_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim LocalWorldSettings As Settings = GUI_To_WorldSettings()
        Dim SaveSettingsFileDialog As New SaveFileDialog With {
            .FileName = "custom_settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If SaveSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                CustomXmlSerialiser.SaveXML(SaveSettingsFileDialog.FileName, LocalWorldSettings)
                Dim MessageBox As New MessageBoxWindow("World Settings saved to file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Save_Tiles_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim localTilesSettings As TilesSettings = GUI_To_TilesSettings()
        Dim SaveSettingsFileDialog As New SaveFileDialog With {
            .FileName = "custom_tiles_settings.xml",
            .Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
            .FilterIndex = 1
        }
        If SaveSettingsFileDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                CustomXmlSerialiser.SaveXML(SaveSettingsFileDialog.FileName, localTilesSettings)
                Dim MessageBox As New MessageBoxWindow("Tiles Settings saved to file.")
                MessageBox.ShowDialog()
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        Process.Start("https://earth.motfe.net/tiles-settings/")
    End Sub

#End Region

#Region "Path Dialogs"

    Private Sub Btn_PathToScriptFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = True,
            .InitialDirectory = My.Application.Info.DirectoryPath
        }
        If MyFolderBrowserDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToScriptsFolder.Text = MyFolderBrowserDialog.FileName
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToWorldPainter_Click(sender As Object, e As RoutedEventArgs)
        Dim WorldPainterScriptFileDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = False,
            .DefaultFileName = "wpscript.exe",
            .InitialDirectory = "C:\Program Files\"
        }
        WorldPainterScriptFileDialog.Filters.Add(New CommonFileDialogFilter("Executables", "exe"))
        If WorldPainterScriptFileDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToWorldPainterFile.Text = WorldPainterScriptFileDialog.FileName
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToQGIS_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New CommonOpenFileDialog With {
            .InitialDirectory = "C:\Program Files\",
            .IsFolderPicker = True
        }
        If MyFolderBrowserDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            If My.Computer.FileSystem.FileExists(MyFolderBrowserDialog.FileName & "\bin\qgis-bin.exe") Or My.Computer.FileSystem.FileExists(MyFolderBrowserDialog.FileName & "\bin\qgis-ltr-bin.exe") Then
                txb_PathToQGIS.Text = MyFolderBrowserDialog.FileName
            Else
                Dim MessageBox As New MessageBoxWindow("QGIS not found. Maybe it is in another folder!")
                MessageBox.ShowDialog()
            End If
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToMagick_Click(sender As Object, e As RoutedEventArgs)
        Dim MagickFileDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = False,
            .DefaultFileName = "magick.exe",
            .InitialDirectory = "C:\Program Files\"
        }
        MagickFileDialog.Filters.Add(New CommonFileDialogFilter("Executables", "exe"))
        If MagickFileDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToMagick.Text = MagickFileDialog.FileName
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToMinutor_Click(sender As Object, e As RoutedEventArgs)
        Dim MinutorFileDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = False,
            .DefaultFileName = "minutor.exe",
            .InitialDirectory = "C:\Program Files\"
        }
        MinutorFileDialog.Filters.Add(New CommonFileDialogFilter("Executables", "exe"))
        If MinutorFileDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToMinutor.Text = MinutorFileDialog.FileName
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToPBF_Click(sender As Object, e As RoutedEventArgs)
        Dim PBFFileDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = False,
            .DefaultExtension = "pbf"
        }
        PBFFileDialog.Filters.Add(New CommonFileDialogFilter("Geofabrik Files", "pbf"))
        If PBFFileDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToPBF.Text = PBFFileDialog.FileName
            chb_geofabrik.IsChecked = True
        End If
        Me.Focus()
    End Sub

    Private Sub Btn_PathToExport_Click(sender As Object, e As RoutedEventArgs)
        Dim MyFolderBrowserDialog As New CommonOpenFileDialog With {
            .IsFolderPicker = True,
            .DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        }
        If MyFolderBrowserDialog.ShowDialog() = CommonFileDialogResult.Ok Then
            txb_PathToExport.Text = MyFolderBrowserDialog.FileName
        End If
        Me.Focus()
    End Sub

#End Region

#Region "Save/Cancel"

    Private Sub Save_Close_Click(sender As Object, e As RoutedEventArgs)
        Save_Click(sender, e)
        Close_Click(sender, e)
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            If Not ClassWorker.GetWorldSettings.TilesPerMap = cbb_TilesPerMap.Text Then
                ClassWorker.SetSelection(New Selection)
            End If
            ClassWorker.SetWorldSettings(GUI_To_WorldSettings())
            ClassWorker.SetTilesSettings(GUI_To_TilesSettings())
            Try
                ClassWorker.SaveWorldSettingsToFile(ClassWorker.GetWorldSettings, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/settings.xml")
                ClassWorker.SaveTilesSettingsToFile(ClassWorker.GetTilesSettings, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/tiles_settings.xml")
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

    Private Sub Settings_To_GUI(WorldSettings As Settings, TilesSettings As TilesSettings)

        txb_PathToScriptsFolder.Text = TilesSettings.PathToScriptsFolder
        If WorldSettings.PathToScriptsFolder IsNot Nothing Then
            txb_PathToScriptsFolder.Text = WorldSettings.PathToScriptsFolder
        End If
        txb_PathToWorldPainterFile.Text = TilesSettings.PathToWorldPainterFolder
        If WorldSettings.PathToWorldPainterFolder IsNot Nothing Then
            txb_PathToWorldPainterFile.Text = WorldSettings.PathToWorldPainterFolder
        End If
        txb_PathToQGIS.Text = TilesSettings.PathToQGIS
        If WorldSettings.PathToQGIS IsNot Nothing Then
            txb_PathToQGIS.Text = WorldSettings.PathToQGIS
        End If
        txb_PathToMagick.Text = TilesSettings.PathToMagick
        If WorldSettings.PathToMagick IsNot Nothing Then
            txb_PathToMagick.Text = WorldSettings.PathToMagick
        End If
        txb_PathToMinutor.Text = TilesSettings.PathToMinutor
        If WorldSettings.PathToMinutor IsNot Nothing Then
            txb_PathToMinutor.Text = WorldSettings.PathToMinutor
        End If
        txb_PathToPBF.Text = WorldSettings.PathToPBF
        txb_PathToExport.Text = TilesSettings.PathToExport
        If WorldSettings.PathToExport IsNot Nothing Then
            txb_PathToExport.Text = WorldSettings.PathToExport
        End If
        If Not WorldSettings.WorldName = "" Then
            txb_WorldName.Text = RemoveIllegalFileNameChars(WorldSettings.WorldName)
        Else
            txb_WorldName.Text = "world"
        End If

        If CType(WorldSettings.BlocksPerTile, Int16) Mod 512 = 0.0 Then
            cbb_BlocksPerTile.SelectedValue = WorldSettings.BlocksPerTile
            cbb_BlocksPerTile.Text = WorldSettings.BlocksPerTile
        End If

        If WorldSettings.VerticalScale = "5" Or WorldSettings.VerticalScale = "10" Or WorldSettings.VerticalScale = "25" Or WorldSettings.VerticalScale = "35" Or WorldSettings.VerticalScale = "50" Or WorldSettings.VerticalScale = "75" Or WorldSettings.VerticalScale = "100" Or WorldSettings.VerticalScale = "200" Or WorldSettings.VerticalScale = "300" Then
            cbb_VerticalScale.SelectedValue = WorldSettings.VerticalScale
            cbb_VerticalScale.Text = WorldSettings.VerticalScale
            If Not WorldSettings.TilesPerMap = "1" And (WorldSettings.VerticalScale = "5" Or WorldSettings.VerticalScale = "10" Or WorldSettings.VerticalScale = "25") Then
                cbb_VerticalScale.SelectedValue = "35"
                cbb_VerticalScale.Text = "35"
            End If
        End If

        If WorldSettings.Terrain = "Standard" Or WorldSettings.Terrain = "Custom" Then
            cbb_TerrainMapping.SelectedValue = WorldSettings.Terrain
            cbb_TerrainMapping.Text = WorldSettings.Terrain
        End If

        If 90 Mod CType(WorldSettings.TilesPerMap, Int16) = 0.0 Then
            cbb_TilesPerMap.SelectedValue = WorldSettings.TilesPerMap
            cbb_TilesPerMap.Text = WorldSettings.TilesPerMap
        End If

        If WorldSettings.MapVersion = "1.12" Or WorldSettings.MapVersion = "1.16" Or WorldSettings.MapVersion = "1.17" Or WorldSettings.MapVersion = "1.18" Or WorldSettings.MapVersion = "1.19" Then
            cbb_MapVersion.SelectedValue = WorldSettings.MapVersion
            cbb_MapVersion.Text = WorldSettings.MapVersion
        End If

        If WorldSettings.MapVersion = "1.12" Or WorldSettings.MapVersion = "1.18" Or WorldSettings.MapVersion = "1.19" Then
            chb_VanillaPopulation.IsEnabled = True
        Else
            chb_VanillaPopulation.IsEnabled = False
        End If

        chb_Borders.IsChecked = WorldSettings.bordersBoolean

        If WorldSettings.borders = "2000bc" Or WorldSettings.borders = "1000bc" Or WorldSettings.borders = "500bc" Or WorldSettings.borders = "323bc" Or WorldSettings.borders = "200bc" Or WorldSettings.borders = "1bc" Or WorldSettings.borders = "400" Or WorldSettings.borders = "600" Or WorldSettings.borders = "800" Or WorldSettings.borders = "1000" Or WorldSettings.borders = "1279" Or WorldSettings.borders = "1482" Or WorldSettings.borders = "1530" Or WorldSettings.borders = "1650" Or WorldSettings.borders = "1715" Or WorldSettings.borders = "1783" Or WorldSettings.borders = "1815" Or WorldSettings.borders = "1880" Or WorldSettings.borders = "1914" Or WorldSettings.borders = "1920" Or WorldSettings.borders = "1938" Or WorldSettings.borders = "1945" Or WorldSettings.borders = "1994" Or WorldSettings.borders = "Current" Then
            cbb_Borders.SelectedValue = WorldSettings.borders
            cbb_Borders.Text = WorldSettings.borders
        End If

        chb_HeightmapErrorCorrection.IsChecked = WorldSettings.Heightmap_Error_Correction

        chb_geofabrik.IsChecked = WorldSettings.geofabrik

        chb_Bathymetry.IsChecked = WorldSettings.bathymetry

        If WorldSettings.TerrainSource = "Offline Terrain (high res)" Or WorldSettings.TerrainSource = "Offline Terrain (low res)" Or WorldSettings.TerrainSource = "Arcgis" Or WorldSettings.TerrainSource = "Google" Or WorldSettings.TerrainSource = "Bing" Then
            cbb_TerrainSource.SelectedValue = WorldSettings.TerrainSource
            cbb_TerrainSource.Text = WorldSettings.TerrainSource
        End If

        If WorldSettings.biomeSource = "Köppen Climate Classification" Or WorldSettings.biomeSource = "Terrestrial Ecoregions (WWF)" Then
            cbb_BiomeSource.SelectedValue = WorldSettings.biomeSource
            cbb_BiomeSource.Text = WorldSettings.biomeSource
        End If

        chb_highways.IsChecked = WorldSettings.highways

        chb_streets.IsChecked = WorldSettings.streets

        chb_small_streets.IsChecked = WorldSettings.small_streets

        chb_buildings.IsChecked = WorldSettings.buildings

        chb_ores.IsChecked = WorldSettings.ores

        chb_netherrite.IsChecked = WorldSettings.netherite

        chb_farms.IsChecked = WorldSettings.farms

        chb_meadows.IsChecked = WorldSettings.meadows

        chb_quarrys.IsChecked = WorldSettings.quarrys

        chb_forest.IsChecked = WorldSettings.forests

        chb_aerodrome.IsChecked = WorldSettings.aerodrome

        chb_mob_spawner.IsChecked = WorldSettings.mobSpawner

        chb_animal_spawner.IsChecked = WorldSettings.animalSpawner

        chb_rivers.IsChecked = WorldSettings.riversBoolean

        If WorldSettings.rivers = "All (small)" Or WorldSettings.rivers = "All (medium)" Or WorldSettings.rivers = "All (large)" Or WorldSettings.rivers = "Major" Or WorldSettings.rivers = "Major + Minor" Then
            cbb_Rivers.SelectedValue = WorldSettings.rivers
            cbb_Rivers.Text = WorldSettings.rivers
        End If
        If WorldSettings.rivers = "small" Then
            cbb_Rivers.SelectedValue = "All (small)"
            cbb_Rivers.Text = "All (small)"
        End If
        If WorldSettings.rivers = "medium" Then
            cbb_Rivers.SelectedValue = "All (medium)"
            cbb_Rivers.Text = "All (medium)"
        End If
        If WorldSettings.rivers = "large" Then
            cbb_Rivers.SelectedValue = "All (large)"
            cbb_Rivers.Text = "All (large)"
        End If
        If WorldSettings.rivers = "major" Then
            cbb_Rivers.SelectedValue = "Major"
            cbb_Rivers.Text = "Major"
        End If

        If WorldSettings.MapVersion = "1.12" Or WorldSettings.MapVersion = "1.18" Or WorldSettings.MapVersion = "1.19" Then
            chb_VanillaPopulation.IsChecked = WorldSettings.vanillaPopulation
        Else
            chb_VanillaPopulation.IsChecked = False
        End If

        chb_streams.IsChecked = WorldSettings.streams

        chb_volcanos.IsChecked = WorldSettings.volcanos
        chb_shrubs.IsChecked = WorldSettings.shrubs
        chb_crops.IsChecked = WorldSettings.crops

        If CType(TilesSettings.NumberOfCores, Int32) <= 16 Then
            cbb_NumberOfCores.SelectedValue = TilesSettings.NumberOfCores
            cbb_NumberOfCores.Text = TilesSettings.NumberOfCores
        End If
        If WorldSettings.NumberOfCores IsNot Nothing Then
            If CType(WorldSettings.NumberOfCores, Int32) <= 16 Then
                cbb_NumberOfCores.SelectedValue = WorldSettings.NumberOfCores
                cbb_NumberOfCores.Text = WorldSettings.NumberOfCores
            End If
        End If

        chb_ParallelWorldPainterGenerations.IsChecked = TilesSettings.ParallelWorldPainterGenerations
        If WorldSettings.ParallelWorldPainterGenerations IsNot Nothing Then
            chb_ParallelWorldPainterGenerations.IsChecked = WorldSettings.ParallelWorldPainterGenerations
        End If

        chb_keepPbfFiles.IsChecked = TilesSettings.keepPbfFile
        If WorldSettings.keepPbfFile IsNot Nothing Then
            chb_keepPbfFiles.IsChecked = WorldSettings.keepPbfFile
        End If
        chb_reUsePbfFiles.IsChecked = TilesSettings.reUsePbfFile
        If WorldSettings.reUsePbfFile IsNot Nothing Then
            chb_reUsePbfFiles.IsChecked = WorldSettings.reUsePbfFile
        End If
        chb_keepOsmFiles.IsChecked = TilesSettings.keepOsmFiles
        If WorldSettings.keepOsmFiles IsNot Nothing Then
            chb_keepOsmFiles.IsChecked = WorldSettings.keepOsmFiles
        End If
        chb_reUseOsmFiles.IsChecked = TilesSettings.reUseOsmFiles
        If WorldSettings.reUseOsmFiles IsNot Nothing Then
            chb_reUseOsmFiles.IsChecked = WorldSettings.reUseOsmFiles
        End If
        chb_keepImages.IsChecked = TilesSettings.keepImageFiles
        If WorldSettings.keepImageFiles IsNot Nothing Then
            chb_keepImages.IsChecked = WorldSettings.keepImageFiles
        End If
        chb_reUseImages.IsChecked = TilesSettings.reUseImageFiles
        If WorldSettings.reUseImageFiles IsNot Nothing Then
            chb_reUseImages.IsChecked = WorldSettings.reUseImageFiles
        End If
        chb_keepWorldPainter.IsChecked = TilesSettings.keepWorldPainterFiles
        If WorldSettings.keepWorldPainterFiles IsNot Nothing Then
            chb_keepWorldPainter.IsChecked = WorldSettings.keepWorldPainterFiles
        End If
        chb_minutor.IsChecked = TilesSettings.minutor
        If WorldSettings.minutor IsNot Nothing Then
            chb_minutor.IsChecked = WorldSettings.minutor
        End If

        chb_CmdVisibility.IsChecked = TilesSettings.cmdVisibility
        If WorldSettings.cmdVisibility IsNot Nothing Then
            chb_CmdVisibility.IsChecked = WorldSettings.cmdVisibility
        End If
        chb_CmdPause.IsChecked = TilesSettings.cmdPause
        If WorldSettings.cmdPause IsNot Nothing Then
            chb_CmdPause.IsChecked = WorldSettings.cmdPause
        End If

        chb_continue.IsChecked = TilesSettings.continueGeneration
        If WorldSettings.continueGeneration IsNot Nothing Then
            chb_CmdPause.IsChecked = WorldSettings.continueGeneration
        End If

        chb_closeAfterFinish.IsChecked = TilesSettings.closeAfterFinish

        txb_Proxy.Text = TilesSettings.Proxy
        If WorldSettings.Proxy IsNot Nothing Then
            txb_Proxy.Text = WorldSettings.Proxy
        End If

        If StartupWindow.MyVersion = "Full" Then
            chb_alertAfterFinish.IsChecked = TilesSettings.alertAfterFinish
            Try
                Dim localMail As New MailAddress(TilesSettings.alertMail)
                txb_AlertMail.Text = localMail.Address
            Catch ex As Exception
            End Try
        Else
            chb_alertAfterFinish.IsChecked = False
            chb_alertAfterFinish.IsEnabled = False
            txb_AlertMail.Text = ""
            txb_AlertMail.IsEnabled = False
            txb_AlertMailFullVersion.Text = "Alerts are only available in the Full version."
        End If

        If WorldSettings.mapOffset = "-1" Or WorldSettings.mapOffset = "0" Or WorldSettings.mapOffset = "1" Then
            cbb_MapOffset.SelectedValue = WorldSettings.mapOffset
            cbb_MapOffset.Text = WorldSettings.mapOffset
        End If

        txb_OsmURL.Text = WorldSettings.OverpassURL

        chb_bop.IsChecked = WorldSettings.mod_BOP

        chb_byg.IsChecked = WorldSettings.mod_BYG

        chb_terralith.IsChecked = WorldSettings.mod_Terralith

        chb_create.IsChecked = WorldSettings.mod_Create

        txb_custom_layers.Text = WorldSettings.custom_layers

        Calculate_Scale()

    End Sub

    Private Function GUI_To_WorldSettings() As Settings
        Dim LocalWorldSettings As New Settings
        If File.Exists(txb_PathToPBF.Text) Then
            LocalWorldSettings.PathToPBF = txb_PathToPBF.Text
        End If
        If Not txb_WorldName.Text = "" Then
            LocalWorldSettings.WorldName = RemoveIllegalFileNameChars(txb_WorldName.Text)
            txb_WorldName.Text = RemoveIllegalFileNameChars(txb_WorldName.Text)
        End If
        If CType(cbb_BlocksPerTile.Text, Int16) Mod 512 = 0.0 Then
            LocalWorldSettings.BlocksPerTile = cbb_BlocksPerTile.Text
        End If
        If cbb_VerticalScale.Text = "300" Or cbb_VerticalScale.Text = "200" Or cbb_VerticalScale.Text = "100" Or cbb_VerticalScale.Text = "75" Or cbb_VerticalScale.Text = "50" Or cbb_VerticalScale.Text = "35" Or cbb_VerticalScale.Text = "25" Or cbb_VerticalScale.Text = "10" Or cbb_VerticalScale.Text = "5" Then
            LocalWorldSettings.VerticalScale = cbb_VerticalScale.Text
        End If
        If 90 Mod CType(cbb_TilesPerMap.Text, Int16) = 0.0 Then
            LocalWorldSettings.TilesPerMap = cbb_TilesPerMap.Text
        End If
        If cbb_TerrainMapping.Text = "Default" Or cbb_TerrainMapping.Text = "Custom" Then
            LocalWorldSettings.Terrain = cbb_TerrainMapping.Text
        End If
        If cbb_MapVersion.Text = "1.12" Or cbb_MapVersion.Text = "1.16" Or cbb_MapVersion.Text = "1.17" Or cbb_MapVersion.Text = "1.18" Or cbb_MapVersion.Text = "1.19" Then
            LocalWorldSettings.MapVersion = cbb_MapVersion.Text
        End If

        LocalWorldSettings.bordersBoolean = CBool(chb_Borders.IsChecked)

        If cbb_Borders.Text = "2000bc" Or cbb_Borders.Text = "1000bc" Or cbb_Borders.Text = "500bc" Or cbb_Borders.Text = "323bc" Or cbb_Borders.Text = "200bc" Or cbb_Borders.Text = "1bc" Or cbb_Borders.Text = "400" Or cbb_Borders.Text = "600" Or cbb_Borders.Text = "800" Or cbb_Borders.Text = "1000" Or cbb_Borders.Text = "1279" Or cbb_Borders.Text = "1482" Or cbb_Borders.Text = "1530" Or cbb_Borders.Text = "1650" Or cbb_Borders.Text = "1715" Or cbb_Borders.Text = "1783" Or cbb_Borders.Text = "1815" Or cbb_Borders.Text = "1880" Or cbb_Borders.Text = "1914" Or cbb_Borders.Text = "1920" Or cbb_Borders.Text = "1938" Or cbb_Borders.Text = "1945" Or cbb_Borders.Text = "1994" Or cbb_Borders.Text = "Current" Then
            LocalWorldSettings.borders = cbb_Borders.Text
        End If

        LocalWorldSettings.Heightmap_Error_Correction = CBool(chb_HeightmapErrorCorrection.IsChecked)

        LocalWorldSettings.geofabrik = CBool(chb_geofabrik.IsChecked)
        LocalWorldSettings.bathymetry = CBool(chb_Bathymetry.IsChecked)

        If cbb_TerrainSource.Text = "Offline Terrain (high res)" Or cbb_TerrainSource.Text = "Offline Terrain (low res)" Or cbb_TerrainSource.Text = "Arcgis" Or cbb_TerrainSource.Text = "Google" Or cbb_TerrainSource.Text = "Bing" Then
            LocalWorldSettings.TerrainSource = cbb_TerrainSource.Text
        End If

        If cbb_BiomeSource.Text = "Terrestrial Ecoregions (WWF)" Or cbb_BiomeSource.Text = "Köppen Climate Classification" Then
            LocalWorldSettings.biomeSource = cbb_BiomeSource.Text
        End If

        LocalWorldSettings.highways = CBool(chb_highways.IsChecked)
        LocalWorldSettings.streets = CBool(chb_streets.IsChecked)
        LocalWorldSettings.buildings = CBool(chb_buildings.IsChecked)
        LocalWorldSettings.ores = CBool(chb_ores.IsChecked)
        LocalWorldSettings.netherite = CBool(chb_netherrite.IsChecked)
        LocalWorldSettings.small_streets = CBool(chb_small_streets.IsChecked)
        LocalWorldSettings.farms = CBool(chb_farms.IsChecked)
        LocalWorldSettings.meadows = CBool(chb_meadows.IsChecked)
        LocalWorldSettings.quarrys = CBool(chb_quarrys.IsChecked)
        LocalWorldSettings.forests = CBool(chb_forest.IsChecked)
        LocalWorldSettings.aerodrome = CBool(chb_aerodrome.IsChecked)
        LocalWorldSettings.mobSpawner = CBool(chb_mob_spawner.IsChecked)
        LocalWorldSettings.animalSpawner = CBool(chb_animal_spawner.IsChecked)
        LocalWorldSettings.riversBoolean = CBool(chb_rivers.IsChecked)
        LocalWorldSettings.streams = CBool(chb_streams.IsChecked)
        LocalWorldSettings.volcanos = CBool(chb_volcanos.IsChecked)
        LocalWorldSettings.shrubs = CBool(chb_shrubs.IsChecked)
        LocalWorldSettings.crops = CBool(chb_crops.IsChecked)

        If cbb_Rivers.Text = "All (small)" Or cbb_Rivers.Text = "All (medium)" Or cbb_Rivers.Text = "All (large)" Or cbb_Rivers.Text = "Major" Or cbb_Rivers.Text = "Major + Minor" Then
            LocalWorldSettings.rivers = cbb_Rivers.Text
        End If

        LocalWorldSettings.vanillaPopulation = CBool(chb_VanillaPopulation.IsChecked)

        If cbb_MapOffset.Text = "-1" Or cbb_MapOffset.Text = "0" Or cbb_MapOffset.Text = "1" Then
            LocalWorldSettings.mapOffset = cbb_MapOffset.Text
        End If

        LocalWorldSettings.OverpassURL = txb_OsmURL.Text

        LocalWorldSettings.mod_BOP = CBool(chb_bop.IsChecked)

        LocalWorldSettings.mod_BYG = CBool(chb_byg.IsChecked)

        LocalWorldSettings.mod_Terralith = CBool(chb_terralith.IsChecked)

        LocalWorldSettings.mod_Create = CBool(chb_create.IsChecked)

        LocalWorldSettings.custom_layers = txb_custom_layers.Text

        Return LocalWorldSettings
    End Function

    Private Function GUI_To_TilesSettings() As TilesSettings
        Dim LocalTilesSettings As New TilesSettings
        If Directory.Exists(txb_PathToScriptsFolder.Text) Then
            LocalTilesSettings.PathToScriptsFolder = txb_PathToScriptsFolder.Text
        End If
        If File.Exists(txb_PathToWorldPainterFile.Text) Then
            LocalTilesSettings.PathToWorldPainterFolder = txb_PathToWorldPainterFile.Text
        End If
        If Directory.Exists(txb_PathToQGIS.Text) Then
            LocalTilesSettings.PathToQGIS = txb_PathToQGIS.Text
        End If
        If File.Exists(txb_PathToMagick.Text) Then
            LocalTilesSettings.PathToMagick = txb_PathToMagick.Text
        End If
        If File.Exists(txb_PathToMinutor.Text) Then
            LocalTilesSettings.PathToMinutor = txb_PathToMinutor.Text
        End If
        If Directory.Exists(txb_PathToExport.Text) Then
            LocalTilesSettings.PathToExport = txb_PathToExport.Text
        End If
        LocalTilesSettings.Theme = ClassWorker.GetTilesSettings.Theme

        If CType(cbb_NumberOfCores.Text, Int32) <= 16 Then
            LocalTilesSettings.NumberOfCores = cbb_NumberOfCores.Text
        End If

        LocalTilesSettings.ParallelWorldPainterGenerations = CBool(chb_ParallelWorldPainterGenerations.IsChecked)

        LocalTilesSettings.keepPbfFile = CBool(chb_keepPbfFiles.IsChecked)
        LocalTilesSettings.reUsePbfFile = CBool(chb_reUsePbfFiles.IsChecked)
        LocalTilesSettings.keepOsmFiles = CBool(chb_keepOsmFiles.IsChecked)
        LocalTilesSettings.reUseOsmFiles = CBool(chb_reUseOsmFiles.IsChecked)
        LocalTilesSettings.keepImageFiles = CBool(chb_keepImages.IsChecked)
        LocalTilesSettings.reUseImageFiles = CBool(chb_reUseImages.IsChecked)
        LocalTilesSettings.keepWorldPainterFiles = CBool(chb_keepWorldPainter.IsChecked)
        LocalTilesSettings.minutor = CBool(chb_minutor.IsChecked)

        LocalTilesSettings.cmdVisibility = CBool(chb_CmdVisibility.IsChecked)
        LocalTilesSettings.cmdPause = CBool(chb_CmdPause.IsChecked)
        LocalTilesSettings.continueGeneration = CBool(chb_continue.IsChecked)
        LocalTilesSettings.closeAfterFinish = CBool(chb_closeAfterFinish.IsChecked)

        LocalTilesSettings.Proxy = txb_Proxy.Text

        If StartupWindow.MyVersion = "Full" Then
            LocalTilesSettings.alertAfterFinish = CBool(chb_alertAfterFinish.IsChecked)
            Try
                Dim localMail As New MailAddress(txb_AlertMail.Text)
                LocalTilesSettings.alertMail = localMail.Address
            Catch ex As Exception
            End Try
        Else
            LocalTilesSettings.alertAfterFinish = False
            LocalTilesSettings.alertMail = ""
        End If

        Dim assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
        LocalTilesSettings.version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"

        Return LocalTilesSettings
    End Function

    Private Sub Calculate_Scale()
        If Not cbb_BlocksPerTile Is Nothing And Not cbb_TilesPerMap Is Nothing Then
            If Not cbb_BlocksPerTile.Text = "" And Not cbb_TilesPerMap.Text = "" Then
                Dim ScaleCalc As Double = (CType(cbb_BlocksPerTile.Text, Int16)) / 1.024

                lbl_ScaleQuantity.Content = "1:" & (Math.Round(40075000 / ((CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesPerMap.Text, Int16)))), 1)).ToString
                lbl_ScaleRoundedQuantity.Content = "1:" & (Math.Round(36768000 / ((CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesPerMap.Text, Int16)))), 1)).ToString

                If 36768000 / (CType(cbb_BlocksPerTile.Text, Int16) * (360 / CType(cbb_TilesPerMap.Text, Int16))) >= 500 Then
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
                Else
                    chb_small_streets.IsEnabled = True
                    chb_farms.IsEnabled = True
                    chb_meadows.IsEnabled = True
                    chb_quarrys.IsEnabled = True
                    chb_forest.IsEnabled = True
                    chb_streams.IsEnabled = True
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

    Private Sub cbb_MapVersion_DropDownClosed(sender As Object, e As EventArgs) Handles cbb_MapVersion.DropDownClosed
        If cbb_MapVersion.Text = "1.12" Or cbb_MapVersion.Text = "1.18" Or cbb_MapVersion.Text = "1.19" Then
            chb_VanillaPopulation.IsEnabled = True
        Else
            chb_VanillaPopulation.IsEnabled = False
            chb_VanillaPopulation.IsChecked = False
        End If
    End Sub

    Private Sub chb_bop_clicked()
        chb_byg.IsChecked = False
        chb_terralith.IsChecked = False
    End Sub

    Private Sub chb_byg_clicked()
        chb_bop.IsChecked = False
        chb_terralith.IsChecked = False
    End Sub

    Private Sub chb_terralith_clicked()
        chb_bop.IsChecked = False
        chb_byg.IsChecked = False
    End Sub

#End Region

End Class

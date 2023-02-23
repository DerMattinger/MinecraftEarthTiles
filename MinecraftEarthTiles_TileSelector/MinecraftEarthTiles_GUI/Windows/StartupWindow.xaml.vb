Imports System.IO
Imports System.Windows.Forms
Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports System.Runtime.InteropServices
Imports MinecraftEarthTiles_Core

Public Class StartupWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Public Shared Property MyTheme As String

    Public Shared Property MyVersion As String = "Demo"
    'Public Shared Property MyVersion As String = "Full"
    'Public Shared Property MyVersion As String = "Demo"

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        If File.Exists(My.Application.Info.DirectoryPath & "\settings.xml") Then
            Try
                ClassWorker.SetWorldSettings(ClassWorker.LoadWorldSettingsFromFile(My.Application.Info.DirectoryPath & "\settings.xml"))
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
        If File.Exists(My.Application.Info.DirectoryPath & "\tiles_settings.xml") Then
            Try
                ClassWorker.SetTilesSettings(ClassWorker.LoadTilesSettingsFromFile(My.Application.Info.DirectoryPath & "\tiles_settings.xml"))
                MyTheme = ClassWorker.GetTilesSettings.Theme
                If MyTheme = "Dark" Then
                    mnu_dark.IsChecked = True
                    mnu_light.IsChecked = False
                End If
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        Else
            Try
                Dim RegistryKeyPath As String = "Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
                Dim RegistryValueName As String = "AppsUseLightTheme"
                Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKeyPath)
                    Dim registryValueObject As Object = key?.GetValue(RegistryValueName)
                    If registryValueObject IsNot Nothing Then
                        Dim registryValue As Integer = CInt(registryValueObject)
                        If registryValue = 0 Then
                            Dim localTilesSettings As MinecraftEarthTiles_Core.TilesSettings = ClassWorker.GetTilesSettings
                            localTilesSettings.Theme = "Dark"
                            ClassWorker.SetTilesSettings(localTilesSettings)
                            MyTheme = "Dark"
                            mnu_dark.IsChecked = True
                            mnu_light.IsChecked = False
                            Try
                                ClassWorker.SaveTilesSettingsToFile(ClassWorker.GetTilesSettings, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/tiles_settings.xml")
                            Catch ex As Exception
                                Dim MessageBox As New MessageBoxWindow(ex.Message)
                                MessageBox.ShowDialog()
                            End Try
                        End If
                    End If
                End Using
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
        Dim LocalSelection As New Selection
        If File.Exists(My.Application.Info.DirectoryPath & "\selection.xml") Then
            Try
                ClassWorker.SetSelection(ClassWorker.LoadSelectionFromFile(My.Application.Info.DirectoryPath & "\selection.xml"))
            Catch ex As Exception
                Dim MessageBox As New MessageBoxWindow(ex.Message)
                MessageBox.ShowDialog()
            End Try
        End If
        Check()
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
                ClassWorker.GetTilesSettings.Theme = "Dark"
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
                ClassWorker.GetTilesSettings.Theme = "Light"
                DwmSetWindowAttribute(New System.Windows.Interop.WindowInteropHelper(Me).Handle, 20, False, Runtime.InteropServices.Marshal.SizeOf(True))
        End Select
    End Sub

#Region "Menu"

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        End
    End Sub

    Private Sub Light_Click(sender As Object, e As RoutedEventArgs)
        Dim localTilesSettings As MinecraftEarthTiles_Core.TilesSettings = ClassWorker.GetTilesSettings
        localTilesSettings.Theme = "Light"
        ClassWorker.SetTilesSettings(localTilesSettings)
        ApplyTheme()
        mnu_light.IsChecked = True
        mnu_dark.IsChecked = False
        Try
            ClassWorker.SaveTilesSettingsToFile(ClassWorker.GetTilesSettings, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/tiles_settings.xml")
        Catch ex As Exception
            Dim MessageBox As New MessageBoxWindow(ex.Message)
            MessageBox.ShowDialog()
        End Try
    End Sub

    Private Sub Dark_Click(sender As Object, e As RoutedEventArgs)
        Dim localTilesSettings As MinecraftEarthTiles_Core.TilesSettings = ClassWorker.GetTilesSettings
        localTilesSettings.Theme = "Dark"
        ClassWorker.SetTilesSettings(localTilesSettings)
        ApplyTheme()
        mnu_dark.IsChecked = True
        mnu_light.IsChecked = False
        Try
            ClassWorker.SaveTilesSettingsToFile(ClassWorker.GetTilesSettings, ClassWorker.GetTilesSettings.PathToScriptsFolder & "/tiles_settings.xml")
        Catch ex As Exception
            Dim MessageBox As New MessageBoxWindow(ex.Message)
            MessageBox.ShowDialog()
        End Try
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        Process.Start("https://earth.motfe.net/")
    End Sub

    Private Sub Info_Click(sender As Object, e As RoutedEventArgs)
        Dim InfoWindow As New InfoWindow
        InfoWindow.ShowDialog()
    End Sub

#End Region

#Region "Config Buttons"

    Private Sub Btn_Settings_Click(sender As Object, e As RoutedEventArgs)
        Dim SettingsWindow As New SettingsWindow
        SettingsWindow.ShowDialog()
        Check()
    End Sub

    Private Sub Btn_Selection_Click(sender As Object, e As RoutedEventArgs)
        Dim SelectionWindow As New SelectionWindow
        SelectionWindow.ShowDialog()
        Check()
    End Sub

    Private Sub Btn_Generation_Click(sender As Object, e As RoutedEventArgs)
        Dim GenerationWindow As New GenerationWindow
        GenerationWindow.ShowDialog()
        Check()
    End Sub

#End Region

    Private Sub Check()
        lbl_Setting_Status.Content = "Status: incomplete"
        lbl_Selection_Numbers.Content = "Tiles selected: " & (ClassWorker.GetSelection.TilesList.Count - ClassWorker.GetSelection.VoidTiles)
        btn_AllExport.IsEnabled = False

        If Not ClassWorker.GetTilesSettings.PathToMagick = "" _
            And Not ClassWorker.GetTilesSettings.PathToQGIS = "" _
            And Not ClassWorker.GetTilesSettings.PathToScriptsFolder = "" _
            And Not ClassWorker.GetTilesSettings.PathToWorldPainterFolder = "" _
            And (
            (Not ClassWorker.GetWorldSettings.PathToPBF = "" And ClassWorker.GetWorldSettings.geofabrik = True And ClassWorker.GetTilesSettings.reUsePbfFile = False) _
            Or (ClassWorker.GetTilesSettings.reUsePbfFile = True) _
            Or (ClassWorker.GetWorldSettings.reUseOsmFiles = True) _
            Or (ClassWorker.GetWorldSettings.reUseImageFiles = True)
            ) Then
            lbl_Setting_Status.Content = "Status: complete"
            If ClassWorker.GetSelection.SpawnTile = "" Then
                lbl_Selection_Numbers.Content = "No spawn Tile selected"
            ElseIf ClassWorker.GetSelection.TilesList.Count = 0 Then
                lbl_Selection_Numbers.Content = "No Tiles selected"
            Else
                btn_AllExport.IsEnabled = True
            End If
        End If
        lbl_World_Size_Content.Content = ClassWorker.GetWorldSize
    End Sub

End Class

Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports MinecraftEarthTiles_Core
Imports System.Runtime.InteropServices

Public Class InfoWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
        Dim coreType As Type = ClassWorker.GetTilesSettings.GetType
        Dim coreVersion = System.Reflection.Assembly.GetAssembly(coreType).GetName.Version
        txb_version.Text = $"GUI: v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}, Core: v{coreVersion.Major}.{coreVersion.Minor}.{coreVersion.Build}"
        txb_copyright.Text = "Copyright © MattingerSoftwareSolution 2020 - 2024" & Environment.NewLine & "https://github.com/DerMattinger/MinecraftEarthTiles"
        txb_osm.Text = "OSM Data: ©️ OpenStreetMap Contributors" & Environment.NewLine & "https://www.openstreetmap.org/copyright"
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

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub openOSM(sender As Object, e As RoutedEventArgs)
        Process.Start("https://www.openstreetmap.org/copyright")
    End Sub

End Class

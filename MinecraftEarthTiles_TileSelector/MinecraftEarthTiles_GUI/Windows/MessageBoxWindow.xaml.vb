Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports MinecraftEarthTiles_Core

Public Class MessageBoxWindow

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Public Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Boolean, attrSize As Integer) As Integer
    End Function

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        ApplyTheme()
    End Sub

    Public Sub New(ByVal Message As String)
        MyBase.New()
        InitializeComponent()
        txb_Message.Text = Message
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

    Private Sub Btn_Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub btn_PreviewKeyDown(sender As Object, e As Input.KeyEventArgs)
        If e.Key = Keys.Enter Or e.Key = Keys.Escape Or e.Key = Key.Return Then
            Close()
        End If
    End Sub

End Class

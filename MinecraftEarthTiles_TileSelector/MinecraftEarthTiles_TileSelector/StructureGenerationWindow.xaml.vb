Imports System.Windows.Forms

Public Class StructureGenerationWindow

#Region "Menu"

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "Help/Settings.chm")
        Process.Start("https://earth.motfe.net/")
    End Sub

#End Region

End Class

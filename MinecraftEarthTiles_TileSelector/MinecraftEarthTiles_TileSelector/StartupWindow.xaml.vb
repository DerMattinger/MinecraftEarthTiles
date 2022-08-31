Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms

Public Class StartupWindow

    Public Shared Property MySettings As New Settings
    Public Shared Property MySelection As New Selection
    Public Shared Property MyGeneration As New List(Of Generation)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim LocalSettings As Settings
        If File.Exists(My.Application.Info.DirectoryPath & "\settings.xml") Then
            Try
                LocalSettings = CustomXmlSerialiser.GetXMLSettings(My.Application.Info.DirectoryPath & "\settings.xml")
                If Not Directory.Exists(LocalSettings.PathToScriptsFolder) Then
                    MySettings.PathToScriptsFolder = ""
                End If
                If Not File.Exists(LocalSettings.PathToWorldPainterFolder) Then
                    MySettings.PathToWorldPainterFolder = ""
                End If
                If Not Directory.Exists(LocalSettings.PathToQGIS) Then
                    MySettings.PathToQGIS = ""
                End If
                If Not File.Exists(LocalSettings.PathToMagick) Then
                    MySettings.PathToMagick = ""
                End If
                MySettings = LocalSettings
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
        Dim LocalSelection As New Selection
        If File.Exists(My.Application.Info.DirectoryPath & "\selection.xml") Then
            Try
                LocalSelection = CustomXmlSerialiser.GetXMLSelection(My.Application.Info.DirectoryPath & "\selection.xml")
                MySelection = LocalSelection
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
        Check()
    End Sub

#Region "Menu"

    Private Sub Exit_Click(sender As Object, e As RoutedEventArgs)
        End
    End Sub

    Private Sub Help_Click(sender As Object, e As RoutedEventArgs)
        'Help.ShowHelp(Nothing, "MyResources/MinecraftEarthTiles.chm")
        Process.Start("https://earth.motfe.net/")
    End Sub

    Private Sub Info_Click(sender As Object, e As RoutedEventArgs)
        Dim MsgBoxResult As DialogResult = MessageBox.Show("Copyright © 2020 - 2022 by MattiBorchers." & Environment.NewLine & "OSM Data: ©️ OpenStreetMap Contributors" & Environment.NewLine & "https://www.openstreetmap.org/copyright", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
        Dim TilesList As List(Of String) = MySelection.TilesList
        If TilesList.Count = 0 Then
            Throw New System.Exception("No Tiles selected.")
        End If
        TilesList.Sort()
        MyGeneration = New List(Of Generation)
        MyGeneration.Add(New Generation("Convert pbf"))
        For Each Tile In TilesList
            MyGeneration.Add(New Generation(Tile))
        Next
        MyGeneration.Add(New Generation("Combining"))
        MyGeneration.Add(New Generation("Cleanup"))

        Dim GenerationWindow As New GenerationWindow
        GenerationWindow.ShowDialog()
        Check()
    End Sub

    Private Sub Btn_StructureGeneration_Click(sender As Object, e As RoutedEventArgs)
        MsgBox("Not implemented yet!")
        'Dim StructureGenerationWindow As New StructureGenerationWindow
        'StructureGenerationWindow.ShowDialog()
    End Sub

#End Region

    Public Sub Check()
        lbl_Setting_Status.Content = "Status: incomplete"
        lbl_Selection_Numbers.Content = "Tiles selected: " & MySelection.TilesList.Count
        btn_AllExport.IsEnabled = False
        btn_StructureGeneration.IsEnabled = False

        If Not MySettings.PathToMagick = "" _
            And Not MySettings.PathToQGIS = "" _
            And Not MySettings.PathToScriptsFolder = "" _
            And Not MySettings.PathToWorldPainterFolder = "" _
            And ((Not MySettings.PathToPBF = "" And MySettings.geofabrik = True And MySettings.reUsePbfFile = False) Or (MySettings.geofabrik = False) Or (MySettings.geofabrik = True And MySettings.reUsePbfFile = True)) Then
            lbl_Setting_Status.Content = "Status: complete"
            btn_StructureGeneration.IsEnabled = True
            If Not MySelection.SpawnTile = "" And MySelection.TilesList.Count > 0 Then
                btn_AllExport.IsEnabled = True
            End If
        End If

        Dim worldSize As Double = 0.0
        Dim worldSizeString As String = ""
        Try
            worldSize = (5.4 * (((CType(MySettings.BlocksPerTile, Int32) / 512) ^ 2) * (CType(MySelection.TilesList.Count, Int32) - MySelection.VoidTiles))) + (4.0 * MySelection.VoidTiles)
            Select Case worldSize
                Case < 1
                    worldSizeString = worldSize.ToString & " KB"
                Case < 1000
                    worldSizeString = Math.Round(worldSize / 1, 1).ToString & " MB"
                Case < 1000000
                    worldSizeString = Math.Round(worldSize / 1000, 1).ToString & " GB"
                Case < 1000000000
                    worldSizeString = Math.Round(worldSize / 1000000, 1).ToString & " TB"
            End Select
            lbl_World_Size_Content.Content = worldSizeString
        Catch ex As Exception
            worldSizeString = "0 KB"
        End Try

    End Sub

End Class

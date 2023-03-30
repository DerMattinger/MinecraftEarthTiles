Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.IO.Compression
Imports System.Text.RegularExpressions

Public Class ClassWorker

    Friend Shared Property MyWorldSettings As New Settings
    Friend Shared Property MyTilesSettings As New TilesSettings
    Friend Shared Property MySelection As New Selection

#Region "WorldSetting"

    Public Shared Function SetWorldSettings(Settings As Settings) As Boolean
        MyWorldSettings = Settings
        Return True
    End Function

    Public Shared Function GetWorldSettings() As Settings
        Return MyWorldSettings
    End Function

    Public Shared Function LoadWorldSettingsFromFile(filePath As String) As Settings
        Return CustomXmlSerialiser.GetXMLWorldSettings(filePath)
    End Function

    Public Shared Function SaveWorldSettingsToFile(settings As Settings, filePath As String) As Boolean
        CustomXmlSerialiser.SaveXML(filePath, settings)
        Return True
    End Function

#End Region

#Region "TilesSetting"

    Public Shared Function SetTilesSettings(TilesSettings As TilesSettings) As Boolean
        MyTilesSettings = TilesSettings
        Return True
    End Function

    Public Shared Function GetTilesSettings() As TilesSettings
        Return MyTilesSettings
    End Function

    Public Shared Function LoadTilesSettingsFromFile(filePath As String) As TilesSettings
        Return CustomXmlSerialiser.GetXMLTilesSettings(filePath)
    End Function

    Public Shared Function SaveTilesSettingsToFile(tilesSettings As TilesSettings, filePath As String) As Boolean
        CustomXmlSerialiser.SaveXML(filePath, tilesSettings)
        Return True
    End Function

#End Region

#Region "Selection"

    Public Shared Function SetSelection(Selection As Selection) As Boolean
        Selection.TilesList.Sort()
        Selection.FullTileList.Sort()
        MySelection = Selection
        Return True
    End Function

    Public Shared Function GetSelection() As Selection
        Return MySelection
    End Function

    Public Shared Function LoadSelectionFromFile(filePath As String) As Selection
        Dim Selection As Selection = CustomXmlSerialiser.GetXMLSelection(filePath)
        Selection.TilesList.Sort()
        Selection.FullTileList.Sort()
        Return Selection
    End Function

    Public Shared Function SaveSelectionToFile(selection As Selection, filePath As String) As Boolean
        CustomXmlSerialiser.SaveXML(filePath, selection)
        Return True
    End Function

#End Region

#Region "Misc"

    Public Shared Function GetWorldSize() As String
        Dim worldSize As Double = 0.0
        Dim worldSizeString As String = ""
        Try
            worldSize = (5.4 * (((CType(MyWorldSettings.BlocksPerTile, Int32) / 512) ^ 2) * (CType(MySelection.TilesList.Count, Int32) - MySelection.VoidTiles))) + (4.0 * MySelection.VoidTiles)
            If MyWorldSettings.MapVersion = "1.18" Or MyWorldSettings.MapVersion = "1.19" Then
                worldSize *= 2.5
            End If
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
        Catch ex As Exception
            worldSizeString = "0 KB"
        End Try
        Return worldSizeString
    End Function

    Public Shared Function RemoveIllegalFileNameChars(input As String, Optional replacement As String = "_") As String
        Dim regexSearch = New String(Path.GetInvalidFileNameChars()) & New String(Path.GetInvalidPathChars())
        Dim r = New Regex(String.Format("[{0}]", Regex.Escape(regexSearch)))
        Dim returnString As String = ""
        returnString = r.Replace(input, replacement)
        returnString = returnString.Replace(" ", replacement)
        Return returnString
    End Function

    Public Shared Sub CreateDebugZip(Optional path As String = "")
        If path = "" Then
            path = MyTilesSettings.PathToScriptsFolder & "/" & DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") & "_Debug.zip"
        End If
        CustomXmlSerialiser.SaveXML(ClassWorker.GetTilesSettings.PathToScriptsFolder & "/logs/tiles_settings.xml", MyTilesSettings)
        CustomXmlSerialiser.SaveXML(ClassWorker.GetTilesSettings.PathToScriptsFolder & "/logs/settings.xml", MyWorldSettings)
        CustomXmlSerialiser.SaveXML(ClassWorker.GetTilesSettings.PathToScriptsFolder & "/logs/selection.xml", MySelection)
        ZipFile.CreateFromDirectory(ClassWorker.GetTilesSettings.PathToScriptsFolder & "/logs/", path)
    End Sub

#End Region

End Class

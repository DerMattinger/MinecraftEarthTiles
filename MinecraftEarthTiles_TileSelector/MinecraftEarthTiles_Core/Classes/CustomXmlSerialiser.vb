Imports System.IO
Imports System.Net.Mail
Imports System.Text
Imports System.Xml.Serialization

Public Class CustomXmlSerialiser

    Public Shared Function SaveXML(ByVal fileName As String, ByVal DataToSerialize As Object) As Boolean
        Dim nsBlank As New XmlSerializerNamespaces
        nsBlank.Add("", "")
        Dim xSettings As New System.Xml.XmlWriterSettings
        With xSettings
            .Encoding = Encoding.UTF8
            .Indent = True
            .NewLineChars = Environment.NewLine
            .NewLineOnAttributes = False
            .ConformanceLevel = Xml.ConformanceLevel.Document
        End With
        Dim xw As System.Xml.XmlWriter = Xml.XmlWriter.Create(fileName, xSettings)
        Dim writer As New XmlSerializer(DataToSerialize.GetType)
        writer.Serialize(xw, DataToSerialize, nsBlank)
        xw.Close()
        Return True
    End Function

    Public Shared Function GetXMLSelection(ByVal fileName As String) As Selection
        Dim MySelection As New Selection
        If My.Computer.FileSystem.FileExists(fileName) Then
            Dim fs As FileStream = New FileStream(fileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(MySelection.GetType)
            MySelection = CType(xs.Deserialize(fs), Selection)
            fs.Close()
            CheckSelection(MySelection)
            Return MySelection
        Else
            Throw New Exception($"File '{fileName}' not found.")
        End If
    End Function

    Public Shared Sub CheckSelection(ByRef MySelection As Selection)
        'Filter Tiles out of Bound
    End Sub

    Public Shared Function GetXMLWorldSettings(ByVal fileName As String) As Settings
        Dim MyWorldSettings As New Settings
        If My.Computer.FileSystem.FileExists(fileName) Then
            Dim fs As FileStream = New FileStream(fileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(MyWorldSettings.GetType)
            MyWorldSettings = CType(xs.Deserialize(fs), Settings)
            fs.Close()
            CheckWorldSettings(MyWorldSettings)
            Return MyWorldSettings
        Else
            Throw New Exception($"File '{fileName}' not found.")
        End If
    End Function

    Public Shared Sub CheckWorldSettings(ByRef MyWorldSettings As Settings)

        If Not MyWorldSettings.WorldName = "" Then
            MyWorldSettings.WorldName = ClassWorker.RemoveIllegalFileNameChars(MyWorldSettings.WorldName)
        Else
            MyWorldSettings.WorldName = "world"
        End If
        If Not CType(MyWorldSettings.BlocksPerTile, Int16) Mod 512 = 0.0 Then
            MyWorldSettings.BlocksPerTile = 512
        End If

        If Not MyWorldSettings.Terrain = "Standard" And Not MyWorldSettings.Terrain = "Custom" Then
            MyWorldSettings.Terrain = "Standard"
        End If

        If Not 90 Mod CType(MyWorldSettings.TilesPerMap, Int16) = 0.0 Then
            MyWorldSettings.TilesPerMap = 1
        End If

        If Not MyWorldSettings.MapVersion = "1.12" And Not MyWorldSettings.MapVersion = "1.16" And Not MyWorldSettings.MapVersion = "1.17" And Not MyWorldSettings.MapVersion = "1.18" And Not MyWorldSettings.MapVersion = "1.19" And Not MyWorldSettings.MapVersion = "1.20" Then
            MyWorldSettings.MapVersion = "1.20"
        End If

        If Not MyWorldSettings.MapVersion = "1.12" And Not MyWorldSettings.MapVersion = "1.18" And Not MyWorldSettings.MapVersion = "1.19" And Not MyWorldSettings.MapVersion = "1.20" Then
            MyWorldSettings.vanillaPopulation = False
        End If

        If Not MyWorldSettings.lowerBuildLimit = "0" And Not MyWorldSettings.lowerBuildLimit = "-64" And Not MyWorldSettings.lowerBuildLimit = "-128" And Not MyWorldSettings.lowerBuildLimit = "-256" And Not MyWorldSettings.lowerBuildLimit = "-512" And Not MyWorldSettings.lowerBuildLimit = "-1024" And Not MyWorldSettings.lowerBuildLimit = "-2032" Then
            MyWorldSettings.lowerBuildLimit = "-64"
        End If

        If Not MyWorldSettings.upperBuildLimit = "256" And Not MyWorldSettings.upperBuildLimit = "320" And Not MyWorldSettings.upperBuildLimit = "512" And Not MyWorldSettings.upperBuildLimit = "1024" And Not MyWorldSettings.upperBuildLimit = "1536" And Not MyWorldSettings.upperBuildLimit = "2032" Then
            MyWorldSettings.upperBuildLimit = "320"
        End If

        If Not MyWorldSettings.borders = "2000bc" And Not MyWorldSettings.borders = "1000bc" And Not MyWorldSettings.borders = "500bc" And Not MyWorldSettings.borders = "323bc" And Not MyWorldSettings.borders = "200bc" And Not MyWorldSettings.borders = "1bc" And Not MyWorldSettings.borders = "400" And Not MyWorldSettings.borders = "600" And Not MyWorldSettings.borders = "800" And Not MyWorldSettings.borders = "1000" And Not MyWorldSettings.borders = "1279" And Not MyWorldSettings.borders = "1482" And Not MyWorldSettings.borders = "1530" And Not MyWorldSettings.borders = "1650" And Not MyWorldSettings.borders = "1715" And Not MyWorldSettings.borders = "1783" And Not MyWorldSettings.borders = "1815" And Not MyWorldSettings.borders = "1880" And Not MyWorldSettings.borders = "1914" And Not MyWorldSettings.borders = "1920" And Not MyWorldSettings.borders = "1938" And Not MyWorldSettings.borders = "1945" And Not MyWorldSettings.borders = "1994" And Not MyWorldSettings.borders = "Current" Then
            MyWorldSettings.borders = "Current"
        End If

        If Not MyWorldSettings.TerrainSource = "High Quality Offline Terrain (Addon)" And Not MyWorldSettings.TerrainSource = "Low Quality Offline Terrain" And Not MyWorldSettings.TerrainSource = "Arcgis" And Not MyWorldSettings.TerrainSource = "Google" And Not MyWorldSettings.TerrainSource = "Bing" Then
            MyWorldSettings.TerrainSource = "Arcgis"
        End If

        If Not MyWorldSettings.biomeSource = "Köppen Climate Classification" And Not MyWorldSettings.biomeSource = "Terrestrial Ecoregions (WWF)" Then
            MyWorldSettings.biomeSource = "Terrestrial Ecoregions (WWF)"
        End If

        If MyWorldSettings.rivers = "small" Then
            MyWorldSettings.rivers = "All (small)"
        End If
        If MyWorldSettings.rivers = "medium" Then
            MyWorldSettings.rivers = "All (medium)"
        End If
        If MyWorldSettings.rivers = "large" Then
            MyWorldSettings.rivers = "All (large)"
        End If
        If MyWorldSettings.rivers = "major" Then
            MyWorldSettings.rivers = "Major"
        End If
        If MyWorldSettings.rivers = "major" Then
            MyWorldSettings.rivers = "Major"
        End If
        If Not MyWorldSettings.rivers = "All (small)" And Not MyWorldSettings.rivers = "All (medium)" And Not MyWorldSettings.rivers = "All (large)" And Not MyWorldSettings.rivers = "Major" And Not MyWorldSettings.rivers = "Major + Minor" Then
            MyWorldSettings.rivers = "Major"
        End If

        If Not MyWorldSettings.waterBodies = "All" And Not MyWorldSettings.rivers = "Major" Then
            MyWorldSettings.waterBodies = "Major"
        End If

        If Not MyWorldSettings.mapOffset = "-1" And Not MyWorldSettings.mapOffset = "0" And Not MyWorldSettings.mapOffset = "1" Then
            MyWorldSettings.mapOffset = "0"
        End If

        If MyWorldSettings.terrainModifier < -2 Or MyWorldSettings.terrainModifier > 2 Then
            MyWorldSettings.terrainModifier = 0
        End If

        If MyWorldSettings.oreModifier < 1 Or MyWorldSettings.oreModifier > 15 Then
            MyWorldSettings.oreModifier = 8
        End If

    End Sub

    Public Shared Function GetXMLTilesSettings(ByVal fileName As String) As TilesSettings
        Dim MyTilesSettings As New TilesSettings
        If My.Computer.FileSystem.FileExists(fileName) Then
            Dim fs As FileStream = New FileStream(fileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(MyTilesSettings.GetType)
            MyTilesSettings = CType(xs.Deserialize(fs), TilesSettings)
            fs.Close()
            CheckTilesSettings(MyTilesSettings)
            Return MyTilesSettings
        Else
            Throw New Exception($"File '{fileName}' not found.")
        End If
    End Function

    Public Shared Sub CheckTilesSettings(ByRef MyTilesSettings As TilesSettings)

        If Not Directory.Exists(MyTilesSettings.PathToScriptsFolder) Then
            MyTilesSettings.PathToScriptsFolder = ""
        End If

        If Not File.Exists(MyTilesSettings.PathToWorldPainterFolder) Then
            MyTilesSettings.PathToWorldPainterFolder = ""
        End If

        If Not Directory.Exists(MyTilesSettings.PathToQGIS) Then
            MyTilesSettings.PathToQGIS = ""
        End If

        If Not Directory.Exists(MyTilesSettings.PathToTempOSM) Then
            MyTilesSettings.PathToTempOSM = ""
        End If

        If MyTilesSettings.PathToTempOSM = "" Then
            MyTilesSettings.PathToTempOSM = MyTilesSettings.PathToScriptsFolder
        End If

        If Not Directory.Exists(MyTilesSettings.PathToQGISProject) Then
            MyTilesSettings.PathToQGISProject = ""
        End If

        If MyTilesSettings.PathToQGISProject = "" Then
            MyTilesSettings.PathToQGISProject = MyTilesSettings.PathToScriptsFolder & "\QGIS"
        End If

        If Not Directory.Exists(MyTilesSettings.PathToQGISProjectBathymetryAddon) Then
            MyTilesSettings.PathToQGISProjectBathymetryAddon = ""
        End If

        If MyTilesSettings.PathToQGISProjectBathymetryAddon = "" Then
            MyTilesSettings.PathToQGISProjectBathymetryAddon = MyTilesSettings.PathToScriptsFolder & "\QGIS"
        End If

        If Not Directory.Exists(MyTilesSettings.PathToQGISProjectTerrainAddon) Then
            MyTilesSettings.PathToQGISProjectTerrainAddon = ""
        End If

        If MyTilesSettings.PathToQGISProjectTerrainAddon = "" Then
            MyTilesSettings.PathToQGISProjectTerrainAddon = MyTilesSettings.PathToScriptsFolder & "\QGIS"
        End If

        If Not Directory.Exists(MyTilesSettings.PathToQGISProjectHeightmapAddon) Then
            MyTilesSettings.PathToQGISProjectHeightmapAddon = ""
        End If

        If MyTilesSettings.PathToQGISProjectHeightmapAddon = "" Then
            MyTilesSettings.PathToQGISProjectHeightmapAddon = MyTilesSettings.PathToScriptsFolder & "\QGIS"
        End If

        If Not File.Exists(MyTilesSettings.PathToMagick) Then
            MyTilesSettings.PathToMagick = ""
        End If

        If Not File.Exists(MyTilesSettings.PathToMinutor) Then
            MyTilesSettings.PathToMinutor = ""
        End If

        If Not Directory.Exists(MyTilesSettings.PathToExport) Then
            MyTilesSettings.PathToExport = ""
        End If

        If CType(MyTilesSettings.NumberOfCores, Int32) > 16 Then
            MyTilesSettings.NumberOfCores = 16
        End If

        Try
            Dim localMail As New MailAddress(MyTilesSettings.alertMail)
        Catch ex As Exception
            MyTilesSettings.alertMail = ""
        End Try

    End Sub

End Class

Public Class Settings
    Public PathToScriptsFolder As String
    Public PathToWorldPainterFolder As String
    Public PathToQGIS As String
    Public PathToMagick As String
    Public BlocksPerTile As String
    Public TilesPerMap As String
    Public VerticalScale As String
    Public Terrain As String
    Public Heightmap_Error_Correction As Boolean
    Public borders As String
    Public geofabrik As Boolean
    Public highways As Boolean
    Public streets As Boolean
    Public small_streets As Boolean
    Public buildings As Boolean
    Public farms As Boolean
    Public meadows As Boolean
    Public quarrys As Boolean
    Public rivers As Boolean
    Public streams As Boolean
    Public MapVersion As String
    Public Proxy As String

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        PathToScriptsFolder = My.Application.Info.DirectoryPath
        PathToWorldPainterFolder = ""
        PathToQGIS = ""
        PathToMagick = ""
        BlocksPerTile = "1024"
        TilesPerMap = "1"
        VerticalScale = "50"
        Terrain = "Standard"
        Heightmap_Error_Correction = False
        geofabrik = False
        highways = True
        streets = True
        small_streets = False
        buildings = True
        borders = "2020"
        farms = True
        meadows = True
        quarrys = True
        rivers = True
        streams = False
        MapVersion = "1.12"
        Proxy = ""
    End Sub

End Class

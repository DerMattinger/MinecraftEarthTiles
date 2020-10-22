Public Class Settings
    Public PathToScriptsFolder As String
    Public PathToWorldPainterFolder As String
    Public PathToQGIS As String
    Public PathToMagick As String
    Public PathToPBF As String
    Public WorldName As String
    Public BlocksPerTile As String
    Public TilesPerMap As String
    Public VerticalScale As String
    Public Terrain As String
    Public Heightmap_Error_Correction As Boolean
    Public bordersBoolean As Boolean
    Public borders As String
    Public geofabrik As Boolean
    Public geofabrikalreadygenerated As Boolean
    Public bathymetry As Boolean
    Public highways As Boolean
    Public streets As Boolean
    Public small_streets As Boolean
    Public buildings As Boolean
    Public farms As Boolean
    Public meadows As Boolean
    Public quarrys As Boolean
    Public aerodrome As Boolean
    Public mobSpawner As Boolean
    Public animalSpawner As Boolean
    Public riversBoolean As Boolean
    Public rivers As String
    Public streams As Boolean
    Public MapVersion As String
    Public Proxy As String
    Public KeepTemporaryFiles As Boolean
    Public NumberOfCores As String
    Public cmdVisibility As Boolean

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        PathToScriptsFolder = My.Application.Info.DirectoryPath
        PathToWorldPainterFolder = ""
        PathToQGIS = ""
        PathToMagick = ""
        PathToPBF = ""
        WorldName = "world"
        BlocksPerTile = "1024"
        TilesPerMap = "1"
        VerticalScale = "50"
        Terrain = "Standard"
        Heightmap_Error_Correction = False
        geofabrik = False
        geofabrikalreadygenerated = False
        bathymetry = True
        highways = True
        streets = True
        small_streets = False
        buildings = True
        bordersBoolean = True
        borders = "2020"
        farms = True
        meadows = True
        quarrys = True
        aerodrome = True
        mobSpawner = True
        animalSpawner = True
        riversBoolean = True
        rivers = "medium"
        streams = False
        MapVersion = "1.12"
        Proxy = ""
        KeepTemporaryFiles = False
        NumberOfCores = "2"
        cmdVisibility = False
    End Sub

End Class

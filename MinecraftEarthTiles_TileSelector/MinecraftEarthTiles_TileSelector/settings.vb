Public Class Settings
    Public PathToScriptsFolder As String
    Public PathToWorldPainterFolder As String
    Public PathToQGIS As String
    Public PathToMagick As String
    Public PathToPBF As String
    Public PathToExport As String
    Public WorldName As String
    Public BlocksPerTile As String
    Public TilesPerMap As String
    Public VerticalScale As String
    Public Terrain As String
    Public Heightmap_Error_Correction As Boolean
    Public bordersBoolean As Boolean
    Public borders As String
    Public geofabrik As Boolean
    Public bathymetry As Boolean
    Public highways As Boolean
    Public streets As Boolean
    Public small_streets As Boolean
    Public buildings As Boolean
    Public ores As Boolean
    Public netherite As Boolean
    Public farms As Boolean
    Public meadows As Boolean
    Public quarrys As Boolean
    Public aerodrome As Boolean
    Public mobSpawner As Boolean
    Public animalSpawner As Boolean
    Public riversBoolean As Boolean
    Public rivers As String
    Public streams As Boolean
    Public volcanos As Boolean
    Public shrubs As Boolean
    Public crops As Boolean
    Public MapVersion As String
    Public Proxy As String
    Public keepPbfFile As Boolean
    Public reUsePbfFile As Boolean
    Public keepOsmFiles As Boolean
    Public reUseOsmFiles As Boolean
    Public keepImageFiles As Boolean
    Public reUseImageFiles As Boolean
    Public keepWorldPainterFiles As Boolean
    Public NumberOfCores As String
    Public cmdVisibility As Boolean
    Public continueGeneration As Boolean

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        PathToScriptsFolder = My.Application.Info.DirectoryPath
        PathToWorldPainterFolder = ""
        PathToQGIS = ""
        PathToMagick = ""
        PathToPBF = ""
        PathToExport = ""
        WorldName = "world"
        BlocksPerTile = "1024"
        TilesPerMap = "1"
        VerticalScale = "50"
        Terrain = "Standard"
        Heightmap_Error_Correction = False
        geofabrik = False
        bathymetry = True
        highways = True
        streets = True
        small_streets = False
        buildings = True
        ores = True
        netherite = True
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
        volcanos = True
        shrubs = True
        crops = True
        MapVersion = "1.16+"
        Proxy = ""
        keepPbfFile = True
        reUsePbfFile = False
        keepOsmFiles = False
        reUseOsmFiles = False
        keepImageFiles = False
        reUseImageFiles = False
        keepWorldPainterFiles = False
        NumberOfCores = "2"
        cmdVisibility = False
        continueGeneration = False
    End Sub

End Class

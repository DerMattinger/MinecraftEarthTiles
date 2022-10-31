Public Class Settings
    Public version As String
    Public PathToScriptsFolder As String
    Public PathToWorldPainterFolder As String
    Public PathToQGIS As String
    Public PathToMagick As String
    Public PathToMinutor As String
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
    Public TerrainSource As String
    Public biomeSource As String
    Public highways As Boolean
    Public streets As Boolean
    Public small_streets As Boolean
    Public buildings As Boolean
    Public ores As Boolean
    Public netherite As Boolean
    Public farms As Boolean
    Public meadows As Boolean
    Public quarrys As Boolean
    Public forests As Boolean
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
    Public minutor As Boolean
    Public NumberOfCores As String
    Public ParallelWorldPainterGenerations As Boolean
    Public cmdVisibility As Boolean
    Public continueGeneration As Boolean
    Public mapOffset As String
    Public vanillaPopulation As Boolean
    Public OverpassURL As String
    Public mod_BOP As Boolean
    Public mod_BYG As Boolean
    Public mod_Terralith As Boolean
    Public mod_Create As Boolean

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        version = "1.2.1"
        PathToScriptsFolder = My.Application.Info.DirectoryPath
        PathToWorldPainterFolder = ""
        PathToQGIS = ""
        PathToMagick = ""
        PathToMinutor = ""
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
        TerrainSource = "Argcis"
        biomeSource = "Köppen Climate Classification"
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
        forests = True
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
        minutor = True
        NumberOfCores = "2"
        ParallelWorldPainterGenerations = True
        cmdVisibility = False
        continueGeneration = False
        mapOffset = "0"
        vanillaPopulation = False
        OverpassURL = ""
        mod_BOP = False
        mod_BYG = False
        mod_Terralith = False
        mod_Create = False

    End Sub

End Class

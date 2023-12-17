Imports System.Xml.Serialization

Public Class Settings
    Public PathToPBF As String
    Public OverpassURL As String
    Public WorldName As String
    Public BlocksPerTile As String
    Public TilesPerMap As String
    Public VerticalScale As String
    Public Terrain As String
    Public Heightmap_Error_Correction As Boolean
    Public bordersBoolean As Boolean
    Public borders As String
    Public stateBorders As Boolean
    Public geofabrik As Boolean
    Public offlineHeightmap As Boolean
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
    Public waterBodies As String
    Public streams As Boolean
    Public volcanos As Boolean
    Public shrubs As Boolean
    Public crops As Boolean
    Public generateFullEarth As Boolean
    Public MapVersion As String
    Public mapOffset As String
    Public lowerBuildLimit As String
    Public upperBuildLimit As String
    Public vanillaPopulation As Boolean
    Public mod_BOP As Boolean
    Public mod_BYG As Boolean
    Public mod_Terralith As Boolean
    Public mod_WilliamWythers As Boolean
    Public mod_Create As Boolean
    Public custom_layers As String
    Public terrainModifier As Integer
    Public oreModifier As Integer

    Public version As String
    Public Function ShouldSerializeversion() As Boolean
        Return False
    End Function
    Public PathToScriptsFolder As String
    Public Function ShouldSerializePathToScriptsFolder() As Boolean
        Return False
    End Function
    Public PathToWorldPainterFolder As String
    Public Function ShouldSerializePathToWorldPainterFolder() As Boolean
        Return False
    End Function
    Public PathToQGIS As String
    Public Function ShouldSerializePathToQGISn() As Boolean
        Return False
    End Function
    Public PathToMagick As String
    Public Function ShouldSerializePathToMagick() As Boolean
        Return False
    End Function
    Public PathToMinutor As String
    Public Function ShouldSerializePathToMinutor() As Boolean
        Return False
    End Function
    Public PathToExport As String
    Public Function ShouldSerializePathToExport() As Boolean
        Return False
    End Function
    Public Theme As String
    Public Function ShouldSerializeTheme() As Boolean
        Return False
    End Function
    Public Proxy As String
    Public Function ShouldSerializeProxy() As Boolean
        Return False
    End Function
    Public keepPbfFile As Boolean?
    Public Function ShouldSerializekeepPbfFile() As Boolean
        Return False
    End Function
    Public reUsePbfFile As Boolean?
    Public Function ShouldSerializereUsePbfFile() As Boolean
        Return False
    End Function
    Public keepOsmFiles As Boolean?
    Public Function ShouldSerializekeepOsmFiles() As Boolean
        Return False
    End Function
    Public reUseOsmFiles As Boolean?
    Public Function ShouldSerializereUseOsmFiles() As Boolean
        Return False
    End Function
    Public keepImageFiles As Boolean?
    Public Function ShouldSerializekeepImageFiles() As Boolean
        Return False
    End Function
    Public reUseImageFiles As Boolean?
    Public Function ShouldSerializereUseImageFiles() As Boolean
        Return False
    End Function
    Public keepWorldPainterFiles As Boolean?
    Public Function ShouldSerializekeepWorldPainterFiles() As Boolean
        Return False
    End Function
    Public minutor As Boolean?
    Public Function ShouldSerializeminutor() As Boolean
        Return False
    End Function
    Public NumberOfCores As String
    Public Function ShouldSerializeNumberOfCores() As Boolean
        Return False
    End Function
    Public ParallelWorldPainterGenerations As Boolean?
    Public Function ShouldSerializeParallelWorldPainterGenerations() As Boolean
        Return False
    End Function
    Public cmdVisibility As Boolean?
    Public Function ShouldSerializecmdVisibility() As Boolean
        Return False
    End Function
    Public cmdPause As Boolean?
    Public Function ShouldSerializecmdPause() As Boolean
        Return False
    End Function
    Public continueGeneration As Boolean?
    Public Function ShouldSerializecontinueGeneration() As Boolean
        Return False
    End Function

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        PathToPBF = ""
        OverpassURL = ""
        WorldName = "world"
        BlocksPerTile = "512"
        TilesPerMap = "1"
        VerticalScale = "35"
        Terrain = "Standard"
        Heightmap_Error_Correction = True
        geofabrik = True
        offlineHeightmap = False
        bathymetry = True
        TerrainSource = "Argcis"
        biomeSource = "Terrestrial Ecoregions (WWF)"
        highways = True
        streets = True
        small_streets = False
        buildings = True
        ores = True
        netherite = True
        bordersBoolean = True
        borders = "Current"
        stateBorders = False
        farms = True
        meadows = True
        quarrys = True
        forests = True
        aerodrome = True
        mobSpawner = True
        animalSpawner = True
        riversBoolean = True
        rivers = "Major"
        streams = False
        volcanos = True
        shrubs = True
        crops = True
        generateFullEarth = False
        MapVersion = "1.20"
        mapOffset = "0"
        lowerBuildLimit = "-64"
        upperBuildLimit = "320"
        vanillaPopulation = False
        mod_BOP = False
        mod_BYG = False
        mod_Terralith = False
        mod_Create = False
        mod_WilliamWythers = False
        custom_layers = ""
        terrainModifier = 0
        oreModifier = 8

        'Readonly Propertys for backward compatibility
        version = Nothing
        PathToScriptsFolder = Nothing
        PathToWorldPainterFolder = Nothing
        PathToQGIS = Nothing
        PathToMagick = Nothing
        PathToMinutor = Nothing
        PathToExport = Nothing
        Theme = Nothing
        Proxy = Nothing
        keepPbfFile = Nothing
        reUsePbfFile = Nothing
        keepOsmFiles = Nothing
        reUseOsmFiles = Nothing
        keepImageFiles = Nothing
        reUseImageFiles = Nothing
        keepWorldPainterFiles = Nothing
        minutor = Nothing
        NumberOfCores = Nothing
        ParallelWorldPainterGenerations = Nothing
        cmdVisibility = Nothing
        cmdPause = Nothing
        continueGeneration = Nothing
    End Sub

End Class

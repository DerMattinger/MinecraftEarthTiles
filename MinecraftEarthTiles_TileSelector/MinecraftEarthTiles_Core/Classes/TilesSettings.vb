Public Class TilesSettings
    Public version As String
    Public PathToScriptsFolder As String
    Public PathToWorldPainterFolder As String
    Public PathToQGIS As String
    Public PathToMagick As String
    Public PathToMinutor As String
    Public PathToExport As String
    Public Theme As String
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
    Public cmdPause As Boolean
    Public continueGeneration As Boolean
    Public alertAfterFinish As Boolean
    Public alertMail As String
    Public closeAfterFinish As Boolean

    ''' <summary>
    ''' Standard values for settings
    ''' </summary>
    Public Sub New()
        Dim assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
        version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
        PathToScriptsFolder = My.Application.Info.DirectoryPath
        PathToWorldPainterFolder = ""
        PathToQGIS = ""
        PathToMagick = ""
        PathToMinutor = ""
        PathToExport = ""
        Theme = "Light"
        Proxy = ""
        keepPbfFile = True
        reUsePbfFile = False
        keepOsmFiles = True
        reUseOsmFiles = False
        keepImageFiles = True
        reUseImageFiles = False
        keepWorldPainterFiles = True
        minutor = True
        NumberOfCores = "2"
        ParallelWorldPainterGenerations = True
        cmdVisibility = False
        cmdPause = False
        continueGeneration = False
        alertAfterFinish = False
        alertMail = ""
        closeAfterFinish = False
    End Sub

End Class

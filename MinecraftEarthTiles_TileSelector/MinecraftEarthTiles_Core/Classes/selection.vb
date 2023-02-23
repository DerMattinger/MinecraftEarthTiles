Public Class Selection
    Public FullTileList As List(Of String)
    Public TilesList As List(Of String)
    Public SpawnTile As String
    Public VoidBarrier As String
    Public VoidTiles As Int32

    Public Sub New()
        FullTileList = New List(Of String)
        TilesList = New List(Of String)
        SpawnTile = ""
        VoidBarrier = "0"
        VoidTiles = 0
    End Sub

End Class

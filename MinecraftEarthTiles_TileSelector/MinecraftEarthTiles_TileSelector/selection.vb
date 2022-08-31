Public Class Selection
    Public TilesList As List(Of String)
    Public SpawnTile As String
    Public VoidBarrier As String
    Public VoidTiles As Int32

    Public Sub New()
        TilesList = New List(Of String)
        SpawnTile = ""
        VoidBarrier = "0"
        VoidTiles = 0
    End Sub

End Class

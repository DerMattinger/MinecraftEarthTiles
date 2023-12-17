Public Class ecoregions

    Public name As String
    Public symbol As Int16
    Public color As String
    Public biome As String
    Public bop_biome As String
    Public byg_biome As String
    Public terralith_biome As String
    Public williamWythers_biome As String

    Sub New(name As String, symbol As Int16)
        Me.name = name
        Me.symbol = symbol
    End Sub

End Class

Module main

    Sub Main()

        Dim separators As String = " "
        Dim commands As String = Microsoft.VisualBasic.Command()
        Dim args() As String = commands.Split(separators.ToCharArray)

        Dim generationStep As String = "FALSE"
        If Not args(0) Is Nothing Then
            generationStep = args(0)
        End If
        Dim Tile As String = "FALSE"
        If Not args(0) Is Nothing Then
            Tile = args(1)
        End If
        Dim additionalInfo As String = "FALSE"
        If Not args(0) Is Nothing Then
            additionalInfo = args(2)
        End If

        Select Case generationStep
            Case "osmconvert"

                If additionalInfo = "FALSE" Or Tile = "FALSE" Then
                    Console.WriteLine("Missing parameters")
                    Console.ReadKey()
                Else
                    Select Case additionalInfo
                        Case "geofabrik"
                            If Not My.Computer.FileSystem.FileExists("download.osm.pbf") Then
                                Console.WriteLine("File 'download.osm.pbf' not found")
                                Console.ReadKey()
                            End If
                        Case "overpass"
                            If Not My.Computer.FileSystem.FileExists("osm/" & Tile & "/output.osm") Then
                                Console.WriteLine("File 'osm/" & Tile & "/output.osm' not found")
                                Console.ReadKey()
                            End If
                        Case Else
                            Console.WriteLine("Parameter not found: " & additionalInfo)
                            Console.ReadKey()
                    End Select
                End If

            Case "osmfilter"
            Case "qgis"
            Case "tartool"
            Case "gdal"
            Case "magick"
            Case "wpscript"
            Case "combine"
            Case Else
                Console.WriteLine("False or missing parameter: " & generationStep)
                Console.ReadKey()
        End Select
    End Sub

End Module

Imports System.Xml
Imports Microsoft.VisualBasic.FileIO

Module MainModule
    Sub Main()
        Dim clArgs() As String = Environment.GetCommandLineArgs()
        If clArgs.Count() < 3 Then
            Console.WriteLine("Error: Startup parameter missing. Use the following format:")
            Console.WriteLine("ecoregions.exe ecoregions.xml ecoregions.csv")
            Console.WriteLine("")
            Console.WriteLine("With the ecoregions.xml contains the color information and the ecoregion.csv contains the Minecraft Biomes.")
            Console.ReadKey()
            Exit Sub
        End If
        If Not My.Computer.FileSystem.FileExists(clArgs(1)) Then
            Console.WriteLine($"Error: File {clArgs(1)} not found.")
            Console.ReadKey()
            Exit Sub
        End If
        If Not My.Computer.FileSystem.FileExists(clArgs(2)) Then
            Console.WriteLine($"Error: File {clArgs(2)} not found.")
            Console.ReadKey()
            Exit Sub
        End If

        Dim listOfEcoRegions As New List(Of ecoregions)
        Dim doc As New XmlDocument
        doc.Load("ecoregions.xml")
        Dim categories As XmlNodeList = doc.GetElementsByTagName("category")
        For Each category As XmlNode In categories
            listOfEcoRegions.Add(New ecoregions(category.Attributes("label").Value, category.Attributes("symbol").Value))
        Next
        Dim symbols As XmlNodeList = doc.GetElementsByTagName("symbol")
        For Each symbol As XmlNode In symbols
            For Each ecoRegion In listOfEcoRegions
                If ecoRegion.symbol = symbol.Attributes("name").Value Then
                    ecoRegion.color = symbol.ChildNodes(0).Attributes("v").Value
                End If
            Next
        Next
        Dim biomesFile As New TextFieldParser("ecoregions.csv")
        biomesFile.Delimiters = New String() {","}
        biomesFile.TextFieldType = FieldType.Delimited
        biomesFile.ReadLine()
        While biomesFile.EndOfData = False
            Dim fields = biomesFile.ReadFields()
            For Each ecoRegion In listOfEcoRegions
                If ecoRegion.name = fields(0) Then
                    ecoRegion.biome = fields(1)
                    ecoRegion.bop_biome = fields(2)
                    ecoRegion.byg_biome = fields(3)
                    ecoRegion.terralith_biome = fields(4)
                End If
            Next
        End While
        Dim counter As Integer = 1
        For Each ecoRegion In listOfEcoRegions
            If ecoRegion.Equals(listOfEcoRegions.First) Then
                Console.WriteLine("   var eco_vanilla = wp.applyHeightMap(ecoRegionImage)")
                Console.WriteLine("    .toWorld(world)")
                Console.WriteLine("    .shift(shiftLongitute, shiftLatitude)")
                Console.WriteLine("    .applyToLayer(biomesLayer);")
                Console.WriteLine("    ")
            End If
            If ecoRegion.biome IsNot Nothing AndAlso ecoRegion.biome <> "" Then
                If counter Mod 64 = 0 AndAlso Not ecoRegion.Equals(listOfEcoRegions.Last) Then
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.biome & "); //" & ecoRegion.name)
                    Console.WriteLine("    ")
                ElseIf (counter - 1) Mod 64 = 0 Then
                    Console.WriteLine("    eco_vanilla = eco_vanilla.fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.biome & ") //" & ecoRegion.name)
                Else
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.biome & ") //" & ecoRegion.name)
                End If
                counter += 1
            End If
        Next
        Console.WriteLine("    .go();")
        Console.WriteLine("    ")

        Console.WriteLine("  if ( mod_BOP === ""True"" ) {")
        counter = 1
        For Each ecoRegion In listOfEcoRegions
            If ecoRegion.Equals(listOfEcoRegions.First) Then
                Console.WriteLine("   var eco_bop = wp.applyHeightMap(ecoRegionImage)")
                Console.WriteLine("    .toWorld(world)")
                Console.WriteLine("    .shift(shiftLongitute, shiftLatitude)")
                Console.WriteLine("    .applyToLayer(biomesLayer);")
                Console.WriteLine("    ")
            End If
            If ecoRegion.bop_biome IsNot Nothing AndAlso ecoRegion.bop_biome <> "" Then
                If counter Mod 64 = 0 AndAlso Not ecoRegion.Equals(listOfEcoRegions.Last) Then
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.bop_biome & "); //" & ecoRegion.name)
                    Console.WriteLine("    ")
                ElseIf (counter - 1) Mod 64 = 0 Then
                    Console.WriteLine("    eco_bop = eco_bop.fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.bop_biome & ") //" & ecoRegion.name)
                Else
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.bop_biome & ") //" & ecoRegion.name)
                End If
                counter += 1
            End If
        Next
        Console.WriteLine("    .go();")
        Console.WriteLine("  }")
        Console.WriteLine("   ")

        Console.WriteLine("  if ( mod_BYG  === ""True"" ) {")
        counter = 1
        For Each ecoRegion In listOfEcoRegions
            If ecoRegion.Equals(listOfEcoRegions.First) Then
                Console.WriteLine("   var eco_byg = wp.applyHeightMap(ecoRegionImage)")
                Console.WriteLine("    .toWorld(world)")
                Console.WriteLine("    .shift(shiftLongitute, shiftLatitude)")
                Console.WriteLine("    .applyToLayer(biomesLayer);")
                Console.WriteLine("    ")
            End If
            If ecoRegion.byg_biome IsNot Nothing AndAlso ecoRegion.byg_biome <> "" Then
                If counter Mod 64 = 0 AndAlso Not ecoRegion.Equals(listOfEcoRegions.Last) Then
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.byg_biome & "); //" & ecoRegion.name)
                    Console.WriteLine("    ")
                ElseIf (counter - 1) Mod 64 = 0 Then
                    Console.WriteLine("    eco_byg = eco_byg.fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.byg_biome & ") //" & ecoRegion.name)
                Else
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.byg_biome & ") //" & ecoRegion.name)
                End If
                counter += 1
            End If
        Next
        Console.WriteLine("    .go();")
        Console.WriteLine("  }")
        Console.WriteLine("   ")

        Console.WriteLine("  if ( mod_Terralith  === ""True"" ) {")
        counter = 1
        For Each ecoRegion In listOfEcoRegions
            If ecoRegion.Equals(listOfEcoRegions.First) Then
                Console.WriteLine("   var eco_terralith = wp.applyHeightMap(ecoRegionImage)")
                Console.WriteLine("    .toWorld(world)")
                Console.WriteLine("    .shift(shiftLongitute, shiftLatitude)")
                Console.WriteLine("    .applyToLayer(biomesLayer);")
                Console.WriteLine("    ")
            End If
            If ecoRegion.terralith_biome IsNot Nothing AndAlso ecoRegion.terralith_biome <> "" Then
                If counter Mod 64 = 0 AndAlso Not ecoRegion.Equals(listOfEcoRegions.Last) Then
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.terralith_biome & "); //" & ecoRegion.name)
                    Console.WriteLine("    ")
                ElseIf (counter - 1) Mod 64 = 0 Then
                    Console.WriteLine("    eco_terralith = eco_terralith.fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.terralith_biome & ") //" & ecoRegion.name)
                Else
                    Console.WriteLine("    .fromColour(" & ecoRegion.color & ").toLevel(BIOME_" & ecoRegion.terralith_biome & ") //" & ecoRegion.name)
                End If
                counter += 1
            End If
        Next
        Console.WriteLine("    .go();")
        Console.WriteLine("  }")
        Console.WriteLine("   ")

        Console.ReadKey()
    End Sub
End Module
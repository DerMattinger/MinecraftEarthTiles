Imports System.IO
Imports System.Reflection
Imports Microsoft.Win32

''' <summary>
''' 
''' </summary>
Class MainWindow

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub programmLoaded(sender As Object, e As EventArgs) Handles MyBase.Loaded
        For latitude As Integer = -180 To 179 Step 1
            For longitude As Integer = -89 To 90 Step 1
                Dim chb As New CheckBox()
                chb.IsChecked = False
                chb.Height = 4
                chb.Width = 4
                chb.Style = TryFindResource("EarthTilesCheckboxStyle")
                chb.HorizontalAlignment = HorizontalAlignment.Left
                chb.VerticalAlignment = VerticalAlignment.Top
                Dim TilesMargin As Thickness
                TilesMargin.Left = (latitude * 4) + 720
                TilesMargin.Top = (longitude * 4) + 356
                chb.Margin = TilesMargin

                Dim latitudeDirection As String = ""
                If (latitude < 0) Then
                    latitudeDirection = "W"
                Else
                    latitudeDirection = "E"
                End If

                Dim longitudeDirection As String = ""
                If (longitude < 1) Then
                    longitudeDirection = "N"
                Else
                    longitudeDirection = "S"
                End If

                Dim latitudeTemp As Integer = latitude
                If (latitudeTemp < 0) Then
                    latitudeTemp *= -1
                End If

                Dim longitudeTemp As Integer = longitude
                If (longitudeTemp < 0) Then
                    longitudeTemp *= -1
                End If

                Dim latitudeNumber As String = ""
                If (latitudeTemp < 10) Then
                    latitudeNumber = "00" + latitudeTemp.ToString
                ElseIf (latitudeTemp < 100) Then
                    latitudeNumber = "0" + latitudeTemp.ToString
                Else
                    latitudeNumber = latitudeTemp.ToString
                End If

                Dim longitudeNumber As String = ""
                If (longitudeTemp < 10) Then
                    longitudeNumber = "0" + longitudeTemp.ToString
                Else
                    longitudeNumber = longitudeTemp.ToString
                End If

                chb.Name = longitudeDirection + longitudeNumber + latitudeDirection + latitudeNumber
                chb.ToolTip = longitudeDirection + longitudeNumber + latitudeDirection + latitudeNumber

                Tiles.Children.Add(chb)

                txb_PathToScriptsFolder.Text = My.Application.Info.DirectoryPath
            Next
        Next
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_PathToScriptFolder_Click(sender As Object, e As RoutedEventArgs) Handles btn_PathToScriptsFolder.Click
        Dim MyFolderBrowserDialog As New Forms.FolderBrowserDialog()
        MyFolderBrowserDialog.SelectedPath = My.Application.Info.DirectoryPath
        If MyFolderBrowserDialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txb_PathToScriptsFolder.Text = MyFolderBrowserDialog.SelectedPath
        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_PathToWorldPainterFolder_Click(sender As Object, e As RoutedEventArgs) Handles btn_PathToWorldPainterFolder.Click
        Dim WorldPainterScriptFileDialog As New OpenFileDialog()
        WorldPainterScriptFileDialog.FileName = "wpscript.exe"
        WorldPainterScriptFileDialog.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*"
        WorldPainterScriptFileDialog.FilterIndex = 1
        If WorldPainterScriptFileDialog.ShowDialog() = True Then
            txb_PathToWorldPainterFile.Text = WorldPainterScriptFileDialog.FileName
        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_SaveSelection_Click(sender As Object, e As RoutedEventArgs) Handles btn_SaveSelection.Click
        Dim TilesList =
            (
                From T In Me.Tiles.Children.OfType(Of CheckBox)()
                Where T.IsChecked Select T.Name
            ).ToList
        Dim ExportPathName As String = ""
        If txb_PathToScriptsFolder.Text = "" Then
            ExportPathName = My.Application.Info.DirectoryPath
        Else
            ExportPathName = txb_PathToScriptsFolder.Text
        End If
        TilesList.Sort()
        Try
            CustomXmlSerialiser.SaveXML(ExportPathName + "\tiles.xml", TilesList, TilesList.GetType)
            MsgBox("Selection saved as 'tiles.xml'")
        Catch ex As Exception
            MsgBox("File 'tiles.xml' could not be saved")
        End Try
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_LoadSelection_Click(sender As Object, e As RoutedEventArgs) Handles btn_LoadSelection.Click
        Dim ImportPathName As String = ""
        If txb_PathToScriptsFolder.Text = "" Then
            ImportPathName = My.Application.Info.DirectoryPath
        Else
            ImportPathName = txb_PathToScriptsFolder.Text
        End If
        Dim TilesList As List(Of String) = New List(Of String)
        If My.Computer.FileSystem.FileExists(ImportPathName + "\tiles.xml") Then
            Try
                TilesList = CustomXmlSerialiser.GetXML(ImportPathName + "\tiles.xml", TilesList, TilesList.GetType)
                MsgBox("Selection loaded from 'tiles.xml'")
            Catch ex As Exception
                MsgBox("Error while parsing 'tiles.xml'. Is the file correct?\n" + ex.Message)
            End Try
        Else
            MsgBox("Could not find File 'tiles.xml'")
        End If
        For Each Checkbox In Me.Tiles.Children.OfType(Of CheckBox)
            If TilesList.Contains(Checkbox.Name) Then
                Checkbox.IsChecked = True
            Else
                Checkbox.IsChecked = False
            End If
        Next
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_WPScriptExport_Click(sender As Object, e As RoutedEventArgs) Handles btn_WPScriptExport.Click
        Dim ImportPathName As String = ""
        If txb_PathToScriptsFolder.Text = "" Then
            ImportPathName = My.Application.Info.DirectoryPath
        Else
            ImportPathName = txb_PathToScriptsFolder.Text
        End If

        If txb_PathToWorldPainterFile.Text = "" Then
            MsgBox("Please enter the path to the 'wpscript.exe' file")
        Else
            Try
                Dim ScriptBatchFile As System.IO.StreamWriter
                ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ImportPathName + "\wpscript.bat", False, System.Text.Encoding.ASCII)
                Dim TilesList =
                (
                    From T In Me.Tiles.Children.OfType(Of CheckBox)()
                    Where T.IsChecked Select T.Name
                ).ToList
                TilesList.Sort()
                For Each Tile In TilesList
                    Dim LatiDir = Tile.Substring(0, 1)
                    Dim LatiNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                    Dim LongDir = Tile.Substring(3, 1)
                    Dim LongNumber As Int32 = 0
                    Int32.TryParse(Tile.Substring(4, 3), LongNumber)
                    Dim ReplacedString = LatiDir + " " + LatiNumber.ToString + " " + LongDir + " " + LongNumber.ToString
                    ScriptBatchFile.WriteLine(Chr(34) + txb_PathToWorldPainterFile.Text + Chr(34) + " wpscript.js " + Chr(34) + ImportPathName.Replace("\", "/") + "/" + Chr(34) + " " + ReplacedString + " " + cbb_BlocksPerTile.Text)
                Next
                ScriptBatchFile.WriteLine("PAUSE")
                ScriptBatchFile.Close()
                MsgBox("WorldPainter script batch file saved as 'wpscript.bat'")
            Catch ex As Exception
                MsgBox("File 'wpscript.bat' could not be saved\n" + ex.Message)
            End Try
        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_QgisExport_Click(sender As Object, e As RoutedEventArgs) Handles btn_QgisExport.Click
        Dim ImportPathName As String = ""
        If txb_PathToScriptsFolder.Text = "" Then
            ImportPathName = My.Application.Info.DirectoryPath
        Else
            ImportPathName = txb_PathToScriptsFolder.Text
        End If

        Try
            Dim ScriptBatchFile As System.IO.StreamWriter
            ScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ImportPathName + "\qgis.py", False, System.Text.Encoding.ASCII)
            Dim TilesList =
            (
                From T In Me.Tiles.Children.OfType(Of CheckBox)()
                Where T.IsChecked Select T.Name
            ).ToList
            TilesList.Sort()

            Dim AllTilesString As String = "["

            For Each Tile In TilesList
                If Tile Is TilesList.Last Then
                    AllTilesString = AllTilesString + Chr(34) + Tile + Chr(34) + "]"
                Else
                    AllTilesString = AllTilesString + Chr(34) + Tile + Chr(34) + ", "
                End If
            Next

            ScriptBatchFile.WriteLine("import os" + Environment.NewLine +
                                      Environment.NewLine +
                                      "path = '" + ImportPathName.Replace("\", "/") + "/'" + Environment.NewLine +
                                      "tiles = " + AllTilesString + Environment.NewLine +
                                      "scale = " + cbb_BlocksPerTile.Text + Environment.NewLine +
                                      Environment.NewLine +
                                      Environment.NewLine +
                                      "")

            Dim QgisBasescript As String = My.Resources.ResourceManager.GetString("basescript")
            ScriptBatchFile.WriteLine(QgisBasescript)
            ScriptBatchFile.Close()
            MsgBox("Qgis console script file saved as 'qgis.py'. Copy the code and place it inside the Qgis project.")
        Catch ex As Exception
            MsgBox("File 'qgis.py' could not be saved\n" + ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub Btn_ClearSelection_Click(sender As Object, e As RoutedEventArgs) Handles btn_ClearSelection.Click
        For Each Checkbox In Me.Tiles.Children.OfType(Of CheckBox)
            Checkbox.IsChecked = False
        Next
    End Sub

    Private Sub Btn_osmbatExport_Click(sender As Object, e As RoutedEventArgs) Handles btn_osmbatExport.Click
        Dim ImportPathName As String = ""
        If txb_PathToScriptsFolder.Text = "" Then
            ImportPathName = My.Application.Info.DirectoryPath
        Else
            ImportPathName = txb_PathToScriptsFolder.Text
        End If

        Try
            Dim OsmScriptBatchFile As System.IO.StreamWriter
            OsmScriptBatchFile = My.Computer.FileSystem.OpenTextFileWriter(ImportPathName + "\osmconvert.bat", False, System.Text.Encoding.ASCII)
            Dim TilesList =
                (
                    From T In Me.Tiles.Children.OfType(Of CheckBox)()
                    Where T.IsChecked Select T.Name
                ).ToList
            TilesList.Sort()
            Dim biggest_lati As Int16 = 0
            Dim biggest_long As Int16 = 0
            Dim smallest_lati As Int16 = 0
            Dim smallest_long As Int16 = 0
            For Each Tile In TilesList
                Dim LatiDir = Tile.Substring(0, 1)
                Dim LatiNumber As Int32 = 0
                Int32.TryParse(Tile.Substring(1, 2), LatiNumber)
                Dim LongDir = Tile.Substring(3, 1)
                Dim LongNumber As Int32 = 0
                Int32.TryParse(Tile.Substring(4, 3), LongNumber)
                If LatiDir = "S" Then
                    LatiNumber *= -1
                End If
                If LongDir = "W" Then
                    LongNumber *= -1
                End If
                If Tile = TilesList.First Then
                    smallest_lati = LatiNumber
                    biggest_lati = LatiNumber
                    smallest_long = LongNumber
                    biggest_long = LongNumber
                End If
                If LatiNumber < smallest_lati Then
                    smallest_lati = LatiNumber
                End If
                If LatiNumber > biggest_lati Then
                    biggest_lati = LatiNumber
                End If
                If LongNumber < smallest_long Then
                    smallest_long = LongNumber
                End If
                If LongNumber > biggest_long Then
                    biggest_long = LongNumber
                End If
            Next
            OsmScriptBatchFile.WriteLine("osmconvert planet.pbf -b=" + (smallest_long - 1).ToString + "," + (smallest_lati - 1).ToString + "," + (biggest_long + 2).ToString + "," + (biggest_lati + 2).ToString + " --complete-multipolygons -o=osm/output.o5m")

            Dim OsmScrip As String = My.Resources.ResourceManager.GetString("osmscript")
            OsmScriptBatchFile.WriteLine(OsmScrip)

            OsmScriptBatchFile.WriteLine("PAUSE")
            OsmScriptBatchFile.Close()
            MsgBox("OSM script batch file saved as 'osmconvert.bat'")
        Catch ex As Exception
            MsgBox("File 'osmconvert.bat' could not be saved\n" + ex.Message)
        End Try
    End Sub
End Class

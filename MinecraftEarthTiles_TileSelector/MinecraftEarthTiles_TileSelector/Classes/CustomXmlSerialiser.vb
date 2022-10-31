Imports System.Xml.Serialization
Imports System.Text
Imports System.IO

Public Class CustomXmlSerialiser

    Public Shared Function SaveXML(ByVal FileName As String, ByVal DataToSerialize As Object) As Boolean
        Dim nsBlank As New XmlSerializerNamespaces
        nsBlank.Add("", "")
        Dim xSettings As New System.Xml.XmlWriterSettings
        With xSettings
            .Encoding = Encoding.UTF8
            .Indent = True
            .NewLineChars = Environment.NewLine
            .NewLineOnAttributes = False
            .ConformanceLevel = Xml.ConformanceLevel.Document
        End With
        Dim xw As System.Xml.XmlWriter = Xml.XmlWriter.Create(FileName, xSettings)
        Dim writer As New XmlSerializer(DataToSerialize.GetType)
        writer.Serialize(xw, DataToSerialize, nsBlank)
        xw.Close()
        Return True
    End Function

    Public Shared Function GetXMLSelection(ByVal sFileName As String) As Selection
        Dim MySelection As New Selection
        If My.Computer.FileSystem.FileExists(sFileName) Then
            Dim fs As FileStream = New FileStream(sFileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(MySelection.GetType)
            MySelection = CType(xs.Deserialize(fs), Selection)
            fs.Close()
            Return MySelection
        Else
            Return MySelection
        End If
    End Function

    Public Shared Function GetXMLSettings(ByVal sFileName As String) As Settings
        Dim MySettings As New Settings
        If My.Computer.FileSystem.FileExists(sFileName) Then
            Dim fs As FileStream = New FileStream(sFileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(MySettings.GetType)
            MySettings = CType(xs.Deserialize(fs), Settings)
            fs.Close()
            Return MySettings
        Else
            Return MySettings
        End If
    End Function

End Class

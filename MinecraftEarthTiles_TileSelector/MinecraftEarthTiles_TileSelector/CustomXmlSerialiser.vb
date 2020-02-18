Imports System.Xml.Serialization
Imports System.Text
Imports System.IO

Public Class CustomXmlSerialiser

    ''' <summary>XML Datei Speichern</summary>
    ''' <param name="FileName"></param>
    ''' <param name="DataToSerialize"></param>
    ''' <param name="objType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function SaveXML(ByVal FileName As String, ByVal DataToSerialize As Object, ByVal objType As Type) As Boolean
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
        Try
            Dim xw As System.Xml.XmlWriter = Xml.XmlWriter.Create(FileName, xSettings)
            Dim writer As New XmlSerializer(objType)
            writer.Serialize(xw, DataToSerialize, nsBlank)
            xw.Close()
            Return True
        Catch ex As Exception
            MsgBox(ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>XML Datei Lesen</summary>
    ''' <param name="sFileName"></param>
    ''' <param name="objType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetXMLSelection(ByVal sFileName As String, ByVal selection As Selection, ByVal objType As Type) As Selection
        If My.Computer.FileSystem.FileExists(sFileName) Then
            Dim fs As FileStream = New FileStream(sFileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(objType)
            selection = CType(xs.Deserialize(fs), Selection)
            fs.Close()
            Return selection
        Else
            Return selection
        End If
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="sFileName"></param>
    ''' <param name="MySettings"></param>
    ''' <param name="objType"></param>
    ''' <returns></returns>
    Public Shared Function GetXMLSettings(ByVal sFileName As String, ByVal MySettings As settings, ByVal objType As Type) As settings
        If My.Computer.FileSystem.FileExists(sFileName) Then
            Dim fs As FileStream = New FileStream(sFileName, FileMode.Open)
            Dim xs As XmlSerializer = New XmlSerializer(objType)
            MySettings = CType(xs.Deserialize(fs), settings)
            fs.Close()
            Return MySettings
        Else
            Return MySettings
        End If
    End Function

End Class

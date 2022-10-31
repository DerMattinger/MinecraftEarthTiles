Public Class GeneratedImagePreview

    Private imagePath As String
    Private imageLoaded As Boolean
    Dim logo As New BitmapImage()

    Sub New(path As String, width As Integer, tileavailable As Boolean)
        InitializeComponent()
        If tileavailable Then
            imagePath = path
            img_preview.Visibility = Visibility.Visible
            loadImage()
        Else
            img_preview_nodrendered.Visibility = Visibility.Visible
            imageLoaded = True
        End If
    End Sub

    Private Sub loadImage()
        If My.Computer.FileSystem.FileExists(imagePath) Then
            logo.BeginInit()
            logo.UriSource = New Uri(imagePath)
            logo.EndInit()
            img_preview.Source = logo
            imageLoaded = True
        End If
    End Sub

    Public Sub refresh()
        If imageLoaded = False Then
            loadImage()
        End If
    End Sub


    Public Sub Dispose()
        img_preview.Source = Nothing
        imageLoaded = False
    End Sub

End Class

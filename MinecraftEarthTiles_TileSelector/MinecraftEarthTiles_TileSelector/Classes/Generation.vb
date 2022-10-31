Imports System.ComponentModel

Public Class Generation
    Implements INotifyPropertyChanged

    Private _TileName As String
    Private _GenerationProgress As Int16
    Private _Comment As String

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(ByVal propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property TileName As String
        Get
            Return _TileName
        End Get
        Set(ByVal value As String)
            _TileName = value
            OnPropertyChanged("TileName")
        End Set
    End Property

    Public Property Comment As String
        Get
            Return _Comment
        End Get
        Set(ByVal value As String)
            _Comment = value
            OnPropertyChanged("Comment")
        End Set
    End Property

    Public Property GenerationProgress As Int16
        Get
            Return _GenerationProgress
        End Get
        Set(ByVal value As Int16)
            _GenerationProgress = value
            OnPropertyChanged("GenerationProgress")
        End Set
    End Property

    Public Sub New(Tile As String)
        _TileName = Tile
        _GenerationProgress = 0
        _Comment = ""
    End Sub

End Class

﻿#ExternalChecksum("..\..\..\Windows\PreviewWindow.xaml","{8829d00f-11b8-4213-878b-770e8597ac16}","8C9ED7D09578C962AB96B3A5CD3C37CC68A9647ED3A8A27CD5E6FB7B88929960")
'------------------------------------------------------------------------------
' <auto-generated>
'     Dieser Code wurde von einem Tool generiert.
'     Laufzeitversion:4.0.30319.42000
'
'     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
'     der Code erneut generiert wird.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict Off
Option Explicit On

Imports MaterialDesignThemes.Wpf
Imports MaterialDesignThemes.Wpf.Converters
Imports MaterialDesignThemes.Wpf.Transitions
Imports MinecraftEarthTiles_GUI
Imports System
Imports System.Diagnostics
Imports System.Windows
Imports System.Windows.Automation
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Ink
Imports System.Windows.Input
Imports System.Windows.Markup
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Effects
Imports System.Windows.Media.Imaging
Imports System.Windows.Media.Media3D
Imports System.Windows.Media.TextFormatting
Imports System.Windows.Navigation
Imports System.Windows.Shapes
Imports System.Windows.Shell


'''<summary>
'''PreviewWindow
'''</summary>
<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>  _
Partial Public Class PreviewWindow
    Inherits System.Windows.Window
    Implements System.Windows.Markup.IComponentConnector
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",32)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Close_Preview As System.Windows.Controls.Button
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",40)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents lbl_ZoomSlider As System.Windows.Controls.Label
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",41)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents zsl_ZoomSlider As System.Windows.Controls.Slider
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",44)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents ScrollViewer As System.Windows.Controls.ScrollViewer
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",46)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents Tiles As System.Windows.Controls.Grid
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",49)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents scaleTransform As System.Windows.Media.ScaleTransform
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",52)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents img_Background As System.Windows.Controls.Image
    
    #End ExternalSource
    
    Private _contentLoaded As Boolean
    
    '''<summary>
    '''InitializeComponent
    '''</summary>
    <System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")>  _
    Public Sub InitializeComponent() Implements System.Windows.Markup.IComponentConnector.InitializeComponent
        If _contentLoaded Then
            Return
        End If
        _contentLoaded = true
        Dim resourceLocater As System.Uri = New System.Uri("/MinecraftEarthTiles_GUI;component/windows/previewwindow.xaml", System.UriKind.Relative)
        
        #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",1)
        System.Windows.Application.LoadComponent(Me, resourceLocater)
        
        #End ExternalSource
    End Sub
    
    <System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0"),  _
     System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")>  _
    Sub System_Windows_Markup_IComponentConnector_Connect(ByVal connectionId As Integer, ByVal target As Object) Implements System.Windows.Markup.IComponentConnector.Connect
        If (connectionId = 1) Then
            
            #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",9)
            AddHandler CType(target,PreviewWindow).Loaded, New System.Windows.RoutedEventHandler(AddressOf Me.Window_Loaded)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 2) Then
            
            #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",18)
            AddHandler CType(target,System.Windows.Controls.MenuItem).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Close_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 3) Then
            
            #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",24)
            AddHandler CType(target,System.Windows.Controls.MenuItem).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Help_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 4) Then
            Me.btn_Close_Preview = CType(target,System.Windows.Controls.Button)
            
            #ExternalSource("..\..\..\Windows\PreviewWindow.xaml",32)
            AddHandler Me.btn_Close_Preview.Click, New System.Windows.RoutedEventHandler(AddressOf Me.Close_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 5) Then
            Me.lbl_ZoomSlider = CType(target,System.Windows.Controls.Label)
            Return
        End If
        If (connectionId = 6) Then
            Me.zsl_ZoomSlider = CType(target,System.Windows.Controls.Slider)
            Return
        End If
        If (connectionId = 7) Then
            Me.ScrollViewer = CType(target,System.Windows.Controls.ScrollViewer)
            Return
        End If
        If (connectionId = 8) Then
            Me.Tiles = CType(target,System.Windows.Controls.Grid)
            Return
        End If
        If (connectionId = 9) Then
            Me.scaleTransform = CType(target,System.Windows.Media.ScaleTransform)
            Return
        End If
        If (connectionId = 10) Then
            Me.img_Background = CType(target,System.Windows.Controls.Image)
            Return
        End If
        Me._contentLoaded = true
    End Sub
End Class


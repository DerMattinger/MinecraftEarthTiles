﻿#ExternalChecksum("..\..\..\Windows\GenerationWindow.xaml","{8829d00f-11b8-4213-878b-770e8597ac16}","B928E02EAB66CBF24DF4F45A671B6585DF6F6746FB2D00C4AAA95647260EDD72")
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
'''GenerationWindow
'''</summary>
<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>  _
Partial Public Class GenerationWindow
    Inherits System.Windows.Window
    Implements System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",18)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_preview As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",23)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_debugZip As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",35)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents mnu_Start As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",39)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Generate_Batch_Files As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",44)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Generate_OSM_Only As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",49)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Generate_Images_Only As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",54)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Generate_WorldPainter_Only As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",59)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Combining_Only As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",64)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Cleanup_Only As System.Windows.Controls.MenuItem
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",78)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents ckb_pause As System.Windows.Controls.CheckBox
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",80)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Start As System.Windows.Controls.Button
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",87)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents btn_Stop As System.Windows.Controls.Button
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",96)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents lbl_elapsed_time As System.Windows.Controls.Label
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",97)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents lbl_elapsed_time_Seperator As System.Windows.Controls.Label
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",98)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents lbl_Estimated_Duration As System.Windows.Controls.Label
    
    #End ExternalSource
    
    
    #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",101)
    <System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")>  _
    Friend WithEvents dgr_Tiles As System.Windows.Controls.DataGrid
    
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
        Dim resourceLocater As System.Uri = New System.Uri("/MinecraftEarthTiles_GUI;component/windows/generationwindow.xaml", System.UriKind.Relative)
        
        #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",1)
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
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",8)
            AddHandler CType(target,GenerationWindow).Loaded, New System.Windows.RoutedEventHandler(AddressOf Me.Window_Loaded)
            
            #End ExternalSource
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",8)
            AddHandler CType(target,GenerationWindow).Closing, New System.ComponentModel.CancelEventHandler(AddressOf Me.window_closing)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 2) Then
            Me.btn_preview = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",18)
            AddHandler Me.btn_preview.Click, New System.Windows.RoutedEventHandler(AddressOf Me.preview_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 3) Then
            Me.btn_debugZip = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",23)
            AddHandler Me.btn_debugZip.Click, New System.Windows.RoutedEventHandler(AddressOf Me.DebugZip_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 4) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",29)
            AddHandler CType(target,System.Windows.Controls.MenuItem).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Close_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 5) Then
            Me.mnu_Start = CType(target,System.Windows.Controls.MenuItem)
            Return
        End If
        If (connectionId = 6) Then
            Me.btn_Generate_Batch_Files = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",39)
            AddHandler Me.btn_Generate_Batch_Files.Click, New System.Windows.RoutedEventHandler(AddressOf Me.BatchFiles_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 7) Then
            Me.btn_Generate_OSM_Only = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",44)
            AddHandler Me.btn_Generate_OSM_Only.Click, New System.Windows.RoutedEventHandler(AddressOf Me.OsmOnly_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 8) Then
            Me.btn_Generate_Images_Only = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",49)
            AddHandler Me.btn_Generate_Images_Only.Click, New System.Windows.RoutedEventHandler(AddressOf Me.ImagesOnly_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 9) Then
            Me.btn_Generate_WorldPainter_Only = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",54)
            AddHandler Me.btn_Generate_WorldPainter_Only.Click, New System.Windows.RoutedEventHandler(AddressOf Me.WorldPainterOnly_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 10) Then
            Me.btn_Combining_Only = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",59)
            AddHandler Me.btn_Combining_Only.Click, New System.Windows.RoutedEventHandler(AddressOf Me.CombineOnly_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 11) Then
            Me.btn_Cleanup_Only = CType(target,System.Windows.Controls.MenuItem)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",64)
            AddHandler Me.btn_Cleanup_Only.Click, New System.Windows.RoutedEventHandler(AddressOf Me.CleanupOnly_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 12) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",70)
            AddHandler CType(target,System.Windows.Controls.MenuItem).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Help_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 13) Then
            Me.ckb_pause = CType(target,System.Windows.Controls.CheckBox)
            Return
        End If
        If (connectionId = 14) Then
            Me.btn_Start = CType(target,System.Windows.Controls.Button)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",80)
            AddHandler Me.btn_Start.Click, New System.Windows.RoutedEventHandler(AddressOf Me.Start_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 15) Then
            Me.btn_Stop = CType(target,System.Windows.Controls.Button)
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",87)
            AddHandler Me.btn_Stop.Click, New System.Windows.RoutedEventHandler(AddressOf Me.Stop_Click)
            
            #End ExternalSource
            Return
        End If
        If (connectionId = 16) Then
            Me.lbl_elapsed_time = CType(target,System.Windows.Controls.Label)
            Return
        End If
        If (connectionId = 17) Then
            Me.lbl_elapsed_time_Seperator = CType(target,System.Windows.Controls.Label)
            Return
        End If
        If (connectionId = 18) Then
            Me.lbl_Estimated_Duration = CType(target,System.Windows.Controls.Label)
            Return
        End If
        If (connectionId = 19) Then
            Me.dgr_Tiles = CType(target,System.Windows.Controls.DataGrid)
            Return
        End If
        Me._contentLoaded = true
    End Sub
    
    <System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0"),  _
     System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily"),  _
     System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")>  _
    Sub System_Windows_Markup_IStyleConnector_Connect(ByVal connectionId As Integer, ByVal target As Object) Implements System.Windows.Markup.IStyleConnector.Connect
        If (connectionId = 20) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",128)
            AddHandler CType(target,System.Windows.Controls.Button).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Log_Click)
            
            #End ExternalSource
        End If
        If (connectionId = 21) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",129)
            AddHandler CType(target,System.Windows.Controls.Button).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Folder_OSM_Click)
            
            #End ExternalSource
        End If
        If (connectionId = 22) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",130)
            AddHandler CType(target,System.Windows.Controls.Button).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Folder_Images_Click)
            
            #End ExternalSource
        End If
        If (connectionId = 23) Then
            
            #ExternalSource("..\..\..\Windows\GenerationWindow.xaml",131)
            AddHandler CType(target,System.Windows.Controls.Button).Click, New System.Windows.RoutedEventHandler(AddressOf Me.Folder_Batch_Click)
            
            #End ExternalSource
        End If
    End Sub
End Class


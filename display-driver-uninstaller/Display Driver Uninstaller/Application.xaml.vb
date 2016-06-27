Imports System.Threading
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Markup

Class Application
	' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
	' can be handled in this file.
	Private Shared m_dataSaved As Boolean = False
    Private Shared m_Data As Data
	Public Shared ReadOnly Property Data As Data
		Get
			Return m_Data
		End Get
	End Property

    Public Shared ReadOnly Property Settings As AppSettings
        Get
            Return m_Data.Settings
        End Get
    End Property
    Public Shared ReadOnly Property Paths As AppPaths
        Get
            Return m_Data.Paths
        End Get
    End Property
    Public Shared ReadOnly Property Log As AppLog
        Get
            Return m_Data.Log
        End Get
    End Property

    Private Sub App_DispatcherUnhandledException(ByVal sender As Object, ByVal e As Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        'TODO: CRITICAL FAILURES ARE HANDLED HERE


        e.Handled = True 'Close the app
    End Sub

    Public Sub New()
        m_Data = New Data()

        'ALL Exceptions are shown in English
        Thread.CurrentThread.CurrentCulture = New CultureInfo("en-US")
        Thread.CurrentThread.CurrentUICulture = New CultureInfo("en-US")

        FrameworkElement.LanguageProperty.OverrideMetadata(GetType(FrameworkElement),
           New FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)))
	End Sub

	Public Shared Sub SaveData()
		If Not m_dataSaved Then
			Settings.Save()
			Log.SaveToFile()

			m_dataSaved = True
		End If
	End Sub
End Class

Public Module Generic

	''' <summary>Alias for MessageBox.Show(message) as defaults settings: only 'OK' button + 'Information' image</summary>
	Public Function MsgBox(ByVal message As String, Optional ByVal buttons As MessageBoxButton = MessageBoxButton.OK, Optional ByVal image As MessageBoxImage = MessageBoxImage.Information) As MessageBoxResult
		Return MessageBox.Show(message, Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
	End Function

	''' <summary>Alias for MessageBox.Show(message, title) as defaults settings: only 'OK' button + 'Information' image</summary>
	Public Function MsgBox(ByVal message As String, ByVal title As String, Optional ByVal buttons As MessageBoxButton = MessageBoxButton.OK, Optional ByVal image As MessageBoxImage = MessageBoxImage.Information) As MessageBoxResult
		Return MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information)
	End Function

End Module

Public Class Data
	Private m_settings As AppSettings
	Private m_paths As AppPaths
	Private m_log As AppLog
	Private m_debug As Boolean = System.Diagnostics.Debugger.IsAttached

	Public Property IsDebug As Boolean
		Get
			Return m_debug
		End Get
		Set(value As Boolean)
			m_debug = value
		End Set
	End Property

	Public ReadOnly Property Settings As AppSettings
		Get
			Return m_settings
		End Get
	End Property
	Public ReadOnly Property Paths As AppPaths
		Get
			Return m_paths
		End Get
	End Property
	Public ReadOnly Property Log As AppLog
		Get
			Return m_log
		End Get
	End Property

	Public Sub New()
		m_settings = New AppSettings
		m_paths = New AppPaths
		m_log = New AppLog
	End Sub
End Class


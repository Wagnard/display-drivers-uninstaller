Imports System.Threading
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Markup
Imports System.Windows.Threading

Class Application
	' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
	' can be handled in this file.
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
End Class

Public Class Data
	Private m_settings As AppSettings
	Private m_paths As AppPaths
	Private m_log As AppLog

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
		m_paths = New AppPaths
		m_settings = New AppSettings
		m_log = New AppLog
	End Sub
End Class


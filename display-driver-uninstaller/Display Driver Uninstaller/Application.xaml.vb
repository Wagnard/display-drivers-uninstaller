Imports System.IO
Imports System.Threading
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Markup
Imports System.Text

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



	Private Sub InitLanguages()
		Dim defaultLang As Languages.LanguageOption = Languages.DefaultEng
		Dim foundLangs As List(Of Languages.LanguageOption) = Languages.ScanFolderForLang(Application.Paths.Language)

		foundLangs.Add(defaultLang)
		foundLangs.Sort(Function(x, y) x.DisplayText.CompareTo(y.DisplayText))

		For Each lang As Languages.LanguageOption In foundLangs
			Application.Settings.LanguageOptions.Add(lang)
		Next

		Languages.Load()		'default = english

		ExtractEnglishLangFile(Application.Paths.Language & "English.xml", Languages.DefaultEng)
	End Sub

	Private Sub SelectLanguage()
		Dim systemlang As String = PreferredUILanguages()
		Dim lastUsedLang As Languages.LanguageOption = Nothing
		Dim nativeLang As Languages.LanguageOption = Nothing

		For Each item As Languages.LanguageOption In Application.Settings.LanguageOptions
			If lastUsedLang Is Nothing AndAlso item.Equals(Application.Settings.SelectedLanguage) Then
				lastUsedLang = item
			End If

			If nativeLang Is Nothing AndAlso systemlang.Equals(item.ISOLanguage, StringComparison.OrdinalIgnoreCase) Then
				nativeLang = item		'take native on hold incase last used language not found (avoid multiple loops)
			End If
		Next

		If lastUsedLang IsNot Nothing Then
			Application.Settings.SelectedLanguage = lastUsedLang
		Else
			If nativeLang IsNot Nothing Then
				Application.Settings.SelectedLanguage = nativeLang				'couldn't find last used, using native lang
			Else
				Application.Settings.SelectedLanguage = Languages.DefaultEng	'couldn't find last used nor native lang, using default (English)
			End If
		End If
	End Sub

	Private Sub ExtractEnglishLangFile(ByVal fileName As String, ByVal langEng As Languages.LanguageOption)
		Using stream As Stream = Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("{0}.{1}", GetType(Languages).Namespace, "English.xml"))
			If FileIO.ExistsFile(fileName) Then
				Using fsEnglish As FileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None)
					If CompareStreams(stream, fsEnglish) Then
						Return
					End If
				End Using
			End If

			stream.Position = 0L

			Using sr As New StreamReader(stream, Encoding.UTF8, True)
				Using sw As New StreamWriter(fileName, False, Encoding.UTF8)
					While (sr.Peek() <> -1)
						sw.WriteLine(sr.ReadLine())
					End While

					sw.Flush()
					sw.Close()
				End Using

				sr.Close()
			End Using
		End Using
	End Sub

	Private Sub AppStart()
		'Here is change to anything which doesn't require UI

		' ....

		' Loading UI

		Try
			Dim mainWindow As frmMain = New frmMain()
			AddHandler mainWindow.Closed, AddressOf AppClose

			'	Launching frmMain, triggers Events
			'	-> frmMain_Sourceinitialized
			'	-> frmMain_Loaded

			mainWindow.Show()

		Catch ex As Exception
			MessageBox.Show("Launching Main Window failed!" & Environment.NewLine & ex.Message)
			Me.Shutdown(0)
		End Try
	End Sub

	Private Sub AppClose(ByVal sender As Object, ByVal e As System.EventArgs)
		Try
			' frmMain is already closed here

			SaveData()

		Finally
			Me.Shutdown(0)	' Close application
		End Try
	End Sub

	Private Sub Application_Startup(sender As Object, e As System.Windows.StartupEventArgs) Handles Me.Startup
		InitLanguages()					' Load default language (English) + Find language files from folder
		Application.Settings.Load()		' Load AppSettings and select last used language (if settings exists)
		SelectLanguage()				' Select language (last used -> native -> default)

		AppStart()
	End Sub

	' Launching application, Event order
	'	Application : Sub New()
	'
	'	-> Application_Startup	(Event)
	'	---> AppStart()
	'
	'	-> frmMain_Sourceinitialized	(Event)
	'	-> frmMain_Loaded	(Event)


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


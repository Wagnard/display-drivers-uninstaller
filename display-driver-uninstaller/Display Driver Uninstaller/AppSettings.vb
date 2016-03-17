Imports System.IO
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel

Public Class ApplicationSettings
	Inherits DependencyObject

	'Class is reserver for ALL settings ( DontCheckUpdates, Silent etc...)

	Public Shared Property AppName As String		' Name of application (DDU)
	Public Shared Property AppExeFile As String	' Fullpath to application's .Exe
	Public Shared Property DirApp As String		' Fullpath to application's directory (where .exe is)
	Public Shared Property DirSettings As String	' DirApp + \Settings\
	Public Shared Property DirLanguage As String	' DirApp + \Settings\Langauges\

	Public Property Main As ViewMain

	' Register values for 'Binding' (Don't need to undestand)
	Friend Shared Function Reg(ByVal s As String, ByVal t As Type, ByVal c As Type, ByVal m As Object) As DependencyProperty
		If TypeOf (m) Is FrameworkPropertyMetadata Then
			Return DependencyProperty.Register(s, t, c, CType(m, FrameworkPropertyMetadata))
		Else
			Return DependencyProperty.Register(s, t, c, New PropertyMetadata(m))
		End If
	End Function

	Public Sub New()
		If Windows.Application.ResourceAssembly IsNot Nothing Then
			AppName = Application.ResourceAssembly.GetName().Name
		Else
			AppName = "Display Driver Uninstaller"
		End If

		AppExeFile = Assembly.GetExecutingAssembly().Location
		DirApp = Path.GetDirectoryName(AppExeFile)

		If Not DirApp.EndsWith("\") Then
			DirApp &= "\"
		End If

		DirSettings = DirApp & "Settings\"
		DirLanguage = DirSettings & "Languages\"

		If Not Directory.Exists(DirSettings) Then
			Directory.CreateDirectory(DirSettings)
		End If

		If Not Directory.Exists(DirLanguage) Then
			Directory.CreateDirectory(DirLanguage)
		End If

		Main = New ViewMain()
	End Sub

	Public Class ViewMain
		Inherits DependencyObject

		Public Property LanguageOptions As ObservableCollection(Of Languages.LanguageOption)

		Public Sub New()
			LanguageOptions = New ObservableCollection(Of Languages.LanguageOption)()
		End Sub
	End Class
End Class

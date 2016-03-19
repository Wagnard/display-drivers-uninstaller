Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel

Public Class AppSettings
	Inherits DependencyObject
	'Class is reserver for ALL settings ( DontCheckUpdates, Silent etc...)

	Private m_appname As DependencyProperty = Reg("AppName", GetType(String), GetType(AppSettings), "Display Driver Uninstaller (DDU)")
	Private v_main As ViewMain

	Public Property AppName As String ' Name of application (DDU)
		Get
			Return CStr(GetValue(m_appname))
		End Get
		Private Set(value As String)
			SetValue(m_appname, value)
		End Set
	End Property

	Public ReadOnly Property Main As ViewMain	' All stuff for "frmMain" window
		Get
			Return v_main
		End Get
	End Property

	Friend Shared Function Reg(ByVal s As String, ByVal t As Type, ByVal c As Type, ByVal m As Object) As DependencyProperty
		' Register values for 'Binding' (just shorthand, Don't need to undestand)
		If TypeOf (m) Is FrameworkPropertyMetadata Then
			Return DependencyProperty.Register(s, t, c, CType(m, FrameworkPropertyMetadata))
		Else
			Return DependencyProperty.Register(s, t, c, New PropertyMetadata(m))
		End If
	End Function


	Public Sub New()
		v_main = New ViewMain()
	End Sub


	Public Class ViewMain
		Inherits DependencyObject

		Public Property LanguageOptions As ObservableCollection(Of Languages.LanguageOption)

		Public Sub New()
			LanguageOptions = New ObservableCollection(Of Languages.LanguageOption)()
		End Sub
	End Class
End Class

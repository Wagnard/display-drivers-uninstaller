Imports System.IO
Imports System.Reflection

Public Class AppPaths
	Private m_exefile, m_dirapp, m_dirsettings, m_dirlanguage As String

	''' <returns>Fullpath to application's .Exe</returns>
	Public ReadOnly Property AppExeFile As String
		Get
			Return m_exefile
		End Get
	End Property
	''' <returns>Fullpath to application's base directory (where .exe is)</returns>
	Public ReadOnly Property DirApp As String
		Get
			Return m_dirapp
		End Get
	End Property
	''' <returns>Application's base directory + \Settings\</returns>
	Public ReadOnly Property DirSettings As String
		Get
			Return m_dirsettings
		End Get
	End Property
	''' <returns>Application's base directory + \Settings\Langauges\</returns>
	Public ReadOnly Property DirLanguage As String
		Get
			Return m_dirlanguage 
		End Get
	End Property

	Public Sub New()
		m_exefile = Assembly.GetExecutingAssembly().Location
		m_dirapp = Path.GetDirectoryName(AppExeFile)

		If Not m_dirapp.EndsWith("\") Then
			m_dirapp &= "\"
		End If

		m_dirsettings = m_dirapp & "Settings\"
		m_dirlanguage = m_dirsettings & "Languages\"

		If Not Directory.Exists(m_dirsettings) Then
			Directory.CreateDirectory(m_dirsettings)
		End If

		If Not Directory.Exists(m_dirlanguage) Then
			Directory.CreateDirectory(m_dirlanguage)
		End If

	End Sub
End Class

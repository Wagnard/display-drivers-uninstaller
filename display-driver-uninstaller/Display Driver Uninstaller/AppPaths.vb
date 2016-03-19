Imports System.IO
Imports System.Reflection

Public Class AppPaths
	Private m_exefile, m_dirapp, m_dirsettings, m_dirlanguage, m_dirlog, m_sysdrive, m_windir As String

	''' <returns>Fullpath to application's .Exe</returns>
	Public Property AppExeFile As String
		Get
			Return m_exefile
		End Get
		Private Set(value As String)
			m_exefile = value
		End Set
	End Property
	''' <returns>Fullpath to application's base directory (where .exe is)</returns>
	Public Property AppBase As String
		Get
			Return m_dirapp
		End Get
		Private Set(value As String)
			m_dirapp = value
		End Set
	End Property
	''' <returns>Application's base directory + \Settings\</returns>
	Public Property Settings As String
		Get
			Return m_dirsettings
		End Get
		Private Set(value As String)
			m_dirsettings = value
		End Set
	End Property
	''' <returns>Application's base directory + \Settings\Langauges\</returns>
	Public Property Language As String
		Get
			Return m_dirlanguage
		End Get
		Private Set(value As String)
			m_dirlanguage = value
		End Set
	End Property
	''' <returns>Application's base directory + \DDU_Logs\</returns>
	Public Property Logs As String
		Get
			Return m_dirlog
		End Get
		Private Set(value As String)
			m_dirlog = value
		End Set
	End Property
	''' <returns>System Drive ( "C:" )</returns>
	Public Property SystemDrive As String
		Get
			Return m_sysdrive
		End Get
		Private Set(value As String)
			m_sysdrive = value
		End Set
	End Property
	''' <returns>Windows Directory ( "C:\Windows" )</returns>
	Public Property WinDir As String
		Get
			Return m_windir
		End Get
		Private Set(value As String)
			m_windir = value
		End Set
	End Property

	Public Sub New(Optional ByVal createPaths As Boolean = True)
		If createPaths Then
			m_exefile = Assembly.GetExecutingAssembly().Location
			m_dirapp = Path.GetDirectoryName(AppExeFile)

			If Not m_dirapp.EndsWith("\") Then
				m_dirapp &= "\"
			End If

			m_dirsettings = m_dirapp & "Settings\"
			m_dirlanguage = m_dirsettings & "Languages\"
			m_dirlog = m_dirapp & "DDU Logs\"
			m_sysdrive = Environment.GetEnvironmentVariable("systemdrive").ToLower
			m_windir = Environment.GetEnvironmentVariable("windir").ToLower

			CreateDirectories(m_dirsettings, m_dirlanguage, m_dirlog)
		End If
	End Sub

	Public Sub CreateDirectories(ParamArray dirs As String())
		For Each dir As String In dirs
			If Not Directory.Exists(dir) Then
				Directory.CreateDirectory(dir)
			End If
		Next
	End Sub
End Class

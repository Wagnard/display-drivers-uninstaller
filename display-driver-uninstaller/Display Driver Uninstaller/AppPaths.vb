Imports System.IO
Imports System.Reflection

Public Class AppPaths
	Private m_exefile, m_dirapp, m_dirapproaming, m_dirsettings, m_dirlanguage, m_dirlog,
	 m_roaming, m_sysdrive, m_windir As String

	'NOTE!!! All paths ends with \
	'No need to check for ending \

	''' <summary>Fullpath to application's .Exe</summary>
	Public Property AppExeFile As String
		Get
			Return m_exefile
		End Get
		Private Set(value As String)
			m_exefile = value
		End Set
	End Property
	''' <summary>Fullpath to application's base directory (where .exe is)</summary>
	Public Property AppBase As String
		Get
			Return m_dirapp
		End Get
		Private Set(value As String)
			m_dirapp = value
		End Set
	End Property
	''' <summary>Application's base directory + \Settings\</summary>
	Public Property Settings As String
		Get
			Return m_dirsettings
		End Get
		Private Set(value As String)
			m_dirsettings = value
		End Set
	End Property
	''' <summary>C:\%Users%\AppData\Roaming\DisplayDriverUnistaller\</summary>
	Public Property AppBaseRoaming As String
		Get
			Return m_dirapproaming
		End Get
		Private Set(value As String)
			m_dirapproaming = value
		End Set
	End Property
	''' <summary>Application's base directory + \Settings\Langauges\</summary>
	Public Property Language As String
		Get
			Return m_dirlanguage
		End Get
		Private Set(value As String)
			m_dirlanguage = value
		End Set
	End Property
	''' <summary>Application's base directory + \DDU Logs\</summary>
	Public Property Logs As String
		Get
			Return m_dirlog
		End Get
		Private Set(value As String)
			m_dirlog = value
		End Set
	End Property
	''' <summary>System Drive ( "C:" )</summary>
	Public Property SystemDrive As String
		Get
			Return m_sysdrive
		End Get
		Private Set(value As String)
			m_sysdrive = value
		End Set
	End Property
	''' <summary>Windows Directory ( "C:\Windows" )</summary>
	Public Property WinDir As String
		Get
			Return m_windir
		End Get
		Private Set(value As String)
			m_windir = value
		End Set
	End Property
	''' <summary>Roaming Directory ( "C:\%Users%\AppData\Roaming\" )</summary>
	Public Property Roaming As String
		Get
			Return m_roaming
		End Get
		Private Set(value As String)
			m_roaming = value
		End Set
	End Property

	Public Sub New(Optional ByVal createPaths As Boolean = True)
		If createPaths Then
			m_exefile = Assembly.GetExecutingAssembly().Location
			m_dirapp = Path.GetDirectoryName(AppExeFile)

			m_roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
			m_dirapproaming = Path.Combine(m_roaming, Assembly.GetExecutingAssembly().GetName().Name.Replace(" ", ""))

			m_dirsettings = Path.Combine(m_dirapp, "Settings\")
			m_dirlanguage = Path.Combine(m_dirsettings, "Languages\")
			m_dirlog = Path.Combine(m_dirapp, "DDU Logs\")

			m_sysdrive = Environment.GetEnvironmentVariable("systemdrive")
			m_windir = Environment.GetEnvironmentVariable("windir")


			If Not m_dirapp.EndsWith("\") Then m_dirapp &= Path.DirectorySeparatorChar

			If Not m_roaming.EndsWith("\") Then m_roaming &= Path.DirectorySeparatorChar
			If Not m_dirapproaming.EndsWith("\") Then m_dirapproaming &= Path.DirectorySeparatorChar

			If Not m_dirsettings.EndsWith("\") Then m_dirsettings &= Path.DirectorySeparatorChar
			If Not m_dirlanguage.EndsWith("\") Then m_dirlanguage &= Path.DirectorySeparatorChar
			If Not m_dirlog.EndsWith("\") Then m_dirlog &= Path.DirectorySeparatorChar

			If Not m_sysdrive.EndsWith("\") Then m_sysdrive &= Path.DirectorySeparatorChar
			If Not m_windir.EndsWith("\") Then m_windir &= Path.DirectorySeparatorChar

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

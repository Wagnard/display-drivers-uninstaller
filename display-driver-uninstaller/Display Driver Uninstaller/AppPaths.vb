Imports System.IO
Imports System.Reflection

Public Class AppPaths
	Private m_exefile, m_dirapp, m_dirapproaming, m_dirsettings, m_dirlanguage, m_dirlog,
	 m_roaming, m_sysdrive, m_windir, m_programfiles, m_programfilesx86, m_userpath, m_system32 As String

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
	''' <summary>System Drive ( "C:\" )</summary>
	Public Property SystemDrive As String
		Get
			Return m_sysdrive
		End Get
		Private Set(value As String)
			m_sysdrive = value
		End Set
	End Property
	''' <summary>Windows Directory ( "C:\Windows\" )</summary>
	Public Property WinDir As String
		Get
			Return m_windir
		End Get
		Private Set(value As String)
			m_windir = value
		End Set
	End Property
	''' <summary>ProgramFiles Directory(32 and 64bits) ( "C:\Program Files\" )</summary>
	Public Property ProgramFiles As String
		Get
			Return m_programfiles
		End Get
		Private Set(value As String)
			m_programfiles = value
		End Set
	End Property
	''' <summary>( "C:\windows\system32\" )</summary>
	Public Property System32 As String
		Get
			Return m_system32
		End Get
		Private Set(value As String)
			m_system32 = value
		End Set
	End Property
	''' <summary>ProgramFiles(x86) Directory(64bits only) ( "C:\Program Files (x86)\" )</summary>
	Public Property ProgramFilesx86 As String
		Get
			Return m_programfilesx86
		End Get
		Private Set(value As String)
			m_programfilesx86 = value
		End Set
	End Property
	''' <summary>Roaming Directory ( "C:\%Users%\AppData\Roaming\" most likely C:\Windows\System32\comfig\systemprofile\AppData\Roaming\)</summary>
	Public Property Roaming As String
		Get
			Return m_roaming
		End Get
		Private Set(value As String)
			m_roaming = value
		End Set
	End Property

	Public Property UserPath As String
		Get
			Return m_userpath
		End Get
		Private Set(value As String)
			m_userpath = value
		End Set
	End Property

	Public Sub New(Optional ByVal createPaths As Boolean = True)
		If createPaths Then
			m_exefile = New Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath
			m_dirapp = Path.GetDirectoryName(AppExeFile)

			m_roaming = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\config\systemprofile\AppData\Roaming\"
			m_dirapproaming = Path.Combine(m_roaming, Assembly.GetExecutingAssembly().GetName().Name.Replace(" ", ""))

			m_dirsettings = Path.Combine(m_dirapp, "Settings\")
			m_dirlanguage = Path.Combine(m_dirsettings, "Languages\")
			m_dirlog = Path.Combine(m_dirapp, "DDU Logs\")

			m_sysdrive = Environment.GetEnvironmentVariable("systemdrive")
			m_windir = Environment.GetEnvironmentVariable("windir")
			m_programfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
			m_programfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + " (x86)"
			m_system32 = Environment.GetFolderPath(Environment.SpecialFolder.System)

			Using regkey As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist")
				m_userpath = regkey.GetValue("ProfilesDirectory", String.Empty).ToString
			End Using

			If Not m_userpath.EndsWith("\") Then m_userpath &= Path.DirectorySeparatorChar
			If Not m_dirapp.EndsWith("\") Then m_dirapp &= Path.DirectorySeparatorChar

			If Not m_roaming.EndsWith("\") Then m_roaming &= Path.DirectorySeparatorChar
			If Not m_dirapproaming.EndsWith("\") Then m_dirapproaming &= Path.DirectorySeparatorChar

			If Not m_dirsettings.EndsWith("\") Then m_dirsettings &= Path.DirectorySeparatorChar
			If Not m_dirlanguage.EndsWith("\") Then m_dirlanguage &= Path.DirectorySeparatorChar
			If Not m_dirlog.EndsWith("\") Then m_dirlog &= Path.DirectorySeparatorChar

			If Not m_sysdrive.EndsWith("\") Then m_sysdrive &= Path.DirectorySeparatorChar
			If Not m_windir.EndsWith("\") Then m_windir &= Path.DirectorySeparatorChar
			If Not m_programfiles.EndsWith("\") Then m_programfiles &= Path.DirectorySeparatorChar
			If Not m_system32.EndsWith("\") Then m_system32 &= Path.DirectorySeparatorChar
			If Not m_programfilesx86.EndsWith("\") Then m_programfilesx86 &= Path.DirectorySeparatorChar

			CreateDirectories(m_dirsettings, m_dirlanguage, m_dirlog)
		End If
	End Sub

	Public Sub CreateDirectories(ParamArray dirs As String())
		For Each dir As String In dirs
			Try
				If Not Directory.Exists(dir) Then
					Directory.CreateDirectory(dir)
				End If
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		Next
	End Sub
End Class

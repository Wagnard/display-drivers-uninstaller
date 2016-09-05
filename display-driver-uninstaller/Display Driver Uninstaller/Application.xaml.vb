Imports System.IO
Imports System.Threading
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Markup
Imports System.Text

Imports Display_Driver_Uninstaller.Win32
Imports System.Security.Principal
Imports Microsoft.Win32

Class Application

#Region "Visit links URLs"

	Private Const URL_DONATE As String = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"
	Private Const URL_DDUHOME As String = "http://www.wagnardmobile.com"
	Private Const URL_GURU3D_AMD As String = "http://forums.guru3d.com/showthread.php?t=379505"
	Private Const URL_GURU3D_NVIDIA As String = "http://forums.guru3d.com/showthread.php?t=379506"
	Private Const URL_GEFORCE As String = "https://forums.geforce.com/default/topic/550192/geforce-drivers/wagnard-tools-ddu-gmp-tdr-manupulator-updated-01-22-2015-/"
	Private Const URL_SVN As String = "https://github.com/Wagnard/display-drivers-uninstaller"
	Private Const URL_OFFER As String = "https://www.driverdr.com/lp/update-display-drivers.html"

#End Region

	Private Shared m_isDebug As Boolean = System.Diagnostics.Debugger.IsAttached
	Public Shared Property IsDebug As Boolean
		Get
			Return m_isDebug
		End Get
		Set(value As Boolean)
			m_isDebug = value
		End Set
	End Property

	Private Shared m_dispatcher As Threading.Dispatcher
	Private Shared m_isDataSaved As Boolean = False
	Private Shared m_allowSaveData As Boolean = False
	Private Shared m_Data As Data

	Public Shared ReadOnly Property Data As Data
		Get
			Return m_Data
		End Get
	End Property
	Public Shared ReadOnly Property LaunchOptions As AppLaunchOptions
		Get
			Return m_Data.LaunchOptions
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

	Public Sub New()
		m_Data = New Data()
		m_dispatcher = Me.Dispatcher

		'ALL Exceptions are shown in English
		Thread.CurrentThread.CurrentCulture = New CultureInfo("en-US")
		Thread.CurrentThread.CurrentUICulture = New CultureInfo("en-US")

		FrameworkElement.LanguageProperty.OverrideMetadata(GetType(FrameworkElement),
		   New FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)))
	End Sub

	Public Shared Sub SaveData()
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.Invoke(Sub() SaveData())
		Else
			If Not m_isDataSaved AndAlso m_allowSaveData Then
				Settings.Save()
				Log.SaveToFile()

				m_isDataSaved = True
			End If
		End If
	End Sub



	Private Sub InitLanguages()
		Dim defaultLang As Languages.LanguageOption = Languages.DefaultEng
		Dim foundLangs As List(Of Languages.LanguageOption) = Nothing

		Try
			foundLangs = Languages.ScanFolderForLang(Application.Paths.Language)

		Catch ex As Exception
			foundLangs = New List(Of Languages.LanguageOption)(1)
			Log.AddException(ex, "Finding language files failed!")
		End Try

		foundLangs.Add(defaultLang)

		If foundLangs.Count > 1 Then
			foundLangs.Sort(Function(x, y) x.DisplayText.CompareTo(y.DisplayText))
		End If

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

		Languages.Load(Application.Settings.SelectedLanguage)
	End Sub

	Private Sub ExtractEnglishLangFile(ByVal fileName As String, ByVal langEng As Languages.LanguageOption)
		Try
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
		Catch ex As Exception
			Log.AddWarning(ex, "Extracting English.xml to Languages directory failed!")
		End Try
	End Sub



	Private Sub LaunchMainWindow()
		' >>> Loading UI <<<
		Try
			Dim window As frmMain = New frmMain() With {.DataContext = Data, .Topmost = True}

			AddHandler window.Closing, AddressOf AppClosing
			AddHandler window.Closed, AddressOf AppClose

			'	Launching frmMain, triggers Events
			'	-> frmMain_Initialized
			'	-> frmMain_Loaded				(UI elements loaded, but not rendered)
			'	-> frmMain_ContentRendered		(UI is completely ready for use, dimensions of each control aligned etc.)

			If Settings.WinVersion = OSVersion.Unknown Then
				window.EnableControls(False)
			End If

			If LaunchOptions.HasCleanArg AndAlso LaunchOptions.Silent Then
				window.Visibility = Visibility.Hidden
				window.WindowState = WindowState.Minimized
			End If

			m_allowSaveData = True

			window.Show()

			MainWindow = window
		Catch ex As Exception
			Log.AddException(ex, "Some part of window loading failed!" & CRLF & ">> LaunchMainWindow()")
			Log.SaveToFile()

			MessageBox.Show("Launching Main Window failed!" & CRLF &
			 CRLF &
			 ex.Message & CRLF &
			 CRLF &
			 ex.StackTrace, "Display Driver Uninstaller", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly)

			Me.Shutdown(0)
		End Try
	End Sub

	Private Sub AppClosing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
		Try
			If frmMain.workThread IsNot Nothing Then					' workThread running, cleaning in progress!
				' Should take few milliseconds...	
				If frmMain.workThread.IsAlive Then Thread.Sleep(200)
				If frmMain.workThread.IsAlive Then Thread.Sleep(2000)

				' workThread still running!
				If frmMain.workThread.IsAlive Then
					e.Cancel = True
					Exit Sub
				End If
			End If
		Catch ex As Exception
			e.Cancel = True			' frmMain.workThread may be null after checking
		End Try
	End Sub

	Private Sub AppClose(ByVal sender As Object, ByVal e As System.EventArgs)
		Try
			' frmMain is already closed here

			If WindowsIdentity.GetCurrent().IsSystem AndAlso Forms.SystemInformation.BootMode <> Forms.BootMode.Normal Then
				Try
					Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
						If regkey IsNot Nothing Then
							regkey.DeleteSubKeyTree("PAexec")
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (PAExec)!")
				End Try

				Try
					Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
						If regkey IsNot Nothing Then
							regkey.DeleteSubKeyTree("PAexec")
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Network' RegistryKey (PAExec)!")
				End Try
			End If

			SaveData()
		Finally
			Me.Shutdown(0)	' Close application completely
		End Try
	End Sub

	Private Sub Application_Startup(sender As Object, e As System.Windows.StartupEventArgs) Handles Me.Startup
		'If WindowsIdentity.GetCurrent().IsSystem Then
		'	MessageBox.Show("Attach debugger!")		' for Debugging System process
		'	IsDebug = True
		'End If

		Try
			' Launch as Admin if not
			If Not Tools.UserHasAdmin Then
				Using process As Process = New Process() With {.StartInfo = New ProcessStartInfo(Application.Paths.AppExeFile, String.Join(" ", e.Args)) With {.Verb = "runas"}}
					Try
						process.Start()
					Catch ex As ComponentModel.Win32Exception
						Dim errCode As UInt32 = GetUInt32(ex.NativeErrorCode)
						Dim msg As String = String.Format("Error:{0}{1}{0}{0}Message:{0}{2}", CRLF, GetErrorEnum(errCode), ex.Message)

						If errCode = Errors.CANCELLED Then	'User pressed 'No' on UAC screen
							msg = String.Format("Administrator rights are required to use application.{0}{0}{1}", CRLF, msg)
						End If

						MessageBox.Show(msg, "Display Driver Uninstaller", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly)
					End Try

					Me.Shutdown(0)
					Exit Sub
				End Using
			End If


			' Process commandline args
			LaunchOptions.LoadArgs(e.Args)


			' Processing links before launching UI 
			' > Causes no Window 'flash' (frmMain not loaded yet)
			' > Opens link before checking update (not waiting for update check, slow connection => slower link opening)
			' -> Faster link opening

			Try
				If LaunchOptions.HasLinkArg Then
					If ProcessLinks() Then		' Link found and opened?
						Me.Shutdown(0)			' Skip loading if link is opened
						Exit Sub
					End If
				End If
			Catch ex As Exception
				Application.Log.AddException(ex, "Parsing arguments failed!" & CRLF & ">> Application_Startup()")
			End Try

			' Load default language (English) + Find language files from folder
			InitLanguages()


			' Load AppSettings and select last used language (if settings exists)
			Settings.Load()


			' Select language (last used -> native -> default)
			' Now we have translated messages available
			SelectLanguage()


			' Useful on next steps
			GetOSVersion()
			Settings.WinIs64 = (IntPtr.Size = 8)

			Try

				'We check if there are any reboot from windows update pending. and if so we quit.
				If WinUpdatePending() Then
					MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text14"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning)
					Me.Shutdown(0)
					Exit Sub
				End If

			Catch ex As Exception
				Application.Log.AddException(ex)
				Me.Shutdown(0)
				Exit Sub
			End Try

			If Not WindowsIdentity.GetCurrent().IsSystem Then
				If ExtractPAExec() Then				' Extract PAExec to \x64 or \x86 dir
					If LaunchAsSystem() Then		' Launched as System, close this instance, True = close, false = continue
						Me.Shutdown(0)
						Exit Sub
					End If
				End If
			End If

			Try
				' These are only needed to set ONCE during App lifetime
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)

			Catch ex As Exception
				Application.Log.AddException(ex, "AddPriviliges failed!" & CRLF & ">> AppStart()")
			End Try
		Catch ex As Exception
			Log.AddException(ex, "Some part of application startup failed!" & CRLF & ">> Application_Startup()")
			Log.SaveToFile()	' Save to file

			MessageBox.Show("Launching Application failed!" & CRLF &
			 "A problem occurred in one of the module, send your DDU logs to the developer." & CRLF &
			   CRLF &
			   ex.Message, "Display Driver Uninstaller", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly)

			Me.Shutdown(0)
		End Try

		LaunchMainWindow()
	End Sub

	Private Function WinUpdatePending() As Boolean
		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
			Return (regkey IsNot Nothing)
		End Using
	End Function

	Private Function ProcessLinks() As Boolean
		Dim webAddress As String = Nothing

		If Application.LaunchOptions.VisitDonate Then
			webAddress = URL_DONATE
		ElseIf Application.LaunchOptions.VisitGuru3DNvidia Then
			webAddress = URL_GURU3D_NVIDIA
		ElseIf Application.LaunchOptions.VisitGuru3DAMD Then
			webAddress = URL_GURU3D_AMD
		ElseIf Application.LaunchOptions.VisitGeforce Then
			webAddress = URL_GEFORCE
		ElseIf Application.LaunchOptions.VisitDDUHome Then
			webAddress = URL_DDUHOME
		ElseIf Application.LaunchOptions.VisitSVN Then
			webAddress = URL_SVN
		ElseIf Application.LaunchOptions.VisitOffer Then
			webAddress = URL_OFFER
		End If

		If Not IsNullOrWhitespace(webAddress) Then

			Using process As Process = New Process() With
			 {
			  .StartInfo = New ProcessStartInfo(webAddress, Nothing) With
			  {
			   .UseShellExecute = True,
			   .CreateNoWindow = True,
			   .RedirectStandardOutput = False
			  }
			 }

				process.Start()
				'Do not put WaitForExit here. It will cause error and prevent DDU to exit.
				process.Close()

				Me.Shutdown(0)
				Return True
			End Using
		End If

		Return False
	End Function

	Private Sub GetOSVersion()
		'second, we check on what we are running and set variables accordingly (os, architecture)
		Dim versionFound As Boolean = False
		Dim regOSValue As String = Nothing
		Dim version As OSVersion = OSVersion.Unknown

		Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
			If regkey IsNot Nothing Then
				regOSValue = regkey.GetValue("CurrentVersion", String.Empty).ToString()

				If Not IsNullOrWhitespace(regOSValue) Then
					Try
						For Each os As [Enum] In [Enum].GetValues(GetType(OSVersion))
							If GetDescription(os).Equals(regOSValue) Then
								version = DirectCast(os, OSVersion)
								versionFound = (version <> OSVersion.Unknown)
								Exit For
							End If
						Next
					Catch ex As Exception
						versionFound = False
					End Try
				End If
			End If
		End Using

		If Not versionFound Then		' Double check for Unknown
			Select Case regOSValue
				Case "5.1" : version = OSVersion.WinXP
				Case "5.2" : version = OSVersion.WinXPPro_Server2003
				Case "6.0" : version = OSVersion.WinVista
				Case "6.1" : version = OSVersion.Win7
				Case "6.2" : version = OSVersion.Win8
				Case "6.3" : version = OSVersion.Win81
				Case "6.4", "10", "10.0" : version = OSVersion.Win10
				Case Else : version = OSVersion.Unknown
			End Select
		End If

		Application.Settings.WinVersion = version
	End Sub

	Private Function ExtractPAExec() As Boolean
		Try
			Dim isWinXP As Boolean = (Settings.WinVersion = OSVersion.WinXP Or Settings.WinVersion = OSVersion.WinXPPro_Server2003)
			Dim dir As String = Paths.AppBase & If(Settings.WinIs64, "x64\", "x86\")

			Application.Paths.CreateDirectories(dir)

			Try
				Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
					If regkey IsNot Nothing Then
						Using regSubKey As RegistryKey = regkey.CreateSubKey("PAexec", RegistryKeyPermissionCheck.ReadWriteSubTree)
							regSubKey.SetValue("", "Service")
						End Using
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for PAExec!")
				Return False
			End Try

			Try
				Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
					If regkey IsNot Nothing Then
						Using regSubKey As RegistryKey = regkey.CreateSubKey("PAexec", RegistryKeyPermissionCheck.ReadWriteSubTree)
							regSubKey.SetValue("", "Service")
						End Using
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex, "Failed to set '\SafeBoot\Network' RegistryKey for PAExec!")
				Return False
			End Try



			Try
				If FileIO.ExistsFile(dir & "paexec.exe") Then
					Using ms As MemoryStream = New MemoryStream(My.Resources.paexec)
						Using fs As FileStream = File.OpenRead(dir & "paexec.exe")
							If Tools.CompareStreams(ms, fs) Then
								Return True		' paexec.exe already exists and checksum(MD5) matches
							End If
						End Using
					End Using
				End If
			Catch ex As Exception
				Application.Log.AddException(ex, "Checking for existing PAExec failed!")
			End Try

			File.WriteAllBytes(dir & "paexec.exe", My.Resources.paexec)

			If Not FileIO.ExistsFile(dir & "paexec.exe") Then
				MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text4"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
				Return False
			End If


			Return True
		Catch ex As Exception
			Application.Log.AddException(ex, "Extracting PAExec failed!")
			Return False
		End Try
	End Function

	Private Function LaunchAsSystem() As Boolean
		'here I check if the process is running on system user account. if not, make it so.
		'This code checks to see which mode Windows has booted up in.

		Dim isWinXP As Boolean = (Settings.WinVersion = OSVersion.WinXP Or Settings.WinVersion = OSVersion.WinXPPro_Server2003)

		Select Case System.Windows.Forms.SystemInformation.BootMode
			Case System.Windows.Forms.BootMode.FailSafeWithNetwork, System.Windows.Forms.BootMode.FailSafe
				'The computer was booted using only the basic files and drivers.
				'This is the same as Safe Mode

				If Not isWinXP Then
					Using process As Process = New Process() With
					  {
					   .StartInfo = New ProcessStartInfo("cmd.exe", " /CBCDEDIT /deletevalue safeboot") With
					   {
					 .UseShellExecute = False,
					 .CreateNoWindow = True,
					 .RedirectStandardOutput = False
					   }
					  }

						process.Start()
						process.WaitForExit()
						process.Close()
					End Using
				End If

			Case System.Windows.Forms.BootMode.Normal
				' added iselevated so this will not try to boot into safe mode/boot menu without admin rights,
				' as even with the admin check on startup it was for some reason still trying to gain registry access 
				' and throwing an exception --probably because there's no return

				If Not isWinXP AndAlso Tools.UserHasAdmin Then
					If LaunchOptions.NoSafeModeMsg Then
						Exit Select
					End If

					If Settings.ShowSafeModeMsg = True Then
						Dim bootOption As Integer = -1				'-1 = close, 0 = normal, 1 = SafeMode, 2 = SafeMode with network
						Dim frmSafeBoot As New frmLaunch With {.DataContext = Data, .Topmost = True}


						Dim launch As Boolean? = frmSafeBoot.ShowDialog()

						If launch IsNot Nothing AndAlso launch.Value Then
							bootOption = frmSafeBoot.selection
						End If

						Select Case bootOption
							Case 0 'normal
								Exit Select

							Case 1 'SafeMode
								Return RestartToSafemode(False)

							Case 2 'SafeMode with network
								Return RestartToSafemode(True)

							Case Else '-1 = Close
								Return True

						End Select
					End If
				End If
		End Select

		If Application.IsDebug Then
			Return False
		End If

		Dim args() As String = New String() {" /Csc stop PAExec", " /Csc delete PAExec", " /Csc interrogate PAExec"}

		For Each arg As String In args
			Using process As Process = New Process() With
			 {
			  .StartInfo = New ProcessStartInfo("cmd.exe", arg) With
			  {
			   .UseShellExecute = False,
			   .CreateNoWindow = True,
			   .RedirectStandardOutput = False
			  }
			 }
				process.Start()
				process.WaitForExit()
				process.Close()
				Thread.Sleep(10)
			End Using
		Next

		Using process As Process = New Process() With
		  {
		   .StartInfo = New ProcessStartInfo(Paths.AppBase & If(Settings.WinIs64, "x64\", "x86\") & "paexec.exe", "-noname -i -s " & Chr(34) & Paths.AppExeFile & Chr(34) + " " & LaunchOptions.Arguments) With
		   {
		 .UseShellExecute = False,
		 .CreateNoWindow = True,
		 .RedirectStandardOutput = False
		   }
		  }

			Try
				process.Start()
				process.Close()
			Catch ex As Exception
				Log.AddException(ex, "(PAExec) Failed to start process as System user!")
				Return False
			End Try
		End Using

		Return True
	End Function

	Private Function RestartToSafemode(ByVal withNetwork As Boolean) As Boolean
		Try
			SystemRestore(Nothing) 'we try to do a system restore if allowed before going into safemode.
			Application.Log.AddMessage("Restarting in safemode")



			Using process As Process = New Process() With
			  {
			   .StartInfo = New ProcessStartInfo("bcdedit", If(withNetwork, "/set safeboot network", "/set safeboot minimal")) With
			   {
			 .UseShellExecute = False,
			 .CreateNoWindow = True,
			 .RedirectStandardOutput = False
			   }
			  }

				process.Start()
				process.WaitForExit()
				process.Close()
			End Using

			Try
				Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
					If regkey IsNot Nothing Then
						regkey.SetValue("*" + Settings.AppName, Paths.AppExeFile)
						regkey.SetValue("*UndoSM", "BCDEDIT /deletevalue safeboot")
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try


			RestartComputer()

			Return True
		Catch ex As Exception
			Log.AddException(ex, "Failed to reboot into Safemode!")
			Return False
		End Try
	End Function

	Public Shared Sub RestartComputer()
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.Invoke(Sub() RestartComputer())
		Else
			Application.Log.AddMessage("Restarting Computer ")
			Application.SaveData()

			Using process As Process = New Process() With
			  {
			   .StartInfo = New ProcessStartInfo("shutdown", "/r /t 0") With
			   {
			 .WindowStyle = ProcessWindowStyle.Hidden,
			 .UseShellExecute = True,
			 .CreateNoWindow = True,
			 .RedirectStandardOutput = False
			   }
			  }

				process.Start()
				process.WaitForExit()
				process.Close()
			End Using
		End If
	End Sub

	Public Shared Sub ShutdownComputer()
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.Invoke(Sub() ShutdownComputer())
		Else
			Application.Log.AddMessage("Shutdown Computer ")
			Application.SaveData()

			Using process As Process = New Process() With
			{
			  .StartInfo = New ProcessStartInfo("shutdown", "/s /t 0") With
			   {
			   .WindowStyle = ProcessWindowStyle.Hidden,
			   .UseShellExecute = True,
			   .CreateNoWindow = True,
			   .RedirectStandardOutput = False
			   }
			  }

				process.Start()
				process.WaitForExit()
				process.Close()
			End Using
		End If
	End Sub

	Public Shared Sub SystemRestore(ByVal owner As Window)
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.Invoke(Sub() SystemRestore(owner))
		Else
			If Application.Settings.CreateRestorePoint AndAlso Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then
				Dim frmSystemRestore As New frmSystemRestore With
				 {
				  .ResizeMode = ResizeMode.NoResize,
				  .WindowStyle = WindowStyle.ToolWindow
				 }

				If owner IsNot Nothing Then
					With frmSystemRestore
						.WindowStartupLocation = WindowStartupLocation.CenterOwner
						.Background = owner.Background
						.Owner = owner
						.DataContext = owner.DataContext
					End With
				Else
					With frmSystemRestore
						.WindowStartupLocation = WindowStartupLocation.CenterScreen
						.DataContext = Data
					End With
				End If

				frmSystemRestore.ShowDialog()
			End If
		End If
	End Sub

	' Launching application, Event order
	'	Application : Sub New()
	'
	'	-> Application_Startup	(Event)
	'	---> AppStart()
	'
	'	Launching frmMain, triggers Events
	'	-> frmMain_Initialized			(Nothing is actually loaded yet, controls not even added to Window yet)		<-- Don't use
	'	-> frmMain_Loaded				(UI elements added to Window and loaded, but not rendered!)					<-- Use only for Non-UI stuff which are fast to do
	'	-> frmMain_ContentRendered		(UI is completely ready for use, dimensions of each control aligned etc.)	<-- Anything else


End Class

Public Class Data
	Private m_launchOptions As AppLaunchOptions
	Private m_settings As AppSettings
	Private m_paths As AppPaths
	Private m_log As AppLog

	Public ReadOnly Property IsDebug As Boolean
		Get
			Return Application.IsDebug
		End Get
	End Property

	Public ReadOnly Property LaunchOptions As AppLaunchOptions
		Get
			Return m_launchOptions
		End Get
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
		m_launchOptions = New AppLaunchOptions
		m_settings = New AppSettings
		m_paths = New AppPaths
		m_log = New AppLog
	End Sub
End Class


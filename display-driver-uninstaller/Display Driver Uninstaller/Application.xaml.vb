Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Security.Principal
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Interop
Imports System.Windows.Markup
Imports System.Windows.Media
Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32

Namespace Display_Driver_Uninstaller

	Class Application
		Private _fileIo As New FileIO
#Region "Visit links URLs"

		Private Const URL_DONATE As String = "https://www.paypal.com/donate/?hosted_button_id=S8H2QAS7KLV7N"
		Private Const URL_PATRON As String = "https://www.patreon.com/wagnardsoft"
		Private Const URL_DISCORD As String = "https://discord.gg/JsSsKyqzjF"
        Private Const URL_DDUHOME As String = "https://www.wagnardsoft.com/"
        Private Const URL_DDUSHOP As String = "https://shop.wagnardsoft.com"
        Private Const URL_AMD As String = "https://www.amd.com/en/support/download/drivers.html"
        Private Const URL_NVIDIA As String = "https://www.nvidia.com/Download/index.aspx"
        Private Const URL_INTEL As String = "https://www.intel.com/content/www/us/en/download-center/home.html"
        Private Const URL_GEFORCE As String = "https://www.nvidia.com/en-us/geforce/forums/game-ready-drivers/13/1001/wagnard-tools-ddu-more/"
		Private Const URL_SVN As String = "https://github.com/Wagnard/display-drivers-uninstaller"
		Private Const URL_OFFER As String = "https://www.drivereasy.com/update-display-drivers"

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

		Private Shared m_dispatcher As System.Windows.Threading.Dispatcher
		Private Shared m_isDataSaved As Boolean = False
		Private Shared m_allowSaveData As Boolean = False
		Private Shared m_Data As Data
		Private Shared m_useDarkThemeSession As Boolean = False

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
		Public Shared ReadOnly Property UseDarkThemeSession As Boolean
			Get
				Return m_useDarkThemeSession
			End Get
		End Property

		Public Shared Function ShowThemedNotice(message As String,
												Optional title As String = Nothing,
												Optional buttons As MessageBoxButton = MessageBoxButton.OK,
												Optional owner As Window = Nothing) As MessageBoxResult
			Dim noticeOwner As Window = If(owner, TryCast(Current?.MainWindow, Window))
			Dim resolvedTitle As String = If(String.IsNullOrWhiteSpace(title), Application.Settings.AppName, title)

			If Not UseDarkThemeSession Then
				If m_dispatcher Is Nothing Then
					Return ShowStandardNotice(noticeOwner, message, resolvedTitle, buttons)
				End If

				If Not m_dispatcher.CheckAccess() Then
					Return CType(m_dispatcher.Invoke(Function() ShowThemedNotice(message, resolvedTitle, buttons, noticeOwner)), MessageBoxResult)
				End If

				Return ShowStandardNotice(noticeOwner, message, resolvedTitle, buttons)
			End If

			If m_dispatcher Is Nothing Then
				Return FrmNotice.ShowNotice(noticeOwner, resolvedTitle, message, buttons)
			End If

			If Not m_dispatcher.CheckAccess() Then
				Return CType(m_dispatcher.Invoke(Function() ShowThemedNotice(message, resolvedTitle, buttons, noticeOwner)), MessageBoxResult)
			End If

			Return FrmNotice.ShowNotice(noticeOwner, resolvedTitle, message, buttons)
		End Function

		Private Shared Function ShowStandardNotice(owner As Window,
												   message As String,
												   title As String,
												   buttons As MessageBoxButton) As MessageBoxResult
			If owner Is Nothing Then
				Return MessageBox.Show(message, title, buttons, MessageBoxImage.Information)
			End If

			Return MessageBox.Show(owner, message, title, buttons, MessageBoxImage.Information)
		End Function

		Public Shared Sub ApplyWindowTheme(window As Window)
			If window Is Nothing OrElse Not UseDarkThemeSession Then
				Return
			End If

			Select Case True
				Case TypeOf window Is FrmMain
					SetBrush(window, "brushMainText", "#FFF4F7FA")
					SetBrush(window, "brushMainMutedText", "#FFB8C3D1")
					SetBrush(window, "brushMainControlBg", "#FF181E27")
					SetBrush(window, "brushMainControlBgHover", "#FF202834")
					SetBrush(window, "brushMainControlBgPressed", "#FF111720")
					SetBrush(window, "brushMainControlBorder", "#FF546173")
					SetBrush(window, "brushMainLogBg", "#FF1B222D")
					SetBrush(window, "brushMainMenuBg", "#FF10151D")
					SetBrush(window, "brushMainStatusBg", "#FF0F141B")
					SetBrush(window, "brushMainSelection", "#FF394657")
					SetGradient(window, "brushNvidia", "#FF122315", "#FF12161D")
					SetGradient(window, "brushIntel", "#FF13233B", "#FF12161D")
					SetGradient(window, "brushAmd", "#FF32171B", "#FF12161D")
					SetGradient(window, "brushRealtek", "#FF152235", "#FF12161D")
					SetGradient(window, "brushSoundBlaster", "#FF211933", "#FF12161D")
				Case TypeOf window Is FrmLaunch
					SetBrush(window, "LaunchTextBrush", "#FFF4F7FA")
					SetBrush(window, "LaunchMutedTextBrush", "#FFB8C3D1")
					SetBrush(window, "LaunchSurfaceBrush", "#FF12161D")
					SetBrush(window, "LaunchPanelBrush", "#FF181E27")
					SetBrush(window, "LaunchPanelHoverBrush", "#FF202834")
					SetBrush(window, "LaunchPanelPressedBrush", "#FF111720")
					SetBrush(window, "LaunchBorderBrush", "#FF546173")
					SetBrush(window, "LaunchSelectionBrush", "#FF394657")
					SetBrush(window, "LaunchWarningBrush", "#FFFF8F8F")
				Case TypeOf window Is FrmLog
					SetBrush(window, "bWindowBg", "#FF12161D")
					SetBrush(window, "bPanelBg", "#FF181E27")
					SetBrush(window, "bPanelHover", "#FF202834")
					SetBrush(window, "bBorder", "#FF546173")
					SetColor(window, "cNormal", "#FFF2F5F7")
					SetColor(window, "cValue", "#FF7AB8FF")
					SetColor(window, "cWarning", "#FFFFC857")
					SetColor(window, "cError", "#FFFF7B72")
					SetColor(window, "cSelected", "#FF2D3A4B")
					SetGradient(window, "bgBrushEvent", "#FF1E3150", "#FF12161D")
					SetGradient(window, "bgBrushWarning", "#FF4A3B16", "#FF12161D")
					SetGradient(window, "bgBrushError", "#FF4A1F24", "#FF12161D")
				Case TypeOf window Is FrmAbout
					SetBrush(window, "AboutWindowBg", "#FF12161D")
					SetBrush(window, "AboutPanelBg", "#FF181E27")
					SetBrush(window, "AboutPanelHover", "#FF202834")
					SetBrush(window, "AboutBorder", "#FF546173")
					SetBrush(window, "AboutText", "#FFF4F7FA")
					SetBrush(window, "AboutAccent", "#FF7AB8FF")
				Case TypeOf window Is FrmOptions
					SetBrush(window, "OptionsWindowBg", "#FF12161D")
					SetBrush(window, "OptionsPanelBg", "#FF181E27")
					SetBrush(window, "OptionsTextBrush", "#FFF4F7FA")
					SetBrush(window, "OptionsBorderBrush", "#FF546173")
					SetBrush(window, "OptionsButtonBg", "#FF181E27")
				Case TypeOf window Is DebugWindow
					SetBrush(window, "DebugWindowBg", "#FF12161D")
					SetBrush(window, "DebugPanelBg", "#FF181E27")
					SetBrush(window, "DebugPanelHover", "#FF202834")
					SetBrush(window, "DebugBorder", "#FF546173")
					SetBrush(window, "DebugText", "#FFF4F7FA")
				Case TypeOf window Is FrmSystemRestore
					SetBrush(window, "SystemRestoreWindowBg", "#FF12161D")
					SetBrush(window, "SystemRestorePanelBg", "#FF181E27")
					SetBrush(window, "SystemRestoreBorderBrush", "#FF546173")
					SetBrush(window, "SystemRestoreTextBrush", "#FFF4F7FA")
			End Select
		End Sub

		Private Shared Sub SetBrush(window As Window, key As String, colorText As String)
			Dim colorValue As Color = ParseColor(colorText)
			window.Resources(key) = New SolidColorBrush(colorValue)
		End Sub

		Private Shared Sub SetColor(window As Window, key As String, colorText As String)
			window.Resources(key) = ParseColor(colorText)
		End Sub

		Private Shared Sub SetGradient(window As Window, key As String, ParamArray colors() As String)
			If colors Is Nothing OrElse colors.Length = 0 Then
				Return
			End If

			Dim sourceBrush As LinearGradientBrush = TryCast(window.Resources(key), LinearGradientBrush)
			Dim newBrush As LinearGradientBrush

			If sourceBrush Is Nothing Then
				newBrush = New LinearGradientBrush With {
					.StartPoint = New Point(0, 0.5),
					.EndPoint = New Point(1, 0.5)
				}

				For i As Integer = 0 To colors.Length - 1
					Dim offset As Double = If(colors.Length = 1, 0, CDbl(i) / CDbl(colors.Length - 1))
					newBrush.GradientStops.Add(New GradientStop(ParseColor(colors(i)), offset))
				Next
			Else
				newBrush = sourceBrush.Clone()

				For i As Integer = 0 To Math.Min(newBrush.GradientStops.Count, colors.Length) - 1
					newBrush.GradientStops(i).Color = ParseColor(colors(i))
				Next
			End If

			window.Resources(key) = newBrush
		End Sub

		Private Shared Function ParseColor(colorText As String) As Color
			Return CType(ColorConverter.ConvertFromString(colorText), Color)
		End Function

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

			Languages.Load()        'default = english

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
					nativeLang = item       'take native on hold incase last used language not found (avoid multiple loops)
				End If
			Next

			If lastUsedLang IsNot Nothing Then
				Application.Settings.SelectedLanguage = lastUsedLang
			Else
				If nativeLang IsNot Nothing Then
					Application.Settings.SelectedLanguage = nativeLang              'couldn't find last used, using native lang
				Else
					Application.Settings.SelectedLanguage = Languages.DefaultEng    'couldn't find last used nor native lang, using default (English)
				End If
			End If

			Languages.Load(Application.Settings.SelectedLanguage)
		End Sub

		Private Sub ExtractEnglishLangFile(ByVal fileName As String, ByVal langEng As Languages.LanguageOption)
			Try
				Using stream As Stream = Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("English.xml")
					If _fileIo.ExistsFile(fileName) Then
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

		Private Sub KillGPUStatsProcesses()
			' Not sure for the x86 one...
			' Shady: probably the same but without _x64, and a few sites seem to confirm this, doesn't hurt to just add it anyway

			KillProcess(
		 "MSIAfterburner",
		  "PrecisionX_x64",
		  "PrecisionXServer_x64",
		  "PrecisionX",
		  "PrecisionXServer",
		  "RTSS",
		  "RTSSHooksLoader64",
		  "EncoderServer64",
		  "RTSSHooksLoader",
		  "EncoderServer",
		  "nvidiaInspector")
		End Sub

		Private Sub KillProcess(ByVal ParamArray processnames As String())
			For Each processName As String In processnames
				If String.IsNullOrEmpty(processName) Then
					Continue For
				End If

				For Each process As Process In Process.GetProcessesByName(processName)
					Try
						process.Kill()
						Application.Settings.ProcessKilled = True
					Catch ex As Exception
						Log.AddExceptionWithValues(ex, "@KillProcess()", String.Concat("ProcessName: ", processName))
					End Try
				Next
			Next
		End Sub

		Private Sub LaunchMainWindow()
			' >>> Loading UI <<<
			Try
				Dim window As FrmMain = New FrmMain() With {.DataContext = Data, .Topmost = True, .Visibility = Visibility.Visible}

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
				m_isDataSaved = True

				ShowThemedNotice("Launching Main Window failed!" & CRLF &
			 CRLF &
			 ex.Message & CRLF &
			 CRLF &
			 ex.StackTrace, "Display Driver Uninstaller")

				Me.Shutdown(0)
			End Try
		End Sub

		Private Sub AppClosing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
			Try
				If FrmMain.WorkTask IsNot Nothing Then                    ' workThread running, cleaning in progress!
					' Should take few milliseconds...	
					If FrmMain.WorkTask.Status = TaskStatus.Running Then Thread.Sleep(200)
					If FrmMain.WorkTask.Status = TaskStatus.Running Then Thread.Sleep(2000)

					' workThread still running!
					If FrmMain.WorkTask.Status = TaskStatus.Running Then
						e.Cancel = True
						Exit Sub
					End If
				End If
			Catch ex As Exception
				e.Cancel = True         ' frmMain.workThread may be null after checking
			End Try
		End Sub

		Private Sub AppClose(ByVal sender As Object, ByVal e As System.EventArgs)
			Try
				' frmMain is already closed here
				'Here we remove the modification done by DDU to allow PAEXEC (system impersonalisation tools)
				'And Task scheduler service (allowing task removal)

				'Try
				'	Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
				'		If regkey IsNot Nothing Then
				'			regkey.DeleteSubKeyTree("PAexec")
				'		End If
				'	End Using
				'Catch ex As Exception
				'	Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (PAExec)!")
				'End Try

				'Try
				'	Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
				'		If regkey IsNot Nothing Then
				'			regkey.DeleteSubKeyTree("PAexec")
				'		End If
				'	End Using
				'Catch ex As Exception
				'	Log.AddException(ex, "Failed to remove '\SafeBoot\Network' RegistryKey (PAExec)!")
				'End Try

				'Try
				'	Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
				'		If regkey IsNot Nothing Then
				'			regkey.DeleteSubKeyTree("Schedule")
				'		End If
				'	End Using
				'Catch ex As Exception
				'	Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (Schedule)!")
				'End Try

				'Try
				'	Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
				'		If regkey IsNot Nothing Then
				'			regkey.DeleteSubKeyTree("Schedule")
				'		End If
				'	End Using
				'Catch ex As Exception
				'	Log.AddException(ex, "Failed to remove '\SafeBoot\Network' RegistryKey (Schedule)!")
				'End Try

				'Try
				'	Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
				'		If regkey IsNot Nothing Then
				'			regkey.DeleteSubKeyTree("PNP_TDI")
				'		End If
				'	End Using
				'Catch ex As Exception
				'	Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (PNP_TDI)!")
				'End Try

				SaveData()
			Finally
				Me.Shutdown(0)  ' Close application completely
			End Try
		End Sub

		Private Sub Application_Startup(sender As Object, e As System.Windows.StartupEventArgs) Handles Me.Startup
            'If WindowsIdentity.GetCurrent().IsSystem Then
            '	MessageBox.Show("Attach debugger!")		' for Debugging System process
            '	IsDebug = True
            'End If

            ' Force software rendering
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly

            ' Vérify if we started as a service
            If Environment.CommandLine.Contains("/service") Then
				Try
					Dim ServicesToRun() As System.ServiceProcess.ServiceBase = {New DDUSafeBootService()}
					System.ServiceProcess.ServiceBase.Run(ServicesToRun)
					AllowSafeBootServiceToRunInSafemode(False)
					Me.Shutdown()
					Exit Sub
				Catch ex As Exception
					EventLog.WriteEntry("DDUSafeBootHandler", ex.Message, EventLogEntryType.Error)
					Log.AddException(ex, "Failed to start DDU SafeBoot Service")
					Log.SaveToFile()
					Me.Shutdown()
					Exit Sub
				End Try
			End If

			If Not IsNet48OrNewer() Then
				ShowThemedNotice("Minimum requirement is Microsoft .NET Framework 4.8. Please update your current .NET Framework.")
				Me.Shutdown()
				Exit Sub
			End If

			If Not IsNet45OrNewer() Then
				ShowThemedNotice("Minimum requirement is Microsoft .NET Framework 4.8. Please update your current .NET Framework.")
				Me.Shutdown()
				Exit Sub
			End If

			Dim info As LogEntry = Log.CreateEntry(Nothing, "The following paths are detected.")
			info.Type = LogType.Event
			info.Separator = " = "

			info.Add(If(_fileIo.ExistsFile(Paths.AppExeFile), "[Found]", "[Not found]") + " AppExeFile", Paths.AppExeFile)
			info.Add(If(_fileIo.ExistsDir(Paths.AppBase), "[Found]", "[Not found]") + " AppBase", Paths.AppBase)
			info.Add(If(_fileIo.ExistsDir(Paths.Settings), "[Found]", "[Not found]") + " Settings", Paths.Settings)
			info.Add(If(_fileIo.ExistsDir(Paths.Logs), "[Found]", "[Not found]") + " Logs", Paths.Logs)
			info.Add(If(_fileIo.ExistsDir(Paths.Language), "[Found]", "[Not found]") + " Language", Paths.Language)
			info.Add(KvP.Empty)
			info.Add(If(_fileIo.ExistsDir(Paths.ProgramFiles), "[Found]", "[Not found]") + " ProgramFiles", Paths.ProgramFiles)
			info.Add(If(_fileIo.ExistsDir(Paths.ProgramFilesx86), "[Found]", "[Not found]") + " ProgramFilesx86", Paths.ProgramFilesx86)
			info.Add(KvP.Empty)
			info.Add(If(_fileIo.ExistsDir(Paths.Roaming), "[Found]", "[Not found]") + " Roaming", Paths.Roaming)
			info.Add(If(_fileIo.ExistsDir(Paths.AppBaseRoaming), "[Found]", "[Not found]") + " AppBaseRoaming", Paths.AppBaseRoaming)
			info.Add(KvP.Empty)
			info.Add(If(_fileIo.ExistsDir(Paths.SystemDrive), "[Found]", "[Not found]") + " SystemDrive", Paths.SystemDrive)
			info.Add(If(_fileIo.ExistsDir(Paths.WinDir), "[Found]", "[Not found]") + " WinDir", Paths.WinDir)
            info.Add(If(_fileIo.ExistsDir(Paths.UsersPath), "[Found]", "[Not found]") + " UserPath", Paths.UsersPath)
            info.Add(If(_fileIo.ExistsDir(Paths.System32), "[Found]", "[Not found]") + " System32", Paths.System32)
			If IntPtr.Size = 8 Then
				info.Add(If(_fileIo.ExistsDir(Paths.SysWOW64), "[Found]", "[Not found]") + " SysWOW64", Paths.SysWOW64)
			End If

			Application.Log.Add(info)
			'KillGPUStatsProcesses()
			Try
				' Launch as Admin if not
				If Not Tools.UserHasAdmin Then
					Using process As Process = New Process() With {.StartInfo = New ProcessStartInfo(Application.Paths.AppExeFile, String.Join(" ", e.Args) & If(Application.Settings.ProcessKilled, " -processkilled", "")) With {.Verb = "runas"}}

						Try
							process.Start()
						Catch ex As ComponentModel.Win32Exception
							Dim errCode As UInt32 = GetUInt32(ex.NativeErrorCode)
							Dim msg As String = String.Format("Error:{0}{1}{0}{0}Message:{0}{2}", CRLF, GetErrorEnum(errCode), ex.Message)

							If errCode = Errors.CANCELLED Then  'User pressed 'No' on UAC screen
								msg = String.Format("Administrator rights are required to use application.{0}{0}{1}", CRLF, msg)
							End If

							ShowThemedNotice(msg, "Display Driver Uninstaller")
							Log.AddMessage("No admin rights, denied by user via UAC")
							Log.SaveToFile()
						Catch ex2 As Exception
							Log.AddException(ex2, "No admin rights")
							Log.SaveToFile()
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
						If ProcessLinks() Then      ' Link found and opened?
							Log.AddMessage("Closed by HasLinkArg")
							Log.SaveToFile()
							Me.Shutdown(0)          ' Skip loading if link is opened
							Exit Sub
						End If
					End If
				Catch ex As Exception
					Log.AddException(ex, "Parsing arguments failed!" & CRLF & ">> Application_Startup()")
				End Try


				' DDU completed cleaning just close and dont do anything else.
				Try
					If LaunchOptions.CleanComplete Then
						If LaunchOptions.Restart Then
							Thread.Sleep(2000)
							RemoveRegOption()
							RestartComputer()
							AppClose(Me, EventArgs.Empty)          ' Skip loading.
							Exit Sub
						End If
						If LaunchOptions.Shutdown Then
							Thread.Sleep(2000)
							RemoveRegOption()
							ShutdownComputer()
							AppClose(Me, EventArgs.Empty)     ' Skip loading.
							Exit Sub
						End If
						AppClose(Me, EventArgs.Empty)          ' Skip loading.
						Exit Sub
					End If
				Catch ex As Exception
					Log.AddException(ex, "Parsing arguments failed!" & CRLF & ">> Application_Startup()")
				End Try

				' Load default language (English) + Find language files from folder
				InitLanguages()


				' Load AppSettings and select last used language (if settings exists)
				Settings.Load()
				m_useDarkThemeSession = Settings.UseDarkTheme


				' Select language (last used -> native -> default)
				' Now we have translated messages available
				SelectLanguage()

				If StrContainsAny(Application.Settings.SelectedLanguage.ToString(), True, "he-il", "fa-ir", "ar-ye") Then
					Application.Settings.FlowControl = FlowDirection.RightToLeft
				Else
					Application.Settings.FlowControl = FlowDirection.LeftToRight
				End If

				' Useful on next steps
				GetOSVersion()
				Settings.WinIs64 = (IntPtr.Size = 8)

				Try

					'We check if there are any reboot from windows update pending. and if so we quit.
					If Not LaunchOptions.Silent AndAlso WinUpdatePending() Then
						ShowThemedNotice(Languages.GetTranslation("frmMain", "Messages", "Text14"))
						Log.SaveToFile()
						Me.Shutdown(0)
						Exit Sub
					End If

				Catch ex As Exception
					Log.AddException(ex)
					Log.SaveToFile()
					Me.Shutdown(0)
					Exit Sub
				End Try

				'Verify is there is missing files in DDU\settings folder (only check for 2 atm)
				If Not _fileIo.ExistsFile(Application.Paths.AppBase & "settings\NVIDIA\services.cfg") Then
					ShowThemedNotice(Application.Paths.AppBase & "settings\NVIDIA\services.cfg does not exist. please reinstall or extract DDU correctly")
					Log.SaveToFile()
					Me.Shutdown(0)
					Exit Sub
				End If

				If Not _fileIo.ExistsFile(Application.Paths.AppBase & "settings\AMD\services.cfg") Then
					ShowThemedNotice(Application.Paths.AppBase & "settings\AMD\services.cfg does not exist. please reinstall or extract DDU correctly")
					Log.SaveToFile()
					Me.Shutdown(0)
					Exit Sub
				End If

				If Not WindowsIdentity.GetCurrent().IsSystem Then
					If LaunchAsSystem() Then
						' Launched as System, close this instance, True = close, false = continue
						Log.SaveToFile()
						Me.Shutdown(0)
					End If
				End If

			Catch ex As Exception
				Log.AddException(ex, "Some part of application startup failed!" & CRLF & ">> Application_Startup()")
				Log.SaveToFile()    ' Save to file

				ShowThemedNotice("Launching Application failed!" & CRLF &
			 "A problem occurred in one of the module, send your DDU logs to the developer." & CRLF &
			   CRLF &
			   ex.Message, "Display Driver Uninstaller")

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
            ElseIf Application.LaunchOptions.VisitPatron Then
                webAddress = URL_PATRON
            ElseIf Application.LaunchOptions.VisitDiscord Then
                webAddress = URL_DISCORD
            ElseIf Application.LaunchOptions.VisitNvidia Then
                webAddress = URL_NVIDIA
            ElseIf Application.LaunchOptions.VisitAMD Then
                webAddress = URL_AMD
            ElseIf Application.LaunchOptions.VisitINTEL Then
                webAddress = URL_INTEL
            ElseIf Application.LaunchOptions.VisitGeforce Then
                webAddress = URL_GEFORCE
            ElseIf Application.LaunchOptions.VisitDDUHome Then
                webAddress = URL_DDUHOME
            ElseIf Application.LaunchOptions.VisitDDUShop Then
                webAddress = URL_DDUSHOP
            ElseIf Application.LaunchOptions.VisitSVN Then
				webAddress = URL_SVN
			ElseIf Application.LaunchOptions.VisitOffer Then
				webAddress = URL_OFFER
			End If

			If Not String.IsNullOrWhiteSpace(webAddress) Then

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

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
				If regkey IsNot Nothing Then
					regOSValue = regkey.GetValue("CurrentVersion", String.Empty).ToString()

					If Not String.IsNullOrWhiteSpace(regOSValue) Then
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

			If Not versionFound Then        ' Double check for Unknown
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

		Private Function LaunchAsSystem() As Boolean
			'here I check if the process is running on system user account. if not, make it so.
			'This code checks to see which mode Windows has booted up in.

			Dim isWinXP As Boolean = (Settings.WinVersion = OSVersion.WinXP Or Settings.WinVersion = OSVersion.WinXPPro_Server2003)

			Select Case System.Windows.Forms.SystemInformation.BootMode
				Case System.Windows.Forms.BootMode.FailSafeWithNetwork, System.Windows.Forms.BootMode.FailSafe
					'The computer was booted using only the basic files and drivers.
					'This is the same as Safe Mode

					If Not isWinXP Then

						If Settings.UsedBCD Then
							'don't use BCDEDIT if it was not used to get into safe mode.

							Using process As Process = New Process() With
					  {
					   .StartInfo = New ProcessStartInfo(Paths.System32 & "BCDEDIT", " /deletevalue safeboot") With
					   {
					 .UseShellExecute = False,
					 .CreateNoWindow = True,
					 .RedirectStandardOutput = False
					   }
					  }
								Try

									process.Start()
									process.WaitForExit()
									process.Close()
									Settings.UsedBCD = False
								Catch ex As Exception
									Log.AddException(ex, "Failed to use BCDEDIT! - " & Paths.System32 & "BCDEDIT")
								End Try
							End Using
						End If
					End If
				Case System.Windows.Forms.BootMode.Normal
					' added iselevated so this will not try to boot into safe mode/boot menu without admin rights,
					' as even with the admin check on startup it was for some reason still trying to gain registry access 
					' and throwing an exception --probably because there's no return

					If Not isWinXP AndAlso Tools.UserHasAdmin Then
						If LaunchOptions.NoSafeModeMsg Then
							Exit Select
						End If

						If Settings.EnableSafeModeDialog Then
							Dim bootOption As Integer = -1              '-1 = close, 0 = normal, 1 = SafeMode, 2 = SafeMode with network
							Dim frmSafeBoot As New FrmLaunch With {.DataContext = Data, .Topmost = True}


							Dim launch As Boolean? = frmSafeBoot.ShowDialog()

							If launch IsNot Nothing AndAlso launch.Value Then
								bootOption = frmSafeBoot.selection
							End If

							Select Case bootOption
								Case 0 'normal
									Exit Select

								Case 1 'SafeMode
									Settings.UsedBCD = True
									Return RestartToSafemode(False)

								Case 2 'SafeMode with network
									Settings.UsedBCD = True
									Return RestartToSafemode(True)

								Case Else '-1 = Close
									Log.AddMessage("Close on frmLaunch selected.")
									Return True

							End Select
						End If
					End If
			End Select

			If Application.IsDebug Then
				Return False
			End If

			'Dim args() As String = New String() {"stop PAExec", "delete PAExec", "interrogate PAExec"}

			'For Each arg As String In args
			'	Using process As Process = New Process() With
			'	 {
			'	  .StartInfo = New ProcessStartInfo(Paths.System32 & "sc.exe", arg) With
			'	  {
			'	   .UseShellExecute = False,
			'	   .CreateNoWindow = True,
			'	   .RedirectStandardOutput = False
			'	  }
			'	 }
			'		process.Start()
			'		process.WaitForExit()
			'		process.Close()
			'		Thread.Sleep(10)
			'	End Using
			'Next
			Try
				' These are only needed to set ONCE during App lifetime
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
				'ACL.AddPriviliges(ACL.SE.DEBUG_NAME, ACL.SE.ASSIGNPRIMARYTOKEN_NAME, ACL.SE.AUDIT_NAME, ACL.SE.BACKUP_NAME, ACL.SE.CHANGE_NOTIFY_NAME, ACL.SE.CREATE_GLOBAL_NAME, ACL.SE.CREATE_PAGEFILE_NAME, ACL.SE.CREATE_PERMANENT_NAME, ACL.SE.CREATE_TOKEN_NAME, ACL.SE.DEBUG_NAME, ACL.SE.ENABLE_DELEGATION_NAME, ACL.SE.IMPERSONATE_NAME, ACL.SE.INCREAQUOTA_NAME, ACL.SE.INC_BAPRIORITY_NAME, ACL.SE.LOAD_DRIVER_NAME, ACL.SE.LOCK_MEMORY_NAME, ACL.SE.MACHINE_ACCOUNT_NAME, ACL.SE.MANAGE_VOLUME_NAME, ACL.SE.PROF_SINGLE_PROCESS_NAME, ACL.SE.REMOTE_SHUTDOWN_NAME, ACL.SE.RESTORE_NAME, ACL.SE.SECURITY_NAME, ACL.SE.SHUTDOWN_NAME, ACL.SE.SYSTEMTIME_NAME, ACL.SE.SYSTEM_ENVIRONMENT_NAME, ACL.SE.SYSTEM_PROFILE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME)
			Catch ex As Exception
				Log.AddException(ex, "AddPriviliges failed!" & CRLF & ">> AppStart()")
			End Try

			Return False
		End Function

		Private Function RestartToSafemode(ByVal withNetwork As Boolean) As Boolean
			Try
				SystemRestore(Nothing) 'we try to do a system restore if allowed before going into safemode.
				Log.AddMessage("Restarting in safemode")

				Using process As New Process() With
				{
				.StartInfo = New ProcessStartInfo(Paths.System32 & "BCDEDIT", If(withNetwork, "/set {current} safeboot network", "/set {current} safeboot minimal")) With
					{
					.UseShellExecute = False,
					.CreateNoWindow = True,
					.RedirectStandardOutput = False
					}
				}

					Try
						process.Start()
						process.WaitForExit()
						process.Close()
					Catch ex As Exception
						Log.AddException(ex, "Failed to use BCDEDIT! - " & Paths.System32 & "BCDEDIT")
						Return False
					End Try

				End Using

				InstallSafeBootService()

				Try
					Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
						If regkey IsNot Nothing Then
							regkey.SetValue("*" + Settings.AppName, Paths.AppExeFile)
							regkey.SetValue("*UndoSM", Paths.System32 & "BCDEDIT /deletevalue safeboot")
						End If
					End Using
				Catch ex As Exception
					Log.AddException(ex)
				End Try

				AllowSafeBootServiceToRunInSafemode(True)

				RestartComputer()

				Return True
			Catch ex As Exception
				Log.AddException(ex, "Failed to reboot into Safemode!")
				Return False
			End Try
		End Function

		Private Sub AllowSafeBootServiceToRunInSafemode(ByVal addSafeBootValue As Boolean)
			If (addSafeBootValue) Then
				Try
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
						If regkey IsNot Nothing Then
							Using regSubKey As RegistryKey = regkey.CreateSubKey("DDUSafeBootHandler", RegistryKeyPermissionCheck.ReadWriteSubTree)
								regSubKey.SetValue("", "Service")
							End Using
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
				End Try

				Try
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
						If regkey IsNot Nothing Then
							Using regSubKey As RegistryKey = regkey.CreateSubKey("DDUSafeBootHandler", RegistryKeyPermissionCheck.ReadWriteSubTree)
								regSubKey.SetValue("", "Service")
							End Using
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
				End Try
				Return
			End If

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
					If regkey IsNot Nothing Then
						regkey.DeleteSubKeyTree("DDUSafeBootHandler")
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (DDUSafeBootHandler)!")
			End Try

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
					If regkey IsNot Nothing Then
						regkey.DeleteSubKeyTree("DDUSafeBootHandler")
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (DDUSafeBootHandler)!")
			End Try

		End Sub

		Private Sub InstallSafeBootService()
			Try

				Dim serviceExePath As String = Path.Combine(Path.GetTempPath(), "DDUSafeBootHandler.exe")


				File.Copy(Paths.AppExeFile, serviceExePath, True)


				Dim processInfo As New ProcessStartInfo("sc.exe",
			$"create DDUSafeBootHandler binPath= ""{serviceExePath} /service"" start= auto") With {
			.UseShellExecute = True,
			.CreateNoWindow = True,
			.Verb = "runas"
		}

				Using process As New Process With {
				.StartInfo = processInfo
			}
					process.Start()
					process.WaitForExit()
				End Using

				Log.AddMessage("SafeBoot Handler Service installed successfully")

			Catch ex As Exception
				Log.AddException(ex, "Failed to install SafeBoot Handler Service")
			End Try
		End Sub

        Public Shared Sub RemoveRegOption()
            If Forms.SystemInformation.BootMode <> Forms.BootMode.Normal Then
                Try
                    Using regControl As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot", Writable:=True)
                        If regControl IsNot Nothing AndAlso StrContainsAny("Option", True, regControl.GetSubKeyNames()) Then
                            regControl.DeleteSubKeyTree("Option", throwOnMissingSubKey:=False)
                            Application.Log.AddMessage("Deleted SafeBoot\Option key before reboot.")
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddWarningMessage("Could not delete SafeBoot\Option key: " & ex.Message)
                End Try
            End If
        End Sub

        Public Shared Sub RestartComputer()
			If Not m_dispatcher.CheckAccess() Then
				m_dispatcher.Invoke(Sub() RestartComputer())
			Else
				Log.AddMessage("Restarting Computer ")
				Application.SaveData()

				Using process As New Process() With
			  {
			   .StartInfo = New ProcessStartInfo(Paths.System32 & "shutdown", "/r /t 0") With
			   {
			 .WindowStyle = ProcessWindowStyle.Hidden,
			 .UseShellExecute = False,
			 .CreateNoWindow = True,
			 .RedirectStandardOutput = False
			   }
			  }
					Try
						process.Start()
						process.WaitForExit()
					Catch ex As Exception
						Log.AddException(ex, "Failed to use into shutdown! - " & Paths.System32 & "shutdown")
					End Try

				End Using
			End If
		End Sub

		Public Shared Sub ShutdownComputer()
			If Not m_dispatcher.CheckAccess() Then
				m_dispatcher.Invoke(Sub() ShutdownComputer())
			Else
				Log.AddMessage("Shutdown Computer ")
				Application.SaveData()

				Using process As Process = New Process() With
			{
			  .StartInfo = New ProcessStartInfo(Paths.System32 & "shutdown", "/s /t 0") With
			   {
			   .WindowStyle = ProcessWindowStyle.Hidden,
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
		End Sub

		Public Shared Sub SystemRestore(ByVal owner As Window)
			If Not m_dispatcher.CheckAccess() Then
				m_dispatcher.Invoke(Sub() SystemRestore(owner))
			Else
				If Application.Settings.CreateRestorePoint AndAlso Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then
					Dim frmSystemRestore As New FrmSystemRestore With
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
		Private ReadOnly m_launchOptions As AppLaunchOptions
		Private ReadOnly m_settings As AppSettings
		Private ReadOnly m_paths As AppPaths
		Private ReadOnly m_log As AppLog

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
End Namespace

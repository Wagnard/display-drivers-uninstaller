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
                If m_Data IsNot Nothing Then Return Settings.UseDarkTheme
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
            If window Is Nothing Then Return

            If Not UseDarkThemeSession Then
                Select Case True
                    Case TypeOf window Is FrmMain
                        SetBrush(window, "brushMainText", "#FF000000")
                        SetBrush(window, "brushMainMutedText", "#FF5C6670")
                        SetBrush(window, "brushMainControlBg", "#FFFFFFFF")
                        SetBrush(window, "brushMainControlBgHover", "#FFF2F2F2")
                        SetBrush(window, "brushMainControlBgPressed", "#FFE6E6E6")
                        SetBrush(window, "brushMainControlBorder", "#FF7A7A7A")
                        SetBrush(window, "brushMainLogBg", "#FFFFFFFF")
                        SetBrush(window, "brushMainMenuBg", "#28FFFFFF")
                        SetBrush(window, "brushMainStatusBg", "#28FFFFFF")
                        SetBrush(window, "brushMainSelection", "#FFCBDCF4")
                        SetGradient(window, "brushNvidia", "#FFDCFFDC", "#FFFFFFFF")
                        SetGradient(window, "brushIntel", "#FFDCDCFF", "#FFFFFFFF")
                        SetGradient(window, "brushAmd", "#FFFFE6E6", "#FFFFFFFF")
                        SetGradient(window, "brushRealtek", "#FFDCDCFF", "#FFFFFFFF")
                        SetGradient(window, "brushSoundBlaster", "#FFDCDCFF", "#FFFFFFFF")
                    Case TypeOf window Is FrmLaunch
                        SetBrush(window, "LaunchTextBrush", "#FF000000")
                        SetBrush(window, "LaunchMutedTextBrush", "#FF5C6670")
                        SetBrush(window, "LaunchSurfaceBrush", "#FFFFFFFF")
                        SetBrush(window, "LaunchPanelBrush", "#FFFFFFFF")
                        SetBrush(window, "LaunchPanelHoverBrush", "#FFF2F2F2")
                        SetBrush(window, "LaunchPanelPressedBrush", "#FFE6E6E6")
                        SetBrush(window, "LaunchBorderBrush", "#FF000000")
                        SetBrush(window, "LaunchSelectionBrush", "#FFCBDCF4")
                        SetBrush(window, "LaunchWarningBrush", "#FFEB0000")
                    Case TypeOf window Is FrmLog
                        SetBrush(window, "bWindowBg", "#FFFFFFFF")
                        SetBrush(window, "bPanelBg", "#FFF0F0F0")
                        SetBrush(window, "bPanelHover", "#FFE6E6E6")
                        SetBrush(window, "bBorder", "#FF000000")
                        SetColor(window, "cNormal", "#FF000000")
                        SetColor(window, "cValue", "#FF0000D2")
                        SetColor(window, "cWarning", "#FF000000")
                        SetColor(window, "cError", "#FFFF0000")
                        SetColor(window, "cSelected", "#FFCBCBCB")
                        SetGradient(window, "bgBrushEvent", "#FFBEC8FF", "#FFFFFFFF")
                        SetGradient(window, "bgBrushWarning", "#FFFFFFB4", "#FFFFFFFF")
                        SetGradient(window, "bgBrushError", "#FFFFE6E6", "#FFFFFFFF")
                    Case TypeOf window Is FrmAbout
                        SetBrush(window, "AboutWindowBg", "#FFFFFFFF")
                        SetBrush(window, "AboutPanelBg", "#FFFFFFFF")
                        SetBrush(window, "AboutPanelHover", "#FFF2F2F2")
                        SetBrush(window, "AboutBorder", "#FF000000")
                        SetBrush(window, "AboutText", "#FF000000")
                        SetBrush(window, "AboutAccent", "#FF000000")
                    Case TypeOf window Is FrmOptions
                        SetBrush(window, "OptionsWindowBg", "#FFFFFFFF")
                        SetBrush(window, "OptionsPanelBg", "#FFFFFFFF")
                        SetBrush(window, "OptionsTextBrush", "#FF000000")
                        SetBrush(window, "OptionsBorderBrush", "#FF000000")
                        SetBrush(window, "OptionsButtonBg", "#FFF0F0F0")
                        SetBrush(window, "OptionsButtonBgHover", "#FFE5E5E5")
                        SetBrush(window, "OptionsButtonBgPressed", "#FFCCCCCC")
                    Case TypeOf window Is DebugWindow
                        SetBrush(window, "DebugWindowBg", "#FFD2E4FF")
                        SetBrush(window, "DebugPanelBg", "#FFFFFFFF")
                        SetBrush(window, "DebugPanelHover", "#FFF2F2F2")
                        SetBrush(window, "DebugBorder", "#FF000000")
                        SetBrush(window, "DebugText", "#FF000000")
                    Case TypeOf window Is FrmSystemRestore
                        SetBrush(window, "SystemRestoreWindowBg", "#FFFFFFFF")
                        SetBrush(window, "SystemRestorePanelBg", "#FFFFFFFF")
                        SetBrush(window, "SystemRestoreBorderBrush", "#FF000000")
                        SetBrush(window, "SystemRestoreTextBrush", "#FF000000")
                    Case TypeOf window Is FrmNotice
                        SetBrush(window, "NoticeTextBrush", "#FF000000")
                        SetBrush(window, "NoticeSurfaceBrush", "#FFFFFFFF")
                        SetBrush(window, "NoticePanelBrush", "#FFF0F0F0")
                        SetBrush(window, "NoticePanelHoverBrush", "#FFE5E5E5")
                        SetBrush(window, "NoticeBorderBrush", "#FF000000")
                        SetBrush(window, "NoticeAccentBrush", "#FF0078D4")
                End Select
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
                    SetBrush(window, "OptionsButtonBgHover", "#FF202834")
                    SetBrush(window, "OptionsButtonBgPressed", "#FF111720")
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
                Case TypeOf window Is FrmNotice
                    SetBrush(window, "NoticeTextBrush", "#FFF4F7FA")
                    SetBrush(window, "NoticeSurfaceBrush", "#FF12161D")
                    SetBrush(window, "NoticePanelBrush", "#FF181E27")
                    SetBrush(window, "NoticePanelHoverBrush", "#FF202834")
                    SetBrush(window, "NoticeBorderBrush", "#FF546173")
                    SetBrush(window, "NoticeAccentBrush", "#FF58A6FF")
            End Select
        End Sub

        Public Shared Sub ApplyThemeToAllWindows()
            If m_dispatcher Is Nothing Then Return
            If Not m_dispatcher.CheckAccess() Then
                m_dispatcher.Invoke(Sub() ApplyThemeToAllWindows())
                Return
            End If
            For Each w As Window In Current.Windows
                ApplyWindowTheme(w)
            Next
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
                ' Settings are only persisted once the app got far enough to legitimately own
                ' them: an instance that bails out during startup must never overwrite them.
                If Not m_isDataSaved AndAlso m_allowSaveData Then
                    Settings.Save()

                    m_isDataSaved = True
                End If

                ' The log, on the other hand, is always worth writing - it is what users send
                ' for diagnosis, and the startup bail-out paths are exactly where it matters.
                ' Safe to call repeatedly: SaveLog() builds the filename from the first entry's
                ' timestamp, so it rewrites the same file instead of creating a new one.
                Log.SaveToFile()
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

        ''' <summary>
        ''' Single exit point for the application: persists what has to be persisted, then shuts
        ''' down. Wired to frmMain.Closed, and called directly by the startup bail-out paths
        ''' (where frmMain never existed) so they all behave identically.
        ''' SaveData() decides what actually gets written - settings only when this instance owns
        ''' them, the log always.
        ''' </summary>
        Private Sub AppClose(ByVal sender As Object, ByVal e As System.EventArgs)
            Try
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

            ' IsNet48OrNewer already implies IsNet45OrNewer, so a single check is enough.
            If Not IsNet48OrNewer() Then
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

                        Dim handedOver As Boolean = False

                        Try
                            process.Start()
                            handedOver = True
                        Catch ex As ComponentModel.Win32Exception
                            Dim errCode As UInt32 = GetUInt32(ex.NativeErrorCode)
                            Dim msg As String = String.Format("Error:{0}{1}{0}{0}Message:{0}{2}", CRLF, GetErrorEnum(errCode), ex.Message)

                            If errCode = Errors.CANCELLED Then  'User pressed 'No' on UAC screen
                                msg = String.Format("Administrator rights are required to use application.{0}{0}{1}", CRLF, msg)
                            End If

                            ShowThemedNotice(msg, "Display Driver Uninstaller")
                            Log.AddMessage("No admin rights, denied by user via UAC")
                        Catch ex2 As Exception
                            Log.AddException(ex2, "No admin rights")
                        End Try

                        If handedOver Then
                            ' The elevated instance took over and writes its own log. This one has
                            ' nothing to report, so it exits without producing a second, near-empty
                            ' log file for every single launch.
                            Me.Shutdown(0)
                        Else
                            ' Elevation failed or was refused: that IS worth keeping on disk.
                            AppClose(Me, EventArgs.Empty)
                        End If

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
                            AppClose(Me, EventArgs.Empty)   ' Skip loading if link is opened
                            Exit Sub
                        End If
                    End If
                Catch ex As Exception
                    Log.AddException(ex, "Parsing arguments failed!" & CRLF & ">> Application_Startup()")
                End Try

                ' DDU completed cleaning just close and dont do anything else.
                Try
                    If LaunchOptions.CleanComplete Then

                        Dim maxWaitSeconds As Integer = 15
                        Dim startTime As DateTime = DateTime.Now
                        Dim currentProcessName As String = Process.GetCurrentProcess().ProcessName

                        While (DateTime.Now - startTime).TotalSeconds < maxWaitSeconds

                            Dim processes() As Process = Process.GetProcessesByName(currentProcessName)

                            Try
                                Dim otherInstanceExists As Boolean = processes.Length > 1
                                If Not otherInstanceExists Then Exit While
                            Finally
                                For Each p As Process In processes
                                    p.Dispose()
                                Next
                            End Try

                            Thread.Sleep(500)
                        End While

                        If LaunchOptions.Restart Then
                            RemoveRegOption()
                            RestartComputer()
                            AppClose(Me, EventArgs.Empty)
                            Exit Sub
                        End If

                        If LaunchOptions.Shutdown Then
                            RemoveRegOption()
                            ShutdownComputer()
                            AppClose(Me, EventArgs.Empty)
                            Exit Sub
                        End If

                        AppClose(Me, EventArgs.Empty)
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
                        AppClose(Me, EventArgs.Empty)
                        Exit Sub
                    End If

                Catch ex As Exception
                    Log.AddException(ex)
                    AppClose(Me, EventArgs.Empty)
                    Exit Sub
                End Try

                'Verify is there is missing files in DDU\settings folder (only check for 2 atm)
                If Not _fileIo.ExistsFile(Application.Paths.AppBase & "settings\NVIDIA\services.cfg") Then
                    ShowThemedNotice(Application.Paths.AppBase & "settings\NVIDIA\services.cfg does not exist. please reinstall or extract DDU correctly")
                    AppClose(Me, EventArgs.Empty)
                    Exit Sub
                End If

                If Not _fileIo.ExistsFile(Application.Paths.AppBase & "settings\AMD\services.cfg") Then
                    ShowThemedNotice(Application.Paths.AppBase & "settings\AMD\services.cfg does not exist. please reinstall or extract DDU correctly")
                    AppClose(Me, EventArgs.Empty)
                    Exit Sub
                End If

                ' Privileges needed by the registry/file ACL work all over DDU, and only ever
                ' set once per process. Applied unconditionally: this used to sit at the end of
                ' LaunchAsSystem behind a "Not IsSystem" guard, so a SYSTEM-context instance
                ' never received them.
                Try
                    ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
                    'ACL.AddPriviliges(ACL.SE.DEBUG_NAME, ACL.SE.ASSIGNPRIMARYTOKEN_NAME, ACL.SE.AUDIT_NAME, ACL.SE.BACKUP_NAME, ACL.SE.CHANGE_NOTIFY_NAME, ACL.SE.CREATE_GLOBAL_NAME, ACL.SE.CREATE_PAGEFILE_NAME, ACL.SE.CREATE_PERMANENT_NAME, ACL.SE.CREATE_TOKEN_NAME, ACL.SE.DEBUG_NAME, ACL.SE.ENABLE_DELEGATION_NAME, ACL.SE.IMPERSONATE_NAME, ACL.SE.INCREAQUOTA_NAME, ACL.SE.INC_BAPRIORITY_NAME, ACL.SE.LOAD_DRIVER_NAME, ACL.SE.LOCK_MEMORY_NAME, ACL.SE.MACHINE_ACCOUNT_NAME, ACL.SE.MANAGE_VOLUME_NAME, ACL.SE.PROF_SINGLE_PROCESS_NAME, ACL.SE.REMOTE_SHUTDOWN_NAME, ACL.SE.RESTORE_NAME, ACL.SE.SECURITY_NAME, ACL.SE.SHUTDOWN_NAME, ACL.SE.SYSTEMTIME_NAME, ACL.SE.SYSTEM_ENVIRONMENT_NAME, ACL.SE.SYSTEM_PROFILE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME)
                Catch ex As Exception
                    Log.AddException(ex, "AddPriviliges failed!" & CRLF & ">> Application_Startup()")
                End Try

                If HandleBootModeStartup() Then
                    ' True = this instance must close: a Safe Mode reboot has just been armed, or
                    ' the user closed the launch dialog.
                    AppClose(Me, EventArgs.Empty)
                    Exit Sub
                End If

                ' Catches the leftovers of every failed Safe Mode round-trip. The Exit Sub above
                ' keeps it out of reach when a reboot was just armed: Me.Shutdown() alone does not
                ' stop this method, and cleaning up here would delete the service that was just
                ' installed (still Stopped - it only starts on the next boot).
                CleanupSafeBootServiceLeftovers()

            Catch ex As Exception
                Log.AddException(ex, "Some part of application startup failed!" & CRLF & ">> Application_Startup()")

                ' Written before the notice on purpose: that dialog is modal, so if the user
                ' never dismisses it the log has still reached the disk. AppClose below writes
                ' it a second time, which costs nothing - same file, rewritten.
                Log.SaveToFile()

                ShowThemedNotice("Launching Application failed!" & CRLF &
             "A problem occurred in one of the module, send your DDU logs to the developer." & CRLF &
               CRLF &
               ex.Message, "Display Driver Uninstaller")

                AppClose(Me, EventArgs.Empty)
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

        ''' <summary>
        ''' Decides what DDU has to do based on the mode Windows booted into:
        '''   - Safe Mode : remove the safeboot value (only when DDU is the one that set it),
        '''                 so the next boot comes back to normal.
        '''   - Normal    : offer the Safe Mode dialog and, if accepted, arm the reboot.
        ''' Returns True when this instance must close - either a Safe Mode reboot has just
        ''' been armed, or the user closed the launch dialog.
        ''' </summary>
        Private Function HandleBootModeStartup() As Boolean
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
                    '
                    ' Not IsSystem: a SYSTEM-context instance has no interactive desktop to show
                    ' this dialog on. That check used to sit at the call site and gated the whole
                    ' function (a leftover from the PAExec days); the dialog is the only part
                    ' that actually needs it.
                    If Not isWinXP AndAlso Tools.UserHasAdmin AndAlso Not WindowsIdentity.GetCurrent().IsSystem Then
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

            Return False
        End Function

        Private Function RestartToSafemode(ByVal withNetwork As Boolean) As Boolean
            Try
                SystemRestore(Nothing) 'we try to do a system restore if allowed before going into safemode.
                Log.AddMessage("Restarting in safemode")

                ' ORDER MATTERS: every safety net is put in place FIRST, and the BCDEDIT call
                ' that actually arms Safe Mode is the very last step. That way a failure here
                ' leaves the machine completely untouched instead of rebooting into Safe Mode
                ' with no way back out.
                If Not InstallSafeBootService() Then
                    ' The service is the only escape route for a user who cannot log in while
                    ' in Safe Mode, so no service means no Safe Mode reboot. Nothing has been
                    ' modified at this point.
                    Log.AddWarningMessage("SafeBoot Handler Service could not be installed - the Safe Mode reboot has been cancelled to avoid locking the machine in Safe Mode.")
                    ShowThemedNotice(Languages.GetTranslation("frmMain", "Messages", "Text6"))
                    Return False
                End If

                AllowSafeBootServiceToRunInSafemode(True)

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

                ' Point of no return: from here on the next boot goes into Safe Mode.
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

                ' UsedBCD was just set in memory and MUST survive the reboot: it is what tells
                ' DDU to run BCDEDIT itself when it comes back up in Safe Mode, one of the three
                ' ways out of it. m_allowSaveData is normally only enabled by LaunchMainWindow,
                ' which has not run yet at this point, so RestartComputer's own SaveData() call
                ' would silently do nothing - until now the value only got persisted by accident,
                ' by the dying instance winning a race against "shutdown /r /t 0".
                m_allowSaveData = True
                SaveData()

                RestartComputer()

                Return True
            Catch ex As Exception
                Log.AddException(ex, "Failed to reboot into Safemode!")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Removes whatever the DDUSafeBootHandler service may have left behind: the service
        ''' registration, its two SafeBoot entries and the copied executable. Every failure
        ''' path of the Safe Mode workflow ends up here on the next DDU launch (service never
        ''' started, self-uninstall failed, machine came back through the RunOnce fallback).
        ''' Skipped while the service is still running so a boot in progress is never cut short.
        ''' </summary>
        Private Sub CleanupSafeBootServiceLeftovers()
            Const serviceName As String = "DDUSafeBootHandler"

            Try
                ' A pending Safe Mode round-trip must never be dismantled. DDU writes "*UndoSM"
                ' to RunOnce when it arms Safe Mode, and Windows deletes RunOnce entries as it
                ' runs them at the next logon - so as long as that value is still there the
                ' reboot has not happened yet (DDU was closed or crashed before restarting) and
                ' every piece of the setup must stay in place.
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", False)
                        If regkey IsNot Nothing AndAlso regkey.GetValue("*UndoSM", Nothing) IsNot Nothing Then
                            Log.AddMessage("A Safe Mode reboot is still pending - SafeBoot handler cleanup postponed.")
                            Return
                        End If
                    End Using
                Catch
                    ' Unreadable - fall through, the service status check below still applies.
                End Try

                Dim serviceExists As Boolean

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & serviceName, False)
                    serviceExists = (regkey IsNot Nothing)
                End Using

                If serviceExists Then
                    ' Never race the service: if it is still working (or retrying BCDEDIT after
                    ' a failure), leave everything in place and clean up on a later launch.
                    Try
                        Using svc As New System.ServiceProcess.ServiceController(serviceName)
                            If svc.Status <> System.ServiceProcess.ServiceControllerStatus.Stopped Then
                                Log.AddMessage($"SafeBoot handler service is {svc.Status} - leftover cleanup postponed.")
                                Return
                            End If
                        End Using
                    Catch
                        ' Status unreadable (already marked for deletion) - carry on.
                    End Try

                    Log.AddMessage("Removing leftover SafeBoot handler service.")
                    Dim installer As New Win32.ServiceInstaller
                    installer.Uninstall(serviceName)
                End If

                ' SafeBoot\Minimal + SafeBoot\Network entries (silent when absent).
                AllowSafeBootServiceToRunInSafemode(False)

                ' The copied executable: the current ProgramData location, plus the %TEMP% one
                ' used by older DDU versions so machines upgrading get cleaned up too.
                For Each leftover As String In New String() {
                    Path.Combine(Paths.AppBaseRoaming, "DDUSafeBootHandler.exe"),
                    Path.Combine(Path.GetTempPath(), "DDUSafeBootHandler.exe")}

                    Try
                        If _fileIo.ExistsFile(leftover) Then
                            File.Delete(leftover)
                            Log.AddMessage("Removed leftover SafeBoot handler executable: " & leftover)
                        End If
                    Catch ex As Exception
                        Log.AddWarningMessage("Could not remove leftover SafeBoot handler executable '" & leftover & "': " & ex.Message)
                    End Try
                Next

            Catch ex As Exception
                Log.AddException(ex, "SafeBoot handler leftover cleanup failed")
            End Try
        End Sub

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
                    Application.Log.AddException(ex, "Failed to set 'DDUSafeBootHandler' RegistryKey in 'SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal' !")
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
                    Application.Log.AddException(ex, "Failed to set 'DDUSafeBootHandler' RegistryKey in 'SYSTEM\CurrentControlSet\Control\SafeBoot\Network' !")
                End Try
                Return
            End If

            ' throwOnMissingSubKey:=False - this also runs from the startup leftover cleanup on
            ' machines that never used Safe Mode, where the keys legitimately do not exist.
            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
                    If regkey IsNot Nothing Then
                        regkey.DeleteSubKeyTree("DDUSafeBootHandler", throwOnMissingSubKey:=False)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (DDUSafeBootHandler)!")
            End Try

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
                    If regkey IsNot Nothing Then
                        regkey.DeleteSubKeyTree("DDUSafeBootHandler", throwOnMissingSubKey:=False)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Network' RegistryKey (DDUSafeBootHandler)!")
            End Try

        End Sub

        ''' <summary>
        ''' Copies DDU into its ProgramData folder and registers it as the DDUSafeBootHandler
        ''' service. Returns False when anything failed: the caller must NOT reboot into Safe
        ''' Mode then, because this service is the only safety net that works without a user
        ''' logon (forgotten password, PIN unavailable in Safe Mode).
        ''' </summary>
        Private Function InstallSafeBootService() As Boolean
            Try
                ' NOT %TEMP%: temp cleaners, AV and "Disk Cleanup" can wipe the file between
                ' the install and the reboot. The service would then fail to start, leaving a
                ' user who cannot log in stuck in Safe Mode with no way out - the exact case
                ' this service exists to prevent. ProgramData is stable and LocalSystem can
                ' read it. DDU may run portable (USB), so a copy is still required.
                Dim serviceExePath As String = Path.Combine(Paths.AppBaseRoaming, "DDUSafeBootHandler.exe")

                If Not Directory.Exists(Paths.AppBaseRoaming) Then
                    Directory.CreateDirectory(Paths.AppBaseRoaming)
                End If

                File.Copy(Paths.AppExeFile, serviceExePath, True)

                If Not _fileIo.ExistsFile(serviceExePath) Then
                    Log.AddWarningMessage("SafeBoot Handler Service: executable could not be created at " & serviceExePath)
                    Return False
                End If

                ' UseShellExecute = False: keeps CreateNoWindow effective (it is ignored when
                ' ShellExecute is used, which made an sc.exe console window flash) and lets us
                ' read the exit code. DDU already runs elevated, so "runas" was never needed.
                Dim processInfo As New ProcessStartInfo(Paths.System32 & "sc.exe",
            $"create DDUSafeBootHandler binPath= ""{serviceExePath} /service"" start= auto") With {
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardOutput = False
        }

                Using process As New Process With {
                .StartInfo = processInfo
            }
                    process.Start()

                    If Not process.WaitForExit(30000) Then
                        Try
                            process.Kill()
                        Catch
                        End Try

                        Log.AddWarningMessage("SafeBoot Handler Service: 'sc create' timed out.")
                        Return False
                    End If

                    If process.ExitCode <> 0 Then
                        Log.AddWarningMessage($"SafeBoot Handler Service: 'sc create' failed (exit code {process.ExitCode}).")
                        Return False
                    End If
                End Using

                Log.AddMessage("SafeBoot Handler Service installed successfully")
                Return True

            Catch ex As Exception
                Log.AddException(ex, "Failed to install SafeBoot Handler Service")
                Return False
            End Try
        End Function

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

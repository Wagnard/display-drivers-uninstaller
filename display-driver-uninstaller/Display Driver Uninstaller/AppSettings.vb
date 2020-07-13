Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel

Public Enum GPUVendor As Int32
	None
	Nvidia
	AMD
	Intel
End Enum

Public Enum CleanType As Int32
	None
	Audio
	GPU
End Enum

Public Enum AudioVendor As Int32
	None
	Realtek
	SoundBlaster
End Enum

Public Enum UpdateStatus As Int32
	NotChecked = 1
	NoUpdates = 2
	UpdateAvailable = 3
	[Error] = 4
	NotAllowed = 5
End Enum

Public Enum OSVersion As Int32
	''' <summary> [ 0.0 ] - Unsupported OS</summary>
	<ComponentModel.Description("0.0")>
	Unknown = 0

	''' <summary> [ 5.1 ] - Windows XP</summary>
	<ComponentModel.Description("5.1")>
	WinXP = 51

	''' <summary> [ 5.2 ] - Windows XP (x64) or Server 2003</summary>
	<ComponentModel.Description("5.2")>
	WinXPPro_Server2003 = 52

	''' <summary> [ 6.0 ] - Windows Vista or Server 2008</summary>
	<ComponentModel.Description("6.0")>
	WinVista = 60

	''' <summary> [ 6.1 ] - Windows 7 or Server 2008R2</summary>
	<ComponentModel.Description("6.1")>
	Win7 = 61

	''' <summary> [ 6.2 ] - Windows 8 or Server 2012</summary>
	<ComponentModel.Description("6.2")>
	Win8 = 62

	''' <summary> [ 6.3 ] - Windows 8.1</summary>
	<ComponentModel.Description("6.3")>
	Win81 = 63

	''' <summary> [ 6.4 / 10.0 ] - Windows 10</summary>
	<ComponentModel.Description("6.4")>
	Win10 = 64

End Enum

Public Class AppSettings
	Inherits DependencyObject

#Region "Private Fields"
	Private m_appname As DependencyProperty = RegDP("AppName", GetType(String), GetType(AppSettings), "Display Driver Uninstaller")
	Private m_appversion As DependencyProperty = RegDP("AppVersion", GetType(Version), GetType(AppSettings), New Version(0, 0, 0, 0))
	Private m_languageOptions As ObservableCollection(Of Languages.LanguageOption)
	Private m_gpuSelected As DependencyProperty = RegDP("SelectedGPU", GetType(GPUVendor), GetType(AppSettings), GPUVendor.Nvidia)
    Private m_audioSelected As DependencyProperty = RegDP("SelectedAUDIO", GetType(AudioVendor), GetType(AppSettings), AudioVendor.Realtek)
    Private m_cleanType As DependencyProperty = RegDP("SelectedType", GetType(CleanType), GetType(AppSettings), CleanType.None)
	Private m_langSelected As DependencyProperty = RegDP("SelectedLanguage", GetType(Languages.LanguageOption), GetType(AppSettings), Nothing)
	Private m_updateAvailable As DependencyProperty = RegDP("UpdateAvailable", GetType(UpdateStatus), GetType(AppSettings), UpdateStatus.NotAllowed)

	Private m_winVersion As DependencyProperty = RegDP("WinVersion", GetType(OSVersion), GetType(AppSettings), OSVersion.Unknown)
	Private m_winVersionText As DependencyProperty = RegDP("WinVersionText", GetType(String), GetType(AppSettings), "Unknown")
	Private m_winIs64 As DependencyProperty = RegDP("WinIs64", GetType(Boolean), GetType(AppSettings), False)
	Private m_processKilled As DependencyProperty = RegDP("ProcessKilled", GetType(Boolean), GetType(AppSettings), False)

	' Removals
	Private m_remMonitors As DependencyProperty = RegDP("RemoveMonitors", GetType(Boolean), GetType(AppSettings), True)

	Private m_remCrimsonCache As DependencyProperty = RegDP("RemoveCrimsonCache", GetType(Boolean), GetType(AppSettings), True)
	Private m_remAMDDirs As DependencyProperty = RegDP("RemoveAMDDirs", GetType(Boolean), GetType(AppSettings), True)
	Private m_remAudioBus As DependencyProperty = RegDP("RemoveAudioBus", GetType(Boolean), GetType(AppSettings), False)
	Private m_remAMDKMPFD As DependencyProperty = RegDP("RemoveAMDKMPFD", GetType(Boolean), GetType(AppSettings), False)

	Private m_remNvidiaDirs As DependencyProperty = RegDP("RemoveNvidiaDirs", GetType(Boolean), GetType(AppSettings), True)
	Private m_remPhysX As DependencyProperty = RegDP("RemovePhysX", GetType(Boolean), GetType(AppSettings), False)
	Private m_rem3DtvPlay As DependencyProperty = RegDP("Remove3DTVPlay", GetType(Boolean), GetType(AppSettings), True)
	Private m_remGFE As DependencyProperty = RegDP("RemoveGFE", GetType(Boolean), GetType(AppSettings), True)
	Private m_remNVCP As DependencyProperty = RegDP("RemoveNVCP", GetType(Boolean), GetType(AppSettings), True)
	Private m_remINTELCP As DependencyProperty = RegDP("RemoveINTELCP", GetType(Boolean), GetType(AppSettings), True)

	' Settings
	Private m_UseRoamingCfg As DependencyProperty = RegDP("UseRoamingConfig", GetType(Boolean), GetType(AppSettings), False)
	Private m_CheckUpdates As DependencyProperty = RegDP("CheckUpdates", GetType(Boolean), GetType(AppSettings), True)
	Private m_createRestorePoint As DependencyProperty = RegDP("CreateRestorePoint", GetType(Boolean), GetType(AppSettings), True)
	Private m_saveLogs As DependencyProperty = RegDP("SaveLogs", GetType(Boolean), GetType(AppSettings), True)
	Private m_removevulkan As DependencyProperty = RegDP("RemoveVulkan", GetType(Boolean), GetType(AppSettings), True)
	Private m_showoffer As DependencyProperty = RegDP("ShowOffer", GetType(Boolean), GetType(AppSettings), True)
    Private m_enablesafemodedialog As DependencyProperty = RegDP("EnableSafeModeDialog", GetType(Boolean), GetType(AppSettings), False)
	Private m_PreventWinUpdate As DependencyProperty = RegDP("PreventWinUpdate", GetType(Boolean), GetType(AppSettings), False)
	Private m_FirstTimeLaunch As DependencyProperty = RegDP("FirstTimeLaunch", GetType(Boolean), GetType(AppSettings), False)
	Private m_UsedBCD As DependencyProperty = RegDP("UsedBCD", GetType(Boolean), GetType(AppSettings), False)

#End Region

#Region "Public Properties"
	Public Property AppName As String ' Name of application (DDU)
		Get
			Return CStr(GetValue(m_appname))
		End Get
		Private Set(value As String)
			SetValue(m_appname, value)
		End Set
	End Property
	Public Property AppVersion As Version
		Get
			Return CType(GetValue(m_appversion), Version)
		End Get
		Private Set(value As Version)
			SetValue(m_appversion, value)
		End Set
	End Property

	Public Property WinVersion As OSVersion
		Get
			Return DirectCast(GetValue(m_winVersion), OSVersion)
		End Get
		Set(value As OSVersion)
			UpdateWinText(value)
		End Set
	End Property
	Public Property WinVersionText As String
		Get
			Return CStr(GetValue(m_winVersionText))
		End Get
		Set(value As String)
			SetValue(m_winVersionText, value)
		End Set
	End Property
	Public Property WinIs64 As Boolean
		Get
			Return CBool(GetValue(m_winIs64))
		End Get
		Set(value As Boolean)
			SetValue(m_winIs64, value)
		End Set
	End Property

	Public Property ProcessKilled As Boolean
		Get
			Return CBool(GetValue(m_processKilled))
		End Get
		Set(value As Boolean)
			SetValue(m_processKilled, value)
		End Set
	End Property

	Public ReadOnly Property LanguageOptions As ObservableCollection(Of Languages.LanguageOption)
		Get
			Return m_languageOptions
		End Get
	End Property

	Public Property SelectedGPU As GPUVendor
		Get
			Return CType(GetValue(m_gpuSelected), GPUVendor)
		End Get
		Set(value As GPUVendor)
			SetValue(m_gpuSelected, value)
		End Set
	End Property

    Public Property SelectedAUDIO As AudioVendor
        Get
            Return CType(GetValue(m_audioSelected), AudioVendor)
        End Get
        Set(value As AudioVendor)
            SetValue(m_audioSelected, value)
        End Set
    End Property

    Public Property SelectedType As CleanType
		Get
			Return CType(GetValue(m_cleanType), CleanType)
		End Get
		Set(value As CleanType)
			SetValue(m_cleanType, value)
		End Set
	End Property

	Public Property SelectedLanguage As Languages.LanguageOption
		Get
			Return CType(GetValue(m_langSelected), Languages.LanguageOption)
		End Get
		Set(value As Languages.LanguageOption)
			SetValue(m_langSelected, value)
		End Set
	End Property

	Public Property UpdateAvailable As UpdateStatus
		Get
			Return DirectCast(GetValue(m_updateAvailable), UpdateStatus)
		End Get
		Set(value As UpdateStatus)
			SetValue(m_updateAvailable, value)
		End Set
	End Property

	Public Property RemoveMonitors As Boolean
		Get
			Return CBool(GetValue(m_remMonitors))
		End Get
		Set(value As Boolean)
			SetValue(m_remMonitors, value)
		End Set
	End Property

	Public Property RemoveCrimsonCache As Boolean
		Get
			Return CBool(GetValue(m_remCrimsonCache))
		End Get
		Set(value As Boolean)
			SetValue(m_remCrimsonCache, value)
		End Set
	End Property
	Public Property RemoveAMDDirs As Boolean
		Get
			Return CBool(GetValue(m_remAMDDirs))
		End Get
		Set(value As Boolean)
			SetValue(m_remAMDDirs, value)
		End Set
	End Property
	Public Property RemoveAudioBus As Boolean
		Get
			Return CBool(GetValue(m_remAudioBus))
		End Get
		Set(value As Boolean)
			SetValue(m_remAudioBus, value)
		End Set
	End Property
	Public Property RemoveAMDKMPFD As Boolean
		Get
			Return CBool(GetValue(m_remAMDKMPFD))
		End Get
		Set(value As Boolean)
			SetValue(m_remAMDKMPFD, value)
		End Set
	End Property

	Public Property RemoveNvidiaDirs As Boolean
		Get
			Return CBool(GetValue(m_remNvidiaDirs))
		End Get
		Set(value As Boolean)
			SetValue(m_remNvidiaDirs, value)
		End Set
	End Property
	Public Property RemovePhysX As Boolean
		Get
			Return CBool(GetValue(m_remPhysX))
		End Get
		Set(value As Boolean)
			SetValue(m_remPhysX, value)
		End Set
	End Property
	Public Property Remove3DTVPlay As Boolean
		Get
			Return CBool(GetValue(m_rem3DtvPlay))
		End Get
		Set(value As Boolean)
			SetValue(m_rem3DtvPlay, value)
		End Set
	End Property
	Public Property RemoveGFE As Boolean
		Get
			Return CBool(GetValue(m_remGFE))
		End Get
		Set(value As Boolean)
			SetValue(m_remGFE, value)
		End Set
	End Property

	Public Property RemoveNVCP As Boolean
		Get
			Return CBool(GetValue(m_remNVCP))
		End Get
		Set(value As Boolean)
			SetValue(m_remNVCP, value)
		End Set
	End Property

	Public Property RemoveINTELCP As Boolean
		Get
			Return CBool(GetValue(m_remINTELCP))
		End Get
		Set(value As Boolean)
			SetValue(m_remINTELCP, value)
		End Set
	End Property

	Public Property UseRoamingConfig As Boolean
		Get
			Return CBool(GetValue(m_UseRoamingCfg))
		End Get
		Set(value As Boolean)
			SetValue(m_UseRoamingCfg, value)
		End Set
	End Property
	Public Property CheckUpdates As Boolean
		Get
			Return CBool(GetValue(m_CheckUpdates))
		End Get
		Set(value As Boolean)
			SetValue(m_CheckUpdates, value)
		End Set
	End Property
	Public Property CreateRestorePoint As Boolean
		Get
			Return CBool(GetValue(m_createRestorePoint))
		End Get
		Set(value As Boolean)
			SetValue(m_createRestorePoint, value)
		End Set
	End Property
	Public Property SaveLogs As Boolean
		Get
			Return CBool(GetValue(m_saveLogs))
		End Get
		Set(value As Boolean)
			SetValue(m_saveLogs, value)
		End Set
	End Property
	Public Property RemoveVulkan As Boolean
		Get
			Return CBool(GetValue(m_removevulkan))
		End Get
		Set(value As Boolean)
			SetValue(m_removevulkan, value)
		End Set
	End Property

	Public Property ShowOffer As Boolean
		Get
			Return CBool(GetValue(m_showoffer))
		End Get
		Set(value As Boolean)
			SetValue(m_showoffer, value)
		End Set
	End Property

    Public Property EnableSafeModeDialog As Boolean
        Get
            Return CBool(GetValue(m_enablesafemodedialog))
        End Get
        Set(value As Boolean)
            SetValue(m_enablesafemodedialog, value)
        End Set
    End Property

	Public Property PreventWinUpdate As Boolean
		Get
			Return CBool(GetValue(m_PreventWinUpdate))
		End Get
		Set(value As Boolean)
			SetValue(m_PreventWinUpdate, value)
		End Set
	End Property

	Public Property FirstTimeLaunch As Boolean
		Get
			Return CBool(GetValue(m_FirstTimeLaunch))
		End Get
		Set(value As Boolean)
			SetValue(m_FirstTimeLaunch, value)
		End Set
	End Property

	Public Property UsedBCD As Boolean
		Get
			Return CBool(GetValue(m_UsedBCD))
		End Get
		Set(value As Boolean)
			SetValue(m_UsedBCD, value)
		End Set
	End Property

#End Region

	Private Sub UpdateWinText(ByVal version As OSVersion)
		If WinVersion = version Then
			Return
		End If

		' https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832%28v=vs.85%29.aspx

		Select Case version
			Case OSVersion.WinXP : Application.Settings.WinVersionText = "Windows XP"
			Case OSVersion.WinXPPro_Server2003 : Application.Settings.WinVersionText = "Windows XP (x64) or Server 2003"
			Case OSVersion.WinVista : Application.Settings.WinVersionText = "Windows Vista or Server 2008"
			Case OSVersion.Win7 : Application.Settings.WinVersionText = "Windows 7 or Server 2008R2"
			Case OSVersion.Win8 : Application.Settings.WinVersionText = "Windows 8 or Server 2012"
			Case OSVersion.Win81
				Application.Settings.WinVersionText = "Windows 8.1"

				Using regkey As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
					If regkey IsNot Nothing Then
						Dim regValue As String = regkey.GetValue("CurrentMajorVersionNumber", String.Empty).ToString()

						If Not IsNullOrWhitespace(regValue) AndAlso regValue.Equals("10") Then
							version = OSVersion.Win10
							Application.Settings.WinVersionText = "Windows 10"
						End If
					End If
				End Using

			Case OSVersion.Win10
				Application.Settings.WinVersionText = "Windows 10"

			Case Else
				Application.Settings.WinVersionText = "Unsupported OS"
				Application.Log.AddWarningMessage("Unsupported OS.")
		End Select

		SetValue(m_winVersion, version)
	End Sub

	Public Sub New()
		m_languageOptions = New ObservableCollection(Of Languages.LanguageOption)()

		Dim asseemblyName As AssemblyName = Assembly.GetExecutingAssembly().GetName()

		AppName = asseemblyName.Name
		AppVersion = asseemblyName.Version
	End Sub

	Public Sub Save()
		Dim roamingFile As String = Path.Combine(Application.Paths.Roaming, String.Format("{0}\Settings.xml", AppName.Replace(" ", "")))

		If UseRoamingConfig Then
			Save(roamingFile)
		Else
			If File.Exists(roamingFile) Then
				File.Delete(roamingFile) ' avoid opening from roaming file
			End If

			Save(Path.Combine(Application.Paths.Settings, "Settings.xml"))
		End If
	End Sub

	Public Sub Load()
		If Load(Path.Combine(Application.Paths.Roaming, String.Format("{0}\Settings.xml", AppName.Replace(" ", "")))) Then
			UseRoamingConfig = True

		ElseIf Load(Path.Combine(Application.Paths.Settings, "Settings.xml")) Then

			UseRoamingConfig = False

		Else 'Fisrt time launch, no config found
			FirstTimeLaunch = True
		End If
	End Sub

	Private Sub Save(ByVal fileName As String)
		If String.IsNullOrEmpty(fileName) Then
			Return
		End If

		Try
			Dim dir As String = Path.GetDirectoryName(fileName)

			If Not Directory.Exists(dir) Then
				Directory.CreateDirectory(dir)
			End If

			If File.Exists(fileName) Then
				File.Delete(fileName)
			End If

			Using fs As Stream = File.Create(fileName, 4096, FileOptions.WriteThrough)
				Using sw As New StreamWriter(fs, System.Text.Encoding.UTF8)
					Dim settings As New XmlWriterSettings With
					 {
					   .Encoding = sw.Encoding,
					   .Indent = True,
					   .IndentChars = vbTab,
					   .ConformanceLevel = ConformanceLevel.Document
					 }

					Dim writer As XmlWriter = XmlWriter.Create(sw, settings)

					With writer
						.WriteStartDocument()
						.WriteStartElement(AppName.Replace(" ", ""))

						Dim v As Version = AppVersion

						.WriteAttributeString("Version", String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision))
						.WriteStartElement("Settings")

						.WriteElementString("SelectedLanguage", If(SelectedLanguage IsNot Nothing, SelectedLanguage.ISOLanguage, Languages.DefaultEngISO))

						.WriteElementString("RemoveMonitors", RemoveMonitors.ToString())
						.WriteElementString("RemoveCrimsonCache", RemoveCrimsonCache.ToString())
						.WriteElementString("RemoveAMDDirs", RemoveAMDDirs.ToString())
						.WriteElementString("RemoveAudioBus", RemoveAudioBus.ToString())
						.WriteElementString("RemoveAMDKMPFD", RemoveAMDKMPFD.ToString())
						.WriteElementString("RemoveNvidiaDirs", RemoveNvidiaDirs.ToString())
						.WriteElementString("RemovePhysX", RemovePhysX.ToString())
						.WriteElementString("Remove3DTVPlay", Remove3DTVPlay.ToString())
						.WriteElementString("RemoveGFE", RemoveGFE.ToString())
						.WriteElementString("RemoveNVCP", RemoveNVCP.ToString())
						.WriteElementString("RemoveINTELCP", RemoveINTELCP.ToString())
						.WriteElementString("UseRoamingConfig", UseRoamingConfig.ToString())
						.WriteElementString("CheckUpdates", CheckUpdates.ToString())
						.WriteElementString("CreateRestorePoint", CreateRestorePoint.ToString())
						.WriteElementString("SaveLogs", SaveLogs.ToString())
						.WriteElementString("RemoveVulkan", RemoveVulkan.ToString())
						.WriteElementString("ShowOffer", ShowOffer.ToString())
						.WriteElementString("EnableSafeModeDialog", EnableSafeModeDialog.ToString())
						.WriteElementString("PreventWinUpdate", PreventWinUpdate.ToString())
						.WriteElementString("UsedBCD", UsedBCD.ToString())


						.WriteEndElement()

						.WriteEndElement()
						.WriteEndDocument()
						.Close()
					End With

					sw.Flush()
					sw.Close()
				End Using
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Function Load(ByVal fileName As String) As Boolean
        If String.IsNullOrEmpty(fileName) OrElse Not File.Exists(fileName) Then
            Return False
        End If

        Try
			Using fs As Stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None)
				If fs.Length <= 3L Then
					Return False
				End If

				Using sr As New StreamReader(fs, System.Text.Encoding.UTF8, True)
					Dim settings As New XmlReaderSettings With
					 {
					   .IgnoreComments = True,
					   .IgnoreWhitespace = True,
					   .ConformanceLevel = ConformanceLevel.Document
					  }

					Dim reader As XmlReader = XmlReader.Create(sr, settings)

					Do While reader.Read()
						If reader.NodeType = XmlNodeType.Element Then
							Exit Do
						End If
					Loop

					If reader.EOF Then
						Return False
					End If

					If reader.NodeType <> XmlNodeType.Element Or Not reader.Name.Equals(AppName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) Or Not reader.HasAttributes Then
						Throw New InvalidDataException("Log's format is invalid!" & vbCrLf & String.Format("Root node doesn't match '{0}'", AppName.Replace(" ", "")) & vbCrLf & "Or missing attributes")
					End If

					Dim verStr As String() = Nothing
					Do While reader.MoveToNextAttribute()
						If Not String.IsNullOrEmpty(reader.Name) Then
							If reader.Name.Equals("Version", StringComparison.OrdinalIgnoreCase) Then
								verStr = reader.Value.Split(New String() {"."}, StringSplitOptions.None)
							End If
						End If
					Loop

					If verStr Is Nothing Or verStr.Length <> 4 Then
						Throw New InvalidDataException("Log's format is invalid!" & vbCrLf & "Version format doesn't match or missing")
					End If

					Dim vMajor, vMinor, vBuild, vRevision As New Int32
					Int32.TryParse(verStr(0), vMajor)
					Int32.TryParse(verStr(1), vMinor)
					Int32.TryParse(verStr(2), vBuild)
					Int32.TryParse(verStr(3), vRevision)
					Dim ver As Version = New Version(vMajor, vMinor, vBuild, vRevision)

					Dim name As String = ""
					Dim props As New Dictionary(Of String, String)

					reader.Read()

					Do
						name = reader.Name

						If reader.NodeType = XmlNodeType.Element AndAlso reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase) Then
							reader.Read()

							Do
								If reader.NodeType = XmlNodeType.Element Then
									props.Add(reader.Name, reader.ReadElementContentAsString)
								Else
									reader.Read()
								End If
							Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase))

						End If
					Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase))

					For Each KvP As KeyValuePair(Of String, String) In props
						Select Case KvP.Key.ToLower()
							Case "selectedlanguage"
								For Each langOption As Languages.LanguageOption In Application.Settings.LanguageOptions
									If langOption.ISOLanguage.Equals(KvP.Value, StringComparison.OrdinalIgnoreCase) Then
										SelectedLanguage = langOption
										Exit For
									End If
								Next
							Case "removemonitors"
								RemoveMonitors = Boolean.Parse(KvP.Value)

							Case "removecrimsoncache"
								RemoveCrimsonCache = Boolean.Parse(KvP.Value)

							Case "removeamddirs"
								RemoveAMDDirs = Boolean.Parse(KvP.Value)

							Case "removeaudiobus"
								RemoveAudioBus = Boolean.Parse(KvP.Value)

							Case "removeamdkmpfd"
								RemoveAMDKMPFD = Boolean.Parse(KvP.Value)

							Case "removenvidiadirs"
								RemoveNvidiaDirs = Boolean.Parse(KvP.Value)

							Case "removephysx"
								RemovePhysX = Boolean.Parse(KvP.Value)

							Case "remove3dtvplay"
								Remove3DTVPlay = Boolean.Parse(KvP.Value)

							Case "removegfe"
								RemoveGFE = Boolean.Parse(KvP.Value)

							Case "removenvcp"
								RemoveNVCP = Boolean.Parse(KvP.Value)

							Case "removeintelcp"
								RemoveINTELCP = Boolean.Parse(KvP.Value)

							Case "useroamingconfig"
								UseRoamingConfig = Boolean.Parse(KvP.Value)

							Case "checkupdates"
								CheckUpdates = Boolean.Parse(KvP.Value)

							Case "createrestorepoint"
								CreateRestorePoint = Boolean.Parse(KvP.Value)

							Case "savelogs"
								SaveLogs = Boolean.Parse(KvP.Value)

							Case "removevulkan"
								RemoveVulkan = Boolean.Parse(KvP.Value)

							Case "showoffer"
								ShowOffer = Boolean.Parse(KvP.Value)

							Case "enablesafemodedialog"
                                EnableSafeModeDialog = Boolean.Parse(KvP.Value)

                            Case "preventwinupdate"
								PreventWinUpdate = Boolean.Parse(KvP.Value)

							Case "usedbcd"
								UsedBCD = Boolean.Parse(KvP.Value)

						End Select
					Next

					reader.Close()
					sr.Close()
				End Using
			End Using

			Return True
		Catch ex As Exception
			Application.Log.AddException(ex)
			Return False
		End Try
	End Function

End Class

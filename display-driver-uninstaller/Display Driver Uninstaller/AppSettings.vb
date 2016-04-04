Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel

Public Enum GPUVendor
	Nvidia
	AMD
	Intel
End Enum

Public Class AppSettings
	Inherits DependencyObject

#Region "Private Fields"
	Private m_appname As DependencyProperty = Reg("AppName", GetType(String), GetType(AppSettings), "Display Driver Uninstaller")
	Private m_appversion As DependencyProperty = Reg("AppVersion", GetType(Version), GetType(AppSettings), New Version(0, 0, 0, 0))
	Private m_languageOptions As ObservableCollection(Of Languages.LanguageOption)
	Private m_gpuSelected As DependencyProperty = Reg("SelectedGPU", GetType(GPUVendor), GetType(AppSettings), GPUVendor.Nvidia)
	Private m_langSelected As DependencyProperty = Reg("SelectedLanguage", GetType(Languages.LanguageOption), GetType(AppSettings), Nothing)

	' Removals
    Private m_remMonitors As DependencyProperty = Reg("RemoveMonitors", GetType(Boolean), GetType(AppSettings), True)

    Private m_remCrimsonCache As DependencyProperty = Reg("RemoveCrimsonCache", GetType(Boolean), GetType(AppSettings), True)
	Private m_remAMDDirs As DependencyProperty = Reg("RemoveAMDDirs", GetType(Boolean), GetType(AppSettings), False)
    Private m_remAMDAudioBus As DependencyProperty = Reg("RemoveAMDAudioBus", GetType(Boolean), GetType(AppSettings), True)
	Private m_remAMDKMPFD As DependencyProperty = Reg("RemoveAMDKMPFD", GetType(Boolean), GetType(AppSettings), False)

	Private m_remNvidiaDirs As DependencyProperty = Reg("RemoveNvidiaDirs", GetType(Boolean), GetType(AppSettings), False)
    Private m_remPhysX As DependencyProperty = Reg("RemovePhysX", GetType(Boolean), GetType(AppSettings), True)
    Private m_rem3DtvPlay As DependencyProperty = Reg("Remove3DTVPlay", GetType(Boolean), GetType(AppSettings), True)
    Private m_remGFE As DependencyProperty = Reg("RemoveGFE", GetType(Boolean), GetType(AppSettings), True)

	' Settings
	Private m_showSafeModeMsg As DependencyProperty = Reg("ShowSafeModeMsg", GetType(Boolean), GetType(AppSettings), True)
	Private m_UseRoamingCfg As DependencyProperty = Reg("UseRoamingConfig", GetType(Boolean), GetType(AppSettings), False)
	Private m_DontCheckUpdates As DependencyProperty = Reg("DontCheckUpdates", GetType(Boolean), GetType(AppSettings), False)
	Private m_createRestorePoint As DependencyProperty = Reg("CreateRestorePoint", GetType(Boolean), GetType(AppSettings), True)
	Private m_saveLogs As DependencyProperty = Reg("SaveLogs", GetType(Boolean), GetType(AppSettings), True)
	Private m_removevulkan As DependencyProperty = Reg("RemoveVulkan", GetType(Boolean), GetType(AppSettings), False)

	' Visit links
	Private m_goodsite As DependencyProperty = Reg("GoodSite", GetType(Boolean), GetType(AppSettings), False)

	Private m_visitDonate As DependencyProperty = Reg("VisitDonate", GetType(Boolean), GetType(AppSettings), False)
	Private m_visitSVN As DependencyProperty = Reg("VisitSVN", GetType(Boolean), GetType(AppSettings), False)
	Private m_visitGuru3DNvidia As DependencyProperty = Reg("VisitGuru3DNvidia", GetType(Boolean), GetType(AppSettings), False)
	Private m_visitGuru3DAMD As DependencyProperty = Reg("VisitGuru3DAMD", GetType(Boolean), GetType(AppSettings), False)
	Private m_visitDDUHome As DependencyProperty = Reg("VisitDDUHome", GetType(Boolean), GetType(AppSettings), False)
	Private m_visitGeforce As DependencyProperty = Reg("VisitGeforce", GetType(Boolean), GetType(AppSettings), False)

	Private m_arguments As DependencyProperty = Reg("Arguments", GetType(String), GetType(AppSettings), String.Empty)
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
	Public Property SelectedLanguage As Languages.LanguageOption
		Get
			Return CType(GetValue(m_langSelected), Languages.LanguageOption)
		End Get
		Set(value As Languages.LanguageOption)
			SetValue(m_langSelected, value)
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
	Public Property RemoveAMDAudioBus As Boolean
		Get
			Return CBool(GetValue(m_remAMDAudioBus))
		End Get
		Set(value As Boolean)
			SetValue(m_remAMDAudioBus, value)
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

	Public Property ShowSafeModeMsg As Boolean
		Get
			Return CBool(GetValue(m_showSafeModeMsg))
		End Get
		Set(value As Boolean)
			SetValue(m_showSafeModeMsg, value)
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
	Public Property DontCheckUpdates As Boolean
		Get
			Return CBool(GetValue(m_DontCheckUpdates))
		End Get
		Set(value As Boolean)
			SetValue(m_DontCheckUpdates, value)
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

	Public Property GoodSite As Boolean
		Get
			Return CBool(GetValue(m_goodsite))
		End Get
		Set(value As Boolean)
			SetValue(m_goodsite, value)
		End Set
	End Property

	Public Property VisitDonate As Boolean
		Get
			Return CBool(GetValue(m_visitDonate))
		End Get
		Set(value As Boolean)
			SetValue(m_visitDonate, value)
		End Set
	End Property
	Public Property VisitSVN As Boolean
		Get
			Return CBool(GetValue(m_visitSVN))
		End Get
		Set(value As Boolean)
			SetValue(m_visitSVN, value)
		End Set
	End Property
	Public Property VisitGuru3DNvidia As Boolean
		Get
			Return CBool(GetValue(m_visitGuru3DNvidia))
		End Get
		Set(value As Boolean)
			SetValue(m_visitGuru3DNvidia, value)
		End Set
	End Property
	Public Property VisitGuru3DAMD As Boolean
		Get
			Return CBool(GetValue(m_visitGuru3DAMD))
		End Get
		Set(value As Boolean)
			SetValue(m_visitGuru3DAMD, value)
		End Set
	End Property
	Public Property VisitDDUHome As Boolean
		Get
			Return CBool(GetValue(m_visitDDUHome))
		End Get
		Set(value As Boolean)
			SetValue(m_visitDDUHome, value)
		End Set
	End Property
	Public Property VisitGeforce As Boolean
		Get
			Return CBool(GetValue(m_visitGeforce))
		End Get
		Set(value As Boolean)
			SetValue(m_visitGeforce, value)
		End Set
	End Property
	Public Property Arguments As String
		Get
			Return CStr(GetValue(m_arguments))
		End Get
		Set(value As String)
			SetValue(m_arguments, value)
		End Set
	End Property

#End Region

	Friend Shared Function Reg(ByVal s As String, ByVal t As Type, ByVal c As Type, ByVal m As Object) As DependencyProperty
		' Register values for 'Binding' (just shorthand, Don't need to undestand)
		If TypeOf (m) Is FrameworkPropertyMetadata Then
			Return DependencyProperty.Register(s, t, c, CType(m, FrameworkPropertyMetadata))
		Else
			Return DependencyProperty.Register(s, t, c, New PropertyMetadata(m))
		End If
	End Function

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
		Else
			Load(Path.Combine(Application.Paths.Settings, "Settings.xml"))
			UseRoamingConfig = False
		End If

		Dim args As String() = Environment.GetCommandLineArgs()

		If args.Length > 1 Then
			Arguments = String.Join(" ", args, 1, args.Length - 1)

			For i As Int32 = 1 To args.Length - 1
				If StrContainsAny(args(i), True, "donate") Then
					VisitDonate = True

				ElseIf StrContainsAny(args(i), True, "svn") Then
					VisitSVN = True

				ElseIf StrContainsAny(args(i), True, "guru3dnvidia") Then
					VisitGuru3DNvidia = True

				ElseIf StrContainsAny(args(i), True, "guru3damd") Then
					VisitGuru3DAMD = True

				ElseIf StrContainsAny(args(i), True, "dduhome") Then
					VisitDDUHome = True

				ElseIf StrContainsAny(args(i), True, "geforce") Then
					VisitGeforce = True
				End If
			Next
		Else
			Arguments = String.Empty
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

						.WriteElementString("SelectedLanguage", SelectedLanguage.ISOLanguage)

						.WriteElementString("RemoveMonitors", RemoveMonitors.ToString())
						.WriteElementString("RemoveCrimsonCache", RemoveCrimsonCache.ToString())
						.WriteElementString("RemoveAMDDirs", RemoveAMDDirs.ToString())
						.WriteElementString("RemoveAMDAudioBus", RemoveAMDAudioBus.ToString())
						.WriteElementString("RemoveAMDKMPFD", RemoveAMDKMPFD.ToString())
						.WriteElementString("RemoveNvidiaDirs", RemoveNvidiaDirs.ToString())
						.WriteElementString("RemovePhysX", RemovePhysX.ToString())
						.WriteElementString("Remove3DTVPlay", Remove3DTVPlay.ToString())
						.WriteElementString("RemoveGFE", RemoveGFE.ToString())
						.WriteElementString("ShowSafeModeMsg", ShowSafeModeMsg.ToString())
						.WriteElementString("UseRoamingConfig", UseRoamingConfig.ToString())
						.WriteElementString("DontCheckUpdates", DontCheckUpdates.ToString())
						.WriteElementString("CreateRestorePoint", CreateRestorePoint.ToString())
						.WriteElementString("SaveLogs", SaveLogs.ToString())
						.WriteElementString("RemoveVulkan", SaveLogs.ToString())
                        .WriteElementString("GoodSite", GoodSite.ToString())

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
								For Each langOption As Languages.LanguageOption In LanguageOptions
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

							Case "removeamdaudiobus"
								RemoveAMDAudioBus = Boolean.Parse(KvP.Value)

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

							Case "showsafemodemsg"
								ShowSafeModeMsg = Boolean.Parse(KvP.Value)

							Case "useroamingconfig"
								UseRoamingConfig = Boolean.Parse(KvP.Value)

							Case "dontcheckupdates"
								DontCheckUpdates = Boolean.Parse(KvP.Value)

							Case "createrestorepoint"
								CreateRestorePoint = Boolean.Parse(KvP.Value)

							Case "savelogs"
								SaveLogs = Boolean.Parse(KvP.Value)

							Case "removevulkan"
								RemoveVulkan = Boolean.Parse(KvP.Value)

                            Case "goodsite"
                                GoodSite = Boolean.Parse(KvP.Value)
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

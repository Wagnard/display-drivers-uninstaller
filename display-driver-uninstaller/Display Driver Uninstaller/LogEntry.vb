Imports System.Collections.Specialized
Imports System.Xml
Imports System.ComponentModel

Public Enum LogType
	[Event]
	[Warning]
	[Error]
End Enum

Public Class KvP
	Public Shared Empty As KvP = New KvP(Nothing, "")
	Public ReadOnly Property HasKey As Boolean
		Get
			Return String.IsNullOrEmpty(Key) = False
		End Get
	End Property
	Public ReadOnly Property HasValue As Boolean
		Get
			Return String.IsNullOrEmpty(Value) = False
		End Get
	End Property
	Public ReadOnly Property HasAnyValue As Boolean
		Get
			Return HasKey OrElse HasValue
		End Get
	End Property
	Public Property [Key] As String
	Public Property [Value] As String
	Public Property Separator As String

	Public Sub New(ByVal separator As String, ByVal key As String, ByVal value As String)
		Me.Key = key
		Me.Value = value
		Me.Separator = separator
	End Sub

	Public Sub New(ByVal separator As String, ByVal value As String)
		Me.Key = Nothing
		Me.Value = value
		Me.Separator = Separator
	End Sub
End Class

Public Class LogEntry
	Implements INotifyPropertyChanged
	Implements IDisposable

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private _disposed As Boolean
	Private m_exData As Dictionary(Of String, String)
	Private m_values As List(Of KvP)
	Private m_separator As String
	Private m_time As DateTime
	Private m_type As LogType
	Private m_isSelected, m_hasValues, m_hasException, m_hasAnyData As Boolean
	Private m_message As String

	Public Property IsSelected As Boolean
		Get
			Return m_isSelected
		End Get
		Set(value As Boolean)
			m_isSelected = value
			OnPropertyChanged("IsSelected")
		End Set
	End Property
	Public Property Separator As String
		Get
			Return m_separator
		End Get
		Set(value As String)
			If m_separator = value Then
				Return
			End If

			m_separator = value
			OnPropertyChanged("Separator")
		End Set
	End Property
	Public Property Type As LogType
		Get
			Return m_type
		End Get
		Set(value As LogType)
			If m_type = value Then
				Return
			End If

			m_type = value
			OnPropertyChanged("Type")
		End Set
	End Property
	Public Property Time As DateTime
		Get
			Return m_time
		End Get
		Set(value As DateTime)
			If m_time = value Then
				Return
			End If

			m_time = value
			OnPropertyChanged("Type")
		End Set
	End Property
	Public Property Message As String
		Get
			Return m_message
		End Get
		Set(value As String)
			If m_message = value Then
				Return
			End If

			m_message = value
			OnPropertyChanged("Message")
		End Set
	End Property

	Public Property ExceptionData As Dictionary(Of String, String)
		Get
			Return m_exData
		End Get
		Set(value As Dictionary(Of String, String))
			m_exData = value
			OnPropertyChanged("ExceptionData")
		End Set
	End Property
	Public Property Values As List(Of KvP)
		Get
			Return m_values
		End Get
		Set(value As List(Of KvP))
			m_values = value
			OnPropertyChanged("Values")
		End Set
	End Property
	Public Property HasException As Boolean
		Get
			Return m_hasException
		End Get
		Set(value As Boolean)
			m_hasException = value
			OnPropertyChanged("HasException")
		End Set
	End Property
	Public Property HasAnyData As Boolean
		Get
			Return m_hasAnyData
		End Get
		Set(value As Boolean)
			m_hasAnyData = value
			OnPropertyChanged("HasAnyData")
		End Set
	End Property
	Public Property HasValues As Boolean
		Get
			Return m_hasValues
		End Get
		Set(value As Boolean)
			m_hasValues = value
			OnPropertyChanged("HasValues")
		End Set
	End Property

	Public Property ID As Int64 = 0

	''' <summary>DO NOT USE unless on STA Thread (MainThread)</summary>
	Friend Sub New(Optional ByRef Ex As Exception = Nothing)
		Values = New List(Of KvP)
		ExceptionData = New Dictionary(Of String, String)

		Separator = " : "
		Time = DateTime.Now
		Type = LogType.Event
		IsSelected = False

		If Ex IsNot Nothing Then
			AddException(Ex)
		End If
	End Sub

	Public Sub Add(ByVal value As KvP)
		Values.Add(value)
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub Add(ByVal value As String)
		Values.Add(New KvP(m_separator, value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub Add(ByVal key As String, ByVal value As String)
		Values.Add(New KvP(m_separator, key, value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub AddDevices(ByVal extendedDetails As Boolean, ByVal ParamArray devices As Win32.SetupAPI.Device())
		For Each d As Win32.SetupAPI.Device In devices
			Add("Description", If(Not IsNullOrWhitespace(d.Description), d.Description, "-"))
			Add("FriendlyName", If(Not IsNullOrWhitespace(d.FriendlyName), d.FriendlyName, "-"))
			Add("ClassName", If(Not IsNullOrWhitespace(d.ClassName), d.ClassName, "-"))
			Add(KvP.Empty)
			Add("DeviceID", If(Not IsNullOrWhitespace(d.DeviceID), d.DeviceID, "-"))
			Add("DevInst", d.devInst.ToString())
			Add("InstallState", If(Not IsNullOrWhitespace(d.InstallStateStr), d.InstallStateStr, "-"))
			Add(KvP.Empty)
			Add("SiblingDevices", If(d.SiblingDevices Is Nothing, "0", d.SiblingDevices.Length.ToString()))
			Add(KvP.Empty)

			If d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
				Add("HardwareIDs", String.Join(Environment.NewLine, d.HardwareIDs))
			Else : Add("HardwareIDs", "<empty>")
			End If

			Values.Add(KvP.Empty)


			If d.CompatibleIDs IsNot Nothing AndAlso d.CompatibleIDs.Length > 0 Then
				Add("CompatibleIDs", String.Join(Environment.NewLine, d.CompatibleIDs))
			Else : Add("CompatibleIDs", "<empty>")
			End If

			Values.Add(KvP.Empty)

			Add("DevProblem", If(Not IsNullOrWhitespace(d.DevProblemStr), d.DevProblemStr, "-"))

			Values.Add(KvP.Empty)

			If d.DevStatusStr IsNot Nothing AndAlso d.DevStatusStr.Length > 0 Then
				Add("DevStatus", String.Join(Environment.NewLine, d.DevStatusStr))
			Else : Add("DevStatus", "<empty>")
			End If

			Values.Add(KvP.Empty)

			Add("RebootRequired", If(d.RebootRequired, "Yes", "No"))

			Values.Add(KvP.Empty)

			If extendedDetails Then
				If d.InstallFlagsStr IsNot Nothing AndAlso d.InstallFlagsStr.Length > 0 Then
					Add("InstallFlags", String.Join(Environment.NewLine, d.InstallFlagsStr))
				Else : Add("InstallFlags", "<empty>")
				End If

				Values.Add(KvP.Empty)

				If d.ConfigFlagsStr IsNot Nothing AndAlso d.ConfigFlagsStr.Length > 0 Then
					Add("ConfigFlags", String.Join(Environment.NewLine, d.ConfigFlagsStr))
				Else : Add("ConfigFlags", "<empty>")
				End If

				Values.Add(KvP.Empty)

				If d.CapabilitiesStr IsNot Nothing AndAlso d.CapabilitiesStr.Length > 0 Then
					Add("Capabilities", String.Join(Environment.NewLine, d.CapabilitiesStr))
				Else : Add("Capabilities", "<empty>")
				End If

				Values.Add(KvP.Empty)
			End If

			If d.OemInfs IsNot Nothing AndAlso d.OemInfs.Length > 0 Then
				Dim oems(d.OemInfs.Length - 1) As String
				Dim p As Int32 = 0

				For Each oem As Inf In d.OemInfs
					oems(p) = oem.FileName
					p += 1
				Next

				Add("OemInfs", String.Join(Environment.NewLine, oems))
			Else : Add("OemInfs", "<empty>")
			End If

			Values.Add(KvP.Empty)

			If extendedDetails Then
				If d.DriverInfo IsNot Nothing AndAlso d.DriverInfo.Length > 0 Then
					Add("Driver Details")

					For Each drvInfo As Win32.SetupAPI.DriverInfo In d.DriverInfo
						Add("> Description", If(Not IsNullOrWhitespace(drvInfo.Description), drvInfo.Description, "-"))
						Add("> Manufacturer", If(Not IsNullOrWhitespace(drvInfo.MfgName), drvInfo.MfgName, "-"))
						Add("> Provider", If(Not IsNullOrWhitespace(drvInfo.ProviderName), drvInfo.ProviderName, "-"))
						Add("> DriverDate", drvInfo.DriverDate.ToString())
						Add("> DriverVersion", If(Not IsNullOrWhitespace(drvInfo.DriverVersion), drvInfo.DriverVersion, "-"))

						If drvInfo.InfFile IsNot Nothing Then
							Add("> InfFile", If(Not IsNullOrWhitespace(drvInfo.InfFile.FileName), drvInfo.InfFile.FileName, "-"))
							Add(">>   InstallDate", drvInfo.InfFile.InstallDate.ToShortDateString())
							Add(">>   Class", If(Not IsNullOrWhitespace(drvInfo.InfFile.Class), drvInfo.InfFile.Class, "-"))
							Add(">>   Provider", If(Not IsNullOrWhitespace(drvInfo.InfFile.Provider), drvInfo.InfFile.Provider, "-"))
						End If

						Add(">")
					Next
				Else
					Add("<No Driver Details>")
				End If
			End If

			Add("---")
			Add(KvP.Empty)
		Next

		OnPropertyChanged("Values")
	End Sub

	Public Sub AddException(ByRef ex As Exception, Optional ByVal overrideMessage As Boolean = True)
		Type = LogType.Error
		Time = DateTime.Now

		m_exData.Clear()

		If TypeOf (ex) Is Win32Exception Then
			Dim win32Ex As Win32Exception = TryCast(ex, Win32Exception)

			If win32Ex IsNot Nothing Then
				Dim errCode As UInt32 = Win32.GetUInt32(win32Ex.NativeErrorCode)
				Dim msg As String

				If Not StrContainsAny(win32Ex.Message, True, "Unknown error") Then
					msg = win32Ex.Message
				Else : msg = Win32.GetErrorEnum(errCode)
				End If

				m_exData.Add("Win32_Message", msg)
				m_exData.Add("Win32_ErrorName", Win32.GetErrorEnum(errCode))
				m_exData.Add("Win32_ErrorCode", String.Format("{0} (0x{1:X})", win32Ex.NativeErrorCode.ToString(), errCode))

				If overrideMessage OrElse IsNullOrWhitespace(Message) Then
					Message = msg.Trim()
				End If
			End If
		ElseIf TypeOf (ex) Is Runtime.InteropServices.COMException Then
			Dim comEx As Runtime.InteropServices.COMException = TryCast(ex, Runtime.InteropServices.COMException)

			If comEx IsNot Nothing Then
				Dim errCode As UInt32 = Win32.GetUInt32(comEx.ErrorCode)

				m_exData.Add("COM_Message", comEx.Message)
				m_exData.Add("COM_ErrorName", Win32.GetErrorEnum(errCode))
				m_exData.Add("COM_ErrorCode", String.Format("{0} (0x{1:X})", comEx.ErrorCode.ToString(), errCode))

				If overrideMessage OrElse IsNullOrWhitespace(Message) Then
					Message = comEx.Message.Trim()
				End If
			End If
		Else
			If ex IsNot Nothing Then
				If Not String.IsNullOrEmpty(ex.Message) Then m_exData.Add("Message", ex.Message)

				If overrideMessage OrElse IsNullOrWhitespace(Message) Then
					Message = ex.Message.Trim()
				End If
			End If
		End If

		If ex.TargetSite IsNot Nothing AndAlso Not String.IsNullOrEmpty(ex.TargetSite.Name) Then m_exData.Add("TargetSite", ex.TargetSite.Name)
		If Not String.IsNullOrEmpty(ex.Source) Then m_exData.Add("Source", ex.Source)
		If Not String.IsNullOrEmpty(ex.StackTrace) Then m_exData.Add("StackTrace", ex.StackTrace)

		HasAnyData = True
		HasException = True
	End Sub

	Protected Overloads Sub OnPropertyChanged(ByVal name As String)
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
	End Sub

	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me._disposed Then
			If disposing Then
				If m_exData IsNot Nothing Then
					m_exData.Clear()
					m_exData = Nothing
				End If

				If m_values IsNot Nothing Then
					m_values.Clear()
					m_values = Nothing
				End If
			End If
		End If

		Me._disposed = True
	End Sub

	Public Sub Dispose() Implements IDisposable.Dispose
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub

End Class
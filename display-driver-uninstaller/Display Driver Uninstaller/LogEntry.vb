Imports System.Collections.Specialized
Imports System.Xml
Imports System.ComponentModel

Public Enum LogType
	[Event]
	[Warning]
	[Error]
End Enum

Public Class KvP
	Public Shared Empty As KvP = New KvP(vbTab)
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
			Return HasKey Or HasValue
		End Get
	End Property
	Public Property [Key] As String
	Public Property [Value] As String

	Public Sub New(ByRef key As String, ByRef value As String)
		Me.Key = key
		Me.Value = value
	End Sub

	Public Sub New(ByRef value As String)
		Me.Key = Nothing
		Me.Value = value
	End Sub
End Class

Public Class LogEntry
	Implements INotifyPropertyChanged
	Implements IDisposable

	Public Shared Function Create() As LogEntry
		Return Application.Log.CreateEntry()
	End Function

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

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
			m_separator = value
			OnPropertyChanged("Separator")
		End Set
	End Property
	Public Property Type As LogType
		Get
			Return m_type
		End Get
		Set(value As LogType)
			m_type = value
			OnPropertyChanged("Type")
		End Set
	End Property
	Public Property Time As DateTime
		Get
			Return m_time
		End Get
		Set(value As DateTime)
			m_time = value
			OnPropertyChanged("Type")
		End Set
	End Property
	Public Property Message As String
		Get
			Return m_message
		End Get
		Set(value As String)
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
		Else
			Ex = Nothing
		End If
	End Sub

	Public Sub Add(ByVal value As KvP)
		Values.Add(value)
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub Add(ByVal value As String)
		Values.Add(New KvP(value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub Add(ByVal key As String, ByVal value As String)
		Values.Add(New KvP(key, value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub AddDevices(ByVal devices As List(Of Win32.SetupAPI.Device))
		For Each d As Win32.SetupAPI.Device In devices
			Values.Add(New KvP("Description", d.Description))
			Values.Add(New KvP("ClassName", d.ClassName))
			Values.Add(New KvP("DeviceID", d.DeviceID))
			Values.Add(New KvP("HardwareIDs", If(d.HardwareIDs IsNot Nothing, String.Join(Environment.NewLine, d.HardwareIDs), "<empty>")))

			If d.DriverInfo IsNot Nothing Then
				Values.Add(New KvP("Driver Details"))

				For Each drvInfo As Win32.SetupAPI.DriverInfo In d.DriverInfo
					Values.Add(New KvP("  Description", If(Not IsNullOrWhitespace(drvInfo.Description), drvInfo.Description, "-")))
					Values.Add(New KvP("  Manufacturer", If(Not IsNullOrWhitespace(drvInfo.MfgName), drvInfo.MfgName, "-")))
					Values.Add(New KvP("  Provider", If(Not IsNullOrWhitespace(drvInfo.ProviderName), drvInfo.ProviderName, "-")))
					Values.Add(New KvP("  DriverDate", drvInfo.DriverDate.ToString()))
					Values.Add(New KvP("  DriverVersion", If(Not IsNullOrWhitespace(drvInfo.DriverVersion), drvInfo.DriverVersion, "-")))

					If drvInfo.InfFile IsNot Nothing Then
						Values.Add(New KvP("  InfFile", If(Not IsNullOrWhitespace(drvInfo.InfFile.FileName), drvInfo.InfFile.FileName, "-")))
						Values.Add(New KvP("     InstallDate", drvInfo.InfFile.InstallDate.ToShortDateString()))
						Values.Add(New KvP("     Class", If(Not IsNullOrWhitespace(drvInfo.InfFile.Class), drvInfo.InfFile.Class, "-")))
						Values.Add(New KvP("     Provider", If(Not IsNullOrWhitespace(drvInfo.InfFile.Provider), drvInfo.InfFile.Provider, "-")))
					End If

					Values.Add(New KvP("---"))
				Next
			End If

			Values.Add(KvP.Empty)
		Next

		OnPropertyChanged("Values")
	End Sub

	Public Sub AddException(ByRef Ex As Exception)
		Message = Ex.Message.Trim()

		HasAnyData = True

		Type = LogType.Error
		Time = DateTime.Now

		m_exData.Clear()
		m_exData.Add("Message", If(String.IsNullOrEmpty(Ex.Message), "Unknown", Ex.Message))
		m_exData.Add("TargetSite", If(Ex.TargetSite IsNot Nothing AndAlso Not String.IsNullOrEmpty(Ex.TargetSite.Name), Ex.TargetSite.Name, "Unknown"))
		m_exData.Add("Source", If(String.IsNullOrEmpty(Ex.Source), "Unknown", Ex.Source))
		m_exData.Add("StackTrace", If(String.IsNullOrEmpty(Ex.StackTrace), "Unknown", Ex.StackTrace))

        If TypeOf (Ex) Is Win32Exception Then
            Dim win32Ex As Win32Exception = TryCast(Ex, Win32Exception)

            If win32Ex IsNot Nothing Then
                m_values.Add(New KvP("Win32_ErrorCode", Win32.GetUInt32(win32Ex.NativeErrorCode).ToString()))
                m_values.Add(New KvP("Win32_Message", win32Ex.Message))
            End If
        End If

		HasException = True
	End Sub

	Protected Overloads Sub OnPropertyChanged(ByVal name As String)
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
	End Sub

#Region "IDisposable Support"
	Private disposedValue As Boolean

	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me.disposedValue Then
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
		Me.disposedValue = True
	End Sub


	Public Sub Dispose() Implements IDisposable.Dispose
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub
#End Region

End Class
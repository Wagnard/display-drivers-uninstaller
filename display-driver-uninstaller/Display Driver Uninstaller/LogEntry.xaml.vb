Imports System.Collections.Specialized
Imports System.Xml
Imports System.ComponentModel

Public Enum LogType
	[Event]
	[Warning]
	[Error]
End Enum

Public Class KvP
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
	Inherits UserControl
	Implements INotifyPropertyChanged
	Implements IDisposable

	Public Shared Function Create() As LogEntry
		Return Application.Log.CreateEntry()
	End Function

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private m_exception As Exception
	Private m_values As List(Of KvP)
	Private m_time As DateTime
	Private m_type As LogType
	Private m_isExpanded, m_canExpand, m_hasValues, m_hasException, m_hasAnyData As Boolean
	Private m_message As String

	Public Property IsExpanded As Boolean
		Get
			Return m_isExpanded
		End Get
		Set(value As Boolean)
			m_isExpanded = value
			OnPropertyChanged("IsExpanded")
		End Set
	End Property
	Public Property CanExpand As Boolean
		Get
			Return m_canExpand
		End Get
		Set(value As Boolean)
			m_canExpand = value
			OnPropertyChanged("CanExpand")
		End Set
	End Property
	Public Property Type As LogType
		Get
			Return m_type
		End Get
		Set(value As LogType)
			m_type = value

			CanExpand = If(value <> LogType.Error, False, True)
			IsExpanded = If(value <> LogType.Error, True, False)

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

	Public Property Exception As Exception
		Get
			Return m_exception
		End Get
		Set(value As Exception)
			m_exception = value
			OnPropertyChanged("Exception")
		End Set
	End Property
	Public ReadOnly Property Values As List(Of KvP)
		Get
			Return m_values
		End Get
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
		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		m_values = New List(Of KvP)

		Time = DateTime.Now
		Type = LogType.Event

		If Ex IsNot Nothing Then
			AddException(Ex)
		Else
			Ex = Nothing
		End If
	End Sub

	Public Sub Add(ByRef value As String)
		Values.Add(New KvP(value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub Add(ByRef key As String, ByRef value As String)
		Values.Add(New KvP(key, value))
		HasValues = True
		HasAnyData = True

		OnPropertyChanged("Values")
	End Sub

	Public Sub AddException(ByRef Ex As Exception)
		Message = Ex.Message.Trim()

		HasAnyData = True

		Type = LogType.Error
		Time = DateTime.Now

		Me.Exception = Ex
		HasException = True
	End Sub

	Private Sub UserControl_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		Me.DataContext = Me
	End Sub

	Protected Overloads Sub OnPropertyChanged(ByVal name As String)
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
	End Sub

#Region "IDisposable Support"
	Private disposedValue As Boolean

	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me.disposedValue Then
			If disposing Then
				'TODO: ADD STUFF
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
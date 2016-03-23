Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel

Public Class AppLog
	Inherits Control

	Private m_threadlock As Object = "No can do!"
	Private m_logEntries As New ObservableCollection(Of LogEntry)

	Public Property LogEntries As ObservableCollection(Of LogEntry)
		Get
			SyncLock m_threadlock
				Return m_logEntries
			End SyncLock
		End Get
		Private Set(value As ObservableCollection(Of LogEntry))
			SyncLock m_threadlock
				m_logEntries = value
			End SyncLock
		End Set
	End Property

	Public Sub Add(ByRef log As LogEntry)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddEntryDelegate(AddressOf Me.AddEntry), log)
			Else
				Me.AddEntry(log)
			End If
		End SyncLock
	End Sub

	Public Sub AddMessage(ByRef message As String)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddMessageEntryDelegate(AddressOf Me.AddMessageEntry), message)
			Else
				Me.AddMessageEntry(message)
			End If
		End SyncLock
	End Sub

	Public Sub Clear()
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New ClearLogDelegate(AddressOf Me.ClearLog))
			Else
				Me.ClearLog()
			End If
		End SyncLock
	End Sub

	Public Sub AddException(ByRef Ex As Exception)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddExceptionEntryDelegate(AddressOf Me.AddExceptionEntry))
			Else
				Me.AddExceptionEntry(Ex)
			End If
		End SyncLock
	End Sub

	Public Sub AddException(ByRef Ex As Exception, ParamArray otherData As String())
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddExceptionParamsEntry(AddressOf Me.AddExceptionParams))
			Else
				Me.AddExceptionParams(Ex, otherData)
			End If
		End SyncLock
	End Sub

	Public Function CreateEntry(Optional ByRef Ex As Exception = Nothing) As LogEntry
		If Not Me.Dispatcher.CheckAccess() Then
			Return DirectCast(Me.Dispatcher.Invoke(New CreateLogEntryDelegate(AddressOf Me.CreateLogEntry)), LogEntry)
		Else
			Return Me.CreateLogEntry()
		End If
	End Function



	Private Delegate Function CreateLogEntryDelegate() As LogEntry
	Public Function CreateLogEntry() As LogEntry
		Return New LogEntry()
	End Function

	Private Delegate Sub AddExceptionEntryDelegate(ByRef Ex As Exception)
	Private Sub AddExceptionEntry(ByRef Ex As Exception)
		Dim logEntry As LogEntry = logEntry.Create()
		logEntry.AddException(Ex)

		AddEntry(logEntry)
	End Sub

	Public Delegate Sub AddExceptionParamsEntry(ByRef Ex As Exception, otherData As String())
	Private Sub AddExceptionParams(ByRef Ex As Exception, ParamArray otherData As String())
		Dim logEntry As LogEntry = logEntry.Create()
		logEntry.AddException(Ex)

		If otherData IsNot Nothing AndAlso otherData.Length > 0 Then
			For Each text As String In otherData
				logEntry.Values.Add(New KvP(text))
			Next
		End If

		logEntry.Time = DateTime.Now
		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddMessageEntryDelegate(ByRef message As String)
	Private Sub AddMessageEntry(ByRef message As String)
		Dim logEntry As LogEntry = logEntry.Create()

		logEntry.Message = message
		logEntry.Time = DateTime.Now

		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddParamEntryDelegate(otherData As KvP())
	Private Sub AddParamEntry(ParamArray otherData As KvP())
		Dim logEntry As LogEntry = logEntry.Create()

		If otherData IsNot Nothing AndAlso otherData.Length > 0 Then
			For Each kvp As KvP In otherData
				logEntry.Values.Add(kvp)
			Next
		End If

		logEntry.Time = DateTime.Now
		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddEntryDelegate(ByRef log As LogEntry)
	Private Sub AddEntry(ByRef log As LogEntry)
		m_logEntries.Add(log)
	End Sub

	Private Delegate Sub ClearLogDelegate()
	Private Sub ClearLog()
		For Each e As LogEntry In m_logEntries
			e.Dispose()
			e = Nothing
		Next
		m_logEntries.Clear()
		m_logEntries = Nothing
		m_logEntries = New ObservableCollection(Of LogEntry)
	End Sub
End Class
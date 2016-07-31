Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Threading

Public Class AppLog
	Inherits Control
	Implements INotifyPropertyChanged

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private m_threadlock As Object = "No can do!"
	Private m_logEntries As New ObservableCollection(Of LogEntry)
	Private m_dispatcher As Threading.Dispatcher
	Private m_countQueued As Int64 = 0L
	Private m_countAdded As Int64 = 0L

	Public Sub New()
		m_dispatcher = Threading.Dispatcher.CurrentDispatcher
	End Sub

	Protected Overloads Sub OnPropertyChanged(ByVal name As String)
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
	End Sub


	Public Property LogEntries As ObservableCollection(Of LogEntry)
		Get
			SyncLock m_threadlock
				Return m_logEntries
			End SyncLock
		End Get
		Set(value As ObservableCollection(Of LogEntry))
			SyncLock m_threadlock
				m_logEntries = value
				OnPropertyChanged("LogEntries")
			End SyncLock
		End Set
	End Property

	Public Sub Add(ByVal log As LogEntry)
		Me.AddEntry(log)
	End Sub

	Public Sub AddMessage(ByVal message As String, Optional ByVal key As String = Nothing, Optional ByVal value As String = Nothing)
		Me.AddMessageEntry(message, key, value, LogType.Event)
	End Sub

	Public Sub AddWarningMessage(ByVal message As String, Optional ByVal key As String = Nothing, Optional ByVal value As String = Nothing)
		Me.AddMessageEntry(message, key, value, LogType.Warning)
	End Sub

	Public Sub AddWarning(ByVal Ex As Exception, Optional ByVal message As String = Nothing)
		Me.AddExceptionEntry(Ex, message, LogType.Warning)
	End Sub

	Public Sub AddException(ByVal Ex As Exception, Optional ByVal message As String = Nothing)
		Me.AddExceptionEntry(Ex, message, LogType.Error)
	End Sub

	Public Sub AddExceptionWithValues(ByVal Ex As Exception, ParamArray otherData As String())
		Me.AddExceptionParams(Ex, otherData)
	End Sub

	Public Function CreateEntry(Optional ByVal Ex As Exception = Nothing, Optional ByVal message As String = Nothing) As LogEntry
		Dim logEntry As New LogEntry()

		If Not IsNullOrWhitespace(message) Then
			logEntry.Message = message
		End If

		If Ex IsNot Nothing Then
			logEntry.AddException(Ex, False)
		End If

		Return logEntry
	End Function

	Public Sub Clear()
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.BeginInvoke(New ClearLogDelegate(AddressOf Me.ClearLog))
		Else
			Me.ClearLog()
		End If
	End Sub

	Public Sub SaveToFile()
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.BeginInvoke(New SaveLogDelegate(AddressOf Me.SaveLog))
		Else
			Me.SaveLog()
		End If
	End Sub

	Public Sub OpenFromFile(ByVal fileName As String)
		If Not m_dispatcher.CheckAccess() Then
			m_dispatcher.BeginInvoke(New OpenLogDelegate(AddressOf Me.OpenLog), fileName)
		Else
			Me.OpenLog(fileName)
		End If
	End Sub

	Public Function IsQueueEmpty() As Boolean
		Return Interlocked.Read(m_countQueued) <= Interlocked.Read(m_countAdded)
	End Function


	Private Delegate Sub SaveLogDelegate()
	Private Sub SaveLog()
		SyncLock m_threadlock
			If Application.Settings.SaveLogs Then
				Dim time As DateTime = If(LogEntries.Count > 0, LogEntries(0).Time, DateTime.Now)

				SaveLog(String.Format("{0}{1}_DDULog.xml", Application.Paths.Logs, time.ToString("yyyy-MM-dd__HH-mm-ss")))
			End If
		End SyncLock
	End Sub

	Private Sub SaveLog(ByVal fileName As String)
		If String.IsNullOrEmpty(fileName) OrElse LogEntries Is Nothing OrElse LogEntries.Count = 0 Then
			Return
		End If

		Try
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
						.WriteStartElement(Application.Settings.AppName.Replace(" ", ""))

						Dim v As Version = Application.Settings.AppVersion

						.WriteAttributeString("Version", String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision))
						.WriteStartElement("LogEntries")

						m_logEntries.Add(New LogEntry() With {.Message = ">> Successfully saved log to file!"})

						For Each log As LogEntry In LogEntries
							.WriteStartElement(log.Type.ToString())

							.WriteStartElement("Time")
							.WriteValue(log.Time.ToString())
							.WriteEndElement()

							.WriteStartElement("Message")
							.WriteValue(If(log.Message, String.Empty))
							.WriteEndElement()

							If log.HasValues Then
								.WriteStartElement("Values")
								.WriteAttributeString("Separator", log.Separator)

								For Each kvp As KvP In log.Values
									If Not kvp.HasAnyValue Then
										.WriteElementString("KvP", String.Empty)
										Continue For
									End If

									.WriteStartElement("KvP")

									If kvp.HasKey Then
										.WriteStartElement("Key")
										.WriteValue(kvp.Key)
										.WriteEndElement()
									End If

									If kvp.HasValue Then
										.WriteStartElement("Value")
										.WriteValue(kvp.Value)
										.WriteEndElement()
									End If

									.WriteEndElement()
								Next

								.WriteEndElement()
							End If

							If log.HasException Then
								.WriteStartElement("ExceptionData")

								For Each d As KeyValuePair(Of String, String) In log.ExceptionData
									If Not String.IsNullOrEmpty(d.Key) Then
										.WriteStartElement(d.Key)
										.WriteValue(If(String.IsNullOrEmpty(d.Value), "Unknown", d.Value))
										.WriteEndElement()
									End If
								Next

								.WriteEndElement()
							End If

							.WriteEndElement()
						Next

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
			AddException(ex, "Saving log failed!")
		End Try
	End Sub

	Private Delegate Sub OpenLogDelegate(ByVal fileName As String)
	Private Sub OpenLog(ByVal fileName As String)
		SyncLock m_threadlock
			If String.IsNullOrEmpty(fileName) OrElse Not File.Exists(fileName) Then
				Return
			End If

			ClearLog()

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
							Return
						End If

						If reader.NodeType <> XmlNodeType.Element Or Not reader.Name.Equals(Application.Settings.AppName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) Or Not reader.HasAttributes Then
							Throw New InvalidDataException("Log's format is invalid!" & vbCrLf & String.Format("Root node doesn't match '{0}'", Application.Current.MainWindow.GetType().Assembly.GetName().Name.Replace(" ", "")) & vbCrLf & "Or missing attributes")
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

						Dim newEntry As New LogEntry()
						Dim name As String
						Dim value As String = String.Empty
						Dim key As String = String.Empty
						Dim exData As Dictionary(Of String, String)

						reader.Read()

						Do While reader.Read()
							If reader.NodeType = XmlNodeType.Element Then
								newEntry = New LogEntry()
								newEntry.Type = CType([Enum].Parse(GetType(LogType), reader.Name), LogType)

								name = reader.Name

								Do
									reader.Read()

									If reader.NodeType = XmlNodeType.Element Then
										If reader.Name.Equals("Time", StringComparison.OrdinalIgnoreCase) Then
											reader.Read()

											newEntry.Time = DateTime.Parse(reader.ReadContentAsString)
										End If

										If reader.Name.Equals("Message", StringComparison.OrdinalIgnoreCase) Then
											newEntry.Message = reader.ReadElementContentAsString
										End If

										If reader.Name.Equals("Values", StringComparison.OrdinalIgnoreCase) Then
											value = Nothing
											key = Nothing

											If reader.HasAttributes Then
												Do While reader.MoveToNextAttribute()
													If Not String.IsNullOrEmpty(reader.Name) Then
														If reader.Name.Equals("Separator", StringComparison.OrdinalIgnoreCase) Then
															newEntry.Separator = reader.Value
														End If
													End If
												Loop
											End If

											Do
												reader.Read()

												If reader.Name.Equals("KvP", StringComparison.OrdinalIgnoreCase) Then
													If reader.IsEmptyElement Then
														newEntry.Add(String.Empty)
														Continue Do
													End If

													reader.Read()

													Do
														If reader.Name.Equals("Key", StringComparison.OrdinalIgnoreCase) Then
															key = reader.ReadElementContentAsString
														ElseIf reader.Name.Equals("Value", StringComparison.OrdinalIgnoreCase) Then
															value = reader.ReadElementContentAsString
														End If
													Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals("KvP", StringComparison.OrdinalIgnoreCase))

													If key Is Nothing Then
														newEntry.Add(value)
													Else
														newEntry.Add(key, value)
													End If

													key = Nothing
													value = Nothing
												End If
											Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals("Values", StringComparison.OrdinalIgnoreCase))

											If newEntry.Values.Count > 0 Then
												newEntry.HasAnyData = True
												newEntry.HasValues = True
											End If
										End If

										If reader.Name.Equals("ExceptionData", StringComparison.OrdinalIgnoreCase) Then
											reader.Read()

											exData = New Dictionary(Of String, String)()
											Do
												If reader.NodeType = XmlNodeType.Element Then
													exData.Add(reader.Name, reader.ReadElementContentAsString)
												End If
											Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals("ExceptionData", StringComparison.OrdinalIgnoreCase))

											If exData.Count > 0 Then
												newEntry.ExceptionData = exData

												newEntry.HasAnyData = True
												newEntry.HasException = True
											End If
										End If
									End If
								Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase))

								AddEntryFinal(newEntry)
							End If
						Loop


						reader.Close()
						sr.Close()
					End Using
				End Using
			Catch ex As Exception
				AddException(ex, "Opening log failed!")
			End Try
		End SyncLock
	End Sub

	Private Delegate Sub AddExceptionEntryDelegate(ByVal Ex As Exception, ByVal message As String, ByVal type As LogType)
	Private Sub AddExceptionEntry(ByVal Ex As Exception, ByVal message As String, ByVal type As LogType)
		Dim logEntry As New LogEntry() With
		{
			.Message = message,
			.Type = type
		}

		logEntry.AddException(Ex, False)

		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddExceptionParamsEntry(ByVal Ex As Exception, otherData As String())
	Private Sub AddExceptionParams(ByVal Ex As Exception, ParamArray otherData As String())
		Dim logEntry As New LogEntry()
		logEntry.AddException(Ex)

		If otherData IsNot Nothing AndAlso otherData.Length > 0 Then
			For Each text As String In otherData
				logEntry.Add(text)
			Next
		End If

		logEntry.Time = DateTime.Now
		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddMessageEntryDelegate(ByVal message As String, ByVal key As String, ByVal value As String, ByVal type As LogType)
	Private Sub AddMessageEntry(ByVal message As String, ByVal key As String, ByVal value As String, ByVal type As LogType)
		Dim logEntry As New LogEntry()

		logEntry.Type = type
		logEntry.Message = message
		logEntry.Time = DateTime.Now

		If key IsNot Nothing Then
			If value IsNot Nothing Then
				logEntry.Add(key, value)
			Else : logEntry.Add(key)
			End If
		End If

		AddEntry(logEntry)
	End Sub

	Private Delegate Sub AddEntryDelegate(ByVal log As LogEntry)
	Private Sub AddEntry(ByVal log As LogEntry)
		If Not m_dispatcher.CheckAccess() Then
			Queue(New AddEntryFinalDelegate(AddressOf Me.AddEntryFinal), log)
		Else
			AddEntryFinal(log)
		End If
	End Sub

	Private Delegate Sub AddEntryFinalDelegate(ByVal log As LogEntry)
	Private Sub AddEntryFinal(ByVal log As LogEntry)
		SyncLock m_threadlock
			log.ID = Interlocked.Increment(m_countAdded)

			m_logEntries.Add(log)
		End SyncLock
	End Sub

	Private Delegate Sub ClearLogDelegate()
	Private Sub ClearLog()
		If Not IsQueueEmpty() Then
			Return
		End If

		SyncLock m_threadlock
			For Each e As LogEntry In m_logEntries
				e.Dispose()
				e = Nothing
			Next
			m_logEntries.Clear()
			m_logEntries = Nothing

			m_logEntries = New ObservableCollection(Of LogEntry)

			OnPropertyChanged("LogEntries")
		End SyncLock
	End Sub

	Private Sub Queue(ByVal method As [Delegate], ByVal ParamArray args() As Object)
		Interlocked.Increment(m_countQueued)
		m_dispatcher.BeginInvoke(method, args)
	End Sub

End Class
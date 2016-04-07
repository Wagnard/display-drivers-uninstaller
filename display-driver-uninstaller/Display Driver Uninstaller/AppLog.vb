Imports System.IO
Imports System.Xml
Imports System.Windows
Imports System.Reflection
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel

Public Class AppLog
	Inherits Control
	Implements INotifyPropertyChanged

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private m_threadlock As Object = "No can do!"
	Private m_logEntries As New ObservableCollection(Of LogEntry)

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

	Public Sub AddWarning(ByRef Ex As Exception)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddWarningEntryDelegate(AddressOf Me.AddWarningEntry), Ex)
			Else
				Me.AddWarningEntry(Ex)
			End If
		End SyncLock
	End Sub

	Public Sub AddException(ByRef Ex As Exception)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddExceptionEntryDelegate(AddressOf Me.AddExceptionEntry), Ex)
			Else
				Me.AddExceptionEntry(Ex)
			End If
		End SyncLock
	End Sub

	Public Sub AddException(ByRef Ex As Exception, ParamArray otherData As String())
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New AddExceptionParamsEntry(AddressOf Me.AddExceptionParams), Ex, otherData)
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

	Public Sub SaveToFile()
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New SaveLogDelegate(AddressOf Me.SaveLog))
			Else
				Me.SaveLog()
			End If
		End SyncLock
	End Sub

	Public Sub OpenFromFile(ByVal fileName As String)
		SyncLock m_threadlock
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(New OpenLogDelegate(AddressOf Me.OpenLog), fileName)
			Else
				Me.OpenLog(fileName)
			End If
		End SyncLock
	End Sub

	Protected Overloads Sub OnPropertyChanged(ByVal name As String)
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
	End Sub



	Private Delegate Sub SaveLogDelegate()
	Private Sub SaveLog()
		If Application.Settings.SaveLogs Then
			Dim time As DateTime = If(LogEntries.Count > 0, LogEntries(0).Time, DateTime.Now)

			SaveLog(String.Format("{0}{1}_DDULog.xml", Application.Paths.Logs, time.ToString("yyyy-MM-dd__HH-mm-ss")))
		End If
	End Sub
	Private Sub SaveLog(ByVal fileName As String)
		If String.IsNullOrEmpty(fileName) Then
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

						Dim hours As Int32

						For Each log As LogEntry In LogEntries
							hours = log.Time.Subtract(log.Time.ToUniversalTime()).Hours

							.WriteStartElement(log.Type.ToString())

							.WriteStartElement("Time")
							.WriteValue(log.Time.ToString())
							.WriteEndElement()

							.WriteStartElement("Message")
							.WriteValue(log.Message)
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
			AddExceptionEntry(ex)
		End Try
	End Sub

	Private Delegate Sub OpenLogDelegate(ByVal fileName As String)
	Public Sub OpenLog(ByVal fileName As String)
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

					Dim newEntry As LogEntry = LogEntry.Create()
					Dim name As String
					Dim value As String = String.Empty
					Dim key As String = String.Empty
					Dim exData As Dictionary(Of String, String)

					reader.Read()

					Do While reader.Read()
						If reader.NodeType = XmlNodeType.Element Then
							newEntry = LogEntry.Create()
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

							LogEntries.Add(newEntry)
						End If
					Loop


					reader.Close()
					sr.Close()
				End Using
			End Using
		Catch ex As Exception
			AddExceptionEntry(ex)
		End Try
	End Sub

	Private Delegate Function CreateLogEntryDelegate() As LogEntry
	Private Function CreateLogEntry() As LogEntry
		Return New LogEntry()
	End Function

	Private Delegate Sub AddWarningEntryDelegate(ByRef Ex As Exception)
	Private Sub AddWarningEntry(ByRef Ex As Exception)
		Dim logEntry As LogEntry = logEntry.Create()
		logEntry.AddException(Ex)
		logEntry.Type = LogType.Warning

		AddEntry(logEntry)
	End Sub


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

		OnPropertyChanged("LogEntries")
	End Sub

End Class
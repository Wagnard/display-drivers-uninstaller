Imports Display_Driver_Uninstaller.Win32
Imports System.IO

Public Class Inf
	Private ReadOnly _fileName As String = Nothing
	Private ReadOnly _provider As String = Nothing
	Private ReadOnly _class As String = Nothing
	Private ReadOnly _fileExists As Boolean = False
	Private ReadOnly _isValid As Boolean = False
	Private _installDate As DateTime

	Public ReadOnly Property FileName As String
		Get
			Return _fileName
		End Get
	End Property
	Public ReadOnly Property Provider As String
		Get
			Return _provider
		End Get
	End Property
	Public ReadOnly Property [Class] As String
		Get
			Return _class
		End Get
	End Property
	Public ReadOnly Property FileExists As Boolean
		Get
			Return Not IsNullOrWhitespace(_fileName) AndAlso File.Exists(_fileName)
		End Get
	End Property
	Public ReadOnly Property IsValid As Boolean
		Get
			Return _isValid
		End Get
	End Property
	Public Property InstallDate As DateTime
		Get
			Return _installDate
		End Get
		Friend Set(value As DateTime)
			_installDate = value
		End Set
	End Property

	Friend Sub New(ByVal fileName As String)
		_fileName = fileName

		If IsNullOrWhitespace(_fileName) OrElse Not File.Exists(_fileName) Then
			_isValid = False
			Return
		End If

		Try
			Using infFile As SetupAPI.InfFile = New SetupAPI.InfFile(_fileName)
				If infFile.Open() = 0UI Then
					Dim lineClass As SetupAPI.InfLine = infFile.FindFirstKey("Version", "Class")
					Dim lineProvider As SetupAPI.InfLine = infFile.FindFirstKey("Version", "Provider")

					_class = If(lineClass IsNot Nothing, lineClass.GetString(1), String.Empty)
					_provider = If(lineProvider IsNot Nothing, lineProvider.GetString(1), String.Empty)

					If Not IsNullOrWhitespace(_provider) Or Not IsNullOrWhitespace(_class) Then
						_isValid = True
					End If
				Else
					_isValid = False

					Dim logEntry As LogEntry = Application.Log.CreateEntry()

					logEntry.Type = LogType.Warning
					logEntry.Message = String.Concat("Invalid inf file!", CRLF, ">> ", Path.GetFileName(_fileName))

					logEntry.Add("infFile", _fileName)
					logEntry.Add(KvP.Empty)
					logEntry.Add("Win32_ErrorCode", String.Format("{0} (0x{1:X})", infFile.LastError.ToString(), infFile.LastError))
					logEntry.Add("Win32_Message", infFile.LastMessage)

					Application.Log.Add(logEntry)
				End If
			End Using
		Catch ex As Exception
			_isValid = False

			Dim logEntry As LogEntry = Application.Log.CreateEntry()
			logEntry.Add("FileName", _fileName)
			Application.Log.AddException(ex)
		End Try
	End Sub

	Public Overrides Function ToString() As String
		Return If(_fileName, "<empty>")
	End Function
End Class
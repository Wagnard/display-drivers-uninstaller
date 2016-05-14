Imports System.Text
Imports System.Runtime.InteropServices

Namespace Win32
	Partial Public Class SetupAPI
		Friend Class InfError
			Private Const APPLICATION_ERROR_MASK = &H20000000UI
			Private Const ERROR_SEVERITY_ERROR = &HC0000000UI

			Private Const ERROR_LINE_NOT_FOUND As UInt32 = &H800F0102UI
			Private Const ERROR_NO_MORE_ITEMS As UInt32 = &H80070103UI
			Private Const ERROR_INVALID_PARAMETER As UInt32 = &H80070057UI
			Private Const FACILITY_SETUPAPI As UInt32 = 15UI
			Private Const FACILITY_WIN32 As UInt32 = 7UI

			Protected _lastError As UInt32 = 0UI
			Protected _lastMessage As String

			Public ReadOnly Property LastError As UInt32
				Get
					Return _lastError
				End Get
			End Property

			Public ReadOnly Property LastMessage As String
				Get
					Return If(_lastMessage, "Success")
				End Get
			End Property

			Protected Sub ResetError()
				_lastError = 0UI
				_lastMessage = Nothing
			End Sub

			Protected Function GetLastError() As UInt32
				Return GetLastError(True)
			End Function

			Protected Function GetLastError(ByVal setLastError As Boolean) As UInt32
				Dim hresult As UInt32 = HRESULT_FROM_SETUPAPI(GetLastWin32ErrorU())

				If setLastError Then
					_lastError = hresult
					_lastMessage = New System.ComponentModel.Win32Exception(GetInt32(_lastError)).Message
				End If

				Return hresult
			End Function

			Private Shared Function HRESULT_FROM_WIN32(ByVal x As UInt32) As UInt32
				Return If(x <= 0,
				 x,
				 ((x And &HFFFFUI) Or (FACILITY_WIN32 << 16UI) Or &H80000000UI))
			End Function

			Private Shared Function HRESULT_FROM_SETUPAPI(ByVal x As UInt32) As UInt32
				Return CUInt(If(((x And (APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR)) = (APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR)),
				 (((x And &HFFFFUI) Or (FACILITY_SETUPAPI << 16UI) Or &H80000000UI)),
				 HRESULT_FROM_WIN32(x)))
			End Function

		End Class

		Friend Class InfFile
			Inherits InfError
			Implements IDisposable

			Private Class PrivateSection
				Inherits InfLine

				Public Sub New(ByVal context As INFCONTEXT, ByVal section As String, ByVal file As String)
					_context = context
					_section = section
					_file = file
				End Sub
			End Class

			Private _file As String
			Private _handle As IntPtr = INVALID_HANDLE
			Private _disposed As Boolean

			Public ReadOnly Property FilePath As String
				Get
					Return If(_file, "<null>")
				End Get
			End Property

			Public ReadOnly Property IsOpen As Boolean
				Get
					Return _handle <> INVALID_HANDLE
				End Get
			End Property

			Public Sub New(ByVal path As String)
				_file = path
			End Sub

			Public Function Open(ByRef errorLineNumber As UInt32) As UInt32
				_handle = SetupOpenInfFile(_file, Nothing, (INF_STYLE_OLDNT Or INF_STYLE_WIN4), errorLineNumber)

				If (_handle = INVALID_HANDLE) Then
					GetLastError()
					_lastMessage = String.Format("Error opening file '{0}',{3}Bad line = {1},{3}Message = ""{2}""", FilePath, errorLineNumber, LastMessage, CRLF)
				Else
					ResetError()
					errorLineNumber = 0UI
				End If

				Return LastError
			End Function

			Public Function Open() As UInt32
				Dim errLine As UInt32
				Return Open(errLine)
			End Function

			Public Sub Close()
				If _handle <> INVALID_HANDLE Then
					SetupCloseInfFile(_handle)

					_handle = INVALID_HANDLE
				End If
			End Sub

			Public Function FindFirstKey(ByVal section As String, ByVal key As String) As InfLine
				Dim retval As PrivateSection = Nothing
				Dim sectionContext As INFCONTEXT = New INFCONTEXT

				If Not SetupFindFirstLine(_handle, section, key, sectionContext) Then
					GetLastError()
					_lastMessage = String.Format("Error finding key '{0}' in section '{1}' of file '{2}',{4}Message = ""{3}""", If(key, "<null>"), section, FilePath, LastMessage, CRLF)
				Else
					ResetError()
					retval = New PrivateSection(sectionContext, section, _file)
				End If

				Return retval
			End Function

			Public Overrides Function ToString() As String
				Return FilePath
			End Function

			Protected Overrides Sub Finalize()
				Dispose(False)
			End Sub

			Protected Overridable Sub Dispose(disposing As Boolean)
				If Not _disposed Then
					If disposing Then

					End If

					Close()
				End If

				_disposed = True
			End Sub

			Public Sub Dispose() Implements IDisposable.Dispose
				Dispose(True)
				GC.SuppressFinalize(Me)
			End Sub

		End Class

		Friend Class InfLine
			Inherits InfError

			Protected _context As INFCONTEXT = New INFCONTEXT
			Protected _section As String
			Protected _file As String

			Public ReadOnly Property Section As String
				Get
					Return If(_section, "<null>")
				End Get
			End Property

			Public ReadOnly Property FilePath As String
				Get
					Return If(_file, "<null>")
				End Get
			End Property

			Protected Sub New()
			End Sub

			Public Function GetLineText(ByRef line As String) As UInt32
				_lastError = InternalGetLineText(_lastMessage, line)

				Return LastError
			End Function

			Public Function GetLineText() As String
				Dim line As String = Nothing

				GetLineText(line)

				Return line
			End Function

			Public Function GetString(ByVal fieldNum As Int32, ByRef strVal As String) As UInt32
				Dim requiredSize As UInt32 = 0UI
				Dim builder As StringBuilder = New StringBuilder(100)
				Dim lineText As String = Nothing
				Dim msg As String = Nothing

				ResetError()

				Dim setupRetVal As Boolean = SetupGetStringField(_context, fieldNum, builder, builder.Capacity, requiredSize)

				If requiredSize > builder.Capacity Then
					builder.Capacity = CType(requiredSize, Int32)

					setupRetVal = SetupGetStringField(_context, fieldNum, builder, builder.Capacity, requiredSize)
				End If

				If Not setupRetVal Then
					strVal = Nothing
					GetLastError()

					If (InternalGetLineText(msg, lineText) <> 0) Then lineText = "<unknown>"

					_lastMessage = String.Format("Error reading string value from field {0} of line '{1}'{5}in section [{2}] in file '{3}',{5}Message = ""{4}""", fieldNum, lineText, Section, FilePath, LastMessage, CRLF)
				Else
					strVal = builder.ToString()
				End If

				Return LastError
			End Function

			Public Function GetString(ByVal fieldNum As Int32) As String
				Dim val As String = Nothing

				GetString(fieldNum, val)

				Return val
			End Function

			Private Function InternalGetLineText(ByRef errorMessage As String, ByRef line As String) As UInt32
				Dim requiredSize As UInt32 = 0
				Dim builder As StringBuilder = New StringBuilder(200)
				Dim rc As UInt32

				Dim setupRetVal As Boolean = SetupGetLineText(_context, IntPtr.Zero, Nothing, Nothing, builder, GetUInt32(builder.Capacity), requiredSize)

				If requiredSize > builder.Capacity Then
					builder.Capacity = CType(requiredSize, Int32)
					setupRetVal = SetupGetLineText(_context, IntPtr.Zero, Nothing, Nothing, builder, GetUInt32(builder.Capacity), requiredSize)
				End If

				If Not setupRetVal Then
					rc = GetLastError(False)
					errorMessage = String.Format("Error reading INF line text in file '{0}',{2}Message = ""{1}""", FilePath, New System.ComponentModel.Win32Exception(GetInt32(rc)).Message, CRLF)
					line = Nothing
				Else
					rc = 0
					errorMessage = Nothing
					line = builder.ToString()
				End If

				Return rc
			End Function

			Public Overrides Function ToString() As String
				Return String.Format("Section = {0}, FilePath = {1}", Section, FilePath)
			End Function

		End Class

		Private Shared ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)
		Private Const INF_STYLE_OLDNT As UInt32 = &H1UI
		Private Const INF_STYLE_WIN4 As UInt32 = &H2UI

		<StructLayout(LayoutKind.Sequential)>
		Friend Structure INFCONTEXT
			Dim Inf As IntPtr
			Dim CurrentInf As IntPtr
			Dim Section As UInt32
			Dim Line As UInt32
		End Structure

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupGetLineText(
   <[In]()> ByRef context As INFCONTEXT,
   <[In]()> ByVal InfHandle As IntPtr,
   <[In]()> ByVal Section As String,
   <[In]()> ByVal [Key] As String,
   <[In](), [Out]()> ByVal ReturnBuffer As StringBuilder,
   <[In]()> ByVal ReturnBufferSize As UInt32,
   <[In](), [Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupGetStringField(
   <[In]()> ByRef context As INFCONTEXT,
   <[In]()> ByVal fieldIndex As Int32,
   <[In](), [Out]()> ByVal ReturnBuffer As StringBuilder,
   <[In]()> ByVal ReturnBufferSize As Int32,
   <[Out]()> ByRef RequiredSize As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupOpenInfFile(
   <[In]()> <MarshalAs(UnmanagedType.LPWStr)> ByVal FileName As String,
   <[In]()> <MarshalAs(UnmanagedType.LPWStr)> ByVal InfClass As String,
   <[In]()> ByVal InfStyle As UInt32,
   <[In]()> ByRef ErrorLine As UInt32) As IntPtr
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Sub SetupCloseInfFile(
   <[In]()> ByVal InfHandle As IntPtr)
		End Sub

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupFindFirstLine(
   <[In]()> ByVal InfHandle As IntPtr,
   <[In]()> ByVal Section As String,
   <[In]()> ByVal Key As String,
   <[In](), [Out]()> ByRef context As INFCONTEXT) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("setupapi.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function SetupFindNextMatchLine(
   <[In]()> ByRef contextIn As INFCONTEXT,
   <[In]()> ByVal key As String, <MarshalAs(UnmanagedType.Struct)>
   <[In](), [Out]()> ByRef contextOut As INFCONTEXT) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

	End Class
End Namespace

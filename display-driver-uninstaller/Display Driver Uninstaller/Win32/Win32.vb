Option Strict On

Imports System.ComponentModel
Imports System.Runtime.InteropServices

Namespace Win32
	<ComVisible(False)>
	Friend Module Win32Native

#Region "Consts"
		Friend Const ENUM_FORMAT As String = "{0} (0x{1})"

		Friend Const LINE_LEN As Int32 = 256
		Friend Const MAX_LEN As Int32 = 260

		Friend ReadOnly CRLF As String = Environment.NewLine
		Friend ReadOnly DefaultCharSize As Int32 = Marshal.SystemDefaultCharSize
		Friend ReadOnly DefaultCharSizeU As UInt32 = GetUInt32(DefaultCharSize)
		Friend ReadOnly NullChar() As Char = New Char() {CChar(vbNullChar)}

#End Region

#Region "Errors"
		Private Const APPLICATION_ERROR_MASK = &H20000000UI
		Private Const ERROR_SEVERITY_ERROR = &HC0000000UI

		Public Enum [Errors] As UInteger
			BAD_INTERFACE_INSTALLSECT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21DUI
			BAD_SECTION_NAME_LINE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 1UI
			BAD_SERVICE_INSTALLSECT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H217UI
			CANT_LOAD_CLASS_ICON = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20CUI
			CANT_REMOVE_DEVINST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H232UI
			CLASS_MISMATCH = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H201UI
			DEVICE_INTERFACE_ACTIVE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21BUI
			DEVICE_INTERFACE_REMOVED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21CUI
			DEVINFO_DATA_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H213UI
			DEVINFO_LIST_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H212UI
			DEVINFO_NOT_REGISTERED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H208UI
			DEVINSTALL_QUEUE_NONNATIVE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H230UI
			DEVINST_ALREADY_EXISTS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H207UI
			DI_BAD_PATH = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H214UI
			DI_DONT_INSTALL = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22BUI
			DI_DO_DEFAULT = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20EUI
			DI_NOFILECOPY = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20FUI
			DI_POSTPROCESSING_REQUIRED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H226UI
			DUPLICATE_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H202UI
			EXPECTED_SECTION_NAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 0UI
			FILEQUEUE_LOCKED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H216UI
			GENERAL_SYNTAX = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 3UI
			INVALID_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H206UI
			INVALID_CLASS_INSTALLER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20DUI
			INVALID_COINSTALLER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H227UI
			INVALID_DEVINST_NAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H205UI
			INVALID_FILTER_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22CUI
			INVALID_HWPROFILE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H210UI
			INVALID_INF_LOGCONFIG = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22AUI
			INVALID_MACHINENAME = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H220UI
			INVALID_PROPPAGE_PROVIDER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H224UI
			INVALID_REFERENCE_STRING = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21FUI
			INVALID_REG_PROPERTY = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H209UI
			KEY_DOES_NOT_EXIST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H204UI
			LINE_NOT_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H102UI
			MACHINE_UNAVAILABLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H222UI
			NON_WINDOWS_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22EUI
			NON_WINDOWS_NT_DRIVER = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22DUI
			NOT_DISABLEABLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H231UI
			NOT_INSTALLED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H1000UI
			NO_ASSOCIATED_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H200UI
			NO_ASSOCIATED_SERVICE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H219UI
			NO_BACKUP = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H103UI
			NO_CATALOG_FOR_OEM_INF = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H22FUI
			NO_CLASSINSTALL_PARAMS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H215UI
			NO_CLASS_DRIVER_LIST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H218UI
			NO_COMPAT_DRIVERS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H228UI
			NO_CONFIGMGR_SERVICES = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H223UI
			NO_DEFAULT_DEVICE_INTERFACE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21AUI
			NO_DEVICE_ICON = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H229UI
			NO_DEVICE_SELECTED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H211UI
			NO_DRIVER_SELECTED = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H203UI
			NO_INF = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20AUI
			NO_SUCH_DEVICE_INTERFACE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H225UI
			NO_SUCH_DEVINST = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H20BUI
			NO_SUCH_INTERFACE_CLASS = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H21EUI
			REMOTE_COMM_FAILURE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H221UI
			SECTION_NAME_TOO_LONG = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or 2UI
			SECTION_NOT_FOUND = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H101UI
			WRONG_INF_STYLE = APPLICATION_ERROR_MASK Or ERROR_SEVERITY_ERROR Or &H100UI

			INF_NOT_OEM = &HE000023CUI
			INF_IN_USE = &HE000023DUI

			FILE_NOT_FOUND = 2UI
			PATH_NOT_FOUND = 3UI
			ACCESS_DENIED = 5UI
			INVALID_DATA = 13UI
			INSUFFICIENT_BUFFER = 122UI
			DIR_NOT_EMPTY = 145UI
			NO_MORE_ITEMS = 259UI
			NOT_ALL_ASSIGNED = &H514
			INVALID_USER_BUFFER = 1784UI
			INVALID_FILE_ATTRIBUTES = &HFFFFFFFFUI
		End Enum

#End Region

#Region "Structures"

		<StructLayout(LayoutKind.Explicit)>
		Friend Structure EvilInteger
			<FieldOffset(0)>
			Public Int32 As Int32
			<FieldOffset(0)>
			Public UInt32 As UInt32
		End Structure

#End Region

#Region "Functions"

		' <Extension()>
		'Public Function ToStringArray(Of T)(ByVal e As [Enum]) As String()
		'	Dim eNames As List(Of String) = New List(Of String)(10)
		'	Dim flags As UInt32 = Convert.ToUInt32(e)
		'	Dim flags2 As UInt32 = flags

		'	If flags = 0UI Then
		'		Return Nothing
		'	End If

		'	For Each value As T In [Enum].GetValues(GetType(T))
		'		Dim bit As UInt32 = Convert.ToUInt32(value)

		'		If (flags And bit) = bit Then
		'			eNames.Add(value.ToString())
		'			flags2 = flags2 And bit
		'		End If
		'	Next

		'	Return eNames.ToArray()
		'End Function

		' <Extension()>
		Public Function ToStringArray(Of T)(ByVal flags As UInt32, Optional ByVal [default] As String = "OK") As String()
			Dim eNames As List(Of String) = New List(Of String)(10)
			Dim type As Type = GetType(T)

			If flags = 0UI Then
				Return New String() {[default]}
			End If

			Dim bit As UInt32 = 1UI
			Dim size As Int32 = Marshal.SizeOf(flags)

			For i As Int32 = (size * 8 - 1) To 0 Step -1
				If (flags And bit) = bit Then
					If [Enum].IsDefined(type, bit) Then
						eNames.Add(String.Format(ENUM_FORMAT, [Enum].GetName(type, bit), bit.ToString("X2").TrimStart("0"c)))
					Else
						eNames.Add(String.Format(ENUM_FORMAT, "UNKNOWN", bit.ToString("X2").TrimStart("0"c)))
					End If
				End If

				bit <<= 1
			Next

			Return eNames.ToArray()
		End Function

		' <Extension()>
		Friend Function IntPtrAdd(ByVal ptr As IntPtr, ByVal offSet As Int64) As IntPtr
			Return New IntPtr(ptr.ToInt64() + offSet)
		End Function

		' <Extension()>
		Friend Function GetVersion(ByVal version As UInt64) As String
			Dim bytes() As Byte = BitConverter.GetBytes(version)
			Dim format As String = If(BitConverter.IsLittleEndian, "{0}.{1}.{2}.{3}", "{3}.{2}.{1}.{0}")

			Return String.Format(format,
			  BitConverter.ToInt16(bytes, 0).ToString(),
			  BitConverter.ToInt16(bytes, 2).ToString(),
			  BitConverter.ToInt16(bytes, 4).ToString(),
			  BitConverter.ToInt16(bytes, 6).ToString())
		End Function

		' <Extension()>
		Friend Function GetUInt32(ByVal int As Int32) As UInt32
			Return (New EvilInteger() With {.Int32 = int}).UInt32
		End Function

		' <Extension()>
		Friend Function GetInt32(ByVal int As UInt32) As Int32
			Return (New EvilInteger() With {.UInt32 = int}).Int32
		End Function

		Friend Sub ShowException(ByVal ex As Exception)
			If TypeOf (ex) Is Win32Exception Then
				Dim e As UInt32 = GetUInt32(DirectCast(ex, Win32Exception).NativeErrorCode)
				Dim detailMsg As String = Nothing

				If GetErrorMessage(e, detailMsg) Then
					MessageBox.Show(String.Format("{0}{2}{2}Error code: {1}{2}{3}{2}{2}{4}", detailMsg, e.ToString(), CRLF, ex.Message, ex.StackTrace), "Win32Exception!")
				Else
					MessageBox.Show(String.Format("Error code: {0}{1}{2}{1}{1}{3}", e.ToString(), CRLF, ex.Message, ex.StackTrace), "Win32Exception!")
				End If
			Else
				MessageBox.Show(ex.Message & CRLF & CRLF & If(ex.TargetSite IsNot Nothing, ex.TargetSite.Name, "<null>") & CRLF & CRLF & ex.Source & CRLF & CRLF & ex.StackTrace, "Exception!")
			End If
		End Sub

		Friend Sub CheckWin32Error(ByVal success As Boolean)
			If Not success Then
				Throw New Win32Exception()
			End If
		End Sub

		Friend Function GetLastWin32Error() As Int32
			Return Marshal.GetLastWin32Error()
		End Function

		Friend Function GetLastWin32ErrorU() As UInt32
			Return GetUInt32(Marshal.GetLastWin32Error())
		End Function

		Friend Function GetErrorMessage(ByVal errCode As UInt32, ByRef message As String) As Boolean
			Select Case errCode
				Case Errors.ACCESS_DENIED
					message = "You have no rights... run as Admin!"
					Return True
				Case Errors.NO_SUCH_DEVINST
					message = "Device doesn't exists!"
					Return True
				Case Else
					message = String.Empty
					Return False
			End Select
		End Function

#End Region

		Friend Class StructPtr
			Implements IDisposable

			Private _disposed As Boolean
			Private _ptr As IntPtr
			Private _objSize As New EvilInteger

			Public ReadOnly Property Ptr As IntPtr
				Get
					Return _ptr
				End Get
			End Property
			Public ReadOnly Property ObjSize As Int32
				Get
					Return _objSize.Int32
				End Get
			End Property
			Public ReadOnly Property ObjSizeU As UInt32
				Get
					Return _objSize.UInt32
				End Get
			End Property

			Public Sub New(ByVal obj As Object, Optional ByVal size As UInt32 = 0UI)
				If Ptr = Nothing Then
					If (size <= 0UI) Then
						_objSize.Int32 = Marshal.SizeOf(obj)
					Else
						_objSize.UInt32 = size
					End If

					_ptr = Marshal.AllocHGlobal(ObjSize)
					Marshal.StructureToPtr(obj, _ptr, False)
				Else
					_ptr = IntPtr.Zero
				End If
			End Sub

			Protected Overridable Sub Dispose(ByVal disposing As Boolean)
				If Not _disposed Then
					If disposing Then

					End If

					If _ptr <> IntPtr.Zero Then
						Marshal.FreeHGlobal(Ptr)
						_ptr = IntPtr.Zero
					End If

				End If

				_disposed = True
			End Sub

			Protected Overrides Sub Finalize()
				Dispose(False)
				MyBase.Finalize()
			End Sub

			Public Sub Dispose() Implements IDisposable.Dispose
				Dispose(True)
				GC.SuppressFinalize(Me)
			End Sub
		End Class

	End Module
End Namespace
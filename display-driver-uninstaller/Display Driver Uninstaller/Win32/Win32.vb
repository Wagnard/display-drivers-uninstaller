Option Strict On

Imports System.ComponentModel
Imports System.Runtime.InteropServices

Namespace Win32

#Region "Enums"


#End Region

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
		Private Const APPLICATION_ERROR_MASK As UInt32 = &H20000000UI
		Private Const ERROR_SEVERITY_ERROR As UInt32 = &HC0000000UI

		Public Enum [Errors] As UInt32
			' SetupAPI Errors
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

			INF_NOT_OEM = &HE000023CUI		' Inf is not oem installed
			INF_IN_USE = &HE000023DUI		' Inf is currently used by device


			' FileIO Errors
			FILE_NOT_FOUND = 2UI
			PATH_NOT_FOUND = 3UI
			ACCESS_DENIED = 5UI
			INVALID_DATA = 13UI
			SHARING_VIOLATION = 32UI			' File in use
			INSUFFICIENT_BUFFER = 122UI			' Buffer size too small
			DIR_NOT_EMPTY = 145UI
			NO_MORE_ITEMS = 259UI
			NOT_ALL_ASSIGNED = &H514			' AdjustToken, privilige already assigned
			CANCELLED = 1223UI
			INVALID_USER_BUFFER = 1784UI		' Invalid buffer (size or structure doesn't match)
			INVALID_FILE_ATTRIBUTES = &HFFFFFFFFUI


			' Task Scheluder Errors
			SCHED_E_ACCOUNT_DBASE_CORRUPT = &H80041311UI		'Corruption was detected in the Task Scheduler security database; the database has been reset.		
			SCHED_E_ACCOUNT_INFORMATION_NOT_SET = &H8004130FUI	'No account information could be found in the Task Scheduler security database for the task indicated.		
			SCHED_E_ACCOUNT_NAME_NOT_FOUND = &H80041310UI		'Unable to establish existence of the account specified.		
			SCHED_E_ALREADY_RUNNING = &H8004131FUI				'An instance of this task is already running.		
			SCHED_E_CANNOT_OPEN_TASK = &H8004130DUI				'The task object could not be opened.		
			SCHED_E_INVALIDVALUE = &H80041318UI					'The task XML contains a value which is incorrectly formatted or out of range.		
			SCHED_E_INVALID_TASK = &H8004130EUI					'The object is either an invalid task object or is not a task object.		
			SCHED_E_INVALID_TASK_HASH = &H80041321UI			'The task image is corrupt or has been tampered with.		
			SCHED_E_MALFORMEDXML = &H8004131AUI					'The task XML is malformed.		
			SCHED_E_MISSINGNODE = &H80041319UI					'The task XML is missing a required element or attribute.		
			SCHED_E_NAMESPACE = &H80041317UI					'The task XML contains an element or attribute from an unexpected namespace.		
			SCHED_E_NO_SECURITY_SERVICES = &H80041312UI			'Task Scheduler security services are available only on Windows NT.		
			SCHED_E_PAST_END_BOUNDARY = &H8004131EUI			'The task cannot be started after the trigger end boundary.		
			SCHED_E_SERVICE_NOT_AVAILABLE = &H80041322UI		'The Task Scheduler service is not available.		
			SCHED_E_SERVICE_NOT_INSTALLED = &H8004130CUI		'The Task Scheduler service is not installed on this computer.		
			SCHED_E_SERVICE_NOT_RUNNING = &H80041315UI			'The Task Scheduler Service is not running.		
			SCHED_E_SERVICE_TOO_BUSY = &H80041323UI				'The Task Scheduler service is too busy to handle your request. Please try again later.		
			SCHED_E_START_ON_DEMAND = &H80041328UI				'The task settings do not allow the task to start on demand.
			SCHED_E_TASK_ATTEMPTED = &H80041324UI				'The Task Scheduler service attempted to run the task, but the task did not run due to one of the constraints in the task definition.		
			SCHED_E_TASK_DISABLED = &H80041326UI				'The task is disabled.		
			SCHED_E_TASK_NOT_READY = &H8004130AUI				'One or more of the properties required to run this task have not been set.		
			SCHED_E_TASK_NOT_RUNNING = &H8004130BUI				'There is no running instance of the task.		
			SCHED_E_TASK_NOT_V1_COMPAT = &H80041327UI			'The task has properties that are not compatible with earlier versions of Windows.		
			SCHED_E_TOO_MANY_NODES = &H8004131DUI				'The task XML contains too many nodes of the same type.		
			SCHED_E_TRIGGER_NOT_FOUND = &H80041309UI			'A task's trigger is not found.		
			SCHED_E_UNEXPECTEDNODE = &H80041316UI				'The task XML contains an unexpected node.		
			SCHED_E_UNKNOWN_OBJECT_VERSION = &H80041313UI		'The task object version is either unsupported or invalid.		
			SCHED_E_UNSUPPORTED_ACCOUNT_OPTION = &H80041314UI	'The task has been configured with an unsupported combination of account settings and run time options.		
			SCHED_E_USER_NOT_LOGGED_ON = &H80041320UI			'The task will not run because the user is not logged on.		
			SCHED_S_BATCH_LOGON_PROBLEM = &H4131CUI				'The task is registered, but may fail to start. Batch logon privilege needs to be enabled for the task principal.		
			SCHED_S_EVENT_TRIGGER = &H41308UI					'Event triggers do not have set run times.		
			SCHED_S_SOME_TRIGGERS_FAILED = &H4131BUI			'The task is registered, but not all specified triggers will start the task.		
			SCHED_S_TASK_DISABLED = &H41302UI					'The task will not run at the scheduled times because it has been disabled.		
			SCHED_S_TASK_HAS_NOT_RUN = &H4130UI					'The task has not yet run.		
			SCHED_S_TASK_NOT_SCHEDULED = &H41305UI				'One or more of the properties that are needed to run this task on a schedule have not been set.		
			SCHED_S_TASK_NO_MORE_RUNS = &H41304UI				'There are no more runs scheduled for this task.		
			SCHED_S_TASK_NO_VALID_TRIGGERS = &H41307UI			'Either the task has no triggers or the existing triggers are disabled or not set.		
			SCHED_S_TASK_QUEUED = &H41325UI						'The Task Scheduler service has asked the task to run.		
			SCHED_S_TASK_READY = &H41300UI						'The task is ready to run at its next scheduled time.		
			SCHED_S_TASK_RUNNING = &H41301UI					'The task is currently running.		
			SCHED_S_TASK_TERMINATED = &H41306UI					'The last run of the task was terminated by the user.		
			SERVICE_SPECIFIC_ERROR = &H42AUI					'The service has returned a service-specific error code.

			BAD_NETPATH = 53UI
			'This error is returned in the following situations:
			'The computer name specified in the serverName parameter does not exist.
			'When you are trying to connect to a Windows Server 2003 or Windows XP computer, 
			'and the remote computer does not have the File and Printer Sharing firewall exception enabled or the Remote Registry service is not running.
			'When you are trying to connect to a Windows Vista computer, and the remote computer does not have the Remote Scheduled Tasks Management firewall 
			'exception enabled and the File and Printer Sharing firewall exception enabled, or the Remote Registry service is not running.

			NOT_SUPPORTED = 50UI								'The user, password, or domain parameters cannot be specified when connecting to a remote Windows XP or Windows Server 2003 computer from a Windows Vista computer.
			E_ACCESS_DENIED = &H80070005UI						'Access is denied to connect to the Task Scheduler service.


			' Generic errors
			ONLY_IF_CONNECTED = &H800704E3UI					'This operation is supported only when you are connected to the server.
			S_OK = &H0UI										'Operation successful
			E_ABORT = &H80004004UI								'Operation aborted
			E_ACCESSDENIED = &H80070005UI						'General access denied error
			E_FAIL = &H80004005UI								'Unspecified failure
			E_HANDLE = &H80070006UI								'Handle that is not valid
			E_INVALIDARG = &H80070057UI							'One or more arguments are not valid
			E_NOINTERFACE = &H80004002UI						'No such interface supported
			E_NOTIMPL = &H80004001UI							'Not implemented
			E_OUTOFMEMORY = &H8007000EUI						'Failed to allocate necessary memory
			E_POINTER = &H80004003UI							'Pointer that is not valid
			E_UNEXPECTED = &H8000FFFFUI							'Unexpected failure

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

		Friend Function HasFlag(ByVal flags As UInt32, ByVal flag As UInt32) As Boolean
			Return ((flags And flag) = flag)
		End Function

		Friend Function SetFlag(Of T)(ByVal flags As [Enum], ByVal flag As UInt32, ByVal value As Boolean) As T
			Dim flags2 As UInt32 = Convert.ToUInt32(flags)

			If value Then
				flags2 = flags2 Or flag
			Else
				flags2 = flags2 And Not flag
			End If

			Return CType([Enum].ToObject(GetType(T), flags2), T)
		End Function

		Friend Function GetErrorEnum(ByVal errCode As UInt32) As String
			If [Enum].IsDefined(GetType(Errors), errCode) Then
				Return String.Format(If(errCode > &HFFFFUI, "{0} (0x{1:X8})", "{0} (0x{1:X4})"), "ERROR_" & DirectCast(errCode, Errors).ToString(), errCode)
			Else
				Return String.Format(If(errCode > &HFFFFUI, "{0} (0x{1:X8})", "{0} (0x{1:X4})"), "ERROR_UNKNOWN", errCode)
			End If
		End Function

		Friend Sub ShowException(ByVal ex As Exception)
			If TypeOf (ex) Is Win32Exception Then
				Dim e As UInt32 = GetUInt32(DirectCast(ex, Win32Exception).NativeErrorCode)
				MessageBox.Show(String.Format("Error code: {0}{1}{2}{1}{1}{3}", e.ToString(), CRLF, ex.Message, ex.StackTrace), "Win32Exception!")
			Else
				MessageBox.Show(ex.Message & CRLF & CRLF & If(ex.TargetSite IsNot Nothing, ex.TargetSite.Name, "<null>") & CRLF & CRLF & ex.Source & CRLF & CRLF & ex.StackTrace, "Exception!")
			End If
		End Sub

		Friend Function GetLastWin32Error() As Int32
			Return Marshal.GetLastWin32Error()
		End Function

		Friend Function GetLastWin32ErrorU() As UInt32
			Return GetUInt32(Marshal.GetLastWin32Error())
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

		Friend Class SYSTEMTIME
			Public wYear As UInt16
			Public wMonth As UInt16
			Public wDayOfWeek As UInt16
			Public wDay As UInt16
			Public wHour As UInt16
			Public wMinute As UInt16
			Public wSecond As UInt16
			Public wMilliseconds As UInt16

			Public Overrides Function ToString() As String
				Return String.Format("{0}/{1}/{2}  {3}:{4}:{5}", wDay.ToString(), wMonth.ToString(), wYear.ToString(), wHour.ToString(), wMinute.ToString(), wSecond.ToString)
			End Function
		End Class

	End Module
End Namespace
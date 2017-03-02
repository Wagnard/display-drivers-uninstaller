Imports System.Collections
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Security

Imports Display_Driver_Uninstaller.Win32.TaskScheduler

Namespace Win32

	' Windows SDK
	' -> OLE/COM Object Viewer -> GUID
	'
	' Task Scheduler 1.0 Interfaces (-> XP)
	' https://msdn.microsoft.com/en-us/library/windows/desktop/aa383581(v=vs.85).aspx
	'
	' Task Scheduler 2.0 Interfaces (Vista ->)
	' https://msdn.microsoft.com/en-us/library/windows/desktop/aa383600(v=vs.85).aspx

	Public Enum TaskStates
		Unknown = 0
		Disabled = 1
		Queued = 2
		Ready = 3
		Running = 4
	End Enum

	Public Class TaskSchedulerControl
		Private Const _rootFolder As String = "\"
		Private _taskScheduler As Version2.TaskScheduler = Nothing

		Public Sub New()
			If Application.Settings.WinVersion < OSVersion.WinVista Then
				MessageBox.Show("Not supported OS older than Vista! for now...", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error)
				Environment.Exit(0)
			End If

			_taskScheduler = New Version2.TaskScheduler()
			_taskScheduler.Connect()
		End Sub

		Friend Function GetAllTasks(Optional ByVal recursive As Boolean = True) As List(Of Task)
			Dim folders As New List(Of Version2.ITaskFolder)(10)

			If recursive Then
				GetFolders(_taskScheduler.GetFolder(_rootFolder), folders)
			Else
				folders.Add(_taskScheduler.GetFolder(_rootFolder))
			End If

			Dim tasks As New List(Of Task)(100)

			For Each folder As Version2.ITaskFolder In folders
				For Each task As Version2.IRegisteredTask In folder.GetTasks(0)
					tasks.Add(New TaskV2(folder, task))
				Next
			Next

			Return tasks
		End Function

		Private Sub GetFolders(ByVal folder As Version2.ITaskFolder, ByRef folders As List(Of Version2.ITaskFolder))
			folders.Add(folder)

			For Each i As Version2.ITaskFolder In folder.GetFolders(0)
				GetFolders(i, folders)
			Next
		End Sub

		Friend Function GetRunningTasks() As Version2.IRunningTaskCollection
			Return _taskScheduler.GetRunningTasks(0)
		End Function

	End Class

	Public MustInherit Class Task
		Public MustOverride ReadOnly Property Name As String
		Public MustOverride ReadOnly Property Path As String
		Public MustOverride ReadOnly Property State As TaskStates
		Public MustOverride Property Enabled As Boolean

		Public MustOverride ReadOnly Property Author As String
		Public MustOverride ReadOnly Property Description As String

		Public MustOverride Sub Delete()

		Public MustOverride Sub Start()

		Public MustOverride Sub [Stop]()
	End Class

	Public Class TaskV2
		Inherits Task

		Private _taskFolder As Version2.ITaskFolder = Nothing
		Private _task As Version2.IRegisteredTask = Nothing

		Friend Sub New(ByVal folder As Version2.ITaskFolder, ByVal task As Version2.IRegisteredTask)
			_taskFolder = folder
			_task = task
		End Sub

		Public Overrides ReadOnly Property Name As String
			Get
				If _task IsNot Nothing Then
					Return _task.Name
				Else
					Return Nothing
				End If
			End Get
		End Property

		Public Overrides ReadOnly Property Path As String
			Get
				If _task IsNot Nothing Then
					Return _task.Path
				Else
					Return Nothing
				End If
			End Get
		End Property

		Public Overrides ReadOnly Property State As TaskStates
			Get
				If _task IsNot Nothing AndAlso [Enum].IsDefined(GetType(TaskStates), CInt(_task.State)) Then
					Return CType(CInt(_task.State), TaskStates)
				Else
					Return TaskStates.Unknown
				End If
			End Get
		End Property

		Public Overrides Property Enabled As Boolean
			Get
				If _task IsNot Nothing Then
					Return _task.Enabled
				Else
					Return False
				End If
			End Get
			Set(value As Boolean)
				If _task IsNot Nothing Then
					_task.Enabled = value
				End If
			End Set
		End Property

		Public Overrides ReadOnly Property Author As String
			Get
				If _task IsNot Nothing AndAlso _task.Definition IsNot Nothing AndAlso _task.Definition.RegistrationInfo IsNot Nothing Then
					Return _task.Definition.RegistrationInfo.Author
				Else
					Return Nothing
				End If
			End Get
		End Property


		Public Overrides ReadOnly Property Description As String
			Get
				If _task IsNot Nothing AndAlso _task.Definition IsNot Nothing AndAlso _task.Definition.RegistrationInfo.Description IsNot Nothing Then
					Return _task.Definition.RegistrationInfo.Description
				Else
					Return Nothing
				End If
			End Get
		End Property

		Public Overrides Sub Delete()
			[Stop]()

			If _taskFolder IsNot Nothing AndAlso _task IsNot Nothing AndAlso Not String.IsNullOrEmpty(_task.Name) Then
				_taskFolder.DeleteTask(_task.Name, 0)
			End If
		End Sub

		Public Overrides Sub Start()
			If _task IsNot Nothing AndAlso _task.State <> TASK_STATE.RUNNING Then
				_task.Run(Nothing)
			End If
		End Sub

		Public Overrides Sub [Stop]()
			If _task IsNot Nothing Then
				_task.Stop(0)
			End If
		End Sub

	End Class

	Public Class TaskV1
		Inherits Task

		'Private _taskFolder As Version2.ITaskFolder = Nothing
		'Private _task As Version2.IRegisteredTask = Nothing

		'Friend Sub New(ByVal folder As Version2.ITaskFolder, ByVal task As Version2.IRegisteredTask)
		'_taskFolder = folder
		'_task = task
		'End Sub

		Public Overrides ReadOnly Property Name As String
			Get

			End Get
		End Property

		Public Overrides ReadOnly Property Path As String
			Get

			End Get
		End Property

		Public Overrides ReadOnly Property State As TaskStates
			Get

			End Get
		End Property

		Public Overrides Property Enabled As Boolean
			Get

			End Get
			Set(value As Boolean)

			End Set
		End Property

		Public Overrides ReadOnly Property Author As String
			Get

			End Get
		End Property

		Public Overrides ReadOnly Property Description As String
			Get

			End Get
		End Property

		Public Overrides Sub Delete()
			[Stop]()
		End Sub

		Public Overrides Sub Start()
		End Sub

		Public Overrides Sub [Stop]()
		End Sub

	End Class


End Namespace

Namespace Win32.TaskScheduler
	Friend Enum TASK_ACTION_TYPE As UInt32
		EXEC = 0UI
		COM_HANDLER = 5UI
		SEND_EMAIL = 6UI
		SHOW_MESSAGE = 7UI
	End Enum

	Friend Enum TASK_COMPATIBILITY As UInt32
		AT = 0UI
		V1 = 1UI
		V2 = 2UI
	End Enum

	<Flags()>
	Friend Enum TASK_CREATION As UInt32
		VALIDATE_ONLY = &H1UI
		CREATE = &H2UI
		UPDATE = &H4UI
		CREATE_OR_UPDATE = &H6UI
		DISABLE = &H8UI
		DONT_ADD_PRINCIPAL_ACE = &H10UI
		IGNORE_REGISTRATION_TRIGGERS = &H20UI
	End Enum

	Friend Enum TASK_ENUM_FLAGS As UInt32
		TASK_ENUM_HIDDEN = &H1UI
	End Enum

	Friend Enum TASK_INSTANCES_POLICY As UInt32
		PARALLEL = 0UI
		QUEUE = 1UI
		IGNORE_NEW = 2UI
		STOP_EXISTING = 3UI
	End Enum

	Friend Enum TASK_LOGON_TYPE As UInt32
		NONE = 0UI
		PASSWORD = 1UI
		S4U = 2UI
		INTERACTIVE_TOKEN = 3UI
		GROUP = 4UI
		SERVICE_ACCOUNT = 5UI
		INTERACTIVE_TOKEN_OR_PASSWORD = 6UI
	End Enum

	Friend Enum TASK_PROCESSTOKENSID_TYPE As UInt32
		NONE = 0UI
		UNRESTRICTED = 1UI
		[DEFAULT] = 2UI
	End Enum

	<Flags()>
	Friend Enum TASK_RUN_FLAGS As UInt32
		NO_FLAGS = &H0UI
		AS_SELF = &H1UI
		IGNORE_CONSTRAINTS = &H2UI
		USE_SESSION_ID = &H4UI
		USER_SID = &H8UI
	End Enum

	Friend Enum TASK_RUNLEVEL_TYPE As UInt32
		LUA = 0UI
		HIGHEST = 1UI
	End Enum

	Friend Enum TASK_SESSION_STATE_CHANGE_TYPE As UInt32
		CONSOLE_CONNECT = 1UI
		CONSOLE_DISCONNECT = 2UI
		REMOTE_CONNECT = 3UI
		REMOTE_DISCONNECT = 4UI
		SESSION_LOCK = 7UI
		SESSION_UNLOCK = 8UI
	End Enum

	Friend Enum TASK_STATE As UInt32
		UNKNOWN = 0UI
		DISABLED = 1UI
		QUEUED = 2UI
		READY = 3UI
		RUNNING = 4UI
	End Enum

	<Flags()>
	Friend Enum DaysOfWeek As Int16
		Sunday = &H1S
		Monday = &H2S
		Tuesday = &H4S
		Wednesday = &H8S
		Thursday = &H10S
		Friday = &H20S
		Saturday = &H40S
		AllDays = &H7FS
	End Enum

	<Flags()>
	Friend Enum MonthsOfYear As Int16
		January = &H1S
		February = &H2S
		March = &H4S
		April = &H8S
		May = &H10S
		June = &H20S
		July = &H40S
		August = &H80S
		Aeptember = &H100S
		October = &H200S
		November = &H400S
		December = &H800S
		AllMonths = &HFFFS
	End Enum

	<Flags()>
	Friend Enum WeeksOfMonth As Int16
		FirstWeek = &H1S
		SecondWeek = &H2S
		ThirdWeek = &H4S
		FourthWeek = &H8S
		LastWeek = &H10S
		AllWeeks = &H1FS
	End Enum

End Namespace

Namespace Win32.TaskScheduler.Version1
	Friend Enum TASK_TRIGGER_TYPE As UInt32
		ONCE = 0UI
		DAILY = 1UI
		WEEKLY = 2UI
		MONTHLYDATE = 3UI
		MONTHLYDOW = 4UI
		ON_IDLE = 5UI
		AT_SYSTEMSTART = 6UI
		AT_LOGON = 7UI
	End Enum

	Friend Enum TASKPAGE As UInt16
		TASKPAGE_TASK = 0US
		TASKPAGE_SCHEDULE = 1US
		TASKPAGE_SETTINGS = 2US
	End Enum

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380706(v=vs.85).aspx</remarks>
	'<Guid("?"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	'Friend Interface IEnumWorkItems
	'End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381311(v=vs.85).aspx</remarks>
	'<Guid("?"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITask
		Inherits IScheduledWorkItem

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381811(v=vs.85).aspx</remarks>
	'<Guid("?"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	'Friend Interface ITaskScheduler
	'End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381864(v=vs.85).aspx</remarks>
	'<Guid("?"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	'Friend Interface ITaskTrigger
	'End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381216(v=vs.85).aspx</remarks>
	'<Guid("?"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface IScheduledWorkItem
	End Interface

	'<ComImport(), Guid("?"), SuppressUnmanagedCodeSecurity()>
	'Friend Class CLSID_Ctask
	'End Class

	'<ComImport(), Guid("?"), SuppressUnmanagedCodeSecurity()>
	'Friend Class CLSID_CTaskScheduler
	'End Class

End Namespace

Namespace Win32.TaskScheduler.Version2
	Friend Enum TASK_TRIGGER_TYPE As UInt32
		[EVENT] = 0UI
		TIME = 1UI
		DAILY = 2UI
		WEEKLY = 3UI
		MONTHLY = 4UI
		MONTHLYDOW = 5UI
		IDLE = 6UI
		REGISTRATION = 7UI
		BOOT = 8UI
		LOGON = 9UI
		SESSION_STATE_CHANGE = 11UI
		CUSTOM = 12UI
	End Enum

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa446895(v=vs.85).aspx</remarks>
	<ComImport(), Guid("BAE54997-48B1-4CBE-9965-D6BE263EBEA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IAction
		Property Id As <MarshalAs(UnmanagedType.BStr)> String

		ReadOnly Property Type As TASK_ACTION_TYPE
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa446896(v=vs.85).aspx</remarks>
	<ComImport(), Guid("02820E19-7B98-4ED2-B2E8-FDCCCEFF619B"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IActionCollection
		Inherits IEnumerable

		Shadows Function GetEnumerator() As <MarshalAs(UnmanagedType.[Interface])> IEnumerator

		ReadOnly Property Count As Int32

		Default ReadOnly Property Item(<[In]()> ByVal index As Int32) As IAction

		Property XmlText As <MarshalAs(UnmanagedType.BStr)> String

		Function Create(<[In]()> ByVal type As TASK_ACTION_TYPE) As <MarshalAs(UnmanagedType.[Interface])> IAction

		Sub Remove(<[In](), MarshalAs(UnmanagedType.Struct)> ByVal index As Object)

		Sub Clear()

		Property Context As <MarshalAs(UnmanagedType.BStr)> String
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380719(v=vs.85).aspx</remarks>
	<ComImport(), Guid("84594461-0053-4342-A8FD-088FABF11F32"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IIdleSettings
		Property IdleDuration As <MarshalAs(UnmanagedType.BStr)> String

		Property WaitTimeout As <MarshalAs(UnmanagedType.BStr)> String

		Property StopOnIdleEnd As Boolean

		Property RestartOnIdle As Boolean
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380724(v=vs.85).aspx</remarks>
	<ComImport(), Guid("D537D2B0-9FB3-4D34-9739-1FF5CE7B1EF3"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IIdleTrigger
		Inherits ITrigger

		Shadows Property EndBoundary As <MarshalAs(UnmanagedType.BStr)> String

		Shadows Property ExecutionTimeLimit As <MarshalAs(UnmanagedType.BStr)> String

		Shadows Property Id As <MarshalAs(UnmanagedType.BStr)> String

		Shadows Property Enabled As Boolean

		Shadows Property Repetition As <MarshalAs(UnmanagedType.Interface)> IRepetitionPattern

		Shadows Property StartBoundary As <MarshalAs(UnmanagedType.BStr)> String

		Shadows ReadOnly Property Type As TASK_TRIGGER_TYPE
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380739(v=vs.85).aspx</remarks>
	<ComImport(), Guid("9F7DEA84-C30B-4245-80B6-00E9F646F1B4"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface INetworkSettings
		Property Name As <MarshalAs(UnmanagedType.BStr)> String

		Property Id As <MarshalAs(UnmanagedType.BStr)> String
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380742(v=vs.85).aspx</remarks>
	<ComImport(), Guid("D98D51E5-C9B4-496A-A9C1-18980261CF0F"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IPrincipal
		Property DisplayName As <MarshalAs(UnmanagedType.BStr)> String

		Property GroupId As <MarshalAs(UnmanagedType.BStr)> String

		Property Id As <MarshalAs(UnmanagedType.BStr)> String

		Property LogonType As TASK_LOGON_TYPE

		Property RunLevel As TASK_RUNLEVEL_TYPE

		Property UserId As <MarshalAs(UnmanagedType.BStr)> String
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380751(v=vs.85).aspx</remarks>
	<ComImport(), Guid("9C86F320-DEE3-4DD1-B972-A303F26B061E"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity(), DefaultMember("Path")>
	Friend Interface IRegisteredTask
		ReadOnly Property Name As <MarshalAs(UnmanagedType.BStr)> String

		ReadOnly Property Path As <MarshalAs(UnmanagedType.BStr)> String

		ReadOnly Property State As TASK_STATE

		Property Enabled() As Boolean

		Function Run(<[In](), MarshalAs(UnmanagedType.Struct)> ByVal parameters As Object) As <MarshalAs(UnmanagedType.[Interface])> IRunningTask

		Function RunEx(
		  <[In](), MarshalAs(UnmanagedType.Struct)> ByVal parameters As Object,
		   <[In]()> ByVal flags As Int32,
		   <[In]()> ByVal sessionID As Int32,
		   <[In](), MarshalAs(UnmanagedType.BStr)> ByVal user As String) As <MarshalAs(UnmanagedType.[Interface])> IRunningTask

		Function GetInstances(<[In]()> ByVal flags As Int32) As <MarshalAs(UnmanagedType.[Interface])> IRunningTaskCollection

		ReadOnly Property LastRunTime As DateTime

		ReadOnly Property LastTaskResult As Int32

		ReadOnly Property NumberOfMissedRuns As Int32

		ReadOnly Property NextRunTime As DateTime

		ReadOnly Property Definition As <MarshalAs(UnmanagedType.Interface)> ITaskDefinition

		ReadOnly Property Xml As <MarshalAs(UnmanagedType.BStr)> String

		Function GetSecurityDescriptor(<[In]()> ByVal securityInformation As Int32) As <MarshalAs(UnmanagedType.BStr)> String

		Sub SetSecurityDescriptor(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal sddl As String, <[In]()> ByVal flags As Int32)

		Sub [Stop](<[In]()> ByVal flags As Int32)

		<DispId(&H60020011), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Sub GetRunTimes(
   <[In]()> ByRef pstStart As SYSTEMTIME,
   <[In]()> ByRef pstEnd As SYSTEMTIME,
   <[In](), Out()> ByRef pCount As UInt32,
   <[In](), Out()> ByRef pRunTimes As IntPtr)

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380752(v=vs.85).aspx</remarks>
	<ComImport(), Guid("86627EB4-42A7-41E4-A4D9-AC33A72F2D52"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IRegisteredTaskCollection
		Inherits IEnumerable

		ReadOnly Property Count As Int32

		Default ReadOnly Property Item(<[In]()> ByVal index As Object) As <MarshalAs(UnmanagedType.Interface)> IRegisteredTask

		Shadows Function GetEnumerator() As <MarshalAs(UnmanagedType.[Interface])> IEnumerator
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380773(v=vs.85).aspx</remarks>
	<ComImport(), Guid("416D8B73-CB41-4EA1-805C-9BE9A5AC4A74"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IRegistrationInfo
		Property Description As <MarshalAs(UnmanagedType.BStr)> String

		Property Author As <MarshalAs(UnmanagedType.BStr)> String

		Property Version As <MarshalAs(UnmanagedType.BStr)> String

		Property [Date] As <MarshalAs(UnmanagedType.BStr)> String

		Property Documentation As <MarshalAs(UnmanagedType.BStr)> String

		Property XmlText As <MarshalAs(UnmanagedType.BStr)> String

		Property URI As <MarshalAs(UnmanagedType.BStr)> String

		Property SecurityDescriptor As Object

		Property Source As <MarshalAs(UnmanagedType.BStr)> String
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381128(v=vs.85).aspx</remarks>
	<ComImport(), Guid("7FB9ACF1-26BE-400E-85B5-294B9C75DFD6"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IRepetitionPattern
		Property Interval As <MarshalAs(UnmanagedType.BStr)> String

		Property Duration As <MarshalAs(UnmanagedType.BStr)> String

		Property StopAtDurationEnd As Boolean
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381157(v=vs.85).aspx</remarks>
	<ComImport(), Guid("653758FB-7B9A-4F1E-A471-BEEB8E9B834E"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity(), DefaultMember("InstanceGuid")>
	Friend Interface IRunningTask
		ReadOnly Property Name As <MarshalAs(UnmanagedType.BStr)> String

		ReadOnly Property InstanceGuid As <MarshalAs(UnmanagedType.BStr)> String

		ReadOnly Property Path As String

		ReadOnly Property State As TASK_STATE

		ReadOnly Property CurrentAction As <MarshalAs(UnmanagedType.BStr)> String

		Sub [Stop]()

		Sub Refresh()

		ReadOnly Property EnginePID As UInt32
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381166(v=vs.85).aspx</remarks>
	<ComImport(), Guid("6A67614B-6828-4FEC-AA54-6D52E8F1F2DB"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface IRunningTaskCollection
		Inherits IEnumerable

		ReadOnly Property Count As Int32

		Default ReadOnly Property Item(<[In]()> ByVal index As Object) As <MarshalAs(UnmanagedType.Interface)> IRunningTask

		Shadows Function GetEnumerator() As <MarshalAs(UnmanagedType.[Interface])> IEnumerator
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381313(v=vs.85).aspx</remarks>
	<ComImport(), Guid("F5BC8FC5-536D-4F77-B852-FBC1356FDEB6"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITaskDefinition
		Property RegistrationInfo As <MarshalAs(UnmanagedType.Interface)> IRegistrationInfo

		Property Triggers As <MarshalAs(UnmanagedType.Interface)> ITriggerCollection

		Property Settings As <MarshalAs(UnmanagedType.Interface)> ITaskSettings

		Property Data As <MarshalAs(UnmanagedType.BStr)> String

		Property Principal As <MarshalAs(UnmanagedType.Interface)> IPrincipal

		Property Actions As <MarshalAs(UnmanagedType.Interface)> IActionCollection

		Property XmlText As <MarshalAs(UnmanagedType.BStr)> String
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381330(v=vs.85).aspx</remarks>
	<ComImport(), Guid("8CFAC062-A080-4C15-9A88-AA7C2AF80DFC"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity(), DefaultMember("Path")>
	Friend Interface ITaskFolder
		ReadOnly Property Name As <MarshalAs(UnmanagedType.BStr)> String
		ReadOnly Property Path As <MarshalAs(UnmanagedType.BStr)> String

		Function GetFolder(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String) As <MarshalAs(UnmanagedType.[Interface])> ITaskFolder

		Function GetFolders(<[In]()> ByVal flags As Int32) As <MarshalAs(UnmanagedType.[Interface])> ITaskFolderCollection

		Function CreateFolder(
		   <[In](), MarshalAs(UnmanagedType.BStr)> ByVal subFolderName As String,
		   <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> ByVal sddl As Object) As <MarshalAs(UnmanagedType.[Interface])> ITaskFolder

		Sub DeleteFolder(
		 <[In](), MarshalAs(UnmanagedType.BStr)> ByVal subFolderName As String,
		  <[In]()> ByVal flags As Int32)

		Function GetTask(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String) As <MarshalAs(UnmanagedType.[Interface])> IRegisteredTask

		Function GetTasks(<[In]()> ByVal flags As Int32) As <MarshalAs(UnmanagedType.[Interface])> IRegisteredTaskCollection

		Sub DeleteTask(
		  <[In](), MarshalAs(UnmanagedType.BStr)> ByVal Name As String,
		  <[In]()> ByVal flags As Int32)

		Function RegisterTask(
		   <[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String,
		   <[In](), MarshalAs(UnmanagedType.BStr)> ByVal XmlText As String,
		   <[In]()> ByVal flags As Int32,
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal UserId As Object,
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal password As Object,
		   <[In]()> ByVal LogonType As TASK_LOGON_TYPE,
		   <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> ByVal sddl As Object) As <MarshalAs(UnmanagedType.[Interface])> IRegisteredTask

		Function RegisterTaskDefinition(
		 <[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String,
		 <[In](), MarshalAs(UnmanagedType.[Interface])> ByVal pDefinition As ITaskDefinition,
		 <[In]()> ByVal flags As Int32,
		 <[In](), MarshalAs(UnmanagedType.Struct)> ByVal UserId As Object,
		 <[In](), MarshalAs(UnmanagedType.Struct)> ByVal password As Object,
		 <[In]()> ByVal LogonType As TASK_LOGON_TYPE,
		 <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> ByVal sddl As Object) As <MarshalAs(UnmanagedType.[Interface])> IRegisteredTask

		Function GetSecurityDescriptor(<[In]()> ByVal securityInformation As Int32) As <MarshalAs(UnmanagedType.BStr)> String

		Sub SetSecurityDescriptor(
		 <[In](), MarshalAs(UnmanagedType.BStr)> ByVal sddl As String,
		 <[In]()> ByVal flags As Int32)
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381332(v=vs.85).aspx</remarks>
	<ComImport(), Guid("79184A66-8664-423F-97F1-637356A5D812"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITaskFolderCollection
		Inherits IEnumerable

		ReadOnly Property Count As Int32

		Default ReadOnly Property Item(<[In]()> ByVal index As Object) As <MarshalAs(UnmanagedType.Interface)> ITaskFolder

		Shadows Function GetEnumerator() As <MarshalAs(UnmanagedType.[Interface])> IEnumerator
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381832(v=vs.85).aspx</remarks>
	<ComImport(), Guid("2FABA4C7-4DA9-4013-9697-20CC3FD40F85"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity(), TypeLibType(&H10C0S), DefaultMember("TargetServer")>
	Friend Interface ITaskService
		<DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Function GetFolder(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String) As <MarshalAs(UnmanagedType.[Interface])> ITaskFolder

		<DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Function GetRunningTasks(<[In]()> ByVal flags As Int32) As <MarshalAs(UnmanagedType.[Interface])> IRunningTaskCollection

		<DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Function NewTask(<[In]()> ByVal flags As UInt32) As <MarshalAs(UnmanagedType.[Interface])> ITaskDefinition

		<DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Sub Connect(
 <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal serverName As Object = Nothing,
 <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal user As Object = Nothing,
 <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal domain As Object = Nothing,
 <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal password As Object = Nothing)

		<DispId(5)>
		Property Connected As Boolean

		<DispId(0)>
		Property TargetServer As <MarshalAs(UnmanagedType.BStr)> String

		<DispId(6)>
		Property ConnectedUser As <MarshalAs(UnmanagedType.BStr)> String

		<DispId(7)>
		Property ConnectedDomain As <MarshalAs(UnmanagedType.BStr)> String

		<DispId(8)>
		Property HighestVersion As UInt32
	End Interface

	''' <remarks></remarks>
	<ComImport(), Guid("2FABA4C7-4DA9-4013-9697-20CC3FD40F85"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity(), CoClass(GetType(TaskSchedulerClass))>
	Friend Interface TaskScheduler
		Inherits ITaskService

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381843(v=vs.85).aspx</remarks>
	<ComImport(), Guid("8FD4711D-2D02-4C8C-87E3-EFF699DE127E"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITaskSettings
		Property AllowDemandStart As Boolean

		Property RestartInterval As <MarshalAs(UnmanagedType.BStr)> String

		Property RestartCount As Int32

		Property MultipleInstances As TASK_INSTANCES_POLICY

		Property StopIfGoingOnBatteries As Boolean

		Property DisallowStartIfOnBatteries As Boolean

		Property AllowHardTerminate As Boolean

		Property StartWhenAvailable As Boolean

		Property XmlText As <MarshalAs(UnmanagedType.BStr)> String

		Property RunOnlyIfNetworkAvailable As Boolean

		Property ExecutionTimeLimit As <MarshalAs(UnmanagedType.BStr)> String

		Property Enabled As Boolean

		Property DeleteExpiredTaskAfter As <MarshalAs(UnmanagedType.BStr)> String

		Property Priority As Int32

		Property Compatibility As TASK_COMPATIBILITY

		Property Hidden As Boolean

		Property IdleSettings As <MarshalAs(UnmanagedType.Interface)> IIdleSettings

		Property RunOnlyIfIdle As Boolean

		Property WakeToRun As Boolean

		Property NetworkSettings As <MarshalAs(UnmanagedType.Interface)> INetworkSettings
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381887(v=vs.85).aspx</remarks>
	<ComImport(), Guid("09941815-EA89-4B5B-89E0-2A773801FAC3"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITrigger
		ReadOnly Property Type As TASK_TRIGGER_TYPE

		Property Id As <MarshalAs(UnmanagedType.BStr)> String

		Property Repetition As <MarshalAs(UnmanagedType.Interface)> IRepetitionPattern

		Property ExecutionTimeLimit As <MarshalAs(UnmanagedType.BStr)> String

		Property StartBoundary As <MarshalAs(UnmanagedType.BStr)> String

		Property EndBoundary As <MarshalAs(UnmanagedType.BStr)> String

		Property Enabled As Boolean
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381889(v=vs.85).aspx</remarks>
	<ComImport(), Guid("85DF5081-1B24-4F32-878A-D9D14DF4CB77"), InterfaceType(ComInterfaceType.InterfaceIsDual), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITriggerCollection
		Inherits IEnumerable

		ReadOnly Property Count As Int32

		Default ReadOnly Property Item(<[In]()> ByVal index As Int32) As ITrigger

		Shadows Function GetEnumerator() As <MarshalAs(UnmanagedType.[Interface])> IEnumerator

		Function Create(<[In]()> ByVal type As TASK_TRIGGER_TYPE) As <MarshalAs(UnmanagedType.[Interface])> ITrigger

		Sub Remove(<[In](), MarshalAs(UnmanagedType.Struct)> ByVal index As Object)

		Sub Clear()
	End Interface

	<ComImport(), Guid("0F87369F-A4E5-4CFC-BD3E-73E6154572DD"), TypeLibType(2S), ClassInterface(0S), SuppressUnmanagedCodeSecurity(), DefaultMember("TargetServer")>
	Friend Class TaskSchedulerClass
		Implements TaskScheduler

		<DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Public Overridable Function GetFolder(
   <[In](), MarshalAs(UnmanagedType.BStr)> ByVal Path As String) As <MarshalAs(UnmanagedType.[Interface])> ITaskFolder Implements ITaskService.GetFolder
		End Function

		<DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Public Overridable Function GetRunningTasks(<[In]()> ByVal flags As Int32) As <MarshalAs(UnmanagedType.[Interface])> IRunningTaskCollection Implements ITaskService.GetRunningTasks
		End Function

		<DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Public Overridable Function NewTask(<[In]()> ByVal flags As UInt32) As <MarshalAs(UnmanagedType.[Interface])> ITaskDefinition Implements ITaskService.NewTask
		End Function

		<DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)>
		Public Overridable Sub Connect(
  <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal serverName As Object = Nothing,
  <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal user As Object = Nothing,
  <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal domain As Object = Nothing,
  <[In](), [Optional](), MarshalAs(UnmanagedType.Struct)> Optional ByVal password As Object = Nothing) Implements ITaskService.Connect
		End Sub


		<DispId(5)> Public Overridable Property Connected As Boolean Implements ITaskService.Connected

		<DispId(7)> Public Overridable Property ConnectedDomain As <MarshalAs(UnmanagedType.BStr)> String Implements ITaskService.ConnectedDomain

		<DispId(6)> Public Overridable Property ConnectedUser As <MarshalAs(UnmanagedType.BStr)> String Implements ITaskService.ConnectedUser

		<DispId(8)> Public Overridable Property HighestVersion As UInt32 Implements ITaskService.HighestVersion

		<DispId(0)> Public Overridable Property TargetServer As <MarshalAs(UnmanagedType.BStr)> String Implements ITaskService.TargetServer
	End Class

End Namespace
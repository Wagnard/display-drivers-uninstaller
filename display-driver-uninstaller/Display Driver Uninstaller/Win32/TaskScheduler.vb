Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Security

Imports Display_Driver_Uninstaller.Win32.TaskScheduler

Namespace Display_Driver_Uninstaller.Win32

	' Windows SDK
	' -> OLE/COM Object Viewer -> GUID
	'
	' Task Scheduler 1.0 Interfaces (-> XP)
	' https://msdn.microsoft.com/en-us/library/windows/desktop/aa383581(v=vs.85).aspx
	'
	' Task Scheduler 2.0 Interfaces (Vista ->)
	' https://msdn.microsoft.com/en-us/library/windows/desktop/aa383600(v=vs.85).aspx

	Public Enum TaskStates As Integer
		Unknown = 0
		Disabled = 1
		Queued = 2
		Ready = 3
		Running = 4
	End Enum

	Public Class TaskSchedulerControl
		Implements IDisposable

		Friend Const MaxWaits As Int32 = 500      ' 500 * 10ms = 5sec MAX  (takes less than 1 ms usually) || process will stay running after deletion of task if not waited)
		Private Const _rootFolder As String = "\"
		Private ReadOnly _useV2 As Boolean = True
		Friend Shared ReadOnly iTaskGuid As Guid = Marshal.GenerateGuidForType(GetType(Version1.ITask))

		Private _disposed As Boolean
		Private ReadOnly _taskSchedulerV1 As Version1.ITaskScheduler = Nothing
		Private ReadOnly _taskSchedulerV2 As Version2.TaskScheduler = Nothing

		Public Sub New(ByVal config As ThreadSettings)
			_useV2 = (config.WinVersion >= OSVersion.WinVista)

			Try
				If _useV2 Then
					_taskSchedulerV2 = New Version2.TaskScheduler()
					_taskSchedulerV2.Connect()
				Else
					_taskSchedulerV1 = CType(New Version1.CTaskScheduler(), Version1.ITaskScheduler)
					_taskSchedulerV1.SetTargetComputer(Nothing)
				End If
			Catch ex As Exception
				Dim logEntry As LogEntry = Application.Log.CreateEntry(ex)
				logEntry.Type = LogType.Error
				logEntry.Add("_useV2", _useV2.ToString())
				logEntry.Add("config.WinVersion", config.WinVersion.ToString())

				Application.Log.Add(logEntry)
			End Try
		End Sub

		Friend Function GetAllTasks() As List(Of Task)
			Dim tasks As New List(Of Task)(100)

			Try
				If _useV2 Then
					Dim folders As New List(Of Version2.ITaskFolder)(10)

					GetFolders(_taskSchedulerV2.GetFolder(_rootFolder), folders)

					Dim logEntry As LogEntry = Application.Log.CreateEntry()
					logEntry.Add("_useV2", _useV2.ToString())
					logEntry.Add("Failed paths:")

					For Each folder As Version2.ITaskFolder In folders
						Try
							For Each task As Version2.IRegisteredTask In folder.GetTasks(TASK_ENUM_FLAGS.HIDDEN)
								tasks.Add(New TaskV2(folder, task))
							Next
						Catch ex As Exception
							If Not logEntry.HasException Then
								logEntry.AddException(ex, False)
							End If

							logEntry.Add("> " & folder.Path)
						End Try
					Next

					If logEntry.Values.Count > 2 Then
						logEntry.Message = "Failed to get tasks from some paths!"
						logEntry.Type = LogType.Error

						Application.Log.Add(logEntry)
					End If

					Return tasks
				Else
					Dim task As Version1.ITask
					Dim ptrJob As IntPtr = IntPtr.Zero
					Dim i As UInt32 = 0UI
					Dim errCode As UInt32

					Dim workItems As Version1.IEnumWorkItems = _taskSchedulerV1.Enum()

					If workItems IsNot Nothing Then
						While True
							Try
								errCode = workItems.Next(1UI, ptrJob, i)

								If errCode = 1UI OrElse i <> 1UI Then
									Exit While
								End If

								Dim jobName As String = Nothing

								Using coMemStr As Version1.CoTaskMemStr = New Version1.CoTaskMemStr(Marshal.ReadIntPtr(ptrJob))
									jobName = coMemStr.ToString()
								End Using

								If jobName.EndsWith(".job", StringComparison.InvariantCultureIgnoreCase) Then
									jobName = jobName.Substring(0, jobName.Length - 4)
								End If


								task = TaskV1.ReActivate(_taskSchedulerV1, jobName)

								If task IsNot Nothing Then
									tasks.Add(New TaskV1(_taskSchedulerV1, task))
								End If
							Finally
								If ptrJob <> IntPtr.Zero Then
									Marshal.FreeCoTaskMem(ptrJob)
									ptrJob = IntPtr.Zero
								End If
							End Try
						End While
					End If
				End If
			Catch ex As Exception
				Dim logEntry As LogEntry = Application.Log.CreateEntry(ex)
				logEntry.Type = LogType.Error
				logEntry.Add("_useV2", _useV2.ToString())

				Application.Log.Add(logEntry)
			End Try

			Return tasks
		End Function

		Private Sub GetFolders(ByVal folder As Version2.ITaskFolder, ByRef folders As List(Of Version2.ITaskFolder))
			folders.Add(folder)

			For Each i As Version2.ITaskFolder In folder.GetFolders(0)
				GetFolders(i, folders)
			Next
		End Sub



		Protected Overridable Sub Dispose(disposing As Boolean)
			If Not Me._disposed Then
				If disposing Then

				End If

				If _taskSchedulerV1 IsNot Nothing Then
					Marshal.ReleaseComObject(_taskSchedulerV1)
				End If

				If _taskSchedulerV2 IsNot Nothing Then
					Marshal.ReleaseComObject(_taskSchedulerV2)
				End If
			End If

			Me._disposed = True
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

		Protected Sub WaitForTermination()
			[Stop]()

			Dim waits As Int32 = 0

			While State = TaskStates.Running
				System.Threading.Thread.Sleep(10)

				waits += 1

				If (waits >= TaskSchedulerControl.MaxWaits) Then
					Exit While
				End If
			End While
		End Sub
	End Class

	Public Class TaskV2
		Inherits Task

		Private ReadOnly _taskFolder As Version2.ITaskFolder = Nothing
		Private ReadOnly _task As Version2.IRegisteredTask = Nothing

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
			If _taskFolder IsNot Nothing AndAlso _task IsNot Nothing AndAlso Not String.IsNullOrEmpty(_task.Name) Then
				[Stop]()

				WaitForTermination()

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

		Private ReadOnly _taskScheduler As Version1.ITaskScheduler = Nothing
		Private _task As Version1.ITask = Nothing

		Friend Sub New(ByVal taskScheduler As Version1.ITaskScheduler, ByVal task As Version1.ITask)
			_taskScheduler = taskScheduler
			_task = task
		End Sub

		Public Overrides ReadOnly Property Name As String
			Get
				If _task IsNot Nothing Then
					Dim filePath As String = Nothing

					CType(_task, ComTypes.IPersistFile).GetCurFile(filePath)

					Return System.IO.Path.GetFileNameWithoutExtension(filePath)
				End If

				Return Nothing
			End Get
		End Property

		Public Overrides ReadOnly Property Path As String
			Get
				If _task IsNot Nothing Then
					Dim filePath As String = Nothing

					CType(_task, ComTypes.IPersistFile).GetCurFile(filePath)

					Return filePath
				End If

				Return Nothing
			End Get
		End Property

		Public Overrides ReadOnly Property State As TaskStates
			Get
				If _task Is Nothing Then
					Return TaskStates.Unknown
				End If

				Dim task As Version1.ITask

				Try
					task = _taskScheduler.Activate(Name, TaskSchedulerControl.iTaskGuid)
				Catch ex As ArgumentException
					task = _taskScheduler.Activate(Name & ".job", TaskSchedulerControl.iTaskGuid)
				End Try

				If task IsNot Nothing Then
					_task = task
				End If

				Dim status As UInt32 = _task.GetStatus()

				Select Case status
					Case Errors.SCHED_S_TASK_DISABLED
						Return TaskStates.Disabled

					Case Errors.SCHED_S_TASK_QUEUED
						Return TaskStates.Queued

					Case Errors.SCHED_S_TASK_HAS_NOT_RUN,
					 Errors.SCHED_S_TASK_NO_MORE_RUNS,
					 Errors.SCHED_S_TASK_NOT_SCHEDULED,
					 Errors.SCHED_S_TASK_TERMINATED,
					 Errors.SCHED_S_TASK_READY
						Return TaskStates.Ready

					Case Errors.SCHED_S_TASK_RUNNING
						Return TaskStates.Running

					Case Errors.SCHED_S_TASK_NO_VALID_TRIGGERS
						Return TaskStates.Unknown
					Case Else
						Return TaskStates.Unknown
				End Select
			End Get
		End Property

		Public Overrides Property Enabled As Boolean
			Get
				If _task IsNot Nothing Then
					Return Not HasFlag(_task.GetFlags(), Version1.TASK_FLAG.DISABLED)
				End If

				Return False
			End Get
			Set(value As Boolean)
				If _task IsNot Nothing Then
					_task.SetFlags(SetFlag(Of Version1.TASK_FLAG)(_task.GetFlags(), Version1.TASK_FLAG.DISABLED, Not value))

					SaveToFile(Name)
				End If
			End Set
		End Property

		Public Overrides ReadOnly Property Author As String
			Get
				If _task IsNot Nothing Then
					Return If(_task.GetCreator(), Nothing)
				End If

				Return Nothing
			End Get
		End Property

		Public Overrides ReadOnly Property Description As String
			Get
				If _task IsNot Nothing Then
					Return If(_task.GetComment(), Nothing)
				End If

				Return Nothing
			End Get
		End Property

		Public Overrides Sub Delete()
			If _task IsNot Nothing Then
				[Stop]()

				WaitForTermination()

				_taskScheduler.Delete(Name)
			End If
		End Sub

		Public Overrides Sub Start()
			If _task IsNot Nothing AndAlso State <> TaskStates.Running Then
				_task.Run()
			End If
		End Sub

		Public Overrides Sub [Stop]()
			If _task IsNot Nothing AndAlso State = TaskStates.Running Then
				_task.Terminate()
			End If
		End Sub

		Friend Shared Function ReActivate(ByVal taskScheduler As Version1.ITaskScheduler, ByVal name As String) As Version1.ITask
			Dim newTask As Version1.ITask

			Try
				newTask = taskScheduler.Activate(name, TaskSchedulerControl.iTaskGuid)
			Catch ex As ArgumentException
				newTask = taskScheduler.Activate(name & ".job", TaskSchedulerControl.iTaskGuid)
			End Try

			If newTask IsNot Nothing Then
				Return newTask
			End If

			Return Nothing
		End Function

		Friend Sub SaveToFile(ByVal fileName As String)
			If _task IsNot Nothing Then
				Dim iPersistFile As ComTypes.IPersistFile = CType(_task, ComTypes.IPersistFile)

				If String.IsNullOrEmpty(fileName) OrElse fileName = Name Then
					Try
						iPersistFile.Save(Nothing, False)
						iPersistFile = Nothing
						Return
					Catch ex As Exception
					End Try
				End If

				Dim curFile As String = Nothing
				iPersistFile.GetCurFile(curFile)

				If curFile IsNot Nothing Then
					IO.File.Delete(curFile)
				End If

				curFile = IO.Path.GetDirectoryName(curFile) + IO.Path.DirectorySeparatorChar.ToString() + fileName + IO.Path.GetExtension(curFile)

				IO.File.Delete(curFile)

				iPersistFile.Save(curFile, True)
				iPersistFile = Nothing
			End If
		End Sub

	End Class


End Namespace

Namespace Display_Driver_Uninstaller.Win32.TaskScheduler
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

	Friend Enum TASK_ENUM_FLAGS As Integer
		HIDDEN = &H1
	End Enum


End Namespace

Namespace Display_Driver_Uninstaller.Win32.TaskScheduler.Version1
	' GUID @ MSTask.Idl
	'  mstask.h

	Friend Enum TASK_TRIGGER_TYPE As UInt32
		TIME_TRIGGER_ONCE = 0UI
		TIME_TRIGGER_DAILY = 1UI
		TIME_TRIGGER_WEEKLY = 2UI
		TIME_TRIGGER_MONTHLYDATE = 3UI
		TIME_TRIGGER_MONTHLYDOW = 4UI
		EVENT_TRIGGER_ON_IDLE = 5UI
		EVENT_TRIGGER_AT_SYSTEMSTART = 6UI
		EVENT_TRIGGER_AT_LOGON = 7UI
	End Enum

	Friend Enum TASKPAGE As UInt16
		TASKPAGE_TASK = 0US
		TASKPAGE_SCHEDULE = 1US
		TASKPAGE_SETTINGS = 2US
	End Enum

	Friend Enum TASK_FLAG As UInt32
		INTERACTIVE = &H1UI
		DELETE_WHEN_DONE = &H2UI
		DISABLED = &H4UI
		START_ONLY_IF_IDLE = &H10UI
		KILL_ON_IDLE_END = &H20UI
		DONT_START_IF_ON_BATTERIES = &H40UI
		KILL_IF_GOING_ON_BATTERIES = &H80UI
		RUN_ONLY_IF_DOCKED = &H100UI
		HIDDEN = &H200UI
		RUN_IF_CONNECTED_TO_INTERNET = &H400UI
		RESTART_ON_IDLE_RESUME = &H800UI
		SYSTEM_REQUIRED = &H1000UI
		RUN_ONLY_IF_LOGGED_ON = &H2000UI
	End Enum

	Friend Enum TASK_TRIGGER_FLAG As UInt16
		HAS_END_DATE = &H1US
		KILL_AT_DURATION_END = &H2US
		DISABLED = &H4US
	End Enum

	Friend Enum WEEKS As UInt16
		TASK_FIRST_WEEK = 1US
		TASK_SECOND_WEEK = 2US
		TASK_THIRD_WEEK = 3US
		TASK_FOURTH_WEEK = 4US
		TASK_LAST_WEEK = 5US
	End Enum

	Friend Enum DAYS As UInt16
		TASK_SUNDAY = &H1US
		TASK_MONDAY = &H2US
		TASK_TUESDAY = &H4US
		TASK_WEDNESDAY = &H8US
		TASK_THURSDAY = &H10US
		TASK_FRIDAY = &H20US
		TASK_SATURDAY = &H40US
	End Enum

	Friend Enum MONTHS As UInt16
		TASK_JANUARY = &H1US
		TASK_FEBRUARY = &H2US
		TASK_MARCH = &H4US
		TASK_APRIL = &H8US
		TASK_MAY = &H10US
		TASK_JUNE = &H20US
		TASK_JULY = &H40US
		TASK_AUGUST = &H80US
		TASK_SEPTEMBER = &H100US
		TASK_OCTOBER = &H200US
		TASK_NOVEMBER = &H400US
		TASK_DECEMBER = &H800US
	End Enum

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure DAILY
		Public DaysInterval As UInt16
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure WEEKLY
		Public WeeksInterval As UInt16
		Public rgfDaysOfTheWeek As UInt16
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure MONTHLYDATE
		Public rgfDays As UInt32
		Public rgfMonths As UInt16
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure MONTHLYDOW
		Public wWhichWeek As UInt16
		Public rgfDaysOfTheWeek As UInt16
		Public rgfMonths As UInt16
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure TASK_TRIGGER
		Public cbTriggerSize As UInt16
		Public Reserved1 As UInt16
		Public wBeginYear As UInt16
		Public wBeginMonth As UInt16
		Public wBeginDay As UInt16
		Public wEndYear As UInt16
		Public wEndMonth As UInt16
		Public wEndDay As UInt16
		Public wStartHour As UInt16
		Public wStartMinute As UInt16
		Public MinutesDuration As UInt32
		Public MinutesInterval As UInt32
		Public rgFlags As UInt32
		Public TriggerType As TASK_TRIGGER_TYPE
		Public Type As UInt16
		Public Reserved2 As UInt16
		Public wRandomMinutesInterval As UInt16
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Friend Structure TRIGGER_TYPE_UNION
		<FieldOffset(0)> Public Daily As DAILY
		<FieldOffset(0)> Public Weekly As WEEKLY
		<FieldOffset(0)> Public MonthlyDate As MONTHLYDATE
		<FieldOffset(0)> Public MonthlyDOW As MONTHLYDOW
	End Structure

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa380706(v=vs.85).aspx</remarks>
	<Guid("148BD528-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface IEnumWorkItems
		<PreserveSig()>
		Function [Next](<[In]()> ByVal celt As UInt32, <[Out]()> ByRef rgpwszNames As IntPtr, <[Out]()> ByRef pceltFetched As UInt32) As UInt32

		Sub Skip(<[In]()> ByVal pwszComputer As UInt32)

		Sub Reset()

		Function Clone() As <MarshalAs(UnmanagedType.Interface)> IEnumWorkItems

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381311(v=vs.85).aspx</remarks>
	<Guid("148BD524-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITask
		Function CreateTrigger(<[Out]()> ByRef piNewTrigger As UInt16) As <MarshalAs(UnmanagedType.Interface)> ITaskTrigger
		Sub DeleteTrigger(<[In]()> ByVal iTrigger As UInt16)
		Function GetTriggerCount() As <MarshalAs(UnmanagedType.U2)> UInt16
		Function GetTrigger(<[In]()> ByVal iTrigger As UInt16) As <MarshalAs(UnmanagedType.Interface)> ITaskTrigger
		Function GetTriggerString(<[In]()> ByVal iTrigger As UInt16) As CoTaskMemStr

		Sub GetRunTimes(
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal pstBegin As SYSTEMTIME,
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal pstEnd As SYSTEMTIME,
		   <[In](), [Out]()> ByVal pCount As UInt16,
		   <[In]()> ByVal rgstTaskTimes As IntPtr)

		Function GetNextRunTime() As <MarshalAs(UnmanagedType.Struct)> SYSTEMTIME
		Sub SetIdleWait(<[In]()> ByVal wIdleMinutes As UInt16, <[In]()> ByVal wDeadlineMinutes As UInt16)
		Sub GetIdleWait(<[Out]()> ByRef pwIdleMinutes As UInt16, <[Out]()> ByRef pwDeadlineMinutes As UInt16)
		Sub Run()
		Sub Terminate()
		Sub EditWorkItem(<[In]()> ByVal hParent As IntPtr, <[In]()> ByVal dwReserved As UInt32)
		Function GetMostRecentRunTime() As <MarshalAs(UnmanagedType.Struct)> SYSTEMTIME
		Function GetStatus() As UInt32
		Function GetExitCode() As UInt32
		Sub SetComment(<[In]()> ByVal pwszComment As CoTaskMemStr)
		Function GetComment() As CoTaskMemStr
		Sub SetCreator(<[In]()> ByVal pwszCreator As CoTaskMemStr)
		Function GetCreator() As CoTaskMemStr
		Sub SetWorkItemData(<[In]()> ByVal cBytes As UInt16, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=0, ArraySubType:=UnmanagedType.U1)> ByVal rgbData As Byte())
		Sub GetWorkItemData(<[Out]()> ByRef pcBytes As UInt16, <[Out]()> ByRef ppBytes As IntPtr)
		Sub SetErrorRetryCount(<[In]()> ByVal wRetryCount As UInt16)
		Function GetErrorRetryCount() As UInt16
		Sub SetErrorRetryInterval(<[In]()> ByVal wRetryInterval As UInt16)
		Function GetErrorRetryInterval() As UInt16
		Sub SetFlags(<[In]()> ByVal dwFlags As TASK_FLAG)
		Function GetFlags() As TASK_FLAG
		Sub SetAccountInformation(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszAccountName As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszPassword As String)
		Function GetAccountInformation() As CoTaskMemStr
		Sub SetApplicationName(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszApplicationName As String)
		Function GetApplicationName() As CoTaskMemStr
		Sub SetParameters(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszParameters As String)
		Function GetParameters() As CoTaskMemStr
		Sub SetWorkingDirectory(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszWorkingDirectory As String)
		Function GetWorkingDirectory() As CoTaskMemStr
		Sub SetPriority(<[In]()> ByVal dwPriority As UInt32)
		Function GetPriority() As UInt32
		Sub SetTaskFlags(<[In]()> ByVal dwFlags As UInt32)
		Function GetTaskFlags() As UInt32
		Sub SetMaxRunTime(<[In]()> ByVal dwMaxRunTime As UInt32)
		Function GetMaxRunTime() As UInt32
	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381811(v=vs.85).aspx</remarks>
	<Guid("148BD527-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITaskScheduler
		Sub SetTargetComputer(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszComputer As String)

		Function GetTargetComputer() As CoTaskMemStr

		Function [Enum]() As <MarshalAs(UnmanagedType.Interface)> IEnumWorkItems

		Function Activate(
		 <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszName As String,
		 <[In](), MarshalAs(UnmanagedType.LPStruct)> ByVal riid As Guid) As <MarshalAs(UnmanagedType.Interface)> ITask

		Sub Delete(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszName As String)

		Function NewWorkItem(
		 <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszTaskName As String,
		 <[In](), MarshalAs(UnmanagedType.LPStruct)> ByVal rclsid As Guid,
		 <[In](), MarshalAs(UnmanagedType.LPStruct)> ByVal riid As Guid) As <MarshalAs(UnmanagedType.Interface)> ITask

		Sub AddWorkItem(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszTaskName As String, <[In](), MarshalAs(UnmanagedType.Interface)> ByVal pWorkItem As ITask)

		Sub IsOfType(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszName As String, <[In](), MarshalAs(UnmanagedType.LPStruct)> ByVal riid As Guid)

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381864(v=vs.85).aspx</remarks>
	<Guid("148BD520-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface ITaskTrigger
		Sub SetTrigger(<[In](), Out(), MarshalAs(UnmanagedType.Struct)> ByRef pTrigger As TASK_TRIGGER)

		Function GetTrigger() As <MarshalAs(UnmanagedType.Struct)> TASK_TRIGGER

		Function GetTriggerString() As CoTaskMemStr
	End Interface

	''' <remarks></remarks>
	<Guid("4086658a-cbbb-11cf-b604-00c04fd8d565"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface IProvideTaskPage

	End Interface

	''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/aa381216(v=vs.85).aspx</remarks>
	<Guid("A6B952F0-A4B1-11D0-997D-00AA006887EC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity()>
	Friend Interface IScheduledWorkItem
		Sub CreateTrigger(<[Out]()> ByRef piNewTrigger As UInt16, <[Out](), MarshalAs(UnmanagedType.Interface)> ByRef ppTrigger As ITaskTrigger)
		Sub DeleteTrigger(<[In]()> ByVal iTrigger As UInt16)
		Sub GetTriggerCount(<[Out]()> ByRef plCount As UInt16)
		Sub GetTrigger(<[In]()> ByVal iTrigger As UInt16, <[Out](), MarshalAs(UnmanagedType.Interface)> ByRef ppTrigger As ITaskTrigger)
		Sub GetTriggerString(<[In]()> ByVal iTrigger As UInt16, <[Out]()> ByRef ppwszTrigger As CoTaskMemStr)

		Sub GetRunTimes(
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal pstBegin As SYSTEMTIME,
		   <[In](), MarshalAs(UnmanagedType.Struct)> ByVal pstEnd As SYSTEMTIME,
		   <[In](), [Out]()> ByVal pCount As UInt16,
		   <[In]()> ByVal rgstTaskTimes As IntPtr)

		Sub GetNextRunTime(<[Out](), MarshalAs(UnmanagedType.Struct)> ByRef pstNextRun As SYSTEMTIME)
		Sub SetIdleWait(<[In]()> ByVal wIdleMinutes As UInt16, <[In]()> ByVal wDeadlineMinutes As UInt16)
		Sub GetIdleWait(<[Out]()> ByRef pwIdleMinutes As UInt16, <[Out]()> ByRef pwDeadlineMinutes As UInt16)
		Sub Run()
		Sub Terminate()
		Sub EditWorkItem(<[In]()> ByVal hParent As IntPtr, <[In]()> ByVal dwReserved As UInt32)
		Sub GetMostRecentRunTime(<[In](), [Out](), MarshalAs(UnmanagedType.Struct)> ByRef pstLastRun As SYSTEMTIME)
		Sub GetStatus(<[Out]()> ByRef phrStatus As UInt32)
		Sub GetExitCode(<[Out]()> ByRef pdwExitCode As UInt32)
		Sub SetComment(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszComment As String)
		Sub GetComment(<[Out]()> ByVal ppwszComment As CoTaskMemStr)
		Sub SetCreator(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszCreator As String)
		Sub GetCreator(<[Out]()> ByVal ppwszCreator As CoTaskMemStr)
		Sub SetWorkItemData(<[In]()> ByVal cBytes As UInt16, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=0, ArraySubType:=UnmanagedType.U1)> ByVal rgbData As Byte())
		Sub GetWorkItemData(<[Out]()> ByRef pcBytes As UInt16, <[Out]()> ByRef ppBytes As IntPtr)
		Sub SetErrorRetryCount(<[In]()> ByVal wRetryCount As UInt16)
		Sub GetErrorRetryCount(<[Out]()> ByRef pwRetryCount As UInt16)
		Sub SetErrorRetryInterval(<[In]()> ByVal wRetryInterval As UInt16)
		Sub GetErrorRetryInterval(<[Out]()> ByRef pwRetryInterval As UInt16)
		Sub SetFlags(<[In]()> ByVal dwFlags As TASK_FLAG)
		Sub GetFlags(<[Out]()> ByRef pdwFlags As TASK_FLAG)
		Sub SetAccountInformation(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszAccountName As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwszPassword As String)
		Sub GetAccountInformation(<[Out]()> ByVal ppwszAccountName As CoTaskMemStr)
	End Interface

	<ComImport(), Guid("148BD520-A2AB-11CE-B11F-00AA00530503"), SuppressUnmanagedCodeSecurity()>
	Friend Class Ctask
		<MethodImpl(MethodImplOptions.InternalCall)>
		Public Sub New()

		End Sub
	End Class

	<ComImport(), Guid("148BD52A-A2AB-11CE-B11F-00AA00530503"), SuppressUnmanagedCodeSecurity()>
	Friend Class CTaskScheduler
		<MethodImpl(MethodImplOptions.InternalCall)>
		Public Sub New()

		End Sub
	End Class

	Friend Class CoTaskMemStr
		Inherits SafeHandle

		Public Sub New()
			MyBase.New(IntPtr.Zero, True)
		End Sub

		Public Sub New(ByVal handle As IntPtr)
			MyBase.New(IntPtr.Zero, True)

			SetHandle(handle)
		End Sub

		Public Sub New(ByVal text As String)
			MyBase.New(IntPtr.Zero, True)

			SetHandle(Marshal.StringToCoTaskMemUni(text))
		End Sub

		Public Overrides ReadOnly Property IsInvalid As Boolean
			Get
				Return (handle = IntPtr.Zero)
			End Get
		End Property

		Protected Overrides Function ReleaseHandle() As Boolean
			Marshal.FreeCoTaskMem(handle)
			Return True
		End Function

		Public Overrides Function ToString() As String
			Return Marshal.PtrToStringUni(handle)
		End Function

		Public Shared Widening Operator CType(value As CoTaskMemStr) As String
			Return value.ToString()
		End Operator
	End Class

End Namespace

Namespace Display_Driver_Uninstaller.Win32.TaskScheduler.Version2
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
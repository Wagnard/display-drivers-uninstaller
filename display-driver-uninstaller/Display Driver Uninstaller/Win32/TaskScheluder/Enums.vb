Namespace WIN32.TASKSCHEDULER

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

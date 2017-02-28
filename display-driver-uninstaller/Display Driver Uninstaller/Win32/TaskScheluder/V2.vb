Imports System.Collections
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices


Namespace Win32.TaskScheduler.V2

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

	<Flags()>
	Friend Enum TASK_ENUM_FLAGS As UInt32
		HIDDEN = &H1UI
	End Enum


	'WIP
	' V1 <= XP
	' V2 >= Vista

End Namespace
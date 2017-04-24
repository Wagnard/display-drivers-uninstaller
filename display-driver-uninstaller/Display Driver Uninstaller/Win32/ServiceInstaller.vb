Imports System
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Threading
Imports Microsoft.Win32.SafeHandles

Imports Display_Driver_Uninstaller.Win32

' https://msdn.microsoft.com/en-us/library/windows/desktop/ms685974(v=vs.85).aspx

Public NotInheritable Class ServiceInstaller

	Private Const SERVICES_ACTIVE_DATABASE As String = "ServicesActive"
	Private Const SERVICES_FAILED_DATABASE As String = "ServicesFailed"
	Private Const CREATE_SERVICE_AS_SYSTEM As String = "NT AUTHORITY\LocalService"

#Region "Enums"

	Public Enum SERVICE_STATE_TYPE As UInt32
		PROCESS_INFO = 0UI
	End Enum

	<Flags()>
	Public Enum SERVICE_FLAGS As UInt32
		''' <summary>The service is running in a process that is not a system process, or it is not running.
		''' If the service is running in a process that is not a system process, dwProcessId is nonzero. If the service is not running, dwProcessId is zero.</summary>
		NORMAL = &H0UI

		''' <summary>The service runs in a system process that must always be running.</summary>
		RUNS_IN_SYSTEM_PROCESS = &H1UI
	End Enum

	Public Enum SERVICE_STATE
		NO_CHANGE = -1
		UNKNOWN = 0
		NOT_FOUND = UNKNOWN

		''' <summary>The service is not running</summary>
		STOPPED = &H1UI

		''' <summary>The service is starting.</summary>
		START_PENDING = &H2UI

		''' <summary>The service is stopping.</summary>
		STOP_PENDING = &H3UI

		''' <summary>The service is running.</summary>
		RUNNING = &H4UI

		''' <summary>The service continue is pending.</summary>
		CONTINUE_PENDING = &H5UI

		''' <summary>The service pause is pending.</summary>
		PAUSE_PENDING = &H6UI

		''' <summary>The service is paused.</summary>
		PAUSED = &H7UI
	End Enum

	Public Enum SERVICE_CONTROL_ACCEPT As UInt32
		''' <summary>The service can be stopped.
		''' This control code allows the service to receive SERVICE_CONTROL_STOP notifications.</summary>
		[STOP] = &H1

		''' <summary>The service can be paused and continued.
		''' This control code allows the service to receive SERVICE_CONTROL_PAUSE and SERVICE_CONTROL_CONTINUE notifications.</summary>
		PAUSE_CONTINUE = &H2

		''' <summary>The service is notified when system shutdown occurs.
		''' This control code allows the service to receive SERVICE_CONTROL_SHUTDOWN notifications. Note that ControlService and ControlServiceEx cannot send this notification; only the system can send it.</summary>
		SHUTDOWN = &H4

		''' <summary>The service can reread its startup parameters without being stopped and restarted.
		''' This control code allows the service to receive SERVICE_CONTROL_PARAMCHANGE notifications.</summary>
		PARAMCHANGE = &H8

		''' <summary>The service is a network component that can accept changes in its binding without being stopped and restarted.
		''' This control code allows the service to receive SERVICE_CONTROL_NETBINDADD, SERVICE_CONTROL_NETBINDREMOVE, SERVICE_CONTROL_NETBINDENABLE, and SERVICE_CONTROL_NETBINDDISABLE notifications.</summary>
		NETBINDCHANGE = &H10

		''' <summary>The service is notified when the computer's hardware profile has changed. This enables the system to send SERVICE_CONTROL_HARDWAREPROFILECHANGE notifications to the service.</summary>
		HARDWAREPROFILECHANGE = &H20

		''' <summary>The service is notified when the computer's power status has changed. This enables the system to send SERVICE_CONTROL_POWEREVENT notifications to the service.</summary>
		POWEREVENT = &H40

		''' <summary>The service is notified when the computer's session status has changed. This enables the system to send SERVICE_CONTROL_SESSIONCHANGE notifications to the service.</summary>
		SESSIONCHANGE = &H80

		''' <summary>The service can perform preshutdown tasks.
		''' This control code enables the service to receive SERVICE_CONTROL_PRESHUTDOWN notifications. Note that ControlService and ControlServiceEx cannot send this notification; only the system can send it.
		''' Windows Server 2003 and Windows XP:  This value is not supported.</summary>
		PRESHUTDOWN = &H100

		''' <summary>The service is notified when the system time has changed. This enables the system to send SERVICE_CONTROL_TIMECHANGE notifications to the service.
		''' Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP:  This control code is not supported.</summary>
		TIMECHANGE = &H200

		''' <summary>The service is notified when an event for which the service has registered occurs. This enables the system to send SERVICE_CONTROL_TRIGGEREVENT notifications to the service.
		''' Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP:  This control code is not supported.</summary>
		TRIGGEREVENT = &H400

		''' <summary>The services is notified when the user initiates a reboot.
		''' Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP:  This control code is not supported.</summary>
		USERMODEREBOOT = &H800

	End Enum

	<Flags()>
	Friend Enum SC_MANAGER As UInt32
		''' <summary>Required to connect to the service control manager.</summary>
		CONNECT = &H1UI

		''' <summary>Required to call the CreateService function to create a service object and add it to the database.</summary>
		CREATE_SERVICE = &H2UI

		''' <summary>Required to call the EnumServicesStatus or EnumServicesStatusEx function to list the services that are in the database.
		''' Required to call the NotifyServiceStatusChange function to receive notification when any service is created or deleted.</summary>
		ENUMERATE_SERVICE = &H4UI

		''' <summary>Required to call the LockServiceDatabase function to acquire a lock on the database.</summary>
		LOCK = &H8UI

		''' <summary>Required to call the QueryServiceLockStatus function to retrieve the lock status information for the database.</summary>
		QUERY_LOCK_STATUS = &H10UI

		''' <summary>Required to call the NotifyBootConfigStatus function.</summary>
		MODIFY_BOOT_CONFIG = &H20UI

		''' <summary>Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table.</summary>
		ALL_ACCESS = &HF003FUI
	End Enum

	<Flags()>
	Friend Enum SERVICE As UInt32
		''' <summary>Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration.</summary>
		QUERY_CONFIG = &H1UI

		''' <summary>Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. 
		''' Because this grants the caller the right to change the executable file that the system runs, it should be granted only to administrators.</summary>
		CHANGE_CONFIG = &H2UI

		''' <summary>Required to call the QueryServiceStatus or QueryServiceStatusEx function to ask the service control manager about the status of the service.
		''' Required to call the NotifyServiceStatusChange function to receive notification when a service changes status.</summary>
		QUERY_STATUS = &H4UI

		''' <summary>Required to call the EnumDependentServices function to enumerate all the services dependent on the service.</summary>
		ENUMERATE_DEPENDENTS = &H8UI

		''' <summary>Required to call the StartService function to start the service.</summary>
		START = &H10UI

		''' <summary>Required to call the ControlService function to stop the service.</summary>
		[STOP] = &H20UI

		''' <summary>Required to call the ControlService function to pause or continue the service.</summary>
		PAUSE_CONTINUE = &H40UI

		''' <summary>Required to call the ControlService function to ask the service to report its status immediately.</summary>
		INTERROGATE = &H80UI

		''' <summary>Required to call the ControlService function to specify a user-defined control code.</summary>
		USER_DEFINED_CONTROL = &H100UI

		''' <summary>Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table. </summary>
		ALL_ACCESS = &HF01FFUI
	End Enum

	Friend Enum SERVICE_START_TYPE As UInt32
		''' <summary>A device driver started by the system loader. This value is valid only for driver services.</summary>
		BOOT_START = &H0

		''' <summary>A device driver started by the IoInitSystem function. This value is valid only for driver services.</summary>
		SYSTEM_START = &H1

		''' <summary>A service started automatically by the service control manager during system startup. For more information, see Automatically Starting Services.</summary>
		AUTO_START = &H2

		''' <summary>A service started by the service control manager when a process calls the StartService function. For more information, see Starting Services on Demand.</summary>
		DEMAND_START = &H3

		''' <summary>A service that cannot be started. Attempts to start the service result in the error code ERROR_SERVICE_DISABLED.</summary>
		DISABLED = &H4
	End Enum

	Friend Enum SERVICE_ERROR As UInt32
		''' <summary>The startup program ignores the error and continues the startup operation.</summary>
		IGNORE = &H0UI

		''' <summary>The startup program logs the error in the event log but continues the startup operation.</summary>
		NORMAL = &H1UI

		''' <summary>The startup program logs the error in the event log. If the last-known-good configuration is being started, the startup operation continues.
		''' Otherwise, the system is restarted with the last-known-good config</summary>
		SEVERE = &H2UI

		''' <summary>The startup program logs the error in the event log, if possible. If the last-known-good configuration is being started, the startup operation fails. 
		''' Otherwise, the system is restarted with the last-known good configuration.</summary>
		CRITICAL = &H3UI
	End Enum

	Friend Enum SERVICE_TYPE As UInt32
		FILE_SYSTEM_DRIVER = &H2UI
		KERNEL_DRIVER = &H1UI
		WIN32_OWN_PROCESS = &H10UI
		WIN32_SHARE_PROCESS = &H20UI
		USER_OWN_PROCESS = &H50UI
		USER_SHARE_PROCESS = &H60UI
		INTERACTIVE_PROCESS = &H100UI
	End Enum

	Friend Enum SERVICE_CONTROL As UInt32
		''' <summary>Notifies a service that it should stop. The hService handle must have the SERVICE_STOP access right.
		''' After sending the stop request to a service, you should not send other controls to the service.</summary>
		SERVICE_CONTROL_STOP = &H1UI

		''' <summary>Notifies a service that it should pause. The hService handle must have the SERVICE_PAUSE_CONTINUE access right.</summary>
		SERVICE_CONTROL_PAUSE = &H2UI

		''' <summary>Notifies a paused service that it should resume. The hService handle must have the SERVICE_PAUSE_CONTINUE access right.</summary>
		SERVICE_CONTROL_CONTINUE = &H3UI

		''' <summary>Notifies a service that it should report its current status information to the service control manager. The hService handle must have the SERVICE_INTERROGATE access right.
		''' Note that this control is not generally useful as the SCM is aware of the current state of the service.</summary>
		SERVICE_CONTROL_INTERROGATE = &H4UI

		''' <summary>Notifies a service that its startup parameters have changed. The hService handle must have the SERVICE_PAUSE_CONTINUE access right.</summary>
		SERVICE_CONTROL_PARAMCHANGE = &H6UI

		''' <summary>Notifies a network service that there is a new component for binding. The hService handle must have the SERVICE_PAUSE_CONTINUE access right. 
		''' However, this control code has been deprecated; use Plug and Play functionality instead.</summary>
		SERVICE_CONTROL_NETBINDADD = &H7UI

		''' <summary>Notifies a network service that a component for binding has been removed. The hService handle must have the SERVICE_PAUSE_CONTINUE access right. 
		''' However, this control code has been deprecated; use Plug and Play functionality instead.</summary>
		SERVICE_CONTROL_NETBINDREMOVE = &H8UI

		''' <summary>Notifies a network service that a disabled binding has been enabled. The hService handle must have the SERVICE_PAUSE_CONTINUE access right. 
		''' However, this control code has been deprecated; use Plug and Play functionality instead.</summary>
		SERVICE_CONTROL_NETBINDENABLE = &H9UI

		''' <summary>Notifies a network service that one of its bindings has been disabled. The hService handle must have the SERVICE_PAUSE_CONTINUE access right. 
		''' However, this control code has been deprecated; use Plug and Play functionality instead.</summary>
		SERVICE_CONTROL_NETBINDDISABLE = &HAUI
	End Enum

#End Region


#Region "Structures"

	<StructLayout(LayoutKind.Sequential)>
	Private Structure SERVICE_STATUS
		Public dwServiceType As UInt32
		Public dwCurrentState As SERVICE_STATE
		Public dwControlsAccepted As UInt32
		Public dwWin32ExitCode As UInt32
		Public dwServiceSpecificExitCode As UInt32
		Public dwCheckPoint As UInt32
		Public dwWaitHint As UInt32
	End Structure

	<StructLayout(LayoutKind.Sequential)>
	Private Structure SERVICE_STATUS_PROCESS
		Public dwServiceType As UInt32
		Public dwCurrentState As SERVICE_STATE
		Public dwControlsAccepted As UInt32
		Public dwWin32ExitCode As UInt32
		Public dwServiceSpecificExitCode As UInt32
		Public dwCheckPoint As UInt32
		Public dwWaitHint As UInt32

		Public dwProcessId As UInt32
		Public dwServiceFlags As SERVICE_FLAGS
	End Structure
#End Region

#Region "P/Invoke"

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function OpenSCManager(
  <[In](), [Optional]()> ByVal lpMachineName As IntPtr,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpDatabaseName As String,
  <[In]()> ByVal dwDesiredAccess As SC_MANAGER) As SafeServiceHandle
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function OpenService(
  <[In]()> ByVal hSCManager As IntPtr,
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpServiceName As String,
  <[In]()> ByVal dwDesiredAccess As UInt32) As SafeServiceHandle
	End Function


	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function CreateService(
  <[In]()> ByVal hSCManager As IntPtr,
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpServiceName As String,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpDisplayName As String,
  <[In]()> ByVal dwDesiredAccess As SERVICE,
  <[In]()> ByVal dwServiceType As SERVICE_TYPE,
  <[In]()> ByVal dwStartType As SERVICE_START_TYPE,
  <[In]()> ByVal dwErrorControl As SERVICE_ERROR,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpBinaryPathName As String,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpLoadOrderGroup As String,
  <[Out](), [Optional]()> ByRef lpdwTagId As IntPtr,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpDependencies As String,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lp As String,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpPassword As String) As IntPtr
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function CloseServiceHandle(
  <[In]()> ByVal hSCObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function QueryServiceStatusEx(
  <[In]()> ByVal hService As IntPtr,
  <[In]()> ByVal InfoLevel As SERVICE_STATE_TYPE,
  <[Out](), [Optional]()> ByRef lpBuffer As SERVICE_STATUS_PROCESS,
  <[In]()> ByVal cbBufSize As UInt32,
  <[Out]()> ByRef pcbBytesNeeded As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function DeleteService(
  <[In]()> ByVal hService As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function ControlService(
  <[In]()> ByVal hService As IntPtr,
  <[In]()> ByVal dwControl As SERVICE_CONTROL,
  <[Out]()> ByRef lpServiceStatus As SERVICE_STATUS) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("AdvApi32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
	Private Shared Function StartService(
  <[In]()> ByVal hService As IntPtr,
  <[In]()> ByVal dwNumServiceArgs As UInt32,
  <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpServiceArgVectors As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

#End Region



	Public Shared Sub Uninstall(ByVal serviceName As String)
		Using scm As SafeServiceHandle = OpenSCManager(SC_MANAGER.CONNECT)
			If scm.IsInvalid Then
				Throw New Win32Exception()
			End If

			Using ptrService As SafeServiceHandle = OpenService(scm.DangerousGetHandle, serviceName, SERVICE.ALL_ACCESS Or ACL.ACCESS_RIGHTS.DELETE)
				If ptrService.IsInvalid Then
					Throw New Exception("Service not installed!")
				End If

				StopService(serviceName)

				If Not DeleteService(ptrService.DangerousGetHandle) Then
					Throw New Exception("Could not delete service " & Marshal.GetLastWin32Error())
					Microsoft.VisualBasic.MsgBox(Marshal.GetLastWin32Error)
				End If
			End Using
		End Using
	End Sub

	Public Shared Function ServiceIsInstalled(ByVal serviceName As String) As Boolean
		Using scm As SafeServiceHandle = OpenSCManager(SC_MANAGER.CONNECT)
			If scm.IsInvalid Then
				Throw New Win32Exception()
			End If

			Using ptrService As SafeServiceHandle = OpenService(scm.DangerousGetHandle, serviceName, SERVICE.QUERY_STATUS)
				Return ptrService.IsValid
			End Using
		End Using
	End Function


	'Public Shared Sub StartService(serviceName As String)
	'	Dim scm As IntPtr = OpenSCManager(SC_MANAGER.CONNECT)

	'	Try
	'		Dim service As IntPtr = OpenService(scm, serviceName, SERVICE.QUERY_STATUS Or SERVICE.START)
	'		If service = IntPtr.Zero Then
	'			Throw New Exception("Could not open service.")
	'		End If

	'		Try
	'			StartService(service)
	'		Finally
	'			CloseServiceHandle(service)
	'		End Try
	'	Finally
	'		CloseServiceHandle(scm)
	'	End Try
	'End Sub

	'Public Shared Sub StopService(serviceName As String)
	'	Dim scm As IntPtr = OpenSCManager(SC_MANAGER.CONNECT)

	'	Try
	'		Dim service As IntPtr = OpenService(scm, serviceName, SERVICE.QUERY_STATUS Or SERVICE.[STOP])
	'		If service = IntPtr.Zero Then
	'			Throw New Exception("Could not open service.")
	'		End If

	'		Try
	'			StopService(service)
	'		Finally
	'			CloseServiceHandle(service)
	'		End Try
	'	Finally
	'		CloseServiceHandle(scm)
	'	End Try
	'End Sub

	Public Shared Sub StartService(ByVal serviceName As String)
		Using scm As SafeServiceHandle = OpenSCManager(SC_MANAGER.CONNECT)
			If scm.IsInvalid Then
				Throw New Win32Exception()
			End If

			Using ptrService As SafeServiceHandle = OpenService(scm.DangerousGetHandle, serviceName, SERVICE.QUERY_STATUS Or SERVICE.START)
				If ptrService.IsInvalid OrElse Not StartService(ptrService.DangerousGetHandle, 0UI, Nothing) Then
					Application.Log.AddException(New Win32Exception(GetLastWin32Error(), "Failed to start service!"))
				End If

				If Not WaitForServiceStatus(ptrService, SERVICE_STATE.RUNNING) Then
					Application.Log.AddWarningMessage("Failed to start service: " & serviceName & " (timeout)!")
				Else
					Application.Log.AddMessage("Service: " & serviceName & " is successfully started!")
				End If
			End Using
		End Using
	End Sub

	'Private Shared Sub StartService(service As IntPtr)
	'	Dim status As New SERVICE_STATUS()

	'	StartService(service, 0, Nothing)

	'	Dim changedStatus As Boolean = WaitForServiceStatus(service, SERVICE_STATE.START_PENDING, SERVICE_STATE.RUNNING)

	'	If Not changedStatus Then
	'		Throw New Exception("Unable to start service")
	'	End If
	'End Sub

	Private Shared Sub StopService(ByVal ptrService As SafeServiceHandle, ByVal serviceName As String)
		Dim statusProcess As New SERVICE_STATUS_PROCESS
		Dim requiredSize As UInt32 = 0UI

		If Not QueryServiceStatusEx(ptrService.DangerousGetHandle, SERVICE_STATE_TYPE.PROCESS_INFO, statusProcess, CUInt(Marshal.SizeOf(statusProcess)), requiredSize) Then
			Throw New Win32Exception(GetLastWin32Error(), "Failed to query service status!")
		End If

		Dim status As New SERVICE_STATUS

		Select Case status.dwCurrentState
			Case SERVICE_STATE.STOP_PENDING
				WaitForServiceStatus(ptrService, SERVICE_STATE.STOPPED)

			Case SERVICE_STATE.STOPPED
				Return

			Case SERVICE_STATE.CONTINUE_PENDING,
			 SERVICE_STATE.START_PENDING,
			 SERVICE_STATE.PAUSE_PENDING,
			 SERVICE_STATE.RUNNING
				ControlService(ptrService.DangerousGetHandle, SERVICE_CONTROL.SERVICE_CONTROL_STOP, status)

			Case Else
				ControlService(ptrService.DangerousGetHandle, SERVICE_CONTROL.SERVICE_CONTROL_STOP, status)
		End Select

		If Not WaitForServiceStatus(ptrService, SERVICE_STATE.STOPPED) Then
			Application.Log.AddWarningMessage("Failed to stop service: " & serviceName & " (timeout)!")
		Else
			Application.Log.AddMessage("Service: " & serviceName & " is successfully stopped!")
		End If

		Return
	End Sub

	Public Shared Sub StopService(ByVal serviceName As String)
		Using scm As SafeServiceHandle = OpenSCManager(SC_MANAGER.CONNECT)
			If scm.IsInvalid Then
				Throw New Win32Exception()
			End If

			Using ptrService As SafeServiceHandle = OpenService(scm.DangerousGetHandle, serviceName, SERVICE.QUERY_STATUS Or SERVICE.STOP)
				If ptrService.IsInvalid Then
					Dim errcode As Int32 = GetLastWin32Error()

					If errcode <> 0UI Then
						Application.Log.AddException(New Win32Exception(errcode), "Failed to stop service!")
					End If

					Return
				End If

				StopService(ptrService, serviceName)
			End Using
		End Using
	End Sub

	'Private Shared Sub StopService(service As IntPtr)
	'	Dim status As New SERVICE_STATUS()

	'	ControlService(service, SERVICE_CONTROL.SERVICE_CONTROL_STOP, status)

	'	Dim changedStatus As Boolean = WaitForServiceStatus(service, SERVICE_STATE.STOP_PENDING, SERVICE_STATE.STOPPED)

	'	If Not changedStatus Then
	'		Throw New ApplicationException("Unable to stop service")
	'	End If
	'End Sub

	Public Shared Function GetServiceStatus(ByVal serviceName As String) As SERVICE_STATE
		Using scm As SafeServiceHandle = OpenSCManager(SC_MANAGER.CONNECT)
			If scm.IsInvalid Then
				Throw New Win32Exception()
			End If

			Using ptrService As SafeServiceHandle = OpenService(scm.DangerousGetHandle, serviceName, SERVICE.QUERY_STATUS)
				If ptrService.IsInvalid Then
					Return SERVICE_STATE.NOT_FOUND
				End If

				Dim status As New SERVICE_STATUS_PROCESS
				Dim requiredSize As UInt32 = 0UI

				If Not QueryServiceStatusEx(ptrService.DangerousGetHandle, SERVICE_STATE_TYPE.PROCESS_INFO, status, CUInt(Marshal.SizeOf(status)), requiredSize) Then
					Throw New Win32Exception("Failed to query service status!")
				End If

				Return status.dwCurrentState
			End Using
		End Using
	End Function

	Private Shared Function WaitForServiceStatus(ByVal ptrService As SafeServiceHandle, desiredStatus As SERVICE_STATE) As Boolean
		Dim status As New SERVICE_STATUS_PROCESS
		Dim requiredSize As UInt32 = 0UI

		If Not QueryServiceStatusEx(ptrService.DangerousGetHandle, SERVICE_STATE_TYPE.PROCESS_INFO, status, CUInt(Marshal.SizeOf(status)), requiredSize) Then
			Throw New Win32Exception("Failed to query service status!")
		End If

		If status.dwCurrentState = desiredStatus Then
			Return True
		End If


		Dim tickCount As Int32 = Environment.TickCount
		Dim checkPoint As UInt32 = status.dwCheckPoint
		Dim waitTime As Int32 = Math.Max(Math.Min(CInt(status.dwWaitHint \ 10UI), 10000), 1000)

		While status.dwCurrentState <> desiredStatus
			Thread.Sleep(100)

			If Not QueryServiceStatusEx(ptrService.DangerousGetHandle, SERVICE_STATE_TYPE.PROCESS_INFO, status, CUInt(Marshal.SizeOf(status)), requiredSize) Then
				Throw New Win32Exception("Failed to query service status!")
			End If

			If status.dwCheckPoint > checkPoint Then
				tickCount = Environment.TickCount
				checkPoint = status.dwCheckPoint
			Else
				If Environment.TickCount - tickCount > status.dwWaitHint Then
					Exit While
				End If
			End If
		End While

		Return (status.dwCurrentState = desiredStatus)
	End Function

	Private Shared Function OpenSCManager(ByVal rights As SC_MANAGER) As SafeServiceHandle
		Dim scm As SafeServiceHandle = OpenSCManager(IntPtr.Zero, Nothing, rights)

		If scm.IsInvalid Then
			Throw New Win32Exception(GetLastWin32Error(), "Could not connect to Service Control Manager.")
		End If

		Return scm
	End Function

	Private Class SafeServiceHandle
		Inherits SafeHandleZeroOrMinusOneIsInvalid

		Private Sub New()
			MyBase.New(True)
		End Sub

		Private Sub New(ByVal preexistingHandle As IntPtr, ByVal ownsHandle As Boolean)
			MyBase.New(ownsHandle)

			SetHandle(preexistingHandle)
		End Sub

		<SecurityCritical()>
		Protected Overrides Function ReleaseHandle() As Boolean
			Return CloseServiceHandle(handle)
		End Function

		Public ReadOnly Property IsValid As Boolean
			Get
				Return (Not IsInvalid)
			End Get
		End Property
	End Class

End Class

Imports System
Imports System.Runtime.InteropServices
Imports System.Threading

Public NotInheritable Class ServiceInstaller
	Private Sub New()
	End Sub
	Private Const STANDARD_RIGHTS_REQUIRED As Integer = &HF0000
	Private Const SERVICE_WIN32_OWN_PROCESS As Integer = &H10

	<StructLayout(LayoutKind.Sequential)> _
	Private Class SERVICE_STATUS
		Public dwServiceType As Integer = 0
		Public dwCurrentState As ServiceState = 0
		Public dwControlsAccepted As Integer = 0
		Public dwWin32ExitCode As Integer = 0
		Public dwServiceSpecificExitCode As Integer = 0
		Public dwCheckPoint As Integer = 0
		Public dwWaitHint As Integer = 0
	End Class

#Region "OpenSCManager"
	Private Declare Unicode Function OpenSCManager Lib "advapi32.dll" Alias "OpenSCManagerW" (machineName As String, databaseName As String, dwDesiredAccess As ScmAccessRights) As IntPtr
#End Region

#Region "OpenService"
	<DllImport("advapi32.dll", SetLastError:=True, CharSet:=CharSet.Auto)> _
	Private Shared Function OpenService(hSCManager As IntPtr, lpServiceName As String, dwDesiredAccess As ServiceAccessRights) As IntPtr
	End Function
#End Region

#Region "CreateService"
	<DllImport("advapi32.dll", SetLastError:=True, CharSet:=CharSet.Auto)> _
	Private Shared Function CreateService(hSCManager As IntPtr, lpServiceName As String, lpDisplayName As String, dwDesiredAccess As ServiceAccessRights, dwServiceType As Integer, dwStartType As ServiceBootFlag, _
		dwErrorControl As ServiceError, lpBinaryPathName As String, lpLoadOrderGroup As String, lpdwTagId As IntPtr, lpDependencies As String, lp As String, _
		lpPassword As String) As IntPtr
	End Function
#End Region

#Region "CloseServiceHandle"
	<DllImport("advapi32.dll", SetLastError:=True)> _
	Private Shared Function CloseServiceHandle(hSCObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function
#End Region

#Region "QueryServiceStatus"
	<DllImport("advapi32.dll")> _
	Private Shared Function QueryServiceStatus(hService As IntPtr, lpServiceStatus As SERVICE_STATUS) As Integer
	End Function
#End Region

#Region "DeleteService"
	<DllImport("advapi32.dll", SetLastError:=True)> _
	Private Shared Function DeleteService(hService As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function
#End Region

#Region "ControlService"
	<DllImport("advapi32.dll")> _
	Private Shared Function ControlService(hService As IntPtr, dwControl As ServiceControl, lpServiceStatus As SERVICE_STATUS) As Integer
	End Function
#End Region

#Region "StartService"
	<DllImport("advapi32.dll", SetLastError:=True)> _
	Private Shared Function StartService(hService As IntPtr, dwNumServiceArgs As Integer, lpServiceArgVectors As Integer) As Integer
	End Function
#End Region

	Public Shared Sub Uninstall(serviceName As String)
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.AllAccess)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.AllAccess)
			If service = IntPtr.Zero Then
				Throw New ApplicationException("Service not installed.")
			End If

			Try
				StopService(service)
				If Not DeleteService(service) Then
					Throw New ApplicationException("Could not delete service " & Marshal.GetLastWin32Error())
					Microsoft.VisualBasic.MsgBox(Marshal.GetLastWin32Error)
				End If
			Finally
				CloseServiceHandle(service)
			End Try
		Finally
			CloseServiceHandle(scm)
		End Try
	End Sub

	Public Shared Function ServiceIsInstalled(serviceName As String) As Boolean
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.Connect)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus)

			If service = IntPtr.Zero Then
				Return False
			End If

			CloseServiceHandle(service)
			Return True
		Finally
			CloseServiceHandle(scm)
		End Try
	End Function

	Public Shared Sub Install(serviceName As String, displayName As String, fileName As String)
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.AllAccess)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.AllAccess)

			If service = IntPtr.Zero Then
				service = CreateService(scm, serviceName, displayName, ServiceAccessRights.AllAccess, SERVICE_WIN32_OWN_PROCESS, ServiceBootFlag.AutoStart, _
					ServiceError.Normal, fileName, Nothing, IntPtr.Zero, Nothing, Nothing, _
					Nothing)
			End If

			If service = IntPtr.Zero Then
				Throw New ApplicationException("Failed to install service.")
			End If
		Finally
			CloseServiceHandle(scm)
		End Try
	End Sub

	Public Shared Sub InstallAndStart(serviceName As String, displayName As String, fileName As String)
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.AllAccess)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.AllAccess)

			If service = IntPtr.Zero Then
				service = CreateService(scm, serviceName, displayName, ServiceAccessRights.AllAccess, SERVICE_WIN32_OWN_PROCESS, ServiceBootFlag.AutoStart, _
					ServiceError.Normal, fileName, Nothing, IntPtr.Zero, Nothing, Nothing, _
					Nothing)
			End If

			If service = IntPtr.Zero Then
				Throw New ApplicationException("Failed to install service.")
			End If

			Try
				StartService(service)
			Finally
				CloseServiceHandle(service)
			End Try
		Finally
			CloseServiceHandle(scm)
		End Try
	End Sub

	Public Shared Sub StartService(serviceName As String)
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.Connect)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus Or ServiceAccessRights.Start)
			If service = IntPtr.Zero Then
				Throw New ApplicationException("Could not open service.")
			End If

			Try
				StartService(service)
			Finally
				CloseServiceHandle(service)
			End Try
		Finally
			CloseServiceHandle(scm)
		End Try
	End Sub

	Public Shared Sub StopService(serviceName As String)
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.Connect)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus Or ServiceAccessRights.[Stop])
			If service = IntPtr.Zero Then
				Throw New ApplicationException("Could not open service.")
			End If

			Try
				StopService(service)
			Finally
				CloseServiceHandle(service)
			End Try
		Finally
			CloseServiceHandle(scm)
		End Try
	End Sub

	Private Shared Sub StartService(service As IntPtr)
		Dim status As New SERVICE_STATUS()
		StartService(service, 0, 0)
		Dim changedStatus As Boolean = WaitForServiceStatus(service, ServiceState.StartPending, ServiceState.Running)
		If Not changedStatus Then
			Throw New ApplicationException("Unable to start service")
		End If
	End Sub

	Private Shared Sub StopService(service As IntPtr)
		Dim status As New SERVICE_STATUS()
		ControlService(service, ServiceControl.[Stop], status)
		Dim changedStatus As Boolean = WaitForServiceStatus(service, ServiceState.StopPending, ServiceState.Stopped)
		If Not changedStatus Then
			Throw New ApplicationException("Unable to stop service")
		End If
	End Sub

	Public Shared Function GetServiceStatus(serviceName As String) As ServiceState
		Dim scm As IntPtr = OpenSCManager(ScmAccessRights.Connect)

		Try
			Dim service As IntPtr = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus)
			If service = IntPtr.Zero Then
				Return ServiceState.NotFound
			End If

			Try
				Return GetServiceStatus(service)
			Finally
				CloseServiceHandle(service)
			End Try
		Finally
			CloseServiceHandle(scm)
		End Try
	End Function

	Private Shared Function GetServiceStatus(service As IntPtr) As ServiceState
		Dim status As New SERVICE_STATUS()

		If QueryServiceStatus(service, status) = 0 Then
			Throw New ApplicationException("Failed to query service status.")
		End If

		Return status.dwCurrentState
	End Function

	Private Shared Function WaitForServiceStatus(service As IntPtr, waitStatus As ServiceState, desiredStatus As ServiceState) As Boolean
		Dim status As New SERVICE_STATUS()

		QueryServiceStatus(service, status)
		If status.dwCurrentState = desiredStatus Then
			Return True
		End If

		Dim dwStartTickCount As Integer = Environment.TickCount
		Dim dwOldCheckPoint As Integer = status.dwCheckPoint

		While status.dwCurrentState = waitStatus
			' Do not wait longer than the wait hint. A good interval is
			' one tenth the wait hint, but no less than 1 second and no
			' more than 10 seconds.

			Dim dwWaitTime As Integer = status.dwWaitHint \ 10

			If dwWaitTime < 1000 Then
				dwWaitTime = 1000
			ElseIf dwWaitTime > 10000 Then
				dwWaitTime = 10000
			End If

			Thread.Sleep(dwWaitTime)

			' Check the status again.

			If QueryServiceStatus(service, status) = 0 Then
				Exit While
			End If

			If status.dwCheckPoint > dwOldCheckPoint Then
				' The service is making progress.
				dwStartTickCount = Environment.TickCount
				dwOldCheckPoint = status.dwCheckPoint
			Else
				If Environment.TickCount - dwStartTickCount > status.dwWaitHint Then
					' No progress made within the wait hint
					Exit While
				End If
			End If
		End While
		Return (status.dwCurrentState = desiredStatus)
	End Function

	Private Shared Function OpenSCManager(rights As ScmAccessRights) As IntPtr
		Dim scm As IntPtr = OpenSCManager(Nothing, Nothing, rights)
		If scm = IntPtr.Zero Then
			Throw New ApplicationException("Could not connect to service control manager.")
		End If

		Return scm
	End Function
End Class

Public Enum ServiceState
	Unknown = -1
	' The state cannot be (has not been) retrieved.
	NotFound = 0
	' The service is not known on the host server.
	Stopped = 1
	StartPending = 2
	StopPending = 3
	Running = 4
	ContinuePending = 5
	PausePending = 6
	Paused = 7
End Enum

<Flags> _
Public Enum ScmAccessRights
	Connect = &H1
	CreateService = &H2
	EnumerateService = &H4
	Lock = &H8
	QueryLockStatus = &H10
	ModifyBootConfig = &H20
	StandardRightsRequired = &HF0000
	AllAccess = (StandardRightsRequired Or Connect Or CreateService Or EnumerateService Or Lock Or QueryLockStatus Or ModifyBootConfig)
End Enum

<Flags> _
Public Enum ServiceAccessRights
	QueryConfig = &H1
	ChangeConfig = &H2
	QueryStatus = &H4
	EnumerateDependants = &H8
	Start = &H10
	[Stop] = &H20
	PauseContinue = &H40
	Interrogate = &H80
	UserDefinedControl = &H100
	Delete = &H10000
	StandardRightsRequired = &HF0000
	AllAccess = (StandardRightsRequired Or QueryConfig Or ChangeConfig Or QueryStatus Or EnumerateDependants Or Start Or [Stop] Or PauseContinue Or Interrogate Or UserDefinedControl)
End Enum

Public Enum ServiceBootFlag
	Start = &H0
	SystemStart = &H1
	AutoStart = &H2
	DemandStart = &H3
	Disabled = &H4
End Enum

Public Enum ServiceControl
	[Stop] = &H1
	Pause = &H2
	[Continue] = &H3
	Interrogate = &H4
	Shutdown = &H5
	ParamChange = &H6
	NetBindAdd = &H7
	NetBindRemove = &H8
	NetBindEnable = &H9
	NetBindDisable = &HA
End Enum

Public Enum ServiceError
	Ignore = &H0
	Normal = &H1
	Severe = &H2
	Critical = &H3
End Enum
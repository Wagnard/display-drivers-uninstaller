Option Strict On

Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Security.AccessControl
Imports System.Security.Principal

Imports Microsoft.Win32

Namespace Win32

	Namespace ACL

		Public Module Priviliges

#Region "Consts"

			''' <remarks>https://technet.microsoft.com/en-us/library/dd349804%28v=ws.10%29.aspx</remarks>
			Public Class SE
				''' <summary>Allows a parent process to replace the access token that is associated with a child process.</summary>
				Public Const ASSIGNPRIMARYTOKEN_NAME As String = "SeAssignPrimaryTokenPrivilege"

				''' <summary>Allows a process to generate entries in the security log.
				''' The security log is used to trace unauthorized system access. (See also "Manage auditing and security log" in this table.)</summary>
				Public Const AUDIT_NAME As String = "SeAuditPrivilege"

				''' <summary>Allows the user to circumvent file and directory permissions to back up the system.
				''' The privilege is selected only when an application attempts access through the NTFS backup application programming interface (API).
				''' Otherwise, normal file and directory permissions apply.
				''' 
				''' By default, this privilege is assigned to Administrators and Backup Operators. (See also "Restore files and directories" in this table.) </summary>
				Public Const BACKUP_NAME As String = "SeBackupPrivilege"

				''' <summary>Allows the user to pass through folders to which the user otherwise has no access while navigating an object path
				''' in any Microsoft® Windows® file system or in the registry. This privilege does not allow the user to list the contents of a folder
				''' it allows the user only to traverse its directories.
				''' By default, this privilege is assigned to Administrators, Backup Operators, Power Users, Users, and Everyone.</summary>
				Public Const CHANGE_NOTIFY_NAME As String = "SeChangeNotifyPrivilege"

				''' <summary> Required to create named file mapping objects in the global namespace during Terminal Services sessions.
				''' This privilege is enabled by default for administrators, services, and the local system account.
				''' User Right: Create global objects. Windows XP/2000: This privilege is not supported.
				''' 
				''' Note that this value is supported starting with Windows Server 2003, Windows XP with SP2, and Windows 2000 with SP4.</summary>
				Public Const CREATE_GLOBAL_NAME As String = "SeCreateGlobalPrivilege"

				''' <summary>Allows the user to create and change the size of a pagefile. 
				''' This is done by specifying a paging file size for a particular drive under Performance Options on the Advanced tab of System Properties.
				''' 
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const CREATE_PAGEFILE_NAME As String = "SeCreatePagefilePrivilege"

				''' <summary>Allows a process to create a directory object in the Windows 2000 object manager.
				''' This privilege is useful to kernel-mode components that extend the Windows 2000 object namespace.
				''' 
				''' Components that are running in kernel mode already have this privilege assigned to them;
				''' it is not necessary to assign them the privilege.</summary>
				Public Const CREATE_PERMANENT_NAME As String = "SeCreatePermanentPrivilege"

				''' <summary>Allows a process to create an access token by calling NtCreateToken() or other token-creating APIs.
				''' When a process requires this privilege, use the LocalSystem account (which already includes the privilege),
				''' rather than create a separate user account and assign this privilege to it. </summary>
				Public Const CREATE_TOKEN_NAME As String = "SeCreateTokenPrivilege"

				''' <summary>Allows the user to attach a debugger to any process.
				''' This privilege provides access to sensitive and critical operating system components.
				''' 
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const DEBUG_NAME As String = "SeDebugPrivilege"

				''' <summary>Allows the user to change the Trusted for Delegation setting on a user or computer object in Active Directory. 
				''' The user or computer that is granted this privilege must also have write access to the account control flags on the object.
				''' 
				''' Delegation of authentication is a capability that is used by multi-tier client/server applications. 
				''' It allows a front-end service to use the credentials of a client in authenticating to a back-end service. 
				''' For this to be possible, both client and server must be running under accounts that are trusted for delegation.
				''' 
				''' Misuse of this privilege or the Trusted for Delegation settings can make the network vulnerable to sophisticated attacks on the system
				''' that use Trojan horse programs, which impersonate incoming clients and use their credentials to gain access to network resources. </summary>
				Public Const ENABLE_DELEGATION_NAME As String = "SeEnableDelegationPrivilege"

				''' <summary>Required to impersonate. 
				''' User Right: Impersonate a client after authentication. Windows XP/2000: This privilege is not supported.
				''' 
				''' Note that this value is supported starting with Windows Server 2003, Windows XP with SP2, and Windows 2000 with SP4.</summary>
				Public Const IMPERSONATE_NAME As String = "SeImpersonatePrivilege"

				''' <summary>Allows a process that has Write Property access to another process to increase the processor quota that is assigned to the other process.
				''' This privilege is useful for system tuning, but it can be abused, as in a denial-of-service attack.
				''' 
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const INCREAQUOTA_NAME As String = "SeIncreaseQuotaPrivilege"

				''' <summary>Allows a process that has Write Property access to another process to increase the execution priority of the other process.
				''' A user with this privilege can change the scheduling priority of a process in the Task Manager dialog box.
				''' 
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const INC_BAPRIORITY_NAME As String = "SeIncreaseBasePriorityPrivilege"

				''' <summary>Allows a user to install and uninstall Plug and Play device drivers. 
				''' This privilege does not apply to device drivers that are not Plug and Play; 
				''' these device drivers can be installed only by Administrators. 
				''' 
				''' Note that device drivers run as trusted (highly privileged) programs; 
				''' a user can abuse this privilege by installing hostile programs and giving them destructive access to resources.
				''' 
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const LOAD_DRIVER_NAME As String = "SeLoadDriverPrivilege"

				''' <summary>Allows a process to keep data in physical memory, which prevents the system from paging the data to virtual memory on disk.
				''' Assigning this privilege can result in significant degradation of system performance. 
				''' 
				''' This privilege is obsolete and is therefore never selected.</summary>
				Public Const LOCK_MEMORY_NAME As String = "SeLockMemoryPrivilege"

				''' <summary>Allows the user to add a computer to a specific domain. 
				''' For the privilege to be effective, it must be assigned to the user as part of local security policy for domain controllers in the domain.
				''' A user who has this privilege can add up to 10 workstations to the domain.
				'''
				''' In Windows 2000, the behavior of this privilege is duplicated by the Create Computer Objects permission for organizational units
				''' and the default Computers container in Active DirectorySUP>™ Users who have the Create Computer Objects permission can add an
				''' unlimited number of computers to the domain. </summary>
				Public Const MACHINE_ACCOUNT_NAME As String = "SeMachineAccountPrivilege"

				''' <summary>Required to enable volume management privileges.
				''' User Right: Manage the files on a volume. </summary>
				Public Const MANAGE_VOLUME_NAME As String = "SeManageVolumePrivilege"

				''' <summary>Allows a user to run Microsoft® Windows NT® and Windows 2000 performance-monitoring tools to monitor the performance of nonsystem processes.
				''' By default, this privilege is assigned to Administrators and Power Users. </summary>
				Public Const PROF_SINGLE_PROCESS_NAME As String = "SeProfileSingleProcessPrivilege"

				''' <summary>Allows a user to shut down a computer from a remote location on the network. (See also "Shut down the system" in this table.)
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const REMOTE_SHUTDOWN_NAME As String = "SeRemoteShutdownPrivilege"

				''' <summary>Allows a user to circumvent file and directory permissions when restoring backed-up files and directories
				''' and to set any valid security principal as the owner of an object. (See also "Back up files and directories" in this table.)
				''' By default, this privilege is assigned to Administrators and Backup Operators. </summary>
				Public Const RESTORE_NAME As String = "SeRestorePrivilege"

				''' <summary>Allows a user to specify object access auditing options for individual resources such as files,
				''' Active Directory objects, and registry keys. Object access auditing is not actually performed unless you have enabled
				''' it in Audit Policy (under Security Settings , Local Policies ). 
				''' 
				''' A user who has this privilege also can view and clear the security log from Event Viewer.
				''' By default, this privilege is assigned to Administrators.</summary>
				Public Const SECURITY_NAME As String = "SeSecurityPrivilege"

				''' <summary>Allows a user to shut down the local computer. (See also "Force shutdown from a remote system" in this table.)
				''' 
				''' In Microsoft® Windows® 2000 Professional, this privilege is assigned by default to Administrators, Backup Operators, Power Users, and Users.
				''' In Microsoft® Windows® 2000 Server, this privilege is by default not assigned to Users; it is assigned only to Administrators, Backup Operators, and Power Users.</summary>
				Public Const SHUTDOWN_NAME As String = "SeShutdownPrivilege"

				''' <summary>Allows the user to set the time for the internal clock of the computer.
				''' By default, this privilege is assigned to Administrators and Power Users. </summary>
				Public Const SYSTEMTIME_NAME As String = "SeSystemtimePrivilege"

				''' <summary>Allows modification of system environment variables either by a process through an API or by a user through System Properties .
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const SYSTEM_ENVIRONMENT_NAME As String = "SeSystemEnvironmentPrivilege"

				''' <summary>Allows a user to run Windows NT and Windows 2000 performance-monitoring tools to monitor the performance of system processes.
				''' By default, this privilege is assigned to Administrators. </summary>
				Public Const SYSTEM_PROFILE_NAME As String = "SeSystemProfilePrivilege"

				''' <summary>Allows a user to take ownership of any securable object in the system, 
				''' including Active Directory objects, files and folders, printers, registry keys, processes, and threads.
				''' By default, this privilege is assigned to Administrators.</summary>
				Public Const TAKE_OWNERSHIP_NAME As String = "SeTakeOwnershipPrivilege"

				''' <summary>Allows a process to authenticate like a user and thus gain access to the same resources as a user.
				''' Only low-level authentication services should require this privilege.
				''' 
				''' Note that potential access is not limited to what is associated with the user by default;
				''' the calling process might request that arbitrary additional privileges be added to the access token.
				''' Note that the calling process can also build an anonymous token that does not provide a primary identity for tracking events in the audit log.
				'''
				''' When a service requires this privilege, configure the service to use the LocalSystem account (which already includes the privilege),
				''' rather than create a separate account and assign the privilege to it.</summary>
				Public Const TCB_NAME As String = "SeTcbPrivilege"

				''' <summary>Allows the user of a portable computer to undock the computer by clicking Eject PC on the Start menu.
				''' By default, this privilege is assigned to Administrators, Power Users, and Users. </summary>
				Public Const UNDOCK_NAME As String = "SeUndockPrivilege"

				''' <summary>Required to read unsolicited input from a terminal device.
				''' User Right: Not applicable. </summary>
				Public Const UNSOLICITED_INPUT_NAME As String = "SeUnsolicitedInputPrivilege"
			End Class
#End Region

#Region "Enums"

			<Flags()>
			Private Enum TOKENS As UInt32
				READ_CONTROL = &H20000UI

				STANDARD_RIGHTS_READ = READ_CONTROL
				STANDARD_RIGHTS_WRITE = READ_CONTROL
				STANDARD_RIGHTS_EXECUTE = READ_CONTROL
				STANDARD_RIGHTS_REQUIRED = &HF0000UI

				''' <summary>Required to change the default owner, primary group, or DACL of an access token.</summary>
				ADJUST_DEFAULT = &H80UI

				''' <summary>Required to adjust the attributes of the groups in an access token.</summary>
				ADJUST_GROUPS = &H40UI

				''' <summary>Required to enable or disable the privileges in an access token.</summary>
				ADJUST_PRIVILEGES = &H20UI

				''' <summary>Required to adjust the session ID of an access token.
				''' The SE_TCB_NAME privilege is required.</summary>
				ADJUST_SESSIONID = &H100UI

				''' <summary>Required to attach a primary token to a process. 
				''' The SE_ASSIGNPRIMARYTOKEN_NAME privilege is also required to accomplish this task.</summary>
				ASSIGN_PRIMARY = &H1UI

				''' <summary>Required to duplicate an access token.</summary>
				DUPLICATE = &H2UI

				''' <summary>Combines STANDARD_RIGHTS_EXECUTE and TOKEN_IMPERSONATE.</summary>
				EXECUTE = STANDARD_RIGHTS_EXECUTE Or IMPERSONATE

				''' <summary>Required to attach an impersonation access token to a process.</summary>
				IMPERSONATE = &H4UI

				''' <summary>Required to query an access token.</summary>
				QUERY = &H8UI

				''' <summary>Required to query the source of an access token.</summary>
				QUERY_SOURCE = &H10UI

				''' <summary>Combines STANDARD_RIGHTS_READ and TOKEN_QUERY.</summary>
				READ = STANDARD_RIGHTS_READ Or QUERY

				''' <summary>Combines STANDARD_RIGHTS_WRITE, TOKEN_ADJUST_PRIVILEGES, TOKEN_ADJUST_GROUPS, and TOKEN_ADJUST_DEFAULT.</summary>
				WRITE = STANDARD_RIGHTS_WRITE Or ADJUST_PRIVILEGES Or ADJUST_GROUPS Or ADJUST_DEFAULT

				''' <summary>Combines all possible access rights for a token.</summary>
				ALL_ACCESS_P = STANDARD_RIGHTS_REQUIRED Or ASSIGN_PRIMARY Or DUPLICATE Or IMPERSONATE Or QUERY Or QUERY_SOURCE Or ADJUST_PRIVILEGES Or ADJUST_GROUPS Or ADJUST_DEFAULT

				''' <summary>Combines all possible access rights for a token.</summary>
				ALL_ACCESS = ALL_ACCESS_P Or ADJUST_SESSIONID
			End Enum

			<Flags()>
			Private Enum SE_PRIVILEGE As UInt32
				''' <summary></summary>
				ENABLED_BY_DEFAULT = &H1UI

				''' <summary>The function enables the privilege.</summary>
				ENABLED = &H2UI

				''' <summary>The privilege is removed from the list of privileges in the token.
				''' The other privileges in the list are reordered to remain contiguous.
				''' 
				''' SE_PRIVILEGE_REMOVED supersedes SE_PRIVILEGE_ENABLED.</summary>
				REMOVED = &H4UI

				''' <summary></summary>
				USED_FOR_ACCESS = &H80000000UI
			End Enum

			<Flags()>
			Friend Enum SECURITY_INFORMATION As UInt32
				''' <summary>The owner identifier of the object is being referenced.</summary>
				OWNER_SECURITY_INFORMATION = &H1UI

				''' <summary>The primary group identifier of the object is being referenced.</summary>
				GROUP_SECURITY_INFORMATION = &H2UI

				''' <summary>The DACL of the object is being referenced.</summary>
				DACL_SECURITY_INFORMATION = &H4UI

				''' <summary>The SACL of the object is being referenced.</summary>
				SACL_SECURITY_INFORMATION = &H8UI

				''' <summary>The mandatory integrity label is being referenced.</summary>
				LABEL_SECURITY_INFORMATION = &H10UI

				''' <summary>The SACL inherits access control entries (ACEs) from the parent object.</summary>
				UNPROTECTED_SACL_SECURITY_INFORMATION

				''' <summary>The DACL inherits ACEs from the parent object.</summary>
				UNPROTECTED_DACL_SECURITY_INFORMATION

				''' <summary>The SACL cannot inherit ACEs.</summary>
				PROTECTED_SACL_SECURITY_INFORMATION

				''' <summary>The DACL cannot inherit ACEs.</summary>
				PROTECTED_DACL_SECURITY_INFORMATION

				''' <summary>A SYSTEM_RESOURCE_ATTRIBUTE_ACE (section 2.4.4.15) is being referenced.</summary>
				''' <remarks>https://msdn.microsoft.com/en-us/library/hh877837.aspx</remarks>
				ATTRIBUTE_SECURITY_INFORMATION

				''' <summary>A SYSTEM_SCOPED_POLICY_ID_ACE (section 2.4.4.16) is being referenced.</summary>
				''' <remarks>https://msdn.microsoft.com/en-us/library/hh877846.aspx</remarks>
				SCOPE_SECURITY_INFORMATION

				''' <summary>The security descriptor is being accessed for use in a backup operation.</summary>
				BACKUP_SECURITY_INFORMATION
			End Enum

#End Region

#Region "Structures"

			<StructLayout(LayoutKind.Sequential)>
			Private Structure TOKEN_PRIVILEGES
				Public PrivilegeCount As UInt32
				<MarshalAs(UnmanagedType.ByValArray, SizeConst:=1)>
				Public Privileges() As LUID_AND_ATTRIBUTES
			End Structure

			<StructLayout(LayoutKind.Sequential)>
			Private Structure LUID_AND_ATTRIBUTES
				Public Luid As LUID
				Public Attributes As UInt32
			End Structure

			<StructLayout(LayoutKind.Sequential)>
			Private Structure LUID
				Public LowPart As UInt32
				Public HighPart As UInt32
			End Structure
#End Region

#Region "P/Invoke"
			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function AdjustTokenPrivileges(
   <[In]()> ByVal TokenHandle As IntPtr,
   <[In]()> ByVal DisableAllPrivileges As Boolean,
   <[In](), [Optional]()> ByVal NewState As IntPtr,
   <[In](), [Optional]()> ByVal BufferLength As UInt32,
   <[Out](), [Optional]()> ByVal PreviousState As IntPtr,
   <[Out](), [Optional]()> ByRef ReturnLength As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function OpenProcessToken(
   <[In]()> ByVal ProcessHandle As IntPtr,
   <[In]()> ByVal DesiredAccess As UInt32,
   <[Out]()> ByRef TokenHandle As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function GetCurrentProcess() As IntPtr
			End Function

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function LookupPrivilegeValue(
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpSystemName As String,
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpName As String,
   <[Out]()> ByRef lpLuid As LUID) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("kernel32.dll", SetLastError:=True)>
			Private Function LocalFree(
   <[In]()> ByVal handle As IntPtr) As IntPtr
			End Function

			<DllImport("kernel32.dll", SetLastError:=True)>
			Private Function CloseHandle(
   <[In]()> ByVal hHandle As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

#End Region

#Region "Functions"

			Sub New()
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)
			End Sub

			Public Sub AddPriviliges(ByVal ParamArray priviliges() As String)
				AdjustToken(True, GetCurrentProcess(), priviliges)
			End Sub

			Public Sub RemovePriviliges(ByVal ParamArray priviliges() As String)
				AdjustToken(False, GetCurrentProcess(), priviliges)
			End Sub

			Private Sub AdjustToken(ByVal enable As Boolean, ByVal ptrProcess As IntPtr, ByVal ParamArray priviliges() As String)
				Dim ptrToken As IntPtr = IntPtr.Zero

				Try
					Dim success As Boolean = OpenProcessToken(ptrProcess, TOKENS.ADJUST_PRIVILEGES Or TOKENS.QUERY, ptrToken)

					If Not success Then
						Throw New Win32Exception()
					End If

					Dim luid As LUID
					Dim luidAndAttributes As New List(Of LUID_AND_ATTRIBUTES)
					Dim requiredSize As UInt32

					For Each privilige In priviliges
						If Not LookupPrivilegeValue(Nothing, privilige, luid) Then
							Throw New Win32Exception()
						End If

						Using newState = New StructPtr(New TOKEN_PRIVILEGES With
						 {
						  .PrivilegeCount = 1,
						  .Privileges =
						  {
						   New LUID_AND_ATTRIBUTES() With
						   {
						 .Luid = luid,
						 .Attributes = If(enable, SE_PRIVILEGE.ENABLED, SE_PRIVILEGE.REMOVED)
						   }
						  }
						 })

							If Not AdjustTokenPrivileges(ptrToken, False, newState.Ptr, 0UI, IntPtr.Zero, requiredSize) Then
								Dim err As UInt32 = GetLastWin32ErrorU()

								If err <> Errors.INSUFFICIENT_BUFFER AndAlso err <> Errors.NOT_ALL_ASSIGNED Then
									Throw New Win32Exception(GetInt32(err))
								End If
							End If

						End Using

					Next

				Catch ex As Exception
					ShowException(ex)
				Finally
					If ptrToken <> IntPtr.Zero Then
						CloseHandle(ptrToken)
					End If
				End Try
			End Sub

#End Region

		End Module

		Public Module FileSystem
#Region "Consts"

			Private ReadOnly _sidSystem As SecurityIdentifier = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
			Private ReadOnly _sidAdmin As SecurityIdentifier = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)
			Private ReadOnly _sidAuthUser As SecurityIdentifier = New SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, Nothing)
#End Region

#Region "Enums"

			Private Enum SE_OBJECT_TYPE As UInt32
				''' <summary>Unknown object type.</summary>
				SE_UNKNOWN_OBJECT_TYPE

				''' <summary>Indicates a file or directory. 
				''' The name string that identifies a file or directory object can be in one of the following formats:
				''' 
				''' - A relative path, such as FileName.dat or ..\FileName
				''' - An absolute path, such as FileName.dat, C:\DirectoryName\FileName.dat, or G:\RemoteDirectoryName\FileName.dat.
				''' - A UNC name, such as \\ComputerName\ShareName\FileName.dat.</summary>
				SE_FILE_OBJECT

				''' <summary>Indicates a Windows service. 
				''' A service object can be a local service, such as ServiceName, or a remote service, such as \\ComputerName\ServiceName.</summary>
				SE_SERVICE

				''' <summary>Indicates a printer.
				''' A printer object can be a local printer, such as PrinterName, or a remote printer, such as \\ComputerName\PrinterName.</summary>
				SE_PRINTER

				''' <summary>Indicates a registry key. 
				''' A registry key object can be in the local registry, such as CLASSES_ROOT\SomePath or in a remote registry,
				''' such as \\ComputerName\CLASSES_ROOT\SomePath.
				''' 
				''' The names of registry keys must use the following literal strings to identify the predefined registry keys:
				''' "CLASSES_ROOT", "CURRENT_USER", "MACHINE", and "USERS".</summary>
				SE_REGISTRY_KEY

				''' <summary>Indicates a network share.
				''' A share object can be local, such as ShareName, or remote, such as \\ComputerName\ShareName.</summary>
				SE_LMSHARE

				''' <summary>Indicates a local kernel object.
				''' The GetSecurityInfo and SetSecurityInfo functions support all types of kernel objects. 
				''' The GetNamedSecurityInfo and SetNamedSecurityInfo functions work only with the following kernel objects: 
				''' semaphore, event, mutex, waitable timer, and file mapping.</summary>
				SE_KERNEL_OBJECT

				''' <summary>Indicates a window station or desktop object on the local computer.
				''' You cannot use GetNamedSecurityInfo and SetNamedSecurityInfo with these objects
				''' because the names of window stations or desktops are not unique.</summary>
				SE_WINDOW_OBJECT

				''' <summary>Indicates a directory service object or a property set or property of a directory service object.
				''' The name string for a directory service object must be in X.500 form, for example:
				'''
				''' CN=SomeObject,OU=ou2,OU=ou1,DC=DomainName,DC=CompanyName,DC=com,O=internet</summary>
				SE_DS_OBJECT

				''' <summary>Indicates a directory service object and all of its property sets and properties. </summary>
				SE_DS_OBJECT_ALL

				''' <summary>Indicates a provider-defined object.</summary>
				SE_PROVIDER_DEFINED_OBJECT

				''' <summary>Indicates a WMI object.</summary>
				SE_WMIGUID_OBJECT

				''' <summary>Indicates an object for a registry entry under WOW64. </summary>
				SE_REGISTRY_WOW64_32KEY
			End Enum

			Private Enum FILE_OPEN As UInt32
				CREATE_NEW = 1
				CREATE_ALWAYS = 2
				OPEN_EXISTING = 3
				OPEN_ALWAYS = 4
				TRUNCATE_EXISTING = 5
			End Enum

			<Flags()>
			Private Enum FILE_RIGHTS As UInt32
				FILE_READ_ATTRIBUTES = &H80UI

				DELETE = &H10000UI
				READ_CONTROL = &H20000UI
				WRITE_DAC = &H40000UI
				WRITE_OWNER = &H80000UI
				SYNCHRONIZE = &H100000UI

				STANDARD_RIGHTS_READ = READ_CONTROL
				STANDARD_RIGHTS_WRITE = READ_CONTROL
				STANDARD_RIGHTS_EXECUTE = READ_CONTROL
				STANDARD_RIGHTS_REQUIRED = &HF0000UI
				STANDARD_RIGHTS_ALL = &H1F0000UI
			End Enum

			Private Enum FILE_SHARE As UInt32
				NONE = &H0UI
				READ = &H1UI
				WRITE = &H2UI
				DELETE = &H4UI
			End Enum

#End Region

#Region "P/Invoke"

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function GetSecurityInfo(
   <[In]()> ByVal handle As IntPtr,
   <[In]()> ByVal ObjectType As SE_OBJECT_TYPE,
   <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
   <[Out](), [Optional]()> ByRef psidOwner As IntPtr,
   <[Out](), [Optional]()> ByRef psidGroup As IntPtr,
   <[Out](), [Optional]()> ByRef pDacl As IntPtr,
   <[Out](), [Optional]()> ByRef pSacl As IntPtr,
   <[Out](), [Optional]()> ByRef ppSecurityDescriptor As IntPtr) As UInt32
			End Function

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function SetSecurityInfo(
   <[In]()> ByVal handle As IntPtr,
   <[In]()> ByVal ObjectType As SE_OBJECT_TYPE,
   <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
   <[In](), [Optional]()> ByVal psidOwner As IntPtr,
   <[In](), [Optional]()> ByVal psidGroup As IntPtr,
   <[In](), [Optional]()> ByVal pDacl As IntPtr,
   <[In](), [Optional]()> ByVal pSacl As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function ConvertSidToStringSid(
   <[In]()> ByVal Sid As IntPtr,
   <[Out]()> ByRef StringSid As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function ConvertStringSidToSid(
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal Sid As String,
   <[Out]()> ByRef StringSid As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("Advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function LookupAccountSid(
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpSystemName As String,
   <[In]()> ByVal lpSid As IntPtr,
   <[Out](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpName As Text.StringBuilder,
   <[In](), [Out]()> ByRef cchName As UInt32,
   <[Out](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpReferencedDomainName As Text.StringBuilder,
   <[In](), [Out]()> ByRef cchReferencedDomainName As UInt32,
   <[Out]()> ByRef peUse As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("Kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function CreateFile(
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String,
   <[In]()> ByVal dwDesiredAccess As FILE_RIGHTS,
   <[In]()> ByVal dwShareMode As FILE_SHARE,
   <[In](), [Optional]()> ByVal lpSecurityAttribute As IntPtr,
   <[In]()> ByVal dwCreationDisposition As FILE_OPEN,
   <[In]()> ByVal dwFlagsAndAttributes As UInt32,
   <[In](), [Optional]()> ByVal hTemplateFile As IntPtr) As IntPtr
			End Function

			<DllImport("Kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function CloseHandle(
   <[In]()> ByVal hObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
			End Function

			<DllImport("Kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Function LocalFree(
   <[In]()> ByVal hMem As IntPtr) As IntPtr
			End Function

#End Region

#Region "Functions"

			Sub New()
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)
			End Sub

			' Adds an ACL entry on the specified directory for the specified account.
			Public Sub AddDirectorySecurity(ByVal path As String, ByVal Rights As FileSystemRights, ByVal ControlType As AccessControlType)
				' Create a new DirectoryInfoobject.
				Dim dInfo As New DirectoryInfo(path)

				Dim sid = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)
				' Get a DirectorySecurity object that represents the 
				' current security settings.
				'Dim dSecurity As DirectorySecurity = dInfo.GetAccessControl()
				'Activate necessary admin privileges to make changes without NTFS perms
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)

				'Create a new acl from scratch.
				'Dim newacl As New System.Security.AccessControl.DirectorySecurity()
				Dim newacl As System.Security.AccessControl.DirectorySecurity = Directory.GetAccessControl(path, AccessControlSections.Owner)
				'set owner only here (needed for WinXP)
				newacl.SetOwner(sid)
				dInfo.SetAccessControl(newacl)
				'This remove inheritance.
				newacl.SetAccessRuleProtection(False, True)

				newacl = Directory.GetAccessControl(path)
				' Add the FileSystemAccessRule to the security settings. 
				newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))

				sid = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
				newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))

				sid = New SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, Nothing)
				newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))

				' Set the new access settings.
				dInfo.SetAccessControl(newacl)
			End Sub

			Public Sub Addregistrysecurity(ByVal regkey As RegistryKey, ByVal subkeyname As String, ByVal Rights As RegistryRights, ByVal ControlType As AccessControlType)

				Dim rs As New RegistrySecurity()
				Dim sid = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)

				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)

				'Dim originalsid = regkey.OpenSubKey(subkeyname, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions).GetAccessControl.GetOwner(GetType(System.Security.Principal.SecurityIdentifier))
				'MsgBox(originalsid.ToString)
				Using subkey As RegistryKey = regkey.OpenSubKey(subkeyname, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership)
					rs.SetOwner(sid)

					' Set the new access settings.Owner
					subkey.SetAccessControl(rs)
					rs.SetAccessRuleProtection(False, True)

					'rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))
					sid = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
					rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))

					'sid = New SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, Nothing)
					'rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))
					' Set the new access settings.
					subkey.SetAccessControl(rs)
				End Using
			End Sub

			' Removes an ACL entry on the specified directory for the specified account.
			Public Sub RemoveDirectorySecurity(ByVal FileName As String, ByVal Account As String, ByVal Rights As FileSystemRights, ByVal ControlType As AccessControlType)
				' Create a new DirectoryInfo object.
				Dim dInfo As New DirectoryInfo(FileName)

				' Get a DirectorySecurity object that represents the 
				' current security settings.
				Dim dSecurity As DirectorySecurity = dInfo.GetAccessControl()

				' Add the FileSystemAccessRule to the security settings. 
				dSecurity.RemoveAccessRule(New FileSystemAccessRule(Account, Rights, ControlType))

				' Set the new access settings.
				dInfo.SetAccessControl(dSecurity)

			End Sub


			' path = File or Dir, deleteRights = Path (dir or file) going to be deleted?
			Friend Function FixFileSecurity(ByVal uncPath As String, ByVal deleteRights As Boolean, ByRef logEntry As LogEntry) As Boolean
				If Not deleteRights Then
					Return False	' Do not change rights if not going to delete it. NEED TO FIX...
				End If

				Dim logEvents As Boolean = logEntry IsNot Nothing
				Dim errCode As UInt32 = 0UI
				Dim ptrFile As IntPtr = IntPtr.Zero

				Try
					ptrFile = CreateFile(uncPath, FILE_RIGHTS.READ_CONTROL Or FILE_RIGHTS.FILE_READ_ATTRIBUTES, FILE_SHARE.NONE, IntPtr.Zero, FILE_OPEN.OPEN_EXISTING, FileIO.FILE_ATTRIBUTES.NORMAL Or FileIO.FILE_ATTRIBUTES.FLAG_BACKUP_SEMANTICS Or FileIO.FILE_ATTRIBUTES.FLAG_OPEN_REPARSE_POINT, IntPtr.Zero)

					errCode = GetLastWin32ErrorU()

					If errCode = Errors.PATH_NOT_FOUND OrElse errCode = Errors.FILE_NOT_FOUND Then
						Return False
					End If

					If errCode <> 0UI Then
						Throw New Win32Exception(GetInt32(errCode))
					End If

					Dim ptrOwner As IntPtr = IntPtr.Zero
					Dim ptrGroup As IntPtr = IntPtr.Zero
					Dim ptrDACL As IntPtr = IntPtr.Zero
					Dim ptrSACL As IntPtr = IntPtr.Zero
					Dim ptrSecurity As IntPtr = IntPtr.Zero

					Try
						errCode = GetSecurityInfo(ptrFile, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION Or SECURITY_INFORMATION.BACKUP_SECURITY_INFORMATION, ptrOwner, ptrGroup, ptrDACL, ptrSACL, ptrSecurity)

						If errCode <> 0UI Then
							Throw New Win32Exception(GetInt32(errCode))
						End If

						Dim ptrOwnerStr As IntPtr = IntPtr.Zero
						Dim strOwner As String

						If logEvents Then		' Log current Owner
							Try
								If Not ConvertSidToStringSid(ptrOwner, ptrOwnerStr) Then
									Throw New Win32Exception(GetLastWin32Error())
								End If

								strOwner = Marshal.PtrToStringUni(ptrOwnerStr)
								logEntry.Add("OwnerSID", strOwner)

								Dim sbName As New Text.StringBuilder(260)
								Dim sbDomain As New Text.StringBuilder(260)
								Dim sizeName As UInt32 = GetUInt32(sbName.Capacity)
								Dim sizeDomain As UInt32 = GetUInt32(sbName.Capacity)

								If Not LookupAccountSid(Nothing, ptrOwner, sbName, sizeName, sbDomain, sizeDomain, 0UI) Then
									Throw New Win32Exception(GetLastWin32Error())
								End If

								logEntry.Add("OwnerDomain", sbDomain.ToString())
								logEntry.Add("OwnerName", sbName.ToString())
							Catch ex As Win32Exception
								logEntry.Add("Couldn't find current path's Owner!")
							Finally
								If ptrOwnerStr <> IntPtr.Zero Then
									Marshal.FreeHGlobal(ptrOwnerStr)
								End If
							End Try
						End If

						If deleteRights Then
							strOwner = _sidSystem.ToString()

							Try
								If Not ConvertStringSidToSid(strOwner, ptrOwnerStr) Then
									Throw New Win32Exception(GetLastWin32Error())
								End If

								If Not SetSecurityInfo(ptrFile, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, ptrOwnerStr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) Then
									Throw New Win32Exception(GetLastWin32Error())
								End If

								Return True
							Finally
								If ptrOwnerStr <> IntPtr.Zero Then
									Marshal.FreeHGlobal(ptrOwnerStr)
								End If
							End Try
						Else

							' WORK IN PROGRESS
							' 
							' GetFiles / GetDirectories fails in some scenarios
							' ... Forgot to check for Error code if no files "found" 
							' => ACCESS_DENIED => No files
							' => No files "found" => Directory can't be removed (contains files which couldn't find)

							Return False
						End If
					Finally
						If ptrSecurity <> IntPtr.Zero Then
							LocalFree(ptrSecurity)
						End If
					End Try
				Finally
					If ptrFile <> IntPtr.Zero Then
						CloseHandle(ptrFile)
					End If
				End Try
			End Function

#End Region

		End Module

		Public Class Registry

#Region "Consts"
			'For Testing
			'Private Shared ReadOnly SYSTEM_ACCOUNT As IdentityReference = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)	
			Private Shared ReadOnly SYSTEM_ACCOUNT As IdentityReference = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
			Private Shared ReadOnly HKEY_CLASSES_ROOT As IntPtr = New IntPtr(-2147483648)
			Private Shared ReadOnly HKEY_CURRENT_USER As IntPtr = New IntPtr(-2147483647)
			Private Shared ReadOnly HKEY_LOCAL_MACHINE As IntPtr = New IntPtr(-2147483646)
			Private Shared ReadOnly HKEY_USERS As IntPtr = New IntPtr(-2147483645)
			Private Shared ReadOnly HKEY_CURRENT_CONFIG As IntPtr = New IntPtr(-2147483643)
			'	Private ReadOnly HKEY_PERFORMANCE_DATA As IntPtr = New IntPtr(-2147483644)
			'	Private ReadOnly HKEY_DYN_DATA As IntPtr = New IntPtr(-2147483642)

#End Region

#Region "Enums"

			<Flags()>
			Private Enum REGSAM As UInt32
				''' <summary>Permission to query subkey data.</summary>
				KEY_QUERY_VALUE = &H1UI

				''' <summary>Permission to set subkey data.</summary>
				KEY_SET_VALUE = &H2UI

				''' <summary>Permission to create subkeys.
				''' Subkeys directly underneath the 'HKEY_LOCAL_MACHINE' and 'HKEY_USERS'
				''' predefined keys cannot be created even if this bit is set.</summary>
				KEY_CREATE_SUB_KEY = &H4UI

				''' <summary>Permission to enumerate subkeys.</summary>
				KEY_ENUMERATE_SUB_KEYS = &H8UI

				''' <summary>Permission to create a symbolic link.</summary>
				KEY_CREATE_LINK = &H20UI

				''' <summary>When set, indicates that a registry server on a 64-bit operating system operates on the 64-bit key namespace.</summary>
				KEY_WOW64_64KEY = &H100UI

				''' <summary>When set, indicates that a registry server on a 64-bit operating system operates on the 32-bit key namespace.</summary>
				KEY_WOW64_32KEY = &H200UI

				''' <summary>Permission for read access.</summary>
				KEY_EXECUTE = &H20019UI

				''' <summary>Permission for change notification.</summary>
				KEY_NOTIFY = &H10UI

				''' <summary>Combination of KEY_QUERY_VALUE, KEY_ENUMERATE_SUB_KEYS, and KEY_NOTIFY access.</summary>
				KEY_READ = &H20019UI

				''' <summary>Combination of KEY_SET_VALUE and KEY_CREATE_SUB_KEY access.</summary>
				KEY_WRITE = &H20006UI

				''' <summary>Combination of KEY_QUERY_VALUE, KEY_ENUMERATE_SUB_KEYS, KEY_NOTIFY, KEY_CREATE_SUB_KEY, KEY_CREATE_LINK, and KEY_SET_VALUE access.</summary>
				KEY_ALL_ACCESS = &H2F003FUI
			End Enum

			<Flags()>
			Private Enum REG_OPTION As UInt32
				''' <summary>This key is not volatile; this is the default.
				''' The information is stored in a file and is preserved when the system is restarted.
				''' The RegSaveKey function saves keys that are not volatile.</summary>
				NON_VOLATILE = &H0UI

				''' <summary>All keys created by the function are volatile.
				''' The information is stored in memory and is not preserved when the corresponding registry hive is unloaded.
				''' For HKEY_LOCAL_MACHINE, this occurs only when the system initiates a full shutdown.
				''' For registry keys loaded by the RegLoadKey function, this occurs when the corresponding RegUnLoadKey is performed. 
				''' The RegSaveKey function does not save volatile keys. This flag is ignored for keys that already exist. 
				''' 
				''' Note: On a user selected shutdown, a fast startup shutdown is the default behavior for the system.</summary>
				VOLATILE = &H1UI

				''' <summary>This key is a symbolic link. 
				''' The target path is assigned to the L"SymbolicLinkValue" value of the key.
				''' The target path must be an absolute registry path.
				''' 
				''' Note: Registry symbolic links should only be used for for application compatibility when absolutely necessary.</summary>
				CREATE_LINK = &H2UI

				''' <summary>If this flag is set, the function ignores the samDesired parameter and attempts to open the key with the access required to backup or restore the key. 
				''' If the calling thread has the SE_BACKUP_NAME privilege enabled, 
				''' the key is opened with the ACCESS_SYSTEM_SECURITY and KEY_READ access rights.
				''' 
				''' If the calling thread has the SE_RESTORE_NAME privilege enabled, beginning with Windows Vista, 
				''' the key is opened with the ACCESS_SYSTEM_SECURITY, DELETE and KEY_WRITE access rights. 
				''' 
				''' If both privileges are enabled, the key has the combined access rights for both privileges. 
				''' For more information, see Running with Special Privileges.</summary>
				''' <remarks>https://msdn.microsoft.com/en-us/library/windows/desktop/ms717802(v=vs.85).aspx</remarks>
				BACKUP_RESTORE = &H4UI
			End Enum

			Private Enum REG_RESULT As UInt32
				''' <summary>The key did not exist and was created.</summary>
				REG_CREATED_NEW_KEY = &H1UI

				''' <summary>The key existed and was simply opened without being changed.</summary>
				REG_OPENED_EXISTING_KEY = &H2UI
			End Enum

#End Region

#Region "P/Invoke"

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegOpenKeyEx(
   <[In]()> ByVal hKey As IntPtr,
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal subKey As String,
   <[In]()> ByVal ulOptions As UInt32,
   <[In]()> ByVal samDesired As REGSAM,
   <[Out]()> ByRef phkResult As IntPtr) As UInt32
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegCloseKey(
 <[In]()> ByVal hKey As IntPtr) As UInt32
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegDeleteKey(
 <[In]()> ByVal hKey As IntPtr,
 <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpSubKey As String,
 <[In]()> ByVal samDesired As REGSAM,
 <[In]()> ByVal Reserved As UInt32) As UInt32
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegCreateKeyEx(
   <[In]()> ByVal hKey As IntPtr,
   <[In]()> lpSubKey As String,
   ByVal Reserved As Integer,
   <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> lpClass As String,
   <[In]()> dwOptions As REG_OPTION,
   <[In]()> samDesired As REGSAM,
   <[In](), [Optional]()> lpSecurityAttributes As IntPtr,
   <[Out]()> ByRef hkResult As IntPtr,
   <[Out](), [Optional]()> ByRef lpdwDisposition As UInt32) As UInt32
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegGetKeySecurity(
   <[In]()> ByVal hKey As IntPtr,
   <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
   <[In](), [Out](), [Optional]()> ByVal pSecurityDescriptor() As Byte,
   <[In](), [Out]()> ByRef lpcbSecurityDescriptor As UInt32) As UInt32
			End Function

			<DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
			Private Shared Function RegSetKeySecurity(
   <[In]()> ByVal hKey As IntPtr,
   <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
   <[In]()> ByVal pSecurityDescriptor() As Byte) As UInt32
			End Function

#End Region

#Region "Functions"

			Shared Sub New()
				ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME)
			End Sub

			''' <summary>
			''' Close key before using this and reopen after. Otherwise changes may not be applied!
			''' </summary>
			''' <param name="fullPath">Fullpath of regkey including beginning HKEY_ part
			''' eg.  HKEY_LOCAL_MACHINE\SOFTWARE\ATI</param>
			''' <returns>
			''' True = OK
			''' False = couldn't be fix'd or error thrown (added to log)</returns>
			Public Shared Function FixRights(ByVal fullPath As String) As Boolean
				Dim ptrRegKey As IntPtr = IntPtr.Zero
				Dim retVal As UInt32
				Dim ownerModified As Boolean = False
				Dim rootKey As IntPtr = GetRootKey(fullPath)
				Dim pathKey As String = fullPath.Substring(fullPath.IndexOf("\"c) + 1)
				Dim previousOwner As IdentityReference = Nothing

				Try
					retVal = RegOpenKeyEx(rootKey, pathKey, 0UI, REGSAM.KEY_READ Or REGSAM.KEY_WOW64_64KEY, ptrRegKey)

					If retVal <> 0UI Then
						If retVal = 5UI Then
							Dim returnAction As UInt32 = 0UI

							retVal = RegCreateKeyEx(rootKey, pathKey, 0UI, Nothing, REG_OPTION.BACKUP_RESTORE, REGSAM.KEY_READ Or REGSAM.KEY_WOW64_64KEY, IntPtr.Zero, ptrRegKey, returnAction)

							If returnAction = REG_RESULT.REG_CREATED_NEW_KEY Then
								DeleteKey(rootKey, pathKey)
								Return False
							ElseIf returnAction = REG_RESULT.REG_OPENED_EXISTING_KEY Then
							Else
								Throw New Win32Exception(GetInt32(retVal))
								Exit Function
							End If
						ElseIf retVal = 2UI Then
							Return False	' Key doesn't exists
						Else
							Throw New Win32Exception(GetInt32(retVal))
						End If
					End If


					Dim rs As New RegistrySecurity


					retVal = GetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
					If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))
					previousOwner = rs.GetOwner(GetType(SecurityIdentifier))

					rs.SetOwner(SYSTEM_ACCOUNT)
					retVal = SetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
					If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))

					ownerModified = True

					SetAccessRights(ptrRegKey,
					  New RegistryAccessRule(
					   SYSTEM_ACCOUNT,
					   RegistryRights.FullControl,
					   InheritanceFlags.ContainerInherit Or InheritanceFlags.ObjectInherit,
					   PropagationFlags.None,
					   AccessControlType.Allow)
					  )


					retVal = GetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
					If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))

					rs.SetOwner(previousOwner)
					retVal = SetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
					If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))

					ownerModified = False
					FixRights = True
				Catch ex As Exception
					Application.Log.AddException(ex)
					Return False
				Finally
					Try
						If ownerModified Then
							Dim rs As New RegistrySecurity

							retVal = GetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
							If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))

							rs.SetOwner(previousOwner)
							retVal = SetSD(ptrRegKey, rs, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
							If retVal <> 0 Then Throw New Win32Exception(GetInt32(retVal))

							ownerModified = False
						End If
					Catch ex As Exception
						FixRights = False
						Application.Log.AddWarningMessage("Registry key's owner changed, but permission couldn't be fixed. Owner wasn't restored!", "fullPath", fullPath)
					Finally
						If ptrRegKey <> IntPtr.Zero Then
							RegCloseKey(ptrRegKey)
						End If
					End Try
				End Try

				Return FixRights
			End Function

			Private Shared Function GetRootKey(ByVal name As String) As IntPtr
				Select Case True
					Case name.StartsWith("HKEY_CLASSES_ROOT", StringComparison.OrdinalIgnoreCase)
						Return HKEY_CLASSES_ROOT
					Case name.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase)
						Return HKEY_CURRENT_USER
					Case name.StartsWith("HKEY_CURRENT_CONFIG", StringComparison.OrdinalIgnoreCase)
						Return HKEY_CURRENT_CONFIG
					Case name.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase)
						Return HKEY_LOCAL_MACHINE
					Case name.StartsWith("HKEY_USERS", StringComparison.OrdinalIgnoreCase)
						Return HKEY_USERS
					Case Else
						Throw New ArgumentException("name is unknown!" & Environment.NewLine & name, "name")
				End Select
			End Function

			Private Shared Sub DeleteKey(ByVal rootKey As IntPtr, ByVal pathKey As String)
				Dim retVal As UInt32 = RegDeleteKey(rootKey, pathKey, REGSAM.KEY_WRITE, 0UI)

				If retVal <> 0UI AndAlso retVal <> 2UI Then
					Throw New Win32Exception(GetInt32(retVal))
				End If
			End Sub

			Private Shared Sub SetAccessRights(ByVal ptrRegKey As IntPtr, ByVal regAccessRule As RegistryAccessRule)
				Dim rs As New RegistrySecurity
				Dim returnValue As UInt32 = GetSD(ptrRegKey, rs, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION Or SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION Or SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION)
				If returnValue <> 0 Then Throw New Win32Exception(GetInt32(returnValue))

				rs.AddAccessRule(regAccessRule)
				rs.SetAccessRuleProtection(False, True)

				returnValue = SetSD(ptrRegKey, rs, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION Or SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION Or SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION)
				If returnValue <> 0 Then Throw New Win32Exception(GetInt32(returnValue))
			End Sub

			Private Shared Function GetSD(ByVal ptrRegKey As IntPtr, ByRef rs As RegistrySecurity, ByVal securityInformation As SECURITY_INFORMATION) As UInt32
				rs = New RegistrySecurity()

				Dim ptrSecurityBytes() As Byte = Nothing
				Dim requiredSize As UInt32 = 0UI
				Dim returnValue As UInt32 = RegGetKeySecurity(ptrRegKey, securityInformation, ptrSecurityBytes, requiredSize)

				If returnValue <> 122UI Then
					Return returnValue
				End If

				ReDim ptrSecurityBytes(GetInt32(requiredSize) - 1)

				returnValue = RegGetKeySecurity(ptrRegKey, securityInformation, ptrSecurityBytes, requiredSize)

				If returnValue <> 0UI Then
					Return returnValue
				End If

				rs.SetSecurityDescriptorBinaryForm(ptrSecurityBytes)

				Return 0UI
			End Function

			Private Shared Function SetSD(ByVal ptrRegKey As IntPtr, ByRef rs As RegistrySecurity, ByVal securityInformation As SECURITY_INFORMATION) As UInt32
				Return RegSetKeySecurity(ptrRegKey, securityInformation, rs.GetSecurityDescriptorBinaryForm())
			End Function

#End Region

		End Class

	End Namespace

End Namespace
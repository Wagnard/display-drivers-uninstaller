Option Strict On

Imports System.ComponentModel
Imports System.Runtime.InteropServices

Namespace ACL
    Public Module NativeMethods

#Region "Consts"
        Private ReadOnly HKEY_CLASSES_ROOT As IntPtr = New IntPtr(-2147483648)
        Private ReadOnly HKEY_CURRENT_USER As IntPtr = New IntPtr(-2147483647)
        Private ReadOnly HKEY_LOCAL_MACHINE As IntPtr = New IntPtr(-2147483646)
        Private ReadOnly HKEY_USERS As IntPtr = New IntPtr(-2147483645)
        Private ReadOnly HKEY_PERFORMANCE_DATA As IntPtr = New IntPtr(-2147483644)
        Private ReadOnly HKEY_CURRENT_CONFIG As IntPtr = New IntPtr(-2147483643)
        Private ReadOnly HKEY_DYN_DATA As IntPtr = New IntPtr(-2147483642)

        Public Const SE_ASSIGNPRIMARYTOKEN_NAME As String = "SeAssignPrimaryTokenPrivilege"
        Public Const SE_AUDIT_NAME As String = "SeAuditPrivilege"
        Public Const SE_BACKUP_NAME As String = "SeBackupPrivilege"
        Public Const SE_CHANGE_NOTIFY_NAME As String = "SeChangeNotifyPrivilege"
        Public Const SE_CREATE_GLOBAL_NAME As String = "SeCreateGlobalPrivilege"
        Public Const SE_CREATE_PAGEFILE_NAME As String = "SeCreatePagefilePrivilege"
        Public Const SE_CREATE_PERMANENT_NAME As String = "SeCreatePermanentPrivilege"
        Public Const SE_CREATE_TOKEN_NAME As String = "SeCreateTokenPrivilege"
        Public Const SE_DEBUG_NAME As String = "SeDebugPrivilege"
        Public Const SE_ENABLE_DELEGATION_NAME As String = "SeEnableDelegationPrivilege"
        Public Const SE_IMPERSONATE_NAME As String = "SeImpersonatePrivilege"
        Public Const SE_INCREASE_QUOTA_NAME As String = "SeIncreaseQuotaPrivilege"
        Public Const SE_INC_BASE_PRIORITY_NAME As String = "SeIncreaseBasePriorityPrivilege"
        Public Const SE_LOAD_DRIVER_NAME As String = "SeLoadDriverPrivilege"
        Public Const SE_LOCK_MEMORY_NAME As String = "SeLockMemoryPrivilege"
        Public Const SE_MACHINE_ACCOUNT_NAME As String = "SeMachineAccountPrivilege"
        Public Const SE_MANAGE_VOLUME_NAME As String = "SeManageVolumePrivilege"
        Public Const SE_PROF_SINGLE_PROCESS_NAME As String = "SeProfileSingleProcessPrivilege"
        Public Const SE_REMOTE_SHUTDOWN_NAME As String = "SeRemoteShutdownPrivilege"
        Public Const SE_RESTORE_NAME As String = "SeRestorePrivilege"
        Public Const SE_SECURITY_NAME As String = "SeSecurityPrivilege"
        Public Const SE_SHUTDOWN_NAME As String = "SeShutdownPrivilege"
        Public Const SE_SYSTEMTIME_NAME As String = "SeSystemtimePrivilege"
        Public Const SE_SYSTEM_ENVIRONMENT_NAME As String = "SeSystemEnvironmentPrivilege"
        Public Const SE_SYSTEM_PROFILE_NAME As String = "SeSystemProfilePrivilege"
        Public Const SE_TAKE_OWNERSHIP_NAME As String = "SeTakeOwnershipPrivilege"
        Public Const SE_TCB_NAME As String = "SeTcbPrivilege"
        Public Const SE_UNDOCK_NAME As String = "SeUndockPrivilege"
        Public Const SE_UNSOLICITED_INPUT_NAME As String = "SeUnsolicitedInputPrivilege"
#End Region

#Region "Enums"

        Private Enum SID_NAME_USE As Integer
            SidTypeUser = 1
            SidTypeGroup
            SidTypeDomain
            SidTypeAlias
            SidTypeWellKnownGroup
            SidTypeDeletedAccount
            SidTypeInvalid
            SidTypeUnknown
            SidTypeComputer
        End Enum

        Private Enum SE_OBJECT_TYPE As UInt32
            SE_UNKNOWN_OBJECT_TYPE
            SE_FILE_OBJECT
            SE_SERVICE
            SE_PRINTER
            SE_REGISTRY_KEY
            SE_LMSHARE
            SE_KERNEL_OBJECT
            SE_WINDOW_OBJECT
            SE_DS_OBJECT
            SE_DS_OBJECT_ALL
            SE_PROVIDER_DEFINED_OBJECT
            SE_WMIGUID_OBJECT
            SE_REGISTRY_WOW64_32KEY
        End Enum

        <Flags()>
        Private Enum SECURITY_INFORMATION As UInt32
            OWNER_SECURITY_INFORMATION = &H1UI
            GROUP_SECURITY_INFORMATION = &H2UI
            DACL_SECURITY_INFORMATION = &H4UI
            SACL_SECURITY_INFORMATION = &H8UI
        End Enum

        <Flags()>
        Private Enum REGSAM As UInt32
            KEY_QUERY_VALUE = &H1UI
            KEY_SET_VALUE = &H2UI
            KEY_CREATE_SUB_KEY = &H4UI
            KEY_ENUMERATE_SUB_KEYS = &H8UI
            KEY_CREATE_LINK = &H20UI
            KEY_WOW64_64KEY = &H100UI
            KEY_WOW64_32KEY = &H200UI

            KEY_EXECUTE = &H20019UI
            KEY_NOTIFY = &H10UI
            KEY_READ = &H20019UI
            KEY_WRITE = &H20006UI
            KEY_ALL_ACCESS = &H2F003FUI
        End Enum

        <Flags()>
        Private Enum REG_OPTION As UInt32
            NON_VOLATILE = &H0UI
            VOLATILE = &H1UI
            CREATE_LINK = &H2UI
            BACKUP_RESTORE = &H4UI
        End Enum

        <Flags()>
        Private Enum TOKENS As UInt32
            READ_CONTROL = &H20000UI

            STANDARD_RIGHTS_READ = READ_CONTROL
            STANDARD_RIGHTS_WRITE = READ_CONTROL
            STANDARD_RIGHTS_EXECUTE = READ_CONTROL
            STANDARD_RIGHTS_REQUIRED = &HF0000UI

            <Description("Required to change the default owner, primary group, or DACL of an access token.")>
            ADJUST_DEFAULT = &H80UI

            <Description("Required to adjust the attributes of the groups in an access token.")>
            ADJUST_GROUPS = &H40UI

            <Description("Required to enable or disable the privileges in an access token.")>
            ADJUST_PRIVILEGES = &H20UI

            <Description("Required to adjust the session ID of an access token. The SE_TCB_NAME privilege is required.")>
            ADJUST_SESSIONID = &H100UI

            <Description("Required to attach a primary token to a process. The SE_ASSIGNPRIMARYTOKEN_NAME privilege is also required to accomplish this task.")>
            ASSIGN_PRIMARY = &H1UI

            <Description("Required to duplicate an access token.")>
            DUPLICATE = &H2UI

            <Description("Combines STANDARD_RIGHTS_EXECUTE and TOKEN_IMPERSONATE.")>
            EXECUTE = STANDARD_RIGHTS_EXECUTE Or IMPERSONATE

            <Description("Required to attach an impersonation access token to a process.")>
            IMPERSONATE = &H4UI

            <Description("Required to query an access token.")>
            QUERY = &H8UI

            <Description("	Required to query the source of an access token.")>
            QUERY_SOURCE = &H10UI

            <Description("Combines STANDARD_RIGHTS_READ and TOKEN_QUERY.")>
            READ = STANDARD_RIGHTS_READ Or QUERY

            <Description("Combines STANDARD_RIGHTS_WRITE, TOKEN_ADJUST_PRIVILEGES, TOKEN_ADJUST_GROUPS, and TOKEN_ADJUST_DEFAULT.")>
            WRITE = STANDARD_RIGHTS_WRITE Or ADJUST_PRIVILEGES Or ADJUST_GROUPS Or ADJUST_DEFAULT

            <Description("Combines all possible access rights for a token.")>
            ALL_ACCESS_P = STANDARD_RIGHTS_REQUIRED Or ASSIGN_PRIMARY Or DUPLICATE Or IMPERSONATE Or QUERY Or QUERY_SOURCE Or ADJUST_PRIVILEGES Or ADJUST_GROUPS Or ADJUST_DEFAULT

            <Description("Combines all possible access rights for a token.")>
            ALL_ACCESS = ALL_ACCESS_P Or ADJUST_SESSIONID
        End Enum

        <Flags()>
        Private Enum SE_PRIVILEGE As UInt32
            ENABLED_BY_DEFAULT = &H1UI
            ENABLED = &H2UI
            REMOVED = &H4UI
            USED_FOR_ACCESS = &H80000000UI
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


        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function RegGetValue(
            <[In]()> ByVal hKey As IntPtr,
            <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpSubKey As String,
            <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpValue As String,
            <[In](), [Optional]()> ByVal dwFlags As UInt32,
            <[Out](), [Optional]()> ByRef pdwType As UInt32,
            <[Out](), [Optional]()> ByVal pvData As IntPtr,
            <[In](), [Out](), [Optional]()> ByRef pcbData As UInt32) As UInt32
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function RegGetKeySecurity(
            <[In]()> ByVal hKey As IntPtr,
            <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
            <[In](), [Out](), [Optional]()> ByRef pSecurityDescriptor As IntPtr,
            <[In](), [Out]()> ByRef lpcbSecurityDescriptor As UInt32) As UInt32
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function GetSecurityDescriptorOwner(
            <[In]()> ByRef pSecurityDescriptor As IntPtr,
            <[In](), [Out]()> ByRef pOwner As IntPtr,
            <[Out]()> ByRef lpbOwnerDefaulted As Integer) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function IsValidSecurityDescriptor(
            <[In]()> ByVal pSecurityDescriptor As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function InitializeSecurityDescriptor(
            <[Out]()> ByVal pSecurityDescriptor As IntPtr,
            <[In]()> ByVal dwRevision As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function RegOpenKeyEx(
            <[In]()> ByVal hKey As IntPtr,
            <[In](), [Optional](), MarshalAs(UnmanagedType.LPWStr)> ByVal subKey As String,
            <[In]()> ByVal ulOptions As UInt32,
            <[In]()> ByVal samDesired As REGSAM,
            <[Out]()> ByRef phkResult As IntPtr) As UInt32
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function RegCloseKey(
          <[In]()> ByVal hKey As IntPtr) As UInt32
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function RegCreateKeyEx(
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
        Private Function GetSecurityInfo(
            <[In]()> ByVal handle As IntPtr,
            <[In]()> ByVal objectType As SE_OBJECT_TYPE,
            <[In]()> ByVal securityInfo As SECURITY_INFORMATION,
            <[Out](), [Optional]()> ByRef sidOwner As IntPtr,
            <[Out](), [Optional]()> ByRef sidGroup As IntPtr,
            <[Out](), [Optional]()> ByRef dacl As IntPtr,
            <[Out](), [Optional]()> ByRef sacl As IntPtr,
            <[Out](), [Optional]()> ByRef securityDescriptor As IntPtr) As UInt32
        End Function

        <DllImport("advapi32", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Function ConvertSidToStringSid(
            <[In]()> ByRef Sid As IntPtr,
            <[Out](), MarshalAs(UnmanagedType.LPTStr)> ByRef StringSid As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function



        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Function LocalFree(
            <[In]()> ByVal handle As IntPtr) As IntPtr
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Function CloseHandle(
            <[In]()> ByVal hHandle As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function InitializeSid(
            <[Out]()> ByRef Sid As IntPtr,
            <[In]()> ByVal pIdentifierAuthority As IntPtr,
            <[In]()> ByVal nSubAuthorityCount As Byte) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        'BOOL WINAPI ConvertSecurityDescriptorToStringSecurityDescriptor(
        '  _In_  PSECURITY_DESCRIPTOR SecurityDescriptor,
        '  _In_  DWORD                RequestedStringSDRevision,
        '  _In_  SECURITY_INFORMATION SecurityInformation,
        '  _Out_ LPTSTR               *StringSecurityDescriptor,
        '  _Out_ PULONG               StringSecurityDescriptorLen
        ');

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function GetSecurityDescriptorControl(
            <[In]()> ByRef pSecurityDescriptor As IntPtr,
            <[Out]()> ByRef pControl As IntPtr,
            <[Out]()> ByRef lpdwRevision As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Function ConvertSecurityDescriptorToStringSecurityDescriptor(
            <[In]()> ByRef SecurityDescriptor As IntPtr,
            <[In]()> ByVal RequestedStringSDRevision As UInt32,
            <[In]()> ByVal SecurityInformation As SECURITY_INFORMATION,
            <[Out](), MarshalAs(UnmanagedType.LPWStr)> ByRef StringSecurityDescriptor As String,
            <[Out]()> ByRef StringSecurityDescriptorLen As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function
#End Region

        Public Sub AdjustToken(ByVal ParamArray priviliges() As String)
            AdjustToken(GetCurrentProcess(), priviliges)
        End Sub

        Private Sub AdjustToken(ByVal ptrProcess As IntPtr, ByVal ParamArray priviliges() As String)
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
                                           .Attributes = SE_PRIVILEGE.ENABLED
                                       }
                                   }
                               })

                        If Not AdjustTokenPrivileges(ptrToken, False, newState.Ptr, 0UI, IntPtr.Zero, requiredSize) Then
                            Dim err As UInt32 = GetLastWin32ErrorU()

                            If err <> Errors.INSUFFICIENT_BUFFER Then
                                MessageBox.Show(privilige & " -> " & err.ToString())
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

        Public Sub test3(ByVal debugOpt As Int32, Optional ByVal regKey As String = "SOFTWARE\ATI")
            '  Throw New Win32Exception(5)

            If debugOpt = 1 Then
                AdjustToken(SE_BACKUP_NAME, SE_RESTORE_NAME, SE_SECURITY_NAME, SE_TAKE_OWNERSHIP_NAME)
            ElseIf debugOpt = 2 Then
                TokenManipulator.AddPrivilege("SeSecurityPrivilege")
                TokenManipulator.AddPrivilege("SeBackupPrivilege")
                TokenManipulator.AddPrivilege("SeRestorePrivilege")
                TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege")
            End If

            Dim ptrRegKey As IntPtr = IntPtr.Zero
            Dim returnValue As UInt32

            Try
                returnValue = RegOpenKeyEx(HKEY_LOCAL_MACHINE, regKey, 0UI, REGSAM.KEY_READ, ptrRegKey)
                MsgBox("RegOpenKeyEx: " & returnValue.ToString())

                If returnValue <> 0UI Then
                    Throw New Win32Exception(GetInt32(returnValue))
                End If

                Dim ptrOwner As IntPtr = IntPtr.Zero
                Dim ptrSecurity As IntPtr = IntPtr.Zero
                Dim requiredSize As UInt32 = 0UI

                returnValue = RegGetKeySecurity(ptrRegKey, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, ptrSecurity, requiredSize)

                If returnValue <> Errors.INSUFFICIENT_BUFFER Then
                    Throw New Win32Exception(GetInt32(returnValue))
                End If

                ptrSecurity = Marshal.AllocHGlobal(GetInt32(requiredSize))
                returnValue = 0UI

                If Not InitializeSecurityDescriptor(ptrSecurity, 1) Then
                    Throw New Win32Exception(GetLastWin32Error)
                End If

                returnValue = RegGetKeySecurity(ptrRegKey, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, ptrSecurity, requiredSize)

                If returnValue <> 0UI Then
                    Throw New Win32Exception(GetInt32(returnValue))
                End If

                Dim rev As UInt32
                Dim ptrSecurityControl As IntPtr = IntPtr.Zero
                Dim getRev As Boolean = GetSecurityDescriptorControl(ptrSecurity, ptrSecurityControl, rev)
                Dim isValid As Boolean = IsValidSecurityDescriptor(ptrSecurity)

                Dim strOwner As String = Nothing
                Dim strOwnerLen As UInt32

                Dim success As Boolean = ConvertSecurityDescriptorToStringSecurityDescriptor(ptrSecurity, rev, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, strOwner, strOwnerLen)

                If Not success Then
                    Throw New Win32Exception(GetLastWin32Error)
                End If

                'Dim defaulted As Int32
                'Dim success As Boolean = GetSecurityDescriptorOwner(ptrSecurity, ptrOwner, defaulted)

                'If Not success Then
                '    Throw New Win32Exception(GetLastWin32Error)
                'End If

                'If returnValue <> 0UI Then
                '    Throw New Win32Exception(GetInt32(returnValue))
                'End If

                'If ptrOwner <> IntPtr.Zero Then
                '    Dim ptrOwnerSid As IntPtr = IntPtr.Zero

                '    Try
                '        Dim sidStr As String = Nothing

                '        If ConvertSidToStringSid(ptrOwner, sidStr) Then
                '            MsgBox(sidStr)
                '        Else
                '            '   ERROR_INVALID_SID()
                '            '   1337 (0x539)
                '            '   The security ID structure is invalid.

                '            Throw New Win32Exception(GetLastWin32Error)
                '        End If
                '    Finally
                '        If ptrOwnerSid <> IntPtr.Zero Then
                '            LocalFree(ptrOwnerSid)
                '        End If
                '    End Try
                'End If
            Catch Ex As Exception
                ShowException(Ex)
            Finally
                If ptrRegKey <> IntPtr.Zero Then
                    RegCloseKey(ptrRegKey)
                End If
            End Try
        End Sub
    End Module
End Namespace
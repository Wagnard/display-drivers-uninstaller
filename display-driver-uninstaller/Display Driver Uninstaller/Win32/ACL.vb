Imports System
Imports System.IO
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Runtime.InteropServices
Imports Microsoft.Win32

Namespace Win32
    Module FixACL
        Public Class TokenManipulator
            Friend Declare Auto Function AdjustTokenPrivileges Lib "advapi32.dll" (htok As IntPtr, disall As Boolean, ByRef newst As TokPriv1Luid, len As Integer, prev As IntPtr, relen As IntPtr) As Boolean

            <DllImport("kernel32.dll", ExactSpelling:=True)>
            Friend Shared Function GetCurrentProcess() As IntPtr
            End Function

            Friend Declare Auto Function OpenProcessToken Lib "advapi32.dll" (h As IntPtr, acc As Integer, ByRef phtok As IntPtr) As Boolean

            <DllImport("advapi32.dll", SetLastError:=True)>
            Friend Shared Function LookupPrivilegeValue(host As String, name As String, ByRef pluid As Long) As Boolean
            End Function

            <StructLayout(LayoutKind.Sequential, Pack:=1)>
            Friend Structure TokPriv1Luid
                Public Count As Integer
                Public Luid As Long
                Public Attr As Integer
            End Structure

            Friend Const SE_PRIVILEGE_DISABLED As Integer = &H0
            Friend Const SE_PRIVILEGE_ENABLED As Integer = &H2
            Friend Const TOKEN_QUERY As Integer = &H8
            Friend Const TOKEN_ADJUST_PRIVILEGES As Integer = &H20

            Public Shared Function AddPrivilege(privilege As String) As Boolean
                Try
                    Dim retVal As Boolean
                    Dim tp As TokPriv1Luid
                    Dim hproc As IntPtr = GetCurrentProcess()
                    Dim htok As IntPtr = IntPtr.Zero
                    retVal = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES Or TOKEN_QUERY, htok)
                    tp.Count = 1
                    tp.Luid = 0
                    tp.Attr = SE_PRIVILEGE_ENABLED
                    retVal = LookupPrivilegeValue(Nothing, privilege, tp.Luid)
                    retVal = AdjustTokenPrivileges(htok, False, tp, 0, IntPtr.Zero, IntPtr.Zero)
                    Return retVal
                Catch ex As Exception
                    Throw ex
                End Try
            End Function

            Public Shared Function RemovePrivilege(privilege As String) As Boolean
                Try
                    Dim retVal As Boolean
                    Dim tp As TokPriv1Luid
                    Dim hproc As IntPtr = GetCurrentProcess()
                    Dim htok As IntPtr = IntPtr.Zero
                    retVal = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES Or TOKEN_QUERY, htok)
                    tp.Count = 1
                    tp.Luid = 0
                    tp.Attr = SE_PRIVILEGE_DISABLED
                    retVal = LookupPrivilegeValue(Nothing, privilege, tp.Luid)
                    retVal = AdjustTokenPrivileges(htok, False, tp, 0, IntPtr.Zero, IntPtr.Zero)
                    Return retVal
                Catch ex As Exception
                    Throw ex
                End Try
            End Function
        End Class

        ' Adds an ACL entry on the specified directory for the specified account.
        Sub AddDirectorySecurity(ByVal path As String, ByVal Rights As FileSystemRights, ByVal ControlType As AccessControlType)
            ' Create a new DirectoryInfoobject.
            Dim dInfo As New DirectoryInfo(path)

            Dim sid = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)
            ' Get a DirectorySecurity object that represents the 
            ' current security settings.
            'Dim dSecurity As DirectorySecurity = dInfo.GetAccessControl()
            'Activate necessary admin privileges to make changes without NTFS perms
            TokenManipulator.AddPrivilege("SeSecurityPrivilege")
            TokenManipulator.AddPrivilege("SeBackupPrivilege")
            TokenManipulator.AddPrivilege("SeRestorePrivilege")
            TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege")
            'Create a new acl from scratch.
            Dim newacl As New System.Security.AccessControl.DirectorySecurity()
            'set owner only here (needed for WinXP)
            newacl.SetOwner(sid)
            dInfo.SetAccessControl(newacl)
            'This remove inheritance.
            newacl.SetAccessRuleProtection(True, False)
            ' Add the FileSystemAccessRule to the security settings. 
            newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))
            sid = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
            newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))
            sid = New SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, Nothing)
            newacl.AddAccessRule(New FileSystemAccessRule(sid, Rights, ControlType))

            ' Set the new access settings.
            dInfo.SetAccessControl(newacl)

        End Sub

        Sub Addregistrysecurity(ByVal regkey As RegistryKey, ByVal subkeyname As String, ByVal Rights As RegistryRights, ByVal ControlType As AccessControlType)
            Dim subkey As RegistryKey
            Dim rs As New RegistrySecurity()
            Dim sid = New SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, Nothing)

            TokenManipulator.AddPrivilege("SeSecurityPrivilege")
            TokenManipulator.AddPrivilege("SeBackupPrivilege")
            TokenManipulator.AddPrivilege("SeRestorePrivilege")
            TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege")

            'Dim originalsid = regkey.OpenSubKey(subkeyname, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions).GetAccessControl.GetOwner(GetType(System.Security.Principal.SecurityIdentifier))
            'MsgBox(originalsid.ToString)
            subkey = regkey.OpenSubKey(subkeyname, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership)
            rs.SetOwner(sid)

            ' Set the new access settings.Owner
            subkey.SetAccessControl(rs)
            rs.SetAccessRuleProtection(True, False)
            'rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))
            sid = New SecurityIdentifier(WellKnownSidType.LocalSystemSid, Nothing)
            rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))
            'sid = New SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, Nothing)
            'rs.AddAccessRule(New RegistryAccessRule(sid, Rights, ControlType))
            ' Set the new access settings.
            subkey.SetAccessControl(rs)
        End Sub

        ' Removes an ACL entry on the specified directory for the specified account.
        Sub RemoveDirectorySecurity(ByVal FileName As String, ByVal Account As String, ByVal Rights As FileSystemRights, ByVal ControlType As AccessControlType)
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
    End Module
End Namespace
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Security.Principal
Imports Display_Driver_Uninstaller.Win32

Public Class ImpersonateLoggedOnUser
	<SuppressUnmanagedCodeSecurityAttribute()>
	Private Declare Function OpenProcessToken Lib "advapi32" (ByVal ProcessHandle As System.IntPtr, ByVal DesiredAccess As Integer, ByRef TokenHandle As IntPtr) As Integer

	<SuppressUnmanagedCodeSecurityAttribute()>
	Private Declare Function CloseHandle Lib "kernel32" (ByVal handle As IntPtr) As Boolean

	Public Declare Function DuplicateToken Lib "advapi32.dll" (ByVal ExistingTokenHandle As IntPtr, ByVal SECURITY_IMPERSONATION_LEVEL As Integer, ByRef DuplicateTokenHandle As IntPtr) As Boolean

	Private Declare Auto Function RevertToSelf Lib "advapi32.dll" () As Long

	Declare Function ImpersonateLoggedOnUser Lib "advapi32.dll" (ByVal hToken As Integer) As Integer

	Public Const TOKEN_DUPLICATE As Integer = 2

	Public Const TOKEN_QUERY As Integer = 8

	Public Const TOKEN_IMPERSONATE As Integer = 4

	Public Shared Sub Taketoken()
		Dim hToken As IntPtr = IntPtr.Zero
		Dim dupeTokenHandle As IntPtr = IntPtr.Zero
		'Dim procs As Process() = Process.GetProcessesByName("LSASS")
		Dim procs As Process() = Process.GetProcesses()
		Application.Log.AddMessage("Trying to impersonate the SYSTEM account...")
		ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)

		If procs IsNot Nothing AndAlso procs.Length > 0 Then
			Try
				For Each proc As Process In procs
					If IsNullOrWhitespace(proc.ToString) Then Continue For
					Try
						If OpenProcessToken(proc.Handle, TOKEN_QUERY Or TOKEN_IMPERSONATE Or TOKEN_DUPLICATE, hToken) <> 0 Then
							Dim newId As WindowsIdentity = New WindowsIdentity(hToken)


							Const SecurityImpersonation As Integer = 2
							dupeTokenHandle = DupeToken(hToken, SecurityImpersonation)

							If IntPtr.Zero = dupeTokenHandle Then
								Dim s As String = String.Format("Dup failed {0}, privilege not held", Marshal.GetLastWin32Error())
								Throw New Exception(s)
							End If

							Dim impersonatedUser As WindowsImpersonationContext = newId.Impersonate()
							Dim accountToken As IntPtr = WindowsIdentity.GetCurrent().Token

							ImpersonateLoggedOnUser(CInt((hToken)))

							If WindowsIdentity.GetCurrent().IsSystem Then
								ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
								Exit For
							Else
								RevertToSelf()
							End If
						Else
							Dim s As String = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error())
							'Throw New Exception(s)

						End If
					Catch exARG As SystemException
						'access denied ,can happen, just continue checking the next process.
					Catch ex As Exception
						Application.Log.AddMessage(ex.Message + ex.StackTrace)
					End Try
				Next
			Catch ex As Exception
				Application.Log.AddMessage(ex.Message + ex.StackTrace)
			Finally
				CloseHandle(hToken)
			End Try
		End If
		If WindowsIdentity.GetCurrent().IsSystem Then
			Application.Log.AddMessage("SYSTEM account impersonalisation successful")
		Else
			Application.Log.AddWarningMessage("SYSTEM account impersonalisation failed ! Cleanup may not be efficient. ")
		End If
	End Sub

	Public Shared Sub ReleaseToken()
		RevertToSelf()
		If WindowsIdentity.GetCurrent().IsSystem Then
			Application.Log.AddWarningMessage("Reverting Impersonalisation failed!")
		Else
			Application.Log.AddMessage("Reverting the Impersonalisation is successful !")
		End If
	End Sub

	Private Shared Function DupeToken(ByVal token As IntPtr, ByVal Level As Integer) As IntPtr
		Dim dupeTokenHandle As IntPtr = IntPtr.Zero
		Dim retVal As Boolean = DuplicateToken(token, Level, dupeTokenHandle)
		Return dupeTokenHandle
	End Function

End Class

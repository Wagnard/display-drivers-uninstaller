Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Security.Principal

Public Class ImpersonateLoggedOnUser
	<SuppressUnmanagedCodeSecurityAttribute()>
	Private Declare Function OpenProcessToken Lib "advapi32" (ByVal ProcessHandle As System.IntPtr, ByVal DesiredAccess As Integer, ByRef TokenHandle As IntPtr) As Integer

	<SuppressUnmanagedCodeSecurityAttribute()>
	Private Declare Function CloseHandle Lib "kernel32" (ByVal handle As IntPtr) As Boolean

	Public Declare Function DuplicateToken Lib "advapi32.dll" (ByVal ExistingTokenHandle As IntPtr, ByVal SECURITY_IMPERSONATION_LEVEL As Integer, ByRef DuplicateTokenHandle As IntPtr) As Boolean

	Declare Function ImpersonateLoggedOnUser Lib "advapi32.dll" (ByVal hToken As Integer) As Integer

	Public Const TOKEN_DUPLICATE As Integer = 2

	Public Const TOKEN_QUERY As Integer = 8

	Public Const TOKEN_IMPERSONATE As Integer = 4

	Public Shared Sub Taketoken()
		Dim hToken As IntPtr = IntPtr.Zero
		Dim dupeTokenHandle As IntPtr = IntPtr.Zero
		Dim proc As Process() = Process.GetProcessesByName("LSASS")
		Application.Log.AddMessage("Trying to impersonate the SYSTEM account...")
		If OpenProcessToken(proc(0).Handle, TOKEN_QUERY Or TOKEN_IMPERSONATE Or TOKEN_DUPLICATE, hToken) <> 0 Then
			Dim newId As WindowsIdentity = New WindowsIdentity(hToken)

			Try
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
					Application.Log.AddMessage("SYSTEM account impersonalisation successful")
				Else
					Application.Log.AddWarningMessage("SYSTEM account impersonalisation failed ! Cleanup may not be efficient. ")
				End If

			Finally
				CloseHandle(hToken)
			End Try
		Else
			Dim s As String = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error())
			Throw New Exception(s)
		End If
	End Sub

	Private Shared Function DupeToken(ByVal token As IntPtr, ByVal Level As Integer) As IntPtr
		Dim dupeTokenHandle As IntPtr = IntPtr.Zero
		Dim retVal As Boolean = DuplicateToken(token, Level, dupeTokenHandle)
		Return dupeTokenHandle
	End Function

End Class

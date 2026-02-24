Imports Microsoft.Win32.SafeHandles
Imports System.Runtime.InteropServices
Imports System.Security.Principal

Namespace Display_Driver_Uninstaller.Win32

    Friend Class ImpersonateUser

        <DllImport("advapi32.dll", SetLastError:=True)>
        Private Shared Function OpenProcessToken(ByVal processHandle As IntPtr, ByVal desiredAccess As Integer, ByRef tokenHandle As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function CloseHandle(ByVal handle As IntPtr) As Boolean
        End Function

        <DllImport("advapi32.dll", SetLastError:=True)>
        Public Shared Function DuplicateToken(ByVal existingTokenHandle As IntPtr, ByVal securityImpersonationLevel As Integer, ByRef duplicateTokenHandle As IntPtr) As Boolean
        End Function

        Private Const TOKEN_DUPLICATE As Integer = 2
        Private Const TOKEN_QUERY As Integer = 8
        Private Const TOKEN_IMPERSONATE As Integer = 4

        Private Shared Function TakeTokenInternal() As SafeAccessTokenHandle
            Dim hToken As IntPtr = IntPtr.Zero
            Dim dupeTokenHandle As IntPtr = IntPtr.Zero
            Dim tokenReturned As Boolean = False
            Dim logEntry As New LogEntry() With {.Message = "Trying to impersonate the SYSTEM account..."}
            logEntry.Type = LogType.Warning

            Try
                ACL.AddPriviliges(ACL.SE.DEBUG_NAME, ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)

                Dim procs As Process() = Process.GetProcesses()

                If procs.Length = 0 Then
                    logEntry.Message &= " FAILED ! (Cleanup may not be efficient.)"
                    logEntry.Add("No processes available to obtain SYSTEM token.")
                    Throw New InvalidOperationException("No processes available to obtain SYSTEM token.")
                End If

                logEntry.Add("Number of process to check", procs.Length.ToString)

                For Each proc As Process In procs
                    If String.IsNullOrWhiteSpace(proc.ToString()) OrElse
               StrContainsAny(proc.ProcessName, True, "searchfilterhost", "idle", "wininit", "system", "registry", "smss", "services", "csrss", "lsass") Then
                        Continue For
                    End If

                    Try
                        If Not OpenProcessToken(proc.Handle, TOKEN_QUERY Or TOKEN_IMPERSONATE Or TOKEN_DUPLICATE, hToken) Then
                            logEntry.Add(proc.ProcessName, String.Format("OpenProcessToken Failed {0}, privilege not held", Marshal.GetLastWin32Error()))
                            Continue For
                        End If

                        Using newId As New WindowsIdentity(hToken)
                            If Not newId.IsSystem Then
                                logEntry.Add(proc.ProcessName, "Skipping : " & newId.User.ToString())
                                Continue For
                            End If
                            logEntry.Add(proc.ProcessName, newId.User.ToString())
                        End Using

                        Const SecurityImpersonation As Integer = 2
                        dupeTokenHandle = DupeToken(hToken, SecurityImpersonation)

                        If dupeTokenHandle = IntPtr.Zero Then
                            Throw New Exception(String.Format("DuplicateToken failed for {0}: {1}", proc.ProcessName, Marshal.GetLastWin32Error()))
                        End If

                        logEntry.Type = LogType.Event
                        logEntry.Message &= " SUCCESS !"
                        logEntry.Add(proc.ProcessName, "SYSTEM token obtained: SUCCESS")
                        tokenReturned = True
                        Return New SafeAccessTokenHandle(dupeTokenHandle)

                    Catch ex As ComponentModel.Win32Exception
                        logEntry.Add(proc.ProcessName, ex.Message)
                    Catch ex As Exception
                        logEntry.Add(proc.ProcessName, ex.Message & ex.StackTrace)
                    Finally
                        If hToken <> IntPtr.Zero Then
                            CloseHandle(hToken)
                            hToken = IntPtr.Zero
                        End If
                    End Try
                Next

                logEntry.Message &= " FAILED ! (Cleanup may not be efficient.)"
                Throw New InvalidOperationException("Could not obtain SYSTEM token.")

            Finally
                If dupeTokenHandle <> IntPtr.Zero AndAlso Not tokenReturned Then
                    CloseHandle(dupeTokenHandle)
                End If
                Application.Log.Add(logEntry)
            End Try
        End Function

        Private Shared Function DupeToken(ByVal token As IntPtr, ByVal level As Integer) As IntPtr
            Dim dupeTokenHandle As IntPtr = IntPtr.Zero
            If Not DuplicateToken(token, level, dupeTokenHandle) Then
                Application.Log.AddMessage("DuplicateToken failed: " & Marshal.GetLastWin32Error().ToString())
            End If
            Return dupeTokenHandle
        End Function

        Public Shared Sub RunImpersonatedSystem(ByVal action As Action)
            Try
                Using systemToken As SafeAccessTokenHandle = TakeTokenInternal()
                    WindowsIdentity.RunImpersonated(systemToken, action)
                End Using
                Application.Log.AddMessage("Reverting the Impersonalisation is successful !")
            Catch ex As Exception
                Application.Log.AddMessage("RunImpersonatedSystem failed: " & ex.Message)
            End Try
        End Sub

    End Class
End Namespace
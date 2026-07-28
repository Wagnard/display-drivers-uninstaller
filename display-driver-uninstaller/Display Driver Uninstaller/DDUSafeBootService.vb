Imports System.ServiceProcess
Imports System.Threading

Namespace Display_Driver_Uninstaller
	Public Class DDUSafeBootService
		Inherits ServiceBase

		Private Const SERVICE_NAME As String = "DDUSafeBootHandler"

		Public Sub New()
			ServiceName = SERVICE_NAME
			CanStop = True
			CanPauseAndContinue = False
			AutoLog = True
		End Sub

		Protected Overrides Sub OnStart(ByVal args() As String)
			Try
				' Tell the SCM to be patient: the retries below can outlast its default
				' 30s start timeout if BCDEDIT itself is slow to answer.
				Try
					RequestAdditionalTime(90000)
				Catch
				End Try

				' Attendre un peu pour s'assurer que tous les services nécessaires sont démarrés
				Thread.Sleep(2000)

				' Exécuter bcdedit pour supprimer safeboot.
				'
				' This is the whole reason this service exists: it runs BEFORE any user logs
				' in, so it is the only safety net for someone who cannot sign in while in
				' Safe Mode (forgotten password, PIN unavailable there). The RunOnce entry and
				' DDU's own startup check both require a successful logon, this does not.
				' So: retry, and never assume success - check the exit code.
				Dim safeBootRemoved As Boolean = False

				For attempt As Integer = 1 To 3
					If RunBcdEditDeleteSafeBoot() Then
						safeBootRemoved = True
						Exit For
					End If

					WriteLog(String.Format("BCDEDIT attempt {0}/3 failed to remove the safeboot value.", attempt), EventLogEntryType.Warning)
					Thread.Sleep(2000)
				Next

				If Not safeBootRemoved Then
					' BCDEDIT also returns a non-zero exit code when there is simply no safeboot
					' value to delete, and this service is start=auto so it runs on normal boots
					' too. Only a failure *while actually in Safe Mode* means the machine is at
					' risk of staying trapped - that is the only case worth keeping the service
					' installed for. Anything else must still self-uninstall, or the service
					' would survive forever and start on every boot.
					If System.Windows.Forms.SystemInformation.BootMode <> System.Windows.Forms.BootMode.Normal Then
						WriteLog("BCDEDIT could not remove the safeboot value after 3 attempts while in Safe Mode. The service is kept installed so it can retry on the next boot.", EventLogEntryType.Error)
						Return
					End If

					WriteLog("BCDEDIT reported no safeboot value to remove (normal boot). Uninstalling the service.", EventLogEntryType.Information)
				Else
					WriteLog("BCDEDIT completed. Stopping the service...", EventLogEntryType.Information)
				End If

				' IsBackground: this thread must never keep the service process alive on its own.
				Dim stopThread As New Thread(AddressOf StopService) With {.IsBackground = True}
				stopThread.Start()
			Catch ex As Exception
				WriteLog(ex.Message, EventLogEntryType.Error)
			End Try
		End Sub

		''' <summary>
		''' Runs "BCDEDIT /deletevalue safeboot". Returns True only when bcdedit actually
		''' reported success - the caller must be able to tell a real failure from a silent one.
		''' </summary>
		Private Function RunBcdEditDeleteSafeBoot() As Boolean
			Try
				Dim processInfo As New ProcessStartInfo(Application.Paths.System32 & "BCDEDIT", "/deletevalue safeboot") With {
				.UseShellExecute = False,
				.CreateNoWindow = True,
				.RedirectStandardOutput = False
			}

				Using process As New Process With {
				.StartInfo = processInfo
			}
					process.Start()

					' Bounded so three attempts always fit inside the extended SCM start timeout.
					If Not process.WaitForExit(20000) Then
						Try
							process.Kill()
						Catch
						End Try

						WriteLog("BCDEDIT did not exit within 20 seconds and was terminated.", EventLogEntryType.Warning)
						Return False
					End If

					Return (process.ExitCode = 0)
				End Using
			Catch ex As Exception
				WriteLog("BCDEDIT failed: " & ex.Message, EventLogEntryType.Error)
				Return False
			End Try
		End Function

		Private Sub StopService()
			' Runs on its own thread: an unhandled exception here would take the whole service
			' process down before OnStop ever got the chance to uninstall the service.
			Try
				' Wait for a short period before stopping the service, just to ensure everything is settled
				Thread.Sleep(500)

				' Stop the service
				Me.Stop()
			Catch ex As Exception
				WriteLog("Failed to stop the service: " & ex.Message, EventLogEntryType.Error)
			End Try
		End Sub

		Protected Overrides Sub OnStop()
			' Clean up and uninstall the service
			Try
				' Uninstall the service
				UninstallService()
				WriteLog("Service Uninstalled", EventLogEntryType.Information)
			Catch ex As Exception
				WriteLog("Error uninstalling the service: " & ex.Message, EventLogEntryType.Error)
			End Try
		End Sub

		Private Sub UninstallService()
			Try
				' UseShellExecute = False: CreateNoWindow is ignored when ShellExecute is used,
				' and the "runas" verb is meaningless here - the service already runs as
				' LocalSystem, and ShellExecuteEx from session 0 is best avoided anyway.
				Dim processInfo As New ProcessStartInfo(Application.Paths.System32 & "sc.exe", "delete " & SERVICE_NAME) With {
				.UseShellExecute = False,
				.CreateNoWindow = True,
				.RedirectStandardOutput = False
			}

				Using process As New Process With {
				.StartInfo = processInfo
			}
					process.Start()

					' Never wait indefinitely: DeleteService is being called from inside OnStop,
					' which is exactly the re-entrancy case where the SCM can block. A hang here
					' would leave the service stuck in STOP_PENDING forever.
					If Not process.WaitForExit(30000) Then
						Try
							process.Kill()
						Catch
						End Try

						WriteLog("'sc delete' timed out. DDU will remove the leftovers on its next launch.", EventLogEntryType.Warning)
					ElseIf process.ExitCode <> 0 Then
						WriteLog(String.Format("'sc delete' failed (exit code {0}). DDU will remove the leftovers on its next launch.", process.ExitCode), EventLogEntryType.Warning)
					End If
				End Using
			Catch ex As Exception
				WriteLog("Error uninstalling the service: " & ex.Message, EventLogEntryType.Error)
			End Try
		End Sub

		''' <summary>
		''' EventLog.WriteEntry creates the event source on first use, which can itself throw
		''' (registry access, event log service unavailable). Never let logging bring down the
		''' service - especially not from inside a Catch block.
		''' </summary>
		Private Shared Sub WriteLog(ByVal message As String, ByVal entryType As EventLogEntryType)
			Try
				EventLog.WriteEntry(SERVICE_NAME, message, entryType)
			Catch
				' Nothing sensible to do here - the service must keep going.
			End Try
		End Sub
	End Class
End Namespace

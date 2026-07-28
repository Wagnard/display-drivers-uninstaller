Imports System.ComponentModel
Imports System.ServiceProcess
Imports System.Configuration.Install

' https://msdn.microsoft.com/en-us/library/windows/desktop/ms685974(v=vs.85).aspx
Namespace Display_Driver_Uninstaller.Win32
	Public Class ServiceInstaller

		Public Sub StartService(ByVal service As String)
			For Each svc As ServiceController In ServiceController.GetServices()
				Using svc
					If svc.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase) Then
						If svc.Status = ServiceControllerStatus.Stopped Then
							Try
								svc.Start()
								svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5))
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
						End If
					End If
				End Using
			Next
		End Sub

		Public Sub Uninstall(ByVal serviceName As String)
			Dim serviceInstallerObj As System.ServiceProcess.ServiceInstaller = New System.ServiceProcess.ServiceInstaller()
			Dim context As InstallContext = New InstallContext("<<log file path>>", Nothing)
			serviceInstallerObj.Context = context
			serviceInstallerObj.ServiceName = serviceName
			Try
				serviceInstallerObj.Uninstall(Nothing)
				Application.Log.AddMessage("Service : " & serviceName & " removed.")
			Catch ex As Win32Exception
				Application.Log.AddException(ex, serviceName)
			End Try

			GetServiceStatus(serviceName)
			GetServiceStatus(serviceName, False)

			'Verify that the service was indeed removed via registry.
			Using regkey As Microsoft.Win32.RegistryKey = MyRegistry.OpenSubKey(Microsoft.Win32.Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & serviceName, False)
				If regkey IsNot Nothing Then
					Application.Log.AddWarningMessage("Failed to remove the service : " & serviceName)
				End If
			End Using

		End Sub

		Public Sub StopService(ByVal service As String)
            ' New ServiceController(name) opens a direct handle to the named service —
            ' no full SCM scan needed. The constructor itself never throws; property
            ' access (Status, CanStop, …) is where failures surface.
            Dim target As New ServiceController(service)
            Dim deps As ServiceController() = Nothing

            Try
                Dim status As ServiceControllerStatus
                Try
                    status = target.Status
                Catch ex As Exception
                    Application.Log.AddException(ex, String.Format("StopService: cannot query status of '{0}'", service))
                    Return
                End Try

                Select Case status
                    Case ServiceControllerStatus.Stopped
                        Return
                    Case ServiceControllerStatus.StopPending
                        ' Already stopping — just wait it out.
                        target.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10))
                        Return
                    Case ServiceControllerStatus.StartPending, ServiceControllerStatus.ContinuePending
                        ' Service hasn't finished initialising yet — it won't accept a stop command
                        ' until it calls SetServiceStatus(ACCEPT_STOP). Wait for a stable state first.
                        Application.Log.AddMessage(String.Format("StopService: '{0}' is {1}, waiting for stable state...", service, status))
                        Try
                            target.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10))
                        Catch ex As System.ServiceProcess.TimeoutException
                            Application.Log.AddWarningMessage(String.Format("StopService: '{0}' did not reach Running after 10s (still {1}), attempting stop anyway.", service, target.Status))
                        End Try
                End Select

                ' Refresh first: the CanStop decision below must be based on the service's
                ' current state, not on a cached one.
                '
                ' When a service does not accept SERVICE_CONTROL_STOP, the SCM rejects the
                ' request outright (ERROR_INVALID_SERVICE_CONTROL) and the service never sees
                ' it — trying anyway can only add a misleading error to the log. This is the
                ' permanent state of kernel drivers with no unload routine, which DDU meets
                ' constantly, so skip instead of failing loudly.
                ' Nothing stoppable is lost: a service held by running dependents still
                ' reports CanStop = True and fails later with ERROR_DEPENDENT_SERVICES_RUNNING.
                Dim canStop As Boolean = True
                Try
                    target.Refresh()
                    canStop = target.CanStop
                Catch ex As Exception
                    ' State unreadable — fail open and let the stop attempt report the real error.
                    Application.Log.AddException(ex, String.Format("StopService: cannot read CanStop for '{0}', attempting stop anyway", service))
                End Try

                If Not canStop Then
                    Application.Log.AddWarningMessage(String.Format("StopService: '{0}' does not accept stop requests (status = {1}), skipping.", service, status))
                    Return
                End If

                ' Log any running dependents — Windows refuses to stop a service while a
                ' dependent is still running, and the resulting error message won't say why.
                ' deps is kept alive (and Nothing on failure) until after Stop()/WaitForStatus();
                ' disposing earlier can invalidate target's internal handles.
                Try
                    deps = target.DependentServices
                    Dim runningDeps As New List(Of String)
                    For Each dep As ServiceController In deps
                        If dep.Status <> ServiceControllerStatus.Stopped AndAlso
                           dep.Status <> ServiceControllerStatus.StopPending Then
                            runningDeps.Add(dep.ServiceName)
                        End If
                    Next
                    If runningDeps.Count > 0 Then
                        Application.Log.AddWarningMessage(String.Format(
                            "StopService: '{0}' has {1} running dependent(s): {2}",
                            service, runningDeps.Count, String.Join(", ", runningDeps)))
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex, String.Format("StopService: cannot enumerate dependents of '{0}'", service))
                End Try

                Try
                    target.Stop()
                    Try
                        target.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5))
                    Catch ex As System.ServiceProcess.TimeoutException
                        Application.Log.AddMessage(String.Format("StopService: '{0}' still stopping after 5s, retrying stop and waiting 10s more...", service))
                        Try
                            target.Stop()
                        Catch
                        End Try
                        target.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10))
                    End Try
                    Application.Log.AddMessage(String.Format("Service '{0}' stopped.", service))
                Catch ex As Exception
                    ' ServiceController wraps the real Win32 error inside InnerException.
                    Dim win32ex As ComponentModel.Win32Exception = TryCast(ex.InnerException, ComponentModel.Win32Exception)
                    If win32ex IsNot Nothing Then
                        Application.Log.AddException(win32ex, String.Format("StopService: '{0}' failed — Win32 error {1}", service, win32ex.NativeErrorCode))
                    Else
                        Application.Log.AddException(ex, String.Format("StopService: '{0}' failed", service))
                    End If
                End Try

            Finally
                If deps IsNot Nothing Then
                    For Each dep As ServiceController In deps
                        Try
                            dep.Dispose()
                        Catch
                        End Try
                    Next
                End If

                Try
                    target.Dispose()
                Catch
                End Try
            End Try
        End Sub

        Public Function GetServiceStatus(ByVal serviceName As String, Optional getdevice As Boolean = True) As ServiceControllerStatus

			If getdevice Then
				For Each svc As ServiceController In ServiceController.GetDevices()
					Using svc
						If svc.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) Then
							Try
								Return svc.Status
							Catch ex As Exception
								Application.Log.AddException(ex)
								Return Nothing
							End Try
						End If
					End Using
				Next
				Return Nothing
			End If

			For Each svc As ServiceController In ServiceController.GetServices()
				Using svc
					If svc.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) Then
						Try
							Return svc.Status
						Catch ex As Exception
							Application.Log.AddException(ex)
							Return Nothing
						End Try
					End If
				End Using
			Next

			Return Nothing
		End Function
	End Class
End Namespace
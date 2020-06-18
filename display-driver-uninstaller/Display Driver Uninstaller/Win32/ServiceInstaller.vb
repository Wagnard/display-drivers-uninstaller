Imports System.ComponentModel
Imports System.ServiceProcess
Imports System.Configuration.Install

' https://msdn.microsoft.com/en-us/library/windows/desktop/ms685974(v=vs.85).aspx

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
		Dim ServiceInstallerObj As System.ServiceProcess.ServiceInstaller = New System.ServiceProcess.ServiceInstaller()
		Dim Context As InstallContext = New InstallContext("<<log file path>>", Nothing)
		ServiceInstallerObj.Context = Context
		ServiceInstallerObj.ServiceName = serviceName
		Try
			ServiceInstallerObj.Uninstall(Nothing)
		Catch ex As Win32Exception
			Application.Log.AddException(ex)
		End Try

		'Verify that the service was indeed removed via registry.
		Using regkey As Microsoft.Win32.RegistryKey = MyRegistry.OpenSubKey(Microsoft.Win32.Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & serviceName, False)
			If regkey IsNot Nothing Then

				Application.Log.AddWarningMessage("Failed to remove the service : " & serviceName)
			Else
				Application.Log.AddMessage("Service : " & serviceName & " removed.")
			End If
		End Using

	End Sub

	Public Sub StopService(ByVal service As String)
		For Each svc As ServiceController In ServiceController.GetServices()
			Using svc
				If svc.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase) Then
					If svc.Status <> ServiceControllerStatus.Stopped AndAlso svc.Status <> ServiceControllerStatus.StopPending Then
						Try
							svc.Stop()
							svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10))
							Application.Log.AddMessage("Service : " & service & " stopped.")
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try

					End If
				End If
			End Using
		Next
	End Sub

	Public Function GetServiceStatus(ByVal serviceName As String, Optional getdevice As Boolean = True) As ServiceControllerStatus
		For Each svc As ServiceController In ServiceController.GetServices()
			Using svc
				If svc.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) Then
					Try
						Return svc.Status
					Catch ex As Exception
						Application.Log.AddException(ex)
						Exit For
					End Try
					Exit For
				End If
			End Using
		Next

		If getdevice Then
			For Each svc As ServiceController In ServiceController.GetDevices()
				Using svc
					If svc.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) Then
						Try
							Return svc.Status
						Catch ex As Exception
							Application.Log.AddException(ex)
							Exit For
						End Try
						Exit For
					End If
				End Using
			Next
		End If
		Return Nothing
	End Function
End Class

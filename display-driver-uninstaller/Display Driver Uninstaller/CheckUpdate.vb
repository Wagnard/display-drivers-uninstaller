Imports Display_Driver_Uninstaller.Win32
Imports System.Net
Imports System.Net.Http
Imports System.Reflection
Imports System.Threading.Tasks

Namespace Display_Driver_Uninstaller

	Public Class CheckUpdate

		Private Shared _alreadyChecked As Boolean = False

		Private Async Function CheckUpdatesThreadAsync(ByVal currentVersion As Version, ByVal checkUpdate As Boolean) As Task
			Dim status As UpdateStatus = UpdateStatus.NotChecked

			Try
				If Not checkUpdate Then
					status = UpdateStatus.NotAllowed
					Return
				End If

				Try
					If Not My.Computer.Network.IsAvailable Then
						status = UpdateStatus.Error
						Return
					End If
				Catch ex As Exception
					Application.Log.AddWarning(ex)
					status = UpdateStatus.Error
					Return
				End Try

				' Add this code before creating the HttpClient
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 Or SecurityProtocolType.Tls12

				Dim url As String = "https://www.wagnardsoft.com/DDU/currentversion2.txt"
				Dim url2 As String = "https://www.wagnardsoft.com/api/ddu/version.json"
				Dim newestVersionStr As String = Nothing

				Using client As New HttpClient()
					Dim version = If(Assembly.GetExecutingAssembly().GetName().Version?.ToString(), "1.0.0.0")

					client.DefaultRequestHeaders.UserAgent.ParseAdd($"DDU/{version} (Display Driver Uninstaller)")
					client.Timeout = TimeSpan.FromMilliseconds(5000) ' Set timeout to 5 seconds
					Try
						Using response As HttpResponseMessage = Await client.GetAsync(url)
							response.EnsureSuccessStatusCode() ' Throws an exception if the request is not successful

							newestVersionStr = Await response.Content.ReadAsStringAsync()
						End Using
						Try
							Using response As HttpResponseMessage = Await client.GetAsync(url2)
								response.EnsureSuccessStatusCode() ' Throws an exception if the request is not successful

								'newestVersionStr = Await response.Content.ReadAsStringAsync()
							End Using
						Catch ex As Exception
							'For the moment we do absolutely nothing with this.
							Application.Log.AddException(ex)
						End Try
					Catch ex As Exception
						' Handle the error appropriately
						status = UpdateStatus.Error
						Application.Log.AddException(ex)
						Return
					End Try
				End Using

				Dim newestVersion As Integer
				Dim applicationVersion As Integer

				If String.IsNullOrWhiteSpace(newestVersionStr) OrElse
		   Not Integer.TryParse(newestVersionStr.Replace(".", ""), newestVersion) OrElse
		   Not Integer.TryParse(currentVersion.ToString().Replace(".", ""), applicationVersion) Then

					status = UpdateStatus.Error
					Return
				End If

				If newestVersion <= applicationVersion Then
					status = UpdateStatus.NoUpdates
				Else
					status = UpdateStatus.UpdateAvailable
				End If

			Catch ex As Exception
				Application.Log.AddWarning(ex, "Checking updates failed!")
				status = UpdateStatus.Error
			Finally
				Update(status)
				_alreadyChecked = True
			End Try
		End Function


		Private Sub Update(ByVal status As UpdateStatus)
			If Not Application.Data.Settings.Dispatcher.CheckAccess() Then
				Application.Data.Settings.Dispatcher.Invoke(Sub() Update(status))
			Else
				Application.Settings.UpdateAvailable = status

				If status = UpdateStatus.UpdateAvailable Then
					If Not System.Security.Principal.WindowsIdentity.GetCurrent().IsSystem AndAlso Not Application.LaunchOptions.Silent Then
						Select Case MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text1"), "Display Driver Uninstaller", MessageBoxButton.YesNoCancel, MessageBoxImage.Information)
							Case MessageBoxResult.Yes
								WinAPI.OpenVisitLink(" -visitdduhome")

								'Me.Close()
								Return

							Case MessageBoxResult.No
								MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text2"), "Display Driver Uninstaller", MessageBoxButton.OK, MessageBoxImage.Information)
						End Select
					End If
				End If
			End If
		End Sub

		Public Async Function CheckUpdatesAsync() As Task
			Try
				If _alreadyChecked Then Return

				If Application.IsDebug Then
					Application.Settings.UpdateAvailable = UpdateStatus.Error
					Return
				End If

				Dim currentVersion As Version = Application.Settings.AppVersion
				Dim checkUpdate As Boolean = Application.Settings.CheckUpdates

				If Application.Settings.EnableSafeModeDialog Then
					' Run the async update check in the background
					Await Task.Run(Async Function()
									   Try
										   Await CheckUpdatesThreadAsync(currentVersion, checkUpdate)
									   Catch ex As Exception
										   Application.Log.AddException(ex, "Failed during async update check!")
									   End Try
								   End Function)
					Return
				End If

				' Run the async update check directly
				Await CheckUpdatesThreadAsync(currentVersion, checkUpdate)

			Catch ex As Exception
				Application.Log.AddException(ex, "Failed to start UpdateCheck!")
			End Try
		End Function
	End Class
End Namespace
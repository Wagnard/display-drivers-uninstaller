Imports System.Threading
Imports System.IO
Imports Display_Driver_Uninstaller.Win32
Imports System.Windows.Threading



Public Class CheckUpdate

	Private Sub CheckUpdatesThread(ByVal currentVersion As Version, ByVal CheckUpdate As Boolean)
		Dim status As UpdateStatus = UpdateStatus.NotChecked

		Try

			If CheckUpdate = False Then
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
			End Try

			Dim response As System.Net.WebResponse = Nothing
			Dim request As System.Net.WebRequest = System.Net.HttpWebRequest.Create("http://www.wagnardsoft.com/DDU/currentversion2.txt")
			request.Timeout = 5000

			Try
				response = request.GetResponse()
			Catch ex As Exception
				status = UpdateStatus.Error
				Application.Log.AddException(ex)
				Return
			End Try

			Dim newestVersionStr As String = Nothing
			Using sr As StreamReader = New StreamReader(response.GetResponseStream())
				newestVersionStr = sr.ReadToEnd()

				sr.Close()
			End Using

			Dim newestVersion As Integer
			Dim applicationversion As Integer

			If IsNullOrWhitespace(newestVersionStr) OrElse
			   Not Int32.TryParse(newestVersionStr.Replace(".", ""), newestVersion) OrElse
			   Not Int32.TryParse(currentVersion.ToString().Replace(".", ""), applicationversion) Then

				status = UpdateStatus.Error
				Return
			End If

			If newestVersion <= applicationversion Then
				status = UpdateStatus.NoUpdates
			Else
				status = UpdateStatus.UpdateAvailable
			End If

		Catch ex As Exception
			Application.Log.AddWarning(ex, "Checking updates failed!")
			status = UpdateStatus.Error
		Finally
			Update(status)
		End Try
	End Sub

	Private Sub Update(ByVal status As UpdateStatus)
		If Not Application.Data.Settings.Dispatcher.CheckAccess() Then
			Application.Data.Settings.Dispatcher.Invoke(Sub() Update(status))
		Else
			Application.Settings.UpdateAvailable = status

			If status = UpdateStatus.UpdateAvailable Then
				If Not Security.Principal.WindowsIdentity.GetCurrent().IsSystem Then
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

	Public Sub CheckUpdates()
		Try
			If Application.IsDebug Then
				Application.Settings.UpdateAvailable = UpdateStatus.Error
			Else
				If Application.Settings.EnableSafeModeDialog Then
					Dim currentVersion As Version = Application.Settings.AppVersion
					Dim CheckUpdate As Boolean = Application.Settings.CheckUpdates
					Dim trd As Thread = New Thread(Sub() CheckUpdatesThread(currentVersion, CheckUpdate)) With
					  {
					  .CurrentCulture = New Globalization.CultureInfo("en-US"),
					  .CurrentUICulture = New Globalization.CultureInfo("en-US"),
					  .IsBackground = True
					  }

					trd.Start()
				Else
					Dim currentVersion As Version = Application.Settings.AppVersion
					Dim CheckUpdate As Boolean = Application.Settings.CheckUpdates
					CheckUpdatesThread(currentVersion, CheckUpdate)
				End If
			End If
		Catch ex As Exception
			Application.Log.AddException(ex, "Failed to start UpdateCheck thread!")
		End Try
	End Sub
End Class

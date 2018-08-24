Imports Display_Driver_Uninstaller.Win32



Public Class AUDIOCleanup
	'todo
	Dim CleanupEngine As New CleanupEngine

	Public Sub start(ByVal config As ThreadSettings)
		Dim vendidexpected As String = ""
		Select Case config.SelectedAUDIO
			Case AudioVendor.Realtek
				vendidexpected = "VEN_10EC"
			Case AudioVendor.SoundBlaster
				vendidexpected = "VEN_1102"
			Case AudioVendor.None
				vendidexpected = "NONE"
		End Select

		If vendidexpected = "NONE" Then
			Application.Log.AddWarningMessage("VendID is NONE, this is unexpected, cleaning aborted.")
			Exit Sub
		End If

		UpdateTextMethod(UpdateTextTranslated(20) + " " & config.SelectedGPU.ToString() & " " + UpdateTextTranslated(21))
		Application.Log.AddMessage("Uninstalling " + config.SelectedGPU.ToString() + " driver ...")
		UpdateTextMethod(UpdateTextTranslated(22))

		'----------------------------------
		'Removing the Audio card-----------
		'----------------------------------

		Try
			Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendidexpected, False)
			If found.Count > 0 Then
				For Each d As SetupAPI.Device In found
					SetupAPI.UninstallDevice(d)
				Next
				found.Clear()
			End If
		Catch ex As Exception
			'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			Application.Log.AddException(ex)
		End Try

		UpdateTextMethod(UpdateTextTranslated(25))

		Application.Log.AddMessage("SetupAPI Remove Audio controler Complete.")

		'Removing Audio endpoints
		If config.SelectedAUDIO = AudioVendor.Realtek Then
			Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("audioendpoint", Nothing, False)
			If found.Count > 0 Then
				For Each d As SetupAPI.Device In found
					If StrContainsAny(d.FriendlyName, True, "realtek high definition audio") Then
						SetupAPI.UninstallDevice(d)
					End If
				Next
				found.Clear()
			End If
		End If

		If config.SelectedAUDIO = AudioVendor.Realtek Then
			CleanRealtekserviceprocess()
			CleanRealtek(config)

			CleanRealtekFolders(config)
		End If

		config.Success = True
	End Sub

	Private Sub CleanRealtekserviceprocess()
		Application.Log.AddMessage("Cleaning Process/Services...")

		CleanupEngine.Cleanserviceprocess(IO.File.ReadAllLines(Application.Paths.AppBase & "settings\REALTEK\services.cfg"))    '// add each line as String Array.

		KillProcess("RtkNGUI64")
		Application.Log.AddMessage("Process/Services CleanUP Complete")
		System.Threading.Thread.Sleep(10)
	End Sub

	Private Sub CleanRealtek(ByVal config As ThreadSettings)

		Application.Log.AddMessage("Cleaning known Regkeys")

		Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")

		CleanupEngine.ClassRoot(IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\classroot.cfg"))  '// add each line as String Array.

	End Sub

	Private Sub CleanRealtekFolders(ByVal config As ThreadSettings)

	End Sub

	Private Sub KillProcess(ByVal ParamArray processnames As String())
		For Each processName As String In processnames
			If String.IsNullOrEmpty(processName) Then
				Continue For
			End If

			For Each process As Process In Process.GetProcessesByName(processName)
				Try
					process.Kill()
				Catch ex As Exception
					Application.Log.AddExceptionWithValues(ex, "@KillProcess()", String.Concat("ProcessName: ", processName))
				End Try
			Next
		Next
	End Sub

	Private Sub UpdateTextMethod(ByVal strMessage As String)
		frmMain.UpdateTextMethod(strMessage)
	End Sub

	Private Function UpdateTextTranslated(ByVal number As Integer) As String
		Return frmMain.UpdateTextTranslated(number)
	End Function

End Class

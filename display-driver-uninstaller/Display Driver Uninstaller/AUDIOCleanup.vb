Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32

Public Class AUDIOCleanup
	'todo
	Dim CleanupEngine As New CleanupEngine

	Public Sub Start(ByVal config As ThreadSettings)

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

		UpdateTextMethod(UpdateTextTranslated(20) + " " & config.SelectedAUDIO.ToString() & " " + UpdateTextTranslated(21))
		Application.Log.AddMessage("Uninstalling " + config.SelectedAUDIO.ToString() + " driver ...")
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

		Select Case config.SelectedAUDIO
			Case AudioVendor.Realtek
				CleanRealtekserviceprocess()
				CleanRealtek(config)
				CleanRealtekFolders(config)
			Case AudioVendor.SoundBlaster
				'Todo
		End Select

		config.Success = True
	End Sub

	Private Sub CleanRealtekserviceprocess()
		Application.Log.AddMessage("Cleaning Process/Services...")

		CleanupEngine.Cleanserviceprocess(IO.File.ReadAllLines(Application.Paths.AppBase & "settings\REALTEK\services.cfg"))

		KillProcess("RtkNGUI64")
		Application.Log.AddMessage("Process/Services CleanUP Complete")
		System.Threading.Thread.Sleep(10)
	End Sub

	Private Sub CleanRealtek(ByVal config As ThreadSettings)
		Dim win10 As Boolean = frmMain.win10
		Dim packages As String()
		Dim wantedvalue As String = Nothing

		Application.Log.AddMessage("Cleaning known Regkeys")

		'Removal of the (DCH) Nvidia control panel comming from the Window Store. (In progress...)
		If win10 Then
			CleanupEngine.RemoveAppx("RealtekAudioControl")
		End If

		Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")

		CleanupEngine.ClassRoot(IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\classroot.cfg"))  '// add each line as String Array.

		CleanupEngine.Clsidleftover(IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\clsidleftover.cfg"))

		Application.Log.AddMessage("Removing known Packages")

		packages = IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\packages.cfg")   '// add each line as String Array.

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)

							If subregkey IsNot Nothing Then
								If IsNullOrWhitespace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then Continue For
								wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
								If IsNullOrWhitespace(wantedvalue) Then Continue For
								For i As Integer = 0 To packages.Length - 1
									If IsNullOrWhitespace(packages(i)) Then Continue For
									If StrContainsAny(wantedvalue, True, packages(i)) Then
										Try
											If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
												Deletesubregkey(regkey, child)
											End If
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			packages = IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\packages.cfg")   '// add each line as String Array.
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
								 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then Continue For
									wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
									If IsNullOrWhitespace(wantedvalue) Then Continue For
									For i As Integer = 0 To packages.Length - 1
										If IsNullOrWhitespace(packages(i)) Then Continue For
										If StrContainsAny(wantedvalue, True, packages(i)) Then
											Try
												Deletesubregkey(regkey, child)
											Catch ex As Exception
												Application.Log.AddException(ex)
											End Try
										End If
									Next
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

	End Sub

	Private Sub CleanRealtekFolders(ByVal config As ThreadSettings)
		Dim filePath As String = Nothing
		UpdateTextMethod(UpdateTextTranslated(1))

		Application.Log.AddMessage("Cleaning Directories (Please Wait...)")

		CleanupEngine.Folderscleanup(IO.File.ReadAllLines(Application.Paths.AppBase & "settings\REALTEK\driverfiles.cfg"))

		filePath = config.Paths.ProgramFiles + "Realtek"
		If FileIO.ExistsDir(filePath) Then

			For Each child As String In FileIO.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If StrContainsAny(child, True, "audio") Then

						Delete(child)

					End If
				End If
			Next
			If FileIO.CountDirectories(filePath) = 0 Then

				Delete(filePath)

			Else
				For Each data As String In FileIO.GetDirectories(filePath)
					If IsNullOrWhitespace(data) Then Continue For
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
				Next
			End If
		End If

		filePath = config.Paths.SysWOW64 + "RTCOM"
		If FileIO.ExistsDir(filePath) Then
			If filePath IsNot Nothing Then
				If FileIO.CountDirectories(filePath) = 0 Then
					Delete(filePath)
				Else
					For Each data As String In FileIO.GetDirectories(filePath)
						If IsNullOrWhitespace(data) Then Continue For
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
					Next
				End If
			End If
		End If


		'64Bit zone
		If IntPtr.Size = 8 Then
			filePath = config.Paths.ProgramFilesx86 + "Realtek"
			If FileIO.ExistsDir(filePath) Then
				If filePath IsNot Nothing Then

					For Each child As String In FileIO.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If StrContainsAny(child, True, "Audio") Then

								Delete(child)

							End If
						End If
					Next
					If FileIO.CountDirectories(filePath) = 0 Then

						Delete(filePath)

					Else
						For Each data As String In FileIO.GetDirectories(filePath)
							If IsNullOrWhitespace(data) Then Continue For
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
						Next

					End If
				End If
			End If
		End If

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

	Private Sub Delete(ByVal filename As String)
		FileIO.Delete(filename)
		CleanupEngine.RemoveSharedDlls(filename)
	End Sub

	Private Sub Deletesubregkey(ByVal value1 As RegistryKey, ByVal value2 As String)
		CleanupEngine.Deletesubregkey(value1, value2)
	End Sub

End Class

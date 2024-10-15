Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32

Namespace Display_Driver_Uninstaller
	Public Class AUDIOCleanup
		'todo
		Private _cleanupEngine As New CleanupEngine
		Private _fileIO As New FileIO

		Public Sub Start(ByVal config As ThreadSettings)
			Dim win10 As Boolean = FrmMain.IsWindows10
			Dim vendidexpected As String = ""
			Dim VendidSC As String() = Nothing   ' "SoftwareComponent" Vendor ID

			Select Case config.SelectedAUDIO
				Case AudioVendor.Realtek
					vendidexpected = "VEN_10EC" : VendidSC = {"VEN_10EC&ASIO", "VEN_10EC&AID", "VEN_10EC&SID", "VEN_10EC&HID"}
				Case AudioVendor.SoundBlaster
					vendidexpected = "VEN_1102" : VendidSC = {"VEN_1102"}
				Case AudioVendor.None
					vendidexpected = "NONE" : VendidSC = {"NONE"}
			End Select

			If vendidexpected = "NONE" Then
				Application.Log.AddWarningMessage("VendID is NONE, this is unexpected, cleaning aborted.")
				Exit Sub
			End If

			UpdateTextMethod(UpdateTextTranslated(20) + " " & config.SelectedAUDIO.ToString() & " " + UpdateTextTranslated(21))
			Application.Log.AddMessage("Uninstalling " + config.SelectedAUDIO.ToString() + " driver ...")
			UpdateTextMethod(UpdateTextTranslated(22))

			'----------------------------------------------------------------------------------
			'--Identifying and removing the Audio card + AudioEndpoint+SoftwareComponent(DCH)--
			'----------------------------------------------------------------------------------
			Try
				UpdateTextMethod(UpdateTextTranslated(24))
				Application.Log.AddMessage("Executing SetupAPI Remove Audio controler.")
				Dim AudioDevices As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendidexpected, False, True)
				If AudioDevices.Count > 0 Then
					For Each AudioDevice As SetupAPI.Device In AudioDevices

						'Removing Audio endpoints
						Dim AudioEnpointfound As List(Of SetupAPI.Device) = SetupAPI.GetDevices("audioendpoint", Nothing, False, True)
						If AudioEnpointfound.Count > 0 Then
							For Each d2 As SetupAPI.Device In AudioEnpointfound
								If d2 IsNot Nothing Then
									For Each Parent As SetupAPI.Device In d2.ParentDevices
										If Parent IsNot Nothing Then
											If StrContainsAny(Parent.DeviceID, True, AudioDevice.DeviceID) Then
												SetupAPI.UninstallDevice(d2) 'Removing the audioenpoint associated with the device we are trying to remove.
											End If
										End If
									Next
								End If
							Next
							AudioEnpointfound.Clear()
						End If

						'Removing Software components (DCH stuff, win10+)
						If win10 Then
							Dim SCfound As List(Of SetupAPI.Device) = SetupAPI.GetDevices("SoftwareComponent", Nothing, False, True)
							If SCfound.Count > 0 Then
								For Each d3 As SetupAPI.Device In SCfound
									For Each Parent As SetupAPI.Device In d3.ParentDevices
										If Parent IsNot Nothing Then
											If StrContainsAny(Parent.DeviceID, True, AudioDevice.DeviceID) Then
												SetupAPI.UninstallDevice(d3)
											End If
										End If
									Next
								Next
								SCfound.Clear()
							End If
							If config.RemoveAudioBus Then
								For Each Parent As SetupAPI.Device In AudioDevice.ParentDevices
									If Parent IsNot Nothing Then
										SetupAPI.UninstallDevice(Parent) 'Removing the Audio bus.
									End If
								Next
							End If
						End If
						SetupAPI.UninstallDevice(AudioDevice) 'Removing the audio card
					Next
					AudioDevices.Clear()
				End If
				UpdateTextMethod(UpdateTextTranslated(25))
				Application.Log.AddMessage("SetupAPI Remove Audio controler Complete.")
			Catch ex As Exception
				'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				Application.Log.AddException(ex)
			End Try

			'Removing Audio endpoints
			If config.SelectedAUDIO = AudioVendor.Realtek Then
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("audioendpoint", Nothing, False)
				If found.Count > 0 Then
					For Each d As SetupAPI.Device In found
						If StrContainsAny(d.FriendlyName, True, "realtek high definition audio", "Realtek(R) Audio") Then
							SetupAPI.UninstallDevice(d)
						End If
					Next
					found.Clear()
				End If
			End If

			'Removing Software components (DCH stuff, win10+) (no parents, because old device is removed. SafeMode behavior)
			If win10 Then

				If config.SelectedAUDIO = AudioVendor.Realtek Then
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("SoftwareComponent", Nothing, False)
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.HardwareIDs(0), True, VendidSC) Then
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If
				End If
			End If

			System.Threading.Thread.Sleep(10)

			_cleanupEngine.Cleandriverstore(config)

			Select Case config.SelectedAUDIO
				Case AudioVendor.Realtek
					CleanRealtekserviceprocess(config)
					CleanRealtek(config)
					CleanRealtekFolders(config)
				Case AudioVendor.SoundBlaster
					'Todo
			End Select

			config.Success = True
		End Sub

		Private Sub CleanRealtekserviceprocess(ByVal config As ThreadSettings)
			Application.Log.AddMessage("Cleaning Process/Services...")

			_cleanupEngine.Cleanserviceprocess(IO.File.ReadAllLines(Application.Paths.AppBase & "settings\REALTEK\services.cfg"), config)

			KillProcess("RtkNGUI64", "RtkAudUService64", "audiodg")
			Application.Log.AddMessage("Process/Services CleanUP Complete")
			System.Threading.Thread.Sleep(10)
		End Sub

		Private Sub CleanRealtek(ByVal config As ThreadSettings)
			Dim win10 As Boolean = FrmMain.IsWindows10
			Dim packages As String()
			Dim wantedvalue As String = Nothing

			Application.Log.AddMessage("Cleaning known Regkeys")

			'Removal of the (DCH) Nvidia control panel comming from the Window Store. (In progress...)
			If win10 Then
				If CanDeprovisionPackageForAllUsersAsync() Then
					_cleanupEngine.RemoveAppx1809("RealtekAudioControl")
				Else
					_cleanupEngine.RemoveAppx("RealtekAudioControl")
				End If
			End If
			Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")

			_cleanupEngine.ClassRoot(IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\classroot.cfg"), config)  '// add each line as String Array.

			_cleanupEngine.Clsidleftover(IO.File.ReadAllLines(config.Paths.AppBase & "settings\REALTEK\clsidleftover.cfg"))

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

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For
						If StrContainsAny(child, True, "realtek", "ASIO") Then
							Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
								If regkey2 IsNot Nothing Then
									For Each child2 As String In regkey2.GetSubKeyNames()
										If IsNullOrWhitespace(child2) Then Continue For
										If StrContainsAny(child2, True, "aecbf", "audio", "realtekeffects", "realtekoptions", "smartampcmd", "spkprotection", "Realtek ASIO") Then
											Try
												Deletesubregkey(regkey2, child2)
											Catch ex As Exception
												Application.Log.AddException(ex)
											End Try
										End If
									Next
									If regkey2.SubKeyCount = 0 Then
										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try
									Else
										For Each data As String In regkey2.GetSubKeyNames()
											If IsNullOrWhitespace(data) Then Continue For
											Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
										Next
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using

			If IntPtr.Size = 8 Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
	"Software\WOW6432Node", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							If StrContainsAny(child, True, "realtek", "ASIO") Then
								Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
									If regkey2 IsNot Nothing Then
										For Each child2 As String In regkey2.GetSubKeyNames()
											If IsNullOrWhitespace(child2) Then Continue For
											If StrContainsAny(child2, True, "aecbf", "audio", "realtekeffects", "realtekoptions", "smartampcmd", "spkprotection", "Realtek ASIO") Then
												Try
													Deletesubregkey(regkey2, child2)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										Next
										If regkey2.SubKeyCount = 0 Then
											Try
												Deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										Else
											For Each data As String In regkey2.GetSubKeyNames()
												If IsNullOrWhitespace(data) Then Continue For
												Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
											Next
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			End If

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Run", True)
					If regkey IsNot Nothing Then
						Try
							Deletevalue(regkey, "RtkAudUService")
						Catch ex As Exception
						End Try
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

		End Sub

		Private Sub CleanRealtekFolders(ByVal config As ThreadSettings)
			Dim filePath As String = Nothing
			UpdateTextMethod(UpdateTextTranslated(1))

			Application.Log.AddMessage("Cleaning Directories (Please Wait...)")

			_cleanupEngine.Folderscleanup(IO.File.ReadAllLines(Application.Paths.AppBase & "settings\REALTEK\driverfiles.cfg"), config)

			filePath = config.Paths.ProgramFiles + "Realtek"
			If _fileIO.ExistsDir(filePath) Then

				For Each child As String In _fileIO.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If StrContainsAny(child, True, "audio") Then

							Delete(child)

						End If
					End If
				Next
				If _fileIO.CountDirectories(filePath) = 0 Then

					Delete(filePath)

				Else
					For Each data As String In _fileIO.GetDirectories(filePath)
						If IsNullOrWhitespace(data) Then Continue For
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
					Next
				End If
			End If

			filePath = config.Paths.SysWOW64 + "RTCOM"
			If _fileIO.ExistsDir(filePath) Then
				If filePath IsNot Nothing Then
					If _fileIO.CountDirectories(filePath) = 0 Then
						Delete(filePath)
					Else
						For Each data As String In _fileIO.GetDirectories(filePath)
							If IsNullOrWhitespace(data) Then Continue For
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
						Next
					End If
				End If
			End If


			'64Bit zone
			If IntPtr.Size = 8 Then
				filePath = config.Paths.ProgramFilesx86 + "Realtek"
				If _fileIO.ExistsDir(filePath) Then
					If filePath IsNot Nothing Then

						For Each child As String In _fileIO.GetDirectories(filePath)
							If IsNullOrWhitespace(child) = False Then
								If StrContainsAny(child, True, "Audio") Then

									Delete(child)

								End If
							End If
						Next
						If _fileIO.CountDirectories(filePath) = 0 Then

							Delete(filePath)

						Else
							For Each data As String In _fileIO.GetDirectories(filePath)
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
			FrmMain.UpdateTextMethod(strMessage)
		End Sub

		Private Function UpdateTextTranslated(ByVal number As Integer) As String
			Return FrmMain.UpdateTextTranslated(number)
		End Function

		Private Sub Delete(ByVal filename As String)
			_fileIO.Delete(filename)
			_cleanupEngine.RemoveSharedDlls(filename)
		End Sub

		Private Sub Deletesubregkey(ByVal value1 As RegistryKey, ByVal value2 As String)
			_cleanupEngine.Deletesubregkey(value1, value2)
		End Sub

		Private Sub Deletevalue(ByVal value1 As RegistryKey, ByVal value2 As String)
			_cleanupEngine.Deletevalue(value1, value2)
		End Sub
	End Class
End Namespace
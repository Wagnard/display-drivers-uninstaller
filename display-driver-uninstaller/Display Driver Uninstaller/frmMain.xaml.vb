'    Display driver Uninstaller (DDU) a driver uninstaller / Cleaner for Windows
'    Copyright (C) <2013>  <DDU dev team>

'    This program is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.

'    This program is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.

'    You should have received a copy of the GNU General Public License
'    along with DDU.  If not, see <http://www.gnu.org/licenses/>.
Option Strict On

Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading
Imports System.Security.Principal
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports System.Text

Imports WinForm = System.Windows.Forms
Imports Display_Driver_Uninstaller.Win32

Public Class frmMain
	Friend Shared cleaningThread As Thread = Nothing
	Friend Shared workThread As Thread = Nothing

	Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
	Dim principal As WindowsPrincipal = New WindowsPrincipal(identity)
	Dim processinfo As New ProcessStartInfo
	Dim process As New Process

	Public Shared baseDir As String = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
	Public Shared win8higher As Boolean = Application.Settings.WinVersion > OSVersion.Win7
	Public win10 As Boolean = Application.Settings.WinVersion = OSVersion.Win10
	Public Shared winxp As Boolean = Application.Settings.WinVersion < OSVersion.WinVista

	Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive").ToLower
	Dim reply As String = Nothing
	Dim reply2 As String = Nothing

	Dim CleanupEngine As New CleanupEngine
	Dim enduro As Boolean = False

	Public Shared donotremoveamdhdaudiobusfiles As Boolean = True

	Private Sub CheckUpdatesThread(ByVal currentVersion As Version)
		Dim status As UpdateStatus = UpdateStatus.NotChecked

		Try
			Try
				If Not My.Computer.Network.IsAvailable Then
					status = UpdateStatus.Error
					Return
				End If
			Catch ex As Exception
			End Try

			Dim response As System.Net.WebResponse = Nothing
			Dim request As System.Net.WebRequest = System.Net.HttpWebRequest.Create("http://www.wagnardmobile.com/DDU/currentversion2.txt")
			request.Timeout = 2500

			Try
				response = request.GetResponse()
			Catch ex As Exception
				status = UpdateStatus.Error
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
		If Not Dispatcher.CheckAccess() Then
			Dispatcher.Invoke(Sub() Update(status))
		Else
			Application.Settings.UpdateAvailable = status

			If status = UpdateStatus.UpdateAvailable Then
				If Not Application.Settings.ShowSafeModeMsg Then
					Return
				End If

				Select Case MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text1"), "Display Driver Uninstaller", MessageBoxButton.YesNoCancel, MessageBoxImage.Information)
					Case MessageBoxResult.Yes
						WinAPI.OpenVisitLink(" -dduhome")

						Me.Close()
						Return

					Case MessageBoxResult.No
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text2"), "Display Driver Uninstaller", MessageBoxButton.OK, MessageBoxImage.Information)
				End Select
			End If
		End If
	End Sub

	Private Sub CheckUpdates()
		Try
			If Application.IsDebug Then
				Application.Settings.UpdateAvailable = UpdateStatus.Error
			Else
				Dim currentVersion As Version = Application.Settings.AppVersion
				Dim trd As Thread = New Thread(Sub() CheckUpdatesThread(currentVersion)) With
				  {
				  .CurrentCulture = New Globalization.CultureInfo("en-US"),
				  .CurrentUICulture = New Globalization.CultureInfo("en-US"),
				  .IsBackground = True
				  }

				trd.Start()
			End If
		Catch ex As Exception
			Application.Log.AddException(ex, "Failed to start UpdateCheck thread!")
		End Try
	End Sub

	Private Sub cleandriverstore(ByVal config As ThreadSettings)
		Dim catalog As String = ""
		Dim CurrentProvider As String = ""
		UpdateTextMethod("Executing Driver Store cleanUP(finding OEM step)...")
		Application.Log.AddMessage("Executing Driver Store cleanUP(Find OEM)...")
		'Check the driver from the driver store  ( oemxx.inf)

		UpdateTextMethod(UpdateTextTranslated(0))

		Select Case config.SelectedGPU
			Case GPUVendor.Nvidia
				CurrentProvider = "NVIDIA"
			Case GPUVendor.AMD
				CurrentProvider = "AdvancedMicroDevices"
			Case GPUVendor.Intel
				CurrentProvider = "Intel"
		End Select


		For Each oem As Inf In GetOemInfList(Application.Paths.WinDir & "inf\")
			If Not oem.IsValid Then
				Continue For
			End If

			If StrContainsAny(oem.Provider, True, CurrentProvider) Or
			   oem.Provider.ToLower.StartsWith("atitech") Or
			   oem.Provider.ToLower.StartsWith("amd") Then

				'before removing the oem we try to get the original inf name (win8+)
				If win8higher Then
					Try
						catalog = myregistry.opensubkey(registry.localmachine, "DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName).GetValue("Active").ToString
						catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
					Catch ex As Exception
						catalog = ""
					End Try
				End If
				If StrContainsAny(oem.Class, True, "display") Or StrContainsAny(oem.Class, True, "media") Then
					SetupAPI.RemoveInf(oem, True)
				Else
					If Not StrContainsAny(oem.Class, True, "HDC") Then 'we dont want to ever remove an HDC class device or info.
						SetupAPI.RemoveInf(oem, False)
					End If
				End If
			End If
			'check if the oem was removed to process to the pnplockdownfile if necessary
			If win8higher AndAlso (Not FileIO.ExistsFile(oem.FileName)) AndAlso (Not IsNullOrWhitespace(catalog)) Then
				CleanupEngine.prePnplockdownfiles(catalog)
			End If
		Next

		UpdateTextMethod("Driver Store cleanUP complete.")

		Application.Log.AddMessage("Driver Store CleanUP Complete.")

	End Sub

	Private Sub cleanamdserviceprocess()


		CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(baseDir & "\settings\AMD\services.cfg"))	'// add each line as String Array.

		Dim killpid As New ProcessStartInfo
		killpid.FileName = "cmd.exe"
		killpid.Arguments = " /C" & "taskkill /f /im CLIStart.exe"
		killpid.UseShellExecute = False
		killpid.CreateNoWindow = True
		killpid.RedirectStandardOutput = False

		Dim processkillpid As New Process
		processkillpid.StartInfo = killpid
		processkillpid.Start()
		processkillpid.WaitForExit()
		processkillpid.Close()

		KillProcess(
		 "MOM",
		 "CLIStart",
		 "CLI",
		 "CCC",
		 "Cnext",
		 "HydraDM",
		 "HydraDM64",
		 "HydraGrd",
		 "Grid64",
		 "HydraMD64",
		 "HydraMD",
		 "RadeonSettings",
		 "ThumbnailExtractionHost",
		 "jusched")

		System.Threading.Thread.Sleep(10)
	End Sub

	Private Sub cleanamdfolders(ByVal config As ThreadSettings)
		Dim filePath As String = Nothing
		Dim removedxcache As Boolean = config.RemoveCrimsonCache
		'Delete AMD data Folders
		UpdateTextMethod(UpdateTextTranslated(1))

		Application.Log.AddMessage("Cleaning Directory (Please Wait...)")


		If config.RemoveAMDDirs Then
			filePath = sysdrv + "\AMD"

			Delete(filePath)

		End If

		'Delete driver files
		'delete OpenCL

		CleanupEngine.folderscleanup(IO.File.ReadAllLines(baseDir & "\settings\AMD\driverfiles.cfg")) '// add each line as String Array.



		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			Delete(filePath + "\atiogl.xml")
		Catch ex As Exception
		End Try

		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			Delete(filePath + "\ativpsrm.bin")
		Catch ex As Exception
		End Try


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
		If FileIO.ExistsDir(filePath) Then

			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ati.ace") Or
					   child.ToLower.Contains("ati catalyst control center") Or
					   child.ToLower.Contains("application profiles") Or
					   child.ToLower.EndsWith("\px") Or
					   child.ToLower.Contains("hydravision") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\ATI"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("cim") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If


		filePath = Environment.GetFolderPath _
		  (Environment.SpecialFolder.ProgramFiles) + "\Common Files" + "\ATI Technologies"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("multimedia") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\AMD APP"
		If FileIO.ExistsDir(filePath) Then

			Delete(filePath)

		End If

		If IntPtr.Size = 8 Then

			filePath = Environment.GetFolderPath _
			  (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"
			If FileIO.ExistsDir(filePath) Then
				Try
					For Each child As String In Directory.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ati.ace") Or
							 child.ToLower.Contains("ati catalyst control center") Or
							 child.ToLower.Contains("application profiles") Or
							 child.ToLower.EndsWith("\px") Or
							 child.ToLower.Contains("hydravision") Then

								Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			End If

			filePath = System.Environment.SystemDirectory
			Dim files() As String = IO.Directory.GetFiles(filePath + "\", "coinst_*.*")
			For i As Integer = 0 To files.Length - 1
				If Not IsNullOrWhitespace(files(i)) Then

					Delete(files(i))

				End If
			Next

			filePath = Environment.GetFolderPath _
			   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoFirefox"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoChrome"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies"
			If FileIO.ExistsDir(filePath) Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("multimedia") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			End If
		End If


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Catalyst Control Center"
		If FileIO.ExistsDir(filePath) Then

			Delete(filePath)

		End If


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Catalyst Control Center"
		If FileIO.ExistsDir(filePath) Then

			Delete(filePath)

		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\ATI"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ace") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\AMD"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("kdb") Or _
					   child.ToLower.Contains("fuel") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(config.Paths.UserPath))
			filePath = filepaths + "\AppData\Roaming\ATI"
			If winxp Then
				filePath = filepaths + "\Application Data\ATI"
			End If
			If FileIO.ExistsDir(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ace") Then

								Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
					Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
				End Try
			End If


			filePath = filepaths + "\AppData\Local\ATI"
			If winxp Then
				filePath = filepaths + "\Local Settings\Application Data\ATI"
			End If
			If FileIO.ExistsDir(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ace") Then

								Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
					Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
				End Try
			End If

			filePath = filepaths + "\AppData\Local\AMD"
			If winxp Then
				filePath = filepaths + "\Local Settings\Application Data\AMD"
			End If
			If FileIO.ExistsDir(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("cn") Or
							 child.ToLower.Contains("fuel") Or _
							 removedxcache AndAlso child.ToLower.Contains("dxcache") Or _
							 removedxcache AndAlso child.ToLower.Contains("glcache") Then

								Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
					Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
				End Try
			End If

		Next

		'starting with AMD  14.12 Omega driver folders

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\AMD"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("amdkmpfd") Or
					 child.ToLower.Contains("cnext") Or
					 child.ToLower.Contains("steadyvideo") Or
					 child.ToLower.Contains("920dec42-4ca5-4d1d-9487-67be645cddfc") Or
					   child.ToLower.Contains("cim") Then

						Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					Delete(filePath)

				Else
					For Each data As String In Directory.GetDirectories(filePath)
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
					Next

				End If
			Catch ex As Exception
			End Try
		End If

		filePath = Environment.GetFolderPath _
		  (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD"
		If FileIO.ExistsDir(filePath) Then

			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ati.ace") Or _
					   child.ToLower.Contains("cnext") Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		'Cleaning the CCC assemblies.


		filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_64"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.EndsWith("\mom") Or
					 child.ToLower.Contains("\mom.") Or
					 child.ToLower.Contains("newaem.foundation") Or
					 child.ToLower.Contains("fuel.foundation") Or
					 child.ToLower.Contains("\localizatio") Or
					 child.ToLower.EndsWith("\log") Or
					 child.ToLower.Contains("log.foundat") Or
					 child.ToLower.EndsWith("\cli") Or
					 child.ToLower.Contains("\cli.") Or
					 child.ToLower.Contains("ace.graphi") Or
					 child.ToLower.Contains("adl.foundation") Or
					 child.ToLower.Contains("64\aem.") Or
					 child.ToLower.Contains("aticccom") Or
					 child.ToLower.EndsWith("\ccc") Or
					 child.ToLower.Contains("\ccc.") Or
					 child.ToLower.Contains("\pckghlp.") Or
					 child.ToLower.Contains("\resourceman") Or
					 child.ToLower.Contains("\apm.") Or
					 child.ToLower.Contains("\a4.found") Or
					 child.ToLower.Contains("\atixclib") Or
					   child.ToLower.Contains("\dem.") Then

						Delete(child)

					End If
				End If
			Next
		End If

		filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\GAC_MSIL"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.EndsWith("\mom") Or
					 child.ToLower.Contains("\mom.") Or
					 child.ToLower.Contains("newaem.foundation") Or
					 child.ToLower.Contains("fuel.foundation") Or
					 child.ToLower.Contains("\localizatio") Or
					 child.ToLower.EndsWith("\log") Or
					 child.ToLower.Contains("log.foundat") Or
					 child.ToLower.EndsWith("\cli") Or
					 child.ToLower.Contains("\cli.") Or
					 child.ToLower.Contains("ace.graphi") Or
					 child.ToLower.Contains("adl.foundation") Or
					 child.ToLower.Contains("64\aem.") Or
					 child.ToLower.Contains("msil\aem.") Or
					 child.ToLower.Contains("aticccom") Or
					 child.ToLower.EndsWith("\ccc") Or
					 child.ToLower.Contains("\ccc.") Or
					 child.ToLower.Contains("\pckghlp.") Or
					 child.ToLower.Contains("\resourceman") Or
					 child.ToLower.Contains("\apm.") Or
					 child.ToLower.Contains("\a4.found") Or
					 child.ToLower.Contains("\atixclib") Or
					 child.ToLower.Contains("\dem.") Then

						Delete(child)

					End If
				End If
			Next
		End If

	End Sub

	Private Sub cleanamd(ByVal config As ThreadSettings)

		Dim wantedvalue As String = Nothing
		Dim wantedvalue2 As String = Nothing
		Dim filePath As String = Nothing
		Dim packages As String()

		UpdateTextMethod(UpdateTextTranslated(2))
		Application.Log.AddMessage("Cleaning known Regkeys")


		'Delete AMD regkey
		'Deleting DCOM object

		Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")

		CleanupEngine.ClassRoot(IO.File.ReadAllLines(baseDir & "\settings\AMD\classroot.cfg")) '// add each line as String Array.


		'-----------------
		'interface cleanup
		'-----------------



		CleanupEngine.interfaces(IO.File.ReadAllLines(baseDir & "\settings\AMD\interface.cfg"))	'// add each line as String Array.

		Application.Log.AddMessage("Instance class cleanUP")
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "CLSID", False)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "CLSID\" & child, False)
								If subregkey IsNot Nothing Then
									Using subregkey2 As RegistryKey = myregistry.opensubkey(registry.classesroot, "CLSID\" & child & "\Instance", False)
										If subregkey2 IsNot Nothing Then
											For Each child2 As String In subregkey2.GetSubKeyNames()
												If IsNullOrWhitespace(child2) = False Then
													Using superkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "CLSID\" & child & "\Instance\" & child2)
														If superkey IsNot Nothing Then
															If IsNullOrWhitespace(CStr(superkey.GetValue("FriendlyName"))) = False Then
																wantedvalue2 = superkey.GetValue("FriendlyName").ToString
																If wantedvalue2.ToLower.Contains("ati mpeg") Or
																 wantedvalue2.ToLower.Contains("amd mjpeg") Or
																 wantedvalue2.ToLower.Contains("ati ticker") Or
																 wantedvalue2.ToLower.Contains("mmace softemu") Or
																 wantedvalue2.ToLower.Contains("mmace deinterlace") Or
																 wantedvalue2.ToLower.Contains("amd video") Or
																 wantedvalue2.ToLower.Contains("mmace procamp") Or
																 wantedvalue2.ToLower.Contains("ati video") Then
																	Try
																		deletesubregkey(Registry.ClassesRoot, "CLSID\" & child & "\Instance\" & child2)
																	Catch ex As Exception
																	End Try
																End If
															End If
														End If
													End Using
												End If
											Next
										End If
									End Using
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\CLSID", False)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\CLSID\" & child, False)
									If subregkey IsNot Nothing Then
										Using subregkey2 As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\CLSID\" & child & "\Instance", False)
											If subregkey2 IsNot Nothing Then
												For Each child2 As String In subregkey2.GetSubKeyNames()
													If IsNullOrWhitespace(child2) = False Then
														Using superkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2)
															If superkey IsNot Nothing Then
																If IsNullOrWhitespace(CStr(superkey.GetValue("FriendlyName"))) = False Then
																	wantedvalue2 = superkey.GetValue("FriendlyName").ToString
																	If wantedvalue2.ToLower.Contains("ati mpeg") Or
																	wantedvalue2.ToLower.Contains("amd mjpeg") Or
																	wantedvalue2.ToLower.Contains("ati ticker") Or
																	wantedvalue2.ToLower.Contains("mmace softemu") Or
																	wantedvalue2.ToLower.Contains("mmace deinterlace") Or
																	wantedvalue2.ToLower.Contains("mmace procamp") Or
																	wantedvalue2.ToLower.Contains("amd video") Or
																	wantedvalue2.ToLower.Contains("ati video") Then
																		Try
																			deletesubregkey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2)
																		Catch ex As Exception
																		End Try
																	End If
																End If
															End If
														End Using
													End If
												Next
											End If
										End Using
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Application.Log.AddMessage("MediaFoundation cleanUP")
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "MediaFoundation\Transforms", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child)
							If IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString) Then Continue For

							If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
								Using regkey3 As RegistryKey = myregistry.opensubkey(regkey, "Categories")
									For Each child2 As String In regkey3.GetSubKeyNames
										If IsNullOrWhitespace(child2) Then Continue For
										Using regkey4 As RegistryKey = myregistry.opensubkey(regkey, "Categories\" & child2, True)
											Try
												deletesubregkey(regkey4, child)
											Catch ex As Exception
											End Try
										End Using
									Next
								End Using
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\MediaFoundation\Transforms", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child)
								If IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString) Then Continue For

								If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
									Using regkey3 As RegistryKey = myregistry.opensubkey(regkey, "Categories")
										For Each child2 As String In regkey3.GetSubKeyNames
											If IsNullOrWhitespace(child2) Then Continue For
											Using regkey4 As RegistryKey = myregistry.opensubkey(regkey, "Categories\" & child2, True)
												Try
													deletesubregkey(regkey4, child)
												Catch ex As Exception
												End Try
											End Using
										Next
									End Using
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Application.Log.AddMessage("AppID and clsidleftover cleanUP")
		'old dcom 

		CleanupEngine.clsidleftover(IO.File.ReadAllLines(baseDir & "\settings\AMD\clsidleftover.cfg")) '// add each line as String Array.

		Application.Log.AddMessage("Record CleanUP")

		'--------------
		'Record cleanup
		'--------------
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Record", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = myregistry.opensubkey(regkey, child)
							If subregkey IsNot Nothing Then
								For Each childs As String In subregkey.GetSubKeyNames()
									If IsNullOrWhitespace(childs) Then Continue For

									Using regkey2 As RegistryKey = myregistry.opensubkey(subregkey, childs, False)
										If regkey2 IsNot Nothing Then
											If IsNullOrWhitespace(regkey2.GetValue("Assembly", String.Empty).ToString) Then Continue For

											If StrContainsAny(regkey2.GetValue("Assembly", String.Empty).ToString, True, "aticccom") Then
												Try
													deletesubregkey(regkey, child)
												Catch ex As Exception
												End Try
											End If
										End If
									End Using
								Next
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		Application.Log.AddMessage("Assembly CleanUP")

		'------------------
		'Assemblies cleanUP
		'------------------
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Classes\Installer\Assemblies", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ati.ace") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try

							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'----------------------
		'End Assemblies cleanUP
		'----------------------


		'end of decom?
		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
		  "Display\shellex\PropertySheetHandlers", True)
			Try
				deletesubregkey(regkey, "ATIACE")
			Catch ex As Exception
			End Try
		End Using

		'remove opencl registry Khronos

		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Khronos\OpenCL\Vendors", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("amdocl") Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
					If regkey.GetValueNames().Length = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\Khronos\OpenCL")
						Catch ex As Exception
						End Try
					End If
					CleanVulkan(config)
					Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Khronos", True)
						If subregkey IsNot Nothing Then
							If subregkey.GetSubKeyNames().Length = 0 Then
								Try
									deletesubregkey(Registry.LocalMachine, "Software\Khronos")
								Catch ex As Exception
								End Try
							End If
						End If
					End Using
				End If
			End Using
		Catch ex As Exception
		End Try

		If IntPtr.Size = 8 Then

			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames()
							If IsNullOrWhitespace(child) = False Then
								If child.ToLower.Contains("amdocl") Then
									Try
										deletevalue(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End If
						Next
						If regkey.GetValueNames().Length = 0 Then
							Try
								deletesubregkey(Registry.LocalMachine, "Software\WOW6432Node\Khronos\OpenCL")
							Catch ex As Exception
							End Try
						End If
						Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\WOW6432Node\Khronos", True)
							If subregkey IsNot Nothing Then
								If subregkey.GetSubKeyNames().Length = 0 Then
									Try
										deletesubregkey(Registry.LocalMachine, "Software\WOW6432Node\Khronos")
									Catch ex As Exception
									End Try
								End If
							End If
						End Using
					End If
				End Using
			Catch ex As Exception
			End Try
		End If

		Application.Log.AddMessage("ngenservice Clean")

		'----------------------
		'.net ngenservice clean
		'----------------------
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ati.ace") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'-----------------------------
		'End of .net ngenservice clean
		'-----------------------------

		'-----------------------------
		'Shell extensions\aprouved
		'-----------------------------
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) = False Then
							If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
							 regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames()
							If IsNullOrWhitespace(child) = False Then
								If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
								 regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
									Try
										deletevalue(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
		'-----------------------------
		'End Shell extensions\aprouved
		'-----------------------------

		Application.Log.AddMessage("Pnplockdownfiles region cleanUP")

		CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(baseDir & "\settings\AMD\driverfiles.cfg"))	'// add each line as String Array.

		Try

			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\amdkmdap")
		Catch ex As Exception
		End Try

		If IntPtr.Size = 8 Then
			Try
				deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
			Catch ex As Exception
			End Try

			Try
				deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
			Catch ex As Exception
			End Try
		End If

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
		Catch ex As Exception
		End Try

		'---------------------------------------------
		'Cleaning of Legacy_AMDKMDAG+ on win7 and lower
		'---------------------------------------------

		Try
			If config.WinVersion < OSVersion.Win81 AndAlso WinForm.SystemInformation.BootMode <> WinForm.BootMode.Normal Then 'win 7 and lower + safemode only
				Application.Log.AddMessage("Cleaning LEGACY_AMDKMDAG")
				Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "SYSTEM")
					If subregkey IsNot Nothing Then
						For Each childs As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(childs) = False Then
								If childs.ToLower.Contains("controlset") Then
									Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
									  "SYSTEM\" & childs & "\Enum\Root")
										If regkey IsNot Nothing Then
											For Each child As String In regkey.GetSubKeyNames()
												If IsNullOrWhitespace(child) = False Then
													If child.ToLower.Contains("legacy_amdkmdag") Or _
													 (child.ToLower.Contains("legacy_amdkmpfd") AndAlso config.RemoveAMDKMPFD) Or _
													 child.ToLower.Contains("legacy_amdacpksd") Then

														Try
															deletesubregkey(Registry.LocalMachine, "SYSTEM\" & childs & "\Enum\Root\" & child)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If
												End If
											Next
										End If
									End Using
								End If
							End If
						Next
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'----------------------------------------------------
		'End of Cleaning of Legacy_AMDKMDAG on win7 and lower
		'----------------------------------------------------


		'--------------------------------
		'System environement path cleanup
		'--------------------------------
		Application.Log.AddMessage("System environement cleanUP")
		Try
			Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
				If subregkey IsNot Nothing Then
					For Each child2 As String In subregkey.GetSubKeyNames()
						If child2.ToLower.Contains("controlset") Then
							Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetValueNames()
										If IsNullOrWhitespace(child) = False Then
											If child.Contains("AMDAPPSDKROOT") Then
												Try
													deletesubregkey(regkey, child)
												Catch ex As Exception
												End Try
											End If
											If child.Contains("Path") Then
												If IsNullOrWhitespace(CStr(regkey.GetValue(child))) = False Then
													wantedvalue = regkey.GetValue(child).ToString.ToLower
													Try
														Select Case True
															Case wantedvalue.Contains(";" + sysdrv & "\program files (x86)\amd app\bin\x86_64")
																wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\amd app\bin\x86_64", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(sysdrv & "\program files (x86)\amd app\bin\x86_64;")
																wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\amd app\bin\x86_64;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + sysdrv & "\program files (x86)\amd app\bin\x86")
																wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\amd app\bin\x86", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(sysdrv & "\program files (x86)\amd app\bin\x86;")
																wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\amd app\bin\x86;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static")
																wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;")
																wantedvalue = wantedvalue.Replace(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + sysdrv & "\program Files (x86)\amd\ati.ace\core-static")
																wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(sysdrv & "\program Files (x86)\amd\ati.ace\core-static;")
																wantedvalue = wantedvalue.Replace(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;", "")
																regkey.SetValue(child, wantedvalue)

														End Select
													Catch ex As Exception
													End Try
												End If
											End If
										End If
									Next
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'end system environement patch cleanup

		'-----------------------
		'remove event view stuff
		'-----------------------
		Application.Log.AddMessage("Remove eventviewer stuff")
		Try
			Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
				If subregkey IsNot Nothing Then
					For Each child2 As String In subregkey.GetSubKeyNames()
						If IsNullOrWhitespace(child2) = False Then
							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\" & child2 & "\Services\eventlog", True)
									If regkey IsNot Nothing Then
										For Each child As String In regkey.GetSubKeyNames()
											If IsNullOrWhitespace(child) = False Then
												If child.ToLower.Contains("aceeventlog") Then
													deletesubregkey(regkey, child)
												End If
											End If
										Next


										Try
											deletesubregkey(regkey, "Application\ATIeRecord")
										Catch ex As Exception
										End Try

										Try
											deletesubregkey(regkey, "System\amdkmdag")
										Catch ex As Exception
										End Try

										Try
											deletesubregkey(regkey, "System\amdkmdap")
										Catch ex As Exception
										End Try
									End If
								End Using
								Try
									deletesubregkey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Services\Atierecord")
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		'--------------------------------
		'end of eventviewer stuff removal
		'--------------------------------
		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
			 "Directory\background\shellex\ContextMenuHandlers", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.Contains("ACE") Then

								deletesubregkey(regkey, child)

							End If
						End If

					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		' to fix later, the range is too large and could lead to problems.
		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) = False Then
									If child.StartsWith("ATI") Then
										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try
									End If
								End If
							Next
						End If
					End Using
				End If
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames()
							If IsNullOrWhitespace(child) Then Continue For

							If StrContainsAny(child, True, "radeonsettings.exe") Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						Next
					End If
				End Using
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		' to fix later, the range is too large and could lead to problems.
		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) = False Then
									If child.StartsWith("AMD") Then
										deletesubregkey(regkey, child)
									End If
								End If
							Next
						End If
					End Using
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try

			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\ATI", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ace") Or
							 child.ToLower.Contains("appprofiles") Or
							   child.ToLower.Contains("install") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
					If regkey.SubKeyCount = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\ATI")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\ATI Technologies", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("cbt") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
							If child.ToLower.Contains("ati catalyst control center") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
							If child.ToLower.Contains("cds") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If

							If child.ToLower.Contains("install") Then
								'here we check the install path location in case CCC is not installed on the system drive.  A kill to explorer must be made
								'to help cleaning in normal mode.
								If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
									Application.Log.AddMessage("Killing Explorer.exe")
									KillProcess("explorer")
								End If

								Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child, True)
									If regkey2 IsNot Nothing Then
										If Not IsNullOrWhitespace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then

											filePath = regkey2.GetValue("InstallDir", String.Empty).ToString

											If Not IsNullOrWhitespace(filePath) AndAlso FileIO.ExistsDir(filePath) Then
												For Each childf As String In Directory.GetDirectories(filePath)
													If IsNullOrWhitespace(childf) Then Continue For

													If StrContainsAny(childf, True, "ati.ace", "cnext", "amdkmpfd", "cim") Then
														Delete(childf)
													End If
												Next
												If Directory.GetDirectories(filePath).Length = 0 Then

													Delete(filePath)

												End If
												If Not Directory.Exists(filePath) Then
													'here we will do a special environement path cleanup as there is chances that the installation is
													'somewhere else.
													AmdEnvironementPath(filePath)
												End If
											End If
										End If
										For Each child2 As String In regkey2.GetSubKeyNames()
											If IsNullOrWhitespace(child2) Then Continue For

											If StrContainsAny(child2, True, "ati catalyst", "ati mcat", "avt", "ccc", "cnext", "amd app sdk", "packages",
											   "wirelessdisplay", "hydravision", "avivo", "ati display driver", "installed drivers", "steadyvideo") Then
												Try
													deletesubregkey(regkey2, child2)
												Catch ex As Exception
												End Try
											End If
										Next
										For Each values As String In regkey2.GetValueNames()
											Try
												deletevalue(regkey2, values) 'This is for windows 7, it prevent removing the South Bridge and fix the Catalyst "Upgrade"
											Catch ex As Exception
											End Try
										Next
										If regkey2.SubKeyCount = 0 Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End If
								End Using
							End If
						End If
					Next
					If regkey.SubKeyCount = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\ATI Technologies")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\AMD", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("eeu") Or
							   child.ToLower.Contains("fuel") Or
							   child.ToLower.Contains("cn") Or
							   child.ToLower.Contains("mftvdecoder") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
					If regkey.SubKeyCount = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\AMD")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Wow6432Node\ATI", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								If child.ToLower.Contains("ace") Or
								   child.ToLower.Contains("appprofiles") Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End If
						Next
						If regkey.SubKeyCount = 0 Then
							Try
								deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\ATI")
							Catch ex As Exception
							End Try
						End If
					End If
				End Using

				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Wow6432Node\AMD", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								If child.ToLower.Contains("eeu") Or
								   child.ToLower.Contains("mftvdecoder") Then

									deletesubregkey(regkey, child)

								End If
							End If
						Next
						If regkey.SubKeyCount = 0 Then
							deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\AMD")
						End If
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Wow6432Node\ATI Technologies", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								If child.ToLower.Contains("system wide settings") Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
								If child.ToLower.Contains("install") Then
									'here we check the install path location in case CCC is not installed on the system drive.  A kill to explorer must be made
									'to help cleaning in normal mode.
									If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
										Application.Log.AddMessage("Killing Explorer.exe")
										KillProcess("explorer")
									End If

									Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child, True)
										If regkey2 IsNot Nothing Then
											If Not IsNullOrWhitespace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then

												filePath = regkey2.GetValue("InstallDir", String.Empty).ToString
												If Not IsNullOrWhitespace(filePath) AndAlso My.Computer.FileSystem.DirectoryExists(filePath) Then
													For Each childf As String In Directory.GetDirectories(filePath)
														If IsNullOrWhitespace(childf) Then Continue For

														If StrContainsAny(childf, True, "ati.ace", "cnext", "amdkmpfd", "cim") Then

															Delete(childf)

														End If
													Next
													If Directory.GetDirectories(filePath).Length = 0 Then

														Delete(filePath)

													End If
												End If
											End If

											For Each child2 As String In regkey2.GetSubKeyNames()
												If IsNullOrWhitespace(child2) Then Continue For

												If StrContainsAny(child2, True, "ati catalyst", "ati mcat", "avt", "ccc", "cnext", "packages",
												   "wirelessdisplay", "hydravision", "dndtranscoding64", "avivo", "steadyvideo") Then
													Try
														deletesubregkey(regkey2, child2)
													Catch ex As Exception
													End Try
												End If
											Next
											If MyRegistry.OpenSubKey(regkey, child).SubKeyCount = 0 Then
												Try
													deletesubregkey(regkey, child)
												Catch ex As Exception
												End Try
											End If
										End If
									End Using
								End If
							End If
						Next
						If regkey.SubKeyCount = 0 Then
							Try
								deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\ATI Technologies")
							Catch ex As Exception
							End Try
						End If
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Windows\CurrentVersion\Run", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetValueNames
								If IsNullOrWhitespace(child) Then Continue For

								If StrContainsAny(child, True, "HydraVisionDesktopManager", "Grid", "HydraVisionMDEngine") Then
									deletevalue(regkey, child)
								End If
							Next
						End If
					End Using
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Application.Log.AddMessage("Removing known Packages")

		packages = IO.File.ReadAllLines(baseDir & "\settings\AMD\packages.cfg")	'// add each line as String Array.
		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)

								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
										wantedvalue = subregkey.GetValue("DisplayName").ToString
										If IsNullOrWhitespace(wantedvalue) = False Then
											For i As Integer = 0 To packages.Length - 1
												If Not IsNullOrWhitespace(packages(i)) Then
													If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
														Try
															If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
																deletesubregkey(regkey, child)
															End If
														Catch ex As Exception
														End Try
													End If
												End If
											Next
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			packages = IO.File.ReadAllLines(baseDir & "\settings\AMD\packages.cfg")	'// add each line as String Array.
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
								 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
									If subregkey IsNot Nothing Then
										If IsNullOrWhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
											wantedvalue = subregkey.GetValue("DisplayName").ToString
											If IsNullOrWhitespace(wantedvalue) = False Then
												For i As Integer = 0 To packages.Length - 1
													If Not IsNullOrWhitespace(packages(i)) Then
														If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
															Try
																deletesubregkey(regkey, child)
															Catch ex As Exception
															End Try
														End If
													End If
												Next
											End If
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		CleanupEngine.installer(IO.File.ReadAllLines(baseDir & "\settings\AMD\packages.cfg"), config)

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			 "Software\Microsoft\Windows\CurrentVersion\Run", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames
						If Not IsNullOrWhitespace(child) Then
							If StrContainsAny(child, True, "StartCCC", "StartCN", "AMD AVT") Then
								deletevalue(regkey, child)
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames
							If Not IsNullOrWhitespace(child) Then
								If StrContainsAny(child, True, "StartCCC", "StartCN", "AMD AVT") Then
									deletevalue(regkey, child)
								End If
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			 "Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) Then Continue For

						If child.Contains("ATI\CIM\") Or
						   child.Contains("AMD\CNext\") Or
						   child.Contains("AMD APP\") Or
						   child.Contains("AMD\SteadyVideo\") Or
						   child.Contains("HydraVision\") Then

							Try
								deletevalue(regkey, child)
							Catch ex As Exception
							End Try
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'prevent CCC reinstalltion (comes from drivers installed from windows updates)
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) Then Continue For
						If child.ToLower.Contains("launchwuapp") Then
							deletevalue(regkey, child)
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames()
							If IsNullOrWhitespace(child) Then Continue For
							If child.ToLower.Contains("launchwuapp") Then
								deletevalue(regkey, child)
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		'Saw on Win 10 cat 15.7
		Application.Log.AddMessage("AudioEngine CleanUP")
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "AudioEngine\AudioProcessingObjects", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child)
							If regkey2 IsNot Nothing Then
								If IsNullOrWhitespace(regkey2.GetValue("FriendlyName", String.Empty).ToString) Then Continue For

								If StrContainsAny(regkey2.GetValue("FriendlyName", String.Empty).ToString, True, "cdelayapogfx") Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try
								End If
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'SteadyVideo stuff

		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
		 "Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
							If subregkey IsNot Nothing Then
								If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
									wantedvalue = subregkey.GetValue("").ToString
									If IsNullOrWhitespace(wantedvalue) = False Then
										If wantedvalue.ToLower.Contains("steadyvideo") Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End If
								End If
							End If
						End Using
					End If
				Next
			End If
		End Using

		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "PROTOCOLS\Filter", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If Not IsNullOrWhitespace(child) Then
							Using subregkey As RegistryKey = myregistry.opensubkey(regkey, child, False)
								If subregkey IsNot Nothing Then
									If Not IsNullOrWhitespace(CStr(subregkey.GetValue(""))) Then
										wantedvalue = CStr(subregkey.GetValue(""))
										If Not IsNullOrWhitespace(wantedvalue) Then
											If wantedvalue.ToLower.Contains("steadyvideo") Then
												Try
													deletesubregkey(regkey, child)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			'SteadyVideo stuff

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
										wantedvalue = subregkey.GetValue("").ToString
										If IsNullOrWhitespace(wantedvalue) = False Then
											If wantedvalue.ToLower.Contains("steadyvideo") Then
												Try
													deletesubregkey(regkey, child)
												Catch ex As Exception
												End Try
											End If
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using


			Try
				Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Wow6432Node\PROTOCOLS\Filter", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If Not IsNullOrWhitespace(child) Then
								Using subregkey As RegistryKey = myregistry.opensubkey(regkey, child, False)
									If subregkey IsNot Nothing Then
										If Not IsNullOrWhitespace(CStr(subregkey.GetValue(""))) Then
											wantedvalue = CStr(subregkey.GetValue(""))
											If Not IsNullOrWhitespace(wantedvalue) Then
												If wantedvalue.ToLower.Contains("steadyvideo") Then
													Try
														deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End If
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

		End If

	End Sub

	Private Sub rebuildcountercache()
		Application.Log.AddMessage("Rebuilding the Perf.Counter cache X2")

		Try
			For i = 0 To 1
				Using process As Process = New Process() With
				  {
				   .StartInfo = New ProcessStartInfo("lodctr") With
				   {
				 .Arguments = "/R",
				 .WindowStyle = ProcessWindowStyle.Hidden,
				 .UseShellExecute = False,
				 .CreateNoWindow = True,
				 .RedirectStandardOutput = True
				   }
				  }

					process.Start()

					Application.Log.AddMessage(process.StandardOutput.ReadToEnd())

					process.Close()
				End Using	' Dispose() !
			Next

		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub fixregistrydriverstore()
		'Windows 8 + only
		'This should fix driver installation problem reporting that a file is not found.
		'It is usually caused by Windows somehow losing track of the driver store , This intend to help it a bit.
		If win8higher Then
			Application.Log.AddMessage("Fixing registry driverstore if necessary")
			Try

				Dim infslist As String = ""
				For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", Microsoft.VisualBasic.FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")
					If Not IsNullOrWhitespace(infs) Then
						infslist = infslist + infs
					End If
				Next
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "DRIVERS\DriverDatabase\DriverInfFiles", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							If child.ToLower.StartsWith("oem") AndAlso child.ToLower.EndsWith(".inf") Then
								If Not StrContainsAny(infslist, True, child) Then
									Try
										deletesubregkey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages\" & MyRegistry.OpenSubKey(regkey, child).GetValue("Active", String.Empty).ToString)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try

									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try
								End If
							End If
						Next
					End If
				End Using

				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "DRIVERS\DriverDatabase\DriverPackages", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child)
								If (Not IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString)) AndAlso
								 regkey2.GetValue("", String.Empty).ToString.ToLower.StartsWith("oem") AndAlso
								 regkey2.GetValue("", String.Empty).ToString.ToLower.EndsWith(".inf") AndAlso
								 (Not StrContainsAny(infslist, True, regkey2.GetValue("", String.Empty).ToString)) Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try
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
	Private Sub CleanVulkan(ByRef config As ThreadSettings)

		Dim FilePath As String = Nothing
		Dim files() As String = Nothing

		If config.RemoveVulkan Then
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Khronos", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If Not IsNullOrWhitespace(child) Then
							If StrContainsAny(child, True, "vulkan") Then
								deletesubregkey(regkey, child)
							End If
						End If
					Next
				End If
			End Using
			If IntPtr.Size = 8 Then
				Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\WOW6432Node\Khronos", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not IsNullOrWhitespace(child) Then
								If StrContainsAny(child, True, "vulkan") Then
									deletesubregkey(regkey, child)
								End If
							End If
						Next
					End If
				End Using
			End If

			If config.RemoveVulkan Then
				FilePath = System.Environment.SystemDirectory
				files = IO.Directory.GetFiles(FilePath + "\", "vulkan-1*.dll")
				For i As Integer = 0 To files.Length - 1
					If Not IsNullOrWhitespace(files(i)) Then
						Try
							Delete(files(i))
						Catch ex As Exception
						End Try
					End If
				Next

				files = IO.Directory.GetFiles(FilePath + "\", "vulkaninfo*.*")
				For i As Integer = 0 To files.Length - 1
					If Not IsNullOrWhitespace(files(i)) Then
						Try
							Delete(files(i))
						Catch ex As Exception
						End Try
					End If
				Next
			End If

			If IntPtr.Size = 8 Then
				If config.RemoveVulkan Then
					FilePath = Environment.GetEnvironmentVariable("windir") + "\SysWOW64"
					files = IO.Directory.GetFiles(FilePath + "\", "vulkan-1*.dll")
					For i As Integer = 0 To files.Length - 1
						If Not IsNullOrWhitespace(files(i)) Then
							Try
								Delete(files(i))
							Catch ex As Exception
							End Try
						End If
					Next

					files = IO.Directory.GetFiles(FilePath + "\", "vulkaninfo*.*")
					For i As Integer = 0 To files.Length - 1
						If Not IsNullOrWhitespace(files(i)) Then
							Try
								Delete(files(i))
							Catch ex As Exception
							End Try
						End If
					Next
				End If
			End If
		End If
	End Sub
	Private Sub cleannvidiaserviceprocess(ByVal config As ThreadSettings)
		CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\services.cfg"))
		If config.RemoveGFE Then
			CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\gfeservice.cfg"))
		End If

		'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
		'holding files in the NVIDIA folders sometimes.
		Try
			KillProcess(
			 "Lcore",
			 "nvgamemonitor",
			 "nvstreamsvc",
			 "NvTmru",
			 "nvxdsync",
			 "dwm",
			 "WWAHost",
			 "nvspcaps64",
			 "nvspcaps",
			 "NVIDIA Web Helper",
			 "NvBackend")

			If config.RemoveGFE Then
				KillProcess("nvtray")
			End If

		Catch ex As Exception
		End Try
	End Sub

	Private Sub cleannvidiafolders(ByVal config As ThreadSettings)
		Dim filePath As String = Nothing
		Dim removephysx As Boolean = config.RemovePhysX
		'Delete NVIDIA data Folders
		'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/

		UpdateTextMethod(UpdateTextTranslated(3))
		Application.Log.AddMessage("Cleaning UpdatusUser users ac if present")

		Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName.ToString())
		Dim users As DirectoryEntries = AD.Children
		Dim newuser As DirectoryEntry = Nothing

		Try
			newuser = users.Find("UpdatusUser")
			users.Remove(newuser)
		Catch ex As Exception
		End Try

		UpdateTextMethod(UpdateTextTranslated(4))

		Application.Log.AddMessage("Cleaning Directory")


		If config.RemoveNvidiaDirs = True Then
			filePath = sysdrv + "\NVIDIA"

			Delete(filePath)


		End If

		' here I erase the folders / files of the nvidia GFE / update in users.
		filePath = IO.Path.GetDirectoryName(config.Paths.UserPath)
		For Each child As String In Directory.GetDirectories(filePath)
			If IsNullOrWhitespace(child) = False Then
				If child.ToLower.Contains("updatususer") Then

					Delete(child)

					Delete(child)


					'Yes we do it 2 times. This will workaround a problem on junction/sybolic/hard link
					'(Will have to see if this is this valid. This was on old driver pre 300.xx I beleive :/ )
					Delete(child)

					Delete(child)

				End If
			End If
		Next

		filePath = IO.Path.GetDirectoryName(config.Paths.UserPath) + "\Public\Pictures\NVIDIA Corporation"
		If FileIO.ExistsDir(filePath) Then
			If filePath IsNot Nothing Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If StrContainsAny(child, True, "3d vision experience") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			End If
		End If

		For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(config.Paths.UserPath))

			filePath = filepaths + "\AppData\Local\NVIDIA"


			Try
				For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If (child.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE) Or
						 (child.ToLower.Contains("nvosc.") AndAlso config.RemoveGFE) Or
						 (child.ToLower.Contains("shareconnect") AndAlso config.RemoveGFE) Or
						 (child.ToLower.Contains("nvgs") AndAlso config.RemoveGFE) Or
						 (child.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE) Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			Catch ex As Exception
			End Try


			filePath = filepaths + "\AppData\Roaming\NVIDIA"

			Try
				For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("computecache") Or
						 child.ToLower.Contains("glcache") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			Catch ex As Exception
			End Try


			filePath = filepaths + "\AppData\Local\NVIDIA Corporation"
			If config.RemoveGFE Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If (child.ToLower.Contains("ledvisualizer") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvnode") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvtmmon") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvprofileupdater") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE) Or
							 (child.ToLower.EndsWith("\osc") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvvad") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvidia share") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE) Or
							 (child.ToLower.Contains("shield apps") AndAlso config.RemoveGFE) Then


								Delete(child)

							End If
						End If
					Next
					Try
						If Directory.GetDirectories(filePath).Length = 0 Then

							Delete(filePath)

						Else
							For Each data As String In Directory.GetDirectories(filePath)
								Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
							Next

						End If
					Catch ex As Exception
					End Try
				Catch ex As Exception
				End Try
			End If

		Next

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"

		Try
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("updatus") Or _
					 (child.ToLower.Contains("grid") AndAlso config.RemoveGFE) Then

						Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					Delete(filePath)

				Else
					For Each data As String In Directory.GetDirectories(filePath)
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
					Next
				End If
			Catch ex As Exception
			End Try
		Catch ex As Exception
		End Try

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"
		Try
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("drs") Or
					 (child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("netservice") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("crashdumps") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("nvstream") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("downloader") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("gfebridges") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("ledvisualizer") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("nview") AndAlso config.RemoveGFE) Or
					 (child.ToLower.Contains("nvstreamsvc") AndAlso config.RemoveGFE) Then

						Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		Catch ex As Exception
		End Try

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation"
		Try
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("3d vision") Then

						Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					Delete(filePath)

				Else
					For Each data As String In Directory.GetDirectories(filePath)
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
					Next
				End If
			Catch ex As Exception
			End Try
		Catch ex As Exception
		End Try


		filePath = Environment.GetFolderPath _
		(Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
		If FileIO.ExistsDir(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("control panel client") Or
					   child.ToLower.Contains("display") Or
					   child.ToLower.Contains("coprocmanager") Or
					   child.ToLower.Contains("drs") Or
					   child.ToLower.Contains("nvsmi") Or
					   child.ToLower.Contains("opencl") Or
					   child.ToLower.Contains("ansel") Or
					   child.ToLower.Contains("3d vision") Or
					   child.ToLower.Contains("led visualizer") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("netservice") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nvstreamc") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
					   child.ToLower.EndsWith("\physx") AndAlso config.RemovePhysX Or
					   child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("update common") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("shield") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nview") Or
					   child.ToLower.Contains("nvidia wmi provider") Or
					   child.ToLower.Contains("gamemonitor") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nvcontainer") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE Or
					   child.ToLower.Contains("nvgsync") Or
					   child.ToLower.Contains("update core") AndAlso config.RemoveGFE Then

						Delete(child)

					End If
					If child.ToLower.Contains("installer2") Then
						For Each child2 As String In Directory.GetDirectories(child)
							If IsNullOrWhitespace(child2) = False Then
								If child2.ToLower.Contains("display.3dvision") Or
								   child2.ToLower.Contains("display.controlpanel") Or
								   child2.ToLower.Contains("display.driver") Or
								   child2.ToLower.Contains("display.optimus") Or
								   child2.ToLower.Contains("msvcruntime") Or
								   child2.ToLower.Contains("ansel.") Or
								   child2.ToLower.Contains("display.gfexperience") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("osc.") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("osclib.") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("display.nvirusb") Or
								   child2.ToLower.Contains("display.physx") AndAlso config.RemovePhysX Or
								   child2.ToLower.Contains("display.update") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("display.gamemonitor") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvidia.update") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("installer2\installer") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("network.service") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("miracast.virtualaudio") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("update.core") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("virtualaudio.driver") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("coretemp") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("shield") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvcontainer") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvnodejs") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvplugin") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("hdaudio.driver") Then


									Delete(child2)

								End If
							End If
						Next

						If Directory.GetDirectories(child).Length = 0 Then

							Delete(child)

						Else
							For Each data As String In Directory.GetDirectories(child)
								Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
							Next

						End If
					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next
			End If
		End If


		If config.RemoveVulkan Then
			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + "\AGEIA Technologies"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If
		End If

		If config.RemoveVulkan Then
			filePath = config.Paths.ProgramFiles + "VulkanRT"
			If FileIO.ExistsDir(filePath) Then

				Delete(filePath)

			End If
		End If

		If IntPtr.Size = 8 Then
			filePath = Environment.GetFolderPath _
			  (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
			If FileIO.ExistsDir(filePath) Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("3d vision") Or
						 child.ToLower.Contains("coprocmanager") Or
						 child.ToLower.Contains("led visualizer") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("osc") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("netservice") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvidia geforce experience") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvstreamc") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("update common") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvcontainer") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvnode") AndAlso config.RemoveGFE Or
						 child.ToLower.Contains("nvgsync") Or
						 child.ToLower.EndsWith("\physx") AndAlso config.RemovePhysX Or
						 child.ToLower.Contains("update core") AndAlso config.RemoveGFE Then
							If removephysx Then

								Delete(child)

							Else
								If child.ToLower.Contains("physx") Then
									'do nothing
								Else

									Delete(child)

								End If
							End If
						End If
					End If
				Next

				If Directory.GetDirectories(filePath).Length = 0 Then

					Delete(filePath)

				Else
					For Each data As String In Directory.GetDirectories(filePath)
						Application.Log.AddMessage("Remaining folders found " + " : " + data)
					Next

				End If
			End If
		End If


		If config.RemovePhysX Then
			If IntPtr.Size = 8 Then
				filePath = Environment.GetFolderPath _
				 (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AGEIA Technologies"
				If FileIO.ExistsDir(filePath) Then

					Delete(filePath)

				End If
			End If
		End If

		If config.RemoveVulkan Then
			If IntPtr.Size = 8 Then
				filePath = Application.Paths.ProgramFilesx86 + "VulkanRT"
				If FileIO.ExistsDir(filePath) Then

					Delete(filePath)

				End If
			End If
		End If

		CleanupEngine.folderscleanup(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.
		If config.RemoveGFE Then
			CleanupEngine.folderscleanup(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\gfedriverfiles.cfg"))	'// add each line as String Array.
		End If

		filePath = System.Environment.SystemDirectory
		Dim files() As String = IO.Directory.GetFiles(filePath + "\", "nvdisp*.*")
		For i As Integer = 0 To files.Length - 1
			If Not IsNullOrWhitespace(files(i)) Then

				Delete(files(i))

			End If
		Next

		filePath = System.Environment.SystemDirectory
		files = IO.Directory.GetFiles(filePath + "\", "nvhdagenco*.*")
		For i As Integer = 0 To files.Length - 1
			If Not IsNullOrWhitespace(files(i)) Then

				Delete(files(i))

			End If
		Next

		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			Delete(filePath + "\Help\nvcpl")
		Catch ex As Exception
		End Try

		Try
			filePath = Environment.GetEnvironmentVariable("windir") + "\Temp\NVIDIA Corporation"
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("nv_cache") Then

						Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					Delete(filePath)

				Else
					For Each data As String In Directory.GetDirectories(filePath)
						Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
					Next

				End If
			Catch ex As Exception
			End Try
		Catch ex As Exception
		End Try



		For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(config.Paths.UserPath))

			filePath = filepaths + "\AppData\Local\Temp\NVIDIA Corporation"

			Try
				For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("nv_cache") Or
						 child.ToLower.Contains("displaydriver") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			Catch ex As Exception
			End Try

			filePath = filepaths + "\AppData\Local\Temp\NVIDIA"

			Try
				For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If (child.ToLower.Contains("geforceexperienceselfupdate") AndAlso config.RemoveGFE) Or
						  (child.ToLower.Contains("gfe") AndAlso config.RemoveGFE) Or
						   child.ToLower.Contains("displaydriver") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			Catch ex As Exception
			End Try

			filePath = filepaths + "\AppData\Local\Temp\Low\NVIDIA Corporation"

			Try
				For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("nv_cache") Then

							Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						Delete(filePath)

					Else
						For Each data As String In Directory.GetDirectories(filePath)
							Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
						Next

					End If
				Catch ex As Exception
				End Try
			Catch ex As Exception
			End Try

			'windows 8+ only (store apps nv_cache cleanup)

			Try
				If win8higher Then
					Dim prefilePath As String = filepaths + "\AppData\Local\Packages"
					For Each childs As String In My.Computer.FileSystem.GetDirectories(prefilePath)
						If Not IsNullOrWhitespace(childs) Then
							filePath = childs + "\AC\Temp\NVIDIA Corporation"

							If FileIO.ExistsDir(filePath) Then
								For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
									If IsNullOrWhitespace(child) = False Then
										If child.ToLower.Contains("nv_cache") Then

											Delete(child)

										End If
									End If
								Next

								If Directory.GetDirectories(filePath).Length = 0 Then

									Delete(filePath)

								Else
									For Each data As String In Directory.GetDirectories(filePath)
										Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
									Next

								End If
							End If
						End If
					Next
				End If
			Catch ex As Exception
			End Try

		Next

		'Cleaning the GFE 2.0.1 and earlier assemblies.
		If config.RemoveGFE Then
			filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_32"
			If FileIO.ExistsDir(filePath) Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("gfexperience") Or
						 child.ToLower.Contains("nvidia.sett") Or
						 child.ToLower.Contains("nvidia.updateservice") Or
						 child.ToLower.Contains("nvidia.win32api") Or
						 child.ToLower.Contains("installeruiextension") Or
						 child.ToLower.Contains("installerservice") Or
						 child.ToLower.Contains("gridservice") Or
						 child.ToLower.Contains("shadowplay") Or
						   child.ToLower.Contains("nvidia.gfe") Then

							Delete(child)

						End If
					End If
				Next
			End If
		End If

		'-----------------
		'MUI cache cleanUP
		'-----------------
		'Note: this MUST be done after cleaning the folders.
		Application.Log.AddMessage("MuiCache CleanUP")
		Try
			For Each regusers As String In Registry.Users.GetSubKeyNames
				If IsNullOrWhitespace(regusers) Then Continue For

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\software\classes\local settings\muicache", False)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
								If subregkey IsNot Nothing Then
									For Each childs As String In subregkey.GetSubKeyNames()
										If IsNullOrWhitespace(childs) Then Continue For

										Using regkey2 As RegistryKey = myregistry.opensubkey(subregkey, childs, True)
											For Each Keyname As String In regkey2.GetValueNames
												If IsNullOrWhitespace(Keyname) Then Continue For

												If StrContainsAny(Keyname, True, "nvstlink.exe", "nvstview.exe", "nvcpluir.dll", "nvcplui.exe") Or
												 (StrContainsAny(Keyname, True, "gfexperience.exe") AndAlso config.RemoveGFE) Then
													Try
														deletevalue(regkey2, Keyname)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											Next
										End Using
									Next
								End If
							End Using
						Next
					End If
				End Using

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames()
							If IsNullOrWhitespace(child) Then Continue For

							If StrContainsAny(child, True, "nvcplui.exe", "nvtray.exe") Or
							 (StrContainsAny(child, True, "nvbackend.exe") AndAlso config.RemoveGFE) Or
							 (StrContainsAny(child, True, "GeForce Experience\Update\setup.exe") AndAlso config.RemoveGFE) Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						Next
					End If
				End Using

			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			For Each regusers As String In Registry.Users.GetSubKeyNames
				If IsNullOrWhitespace(regusers) Then Continue For

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\software\classes\local settings\software\microsoft\windows\shell\muicache", True)
					If regkey IsNot Nothing Then
						For Each Keyname As String In regkey.GetValueNames
							If IsNullOrWhitespace(Keyname) Then Continue For

							If StrContainsAny(Keyname, True, "nvcplui.exe", "nvstlink.exe", "nvstview.exe", "nvcpluir.dll") Or
							   (StrContainsAny(Keyname, True, "gfexperience.exe") AndAlso config.RemoveGFE) Then
								Try
									deletevalue(regkey, Keyname)
								Catch ex As Exception
									Application.Log.AddException(ex)
								End Try
							End If
						Next
					End If
				End Using
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

	End Sub

	Private Sub cleannvidia(ByVal config As ThreadSettings)

		Dim wantedvalue As String = Nothing
		Dim wantedvalue2 As String = Nothing
		Dim removegfe As Boolean = config.RemoveGFE
		Dim removephysx As Boolean = config.RemovePhysX

		'-----------------
		'Registry Cleaning
		'-----------------
		UpdateTextMethod(UpdateTextTranslated(5))
		Application.Log.AddMessage("Starting reg cleanUP... May take a minute or two.")


		'Deleting DCOM object /classroot
		Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")

		CleanupEngine.ClassRoot(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\classroot.cfg")) '// add each line as String Array.

		'for GFE removal only
		If removegfe Then
			CleanupEngine.clsidleftover(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\clsidleftoverGFE.cfg")) '// add each line as String Array.
		Else
			CleanupEngine.clsidleftover(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\clsidleftover.cfg")) '// add each line as String Array.
		End If
		'------------------------------
		'Clean the rebootneeded message
		'------------------------------
		Try

			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If Not IsNullOrWhitespace(child) Then
							If child.ToLower.Contains("nvidia_rebootneeded") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
									Application.Log.AddException(ex)
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'-----------------
		'interface cleanup
		'-----------------



		If removegfe Then 'When removing GFE only
			CleanupEngine.interfaces(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\interfaceGFE.cfg")) '// add each line as String Array.
		Else
			CleanupEngine.interfaces(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\interface.cfg")) '// add each line as String Array.
		End If

		Application.Log.AddMessage("Finished dcom/clsid/appid/typelib/interface cleanup")

		'end of deleting dcom stuff
		Application.Log.AddMessage("Pnplockdownfiles region cleanUP")

		CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.

		If removegfe Then
			CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\gfedriverfiles.cfg")) '// add each line as String Array.
		End If
		'Cleaning PNPRessources.  'Will fix this later, its not efficent clean at all. (Wagnard)
		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\Global", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\global")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False)
			If regkey IsNot Nothing Then
				If MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False).SubKeyCount = 0 Then
					Try
						deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation")
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		If IntPtr.Size = 8 Then
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos", False)
				If regkey IsNot Nothing Then
					Try
						deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			End Using
		End If



		If removegfe Then
			'----------------------
			'Firewall entry cleanup
			'----------------------
			Application.Log.AddMessage("Firewall entry cleanUP")
			Try
				If winxp = False Then
					Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
						If subregkey IsNot Nothing Then
							For Each child2 As String In subregkey.GetSubKeyNames()
								If child2.ToLower.Contains("controlset") Then
									Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\" & child2 & "\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules", True)
										If regkey IsNot Nothing Then
											For Each child As String In regkey.GetValueNames()
												If IsNullOrWhitespace(child) Then Continue For

												wantedvalue = regkey.GetValue(child, String.Empty).ToString()
												If IsNullOrWhitespace(wantedvalue) Then Continue For

												If StrContainsAny(wantedvalue, True, "nvstreamsrv", "nvidia network service", "nvidia update core", "NvContainer") Then
													Try
														deletevalue(regkey, child)
													Catch ex As Exception
													End Try
												End If
											Next
										End If
									End Using
								End If
							Next
						End If
					End Using
				End If
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
		'--------------------------
		'End Firewall entry cleanup
		'--------------------------
		Application.Log.AddMessage("End Firewall CleanUP")
		'--------------------------
		'Power Settings CleanUP
		'--------------------------
		Application.Log.AddMessage("Power Settings Cleanup")
		Try
			If winxp = False Then
				Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
					If subregkey IsNot Nothing Then
						For Each child2 As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(child2) Then Continue For

							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
									If regkey IsNot Nothing Then
										For Each childs As String In regkey.GetSubKeyNames()
											If IsNullOrWhitespace(childs) Then Continue For

											Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, childs)
												For Each child As String In regkey2.GetValueNames()
													If IsNullOrWhitespace(child) Then Continue For

													If StrContainsAny(child, True, "description") Then
														wantedvalue = regkey2.GetValue(child, String.Empty).ToString()
														If IsNullOrWhitespace(wantedvalue) Then Continue For

														If StrContainsAny(wantedvalue, True, "nvsvc") Then
															Try
																deletesubregkey(regkey, childs)
																Continue For
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
														If StrContainsAny(wantedvalue, True, "video and display power management") Then
															Using subregkey2 As RegistryKey = myregistry.opensubkey(regkey, childs, True)
																If subregkey2 IsNot Nothing Then
																	For Each childinsubregkey2 As String In subregkey2.GetSubKeyNames()
																		If IsNullOrWhitespace(childinsubregkey2) Then Continue For
																		Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(subregkey2, childinsubregkey2)
																			If regkey3 IsNot Nothing Then
																				For Each childinsubregkey2value As String In regkey3.GetValueNames()
																					If IsNullOrWhitespace(childinsubregkey2value) Then Continue For

																					If childinsubregkey2value.ToString.ToLower.Contains("description") Then
																						wantedvalue2 = regkey3.GetValue(childinsubregkey2value, String.Empty).ToString
																						If IsNullOrWhitespace(wantedvalue2) Then Continue For

																						If wantedvalue2.ToString.ToLower.Contains("nvsvc") Then
																							Try
																								deletesubregkey(subregkey2, childinsubregkey2)
																							Catch ex As Exception
																							End Try
																						End If
																					End If
																				Next
																			End If
																		End Using
																	Next
																End If
															End Using
														End If
													End If
												Next
											End Using
										Next
									End If
								End Using
							End If
						Next
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'--------------------------
		'End Power Settings CleanUP
		'--------------------------
		Application.Log.AddMessage("End Power Settings Cleanup")

		'--------------------------------
		'System environement path cleanup
		'--------------------------------


		If removephysx Then
			Application.Log.AddMessage("System environement CleanUP")
			Try
				Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
					If subregkey IsNot Nothing Then
						For Each child2 As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(child2) Then Continue For

							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
									If regkey IsNot Nothing Then
										For Each child As String In regkey.GetValueNames()
											If IsNullOrWhitespace(child) Then Continue For

											If StrContainsAny(child, True, "Path") Then
												wantedvalue = regkey.GetValue(child, String.Empty).ToString.ToLower
												If IsNullOrWhitespace(wantedvalue) Then Continue For

												Try
													Select Case True
														Case StrContainsAny(wantedvalue, True, sysdrv & "\program files (x86)\nvidia corporation\physx\common;")
															wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\nvidia corporation\physx\common;", "")
															Try
																regkey.SetValue(child, wantedvalue)
															Catch ex As Exception
															End Try
														Case StrContainsAny(wantedvalue, True, ";" + sysdrv & "\program files (x86)\nvidia corporation\physx\common")
															wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\nvidia corporation\physx\common", "")
															Try
																regkey.SetValue(child, wantedvalue)
															Catch ex As Exception
															End Try
													End Select
												Catch ex As Exception
												End Try
											End If
										Next
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
		'-------------------------------------
		'end system environement patch cleanup
		'-------------------------------------
		Application.Log.AddMessage("End System environement path cleanup")

		Try
			sysdrv = sysdrv.ToUpper
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			  "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
				If regkey IsNot Nothing Then
					wantedvalue = regkey.GetValue("AppInit_DLLs", String.Empty).ToString   'Will need to consider the comma in the future for multiple value
					If IsNullOrWhitespace(wantedvalue) = False Then
						Select Case True
							Case wantedvalue.Contains(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & sysdrv & "\PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll")
								wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & sysdrv & "\PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
								regkey.SetValue("AppInit_DLLs", wantedvalue)

							Case wantedvalue.Contains(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL")
								wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL", "")
								regkey.SetValue("AppInit_DLLs", wantedvalue)

							Case wantedvalue.Contains(sysdrv & "\PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll")
								wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
								regkey.SetValue("AppInit_DLLs", wantedvalue)
						End Select
					End If
				End If
				If CStr(regkey.GetValue("AppInit_DLLs")) = "" Then
					Try
						regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
					Catch ex As Exception
					End Try
				End If
			End Using
			sysdrv = sysdrv.ToLower
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			If IntPtr.Size = 8 Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				   "SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)

					If regkey IsNot Nothing Then
						wantedvalue = regkey.GetValue("AppInit_DLLs", String.Empty).ToString
						If IsNullOrWhitespace(wantedvalue) = False Then
							Select Case True
								Case wantedvalue.Contains(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & sysdrv & "\PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll")
									wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & sysdrv & "\PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
									regkey.SetValue("AppInit_DLLs", wantedvalue)

								Case wantedvalue.Contains(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL")
									wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL", "")
									regkey.SetValue("AppInit_DLLs", wantedvalue)

								Case wantedvalue.Contains(sysdrv & "\PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll")
									wantedvalue = wantedvalue.Replace(sysdrv & "\PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
									regkey.SetValue("AppInit_DLLs", wantedvalue)
							End Select
						End If
					End If
					If CStr(regkey.GetValue("AppInit_DLLs")) = "" Then
						Try
							regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
						Catch ex As Exception
						End Try
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'remove opencl registry Khronos
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Khronos\OpenCL\Vendors", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) Then Continue For

						If child.ToLower.Contains("nvopencl") Then
							Try
								deletevalue(regkey, child)
							Catch ex As Exception
							End Try
						End If
					Next
					If regkey.GetValueNames().Length = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\Khronos\OpenCL")
						Catch ex As Exception
						End Try
					End If
					CleanVulkan(config)
					Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Khronos", True)
						If subregkey IsNot Nothing Then
							If subregkey.GetSubKeyNames().Length = 0 Then
								Try
									deletesubregkey(Registry.LocalMachine, "Software\Khronos")
								Catch ex As Exception
								End Try
							End If
						End If
					End Using
				End If
			End Using
		Catch ex As Exception
		End Try

		If IntPtr.Size = 8 Then
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) Then Continue For

						If child.ToLower.Contains("nvopencl") Then
							Try
								deletevalue(regkey, child)
							Catch ex As Exception
							End Try
						End If
					Next
					If regkey.GetValueNames().Length = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\Khronos")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		End If


		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) Then Continue For

								If StrContainsAny(child, True, "nvidia corporation") Then
									Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
										For Each child2 As String In regkey2.GetSubKeyNames()
											If IsNullOrWhitespace(child2) Then Continue For

											If StrContainsAny(child2, True, "global") Then
												If removegfe Then
													Try
														deletesubregkey(regkey2, child2)
													Catch ex As Exception
													End Try
												Else
													Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child + "\" + child2, True)
														For Each child3 As String In regkey3.GetSubKeyNames()
															If IsNullOrWhitespace(child3) Then Continue For
															If StrContainsAny(child3, True, "gfeclient", "gfexperience", "shadowplay", "ledvisualizer") Then
																'do nothing
															Else
																Try
																	deletesubregkey(regkey3, child3)
																Catch ex As Exception
																End Try
															End If
														Next
													End Using
												End If
											End If
											If child2.ToLower.Contains("logging") Or
											 child2.ToLower.Contains("nvbackend") AndAlso removegfe Or
											 child2.ToLower.Contains("nvidia update core") AndAlso removegfe Or
											 child2.ToLower.Contains("nvcontrolpanel2") Or
											 child2.ToLower.Contains("nvcontrolpanel") Or
											 child2.ToLower.Contains("nvcamera") Or
											 child2.ToLower.Contains("nvtray") AndAlso removegfe Or
											 child2.ToLower.Contains("nvcontainer") AndAlso removegfe Or
											 child2.ToLower.Contains("nvstream") AndAlso removegfe Or
											 child2.ToLower.Contains("nvidia control panel") Then
												Try
													deletesubregkey(regkey2, child2)
												Catch ex As Exception
												End Try
											End If
										Next
										If regkey2.SubKeyCount = 0 Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End Using
								End If
							Next
						End If
					End Using

					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetValueNames()
								If IsNullOrWhitespace(child) Then Continue For

								Dim tArray() As String = CType(regkey.GetValue(child), String())
								For Each arrayelement As String In tArray
									If IsNullOrWhitespace(arrayelement) Then Continue For

									If Not arrayelement = "" Then
										If StrContainsAny(arrayelement, True, "nvstview.exe", "vulkaninfo", "nvstlink.exe") Then
											Try
												deletevalue(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End If
								Next
							Next
						End If
					End Using



				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\ARP", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetValueNames()
					If IsNullOrWhitespace(child) Then Continue For

					Dim tArray() As String = CType(regkey.GetValue(child), String())
					For Each arrayelement As String In tArray
						If IsNullOrWhitespace(arrayelement) Then Continue For

						If Not arrayelement = "" Then
							If StrContainsAny(arrayelement, True, "nvi2.dll", "vulkaninfo", "nvstlink.exe") Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				Next
			End If
		End Using

		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, ".DEFAULT\Software", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) Then Continue For

					If child.ToLower.Contains("nvidia corporation") Then
						Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
							For Each child2 As String In regkey2.GetSubKeyNames()
								If IsNullOrWhitespace(child2) Then Continue For

								If StrContainsAny(child2, True, "global", "nvbackend", "nvcontrolpanel2", "nvidia control panel") Or
								  (StrContainsAny(child2, True, "nvidia update core") AndAlso removegfe) Then

									Try
										deletesubregkey(regkey2, child2)
									Catch ex As Exception
									End Try
								End If
							Next

							If regkey2.SubKeyCount = 0 Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End Using
					End If
				Next
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) Then Continue For

					If StrContainsAny(child, True, "ageia technologies") AndAlso removephysx Then

						Try
							deletesubregkey(regkey, child)
						Catch ex As Exception
						End Try

					End If
					If child.ToLower.Contains("nvidia corporation") Then
						Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child, True)
							For Each child2 As String In regkey2.GetSubKeyNames()
								If IsNullOrWhitespace(child2) Then Continue For

								If StrContainsAny(child2, True, "global") Then
									If removegfe Then
										Try
											deletesubregkey(regkey2, child2)
										Catch ex As Exception
										End Try
									Else
										Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2, True)
											For Each child3 As String In regkey3.GetSubKeyNames()
												If IsNullOrWhitespace(child3) Then Continue For

												If StrContainsAny(child3, True, "gfeclient", "gfexperience", "nvbackend", "nvscaps", "shadowplay", "ledvisualizer") Then
													'do nothing
												Else
													Try
														deletesubregkey(regkey3, child3)
													Catch ex As Exception
													End Try
												End If
											Next
										End Using
									End If
								End If
								If StrContainsAny(child2, True, "installer", "logging", "nvidia update core", "nvcontrolpanel", "nvcontrolpanel2", "physx_systemsoftware", "physxupdateloader", "uxd") Or
								(StrContainsAny(child2, True, "installer2", "nvstream", "nvtray", "nvcontainer") AndAlso removegfe) Then
									If removephysx Then
										Try
											deletesubregkey(regkey2, child2)
										Catch ex As Exception
										End Try
									Else
										If child2.ToLower.Contains("physx") Then
											'do nothing
										Else
											Try
												deletesubregkey(regkey2, child2)
											Catch ex As Exception
											End Try
										End If
									End If
								End If
							Next
							If regkey2.SubKeyCount = 0 Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End Using
					End If
				Next
			End If
		End Using


		If IntPtr.Size = 8 Then
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, "ageia technologies") Then
							If removephysx Then
								deletesubregkey(regkey, child)
							End If
						End If
						If StrContainsAny(child, True, "nvidia corporation") Then
							Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
								For Each child2 As String In regkey2.GetSubKeyNames()
									If IsNullOrWhitespace(child2) Then Continue For

									If StrContainsAny(child2, True, "global") Then
										If removegfe Then
											Try
												deletesubregkey(regkey2, child2)
											Catch ex As Exception
											End Try
										Else
											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2, True)
												For Each child3 As String In regkey3.GetSubKeyNames()
													If IsNullOrWhitespace(child3) Then Continue For

													If StrContainsAny(child3, True, "gfeclient", "gfexperience", "nvbackend", "nvscaps", "shadowplay", "ledvisualizer") Then
														'do nothing
													Else
														Try
															deletesubregkey(regkey3, child3)
														Catch ex As Exception
														End Try
													End If
												Next
											End Using
										End If
									End If
									If StrContainsAny(child2, True, "logging", "physx_systemsoftware", "physxupdateloader", "installer2", "physx") Then
										If removephysx Then
											Try
												deletesubregkey(regkey2, child2)
											Catch ex As Exception
											End Try
										Else
											If child2.ToLower.Contains("physx") Then
												'do nothing
											Else
												Try
													deletesubregkey(regkey2, child2)
												Catch ex As Exception
												End Try
											End If
										End If
									End If
									If StrContainsAny(child2, True, "nvcontainer") AndAlso config.RemoveGFE Then
										Try
											deletesubregkey(regkey2, child2)
										Catch ex As Exception
										End Try
									End If
								Next
								If regkey2.SubKeyCount = 0 Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End Using
						End If
					Next
				End If
			End Using
		End If



		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Try
								Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
									If removephysx Then
										If IsNullOrWhitespace(CStr(regkey2.GetValue("DisplayName"))) = False Then
											If regkey2.GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
												deletesubregkey(regkey, child)
												Continue For
											End If
										End If
									End If
								End Using
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
							If child.ToLower.Contains("display.3dvision") Or
							 child.ToLower.Contains("3dtv") Or
							 child.ToLower.Contains("_display.controlpanel") Or
							 child.ToLower.Contains("_display.driver") Or
							 child.ToLower.Contains("_display.gfexperience") AndAlso removegfe Or
							 child.ToLower.Contains("_display.nvirusb") Or
							 child.ToLower.Contains("_display.physx") AndAlso removephysx Or
							 child.ToLower.Contains("_display.update") AndAlso removegfe Or
							 child.ToLower.Contains("_display.gamemonitor") AndAlso removegfe Or
							 child.ToLower.Contains("_gfexperience") AndAlso removegfe Or
							 child.ToLower.Contains("_hdaudio.driver") Or
							 child.ToLower.Contains("_installer") AndAlso removegfe Or
							 child.ToLower.Contains("_network.service") AndAlso removegfe Or
							 child.ToLower.Contains("_shadowplay") AndAlso removegfe Or
							 child.ToLower.Contains("_update.core") AndAlso removegfe Or
							 child.ToLower.Contains("nvidiastereo") Or
							 child.ToLower.Contains("_shieldwireless") AndAlso removegfe Or
							 child.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
							 child.ToLower.Contains("_virtualaudio.driver") AndAlso removegfe Then
								If removephysx = False And child.ToLower.Contains("physx") Then
									Continue For
								End If
								If config.Remove3DTVPlay = False And child.ToLower.Contains("3dtv") Then
									Continue For
								End If
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If


		Try

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			 "Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
							Try
								If removephysx Then
									If IsNullOrWhitespace(regkey2.GetValue("DisplayName", String.Empty).ToString) = False Then
										If StrContainsAny(regkey2.GetValue("DisplayName", String.Empty).ToString, True, "physx") Then
											deletesubregkey(regkey, child)
											Continue For
										End If
									End If
								End If
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
						End Using
						If child.ToLower.Contains("display.3dvision") Or
						 child.ToLower.Contains("3dtv") Or
						 child.ToLower.Contains("_display.controlpanel") Or
						 child.ToLower.Contains("_display.driver") Or
						 child.ToLower.Contains("_display.optimus") Or
						 child.ToLower.Contains("_display.gfexperience") AndAlso removegfe Or
						 child.ToLower.Contains("_display.nvirusb") Or
						 child.ToLower.Contains("_display.physx") Or
						 child.ToLower.Contains("_display.update") AndAlso removegfe Or
						 child.ToLower.Contains("_osc") AndAlso removegfe Or
						 child.ToLower.Contains("_display.nview") Or
						 child.ToLower.Contains("_display.nvwmi") Or
						 child.ToLower.Contains("_display.gamemonitor") AndAlso removegfe Or
						 child.ToLower.Contains("_nvidia.update") AndAlso removegfe Or
						 child.ToLower.Contains("_gfexperience") AndAlso removegfe Or
						 child.ToLower.Contains("_hdaudio.driver") Or
						 child.ToLower.Contains("_installer") AndAlso removegfe Or
						 child.ToLower.Contains("_network.service") AndAlso removegfe Or
						 child.ToLower.Contains("_shadowplay") AndAlso removegfe Or
						 child.ToLower.Contains("_update.core") AndAlso removegfe Or
						 child.ToLower.Contains("nvidiastereo") Or
						 child.ToLower.Contains("_shieldwireless") AndAlso removegfe Or
						 child.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
						 child.ToLower.Contains("_virtualaudio.driver") AndAlso removegfe Or
						 child.ToLower.Contains("vulkanrt1.") AndAlso config.RemoveVulkan Then
							If removephysx = False And child.ToLower.Contains("physx") Then
								Continue For
							End If

							If config.Remove3DTVPlay = False And child.ToLower.Contains("3dtv") Then
								Continue For
							End If
							Try
								deletesubregkey(regkey, child)
							Catch ex As Exception
							End Try
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Using regkey = MyRegistry.OpenSubKey(Registry.CurrentUser,
		 "Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetValueNames()
					If Not IsNullOrWhitespace(child) Then
						If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
							deletevalue(regkey, child)
						End If
					End If
				Next
			End If
		End Using


		Using regkey = MyRegistry.OpenSubKey(Registry.CurrentUser,
		 "Software\Microsoft\.NETFramework\SQM\Apps", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If Not IsNullOrWhitespace(child) Then
						If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
							deletesubregkey(regkey, child)
						End If
					End If
				Next
			End If
		End Using

		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
					 users + "\Software\Microsoft\.NETFramework\SQM\Apps", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If Not IsNullOrWhitespace(child) Then
									If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
										deletesubregkey(regkey, child)
									End If
								End If
							Next
						End If
					End Using
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Try

			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
					 users + "\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetValueNames()
								If Not IsNullOrWhitespace(child) Then
									If StrContainsAny(child, True, "gfexperience.exe", "GeForce Experience.exe") AndAlso removegfe Then
										deletevalue(regkey, child)
									End If
								End If
							Next
						End If
					End Using
				End If
			Next

		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
		 "Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
						"Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
							If subregkey IsNot Nothing Then
								If IsNullOrWhitespace(CStr(subregkey.GetValue("ProfileImagePath"))) = False Then
									wantedvalue = subregkey.GetValue("ProfileImagePath").ToString
									If IsNullOrWhitespace(wantedvalue) = False Then
										If wantedvalue.Contains("UpdatusUser") Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End If
								End If
							End If
						End Using
					End If
				Next
			End If
		End Using

		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
		 "Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
						 "Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
							If subregkey IsNot Nothing Then
								If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
									wantedvalue = subregkey.GetValue("").ToString
									If IsNullOrWhitespace(wantedvalue) = False Then
										If wantedvalue.ToLower.Contains("nvidia control panel") Or
										   wantedvalue.ToLower.Contains("nvidia nview desktop manager") Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
											'special case only to nvidia afaik. there i a clsid for a control pannel that link from namespace.
											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
												Try
													deletesubregkey(regkey2, child)
												Catch ex As Exception
												End Try
											End Using
										End If
									End If
								End If
							End If
						End Using
					End If
				Next
			End If
		End Using

		'----------------------
		'.net ngenservice clean
		'----------------------
		Application.Log.AddMessage("ngenservice Clean")

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
							Try
								deletesubregkey(regkey, child)
							Catch ex As Exception
							End Try
						End If
					End If
				Next
			End If
		End Using
		If IntPtr.Size = 8 Then

			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		End If
		Application.Log.AddMessage("End ngenservice Clean")
		'-----------------------------
		'End of .net ngenservice clean
		'-----------------------------

		'-----------------------------
		'Mozilla plugins
		'-----------------------------
		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\MozillaPlugins", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("nvidia.com/3dvision") Then
							Try
								deletesubregkey(regkey, child)
							Catch ex As Exception
							End Try
						End If
					End If
				Next
			End If
		End Using


		If IntPtr.Size = 8 Then
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Wow6432Node\MozillaPlugins", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("nvidia.com/3dvision") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		End If


		'-----------------------
		'remove event view stuff
		'-----------------------
		Application.Log.AddMessage("Remove eventviewer stuff")

		Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
			If subregkey IsNot Nothing Then
				For Each child2 As String In subregkey.GetSubKeyNames()
					If IsNullOrWhitespace(child2) Then Continue For

					If child2.ToLower.Contains("controlset") Then
						Using regkey As RegistryKey = myregistry.opensubkey(subregkey, child2 & "\Services\eventlog\Application", True)
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames()
									If IsNullOrWhitespace(child) Then Continue For

									If child.ToLower.StartsWith("nvidia update") Or
									 (child.ToLower.StartsWith("nvstreamsvc") AndAlso removegfe) Or
									 child.ToLower.StartsWith("nvidia opengl driver") Or
									 child.ToLower.StartsWith("nvwmi") Or
									 child.ToLower.StartsWith("nview") Then
										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
								Next
							End If
						End Using
					End If
				Next
			End If
		End Using

		Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM", False)
			If subregkey IsNot Nothing Then
				For Each child2 As String In subregkey.GetSubKeyNames()
					If IsNullOrWhitespace(child2) Then Continue For

					If child2.ToLower.Contains("controlset") Then
						Using regkey As RegistryKey = myregistry.opensubkey(subregkey, child2 & "\Services\eventlog\System", True)
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames()
									If IsNullOrWhitespace(child) Then Continue For

									If child.ToLower.StartsWith("nvidia update") Or
									 child.ToLower.StartsWith("nvidia opengl driver") Or
									 child.ToLower.StartsWith("nvwmi") Or
									 child.ToLower.StartsWith("nview") Then
										deletesubregkey(regkey, child)
									End If
								Next
							End If
						End Using
					End If
				Next
			End If
		End Using

		Application.Log.AddMessage("End Remove eventviewer stuff")
		'---------------------------
		'end remove event view stuff
		'---------------------------

		'---------------------------
		'virtual store
		'---------------------------

		Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(regkey, "Global")
				Catch ex As Exception
				End Try
				If regkey.SubKeyCount = 0 Then
					Using regkey2 As RegistryKey = myregistry.opensubkey(registry.classesroot, "VirtualStore\MACHINE\SOFTWARE", True)
						Try
							deletesubregkey(regkey2, "NVIDIA Corporation")
						Catch ex As Exception
						End Try
					End Using
				End If
			End If
		End Using

		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
						If regkey IsNot Nothing Then
							Try
								deletesubregkey(regkey, "Global")
							Catch ex As Exception
							End Try
							If regkey.SubKeyCount = 0 Then
								Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
									Try
										deletesubregkey(regkey2, "NVIDIA Corporation")
									Catch ex As Exception
									End Try
								End Using
							End If
						End If
					End Using
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			For Each child As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(child) Then
					If child.ToLower.Contains("s-1-5") Then
						Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
							If regkey IsNot Nothing Then
								Try
									deletesubregkey(regkey, "Global")
									If regkey.SubKeyCount = 0 Then
										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
											Try
												deletesubregkey(regkey2, "NVIDIA Corporation")
											Catch ex As Exception
											End Try
										End Using
									End If
								Catch ex As Exception
								End Try
							End If
						End Using
					End If
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Microsoft\Windows\CurrentVersion\Run", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames
						If Not IsNullOrWhitespace(child) Then
							If StrContainsAny(child, True, "nvtmru", "NvCplDaemon", "NvMediaCenter", "NvBackend", "nwiz", "ShadowPlay", "StereoLinksInstall", "NvGameMonitor") Then
								deletevalue(regkey, child)
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			If IntPtr.Size = 8 Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
				 "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames
							If Not IsNullOrWhitespace(child) Then
								If StrContainsAny(child, True, "StereoLinksInstall") Then
									deletevalue(regkey, child)
								End If
							End If
						Next
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		CleanupEngine.installer(IO.File.ReadAllLines(baseDir & "\settings\NVIDIA\packages.cfg"), config)


		If config.Remove3DTVPlay Then
			Try
				deletesubregkey(Registry.ClassesRoot, "mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
			Try
				deletesubregkey(Registry.ClassesRoot, "WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
			Try
				deletesubregkey(Registry.ClassesRoot, "AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
		End If

		'-----------------------------
		'Shell extensions\aproved
		'-----------------------------
		Try
			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) = False Then
							If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Or
							   regkey.GetValue(child).ToString.ToLower.Contains("nview desktop context menu") Or
							   regkey.GetValue(child).ToString.ToLower.Contains("nvappshext extension") Or
							   regkey.GetValue(child).ToString.ToLower.Contains("openglshext extension") Or
							   regkey.GetValue(child).ToString.ToLower.Contains("nvidia play on my tv context menu extension") Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
		  "Display\shellex\PropertySheetHandlers", True)
			Try
				deletesubregkey(regkey, "NVIDIA CPL Extension")
			Catch ex As Exception
			End Try
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Extended Properties", False)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) Then Continue For

					Using regkey2 As RegistryKey = myregistry.opensubkey(regkey, child, True)
						For Each childs As String In regkey2.GetValueNames()
							If IsNullOrWhitespace(childs) Then Continue For

							If StrContainsAny(childs, True, "nvcpl.cpl") Then
								Try
									deletevalue(regkey2, childs)
								Catch ex As Exception
								End Try
							End If
						Next
					End Using
				Next
			End If
		End Using

		If IntPtr.Size = 8 Then

			Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(regkey.GetValue(child, String.Empty).ToString, False, "nvcpl desktopcontext class") Then
							Try
								deletevalue(regkey, child)
							Catch ex As Exception
							End Try
						End If
					Next
				End If
			End Using
		End If
		'-----------------------------
		'End Shell extensions\aprouved
		'-----------------------------

		'Shell ext
		Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "Directory\background\shellex\ContextMenuHandlers", True)
			Try
				deletesubregkey(regkey, "NvCplDesktopContext")
			Catch ex As Exception
			End Try

			Try
				deletesubregkey(regkey, "00nView")
			Catch ex As Exception
			End Try
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "Software\Classes\Directory\background\shellex\ContextMenuHandlers", True)
			Try
				deletesubregkey(regkey, "NvCplDesktopContext")
			Catch ex As Exception
			End Try

			Try
				deletesubregkey(regkey, "00nView")
			Catch ex As Exception
			End Try
		End Using

		'Cleaning of some "open with application" related to 3d vision
		Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "jpsfile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CType(regkey.GetValue(""), String))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(Registry.ClassesRoot, "jpsfile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "mpofile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CStr(regkey.GetValue("")))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(Registry.ClassesRoot, "mpofile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = myregistry.opensubkey(registry.classesroot, "pnsfile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CStr(regkey.GetValue("")))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(Registry.ClassesRoot, "pnsfile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Try
			deletesubregkey(Registry.ClassesRoot, ".tvp")  'CrazY_Milojko
		Catch ex As Exception
		End Try

		UpdateTextMethod("End of Registry Cleaning")

		Application.Log.AddMessage("End of Registry Cleaning")

	End Sub

	Private Sub cleanintelfolders()

		Dim filePath As String = Nothing

		UpdateTextMethod(UpdateTextTranslated(4))

		Application.Log.AddMessage("Cleaning Directory")

		CleanupEngine.folderscleanup(IO.File.ReadAllLines(baseDir & "\settings\INTEL\driverfiles.cfg"))	'// add each line as String Array.

		filePath = System.Environment.SystemDirectory
		Dim files() As String = IO.Directory.GetFiles(filePath + "\", "igfxcoin*.*")
		For i As Integer = 0 To files.Length - 1
			If Not IsNullOrWhitespace(files(i)) Then
				Try
					Delete(files(i))
				Catch ex As Exception
				End Try
			End If
		Next

	End Sub

	Private Sub cleanintelserviceprocess()

		CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(baseDir & "\settings\INTEL\services.cfg")) '// add each line as String Array.

		KillProcess("IGFXEM")
	End Sub

	Private Sub cleanintel(ByVal config As ThreadSettings)

		Dim wantedvalue As String = Nothing
		Dim packages As String()

		UpdateTextMethod(UpdateTextTranslated(5))

		Application.Log.AddMessage("Cleaning registry")

		CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(baseDir & "\settings\INTEL\driverfiles.cfg")) '// add each line as String Array.

		CleanupEngine.ClassRoot(IO.File.ReadAllLines(baseDir & "\settings\INTEL\classroot.cfg")) '// add each line as String Array.

		CleanupEngine.interfaces(IO.File.ReadAllLines(baseDir & "\settings\INTEL\interface.cfg")) '// add each line as String Array.

		CleanupEngine.clsidleftover(IO.File.ReadAllLines(baseDir & "\settings\INTEL\clsidleftover.cfg")) '// add each line as String Array.

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Intel", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("igfx") Or
							   child.ToLower.Contains("mediasdk") Or
							   child.ToLower.Contains("opencl") Or
							   child.ToLower.Contains("intel wireless display") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
					If regkey.SubKeyCount = 0 Then
						Try
							deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "Software", True), "Intel")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Intel", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) = False Then
									If child.ToLower.Contains("display") Then
										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try
									End If
								End If
							Next
							If regkey.SubKeyCount = 0 Then
								Try
									deletesubregkey(MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True), "Intel")
								Catch ex As Exception
								End Try
							End If
						End If
					End Using
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Intel", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								If child.ToLower.Contains("igfx") Or
								   child.ToLower.Contains("mediasdk") Or
								   child.ToLower.Contains("opencl") Or
								   child.ToLower.Contains("intel wireless display") Then
									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
							End If
						Next
						If regkey.SubKeyCount = 0 Then
							Try
								deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node", True), "Intel")
							Catch ex As Exception
							End Try
						End If
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If


		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Run", True)
				If regkey IsNot Nothing Then
					Try
						deletevalue(regkey, "IgfxTray")
					Catch ex As Exception
					End Try

					Try
						deletevalue(regkey, "Persistence")
					Catch ex As Exception
					End Try

					Try
						deletevalue(regkey, "HotKeysCmds")
					Catch ex As Exception
					End Try
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Directory\background\shellex\ContextMenuHandlers", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("igfxcui") Or
							   child.ToLower.Contains("igfxosp") Or
							 child.ToLower.Contains("igfxdtcm") Then

								deletesubregkey(regkey, child)

							End If
						End If

					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		CleanupEngine.installer(IO.File.ReadAllLines(baseDir & "\settings\INTEL\packages.cfg"), config)

		If IntPtr.Size = 8 Then
			packages = IO.File.ReadAllLines(baseDir & "\settings\INTEL\packages.cfg") '// add each line as String Array.
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then

								Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)

									If subregkey IsNot Nothing Then
										If IsNullOrWhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
											wantedvalue = subregkey.GetValue("DisplayName").ToString
											If IsNullOrWhitespace(wantedvalue) = False Then
												For i As Integer = 0 To packages.Length - 1
													If Not IsNullOrWhitespace(packages(i)) Then
														If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
															Try
																If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
																	deletesubregkey(regkey, child)
																End If
															Catch ex As Exception
															End Try
														End If
													End If
												Next
											End If
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cpls", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("igfxcpl") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		'Special Cleanup For Intel PnpResources
		Try
			If win8higher Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR", True)
					If regkey IsNot Nothing Then
						Dim classroot As String() = IO.File.ReadAllLines(baseDir & "\settings\INTEL\classroot.cfg")
						For Each child As String In regkey.GetSubKeyNames()
							If Not IsNullOrWhitespace(child) Then
								For i As Integer = 0 To classroot.Length - 1
									If Not IsNullOrWhitespace(classroot(i)) Then
										If child.ToLower.Contains(classroot(i).ToLower) Then
											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									End If
								Next
							End If
						Next
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If Not IsNullOrWhitespace(child) Then
							If child.ToLower.Contains("igfx") Then
								Try
									deletesubregkey(regkey, child)
								Catch ex As Exception
								End Try
							End If
						End If
					Next
					If regkey.SubKeyCount = 0 Then
						Try
							deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		UpdateTextMethod(UpdateTextTranslated(6))
	End Sub

	Private Sub checkpcieroot(ByVal config As ThreadSettings)	'This is for Nvidia Optimus to prevent the yellow mark on the PCI-E controler. We must remove the UpperFilters.
		Dim array() As String = Nothing

		UpdateTextMethod(UpdateTextTranslated(7))

		Application.Log.AddMessage("Starting the removal of nVidia Optimus UpperFilter if present.")

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\PCI")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, "ven_8086") Then
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
								If subregkey IsNot Nothing Then
									For Each childs As String In subregkey.GetSubKeyNames()
										If IsNullOrWhitespace(childs) Then Continue For

										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, childs, True)
											If regkey2 Is Nothing Then Continue For

											array = TryCast(regkey2.GetValue("UpperFilters"), String())

											If array IsNot Nothing AndAlso array.Length > 0 Then
												Application.Log.AddMessage("UpperFilters found", "UpperFilters", String.Join(Environment.NewLine, array))

												Dim fixUpperFilters As Boolean = False

												For Each u As String In array
													If StrContainsAny(u, True, "nvpciflt", "nvkflt") Then

														'If StrContainsAny(u, True, "nvpciflt", "nvkflt") Then
														' Orginal didn't check for "nvkflt", should it?
														' => "nvkflt" won't get removed if "nvpciflt" doesn't exists (uncomment top line)

														Application.Log.AddMessage("nVidia Optimus UpperFilter Found.")

														fixUpperFilters = True
														Exit For
													End If
												Next

												If fixUpperFilters Then
													Try
														Dim AList As List(Of String) = New List(Of String)(array.Length)

														For Each item As String In array
															If IsNullOrWhitespace(item) Then Continue For

															If Not StrContainsAny(item, True, "nvpciflt", "nvkflt") Then
																AList.Add(item)
															End If
														Next

														deletevalue(regkey2, "UpperFilters")

														If AList.Count > 0 Then
															regkey2.SetValue("UpperFilters", AList.ToArray(), RegistryValueKind.MultiString)
														End If
													Catch ex As Exception
														Application.Log.AddException(ex)
														Application.Log.AddWarningMessage("Failed to fix Optimus. You will have to manually remove the device with yellow mark in device manager to fix the missing videocard")
													End Try
												End If

												'For Each item As String In array
												'	If IsNullOrWhitespace(item) Then Continue For

												'	Application.Log.AddMessage("UpperFilter found : " + item)
												'	If StrContainsAny(item, True, "nvpciflt") Then
												'		Dim AList As ArrayList = New ArrayList(array)

												'		AList.Remove("nvpciflt")
												'		AList.Remove("nvkflt")

												'		Application.Log.AddMessage("nVidia Optimus UpperFilter Found.")
												'		Dim upfiler As String() = CType(AList.ToArray(GetType(String)), String())

												'		Try

												'			deletevalue(regkey2, "UpperFilters")
												'			If (upfiler IsNot Nothing) AndAlso (Not upfiler.Length < 1) Then
												'				regkey2.SetValue("UpperFilters", upfiler, RegistryValueKind.MultiString)
												'			End If
												'		Catch ex As Exception
												'			Application.Log.AddException(ex)
												'			Application.Log.AddWarningMessage("Failed to fix Optimus. You will have to manually remove the device with yellow mark in device manager to fix the missing videocard")
												'		End Try
												'	End If
												'Next
											End If
										End Using
									Next
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Function WinUpdatePending() As Boolean
		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
			Return (regkey IsNot Nothing)
		End Using
	End Function

	Private Function GPUIdentify() As GPUVendor
		Dim compatibleIDs() As String
		Dim isGpu As Boolean

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\PCI")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If IsNullOrWhitespace(child) OrElse Not StrContainsAny(child, True, "ven_8086", "ven_1002", "ven_10de") Then Continue For

						Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
							If regkey2 Is Nothing Then Continue For

							For Each child2 As String In regkey2.GetSubKeyNames
								If IsNullOrWhitespace(child2) Then Continue For

								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
									If regkey3 Is Nothing Then Continue For

									compatibleIDs = TryCast(regkey3.GetValue("CompatibleIDs"), String())

									If compatibleIDs IsNot Nothing AndAlso compatibleIDs.Length > 0 Then
										isGpu = False

										For Each id As String In compatibleIDs
											If StrContainsAny(id, True, "pci\cc_03") Then
												isGpu = True
												Exit For
											End If
										Next

										If isGpu Then
											For Each id As String In compatibleIDs
												If StrContainsAny(id, True, "ven_8086") Then
													Return GPUVendor.Intel
												ElseIf StrContainsAny(id, True, "ven_1002") Then
													Return GPUVendor.AMD
												ElseIf StrContainsAny(id, True, "ven_10de") Then
													Return GPUVendor.Nvidia
												End If
											Next
										End If
									End If
								End Using
							Next
						End Using
					Next
				End If
			End Using

			Return GPUVendor.Nvidia
		Catch ex As Exception
			Application.Log.AddException(ex)

			MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Error)

			Return GPUVendor.Nvidia
		End Try
	End Function

	Private Sub CloseDDU()
		If Not Dispatcher.CheckAccess() Then
			Dispatcher.BeginInvoke(Sub() CloseDDU())
		Else
			Try
				Me.Close()
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
	End Sub



#Region "frmMain Controls"

	Private Sub btnCleanRestart_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanRestart.Click
		If Not Application.Settings.GoodSite Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		Dim config As New ThreadSettings(False)
		config.Shutdown = False
		config.Restart = True

		PreCleaning(config)
		StartThread(config)
	End Sub

	Private Sub btnClean_Click(sender As Object, e As RoutedEventArgs) Handles btnClean.Click

		If Not Application.Settings.GoodSite Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		Dim config As New ThreadSettings(False)
		config.Shutdown = False
		config.Restart = False

		PreCleaning(config)
		StartThread(config)
	End Sub

	Private Sub btnCleanShutdown_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanShutdown.Click
		If Not Application.Settings.GoodSite Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		Dim config As New ThreadSettings(False)
		config.Shutdown = True
		config.Restart = False

		PreCleaning(config)
		StartThread(config)
	End Sub

	Private Sub btnWuRestore_Click(sender As Object, e As EventArgs) Handles btnWuRestore.Click
		EnableDriverSearch(True)
	End Sub

	Private Sub cbLanguage_SelectedIndexChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbLanguage.SelectionChanged
		If Application.Settings.SelectedLanguage IsNot Nothing Then
			Languages.Load(Application.Settings.SelectedLanguage)
			Languages.TranslateForm(Me)

			GetGPUDetails(False)
		End If
	End Sub

	Private Sub imgDonate_Click(sender As Object, e As EventArgs) Handles imgDonate.Click
		WinAPI.OpenVisitLink(" -visitdonate")
	End Sub

	Private Sub VisitDDUHomepageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUHomeMenuItem.Click
		WinAPI.OpenVisitLink(" -visitdduhome")
	End Sub

	Private Sub OptionsMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OptionsMenuItem.Click
		Dim frmOptions As New frmOptions

		With frmOptions
			.Background = Me.Background
			.DataContext = Me.DataContext
			.Icon = Me.Icon
			.Owner = Me
		End With

		frmOptions.ShowDialog()
	End Sub

	Private Sub AboutMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles AboutMenuItem.Click, ToSMenuItem.Click, TranslatorsMenuItem.Click
		Dim menuItem As MenuItem = TryCast(sender, MenuItem)

		If menuItem Is Nothing Then
			Return
		End If

		Select Case True
			Case StrContainsAny(menuItem.Name, True, "AboutMenuItem")
				ShowAboutWindow(1)
			Case StrContainsAny(menuItem.Name, True, "ToSMenuItem")
				ShowAboutWindow(2)
			Case StrContainsAny(menuItem.Name, True, "TranslatorsMenuItem")
				ShowAboutWindow(3)
		End Select
	End Sub

	Private Sub VisitSVNMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles VisitSVNMenuItem.Click
		WinAPI.OpenVisitLink(" -visitsvn")
	End Sub

	Private Sub VisitGuru3DNvidiaMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles VisitGuru3DNvidiaMenuItem.Click
		WinAPI.OpenVisitLink(" -visitguru3dnvidia")
	End Sub

	Private Sub VisitGuru3DAMDMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles VisitGuru3DAMDMenuItem.Click
		WinAPI.OpenVisitLink(" -visitguru3damd")
	End Sub

	Private Sub VisitGeforceMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles VisitGeforceMenuItem.Click
		WinAPI.OpenVisitLink(" -visitgeforce")
	End Sub

	Private Sub ExtendedLogMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ExtendedLogMenuItem.Click
		Dim frmLog As New frmLog

		With frmLog
			.Owner = Me
			.DataContext = Me.DataContext
			.Icon = Me.Icon
			.ResizeMode = Windows.ResizeMode.CanResizeWithGrip
			.WindowStyle = Windows.WindowStyle.SingleBorderWindow
			.WindowStartupLocation = Windows.WindowStartupLocation.CenterOwner
		End With

		frmLog.ShowDialog()
		Me.Activate()
	End Sub

	Private Sub imgOffer_Click(sender As Object, e As RoutedEventArgs) Handles imgOffer.Click
		WinAPI.OpenVisitLink(" -visitoffer")
	End Sub

#End Region

#Region "frmMain Events"

	Private Sub frmMain_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
		Languages.TranslateForm(Me, False)
	End Sub

	Private Sub frmMain_ContentRendered(sender As System.Object, e As System.EventArgs) Handles MyBase.ContentRendered
		Me.Topmost = False

		Try
			cbSelectedGPU.ItemsSource = [Enum].GetValues(GetType(GPUVendor))

			If Not Application.LaunchOptions.Silent Then
				CheckUpdates()

				Try

					'We check if there are any reboot from windows update pending. and if so we quit.
					If WinUpdatePending() Then
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text14"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning)
						CloseDDU()
						Exit Sub
					End If

				Catch ex As Exception
					Application.Log.AddException(ex)
					CloseDDU()
					Exit Sub
				End Try
			End If


			' ----------------------------------------------------------------------------
			' Trying to get the installed GPU info 
			' (These list the one that are at least installed with minimal driver support)
			' ----------------------------------------------------------------------------

			GetGPUDetails(True)

			Application.Settings.SelectedGPU = GPUIdentify()

			' -------------------------------------
			' Check if this is an AMD Enduro system
			' -------------------------------------
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\PCI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							If StrContainsAny(child, True, "ven_8086") Then
								Try
									Using subRegKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
										For Each childs As String In subRegKey.GetSubKeyNames()
											If IsNullOrWhitespace(childs) Then Continue For

											Using childRegKey As RegistryKey = MyRegistry.OpenSubKey(subRegKey, childs)
												Dim regValue As String = CStr(childRegKey.GetValue("Service"))

												If Not IsNullOrWhitespace(regValue) AndAlso StrContainsAny(regValue, True, "amdkmdap") Then
													enduro = True
													UpdateTextMethod("System seems to be an AMD Enduro (Intel)")
												End If
											End Using
										Next
									End Using
								Catch ex As Exception
									Continue For
								End Try
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

			If WindowsIdentity.GetCurrent().IsSystem Then
				Select Case WinForm.SystemInformation.BootMode
					Case WinForm.BootMode.FailSafe
						Application.Log.AddMessage("We are in Safe Mode")
					Case WinForm.BootMode.FailSafeWithNetwork
						Application.Log.AddMessage("We are in Safe Mode with Networking")
					Case WinForm.BootMode.Normal
						Application.Log.AddMessage("We are not in Safe Mode")
				End Select
			End If

			GetOemInfo()


			If Application.LaunchOptions.HasCleanArg Then
				Dim config As New ThreadSettings(True)

				workThread = New Thread(Sub() ThreadTask(config)) With
				{
				 .CurrentCulture = New Globalization.CultureInfo("en-US"),
				 .CurrentUICulture = New Globalization.CultureInfo("en-US"),
				 .Name = "workThread",
				 .IsBackground = True
				}

				workThread.Start()
			End If

		Catch ex As Exception
			Application.Log.AddException(ex, "frmMain loading caused error!")
		End Try
	End Sub

	Private Sub frmMain_Closing(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
		Try
			If cleaningThread IsNot Nothing AndAlso cleaningThread.IsAlive Then
				e.Cancel = True
				Exit Sub
			End If
		Catch ex As Exception
		End Try
	End Sub

#End Region

#Region "Cleaning Threads"

	Private Sub CleaningThread_Work(ByVal config As ThreadSettings)
		Try
			If config Is Nothing Then
				Throw New ArgumentNullException("config", "Null ThreadSettings in CleaningWorker as e.Argument!")
			End If

			Dim card1 As Integer = Nothing
			Dim vendid As String = ""
			Dim vendidexpected As String = ""
			Dim removegfe As Boolean = config.RemoveGFE
			Dim array() As String

			UpdateTextMethod(UpdateTextTranslated(19))

			Select Case config.SelectedGPU
				Case GPUVendor.Nvidia : vendidexpected = "VEN_10DE"
				Case GPUVendor.AMD : vendidexpected = "VEN_1002"
				Case GPUVendor.Intel : vendidexpected = "VEN_8086"
			End Select


			UpdateTextMethod(UpdateTextTranslated(20) + " " & config.SelectedGPU.ToString() & " " + UpdateTextTranslated(21))
			Application.Log.AddMessage("Uninstalling " + config.SelectedGPU.ToString() + " driver ...")
			UpdateTextMethod(UpdateTextTranslated(22))


			'SpeedUP the removal of the NVIDIA adapter due to how the NVIDIA installer work.
			If config.SelectedGPU = GPUVendor.Nvidia Then
				temporarynvidiaspeedup(config)
			End If



			'----------------------------------------------
			'Here I remove AMD HD Audio bus (System device)
			'----------------------------------------------

			Try
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", vendidexpected, True)
				If found.Count > 0 Then
					For Each SystemDevice As SetupAPI.Device In found
						For Each Sibling In SystemDevice.SiblingDevices
							If StrContainsAny(Sibling.ClassName, True, "DISPLAY") Then
								Application.Log.AddMessage("Removing AMD HD Audio Bus (amdkmafd)")

								Win32.SetupAPI.UninstallDevice(SystemDevice)

								'Verification is there is still an AMD HD Audio Bus device and set donotremoveamdhdaudiobusfiles to true if thats the case
								Try
									donotremoveamdhdaudiobusfiles = False
									Using subregkey As RegistryKey = myregistry.opensubkey(registry.localmachine, "SYSTEM\CurrentControlSet\Enum\PCI")
										If subregkey IsNot Nothing Then
											For Each child2 As String In subregkey.GetSubKeyNames()
												If IsNullOrWhitespace(child2) Then Continue For

												If StrContainsAny(child2, True, "ven_1002") Then
													Using regkey3 As RegistryKey = myregistry.opensubkey(subregkey, child2)
														For Each child3 As String In regkey3.GetSubKeyNames()
															If IsNullOrWhitespace(child3) Then Continue For

															array = CType(MyRegistry.OpenSubKey(regkey3, child3).GetValue("LowerFilters", String.Empty), String())
															If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
																For Each entry As String In array
																	If IsNullOrWhitespace(entry) Then Continue For

																	If StrContainsAny(entry, True, "amdkmafd") Then
																		Application.Log.AddWarningMessage("Found a remaining AMD audio controller bus ! Preventing the removal of its driverfiles.")
																		donotremoveamdhdaudiobusfiles = True
																	End If
																Next
															End If
														Next
													End Using
												End If
											Next
										End If
									End Using
								Catch ex As Exception
									Application.Log.AddException(ex)
								End Try

							End If
						Next
					Next
				End If
			Catch ex As Exception
				'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				Application.Log.AddException(ex)
			End Try




			' ----------------------
			' Removing the videocard
			' ----------------------


			Try
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("display", vendidexpected)
				If found.Count > 0 Then
					For Each d As SetupAPI.Device In found

						Win32.SetupAPI.UninstallDevice(d)

					Next
				End If

			Catch ex As Exception
				'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				Application.Log.AddException(ex)
			End Try


			UpdateTextMethod(UpdateTextTranslated(23))
			Application.Log.AddMessage("SetupAPI Display Driver removal: Complete.")


			cleandriverstore(config)

			UpdateTextMethod(UpdateTextTranslated(24))
			Application.Log.AddMessage("Executing SetupAPI Remove Audio controler.")


			Try
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendidexpected)
				If found.Count > 0 Then
					For Each d As SetupAPI.Device In found

						SetupAPI.UninstallDevice(d)

					Next
				End If

			Catch ex As Exception
				'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				Application.Log.AddException(ex)
			End Try

			UpdateTextMethod(UpdateTextTranslated(25))


			Application.Log.AddMessage("SetupAPI Remove Audio controler Complete.")


			If config.SelectedGPU <> GPUVendor.Intel Then
				cleandriverstore(config)
			End If


			Dim position2 As Integer = Nothing

			'Here I remove 3dVision USB Adapter.
			If config.SelectedGPU = GPUVendor.Nvidia Then

				Try
					Dim HWID3dvision As String() =
					 {"USB\VID_0955&PID_0007",
					  "USB\VID_0955&PID_7001",
					  "USB\VID_0955&PID_7002",
					  "USB\VID_0955&PID_7003",
					  "USB\VID_0955&PID_7004",
					  "USB\VID_0955&PID_7008",
					  "USB\VID_0955&PID_7009",
					  "USB\VID_0955&PID_700A",
					  "USB\VID_0955&PID_700C",
					  "USB\VID_0955&PID_700D&MI_00",
					  "USB\VID_0955&PID_700E&MI_00"}

					'3dVision Removal
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media")
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.HardwareIDs(0), True, HWID3dvision) Then
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If



					'NVIDIA SHIELD Wireless Controller Trackpad
					found = SetupAPI.GetDevices("mouse")
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.HardwareIDs(0), True, "hid\vid_0955&pid_7210") Then
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If

					If config.RemoveGFE Then
						' NVIDIA Virtual Audio Device (Wave Extensible) (WDM) Removal

						found = SetupAPI.GetDevices("media")
						If found.Count > 0 Then
							For Each d As SetupAPI.Device In found
								If StrContainsAny(d.HardwareIDs(0), True, "USB\VID_0955&PID_9000") Then
									SetupAPI.UninstallDevice(d)
								End If
							Next
							found.Clear()
						End If
					End If

					'nVidia AudioEndpoints Removal
					found = SetupAPI.GetDevices("audioendpoint")
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.FriendlyName, True, "nvidia virtual audio device", "nvidia high definition audio") Then
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If

				Catch ex As Exception
					Application.Log.AddException(ex)
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				End Try
			End If

			If config.SelectedGPU = GPUVendor.AMD Then
				' ------------------------------
				' Removing some of AMD AudioEndpoints
				' ------------------------------
				Application.Log.AddMessage("Removing AMD Audio Endpoints")
				Try
					'AMD AudioEndpoints Removal
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("audioendpoint")
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.FriendlyName, True, "amd high definition audio device", "digital audio (hdmi) (high definition audio device)") Then
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If
				Catch ex As Exception
					MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
					Application.Log.AddException(ex)
				End Try
			End If

			If config.SelectedGPU = GPUVendor.Intel Then

				'Removing Intel WIdI bus Enumerator
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", Nothing, False)
				If found.Count > 0 Then
					For Each d As SetupAPI.Device In found
						If d.HasHardwareID AndAlso StrContainsAny(d.HardwareIDs(0), True, "root\iwdbus") Then  'Workaround for a bug report we got.
							SetupAPI.UninstallDevice(d)
						End If
					Next
					found.Clear()
				End If
			End If

			Application.Log.AddMessage("SetupAPI Remove Audio/HDMI Complete")

			'removing monitor and hidden monitor



			If config.RemoveMonitors Then
				Application.Log.AddMessage("SetupAPI Remove Monitor started")
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("monitor")
				If found.Count > 0 Then
					For Each d As SetupAPI.Device In found
						SetupAPI.UninstallDevice(d)
					Next
				End If
				UpdateTextMethod(UpdateTextTranslated(27))
			End If
			UpdateTextMethod(UpdateTextTranslated(28))

			'here we set back to default the changes made by the AMDKMPFD even if we are cleaning amd or intel. We dont want that
			'espcially if we are not using an AMD GPU

			If config.RemoveAMDKMPFD Then
				Try
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", "0a0", False)
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.HardwareIDs(0), True, "DEV_0A08", "DEV_0A03") Then
								If d.LowerFilters IsNot Nothing AndAlso StrContainsAny(d.LowerFilters(0), True, "amdkmpfd") Then
									If win10 Then
										SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\PCI.inf", True)
									Else
										SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\machine.inf", True)
									End If
								End If
							End If
						Next
					End If

				Catch ex As Exception
					Application.Log.AddException(ex)
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				End Try

				'We now try to remove the service AMDPMPFD if its lowerfilter is not found
				If config.Restart Or config.Shutdown Then
					If Not checkamdkmpfd() Then
						CleanupEngine.cleanserviceprocess({"amdkmpfd"})
					End If
				End If
			End If

			If config.SelectedGPU = GPUVendor.AMD Then
				cleanamdserviceprocess()
				cleanamd(config)

				If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
					Application.Log.AddMessage("Killing Explorer.exe")

					KillProcess("explorer")
				End If

				cleanamdfolders(config)
			End If

			If config.SelectedGPU = GPUVendor.Nvidia Then
				cleannvidiaserviceprocess(config)
				cleannvidia(config)

				'If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
				'	Application.Log.AddMessage("Killing Explorer.exe")

				'	Dim appproc = process.GetProcessesByName("explorer")
				'	For i As Integer = 0 To appproc.Length - 1
				'		appproc(i).Kill()
				'	Next i
				'End If


				cleannvidiafolders(config)
				checkpcieroot(config)
			End If

			If config.SelectedGPU = GPUVendor.Intel Then
				cleanintelserviceprocess()
				cleanintel(config)

				If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
					Application.Log.AddMessage("Killing Explorer.exe")

					KillProcess("explorer")
				End If

				cleanintelfolders()
			End If

			cleandriverstore(config)
			fixregistrydriverstore()
			'rebuildcountercache()

			config.Success = True
		Catch ex As Exception
			Application.Log.AddException(ex)
			config.Success = False
		Finally
			CleaningThread_Completed(config)
		End Try
	End Sub

	Private Sub CleaningThread_Completed(ByVal config As ThreadSettings)
		Try
			Application.Log.AddMessage("Clean uninstall completed!" & CRLF & ">> GPU: " & config.SelectedGPU.ToString())

			If Not config.Success Then
				MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), "Error!", MessageBoxButton.OK, MessageBoxImage.Error)

				'Scan for new hardware to not let users into a non working state.
				SetupAPI.ReScanDevices()

				CloseDDU()
				Exit Sub
			End If

			UpdateTextMethod(UpdateTextTranslated(9))

			If config.PreventClose Then
				Exit Sub
			End If

			If Not config.Shutdown Then
				SetupAPI.ReScanDevices()
			End If

			EnableControls(True)

			If Not config.Silent And Not config.Restart And Not config.Shutdown Then
				If MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text10"), config.AppName, MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
					CloseDDU()
					Exit Sub
				End If
			End If

			If config.Restart Then
				Application.RestartComputer()
				Exit Sub
			End If

			If config.Shutdown Then
				Application.ShutdownComputer()
				Exit Sub
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub ThreadTask(ByVal config As ThreadSettings)
		Try
			config.PreventClose = True

			PreCleaning(config)

			If config.HasCleanArg Then
				If config.CleanAmd Then
					config.Success = False
					config.SelectedGPU = GPUVendor.AMD

					StartThread(config)

					While cleaningThread.IsAlive
						Thread.Sleep(200)
					End While
				End If


				If config.CleanNvidia Then
					config.Success = False
					config.SelectedGPU = GPUVendor.Nvidia

					StartThread(config)

					While cleaningThread.IsAlive
						Thread.Sleep(200)
					End While
				End If

				If config.CleanIntel Then
					config.Success = False
					config.SelectedGPU = GPUVendor.Intel

					StartThread(config)

					While cleaningThread.IsAlive
						Thread.Sleep(200)
					End While
				End If
			End If

			If config.Restart Then
				Application.RestartComputer()
				Exit Sub
			End If

			If config.Shutdown Then
				Application.ShutdownComputer()
				Exit Sub
			End If

			If config.Silent Then
				CloseDDU()
			Else
				If MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text10"), config.AppName, MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
					CloseDDU()
					Exit Sub
				End If
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		Finally
			EnableControls(True)
		End Try
	End Sub

	Private Sub PreCleaning(ByVal config As ThreadSettings)
		If Not Me.Dispatcher.CheckAccess() Then
			Me.Dispatcher.BeginInvoke(Sub() PreCleaning(config))
		Else
			EnableControls(False)

			EnableDriverSearch(False)

			'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
			KillGPUStatsProcesses()
			'this shouldn't be slow, so it isn't on a thread/background worker

			SystemRestore()
		End If
	End Sub

	Private Sub StartThread(ByVal config As ThreadSettings)
		Try
			If System.Diagnostics.Debugger.IsAttached Then			'TODO: remove when tested
				Dim logEntry As New LogEntry() With {.Message = "Used settings for cleaning!"}

				For Each p As PropertyInfo In config.GetType().GetProperties(BindingFlags.Public Or BindingFlags.Instance)
					logEntry.Add(p.Name, If(p.GetValue(config, Nothing) IsNot Nothing, p.GetValue(config, Nothing).ToString(), "-"))
				Next

				Application.Log.Add(logEntry)
			End If

			If cleaningThread IsNot Nothing AndAlso cleaningThread.IsAlive Then
				Throw New ArgumentException("cleaningThread", "Thread already exists and is busy!")
			End If

			cleaningThread = New Thread(Sub() CleaningThread_Work(config)) With
			  {
			   .CurrentCulture = New Globalization.CultureInfo("en-US"),
			   .CurrentUICulture = New Globalization.CultureInfo("en-US"),
			   .Name = "CleaningThread",
			   .IsBackground = True
			  }

			cleaningThread.Start()

		Catch ex As Exception
			cleaningThread = Nothing
			Application.Log.AddException(ex, "Launching cleaning thread failed!")
		End Try
	End Sub

#End Region




	Private Sub ShowAboutWindow(ByVal frmType As Int32)
		Dim frmAbout As New frmAbout With
		{
		  .Owner = Me,
		  .DataContext = Me.DataContext,
		  .Icon = Me.Icon,
		  .Width = Me.Width,
		  .Height = Me.Height,
		  .FrmType = frmType
		}

		frmAbout.ShowDialog()
	End Sub

	Private Sub GetGPUDetails(ByVal firstLaunch As Boolean)
		lbLog.Items.Clear()

		UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(10), Application.Settings.AppVersion.ToString()))

		Dim info As LogEntry = Nothing

		If firstLaunch Then
			info = New LogEntry()
			info.Message = "System Information"
			info.Add("DDU Version", Application.Settings.AppVersion.ToString())
			info.Add("OS", Application.Settings.WinVersionText)
			info.Add("Architecture", If(Application.Settings.WinIs64, "x64", "x86"))

			Try
				Dim windowsPrincipal As WindowsPrincipal = New WindowsPrincipal(WindowsIdentity.GetCurrent())
				If WindowsIdentity.GetCurrent().IsSystem Then
					info.Add("UserRights", "System")
				ElseIf windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator) Then
					info.Add("UserRights", "Admin")
				ElseIf windowsPrincipal.IsInRole(WindowsBuiltInRole.User) Then
					info.Add("UserRights", "User")
				Else
					info.Add("UserRights", "Unknown")
				End If
			Catch ex As Exception
				info.Add("UserRights", "Unknown")
			End Try

			If Application.LaunchOptions.ArgumentsArray IsNot Nothing AndAlso Application.LaunchOptions.ArgumentsArray.Length > 0 Then
				info.Add("Arguments", String.Join(Environment.NewLine, Application.LaunchOptions.ArgumentsArray))
			Else
				info.Add("Arguments", "<empty>")
			End If

			info.Add(KvP.Empty)
		End If

		Try
            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames
                        If IsNullOrWhitespace(child) Then Continue For

                        If Not StrContainsAny(child, True, "properties") Then

                            Using subRegkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                If subRegkey IsNot Nothing Then
                                    Dim regValue As String = subRegkey.GetValue("Device Description", String.Empty).ToString()

                                    If Not IsNullOrWhitespace(regValue) Then
                                        UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
                                        If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)
                                    Else

                                        If subRegkey.GetValueKind("DriverDesc") = RegistryValueKind.Binary Then
                                            regValue = HexToString(GetREG_BINARY(subRegkey, "DriverDesc").Replace("00", ""))

                                        Else
                                            regValue = subRegkey.GetValue("DriverDesc", String.Empty).ToString()
                                        End If

                                        UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
                                        If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)

                                    End If

                                    regValue = subRegkey.GetValue("MatchingDeviceId", String.Empty).ToString()

                                    If Not IsNullOrWhitespace(regValue) Then
                                        UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(13), regValue))
                                        If firstLaunch Then info.Add("GPU DeviceID", regValue)
                                    End If

                                    Try
                                        If subRegkey.GetValueKind("HardwareInformation.BiosString") = RegistryValueKind.Binary Then
                                            regValue = HexToString(GetREG_BINARY(subRegkey, "HardwareInformation.BiosString").Replace("00", ""))

                                            UpdateTextMethod(String.Format("Vbios: {0}", regValue))
                                            If firstLaunch Then info.Add("Vbios", regValue)
                                        Else
                                            regValue = subRegkey.GetValue("HardwareInformation.BiosString", String.Empty).ToString()

                                            Dim sb As New StringBuilder(30)
                                            Dim values() As String = regValue.Split(New String() {" ", "."}, StringSplitOptions.None)

                                            For i As Int32 = 0 To values.Length - 1
                                                If i = values.Length - 1 Then       'Last
                                                    sb.Append(values(i).PadLeft(2, "0"c))
                                                ElseIf i > 0 Then
                                                    sb.AppendFormat("{0}.", values(i).PadLeft(2, "0"c))
                                                Else
                                                    sb.AppendFormat("{0} ", values(i))
                                                End If
                                            Next
                                            regValue = sb.ToString()

                                            UpdateTextMethod(String.Format("Vbios: {0}", regValue))
                                            If firstLaunch Then info.Add("Vbios", regValue)
                                        End If
                                    Catch ex As Exception
                                    End Try

                                    regValue = subRegkey.GetValue("DriverVersion", String.Empty).ToString()

                                    If Not IsNullOrWhitespace(regValue) Then
                                        UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(14), regValue))
                                        If firstLaunch Then info.Add("Detected Driver(s) Version(s)", regValue)
                                    End If

                                    regValue = subRegkey.GetValue("InfPath", String.Empty).ToString()

                                    If Not IsNullOrWhitespace(regValue) Then
                                        UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(15), regValue))
                                        If firstLaunch Then info.Add("INF name", regValue)
                                    End If

                                    regValue = subRegkey.GetValue("InfSection", String.Empty).ToString()

                                    If Not IsNullOrWhitespace(regValue) Then
                                        UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(16), regValue))
                                        If firstLaunch Then info.Add("INF section", regValue)
                                    End If
                                End If

                                UpdateTextMethod("--------------")
                                If firstLaunch Then info.Add(KvP.Empty)
                            End Using
                        End If
                    Next
                End If
            End Using

			If firstLaunch Then
				Application.Log.Add(info)
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Public Sub EnableControls(ByVal enabled As Boolean)
		If Not Me.Dispatcher.CheckAccess() Then
			Me.Dispatcher.BeginInvoke(Sub() EnableControls(enabled))
		Else
			'	Me.IsEnabled = enabled

			Dim uiContent As UIElement = TryCast(Me.Content, UIElement)

			If uiContent IsNot Nothing Then
				uiContent.IsEnabled = enabled
			Else
				cbLanguage.IsEnabled = enabled			'Selecting this at runtime maybe not good idea.. ;)
				cbSelectedGPU.IsEnabled = enabled
				ButtonsPanel.IsEnabled = enabled
				btnWuRestore.IsEnabled = enabled
				MenuStrip1.IsEnabled = enabled
			End If

		End If
	End Sub

	Private Sub SystemRestore()
		If Application.Settings.CreateRestorePoint AndAlso System.Windows.Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then
			Dim frmSystemRestore As New frmSystemRestore

			With frmSystemRestore
				.WindowStartupLocation = Windows.WindowStartupLocation.CenterOwner
				.Background = Me.Background
				.Owner = Me
				.DataContext = Me.DataContext
				.ResizeMode = Windows.ResizeMode.NoResize
				.WindowStyle = Windows.WindowStyle.ToolWindow
			End With

			frmSystemRestore.ShowDialog()

		End If
	End Sub

	Private Sub GetOemInfo()
		Dim info As New LogEntry()
		info.Type = LogType.Event
		info.Separator = " = "
		info.Message = "The following third-party driver packages are installed on this computer"

		Try
			For Each oem As Inf In GetOemInfList(Application.Paths.WinDir & "inf\")
				info.Add(oem.FileName)
				info.Add("Provider", oem.Provider)
				info.Add("Class", oem.Class)

				If Not oem.IsValid Then
					info.Add("This inf entry is corrupted or invalid.")
					'	Delete(oem.FileName)  ' DOUBLE CHECK THIS before uncommentting
				End If

				info.Add(KvP.Empty)
			Next

			Application.Log.Add(info)
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub KillProcess(ByVal ParamArray processnames As String())
		For Each processName As String In processnames
			If String.IsNullOrEmpty(processName) Then
				Continue For
			End If

			For Each process As Process In process.GetProcessesByName(processName)
				Try
					process.Kill()
				Catch ex As Exception
					Application.Log.AddExceptionWithValues(ex, "@KillProcess()", String.Concat("ProcessName: ", processName))
				End Try
			Next
		Next
	End Sub

	Private Sub KillGPUStatsProcesses()
		' Not sure for the x86 one...
		' Shady: probably the same but without _x64, and a few sites seem to confirm this, doesn't hurt to just add it anyway

		KillProcess(
		 "MSIAfterburner",
		  "PrecisionX_x64",
		  "PrecisionXServer_x64",
		  "PrecisionX",
		  "PrecisionXServer",
		  "RTSS",
		  "RTSSHooksLoader64",
		  "EncoderServer64",
		  "RTSSHooksLoader",
		  "EncoderServer",
		  "nvidiaInspector")
	End Sub

	Private Sub EnableDriverSearch(ByVal enable As Boolean)
		Dim version As OSVersion = Application.Settings.WinVersion

		If Not enable Then
			Application.Log.AddMessage("Trying to disable search for Windows Updates", "Version", GetDescription(version))
		End If

		If version >= OSVersion.Win7 Then
			Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
                    Dim regValue As Int32 = CInt(regkey.GetValue("SearchOrderConfig"))

                    If regkey IsNot Nothing AndAlso regValue <> If(enable, 1, 0) Then
                        regkey.SetValue("SearchOrderConfig", If(enable, 1, 0), RegistryValueKind.DWord)

                        If enable Then
                            MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text11"))
                        Else
                            MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text9"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
                        End If
                    End If
                End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		If version >= OSVersion.WinVista AndAlso version < OSVersion.Win7 Then
			Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
                    Dim regValue As Int32 = CInt(regkey.GetValue("DontSearchWindowsUpdate"))

                    If regkey IsNot Nothing AndAlso regValue <> If(enable, 0, 1) Then
                        regkey.SetValue("DontSearchWindowsUpdate", If(enable, 0, 1), RegistryValueKind.DWord)

                        If enable Then
                            MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text11"))
                        Else
                            MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text9"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
                        End If
                    End If
                End Using

			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
	End Sub

	Private Function checkamdkmpfd() As Boolean
		Try
			Application.Log.AddMessage("Checking if AMDKMPFD is present before Service removal")

            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\ACPI")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If IsNullOrWhitespace(child) Then Continue For

                        If StrContainsAny(child, True, "pnp0a08", "pnp0a03") Then
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                If regkey2 IsNot Nothing Then
                                    For Each child2 As String In regkey2.GetSubKeyNames()
                                        If IsNullOrWhitespace(child2) Then Continue For

                                        Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
                                            Dim array As String() = TryCast(regkey3.GetValue("LowerFilters"), String())

                                            If array IsNot Nothing AndAlso array.Length > 0 Then
                                                For Each value As String In array
                                                    If Not IsNullOrWhitespace(value) Then
                                                        If StrContainsAny(value, True, "amdkmpfd") Then
                                                            Application.Log.AddMessage("Found an AMDKMPFD! in " + child)
                                                            Application.Log.AddMessage("We do not remove the AMDKMPFP service yet")

                                                            Return True
                                                        End If
                                                    End If
                                                Next
                                            End If
                                        End Using

                                    Next
                                End If
                            End Using
                        End If
                    Next
                End If
            End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Return False
	End Function

	Public Function UpdateTextTranslated(ByVal number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
	End Function

	Public Function UpdateTextEnglish(ByVal number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1), True)
	End Function

	Private Sub temporarynvidiaspeedup(ByVal config As ThreadSettings)	 'we do this to speedup the removal of the nividia display driver because of the huge time the nvidia installer files take to do unknown stuff.
		Dim filePath As String = Nothing

		Try
			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("installer2") Then
						For Each child2 As String In Directory.GetDirectories(child)
							If IsNullOrWhitespace(child2) = False Then
								If child2.ToLower.Contains("display.3dvision") Or
								   child2.ToLower.Contains("display.controlpanel") Or
								   child2.ToLower.Contains("display.driver") Or
								   child2.ToLower.Contains("display.gfexperience") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("display.nvirusb") Or
								   child2.ToLower.Contains("display.optimus") Or
								   child2.ToLower.Contains("display.physx") AndAlso config.RemovePhysX Or
								   child2.ToLower.Contains("display.update") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("display.nview") Or
								   child2.ToLower.Contains("display.nvwmi") Or
								   child2.ToLower.Contains("ansel.") Or
								   child2.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("nvidia.update") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("installer2\installer") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("network.service") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("miracast.virtualaudio") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("update.core") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("virtualaudio.driver") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("coretemp") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("shield") AndAlso config.RemoveGFE Or
								   child2.ToLower.Contains("hdaudio.driver") Then
									Try
										Delete(child2)
									Catch ex As Exception
									End Try
								End If
							End If
						Next

						If Directory.GetDirectories(child).Length = 0 Then
							Try
								Delete(child)
							Catch ex As Exception
							End Try
						End If
					End If
				End If
			Next
		Catch ex As Exception
		End Try
	End Sub

	Public Sub UpdateTextMethod(ByVal strMessage As String)
		If Not lbLog.Dispatcher.CheckAccess() Then
			Dispatcher.Invoke(Sub() UpdateTextMethod(strMessage))
		Else
			lbLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " - " + strMessage)
			lbLog.SelectedIndex = lbLog.Items.Count - 1
			lbLog.ScrollIntoView(lbLog.SelectedItem)
		End If
	End Sub

	Public Function GetREG_BINARY(ByVal Path As RegistryKey, ByVal Value As String) As String
		Dim Data() As Byte = CType(Microsoft.Win32.Registry.GetValue(Path.ToString, Value, Nothing), Byte())

		If Data Is Nothing Then Return "N/A"

		Dim Result As String = String.Empty

		For j As Integer = 0 To Data.Length - 1
			Result &= Hex(Data(j)).PadLeft(2, "0"c) & ""
		Next

		Return Result
	End Function

	Public Function HexToString(ByVal Data As String) As String
		Dim com As String = ""

		For x = 0 To Data.Length - 1 Step 2
			com &= ChrW(CInt("&H" & Data.Substring(x, 2)))
		Next

		Return com
	End Function

	Private Sub deletesubregkey(ByVal value1 As RegistryKey, ByVal value2 As String)
		CleanupEngine.deletesubregkey(value1, value2)
	End Sub

	Private Sub deletevalue(ByVal value1 As RegistryKey, ByVal value2 As String)
		CleanupEngine.deletevalue(value1, value2)
	End Sub
	Private Sub Delete(ByVal filename As String)
		FileIO.Delete(filename)
		CleanupEngine.RemoveSharedDlls(filename)
	End Sub

	Private Sub AmdEnvironementPath(ByVal filepath As String)
		Dim valuesToFind() As String = New String() {
		 filepath & "\amd app\bin\x86_64",
		 filepath & "\amd app\bin\x86",
		 filepath & "\ati.ace\core-static"
		}

		CleanEnvironementPath(valuesToFind)
	End Sub

	' "Universal" solution, can be used for Nvidia/Intel too
	Private Sub CleanEnvironementPath(ByVal valuesToRemove() As String)
		Dim value As String = Nothing

		Dim paths() As String = Nothing
		Dim newPaths As List(Of String)
		Dim removedPaths As List(Of String)

		'--------------------------------
		'System environement path cleanup
		'--------------------------------

		Dim logEntry As LogEntry = Application.Log.CreateEntry()
		logEntry.Message = "System Environment Path cleanUP"

		Try
            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                If subregkey IsNot Nothing Then
                    For Each child2 As String In subregkey.GetSubKeyNames()
                        If StrContainsAny(child2, True, "controlset") Then

                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetValueNames()
                                        If IsNullOrWhitespace(child) Then Continue For

                                        If child.Equals("Path", StringComparison.OrdinalIgnoreCase) Then
                                            value = regkey.GetValue(child, String.Empty).ToString()

                                            If Not IsNullOrWhitespace(value) Then
                                                paths = If(value.Contains(";"), value.Split(New Char() {";"c}, StringSplitOptions.None), New String() {value})

                                                newPaths = New List(Of String)(paths.Length)
                                                removedPaths = New List(Of String)(paths.Length)

                                                For Each p As String In paths
                                                    If Not StrContainsAny(p, True, valuesToRemove) Then 'StrContainsAny(..) checks p and each valuesToRemove for empty/null
                                                        newPaths.Add(p)
                                                    Else
                                                        removedPaths.Add(p)
                                                    End If
                                                Next

                                                logEntry.Add(child2, String.Join(Environment.NewLine, paths))
                                                logEntry.Add(KvP.Empty)

                                                If removedPaths.Count > 0 Then  'Change regkey's value only if modified
                                                    regkey.SetValue(child, String.Join(";", newPaths.ToArray()))

                                                    logEntry.Add(">> Removed", String.Join(Environment.NewLine, removedPaths.ToArray()))    'Log removed values
                                                Else
                                                    logEntry.Add(">> Not modified")
                                                End If

                                                logEntry.Add(KvP.Empty)
                                                logEntry.Add(KvP.Empty)

                                                '	Select Case True
                                                '		Case value.Contains(";" + filepath & "\amd app\bin\x86_64")
                                                '			value = value.Replace(";" + filepath & "\amd app\bin\x86_64", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(filepath & "\amd app\bin\x86_64;")
                                                '			value = value.Replace(filepath & "\amd app\bin\x86_64;", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(";" + filepath & "\amd app\bin\x86")
                                                '			value = value.Replace(";" + filepath & "\amd app\bin\x86", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(filepath & "\amd app\bin\x86;")
                                                '			value = value.Replace(filepath & "\amd app\bin\x86;", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(";" + filepath & "\ati.ace\core-static")
                                                '			value = value.Replace(";" + filepath & "\ati.ace\core-static", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(filepath & "\ati.ace\core-static;")
                                                '			value = value.Replace(filepath & "\ati.ace\core-static;", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(";" + filepath & "\ati.ace\core-static")
                                                '			value = value.Replace(";" + filepath & "\ati.ace\core-static", "")
                                                '			regkey.SetValue(child, value)

                                                '		Case value.Contains(filepath & "\ati.ace\core-static;")
                                                '			value = value.Replace(filepath & "\ati.ace\core-static;", "")
                                                '			regkey.SetValue(child, value)

                                                '	End Select
                                            End If
                                        End If
                                    Next
                                End If
                            End Using
                        End If
                    Next
                End If
            End Using

			logEntry.Message &= Environment.NewLine & ">> Completed!"
		Catch ex As Exception
			logEntry.Message &= Environment.NewLine & ">> Failed!"
			logEntry.AddException(ex, False)
		Finally
			Application.Log.Add(logEntry)
		End Try

		'end system environement patch cleanup
	End Sub

	Private Sub testingMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles testingMenuItem.Click
		Dim testDir As String = "E:\Program Files\NVIDIA Corporation\Installer2\NvNodejs.{238C4CE3-6554-49D2-BD02-88084DB29453}\node_modules\socket.io\node_modules\socket.io-client\node_modules\engine.io-client\node_modules\engine.io-parser\node_modules\base64-arraybuffer\lib\ddu\wagnard\does\this\work\I\think\it\should"
		Dim testFileShortPath As String = "E:\PROGRA~1\NVIDIA~1\INSTAL~1\NVNODE~1.{23\NODE_M~1\socket.io\NODE_M~1\SOCKET~1.IO-\NODE_M~1\ENGINE~1.IO-\NODE_M~1\ENGINE~1.IO-\NODE_M~1\BASE64~1\lib\ddu\wagnard\does\this\work\I\think\it\should\test\test.txt"
		' 288 chars, feel free to test ;P
		' Btw, change drive letter if you try!

		Try
			'	FileIO.CreateDir(testDir)							' Create dir for testing!
			'	Delete(testFileShortPath)					' Delete file with short path
			'	Delete(testDir)								' Delete deletes both, is it Dir or File
			'	FileIO.ExistsDir(testDir)							' Dir exists?
			'	FileIO.ExistsFile(testFileShortPath)				' File exists?
			'	FileIO.GetFiles("E:\_temp\test\", "*", True)		' Get files
			'	FileIO.GetDirectories("E:\_temp\test\", "*", True)	' Get directories

			'TEST
			'For Each d As SetupAPI.Device In SetupAPI.GetDevices("display")
			'	If MessageBox.Show("Uninstall: " & d.Description, "?", MessageBoxButton.YesNo, MessageBoxImage.Stop) = MessageBoxResult.Yes Then
			'		SetupAPI.UninstallDevice(d)
			'	End If
			'Next


			Dim key As String = "SOFTWARE\ATI"

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, key, True)
				If regkey IsNot Nothing Then
					regkey.SetValue("TEST2", "Some other testvalue")

					MessageBox.Show("Value: " & regkey.GetValue("TEST2", String.Empty).ToString())
				Else
					MessageBox.Show("regkey = Nothing")
				End If
			End Using

			'For i As Int32 = 0 To 1000
			'	Application.Log.AddException(New ComponentModel.Win32Exception(32), "TEST: Message" & i.ToString())
			'Next
			' Multiline TEST
			'Dim logEntry As LogEntry = Application.Log.CreateEntry()

			'logEntry.Message =
			' "|" & vbTab & "Testing Multiline" & vbTab & vbTab & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & "Second Line" & vbTab & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & vbTab & "Third Line" & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & vbTab & vbTab & "Fourth Line" & vbTab & "|"

			'logEntry.Add(New KvP("Numbers", String.Join(Environment.NewLine, New String() {"1", "2", "3", "4", "5", "6"})))
			'Application.Log.Add(logEntry)
		Catch ex As Exception
			MessageBox.Show(ex.Message & CRLF & CRLF & ex.StackTrace, "Testing fails!")
		End Try
	End Sub

	Private Sub checkXMLMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles checkXMLMenuItem.Click
		Dim current As Languages.LanguageOption = Application.Settings.SelectedLanguage

		Languages.CheckLanguageFiles()

		Languages.Load(current)
	End Sub

	Private Sub SetupAPIMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles SetupAPIMenuItem.Click
		Dim testWindow As New SetupAPITestWindow

		testWindow.ShowDialog()
	End Sub
End Class
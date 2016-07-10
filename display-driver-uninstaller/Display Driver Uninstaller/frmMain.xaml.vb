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
	Private WithEvents BackgroundWorker1 As New System.ComponentModel.BackgroundWorker

	Dim backgroundworkcomplete As Boolean = True

	Dim silent As Boolean = False
	Dim argcleanamd As Boolean = False
	Dim argcleanintel As Boolean = False
	Dim argcleannvidia As Boolean = False
	Dim nbclean As Integer = 0
	Dim restart As Boolean = False
	Dim MyIdentity As WindowsIdentity = WindowsIdentity.GetCurrent()
	Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
	Dim principal As WindowsPrincipal = New WindowsPrincipal(identity)
	Dim processinfo As New ProcessStartInfo
	Dim process As New Process

	Dim reboot As Boolean = False
	Dim shutdown As Boolean = False
	Public Shared baseDir As String = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
	Public Shared win8higher As Boolean = False
	Public win10 As Boolean = False
	Public Shared winxp As Boolean = False
	Dim stopme As Boolean = False

	Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive").ToLower
	Dim reply As String = Nothing
	Dim reply2 As String = Nothing

	Dim safemode As Boolean = False
	Dim CleanupEngine As New CleanupEngine
	Dim enduro As Boolean = False
	Public Shared preventclose As Boolean = False
	Dim closeapp As Boolean = False
	Public ddudrfolder As String
	Public Shared donotremoveamdhdaudiobusfiles As Boolean = True



	Private Sub Checkupdates2()
		If Not Me.Dispatcher.CheckAccess() Then
			Dispatcher.Invoke(Sub() Checkupdates2())
		Else
			lblUpdate.Content = Languages.GetTranslation("frmMain", "lblUpdate", "Text")

			Dim updates As Integer = HasUpdates()

			If updates = 1 Then
				lblUpdate.Content = Languages.GetTranslation("frmMain", "lblUpdate", "Text2")

			ElseIf updates = 2 Then
				lblUpdate.Content = Languages.GetTranslation("frmMain", "lblUpdate", "Text3")

				If Not MyIdentity.IsSystem Then	 'we dont want to open a webpage when the app is under "System" user.
					Select Case MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text1"), Application.Settings.AppName, MessageBoxButton.YesNoCancel, MessageBoxImage.Information)
						Case MessageBoxResult.Yes
							process.Start("http://www.wagnardmobile.com")
							closeapp = True
							closeddu()
							Exit Sub
						Case MessageBoxResult.No
							MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text2"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
						Case MessageBoxResult.Cancel
							closeapp = True
							closeddu()
							Exit Sub
					End Select

				End If

			ElseIf updates = 3 Then
				lblUpdate.Content = Languages.GetTranslation("frmMain", "lblUpdate", "Text4")
			End If
		End If
	End Sub

	Private Function HasUpdates() As Integer
		If Application.Data.IsDebug Then
			Return 3
		End If

		Try
			If Not My.Computer.Network.IsAvailable Then
				Return 3
			End If
		Catch ex As Exception
		End Try

		Try
			Dim response As System.Net.WebResponse = Nothing
			Dim request As System.Net.WebRequest = System.Net.HttpWebRequest.Create("http://www.wagnardmobile.com/DDU/currentversion2.txt")
			request.Timeout = 2500

			Try
				response = request.GetResponse()
			Catch ex As Exception
				Return 3

				' >>> Link seems to be dead <<<
				' request = System.Net.HttpWebRequest.Create("http://archive.sunet.se/pub/games/PC/guru3d/ddu/currentversion2.txt")
				' request.Timeout = 2500
				' response = request.GetResponse()
			End Try



			Try
				response = request.GetResponse()
			Catch ex As Exception
				Return 3
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
			   Not Int32.TryParse(Application.Settings.AppVersion.ToString().Replace(".", ""), applicationversion) Then

				Return 3
			End If

			If newestVersion <= applicationversion Then
				Return 1
			Else
				Return 2
			End If

		Catch ex As Exception
			Application.Log.AddWarning(ex, "Checking updates failed!")
			Return 3
		End Try
	End Function

	Private Sub cleandriverstore(ByVal config As ThreadSettings)
		Dim catalog As String = ""
		Dim CurrentProvider As String = ""
		UpdateTextMethod("-Executing Driver Store cleanUP(finding OEM step)...")
		Application.Log.AddMessage("Executing Driver Store cleanUP(Find OEM)...")
		'Check the driver from the driver store  ( oemxx.inf)

		Dim deloem As New Diagnostics.ProcessStartInfo
		deloem.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
		Dim proc3 As New Diagnostics.Process

		UpdateTextMethod(UpdateTextTranslated(0))

		Select Case config.SelectedGPU
			Case GPUVendor.Nvidia
				CurrentProvider = "NVIDIA"
			Case GPUVendor.AMD
				CurrentProvider = "AdvancedMicroDevices"
			Case GPUVendor.Intel
				CurrentProvider = "Intel"
		End Select


		If config.UseSetupAPI Then
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
							catalog = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName).GetValue("Active").ToString
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
					If win8higher AndAlso (Not System.IO.File.Exists(oem.FileName)) AndAlso (Not IsNullOrWhitespace(catalog)) Then
						CleanupEngine.prePnplockdownfiles(catalog)
					End If
			Next
		Else
			For Each oem As Inf In GetOemInfList(Application.Paths.WinDir & "inf\")

				If Not oem.IsValid Then
					Continue For
				End If

				If StrContainsAny(oem.Provider, True, CurrentProvider) Or
				   oem.Provider.StartsWith("atitech", StringComparison.OrdinalIgnoreCase) Or
				   StrContainsAny(oem.Provider, True, "amd") Then

					deloem.Arguments = "dp_delete " + oem.FileName

					'We can force the OEMs removal if they are of Display or Media class.
					If StrContainsAny(oem.Class, True, "display") Or
					   StrContainsAny(oem.Class, True, "media") Then
						deloem.Arguments = "-f dp_delete " + oem.FileName
					End If
				Else
					Continue For
				End If

				'before removing the oem we try to get the original inf name (win8+)
				If win8higher Then
					Try
						catalog = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName).GetValue("Active").ToString
						catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
					Catch ex As Exception
						catalog = ""
					End Try
				End If

				'Uninstall Driver from driver store  delete from (oemxx.inf)

				Application.Log.AddMessage(deloem.Arguments)

				deloem.UseShellExecute = False
				deloem.CreateNoWindow = True
				deloem.RedirectStandardOutput = True
				'creation dun process fantome pour le wait on exit.

				proc3.StartInfo = deloem
				proc3.Start()
				reply2 = proc3.StandardOutput.ReadToEnd
				'proc3.WaitForExit()
				proc3.StandardOutput.Close()
				proc3.Close()
				UpdateTextMethod(reply2)
				Application.Log.AddMessage(reply2)
				'check if the oem was removed to process to the pnplockdownfile if necessary
				If win8higher AndAlso (Not System.IO.File.Exists(Environment.GetEnvironmentVariable("windir") & "\inf\" + oem.FileName)) AndAlso (Not IsNullOrWhitespace(catalog)) Then
					CleanupEngine.prePnplockdownfiles(catalog)
				End If
			Next
		End If

		UpdateTextMethod("-Driver Store cleanUP complete.")

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

			FileIO.Delete(filePath)

		End If

		'Delete driver files
		'delete OpenCL

		CleanupEngine.folderscleanup(IO.File.ReadAllLines(baseDir & "\settings\AMD\driverfiles.cfg")) '// add each line as String Array.



		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			FileIO.delete(filePath + "\atiogl.xml")
		Catch ex As Exception
		End Try

		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			FileIO.delete(filePath + "\ativpsrm.bin")
		Catch ex As Exception
		End Try


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
		If Directory.Exists(filePath) Then

			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ati.ace") Or
					   child.ToLower.Contains("ati catalyst control center") Or
					   child.ToLower.Contains("application profiles") Or
					   child.ToLower.EndsWith("\px") Or
					   child.ToLower.Contains("hydravision") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If



		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\ATI"
		If Directory.Exists(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("cim") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If


		filePath = Environment.GetFolderPath _
		  (Environment.SpecialFolder.ProgramFiles) + "\Common Files" + "\ATI Technologies"
		If Directory.Exists(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("multimedia") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.ProgramFiles) + "\AMD APP"
		If Directory.Exists(filePath) Then

			FileIO.Delete(filePath)

		End If

		If IntPtr.Size = 8 Then

			filePath = Environment.GetFolderPath _
			  (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"
			If Directory.Exists(filePath) Then
				Try
					For Each child As String In Directory.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ati.ace") Or
							 child.ToLower.Contains("ati catalyst control center") Or
							 child.ToLower.Contains("application profiles") Or
							 child.ToLower.EndsWith("\px") Or
							 child.ToLower.Contains("hydravision") Then

								FileIO.Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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

					FileIO.Delete(files(i))

				End If
			Next

			filePath = Environment.GetFolderPath _
			   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoFirefox"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoChrome"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If

			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies"
			If Directory.Exists(filePath) Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If child.ToLower.Contains("multimedia") Then

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
		If Directory.Exists(filePath) Then

			FileIO.Delete(filePath)

		End If


		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Catalyst Control Center"
		If Directory.Exists(filePath) Then

			FileIO.Delete(filePath)

		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\ATI"
		If Directory.Exists(filePath) Then
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ace") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		filePath = Environment.GetFolderPath _
		 (Environment.SpecialFolder.CommonApplicationData) + "\AMD"
		If Directory.Exists(filePath) Then
			For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("kdb") Or _
					   child.ToLower.Contains("fuel") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

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
			If Directory.Exists(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ace") Then

								FileIO.Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
			If Directory.Exists(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("ace") Then

								FileIO.Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
			If Directory.Exists(filePath) Then
				Try
					For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
						If IsNullOrWhitespace(child) = False Then
							If child.ToLower.Contains("cn") Or
							 child.ToLower.Contains("fuel") Or _
							 removedxcache AndAlso child.ToLower.Contains("dxcache") Or _
							 removedxcache AndAlso child.ToLower.Contains("glcache") Then

								FileIO.Delete(child)

							End If
						End If
					Next
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
		If Directory.Exists(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("amdkmpfd") Or
					 child.ToLower.Contains("cnext") Or
					 child.ToLower.Contains("steadyvideo") Or
					 child.ToLower.Contains("920dec42-4ca5-4d1d-9487-67be645cddfc") Or
					   child.ToLower.Contains("cim") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					FileIO.Delete(filePath)

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
		If Directory.Exists(filePath) Then

			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("ati.ace") Or _
					   child.ToLower.Contains("cnext") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next

			End If
		End If

		'Cleaning the CCC assemblies.


		filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_64"
		If Directory.Exists(filePath) Then
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

						FileIO.Delete(child)

					End If
				End If
			Next
		End If

		filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\GAC_MSIL"
		If Directory.Exists(filePath) Then
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

						FileIO.Delete(child)

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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", False)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
								If subregkey IsNot Nothing Then
									Using subregkey2 As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance", False)
										If subregkey2 IsNot Nothing Then
											For Each child2 As String In subregkey2.GetSubKeyNames()
												If IsNullOrWhitespace(child2) = False Then
													Using superkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance\" & child2)
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
																		deletesubregkey(My.Computer.Registry.ClassesRoot, "CLSID\" & child & "\Instance\" & child2)
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
				Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", False)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
									If subregkey IsNot Nothing Then
										Using subregkey2 As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance", False)
											If subregkey2 IsNot Nothing Then
												For Each child2 As String In subregkey2.GetSubKeyNames()
													If IsNullOrWhitespace(child2) = False Then
														Using superkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance\" & child2)
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
																			deletesubregkey(My.Computer.Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2)
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
							If IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString) Then Continue For

							If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
								Using regkey3 As RegistryKey = regkey.OpenSubKey("Categories")
									For Each child2 As String In regkey3.GetSubKeyNames
										If IsNullOrWhitespace(child2) Then Continue For
										Using regkey4 As RegistryKey = regkey.OpenSubKey("Categories\" & child2, True)
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
				Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
								If IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString) Then Continue For

								If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
									Using regkey3 As RegistryKey = regkey.OpenSubKey("Categories")
										For Each child2 As String In regkey3.GetSubKeyNames
											If IsNullOrWhitespace(child2) Then Continue For
											Using regkey4 As RegistryKey = regkey.OpenSubKey("Categories\" & child2, True)
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Record", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = regkey.OpenSubKey(child)
							If subregkey IsNot Nothing Then
								For Each childs As String In subregkey.GetSubKeyNames()
									If IsNullOrWhitespace(childs) Then Continue For

									Using regkey2 As RegistryKey = subregkey.OpenSubKey(childs, False)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Installer\Assemblies", True)
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
		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
			 "Display\shellex\PropertySheetHandlers", True)
			Try
				deletesubregkey(regkey, "ATIACE")
			Catch ex As Exception
			End Try
		End Using

		'remove opencl registry Khronos

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Khronos\OpenCL\Vendors", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos\OpenCL")
						Catch ex As Exception
						End Try
					End If
					CleanVulkan(config)
					Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Khronos", True)
						If subregkey IsNot Nothing Then
							If subregkey.GetSubKeyNames().Length = 0 Then
								Try
									deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos")
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
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
								deletesubregkey(My.Computer.Registry.LocalMachine, "Software\WOW6432Node\Khronos\OpenCL")
							Catch ex As Exception
							End Try
						End If
						Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\WOW6432Node\Khronos", True)
							If subregkey IsNot Nothing Then
								If subregkey.GetSubKeyNames().Length = 0 Then
									Try
										deletesubregkey(My.Computer.Registry.LocalMachine, "Software\WOW6432Node\Khronos")
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
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

			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\amdkmdap")
		Catch ex As Exception
		End Try

		If IntPtr.Size = 8 Then
			Try
				deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
			Catch ex As Exception
			End Try

			Try
				deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
			Catch ex As Exception
			End Try
		End If

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
		Catch ex As Exception
		End Try

		Try
			deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
		Catch ex As Exception
		End Try

		'---------------------------------------------
		'Cleaning of Legacy_AMDKMDAG+ on win7 and lower
		'---------------------------------------------

		Try
			If config.WinVersion < OSVersion.Win81 AndAlso WinForm.SystemInformation.BootMode <> WinForm.BootMode.Normal Then 'win 7 and lower + safemode only
				Application.Log.AddMessage("Cleaning LEGACY_AMDKMDAG")
				Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("SYSTEM")
					If subregkey IsNot Nothing Then
						For Each childs As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(childs) = False Then
								If childs.ToLower.Contains("controlset") Then
									Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
									  ("SYSTEM\" & childs & "\Enum\Root")
										If regkey IsNot Nothing Then
											For Each child As String In regkey.GetSubKeyNames()
												If IsNullOrWhitespace(child) = False Then
													If child.ToLower.Contains("legacy_amdkmdag") Or _
													 (child.ToLower.Contains("legacy_amdkmpfd") AndAlso config.RemoveAMDKMPFD) Or _
													 child.ToLower.Contains("legacy_amdacpksd") Then

														Try
															deletesubregkey(My.Computer.Registry.LocalMachine, "SYSTEM\" & childs & "\Enum\Root\" & child)
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
			Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
				If subregkey IsNot Nothing Then
					For Each child2 As String In subregkey.GetSubKeyNames()
						If child2.ToLower.Contains("controlset") Then
							Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
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
			Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
				If subregkey IsNot Nothing Then
					For Each child2 As String In subregkey.GetSubKeyNames()
						If IsNullOrWhitespace(child2) = False Then
							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog", True)
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
									deletesubregkey(My.Computer.Registry.LocalMachine, "SYSTEM\" & child2 & "\Services\Atierecord")
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
			 ("Directory\background\shellex\ContextMenuHandlers", True)
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
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
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
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\ATI")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies", True)
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
									Dim appproc = process.GetProcessesByName("explorer")
									For i As Integer = 0 To appproc.Length - 1
										appproc(i).Kill()
									Next i
								End If
								Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
									If regkey2 IsNot Nothing Then
										If Not IsNullOrWhitespace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then

											filePath = regkey2.GetValue("InstallDir", String.Empty).ToString
											If Not IsNullOrWhitespace(filePath) AndAlso My.Computer.FileSystem.DirectoryExists(filePath) Then
												For Each childf As String In Directory.GetDirectories(filePath)
													If IsNullOrWhitespace(childf) = False Then
														If childf.ToLower.Contains("ati.ace") Or
														childf.ToLower.Contains("cnext") Or
														childf.ToLower.Contains("amdkmpfd") Or
														childf.ToLower.Contains("cim") Then

															FileIO.Delete(childf)

														End If
													End If
												Next
												If Directory.GetDirectories(filePath).Length = 0 Then

													FileIO.Delete(filePath)

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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\ATI Technologies")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\AMD", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\AMD")
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\ATI", True)
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
								deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\ATI")
							Catch ex As Exception
							End Try
						End If
					End If
				End Using

				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\AMD", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\AMD")
						End If
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

			Try
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\ATI Technologies", True)
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
										Dim appproc = process.GetProcessesByName("explorer")
										For i As Integer = 0 To appproc.Length - 1
											Try
												appproc(i).Kill()
											Catch ex As Exception
												Application.Log.AddException(ex)
											End Try
										Next i
									End If
									Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
										If regkey2 IsNot Nothing Then
											If Not IsNullOrWhitespace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then

												filePath = regkey2.GetValue("InstallDir", String.Empty).ToString
												If Not IsNullOrWhitespace(filePath) AndAlso My.Computer.FileSystem.DirectoryExists(filePath) Then
													For Each childf As String In Directory.GetDirectories(filePath)
														If IsNullOrWhitespace(childf) Then Continue For

														If StrContainsAny(childf, True, "ati.ace", "cnext", "amdkmpfd", "cim") Then

															FileIO.Delete(childf)

														End If
													Next
													If Directory.GetDirectories(filePath).Length = 0 Then

														FileIO.Delete(filePath)

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
											If regkey.OpenSubKey(child).SubKeyCount = 0 Then
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
								deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\ATI Technologies")
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
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Microsoft\Windows\CurrentVersion\Run", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then

							Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
							  ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)

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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
								 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			 ("Software\Microsoft\Windows\CurrentVersion\Run", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			 ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("AudioEngine\AudioProcessingObjects", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
		 ("Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = regkey.OpenSubKey(child, False)
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("PROTOCOLS\Filter", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If Not IsNullOrWhitespace(child) Then
							Using subregkey As RegistryKey = regkey.OpenSubKey(child, False)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = regkey.OpenSubKey(child, False)
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
				Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\PROTOCOLS\Filter", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If Not IsNullOrWhitespace(child) Then
								Using subregkey As RegistryKey = regkey.OpenSubKey(child, False)
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
				processinfo.FileName = "lodctr"
				processinfo.Arguments = "/R"
				processinfo.WindowStyle = ProcessWindowStyle.Hidden
				processinfo.UseShellExecute = False
				processinfo.CreateNoWindow = True
				processinfo.RedirectStandardOutput = True

				process.StartInfo = processinfo
				process.Start()
				reply2 = process.StandardOutput.ReadToEnd
				process.StandardOutput.Close()
				process.Close()
				Application.Log.AddMessage(reply2)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							If child.ToLower.StartsWith("oem") AndAlso child.ToLower.EndsWith(".inf") Then
								If Not StrContainsAny(infslist, True, child) Then
									Try
										deletesubregkey(My.Computer.Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages\" & regkey.OpenSubKey(child).GetValue("Active", String.Empty).ToString)
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

				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverPackages", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Khronos", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\WOW6432Node\Khronos", True)
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
							FileIO.delete(files(i))
						Catch ex As Exception
						End Try
					End If
				Next

				files = IO.Directory.GetFiles(FilePath + "\", "vulkaninfo*.*")
				For i As Integer = 0 To files.Length - 1
					If Not IsNullOrWhitespace(files(i)) Then
						Try
							FileIO.delete(files(i))
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
								FileIO.delete(files(i))
							Catch ex As Exception
							End Try
						End If
					Next

					files = IO.Directory.GetFiles(FilePath + "\", "vulkaninfo*.*")
					For i As Integer = 0 To files.Length - 1
						If Not IsNullOrWhitespace(files(i)) Then
							Try
								FileIO.delete(files(i))
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

			FileIO.Delete(filePath)


		End If

		' here I erase the folders / files of the nvidia GFE / update in users.
		filePath = IO.Path.GetDirectoryName(config.Paths.UserPath)
		For Each child As String In Directory.GetDirectories(filePath)
			If IsNullOrWhitespace(child) = False Then
				If child.ToLower.Contains("updatususer") Then

					FileIO.Delete(child)

					FileIO.Delete(child)


					'Yes we do it 2 times. This will workaround a problem on junction/sybolic/hard link
					'(Will have to see if this is this valid. This was on old driver pre 300.xx I beleive :/ )
					FileIO.Delete(child)

					FileIO.Delete(child)

				End If
			End If
		Next

		filePath = IO.Path.GetDirectoryName(config.Paths.UserPath) + "\Public\Pictures\NVIDIA Corporation"
		If Directory.Exists(filePath) Then
			If filePath IsNot Nothing Then
				For Each child As String In Directory.GetDirectories(filePath)
					If IsNullOrWhitespace(child) = False Then
						If StrContainsAny(child, True, "3d vision experience") Then

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
							 (child.ToLower.Contains("shield apps") AndAlso config.RemoveGFE) Then


								FileIO.Delete(child)

							End If
						End If
					Next
					Try
						If Directory.GetDirectories(filePath).Length = 0 Then

							FileIO.Delete(filePath)

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

						FileIO.Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					FileIO.Delete(filePath)

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

						FileIO.Delete(child)

					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

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

						FileIO.Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					FileIO.Delete(filePath)

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
		If Directory.Exists(filePath) Then
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("control panel client") Or
					   child.ToLower.Contains("display") Or
					   child.ToLower.Contains("coprocmanager") Or
					   child.ToLower.Contains("drs") Or
					   child.ToLower.Contains("nvsmi") Or
					   child.ToLower.Contains("opencl") Or
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
					   child.ToLower.Contains("nvgsync") Or
					   child.ToLower.Contains("update core") AndAlso config.RemoveGFE Then

						FileIO.Delete(child)

					End If
					If child.ToLower.Contains("installer2") Then
						For Each child2 As String In Directory.GetDirectories(child)
							If IsNullOrWhitespace(child2) = False Then
								If child2.ToLower.Contains("display.3dvision") Or
								   child2.ToLower.Contains("display.controlpanel") Or
								   child2.ToLower.Contains("display.driver") Or
								   child2.ToLower.Contains("display.optimus") Or
								   child2.ToLower.Contains("msvcruntime") Or
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
								   child2.ToLower.Contains("hdaudio.driver") Then


									FileIO.Delete(child2)

								End If
							End If
						Next

						If Directory.GetDirectories(child).Length = 0 Then

							FileIO.Delete(child)

						Else
							For Each data As String In Directory.GetDirectories(child)
								Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
							Next

						End If
					End If
				End If
			Next
			If Directory.GetDirectories(filePath).Length = 0 Then

				FileIO.Delete(filePath)

			Else
				For Each data As String In Directory.GetDirectories(filePath)
					Application.Log.AddWarningMessage("Remaining folders found " + " : " + data)
				Next
			End If
		End If


		If config.RemoveVulkan Then
			filePath = Environment.GetFolderPath _
			 (Environment.SpecialFolder.ProgramFiles) + "\AGEIA Technologies"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If
		End If

		If config.RemoveVulkan Then
			filePath = config.Paths.ProgramFiles + "VulkanRT"
			If Directory.Exists(filePath) Then

				FileIO.Delete(filePath)

			End If
		End If

		If IntPtr.Size = 8 Then
			filePath = Environment.GetFolderPath _
			  (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
			If Directory.Exists(filePath) Then
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

								FileIO.Delete(child)

							Else
								If child.ToLower.Contains("physx") Then
									'do nothing
								Else

									FileIO.Delete(child)

								End If
							End If
						End If
					End If
				Next

				If Directory.GetDirectories(filePath).Length = 0 Then

					FileIO.Delete(filePath)

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
				If Directory.Exists(filePath) Then

					FileIO.Delete(filePath)

				End If
			End If
		End If

		If config.RemoveVulkan Then
			If IntPtr.Size = 8 Then
				filePath = Application.Paths.ProgramFilesx86 + "VulkanRT"
				If Directory.Exists(filePath) Then

					FileIO.Delete(filePath)

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

				FileIO.Delete(files(i))

			End If
		Next

		filePath = System.Environment.SystemDirectory
		files = IO.Directory.GetFiles(filePath + "\", "nvhdagenco*.*")
		For i As Integer = 0 To files.Length - 1
			If Not IsNullOrWhitespace(files(i)) Then

				FileIO.Delete(files(i))

			End If
		Next

		filePath = Environment.GetEnvironmentVariable("windir")
		Try
			FileIO.Delete(filePath + "\Help\nvcpl")
		Catch ex As Exception
		End Try

		Try
			filePath = Environment.GetEnvironmentVariable("windir") + "\Temp\NVIDIA Corporation"
			For Each child As String In Directory.GetDirectories(filePath)
				If IsNullOrWhitespace(child) = False Then
					If child.ToLower.Contains("nv_cache") Then

						FileIO.Delete(child)

					End If
				End If
			Next
			Try
				If Directory.GetDirectories(filePath).Length = 0 Then

					FileIO.Delete(filePath)

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

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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
						If child.ToLower.Contains("geforceexperienceselfupdate") AndAlso config.RemoveGFE Or _
						   child.ToLower.Contains("displaydriver") Then

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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

							FileIO.Delete(child)

						End If
					End If
				Next
				Try
					If Directory.GetDirectories(filePath).Length = 0 Then

						FileIO.Delete(filePath)

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

							If Directory.Exists(filePath) Then
								For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
									If IsNullOrWhitespace(child) = False Then
										If child.ToLower.Contains("nv_cache") Then

											FileIO.Delete(child)

										End If
									End If
								Next

								If Directory.GetDirectories(filePath).Length = 0 Then

									FileIO.Delete(filePath)

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
			If Directory.Exists(filePath) Then
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

							FileIO.Delete(child)

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
			For Each regusers As String In My.Computer.Registry.Users.GetSubKeyNames
				If IsNullOrWhitespace(regusers) Then Continue For

				Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(regusers & "\software\classes\local settings\muicache", False)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = regkey.OpenSubKey(child, False)
								If subregkey IsNot Nothing Then
									For Each childs As String In subregkey.GetSubKeyNames()
										If IsNullOrWhitespace(childs) Then Continue For

										Using regkey2 As RegistryKey = subregkey.OpenSubKey(childs, True)
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

				Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(regusers & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
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
			For Each regusers As String In My.Computer.Registry.Users.GetSubKeyNames
				If IsNullOrWhitespace(regusers) Then Continue For

				Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(regusers & "\software\classes\local settings\software\microsoft\windows\shell\muicache", True)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE", True)
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

		'Cleaning PNPRessources.  'Will fix this later, its not efficent clean at all. (Wagnard)
		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\Global", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\global")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False)
			If regkey IsNot Nothing Then
				If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False).SubKeyCount = 0 Then
					Try
						deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation")
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation", False)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		End Using

		If IntPtr.Size = 8 Then
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos", False)
				If regkey IsNot Nothing Then
					Try
						deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
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
					Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
						If subregkey IsNot Nothing Then
							For Each child2 As String In subregkey.GetSubKeyNames()
								If child2.ToLower.Contains("controlset") Then
									Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules", True)
										If regkey IsNot Nothing Then
											For Each child As String In regkey.GetValueNames()
												If IsNullOrWhitespace(child) Then Continue For

												wantedvalue = regkey.GetValue(child, String.Empty).ToString()
												If IsNullOrWhitespace(wantedvalue) Then Continue For

												If StrContainsAny(wantedvalue, True, "nvstreamsrv", "nvidia network service", "nvidia update core") Then
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
				Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
					If subregkey IsNot Nothing Then
						For Each child2 As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(child2) Then Continue For

							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
									If regkey IsNot Nothing Then
										For Each childs As String In regkey.GetSubKeyNames()
											If IsNullOrWhitespace(childs) Then Continue For

											Using regkey2 As RegistryKey = regkey.OpenSubKey(childs)
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
															Using subregkey2 As RegistryKey = regkey.OpenSubKey(childs, True)
																If subregkey2 IsNot Nothing Then
																	For Each childinsubregkey2 As String In subregkey2.GetSubKeyNames()
																		If IsNullOrWhitespace(childinsubregkey2) Then Continue For
																		Using regkey3 As RegistryKey = subregkey2.OpenSubKey(childinsubregkey2)
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
				Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
					If subregkey IsNot Nothing Then
						For Each child2 As String In subregkey.GetSubKeyNames()
							If IsNullOrWhitespace(child2) Then Continue For

							If child2.ToLower.Contains("controlset") Then
								Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			  ("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				   ("SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)

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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Khronos\OpenCL\Vendors", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos\OpenCL")
						Catch ex As Exception
						End Try
					End If
					CleanVulkan(config)
					Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Khronos", True)
						If subregkey IsNot Nothing Then
							If subregkey.GetSubKeyNames().Length = 0 Then
								Try
									deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos")
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\Khronos")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		End If


		Try
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) Then Continue For

								If StrContainsAny(child, True, "nvidia corporation") Then
									Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
										For Each child2 As String In regkey2.GetSubKeyNames()
											If IsNullOrWhitespace(child2) Then Continue For

											If StrContainsAny(child2, True, "global") Then
												If removegfe Then
													Try
														deletesubregkey(regkey2, child2)
													Catch ex As Exception
													End Try
												Else
													Using regkey3 As RegistryKey = regkey.OpenSubKey(child + "\" + child2, True)
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

					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC", True)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\ARP", True)
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

		Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(".DEFAULT\Software", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) Then Continue For

					If child.ToLower.Contains("nvidia corporation") Then
						Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
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
						Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
							For Each child2 As String In regkey2.GetSubKeyNames()
								If IsNullOrWhitespace(child2) Then Continue For

								If StrContainsAny(child2, True, "global") Then
									If removegfe Then
										Try
											deletesubregkey(regkey2, child2)
										Catch ex As Exception
										End Try
									Else
										Using regkey3 As RegistryKey = regkey2.OpenSubKey(child2, True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, "ageia technologies") Then
							If removephysx Then
								deletesubregkey(regkey, child)
							End If
						End If
						If StrContainsAny(child, True, "nvidia corporation") Then
							Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
								For Each child2 As String In regkey2.GetSubKeyNames()
									If IsNullOrWhitespace(child2) Then Continue For

									If StrContainsAny(child2, True, "global") Then
										If removegfe Then
											Try
												deletesubregkey(regkey2, child2)
											Catch ex As Exception
											End Try
										Else
											Using regkey3 As RegistryKey = regkey2.OpenSubKey(child2, True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Try
								Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			 ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
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

		Using regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
		 ("Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
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


		Using regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
		 ("Software\Microsoft\.NETFramework\SQM\Apps", True)
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
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey _
					 (users + "\Software\Microsoft\.NETFramework\SQM\Apps", True)
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

			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey _
					 (users + "\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
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
				End If
			Next

		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
		 ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
						("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
		 ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) = False Then
						Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
						 ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
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
											Using regkey2 As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
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
		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\MozillaPlugins", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\MozillaPlugins", True)
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

		Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
			If subregkey IsNot Nothing Then
				For Each child2 As String In subregkey.GetSubKeyNames()
					If IsNullOrWhitespace(child2) Then Continue For

					If child2.ToLower.Contains("controlset") Then
						Using regkey As RegistryKey = subregkey.OpenSubKey(child2 & "\Services\eventlog\Application", True)
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

		Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
			If subregkey IsNot Nothing Then
				For Each child2 As String In subregkey.GetSubKeyNames()
					If IsNullOrWhitespace(child2) Then Continue For

					If child2.ToLower.Contains("controlset") Then
						Using regkey As RegistryKey = subregkey.OpenSubKey(child2 & "\Services\eventlog\System", True)
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

		Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
			If regkey IsNot Nothing Then
				Try
					deletesubregkey(regkey, "Global")
				Catch ex As Exception
				End Try
				If regkey.SubKeyCount = 0 Then
					Using regkey2 As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE", True)
						Try
							deletesubregkey(regkey2, "NVIDIA Corporation")
						Catch ex As Exception
						End Try
					End Using
				End If
			End If
		End Using

		Try
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
						If regkey IsNot Nothing Then
							Try
								deletesubregkey(regkey, "Global")
							Catch ex As Exception
							End Try
							If regkey.SubKeyCount = 0 Then
								Using regkey2 As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
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
			For Each child As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(child) Then
					If child.ToLower.Contains("s-1-5") Then
						Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
							Try
								deletesubregkey(regkey, "Global")
								If regkey.SubKeyCount = 0 Then
									Using regkey2 As RegistryKey = My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
										Try
											deletesubregkey(regkey2, "NVIDIA Corporation")
										Catch ex As Exception
										End Try
									End Using
								End If
							Catch ex As Exception
							End Try
						End Using
					End If
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			("Software\Microsoft\Windows\CurrentVersion\Run", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
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
				deletesubregkey(My.Computer.Registry.ClassesRoot, "mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
			Try
				deletesubregkey(My.Computer.Registry.ClassesRoot, "WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
			Try
				deletesubregkey(My.Computer.Registry.ClassesRoot, "AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
			Catch ex As Exception
			End Try
		End If

		'-----------------------------
		'Shell extensions\aproved
		'-----------------------------
		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
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

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
		  "Display\shellex\PropertySheetHandlers", True)
			Try
				deletesubregkey(regkey, "NVIDIA CPL Extension")
			Catch ex As Exception
			End Try
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Extended Properties", False)
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If IsNullOrWhitespace(child) Then Continue For

					Using regkey2 As RegistryKey = regkey.OpenSubKey(child, True)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
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
		Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("Directory\background\shellex\ContextMenuHandlers", True)
			Try
				deletesubregkey(regkey, "NvCplDesktopContext")
			Catch ex As Exception
			End Try

			Try
				deletesubregkey(regkey, "00nView")
			Catch ex As Exception
			End Try
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Directory\background\shellex\ContextMenuHandlers", True)
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
		Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("jpsfile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CType(regkey.GetValue(""), String))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(My.Computer.Registry.ClassesRoot, "jpsfile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("mpofile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CStr(regkey.GetValue("")))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(My.Computer.Registry.ClassesRoot, "mpofile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("pnsfile\shell\open\command", True)
			If regkey IsNot Nothing Then
				If (Not IsNullOrWhitespace(CStr(regkey.GetValue("")))) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
					Try
						deletesubregkey(My.Computer.Registry.ClassesRoot, "pnsfile")
					Catch ex As Exception
					End Try
				End If
			End If
		End Using

		Try
			deletesubregkey(My.Computer.Registry.ClassesRoot, ".tvp")  'CrazY_Milojko
		Catch ex As Exception
		End Try

		UpdateTextMethod("-End of Registry Cleaning")

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
					FileIO.delete(files(i))
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Intel", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software", True), "Intel")
						Catch ex As Exception
						End Try
					End If
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
				If Not IsNullOrWhitespace(users) Then
					Using regkey As RegistryKey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Intel", True)
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
									deletesubregkey(My.Computer.Registry.Users.OpenSubKey(users & "\Software", True), "Intel")
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Intel", True)
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
								deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True), "Intel")
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
			("Software\Microsoft\Windows\CurrentVersion\Run", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
			 ("Directory\background\shellex\ContextMenuHandlers", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
				 ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then

								Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
								("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)

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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cpls", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR", True)
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
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify", True)
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
							deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify")
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
			Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, "ven_8086") Then
							Using subregkey As RegistryKey = regkey.OpenSubKey(child)
								If subregkey IsNot Nothing Then
									For Each childs As String In subregkey.GetSubKeyNames()
										If IsNullOrWhitespace(childs) Then Continue For

										Using regkey2 As RegistryKey = subregkey.OpenSubKey(childs, True)
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

	Private Sub restartcomputer()
		Application.Log.AddMessage("Restarting Computer ")

		Application.SaveData()

		process.Start(New ProcessStartInfo("shutdown", "/r /t 0") With
		  {
		   .WindowStyle = ProcessWindowStyle.Hidden,
		   .UseShellExecute = True,
		   .CreateNoWindow = True,
		   .RedirectStandardOutput = False
		  }
		 )
		process.WaitForExit()
		process.Close()

		closeddu()
	End Sub

	Private Sub shutdowncomputer()
		Application.Log.AddMessage("Shutdown Computer ")

		Application.SaveData()

		preventclose = False

		process.Start(New ProcessStartInfo("shutdown", "/s /t 0") With
		  {
		   .WindowStyle = ProcessWindowStyle.Hidden,
		   .UseShellExecute = True,
		   .CreateNoWindow = True,
		   .RedirectStandardOutput = False
		  }
		 )

		process.WaitForExit()
		process.Close()

		closeddu()
	End Sub

	Private Sub rescan()

		'Scan for new devices...
		If Application.Settings.UseSetupAPI Then
			Application.Log.AddMessage("Scanning for new device...")
			SetupAPI.ReScanDevices()
		Else
			Dim scan As New ProcessStartInfo
			scan.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
			scan.Arguments = "rescan"
			scan.UseShellExecute = False
			scan.CreateNoWindow = True
			scan.RedirectStandardOutput = False


			UpdateTextMethod(UpdateTextTranslated(8))
			Application.Log.AddMessage("Scanning for new device...")
			Dim proc4 As New Process
			proc4.StartInfo = scan
			proc4.Start()
			proc4.WaitForExit()
			proc4.Close()
			System.Threading.Thread.Sleep(2000)
			If Not safemode Then
				Dim appproc = process.GetProcessesByName("explorer")
				For i As Integer = 0 To appproc.Length - 1
					appproc(i).Kill()
				Next i
			End If
		End If

	End Sub

	Private Function WinUpdatePending() As Boolean
		Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
			Return (regkey IsNot Nothing)
		End Using
	End Function

	Private Function GPUIdentify() As GPUVendor
		Dim compatibleIDs() As String
		Dim isGpu As Boolean

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If IsNullOrWhitespace(child) OrElse Not StrContainsAny(child, True, "ven_8086", "ven_1002", "ven_10de") Then Continue For

						Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
							If regkey2 Is Nothing Then Continue For

							For Each child2 As String In regkey2.GetSubKeyNames
								If IsNullOrWhitespace(child2) Then Continue For

								Using regkey3 As RegistryKey = regkey2.OpenSubKey(child2)
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

	Private Sub restartinsafemode(Optional ByVal withNetwork As Boolean = False)

		SystemRestore()	'we try to do a system restore if allowed before going into safemode.
		Application.Log.AddMessage("Restarting in safemode")


		Me.Topmost = False

		Dim setbootconf As New ProcessStartInfo("bcdedit")

		If withNetwork Then
			setbootconf.Arguments = "/set safeboot network"
		Else
			setbootconf.Arguments = "/set safeboot minimal"
		End If

		setbootconf.UseShellExecute = False
		setbootconf.CreateNoWindow = True
		setbootconf.RedirectStandardOutput = False

		Dim processstopservice As New Process
		processstopservice.StartInfo = setbootconf
		processstopservice.Start()
		processstopservice.WaitForExit()
		processstopservice.Close()

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
				If regkey IsNot Nothing Then
					regkey.SetValue("*" + Application.Settings.AppName, System.Reflection.Assembly.GetExecutingAssembly().Location)
					regkey.SetValue("*UndoSM", "BCDEDIT /deletevalue safeboot")
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		processinfo.FileName = "shutdown"
		processinfo.Arguments = "/r /t 0"
		processinfo.WindowStyle = ProcessWindowStyle.Hidden
		processinfo.UseShellExecute = True
		processinfo.CreateNoWindow = True
		processinfo.RedirectStandardOutput = False

		process.StartInfo = processinfo
		process.Start()
		process.WaitForExit()
		process.Close()

		closeddu()
	End Sub

	Private Sub closeddu()
		If Not Dispatcher.CheckAccess() Then
			Dispatcher.Invoke(Sub() closeddu())
		Else
			Try
				preventclose = False

				' Me.Close()
				Application.Current.MainWindow.Close()

			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
	End Sub

#Region "frmMain Controls"

	Private Sub btnCleanRestart_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanRestart.Click

		If Not CBool(Application.Settings.GoodSite) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		EnableControls(False)

		EnableDriverSearch(False)
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = True
		SystemRestore()
		BackgroundWorker1.RunWorkerAsync(
		 New ThreadSettings() With {
		   .DoShutdown = False,
		   .DoReboot = True})
	End Sub

	Private Sub btnClean_Click(sender As Object, e As RoutedEventArgs) Handles btnClean.Click

		If Not CBool(Application.Settings.GoodSite) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		EnableControls(False)

		EnableDriverSearch(False)
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = False
		shutdown = False
		SystemRestore()
		BackgroundWorker1.RunWorkerAsync(
		 New ThreadSettings() With {
		   .DoShutdown = False,
		   .DoReboot = False})

	End Sub

	Private Sub btnCleanShutdown_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanShutdown.Click
		If Not CBool(Application.Settings.GoodSite) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			Application.Settings.GoodSite = True
		End If

		EnableControls(False)

		EnableDriverSearch(False)
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = False
		shutdown = True
		SystemRestore()
		BackgroundWorker1.RunWorkerAsync(
		 New ThreadSettings() With {
		   .DoShutdown = True,
		   .DoReboot = False})
	End Sub

	Private Sub btnWuRestore_Click(sender As Object, e As EventArgs) Handles btnWuRestore.Click
		EnableDriverSearch(True)
	End Sub


	Private Sub cbLanguage_SelectedIndexChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbLanguage.SelectionChanged
		If cbLanguage.SelectedItem IsNot Nothing Then
			InitLanguage(False, DirectCast(cbLanguage.SelectedItem, Languages.LanguageOption))
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



	Private Sub frmMain_Sourceinitialized(sender As Object, e As EventArgs) Handles MyBase.SourceInitialized
		Me.WindowState = Windows.WindowState.Minimized
	End Sub

	Private Sub frmMain_Loaded(sender As Object, e As RoutedEventArgs)
		If Me.DataContext Is Nothing Then
			Me.DataContext = Application.Data
		End If

		Try
			Dim defaultLang As Languages.LanguageOption = Languages.DefaultEng
			Dim foundLangs As List(Of Languages.LanguageOption) = Languages.ScanFolderForLang(Application.Paths.Language)

			foundLangs.Add(defaultLang)
			foundLangs.Sort(Function(x, y) x.DisplayText.CompareTo(y.DisplayText))

			For Each lang As Languages.LanguageOption In foundLangs
				Application.Settings.LanguageOptions.Add(lang)
			Next

			Application.Settings.Load()
			cbSelectedGPU.ItemsSource = [Enum].GetValues(GetType(GPUVendor))

			InitLanguage(True)

			Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)

			If Application.Settings.CheckUpdates AndAlso isElevated Then
				Me.Topmost = True
				Checkupdates2()

				Me.Topmost = False

				If closeapp Then
					Exit Sub
				End If
			End If

			If Application.Settings.ArgumentsArray IsNot Nothing AndAlso Application.Settings.ArgumentsArray.Length > 0 AndAlso Not isElevated Then
				For Each arg As String In Application.Settings.ArgumentsArray
					If StrContainsAny(arg, True, "donate", "svn", "guru3dnvidia", "guru3damd", "dduhome", "geforce", "visitoffer") Then
						Try
							System.Diagnostics.Process.Start(New ProcessStartInfo(Application.Paths.AppExeFile, arg) With {.Verb = "runas"})
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try

						Application.Current.Shutdown()
						Exit Sub
					End If
				Next
			Else

			End If

			'we check if the donate/guru3dnvidia/gugu3damd/geforce/dduhome is trigger here directly.

			Dim webAddress As String = Nothing

			If Application.Settings.VisitDonate Then
				webAddress = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"

			ElseIf Application.Settings.VisitGuru3DNvidia Then
				webAddress = "http://forums.guru3d.com/showthread.php?t=379506"

			ElseIf Application.Settings.VisitGuru3DAMD Then
				webAddress = "http://forums.guru3d.com/showthread.php?t=379505"

			ElseIf Application.Settings.VisitGeforce Then
				webAddress = "https://forums.geforce.com/default/topic/550192/geforce-drivers/wagnard-tools-ddu-gmp-tdr-manupulator-updated-01-22-2015-/"

			ElseIf Application.Settings.VisitDDUHome Then
				webAddress = "http://www.wagnardmobile.com"

			ElseIf Application.Settings.VisitSVN Then
				webAddress = "https://github.com/Wagnard/display-drivers-uninstaller"

			ElseIf Application.Settings.VisitOffer Then
				webAddress = "https://www.driverdr.com/lp/update-display-drivers.html"
			End If

			If Not IsNullOrWhitespace(webAddress) Then

				Dim process As Process = New Process() With
				 {
				  .StartInfo = New ProcessStartInfo(webAddress, Nothing) With
				  {
				   .UseShellExecute = True,
				   .CreateNoWindow = True,
				   .RedirectStandardOutput = False
				  }
				 }

				process.Start()
				'Do not put WaitForExit here. It will cause error and prevent DDU to exit.
				process.Close()
				closeddu()
				Exit Sub
			End If

			If Not Application.Data.IsDebug Then

				If Not isElevated Then
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text3"), Application.Settings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					'closeddu()
					' Restart program and run as admin
					Try
						System.Diagnostics.Process.Start(New ProcessStartInfo(Application.Paths.AppExeFile) With {.Verb = "runas"})
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try

					Application.Current.Shutdown()
					Exit Sub
				End If
			End If

			'second, we check on what we are running and set variables accordingly (os, architecture)
			Dim versionFound As Boolean = False
			Dim regOSValue As String = Nothing

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
				If regkey IsNot Nothing Then
					regOSValue = CStr(regkey.GetValue("CurrentVersion"))

					If Not IsNullOrWhitespace(regOSValue) Then
						Try
							For Each os As [Enum] In [Enum].GetValues(GetType(OSVersion))
								If GetDescription(os).Equals(regOSValue) Then
									Application.Settings.WinVersion = DirectCast(os, OSVersion)
									versionFound = (Application.Settings.WinVersion <> OSVersion.Unknown)
									Exit For
								End If
							Next
						Catch ex As Exception
							versionFound = False
						End Try
					End If
				End If
			End Using

			If Not versionFound Then		' Double check
				Select Case regOSValue
					Case "5.1" : Application.Settings.WinVersion = OSVersion.WinXP
					Case "5.2" : Application.Settings.WinVersion = OSVersion.WinXPPro_Server2003
					Case "6.0" : Application.Settings.WinVersion = OSVersion.WinVista
					Case "6.1" : Application.Settings.WinVersion = OSVersion.Win7
					Case "6.2" : Application.Settings.WinVersion = OSVersion.Win8
					Case "6.3" : Application.Settings.WinVersion = OSVersion.Win81
					Case "6.4", "10", "10.0" : Application.Settings.WinVersion = OSVersion.Win10
					Case Else : Application.Settings.WinVersion = OSVersion.Unknown
				End Select
			End If


			' https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832%28v=vs.85%29.aspx

			Select Case Application.Settings.WinVersion
				Case OSVersion.WinXP
					Application.Settings.WinVersionText = "Windows XP"
					winxp = True

				Case OSVersion.WinXPPro_Server2003
					Application.Settings.WinVersionText = "Windows XP (x64) or Server 2003"
					winxp = True

				Case OSVersion.WinVista
					Application.Settings.WinVersionText = "Windows Vista or Server 2008"

				Case OSVersion.Win7
					Application.Settings.WinVersionText = "Windows 7 or Server 2008R2"

				Case OSVersion.Win8
					Application.Settings.WinVersionText = "Windows 8 or Server 2012"
					win8higher = True

				Case OSVersion.Win81
					Application.Settings.WinVersionText = "Windows 8.1"
					win8higher = True

					Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
						If regkey IsNot Nothing Then
							Dim regValue As String = CStr(regkey.GetValue("CurrentMajorVersionNumber"))

							If Not IsNullOrWhitespace(regValue) AndAlso regValue.Equals("10") Then
								Application.Settings.WinVersion = OSVersion.Win10
								Application.Settings.WinVersionText = "Windows 10"
								win10 = True
							End If
						End If
					End Using

				Case OSVersion.Win10
					Application.Settings.WinVersionText = "Windows 10"
					win8higher = True
					win10 = True

				Case Else
					Application.Settings.WinVersionText = "Unsupported OS"
					Application.Log.AddMessage("Unsupported OS.")

					EnableControls(False)
			End Select


			Try
				'allow Paexec to run in safemode

				'  If BootMode.FailSafe Or BootMode.FailSafeWithNetwork Then ' we do this in safemode because of some Antivirus....(Kaspersky)
				Try
					Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
						Using regSubKey As RegistryKey = regkey.CreateSubKey("PAexec", RegistryKeyPermissionCheck.ReadWriteSubTree)
							regSubKey.SetValue("", "Service")
						End Using

						'regkey.CreateSubKey("PAexec")
						'regkey.OpenSubKey("Paexec", True).SetValue("", "Service")
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for PAexec!")
				End Try

				Try
					Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
						Using regSubKey As RegistryKey = regkey.CreateSubKey("PAexec", RegistryKeyPermissionCheck.ReadWriteSubTree)
							regSubKey.SetValue("", "Service")
						End Using

						'regkey.CreateSubKey("PAexec")
						'regkey.OpenSubKey("Paexec", True).SetValue("", "Service")
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex, "Failed to set '\SafeBoot\Network' RegistryKey for PAexec!")
				End Try
				'End If

				'read config file

				If closeapp Then
					Exit Sub
				End If


				'----------------------
				'check computer/os info
				'----------------------

				Dim archIs64 As Boolean


				Application.Settings.SelectedGPU = GPUVendor.Nvidia

				If IntPtr.Size = 8 Then
					archIs64 = True
					Application.Paths.CreateDirectories(Application.Paths.AppBase & "\x64")

				ElseIf IntPtr.Size = 4 Then
					archIs64 = False
					Application.Paths.CreateDirectories(Application.Paths.AppBase & "\x86")
				End If


				Application.Settings.WinIs64 = archIs64


				ddudrfolder = If(archIs64, "x64", "x86")

				If Not identity.IsSystem Then
					If archIs64 Then
						Try
							If winxp Then  'XP64
								File.WriteAllBytes(Application.Paths.AppBase & "x64\ddudr.exe", My.Resources.ddudrxp64)
							Else
								File.WriteAllBytes(Application.Paths.AppBase & "x64\ddudr.exe", My.Resources.ddudr64)
							End If

							File.WriteAllBytes(Application.Paths.AppBase & "x64\paexec.exe", My.Resources.paexec)
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					Else
						Try
							If winxp Then  'XP32
								System.IO.File.WriteAllBytes(Application.Paths.AppBase & "x86\ddudr.exe", My.Resources.ddudrxp32)
							Else 'all other 32 bits
								System.IO.File.WriteAllBytes(Application.Paths.AppBase & "x86\ddudr.exe", My.Resources.ddudr32)
							End If

							System.IO.File.WriteAllBytes(Application.Paths.AppBase & "x86\paexec.exe", My.Resources.paexec)
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					End If

					If archIs64 = True Then
						If Not File.Exists(Application.Paths.AppBase & "x64\ddudr.exe") Then
							MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text4"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Error)

							btnCleanRestart.IsEnabled = False
							btnClean.IsEnabled = False
							btnCleanShutdown.IsEnabled = False
							Exit Sub
						End If
					ElseIf archIs64 = False Then
						If Not File.Exists(Application.Paths.AppBase & "x86\ddudr.exe") Then
							MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text4"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Error)

							btnCleanRestart.IsEnabled = False
							btnClean.IsEnabled = False
							btnCleanShutdown.IsEnabled = False
							Exit Sub
						End If
					End If
				End If

				'processing arguments

				'arg = String.Join(" ", arguments, 1, arguments.Length - 1)
				'arg = arg.ToLower.Replace("  ", " ")

				'If Not IsNullOrWhitespace(settings.getconfig("arguments")) Then
				'    arg = settings.getconfig("arguments")
				'End If

				'settings.setconfig("arguments", "")

				'If Not IsNullOrWhitespace(arg) Then
				'    If Not arg = " " Then
				'        settings.setconfig("logbox", "false")
				'        settings.setconfig("systemrestore", "false")
				'        settings.setconfig("removemonitor", "false")
				'        settings.setconfig("showsafemodebox", "true")
				'        settings.setconfig("removeamdaudiobus", "false")
				'        settings.setconfig("removeamdkmpfd", "false")
				'        settings.setconfig("removegfe", "false")

				'        If arg.Contains("-silent") Then
				'            silent = True
				'            Me.WindowState = Windows.WindowState.Minimized
				'        Else
				'            Checkupdates2()
				'            If closeapp Then
				'                Exit Sub
				'            End If
				'        End If
				'        If arg.Contains("-logging") Then
				'            settings.setconfig("logbox", "true")
				'        End If
				'        If arg.Contains("-createsystemrestorepoint") Then
				'            settings.setconfig("systemrestore", "true")
				'        End If
				'        If arg.Contains("-removemonitors") Then
				'            settings.setconfig("removemonitor", "true")
				'        End If
				'        If arg.Contains("-nosafemode") Then
				'            settings.setconfig("showsafemodebox", "false")
				'        End If
				'        If arg.Contains("-restart") Then
				'            restart = True
				'        End If
				'        If arg.Contains("-removeamdaudiobus") Then
				'            settings.setconfig("removeamdaudiobus", "true")
				'        End If
				'        If arg.Contains("-removeamdkmpfd") Then
				'            settings.setconfig("removeamdkmpfd", "true")
				'        End If
				'        If arg.Contains("-removegfe") Then
				'            settings.setconfig("removegfe", "true")
				'        End If
				'        If arg.Contains("-cleanamd") Then
				'            argcleanamd = True
				'            nbclean = nbclean + 1
				'        End If
				'        If arg.Contains("-cleanintel") Then
				'            argcleanintel = True
				'            nbclean = nbclean + 1
				'        End If
				'        If arg.Contains("-cleannvidia") Then
				'            argcleannvidia = True
				'            nbclean = nbclean + 1
				'        End If
				'    End If
				'End If


				'We check if there are any reboot from windows update pending. and if so we quit.
				If WinUpdatePending() Then
					MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text14"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning)
					closeddu()
					Exit Sub
				End If

				Me.Topmost = True


				'here I check if the process is running on system user account. if not, make it so.
				If Not MyIdentity.IsSystem Then
					'This code checks to see which mode Windows has booted up in.
					Dim processstopservice As New Process
					Select Case System.Windows.Forms.SystemInformation.BootMode
						Case WinForm.BootMode.FailSafeWithNetwork, WinForm.BootMode.FailSafe
							'The computer was booted using only the basic files and drivers.
							'This is the same as Safe Mode
							safemode = True
							Me.WindowState = Windows.WindowState.Normal
							If winxp = False Then
								Dim setbcdedit As New ProcessStartInfo
								setbcdedit.FileName = "cmd.exe"
								setbcdedit.Arguments = " /CBCDEDIT /deletevalue safeboot"
								setbcdedit.UseShellExecute = False
								setbcdedit.CreateNoWindow = True
								setbcdedit.RedirectStandardOutput = False

								processstopservice.StartInfo = setbcdedit
								processstopservice.Start()
								processstopservice.WaitForExit()
								processstopservice.Close()
							End If
						Case WinForm.BootMode.Normal
							safemode = False

							If winxp = False AndAlso isElevated Then 'added iselevated so this will not try to boot into safe mode/boot menu without admin rights, as even with the admin check on startup it was for some reason still trying to gain registry access and throwing an exception --probably because there's no return
								If restart Then	 'restart command line argument
									restartinsafemode()
									Exit Sub
								Else
									If Application.Settings.ShowSafeModeMsg = True Then
										If Not silent Then
											Dim bootOption As Integer = -1 '-1 = close, 0 = normal, 1 = SafeMode, 2 = SafeMode with network
											Dim frmSafeBoot As New frmLaunch

											With frmSafeBoot
												.DataContext = Me.DataContext
												.Topmost = True
												.ShowInTaskbar = False
												.ResizeMode = Windows.ResizeMode.NoResize
												.Owner = Me
											End With



											' frmMain could be Invisible from start and shown AFTER all "processing"
											' (WPF renders UI too fast which cause 'flash' before frmLaunch on start)

											Dim launch As Boolean? = frmSafeBoot.ShowDialog()

											If launch IsNot Nothing AndAlso launch Then
												bootOption = frmSafeBoot.selection
											End If

											Select Case bootOption
												Case 0 'normal
													Exit Select
												Case 1 'SafeMode
													restartinsafemode(False)
													Exit Sub
												Case 2 'SafeMode with network
													restartinsafemode(True)
													Exit Sub
												Case Else '-1 = Close
													Me.Topmost = False
													closeddu()
													Exit Sub
											End Select
										End If
									End If
								End If
							End If
					End Select

					Topmost = False

					If Not Application.Data.IsDebug Then

						Dim stopservice As New ProcessStartInfo
						stopservice.FileName = "cmd.exe"
						stopservice.Arguments = " /Csc stop PAExec"
						stopservice.UseShellExecute = False
						stopservice.CreateNoWindow = True
						stopservice.RedirectStandardOutput = False

						processstopservice.StartInfo = stopservice
						processstopservice.Start()
						processstopservice.WaitForExit()
						processstopservice.Close()
						System.Threading.Thread.Sleep(10)

						stopservice.Arguments = " /Csc delete PAExec"

						processstopservice.StartInfo = stopservice
						processstopservice.Start()
						processstopservice.WaitForExit()
						processstopservice.Close()

						stopservice.Arguments = " /Csc interrogate PAExec"
						processstopservice.StartInfo = stopservice
						processstopservice.Start()
						processstopservice.WaitForExit()
						processstopservice.Close()

						processinfo.FileName = baseDir & "\" & ddudrfolder & "\paexec.exe"
						processinfo.Arguments = "-noname -i -s " & Chr(34) & baseDir & "\" & System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" & Chr(34) + Application.Settings.Arguments
						processinfo.UseShellExecute = False
						processinfo.CreateNoWindow = True
						processinfo.RedirectStandardOutput = False

						process.StartInfo = processinfo
						process.Start()
						'Do not add waitforexit here or DDU(current user)will not close
						process.Close()

						closeddu()
						Exit Sub
					Else
						Me.WindowState = Windows.WindowState.Normal
					End If
				Else
					Select Case System.Windows.Forms.SystemInformation.BootMode
						Case Forms.BootMode.FailSafe
							safemode = True
						Case Forms.BootMode.FailSafeWithNetwork
							safemode = True
						Case Forms.BootMode.Normal
							safemode = False
					End Select
					Me.WindowState = Windows.WindowState.Normal
				End If

				GetGPUDetails(True)

				' ----------------------------------------------------------------------------
				' Trying to get the installed GPU info 
				' (These list the one that are at least installed with minimal driver support)
				' ----------------------------------------------------------------------------

				Application.Settings.SelectedGPU = GPUIdentify()

				' -------------------------------------
				' Check if this is an AMD Enduro system
				' -------------------------------------
				Try
					Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames()
								If IsNullOrWhitespace(child) Then Continue For

								If StrContainsAny(child, True, "ven_8086") Then
									Try
										Using subRegKey As RegistryKey = regkey.OpenSubKey(child)
											For Each childs As String In subRegKey.GetSubKeyNames()
												If IsNullOrWhitespace(childs) Then Continue For

												Using childRegKey As RegistryKey = subRegKey.OpenSubKey(childs)
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

				If MyIdentity.IsSystem Then
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

			Catch ex As Exception
				Application.Log.AddException(ex)
				closeddu()
				Exit Sub
			End Try

			Topmost = False

			EnableControls(True)

			If argcleanamd Or argcleannvidia Or argcleanintel Or restart Or silent Then
				Dim trd As Thread = New Thread(AddressOf ThreadTask) With
				{
				 .CurrentCulture = New Globalization.CultureInfo("en-US"),
				 .CurrentUICulture = New Globalization.CultureInfo("en-US"),
				 .IsBackground = True
				}

				trd.Start()
			End If
		Catch ex As Exception
			Application.Log.AddException(ex, "frmMain loading caused error!")
		End Try
	End Sub

	Private Sub frmMain_ContentRendered(sender As System.Object, e As System.EventArgs) Handles MyBase.ContentRendered
		If silent Then
			Me.Hide()
		End If
	End Sub

	Private Sub frmMain_Closing(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
		If preventclose Then
			e.Cancel = True
			Exit Sub
		End If

		If MyIdentity.IsSystem AndAlso safemode Then
			Try
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
					regkey.DeleteSubKeyTree("PAexec")
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
			Try
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
					regkey.DeleteSubKeyTree("PAexec")
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Application.SaveData()
	End Sub



	Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
		Dim config As ThreadSettings = TryCast(e.Argument, ThreadSettings)

		Try
			Dim card1 As Integer = Nothing
			Dim vendid As String = ""
			Dim vendidexpected As String = ""
			Dim removegfe As Boolean = config.RemoveGFE
			Dim array() As String

			UpdateTextMethod(UpdateTextTranslated(19))

			preventclose = True

			' Application.Settings is created on MainThread = crossthread
			' Instead: use config.SelectedGPU  <-- Thread safe (actually, combobox1value not needed anymore)
			' If you need any properties,  ThreadSettings.vb <-- just put new Propery line there and assign at btnClean / btnCleanShutdown / btnCleanRestart

			'combobox1value = config.SelectedGPU.ToString()


			Select Case config.SelectedGPU
				Case GPUVendor.Nvidia
					vendidexpected = "VEN_10DE"
				Case GPUVendor.AMD
					vendidexpected = "VEN_1002"
				Case GPUVendor.Intel
					vendidexpected = "VEN_8086"
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
			If config.UseSetupAPI AndAlso config.SelectedGPU = GPUVendor.AMD Then
				Try
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", vendidexpected, True)
					If found.Count > 0 Then
						For Each SystemDevice As SetupAPI.Device In found
							For Each Sibling In SystemDevice.SiblingDevices
								If SystemDevice.LowerFilters IsNot Nothing AndAlso StrContainsAny(SystemDevice.LowerFilters(0), True, "amdkmafd") Then
									If StrContainsAny(Sibling.ClassName, True, "DISPLAY") Then
										Dim logEntry As LogEntry = Application.Log.CreateEntry()
										logEntry.Message = "Removing AMD HD Audio Bus (amdkmafd)"
										logEntry.AddDevices(SystemDevice)
										Application.Log.Add(logEntry)

										Win32.SetupAPI.UninstallDevice(SystemDevice)
									End If
								End If
							Next
						Next
					End If
				Catch ex As Exception
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					Application.Log.AddException(ex)
				End Try
			Else
				' First , get the ParentIdPrefix

				If config.RemoveAMDAudioBus AndAlso config.SelectedGPU = GPUVendor.AMD Then
					Try
						If config.SelectedGPU = GPUVendor.AMD Then
							Dim removed As Boolean = False
							Application.Log.AddMessage("Trying to remove the AMD HD Audio BUS")
							Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\HDAUDIO")
								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetSubKeyNames()
										If IsNullOrWhitespace(child) Then Continue For

										If StrContainsAny(child, True, "ven_1002") Then
											Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
												If regkey2 IsNot Nothing Then
													For Each ParentIdPrefix As String In regkey2.GetSubKeyNames
														If IsNullOrWhitespace(ParentIdPrefix) Then Continue For

														Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
															If subregkey IsNot Nothing Then
																For Each child2 As String In subregkey.GetSubKeyNames()
																	If IsNullOrWhitespace(child2) Then Continue For
																	removed = False

																	If StrContainsAny(child2, True, "ven_1002") Then
																		Using subregkey2 As RegistryKey = subregkey.OpenSubKey(child2)
																			If subregkey2 IsNot Nothing Then
																				For Each child3 As String In subregkey2.GetSubKeyNames()
																					If IsNullOrWhitespace(child3) Then Continue For

																					Using subregkey3 As RegistryKey = subregkey2.OpenSubKey(child3)
																						If subregkey3 IsNot Nothing Then
																							array = CType(subregkey3.GetValue("LowerFilters"), String())
																							If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
																								For i As Integer = 0 To array.Length - 1
																									If Not IsNullOrWhitespace(array(i)) Then
																										If array(i).ToLower.Contains("amdkmafd") AndAlso ParentIdPrefix.ToLower.Contains(subregkey3.GetValue("ParentIdPrefix", String.Empty).ToString.ToLower) Then
																											Application.Log.AddMessage("Found an AMD audio controller bus !")
																											Try
																												Application.Log.AddMessage("array result: " + array(i))
																											Catch ex As Exception
																											End Try
																											processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
																											processinfo.Arguments = "remove =system " & Chr(34) & "*" & child2 & Chr(34)
																											processinfo.UseShellExecute = False
																											processinfo.CreateNoWindow = True
																											processinfo.RedirectStandardOutput = True
																											process.StartInfo = processinfo
																											process.Start()
																											reply2 = process.StandardOutput.ReadToEnd
																											process.StandardOutput.Close()
																											process.Close()
																											Application.Log.AddMessage(reply2)
																											Application.Log.AddMessage("AMD HD Audio Bus Removed !")
																											removed = True
																										End If
																									End If
																								Next
																							End If
																							If removed Then
																								Exit For
																							End If
																						End If
																					End Using
																				Next
																			End If
																		End Using
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
						End If
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try

				End If

				'Verification is there is still an AMD HD Audio Bus device and set donotremoveamdhdaudiobusfiles to true if thats the case
				Try
					donotremoveamdhdaudiobusfiles = False
					Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
						If subregkey IsNot Nothing Then
							For Each child2 As String In subregkey.GetSubKeyNames()
								If Not IsNullOrWhitespace(child2) AndAlso child2.ToLower.Contains("ven_1002") Then
									For Each child3 As String In subregkey.OpenSubKey(child2).GetSubKeyNames()
										If IsNullOrWhitespace(child3) = False Then
											array = CType(subregkey.OpenSubKey(child2 & "\" & child3).GetValue("LowerFilters"), String())
											If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
												For i As Integer = 0 To array.Length - 1
													If Not IsNullOrWhitespace(array(i)) Then
														If array(i).ToLower.Contains("amdkmafd") Then
															Application.Log.AddWarningMessage("Found a remaining AMD audio controller bus ! Preventing the removal of its driverfiles.")
															donotremoveamdhdaudiobusfiles = True
														End If
													End If
												Next
											End If
										End If
									Next
								End If
							Next
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try

			End If



			' ----------------------
			' Removing the videocard
			' ----------------------

			If config.UseSetupAPI Then
				Try
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("display", vendidexpected)
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							'If StrContainsAny(d.HardwareIDs(0), True, vendidexpected) Then
							'SetupAPI.TEST_RemoveDevice(d.HardwareIDs(0))
							Win32.SetupAPI.UninstallDevice(d)
							'End If
						Next
					End If

				Catch ex As Exception
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					Application.Log.AddException(ex)
				End Try

			Else
				'OLD DDUDR (DEVCON Section)
				For a = 1 To 2	 'loop 2 time here for nVidia SLI pupose in normal mode.(4 may be necessary for quad SLI... need to check.)
					Try
						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames
									If Not IsNullOrWhitespace(child) AndAlso
									 (child.ToLower.Contains("ven_10de") Or
									 child.ToLower.Contains("ven_8086") Or
									 child.ToLower.Contains("ven_1002")) Then

										Using subregkey As RegistryKey = regkey.OpenSubKey(child)
											If subregkey IsNot Nothing Then

												For Each child2 As String In subregkey.GetSubKeyNames

													If subregkey.OpenSubKey(child2) Is Nothing Then
														Continue For
													End If

													array = CType(subregkey.OpenSubKey(child2).GetValue("CompatibleIDs"), String())

													If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
														For i As Integer = 0 To array.Length - 1

															If Not IsNullOrWhitespace(array(i)) AndAlso array(i).ToLower.Contains("pci\cc_03") Then

																vendid = child & "\" & child2

																If vendid.ToLower.Contains(vendidexpected.ToLower) Then
																	processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
																	processinfo.Arguments = "remove " & Chr(34) & "@pci\" & vendid & Chr(34)
																	processinfo.UseShellExecute = False
																	processinfo.CreateNoWindow = True
																	processinfo.RedirectStandardOutput = True
																	process.StartInfo = processinfo

																	process.Start()
																	reply2 = process.StandardOutput.ReadToEnd
																	process.StandardOutput.Close()
																	process.Close()
																	'process.WaitForExit()
																	Application.Log.AddMessage(reply2)
																End If
																Exit For   'the card is removed so we exit the loop from here.
															End If
														Next
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
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
					End Try
				Next
			End If

			UpdateTextMethod(UpdateTextTranslated(23))
			Application.Log.AddMessage("SetupAPI Display Driver removal: Complete.")


			cleandriverstore(config)

			UpdateTextMethod(UpdateTextTranslated(24))
			Application.Log.AddMessage("Executing DDUDR Remove Audio controler.")

			If config.UseSetupAPI Then
				Try
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendidexpected)
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							'If StrContainsAny(d.HardwareIDs(0), True, vendidexpected) Then
							SetupAPI.UninstallDevice(d)
							'End If
						Next
					End If

				Catch ex As Exception
					'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					Application.Log.AddException(ex)
				End Try
			Else
				Try
					Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\HDAUDIO")
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames
								If Not IsNullOrWhitespace(child) AndAlso
								   (child.ToLower.Contains("ven_10de") Or
								   child.ToLower.Contains("ven_8086") Or
								   child.ToLower.Contains("ven_1002")) Then

									Using subregkey As RegistryKey = regkey.OpenSubKey(child)
										If subregkey IsNot Nothing Then

											For Each child2 As String In subregkey.GetSubKeyNames

												If subregkey.OpenSubKey(child2) Is Nothing Then
													Continue For
												End If

												vendid = child & "\" & child2

												If vendid.ToLower.Contains(vendidexpected.ToLower) Then
													processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
													processinfo.Arguments = "remove " & Chr(34) & "@HDAUDIO\" & vendid & Chr(34)
													processinfo.UseShellExecute = False
													processinfo.CreateNoWindow = True
													processinfo.RedirectStandardOutput = True
													process.StartInfo = processinfo

													process.Start()
													reply2 = process.StandardOutput.ReadToEnd
													process.StandardOutput.Close()
													process.Close()
													'process.WaitForExit()
													Application.Log.AddMessage(reply2)


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
					MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
				End Try
			End If
			UpdateTextMethod(UpdateTextTranslated(25))


			Application.Log.AddMessage("DDUDR Remove Audio controler Complete.")


			If config.SelectedGPU <> GPUVendor.Intel Then
				cleandriverstore(config)
			End If


			Dim position2 As Integer = Nothing

			'Here I remove 3dVision USB Adapter.
			If config.SelectedGPU = GPUVendor.Nvidia Then
				If config.UseSetupAPI Then
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
				Else
					Try
						'removing 3DVision USB driver
						processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
						processinfo.Arguments = "findall =USB"
						processinfo.UseShellExecute = False
						processinfo.CreateNoWindow = True
						processinfo.RedirectStandardOutput = True

						'creation dun process fantome pour le wait on exit.

						process.StartInfo = processinfo
						process.Start()
						reply = process.StandardOutput.ReadToEnd
						process.StandardOutput.Close()
						process.Close()
						'process.WaitForExit()

						Try
							card1 = reply.IndexOf("USB\")
						Catch ex As Exception
						End Try

						While card1 > -1

							position2 = reply.IndexOf(":", card1)
							vendid = reply.Substring(card1, position2 - card1).Trim
							If vendid.Contains("USB\VID_0955&PID_0007") Or
							 vendid.Contains("USB\VID_0955&PID_7001") Or
							 vendid.Contains("USB\VID_0955&PID_7002") Or
							 vendid.Contains("USB\VID_0955&PID_7003") Or
							 vendid.Contains("USB\VID_0955&PID_7004") Or
							 vendid.Contains("USB\VID_0955&PID_7008") Or
							 vendid.Contains("USB\VID_0955&PID_7009") Or
							 vendid.Contains("USB\VID_0955&PID_700A") Or
							 vendid.Contains("USB\VID_0955&PID_700C") Or
							 vendid.Contains("USB\VID_0955&PID_700D&MI_00") Or
							 vendid.Contains("USB\VID_0955&PID_700E&MI_00") Then
								Application.Log.AddMessage("-" & vendid & "- 3D vision usb controler found")


								processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
								processinfo.Arguments = "remove =USB " & Chr(34) & vendid & Chr(34)
								processinfo.UseShellExecute = False
								processinfo.CreateNoWindow = True
								processinfo.RedirectStandardOutput = True
								process.StartInfo = processinfo

								process.Start()
								reply2 = process.StandardOutput.ReadToEnd
								process.StandardOutput.Close()
								process.Close()
								'process.WaitForExit()
								Application.Log.AddMessage(reply2)



							End If
							card1 = reply.IndexOf("USB\", card1 + 1)

						End While

					Catch ex As Exception
						Application.Log.AddException(ex)
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
					End Try

					UpdateTextMethod(UpdateTextTranslated(26))

					Try
						'removing NVIDIA SHIELD Wireless Controller Trackpad
						processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
						processinfo.Arguments = "findall =MOUSE"
						processinfo.UseShellExecute = False
						processinfo.CreateNoWindow = True
						processinfo.RedirectStandardOutput = True

						'creation dun process fantome pour le wait on exit.

						process.StartInfo = processinfo
						process.Start()
						reply = process.StandardOutput.ReadToEnd
						process.StandardOutput.Close()
						process.Close()
						'process.WaitForExit()

						Try
							card1 = reply.IndexOf("HID\")
						Catch ex As Exception
						End Try

						While card1 > -1

							position2 = reply.IndexOf(":", card1)
							vendid = reply.Substring(card1, position2 - card1).Trim
							If vendid.ToLower.Contains("hid\vid_0955&pid_7210") Then
								Application.Log.AddMessage("-" & vendid & "- NVIDIA SHIELD Wireless Controller Trackpad found")


								processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
								processinfo.Arguments = "remove =MOUSE " & Chr(34) & vendid & Chr(34)
								processinfo.UseShellExecute = False
								processinfo.CreateNoWindow = True
								processinfo.RedirectStandardOutput = True
								process.StartInfo = processinfo

								process.Start()
								reply2 = process.StandardOutput.ReadToEnd
								process.StandardOutput.Close()
								process.Close()
								'process.WaitForExit()

								Application.Log.AddMessage(reply2)


							End If
							card1 = reply.IndexOf("HID\", card1 + 1)

						End While

					Catch ex As Exception
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
						Application.Log.AddException(ex)
					End Try

					'Removing NVIDIA Virtual Audio Device (Wave Extensible) (WDM)
					If removegfe Then

						Try
							Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ROOT")
								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetSubKeyNames
										If Not IsNullOrWhitespace(child) Then

											Using subregkey As RegistryKey = regkey.OpenSubKey(child)
												If subregkey IsNot Nothing Then

													For Each child2 As String In subregkey.GetSubKeyNames
														If Not IsNullOrWhitespace(child2) Then
															If subregkey.OpenSubKey(child2) Is Nothing Then
																Continue For
															End If

															If Not IsNullOrWhitespace(CStr(subregkey.OpenSubKey(child2).GetValue("DeviceDesc"))) AndAlso
															   subregkey.OpenSubKey(child2).GetValue("DeviceDesc").ToString.ToLower.Contains("nvidia virtual audio device") Then

																vendid = child & "\" & child2

																processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
																processinfo.Arguments = "remove " & Chr(34) & "@ROOT\" & vendid & Chr(34)
																processinfo.UseShellExecute = False
																processinfo.CreateNoWindow = True
																processinfo.RedirectStandardOutput = True
																process.StartInfo = processinfo

																process.Start()
																reply2 = process.StandardOutput.ReadToEnd
																process.StandardOutput.Close()
																process.Close()
																'process.WaitForExit()
																Application.Log.AddMessage(reply2)


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
							MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
							Application.Log.AddException(ex)
						End Try

					End If
					' ------------------------------
					' Removing nVidia AudioEndpoints
					' ------------------------------

					Application.Log.AddMessage("Removing nVidia Audio Endpoints")


					Try
						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames
									If Not IsNullOrWhitespace(child) Then

										If Not IsNullOrWhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
										   (regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("nvidia virtual audio device") AndAlso removegfe) Or
										   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("nvidia high definition audio") Then

											vendid = child

											processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
											processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
											processinfo.UseShellExecute = False
											processinfo.CreateNoWindow = True
											processinfo.RedirectStandardOutput = True
											process.StartInfo = processinfo

											process.Start()
											reply2 = process.StandardOutput.ReadToEnd
											process.StandardOutput.Close()
											process.Close()
											'process.WaitForExit()
											Application.Log.AddMessage(reply2)


										End If
									End If
								Next
							End If
						End Using
					Catch ex As Exception
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
						Application.Log.AddException(ex)
					End Try
				End If
			End If

			If config.SelectedGPU = GPUVendor.AMD Then
				' ------------------------------
				' Removing some of AMD AudioEndpoints
				' ------------------------------
				Application.Log.AddMessage("Removing AMD Audio Endpoints")
				Try
					If config.UseSetupAPI Then
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
					Else
						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames
									If Not IsNullOrWhitespace(child) Then

										If Not IsNullOrWhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
										   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("amd high definition audio device") Or
										   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("digital audio (hdmi) (high definition audio device)") Then

											vendid = child

											processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
											processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
											processinfo.UseShellExecute = False
											processinfo.CreateNoWindow = True
											processinfo.RedirectStandardOutput = True
											process.StartInfo = processinfo

											process.Start()
											reply2 = process.StandardOutput.ReadToEnd
											process.StandardOutput.Close()
											process.Close()
											'process.WaitForExit()
											Application.Log.AddMessage(reply2)


										End If
									End If
								Next
							End If
						End Using
					End If
				Catch ex As Exception
					MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
					Application.Log.AddException(ex)
				End Try

			End If

			If config.SelectedGPU = GPUVendor.Intel Then
				If config.UseSetupAPI Then
					'Removing Intel WIdI bus Enumerator
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", Nothing, False)
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							If StrContainsAny(d.HardwareIDs(0), True, "root\iwdbus") Then  'May need to confirm this one.
								SetupAPI.UninstallDevice(d)
							End If
						Next
						found.Clear()
					End If
				Else
					'Removing Intel WIdI bus Enumerator
					Application.Log.AddMessage("Removing IWD Bus Enumerator")

					processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
					processinfo.Arguments = "remove =system " & Chr(34) & "root\iwdbus" & Chr(34)
					processinfo.UseShellExecute = False
					processinfo.CreateNoWindow = True
					processinfo.RedirectStandardOutput = True
					process.StartInfo = processinfo

					process.Start()
					reply2 = process.StandardOutput.ReadToEnd
					process.StandardOutput.Close()
					process.Close()
					'process.WaitForExit()
					Application.Log.AddMessage(reply2)



					' ------------------------------
					' Removing Intel AudioEndpoints
					' ------------------------------
					Application.Log.AddMessage("Removing Intel Audio Endpoints")

					Try
						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames
									If Not IsNullOrWhitespace(child) Then

										If Not IsNullOrWhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
										   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("intel widi") Or
										   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("intel(r)") Then

											vendid = child

											processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
											processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
											processinfo.UseShellExecute = False
											processinfo.CreateNoWindow = True
											processinfo.RedirectStandardOutput = True
											process.StartInfo = processinfo

											process.Start()
											reply2 = process.StandardOutput.ReadToEnd
											process.StandardOutput.Close()
											process.Close()
											'process.WaitForExit()
											Application.Log.AddMessage(reply2)


										End If
									End If
								Next
							End If
						End Using
					Catch ex As Exception
						MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
						Application.Log.AddException(ex)
					End Try
				End If
			End If

			Application.Log.AddMessage("ddudr Remove Audio/HDMI Complete")

			'removing monitor and hidden monitor



			If config.RemoveMonitors Then
				Application.Log.AddMessage("ddudr Remove Monitor started")
				If config.UseSetupAPI Then
					Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("monitor")
					If found.Count > 0 Then
						For Each d As SetupAPI.Device In found
							SetupAPI.UninstallDevice(d)
						Next
					End If
				Else
					Try
						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\DISPLAY")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames
									If Not IsNullOrWhitespace(child) Then

										Using subregkey As RegistryKey = regkey.OpenSubKey(child)
											If subregkey IsNot Nothing Then

												For Each child2 As String In subregkey.GetSubKeyNames
													If Not IsNullOrWhitespace(child2) Then

														If subregkey.OpenSubKey(child2) Is Nothing Then
															Continue For
														End If

														vendid = child & "\" & child2


														processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
														processinfo.Arguments = "remove " & Chr(34) & "@DISPLAY\" & vendid & Chr(34)
														processinfo.UseShellExecute = False
														processinfo.CreateNoWindow = True
														processinfo.RedirectStandardOutput = True
														process.StartInfo = processinfo

														process.Start()
														reply2 = process.StandardOutput.ReadToEnd
														process.StandardOutput.Close()
														process.Close()
														'process.WaitForExit()
													End If
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
				End If
				UpdateTextMethod(UpdateTextTranslated(27))
			End If
			UpdateTextMethod(UpdateTextTranslated(28))

			'here we set back to default the changes made by the AMDKMPFD even if we are cleaning amd or intel. We dont what that
			'espcially if we are not using an AMD GPU

			If config.RemoveAMDKMPFD Then
				If config.UseSetupAPI Then
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
				Else

					Try
						Application.Log.AddMessage("Checking and Removing AMDKMPFD Filter if present")

						Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ACPI")
							If regkey IsNot Nothing Then
								For Each child As String In regkey.GetSubKeyNames()
									If IsNullOrWhitespace(child) = False Then
										If child.ToLower.Contains("pnp0a08") Or
										   child.ToLower.Contains("pnp0a03") Then
											Using subregkey As RegistryKey = regkey.OpenSubKey(child)
												If subregkey IsNot Nothing Then
													For Each child2 As String In subregkey.GetSubKeyNames()
														If Not IsNullOrWhitespace(child2) Then
															array = CType(subregkey.OpenSubKey(child2).GetValue("LowerFilters"), String())
															If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
																For i As Integer = 0 To array.Length - 1
																	If Not IsNullOrWhitespace(array(i)) Then
																		If array(i).ToLower.Contains("amdkmpfd") Then
																			Application.Log.AddMessage("Found an AMDKMPFD! in " + child)

																			Try
																				Application.Log.AddMessage("array result: " + array(i))
																			Catch ex As Exception
																			End Try
																			processinfo.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
																			If win10 Then
																				processinfo.Arguments = "update " & config.Paths.WinDir & "inf\pci.inf " & Chr(34) & "*" & child & Chr(34)
																			Else
																				processinfo.Arguments = "update " & config.Paths.WinDir & "inf\machine.inf " & Chr(34) & "*" & child & Chr(34)
																			End If
																			processinfo.UseShellExecute = False
																			processinfo.CreateNoWindow = True
																			processinfo.RedirectStandardOutput = True
																			process.StartInfo = processinfo

																			process.Start()
																			reply2 = process.StandardOutput.ReadToEnd
																			'process.WaitForExit()
																			process.StandardOutput.Close()
																			process.Close()

																			Application.Log.AddMessage(reply2)
																			Application.Log.AddMessage(child + " Restored.")

																		End If
																	End If
																Next
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
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
				'We now try to remove the service AMDPMPFD if its lowerfilter is not found
				If reboot Or shutdown Then
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

					Dim appproc = process.GetProcessesByName("explorer")
					For i As Integer = 0 To appproc.Length - 1
						Try
							appproc(i).Kill()
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					Next i
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

					Dim appproc = process.GetProcessesByName("explorer")
					For i As Integer = 0 To appproc.Length - 1
						appproc(i).Kill()
					Next i
				End If

				cleanintelfolders()
			End If

			cleandriverstore(config)
			fixregistrydriverstore()
			'rebuildcountercache()
		Catch ex As Exception
			Application.Log.AddException(ex)
			MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButton.OK, MessageBoxImage.Error)
			stopme = True
		End Try
	End Sub

	Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
		'Scan for new hardware to not let users into a non working state.
		Try

			If stopme = True Then
				If Application.Settings.UseSetupAPI Then
					SetupAPI.ReScanDevices()
					closeddu()
					Exit Sub
				Else
					Dim scan As New ProcessStartInfo
					scan.FileName = baseDir & "\" & ddudrfolder & "\ddudr.exe"
					scan.Arguments = "rescan"
					scan.UseShellExecute = False
					scan.CreateNoWindow = True
					scan.RedirectStandardOutput = False
					Dim proc4 As New Process
					proc4.StartInfo = scan
					proc4.Start()
					proc4.WaitForExit()
					proc4.Close()
					'then quit
					closeddu()
					Exit Sub
				End If
			End If


			'For command line arguement to know if there is more cleans to be done.

			preventclose = False
			backgroundworkcomplete = True

			UpdateTextMethod(UpdateTextTranslated(9))

			Application.Log.AddMessage("Clean uninstall completed!")


			If Not shutdown Then
				rescan()
			End If

			EnableControls(True)

			If nbclean < 2 And Not silent And Not reboot And Not shutdown Then
				If MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text10"), Application.Settings.AppName, MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
					closeddu()
					Exit Sub
				End If
			End If

			If reboot Then
				restartcomputer()
			End If

			If shutdown Then
				shutdowncomputer()
			End If

		Catch ex As Exception
			preventclose = False
			Application.Log.AddException(ex)
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
			info = LogEntry.Create()

			info.Message = "System Information"
			info.Add("DDU Version", Application.Settings.AppVersion.ToString())
			info.Add("OS", Application.Settings.WinVersionText)
			info.Add("Architecture", If(Application.Settings.WinIs64, "x64", "x86"))
			info.Add(KvP.Empty)
		End If

		Try
			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If IsNullOrWhitespace(child) Then Continue For

						If Not StrContainsAny(child, True, "properties") Then

							Using subRegkey As RegistryKey = regkey.OpenSubKey(child)
								If subRegkey IsNot Nothing Then
									Dim regValue As String = CStr(subRegkey.GetValue("Device Description"))

									If Not IsNullOrWhitespace(regValue) Then
										UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
										If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)
									Else
										regValue = CStr(subRegkey.GetValue("DriverDesc"))

										If Not IsNullOrWhitespace(regValue) Then
											If subRegkey.GetValueKind("DriverDesc") = RegistryValueKind.Binary Then
												regValue = HexToString(GetREG_BINARY(regValue, "DriverDesc").Replace("00", ""))
											End If

											UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
											If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)
										End If
									End If

									regValue = CStr(subRegkey.GetValue("MatchingDeviceId"))

									If Not IsNullOrWhitespace(regValue) Then
										UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(13), regValue))
										If firstLaunch Then info.Add("GPU DeviceID", regValue)
									End If

									Try
										regValue = CStr(subRegkey.GetValue("HardwareInformation.BiosString"))

										If Not IsNullOrWhitespace(regValue) Then
											If subRegkey.GetValueKind("HardwareInformation.BiosString") = RegistryValueKind.Binary Then
												regValue = HexToString(GetREG_BINARY(subRegkey.ToString, "HardwareInformation.BiosString").Replace("00", ""))

												UpdateTextMethod(String.Format("Vbios: {0}", regValue))
												If firstLaunch Then info.Add("Vbios", regValue)
											Else
												regValue = CStr(subRegkey.GetValue("HardwareInformation.BiosString"))

												' Devmltk; missed last char (no dot at end)
												' regvalue	=	"Version 84.0.36.0.7"
												' after		=	"Version 84.00.36.00.7"
												' Should	=	"Version 84.00.36.00.07"
												'For i As Integer = 0 To 9
												'	'this is a little fix to correctly show the vbios version info
												'	regValue = regValue.Replace("." & i.ToString & ".", ".0" & i.ToString & ".")
												'Next

												Dim sb As New StringBuilder(30)
												Dim values() As String = regValue.Split(New String() {" ", "."}, StringSplitOptions.None)

												For i As Int32 = 0 To values.Length - 1
													If i = values.Length - 1 Then		'Last
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
										End If
									Catch ex As Exception
									End Try

									regValue = CStr(subRegkey.GetValue("DriverVersion"))

									If Not IsNullOrWhitespace(regValue) Then
										UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(14), regValue))
										If firstLaunch Then info.Add("Detected Driver(s) Version(s)", regValue)
									End If

									regValue = CStr(subRegkey.GetValue("InfPath"))

									If Not IsNullOrWhitespace(regValue) Then
										UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(15), regValue))
										If firstLaunch Then info.Add("INF name", regValue)
									End If

									regValue = CStr(subRegkey.GetValue("InfSection"))

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

	Private Sub EnableControls(ByVal enabled As Boolean)
		'	Me.IsEnabled = enabled

		cbSelectedGPU.IsEnabled = enabled
		ButtonsPanel.IsEnabled = enabled
		btnWuRestore.IsEnabled = enabled
		MenuStrip1.IsEnabled = enabled
	End Sub

	Private Sub ThreadTask()

		Try
			If argcleanamd Then
				backgroundworkcomplete = False
				cleananddonothing("AMD")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If argcleannvidia Then
				backgroundworkcomplete = False
				cleananddonothing("NVIDIA")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If argcleanintel Then
				backgroundworkcomplete = False
				cleananddonothing("INTEL")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If restart Then
				Application.Log.AddMessage("Restarting Computer ")
				processinfo.FileName = "shutdown"
				processinfo.Arguments = "/r /t 0"
				processinfo.WindowStyle = ProcessWindowStyle.Hidden
				processinfo.UseShellExecute = True
				processinfo.CreateNoWindow = True
				processinfo.RedirectStandardOutput = False

				process.StartInfo = processinfo
				process.Start()
				process.WaitForExit()
				process.Close()

				closeddu()
				Exit Sub
			End If

			If silent And (Not restart) Then
				closeddu()
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
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
		Dim info As LogEntry = LogEntry.Create()
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
					'	FileIO.delete(oem.FileName)  ' DOUBLE CHECK THIS before uncommentting
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

	Private Sub cleananddonothing(ByVal gpu As String)
		reboot = False
		shutdown = False
		BackgroundWorker1.RunWorkerAsync()
	End Sub

	Private Sub cleanandandreboot(ByVal gpu As String)
		reboot = True
		shutdown = False
		BackgroundWorker1.RunWorkerAsync()

	End Sub

	Private Sub EnableDriverSearch(ByVal enable As Boolean)
		Dim version As OSVersion = Application.Settings.WinVersion

		If Not enable Then
			Application.Log.AddMessage("Trying to disable search for Windows Updates", "Version", GetDescription(version))
		End If

		If version >= OSVersion.Win7 Then
			Try
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
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
				Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
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

			Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ACPI")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, "pnp0a08", "pnp0a03") Then
							Using regkey2 As RegistryKey = regkey.OpenSubKey(child)
								If regkey2 IsNot Nothing Then
									For Each child2 As String In regkey2.GetSubKeyNames()
										If IsNullOrWhitespace(child2) Then Continue For

										Using regkey3 As RegistryKey = regkey2.OpenSubKey(child2)
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

	Private Sub InitLanguage(ByVal firstLaunch As Boolean, Optional ByVal changeTo As Languages.LanguageOption = Nothing)
		'TODO: InitLanguage (just comment for quick find)

		If firstLaunch Then
			Languages.Load()		'default = english

			ExtractEnglishLangFile(Application.Paths.Language & "English.xml", Languages.DefaultEng)

			'Dim systemLang As String = Globalization.CultureInfo.InstalledUICulture.Name	'en-US, en-GB, fr-FR
			Dim systemlang = PreferredUILanguages()
			Dim lastUsedLang As Languages.LanguageOption = Nothing
			Dim nativeLang As Languages.LanguageOption = Nothing

			For Each item As Languages.LanguageOption In Application.Settings.LanguageOptions
				If lastUsedLang Is Nothing AndAlso item.Equals(Application.Settings.SelectedLanguage) Then
					lastUsedLang = item
				End If

				If nativeLang Is Nothing AndAlso systemlang.Equals(item.ISOLanguage, StringComparison.OrdinalIgnoreCase) Then
					nativeLang = item		'take native on hold incase last used language not found (avoid multiple loops)
				End If
			Next

			If lastUsedLang IsNot Nothing Then
				Application.Settings.SelectedLanguage = lastUsedLang
			Else
				If nativeLang IsNot Nothing Then
					Application.Settings.SelectedLanguage = nativeLang				'couldn't find last used, using native lang
				Else
					Application.Settings.SelectedLanguage = Languages.DefaultEng	'couldn't find last used nor native lang, using default (English)
				End If
			End If

			Languages.TranslateForm(Me, False)

		Else
			If changeTo IsNot Nothing AndAlso Not changeTo.Equals(Languages.Current) Then
				Languages.Load(changeTo)
				Languages.TranslateForm(Me)

				GetGPUDetails(False)
			End If
		End If
	End Sub

	Private Sub ExtractEnglishLangFile(ByVal fileName As String, ByVal langEng As Languages.LanguageOption)
		Using stream As Stream = Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("{0}.{1}", GetType(Languages).Namespace, "English.xml"))
			If File.Exists(fileName) Then
				Using fsEnglish As FileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None)
					If CompareStreams(stream, fsEnglish) Then
						Return
					End If
				End Using
			End If

			stream.Position = 0L

			Using sr As New StreamReader(stream, Encoding.UTF8, True)
				Using sw As New StreamWriter(fileName, False, Encoding.UTF8)
					While (sr.Peek() <> -1)
						sw.WriteLine(sr.ReadLine())
					End While

					sw.Flush()
					sw.Close()
				End Using

				sr.Close()
			End Using
		End Using
	End Sub

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
										FileIO.Delete(child2)
									Catch ex As Exception
									End Try
								End If
							End If
						Next

						If Directory.GetDirectories(child).Length = 0 Then
							Try
								FileIO.Delete(child)
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
			lbLog.Items.Add(strMessage)
			lbLog.Items.MoveCurrentToLast()
			lbLog.ScrollIntoView(lbLog.Items.CurrentItem)
		End If
	End Sub

	Public Function GetREG_BINARY(ByVal Path As String, ByVal Value As String) As String
		Dim Data() As Byte = CType(Microsoft.Win32.Registry.GetValue(Path, Value, Nothing), Byte())

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

	Private Sub RemoveSharedDlls(ByVal directory As String)
		CleanupEngine.RemoveSharedDlls(directory)
	End Sub

	Private Sub deletevalue(ByVal value1 As RegistryKey, ByVal value2 As String)
		CleanupEngine.deletevalue(value1, value2)
	End Sub

	Private Sub amdenvironementpath(ByVal filepath As String)
		Dim wantedvalue As String = Nothing

		'--------------------------------
		'System environement path cleanup
		'--------------------------------

		Application.Log.AddMessage("System environement cleanUP")
		filepath = filepath.ToLower
		Try
			Using subregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
				If subregkey IsNot Nothing Then
					For Each child2 As String In subregkey.GetSubKeyNames()
						If child2.ToLower.Contains("controlset") Then

							Using regkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetValueNames()
										If IsNullOrWhitespace(child) = False Then
											If child.Contains("Path") Then
												If IsNullOrWhitespace(CStr(regkey.GetValue(child))) = False Then
													wantedvalue = regkey.GetValue(child).ToString.ToLower
													Try
														Select Case True
															Case wantedvalue.Contains(";" + filepath & "\amd app\bin\x86_64")
																wantedvalue = wantedvalue.Replace(";" + filepath & "\amd app\bin\x86_64", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(filepath & "\amd app\bin\x86_64;")
																wantedvalue = wantedvalue.Replace(filepath & "\amd app\bin\x86_64;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + filepath & "\amd app\bin\x86")
																wantedvalue = wantedvalue.Replace(";" + filepath & "\amd app\bin\x86", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(filepath & "\amd app\bin\x86;")
																wantedvalue = wantedvalue.Replace(filepath & "\amd app\bin\x86;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + filepath & "\ati.ace\core-static")
																wantedvalue = wantedvalue.Replace(";" + filepath & "\ati.ace\core-static", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(filepath & "\ati.ace\core-static;")
																wantedvalue = wantedvalue.Replace(filepath & "\ati.ace\core-static;", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(";" + filepath & "\ati.ace\core-static")
																wantedvalue = wantedvalue.Replace(";" + filepath & "\ati.ace\core-static", "")
																regkey.SetValue(child, wantedvalue)

															Case wantedvalue.Contains(filepath & "\ati.ace\core-static;")
																wantedvalue = wantedvalue.Replace(filepath & "\ati.ace\core-static;", "")
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
	End Sub

	Private Sub RegMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RegMenuItem.Click
		MessageBox.Show(If(ACL.Registry.FixRights("HKEY_LOCAL_MACHINE\SOFTWARE\ATI"), "Fix'd!", "Fix failed"), "Test", MessageBoxButton.OK, MessageBoxImage.Information)
	End Sub

	Private Sub restoreMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles restoreMenuItem.Click
		Dim testDir As String = "E:\Program Files\NVIDIA Corporation\Installer2\NvNodejs.{238C4CE3-6554-49D2-BD02-88084DB29453}\node_modules\socket.io\node_modules\socket.io-client\node_modules\engine.io-client\node_modules\engine.io-parser\node_modules\base64-arraybuffer\lib\ddu\wagnard\does\this\work\I\think\it\should"
		Dim testDirBrokenACL As String = "E:\Program Files\NVIDIA Corporation\Installer2\NvNodejs.{238C4CE3-6554-49D2-BD02-88084DB29453}\node_modules\socket.io\node_modules\socket.io-client\node_modules\engine.io-client\node_modules\engine.io-parser\node_modules\base64-arraybuffer\lib\ddu\wagnard\does\this\work\I\think\it\should\test"
		Dim testFileShortPath As String = "E:\PROGRA~1\NVIDIA~1\INSTAL~1\NVNODE~1.{23\NODE_M~1\socket.io\NODE_M~1\SOCKET~1.IO-\NODE_M~1\ENGINE~1.IO-\NODE_M~1\ENGINE~1.IO-\NODE_M~1\BASE64~1\lib\ddu\wagnard\does\this\work\I\think\it\should\test\test.txt"

		' 288 chars, feel free to test ;P
		' Btw, change drive letter if you try!

		Try
			'	FileIO.CreateDir(testDir)			' Create dir for testing!
			'	FileIO.Delete(testFileShortPath)	' Delete file with short path
			'	FileIO.Delete(testDir)				' FileIO.Delete deletes both, is it Dir or File


			'Dim files As List(Of String) = FileIO.GetFiles("E:\_temp\test\", "*", True)
			'MessageBox.Show(String.Join(Environment.NewLine, files.ToArray()))

			FileIO.CreateDir("E:\_temp\test\more test\Some long paths\Program Files\NVIDIA Corporation\Installer2\NvNodejs.{238C4CE3-6554-49D2-BD02-88084DB29453}\node_modules\socket.io\node_modules\socket.io-client\node_modules\engine.io-client\node_modules\engine.io-parser\node_modules\base64-arraybuffer\lib\ddu\wagnard\does\this\work\I\think\it\should")


			FileIO.Delete("E:\_temp\test\")



			' Multiline TEST
			'Dim logEntry As LogEntry = Application.Log.CreateEntry()

			'logEntry.Message =
			' "|" & vbTab & "Testing Multiline" & vbTab & vbTab & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & "Second Line" & vbTab & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & vbTab & "Third Line" & vbTab & vbTab & "|" & Environment.NewLine &
			' "|" & vbTab & vbTab & vbTab & vbTab & "Fourth Line" & vbTab & "|"

			'logEntry.Add(New KvP("Numbers", String.Join(Environment.NewLine, New String() {"1", "2", "3", "4", "5", "6"})))



			'Application.Log.Add(logEntry)

			'Dim path As String = "E:\_temp\test"
			'MessageBox.Show("ExistsDir: " & FileIO.ExistsDir(path).ToString() & Environment.NewLine & "ExistsFile: " & FileIO.ExistsFile(path).ToString())

			'Dim files As List(Of String) = FileIO.GetFiles("C:\", "*", True)
			'Dim dirs As List(Of String) = FileIO.GetDirectories("C:\", "*", True)
			'Dim fileCount As Int32 = FileIO.CountFiles("C:\", "*", True)
			'Dim dirCount As Int32 = FileIO.CountDirectories("C:\", "*", True)

			'MessageBox.Show("Dirs: " & dirs.Count.ToString() & " -> " & dirCount.ToString() & vbCrLf & "Files: " & files.Count.ToString() & " -> " & fileCount.ToString())

		Catch ex As Exception
			MessageBox.Show(ex.Message, "FileIO Failure!")
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
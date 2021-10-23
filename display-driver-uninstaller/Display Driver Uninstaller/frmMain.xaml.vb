'    Display Driver Uninstaller (DDU) a driver uninstaller / Cleaner for Windows
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

Imports Microsoft.Win32
Imports System.IO
Imports System.Threading
Imports System.Security.Principal
Imports System.Reflection
Imports System.Text
Imports WinForm = System.Windows.Forms
Imports Display_Driver_Uninstaller.Win32

Public Class frmMain
	Friend Shared cleaningThread As Tasks.Task = Nothing
	Friend Shared workThread As Tasks.Task = Nothing

	Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
	Dim principal As WindowsPrincipal = New WindowsPrincipal(identity)
	Dim processinfo As New ProcessStartInfo
	Dim process As New Process

	Public Shared win8higher As Boolean = Application.Settings.WinVersion > OSVersion.Win7
	Public Shared win10 As Boolean = Application.Settings.WinVersion = OSVersion.Win10
	Public Shared win10_1809 As Boolean = Application.Settings.Win10_1809
	Public Shared winxp As Boolean = Application.Settings.WinVersion < OSVersion.WinVista
	Public Shared SharedlbLog As ListBox

	Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive").ToLower
	Dim reply As String = Nothing
	Dim reply2 As String = Nothing

	Dim CheckUpdate As New CheckUpdate
	Dim CleanupEngine As New CleanupEngine
	Dim ServiceInstaller As New ServiceInstaller
	Dim GPUCleanup As New GPUCleanup
	Dim AUDIOCleanup As New AUDIOCleanup
	Dim enduro As Boolean = False
	Public Shared donotremoveamdhdaudiobusfiles As Boolean = True

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

									compatibleIDs = TryCast(regkey3.GetValue("CompatibleIDs", String.Empty), String())

									If compatibleIDs IsNot Nothing AndAlso compatibleIDs.Length > 0 Then
										isGpu = False

										For Each id As String In compatibleIDs
											If IsNullOrWhitespace(id) Then Continue For
											If StrContainsAny(id, True, "pci\cc_03") Then
												isGpu = True
												Exit For
											End If
										Next

										If isGpu Then
											For Each id As String In compatibleIDs
												If IsNullOrWhitespace(id) Then Continue For
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

			Return GPUVendor.None
		Catch ex As Exception
			Application.Log.AddException(ex)

			MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Error)

			Return GPUVendor.None
		End Try
	End Function

	Private Sub SaveData()
		Application.SaveData()
	End Sub

	Private Sub CloseDDU()
		If Not Dispatcher.CheckAccess() Then
			Dispatcher.BeginInvoke(Sub() CloseDDU())
		Else
			SaveData()
			Try
				Close()
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
	End Sub



#Region "frmMain Controls"

	Private Sub btnCleanRestart_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanRestart.Click

		Dim config As New ThreadSettings(False)
		config.Shutdown = False
		config.Restart = True

		PreCleaning(config)
		StartThread(config)
	End Sub

	Private Sub btnClean_Click(sender As Object, e As RoutedEventArgs) Handles btnClean.Click

		Dim config As New ThreadSettings(False)
		config.Shutdown = False
		config.Restart = False

		PreCleaning(config)
		StartThread(config)
	End Sub

	Private Sub btnCleanShutdown_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanShutdown.Click

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

			'Combobox does not translate themselve, we must push the updated ItemsSource.
			cbSelectedType.ItemsSource = {Languages.GetTranslation("frmMain", "Options_Type", "Options1"), Languages.GetTranslation("frmMain", "Options_Type", "Options2"), Languages.GetTranslation("frmMain", "Options_Type", "Options3")}
			cbSelectedType.SelectedIndex = 0
		End If
	End Sub

	Private Sub imgDonate_Click(sender As Object, e As EventArgs) Handles imgDonate.Click
		WinAPI.OpenVisitLink(" -visitdonate")
	End Sub
	Private Sub imgPatron_Click(sender As Object, e As EventArgs) Handles imgPatron.Click
		WinAPI.OpenVisitLink(" -visitpatron")
	End Sub

	Private Sub VisitDDUHomepageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUHomeMenuItem.Click
		WinAPI.OpenVisitLink(" -visitdduhome")
	End Sub

	Private Sub OptionsMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OptionsMenuItem.Click
		Dim frmOptions As New frmOptions

		With frmOptions
			.Owner = Me
			.Background = Me.Background
			.DataContext = Me.DataContext
			.Icon = Me.Icon
			.SizeToContent = SizeToContent.WidthAndHeight
			.ResizeMode = ResizeMode.CanResizeWithGrip
			.WindowStartupLocation = WindowStartupLocation.CenterOwner
		End With

		frmOptions.ShowDialog()
	End Sub

	Private Sub AboutMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles AboutMenuItem.Click, ToSMenuItem.Click, TranslatorsMenuItem.Click, PatronMenuItem.Click
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
			Case StrContainsAny(menuItem.Name, True, "PatronMenuItem")
				ShowAboutWindow(4)
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
			.ResizeMode = ResizeMode.CanResizeWithGrip
			.WindowStyle = WindowStyle.SingleBorderWindow
			.WindowStartupLocation = WindowStartupLocation.CenterOwner
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

		If Application.Settings.ProcessKilled AndAlso (Not Application.LaunchOptions.Silent) AndAlso (Not Application.Settings.EnableSafeModeDialog) Then
			MessageBox.Show(Languages.GetTranslation("frmLaunch", "Messages", "Text1"), Application.Settings.AppName, Nothing, MessageBoxImage.Information)
			Application.Settings.ProcessKilled = False
		End If

		Try
			'cbSelectedGPU.ItemsSource = [Enum].GetValues(GetType(GPUVendor))
			cbSelectedType.ItemsSource = {Languages.GetTranslation("frmMain", "Options_Type", "Options1"), Languages.GetTranslation("frmMain", "Options_Type", "Options2"), Languages.GetTranslation("frmMain", "Options_Type", "Options3")}
			cbSelectedGPU.ItemsSource = {Languages.GetTranslation("frmMain", "Options_GPU", "Options1"), Languages.GetTranslation("frmMain", "Options_GPU", "Options2"), Languages.GetTranslation("frmMain", "Options_GPU", "Options3"), Languages.GetTranslation("frmMain", "Options_GPU", "Options4")} 'the order is important, check Appsettings.vb
			'cbSelectedType.ItemsSource = [Enum].GetValues(GetType(CleanType))


			cbSelectedType.SelectedIndex = 0
			If Not Application.LaunchOptions.Silent Then
				If WinForm.SystemInformation.BootMode <> Forms.BootMode.FailSafe Then
					CheckUpdate.CheckUpdates()
				End If
			End If


			' ----------------------------------------------------------------------------
			' Trying to get the installed GPU info 
			' (These list the one that are at least installed with minimal driver support)
			' ----------------------------------------------------------------------------

			GetGPUDetails(True)

			'Application.Settings.SelectedGPU = GPUIdentify()

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
										If subRegKey IsNot Nothing Then

											For Each childs As String In subRegKey.GetSubKeyNames()
												If IsNullOrWhitespace(childs) Then Continue For

												Using childRegKey As RegistryKey = MyRegistry.OpenSubKey(subRegKey, childs)
													If childRegKey IsNot Nothing Then
														Dim regValue As String = childRegKey.GetValue("Service", String.Empty).ToString

														If Not IsNullOrWhitespace(regValue) AndAlso StrContainsAny(regValue, True, "amdkmdap") Then
															enduro = True
															UpdateTextMethod("System seems to be an AMD Enduro (Intel)")
														End If
													End If
												End Using
											Next
										End If
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


			Select Case WinForm.SystemInformation.BootMode
				Case WinForm.BootMode.FailSafe
					Application.Log.AddMessage("We are in Safe Mode")
				Case WinForm.BootMode.FailSafeWithNetwork
					Application.Log.AddMessage("We are in Safe Mode with Networking")
				Case WinForm.BootMode.Normal
					Application.Log.AddWarningMessage("We are not in Safe Mode")
			End Select


			GetOemInfo()


			If Application.LaunchOptions.HasCleanArg Then
				Dim config As New ThreadSettings(True)

				workThread = New Tasks.Task(Sub() ThreadTask(config))

				workThread.Start()
			End If

		Catch ex As Exception
			Application.Log.AddException(ex, "frmMain loading caused error!")
		End Try

		If Application.Settings.FirstTimeLaunch AndAlso Not Application.LaunchOptions.Silent Then
			Microsoft.VisualBasic.MsgBox("This seems to be the first time you launch DDU." & vbNewLine &
				"Before using please know that by using DDU : " & vbNewLine & vbNewLine &
			"1- Depending on your problems and configurations, it could help or make things worse." & vbNewLine &
			"2- You should have a backup" & vbNewLine &
			"3- You should read the license, Readme and ToS." & vbNewLine &
			"4- We are not responsible for any damage or loss of data of any kind." & vbNewLine &
			"5- We are always willing to help if there is a problem")
			Dim frmOptions As New frmOptions

			With frmOptions
				.Owner = Me
				.Background = Me.Background
				.DataContext = Me.DataContext
				.Icon = Me.Icon
				.SizeToContent = SizeToContent.WidthAndHeight
				.ResizeMode = ResizeMode.CanResizeWithGrip
				.WindowStartupLocation = WindowStartupLocation.CenterOwner
			End With

			frmOptions.ShowDialog()
		End If

		If Not Application.LaunchOptions.Silent Then
			Select Case System.Windows.Forms.SystemInformation.BootMode

				Case Forms.BootMode.Normal
					Microsoft.VisualBasic.MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text8"), MsgBoxStyle.Information, Application.Settings.AppName)

			End Select
		End If

		If Application.LaunchOptions.PreventWinUpdateArg Then
			EnableDriverSearch(False)
		End If

		ImpersonateLoggedOnUser.Taketoken()
		If Not WindowsIdentity.GetCurrent().IsSystem Then
			MsgBox("Could not impersonate the SYSTEM account, it is NOT recommended to use DDU in this state.")
		End If

		If WindowsIdentity.GetCurrent().IsSystem Then
			ImpersonateLoggedOnUser.ReleaseToken()
		End If

	End Sub

	Private Sub frmMain_Closing(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
		Try
			If cleaningThread IsNot Nothing AndAlso Not cleaningThread.IsCompleted Then
				e.Cancel = True
				Exit Sub
			End If
		Catch ex As Exception
		End Try
	End Sub

#End Region

#Region "Cleaning Threads"

	Private Sub CleaningThread_Work(ByVal config As ThreadSettings)
		If Not WindowsIdentity.GetCurrent().IsSystem Then
			ImpersonateLoggedOnUser.Taketoken()
		End If

		Try
			If config Is Nothing Then
				Throw New ArgumentNullException("config", "Null ThreadSettings in CleaningWorker as e.Argument!")
			End If

			Dim card1 As Integer = Nothing
			Dim vendid As String = ""

			Dim removegfe As Boolean = config.RemoveGFE

			UpdateTextMethod(UpdateTextTranslated(19))

			Select Case config.SelectedType
				Case CleanType.GPU
					GPUCleanup.Start(config)
				Case CleanType.Audio
					AUDIOCleanup.Start(config)
			End Select

		Catch ex As Exception
			Application.Log.AddException(ex)
			Microsoft.VisualBasic.MsgBox(ex.Message + ex.StackTrace)
			config.Success = False
		Finally
			CleaningThread_Completed(config)
		End Try
	End Sub

	Private Sub CleaningThread_Completed(ByVal config As ThreadSettings)
		Try
			
			If WindowsIdentity.GetCurrent().IsSystem Then
				ImpersonateLoggedOnUser.ReleaseToken()
			End If

			Application.Log.AddMessage("Clean uninstall completed!" & CRLF & ">> GPU: " & config.SelectedGPU.ToString())

			If Not config.Success AndAlso config.GPURemovedSuccess Then
				MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), "Error!", MessageBoxButton.OK, MessageBoxImage.Error)
				Application.Log.SaveToFile()    ' Save to file
				'Scan for new hardware to not let users into a non working state.
				SetupAPI.ReScanDevices()

				CloseDDU()
				Exit Sub
			End If

			If Not config.GPURemovedSuccess Then
				MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text16"), "Error!", MessageBoxButton.OK, MessageBoxImage.Error)
				Application.Log.SaveToFile()    ' Save to file
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

			If WindowsIdentity.GetCurrent().IsSystem Then
				ImpersonateLoggedOnUser.ReleaseToken()
			End If

			EnableControls(True)

			If Not config.Silent And Not config.Restart And Not config.Shutdown Then
				If MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text10"), config.AppName, MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
					CloseDDU()
					Exit Sub
				End If
			End If

			If config.Restart Then

				'Application.RestartComputer()
				WinAPI.OpenVisitLink(" -CleanComplete -Restart")
				CloseDDU()
				Exit Sub
			End If

			If config.Shutdown Then
				'Application.ShutdownComputer()
				WinAPI.OpenVisitLink(" -CleanComplete -Shutdown")
				CloseDDU()
				Exit Sub
			End If


		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub ThreadTask(ByVal config As ThreadSettings)

		Dim autoresetevent As New AutoResetEvent(False)

		Try
			config.PreventClose = True

			PreCleaning(config)

			If config.HasCleanArg Then
				If config.CleanAmd Then
					config.Success = False
					config.SelectedType = CleanType.GPU
					config.SelectedAUDIO = AudioVendor.None
					config.SelectedGPU = GPUVendor.AMD

					StartThread(config)

					While Not cleaningThread.IsCompleted
						autoresetevent.WaitOne(200)
					End While
					cleaningThread = Nothing
				End If


				If config.CleanNvidia Then
					config.Success = False
					config.SelectedType = CleanType.GPU
					config.SelectedAUDIO = AudioVendor.None
					config.SelectedGPU = GPUVendor.Nvidia

					StartThread(config)

					While Not cleaningThread.IsCompleted
						autoresetevent.WaitOne(200)
					End While
					cleaningThread = Nothing
				End If

				If config.CleanIntel Then
					config.Success = False
					config.SelectedType = CleanType.GPU
					config.SelectedAUDIO = AudioVendor.None
					config.SelectedGPU = GPUVendor.Intel

					StartThread(config)

					While Not cleaningThread.IsCompleted
						autoresetevent.WaitOne(200)
					End While
					cleaningThread = Nothing
				End If

				If config.CleanRealtek Then
					config.Success = False
					config.SelectedType = CleanType.Audio
					config.SelectedGPU = GPUVendor.None
					config.SelectedAUDIO = AudioVendor.Realtek

					StartThread(config)

					While Not cleaningThread.IsCompleted
						autoresetevent.WaitOne(200)
					End While
					cleaningThread = Nothing
				End If

				If config.CleanSoundBlaster Then
					config.Success = False
					config.SelectedType = CleanType.Audio
					config.SelectedGPU = GPUVendor.None
					config.SelectedAUDIO = AudioVendor.SoundBlaster

					StartThread(config)

					While Not cleaningThread.IsCompleted
						autoresetevent.WaitOne(200)
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

			'EnableDriverSearch(True, True)
			SystemRestore()

		End If
	End Sub

	Private Sub StartThread(ByVal config As ThreadSettings)
		Try
			'If System.Diagnostics.Debugger.IsAttached Then          'TODO: remove when tested
			Dim logEntry As New LogEntry() With {.Message = "Used settings for cleaning!"}

			For Each p As PropertyInfo In config.GetType().GetProperties(BindingFlags.Public Or BindingFlags.Instance)
				logEntry.Add(p.Name, If(p.GetValue(config, Nothing) IsNot Nothing, p.GetValue(config, Nothing).ToString(), "-"))
			Next

			Application.Log.Add(logEntry)
			'End If

			If cleaningThread IsNot Nothing AndAlso Not cleaningThread.IsCompleted Then
				Throw New ArgumentException("cleaningThread", "Thread already exists and is busy!")
			End If

			cleaningThread = New Tasks.Task(Sub() CleaningThread_Work(config))


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
			info.Add("Win 10 1809+ ?", Application.Settings.Win10_1809.ToString())
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

										regValue = subRegkey.GetValue("DriverDesc", String.Empty).ToString()

										If Not IsNullOrWhitespace(regValue) Then
											If subRegkey.GetValueKind("DriverDesc") = RegistryValueKind.Binary Then
												regValue = HexToString(GetREG_BINARY(subRegkey, "DriverDesc").Replace("00", ""))

											Else
												regValue = subRegkey.GetValue("DriverDesc", String.Empty).ToString()
											End If
										End If

										If IsNullOrWhitespace(regValue) Then Continue For

										UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
										If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)

									End If

									regValue = subRegkey.GetValue("MatchingDeviceId", String.Empty).ToString()

									If Not IsNullOrWhitespace(regValue) Then
										UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(13), regValue))
										If firstLaunch Then info.Add("GPU DeviceID", regValue)
									End If

									Try
										regValue = subRegkey.GetValue("HardwareInformation.BiosString", String.Empty).ToString()

										If Not IsNullOrWhitespace(regValue) Then
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
										End If
									Catch ex As Exception
										Application.Log.AddException(ex)
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
				cbLanguage.IsEnabled = enabled          'Selecting this at runtime maybe not good idea.. ;)
				cbSelectedGPU.IsEnabled = enabled
				ButtonsPanel.IsEnabled = enabled
				btnWuRestore.IsEnabled = enabled
				MenuStrip1.IsEnabled = enabled
			End If

		End If
	End Sub

	Private Sub SystemRestore()
		If Application.LaunchOptions.NoRestorePoint Then
			Exit Sub
		End If

		If Application.Settings.CreateRestorePoint AndAlso System.Windows.Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then
			Dim frmSystemRestore As New frmSystemRestore

			With frmSystemRestore
				.WindowStartupLocation = WindowStartupLocation.CenterOwner
				.Background = Me.Background
				.Owner = Me
				.DataContext = Me.DataContext
				.ResizeMode = ResizeMode.NoResize
				.WindowStyle = WindowStyle.ToolWindow
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

	Public Shared Sub EnableDriverSearch(ByVal enable As Boolean)
		Dim version As OSVersion = Application.Settings.WinVersion

		If Not enable Then
			Application.Log.AddMessage("Trying to disable search for Windows Updates", "Version", GetDescription(version))
		End If

		If version >= OSVersion.Win7 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
					If regkey IsNot Nothing Then
						Dim regValue As Int32 = CInt(regkey.GetValue("SearchOrderConfig", Nothing))

						If regValue <> If(enable, 1, 0) Then
							regkey.SetValue("SearchOrderConfig", If(enable, 1, 0), RegistryValueKind.DWord)

							If Not Application.LaunchOptions.Silent Then
								If enable Then
									MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text11"))
								Else
									MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text9"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
								End If
							End If
						ElseIf enable <> False AndAlso Not Application.LaunchOptions.Silent Then
							MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text15"))
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
					If regkey IsNot Nothing Then
						Dim regValue As Int32 = CInt(regkey.GetValue("DontSearchWindowsUpdate", Nothing))

						If regkey IsNot Nothing Then
							If regValue <> If(enable, 0, 1) Then
								regkey.SetValue("DontSearchWindowsUpdate", If(enable, 0, 1), RegistryValueKind.DWord)

								If Not Application.LaunchOptions.Silent Then
									If enable Then
										MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text11"))
									Else
										MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text9"), Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
									End If
								End If
							ElseIf enable <> False AndAlso Not Application.LaunchOptions.Silent Then
								MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text15"))
							End If
						End If
					End If
				End Using

			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
	End Sub

	Public Shared Function InfoDriverSearch() As Boolean
		Dim version As OSVersion = Application.Settings.WinVersion
		Dim regValue As Int32
		Dim response As Boolean
		If version >= OSVersion.Win7 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
					If regkey IsNot Nothing Then
						regValue = CInt(regkey.GetValue("SearchOrderConfig", 1))
						If regValue = 0 Then
							response = True
						Else
							response = False
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
					If regkey IsNot Nothing Then
						regValue = CInt(regkey.GetValue("DontSearchWindowsUpdate", 0))
						If regValue = 1 Then
							response = True
						Else
							response = False
						End If
					End If
				End Using

			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If
		Return response
	End Function

	Public Shared Function UpdateTextTranslated(ByVal number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
	End Function

	Public Function UpdateTextEnglish(ByVal number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1), True)
	End Function

	Public Shared Sub UpdateTextMethod(ByVal strMessage As String)
		If Not SharedlbLog.Dispatcher.CheckAccess() Then
			SharedlbLog.Dispatcher.Invoke(Sub() UpdateTextMethod(strMessage))
		Else
			SharedlbLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " - " + strMessage)
			SharedlbLog.SelectedIndex = SharedlbLog.Items.Count - 1
			SharedlbLog.ScrollIntoView(SharedlbLog.SelectedItem)
		End If
	End Sub

	Private Function GetREG_BINARY(ByVal Path As RegistryKey, ByVal Value As String) As String
		Dim Data() As Byte = CType(Microsoft.Win32.Registry.GetValue(Path.ToString, Value, Nothing), Byte())

		If Data Is Nothing Then Return "N/A"

		Dim Result As String = String.Empty

		For j As Integer = 0 To Data.Length - 1
			Result &= Hex(Data(j)).PadLeft(2, "0"c) & ""
		Next

		Return Result
	End Function

	Private Function HexToString(ByVal Data As String) As String
		Dim com As String = ""

		For x = 0 To Data.Length - 1 Step 2
			com &= ChrW(CInt("&H" & Data.Substring(x, 2)))
		Next

		Return com
	End Function

	Private Sub StartService(ByVal service As String)
		ServiceInstaller.StartService(service)
	End Sub
	Private Function CheckServiceStartupType(ByVal service As String) As String
		Return CleanupEngine.CheckServiceStartupType(service)
	End Function

	Private Sub SetServiceStartupType(ByVal service As String, value As String)
		CleanupEngine.SetServiceStartupType(service, value)
	End Sub
	Private Sub StopService(ByVal service As String)
		ServiceInstaller.StopService(service)
	End Sub

	' "Universal" solution, can be used for Nvidia/Intel too


	Private Sub testing2MenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles testingMenuItem.Click
		' TESTING / LOGGING (FILE)

		Dim sfd As New SaveFileDialog() With
		{
		 .Title = "Select file for tasklist output",
		 .Filter = "Txt files (*.txt)|*.txt",
		 .FilterIndex = 1,
		 .AddExtension = True,
		 .DefaultExt = ".txt"
		}

		Dim delete As Boolean = (MessageBox.Show("Delete tasks?" & CRLF & "Yes = Ask delete for each task" & CRLF & "No = Just save to file", "Question", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes)

		If sfd.ShowDialog() = True Then

			Using sw As New StreamWriter(sfd.FileName, False, Encoding.UTF8)

				Using tsc As New TaskSchedulerControl(New ThreadSettings(False))
					For Each task As Task In tsc.GetAllTasks()

						sw.WriteLine("Name:  ".PadLeft(14, " "c) & task.Name)
						sw.WriteLine("Path:  ".PadLeft(14, " "c) & task.Path)
						sw.WriteLine("Enabled:  ".PadLeft(14, " "c) & If(task.Enabled, "Yes", "No"))
						sw.WriteLine("State:  ".PadLeft(14, " "c) & task.State.ToString())

						If task.Author IsNot Nothing Then sw.WriteLine("Author:  ".PadLeft(14, " "c) & task.Author)
						If task.Description IsNot Nothing Then sw.WriteLine("Description:  ".PadLeft(14, " "c) & task.Description)

						If delete Then
							Select Case MessageBox.Show("Task:" & CRLF & task.Name & CRLF & task.Description & CRLF & CRLF & "Delete?", "Delete task?", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)
								Case MessageBoxResult.Yes
									'	task.Delete()  ' USE WITH CAUTION! 

									'	task.Enabled = Not task.Enabled	   ' enable / disable

									'	If task.State = TaskStates.Running Then	'Start/Stop
									'		task.Stop()
									'	Else
									'		task.Start()
									'	End If

								Case MessageBoxResult.No
									Continue For
								Case MessageBoxResult.Cancel
									Exit For
							End Select
						End If

						sw.WriteLine("")

					Next
				End Using
			End Using
		End If
	End Sub

	Private Sub checkXMLMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles checkXMLMenuItem.Click
		Dim current As Languages.LanguageOption = Application.Settings.SelectedLanguage

		Languages.CheckLanguageFiles()

		Languages.Load(current)
	End Sub

	Private Sub SetupAPIMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles SetupAPIMenuItem.Click
		Dim testWindow As New DebugWindow

		testWindow.ShowDialog()
	End Sub

	Private Sub cbSelectedGPU_Changed(sender As Object, e As SelectionChangedEventArgs) Handles cbSelectedGPU.SelectionChanged

		Select Case cbSelectedType.SelectedIndex

			Case CleanType.None
				Application.Settings.SelectedGPU = GPUVendor.None
				Application.Settings.SelectedAUDIO = AudioVendor.None
				ButtonsPanel.IsEnabled = False
			Case CleanType.Audio
				Select Case cbSelectedGPU.SelectedIndex

					Case 0
						Application.Settings.SelectedGPU = GPUVendor.None
						Application.Settings.SelectedAUDIO = AudioVendor.None
						ButtonsPanel.IsEnabled = False
					Case 1
						Application.Settings.SelectedAUDIO = AudioVendor.Realtek
						cbSelectedGPU.IsEnabled = True
						ButtonsPanel.IsEnabled = True
					Case 2
						Application.Settings.SelectedAUDIO = AudioVendor.SoundBlaster
						cbSelectedGPU.IsEnabled = True
						ButtonsPanel.IsEnabled = True

				End Select
			Case CleanType.GPU

				Select Case cbSelectedGPU.SelectedIndex

					Case 0
						Application.Settings.SelectedGPU = GPUVendor.None
						Application.Settings.SelectedAUDIO = AudioVendor.None
						ButtonsPanel.IsEnabled = False
					Case 1
						Application.Settings.SelectedGPU = GPUVendor.Nvidia
						cbSelectedGPU.IsEnabled = True
						ButtonsPanel.IsEnabled = True
					Case 2
						Application.Settings.SelectedGPU = GPUVendor.AMD
						cbSelectedGPU.IsEnabled = True
						ButtonsPanel.IsEnabled = True
					Case 3
						Application.Settings.SelectedGPU = GPUVendor.Intel
						cbSelectedGPU.IsEnabled = True
						ButtonsPanel.IsEnabled = True
				End Select

		End Select

	End Sub

	Private Sub cbSelectedType_Changed(sender As Object, e As SelectionChangedEventArgs) Handles cbSelectedType.SelectionChanged

		Select Case cbSelectedType.SelectedIndex
			Case 0
				Application.Settings.SelectedType = CleanType.None
				cbSelectedGPU.IsEnabled = True
				cbSelectedGPU.ItemsSource = {Languages.GetTranslation("frmMain", "Options_GPU", "Options1"), Languages.GetTranslation("frmMain", "Options_GPU", "Options2"), Languages.GetTranslation("frmMain", "Options_GPU", "Options3"), Languages.GetTranslation("frmMain", "Options_GPU", "Options4")} 'the order is important, check Appsettings.vb
				cbSelectedGPU.SelectedIndex = 0
				cbSelectedGPU.IsEnabled = False


			Case 1
				Application.Settings.SelectedType = CleanType.Audio
				cbSelectedGPU.IsEnabled = True
				cbSelectedGPU.ItemsSource = {Languages.GetTranslation("frmMain", "Options_AUDIO", "Options1"), Languages.GetTranslation("frmMain", "Options_AUDIO", "Options2"), Languages.GetTranslation("frmMain", "Options_AUDIO", "Options3")}  ' the order is important, check Appsettings.vb
				cbSelectedGPU.SelectedIndex = 0

			Case 2
				Application.Settings.SelectedType = CleanType.GPU
				cbSelectedGPU.IsEnabled = True
				cbSelectedGPU.ItemsSource = {Languages.GetTranslation("frmMain", "Options_GPU", "Options1"), Languages.GetTranslation("frmMain", "Options_GPU", "Options2"), Languages.GetTranslation("frmMain", "Options_GPU", "Options3"), Languages.GetTranslation("frmMain", "Options_GPU", "Options4")} 'the order is important, check Appsettings.vb
				cbSelectedGPU.SelectedIndex = 0
				cbSelectedGPU.SelectedIndex = GPUIdentify()
		End Select

	End Sub
	Private Sub Cleandriverstore(ByVal config As ThreadSettings)
		CleanupEngine.Cleandriverstore(config)
	End Sub

	Private Sub frmMain_Initialized(sender As Object, e As EventArgs) Handles MyBase.Initialized
		SharedlbLog = lbLog
	End Sub

	Private Sub lblOffer_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles lblOffer.MouseDown
		WinAPI.OpenVisitLink(" -visitoffer")
	End Sub
End Class

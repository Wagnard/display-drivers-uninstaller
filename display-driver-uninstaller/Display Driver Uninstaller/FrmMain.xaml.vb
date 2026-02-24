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

Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Security.Principal
Imports System.Text
Imports System.Threading.Tasks
Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32
Imports WinForm = System.Windows.Forms

Namespace Display_Driver_Uninstaller

	Public Class FrmMain
		Private Shared _cleaningTask As Task = Nothing
		Private Shared _workTask As Task = Nothing

		Private Shared _isWindows8OrHigher As Boolean = Application.Settings.WinVersion > OSVersion.Win7
		Private Shared _isWindows10 As Boolean = Application.Settings.WinVersion = OSVersion.Win10
		Private Shared _isWindows10_1809 As Boolean = Application.Settings.Win10_1809
		Private Shared _isWindowsXp As Boolean = Application.Settings.WinVersion < OSVersion.WinVista
		Private Shared _sharedLogBox As ListBox
		Private Shared _donotremoveamdhdaudiobusfiles As Boolean = True
		Private Shared _intelNpuPresent As Boolean = IsIntelNpuPresent()

		Private _checkUpdate As New CheckUpdate
		Private _cleanupEngine As New CleanupEngine
		Private _serviceInstaller As New ServiceInstaller
		Private _gpuCleanup As New GPUCleanup
		Private _audioCleanup As New AUDIOCleanup
        Private _enduro As Boolean = False
        Private isUpdatingComboBox As Boolean = False

        Friend Shared Property CleaningTask As Task
			Get
				Return _cleaningTask
			End Get
			Set(value As Task)
				_cleaningTask = value
			End Set
		End Property

		Friend Shared Property WorkTask As Task
			Get
				Return _workTask
			End Get
			Set(value As Task)
				_workTask = value
			End Set
		End Property

		Public Shared Property DoNotRemoveAmdHdAudioBusFiles As Boolean
			Get
				Return _donotremoveamdhdaudiobusfiles
			End Get
			Set(value As Boolean)
				_donotremoveamdhdaudiobusfiles = value
			End Set
		End Property

		Public Shared Property IntelNpuPresent As Boolean
			Get
				Return _intelNpuPresent
			End Get
			Set(value As Boolean)
				_intelNpuPresent = value
			End Set
		End Property

		Public Shared ReadOnly Property IsWindows8OrHigher As Boolean
			Get
				Return _isWindows8OrHigher
			End Get
		End Property

		Public Shared ReadOnly Property IsWindows10 As Boolean
			Get
				Return _isWindows10
			End Get
		End Property

		Public Shared ReadOnly Property IsWindows10_1809 As Boolean
			Get
				Return _isWindows10_1809
			End Get
		End Property

		Public Shared ReadOnly Property IsWindowsXp As Boolean
			Get
				Return _isWindowsXp
			End Get
		End Property

		Public Shared Property SharedLogBox As ListBox
			Get
				Return _sharedLogBox
			End Get
			Set(value As ListBox)
				_sharedLogBox = value
			End Set
		End Property

		Private Function GPUIdentify() As GPUVendor
			Dim compatibleIDs() As String
			Dim isGpu As Boolean

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\PCI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If String.IsNullOrWhiteSpace(child) OrElse Not StrContainsAny(child, True, "ven_8086", "ven_1002", "ven_10de") Then Continue For

							Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
								If regkey2 Is Nothing Then Continue For

								For Each child2 As String In regkey2.GetSubKeyNames
									If String.IsNullOrWhiteSpace(child2) Then Continue For

									Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
										If regkey3 Is Nothing Then Continue For

										compatibleIDs = TryCast(regkey3.GetValue("CompatibleIDs", String.Empty), String())

										If compatibleIDs IsNot Nothing AndAlso compatibleIDs.Length > 0 Then
											isGpu = False

											For Each id As String In compatibleIDs
												If String.IsNullOrWhiteSpace(id) Then Continue For
												If StrContainsAny(id, True, "pci\cc_03") Then
													isGpu = True
													Exit For
												End If
											Next

											If isGpu Then
												For Each id As String In compatibleIDs
													If String.IsNullOrWhiteSpace(id) Then Continue For
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
				Return
			End If

			SaveData()
			Try
				Close()
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End Sub



#Region "frmMain Controls"

		Private Async Sub BtnCleanRestart_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanRestart.Click

			Dim config As New ThreadSettings(False)
			config.Shutdown = False
			config.Restart = True

			KillGPUStatsProcesses()

			Await ThreadTaskAsync(config)
		End Sub

		Private Async Sub BtnClean_Click(sender As Object, e As RoutedEventArgs) Handles btnClean.Click

			Dim config As New ThreadSettings(False)
			config.Shutdown = False
			config.Restart = False

			KillGPUStatsProcesses()

			Await ThreadTaskAsync(config)
		End Sub

		Private Async Sub BtnCleanShutdown_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanShutdown.Click

			Dim config As New ThreadSettings(False)
			config.Shutdown = True
			config.Restart = False

			KillGPUStatsProcesses()

			Await ThreadTaskAsync(config)
		End Sub

		Private Async Sub BtnCleanCaches_Click(sender As Object, e As RoutedEventArgs) Handles btnCleanCaches.Click

			Dim config As New ThreadSettings(False)
			config.CleanCache = True
			Await ThreadTaskAsync(config)
		End Sub

		Private Sub BtnWuRestore_Click(sender As Object, e As EventArgs) Handles btnWuRestore.Click
			EnableDriverSearch(True)
		End Sub

		Private Sub CbLanguage_SelectedIndexChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbLanguage.SelectionChanged
            If Application.Settings.SelectedLanguage IsNot Nothing Then
                isUpdatingComboBox = True
                Languages.Load(Application.Settings.SelectedLanguage)

                If StrContainsAny(Application.Settings.SelectedLanguage.ToString(), True, "he-il", "fa-ir", "ar-ye") Then
                    Application.Settings.FlowControl = FlowDirection.RightToLeft
                Else
                    Application.Settings.FlowControl = FlowDirection.LeftToRight
                End If

                Languages.TranslateForm(Me)

                GetGPUDetails(False)

                Dim index As Integer = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedTypeIndex, cbSelectedType.SelectedIndex)
                Dim gpuIndex = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedGPUIndex, cbSelectedGPU.SelectedIndex)

                cbSelectedType.ItemsSource = {
                    Languages.GetTranslation("frmMain", "Options_Type", "Options1"),
                    Languages.GetTranslation("frmMain", "Options_Type", "Options2"),
                    Languages.GetTranslation("frmMain", "Options_Type", "Options3")
                }

                cbSelectedType.SelectedIndex = index

                Select Case index
                    Case 1 ' Audio
                        cbSelectedGPU.ItemsSource = {
                    Languages.GetTranslation("frmMain", "Options_AUDIO", "Options1"),
                    Languages.GetTranslation("frmMain", "Options_AUDIO", "Options2"),
                    Languages.GetTranslation("frmMain", "Options_AUDIO", "Options3")
                }
                    Case 2 ' GPU
                        cbSelectedGPU.ItemsSource = {
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options1"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options2"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options3"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options4"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options5")
                }
                    Case Else ' None
                        cbSelectedGPU.ItemsSource = {
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options1"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options2"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options3"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options4"),
                    Languages.GetTranslation("frmMain", "Options_GPU", "Options5")
                }
                End Select

                cbSelectedGPU.SelectedIndex = gpuIndex

                isUpdatingComboBox = False
            End If
        End Sub

        Private Sub ImgDonate_Click(sender As Object, e As EventArgs) Handles imgDonate.Click
            WinAPI.OpenVisitLink(" -visitdonate")
        End Sub

        Private Sub ImgPatron_Click(sender As Object, e As EventArgs) Handles imgPatron.Click
            WinAPI.OpenVisitLink(" -visitpatron")
        End Sub

        Private Sub ImgDiscord_Click(sender As Object, e As EventArgs) Handles imgDiscord.Click
			WinAPI.OpenVisitLink(" -visitdiscord")
		End Sub

        Private Sub VisitDDUShopPageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUShopMenuItem.Click
            WinAPI.OpenVisitLink(" -visitddushop")
        End Sub

        Private Sub VisitDDUHomepageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUHomeMenuItem.Click
            WinAPI.OpenVisitLink(" -visitdduhome")
        End Sub

        Private Sub OptionsMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OptionsMenuItem.Click
			Dim frmOptions As New FrmOptions

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

		'Private Sub VisitGeforceMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles VisitGeforceMenuItem.Click
		'	WinAPI.OpenVisitLink(" -visitgeforce")
		'End Sub

		Private Sub ExtendedLogMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ExtendedLogMenuItem.Click
			Dim frmLog As New FrmLog

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

		Private Sub ImgOffer_Click(sender As Object, e As RoutedEventArgs) Handles imgOffer.Click
			WinAPI.OpenVisitLink(" -visitoffer")
		End Sub

#End Region

#Region "frmMain Events"

		Private Sub FrmMain_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded

            Languages.TranslateForm(Me, False)

            isUpdatingComboBox = True

            Dim typeIndex As Integer = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedTypeIndex, 0)
            cbSelectedType.SelectedIndex = typeIndex


            Select Case typeIndex
                Case 0, -1 ' None
                    Application.Settings.SelectedType = CleanType.None
                    Application.Settings.SelectedGPU = GPUVendor.None
                    Application.Settings.SelectedAUDIO = AudioVendor.None
                    cbSelectedGPU.SelectedIndex = 0
                    ButtonsPanel.IsEnabled = False
                    cbSelectedGPU.IsEnabled = False

                Case 1 ' Audio
                    Application.Settings.SelectedType = CleanType.Audio
                    Dim audioIndex As Integer = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedGPUIndex, 0)
                    cbSelectedGPU.SelectedIndex = audioIndex

                    Select Case audioIndex
                        Case 1
                            Application.Settings.SelectedAUDIO = AudioVendor.Realtek
                            Application.Settings.SelectedGPU = GPUVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case 2
                            Application.Settings.SelectedAUDIO = AudioVendor.SoundBlaster
                            Application.Settings.SelectedGPU = GPUVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case Else
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            Application.Settings.SelectedGPU = GPUVendor.None
                            ButtonsPanel.IsEnabled = False
                    End Select
                    btnCleanCaches.IsEnabled = False

                Case 2 ' GPU
                    Application.Settings.SelectedType = CleanType.GPU
                    Dim gpuIndex As Integer = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedGPUIndex, GPUIdentify())
                    cbSelectedGPU.SelectedIndex = gpuIndex

                    Select Case gpuIndex
                        Case 1
                            Application.Settings.SelectedGPU = GPUVendor.Nvidia
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case 2
                            Application.Settings.SelectedGPU = GPUVendor.AMD
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case 3
                            Application.Settings.SelectedGPU = GPUVendor.Intel
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case 4
                            Application.Settings.SelectedGPU = GPUVendor.All
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            ButtonsPanel.IsEnabled = True
                        Case Else
                            Application.Settings.SelectedGPU = GPUVendor.None
                            Application.Settings.SelectedAUDIO = AudioVendor.None
                            ButtonsPanel.IsEnabled = False
                    End Select
                    btnCleanCaches.IsEnabled = True
            End Select

            isUpdatingComboBox = False
        End Sub
		Private Sub KillProcess(ByVal ParamArray processnames As String())
			For Each processName As String In processnames
				If String.IsNullOrEmpty(processName) Then
					Continue For
				End If

				For Each process As Process In Process.GetProcessesByName(processName)
					Try
						process.Kill()
						Application.Settings.ProcessKilled = True
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
		 "CapFrameX",
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

			If Application.Settings.ProcessKilled AndAlso (Not Application.LaunchOptions.Silent) Then
				MessageBox.Show(Languages.GetTranslation("frmLaunch", "Messages", "Text1"), Application.Settings.AppName, Nothing, MessageBoxImage.Information)
				Application.Settings.ProcessKilled = False
			End If

		End Sub
		Private Async Sub FrmMain_ContentRendered(sender As System.Object, e As System.EventArgs) Handles MyBase.ContentRendered
			Me.Topmost = False

			Try
				'cbSelectedGPU.ItemsSource = [Enum].GetValues(GetType(GPUVendor))
				'cbSelectedType.ItemsSource = {Languages.GetTranslation("frmMain", "Options_Type", "Options1"), Languages.GetTranslation("frmMain", "Options_Type", "Options2"), Languages.GetTranslation("frmMain", "Options_Type", "Options3")}
				'cbSelectedGPU.ItemsSource = {Languages.GetTranslation("frmMain", "Options_GPU", "Options1"), Languages.GetTranslation("frmMain", "Options_GPU", "Options2"), Languages.GetTranslation("frmMain", "Options_GPU", "Options3"), Languages.GetTranslation("frmMain", "Options_GPU", "Options4")} 'the order is important, check Appsettings.vb
				'cbSelectedType.ItemsSource = [Enum].GetValues(GetType(CleanType))


				cbSelectedType.SelectedIndex = If(Application.Settings.RememberLastChoice, Application.Settings.LastSelectedTypeIndex, 0)
				If Not Application.LaunchOptions.Silent Then
					If WinForm.SystemInformation.BootMode <> Forms.BootMode.FailSafe Then
						Await _checkUpdate.CheckUpdatesAsync()
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
								If String.IsNullOrWhiteSpace(child) Then Continue For

								If StrContainsAny(child, True, "ven_8086") Then
									Try
										Using subRegKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
											If subRegKey IsNot Nothing Then

												For Each childs As String In subRegKey.GetSubKeyNames()
													If String.IsNullOrWhiteSpace(childs) Then Continue For

													Using childRegKey As RegistryKey = MyRegistry.OpenSubKey(subRegKey, childs)
														If childRegKey IsNot Nothing Then
															Dim regValue As String = childRegKey.GetValue("Service", String.Empty).ToString

															If Not String.IsNullOrWhiteSpace(regValue) AndAlso StrContainsAny(regValue, True, "amdkmdap") Then
																_enduro = True
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

					'WorkTask = New Task(Sub() ThreadTask(config))

					'WorkTask.Start()
					Await ThreadTaskAsync(config)
				End If

			Catch ex As Exception
				Application.Log.AddException(ex, "frmMain loading caused error!")
			End Try

			If Application.Settings.FirstTimeLaunch AndAlso Not Application.LaunchOptions.Silent Then
				Microsoft.VisualBasic.MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text17"), MsgBoxStyle.Information, Application.Settings.AppName)
				Dim frmOptions As New FrmOptions

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

			If Not Application.LaunchOptions.Silent AndAlso Not Application.Settings.EnableSafeModeDialog Then
				Select Case System.Windows.Forms.SystemInformation.BootMode

					Case Forms.BootMode.Normal
						Microsoft.VisualBasic.MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text8"), MsgBoxStyle.Information, Application.Settings.AppName)

				End Select
			End If

			If Application.LaunchOptions.PreventWinUpdateArg Then
				EnableDriverSearch(False)
			End If

            Dim canImpersonate As Boolean = False

            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    canImpersonate = WindowsIdentity.GetCurrent().IsSystem
                End Sub)

            If Not canImpersonate Then
                MsgBox("Could not impersonate the SYSTEM account, it is NOT recommended to use DDU in this state.")
            End If
            Application.RemoveRegOption()
        End Sub

		Private Sub FrmMain_Closing(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing

			Try
				If CleaningTask IsNot Nothing AndAlso Not CleaningTask.IsCompleted Then
					Application.Log.SaveToFile()
					Select Case MessageBox.Show("If DDU hasn't progressed since 5 minutes and you think it is stuck, you can click *Yes* If not then at your own risk of corruption.", "Warning, DDU is still executing ! Are you sure you want to close ? ", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)
						Case MessageBoxResult.Yes

						Case MessageBoxResult.No
							e.Cancel = True
						Case MessageBoxResult.Cancel
							e.Cancel = True
					End Select
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

                UpdateTextMethod(UpdateTextTranslated(19))

				Select Case config.SelectedType
					Case CleanType.GPU
						_gpuCleanup.Start(config)
					Case CleanType.Audio
						_audioCleanup.Start(config)
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

            Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End Sub

		Private Sub CleaningCompleted(ByVal config As ThreadSettings)

			EnableControls(True)

			If Not config.Shutdown Then
				SetupAPI.ReScanDevices()
			End If

			If config.Restart Then
				RemoveRegOption()
				'Application.RestartComputer()
				WinAPI.OpenVisitLink(" -CleanComplete -Restart")
				Application.Log.AddMessage("Restarting the computer...")
				CloseDDU()
				Return
			End If

			If config.Shutdown Then
				RemoveRegOption()
				'Application.ShutdownComputer()
				WinAPI.OpenVisitLink(" -CleanComplete -Shutdown")
				Application.Log.AddMessage("Shutting down the computer...")
				CloseDDU()
				Return
			End If

			If config.Silent Then
				CloseDDU()
				Return
			End If

			If MessageBox.Show(Application.Current.MainWindow, Languages.GetTranslation("frmMain", "Messages", "Text10"), config.AppName, MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
				CloseDDU()
				Return
			End If

		End Sub

		Private Sub RemoveRegOption()
			If WinForm.SystemInformation.BootMode <> WinForm.BootMode.Normal Then
				Try
					Using regControl As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot", Writable:=True)
						If regControl IsNot Nothing AndAlso regControl.GetSubKeyNames().Contains("Option") Then
							regControl.DeleteSubKeyTree("Option", throwOnMissingSubKey:=False)
							Application.Log.AddMessage("Deleted SafeBoot\Option key before reboot.")
						End If
					End Using
				Catch ex As Exception
					Application.Log.AddWarningMessage("Could not delete SafeBoot\Option key: " & ex.Message)
				End Try
			End If
		End Sub

		Private Async Function ThreadTaskAsync(ByVal config As ThreadSettings) As Task

			Try

				PreCleaning()

				If Not config.HasCleanArg AndAlso Not config.SelectedGPU = GPUVendor.All Then
					Await StartThreadAsync(config)
					Return
				End If

				Await ProcessCleaningArgumentsAsync(config)

			Catch ex As Exception
				Application.Log.AddException(ex)
			Finally
				CleaningCompleted(config)
			End Try
		End Function

		Private Async Function ProcessCleaningArgumentsAsync(config As ThreadSettings) As Task

			Dim cleanAllGpus As Boolean = config.SelectedGPU = GPUVendor.All

			If config.CleanAmd OrElse cleanAllGpus Then
				config.Success = False
				config.SelectedType = CleanType.GPU
				config.SelectedAUDIO = AudioVendor.None
				config.SelectedGPU = GPUVendor.AMD

				Await StartThreadAsync(config)

				CleaningTask = Nothing
			End If

			If config.CleanNvidia OrElse cleanAllGpus Then
				config.Success = False
				config.SelectedType = CleanType.GPU
				config.SelectedAUDIO = AudioVendor.None
				config.SelectedGPU = GPUVendor.Nvidia

				Await StartThreadAsync(config)

				CleaningTask = Nothing
			End If

			If config.CleanIntel OrElse cleanAllGpus Then
				config.Success = False
				config.SelectedType = CleanType.GPU
				config.SelectedAUDIO = AudioVendor.None
				config.SelectedGPU = GPUVendor.Intel

				Await StartThreadAsync(config)

				CleaningTask = Nothing
			End If

			If config.CleanRealtek Then
				config.Success = False
				config.SelectedType = CleanType.Audio
				config.SelectedGPU = GPUVendor.None
				config.SelectedAUDIO = AudioVendor.Realtek

				Await StartThreadAsync(config)

				CleaningTask = Nothing
			End If

			If config.CleanSoundBlaster Then
				config.Success = False
				config.SelectedType = CleanType.Audio
				config.SelectedGPU = GPUVendor.None
				config.SelectedAUDIO = AudioVendor.SoundBlaster

				Await StartThreadAsync(config)
			End If

		End Function

		Private Sub PreCleaning()
			If Not Me.Dispatcher.CheckAccess() Then
				Me.Dispatcher.Invoke(Sub() PreCleaning())
				Return
			End If

			EnableControls(False)

			'EnableDriverSearch(True, True)
			SystemRestore()

		End Sub

		Private Async Function StartThreadAsync(ByVal config As ThreadSettings) As Task
			Try
				'If System.Diagnostics.Debugger.IsAttached Then          'TODO: remove when tested
				Dim logEntry As New LogEntry() With {.Message = "Used settings for cleaning!"}

				For Each p As PropertyInfo In config.GetType().GetProperties(BindingFlags.Public Or BindingFlags.Instance)
					logEntry.Add(p.Name, If(p.GetValue(config, Nothing) IsNot Nothing, p.GetValue(config, Nothing).ToString(), "-"))
				Next

				Application.Log.Add(logEntry)
				'End If

				If CleaningTask IsNot Nothing AndAlso Not CleaningTask.IsCompleted Then
					Throw New ArgumentException("cleaningThread", "Thread already exists and is busy!")
				End If

				Await Task.Run(Sub() CleaningThread_Work(config))

			Catch ex As Exception
				CleaningTask = Nothing
				Application.Log.AddException(ex, "Launching cleaning thread failed!")
			End Try
		End Function

#End Region




		Private Sub ShowAboutWindow(ByVal frmType As Int32)
			Dim frmAbout As New FrmAbout With
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
				info.Add("Build", Application.Settings.WinBuildText)
				info.Add("Win 10 1809+ ?", Application.Settings.Win10_1809.ToString())
				info.Add("NVIDIA App installed ?", Application.Settings.NVIDIA_App_Installed.ToString())
				info.Add("NVIDIA Broadcast ?", Application.Settings.NVIDIA_Broadcast_Installed.ToString())
				info.Add("Intel NPU Present ?", IntelNpuPresent.ToString())
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
							If String.IsNullOrWhiteSpace(child) Then Continue For

							If Not StrContainsAny(child, True, "properties") Then

								Using subRegkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
									If subRegkey IsNot Nothing Then
										Dim regValue As String = subRegkey.GetValue("Device Description", String.Empty).ToString()

										If Not String.IsNullOrWhiteSpace(regValue) Then
											UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
											If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)
										Else

											regValue = subRegkey.GetValue("DriverDesc", String.Empty).ToString()

											If Not String.IsNullOrWhiteSpace(regValue) Then
												If subRegkey.GetValueKind("DriverDesc") = RegistryValueKind.Binary Then
													regValue = HexToString(GetREG_BINARY(subRegkey, "DriverDesc").Replace("00", ""))

												Else
													regValue = subRegkey.GetValue("DriverDesc", String.Empty).ToString()
												End If
											End If

											If String.IsNullOrWhiteSpace(regValue) Then Continue For

											UpdateTextMethod(String.Format("{0}{1} - {2}: {3}", UpdateTextTranslated(11), child, UpdateTextTranslated(12), regValue))
											If firstLaunch Then info.Add(String.Format("GPU #{0}", child), regValue)

										End If

										regValue = subRegkey.GetValue("MatchingDeviceId", String.Empty).ToString()

										If Not String.IsNullOrWhiteSpace(regValue) Then
											UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(13), regValue))
											If firstLaunch Then info.Add("GPU DeviceID", regValue)
										End If

										Try
											regValue = subRegkey.GetValue("HardwareInformation.BiosString", String.Empty).ToString()

											If Not String.IsNullOrWhiteSpace(regValue) Then
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

										If Not String.IsNullOrWhiteSpace(regValue) Then
											UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(14), regValue))
											If firstLaunch Then info.Add("Detected Driver(s) Version(s)", regValue)
										End If

										regValue = subRegkey.GetValue("InfPath", String.Empty).ToString()

										If Not String.IsNullOrWhiteSpace(regValue) Then
											UpdateTextMethod(String.Format("{0}: {1}", UpdateTextTranslated(15), regValue))
											If firstLaunch Then info.Add("INF name", regValue)
										End If

										regValue = subRegkey.GetValue("InfSection", String.Empty).ToString()

										If Not String.IsNullOrWhiteSpace(regValue) Then
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
				Me.Dispatcher.Invoke(Sub() EnableControls(enabled))
				Return
			End If
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

		End Sub

		Private Sub SystemRestore()
			If Application.LaunchOptions.NoRestorePoint Then
				Exit Sub
			End If

			If Application.Settings.CreateRestorePoint AndAlso System.Windows.Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then
				Dim frmSystemRestore As New FrmSystemRestore

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
			Application.Settings.PreventWinUpdate = InfoDriverSearch()
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
			If Not SharedLogBox.Dispatcher.CheckAccess() Then
				SharedLogBox.Dispatcher.Invoke(Sub() UpdateTextMethod(strMessage))
			Else
				SharedLogBox.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " - " + strMessage)
				SharedLogBox.SelectedIndex = SharedLogBox.Items.Count - 1
				SharedLogBox.ScrollIntoView(SharedLogBox.SelectedItem)
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
			_serviceInstaller.StartService(service)
		End Sub
		Private Function CheckServiceStartupType(ByVal service As String) As String
			Return _cleanupEngine.CheckServiceStartupType(service)
		End Function

		Private Sub SetServiceStartupType(ByVal service As String, value As String)
			_cleanupEngine.SetServiceStartupType(service, value)
		End Sub
		Private Sub StopService(ByVal service As String)
			_serviceInstaller.StopService(service)
		End Sub

		' "Universal" solution, can be used for Nvidia/Intel too


		Private Sub Testing2MenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles testingMenuItem.Click
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
						For Each task As SchedulerTask In tsc.GetAllTasks()

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

		Private Sub CheckXMLMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles checkXMLMenuItem.Click
			Dim current As Languages.LanguageOption = Application.Settings.SelectedLanguage

			Languages.CheckLanguageFiles()

			Languages.Load(current)
		End Sub

		Private Sub SetupAPIMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles SetupAPIMenuItem.Click
			Dim testWindow As New DebugWindow

			testWindow.ShowDialog()
		End Sub

        Private Sub CbSelectedType_Changed(sender As Object, e As SelectionChangedEventArgs) Handles cbSelectedType.SelectionChanged
            If isUpdatingComboBox Then Return

            Application.Settings.LastSelectedTypeIndex = cbSelectedType.SelectedIndex

            Select Case cbSelectedType.SelectedIndex
                Case 0, -1
                    Application.Settings.SelectedType = CleanType.None
                    cbSelectedGPU.ItemsSource = {
                Languages.GetTranslation("frmMain", "Options_GPU", "Options1"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options2"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options3"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options4"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options5")
            }
                    cbSelectedGPU.SelectedIndex = 0
                    cbSelectedGPU.IsEnabled = False

                Case 1
                    Application.Settings.SelectedType = CleanType.Audio
                    cbSelectedGPU.IsEnabled = True
                    cbSelectedGPU.ItemsSource = {
                Languages.GetTranslation("frmMain", "Options_AUDIO", "Options1"),
                Languages.GetTranslation("frmMain", "Options_AUDIO", "Options2"),
                Languages.GetTranslation("frmMain", "Options_AUDIO", "Options3")
            }
                    cbSelectedGPU.SelectedIndex = 0

                Case 2
                    Application.Settings.SelectedType = CleanType.GPU
                    cbSelectedGPU.IsEnabled = True
                    cbSelectedGPU.ItemsSource = {
                Languages.GetTranslation("frmMain", "Options_GPU", "Options1"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options2"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options3"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options4"),
                Languages.GetTranslation("frmMain", "Options_GPU", "Options5")
            }
                    cbSelectedGPU.SelectedIndex = GPUIdentify()
            End Select

            isUpdatingComboBox = False
        End Sub

        Private Sub CbSelectedGPU_Changed(sender As Object, e As SelectionChangedEventArgs) Handles cbSelectedGPU.SelectionChanged
            If isUpdatingComboBox Then Return

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
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                        Case 2
                            Application.Settings.SelectedAUDIO = AudioVendor.SoundBlaster
                            cbSelectedGPU.IsEnabled = True
                            ButtonsPanel.IsEnabled = True
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                    End Select
                    btnCleanCaches.IsEnabled = False

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
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                        Case 2
                            Application.Settings.SelectedGPU = GPUVendor.AMD
                            cbSelectedGPU.IsEnabled = True
                            ButtonsPanel.IsEnabled = True
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                        Case 3
                            Application.Settings.SelectedGPU = GPUVendor.Intel
                            cbSelectedGPU.IsEnabled = True
                            ButtonsPanel.IsEnabled = True
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                        Case 4
                            Application.Settings.SelectedGPU = GPUVendor.All
                            cbSelectedGPU.IsEnabled = True
                            ButtonsPanel.IsEnabled = True
                            Application.Settings.LastSelectedGPUIndex = cbSelectedGPU.SelectedIndex
                    End Select
                    btnCleanCaches.IsEnabled = True
            End Select
        End Sub

        Private Sub Cleandriverstore(ByVal config As ThreadSettings)
			_cleanupEngine.Cleandriverstore(config)
		End Sub

		Private Sub FrmMain_Initialized(sender As Object, e As EventArgs) Handles MyBase.Initialized
			SharedLogBox = lbLog
		End Sub

		Private Sub LblOffer_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles lblOffer.MouseDown
			WinAPI.OpenVisitLink(" -visitoffer")
		End Sub

        Private Sub VisitDiscord_Click(sender As Object, e As RoutedEventArgs) Handles VisitDiscord.Click
            WinAPI.OpenVisitLink(" -visitdiscord")
        End Sub

        Private Sub DownloadNvidiaMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles DownloadNvidiaMenuItem.Click
            WinAPI.OpenVisitLink(" -visitnvidia")
        End Sub

        Private Sub DownloadAmdMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles DownloadAmdMenuItem.Click
            WinAPI.OpenVisitLink(" -visitamd")
        End Sub

        Private Sub DownloadIntelMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles DownloadIntelMenuItem.Click
            WinAPI.OpenVisitLink(" -visitintel")
        End Sub
    End Class
End Namespace
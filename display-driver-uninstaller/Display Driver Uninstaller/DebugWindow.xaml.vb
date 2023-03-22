Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Input
Imports System.Windows.Media
Imports Display_Driver_Uninstaller.Win32
Imports System.Threading

Namespace Display_Driver_Uninstaller
	Public Class DebugWindow
		Dim FileIO As New FileIO
		Private _devices As ObservableCollection(Of SetupAPI.Device) = New ObservableCollection(Of SetupAPI.Device)

		Public ReadOnly Property Devices As ObservableCollection(Of SetupAPI.Device)
			Get
				Return _devices
			End Get
		End Property

		Private FiltersDev As List(Of String) = New List(Of String)({
	 "Device_ClassName",
	 "Device_Description",
	 "Device_HardwareID"})
		Private Filters As List(Of String) = New List(Of String)({
	 "Device_Description",
	 "Device_ClassName",
	 "Device_HardwareID",
	 "Device_CompatibleIDs",
	 "Device_ClassGuid",
	 "Driver_Manufacturer",
	 "Driver_Provider",
	 "Driver_InfFileName",
	 "Driver_HardwareID"})

		Public Sub New()
			' This call is required by the designer.
			InitializeComponent()

			' Add any initialization after the InitializeComponent() call.
		End Sub



		Private Sub btnDisable_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If listBox1.SelectedItem Is Nothing Then
				MessageBox.Show("No selected device!")
				Return
			Else
				Dim d As SetupAPI.Device = TryCast(listBox1.SelectedItem, SetupAPI.Device)
				If d Is Nothing OrElse Not d.HasHardwareID Then
					MessageBox.Show("Selected device doesn't contain Hardware ID!")
					Return
				End If
			End If

			SetupAPI.TEST_EnableDevice(DirectCast(listBox1.SelectedItem, SetupAPI.Device), False)
		End Sub

		Private Sub btnEnable_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If listBox1.SelectedItem Is Nothing Then
				MessageBox.Show("No selected device!")
				Return
			Else
				Dim d As SetupAPI.Device = TryCast(listBox1.SelectedItem, SetupAPI.Device)
				If d Is Nothing OrElse Not d.HasHardwareID Then
					MessageBox.Show("Selected device doesn't contain Hardware ID!")
					Return
				End If
			End If

			SetupAPI.TEST_EnableDevice(DirectCast(listBox1.SelectedItem, SetupAPI.Device), True)
		End Sub

		Private Sub btnRemove_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If listBox1.SelectedItem Is Nothing Then
				MessageBox.Show("No selected device!")
				Return
			Else
				Dim d As SetupAPI.Device = TryCast(listBox1.SelectedItem, SetupAPI.Device)

				If d Is Nothing OrElse Not d.HasHardwareID Then
					MessageBox.Show("Selected device doesn't contain Hardware ID!")
					Return
				End If
			End If

			SetupAPI.UninstallDevice(DirectCast(listBox1.SelectedItem, SetupAPI.Device))

			MessageBox.Show("Done! Check log for results.")
		End Sub

		Private Sub btnRemoveInf_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnRemoveInf.Click
			Dim ofd As Microsoft.Win32.OpenFileDialog = New Microsoft.Win32.OpenFileDialog()

			ofd.InitialDirectory = "C:\Windows\inf"
			ofd.Filter = "Inf Files (*.inf)|*.inf"

			Dim result As Boolean? = ofd.ShowDialog(Me)

			If result IsNot Nothing AndAlso result.Value Then
				Dim inf As Inf = New Inf(ofd.FileName)

				If MessageBox.Show("Remove INF?" & CRLF & "Class: " & inf.Class & CRLF & "Provider: " & inf.Provider, inf.FileName, MessageBoxButton.YesNo) = MessageBoxResult.Yes Then
					SetupAPI.RemoveInf(inf, True)
				End If
			End If
		End Sub

		Private Sub btnFindDevs_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If Devices.Count > 0 Then
				Devices.Clear()
			End If

			lblDevicesDev.Content = "Devices: 0"

			If cbFilterDev.SelectedItem Is Nothing Then
				cbFilterDev.SelectedIndex = 0
			End If

#If DEBUG Then
			Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
#End If

			Dim found As List(Of SetupAPI.Device)

			If cbFilterDev.SelectedIndex = 0 Then
				found = SetupAPI.GetDevices(tbFilterDev.Text, Nothing, If(chbSearchSiblings.IsChecked.HasValue, chbSearchSiblings.IsChecked.Value, False), True, True)
			ElseIf cbFilterDev.SelectedItem IsNot Nothing Then
				found = SetupAPI.TEST_GetDevices(cbFilterDev.SelectedItem.ToString(), tbFilterDev.Text, If(chbSearchSiblings.IsChecked.HasValue, chbSearchSiblings.IsChecked.Value, False), True)
			Else
				found = New List(Of SetupAPI.Device)(0)
			End If

#If DEBUG Then
			sw.Stop()
			MessageBox.Show("Time: " & sw.Elapsed.ToString())
#End If


			If found.Count > 0 Then
				For Each d As SetupAPI.Device In found
					Devices.Add(d)
				Next

				lblDevicesDev.Content = String.Format("Devices: {0}", Devices.Count)
				UpdateFilter()

				MessageBox.Show(String.Format("{0} devices found!", Devices.Count))
			Else
				MessageBox.Show("Devices not found!")
			End If
		End Sub

		Private Sub UpdateFilter()
			Dim view As ICollectionView = CollectionViewSource.GetDefaultView(listBox1.ItemsSource)

			view.Filter = New Predicate(Of Object)(AddressOf Filter)

			lblDevices.Content = String.Format("Items: {0}", listBox1.Items.Count)
		End Sub

		Private Function Filter(ByVal obj As Object) As Boolean
			Dim d As SetupAPI.Device = TryCast(obj, SetupAPI.Device)

			If d IsNot Nothing AndAlso cbFilter.SelectedItem IsNot Nothing Then
				If String.IsNullOrEmpty(tbFilter.Text) Then
					Return True
				End If

				Dim filter__1 As String = cbFilter.SelectedItem.ToString()
				Dim text As String = tbFilter.Text

				Select Case filter__1
					Case "Device_Description"
						Return If(Not String.IsNullOrEmpty(d.Description), d.Description.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1, False)
					Case "Device_ClassName"
						Return If(Not String.IsNullOrEmpty(d.ClassName), d.ClassName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1, False)
					Case "Device_HardwareID"
						If d.HardwareIDs IsNot Nothing Then
							For Each hdID As String In d.HardwareIDs
								If hdID.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
					Case "Device_CompatibleIDs"
						If d.CompatibleIDs IsNot Nothing Then
							For Each cID As String In d.CompatibleIDs
								If cID.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
					Case "Device_ClassGuid"
						Return If(Not String.IsNullOrEmpty(d.ClassGuid), d.ClassGuid.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1, False)
					Case "Driver_Manufacturer"
						If d.DriverInfo IsNot Nothing Then
							For Each drvInfo As SetupAPI.DriverInfo In d.DriverInfo
								If drvInfo.MfgName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
					Case "Driver_Provider"
						If d.DriverInfo IsNot Nothing Then
							For Each drvInfo As SetupAPI.DriverInfo In d.DriverInfo
								If drvInfo.ProviderName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
					Case "Driver_InfFileName"
						If d.DriverInfo IsNot Nothing Then
							For Each drvInfo As SetupAPI.DriverInfo In d.DriverInfo
								If drvInfo.InfFile.FileName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
					Case "Driver_HardwareID"
						If d.DriverInfo IsNot Nothing Then
							For Each drvInfo As SetupAPI.DriverInfo In d.DriverInfo
								If drvInfo.HardwareID.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
									Return True
								End If
							Next
						End If
						Return False
				End Select
			End If

			Return False
		End Function

		Private Sub CopyCommand(ByVal sender As Object, e As ExecutedRoutedEventArgs)
			Dim lb As ListBox = TryCast(sender, ListBox)

			If lb Is Nothing Then
				Return
			End If

			Dim device As SetupAPI.Device = TryCast(lb.SelectedItem, SetupAPI.Device)
			If device Is Nothing Then
				Return
			End If

			Dim sb As New StringBuilder()

			sb.AppendLine("Description: " & If(IsNullOrWhitespace(device.Description), "<empty>", device.Description))
			sb.AppendLine("FriendlyName: " & If(IsNullOrWhitespace(device.FriendlyName), "<empty>", device.FriendlyName))
			sb.AppendLine("ClassName: " & If(IsNullOrWhitespace(device.ClassName), "<empty>", device.ClassName))
			sb.AppendLine("ClassGuid: " & If(IsNullOrWhitespace(device.ClassGuid), "<empty>", device.ClassGuid))
			sb.AppendLine("DeviceID: " & If(IsNullOrWhitespace(device.DeviceID), "<empty>", device.DeviceID))
			sb.AppendLine("InstallState: " & If(IsNullOrWhitespace(device.InstallStateStr), device.InstallState.ToString(), device.InstallStateStr))

			sb.AppendLine("RebootRequired: " & If(device.RebootRequired, "Yes", "No"))

			sb.AppendLine("DevInstID: " & device.DevInstID.ToString())
			sb.AppendLine("DevProblemStr: " & If(IsNullOrWhitespace(device.DevProblemStr), device.DevProblem.ToString(), device.DevProblemStr))

			If device.DevStatusStr IsNot Nothing AndAlso device.DevStatusStr.Length > 0 Then
				sb.AppendLine("DevStatusStr:")

				For Each s As String In device.DevStatusStr
					sb.AppendLine(vbTab + s)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("DevStatusStr: <empty>")
			End If

			sb.AppendLine("RebootRequired: " & If(device.RebootRequired, "Yes", "No"))

			If device.OemInfs IsNot Nothing AndAlso device.OemInfs.Length > 0 Then
				sb.AppendLine("OemInfs:")

				For Each inf As Inf In device.OemInfs
					sb.AppendLine(vbTab + inf.FileName)
					sb.AppendLine(vbTab + vbTab + String.Format("Install Date: '{0}'", inf.InstallDate.ToShortDateString()))
					sb.AppendLine(vbTab + vbTab + String.Format("Class: '{0}'", inf.Class))
					sb.AppendLine(vbTab + vbTab + String.Format("Provider: '{0}'", inf.Provider))
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("OemInfs: <empty>")
			End If


			If device.HardwareIDs IsNot Nothing AndAlso device.HardwareIDs.Length > 0 Then
				sb.AppendLine("Hardware IDs:")

				For Each hwid As String In device.HardwareIDs
					sb.AppendLine(vbTab + hwid)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("Hardware IDs: <empty>")
			End If

			If device.CompatibleIDs IsNot Nothing AndAlso device.CompatibleIDs.Length > 0 Then
				sb.AppendLine("Compatible IDs:")

				For Each cid As String In device.CompatibleIDs
					sb.AppendLine(vbTab + cid)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("Compatible IDs: <empty>")
			End If

			If device.LowerFilters IsNot Nothing AndAlso device.LowerFilters.Length > 0 Then
				sb.AppendLine("LowerFilters:")

				For Each cid As String In device.LowerFilters
					sb.AppendLine(vbTab + cid)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("LowerFilters: <empty>")
			End If

			If device.ParentDevices IsNot Nothing AndAlso device.ParentDevices.Length > 0 Then
				sb.AppendLine("ParentDevices:")

				For Each d2 As SetupAPI.Device In device.ParentDevices
					sb.AppendLine(vbTab + If(d2.FriendlyName, d2.Description))
					sb.AppendLine(vbTab + If(d2.DeviceID, "-"))
				Next

			Else
				sb.AppendLine("ParentDevices: <empty>")
			End If

			If device.ChildDevices IsNot Nothing AndAlso device.ChildDevices.Length > 0 Then
				sb.AppendLine("ChildDevices:")

				For Each d2 As SetupAPI.Device In device.ChildDevices
					sb.AppendLine(vbTab + If(d2.FriendlyName, d2.Description))
					sb.AppendLine(vbTab + If(d2.DeviceID, "-"))
				Next

			Else
				sb.AppendLine("ParentDevices: <empty>")
			End If

			If device.CapabilitiesStr IsNot Nothing AndAlso device.CapabilitiesStr.Length > 0 Then
				sb.AppendLine("Capabilities:")

				For Each s As String In device.CapabilitiesStr
					sb.AppendLine(vbTab + s)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("Capabilities: <empty>")
			End If


			If device.ConfigFlagsStr IsNot Nothing AndAlso device.ConfigFlagsStr.Length > 0 Then
				sb.AppendLine("ConfigFlags:")

				For Each s As String In device.ConfigFlagsStr
					sb.AppendLine(vbTab + s)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("Capabilities: <empty>")
			End If


			If device.InstallFlagsStr IsNot Nothing AndAlso device.InstallFlagsStr.Length > 0 Then
				sb.AppendLine("InstallFlags:")

				For Each s As String In device.InstallFlagsStr
					sb.AppendLine(vbTab + s)
				Next

				sb.AppendLine(String.Empty)
			Else
				sb.AppendLine("InstallFlags: <empty>")
			End If


			If device.DriverInfo IsNot Nothing AndAlso device.DriverInfo.Length > 0 Then
				sb.AppendLine("Driver(s) details:")

				For Each drvInfo As SetupAPI.DriverInfo In device.DriverInfo
					sb.AppendLine(vbTab & "Description: " & drvInfo.Description)
					sb.AppendLine(vbTab & "Manufacturer: " & drvInfo.MfgName)
					sb.AppendLine(vbTab & "Provider: " & drvInfo.ProviderName)
					sb.AppendLine(vbTab & "Driver Version: " & drvInfo.DriverVersion)
					sb.AppendLine(vbTab & "Driver Date: " & drvInfo.DriverDate.ToShortDateString)
					sb.AppendLine(vbTab & "Inf FileName: " & drvInfo.InfFile.FileName)
					sb.AppendLine(vbTab & vbTab & "Inf Install Date: " & drvInfo.InfFile.InstallDate.ToShortDateString)
					sb.AppendLine(vbTab & vbTab & "Inf Class: '" & drvInfo.InfFile.Class & "'")
					sb.AppendLine(vbTab & vbTab & "Inf Provider: '" & drvInfo.InfFile.Provider & "'")
					sb.AppendLine(vbTab & "Hardware ID: " & drvInfo.HardwareID)

					If drvInfo.CompatibleIDs IsNot Nothing AndAlso drvInfo.CompatibleIDs.Length > 0 Then
						sb.AppendLine(vbTab & "Compatible IDs:")

						For Each cid As String In drvInfo.CompatibleIDs
							sb.AppendLine(vbTab & vbTab + cid)
						Next
					End If

					sb.AppendLine(String.Empty)
				Next
			Else
				sb.AppendLine("Driver(s) details: <empty>")
			End If


			Clipboard.SetText(sb.ToString())
		End Sub

		Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			cbFilter.ItemsSource = Filters
			cbFilterDev.ItemsSource = FiltersDev

			Dim isAdmin As Boolean = Tools.UserHasAdmin

			If isAdmin Then
				Me.Title = "Process running as " & (If(Tools.ProcessIs64, "x64", "x86")) & " --- " & "Admin: Yes"
			Else
				btnDisable.IsEnabled = False
				btnEnable.IsEnabled = False
				btnRemove.IsEnabled = False

				Me.Title = "Process running as " & (If(Tools.ProcessIs64, "x64", "x86")) & " --- " & "Admin: No!   (You can find devices but can't enable/disable/remove!)"
			End If
		End Sub

		Public Shared Function FindVisualChildren(Of T As DependencyObject)(ByVal depObj As DependencyObject) As IEnumerable(Of T)
			If depObj IsNot Nothing Then
				Dim childs As List(Of T) = New List(Of T)

				For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1

					Dim child As DependencyObject = VisualTreeHelper.GetChild(depObj, i)

					If child IsNot Nothing AndAlso TypeOf child Is T Then
						childs.Add(DirectCast(child, T))
					End If

					For Each childOfChild As T In FindVisualChildren(Of T)(child)
						childs.Add(childOfChild)
					Next
				Next

				Return childs
			End If

			Return Nothing
		End Function

		Public Shared Function FindVisualChild(Of childItem As DependencyObject)(ByVal obj As DependencyObject) As childItem
			For Each child As childItem In FindVisualChildren(Of childItem)(obj)
				Return child
			Next

			Return Nothing
		End Function

		Private Sub tbFilter_TextChanged(ByVal sender As Object, ByVal e As TextChangedEventArgs)
			UpdateFilter()
		End Sub

		Private Sub cbFilter_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
			UpdateFilter()
		End Sub

		Private Sub btnUpdate_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnUpdate.Click
			Dim ofd As Microsoft.Win32.OpenFileDialog = New Microsoft.Win32.OpenFileDialog()

			ofd.InitialDirectory = "C:\Windows\inf"
			ofd.Filter = "Inf Files (*.inf)|*.inf"

			Dim result As Boolean? = ofd.ShowDialog(Me)

			If result IsNot Nothing AndAlso result.Value Then
				Dim dev As SetupAPI.Device = DirectCast(listBox1.SelectedItem, SetupAPI.Device)

				Dim msgResult As MessageBoxResult = MessageBox.Show("Force update INF?" & CRLF & "Device: " & dev.Description & CRLF & "INF: " & ofd.FileName, "Update INF", MessageBoxButton.YesNoCancel)

				If msgResult = MessageBoxResult.Yes Then
					SetupAPI.UpdateDeviceInf(dev, ofd.FileName, True)
				ElseIf msgResult = MessageBoxResult.No Then
					SetupAPI.UpdateDeviceInf(dev, ofd.FileName, False)
				End If
			End If
		End Sub

		Private Sub btnReScanDevices_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnReScanDevices.Click
			SetupAPI.ReScanDevices()
			MessageBox.Show("Done!")
		End Sub



		Public Shared Property CurrentProperty As DependencyProperty = DependencyProperty.Register("Current", GetType(Int64), GetType(DebugWindow), New PropertyMetadata(0L))
		Public Shared Property CountProperty As DependencyProperty = DependencyProperty.Register("Count", GetType(Int64), GetType(DebugWindow), New PropertyMetadata(0L))

		Public Property Current As Int64
			Get
				Return DirectCast(GetValue(CurrentProperty), Int64)
			End Get
			Set(value As Int64)
				SetValue(CurrentProperty, value)
			End Set
		End Property

		Public Property Count As Int64
			Get
				Return DirectCast(GetValue(CountProperty), Int64)
			End Get
			Set(value As Int64)
				SetValue(CountProperty, value)
			End Set
		End Property


		Private lockPathList As Object = "SomeVeryHeavyLock"
		Public Property PathList As New ObservableCollection(Of PathEntry)
		Private pathThread As Thread = Nothing
		Private cancelHandle As EventWaitHandle = New EventWaitHandle(False, EventResetMode.ManualReset)

		Private Sub btnGetFiles_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnGetFiles.Click
			If String.IsNullOrEmpty(tbWildCards.Text) Then
				tbWildCards.Text = "*"
			End If

			PathList.Clear()

			cancelHandle.Reset()
			Current = 0
			Count = 0

			Dim path As String = tbPath.Text
			Dim wildCard As String = tbWildCards.Text
			Dim subFolders As Boolean = If(chkSubFolders.IsChecked.HasValue, chkSubFolders.IsChecked.Value, False)

			btnGetFiles.IsEnabled = False
			btnGetFolders.IsEnabled = False


			pathThread = New Thread(New ThreadStart(Sub() GetPathsThread(path, wildCard, subFolders, True))) With {.IsBackground = True, .Name = "bgThread"}
			pathThread.Start()
		End Sub

		Private Sub btnGetFolders_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnGetFolders.Click
			If String.IsNullOrEmpty(tbWildCards.Text) Then
				tbWildCards.Text = "*"
			End If

			PathList.Clear()

			cancelHandle.Reset()
			Current = 0
			Count = 0

			Dim path As String = tbPath.Text
			Dim wildCard As String = tbWildCards.Text
			Dim subFolders As Boolean = If(chkSubFolders.IsChecked.HasValue, chkSubFolders.IsChecked.Value, False)

			btnGetFiles.IsEnabled = False
			btnGetFolders.IsEnabled = False

			pathThread = New Thread(New ThreadStart(Sub() GetPathsThread(path, wildCard, subFolders, False))) With {.IsBackground = True, .Name = "bgThread"}
			pathThread.Start()
		End Sub

		Private Sub GetPathsThread(ByVal path As String, ByVal wildCard As String, ByVal subFolders As Boolean, ByVal filesOnly As Boolean)
			Try
				Dim paths As List(Of String)

				If filesOnly Then
					paths = FileIO.GetFiles(path, wildCard, subFolders)
				Else
					paths = FileIO.GetDirectories(path, wildCard, subFolders)
				End If

				Application.Current.Dispatcher.Invoke(DirectCast(Sub() Count = paths.Count, ThreadStart), Threading.DispatcherPriority.DataBind)

				Dim attr As UInt32 = 0UI

				For Each p As String In paths
					If cancelHandle.WaitOne(0) Then
						Exit For
					End If

					attr = FileIO.GetAttributes(p)


					If (filesOnly AndAlso Not ((attr And FileIO.FILE_ATTRIBUTES.DIRECTORY) = FileIO.FILE_ATTRIBUTES.DIRECTORY)) OrElse (Not filesOnly AndAlso ((attr And FileIO.FILE_ATTRIBUTES.DIRECTORY) = FileIO.FILE_ATTRIBUTES.DIRECTORY)) Then
						AddEntry(New PathEntry(p, attr))
					End If
				Next
			Catch ex As ThreadAbortException
			Catch ex As Exception
				Application.Log.AddException(ex)
			Finally
				Application.Current.Dispatcher.Invoke(DirectCast(Sub() ThreadDone(), ThreadStart), Threading.DispatcherPriority.DataBind)
			End Try
		End Sub

		Private Delegate Sub AddEntryFinalDelegate(ByVal entry As PathEntry)
		Private Sub AddEntryFinal(ByVal entry As PathEntry)
			SyncLock lockPathList
				Interlocked.Increment(Current)
				PathList.Add(entry)
			End SyncLock
		End Sub

		Private Sub AddEntry(ByVal entry As PathEntry)
			If Not Application.Current.Dispatcher.CheckAccess() Then
				Queue(New AddEntryFinalDelegate(AddressOf Me.AddEntryFinal), entry)
			Else
				AddEntryFinal(entry)
			End If
		End Sub

		Private Sub Queue(ByVal method As [Delegate], ByVal ParamArray args() As Object)
			Application.Current.Dispatcher.Invoke(method, Threading.DispatcherPriority.Background, args)
		End Sub

		Private Sub btnCancel_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnCancel.Click
			If pathThread IsNot Nothing AndAlso pathThread.IsAlive Then
				cancelHandle.Set()
			End If
		End Sub

		Private Sub ThreadDone()
			btnGetFiles.IsEnabled = True
			btnGetFolders.IsEnabled = True

			Count = Current
		End Sub

		Private Sub tbPath_MouseDoubleClick(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles tbPath.MouseDoubleClick
			Using fbd As New System.Windows.Forms.FolderBrowserDialog
				fbd.SelectedPath = tbPath.Text

				If fbd.ShowDialog() = Forms.DialogResult.OK Then
					tbPath.Text = fbd.SelectedPath
				End If
			End Using

		End Sub

		Private Sub menuFileIODelete_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles menuFileIODelete.Click

			If lbPaths.SelectedItem Is Nothing Then
				Return
			End If

			Dim pathEntry As PathEntry = TryCast(lbPaths.SelectedItem, PathEntry)

			If pathEntry Is Nothing Then
				Return
			End If

			If MessageBox.Show("Delete path:" & CRLF & pathEntry.Path, "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
				FileIO.Delete(pathEntry.Path)
			End If
		End Sub

	End Class

	Public Class PathEntry
		Public Property IsDirectory As Boolean
		Public Property Path As String
		Public Property Flags As UInt32

		Public ReadOnly Property FlagsStr As String
			Get
				Return "0x" & Flags.ToString("X2") & "  (" & Flags.ToString() & ")"
			End Get
		End Property

		Public ReadOnly Property FlagsList As String
			Get
				Return String.Join(Environment.NewLine, ToStringArray(Of FileIO.FILE_ATTRIBUTES)(Flags, "-"))
			End Get
		End Property

		Public Sub New(ByVal path As String, ByVal flags As UInt32)
			Me.Path = path
			Me.Flags = flags

			Me.IsDirectory = (flags And FileIO.FILE_ATTRIBUTES.DIRECTORY) = FileIO.FILE_ATTRIBUTES.DIRECTORY
		End Sub
	End Class
End Namespace
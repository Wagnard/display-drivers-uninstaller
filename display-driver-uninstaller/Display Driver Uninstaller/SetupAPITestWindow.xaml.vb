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
Imports Display_Driver_Uninstaller.SetupAPI


Public Class SetupAPITestWindow
    Private _devices As ObservableCollection(Of Device) = New ObservableCollection(Of Device)

    Public ReadOnly Property Devices As ObservableCollection(Of Device)
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
            Dim d As Device = TryCast(listBox1.SelectedItem, Device)
            If d Is Nothing OrElse d.HardwareIDs Is Nothing OrElse d.HardwareIDs.Length <= 0 Then
                MessageBox.Show("Selected device doesn't contain Hardware ID!")
                Return
            End If
        End If

        SetupAPI.TEST_EnableDevice(DirectCast(listBox1.SelectedItem, Device).HardwareIDs(0), False)
    End Sub

    Private Sub btnEnable_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If listBox1.SelectedItem Is Nothing Then
            MessageBox.Show("No selected device!")
            Return
        Else
            Dim d As Device = TryCast(listBox1.SelectedItem, Device)
            If d Is Nothing OrElse d.HardwareIDs Is Nothing OrElse d.HardwareIDs.Length <= 0 Then
                MessageBox.Show("Selected device doesn't contain Hardware ID!")
                Return
            End If
        End If

        SetupAPI.TEST_EnableDevice(DirectCast(listBox1.SelectedItem, Device).HardwareIDs(0), True)
    End Sub

    Private Sub btnRemove_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If listBox1.SelectedItem Is Nothing Then
            MessageBox.Show("No selected device!")
            Return
        Else
            Dim d As Device = TryCast(listBox1.SelectedItem, Device)
            If d Is Nothing OrElse d.HardwareIDs Is Nothing OrElse d.HardwareIDs.Length <= 0 Then
                MessageBox.Show("Selected device doesn't contain Hardware ID!")
                Return
            End If
        End If

        SetupAPI.TEST_RemoveDevice(DirectCast(listBox1.SelectedItem, Device).HardwareIDs(0))
    End Sub

    Private Sub btnFindDevs_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If Devices.Count > 0 Then
            Devices.Clear()
        End If

        lblDevicesDev.Content = "Devices: 0"

        If cbFilterDev.SelectedItem Is Nothing Then
            cbFilterDev.SelectedIndex = 0
        End If

        If System.IO.File.Exists("C:\SafeWinAPI.log") Then
            System.IO.File.Delete("C:\SafeWinAPI.log")
        End If
        Dim found As List(Of Device) = SetupAPI.TEST_GetDevices(cbFilterDev.SelectedItem.ToString(), tbFilterDev.Text)

        If found.Count > 0 Then
            For Each d As Device In found
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
        Dim d As Device = TryCast(obj, Device)

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
                        For Each drvInfo As DriverInfo In d.DriverInfo
                            If drvInfo.MfgName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                Return True
                            End If
                        Next
                    End If
                    Return False
                Case "Driver_Provider"
                    If d.DriverInfo IsNot Nothing Then
                        For Each drvInfo As DriverInfo In d.DriverInfo
                            If drvInfo.ProviderName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                Return True
                            End If
                        Next
                    End If
                    Return False
                Case "Driver_InfFileName"
                    If d.DriverInfo IsNot Nothing Then
                        For Each drvInfo As DriverInfo In d.DriverInfo
                            If drvInfo.InfFileName.IndexOf(tbFilter.Text, StringComparison.OrdinalIgnoreCase) <> -1 Then
                                Return True
                            End If
                        Next
                    End If
                    Return False
                Case "Driver_HardwareID"
                    If d.DriverInfo IsNot Nothing Then
                        For Each drvInfo As DriverInfo In d.DriverInfo
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

        Dim device As Device = TryCast(lb.SelectedItem, Device)
        If device Is Nothing Then
            Return
        End If

        Dim sb As New StringBuilder()
        sb.AppendLine("Description: " + device.Description)
        sb.AppendLine("ClassGuid: " + device.ClassGuid)

        If device.OemInfs IsNot Nothing AndAlso device.OemInfs.Length > 0 Then
            sb.AppendLine("OemInfs:")

            For Each inf As String In device.OemInfs
                sb.AppendLine(vbTab + inf)
            Next

            sb.AppendLine(String.Empty)
        Else
            sb.AppendLine("OemInfs: <null>")
        End If


        If device.HardwareIDs IsNot Nothing AndAlso device.HardwareIDs.Length > 0 Then
            sb.AppendLine("Hardware IDs:")

            For Each hwid As String In device.HardwareIDs
                sb.AppendLine(vbTab + hwid)
            Next

            sb.AppendLine(String.Empty)
        Else
            sb.AppendLine("Hardware IDs: <null>")
        End If

        If device.CompatibleIDs IsNot Nothing AndAlso device.CompatibleIDs.Length > 0 Then
            sb.AppendLine("Compatible IDs:")

            For Each cid As String In device.CompatibleIDs
                sb.AppendLine(vbTab + cid)
            Next

            sb.AppendLine(String.Empty)
        Else
            sb.AppendLine("Compatible IDs: <null>")
        End If

        If device.DriverInfo IsNot Nothing AndAlso device.DriverInfo.Count > 0 Then
            sb.AppendLine("Driver(s) details:")

            For Each drvInfo As DriverInfo In device.DriverInfo
                sb.AppendLine(vbTab & "Description: " + drvInfo.Description)
                sb.AppendLine(vbTab & "Manufacturer: " + drvInfo.MfgName)
                sb.AppendLine(vbTab & "Provider: " + drvInfo.ProviderName)
                sb.AppendLine(vbTab & "Driver Version: " + drvInfo.DriverVersion)
                sb.AppendLine(vbTab & "Driver Date: " + drvInfo.DriverDate.ToShortDateString)
                sb.AppendLine(vbTab & "Inf FileName: " + drvInfo.InfFileName)
                sb.AppendLine(vbTab & "Inf Date: " + drvInfo.InfDate.ToShortDateString)
                sb.AppendLine(vbTab & "Hardware ID: " + drvInfo.HardwareID)

                If drvInfo.CompatibleIDs IsNot Nothing AndAlso drvInfo.CompatibleIDs.Length > 0 Then
                    sb.AppendLine(vbTab & "Compatible IDs:")

                    For Each cid As String In drvInfo.CompatibleIDs
                        sb.AppendLine(vbTab & vbTab + cid)
                    Next
                End If

                sb.AppendLine(String.Empty)
            Next
        Else
            sb.AppendLine("Driver(s) details: <null>")
        End If


        Clipboard.SetText(sb.ToString())
    End Sub

    Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
        cbFilter.ItemsSource = Filters
        cbFilterDev.ItemsSource = FiltersDev

        Dim isAdmin As Boolean = Win32.IsAdmin()

        If isAdmin Then
            MessageBox.Show("Process running as " & (If(Win32.Is64(), "x64", "x86")) & vbCrLf & "Admin? Yes")
        Else
            btnDisable.IsEnabled = False
            btnEnable.IsEnabled = False
            btnRemove.IsEnabled = False

            MessageBox.Show("Process running as " & (If(Win32.Is64(), "x64", "x86")) & vbCrLf & "Admin? No!" & vbCrLf & vbCrLf & "You can find devices but can't enable/disable/remove!")
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

    Private Sub Button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        UpdateFilter()
    End Sub

    Private Sub tbFilter_TextChanged(ByVal sender As Object, ByVal e As TextChangedEventArgs)
        UpdateFilter()
    End Sub

    Private Sub cbFilter_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
        UpdateFilter()
    End Sub
End Class

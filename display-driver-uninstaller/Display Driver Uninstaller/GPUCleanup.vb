Imports System.DirectoryServices
Imports System.IO
Imports System.Linq
Imports System.Security.Principal
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks
Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32
Imports WinForm = System.Windows.Forms

Namespace Display_Driver_Uninstaller
    Public Class GPUCleanup
        Private ReadOnly _fileIo As New FileIO
        Private ReadOnly _winxp As Boolean = FrmMain.IsWindowsXp
        Private ReadOnly _win10 As Boolean = FrmMain.IsWindows10
        Private ReadOnly _isWindows8OrHigher As Boolean = FrmMain.IsWindows8OrHigher
        Private ReadOnly _sysdrv As String = Application.Paths.SystemDrive

        Public Sub Start(ByVal config As ThreadSettings)
            Dim cleanupEngine As New CleanupEngine
            Dim serviceInstaller As New ServiceInstaller
            Dim array As String()
            Dim vendCompatibleID As String = ""
            Dim vendIdExpected As String = ""
            Dim vendidSC As String()
            Dim audioServices As String() = Nothing

            Select Case config.SelectedGPU
                Case GPUVendor.Nvidia
                    vendIdExpected = "VEN_10DE"
                    vendCompatibleID = "VEN_10DE&CC_03"
                    vendidSC = {"VEN_10DE"}
                    audioServices = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\servicesaudio.cfg")
                Case GPUVendor.AMD
                    vendIdExpected = "VEN_1002"
                    vendCompatibleID = "VEN_1002&CC_03"
                    vendidSC = {"VEN_1002"}
                    audioServices = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\servicesaudio.cfg")
                Case GPUVendor.Intel
                    vendIdExpected = "VEN_8086"
                    vendCompatibleID = "VEN_8086&CC_03"
                    vendidSC = {"VEN8086_MSDK", "VEN8086_GFXUI"}
                    audioServices = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\servicesaudio.cfg")
                Case GPUVendor.None : vendIdExpected = "NONE"
                Case GPUVendor.All : vendIdExpected = "ALL"
            End Select

            If vendIdExpected = "NONE" OrElse vendIdExpected = "ALL" Then
                Application.Log.AddWarningMessage($"VendID is {vendIdExpected}, this is unexpected, cleaning aborted.")
                Exit Sub
            End If

            If config.CleanCache AndAlso Not config.HasCleanArg Then
                Application.Log.AddMessage("Cleaning " + config.SelectedGPU.ToString() + " cache ...")
                Select Case config.SelectedGPU
                    Case GPUVendor.Nvidia
                        CleanNvidiaCache(config)
                    Case GPUVendor.AMD
                        CleanAmdCache(config)
                    Case GPUVendor.Intel
                        CleanIntelCache(config)
                End Select

                config.Success = True
                Return
            End If

            UpdateTextMethod(UpdateTextTranslated(20) + " " & config.SelectedGPU.ToString() & " " + UpdateTextTranslated(21))
            Application.Log.AddMessage("Uninstalling " + config.SelectedGPU.ToString() + " driver ...")
            UpdateTextMethod(UpdateTextTranslated(22))

            'Removing the services except for the "device driver services"
            'Theses service(s) need to be disabled. Ex: if we remove the AMD driver in normal mode, the device removal will be counter immediately by the device reinstallation.

            Application.Log.AddMessage("Removing service(s) except for the <device driver service(s)>")
            Select Case config.SelectedGPU
                Case GPUVendor.AMD
                    Dim services As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\services.cfg")
                    For Each service As String In services
                        If String.IsNullOrWhiteSpace(service) Then Continue For
                        If serviceInstaller.GetServiceStatus(service, False) = Nothing Then
                            'Service is not present
                        Else
                            Try
                                serviceInstaller.Uninstall(service)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    Next
                    KillProcess("auepmaster")
                    KillProcess("cncmd")    'This avoid an error message when the device is removed.
                    KillProcess("radeonsoftware")    'This avoid an error message when the device is removed.
                    KillProcess("amdow")    'This avoid an error message when the device is removed.
                    KillProcess("amdrsserv")    'This avoid an error message when the device is removed.

                Case GPUVendor.Nvidia
                    Dim services As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\services.cfg")
                    If config.RemoveGFE Then
                        Dim gfeservices As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\gfeservice.cfg")
                        For Each service As String In gfeservices
                            If String.IsNullOrWhiteSpace(service) Then Continue For
                            If serviceInstaller.GetServiceStatus(service, False) = Nothing Then Continue For
                            'Service is not present

                            Try
                                serviceInstaller.Uninstall(service)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Next
                    End If

                    If config.RemoveNVBROADCAST Then
                        Dim nvbservices As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\nvbservice.cfg")
                        KillProcess("nvidia broadcast")
                        For Each service As String In nvbservices
                            If String.IsNullOrWhiteSpace(service) Then Continue For
                            If serviceInstaller.GetServiceStatus(service, False) = Nothing Then Continue For
                            'Service is not present

                            Try
                                serviceInstaller.Uninstall(service)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Next
                    End If

                    For Each service As String In services
                        If String.IsNullOrWhiteSpace(service) Then Continue For
                        If serviceInstaller.GetServiceStatus(service, False) = Nothing Then Continue For
                        Try
                            serviceInstaller.Uninstall(service)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    Next

                    'Fix a possible permission problem when removing the driver via SetupAPI
                    'Temporarynvidiaspeedup(config)
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "software\nvidia corporation", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                    'This is not a typo, it is only to trigger the persmission check on the previous line.
                                End Using
                            Next
                        End If
                    End Using

                Case GPUVendor.Intel
                    Dim services As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\services.cfg")

                    If config.RemoveINTELIGS Then
                        If _win10 Then
                            cleanupEngine.RemoveAppxAsync("IntelArcSoftware").Wait()
                        End If

                        Dim igsServices As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\servicesigs.cfg")
                        For Each service As String In igsServices
                            If String.IsNullOrWhiteSpace(service) Then Continue For
                            If serviceInstaller.GetServiceStatus(service, False) = Nothing Then Continue For
                            'Service is not present

                            Try
                                serviceInstaller.Uninstall(service)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Next
                        KillProcess("arccontrol", "arccontrolassist", "ArcControlLauncher", "ArcControlPostProcessing", "intelgraphicssoftware", "EnduranceGamingProcess")
                    End If

                    For Each service As String In services
                        If String.IsNullOrWhiteSpace(service) Then Continue For
                        If serviceInstaller.GetServiceStatus(service, False) = Nothing Then Continue For
                        'Service is not present

                        Try
                            serviceInstaller.Uninstall(service)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    Next
            End Select


            'Required since Windows 11, uppon device removal, windows 11 try to remove regkeys but leave empty subkeys.
            'So we let DDU do the job as usual because if set later, DDU won't find the required information to clean.

            '	SetupAPI.EnableDevice(GPU, False)
            If config.SelectedGPU = GPUVendor.AMD Then
                CleanAmd(config, True) 'needed since 24h2 it seems
            End If

            If config.SelectedGPU = GPUVendor.Nvidia Then
                CleanNvidia(config, True) 'needed since 24h2 it seems
            End If

            If config.SelectedGPU = GPUVendor.Intel Then
                CleanIntel(config, True) 'needed since 24h2 it seems
            End If

            If (Not Application.LaunchOptions.NoSetupAPI) Then
                If config.SelectedGPU = GPUVendor.AMD AndAlso config.RemoveAMDKMPFD AndAlso (config.Restart OrElse config.Shutdown) Then
                    Try
                        UpdateTextMethod("Start - Check for AMDKMPFD system device.")
                        Application.Log.AddMessage("Executing SetupAPI: check AMDKMPFD system device started")
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", "0a0", False, driverDetails:=True)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, "DEV_0A08", "DEV_0A03") Then
                                        If d.LowerFilters IsNot Nothing AndAlso d.LowerFilters.Length > 0 Then
                                            For Each LowerFilter In d.LowerFilters
                                                If String.IsNullOrWhiteSpace(LowerFilter) Then Continue For
                                                If StrContainsAny(LowerFilter, True, "amdkmpfd") Then
                                                    If d.OemInfs IsNot Nothing AndAlso d.OemInfs.Count > 0 Then
                                                        Dim oem As Inf = d.OemInfs(0)
                                                        If _win10 Then
                                                            SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\PCI.inf", True)
                                                        Else
                                                            SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\machine.inf", True)
                                                        End If
                                                        SetupAPI.RemoveInf(oem, False, True)
                                                        SetupAPI.ReScanDevices()
                                                        Exit For
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                        UpdateTextMethod("End - Check for AMDKMPFD system device.")
                        Application.Log.AddMessage("SetupAPI: Check AMDKMPFD system device Complete .")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                        'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                    UpdateTextMethod(UpdateTextTranslated(28))


                    'We now try to remove the service AMDKMPFD if its lowerfilter is not found

                    If Not Checkamdkmpfd() Then
                        config.NotPresentAMDKMPFD = True
                        UpdateTextMethod("Start - Check for AMDKMPFD service.")
                        cleanupEngine.Cleanserviceprocess({"amdkmpfd"}, config)
                        UpdateTextMethod("End - Check for AMDKMPFD service.")
                    End If
                End If

                If config.SelectedGPU = GPUVendor.Intel Then

                    If config.RemoveIntelNpu Then
                        Application.Log.AddMessage("Executing SetupAPI: Intel NPU and it's childrens") 'PCI Upstream
                        Dim npus = SetupAPI.GetDevices("ComputeAccelerator", vendIdExpected, False, includeChilds:=True)
                        If npus IsNot Nothing AndAlso npus.Count > 0 Then

                            ' Create a list to track devices already removed
                            Dim removedDevices As New List(Of String)
                            For Each npu As SetupAPI.Device In npus
                                If npu IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(npu.DeviceID) Then

                                    ' Check if the device has already been removed
                                    If removedDevices.Contains(npu.ToString) Then
                                        Exit For
                                    End If

                                    If npu.ChildDevices IsNot Nothing AndAlso npu.ChildDevices.Length > 0 Then
                                        Application.Log.AddMessage("SetupAPI: Removing childrens associated to the Intel NPU")
                                        RemoveChiendrensFromDevices(npu.ChildDevices, removedDevices)
                                        Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the Intel NPU completed.")
                                    End If

                                    ' Uninstall the current device
                                    SetupAPI.UninstallDevice(npu)
                                    removedDevices.Add(npu.ToString)

                                End If
                            Next
                            npus.Clear()
                            removedDevices.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Removal of Intel NPUs and it's childrens completed") 'PCI Upstream
                        FrmMain.IntelNpuPresent = Tools.IsIntelNpuPresent()
                    End If

                    Dim PCIEUPORT As String() =
                        {"PCI\VEN_8086&DEV_4910",
                        "PCI\VEN_8086&DEV_4FA0",
                        "PCI\VEN_8086&DEV_4FA1",
                        "PCI\VEN_8086&DEV_E2FF"}

                    Dim PCIEDPORT As String() =
                        {"PCI\VEN_8086&DEV_490F",
                        "PCI\VEN_8086&DEV_4FA4",
                        "PCI\VEN_8086&DEV_E2F0",
                        "PCI\VEN_8086&DEV_E2F1"}

                    'Removing Intel(R) Graphics System Controller Auxiliary Firmware Interface.
                    Application.Log.AddMessage("Executing SetupAPI: PCI-E Upstream Switch port and it's childrens") 'PCI Upstream
                    Dim found = SetupAPI.GetDevices("system", Nothing, False, includeChilds:=True)
                    If found IsNot Nothing AndAlso found.Count > 0 Then

                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID Then
                                    For Each hardwareid As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                        If StrContainsAny(hardwareid, True, PCIEUPORT) Then

                                            ' Check if the device has already been removed
                                            If removedDevices.Contains(d.ToString) Then
                                                Exit For
                                            End If
                                            If d.ChildDevices IsNot Nothing AndAlso d.ChildDevices.Length > 0 Then
                                                Application.Log.AddMessage("SetupAPI: Removing childrens associated to the PCI-E Upstream Switch port")
                                                RemoveChiendrensFromDevices(d.ChildDevices, removedDevices)
                                                Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the PCI-E Upstream Switch port completed.")
                                            End If

                                            ' Uninstall the current device
                                            SetupAPI.UninstallDevice(d)
                                            removedDevices.Add(d.ToString)
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Next
                        found.Clear()
                        removedDevices.Clear()
                    End If
                    Application.Log.AddMessage("SetupAPI: Removal of PCI-E Upstream Switch port and it's childrens completed") 'PCI Upstream

                    Application.Log.AddMessage("Executing SetupAPI: (Leftover) PCI-E Downstream Switch port and it's childrens") 'PCI Upstream

                    found = SetupAPI.GetDevices("system", Nothing, False, includeChilds:=True)  'PCI Downpstream
                    If found IsNot Nothing AndAlso found.Count > 0 Then

                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID Then
                                    For Each hardwareid As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                        If StrContainsAny(hardwareid, True, PCIEDPORT) Then

                                            ' Check if the device has already been removed
                                            If removedDevices.Contains(d.ToString) Then
                                                Exit For
                                            End If
                                            If d.ChildDevices IsNot Nothing AndAlso d.ChildDevices.Length > 0 Then
                                                Application.Log.AddMessage("SetupAPI: Removing childrens associated to the PCI-E Downstream Switch port")
                                                RemoveChiendrensFromDevices(d.ChildDevices, removedDevices)
                                                Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the PCI-E Downstream Switch port completed.")
                                            End If

                                            ' Uninstall the current device
                                            SetupAPI.UninstallDevice(d)
                                            removedDevices.Add(d.ToString)
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Next
                        found.Clear()
                        removedDevices.Clear()
                    End If

                    Application.Log.AddMessage("SetupAPI: Removal of (Leftover) PCI-E Downstream Switch port and it's childrens completed") 'PCI Upstream

                End If

                If config.SelectedGPU = GPUVendor.AMD Then
                    Dim PCIEUPORT As String() =
                        {"PCI\VEN_1002&DEV_1478"}

                    Dim PCIEDPORT As String() =
                        {"PCI\VEN_1002&DEV_1479"}

                    'Removing Intel(R) Graphics System Controller Auxiliary Firmware Interface.
                    Application.Log.AddMessage("Executing SetupAPI: PCI-E Upstream Switch port and it's childrens") 'PCI Upstream
                    Dim found = SetupAPI.GetDevices("system", Nothing, False, includeChilds:=True)
                    If found IsNot Nothing AndAlso found.Count > 0 Then

                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID Then
                                    For Each hardwareid As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                        If StrContainsAny(hardwareid, True, PCIEUPORT) Then

                                            ' Check if the device has already been removed
                                            If removedDevices.Contains(d.ToString) Then
                                                Exit For
                                            End If
                                            If d.ChildDevices IsNot Nothing AndAlso d.ChildDevices.Length > 0 Then
                                                Application.Log.AddMessage("SetupAPI: Removing childrens associated to the PCI-E Upstream Switch port")
                                                RemoveChiendrensFromDevices(d.ChildDevices, removedDevices)
                                                Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the PCI-E Upstream Switch port completed.")
                                            End If

                                            ' Uninstall the current device
                                            SetupAPI.UninstallDevice(d)
                                            removedDevices.Add(d.ToString)
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Next
                        found.Clear()
                        removedDevices.Clear()
                    End If
                    Application.Log.AddMessage("SetupAPI: Removal of PCI-E Upstream Switch port and it's childrens completed") 'PCI Upstream

                    Application.Log.AddMessage("Executing SetupAPI: (Leftover) PCI-E Downstream Switch port and it's childrens") 'PCI Upstream

                    found = SetupAPI.GetDevices("system", Nothing, False, includeChilds:=True)  'PCI Downpstream
                    If found IsNot Nothing AndAlso found.Count > 0 Then

                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID Then
                                    For Each hardwareid As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                        If StrContainsAny(hardwareid, True, PCIEDPORT) Then

                                            ' Check if the device has already been removed
                                            If removedDevices.Contains(d.ToString) Then
                                                Exit For
                                            End If
                                            If d.ChildDevices IsNot Nothing AndAlso d.ChildDevices.Length > 0 Then
                                                Application.Log.AddMessage("SetupAPI: Removing childrens associated to the PCI-E Downstream Switch port")
                                                RemoveChiendrensFromDevices(d.ChildDevices, removedDevices)
                                                Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the PCI-E Downstream Switch port completed.")
                                            End If

                                            ' Uninstall the current device
                                            SetupAPI.UninstallDevice(d)
                                            removedDevices.Add(d.ToString)
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Next
                        found.Clear()
                        removedDevices.Clear()
                    End If

                    Application.Log.AddMessage("SetupAPI: Removal of (Leftover) PCI-E Downstream Switch port and it's childrens completed") 'PCI Upstream

                End If

                '------------------------------------------------------------------------------------
                ' Removing the Audio associated with the GPU + AudioEndpoint+SoftwareComponent(DCH)--
                '------------------------------------------------------------------------------------
                Try
                    UpdateTextMethod(UpdateTextTranslated(24))
                    Application.Log.AddMessage("SetupAPI: Removing Audio device associated to the GPU(s).")
                    Dim audioDevices As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendIdExpected, False, True, True)
                    If audioDevices IsNot Nothing AndAlso audioDevices.Count > 0 Then
                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each audioDevice As SetupAPI.Device In audioDevices
                            If audioDevice IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(audioDevice.Service) Then
                                If Not StrContainsAny(audioDevice.Service, True, audioServices) Then Continue For

                                ' Check if the device has already been removed
                                If removedDevices.Contains(audioDevice.ToString()) Then
                                    Continue For
                                End If
                                If audioDevice.ChildDevices IsNot Nothing AndAlso audioDevice.ChildDevices.Length > 0 Then
                                    'Removing every children of the "Audio device"
                                    Application.Log.AddMessage("SetupAPI: Removing childrens associated to the GPU(s) Audio device.")
                                    RemoveChiendrensFromDevices(audioDevice.ChildDevices, removedDevices)
                                    Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the GPU(s) Audio device completed.")
                                End If

                                SetupAPI.UninstallDevice(audioDevice) 'Removing the audio card

                                removedDevices.Add(audioDevice.ToString)

                                Application.Log.AddMessage("SetupAPI: Removal of the Audio controller associated to the GPU(s) completed.")
                                If Not (config.SelectedGPU = GPUVendor.AMD AndAlso Not config.RemoveAudioBus) Then
                                    Application.Log.AddMessage("SetupAPI: Removing the AudioBus associated to the GPU(s) Audio media.")
                                    For Each Parent As SetupAPI.Device In audioDevice.ParentDevices
                                        If Parent IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(Parent.DeviceID) Then 'TODO : Parent.ChildDevices.Length < 1
                                            Dim audiobusList As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", Parent.DeviceID, False, False, False, True)
                                            If audiobusList IsNot Nothing AndAlso audiobusList.Count > 0 Then
                                                For Each audiobus As SetupAPI.Device In audiobusList
                                                    If audiobus IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(audiobus.Service) Then
                                                        If StrContainsAny(audiobus.Service, True, "HDAudBus") Then
                                                            If audiobus.ExtendedInfs IsNot Nothing AndAlso audiobus.ExtendedInfs.Length > 0 AndAlso
                                                            Not String.IsNullOrWhiteSpace(audiobus.ExtendedInfs(0)) Then
                                                                Dim inf As Inf = GetOemInf(Application.Paths.WinDir & "inf\", audiobus.ExtendedInfs(0))
                                                                SetupAPI.UninstallDevice(audiobus) 'Removing the Audio bus.
                                                                If inf IsNot Nothing Then
                                                                    SetupAPI.RemoveInf(inf, False, True)
                                                                End If
                                                            End If
                                                        End If
                                                    End If
                                                Next
                                            End If
                                        End If
                                    Next
                                End If
                                Application.Log.AddMessage("SetupAPI: Removal of the AudioBus associated to the GPU(s) Audio controler completed.")
                            End If
                        Next
                        audioDevices.Clear()
                        removedDevices.Clear()
                    End If
                    UpdateTextMethod(UpdateTextTranslated(25))
                    Application.Log.AddMessage("SetupAPI: Remove Audio controler Complete.")
                Catch ex As Exception
                    'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Application.Log.AddException(ex)
                End Try

                ImpersonateUser.RunImpersonatedSystem(
                Sub()

                    'Verification is there is still an AMD HD Audio Bus device and set donotremoveamdhdaudiobusfiles to true if thats the case
                    Try
                        FrmMain.DoNotRemoveAmdHdAudioBusFiles = False
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\PCI")
                            If subregkey IsNot Nothing Then
                                For Each child2 As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                    If StrContainsAny(child2, True, "ven_1002") Then
                                        Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2)
                                            If regkey3 IsNot Nothing Then
                                                For Each child3 As String In regkey3.GetSubKeyNames()
                                                    If String.IsNullOrWhiteSpace(child3) Then Continue For
                                                    'need to test more this code. got an error on a friend computer (Wagnard)(Possibly fixed with the trycast)
                                                    array = TryCast(MyRegistry.OpenSubKey(regkey3, child3).GetValue("LowerFilters"), String())
                                                    If (array IsNot Nothing) AndAlso array.Length > 0 Then
                                                        For Each entry As String In array
                                                            If String.IsNullOrWhiteSpace(entry) Then Continue For
                                                            If StrContainsAny(entry, True, "amdkmafd") Then
                                                                Application.Log.AddWarningMessage("Found a remaining AMD audio controller bus ! Preventing the removal of its driverfiles.")
                                                                FrmMain.DoNotRemoveAmdHdAudioBusFiles = True
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
                        FrmMain.DoNotRemoveAmdHdAudioBusFiles = True  ' A security if the code to check fail.
                    End Try

                End Sub)

                ' ----------------------
                ' Removing the videocard
                ' ----------------------

                Try
                    Application.Log.AddMessage("Executing SetupAPI: Remove GPU(s).")
                    Dim GPUs As List(Of SetupAPI.Device) = SetupAPI.GetDevicesByCompatibleID(vendCompatibleID, False, False, True)
                    If GPUs IsNot Nothing AndAlso GPUs.Count > 0 Then
                        ' Create a list to track devices already removed
                        Dim removedDevices As New List(Of String)
                        For Each GPU As SetupAPI.Device In GPUs
                            If GPU IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(GPU.DeviceID) Then
                                If removedDevices.Contains(GPU.ToString()) Then
                                    Continue For
                                End If

                                If GPU.ChildDevices IsNot Nothing AndAlso GPU.ChildDevices.Length > 0 Then
                                    Application.Log.AddMessage("SetupAPI: Removing childrens associated to the GPU(s)")
                                    RemoveChiendrensFromDevices(GPU.ChildDevices, removedDevices)
                                    Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the GPU(s) completed.")
                                End If
                                SetupAPI.UninstallDevice(GPU) 'Then we remove the GPU itself.

                                removedDevices.Add(GPU.ToString())

                            End If
                        Next
                        GPUs.Clear()
                        removedDevices.Clear()
                    End If
                    UpdateTextMethod(UpdateTextTranslated(23))
                    Application.Log.AddMessage("SetupAPI: Remove GPU(s) Complete.")
                Catch ex As Exception
                    'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Application.Log.AddException(ex)
                    config.GPURemovedSuccess = False
                    Exit Sub
                End Try

                If config.SelectedGPU = GPUVendor.AMD Then
                    ' ------------------------------
                    ' Removing some of AMD AudioEndpoints
                    ' ------------------------------
                    Application.Log.AddMessage("Removing AMD Audio Endpoints")
                    Try
                        'AMD AudioEndpoints Removal
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("audioendpoint")
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(d.FriendlyName) Then
                                    If StrContainsAny(d.FriendlyName, True, "amd high definition audio device", "digital audio (hdmi) (high definition audio device)") Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                    Catch ex As Exception
                        Application.ShowThemedNotice(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName)
                        Application.Log.AddException(ex)
                    End Try

                    ' -----------------------------------
                    ' Removing AMD Streaming Audio Device 
                    ' -----------------------------------
                    Application.Log.AddMessage("Removing AMD Streaming Audio Device")
                    Try
                        'AMD AudioEndpoints Removal
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media")
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, "ROOT\AMDSAFD") Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                    Catch ex As Exception
                        Application.ShowThemedNotice(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName)
                        Application.Log.AddException(ex)
                    End Try


                    '------------------------------------------------------------
                    'Removing AMD Crash Defender and AMD Link Controler Emulation
                    '------------------------------------------------------------
                    Try
                        Application.Log.AddMessage("Executing SetupAPI: Remove AMD Crash Defender and AMD Link Controler Emulation.")
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, "ROOT\AMDXE", "ROOT\AMDLOG") Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove AMD Crash Defender and AMD Link Controler Emulation.")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    Try
                        UpdateTextMethod("Start - Check for AMD-OpenCL / AMD-Windows")
                        Application.Log.AddMessage("Executing SetupAPI: check AMD-OpenCL / OGL / AMD-Windows SoftwareComponent / AMD-Vulkan User Mode Driver and others started")
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("SoftwareComponent", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso StrContainsAny(d.Description, True, "AMD-Windows Support Components", "AMD-OpenCL User Mode Driver", "AMD-Vulkan User Mode Driver", "AMD-OpenGL User Mode Driver", "AMD-Dynamic Audio Noise Supression") Then
                                    SetupAPI.UninstallDevice(d)
                                End If
                            Next
                            found.Clear()
                        End If
                        UpdateTextMethod("End - Check for AMD-OpenCL system device.")
                        Application.Log.AddMessage("SetupAPI: Check AMD-OpenCL system device Complete .")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                End If

                If config.SelectedGPU = GPUVendor.Intel Then

                    Dim igcc As String() =
                    {"VEN8086_IGCC",
                    "VEN8086_IGS"}

                    Dim intelACX As String() =
                    {"VEN_8086&DEV_2812",
                    "VEN_8086&DEV_2815",
                    "VEN_8086&DEV_2818",
                    "VEN_8086&DEV_2816",
                    "VEN_8086&DEV_2814",
                    "VEN_8086&DEV_2819",
                    "VEN_8086&DEV_281C",
                    "VEN_8086&DEV_281D",
                    "VEN_8086&DEV_281E",
                    "VEN_8086&DEV_281F"}

                    Dim found = SetupAPI.GetDevices("SoftwareComponent", Nothing, False)
                    Try
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso StrContainsAny(d.Description, True, "Intel(R) Graphics firmware update service", "Intel GFX Firmware Update Service") Then
                                    SetupAPI.UninstallDevice(d)
                                    Continue For
                                End If
                                If d IsNot Nothing AndAlso StrContainsAny(d.FriendlyName, True, "Intel(R) Graphics firmware update service", "Intel GFX Firmware Update Service") Then
                                    SetupAPI.UninstallDevice(d)
                                    Continue For
                                End If
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If d.HasHardwareID Then   'Workaround for a bug report we got.
                                        For Each hardwareid As String In d.HardwareIDs
                                            If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                            If StrContainsAny(hardwareid, True, igcc) Then
                                                SetupAPI.UninstallDevice(d)
                                                Exit For
                                            End If
                                        Next
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    'Removing Intel WIdI bus Enumerator
                    Application.Log.AddMessage("Executing SetupAPI: Remove Intel WIdI bus Enumerator, CTA and NF I2C system driver.")
                    found = SetupAPI.GetDevices("system", Nothing, False)
                    If found IsNot Nothing AndAlso found.Count > 0 Then
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID Then   'Workaround for a bug report we got.
                                    For Each hardwareid As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                        If StrContainsAny(hardwareid, True, "root\iwdbus", "VIDEO\INTC_CTA", "VIDEO\INTC_I2C", "VIDEO\INTC_PMT", "VIDEO\INTC_HECI_2", "{55BC022C-955B-4D87-A88D-D3E68CBEB2F4}\CT_28bb0e51-b4b0-4509-9e51-78d48daae82b") Then
                                            SetupAPI.UninstallDevice(d)
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Next
                        found.Clear()
                    End If
                    Application.Log.AddMessage("SetupAPI: Remove Intel WIdI bus Enumerator Complete .")

                    'Removing Mini CTA Driver
                    Application.Log.AddMessage("Executing SetupAPI: Remove Intel Mini CTA Driver")
                    found = SetupAPI.GetDevices("CTA Driver Devices", Nothing, False)
                    If found IsNot Nothing AndAlso found.Count > 0 Then
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                If d.HasHardwareID AndAlso StrContainsAny(d.HardwareIDs(0), True, "VEN_8086&DEV_490E", "VEN_8086&DEV_4F93", "PCI\VEN_8086&DEV_4F95") Then  'Workaround for a bug report we got.
                                    SetupAPI.UninstallDevice(d)
                                End If
                            End If
                        Next
                        found.Clear()
                    End If
                    Application.Log.AddMessage("SetupAPI: Remove Intel Mini CTA Driver Complete .")

                    'remove left over audio
                    found = SetupAPI.GetDevices("media", vendIdExpected, False)
                    Try
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If d.HasHardwareID Then   'Workaround for a bug report we got.
                                        For Each hardwareid As String In d.HardwareIDs
                                            If String.IsNullOrWhiteSpace(hardwareid) Then Continue For
                                            If StrContainsAny(hardwareid, True, intelACX) Then
                                                SetupAPI.UninstallDevice(d)
                                                Exit For
                                            End If
                                        Next
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Application.Log.AddMessage("SetupAPI: Remove Intel(R) Leftover audio complete")

                End If

                'Here I remove 3dVision USB Adapter and USB type C(RTX).
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

                        Dim USBTypeC As String() =
                        {"PCI\VEN_10DE&DEV_1AD7",
                        "PCI\VEN_10DE&DEV_1AD9",
                        "PCI\VEN_10DE&DEV_1ADB",
                        "PCI\VEN_10DE&DEV_1AED"}

                        '3dVision Removal
                        Application.Log.AddMessage("Executing SetupAPI: Remove 3dVision USB Adapter.")
                        Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, HWID3dvision) Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove 3dVision USB Adapter Complete.")

                        'USB Type C Removal
                        Application.Log.AddMessage("Executing SetupAPI: Remove USB type C(RTX).")
                        found = SetupAPI.GetDevices("usb", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, USBTypeC) Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove USB type C(RTX) Complete.")

                        'NVIDIA SHIELD Wireless Controller Trackpad
                        Application.Log.AddMessage("Executing SetupAPI: Remove NVIDIA SHIELD Wireless Controller Trackpad.")
                        found = SetupAPI.GetDevices("mouse", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    If StrContainsAny(d.HardwareIDs(0), True, "hid\vid_0955&pid_7210") Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove NVIDIA SHIELD Wireless Controller Trackpad Complete.")

                        'NVIDIA Platform Controllers and Framework
                        Application.Log.AddMessage("Executing SetupAPI: Remove NVIDIA Platform Controllers and Framework")
                        found = SetupAPI.GetDevices("SoftwareDevice", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                    For Each HwID As String In d.HardwareIDs
                                        If String.IsNullOrWhiteSpace(HwID) Then Continue For
                                        If StrContainsAny(HwID, True, "ACPI\NVDA0820") Then
                                            SetupAPI.UninstallDevice(d)
                                            Exit For
                                        End If
                                    Next
                                End If
                            Next
                            found.Clear()
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove NVIDIA Platform Controllers and Framework Complete.")

                        If config.RemoveNVBROADCAST Then
                            ' NVIDIA Broadcast(Wave Extensible) (WDM) Removal
                            Application.Log.AddMessage("Executing SetupAPI: Remove NVIDIA Broadcast Audio Device (Wave Extensible) (WDM).")
                            found = SetupAPI.GetDevicesByHID("USB\VID_0956&PID_9001", False, False, True)
                            Dim removedDevices As New List(Of String)
                            If found IsNot Nothing AndAlso found.Count > 0 Then
                                For Each d As SetupAPI.Device In found
                                    If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                        If StrContainsAny(d.HardwareIDs(0), True, "USB\VID_0956&PID_9001") Then
                                            If removedDevices.Contains(d.ToString()) Then
                                                Continue For
                                            End If

                                            If d.ChildDevices IsNot Nothing AndAlso d.ChildDevices.Length > 0 Then
                                                Application.Log.AddMessage("SetupAPI: Removing childrens associated to the GPU(s)")
                                                RemoveChiendrensFromDevices(d.ChildDevices, removedDevices)
                                                Application.Log.AddMessage("SetupAPI: Removal of the childrens associated to the GPU(s) completed.")
                                            End If
                                            SetupAPI.UninstallDevice(d)
                                            removedDevices.Add(d.ToString)
                                        End If
                                    End If
                                Next
                                found.Clear()
                            End If
                        End If
                        Application.Log.AddMessage("SetupAPI: Remove NVIDIA Broadcast Audio Device (Wave Extensible) (WDM) Complete .")


                        'nVidia AudioEndpoints leftover Removal
                        found = SetupAPI.GetDevices("audioendpoint", Nothing, False)
                        If found IsNot Nothing AndAlso found.Count > 0 Then
                            For Each d As SetupAPI.Device In found
                                If d IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(d.FriendlyName) Then
                                    If StrContainsAny(d.FriendlyName, True, "nvidia virtual audio device") AndAlso config.RemoveGFE Then
                                        SetupAPI.UninstallDevice(d)
                                        Continue For
                                    End If
                                    If StrContainsAny(d.FriendlyName, True, "nvidia high definition audio") Then
                                        SetupAPI.UninstallDevice(d)
                                        Continue For
                                    End If
                                    If StrContainsAny(d.FriendlyName, True, "NVIDIA Broadcast") AndAlso config.RemoveNVBROADCAST Then
                                        SetupAPI.UninstallDevice(d)
                                    End If
                                End If
                            Next
                            found.Clear()
                        End If

                        If config.RemoveGFE Then
                            ' NVIDIA Virtual Audio Device (Wave Extensible) (WDM) Removal
                            Application.Log.AddMessage("Executing SetupAPI: Remove NVIDIA Virtual Audio Device (Wave Extensible) (WDM).")
                            found = SetupAPI.GetDevicesByHID("USB\VID_0955&PID_9000", False, False, False)
                            If found IsNot Nothing AndAlso found.Count > 0 Then
                                For Each d As SetupAPI.Device In found
                                    If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                        If StrContainsAny(d.HardwareIDs(0), True, "USB\VID_0955&PID_9000") Then
                                            SetupAPI.UninstallDevice(d)
                                        End If
                                    End If
                                Next
                                found.Clear()
                            End If
                            Application.Log.AddMessage("SetupAPI: Remove NVIDIA Virtual Audio Device (Wave Extensible) (WDM) Complete .")

                            ' NVIDIA NvModuleTracker Device Removal
                            Application.Log.AddMessage("Executing SetupAPI: Remove NVIDIA NvModuleTracker Device.")
                            found = SetupAPI.GetDevicesByHID("ROOT\NVMODULETRACKER", False, False, False)
                            If found IsNot Nothing AndAlso found.Count > 0 Then
                                For Each d As SetupAPI.Device In found
                                    If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                        If StrContainsAny(d.HardwareIDs(0), True, "ROOT\NVMODULETRACKER") Then
                                            SetupAPI.UninstallDevice(d)
                                        End If
                                    End If
                                Next
                                found.Clear()
                            End If
                            Application.Log.AddMessage("SetupAPI: Remove NVIDIA NvModuleTracker Device Complete .")
                        End If


                        '-----------------------
                        'Removing NVVHCI
                        '-----------------------

                        Try
                            Application.Log.AddMessage("Executing SetupAPI: Remove NVVHCI.")
                            found = SetupAPI.GetDevicesByHID("ROOT\NVVHCI", False, False, False)
                            If found IsNot Nothing AndAlso found.Count > 0 Then
                                For Each d As SetupAPI.Device In found
                                    If d IsNot Nothing AndAlso d.HardwareIDs IsNot Nothing AndAlso d.HardwareIDs.Length > 0 Then
                                        If StrContainsAny(d.HardwareIDs(0), True, "ROOT\NVVHCI") Then
                                            SetupAPI.UninstallDevice(d)
                                        End If
                                    End If
                                Next
                                found.Clear()
                            End If
                            Application.Log.AddMessage("SetupAPI: Remove NVVHCI Complete.")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try

                    Catch ex As Exception
                        Application.Log.AddException(ex)
                        'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If

                Application.Log.AddMessage("SetupAPI: Remove Audio/HDMI Complete")
                If config.SelectedGPU <> GPUVendor.Intel Then
                    cleanupEngine.Cleandriverstore(config)
                End If

                'removing monitor and hidden monitor

                If config.RemoveMonitors Then
                    Application.Log.AddMessage("Executing SetupAPI: Remove Monitor(s) started")
                    Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("monitor", Nothing, False)
                    If found IsNot Nothing AndAlso found.Count > 0 Then
                        For Each d As SetupAPI.Device In found
                            If d IsNot Nothing Then
                                SetupAPI.UninstallDevice(d)
                            End If
                        Next
                        found.Clear()
                    End If
                    UpdateTextMethod(UpdateTextTranslated(27))
                    cleanupEngine.RemoveMonitorConfiguration()
                    Application.Log.AddMessage("SetupAPI: Remove Monitor(s) Complete .")
                End If

                If config.SelectedGPU = GPUVendor.AMD Then

                End If

            End If

            If config.SelectedGPU = GPUVendor.AMD Then
                CleanAmdServiceProcess(config)
                CleanAmd(config)
                CleanAmdFolders(config)
            End If

            If config.SelectedGPU = GPUVendor.Nvidia Then
                Checkpcieroot(config)
                CleanNvidiaServiceProcess(config)
                CleanNvidia(config)
                CleanNvidiaFolders(config)
                cleanupEngine.RemoveRegDeviceSoftware("NVIDIA CoInstaller Display.Driver")
            End If

            If config.SelectedGPU = GPUVendor.Intel Then
                CleanIntelServiceProcess(config)
                CleanIntel(config)
                CleanIntelFolders(config)
            End If

            cleanupEngine.Cleandriverstore(config)
            cleanupEngine.Fixregistrydriverstore(config)
            config.Success = True
        End Sub

        Private Sub KillProcess(ByVal ParamArray processnames As String())
            For Each processName As String In processnames
                If String.IsNullOrEmpty(processName) Then
                    Continue For
                End If
                For Each process As Process In Process.GetProcessesByName(processName)
                    Try
                        process.Kill()
                        Application.Log.AddMessage(String.Concat("Killed process: ", processName))
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
        End Sub

        Private Sub RemoveChiendrensFromDevices(devices As SetupAPI.Device(), removedDevices As List(Of String))
            For Each device As SetupAPI.Device In devices
                If device IsNot Nothing Then
                    If removedDevices.Contains(device.ToString()) Then
                        Continue For
                    End If
                    ' Check if the device has child devices
                    If device.ChildDevices IsNot Nothing AndAlso device.ChildDevices.Length > 0 Then
                        ' Recursively remove child devices
                        RemoveChiendrensFromDevices(device.ChildDevices, removedDevices)
                    End If
                    ' Uninstall the current device
                    SetupAPI.UninstallDevice(device)
                    removedDevices.Add(device.ToString)
                End If
            Next
        End Sub

        Private Sub CleanAmdServiceProcess(ByVal config As ThreadSettings)
            Dim cleanupEngine As New CleanupEngine
            Dim services As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\services.cfg")
            Dim objAuto As AutoResetEvent = New AutoResetEvent(False)

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning Process/Services...")
                cleanupEngine.Cleanserviceprocess(services, config)    '// add each line as String Array.

                Dim killpid As New ProcessStartInfo With {
.FileName = config.Paths.System32 & "cmd.exe",
.Arguments = " /C" & "taskkill /f /im CLIStart.exe",
.UseShellExecute = False,
.CreateNoWindow = True,
.RedirectStandardOutput = False
}
                Dim processkillpid As New Process With {
.StartInfo = killpid
}
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
"jusched",
"radeonsoftware")
            End Sub)
            Application.Log.AddMessage("Process/Services CleanUP Complete")
            objAuto.WaitOne(10)

        End Sub

        Private Sub CleanAmd(ByVal config As ThreadSettings, ByVal Optional preclean As Boolean = False)
            Dim CleanupEngine As New CleanupEngine
            Dim wantedvalue As String = Nothing
            Dim wantedvalue2 As String = Nothing
            Dim filePath As String
            Dim packages = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\packages.cfg")   '// add each line as String Array.
            Dim classroot As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\classroot.cfg")
            Dim reginterface As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\interface.cfg")
            Dim clsidleftover As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\clsidleftover.cfg")
            Dim driverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfiles.cfg")
            Dim driverfilesKMPFD As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfilesKMPFD.cfg")
            Dim driverfilesKMAFD As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfilesKMAFD.cfg")

            If preclean Then

                UpdateTextMethod(UpdateTextTranslated(2))
                Application.Log.AddMessage("Cleaning registry Part 1/2")


                'Delete AMD regkey
                'Deleting DCOM object



                'Removal of the (DCH) AMD control panel comming from the Window Store. (In progress...)
                If _win10 Then
                    If config.RemoveAMDCP Then
                        CleanupEngine.RemoveAppxAsync("AMDRadeonSoftware").Wait()
                        CleanupEngine.RemoveAppxAsync("AdvancedMicroDevicesInc-RSXCM").Wait()
                    End If
                    CleanupEngine.RemoveAppxAsync("AdvancedMicroDevicesInc-2.AMDLink").Wait()
                End If

                ImpersonateUser.RunImpersonatedSystem(
                Sub()

                    Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")
                    CleanupEngine.ClassRoot(classroot, config)  '// add each line as String Array.

                    '-----------------
                    'interface cleanup
                    '-----------------

                    CleanupEngine.Interfaces(reginterface)    '// add each line as String Array.

                    Application.Log.AddMessage("Instance class cleanUP")
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", False)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child, False)
                                            If subregkey IsNot Nothing Then
                                                Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\Instance", False)
                                                    If subregkey2 IsNot Nothing Then
                                                        For Each child2 As String In subregkey2.GetSubKeyNames()
                                                            If String.IsNullOrWhiteSpace(child2) = False Then
                                                                Using superkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\Instance\" & child2)
                                                                    If superkey IsNot Nothing Then
                                                                        If Not String.IsNullOrWhiteSpace(superkey.GetValue("FriendlyName", String.Empty).ToString) Then
                                                                            wantedvalue2 = superkey.GetValue("FriendlyName", String.Empty).ToString
                                                                            If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                                                If wantedvalue2.ToLower.Contains("ati mpeg") Or
wantedvalue2.ToLower.Contains("amd mjpeg") Or
wantedvalue2.ToLower.Contains("ati ticker") Or
wantedvalue2.ToLower.Contains("mmace softemu") Or
wantedvalue2.ToLower.Contains("mmace deinterlace") Or
wantedvalue2.ToLower.Contains("amd video") Or
wantedvalue2.ToLower.Contains("mmace procamp") Or
wantedvalue2.ToLower.Contains("ati video") Then
                                                                                    Try
                                                                                        Deletesubregkey(Registry.ClassesRoot, "CLSID\" & child & "\Instance\" & child2, False)
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
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", False)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child) = False Then
                                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child, False)
                                                If subregkey IsNot Nothing Then
                                                    Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance", False)
                                                        If subregkey2 IsNot Nothing Then
                                                            For Each child2 As String In subregkey2.GetSubKeyNames()
                                                                If String.IsNullOrWhiteSpace(child2) = False Then
                                                                    Using superkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2)
                                                                        If superkey IsNot Nothing Then
                                                                            If String.IsNullOrWhiteSpace(superkey.GetValue("FriendlyName", String.Empty).ToString) = False Then
                                                                                wantedvalue2 = superkey.GetValue("FriendlyName", String.Empty).ToString
                                                                                If wantedvalue2.ToLower.Contains("ati mpeg") Or
wantedvalue2.ToLower.Contains("amd mjpeg") Or
wantedvalue2.ToLower.Contains("ati ticker") Or
wantedvalue2.ToLower.Contains("mmace softemu") Or
wantedvalue2.ToLower.Contains("mmace deinterlace") Or
wantedvalue2.ToLower.Contains("mmace procamp") Or
wantedvalue2.ToLower.Contains("amd video") Or
wantedvalue2.ToLower.Contains("ati video") Then
                                                                                    Try
                                                                                        Deletesubregkey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2, False)
                                                                                    Catch ex As Exception
                                                                                        Application.Log.AddException(ex)
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
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                        If regkey2 IsNot Nothing Then
                                            If String.IsNullOrWhiteSpace(regkey2.GetValue("", String.Empty).ToString) Then Continue For
                                            If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
                                                Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Categories")
                                                    If regkey3 IsNot Nothing Then
                                                        For Each child2 As String In regkey3.GetSubKeyNames
                                                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                            Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Categories\" & child2, True)
                                                                If regkey4 IsNot Nothing Then
                                                                    Try
                                                                        Deletesubregkey(regkey4, child, False)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                                End If
                                                            End Using
                                                        Next
                                                    End If
                                                End Using
                                                Try
                                                    Deletesubregkey(regkey, child)
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
                    If IntPtr.Size = 8 Then
                        Try
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                            If regkey2 IsNot Nothing Then
                                                If String.IsNullOrWhiteSpace(regkey2.GetValue("", String.Empty).ToString) Then Continue For
                                                If StrContainsAny(regkey2.GetValue("").ToString, True, "amd d3d11 hardware mft", "amd fast (dnd) decoder", "amd h.264 hardware mft encoder", "amd playback decoder mft") Then
                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Categories")
                                                        If regkey3 IsNot Nothing Then
                                                            For Each child2 As String In regkey3.GetSubKeyNames
                                                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                                Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Categories\" & child2, True)
                                                                    If regkey4 IsNot Nothing Then
                                                                        Try
                                                                            Deletesubregkey(regkey4, child, False)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                End Using
                                                            Next
                                                        End If
                                                    End Using
                                                    Try
                                                        Deletesubregkey(regkey, child)
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
                    End If
                    Application.Log.AddMessage("AppID and clsidleftover cleanUP")
                    'old dcom 

                    CLSIDCleanThread(clsidleftover)
                End Sub)
                Return
            End If

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning registry Part 2/2")
                Application.Log.AddMessage("Record CleanUP")

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows Media Foundation", True)
                        If regkey IsNot Nothing Then
                            Deletevalue(regkey, "EMUOPM", False)
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                '--------------
                'Record cleanup
                '--------------
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Record", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                    If subregkey IsNot Nothing Then
                                        For Each childs As String In subregkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(childs) Then Continue For
                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, childs, False)
                                                If regkey2 IsNot Nothing Then
                                                    If String.IsNullOrWhiteSpace(regkey2.GetValue("Assembly", String.Empty).ToString) Then Continue For
                                                    If StrContainsAny(regkey2.GetValue("Assembly", String.Empty).ToString, True, "aticccom") Then
                                                        Try
                                                            Deletesubregkey(regkey, child)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
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
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Classes\Installer\Assemblies", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("ati.ace") Then
                                        Try
                                            Deletesubregkey(regkey, child)
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

                '----------------------
                'End Assemblies cleanUP
                '----------------------


                'end of decom?
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
"Display\shellex\PropertySheetHandlers", True)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(regkey, "ATIACE", False)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                If config.RemoveVulkan Then
                    CleanVulkan(config)
                End If

                Application.Log.AddMessage("ngenservice Clean")

                '----------------------
                '.net ngenservice clean
                '----------------------
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("ati.ace") Then
                                        Try
                                            Deletesubregkey(regkey, child)
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

                '-----------------------------
                'End of .net ngenservice clean
                '-----------------------------

                '-----------------------------
                'Shell extensions\aprouved
                '-----------------------------
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                                        Try
                                            Deletevalue(regkey, child)
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
                If IntPtr.Size = 8 Then
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                                            Try
                                                Deletevalue(regkey, child)
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
                End If
                '-----------------------------
                'End Shell extensions\aprouved
                '-----------------------------

                Application.Log.AddMessage("Pnplockdownfiles region cleanUP")
                CleanupEngine.PnpLockdownFiles(driverfiles)   '// add each line as String Array.

                If config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD Then
                    CleanupEngine.PnpLockdownFiles(driverfilesKMPFD)
                End If
                If config.RemoveAudioBus AndAlso FrmMain.DoNotRemoveAmdHdAudioBusFiles = False Then
                    CleanupEngine.PnpLockdownFiles(driverfilesKMAFD)
                End If
                If config.RemoveVulkan Then
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If
                If config.SelectedGPU = GPUVendor.AMD Then
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\amdkmdap", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups", False)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If
                If IntPtr.Size = 8 Then
                    If config.RemoveVulkan Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos", False)
                        Catch ex As Exception
                        End Try
                    End If
                    If config.SelectedGPU = GPUVendor.AMD Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE", False)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If



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
                                    If String.IsNullOrWhiteSpace(childs) = False Then
                                        If StrContainsAny(childs, True, "controlset") Then
                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"SYSTEM\" & childs & "\Enum\Root")
                                                If regkey IsNot Nothing Then
                                                    For Each child As String In regkey.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                                        If child.ToLower.Contains("legacy_amdkmdag") Or
(child.ToLower.Contains("legacy_amdkmpfd") AndAlso config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD) Or
child.ToLower.Contains("legacy_amdacpksd") Then
                                                            Try
                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\" & childs & "\Enum\Root\" & child)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
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
                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                        If subregkey IsNot Nothing Then
                            For Each child2 As String In subregkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                If StrContainsAny(child2, True, "controlset") Then
                                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                                        If regkey IsNot Nothing Then
                                            For Each child As String In regkey.GetValueNames()
                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                If child.Contains("AMDAPPSDKROOT") Then
                                                    Try
                                                        Deletevalue(regkey, child)
                                                    Catch ex As Exception
                                                        Application.Log.AddExceptionWithValues(ex, "Path: " + regkey.ToString + " Key : " + child)
                                                    End Try
                                                End If
                                                If child.Contains("Path") Then
                                                    wantedvalue = CType(regkey.GetValue(child, String.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames), String)
                                                    If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                        Dim changed As Boolean = False
                                                        Dim originalKind As RegistryValueKind
                                                        originalKind = regkey.GetValueKind(child)

                                                        Try
                                                            If wantedvalue.IndexOf(";" & _sysdrv & "program files (x86)\amd app\bin\x86_64", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, ";" & _sysdrv & "program files (x86)\amd app\bin\x86_64", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(_sysdrv & "program files (x86)\amd app\bin\x86_64;", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, _sysdrv & "program files (x86)\amd app\bin\x86_64;", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(";" & _sysdrv & "program files (x86)\amd app\bin\x86", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, ";" & _sysdrv & "program files (x86)\amd app\bin\x86", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(_sysdrv & "program files (x86)\amd app\bin\x86;", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, _sysdrv & "program files (x86)\amd app\bin\x86;", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(";" & _sysdrv & "program Files (x86)\ati technologies\ati.ace\core-static", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, ";" & _sysdrv & "program Files (x86)\ati technologies\ati.ace\core-static", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(_sysdrv & "program Files (x86)\ati technologies\ati.ace\core-static;", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, _sysdrv & "program Files (x86)\ati technologies\ati.ace\core-static;", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(";" & _sysdrv & "program Files (x86)\amd\ati.ace\core-static", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, ";" & _sysdrv & "program Files (x86)\amd\ati.ace\core-static", "")
                                                                changed = True
                                                            End If

                                                            If wantedvalue.IndexOf(_sysdrv & "program Files (x86)\amd\ati.ace\core-static;", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                                wantedvalue = ReplaceIgnoreCase(wantedvalue, _sysdrv & "program Files (x86)\amd\ati.ace\core-static;", "")
                                                                changed = True
                                                            End If

                                                            If changed Then
                                                                Try
                                                                    regkey.SetValue(child, wantedvalue, originalKind)
                                                                Catch ex As Exception
                                                                    Application.Log.AddException(ex)
                                                                End Try
                                                            End If

                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
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

                FixBrokenPathIfNeeded()

                '-----------------------
                'remove event view stuff
                '-----------------------
                Application.Log.AddMessage("Remove eventviewer stuff")
                Try
                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                        If subregkey IsNot Nothing Then
                            For Each child2 As String In subregkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                If StrContainsAny(child2, True, "controlset") Then
                                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\eventlog", True)
                                        If regkey IsNot Nothing Then
                                            For Each child As String In regkey.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                If StrContainsAny(child, True, "aceeventlog") Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                        Continue For
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If

                                                If StrContainsAny(child, True, "Application") Then
                                                    Using applicationKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                                        If applicationKey IsNot Nothing Then
                                                            ' check for any of these sub‑keys…
                                                            Dim toDelete = New String() {
                                                            "AMD_ANR_BG_PROC",
                                                            "ATIeRecord"
                                                        }

                                                            For Each sk In toDelete
                                                                If applicationKey.GetSubKeyNames().Contains(sk, StringComparer.OrdinalIgnoreCase) Then
                                                                    Try
                                                                        Deletesubregkey(applicationKey, sk)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                                End If
                                                            Next
                                                            Continue For
                                                        End If
                                                    End Using
                                                End If

                                                If StrContainsAny(child, True, "System") Then
                                                    Using systemKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                                        If systemKey IsNot Nothing Then
                                                            ' check for any of these sub‑keys…
                                                            Dim toDelete = New String() {
                                                            "amdkmdag",
                                                            "amdkmdap"
                                                        }

                                                            For Each sk In toDelete
                                                                If systemKey.GetSubKeyNames().Contains(sk, StringComparer.OrdinalIgnoreCase) Then
                                                                    Try
                                                                        Deletesubregkey(systemKey, sk)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                                End If
                                                            Next
                                                            Continue For
                                                        End If
                                                    End Using
                                                End If
                                            Next
                                        End If
                                    End Using
                                    Try
                                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\Atierecord", False)
                                            If regkey IsNot Nothing Then
                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Services\Atierecord", False)
                                            End If
                                        End Using
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
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
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If child.Contains("ACE") Then
                                    Try
                                        Deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
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
                        If String.IsNullOrWhiteSpace(users) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If child.StartsWith("ATI") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End Using
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "radeonsettings.exe", "amdrsserv.exe") Then
                                        Try
                                            Deletevalue(regkey, child)
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

                ' to fix later, the range is too large and could lead to problems.
                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If String.IsNullOrWhiteSpace(users) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\AMD", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "AIM", "CN", "DVR", "HKIDs", "MOBILE", "SCENE", "SA", "AMDInstallManager", "BOOST", "CHILL", "DELAG") Then
                                        Deletesubregkey(regkey, child)
                                    End If
                                Next
                                If regkey.SubKeyCount = 0 Then
                                    Try
                                        Deletesubregkey(Registry.Users, users & "\Software\AMD")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                Else
                                    For Each data As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End Using
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\ATI", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "ace", "appprofiles", "A4", "install") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "Software\ATI")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\AUEP", True)
                        If regkey IsNot Nothing Then
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "Software\AUEP")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\ATI Technologies", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "cbt") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "ati catalyst control center") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "cds") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "log") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "prw") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "install") Then
                                        'here we check the install path location in case CCC is not installed on the system drive.  A kill to explorer must be made
                                        'to help cleaning in normal mode.
                                        If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
                                            Application.Log.AddMessage("Killing Explorer.exe")
                                            KillProcess("explorer")
                                        End If
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                            If regkey2 IsNot Nothing Then
                                                If Not String.IsNullOrWhiteSpace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then
                                                    filePath = regkey2.GetValue("InstallDir", String.Empty).ToString
                                                    If Not String.IsNullOrWhiteSpace(filePath) AndAlso _fileIo.ExistsDir(filePath) Then
                                                        For Each childf As String In _fileIo.GetDirectories(filePath)
                                                            If String.IsNullOrWhiteSpace(childf) Then Continue For
                                                            If StrContainsAny(childf, True, "ati.ace", "cnext", "cim", "Performance Profile Client") Then
                                                                Delete(childf)
                                                            End If
                                                            If config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD AndAlso StrContainsAny(childf, True, "amdkmpfd") Then
                                                                Delete(childf)
                                                            End If
                                                        Next
                                                        If _fileIo.CountDirectories(filePath) = 0 Then
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
                                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                    If StrContainsAny(child2, True, "A464", "ati catalyst", "ati mcat", "avt", "ccc", "cnext", "amd app sdk", "packages", "distribution", "ppc",
"wirelessdisplay", "hydravision", "avivo", "ati display driver", "installed drivers", "steadyvideo", "amd dvr", "ati problem report wizard", "amd problem report wizard", "cnbranding", "WVR64") Then
                                                        Try
                                                            Deletesubregkey(regkey2, child2)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                Next
                                                For Each values As String In regkey2.GetValueNames()
                                                    If String.IsNullOrWhiteSpace(values) Then Continue For
                                                    Try
                                                        Deletevalue(regkey2, values) 'This is for windows 7, it prevent removing the South Bridge and fix the Catalyst "Upgrade"
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                Next
                                                If regkey2.SubKeyCount = 0 Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                Else
                                                    For Each data As String In regkey2.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
                                                    Next
                                                End If
                                            End If
                                        End Using
                                    End If
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "Software\ATI Technologies")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\AMD", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "AMDInstallManager", "aim", "eeu", "fuel", "cn", "chill", "mftvdecoder", "dvr", "gpu", "amdanalytics", "ppc", "DU", "DUTrack", "SmartDC") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    'If StrContainsAny(child, True, "AMDInstallManager") Then  'Just a safety here....
                                    '	Try
                                    '		Deletesubregkey(regkey, child)
                                    '	Catch ex As Exception
                                    '		Application.Log.AddException(ex)
                                    '	End Try
                                    'End If
                                    If StrContainsAny(child, True, "install") Then  'Just a safety here....
                                        Using installKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                            If installKey IsNot Nothing Then
                                                For Each key As String In installKey.GetSubKeyNames()
                                                    If StrContainsAny(key, True, "autoupdate", "checkforupdates", "lastrun", "progress") Then
                                                        Try
                                                            Deletesubregkey(installKey, key)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                Next
                                            End If
                                        End Using
                                        Dim shouldDelete As Boolean = False

                                        Using checkKey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                            If checkKey IsNot Nothing AndAlso checkKey.SubKeyCount = 0 Then
                                                shouldDelete = True
                                            End If
                                        End Using

                                        If shouldDelete Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "Software\AMD")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\AMDDVR", True)
                        If regkey IsNot Nothing Then
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "Software\AMDDVR")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                If IntPtr.Size = 8 Then
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\ATI", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        If StrContainsAny(child, True, "ace", "appprofiles", "A4") Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                Next
                                If regkey.SubKeyCount = 0 Then
                                    Try
                                        Deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\ATI")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                Else
                                    For Each data As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End Using
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\AMD", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        If child.ToLower.Contains("eeu") Or
child.ToLower.Contains("mftvdecoder") Then
                                            Deletesubregkey(regkey, child)
                                        End If
                                    End If
                                Next
                                If regkey.SubKeyCount = 0 Then
                                    Deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\AMD")
                                Else
                                    For Each data As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\ATI Technologies", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        If StrContainsAny(child, True, "system wide settings", "log", "prw") Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                        If child.ToLower.Contains("install") Then
                                            'here we check the install path location in case CCC is not installed on the system drive.  A kill to explorer must be made
                                            'to help cleaning in normal mode.
                                            If System.Windows.Forms.SystemInformation.BootMode = WinForm.BootMode.Normal Then
                                                Application.Log.AddMessage("Killing Explorer.exe")
                                                KillProcess("explorer")
                                            End If
                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                                If regkey2 IsNot Nothing Then
                                                    If Not String.IsNullOrWhiteSpace(regkey2.GetValue("InstallDir", String.Empty).ToString) Then
                                                        filePath = regkey2.GetValue("InstallDir", String.Empty).ToString
                                                        If Not String.IsNullOrWhiteSpace(filePath) AndAlso _fileIo.ExistsDir(filePath) Then
                                                            For Each childf As String In _fileIo.GetDirectories(filePath)
                                                                If String.IsNullOrWhiteSpace(childf) Then Continue For
                                                                If StrContainsAny(childf, True, "ati.ace", "cnext", "cim") Then
                                                                    Delete(childf)
                                                                End If
                                                                If config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD AndAlso StrContainsAny(childf, True, "amdkmpfd") Then
                                                                    Delete(childf)
                                                                End If
                                                            Next
                                                            If _fileIo.CountDirectories(filePath) = 0 Then
                                                                Delete(filePath)
                                                            End If
                                                        End If
                                                    End If
                                                    For Each child2 As String In regkey2.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                        If StrContainsAny(child2, True, "A464", "ati catalyst", "ati mcat", "avt", "ccc", "cnext", "packages",
"wirelessdisplay", "hydravision", "dndtranscoding64", "avivo", "steadyvideo", "amd app sdk runtime", "amd media foundation decoders") Then
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
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    Else
                                                        For Each data As String In regkey2.GetSubKeyNames()
                                                            If String.IsNullOrWhiteSpace(data) Then Continue For
                                                            Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
                                                        Next
                                                    End If
                                                End If
                                            End Using
                                        End If
                                    End If
                                Next
                                If regkey.SubKeyCount = 0 Then
                                    Try
                                        Deletesubregkey(Registry.LocalMachine, "Software\Wow6432Node\ATI Technologies")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                Else
                                    For Each data As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If
                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If Not String.IsNullOrWhiteSpace(users) Then
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Windows\CurrentVersion\Run", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetValueNames
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        If StrContainsAny(child, True, "HydraVisionDesktopManager", "Grid", "HydraVisionMDEngine", "AMDDVR", "AMDNoiseSuppression") Then
                                            Deletevalue(regkey, child)
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
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)
                                    If subregkey IsNot Nothing Then
                                        If String.IsNullOrWhiteSpace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then Continue For
                                        wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
                                        If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                        For i As Integer = 0 To packages.Length - 1
                                            If String.IsNullOrWhiteSpace(packages(i)) Then Continue For
                                            If StrContainsAny(wantedvalue, True, packages(i)) Then
                                                Try
                                                    If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
                                                        Deletesubregkey(regkey, child)
                                                        Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                            If dependencyRegkey IsNot Nothing Then
                                                                For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                    If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                    If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                    If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                        Try
                                                                            Deletesubregkey(dependencyRegkey, depChild, False)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                            End If
                                                        End Using
                                                        If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                            Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                        End If
                                                        Continue For
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
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                                        If subregkey IsNot Nothing Then
                                            If String.IsNullOrWhiteSpace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then Continue For
                                            wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
                                            If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                            For i As Integer = 0 To packages.Length - 1
                                                If Not String.IsNullOrWhiteSpace(packages(i)) Then
                                                    If StrContainsAny(wantedvalue, True, packages(i)) Then
                                                        Try
                                                            Deletesubregkey(regkey, child)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
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
                CleanupEngine.Installer(packages, config)
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Run", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames
                                If Not String.IsNullOrWhiteSpace(child) Then
                                    If StrContainsAny(child, True, "StartCCC", "StartCN", "AMD AVT", "AMDNoiseSuppression") Then
                                        Deletevalue(regkey, child)
                                    End If
                                End If
                            Next
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                For Each users As String In Registry.Users.GetSubKeyNames()
                    If String.IsNullOrWhiteSpace(users) Then Continue For
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "AMDNoiseSuppression") Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next
                        End If
                    End Using
                Next
                If IntPtr.Size = 8 Then
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames
                                    If Not String.IsNullOrWhiteSpace(child) Then
                                        If StrContainsAny(child, True, "StartCCC", "StartCN", "AMD AVT", "AMDNoiseSuppression") Then
                                            Deletevalue(regkey, child)
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
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If child.Contains("ATI\CIM\") Or
child.Contains("AMD\CNext\") Or
child.Contains("AMD APP\") Or
child.Contains("AMD\SteadyVideo\") Or
child.Contains("HydraVision\") Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
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
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If child.ToLower.Contains("launchwuapp") Then
                                    Deletevalue(regkey, child)
                                End If
                            Next
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                If IntPtr.Size = 8 Then
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If child.ToLower.Contains("launchwuapp") Then
                                        Deletevalue(regkey, child)
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
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "AudioEngine\AudioProcessingObjects", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                    If regkey2 IsNot Nothing Then
                                        If String.IsNullOrWhiteSpace(regkey2.GetValue("FriendlyName", String.Empty).ToString) Then Continue For
                                        If StrContainsAny(regkey2.GetValue("FriendlyName", String.Empty).ToString, True, "cdelayapogfx") Then
                                            Try
                                                Deletesubregkey(regkey, child)
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
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                If subregkey IsNot Nothing Then
                                    If Not String.IsNullOrWhiteSpace(subregkey.GetValue("", String.Empty).ToString) Then
                                        wantedvalue = subregkey.GetValue("", String.Empty).ToString
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                            If StrContainsAny(wantedvalue, True, "steadyvideo") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
                                    End If
                                End If
                            End Using
                        Next
                    End If
                End Using
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "PROTOCOLS\Filter", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                    If subregkey IsNot Nothing Then
                                        If Not String.IsNullOrWhiteSpace(subregkey.GetValue("", String.Empty).ToString) Then
                                            wantedvalue = subregkey.GetValue("", String.Empty).ToString
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                If wantedvalue.ToLower.Contains("steadyvideo") Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End If
                                        End If
                                    End If
                                End Using
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
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                    If subregkey IsNot Nothing Then
                                        If Not String.IsNullOrWhiteSpace(subregkey.GetValue("", String.Empty).ToString) Then
                                            wantedvalue = subregkey.GetValue("", String.Empty).ToString
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                If wantedvalue.ToLower.Contains("steadyvideo") Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End If
                                        End If
                                    End If
                                End Using
                            Next
                        End If
                    End Using
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\PROTOCOLS\Filter", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                        If subregkey IsNot Nothing Then
                                            If Not String.IsNullOrWhiteSpace(subregkey.GetValue("", String.Empty).ToString) Then
                                                wantedvalue = subregkey.GetValue("", String.Empty).ToString
                                                If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                    If StrContainsAny(wantedvalue, True, "steadyvideo") Then
                                                        Try
                                                            Deletesubregkey(regkey, child)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End Using
                                Next
                            End If
                        End Using
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                'Task Scheduler cleanUP (AMD Updater)
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                If regkey2 IsNot Nothing Then
                                    If Not String.IsNullOrWhiteSpace(regkey2.GetValue("Description", String.Empty).ToString) Then
                                        If StrContainsAny(regkey2.GetValue("Description", String.Empty).ToString, True, "AMD Updater", "AMDLinkUpdate", "ModifyLinkUpdate", "AMDInstallUEP", "AMDInstallLauncher") Then
                                            Deletesubregkey(regkey, child)
                                        End If
                                    End If
                                    If Not String.IsNullOrWhiteSpace(regkey2.GetValue("Path", String.Empty).ToString) Then
                                        If StrContainsAny(regkey2.GetValue("Path", String.Empty).ToString, True, "\StartCN", "\StartCNBM", "\AMD ThankingURL", "\StartAUEP") Then
                                            Deletesubregkey(regkey, child)
                                        End If
                                    End If
                                End If
                            End Using
                        Next
                    End If
                End Using
                Using schedule As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache", True)
                    If schedule IsNot Nothing Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(schedule, "Tree", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "AMD Updater", "AMDLinkUpdate", "StartCN", "StartDVR", "StartCNBM", "ModifyLinkUpdate", "AMD ThankingURL", "AMDInstallLauncher", "AMDInstallUEP", "StartAUEP", "AMD Install Manager") Then
                                        For Each ScheduleChild As String In schedule.GetSubKeyNames
                                            If String.IsNullOrWhiteSpace(ScheduleChild) Then Continue For
                                            Try
                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                                    If regkey2 IsNot Nothing Then
                                                        If Not String.IsNullOrWhiteSpace(regkey2.GetValue("Id", String.Empty).ToString) Then
                                                            wantedvalue = regkey2.GetValue("Id", String.Empty).ToString
                                                            If String.IsNullOrEmpty(wantedvalue) Then Continue For
                                                            Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(schedule, ScheduleChild, True)
                                                                If regkey3 IsNot Nothing Then
                                                                    For Each child2 As String In regkey3.GetSubKeyNames
                                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                                        If StrContainsAny(wantedvalue, True, child2) Then
                                                                            Deletesubregkey(regkey3, child2)
                                                                        End If
                                                                    Next
                                                                End If
                                                            End Using
                                                        End If
                                                    End If
                                                End Using
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        Next
                                        Deletesubregkey(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    End If
                End Using

                '      Dim OldValue As String = Nothing
                'Select Case System.Windows.Forms.SystemInformation.BootMode
                '          Case Forms.BootMode.FailSafe
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If
                '	Case Forms.BootMode.FailSafeWithNetwork
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If
                '	Case Forms.BootMode.Normal
                '		'Usually this service is Running in normal mode, we *could* in the future check all this.
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If
                '      End Select
                'Using tsc As New TaskSchedulerControl(config)
                '	For Each task As Task In tsc.GetAllTasks
                '		If StrContainsAny(task.Name, True, "AMD Updater", "StartCN") Then
                '			Try
                '				task.Delete()
                '			Catch ex As Exception
                '				Application.Log.AddException(ex)
                '			End Try
                '			Application.Log.AddMessage("TaskScheduler: " & task.Name & " as been removed")
                '		End If
                '	Next
                'End Using

                'Select Case System.Windows.Forms.SystemInformation.BootMode
                '	Case Forms.BootMode.FailSafe
                '              StopService("Schedule")
                '              If OldValue IsNot Nothing Then
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                '	Case Forms.BootMode.FailSafeWithNetwork
                '              StopService("Schedule")
                '              If OldValue IsNot Nothing Then
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                '	Case Forms.BootMode.Normal
                '              'Usually this service is running in normal mode, we don't need to stop it.
                '              If OldValue IsNot Nothing Then
                '                  StopService("Schedule")
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                'End Select

                'Killing Explorer.exe to help releasing file that were open.
            End Sub)
            Application.Log.AddMessage("Killing Explorer.exe")
            KillProcess("explorer")
        End Sub

        Private Sub CleanAmdCache(ByVal config As ThreadSettings)
            For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                Dim filePath As String = filepaths + "\AppData\Local\AMD"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "dxcache", "vkcache", "glcache", "dxccache", "dx9cache", "OglpCache", "cl.cache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = config.Paths.System32 + "config\systemprofile\AppData\Local\D3DSCache"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = filepaths + "\AppData\LocalLow\AMD"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "dxcache", "vkcache", "glcache", "dxccache", "dx9cache", "OglpCache", "cl.cache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = filepaths + "\AppData\Local\D3DSCache"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If
            Next
        End Sub

        Private Sub CleanAmdFolders(ByVal config As ThreadSettings)
            Dim filePath As String = Nothing
            Dim removedxcache As Boolean = config.RemoveCrimsonCache
            Dim driverfiles = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfiles.cfg")
            Dim driverfilesKMPFD = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfilesKMPFD.cfg")
            Dim driverfilesKMAFD = IO.File.ReadAllLines(config.Paths.AppBase & "settings\AMD\driverfilesKMAFD.cfg")
            Dim TaskList = New List(Of Task)()

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                'Delete AMD data Folders
                UpdateTextMethod(UpdateTextTranslated(1))
                Application.Log.AddMessage("Cleaning Directory (Please Wait...)")
                If config.RemoveAMDDirs Then
                    filePath = _sysdrv + "AMD"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If Not StrContainsAny(child, True, "Chipset_Software") Then
                                Delete(child)
                            End If
                        Next
                        If _fileIo.CountDirectories(filePath) = 0 Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    End If
                End If

                'Delete driver files
                'delete OpenCL
            End Sub)

            Dim thread1 As Task = Task.Run(Sub() Threaddata1(driverfiles))
                TaskList.Add(thread1)

                If config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD Then
                    Dim thread2 As Task = Task.Run(Sub() Threaddata1(driverfilesKMPFD))
                    TaskList.Add(thread2)
                End If

                If config.RemoveAudioBus AndAlso FrmMain.DoNotRemoveAmdHdAudioBusFiles = False Then
                    Dim thread3 As Task = Task.Run(Sub() Threaddata1(driverfilesKMAFD))
                    TaskList.Add(thread3)
                End If

            Task.WaitAll(TaskList.ToArray())

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                If removedxcache Then
                    CleanAmdCache(config)
                End If

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
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("ati.ace") Or
child.ToLower.Contains("ati catalyst control center") Or
child.ToLower.Contains("application profiles") Or
child.ToLower.EndsWith("\px") Or
child.ToLower.Contains("hydravision") Then
                                Delete(child)
                            End If
                        End If
                    Next

                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\ATI"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("cim") Then
                                Delete(child)
                            End If
                        End If
                    Next

                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\Common Files" + "\ATI Technologies"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("multimedia") Then
                                Delete(child)
                            End If
                        End If
                    Next

                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\AMD APP"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                If IntPtr.Size = 8 Then
                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("ati.ace") Or
child.ToLower.Contains("ati catalyst control center") Or
child.ToLower.Contains("application profiles") Or
child.ToLower.EndsWith("\px") Or
child.ToLower.Contains("hydravision") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                        End Try
                    End If

                    filePath = System.Environment.SystemDirectory
                    If _fileIo.ExistsDir(filePath) Then
                        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "coinst_*.*")
                        For i As Integer = 0 To files.Length - 1
                            If Not String.IsNullOrWhiteSpace(files(i)) Then
                                Delete(files(i))
                            End If
                        Next
                    End If

                    filePath = System.Environment.SystemDirectory + "\amd"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "acrdumps", "mmddumps", "real", "amdfendr", "EeuDumps", "Persistent", "ANR") Or
(child.ToLower.Contains("amdkmpfd") AndAlso config.NotPresentAMDKMPFD AndAlso config.RemoveAMDKMPFD) Or
(StrContainsAny(child, True, "amdkmafd", "amdafd") AndAlso config.RemoveAudioBus AndAlso FrmMain.DoNotRemoveAmdHdAudioBusFiles = False) Then
                                        Try
                                            Delete(child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                        End Try
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoFirefox"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoChrome"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If child.ToLower.Contains("multimedia") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMDInstallManager"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Catalyst Control Center"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Problem Report Wizard"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Settings"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Catalyst Control Center"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Radeon Software"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Software꞉ Adrenalin Edition"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMDBugReportTool"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Bug Report Tool"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD link for Windows"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\ATI"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("ace") Then
                                Delete(child)
                            End If
                        End If
                    Next
                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\AMD"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "kdb", "ppc", "fuel", "installuep", "uxg") Then
                                Delete(child)
                            End If
                        End If
                    Next
                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                    If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                    filePath = filepaths + "\AppData\Roaming\ATI"
                    If _winxp Then
                        filePath = filepaths + "\Application Data\ATI"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "ace", "Usersettings") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\ATI"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\ATI"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("ace") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\AMD"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\AMD"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "cn", "fuel", "dvr", "wvr", "openvr", "radeonsoftware", "link") Or
removedxcache AndAlso StrContainsAny(child, True, "dxcache", "vkcache", "glcache", "dxccache", "dx9cache", "OglpCache", "cl.cache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\RadeonInstaller"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\RadeonInstaller"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "cache", "QtWeb Engine") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\AMDSoftwareInstaller"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\AMDSoftwareInstaller"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "cache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\AMD_Common"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\AMD_Common"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\D3DSCache"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\D3DSCache"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    Delete(child)
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\LocalLow\AMD"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\AMD"  'need check in the future.
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("cn") Or
child.ToLower.Contains("fuel") Or
removedxcache AndAlso child.ToLower.Contains("dxcache") Or
removedxcache AndAlso child.ToLower.Contains("vkcache") Or
removedxcache AndAlso child.ToLower.Contains("glcache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
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
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "ccc2", "prw", "cnext", "steadyvideo", "920dec42-4ca5-4d1d-9487-67be645cddfc", "cim", "performance profile client", "wvr", "installuep", "AMDInstallManager") Then
                                Delete(child)
                            End If
                            If (config.RemoveAudioBus AndAlso FrmMain.DoNotRemoveAmdHdAudioBusFiles = False) AndAlso StrContainsAny(child, True, "amdkmafd") Then
                                Delete(child)
                            End If
                            If config.RemoveAMDKMPFD AndAlso config.NotPresentAMDKMPFD AndAlso StrContainsAny(child, True, "amdkmpfd") Then
                                Delete(child)
                            End If
                            If child.ToLower.EndsWith("\a") Then
                                Delete(child)
                            End If
                        End If
                    Next
                    Try
                        If _fileIo.CountDirectories(filePath) = 0 Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    Catch ex As Exception
                    End Try
                End If
                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("ati.ace") Or
child.ToLower.Contains("cnext") Then
                                Delete(child)
                            End If
                        End If
                    Next
                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                'Cleaning the CCC assemblies.


                filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_64"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
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
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
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

            End Sub)
        End Sub

        Private Sub CleanEnvironementPath(ByVal valuesToRemove() As String)
            Dim value As String
            Dim paths() As String = Nothing
            Dim newPaths As List(Of String)
            Dim removedPaths As List(Of String)

            '--------------------------------
            'System environment path cleanup
            '--------------------------------

            Dim logEntry As LogEntry = Application.Log.CreateEntry()
            logEntry.Message = "System Environment Path cleanUP"
            Try
                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                            If StrContainsAny(child2, True, "controlset") Then
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetValueNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            If child.Equals("Path", StringComparison.OrdinalIgnoreCase) Then
                                                value = CType(regkey.GetValue(child, String.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames), String)
                                                If Not String.IsNullOrWhiteSpace(value) Then
                                                    Dim originalKind As RegistryValueKind
                                                    originalKind = regkey.GetValueKind(child)
                                                    paths = If(value.Contains(";"), value.Split(New Char() {";"c}, StringSplitOptions.None), New String() {value})
                                                    newPaths = New List(Of String)(paths.Length)
                                                    removedPaths = New List(Of String)(paths.Length)
                                                    For Each p As String In paths
                                                        If String.IsNullOrWhiteSpace(p) Then Continue For
                                                        If Not StrContainsAny(p, True, valuesToRemove) Then 'StrContainsAny(..) checks p and each valuesToRemove for empty/null
                                                            newPaths.Add(p)
                                                        Else
                                                            removedPaths.Add(p)
                                                        End If
                                                    Next
                                                    logEntry.Add(child2, String.Join(Environment.NewLine, paths))
                                                    logEntry.Add(KvP.Empty)
                                                    If removedPaths.Count > 0 Then  'Change regkey's value only if modified
                                                        regkey.SetValue(child, String.Join(";", newPaths.ToArray()), originalKind)
                                                        logEntry.Add(">> Removed", String.Join(Environment.NewLine, removedPaths.ToArray()))    'Log removed values
                                                    Else
                                                        logEntry.Add(">> Not modified")
                                                    End If
                                                    logEntry.Add(KvP.Empty)
                                                    logEntry.Add(KvP.Empty)
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

        Private Function Checkamdkmpfd() As Boolean
            Try
                Application.Log.AddMessage("Checking if AMDKMPFD is present before Service removal")
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\ACPI")
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "pnp0a08", "pnp0a03") Then
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                    If regkey2 IsNot Nothing Then
                                        For Each child2 As String In regkey2.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                                            Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
                                                If regkey3 IsNot Nothing Then
                                                    Dim array As String() = TryCast(regkey3.GetValue("LowerFilters"), String())
                                                    If array IsNot Nothing AndAlso array.Length > 0 Then
                                                        For Each value As String In array
                                                            If Not String.IsNullOrWhiteSpace(value) Then
                                                                If StrContainsAny(value, True, "amdkmpfd") Then
                                                                    Application.Log.AddMessage("Found an AMDKMPFD! in " + child)
                                                                    Application.Log.AddMessage("We do not remove the AMDKMPFP service yet")
                                                                    Return True
                                                                End If
                                                            End If
                                                        Next
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
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
            Return False
        End Function

        Private Sub Checkpcieroot(ByVal config As ThreadSettings)   'This is for Nvidia Optimus to prevent the yellow mark on the PCI-E controler. We must remove the UpperFilters.
            Dim win10 As Boolean = FrmMain.IsWindows10

            UpdateTextMethod(UpdateTextTranslated(7))
            Try
                Application.Log.AddMessage("Starting the removal of nVidia Optimus UpperFilter if present.")
                Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("system", Nothing, False)
                If found IsNot Nothing AndAlso found.Count > 0 Then
                    For Each d As SetupAPI.Device In found
                        If StrContainsAny(d.HardwareIDs(0), True, "VEN_8086") Then
                            If d.UpperFilters IsNot Nothing AndAlso d.UpperFilters.Length > 0 AndAlso StrContainsAny(String.Join(",", d.UpperFilters), True, "nvpciflt", "nvkflt") Then
                                Application.Log.AddMessage("Upper filter found on device : " + d.Description)
                                If d.OemInfs IsNot Nothing AndAlso d.OemInfs.Length > 0 AndAlso (Not String.IsNullOrWhiteSpace(d.OemInfs(0).ToString)) AndAlso _fileIo.ExistsFile(d.OemInfs(0).ToString) Then
                                    SetupAPI.UpdateDeviceInf(d, d.OemInfs(0).ToString, True)
                                Else
                                    If win10 Then
                                        If SetupAPI.MethodExists("newdev.dll", "DiUninstallDriverW") Then
                                            SetupAPI.UninstallDevice(d)
                                        Else
                                            SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\PCI.inf", True)
                                        End If
                                    Else
                                        SetupAPI.UpdateDeviceInf(d, config.Paths.WinDir + "inf\machine.inf", True)
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
                Application.Log.AddMessage("SetupAPI removal of nVidia Optimus UpperFilter if present. Completed.")
            Catch ex As Exception
                Application.Log.AddException(ex)
                'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
            UpdateTextMethod(UpdateTextTranslated(28))
        End Sub

        Private Sub CleanNvidiaServiceProcess(ByVal config As ThreadSettings)
            Dim CleanupEngine As New CleanupEngine
            Dim services As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\services.cfg")
            Dim gfeservices As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\gfeservice.cfg")
            Dim nvbservices As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\nvbservice.cfg")

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning Process/Services...")
                CleanupEngine.Cleanserviceprocess(services, config)
                If config.RemoveGFE Then
                    CleanupEngine.Cleanserviceprocess(gfeservices, config)
                End If


                'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
                'holding files in the NVIDIA folders sometimes.
                '10-10-2016 (removed dwm.exe from the list because of issues in win10 IB 14942 Wagnard)
                Try
                    KillProcess(
"Lcore",
"nvgamemonitor",
"nvstreamsvc",
"NvTmru",
"nvxdsync",
"WWAHost",
"nvspcaps64",
"nvspcaps",
"NVIDIA Web Helper",
"NvBackend",
"NVIDIA Broadcast",
"NVIDIA Broadcast UI")
                    If config.RemoveGFE Then
                        KillProcess("nvtray")
                    End If
                Catch ex As Exception
                    Application.Log.AddMessage(ex.Message)
                End Try
                If config.RemoveNVBROADCAST Then
                    CleanupEngine.Cleanserviceprocess(nvbservices, config)
                End If
                Application.Log.AddMessage("Process/Services CleanUP Complete")
            End Sub)
        End Sub

        Private Sub Old_TemporaryNvidiaSpeedup(ByVal config As ThreadSettings)   'we do this to speedup the removal of the nividia display driver because of the huge time the nvidia installer files take to do unknown stuff.
            Dim filePath As String = Nothing
            Try
                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
                For Each child As String In _fileIo.GetDirectories(filePath)
                    If String.IsNullOrWhiteSpace(child) = False Then
                        If child.ToLower.Contains("installer2") Then
                            For Each child2 As String In _fileIo.GetDirectories(child)
                                If String.IsNullOrWhiteSpace(child2) = False Then
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
child2.ToLower.Contains("nvdisplaycontainer") Or
child2.ToLower.Contains("ansel.") Or
child2.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvab") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvidia.update") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("installer2\installer") AndAlso config.RemoveGFE AndAlso config.RemovePhysX Or
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
                            If _fileIo.CountDirectories(child) = 0 Then
                                Try
                                    Delete(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        Private Sub CleanNvidia(ByVal config As ThreadSettings, ByVal Optional preclean As Boolean = False)
            Dim CleanupEngine As New CleanupEngine
            Dim TaskList = New List(Of Task)()
            Dim wantedvalue As String = Nothing
            Dim wantedvalue2 As String = Nothing
            Dim removegfe As Boolean = config.RemoveGFE
            Dim removenvbroadcast As Boolean = config.RemoveNVBROADCAST
            Dim removephysx As Boolean = config.RemovePhysX
            Dim classroot As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\classroot.cfg")
            Dim classrootgfe As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\classrootgfe.cfg")
            Dim clsidleftoverGFE As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\clsidleftoverGFE.cfg")
            Dim clsidleftoverNVB As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\clsidleftoverNVB.cfg")
            Dim clsidleftover As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\clsidleftover.cfg")
            Dim packages As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\packages.cfg")
            Dim reginterfaceGFE As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\interfaceGFE.cfg")
            Dim reginterface As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\interface.cfg")
            Dim driverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\driverfiles.cfg")
            Dim gfedriverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\gfedriverfiles.cfg")

            If preclean Then

                ImpersonateUser.RunImpersonatedSystem(
                Sub()

                    '-----------------
                    'Registry Cleaning
                    '-----------------
                    UpdateTextMethod(UpdateTextTranslated(5))
                    Application.Log.AddMessage("Cleaning registry Part 1/2")


                    'Deleting DCOM object /classroot
                    Application.Log.AddMessage("Starting dcom/clsid/appid/typelib cleanup")
                    CleanupEngine.ClassRoot(classroot, config)

                    If config.RemoveGFE Then
                        CleanupEngine.ClassRoot(classrootgfe, config)
                    End If

                End Sub)

                'Removal of the (DCH) Nvidia control panel comming from the Window Store. (In progress...)
                If _win10 AndAlso config.RemoveNVCP Then
                        CleanupEngine.RemoveAppxAsync("NVIDIAControlPanel").Wait()
                    End If

                    'for GFE removal only
                    If removegfe Then
                        Dim thread1 As Task = Task.Run(Sub() CLSIDCleanThread(clsidleftoverGFE))
                        TaskList.Add(thread1)
                    Else
                        Dim thread1 As Task = Task.Run(Sub() CLSIDCleanThread(clsidleftover))
                        TaskList.Add(thread1)
                    End If

                    If removenvbroadcast Then
                        Dim thread2 As Task = Task.Run(Sub() CLSIDCleanThread(clsidleftoverNVB))
                        TaskList.Add(thread2)
                    End If

                    Dim thread3 As Task = Task.Run(Sub() InstallerCleanThread(packages, config))
                    TaskList.Add(thread3)

                    If removenvbroadcast Then
                        Dim thread4 As Task = Task.Run(Sub() InstallerCleanThread(clsidleftoverNVB, config))
                        TaskList.Add(thread4)
                    End If

                    Task.WaitAll(TaskList.ToArray())

                ImpersonateUser.RunImpersonatedSystem(
                Sub()

                    '------------------------------
                    'Clean the rebootneeded message
                    '------------------------------
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If Not String.IsNullOrWhiteSpace(child) Then
                                        If child.ToLower.Contains("nvidia_rebootneeded") Then
                                            Try
                                                Deletesubregkey(regkey, child)
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
                        CleanupEngine.Interfaces(reginterfaceGFE) '// add each line as String Array.
                    Else
                        CleanupEngine.Interfaces(reginterface)  '// add each line as String Array.
                    End If

                    Application.Log.AddMessage("Finished dcom/clsid/appid/typelib/interface cleanup")

                End Sub)

                Return
            End If

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning registry Part 2/2")

                'end of deleting dcom stuff
                Application.Log.AddMessage("Pnplockdownfiles region cleanUP")
                CleanupEngine.PnpLockdownFiles(driverfiles)  '// add each line as String Array.

                If removegfe Then
                    CleanupEngine.PnpLockdownFiles(gfedriverfiles) '// add each line as String Array.
                End If

                'Cleaning PNPRessources.  'Will fix this later, its not efficent clean at all. (Wagnard)
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\Global", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\global")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\SOFTWARE\NVIDIA Corporation\global", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\SOFTWARE\NVIDIA Corporation\global")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\SOFTWARE\NVIDIA Corporation", False)
                    If regkey IsNot Nothing Then
                        If regkey.SubKeyCount = 0 Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\SOFTWARE\NVIDIA Corporation")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Else
                            For Each data As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                            Next
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False)
                    If regkey IsNot Nothing Then
                        If regkey.SubKeyCount = 0 Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Else
                            For Each data As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                            Next
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\services\nvlddmkm", False)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\services\nvlddmkm")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                If IntPtr.Size = 8 Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos", False)
                        If regkey IsNot Nothing Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
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
                        If _winxp = False Then
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                                If subregkey IsNot Nothing Then
                                    For Each child2 As String In subregkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child2) Then Continue For
                                        If StrContainsAny(child2, True, "controlset") Then
                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules", True)
                                                If regkey IsNot Nothing Then
                                                    For Each child As String In regkey.GetValueNames()
                                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                                        wantedvalue = regkey.GetValue(child, String.Empty).ToString()
                                                        If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                                        If StrContainsAny(wantedvalue, True, "nvstreamsrv", "nvidia network service", "nvidia update core", "NvContainer") Then
                                                            Try
                                                                Deletevalue(regkey, child)
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
                    If _winxp = False Then
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                            If subregkey IsNot Nothing Then
                                For Each child2 As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                    If child2.ToLower.Contains("controlset") Then
                                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
                                            If regkey IsNot Nothing Then
                                                For Each childs As String In regkey.GetSubKeyNames()
                                                    If String.IsNullOrWhiteSpace(childs) Then Continue For
                                                    Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, childs)
                                                        If regkey2 IsNot Nothing Then
                                                            For Each child As String In regkey2.GetValueNames()
                                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                                If StrContainsAny(child, True, "description") Then
                                                                    wantedvalue = regkey2.GetValue(child, String.Empty).ToString()
                                                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                                                    If StrContainsAny(wantedvalue, True, "nvsvc") Then
                                                                        Try
                                                                            Deletesubregkey(regkey, childs)
                                                                            Continue For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                    Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, childs, True)
                                                                        If subregkey2 IsNot Nothing Then
                                                                            For Each childinsubregkey2 As String In subregkey2.GetSubKeyNames()
                                                                                If String.IsNullOrWhiteSpace(childinsubregkey2) Then Continue For
                                                                                If StrContainsAny(childinsubregkey2, True, "89cc76a4-f226-4d4b-a040-6e9a1da9b882", "aded5e82-b909-4619-9949-f5d71dac0bcc") Then
                                                                                    'This is a key that is installed with the nvidia driver and have the same name on any computer.
                                                                                    'There is no relatation that allow to detect it with any logic and thus I remove it directly.
                                                                                    Try
                                                                                        Deletesubregkey(subregkey2, childinsubregkey2)
                                                                                        Continue For
                                                                                    Catch ex As Exception
                                                                                        Application.Log.AddException(ex)
                                                                                    End Try
                                                                                End If
                                                                                Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(subregkey2, childinsubregkey2)
                                                                                    If regkey3 IsNot Nothing Then
                                                                                        For Each childinsubregkey2value As String In regkey3.GetValueNames()
                                                                                            If String.IsNullOrWhiteSpace(childinsubregkey2value) Then Continue For
                                                                                            If childinsubregkey2value.ToString.ToLower.Contains("description") Then
                                                                                                wantedvalue2 = regkey3.GetValue(childinsubregkey2value, String.Empty).ToString
                                                                                                If String.IsNullOrWhiteSpace(wantedvalue2) Then Continue For
                                                                                                If wantedvalue2.ToString.ToLower.Contains("nvsvc") Then
                                                                                                    Try
                                                                                                        Deletesubregkey(subregkey2, childinsubregkey2)
                                                                                                    Catch ex As Exception
                                                                                                        Application.Log.AddException(ex)
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

                '--------------------------
                'End Power Settings CleanUP
                '--------------------------
                Application.Log.AddMessage("End Power Settings Cleanup")

                '----------------------------------------------------------
                'Fix, if needed, for previously corrupted environement Path
                '----------------------------------------------------------

                FixBrokenPathIfNeeded()


                '--------------------------------
                'System environement path cleanup
                '--------------------------------


                If removephysx Then
                    Application.Log.AddMessage("System environement CleanUP")
                    Try
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                            If subregkey IsNot Nothing Then
                                For Each child2 As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                    If child2.ToLower.Contains("controlset") Then
                                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                                            If regkey IsNot Nothing Then
                                                For Each child As String In regkey.GetValueNames()
                                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                                    If StrContainsAny(child, True, "Path") Then
                                                        wantedvalue = CType(regkey.GetValue(child, String.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames), String)
                                                        If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                                        Dim originalKind As RegistryValueKind
                                                        originalKind = regkey.GetValueKind(child)
                                                        Try
                                                            Select Case True
                                                                Case StrContainsAny(wantedvalue, True, _sysdrv & "program files (x86)\nvidia corporation\physx\common;")
                                                                    wantedvalue = Regex.Replace(wantedvalue,
                                                    Regex.Escape(_sysdrv & "program files (x86)\nvidia corporation\physx\common;"),
                                                    "",
                                                    RegexOptions.IgnoreCase)
                                                                    Try
                                                                        regkey.SetValue(child, wantedvalue, originalKind)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try

                                                                Case StrContainsAny(wantedvalue, True, ";" & _sysdrv & "program files (x86)\nvidia corporation\physx\common")
                                                                    wantedvalue = Regex.Replace(wantedvalue,
                                                    Regex.Escape(";" & _sysdrv & "program files (x86)\nvidia corporation\physx\common"),
                                                    "",
                                                    RegexOptions.IgnoreCase)
                                                                    Try
                                                                        regkey.SetValue(child, wantedvalue, originalKind)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                            End Select
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
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                    Application.Log.AddMessage("End System environement path cleanup")
                End If

                '-------------------------------------
                'end system environement patch cleanup
                '-------------------------------------

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
                        If regkey IsNot Nothing Then
                            wantedvalue = regkey.GetValue("AppInit_DLLs", String.Empty).ToString   'Will need to consider the comma in the future for multiple value
                            If String.IsNullOrWhiteSpace(wantedvalue) = False Then
                                Select Case True
                                    Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & _sysdrv.ToUpper & "PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll")
                                        wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & _sysdrv.ToUpper & "PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
                                        regkey.SetValue("AppInit_DLLs", wantedvalue)
                                    Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL")
                                        wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL", "")
                                        regkey.SetValue("AppInit_DLLs", wantedvalue)
                                    Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll")
                                        wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~1\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
                                        regkey.SetValue("AppInit_DLLs", wantedvalue)
                                End Select
                            End If
                        End If
                        If regkey.GetValue("AppInit_DLLs", String.Empty).ToString = "" Then
                            Try
                                regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    If IntPtr.Size = 8 Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)
                            If regkey IsNot Nothing Then
                                wantedvalue = regkey.GetValue("AppInit_DLLs", String.Empty).ToString
                                If String.IsNullOrWhiteSpace(wantedvalue) = False Then
                                    Select Case True
                                        Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & _sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll")
                                            wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL, " & _sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
                                            regkey.SetValue("AppInit_DLLs", wantedvalue)
                                        Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL")
                                            wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\3DVISI~1\NVSTIN~1.DLL", "")
                                            regkey.SetValue("AppInit_DLLs", wantedvalue)
                                        Case wantedvalue.Contains(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll")
                                            wantedvalue = wantedvalue.Replace(_sysdrv.ToUpper & "PROGRA~2\NVIDIA~1\NVSTRE~1\rxinput.dll", "")
                                            regkey.SetValue("AppInit_DLLs", wantedvalue)
                                    End Select
                                End If
                            End If
                            If regkey.GetValue("AppInit_DLLs", String.Empty).ToString = "" Then
                                Try
                                    regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        End Using
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                If config.RemoveVulkan Then
                    CleanVulkan(config)
                End If

                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If Not String.IsNullOrWhiteSpace(users) Then
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        If StrContainsAny(child, True, "nvidia corporation") Then
                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                                If regkey2 IsNot Nothing Then
                                                    For Each child2 As String In regkey2.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                        If StrContainsAny(child2, True, "global") Then
                                                            If removegfe Then
                                                                Try
                                                                    Deletesubregkey(regkey2, child2)
                                                                Catch ex As Exception
                                                                    Application.Log.AddException(ex)
                                                                End Try
                                                            Else
                                                                Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child + "\" + child2, True)
                                                                    If regkey3 IsNot Nothing Then
                                                                        For Each child3 As String In regkey3.GetSubKeyNames()
                                                                            If String.IsNullOrWhiteSpace(child3) Then Continue For
                                                                            If StrContainsAny(child3, True, "gfeclient", "gfexperience", "shadowplay", "ledvisualizer", "nvapp") Then
                                                                                'do nothing
                                                                            Else
                                                                                Try
                                                                                    Deletesubregkey(regkey3, child3)
                                                                                Catch ex As Exception
                                                                                    Application.Log.AddException(ex)
                                                                                End Try
                                                                            End If
                                                                        Next
                                                                    End If
                                                                End Using
                                                            End If
                                                        End If
                                                        If child2.ToLower.Contains("logging") Or
child2.ToLower.Contains("nvbackend") AndAlso removegfe Or
child2.ToLower.Contains("nvidia update core") AndAlso removegfe Or
child2.ToLower.Contains("nvcontrolpanel2") Or
child2.ToLower.Contains("nvcontrolpanel") Or
child2.ToLower.Contains("nvcamera") Or  'part of nv broadcast ?
child2.ToLower.Contains("nvidia broadcast") AndAlso removenvbroadcast Or
child2.ToLower.Contains("nvidia rtx voice") AndAlso removenvbroadcast Or
child2.ToLower.Contains("nvidia audio effects sdk") AndAlso removenvbroadcast Or
child2.ToLower.Contains("nvtray") AndAlso removegfe Or
child2.ToLower.Contains("ansel") AndAlso removegfe Or
child2.ToLower.Contains("nvcontainer") AndAlso removegfe Or
child2.ToLower.Contains("nvstream") AndAlso removegfe Or
child2.ToLower.Contains("nvidia control panel") Then
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
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    Else
                                                        For Each data As String In regkey2.GetSubKeyNames()
                                                            If String.IsNullOrWhiteSpace(data) Then Continue For
                                                            Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
                                                        Next
                                                    End If
                                                End If
                                            End Using
                                        End If
                                    Next
                                End If
                            End Using

                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetValueNames()
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        Dim tArray() As String = CType(regkey.GetValue(child), String())
                                        If tArray.Length > 0 Then
                                            For Each arrayelement As String In tArray
                                                If String.IsNullOrWhiteSpace(arrayelement) Then Continue For
                                                If Not arrayelement = "" Then
                                                    If StrContainsAny(arrayelement, True, "nvstview.exe", "vulkaninfo", "nvstlink.exe") Then
                                                        Try
                                                            Deletevalue(regkey, child)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                    If StrContainsAny(arrayelement, True, "geforce experience") AndAlso config.RemoveGFE Then
                                                        Try
                                                            Deletevalue(regkey, child)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End If
                                            Next
                                        End If
                                    Next
                                End If
                            End Using
                        End If

                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Windows\CurrentVersion\Run", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "NVIDIA Broadcast") AndAlso config.RemoveNVBROADCAST Then
                                        Deletevalue(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\ARP", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetValueNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Dim tArray() As String = CType(regkey.GetValue(child), String())
                            If tArray.Length > 0 Then
                                For Each arrayelement As String In tArray
                                    If String.IsNullOrWhiteSpace(arrayelement) Then Continue For
                                    If Not arrayelement = "" Then
                                        If StrContainsAny(arrayelement, True, "nvi2.dll", "vulkaninfo", "nvstlink.exe", "nvidiastereo") Then
                                            Try
                                                Deletevalue(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                Next
                            End If
                        Next
                    End If
                End Using

                If IntPtr.Size = 8 Then
                    Try
                        Dim CanRemove As Boolean = True
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    Try
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                            If regkey2 IsNot Nothing Then
                                                If removephysx Then
                                                    If Not String.IsNullOrWhiteSpace(regkey2.GetValue("DisplayName", String.Empty).ToString) Then
                                                        If regkey2.GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
                                                            Deletesubregkey(regkey, child)
                                                            Continue For
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End Using
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                    If child.ToLower.Contains("display.3dvision") Or
child.ToLower.Contains("3dtv") AndAlso config.Remove3DTVPlay Or
child.ToLower.Contains("_display.controlpanel") Or
child.ToLower.Contains("_display.driver") Or
child.ToLower.Contains("_display.gfexperience") AndAlso removegfe Or
child.ToLower.Contains("_display.nvapp") AndAlso removegfe Or
child.ToLower.Contains("nvdlisr") AndAlso removegfe Or
child.ToLower.Contains("_display.nvirusb") Or
child.ToLower.Contains("_display.physx") AndAlso removephysx Or
child.ToLower.Contains("_frameviewsdk") AndAlso removegfe Or
child.ToLower.Contains("_gpxcommon.oss") AndAlso removegfe Or
child.ToLower.Contains("_display.update") AndAlso removegfe Or
child.ToLower.Contains("_display.gamemonitor") AndAlso removegfe Or
child.ToLower.Contains("_gfexperience") AndAlso removegfe Or
child.ToLower.Contains("_hdaudio.driver") Or
child.ToLower.Contains("_network.service") AndAlso removegfe Or
child.ToLower.Contains("_shadowplay") AndAlso removegfe Or
child.ToLower.Contains("_update.core") AndAlso removegfe Or
child.ToLower.Contains("nvidiastereo") Or
child.ToLower.Contains("_displaydriveranalyzer") Or
child.ToLower.Contains("_shieldwireless") AndAlso removegfe Or
child.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
child.ToLower.Contains("_nvdisplaypluginwatchdog") AndAlso removegfe Or
child.ToLower.Contains("_nvdisplaysessioncontainer") AndAlso removegfe Or
child.ToLower.Contains("_virtualaudio.driver") AndAlso removegfe Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "B2FE1952-0186-46C3-BAEC-A80AA35AC5B8") AndAlso Not StrContainsAny(child, True, "_installer") Then
                                        CanRemove = False
                                    End If
                                Next
                                If CanRemove Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        If StrContainsAny(child, True, "_installer") Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    Next
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                Try
                    Dim CanRemove As Boolean = True
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                    If regkey2 IsNot Nothing Then
                                        Try
                                            If removephysx Then
                                                If String.IsNullOrWhiteSpace(regkey2.GetValue("DisplayName", String.Empty).ToString) = False Then
                                                    If StrContainsAny(regkey2.GetValue("DisplayName", String.Empty).ToString, True, "physx") Then
                                                        Deletesubregkey(regkey, child)
                                                        Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                            If dependencyRegkey IsNot Nothing Then
                                                                For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                    If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                    If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                    If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                        Try
                                                                            Deletesubregkey(dependencyRegkey, depChild, False)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                            End If
                                                        End Using
                                                        If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                            Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                        End If
                                                        Continue For
                                                    End If
                                                End If
                                            End If
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End Using

                                If child.ToLower.Contains("display.3dvision") Or
                                child.ToLower.Contains("3dtv") AndAlso config.Remove3DTVPlay Or
                                child.ToLower.Contains("_display.controlpanel") Or
                                child.ToLower.Contains("_display.driver") Or
                                child.ToLower.Contains("_display.optimus") Or
child.ToLower.Contains("_frameviewsdk") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_gpxcommon.oss") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_display.gfexperience") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_display.nvapp") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_display.nvirusb") Or
child.ToLower.Contains("_nvabhub") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_display.physx") AndAlso config.RemovePhysX Or
child.ToLower.Contains("_display.update") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_osc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvdlisr") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_display.nview") Or
child.ToLower.Contains("_display.nvwmi") Or
child.ToLower.Contains("_display.gamemonitor") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvidia.update") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_gfexperience") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_hdaudio.driver") Or
child.ToLower.Contains("_network.service") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_shadowplay") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_update.core") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvidiastereo") Or
child.ToLower.Contains("_usbc") Or
child.ToLower.Contains("_ansel") Or
child.ToLower.Contains("_shieldwireless") AndAlso config.RemoveGFE Or
child.ToLower.Contains("miracast.virtualaudio") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_virtualaudio.driver") AndAlso config.RemoveGFE Or
child.ToLower.Contains("vulkanrt1.") AndAlso config.RemoveVulkan Or
child.ToLower.Contains("_nvnodejs") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvbackend") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvplugin") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvtelemetry") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_ngxcore") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvvhci") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvdisplaycontainer") Or
child.ToLower.Contains("_displaydriveranalyzer") Or
child.ToLower.Contains("_nvdisplay.messagebus") Or
child.ToLower.Contains("_broadcastvoice.driver") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("_nvmodels") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("_nvbroadcastcontainer") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("_nvidiabroadcast") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("_nvvirtualcamera") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("_nvdisplaypluginwatchdog") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvdisplaysessioncontainer") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_osc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvmoduletracker.driver") AndAlso config.RemoveGFE Or
child.ToLower.Contains("_nvcontainer") AndAlso config.RemoveGFE Then
                                    Try
                                        Deletesubregkey(regkey, child)
                                        Deletesubregkey(Registry.ClassesRoot, "Installer\Dependencies\" + child, False)
                                        If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                            Delete(config.Paths.Roaming + "Package Cache\" + child)
                                        End If
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next

                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "B2FE1952-0186-46C3-BAEC-A80AA35AC5B8") AndAlso Not StrContainsAny(child, True, "_installer") Then
                                    CanRemove = False
                                End If
                            Next

                            If CanRemove Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "_installer") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                            Deletesubregkey(Registry.ClassesRoot, "Installer\Dependencies\" + child, False)
                                            If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                Delete(config.Paths.Roaming + "Package Cache\" + child)
                                            End If
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, ".DEFAULT\Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "nvidia corporation") Then
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                    If regkey2 IsNot Nothing Then
                                        For Each child2 As String In regkey2.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                                            If StrContainsAny(child2, True, "global", "nvbackend", "nvcontrolpanel2", "nvidia control panel") Or
(StrContainsAny(child2, True, "nvidia update core", "nvcontainer") AndAlso removegfe) Then
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
                                                Application.Log.AddException(ex)
                                            End Try
                                        Else
                                            For Each data As String In regkey2.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey2.ToString + "\ --> " + data)
                                            Next
                                        End If

                                    End If
                                End Using
                            End If
                        Next
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "ageia technologies") AndAlso removephysx Then
                                Try
                                    Deletesubregkey(regkey, child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                            If StrContainsAny(child, True, "nvidia corporation") Then
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                    If regkey2 IsNot Nothing Then
                                        For Each child2 As String In regkey2.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                                            If StrContainsAny(child2, True, "global") Then
                                                If removegfe AndAlso removenvbroadcast Then
                                                    Try
                                                        Deletesubregkey(regkey2, child2)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                Else
                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2, True)
                                                        If regkey3 IsNot Nothing Then
                                                            For Each child3 As String In regkey3.GetSubKeyNames()
                                                                If String.IsNullOrWhiteSpace(child3) Then Continue For
                                                                If StrContainsAny(child3, True, "gfeclient", "gfexperience", "nvbackend", "nvscaps", "shadowplay", "ledvisualizer", "nvUpdate", "nvcontainer", "NvApp") AndAlso Not removegfe Or
StrContainsAny(child3, True, "nvbroadcast") AndAlso Not removenvbroadcast Then
                                                                    'do nothing
                                                                Else
                                                                    Try
                                                                        Deletesubregkey(regkey3, child3)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                                End If
                                                            Next
                                                        End If
                                                    End Using
                                                End If
                                            End If
                                            If StrContainsAny(child2, True, "installer", "logging", "nvidia update core", "nvcontrolpanel", "nvcontrolpanel2", "physx_systemsoftware", "physxupdateloader", "uxd", "nvidia updatus", "parameters") OrElse
(StrContainsAny(child2, True, "nvstream", "nvtray", "nvcontainer", "nvdisplay.container") AndAlso removegfe) OrElse
(StrContainsAny(child2, True, "nvbroadcast") AndAlso removenvbroadcast) Then
                                                Select Case Not removephysx AndAlso StrContainsAny(child2, True, "physx")
                                                    Case True
                                                'Do nothing
                                                    Case False
                                                        If StrContainsAny(child2, True, "installer2") Then
                                                            Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2, True)
                                                                If regkey4 IsNot Nothing Then
                                                                    For Each subkeys In regkey4.GetSubKeyNames
                                                                        If String.IsNullOrWhiteSpace(subkeys) Then Continue For
                                                                        If StrContainsAny(subkeys, True, "configs", "cache", "extensions", "relationships", "stripped") Then
                                                                            Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkey4, subkeys, True)
                                                                                If regkey5 IsNot Nothing Then
                                                                                    For Each ValueName As String In regkey5.GetValueNames
                                                                                        If String.IsNullOrWhiteSpace(ValueName) Then Continue For
                                                                                        If StrContainsAny(ValueName, True, "ansel", "display.gfexperience", "display.nvapp", "nvdlisr", "display.update", "display.optimus", "frameviewsdk", "gfexperience", "gpxcommon.oss", "nvbackend", "nvcontainer", "nvmoduletracker", "nvnodejs", "nvplugin.watchdog", "nvtelemetry", "nvvhci", "osc", "shadowplay", "shieldwirelesscontroller", "update.core", "virtualaudio") AndAlso config.RemoveGFE Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                        If StrContainsAny(ValueName, True, "NVDisplaySessionContainer", "NVDisplayPluginWatchdog", "nvdisplaycontainer", "display.controlPanel", "display.driver", "hdaudio.driver", "nvabhub", "msvcruntime", "NGXCore", "USBC") Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                        If StrContainsAny(ValueName, True, "nvbroadcast", "broadcastvoice", "nvidiabroadcast", "nvvirtualcamera", "nvmodels") AndAlso removenvbroadcast Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                        If StrContainsAny(ValueName, True, "Display.PhysX") AndAlso removephysx Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                    Next
                                                                                    If regkey5.ValueCount = 0 Then
                                                                                        Try
                                                                                            Deletesubregkey(regkey4, subkeys)
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                End If
                                                                            End Using
                                                                        End If
                                                                        If StrContainsAny(subkeys, True, "drivers") Then
                                                                            Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkey4, subkeys, True)
                                                                                If regkey5 IsNot Nothing Then
                                                                                    For Each ValueName As String In regkey5.GetValueNames
                                                                                        If String.IsNullOrWhiteSpace(ValueName) AndAlso String.IsNullOrWhiteSpace(regkey5.GetValue(ValueName, String.Empty).ToString) Then Continue For
                                                                                        If StrContainsAny(regkey5.GetValue(ValueName, String.Empty).ToString, True, "display.driver", "hdaudio.driver", "usbc") Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                        If StrContainsAny(regkey5.GetValue(ValueName, String.Empty).ToString, True, "shieldwirelesscontroller") AndAlso removegfe Then
                                                                                            Try
                                                                                                Deletevalue(regkey5, ValueName)
                                                                                            Catch ex As Exception
                                                                                                Application.Log.AddException(ex)
                                                                                            End Try
                                                                                        End If
                                                                                    Next
                                                                                    If regkey5.ValueCount = 0 Then
                                                                                        Try
                                                                                            Deletesubregkey(regkey4, subkeys)
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                End If
                                                                            End Using
                                                                        End If
                                                                        If StrContainsAny(subkeys, True, "pending") Then
                                                                            Try
                                                                                Deletesubregkey(regkey4, subkeys)
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                    Next
                                                                    If regkey4.SubKeyCount = 0 Then
                                                                        Try
                                                                            Deletesubregkey(regkey2, child2)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    Else
                                                                        For Each data As String In regkey4.GetSubKeyNames()
                                                                            If String.IsNullOrWhiteSpace(data) Then Continue For
                                                                            Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey4.ToString + "\ --> " + data)
                                                                        Next
                                                                    End If
                                                                End If
                                                            End Using
                                                        Else
                                                            Try
                                                                Deletesubregkey(regkey2, child2)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                End Select
                                            End If
                                        Next
                                        If regkey2.SubKeyCount = 0 Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        Else
                                            For Each data As String In regkey2.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(data) Then Continue For
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
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "ageia technologies") Then
                                    If removephysx Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                                If StrContainsAny(child, True, "nvidia corporation") Then
                                    Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                        If regkey2 IsNot Nothing Then
                                            For Each child2 As String In regkey2.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                If StrContainsAny(child2, True, "global") Then
                                                    If removegfe Then
                                                        Try
                                                            Deletesubregkey(regkey2, child2)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    Else
                                                        Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2, True)
                                                            If regkey3 IsNot Nothing Then
                                                                For Each child3 As String In regkey3.GetSubKeyNames()
                                                                    If String.IsNullOrWhiteSpace(child3) Then Continue For
                                                                    If StrContainsAny(child3, True, "gfeclient", "gfexperience", "nvbackend", "nvscaps", "shadowplay", "ledvisualizer", "nvapp") Then
                                                                        'do nothing
                                                                    Else
                                                                        Try
                                                                            Deletesubregkey(regkey3, child3)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                            End If
                                                        End Using
                                                    End If
                                                End If
                                                If StrContainsAny(child2, True, "logging", "physx_systemsoftware", "physxupdateloader", "installer2", "physx", "nvnetworkservice", "installer") Then
                                                    If removephysx Then
                                                        Try
                                                            Deletesubregkey(regkey2, child2)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    Else
                                                        If child2.ToLower.Contains("physx") Then
                                                            'do nothing
                                                        Else
                                                            Try
                                                                Deletesubregkey(regkey2, child2)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End If
                                                End If
                                                If StrContainsAny(child2, True, "nvcontainer") AndAlso config.RemoveGFE Then
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
                                                    Application.Log.AddException(ex)
                                                End Try
                                            Else
                                                For Each data As String In regkey2.GetSubKeyNames()
                                                    If String.IsNullOrWhiteSpace(data) Then Continue For
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

                Using regkey = MyRegistry.OpenSubKey(Registry.CurrentUser,
"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetValueNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "gfexperience.exe", "nvidia app") AndAlso removegfe Or
(StrContainsAny(child, True, "nvidia broadcast") AndAlso config.RemoveNVBROADCAST) Then
                                Deletevalue(regkey, child)
                            End If
                        Next
                    End If
                End Using

                Using regkey = MyRegistry.OpenSubKey(Registry.CurrentUser,
"Software\Microsoft\.NETFramework\SQM\Apps", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "gfexperience.exe") AndAlso removegfe Then
                                Deletesubregkey(regkey, child)
                            End If
                        Next
                    End If
                End Using

                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If String.IsNullOrWhiteSpace(users) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
users + "\Software\Microsoft\.NETFramework\SQM\Apps", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                                        Deletesubregkey(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If String.IsNullOrWhiteSpace(users) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
users + "\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "gfexperience.exe", "GeForce Experience.exe") AndAlso removegfe Then
                                        Deletevalue(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
                                If subregkey IsNot Nothing Then
                                    If Not String.IsNullOrWhiteSpace(subregkey.GetValue("ProfileImagePath", String.Empty).ToString) Then
                                        wantedvalue = subregkey.GetValue("ProfileImagePath", String.Empty).ToString
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                            If wantedvalue.Contains("UpdatusUser") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
                                    End If
                                End If
                            End Using
                        Next
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
                                If subregkey IsNot Nothing Then
                                    If Not String.IsNullOrWhiteSpace(subregkey.GetValue("", String.Empty).ToString) Then
                                        wantedvalue = subregkey.GetValue("", String.Empty).ToString
                                        If String.IsNullOrWhiteSpace(wantedvalue) = False Then
                                            If wantedvalue.ToLower.Contains("nvidia control panel") Or
wantedvalue.ToLower.Contains("nvidia nview desktop manager") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                                'special case only to nvidia afaik. there i a clsid for a control pannel that link from namespace.
                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
                                                    If regkey2 IsNot Nothing Then
                                                        Try
                                                            Deletesubregkey(regkey2, child, False)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End Using
                                            End If
                                        End If
                                    End If
                                End If
                            End Using
                        Next
                    End If
                End Using

                '----------------------
                '.net ngenservice clean
                '----------------------
                Application.Log.AddMessage("ngenservice Clean")
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                                    Try
                                        Deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        Next
                    End If
                End Using

                If IntPtr.Size = 8 Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
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
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\MozillaPlugins", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If child.ToLower.Contains("nvidia.com/3dvision") Then
                                    Try
                                        Deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        Next
                    End If
                End Using

                If IntPtr.Size = 8 Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Wow6432Node\MozillaPlugins", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("nvidia.com/3dvision") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
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
                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                            If child2.ToLower.Contains("controlset") Then
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\eventlog\Application", True)
                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            If child.ToLower.StartsWith("nvidia update") Or
(child.ToLower.StartsWith("nvstreamsvc") AndAlso removegfe) Or
child.ToLower.StartsWith("nvidia opengl driver") Or
child.ToLower.StartsWith("nvwmi") Or
child.ToLower.StartsWith("nview") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
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

                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                            If child2.ToLower.Contains("controlset") Then
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\eventlog\System", True)
                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            If child.ToLower.StartsWith("nvidia update") Or
child.ToLower.StartsWith("nvidia opengl driver") Or
child.ToLower.StartsWith("nvwmi") Or
child.ToLower.StartsWith("nvlddmkm") Or
child.ToLower.StartsWith("nview") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
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
                Application.Log.AddMessage("End Remove eventviewer stuff")
                '---------------------------
                'end remove event view stuff
                '---------------------------


                '-----------------------
                'Windows Error Reporting
                '-----------------------

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "nvidia app", "nvidia overlay", "nvdlisrwrapper", "nvcontainer.exe", "nvidia geforce experience", "nvnodejslauncher", "nvidia share.exe", "nvidia web helper.exe", "nvidia.steamlauncher.exe", "nvoawrappercache.exe", "nvprofileupdater", "nvshim", "nvsphelper", "nvstreamer", "nvtelemetrycontainer", "nvtmmon", "nvtmrep", "oawrapper") AndAlso removegfe Then
                                Try
                                    Deletesubregkey(regkey, child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex, "Windows error Reporting (LocalDumps)")
                                End Try
                            End If
                        Next
                    End If
                End Using


                '---------------------------
                'virtual store
                '---------------------------

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(regkey, "Global", False)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                        If regkey.SubKeyCount = 0 Then
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "VirtualStore\MACHINE\SOFTWARE", True)
                                If regkey2 IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey2, "NVIDIA Corporation", False)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End Using
                        Else
                            For Each data As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                            Next
                        End If
                    End If
                End Using

                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If Not String.IsNullOrWhiteSpace(users) Then
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "Global", False)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                    If regkey.SubKeyCount = 0 Then
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
                                            If regkey2 IsNot Nothing Then
                                                Try
                                                    Deletesubregkey(regkey2, "NVIDIA Corporation", False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End Using
                                    Else
                                        For Each data As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(data) Then Continue For
                                            Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                        Next
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
                        If String.IsNullOrWhiteSpace(child) Then Continue For
                        If StrContainsAny(child, True, "s-1-5") Then
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, child & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "Global", False)
                                        If regkey.SubKeyCount = 0 Then
                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, child & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True)
                                                If regkey2 IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey2, "NVIDIA Corporation", False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using
                                        Else
                                            For Each data As String In regkey.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                            Next
                                        End If
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End Using
                        End If
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "SOFTWARE\NVIDIA Corporation", True)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(regkey, "Global", False)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                        If regkey.SubKeyCount = 0 Then
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "SOFTWARE", True)
                                If regkey2 IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey2, "NVIDIA Corporation", False)
                                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\SOFTWARE\NVIDIA Corporation", False)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End Using
                        Else
                            For Each data As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                            Next
                        End If
                    End If
                End Using

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Run", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "nvtmru", "NvCplDaemon", "NvMediaCenter", "NvBackend", "nwiz", "ShadowPlay", "StereoLinksInstall", "NvGameMonitor") Then
                                    Deletevalue(regkey, child)
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
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "StereoLinksInstall") Then
                                        Deletevalue(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                If config.Remove3DTVPlay Then
                    If MyRegistry.OpenSubKey(Registry.ClassesRoot, "mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV", False) IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.ClassesRoot, "mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                    If MyRegistry.OpenSubKey(Registry.ClassesRoot, "WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV", False) IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.ClassesRoot, "WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                    If MyRegistry.OpenSubKey(Registry.ClassesRoot, "AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV", False) IsNot Nothing Then
                        Try
                            Deletesubregkey(Registry.ClassesRoot, "AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If
                '-----------------------------
                'Shell extensions\approved
                '-----------------------------
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Or
regkey.GetValue(child).ToString.ToLower.Contains("nview desktop context menu") Or
regkey.GetValue(child).ToString.ToLower.Contains("nvappshext extension") Or
regkey.GetValue(child).ToString.ToLower.Contains("openglshext extension") Or
regkey.GetValue(child).ToString.ToLower.Contains("nvidia play on my tv context menu extension") Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
"Display\shellex\PropertySheetHandlers", True)
                    If regkey IsNot Nothing Then
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "NVIDIA CPL Extension", True)
                            If subregkey IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("NVIDIA CPL Extension", String.Empty).ToString
                                If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                    Using regkey2 As RegistryKey = Registry.Users
                                        If regkey2 IsNot Nothing Then
                                            For Each child As String In regkey2.GetSubKeyNames
                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child & "\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Cached", True)
                                                    If regkey3 IsNot Nothing Then
                                                        For Each valuename As String In regkey3.GetValueNames
                                                            If String.IsNullOrWhiteSpace(valuename) Then Continue For
                                                            If StrContainsAny(valuename, True, wantedvalue) Then
                                                                Try
                                                                    Deletevalue(regkey3, valuename, False)
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
                                End If
                                Try
                                    Deletesubregkey(regkey, "NVIDIA CPL Extension", False)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        End Using
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
"Display\shellex\PropertySheetHandlers", True)
                    If regkey IsNot Nothing Then
                        Try
                            Deletesubregkey(regkey, "NVIDIA CPL Extension", False)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Extended Properties", False)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                If regkey2 IsNot Nothing Then
                                    For Each childs As String In regkey2.GetValueNames()
                                        If String.IsNullOrWhiteSpace(childs) Then Continue For
                                        If StrContainsAny(childs, True, "nvcpl.cpl") Then
                                            Try
                                                Deletevalue(regkey2, childs)
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

                If IntPtr.Size = 8 Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(regkey.GetValue(child, String.Empty).ToString, False, "nvcpl desktopcontext class") Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
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
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Directory\background\shellex\ContextMenuHandlers", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "NvCplDesktopContext") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "NvCplDesktopContext")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If MyRegistry.OpenSubKey(regkey, "00nView") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "00nView")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If config.RemoveGFE AndAlso MyRegistry.OpenSubKey(regkey, "NvAppDesktopContext") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "NvAppDesktopContext")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Classes\Directory\background\shellex\ContextMenuHandlers", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "NvCplDesktopContext") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "NvCplDesktopContext")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If MyRegistry.OpenSubKey(regkey, "00nView") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "00nView")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "batfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "batfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "cmdfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "cmdfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "comfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "comfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "exefile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "exefile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "exefile\shellex\ContextMenuHandlers\NvAppShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "exefile\shellex\ContextMenuHandlers\NvAppShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "jsefile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "jsefile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "jsfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "jsfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "lnkfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "lnkfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "lnkfile\shellex\ContextMenuHandlers\NvAppShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "lnkfile\shellex\ContextMenuHandlers\NvAppShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "mscfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "mscfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "vbefile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "vbefile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "vbsfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "vbsfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "wsffile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "wsffile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, "wshfile\shellex\ContextMenuHandlers\OpenGLShExt", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, "wshfile\shellex\ContextMenuHandlers\OpenGLShExt")
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, ".avi\shellex", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, ".mpe\shellex", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}")
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, ".mpeg\shellex", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, ".mpg\shellex", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, ".wmv\shellex", True)
                    If regkey IsNot Nothing Then
                        If MyRegistry.OpenSubKey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}") IsNot Nothing Then
                            Try
                                Deletesubregkey(regkey, "{3D1975AF-0FC3-463d-8965-4DC6B5A840F4}")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                'Cleaning of some "open with application" related to 3d vision
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "jpsfile\shell\open\command", True)
                    If regkey IsNot Nothing Then
                        If (Not String.IsNullOrWhiteSpace(regkey.GetValue("", String.Empty).ToString)) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
                            Try
                                Deletesubregkey(Registry.ClassesRoot, "jpsfile", False)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "mpofile\shell\open\command", True)
                    If regkey IsNot Nothing Then
                        If (Not String.IsNullOrWhiteSpace(regkey.GetValue("", String.Empty).ToString)) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
                            Try
                                Deletesubregkey(Registry.ClassesRoot, "mpofile", False)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "pnsfile\shell\open\command", True)
                    If regkey IsNot Nothing Then
                        If (Not String.IsNullOrWhiteSpace(regkey.GetValue("", String.Empty).ToString)) AndAlso StrContainsAny(regkey.GetValue("", String.Empty).ToString, True, "nvstview") Then
                            Try
                                Deletesubregkey(Registry.ClassesRoot, "pnsfile", False)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, ".tvp") IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, ".tvp")  'CrazY_Milojko
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                'Task Scheduler cleanUP 
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                If regkey2 IsNot Nothing Then
                                    If Not String.IsNullOrWhiteSpace(regkey2.GetValue("Description", String.Empty).ToString) Then
                                        If StrContainsAny(regkey2.GetValue("Description", String.Empty).ToString, True, "nvprofileupdater", "nvnodelauncher", "nvtmmon", "nvtmrep", "NvDriverUpdateCheckDaily", "NVIDIA GeForce Experience", "NVIDIA Profile Updater", "NVIDIA telemetry monitor", "NVIDIA crash and telemetry reporter", "batteryboost", "nvngx", "NVIDIA App SelfUpdate") AndAlso config.RemoveGFE Then
                                            Deletesubregkey(regkey, child)
                                        End If
                                    End If
                                End If
                            End Using
                        Next
                    End If
                End Using

                Using schedule As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache", True)
                    If schedule IsNot Nothing Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(schedule, "Tree", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "nvprofileupdater", "nvnodelauncher", "nvtmmon", "nvtmrep", "NvDriverUpdateCheckDaily", "NVIDIA GeForce Experience", "NvBatteryBoostCheckOnLogon", "nvngx", "NVIDIA App SelfUpdate") AndAlso config.RemoveGFE Then
                                        For Each ScheduleChild As String In schedule.GetSubKeyNames
                                            If String.IsNullOrWhiteSpace(ScheduleChild) Then Continue For
                                            Try
                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                                    If regkey2 IsNot Nothing Then
                                                        If Not String.IsNullOrWhiteSpace(regkey2.GetValue("Id", String.Empty).ToString) Then
                                                            wantedvalue = regkey2.GetValue("Id", String.Empty).ToString
                                                            Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(schedule, ScheduleChild, True)
                                                                If regkey3 IsNot Nothing Then
                                                                    For Each child2 As String In regkey3.GetSubKeyNames
                                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                                        If StrContainsAny(wantedvalue, True, child2) Then
                                                                            Deletesubregkey(regkey3, child2)
                                                                        End If
                                                                    Next
                                                                End If
                                                            End Using
                                                        End If
                                                    End If
                                                End Using
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        Next
                                        Deletesubregkey(regkey, child)
                                    End If
                                Next
                            End If
                        End Using
                    End If
                End Using

                Dim filePath As String = config.Paths.System32 + "Tasks"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetFiles(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "nvprofileupdater", "nvnodelauncher", "nvtmmon", "nvtmrep", "NvDriverUpdateCheckDaily", "NVIDIA GeForce Experience", "NvBatteryBoostCheckOnLogon", "nvngx") AndAlso config.RemoveGFE Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    End If
                End If


                '      Dim OldValue As String = Nothing
                '      Select Case System.Windows.Forms.SystemInformation.BootMode
                '          Case Forms.BootMode.FailSafe
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If

                '          Case Forms.BootMode.FailSafeWithNetwork
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If
                '          Case Forms.BootMode.Normal
                '              'Usually this service is Running in normal mode, we *could* in the future check all this.
                '              If (CheckServiceStartupType("Schedule")) <> "4" Then
                '                  StartService("Schedule")
                '              Else
                '                  OldValue = CheckServiceStartupType("Schedule")
                '                  SetServiceStartupType("Schedule", "3")
                '                  StartService("Schedule")
                '              End If
                '      End Select

                'Using tsc As New TaskSchedulerControl(config)
                '	For Each task As Task In tsc.GetAllTasks
                '		If StrContainsAny(task.Name, True, "nvprofileupdater", "nvnodelauncher", "nvtmmon", "nvtmrep", "NvDriverUpdateCheckDaily", "NVIDIA GeForce Experience") AndAlso config.RemoveGFE Then
                '			Try
                '				task.Delete()
                '			Catch ex As Exception
                '				Application.Log.AddException(ex)
                '			End Try
                '			Application.Log.AddMessage("TaskScheduler: " & task.Name & " as been removed")
                '		End If
                '	Next
                'End Using

                '      Select Case System.Windows.Forms.SystemInformation.BootMode
                '          Case Forms.BootMode.FailSafe
                '              StopService("Schedule")
                '              If OldValue IsNot Nothing Then
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                '          Case Forms.BootMode.FailSafeWithNetwork
                '              StopService("Schedule")
                '              If OldValue IsNot Nothing Then
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                '          Case Forms.BootMode.Normal
                '              'Usually this service is running in normal mode, we don't need to stop it.
                '              If OldValue IsNot Nothing Then
                '                  StopService("Schedule")
                '                  SetServiceStartupType("Schedule", OldValue)
                '              End If
                '      End Select

                UpdateTextMethod("End of Registry Cleaning")
                Application.Log.AddMessage("End of Registry Cleaning")

                'Killing Explorer.exe to help releasing file that were open.
                Application.Log.AddMessage("Killing Explorer.exe")
                KillProcess("explorer")
            End Sub)
        End Sub

        Private Sub CleanNvidiaCache(ByVal config As ThreadSettings)
            Dim filePath As String = config.Paths.System32 + "config\systemprofile\AppData\Local\NVIDIA"
            If _fileIo.ExistsDir(filePath) Then
                If filePath IsNot Nothing Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                Delete(child)
                            End If
                        End If
                    Next
                End If
            End If

            filePath = config.Paths.System32 + "config\systemprofile\AppData\Local\D3DSCache"
            If _fileIo.ExistsDir(filePath) Then
                Try
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            Delete(child)
                        End If
                    Next
                Catch ex As Exception
                    Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                End Try
            End If

            filePath = config.Paths.System32 + "config\systemprofile\AppData\LocalLow\NVIDIA"
            If _fileIo.ExistsDir(filePath) Then
                If filePath IsNot Nothing Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "PerDriverVersion", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If
            End If

            filePath = config.Paths.WinDir + "ServiceProfiles\LocalService\AppData\Local\NVIDIA"
            If _fileIo.ExistsDir(filePath) Then
                If filePath IsNot Nothing Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If
            End If

            For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                filePath = filepaths + "\AppData\LocalLow\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "PerDriverVersion", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = filepaths + "\AppData\Local\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                filePath = filepaths + "\AppData\Roaming\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                filePath = filepaths + "\AppData\Local\D3DSCache"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                Try
                    Dim paths() As String = {"\AC\Temp\NVIDIA Corporation", "\AC\NVIDIA", "\LocalCache\Local\NVIDIA"}
                    If _isWindows8OrHigher Then
                        Dim prefilePath As String = filepaths + "\AppData\Local\Packages"
                        If _fileIo.ExistsDir(prefilePath) Then
                            For Each childs As String In _fileIo.GetDirectories(prefilePath)
                                If Not String.IsNullOrWhiteSpace(childs) Then
                                    For Each path As String In paths
                                        filePath = childs + path
                                        If _fileIo.ExistsDir(filePath) Then
                                            For Each child As String In _fileIo.GetDirectories(filePath)
                                                If String.IsNullOrWhiteSpace(child) = False Then
                                                    If StrContainsAny(child, True, "nv_cache", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                                        Delete(child)
                                                    End If
                                                End If
                                            Next
                                            If _fileIo.CountDirectories(filePath) = 0 Then
                                                Delete(filePath)
                                            Else
                                                For Each data As String In _fileIo.GetDirectories(filePath)
                                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                                Next
                                            End If
                                        End If
                                    Next
                                End If
                            Next
                        End If
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
            Next
        End Sub

        Private Sub CleanNvidiaFolders(ByVal config As ThreadSettings)
            Dim filePath As String = Nothing
            Dim removephysx As Boolean = config.RemovePhysX
            Dim driverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\driverfiles.cfg")
            Dim gfedriverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\gfedriverfiles.cfg")
            Dim rtxAud As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\rtxaud.cfg")
            Dim nvbdriverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\NVIDIA\nvbdriverfiles.cfg")
            Dim TaskList = New List(Of Task)()

            Dim thread1 As Task = Task.Run(Sub() Threaddata1(driverfiles))
            TaskList.Add(thread1)

            If config.RemoveGFE Then
                Dim thread2 As Task = Task.Run(Sub() Threaddata1(gfedriverfiles))
                TaskList.Add(thread2)
            End If

            If config.RemoveNVBROADCAST Then
                Dim thread3 As Task = Task.Run(Sub() Threaddata1(nvbdriverfiles))
                TaskList.Add(thread3)
            End If

            ' Check if both apps are either not installed or set to be removed
            Dim shouldRemoveBoth As Boolean =
    ((Not config.NVIDIA_App_Installed) OrElse config.RemoveGFE) AndAlso
    ((Not config.NVIDIA_Broadcast_Installed) OrElse config.RemoveNVBROADCAST)

            If shouldRemoveBoth Then
                Dim thread3 As Task = Task.Run(Sub() Threaddata1(rtxAud))
                TaskList.Add(thread3)
            End If

            Task.WaitAll(TaskList.ToArray())

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

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

                CleanNvidiaCache(config)

                If config.RemoveNvidiaDirs = True Then
                    filePath = _sysdrv + "NVIDIA"
                    Delete(filePath)
                End If

                ' here I erase the folders / files of the nvidia GFE / update in users.
                filePath = config.Paths.UsersPath
                For Each child As String In _fileIo.GetDirectories(filePath)
                    If String.IsNullOrWhiteSpace(child) = False Then
                        If StrContainsAny(child, True, "updatususer") Then
                            Delete(child)
                            Delete(child)


                            'Yes we do it 2 times. This will workaround a problem on junction/sybolic/hard link
                            '(Will have to see if this is still valid. This was on old driver pre 300.xx I believe :/ )
                            Delete(child)
                            Delete(child)
                        End If
                    End If
                Next

                filePath = config.Paths.UsersPath + "Public\Desktop"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetFiles(filePath, "*.lnk")
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(DesktopIconRemover.GetShortcutTargetPath(child), True, "GeForce Experience.exe", "NVIDIA App.exe") AndAlso config.RemoveGFE Then
                                Delete(child)
                                Continue For
                            End If

                            If StrContainsAny(DesktopIconRemover.GetShortcutTargetPath(child), True, "3d vision photo viewer") Then
                                Delete(child)
                                Continue For
                            End If

                            If (StrContainsAny(DesktopIconRemover.GetShortcutTargetPath(child), True, "NVIDIA Broadcast.exe") AndAlso config.RemoveNVBROADCAST) Then
                                Delete(child)
                            End If
                        Next
                    End If
                End If

                filePath = config.Paths.UsersPath + "Public\Pictures\NVIDIA Corporation"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "3d vision experience") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                filePath = config.Paths.System32 + "drivers\NVIDIA Corporation"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "drs") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                filePath = config.Paths.System32 + "config\systemprofile\AppData\Local\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                filePath = config.Paths.System32 + "config\systemprofile\AppData\LocalLow\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "PerDriverVersion", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                filePath = config.Paths.WinDir + "ServiceProfiles\LocalService\AppData\Local\NVIDIA"
                If _fileIo.ExistsDir(filePath) Then
                    If filePath IsNot Nothing Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                    If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                    filePath = filepaths + "\AppData\LocalLow\NVIDIA"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "PerDriverVersion", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\NVIDIA"
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "nvbackend", "gfexperience") AndAlso config.RemoveGFE Or StrContainsAny(child, True, "nvosc", "shareconnect", "nvgs", "glcache", "DXCache", "FrameViewSdk", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                        End Try
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    filePath = filepaths + "\AppData\Roaming\NVIDIA"
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "DXCache", "GLCache", "OptixCache", "computecache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        Try
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                        End Try
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    filePath = filepaths + "\AppData\Local\NVIDIA Corporation"
                    If config.RemoveGFE Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If (child.ToLower.Contains("ledvisualizer") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvab") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvnode") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvtmmon") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvprofileupdater") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE) Or
(child.ToLower.EndsWith("\osc") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvvad") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia share") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia notification") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvfbc") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvtmrep") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfesdk") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("ansel") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvdriverupdatecheck") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvbatteryboostcheck") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("scanner") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvetwlog")) Or
(child.ToLower.Contains("nv_cache") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfnruntimesdk") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("frameviewsdk") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia app") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("shared store") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia overlay") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("shield apps") AndAlso config.RemoveGFE) Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            Try
                                If _fileIo.CountDirectories(filePath) = 0 Then
                                    Delete(filePath)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                    Next
                                End If
                            Catch ex As Exception
                            End Try
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\D3DSCache"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\D3DSCache"
                    End If

                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    Delete(child)
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                Next

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"
                Try
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("updatus") Or
child.ToLower.Contains("shimgen") Or
child.ToLower.Contains("streamline") Or
(child.ToLower.Contains("nvidiabroadcast") AndAlso config.RemoveNVBROADCAST) Or
(child.ToLower.Contains("grid") AndAlso config.RemoveGFE) Then
                                Delete(child)
                            End If
                            If StrContainsAny(child, True, "ngx") Then
                                For Each child2 As String In _fileIo.GetDirectories(child)
                                    If Not String.IsNullOrWhiteSpace(child2) Then
                                        For Each child3 As String In _fileIo.GetDirectories(child2)
                                            If String.IsNullOrWhiteSpace(child3) Then Continue For
                                            If StrContainsAny(child3, True, "nvbroadcast", "nvbcast") AndAlso Not config.RemoveNVBROADCAST Then
                                                'do nothing
                                            Else
                                                Delete(child3)
                                            End If
                                        Next
                                        Try
                                            If _fileIo.CountDirectories(child2) = 0 Then
                                                Delete(child2)
                                            Else
                                                For Each data As String In _fileIo.GetDirectories(child2)
                                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + child2 + "\ --> " + data)
                                                Next
                                            End If
                                        Catch ex As Exception
                                        End Try
                                    End If
                                Next
                                Try
                                    If _fileIo.CountDirectories(child) = 0 Then
                                        Delete(child)
                                    Else
                                        For Each data As String In _fileIo.GetDirectories(child)
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + child + "\ --> " + data)
                                        Next
                                    End If
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next

                    Try
                        If _fileIo.CountDirectories(filePath) = 0 AndAlso config.RemoveGFE Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    Catch ex As Exception
                    End Try

                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"
                Try
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If (StrContainsAny(child, True, "drs") AndAlso Not config.KeepNVCPopt) Or
StrContainsAny(child, True, "nv_cache", "umdlogs", "nvtopps", "GameSessionTelemetry") Or
(child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvnode") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvab") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("netservice") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("rx") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("crashdumps") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvstream") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("downloader") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfebridges") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("ledvisualizer") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nview") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvfbc") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvstapisvr") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvstereoinstaller") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvvad") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("driverdumps") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("displaydriverras") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvprofileupdaterplugin") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvapp-updateframework") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia app") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("frameviewsdk") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvidia broadcast") AndAlso config.RemoveNVBROADCAST) Or
(child.ToLower.Contains("gfnruntimesdk") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("nvstreamsvc") AndAlso config.RemoveGFE) Then
                                Delete(child)
                            End If
                        End If
                    Next

                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                Catch ex As Exception
                End Try
                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData)
                Try
                    For Each child As String In _fileIo.GetFiles(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "DisplaySessionContainer", "", "nvcdispcoreplugin", "NVDisplay.Container") Then
                                Delete(child)
                            End If
                        End If
                    Next
                Catch ex As Exception
                End Try
                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation"
                Try
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("3d vision") Then
                                Delete(child)
                            End If
                        End If
                    Next
                    For Each child As String In _fileIo.GetFiles(filePath, "*.lnk")
                        If String.IsNullOrWhiteSpace(child) Then Continue For
                        If (StrContainsAny(DesktopIconRemover.GetShortcutTargetPath(child), True, "GeForce Experience.exe", "NVIDIA App.exe") AndAlso config.RemoveGFE) Or
StrContainsAny(DesktopIconRemover.GetShortcutTargetPath(child), True, "nvidia broadcast") AndAlso config.RemoveNVBROADCAST Then
                            Delete(child)
                        End If
                    Next
                    Try
                        If _fileIo.CountDirectories(filePath) = 0 AndAlso (_fileIo.CountFiles(filePath) = 0 AndAlso config.RemoveGFE) Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                            For Each data As String In _fileIo.GetFiles(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining file(s) found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    Catch ex As Exception
                    End Try
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
                If _fileIo.ExistsDir(filePath) Then
                    Dim hit As Boolean = False
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If child.ToLower.Contains("control panel client") Or
child.ToLower.Contains("display") Or
child.ToLower.Contains("vgpu licensing") Or
child.ToLower.Contains("coprocmanager") Or
child.ToLower.Contains("drs") Or
child.ToLower.Contains("nvsmi") Or
child.ToLower.Contains("opencl") Or
child.ToLower.Contains("ansel") Or
child.ToLower.Contains("nvtopps") Or
child.ToLower.Contains("3d vision") Or
child.ToLower.Contains("frameviewsdk") AndAlso config.RemoveGFE Or
child.ToLower.Contains("led visualizer") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvab") AndAlso config.RemoveGFE Or
child.ToLower.Contains("netservice") AndAlso config.RemoveGFE Or
child.ToLower.Contains("geforce experience") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvidia app") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvstreamc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
child.ToLower.EndsWith("\physx") AndAlso config.RemovePhysX Or
child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvfbc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("update common") AndAlso config.RemoveGFE Or
child.ToLower.Contains("shield") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nview") Or
child.ToLower.Contains("nvidia wmi provider") Or
child.ToLower.Contains("gamemonitor") AndAlso config.RemoveGFE Or
child.ToLower.Contains("\nvcontainer") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvidia ngx") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvprofileupdater") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvdriverupdatecheck") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvdlisr") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvgsync") Or
child.ToLower.Contains("nvupdate") Or
child.ToLower.Contains("wksserviceplugin") Or
child.ToLower.Contains("nvidia broadcast") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("nvbroadcast.nvcontainer") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("update core") AndAlso config.RemoveGFE Then
                                Delete(child)
                            End If
                            If child.ToLower.Contains("installer2") Then
                                For Each child2 As String In _fileIo.GetDirectories(child)
                                    If String.IsNullOrWhiteSpace(child2) = False Then
                                        If child2.ToLower.Contains("display.3dvision") Or
child2.ToLower.Contains("display.controlpanel") Or
child2.ToLower.Contains("display.driver") Or
child2.ToLower.Contains("displaydriveranalyzer") Or
child2.ToLower.Contains("display.optimus") Or
child2.ToLower.Contains("ngxcore.") Or
child2.ToLower.Contains("msvcruntime") Or
child2.ToLower.Contains("ansel.") Or
child2.ToLower.Contains("nvdisplaycontainer") Or
child2.ToLower.Contains("display.gfexperience") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvab") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("osc.") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("osclib.") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("display.nvirusb") Or
child2.ToLower.Contains("usbc.") Or
child2.ToLower.Contains("nvdisplay.messagebus") Or
child2.ToLower.Contains("frameviewsdk") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("gpxcommon.oss") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("display.physx") AndAlso config.RemovePhysX Or
child2.ToLower.Contains("display.update") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("display.gamemonitor") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("gfexperience") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("display.nvapp") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvdlisr") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvidia.update") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("installer2\installer") AndAlso config.RemoveGFE AndAlso config.RemovePhysX Or
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
child2.ToLower.Contains("nvdisplaypluginwatchdog") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvdisplaysessioncontainer") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvtelemetry") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvvhci") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("nvmoduletracker.driver") AndAlso config.RemoveGFE Or
child2.ToLower.Contains("broadcastvoice.driver") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("nvmodels.") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("nvbroadcastcontainer.") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("rtx voice") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("nvvirtualcamera.") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("nvidiabroadcast.") AndAlso config.RemoveNVBROADCAST Or
child2.ToLower.Contains("hdaudio.driver") AndAlso config.RemoveGFE Then


                                            'This registry check is for protection to prevent removal of CUDA (or other) Nvidia uninstall association.
                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
                                                If regkey IsNot Nothing Then
                                                    For Each childs As String In regkey.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(childs) Then Continue For
                                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, childs)
                                                            If regkey2 IsNot Nothing Then
                                                                If removephysx Then
                                                                    If String.IsNullOrWhiteSpace(regkey2.GetValue("NVI2_Package", String.Empty).ToString) = False Then
                                                                        If StrContainsAny(regkey2.GetValue("NVI2_Package", String.Empty).ToString, True, child2) Then
                                                                            hit = True
                                                                        End If
                                                                    End If
                                                                    If String.IsNullOrWhiteSpace(regkey2.GetValue("UninstallString_Hidden", String.Empty).ToString) = False Then
                                                                        If StrContainsAny(regkey2.GetValue("UninstallString_Hidden", String.Empty).ToString, True, child2) Then
                                                                            hit = True
                                                                        End If
                                                                    End If
                                                                    If String.IsNullOrWhiteSpace(regkey2.GetValue("UninstallString", String.Empty).ToString) = False Then
                                                                        If StrContainsAny(regkey2.GetValue("UninstallString", String.Empty).ToString, True, child2) Then
                                                                            hit = True
                                                                        End If
                                                                    End If
                                                                    If String.IsNullOrWhiteSpace(regkey2.GetValue("NVI2_Setup", String.Empty).ToString) = False Then
                                                                        If StrContainsAny(regkey2.GetValue("NVI2_Setup", String.Empty).ToString, True, child2) Then
                                                                            hit = True
                                                                        End If
                                                                    End If
                                                                End If
                                                            End If
                                                        End Using
                                                    Next
                                                End If
                                            End Using
                                            If Not hit Then
                                                Delete(child2)
                                            Else
                                                hit = False
                                            End If
                                        End If
                                    End If
                                Next
                                If _fileIo.CountDirectories(child) = 0 Then
                                    Delete(child)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(child)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + child + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End If
                    Next

                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                If config.RemovePhysX Then
                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\AGEIA Technologies"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If
                End If

                If IntPtr.Size = 8 Then
                    filePath = config.Paths.ProgramFilesx86 & "NVIDIA Corporation"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If child.ToLower.Contains("3d vision") Or
child.ToLower.Contains("coprocmanager") Or
child.ToLower.Contains("led visualizer") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvab") AndAlso config.RemoveGFE Or
child.ToLower.Contains("osc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("frameviewsdk") AndAlso config.RemoveGFE Or
child.ToLower.Contains("netservice") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvidia geforce experience") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvidia app") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvstreamc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvstreamsrv") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvfbc") AndAlso config.RemoveGFE Or
child.ToLower.Contains("update common") AndAlso config.RemoveGFE Or
child.ToLower.Contains("display.nvcontainer") AndAlso config.RemoveGFE Or
child.ToLower.Equals(filePath.ToLower + "\nvcontainer") AndAlso config.RemoveGFE Or
child.ToLower.Equals(filePath.ToLower + "\nvbroadcast.nvcontainer") AndAlso config.RemoveNVBROADCAST Or
child.ToLower.Contains("nvbackend") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvnode") AndAlso config.RemoveGFE Or
child.ToLower.Contains("shadowplay") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvinstallerutil") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvprofileupdater") AndAlso config.RemoveGFE Or
child.ToLower.Contains("nvgsync") Or
child.ToLower.Contains("nvidia updatus") Or
child.ToLower.EndsWith("\physx") AndAlso config.RemovePhysX Or
child.ToLower.EndsWith("nvtelemetry") AndAlso config.RemoveGFE Or
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
                        If _fileIo.CountDirectories(filePath) = 0 Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    End If
                End If

                If config.RemovePhysX Then
                    If IntPtr.Size = 8 Then
                        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AGEIA Technologies"
                        If _fileIo.ExistsDir(filePath) Then
                            Delete(filePath)
                        End If
                    End If
                End If

                filePath = config.Paths.System32
                Dim files() As String = IO.Directory.GetFiles(filePath, "nvdisp*.*")
                For i As Integer = 0 To files.Length - 1
                    If Not String.IsNullOrWhiteSpace(files(i)) Then
                        Delete(files(i))
                    End If
                Next

                filePath = config.Paths.System32
                files = IO.Directory.GetFiles(filePath, "nvhdagenco*.*")
                For i As Integer = 0 To files.Length - 1
                    If Not String.IsNullOrWhiteSpace(files(i)) Then
                        Delete(files(i))
                    End If
                Next

                filePath = config.Paths.WinDir
                Try
                    Delete(filePath + "Help\nvcpl")
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    filePath = config.Paths.WinDir + "Temp"
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "NVIDIA Corporation", "NvidiaLogging") Then
                                Delete(child)
                            End If
                        End If
                    Next
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    filePath = config.Paths.SystemDrive & "Temp"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "NVIDIA") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                    If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                    filePath = filepaths + "\AppData\Local\Temp\NvidiaLogging"
                    If _fileIo.ExistsDir(filePath) AndAlso config.RemoveGFE Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    Delete(child)
                                End If
                            Next
                            Try
                                If _fileIo.CountDirectories(filePath) = 0 Then
                                    Delete(filePath)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                    Next
                                End If
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\Temp\NVIDIA Corporation"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("nv_cache") Or
child.ToLower.Contains("displaydriver") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            Try
                                If _fileIo.CountDirectories(filePath) = 0 Then
                                    Delete(filePath)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                    Next
                                End If
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\Temp\NVIDIA"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If (child.ToLower.Contains("geforceexperienceselfupdate") AndAlso config.RemoveGFE) Or
(child.ToLower.Contains("gfe") AndAlso config.RemoveGFE) Or
child.ToLower.Contains("displaydriver") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            Try
                                If _fileIo.CountDirectories(filePath) = 0 Then
                                    Delete(filePath)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                    Next
                                End If
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                    filePath = filepaths + "\AppData\Local\Temp\Low\NVIDIA Corporation"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("nv_cache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            Try
                                If _fileIo.CountDirectories(filePath) = 0 Then
                                    Delete(filePath)
                                Else
                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                    Next
                                End If
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                    'windows 8+ only (store apps nv_cache cleanup)

                    Try
                        Dim paths() As String = {"\AC\Temp\NVIDIA Corporation", "\AC\NVIDIA", "\LocalCache\Local\NVIDIA"}
                        If _isWindows8OrHigher Then
                            Dim prefilePath As String = filepaths + "\AppData\Local\Packages"
                            If _fileIo.ExistsDir(prefilePath) Then
                                For Each childs As String In _fileIo.GetDirectories(prefilePath)
                                    If Not String.IsNullOrWhiteSpace(childs) Then
                                        For Each path As String In paths
                                            filePath = childs + path
                                            If _fileIo.ExistsDir(filePath) Then
                                                For Each child As String In _fileIo.GetDirectories(filePath)
                                                    If String.IsNullOrWhiteSpace(child) = False Then
                                                        If StrContainsAny(child, True, "nv_cache", "DXCache", "GLCache", "OptixCache", "computecache") Then
                                                            Delete(child)
                                                        End If
                                                    End If
                                                Next
                                                If _fileIo.CountDirectories(filePath) = 0 Then
                                                    Delete(filePath)
                                                Else
                                                    For Each data As String In _fileIo.GetDirectories(filePath)
                                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                                        Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                                    Next
                                                End If
                                            End If
                                        Next
                                    End If
                                Next
                            End If
                        End If
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                Next

                'Cleaning the GFE 2.0.1 and earlier assemblies.
                If config.RemoveGFE Then
                    filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_32"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
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
                        If String.IsNullOrWhiteSpace(regusers) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\software\classes\local settings\muicache", False)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                        If subregkey IsNot Nothing Then
                                            For Each childs As String In subregkey.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(childs) Then Continue For
                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, childs, True)
                                                    If regkey2 IsNot Nothing Then
                                                        For Each Keyname As String In regkey2.GetValueNames
                                                            If String.IsNullOrWhiteSpace(Keyname) Then Continue For
                                                            If StrContainsAny(Keyname, True, "nvstlink.exe", "nvstview.exe", "nvcpluir.dll", "nvcplui.exe", "mcu.exe") Or
(StrContainsAny(Keyname, True, "gfexperience.exe", "nvidia share.exe", "nvidia app") AndAlso config.RemoveGFE) Then
                                                                Try
                                                                    Deletevalue(regkey2, Keyname)
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
                                Next
                            End If
                        End Using

                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "nvcplui.exe", "nvtray.exe") Or
(StrContainsAny(child, True, "nvbackend.exe") AndAlso config.RemoveGFE) Or
(StrContainsAny(child, True, "GeForce Experience\Update\setup.exe") AndAlso config.RemoveGFE) Then
                                        Try
                                            Deletevalue(regkey, child)
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
                        If String.IsNullOrWhiteSpace(regusers) Then Continue For
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regusers & "\software\classes\local settings\software\microsoft\windows\shell\muicache", True)
                            If regkey IsNot Nothing Then
                                For Each Keyname As String In regkey.GetValueNames
                                    If String.IsNullOrWhiteSpace(Keyname) Then Continue For
                                    If StrContainsAny(Keyname, True, "nvcplui.exe", "nvstlink.exe", "nvstview.exe", "nvcpluir.dll") Or
(StrContainsAny(Keyname, True, "gfexperience.exe", "nvidia share.exe") AndAlso config.RemoveGFE) Then
                                        Try
                                            Deletevalue(regkey, Keyname)
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

            End Sub)
        End Sub

        Private Sub CleanIntel(ByVal config As ThreadSettings, ByVal Optional preclean As Boolean = False)
            Dim CleanupEngine As New CleanupEngine
            Dim wantedvalue As String = Nothing
            Dim packages As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\packages.cfg")
            Dim packagesigs As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\packagesigs.cfg")
            Dim packagesoneapi As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\packagesoneapi.cfg")
            Dim packagesEndurance As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\packagesendurance.cfg")
            Dim packagesNpu As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\packagesnpu.cfg")
            Dim classroot As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\classroot.cfg")
            Dim reginterface As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\interface.cfg")
            Dim clsidleftover As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\clsidleftover.cfg")
            Dim clsidleftoverigs As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\clsidleftoverigs.cfg")
            Dim driverfiles As String() = IO.File.ReadAllLines(config.Paths.AppBase & "settings\INTEL\driverfiles.cfg")

            If preclean Then
                UpdateTextMethod(UpdateTextTranslated(5))
                Application.Log.AddMessage("Cleaning registry Part 1/2")

                'Removal of the (DCH) from the Window Store. (In progress...)
                If _win10 Then
                    If config.RemoveINTELCP Then
                        CleanupEngine.RemoveAppxAsync("IntelGraphicsControlPanel").Wait()
                        CleanupEngine.RemoveAppxAsync("IntelGraphicsExperience").Wait()
                        CleanupEngine.RemoveAppxAsync("IntelGraphicsCommandCenter").Wait()
                    End If
                End If

                ImpersonateUser.RunImpersonatedSystem(
                Sub()

                    CleanupEngine.PnpLockdownFiles(driverfiles) '// add each line as String Array.

                    CleanupEngine.ClassRoot(classroot, config) '// add each line as String Array.

                    CleanupEngine.Interfaces(reginterface) '// add each line as String Array.

                    CleanupEngine.Clsidleftover(clsidleftover) '// add each line as String Array.

                    If config.RemoveINTELIGS Then
                        CleanupEngine.Clsidleftover(clsidleftoverigs) '// add each line as String Array.
                    End If

                End Sub)

                Return
            End If

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning registry Part 2/2")

                '--------------------------
                'Power Settings CleanUP
                '--------------------------
                Application.Log.AddMessage("Power Settings Cleanup")
                Try
                    If _winxp = False Then
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                            If subregkey IsNot Nothing Then
                                For Each child2 As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child2) Then Continue For
                                    If child2.ToLower.Contains("controlset") Then
                                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
                                            If regkey IsNot Nothing Then
                                                For Each childs As String In regkey.GetSubKeyNames()
                                                    If String.IsNullOrWhiteSpace(childs) Then Continue For
                                                    Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, childs)
                                                        If regkey2 IsNot Nothing Then
                                                            For Each child As String In regkey2.GetValueNames()
                                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                                If StrContainsAny(child, True, "Description") Then
                                                                    wantedvalue = regkey2.GetValue(child, String.Empty).ToString()
                                                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For

                                                                    'Usually this key : 44f3beca-a7c0-460e-9df2-bb8b99e0cba6
                                                                    If StrContainsAny(wantedvalue, True, "Configure Intel(R) Graphics Settings") Then
                                                                        Try
                                                                            Deletesubregkey(regkey, childs)
                                                                            Continue For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
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
                                Next
                            End If
                        End Using
                    End If
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                If config.RemoveVulkan Then
                    CleanVulkan(config)
                End If

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Intel", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "display", "igd", "gfx", "mediasdk", "opencl", "intel wireless display", "kmd", "mdf", "xesdk") OrElse
(config.RemoveINTELIGS AndAlso StrContainsAny(child, True, "Intel Arc Control")) OrElse
(config.RemoveEnduranceGaming AndAlso child.Equals("EG", StringComparison.OrdinalIgnoreCase)) Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "Software", True), "Intel")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Intel Corporation", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If config.RemoveEnduranceGaming AndAlso StrContainsAny(child, True, "Intel Endurance Gaming") Then
                                    Try
                                        Deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "Software", True), "Intel Corporation")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    For Each users As String In Registry.Users.GetSubKeyNames()
                        If Not String.IsNullOrWhiteSpace(users) Then
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Intel", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(child) = False Then
                                            If StrContainsAny(child, True, "display", "IGN") OrElse (config.RemoveINTELIGS AndAlso StrContainsAny(child, True, "IntelGraphicsSoftware", "presentmon", "gameassist")) Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    Next
                                    If regkey.SubKeyCount = 0 Then
                                        Try
                                            Deletesubregkey(MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True), "Intel")
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    Else
                                        For Each data As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(data) Then Continue For
                                            Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                        Next
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
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        If StrContainsAny(child, True, "display", "igd", "gfx", "mediasdk", "opencl", "intel wireless display", "mdf") Then
                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                Next
                                If regkey.SubKeyCount = 0 Then
                                    Try
                                        Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node", True), "Intel")
                                    Catch ex As Exception
                                    End Try
                                Else
                                    For Each data As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(data) Then Continue For
                                        Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                    Next
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    If config.RemoveINTELIGS Then
                        Try
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                                If regkey IsNot Nothing Then
                                    If regkey.GetValue("Intel® Arc™ Control") IsNot Nothing Then
                                        Try
                                            Deletevalue(regkey, "Intel® Arc™ Control", False)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If regkey.GetValue("Intel® Graphics Software") IsNot Nothing Then
                                        Try
                                            Deletevalue(regkey, "Intel® Graphics Software", False)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            End Using
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Run", True)
                        If regkey IsNot Nothing Then
                            If regkey.GetValue("IgfxTray") IsNot Nothing Then
                                Try
                                    Deletevalue(regkey, "IgfxTray")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                            If regkey.GetValue("Persistence") IsNot Nothing Then
                                Try
                                    Deletevalue(regkey, "Persistence")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                            If regkey.GetValue("HotKeysCmds") IsNot Nothing Then
                                Try
                                    Deletevalue(regkey, "HotKeysCmds")
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                            If config.RemoveEnduranceGaming Then
                                If regkey.GetValue("Intel Endurance Gaming") IsNot Nothing Then
                                    Try
                                        Deletevalue(regkey, "Intel Endurance Gaming")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Directory\background\shellex\ContextMenuHandlers", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("igfxcui") Or
child.ToLower.Contains("igfxosp") Or
child.ToLower.Contains("igfxdtcm") Then
                                        Deletesubregkey(regkey, child)
                                    End If
                                End If
                            Next
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Directory\background\shell", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If config.RemoveINTELIGS AndAlso StrContainsAny(child, True, "Intel® Arc™ Control", "Intel Arc Control") Then
                                    Deletesubregkey(regkey, child)
                                End If
                            Next
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
                CleanupEngine.Installer(packages, config)

                If config.RemoveINTELIGS Then
                    CleanupEngine.Installer(packagesigs, config)
                End If

                If config.RemoveOneAPI Then
                    CleanupEngine.Installer(packagesoneapi, config)
                End If

                If config.RemoveEnduranceGaming Then
                    CleanupEngine.Installer(packagesEndurance, config)
                End If

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
"Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)
                                    If subregkey IsNot Nothing Then
                                        If String.IsNullOrWhiteSpace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then
                                            'Specific fix/workaround for failing to remove in the past the Package cache and causing the Intel installer to create an incomplete GUID regkey
                                            If StrContainsAny(child, True, "{43B4715B-9FFB-47B0-AEAD-7C6D755EE010}", "{41a5e581-4a2c-406c-a1b5-ec680ffc64c8}", "{f8176a62-cc98-418f-a208-e187faebe116}") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                        If dependencyRegkey IsNot Nothing Then
                                                            For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                    Try
                                                                        Deletesubregkey(dependencyRegkey, depChild, False)
                                                                    Catch ex As Exception
                                                                        Application.Log.AddException(ex)
                                                                    End Try
                                                                End If
                                                            Next
                                                        End If
                                                    End Using
                                                    If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                        Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                    End If
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                            Continue For
                                        Else
                                            wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
                                            Dim InstallSource = subregkey.GetValue("InstallSource", String.Empty).ToString.TrimEnd(CChar("\"))
                                            If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                            If StrContainsAny(wantedvalue, True, packages) OrElse
                                            (config.RemoveINTELIGS AndAlso StrContainsAny(wantedvalue, True, packagesigs)) OrElse
                                            (config.RemoveEnduranceGaming AndAlso StrContainsAny(wantedvalue, True, packagesEndurance)) OrElse
                                            (config.RemoveIntelNpu AndAlso Not Tools.IsIntelNpuPresent AndAlso StrContainsAny(wantedvalue, True, packagesNpu)) OrElse
                                            (config.RemoveOneAPI AndAlso StrContainsAny(wantedvalue, True, packagesoneapi)) Then
                                                Try
                                                    If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
                                                        Deletesubregkey(regkey, child)
                                                        Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                            If dependencyRegkey IsNot Nothing Then
                                                                For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                    If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                    If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                    If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                        Try
                                                                            Deletesubregkey(dependencyRegkey, depChild, False)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                            End If
                                                        End Using
                                                        If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                            Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                        End If
                                                        If ((Not String.IsNullOrWhiteSpace(InstallSource)) AndAlso Directory.Exists(InstallSource)) Then
                                                            'Delete(InstallSource)
                                                        End If
                                                    End If
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
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
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                            If subregkey IsNot Nothing Then
                                                If String.IsNullOrWhiteSpace(subregkey.GetValue("DisplayName", String.Empty).ToString) Then
                                                    If StrContainsAny(child, True, "{41a5e581-4a2c-406c-a1b5-ec680ffc64c8}", "{f8176a62-cc98-418f-a208-e187faebe116}", "{eec228c7-0de3-4e67-b631-359fb10e0bbe}") Then
                                                        Try
                                                            Deletesubregkey(regkey, child)
                                                            Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                                If dependencyRegkey IsNot Nothing Then
                                                                    For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                        If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                        If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                        If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                            Try
                                                                                Deletesubregkey(dependencyRegkey, depChild, False)
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                    Next
                                                                End If
                                                            End Using
                                                            If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                                Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                            End If
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                    Continue For
                                                Else
                                                    wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
                                                    Dim InstallSource = subregkey.GetValue("InstallSource", String.Empty).ToString.TrimEnd(CChar("\"))
                                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                                    If StrContainsAny(wantedvalue, True, packages) OrElse
                                            (config.RemoveINTELIGS AndAlso StrContainsAny(wantedvalue, True, packagesigs)) OrElse
                                            (config.RemoveEnduranceGaming AndAlso StrContainsAny(wantedvalue, True, packagesEndurance)) OrElse
                                            (config.RemoveIntelNpu AndAlso Not Tools.IsIntelNpuPresent AndAlso StrContainsAny(wantedvalue, True, packagesNpu)) OrElse
                                            (config.RemoveOneAPI AndAlso StrContainsAny(wantedvalue, True, packagesoneapi)) Then
                                                        Try
                                                            If Not (config.RemoveVulkan = False AndAlso StrContainsAny(wantedvalue, True, "vulkan")) Then
                                                                Deletesubregkey(regkey, child)
                                                                Using dependencyRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Dependencies", True)
                                                                    If dependencyRegkey IsNot Nothing Then
                                                                        For Each depChild As String In dependencyRegkey.GetSubKeyNames
                                                                            If String.IsNullOrWhiteSpace(depChild) Then Continue For
                                                                            If String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then Continue For
                                                                            If StrContainsAny(child, True, MyRegistry.OpenSubKey(dependencyRegkey, depChild, False).GetValue("", String.Empty).ToString()) Then
                                                                                Try
                                                                                    Deletesubregkey(dependencyRegkey, depChild, False)
                                                                                Catch ex As Exception
                                                                                    Application.Log.AddException(ex)
                                                                                End Try
                                                                            End If
                                                                        Next
                                                                    End If
                                                                End Using
                                                                If (Directory.Exists(config.Paths.Roaming + "Package Cache\" + child)) Then
                                                                    Delete(config.Paths.Roaming + "Package Cache\" + child)
                                                                End If
                                                                If ((Not String.IsNullOrWhiteSpace(InstallSource)) AndAlso Directory.Exists(InstallSource)) Then
                                                                    'Delete(InstallSource)
                                                                End If
                                                            End If
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
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

                If config.RemoveINTELIGS Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "Intel® Arc™ Control", "Intel® Graphics Software") OrElse
                                (config.RemoveEnduranceGaming AndAlso StrContainsAny(child, True, "Intel Endurance Gaming")) Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next
                        End If
                    End Using

                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, "Intel® Arc™ Control", "Intel® Graphics Software") Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            Next
                        End If
                    End Using
                End If

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cpls", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If child.ToLower.Contains("igfxcpl") Then
                                        Try
                                            Deletesubregkey(regkey, child)
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
                    If _isWindows8OrHigher Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If Not String.IsNullOrWhiteSpace(child) Then
                                        For i As Integer = 0 To classroot.Length - 1
                                            If Not String.IsNullOrWhiteSpace(classroot(i)) Then
                                                If child.ToLower.Contains(classroot(i).ToLower) Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
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
                                If Not String.IsNullOrWhiteSpace(child) Then
                                    If child.ToLower.Contains("igfx") Then
                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                            If regkey.SubKeyCount = 0 Then
                                Try
                                    Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify")
                                Catch ex As Exception
                                End Try
                            Else
                                For Each data As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining Key(s) found " + " : " + regkey.ToString + "\ --> " + data)
                                Next
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

                If MyRegistry.OpenSubKey(Registry.ClassesRoot, ".igp", False) IsNot Nothing Then
                    Try
                        Deletesubregkey(Registry.ClassesRoot, ".igp")
                    Catch ex As Exception
                    End Try
                End If


                FixBrokenPathIfNeeded()

                '--------------------------------
                'System environement path cleanup
                '--------------------------------
                Application.Log.AddMessage("System environement cleanUP")
                Try
                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                        If subregkey IsNot Nothing Then
                            For Each child2 As String In subregkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                If StrContainsAny(child2, True, "controlset") Then
                                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                                        If regkey IsNot Nothing Then
                                            For Each child As String In regkey.GetValueNames()
                                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                                If child.Contains("LEVEL_ZERO_V1_SDK_PATH") AndAlso config.RemoveOneAPI Then
                                                    Try
                                                        Deletevalue(regkey, child)
                                                    Catch ex As Exception
                                                        Application.Log.AddExceptionWithValues(ex, "Path: " + regkey.ToString + " Key : " + child)
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

                '-----------------------
                'remove event view stuff
                '-----------------------
                Application.Log.AddMessage("Remove eventviewer stuff")
                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                            If child2.ToLower.Contains("controlset") Then
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\eventlog\Application", True)
                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            If config.RemoveINTELIGS AndAlso child.ToLower.StartsWith("intel graphics software service") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    Continue For
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                            If child.ToLower.Equals("igcc") OrElse
child.ToLower.Equals("intel-gfx-firmware-update-service") OrElse
child.ToLower.Equals("oneapp_igcc") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
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

                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child2) Then Continue For
                            If child2.ToLower.Contains("controlset") Then
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(subregkey, child2 & "\Services\eventlog", True)
                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For
                                            If config.RemoveINTELIGS AndAlso child.ToLower.StartsWith("intel graphics software") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    Continue For
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                            If child.ToLower.Equals("oneapp_igcc") Then
                                                Try
                                                    Deletesubregkey(regkey, child)
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

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT", False)
                    If regkey IsNot Nothing Then
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "Channels", True)
                            If subregkey IsNot Nothing Then
                                For Each child As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "intel-gfx-info", "GfxFwError", "Intel-Gfx-Display-External", "GfxFwAnalytic") Then
                                        Try
                                            Deletesubregkey(subregkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End Using
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "Publishers", True)
                            If subregkey IsNot Nothing Then
                                For Each child As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "{f70d4be7-78e4-4edf-91a9-0c1341d500db}", "{47F2193C-E940-4AED-90D0-C1EB9E4C23D6}", "{31ab337f-8ba3-4145-88f3-cea537bfe861}") Then
                                        Try
                                            Deletesubregkey(subregkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End Using
                    End If
                End Using

                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\WMI\Autologger", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Intel-Gfx-Driver", "GfxFwError") Then
                                Try
                                    Deletesubregkey(regkey, child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        Next
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "EventLog-Application", True)
                            If subregkey IsNot Nothing Then
                                For Each child As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "{f70d4be7-78e4-4edf-91a9-0c1341d500db}", "{47f2193c-e940-4aed-90d0-c1eb9e4c23d6}") Then
                                        Try
                                            Deletesubregkey(subregkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End Using
                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "EventLog-System", True)
                            If subregkey IsNot Nothing Then
                                For Each child As String In subregkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "{f70d4be7-78e4-4edf-91a9-0c1341d500db}") Then
                                        Try
                                            Deletesubregkey(subregkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End If
                        End Using
                    End If
                End Using
                Application.Log.AddMessage("End Remove eventviewer stuff")
                '---------------------------
                'end remove event view stuff
                '---------------------------

                UpdateTextMethod(UpdateTextTranslated(6))
                Application.Log.AddMessage("Killing Explorer.exe")
                KillProcess("explorer")
            End Sub)
        End Sub

        Private Sub CleanIntelServiceProcess(ByVal config As ThreadSettings)
            Dim CleanupEngine As New CleanupEngine
            Dim services As String() = IO.File.ReadAllLines(Application.Paths.AppBase & "settings\INTEL\services.cfg")
            Dim servicesIGS As String() = IO.File.ReadAllLines(Application.Paths.AppBase & "settings\INTEL\servicesigs.cfg")

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                Application.Log.AddMessage("Cleaning Process/Services...")
                CleanupEngine.Cleanserviceprocess(services, config) '// add each line as String Array.

                If config.RemoveINTELIGS Then
                    CleanupEngine.Cleanserviceprocess(servicesIGS, config)
                End If
                KillProcess("IGFXEM")
                Application.Log.AddMessage("Process/Services CleanUP Complete")
            End Sub)
        End Sub

        Private Sub CleanIntelCache(config As ThreadSettings)
            For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                If String.IsNullOrWhiteSpace(filepaths) Then Continue For

                Dim filePath As String = filepaths + "\AppData\LocalLow\Intel"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "shadercache") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = config.Paths.System32 + "config\systemprofile\AppData\Local\D3DSCache"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If

                filePath = filepaths + "\AppData\Local\D3DSCache"
                If _fileIo.ExistsDir(filePath) Then
                    Try
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                    End Try
                End If
            Next
        End Sub

        Private Sub CleanIntelFolders(ByVal config As ThreadSettings)
            Dim CleanupEngine As New CleanupEngine
            Dim driverFiles As String() = IO.File.ReadAllLines(Application.Paths.AppBase & "settings\INTEL\driverfiles.cfg")
            Dim sharedDriverFiles As String() = IO.File.ReadAllLines(Application.Paths.AppBase & "settings\INTEL\shareddriverfiles.cfg")
            UpdateTextMethod(UpdateTextTranslated(4))

            Application.Log.AddMessage("Cleaning Directory")

            CleanupEngine.Folderscleanup(driverFiles)      '// add each line as String Array.

            If Not FrmMain.IntelNpuPresent Then
                CleanupEngine.Folderscleanup(sharedDriverFiles)
            End If

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                CleanIntelCache(config)

                Dim filePath As String = System.Environment.SystemDirectory
                Dim files() As String = IO.Directory.GetFiles(filePath + "\", "igfxcoin*.*")
                For i As Integer = 0 To files.Length - 1
                    If Not String.IsNullOrWhiteSpace(files(i)) Then
                        Try
                            Delete(files(i))
                        Catch ex As Exception
                        End Try
                    End If
                Next

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + "\Intel"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "Media SDK", "Media Resource", "ACMirageCache", "Intel(R) Arc Software & Drivers", "PrebuiltShaderBinaries", "Intel(R) Graphics Software & Drivers") OrElse
(config.RemoveINTELIGS AndAlso StrContainsAny(child, True, "Intel Graphics Software", "Intel Arc Control")) OrElse
(config.RemoveEnduranceGaming AndAlso StrContainsAny(child, True, "EnduranceGaming")) OrElse
StrContainsAny(child, True, "intel(r) NPU software & drivers") AndAlso (config.RemoveIntelNpu AndAlso Not Tools.IsIntelNpuPresent) Then
                                Delete(child)
                            End If
                        End If
                    Next
                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                If config.RemoveOneAPI Then
                    filePath = config.Paths.ProgramFiles + "LevelZeroSDK"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If
                End If

                filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Intel"
                If _fileIo.ExistsDir(filePath) Then
                    For Each child As String In _fileIo.GetDirectories(filePath)
                        If String.IsNullOrWhiteSpace(child) = False Then
                            If StrContainsAny(child, True, "shadercache", "ags", "gfxinstaller", "IGN", "FWUpdateService", "IGSDS") Or
StrContainsAny(child, True, "gcc") AndAlso config.RemoveINTELCP Or
StrContainsAny(child, True, "npuinstaller") AndAlso (config.RemoveIntelNpu AndAlso Not Tools.IsIntelNpuPresent) Then
                                Delete(child)
                            End If
                        End If
                    Next
                    If _fileIo.CountDirectories(filePath) = 0 Then
                        Delete(filePath)
                    Else
                        For Each data As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(data) Then Continue For
                            Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                        Next
                    End If
                End If

                If IntPtr.Size = 8 Then
                    filePath = Application.Paths.ProgramFilesx86 + "Intel"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "Media SDK", "Media Resource", "Intel(R) Processor Graphics", "Intel(R) Graphics Software & Drivers") Then
                                    Delete(child)
                                End If
                            End If
                        Next
                        If _fileIo.CountDirectories(filePath) = 0 Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    End If
                End If

                If config.RemoveINTELIGS Then
                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs"
                    Try
                        For Each child As String In _fileIo.GetFiles(filePath)
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Intel Arc Control") Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Intel"
                    If _fileIo.ExistsDir(filePath) Then
                        For Each child As String In _fileIo.GetDirectories(filePath)
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Intel Arc Control", "Intel Graphics Software") Then
                                Delete(child)
                            End If
                        Next
                        If _fileIo.CountDirectories(filePath) = 0 Then
                            Delete(filePath)
                        Else
                            For Each data As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(data) Then Continue For
                                Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                            Next
                        End If
                    End If

                    filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Startup"
                    Try
                        For Each child As String In _fileIo.GetFiles(filePath)
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Intel Arc Control") Then
                                Delete(child)
                            End If
                        Next
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If

                For Each filepaths As String In _fileIo.GetDirectories(config.Paths.UsersPath)
                    If String.IsNullOrWhiteSpace(filepaths) Then Continue For
                    For Each child As String In _fileIo.GetDirectories(filepaths)
                        If String.IsNullOrWhiteSpace(child) Then Continue For
                        If StrContainsAny(child, True, "intelgraphicsprofiles") Then
                            Delete(child)
                        End If
                    Next

                    filePath = filepaths + "\AppData\LocalLow\Intel"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\Intel"  'need check in the future.
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If StrContainsAny(child, True, "shadercache") Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\Intel"
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    If config.RemoveINTELCP AndAlso StrContainsAny(child, True, "gcc", "games", "cuipromotions", "ags", "ign") OrElse
(config.RemoveINTELIGS AndAlso StrContainsAny(child, True, "intelgraphicssoftware")) OrElse
StrContainsAny(child, True, "NPU") AndAlso (config.RemoveIntelNpu AndAlso Not Tools.IsIntelNpuPresent) Then
                                        Delete(child)
                                    End If
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If

                    filePath = filepaths + "\AppData\Local\D3DSCache"
                    If _winxp Then
                        filePath = filepaths + "\Local Settings\Application Data\D3DSCache"
                    End If
                    If _fileIo.ExistsDir(filePath) Then
                        Try
                            For Each child As String In _fileIo.GetDirectories(filePath)
                                If String.IsNullOrWhiteSpace(child) = False Then
                                    Delete(child)
                                End If
                            Next
                            If _fileIo.CountDirectories(filePath) = 0 Then
                                Delete(filePath)
                            Else
                                For Each data As String In _fileIo.GetDirectories(filePath)
                                    If String.IsNullOrWhiteSpace(data) Then Continue For
                                    Application.Log.AddWarningMessage("Remaining folders found " + " : " + filePath + "\ --> " + data)
                                Next
                            End If
                        Catch ex As Exception
                            Application.Log.AddMessage("Possible permission issue detected on : " + filePath)
                        End Try
                    End If
                Next

            End Sub)
        End Sub

        Private Sub CleanVulkan(ByVal config As ThreadSettings)
            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Khronos\OpenCL\Vendors", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If String.IsNullOrWhiteSpace(child) Then Continue For
                        If StrContainsAny(child, True, "amdocl") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                            Try
                                Deletevalue(regkey, child)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If StrContainsAny(child, True, "nvopencl") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                            Try
                                Deletevalue(regkey, child)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If StrContainsAny(child, True, "intelopencl") AndAlso config.SelectedGPU = GPUVendor.Intel Then
                            Try
                                Deletevalue(regkey, child)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    Next
                    If regkey.GetValueNames().Length = 0 Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "Software\Khronos\OpenCL")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If
            End Using

            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Khronos\vulkan\Drivers", True)
                If regkey2 IsNot Nothing Then
                    For Each child As String In regkey2.GetValueNames
                        If String.IsNullOrWhiteSpace(child) Then Continue For
                        If StrContainsAny(child, True, "amd-vulkan64") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                            Try
                                Deletevalue(regkey2, child)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                        If StrContainsAny(child, True, "nv-vk64") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                            Try
                                Deletevalue(regkey2, child)
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    Next
                    If regkey2.GetValueNames().Length = 0 Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "Software\Khronos\vulkan\Drivers")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If
            End Using

            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Khronos", True)
                If subregkey IsNot Nothing Then
                    If subregkey.GetSubKeyNames().Length = 0 Then
                        Try
                            Deletesubregkey(Registry.LocalMachine, "Software\Khronos")
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End If
            End Using

            For Each users As String In Registry.Users.GetSubKeyNames()
                If String.IsNullOrWhiteSpace(users) Then Continue For
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software", True)
                    If regkey IsNot Nothing Then
                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos\OpenCL\Vendors", True)
                            If regkey2 IsNot Nothing Then
                                For Each child As String In regkey2.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "amdocl") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                        Try
                                            Deletevalue(regkey2, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "nvopencl") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                        Try
                                            Deletevalue(regkey2, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "intelopencl") AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                        Try
                                            Deletevalue(regkey2, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                                If regkey2.GetValueNames().Length = 0 Then
                                    Try
                                        Deletesubregkey(regkey, "Khronos\OpenCL")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        End Using

                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos\vulkan\Drivers", True)
                            If regkey2 IsNot Nothing Then
                                For Each child As String In regkey2.GetValueNames
                                    If String.IsNullOrWhiteSpace(child) Then Continue For
                                    If StrContainsAny(child, True, "amd-vulkan64") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                        Try
                                            Deletevalue(regkey2, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                    If StrContainsAny(child, True, "nv-vk64") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                        Try
                                            Deletevalue(regkey2, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                                If regkey2.GetValueNames().Length = 0 Then
                                    Try
                                        Deletesubregkey(regkey, "Khronos\vulkan\Drivers")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        End Using

                        Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos", True)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetSubKeyNames().Length = 0 Then
                                    Try
                                        Deletesubregkey(regkey, "Khronos")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        End Using
                    End If
                End Using
            Next

            If config.WinIs64 Then
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\WOW6432Node\Khronos\OpenCL\Vendors", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetValueNames()
                            If String.IsNullOrWhiteSpace(child) = False Then
                                If StrContainsAny(child, True, "amdocl") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                                If StrContainsAny(child, True, "nvopencl") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                                If StrContainsAny(child, True, "intelopencl") AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                    Try
                                        Deletevalue(regkey, child)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End If
                        Next
                        If regkey.GetValueNames().Length = 0 Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "Software\WOW6432Node\Khronos\OpenCL")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\WOW6432Node\Khronos\vulkan\Drivers", True)
                    If regkey2 IsNot Nothing Then
                        For Each child As String In regkey2.GetValueNames
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "amd-vulkan32") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                Try
                                    Deletevalue(regkey2, child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                            If StrContainsAny(child, True, "nv-vk") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                Try
                                    Deletevalue(regkey2, child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        Next
                        If regkey2.GetValueNames().Length = 0 Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "Software\WOW6432Node\Khronos\vulkan\Drivers")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Khronos", True)
                    If subregkey IsNot Nothing Then
                        If subregkey.GetSubKeyNames().Length = 0 Then
                            Try
                                Deletesubregkey(Registry.LocalMachine, "Software\WOW6432Node\Khronos")
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End If
                End Using

                For Each users As String In Registry.Users.GetSubKeyNames()
                    If String.IsNullOrWhiteSpace(users) Then Continue For
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Wow6432Node", True)
                        If regkey IsNot Nothing Then
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos\OpenCL\Vendors", True)
                                If regkey2 IsNot Nothing Then
                                    For Each child As String In regkey2.GetValueNames()
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        If StrContainsAny(child, True, "amdocl") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                            Try
                                                Deletevalue(regkey2, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                        If StrContainsAny(child, True, "nvopencl") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                            Try
                                                Deletevalue(regkey2, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                        If StrContainsAny(child, True, "intelopencl") AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                            Try
                                                Deletevalue(regkey2, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    Next
                                    If regkey2.GetValueNames().Length = 0 Then
                                        Try
                                            Deletesubregkey(regkey, "Khronos\OpenCL")
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            End Using
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos\vulkan\Drivers", True)
                                If regkey2 IsNot Nothing Then
                                    For Each child As String In regkey2.GetValueNames
                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                        If StrContainsAny(child, True, "amd-vulkan32") AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                            Try
                                                Deletevalue(regkey2, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                        If StrContainsAny(child, True, "nv-vk") AndAlso config.SelectedGPU = GPUVendor.Nvidia Then
                                            Try
                                                Deletevalue(regkey2, child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    Next
                                    If regkey2.GetValueNames().Length = 0 Then
                                        Try
                                            Deletesubregkey(regkey, "Khronos\vulkan\Drivers")
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            End Using
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, "Khronos", True)
                                If subregkey IsNot Nothing Then
                                    If subregkey.GetSubKeyNames().Length = 0 Then
                                        Try
                                            Deletesubregkey(regkey, "Khronos")
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            End Using
                        End If
                    End Using
                Next
            End If

            Dim filePath As String = System.Environment.SystemDirectory
            Dim files As String() = IO.Directory.GetFiles(filePath + "\", "vulkan-1*.dll")
            For i As Integer = 0 To files.Length - 1
                If Not String.IsNullOrWhiteSpace(files(i)) Then
                    Try
                        Delete(files(i))
                    Catch ex As Exception
                    End Try
                End If
            Next

            files = IO.Directory.GetFiles(filePath + "\", "vulkaninfo*.*")
            For i As Integer = 0 To files.Length - 1
                If Not String.IsNullOrWhiteSpace(files(i)) Then
                    Try
                        Delete(files(i))
                    Catch ex As Exception
                    End Try
                End If
            Next

            If IntPtr.Size = 8 Then
                filePath = Environment.GetEnvironmentVariable("windir") + "\SysWOW64"
                files = IO.Directory.GetFiles(filePath + "\", "vulkan-1*.dll")
                For i As Integer = 0 To files.Length - 1
                    If Not String.IsNullOrWhiteSpace(files(i)) Then
                        Try
                            Delete(files(i))
                        Catch ex As Exception
                        End Try
                    End If
                Next
                files = IO.Directory.GetFiles(filePath + "\", "vulkaninfo*.*")
                For i As Integer = 0 To files.Length - 1
                    If Not String.IsNullOrWhiteSpace(files(i)) Then
                        Try
                            Delete(files(i))
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If

            If config.RemoveVulkan Then
                filePath = config.Paths.ProgramFiles + "VulkanRT"
                If _fileIo.ExistsDir(filePath) Then
                    Delete(filePath)
                End If
                If IntPtr.Size = 8 Then
                    filePath = Application.Paths.ProgramFilesx86 + "VulkanRT"
                    If _fileIo.ExistsDir(filePath) Then
                        Delete(filePath)
                    End If
                End If
            End If
        End Sub

        Private Sub AmdEnvironementPath(ByVal filepath As String)
            Dim valuesToFind() As String = New String() {
filepath & "\amd app\bin\x86_64",
filepath & "\amd app\bin\x86",
filepath & "\ati.ace\core-static"
}
            CleanEnvironementPath(valuesToFind)
        End Sub

        Private Sub IntelCleanEnvironementPath(ByVal filepath As String)
            Dim valuesToFind() As String = New String() {
filepath & "\amd app\bin\x86_64",
filepath & "\amd app\bin\x86",
filepath & "\ati.ace\core-static"
}
            CleanEnvironementPath(valuesToFind)
        End Sub

        Private Sub UpdateTextMethod(ByVal strMessage As String)
            FrmMain.UpdateTextMethod(strMessage)
        End Sub

        Private Function UpdateTextTranslated(ByVal number As Integer) As String
            Return FrmMain.UpdateTextTranslated(number)
        End Function

        Private Sub Delete(ByVal filename As String)
            Dim CleanupEngine As New CleanupEngine
            If _fileIo.ExistsFile(filename) OrElse _fileIo.ExistsDir(filename) Then
                _fileIo.Delete(filename)
            End If
            CleanupEngine.RemoveSharedDlls(filename)
        End Sub

        Private Sub Threaddata1(ByVal driverfiles As String())
            Dim CleanupEngine As New CleanupEngine

            CleanupEngine.Folderscleanup(driverfiles)

        End Sub

        Private Sub Deletesubregkey(ByVal value1 As RegistryKey, ByVal value2 As String, Optional ByVal throwOnMissingSubKey As Boolean = True)
            Dim CleanupEngine As New CleanupEngine
            CleanupEngine.Deletesubregkey(value1, value2, throwOnMissingSubKey)
        End Sub

        Private Sub Deletevalue(ByVal value1 As RegistryKey, ByVal value2 As String, Optional ByVal throwOnMissingSubKey As Boolean = True)
            Dim CleanupEngine As New CleanupEngine
            CleanupEngine.Deletevalue(value1, value2, throwOnMissingSubKey)
        End Sub

        Private Sub CLSIDCleanThread(ByVal Clsidleftover As String())
            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    Dim engine As New CleanupEngine
                    engine.Clsidleftover(Clsidleftover)
                End Sub)
        End Sub

        Private Sub InstallerCleanThread(ByVal Packages As String(), config As ThreadSettings)
            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    Dim engine As New CleanupEngine
                    engine.Installer(Packages, config)
                End Sub)
        End Sub

        Private Sub ClassrootCleanThread(ByRef ThreadFinised As Boolean, ByVal Classroot As String(), config As ThreadSettings)
            ThreadFinised = False
            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    Dim engine As New CleanupEngine
                    engine.ClassRoot(Classroot, config)
                End Sub)
            ThreadFinised = True
        End Sub

    End Class
End Namespace

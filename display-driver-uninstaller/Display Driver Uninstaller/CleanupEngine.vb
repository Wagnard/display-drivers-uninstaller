Imports System.IO
Imports System.Linq
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Threading
Imports System.Threading.Tasks
Imports Display_Driver_Uninstaller.Win32
Imports Microsoft.Win32
Imports Windows.Management.Deployment

Namespace Display_Driver_Uninstaller

    Public Class CleanupEngine
        Private Shared ReadOnly _listLock As Object = New Object()
        Private Shared ReadOnly _registryLock As Object = New Object()

        '	Private win8higher As Boolean = frmMain.win8higher

        Private Function UpdateTextMethodmessagefn(ByRef number As Integer) As String
            Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
        End Function


        Public Sub Deletesubregkey(ByRef regkeypath As RegistryKey, ByVal child As String, Optional ByVal throwOnMissingSubKey As Boolean = True)
            SyncLock _registryLock
                Dim fixregacls As Boolean = False
                If (regkeypath IsNot Nothing) AndAlso (Not String.IsNullOrWhiteSpace(child)) Then
                    Try
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(regkeypath, child, True)
                            If regkey Is Nothing AndAlso Not throwOnMissingSubKey Then
                                Return
                            End If
                            'we do this simply to ensure that the permissions are set to open this registrykey.
                            'or else we will get an argument exception when trying to remove the key if permission are wrong.
                            If regkey IsNot Nothing Then
                                For Each childs As String In regkey.GetSubKeyNames
                                    If String.IsNullOrWhiteSpace(childs) Then Continue For
                                    Deletesubregkey(regkey, childs, throwOnMissingSubKey)
                                Next
                            End If
                        End Using
                        regkeypath.DeleteSubKeyTree(child, throwOnMissingSubKey)

                        Application.Log.AddMessage(regkeypath.ToString & "\" & child & " - " & UpdateTextMethodmessagefn(39))

                        Select Case True
                            Case StrContainsAny(regkeypath.Name, True, "HKEY_LOCAL_MACHINE")
                                Dim path As String = StrReplace(regkeypath.Name, "HKEY_LOCAL_MACHINE", "").ToString()
                                Using PnpRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM" + path + "\" + child)
                                    If PnpRegkey IsNot Nothing Then
                                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM" + path + "\" + child, throwOnMissingSubKey)
                                    End If
                                End Using

                            Case StrContainsAny(regkeypath.Name, True, "HKEY_CLASSES_ROOT")
                                Dim path As String = StrReplace(regkeypath.Name, "HKEY_CLASSES_ROOT", "").ToString()

                                Using PnpRegkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR" + path + "\" + child)
                                    If PnpRegkey IsNot Nothing Then
                                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR" + path + "\" + child, throwOnMissingSubKey)
                                    End If
                                End Using

                        End Select
                    Catch ex As UnauthorizedAccessException
                        Application.Log.AddWarningMessage("Failed to remove registry subkey " + child + " Will try to set ACLs permission and try again.")
                        fixregacls = True
                    End Try
                    'If exists, it means we need to modify it's ACls.
                    If fixregacls AndAlso (regkeypath IsNot Nothing) Then
                        ACL.Addregistrysecurity(regkeypath, child, RegistryRights.FullControl, AccessControlType.Allow)
                        Try
                            regkeypath.DeleteSubKeyTree(child)
                        Catch ex As Exception
                            Application.Log.AddWarning(ex, " Failed or already removed with another Thread ? " & child)
                        End Try
                        Application.Log.AddMessage(child + " - " + UpdateTextMethodmessagefn(39))
                    End If
                End If
            End SyncLock
        End Sub



        Public Sub RemoveSharedDlls(ByVal directorypath As String)
            Dim FileIO As New FileIO
            If Not String.IsNullOrWhiteSpace(directorypath) AndAlso Not FileIO.ExistsDir(directorypath) Then
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
                    If regkey IsNot Nothing AndAlso regkey.GetValue(If(Not directorypath.EndsWith("\"), directorypath & "\", directorypath)) IsNot Nothing Then
                        Try
                            Deletevalue(regkey, If(Not directorypath.EndsWith("\"), directorypath & "\", directorypath))
                        Catch exARG As ArgumentException
                            'nothing to do,it probably doesn't exit.
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                End Using

                If Not directorypath.EndsWith("\") Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                        If regkey IsNot Nothing AndAlso regkey.GetValue(directorypath) IsNot Nothing Then
                            Try
                                Deletevalue(regkey, directorypath)
                            Catch exARG As ArgumentException
                                'nothing to do,it probably doesn't exit.
                            Catch ex As Exception
                                Application.Log.AddException(ex)
                            End Try
                        End If
                    End Using

                    If IntPtr.Size = 8 Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                            If regkey IsNot Nothing AndAlso regkey.GetValue(directorypath) IsNot Nothing Then
                                Try
                                    Deletevalue(regkey, directorypath)
                                Catch exARG As ArgumentException
                                    'nothing to do,it probably doesn't exit.
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        End Using
                    End If
                End If
            End If

        End Sub

        Private Async Function RemoveAppx1809Async(ByVal AppxToRemove As String) As Task
            Dim ServiceInstaller As New ServiceInstaller
            Dim win10 As Boolean = FrmMain.IsWindows10
            Dim win10_1809 As Boolean = FrmMain.IsWindows10_1809

            If win10 Then
                'We must not be impersonated here !!
                Try
                    Select Case System.Windows.Forms.SystemInformation.BootMode
                        Case System.Windows.Forms.BootMode.FailSafe
                            Try
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
                                    If regkey IsNot Nothing Then
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                    End If
                                End Using
                            Catch ex As Exception
                                Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
                            End Try
                        Case System.Windows.Forms.BootMode.FailSafeWithNetwork
                            Try
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
                                    If regkey IsNot Nothing Then
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                    End If
                                End Using
                            Catch ex As Exception
                                Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
                            End Try
                    End Select


                    'Dim opCompletedEvent As ManualResetEvent = New ManualResetEvent(False)
                    '			End Function
                    'Windows.Foundation.IAsyncAction() = packageManager.RemovePackageAsync("NVIDIACorp.NVIDIAControlPanel_8.1.949.0_x64__56jybvy8sckqj")

                    Dim packageManager As New PackageManager()

                    For Each package In packageManager.FindPackages()
                        Dim WasRemoved As Boolean = False

                        If package Is Nothing OrElse String.IsNullOrWhiteSpace(package.Id.FullName) Then Continue For
                        If Not StrContainsAny(package.Id.FullName, True, AppxToRemove) Then Continue For
                        Dim packageIdFamilyName = package.Id.FamilyName

                        Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(10))
                            Try
                                If win10_1809 AndAlso CanDeprovisionPackageForAllUsers() Then

                                    Await packageManager.DeprovisionPackageForAllUsersAsync(package.Id.FamilyName).AsTask(cts.Token)
                                    Application.Log.AddMessage($"{package.Id.FullName} package deprovisioned.")
                                End If
                            Catch ex As OperationCanceledException
                                Application.Log.AddMessage($"{package.Id.FullName} deprovision timed out.")
                            Catch ex As Exception
                                Application.Log.AddMessage($"{package.Id.FullName} deprovisioned failed: {ex.Message}")
                            End Try
                        End Using

                        Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(10))
                            Try
                                Await packageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.RemoveForAllUsers).AsTask(cts.Token)
                                Application.Log.AddMessage($"{package.Id.FullName} package removed.")
                                WasRemoved = True
                            Catch ex As OperationCanceledException
                                Application.Log.AddMessage($"{package.Id.FullName} package removal timed out.")
                            Catch ex As Exception
                                Application.Log.AddMessage($"{package.Id.FullName} package removal failed: {ex.Message}")
                            End Try
                        End Using

                        If WasRemoved Then
                            RemoveInstalledPfns(packageIdFamilyName)
                        End If
                    Next

                Catch ex As Exception
                    Application.Log.AddException(ex)
                Finally
                    Select Case System.Windows.Forms.SystemInformation.BootMode
                        Case System.Windows.Forms.BootMode.FailSafe
                            ServiceInstaller.StopService("AppXSvc")
                            ServiceInstaller.StopService("camsvc")
                            ServiceInstaller.StopService("clipSVC")
                            ServiceInstaller.StopService("Wsearch")
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "AppXSvc")
                                        Deletesubregkey(regkey, "camsvc")
                                        Deletesubregkey(regkey, "clipSVC")
                                        Deletesubregkey(regkey, "Wsearch")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
                                    End Try
                                End If
                            End Using

                        Case System.Windows.Forms.BootMode.FailSafeWithNetwork
                            ServiceInstaller.StopService("AppXSvc")
                            ServiceInstaller.StopService("camsvc")
                            ServiceInstaller.StopService("clipSVC")
                            ServiceInstaller.StopService("Wsearch")
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "AppXSvc")
                                        Deletesubregkey(regkey, "camsvc")
                                        Deletesubregkey(regkey, "clipSVC")
                                        Deletesubregkey(regkey, "Wsearch")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
                                    End Try
                                End If
                            End Using
                    End Select
                End Try
            End If
        End Function

        Private Sub RemoveInstalledPfns(packageIdFamilyName As String)
            ImpersonateUser.RunImpersonatedSystem(
                                        Sub()

                                            'Win 10 (1809)
                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletevalue(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\bam\State\UserSettings", True)
                                                If regkey IsNot Nothing Then
                                                    For Each child As String In regkey.GetSubKeyNames
                                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                                            Try
                                                                Deletevalue(regkey2, packageIdFamilyName, False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End Using
                                                    Next
                                                End If
                                            End Using

                                            'Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StateRepository\Cache\PackageFamily", True)
                                            '	If regkey IsNot Nothing Then
                                            '		Using subregkey As RegistryKey = regkey.OpenSubKey("Index\PackageFamilyName\" + packageIdFamilyName)
                                            '			If subregkey IsNot Nothing Then
                                            '				For Each child As String In subregkey.GetSubKeyNames
                                            '					If IsNullOrWhitespace(child) Then Continue For
                                            '					Try
                                            '						Deletesubregkey(regkey.OpenSubKey("data", True), child)
                                            '					Catch ex As ArgumentException

                                            '					Catch ex As Exception
                                            '						Application.Log.AddException(ex)
                                            '					End Try
                                            '				Next
                                            '				Try
                                            '					Deletesubregkey(regkey.OpenSubKey("Index\PackageFamilyName", True), packageIdFamilyName)
                                            '				Catch ex As Exception
                                            '					Application.Log.AddException(ex)
                                            '				End Try
                                            '			End If
                                            '		End Using
                                            '	End If
                                            'End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PolicyCache", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.CurrentUser, "Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PolicyCache", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.CurrentUser, "Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.CurrentUser, "Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", True)
                                                If regkey IsNot Nothing Then
                                                    For Each child As String In regkey.GetValueNames
                                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                                        If StrContainsAny(child, True, packageIdFamilyName) Then
                                                            Try
                                                                Deletevalue(regkey, child, False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    Next
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.CurrentUser, "Software\Microsoft\Windows NT\CurrentVersion\HostActivityManager\CommitHistory", True)
                                                If regkey IsNot Nothing Then
                                                    For Each child As String In regkey.GetSubKeyNames
                                                        If String.IsNullOrWhiteSpace(child) Then Continue For
                                                        If StrContainsAny(child, True, packageIdFamilyName) Then
                                                            Try
                                                                Deletesubregkey(regkey, child, False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    Next
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, ".DEFAULT\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, "S-1-5-18\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, "S-1-5-19\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, "S-1-5-20\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", True)
                                                If regkey IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey, packageIdFamilyName, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            'Win 10 (1803)
                                            For Each regkeyusers As String In Registry.Users.GetSubKeyNames
                                                If String.IsNullOrWhiteSpace(regkeyusers) Then Continue For
                                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regkeyusers & "\Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
                                                    If regkey IsNot Nothing Then
                                                        For Each ValueName As String In regkey.GetValueNames
                                                            If String.IsNullOrWhiteSpace(ValueName) Then Continue For
                                                            If StrContainsAny(ValueName, True, packageIdFamilyName) Then  'Not working need fixing
                                                                Try
                                                                    Deletevalue(regkey, ValueName)
                                                                Catch ex As Exception
                                                                    Application.Log.AddException(ex)
                                                                End Try
                                                            End If
                                                        Next
                                                    End If
                                                End Using
                                            Next

                                            'Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StateRepository\Cache\ApplicationUser\Index\UserAndApplicationUserModelId", True)
                                            '	If regkey IsNot Nothing Then
                                            '		For Each child As String In regkey.GetSubKeyNames
                                            '			If IsNullOrWhitespace(child) Then Continue For
                                            '			If StrContainsAny(child, True, packageIdFamilyName) Then
                                            '				Try
                                            '					Deletesubregkey(regkey, child)
                                            '				Catch ex As Exception
                                            '					Application.Log.AddException(ex)
                                            '				End Try
                                            '			End If
                                            '		Next
                                            '	End If
                                            'End Using

                                            'Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StateRepository\Cache\ApplicationUser", True)
                                            '	If regkey IsNot Nothing Then
                                            '		Using subregkey As RegistryKey = regkey.OpenSubKey("Index\UserAndApplicationUserModelId", True)
                                            '			If subregkey IsNot Nothing Then
                                            '				For Each child As String In subregkey.GetSubKeyNames
                                            '					If IsNullOrWhitespace(child) Then Continue For
                                            '					If StrContainsAny(child, True, packageIdFamilyName) Then
                                            '						Using subregkey2 As RegistryKey = subregkey.OpenSubKey(child, True)
                                            '							If subregkey2 IsNot Nothing Then
                                            '								For Each child2 As String In subregkey2.GetSubKeyNames
                                            '									If IsNullOrWhitespace(child2) Then Continue For
                                            '									Try
                                            '										Deletesubregkey(regkey.OpenSubKey("Data", True), child2)
                                            '									Catch argEx As ArgumentException

                                            '									Catch ex As Exception
                                            '										Application.Log.AddException(ex)
                                            '									End Try
                                            '								Next
                                            '								Try
                                            '									Deletesubregkey(subregkey, child)
                                            '								Catch ex As Exception
                                            '									Application.Log.AddException(ex)
                                            '								End Try
                                            '							End If
                                            '						End Using
                                            '					End If
                                            '				Next
                                            '			End If
                                            '		End Using
                                            '	End If
                                            'End Using

                                        End Sub)
        End Sub

        Public Async Function RemoveAppxAsync(ByVal appxToRemove As String) As Task
            If CanDeprovisionPackageForAllUsers() Then
                Await RemoveAppx1809Async(appxToRemove)
            Else
                Await RemoveAppxPre1809Async(appxToRemove)
            End If

            ' If the package was already uninstalled outside DDU (eg. through Windows),
            ' FindPackages() never returns it, so the removal loops above never clean
            ' its 'InstalledPfns' registration (eg. NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj).
            ' Sweep those leftovers here, independently of the package enumeration.
            RemoveStaleInstalledPfns(appxToRemove)
        End Function

        ''' <summary>
        ''' Removes leftover 'InstalledPfns' registrations (and related keys, via RemoveInstalledPfns)
        ''' matching the given Appx name, even when the package itself is no longer installed.
        ''' </summary>
        Private Sub RemoveStaleInstalledPfns(ByVal appxToRemove As String)
            If Not FrmMain.IsWindows10 Then Return

            Try
                Dim staleFamilyNames As New List(Of String)

                'Win 10 (1809)
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", False)
                    If regkey IsNot Nothing Then
                        For Each valueName As String In regkey.GetValueNames()
                            If String.IsNullOrWhiteSpace(valueName) Then Continue For
                            If StrContainsAny(valueName, True, appxToRemove) AndAlso Not staleFamilyNames.Contains(valueName) Then
                                staleFamilyNames.Add(valueName)
                            End If
                        Next
                    End If
                End Using

                'Win 10 (1803)
                For Each userKeyName As String In Registry.Users.GetSubKeyNames()
                    If String.IsNullOrWhiteSpace(userKeyName) Then Continue For
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, userKeyName & "\Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", False)
                        If regkey Is Nothing Then Continue For
                        For Each valueName As String In regkey.GetValueNames()
                            If String.IsNullOrWhiteSpace(valueName) Then Continue For
                            If StrContainsAny(valueName, True, appxToRemove) AndAlso Not staleFamilyNames.Contains(valueName) Then
                                staleFamilyNames.Add(valueName)
                            End If
                        Next
                    End Using
                Next

                For Each familyName As String In staleFamilyNames
                    Application.Log.AddMessage($"Removing leftover InstalledPfns registration: {familyName}")
                    RemoveInstalledPfns(familyName)
                Next
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        ''' <summary>
        ''' Deletes the MMDevices audio endpoints (Render + Capture) whose underlying device
        ''' belongs to the given audio codec vendor id(s) (eg. "ven_10de" NVIDIA, "ven_1002" AMD,
        ''' "ven_10ec" Realtek). Windows (AudioEndpointBuilder) rebuilds endpoints from the device's
        ''' KS filter when it comes back, so removing them guarantees a clean endpoint state on
        ''' driver reinstall and gets rid of ghost endpoints carrying stale settings/FxProperties.
        ''' Endpoints of other vendors' devices are never touched.
        ''' </summary>
        Public Sub RemoveVendorAudioEndpoints(ByVal ParamArray vendorCodecIds As String())
            If vendorCodecIds Is Nothing OrElse vendorCodecIds.Length = 0 Then Return

            ' Intel requires a device-anchored pattern ("ven_8086&dev_28"): the Intel DSP also
            ' hosts Bluetooth-offload and DMIC endpoints whose codec ids are VEN_8086 with
            ' DEV_AExx (eg. VEN_8086&DEV_AE30 = the user's Bluetooth headset, verified on
            ' Panther Lake), while Intel display-audio codecs are always DEV_28xx. A bare
            ' "ven_8086" pattern would wipe the user's Bluetooth/mic endpoints - refuse it.
            Dim codecIds As New List(Of String)
            For Each id As String In vendorCodecIds
                If String.IsNullOrWhiteSpace(id) Then Continue For
                If StrContainsAny(id, True, "8086") AndAlso Not StrContainsAny(id, True, "dev_28") Then
                    Application.Log.AddWarningMessage($"MMDevices endpoint cleanup: unqualified Intel pattern '{id}' refused (would match Bluetooth/DMIC endpoints), use 'ven_8086&dev_28'.")
                    Continue For
                End If
                codecIds.Add(id)
            Next

            If codecIds.Count = 0 Then Return

            Dim serviceInstaller As New ServiceInstaller
            Dim removedCount As Integer = 0

            Try
                ' Audiosrv depends on AudioEndpointBuilder: stop the dependent first so the
                ' services don't rewrite endpoint state from their cache while we delete.
                serviceInstaller.StopService("Audiosrv")
                serviceInstaller.StopService("AudioEndpointBuilder")

                ImpersonateUser.RunImpersonatedSystem(
                    Sub()
                        ' MMDevices\Audio ACLs are legitimately restrictive: only the audio services
                        ' (Audiosrv/AudioEndpointBuilder) and TrustedInstaller have write access - not
                        ' even SYSTEM. FixRights adds a SYSTEM ACE on Render/Capture so we can work,
                        ' so snapshot their DACLs first and restore them afterwards to preserve the
                        ' original security. This restore is specific to this area: on most other keys
                        ' SYSTEM access is expected by default, and FixRights' ACE is meant to stay.
                        Dim baseAudioPath As String = "SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio"
                        Dim flows As String() = New String() {"Render", "Capture"}
                        Dim originalDacls As New Dictionary(Of String, Byte())

                        For Each flow As String In flows
                            originalDacls(flow) = SnapshotKeyDacl($"{baseAudioPath}\{flow}")
                        Next

                        Try
                            For Each flow As String In flows
                                Using flowKey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, $"{baseAudioPath}\{flow}", True)
                                    If flowKey Is Nothing Then Continue For

                                    For Each endpointGuid As String In flowKey.GetSubKeyNames()
                                        If String.IsNullOrWhiteSpace(endpointGuid) Then Continue For
                                        If Not EndpointBelongsToVendor(flowKey, endpointGuid, codecIds) Then Continue For

                                        Try
                                            Application.Log.AddMessage($"Removing audio endpoint '{flow}\{endpointGuid}' (vendor match: {String.Join(",", codecIds)}).")
                                            Deletesubregkey(flowKey, endpointGuid, False)
                                            removedCount += 1
                                        Catch ex As Exception
                                            Application.Log.AddException(ex, $"Failed to remove audio endpoint '{flow}\{endpointGuid}'")
                                        End Try
                                    Next
                                End Using
                            Next
                        Finally
                            ' Runs while still impersonating SYSTEM (key owner), even if a delete failed.
                            For Each flow As String In flows
                                RestoreKeyDacl($"{baseAudioPath}\{flow}", originalDacls(flow))
                            Next
                        End Try
                    End Sub)

                If removedCount > 0 Then
                    Application.Log.AddMessage($"MMDevices cleanup: {removedCount} audio endpoint(s) removed. Windows will rebuild them on the next device arrival.")
                End If
            Catch ex As Exception
                Application.Log.AddException(ex)
            Finally
                ' Restart in dependency order. If the services were already stopped
                ' (eg. Safe Mode), the start attempt is a logged no-op.
                serviceInstaller.StartService("AudioEndpointBuilder")
                serviceInstaller.StartService("Audiosrv")
            End Try
        End Sub

        ''' <summary>
        ''' True when one of the endpoint's Properties values (KS device path, descriptions...)
        ''' contains one of the given codec vendor ids (eg. "hdaudio#func_01&amp;ven_10de&amp;...").
        ''' </summary>
        Private Function EndpointBelongsToVendor(ByVal flowKey As RegistryKey, ByVal endpointGuid As String, ByVal codecIds As List(Of String)) As Boolean
            Try
                Using propsKey As RegistryKey = MyRegistry.OpenSubKey(flowKey, endpointGuid & "\Properties", False)
                    If propsKey Is Nothing Then Return False

                    ' {a45c254e-df1c-4efd-8020-67d146a850e0},24 = DEVPKEY_Device_EnumeratorName:
                    ' the bus the endpoint's device lives on. Vendor display audio (NVIDIA/AMD/
                    ' Intel HDMI-DP codecs) always enumerates on HDAUDIO, so when the property is
                    ' present anything else is rejected: ROOT/SWD (Stereo Mix, NVIDIA Broadcast,
                    ' streaming devices), USB, BTHENUM (Bluetooth), INTELAUDIO (Intel DSP-hosted
                    ' BT-offload/DMIC) and SOUNDWIRE (laptop speakers/mics), whatever their other
                    ' property strings contain. Older builds may not populate the property: fall
                    ' back to the vendor-token match alone.
                    Dim enumeratorName As String = TryCast(propsKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},24", Nothing), String)
                    If Not String.IsNullOrWhiteSpace(enumeratorName) AndAlso
                       Not enumeratorName.Trim().Equals("HDAUDIO", StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    For Each valueName As String In propsKey.GetValueNames()
                        Dim value As Object = propsKey.GetValue(valueName, Nothing)
                        Dim text As String = Nothing

                        If TypeOf value Is String Then
                            text = DirectCast(value, String)
                        ElseIf TypeOf value Is String() Then
                            text = String.Join(vbNullChar, DirectCast(value, String()))
                        ElseIf TypeOf value Is Byte() Then
                            'REG_BINARY property-store values may embed UTF-16 strings (device paths).
                            text = System.Text.Encoding.Unicode.GetString(DirectCast(value, Byte()))
                        End If

                        If Not String.IsNullOrEmpty(text) AndAlso StrContainsAny(text, True, codecIds.ToArray()) Then
                            Return True
                        End If
                    Next
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, endpointGuid)
            End Try

            Return False
        End Function

        ''' <summary>Returns the HKLM key's current DACL in binary form (Nothing if missing/unreadable).</summary>
        Private Function SnapshotKeyDacl(ByVal keyPath As String) As Byte()
            Try
                Using key As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, keyPath, False)
                    If key Is Nothing Then Return Nothing
                    Return key.GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorBinaryForm()
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Couldn't snapshot permissions of " & keyPath)
                Return Nothing
            End Try
        End Function

        ''' <summary>Writes back a DACL previously captured with SnapshotKeyDacl. No-op when the
        ''' snapshot is Nothing. Requires WRITE_DAC - call while impersonating SYSTEM (key owner).</summary>
        Private Sub RestoreKeyDacl(ByVal keyPath As String, ByVal originalDacl As Byte())
            If originalDacl Is Nothing Then Return

            Try
                ' ReadWriteSubTree is required even though we only touch the DACL:
                ' SetAccessControl's EnsureWriteable() checks the .NET-side writable flag
                ' (set by the permission-check mode), not the native rights of the handle.
                ' With ReadSubTree it throws "Cannot write to the registry key" before
                ' ever reaching Windows. The native handle still only asks for
                ' ChangePermissions + ReadPermissions (WRITE_DAC + READ_CONTROL).
                Using key As RegistryKey = Registry.LocalMachine.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions Or RegistryRights.ReadPermissions)
                    If key Is Nothing Then Return

                    Dim rs As New RegistrySecurity()
                    rs.SetSecurityDescriptorBinaryForm(originalDacl, AccessControlSections.Access)
                    key.SetAccessControl(rs)

                    Application.Log.AddMessage("Original permissions restored on HKLM\" & keyPath & ".")
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Couldn't restore original permissions on " & keyPath)
            End Try
        End Sub

        Private Async Function RemoveAppxPre1809Async(ByVal AppxToRemove As String) As Task
            Dim ServiceInstaller As New ServiceInstaller
            Dim win10 As Boolean = FrmMain.IsWindows10
            Dim win10_1809 As Boolean = FrmMain.IsWindows10_1809

            If win10 Then
                'Will not work if we impersonate "SYSTEM"
                Try
                    Select Case System.Windows.Forms.SystemInformation.BootMode
                        Case System.Windows.Forms.BootMode.FailSafe
                            Try
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
                                    If regkey IsNot Nothing Then
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                    End If
                                End Using
                            Catch ex As Exception
                                Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
                            End Try
                        Case System.Windows.Forms.BootMode.FailSafeWithNetwork
                            Try
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
                                    If regkey IsNot Nothing Then
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                        Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
                                            regSubKey.SetValue("", "Service")
                                        End Using
                                    End If
                                End Using
                            Catch ex As Exception
                                Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
                            End Try
                    End Select


                    'Dim opCompletedEvent As ManualResetEvent = New ManualResetEvent(False)
                    '			End Function
                    'Windows.Foundation.IAsyncAction() = packageManager.RemovePackageAsync("NVIDIACorp.NVIDIAControlPanel_8.1.949.0_x64__56jybvy8sckqj")

                    Dim packageManager As New PackageManager()

                    For Each package In packageManager.FindPackages()
                        Dim WasRemoved As Boolean = False

                        If package Is Nothing OrElse String.IsNullOrWhiteSpace(package.Id.FullName) Then Continue For
                        If Not StrContainsAny(package.Id.FullName, True, AppxToRemove) Then Continue For
                        Dim packageIdFamilyName = package.Id.FamilyName

                        Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(10))
                            Try
                                Await packageManager.RemovePackageAsync(package.Id.FullName, If(win10_1809, RemovalOptions.RemoveForAllUsers, Nothing)).AsTask(cts.Token)
                                Application.Log.AddMessage($"{package.Id.FullName} package removed.")
                                WasRemoved = True
                            Catch ex As OperationCanceledException
                                Application.Log.AddMessage($"{package.Id.FullName} package removal timed out.")
                            Catch ex As Exception
                                Application.Log.AddMessage($"{package.Id.FullName} package removal failed: {ex.Message}")
                            End Try
                        End Using

                        If WasRemoved Then

                            ImpersonateUser.RunImpersonatedSystem(
                            Sub()

                                'Win 10 (1809)
                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
                                    If regkey IsNot Nothing Then
                                        For Each ValueName As String In regkey.GetValueNames
                                            If String.IsNullOrWhiteSpace(ValueName) Then Continue For
                                            If StrContainsAny(ValueName, True, package.Id.FamilyName) Then  'Not working need fixing
                                                Try
                                                    Deletevalue(regkey, ValueName)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        Next
                                    End If
                                End Using

                                'Win 10 (1803)
                                For Each regkeyusers As String In Registry.Users.GetSubKeyNames
                                    If String.IsNullOrWhiteSpace(regkeyusers) Then Continue For
                                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regkeyusers & "\Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
                                        If regkey IsNot Nothing Then
                                            For Each ValueName As String In regkey.GetValueNames
                                                If String.IsNullOrWhiteSpace(ValueName) Then Continue For
                                                If StrContainsAny(ValueName, True, package.Id.FamilyName) Then  'Not working need fixing
                                                    Try
                                                        Deletevalue(regkey, ValueName)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            Next
                                        End If
                                    End Using
                                Next
                            End Sub)
                        End If
                    Next

                Catch ex As Exception
                    Application.Log.AddException(ex)
                Finally
                    Select Case System.Windows.Forms.SystemInformation.BootMode
                        Case System.Windows.Forms.BootMode.FailSafe
                            ServiceInstaller.StopService("AppXSvc")
                            ServiceInstaller.StopService("camsvc")
                            ServiceInstaller.StopService("clipSVC")
                            ServiceInstaller.StopService("Wsearch")
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "AppXSvc")
                                        Deletesubregkey(regkey, "camsvc")
                                        Deletesubregkey(regkey, "clipSVC")
                                        Deletesubregkey(regkey, "Wsearch")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
                                    End Try
                                End If
                            End Using

                        Case System.Windows.Forms.BootMode.FailSafeWithNetwork
                            ServiceInstaller.StopService("AppXSvc")
                            ServiceInstaller.StopService("camsvc")
                            ServiceInstaller.StopService("clipSVC")
                            ServiceInstaller.StopService("Wsearch")
                            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
                                If regkey IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey, "AppXSvc")
                                        Deletesubregkey(regkey, "camsvc")
                                        Deletesubregkey(regkey, "clipSVC")
                                        Deletesubregkey(regkey, "Wsearch")
                                    Catch ex As Exception
                                        Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
                                    End Try
                                End If
                            End Using
                    End Select
                End Try
            End If
        End Function

        Public Sub RemoveRegDeviceSoftware(ByVal value As String)
            'Asked by NVIDIA for DCH drivers
            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\DeviceSetup\DeviceSoftware", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames
                                If child IsNot Nothing Then
                                    If StrContainsAny(child, True, value) Then
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
                End Sub)
        End Sub

        Public Sub Deletevalue(ByVal regkeypath As RegistryKey, ByVal child As String, Optional ByVal throwOnMissingValue As Boolean = True)
            If regkeypath IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(child) Then
                regkeypath.DeleteValue(child, throwOnMissingValue)

                Application.Log.AddMessage(regkeypath.ToString & "\" & child & " - " & UpdateTextMethodmessagefn(40))
            End If
        End Sub

        Public Sub ClassRoot(ByVal classroots As String(), config As ThreadSettings)

            Application.Log.AddMessage("Begin ClassRoot CleanUP")

            Dim childlist As New List(Of String)
            Dim typelibList As New List(Of String)

            Try
                Using regkeyRoot As RegistryKey = Registry.ClassesRoot
                    If regkeyRoot IsNot Nothing Then
                        Parallel.ForEach(regkeyRoot.GetSubKeyNames(),
                        Sub(child)
                            If String.IsNullOrWhiteSpace(child) Then Return

                            Dim wantedvalue As String = Nothing
                            Dim wantedvalue2 As String = Nothing
                            Dim appid As String = Nothing
                            Dim typelib As String = Nothing

                            For Each croot As String In classroots
                                If String.IsNullOrWhiteSpace(croot) Then Continue For
                                If StrContainsAny(croot, True, "GeforceExperience", "nvidiaapp") AndAlso Not config.RemoveGFE Then
                                    'do nothing
                                Else
                                    If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, child & "\CLSID")
                                            If regkey2 IsNot Nothing Then
                                                wantedvalue = TryCast(regkey2.GetValue("", String.Empty), String)

                                                If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue)
                                                        If regkey3 IsNot Nothing Then
                                                            appid = TryCast(regkey3.GetValue("AppID", String.Empty), String)

                                                            If Not String.IsNullOrWhiteSpace(appid) Then

                                                                Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "AppID", True)
                                                                    If regkey4 IsNot Nothing Then
                                                                        Try
                                                                            Deletesubregkey(regkey4, appid, False)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                End Using
                                                            End If
                                                        End If
                                                    End Using

                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue & "\TypeLib")
                                                        If regkey3 IsNot Nothing Then
                                                            typelib = TryCast(regkey3.GetValue("", String.Empty), String)

                                                            If Not String.IsNullOrWhiteSpace(typelib) Then
                                                                Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "TypeLib", True)
                                                                    If regkey4 IsNot Nothing Then
                                                                        Try
                                                                            Deletesubregkey(regkey4, typelib)
                                                                            SyncLock _listLock
                                                                                typelibList.Add(typelib)
                                                                            End SyncLock
                                                                        Catch exARG As ArgumentException
                                                                            'Do nothing, can happen (Not found)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                End Using
                                                            End If
                                                        End If
                                                    End Using

                                                    Using crkey As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID", True)
                                                        If crkey IsNot Nothing Then

                                                            Try
                                                                Deletesubregkey(crkey, wantedvalue)
                                                                SyncLock _listLock
                                                                    childlist.Add(wantedvalue)
                                                                End SyncLock
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using


                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""), False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using


                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""), False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using


                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""), False)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using
                                                End If
                                            End If
                                        End Using

                                        Try
                                            Deletesubregkey(regkeyRoot, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                End If
                            Next
                            If child.EndsWith("file", StringComparison.OrdinalIgnoreCase) AndAlso
                            config.SelectedType = CleanType.GPU AndAlso config.SelectedGPU = GPUVendor.Nvidia Then

                                Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, child)
                                    If regkey5 IsNot Nothing Then
                                        For Each shellEX As String In regkey5.GetSubKeyNames
                                            If String.IsNullOrWhiteSpace(shellEX) Then Continue For

                                            If StrContainsAny(shellEX, True, "shellex") Then
                                                Using regkey6 As RegistryKey = MyRegistry.OpenSubKey(regkey5, shellEX & "\ContextMenuHandlers", True)
                                                    If regkey6 IsNot Nothing Then
                                                        For Each ShExt As String In regkey6.GetSubKeyNames
                                                            If String.IsNullOrWhiteSpace(ShExt) Then Continue For

                                                            If StrContainsAny(ShExt, True, "openglshext", "nvappshext") Then
                                                                Using regkey7 As RegistryKey = MyRegistry.OpenSubKey(regkey6, ShExt)
                                                                    If regkey7 IsNot Nothing Then
                                                                        wantedvalue2 = TryCast(regkey7.GetValue("", String.Empty), String)
                                                                        If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                                            'If StrContainsAny(wantedvalue2, True, wantedvalue) Then
                                                                            Try
                                                                                Deletesubregkey(regkey6, ShExt)
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                            'End If
                                                                        End If
                                                                    End If
                                                                End Using
                                                            End If
                                                        Next
                                                    End If
                                                End Using
                                            End If
                                        Next
                                    End If
                                End Using
                            End If
                        End Sub)
                        InterfaceRemovalWithValue(childlist, typelibList, False)
                        RemoveCLSIDInstance(childlist, False)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            childlist.Clear()
            'I do not clear the typelib because when the normal typelib is removed, the WOW6432Node typelib also get removed by windows.
            'So the interfaces of theses must also be.

            If IntPtr.Size = 8 Then
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node", True)
                        If regkey IsNot Nothing Then
                            Parallel.ForEach(regkey.GetSubKeyNames(),
                            Sub(child)
                                If String.IsNullOrWhiteSpace(child) Then Return

                                Dim wantedvalue As String = Nothing
                                Dim wantedvalue2 As String = Nothing
                                Dim appid As String = Nothing
                                Dim typelib As String = Nothing

                                For Each croot As String In classroots
                                    If String.IsNullOrWhiteSpace(croot) Then Continue For

                                    If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\CLSID")
                                            If regkey2 IsNot Nothing Then
                                                wantedvalue = TryCast(regkey2.GetValue("", String.Empty), String)

                                                If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue)
                                                        If regkey3 IsNot Nothing Then
                                                            appid = TryCast(regkey3.GetValue("AppID", String.Empty), String)

                                                            If Not String.IsNullOrWhiteSpace(appid) Then

                                                                Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "AppID", True)
                                                                    If regkey4 IsNot Nothing Then
                                                                        Try
                                                                            Deletesubregkey(regkey4, appid)
                                                                        Catch exARG As ArgumentException
                                                                            'Do nothing, can happen (Not found)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                End Using
                                                            End If
                                                        End If
                                                    End Using

                                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue & "\TypeLib")
                                                        If regkey3 IsNot Nothing Then
                                                            typelib = TryCast(regkey3.GetValue("", String.Empty), String)

                                                            If Not String.IsNullOrWhiteSpace(typelib) Then

                                                                Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "TypeLib", True)
                                                                    If regkey4 IsNot Nothing Then
                                                                        Try
                                                                            Deletesubregkey(regkey4, typelib)
                                                                            SyncLock _listLock
                                                                                typelibList.Add(typelib)
                                                                            End SyncLock
                                                                        Catch exARG As ArgumentException
                                                                            'Do nothing, can happen (Not found)
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                End Using
                                                            End If
                                                        End If
                                                    End Using

                                                    Using regkeyC As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID", True)
                                                        If regkeyC IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyC, wantedvalue)
                                                                SyncLock _listLock
                                                                    childlist.Add(wantedvalue)
                                                                End SyncLock
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using


                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using

                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using



                                                    Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True)
                                                        If regkeyM IsNot Nothing Then
                                                            Try
                                                                Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End Using
                                                End If
                                            End If
                                        End Using

                                        Try
                                            Deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If
                                Next
                            End Sub)
                            InterfaceRemovalWithValue(childlist, typelibList, True)
                            RemoveCLSIDInstance(childlist, True)
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
            End If
            childlist.Clear()
            typelibList.Clear()
            Application.Log.AddMessage("End ClassRoot CleanUP")
        End Sub

        Private Sub InterfaceRemovalWithValue(ByVal childlist As List(Of String), ByVal typelibList As List(Of String), ByVal wow6432 As Boolean)
            If Not wow6432 Then
                Using reginterface As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface", True)
                    If reginterface IsNot Nothing Then
                        Parallel.ForEach(reginterface.GetSubKeyNames(),
                        Sub(interfacechild)
                            If String.IsNullOrWhiteSpace(interfacechild) Then Return
                            Dim wantedvalue2 As String
                            Using reginterface2 As RegistryKey = MyRegistry.OpenSubKey(reginterface, interfacechild, False)
                                If reginterface2 IsNot Nothing Then
                                    If childlist IsNot Nothing AndAlso childlist.Count > 0 Then
                                        If MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32") IsNot Nothing Then
                                            wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32").GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                For Each item As String In childlist
                                                    If String.IsNullOrWhiteSpace(item) Then Continue For
                                                    If StrContainsAny(wantedvalue2, True, item) AndAlso MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" + item) Is Nothing Then
                                                        Try
                                                            Deletesubregkey(reginterface, interfacechild)
                                                            Exit For
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex, "Interface Removal via ClassRoot/CLSID")
                                                        End Try
                                                    End If
                                                Next
                                            End If
                                        End If
                                    End If
                                    'Interface Removal via Typelib list
                                    If typelibList IsNot Nothing AndAlso typelibList.Count > 0 Then
                                        If MyRegistry.OpenSubKey(reginterface2, "TypeLib") IsNot Nothing Then
                                            wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "TypeLib").GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                For Each item As String In typelibList
                                                    If String.IsNullOrWhiteSpace(item) Then Continue For
                                                    If StrContainsAny(wantedvalue2, True, item) AndAlso MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib\" + item) Is Nothing Then
                                                        Try
                                                            Deletesubregkey(reginterface, interfacechild)
                                                            Exit For
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex, "Interface Removal via ClassRoot/tpyelib")
                                                        End Try
                                                    End If
                                                Next
                                            End If
                                        End If
                                    End If
                                End If
                            End Using
                        End Sub)
                    End If
                End Using
            Else
                Using reginterface As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface", True)
                    If reginterface IsNot Nothing Then
                        Parallel.ForEach(reginterface.GetSubKeyNames(),
                        Sub(interfacechild)
                            If String.IsNullOrWhiteSpace(interfacechild) Then Return
                            Dim wantedvalue2 As String
                            Using reginterface2 As RegistryKey = MyRegistry.OpenSubKey(reginterface, interfacechild, False)
                                If reginterface2 IsNot Nothing Then
                                    'Interface removal for the CLSID childlist.
                                    If childlist IsNot Nothing AndAlso childlist.Count > 0 Then
                                        If MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32") IsNot Nothing Then
                                            wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32").GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                For Each item As String In childlist
                                                    If String.IsNullOrWhiteSpace(item) Then Continue For
                                                    If StrContainsAny(wantedvalue2, True, item) AndAlso MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" + item) Is Nothing Then
                                                        Try
                                                            Deletesubregkey(reginterface, interfacechild)
                                                            Exit For
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex, "Interface Removal via Classroot/CLSID childlist")
                                                        End Try
                                                    End If
                                                Next
                                            End If
                                        End If
                                    End If
                                    'Interface removal for the Typelib list.
                                    If typelibList IsNot Nothing AndAlso typelibList.Count > 0 Then
                                        If MyRegistry.OpenSubKey(reginterface2, "TypeLib") IsNot Nothing Then
                                            wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "TypeLib").GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue2) Then
                                                For Each item As String In typelibList
                                                    If String.IsNullOrWhiteSpace(item) Then Continue For
                                                    If StrContainsAny(wantedvalue2, True, item) AndAlso MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\TypeLib\" + item) Is Nothing Then
                                                        Try
                                                            Deletesubregkey(reginterface, interfacechild)
                                                            Exit For
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex, "Interface Removal via ClassRoot/Typelist")
                                                        End Try
                                                    End If
                                                Next
                                            End If
                                        End If
                                    End If
                                End If
                            End Using
                        End Sub)
                    End If
                End Using
            End If
        End Sub

        Public Sub Installer(ByVal packages As String(), config As ThreadSettings)

            Dim wantedvalue As String = Nothing
            Dim removephysx As Boolean = config.RemovePhysX

            Try
                Application.Log.AddMessage("-Starting S-1-5-xx region cleanUP")
                Dim file As String
                Dim folder As String
                Using basekey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
               "Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
                    If basekey IsNot Nothing Then
                        For Each super As String In basekey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(super) Then Continue For

                            If StrContainsAny(super, True, "s-1-5") Then

                                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                             "Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)

                                    If regkey IsNot Nothing Then
                                        For Each child As String In regkey.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child) Then Continue For

                                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                        "Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child &
                                        "\InstallProperties", False)

                                                If subregkey IsNot Nothing Then

                                                    wantedvalue = TryCast(subregkey.GetValue("DisplayName", String.Empty), String)
                                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For

                                                    For Each package As String In packages
                                                        If String.IsNullOrWhiteSpace(package) Then Continue For
                                                        If (StrContainsAny(wantedvalue, True, package)) AndAlso
                                                      Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then


                                                            Application.Log.AddMessage("Removing .msi")
                                                            'Deleting here the c:\windows\installer entries.
                                                            Try
                                                                file = TryCast(subregkey.GetValue("LocalPackage", String.Empty), String)
                                                                If String.IsNullOrWhiteSpace(file) Then Continue For

                                                                If StrContainsAny(file, True, ".msi") Then
                                                                    Delete(file)
                                                                End If
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try

                                                            Try
                                                                folder = TryCast(subregkey.GetValue("UninstallString", String.Empty), String)
                                                                If Not String.IsNullOrWhiteSpace(folder) Then
                                                                    If StrContainsAny(folder, True, "{") AndAlso StrContainsAny(folder, True, "}") Then

                                                                        folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
                                                                        TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)

                                                                        Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
                                                                            If regkey2 IsNot Nothing Then

                                                                                For Each subkeyname As String In regkey2.GetValueNames
                                                                                    If Not String.IsNullOrWhiteSpace(subkeyname) Then
                                                                                        If StrContainsAny(subkeyname, True, folder) Then
                                                                                            Deletevalue(regkey2, subkeyname)
                                                                                        End If
                                                                                    End If
                                                                                Next
                                                                            End If
                                                                        End Using
                                                                    End If
                                                                End If
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try

                                                            Try
                                                                Deletesubregkey(regkey, child)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try

                                                            Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                                        "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes", True)
                                                                If superregkey IsNot Nothing Then
                                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                                        Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                                                   "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\" & child2, True)

                                                                            If subsuperregkey IsNot Nothing Then
                                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                                    If String.IsNullOrWhiteSpace(wantedstring) Then Continue For

                                                                                    If StrContainsAny(wantedstring, True, child) Then
                                                                                        Try
                                                                                            Deletevalue(subsuperregkey, wantedstring)
                                                                                            Exit For
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                Next
                                                                                For Each wantedsubkey In subsuperregkey.GetSubKeyNames()
                                                                                    If String.IsNullOrWhiteSpace(wantedsubkey) Then Continue For
                                                                                    If wantedsubkey.Contains(child) Then
                                                                                        Try
                                                                                            Deletesubregkey(subsuperregkey, wantedsubkey)
                                                                                            Exit For
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                Next
                                                                                If subsuperregkey.ValueCount = 0 AndAlso subsuperregkey.SubKeyCount = 0 Then
                                                                                    Deletesubregkey(superregkey, child2)
                                                                                End If
                                                                            End If
                                                                        End Using
                                                                    Next
                                                                End If
                                                            End Using
                                                            Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                                        "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)
                                                                If superregkey IsNot Nothing Then
                                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                                        Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                                                   "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child2, True)

                                                                            If subsuperregkey IsNot Nothing Then
                                                                                For Each wantedstring In subsuperregkey.GetValueNames()
                                                                                    If String.IsNullOrWhiteSpace(wantedstring) Then Continue For

                                                                                    If wantedstring.Contains(child) Then
                                                                                        Try
                                                                                            Deletevalue(subsuperregkey, wantedstring)
                                                                                            Exit For
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                Next
                                                                                For Each wantedsubkey In subsuperregkey.GetSubKeyNames()
                                                                                    If String.IsNullOrWhiteSpace(wantedsubkey) Then Continue For
                                                                                    If wantedsubkey.Contains(child) Then
                                                                                        Try
                                                                                            Deletesubregkey(subsuperregkey, wantedsubkey)
                                                                                            Exit For
                                                                                        Catch ex As Exception
                                                                                            Application.Log.AddException(ex)
                                                                                        End Try
                                                                                    End If
                                                                                Next
                                                                                If subsuperregkey.ValueCount = 0 AndAlso subsuperregkey.SubKeyCount = 0 Then
                                                                                    Deletesubregkey(superregkey, child2)
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

                Application.Log.AddMessage("-End of S-1-5-xx region cleanUP")
            Catch ex As Exception
                MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
                Application.Log.AddException(ex)
            End Try


            Try
                Dim folder As String
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
            "Installer\Products", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For

                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
                        "Installer\Products\" & child, False)

                                If subregkey IsNot Nothing Then

                                    wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For

                                    For Each package As String In packages
                                        If String.IsNullOrWhiteSpace(package) Then Continue For

                                        If StrContainsAny(wantedvalue, True, package) AndAlso
                                       Not ((removephysx = False) AndAlso StrContainsAny(wantedvalue, True, "physx")) Then

                                            Try
                                                folder = TryCast(subregkey.GetValue("ProductIcon", String.Empty), String)

                                                If (String.IsNullOrWhiteSpace(folder)) Then Continue For
                                                If Not StrContainsAny(folder, True, "{") Then Continue For
                                                If Not StrContainsAny(folder, True, "}") Then Continue For

                                                folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
                                                TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)
                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
                                                    If regkey2 IsNot Nothing Then
                                                        For Each subkeyname As String In regkey2.GetValueNames
                                                            If String.IsNullOrWhiteSpace(subkeyname) Then Continue For

                                                            If StrContainsAny(subkeyname, True, folder) Then
                                                                Deletevalue(regkey2, subkeyname)
                                                            End If
                                                        Next
                                                    End If
                                                End Using

                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try

                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                            End Try

                                            Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Features", True)
                                                If regkey3 IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey3, child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End Using

                                            Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
                                        "Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                        Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
                                                      "Installer\UpgradeCodes\" & child2, True)

                                                            If subsuperregkey IsNot Nothing Then
                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                    If String.IsNullOrWhiteSpace(wantedstring) Then Continue For
                                                                    If wantedstring.Contains(child) Then
                                                                        Try
                                                                            Deletevalue(subsuperregkey, child)
                                                                            Exit For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                                For Each wantedsubkey In subsuperregkey.GetSubKeyNames()
                                                                    If String.IsNullOrWhiteSpace(wantedsubkey) Then Continue For
                                                                    If wantedsubkey.Contains(child) Then
                                                                        Try
                                                                            Deletesubregkey(subsuperregkey, wantedsubkey)
                                                                            Exit For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                                If subsuperregkey.ValueCount = 0 AndAlso subsuperregkey.SubKeyCount = 0 Then
                                                                    Deletesubregkey(superregkey, child2)
                                                                End If
                                                            End If
                                                        End Using
                                                    Next
                                                End If
                                            End Using

                                            Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\Components", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                        Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\Components\" & child2, True)

                                                            If subsuperregkey IsNot Nothing Then
                                                                For Each wantedstring In subsuperregkey.GetValueNames()
                                                                    If String.IsNullOrWhiteSpace(wantedstring) Then Continue For

                                                                    If wantedstring.Contains(child) Then
                                                                        Try
                                                                            Deletevalue(subsuperregkey, wantedstring)
                                                                            Exit For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                                For Each wantedsubkey In subsuperregkey.GetSubKeyNames()
                                                                    If String.IsNullOrWhiteSpace(wantedsubkey) Then Continue For
                                                                    If wantedsubkey.Contains(child) Then
                                                                        Try
                                                                            Deletesubregkey(subsuperregkey, wantedsubkey)
                                                                            Exit For
                                                                        Catch ex As Exception
                                                                            Application.Log.AddException(ex)
                                                                        End Try
                                                                    End If
                                                                Next
                                                                If subsuperregkey.ValueCount = 0 AndAlso subsuperregkey.SubKeyCount = 0 Then
                                                                    Deletesubregkey(superregkey, child2)
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

            Catch ex As Exception
                MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
                Application.Log.AddException(ex)
            End Try


            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
            "Software\Classes\Installer\Products", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For

                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                        "Software\Classes\Installer\Products\" & child, False)

                                If subregkey IsNot Nothing Then
                                    wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For

                                    For Each package As String In packages
                                        If String.IsNullOrWhiteSpace(package) Then Continue For

                                        If (StrContainsAny(wantedvalue, True, package)) AndAlso
                                      Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then

                                            Try
                                                Deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                            End Try

                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Classes\Installer\Features", True)
                                                If regkey2 IsNot Nothing Then
                                                    Try
                                                        Deletesubregkey(regkey2, child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End Using

                                            Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                        "Software\Classes\Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                        Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
                                                      "Software\Classes\Installer\UpgradeCodes\" & child2, True)

                                                            If subsuperregkey IsNot Nothing Then
                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                    If String.IsNullOrWhiteSpace(wantedstring) Then Continue For

                                                                    If StrContainsAny(wantedstring, True, child) Then
                                                                        Try
                                                                            Deletesubregkey(superregkey, child2)
                                                                        Catch ex As Exception
                                                                        End Try
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

            Catch ex As Exception
                MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
                Application.Log.AddException(ex)
            End Try


            Try
                For Each users As String In Registry.Users.GetSubKeyNames()
                    If String.IsNullOrWhiteSpace(users) Then Continue For

                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
                users & "\Software\Microsoft\Installer\Products", True)

                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For

                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
                              users & "\Software\Microsoft\Installer\Products\" & child, False)

                                    If subregkey IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
                                        If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For

                                        For Each package As String In packages
                                            If String.IsNullOrWhiteSpace(package) Then Continue For

                                            If (StrContainsAny(wantedvalue, True, package)) AndAlso
                                           Not ((removephysx = False) AndAlso StrContainsAny(wantedvalue, True, "physx")) Then

                                                Try
                                                    Deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                End Try

                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Installer\Features", True)
                                                    If regkey2 IsNot Nothing Then
                                                        Try
                                                            Deletesubregkey(regkey2, child)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                End Using

                                                Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
                                            users & "\Software\Microsoft\Installer\UpgradeCodes", True)
                                                    If superregkey IsNot Nothing Then
                                                        For Each child2 As String In superregkey.GetSubKeyNames()
                                                            If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                            Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
                                                          users & "\Software\Microsoft\Installer\UpgradeCodes" & child2, True)

                                                                If subsuperregkey IsNot Nothing Then
                                                                    For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                        If String.IsNullOrWhiteSpace(wantedstring) Then Continue For

                                                                        If wantedstring.Contains(child) Then
                                                                            Try
                                                                                Deletesubregkey(superregkey, child2)
                                                                            Catch ex As Exception
                                                                            End Try
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
                Next

            Catch ex As Exception
                MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
                Application.Log.AddException(ex)
            End Try

        End Sub

        Private Function GetServiceBaseNameOnly(imagePath As String) As String
            If String.IsNullOrWhiteSpace(imagePath) Then Return Nothing

            ' Expand environment vars like %SystemRoot%
            Dim expanded As String = Environment.ExpandEnvironmentVariables(imagePath)

            ' If there are command line args, take only the first token (the exe/sys path)
            Dim exePart As String = expanded.Split(New Char() {" "c}, 2)(0).Trim(""""c)

            ' Return just the file name without extension
            Return System.IO.Path.GetFileNameWithoutExtension(exePart)
        End Function

        Public Sub Cleanserviceprocess(ByVal services As String(), config As ThreadSettings)
            Dim ServiceInstaller As New ServiceInstaller
            Dim objAuto As AutoResetEvent = New AutoResetEvent(False)

            For i As Integer = 0 To services.Count - 1
                If String.IsNullOrWhiteSpace(services(i)) Then Continue For
                If (config.RemoveAudioBus = False OrElse FrmMain.DoNotRemoveAmdHdAudioBusFiles) AndAlso StrContainsAny(services(i), True, "amdkmafd") Then Continue For
                If (config.RemoveAMDKMPFD = False Or config.NotPresentAMDKMPFD = False) AndAlso StrContainsAny(services(i), True, "amdkmpfd") Then Continue For

                ' Résolution nom + check présence - en SYSTEM
                Dim serviceExists As Boolean = False
                Dim serviceAbsentWithResidualRegistry As Boolean = False
                Dim service As String = services(i)

                ImpersonateUser.RunImpersonatedSystem(
                    Sub()
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services", True)
                            If regkey IsNot Nothing Then
                                ' Résolution du vrai nom
                                For Each regservice As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(regservice) Then Continue For
                                    Using subkey As RegistryKey = MyRegistry.OpenSubKey(regkey, regservice)
                                        If subkey IsNot Nothing Then
                                            Dim wantedvalue = TryCast(subkey.GetValue("ImagePath", String.Empty), String)
                                            If String.IsNullOrWhiteSpace(wantedvalue) Then Continue For
                                            Try
                                                If GetServiceBaseNameOnly(wantedvalue).Equals(service, StringComparison.OrdinalIgnoreCase) Then
                                                    service = regservice
                                                    Exit For
                                                End If
                                            Catch ex As Exception
                                                Application.Log.AddWarning(ex, wantedvalue)
                                            End Try
                                        End If
                                    End Using
                                Next

                                ' Check présence en SYSTEM
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, service, False)
                                    If regkey2 IsNot Nothing Then
                                        If ServiceInstaller.GetServiceStatus(service) = Nothing AndAlso ServiceInstaller.GetServiceStatus(service, False) = Nothing Then
                                            ' Absent mais registry résiduel - delete directement en SYSTEM
                                            If Not (config.SelectedGPU = GPUVendor.Nvidia AndAlso config.NVIDIA_App_Installed AndAlso (Not config.RemoveGFE) AndAlso StrContainsAny(service, True, "nvlddmkm")) Then
                                                Try
                                                    Deletesubregkey(regkey, service, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        Else
                                            serviceExists = True
                                        End If
                                    End If
                                End Using
                            End If
                        End Using
                    End Sub)

                ' Uninstall - PAS en SYSTEM
                If serviceExists Then
                    Try
                        ServiceInstaller.Uninstall(service)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                        objAuto.WaitOne(10)
                        Continue For
                    End Try

                    ' Attente
                    Dim waits As Int32 = 0
                    While waits < 30
                        If ServiceInstaller.GetServiceStatus(service) <> Nothing OrElse ServiceInstaller.GetServiceStatus(service, False) <> Nothing Then
                            waits += 1
                            objAuto.WaitOne(100)
                        Else
                            Exit While
                        End If
                    End While

                    ' Cleanup registry post-uninstall - en SYSTEM
                    Dim serviceName As String = service
                    ImpersonateUser.RunImpersonatedSystem(
                        Sub()
                            Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\Setup\FirstBoot\Services", True)
                                If regkey4 IsNot Nothing Then
                                    Try
                                        Deletesubregkey(regkey4, serviceName, False)
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If
                            End Using
                        End Sub)
                End If

                objAuto.WaitOne(10)
            Next

            CleanControlVideo(services, config)
            CleanLeftOverRegDirectXSection()
        End Sub

        Public Sub CleanControlVideo(ByVal services As String(), config As ThreadSettings)
            If System.Windows.Forms.SystemInformation.BootMode = Forms.BootMode.Normal Then Return
            ImpersonateUser.RunImpersonatedSystem(
            Sub()
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\Video", True)
                        If regkey IsNot Nothing Then
                            Dim serviceValue As String

                            For Each child As String In regkey.GetSubKeyNames
                                If String.IsNullOrWhiteSpace(child) Then Continue For

                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\Video", False)
                                    If subregkey IsNot Nothing Then
                                        serviceValue = TryCast(subregkey.GetValue("Service", String.Empty), String)

                                        If String.IsNullOrWhiteSpace(serviceValue) Then Continue For

                                        For Each service As String In services
                                            If String.IsNullOrWhiteSpace(service) Then Continue For
                                            If serviceValue.Equals(service, StringComparison.OrdinalIgnoreCase) OrElse serviceValue.Equals("BasicDisplay", StringComparison.OrdinalIgnoreCase) Then
                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                    Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                    'Also remove the HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DirectX keys associated to it.
                                                    Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                Catch ex As ArgumentException
                                                    'avoid an issue specific to this key
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                                Exit For
                                            End If
                                        Next
                                    Else
                                        'Here, if subregkey is nothing, it mean \video doesnt exist and there is no \0000, we can delete it.
                                        'this is a general cleanUP we could say.
                                        Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                            If regkey3 IsNot Nothing Then
                                                If regkey3.SubKeyCount = 0 Then
                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                        Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                        Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                        Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                    Catch ex As ArgumentException
                                                        'avoid an issue specific to this key
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                Else
                                                    Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey3, "0000", False)
                                                        If regkey4 IsNot Nothing Then
                                                            Dim providerName = TryCast(regkey4.GetValue("ProviderName", String.Empty), String)
                                                            If Not String.IsNullOrWhiteSpace(providerName) Then
                                                                Select Case config.SelectedGPU
                                                                    Case GPUVendor.Nvidia
                                                                        If StrContainsAny(providerName, True, "NVIDIA", "Microsoft") Then
                                                                            Try
                                                                                Deletesubregkey(regkey, child)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                                            Catch ex As ArgumentException
                                                                                'avoid an issue specific to this key
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                    Case GPUVendor.AMD
                                                                        If StrContainsAny(providerName, True, "Advanced Micro Devices") Then
                                                                            Try
                                                                                Deletesubregkey(regkey, child)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                                            Catch ex As ArgumentException
                                                                                'avoid an issue specific to this key
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                    Case GPUVendor.Intel
                                                                        If StrContainsAny(providerName, True, "Intel") Then
                                                                            Try
                                                                                Deletesubregkey(regkey, child)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                                            Catch ex As ArgumentException
                                                                                'avoid an issue specific to this key
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                    Case GPUVendor.Lisuan
                                                                        If StrContainsAny(providerName, True, "Lisuan") Then
                                                                            Try
                                                                                Deletesubregkey(regkey, child)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\UnitedVideo\CONTROL\VIDEO\" & child, False)
                                                                                Deletesubregkey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX\" & child, False)
                                                                            Catch ex As ArgumentException
                                                                                'avoid an issue specific to this key
                                                                            Catch ex As Exception
                                                                                Application.Log.AddException(ex)
                                                                            End Try
                                                                        End If
                                                                End Select
                                                            End If
                                                        End If
                                                    End Using
                                                End If
                                            End If
                                        End Using
                                    End If
                                End Using
                            Next
                        End If
                    End Using

                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
            End Sub)
        End Sub

        Private Sub CleanLeftOverRegDirectXSection()
            ImpersonateUser.RunImpersonatedSystem(
            Sub()
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\DirectX", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\Video", True)
                                    If regkey2 IsNot Nothing Then
                                        If Not StrContainsAny(child, True, regkey2.GetSubKeyNames) Then
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
            End Sub)
        End Sub

        Public Sub RemoveMonitorConfiguration()
            Dim keysToClean As String() = {
                "SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration",
                "SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Connectivity",
                "SYSTEM\CurrentControlSet\Control\GraphicsDrivers\ScaleFactors",
                "SYSTEM\CurrentControlSet\Control\GraphicsDrivers\MonitorDataStore"
            }

            For Each keyPath As String In keysToClean
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, keyPath, True)
                    If regkey IsNot Nothing Then
                        For Each subregkey In regkey.GetSubKeyNames()
                            If subregkey IsNot Nothing Then
                                Try
                                    Deletesubregkey(regkey, subregkey)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        Next
                    End If
                End Using
            Next
        End Sub

        Public Function CheckServiceStartupType(ByVal service As String) As String
            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & service, False)
                If regkey IsNot Nothing Then
                    Return TryCast(regkey.GetValue("Start", String.Empty), String)
                End If
            End Using
            Return Nothing
        End Function

        Public Sub SetServiceStartupType(ByVal service As String, value As String)
            Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & service, True)
                regkey?.SetValue("Start", value, RegistryValueKind.DWord)
            End Using
        End Sub

        Public Sub PrePnplockdownfiles(ByVal oeminf As String)
            Dim win8higher = FrmMain.IsWindows8OrHigher
            Dim processinfo As New ProcessStartInfo
            Dim process As New Process
            Dim sourceValue As String

            Try
                If win8higher Then
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                        If regkey IsNot Nothing Then
                            If Not String.IsNullOrWhiteSpace(oeminf) Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    sourceValue = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("Source", String.Empty), String)

                                    If Not String.IsNullOrWhiteSpace(sourceValue) AndAlso StrContainsAny(sourceValue, True, oeminf) Then
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
                End If
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

        End Sub

        Public Sub PnpLockdownFiles(ByVal driverfiles As String())

            Dim winxp = FrmMain.IsWindowsXp
            Dim win8higher = FrmMain.IsWindows8OrHigher
            Dim processinfo As New ProcessStartInfo
            Dim process As New Process
            Dim fileIO As New FileIO

            Try
                If Not winxp Then  'this does not exist on winxp so we skip if winxp detected
                    If win8higher Then
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    'Resolved via GetSystemDirectory, never Environment.ExpandEnvironmentVariables:
                                    'on systems where %SystemRoot% is broken, expansion fails and every entry would
                                    'look orphaned - deleting lockdown entries for files that still exist.
                                    Dim normalizedPath As String = ResolvePnpLockdownPath(child)
                                    If normalizedPath Is Nothing Then Continue For

                                    If Not fileIO.ExistsFile(normalizedPath) Then
                                        If StrContainsAny(normalizedPath, True, driverfiles) Then
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

                    Else   'Older windows  (windows vista and 7 run here)

                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    If Not fileIO.ExistsFile(child) Then
                                        If StrContainsAny(child, True, driverfiles) Then
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
                    End If
                End If

            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

        End Sub

        ''' <summary>
        ''' Resolves a PnpLockdownFiles entry name (eg. "%SystemRoot%/System32/DriverStore/
        ''' FileRepository/nv_dispi.inf_amd64_xxx/_nvngx.dll") to a real filesystem path.
        ''' %SystemRoot% is resolved from GetSystemDirectory - never from the environment
        ''' variable, which is broken on some user systems and would make every entry look
        ''' orphaned. Returns Nothing for any format that cannot be resolved safely: the
        ''' callers must skip those entries instead of guessing.
        ''' </summary>
        Private Function ResolvePnpLockdownPath(ByVal entryName As String) As String
            If String.IsNullOrWhiteSpace(entryName) Then Return Nothing

            Dim normalized As String = entryName.Replace("/"c, "\"c)

            If normalized.StartsWith("%SystemRoot%\", StringComparison.OrdinalIgnoreCase) Then
                Dim winDir As String = System.IO.Path.GetDirectoryName(Environment.SystemDirectory)
                If String.IsNullOrWhiteSpace(winDir) Then Return Nothing

                Return System.IO.Path.Combine(winDir, normalized.Substring("%SystemRoot%\".Length))
            End If

            'Pre-Win8 style entries are plain absolute paths.
            If normalized.Length > 3 AndAlso Char.IsLetter(normalized(0)) AndAlso normalized(1) = ":"c AndAlso normalized(2) = "\"c Then
                Return normalized
            End If

            'Unknown format (another environment variable, relative path...) - do not guess.
            Return Nothing
        End Function

        ''' <summary>
        ''' Removes PnpLockdownFiles entries whose target file no longer exists on disk -
        ''' stale bookkeeping that accumulates when driver packages are removed - whatever
        ''' the vendor. Call it after the driver store cleanup so entries orphaned by the
        ''' current run are caught too. The sanity check on the resolved system directory
        ''' guarantees a broken path resolution can never mass-delete valid entries.
        ''' </summary>
        Public Sub PnpLockdownFilesOrphans()
            If FrmMain.IsWindowsXp Then Return 'the key does not exist on XP

            Dim win8higher As Boolean = FrmMain.IsWindows8OrHigher
            Dim fileIO As New FileIO

            Try
                'Prove that path resolution and file-existence checks work on this system
                'before deleting anything: ntoskrnl.exe always exists on a healthy install.
                Dim winDir As String = System.IO.Path.GetDirectoryName(Environment.SystemDirectory)
                If String.IsNullOrWhiteSpace(winDir) OrElse
                   Not fileIO.ExistsFile(System.IO.Path.Combine(Environment.SystemDirectory, "ntoskrnl.exe")) Then
                    Application.Log.AddWarningMessage("PnpLockdownFiles orphan cleanup skipped: Windows directory sanity check failed.")
                    Return
                End If

                Dim removedCount As Integer = 0
                Dim skippedCount As Integer = 0

                ImpersonateUser.RunImpersonatedSystem(
                    Sub()
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                            If regkey Is Nothing Then Return

                            If win8higher Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    Dim resolved As String = ResolvePnpLockdownPath(child)
                                    If resolved Is Nothing Then
                                        skippedCount += 1
                                        Continue For
                                    End If

                                    If fileIO.ExistsFile(resolved) OrElse fileIO.ExistsDir(resolved) Then Continue For

                                    Try
                                        Deletesubregkey(regkey, child, False)
                                        removedCount += 1
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                Next
                            Else
                                'Older windows (vista and 7): entries are value names holding absolute paths.
                                For Each valueName As String In regkey.GetValueNames()
                                    If String.IsNullOrWhiteSpace(valueName) Then Continue For

                                    Dim resolved As String = ResolvePnpLockdownPath(valueName)
                                    If resolved Is Nothing Then
                                        skippedCount += 1
                                        Continue For
                                    End If

                                    If fileIO.ExistsFile(resolved) OrElse fileIO.ExistsDir(resolved) Then Continue For

                                    Try
                                        Deletevalue(regkey, valueName, False)
                                        removedCount += 1
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                Next
                            End If
                        End Using
                    End Sub)

                Application.Log.AddMessage($"PnpLockdownFiles orphan cleanup: {removedCount} stale entry(ies) removed, {skippedCount} unresolved entry(ies) left untouched.")
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        ''' <summary>
        ''' Makes sure Windows still has a display driver to fall back on once the vendor driver
        ''' is gone. Some users disable "Microsoft Basic Display Adapter" in Device Manager; with
        ''' the vendor driver then removed the machine is left with no WDDM driver at all, which
        ''' means a black screen - Safe Mode included, so there is nothing left to boot into.
        ''' Registry only: clears CONFIGFLAG_DISABLED on the device node and puts a disabled
        ''' service Start back to System.
        ''' Silent for the user: the whole check is ONE log entry carrying the state of each item,
        ''' promoted from Event to Warning only when something actually had to be corrected.
        ''' </summary>
        Public Sub EnsureBasicDisplayFallback()
            Dim logEntry As LogEntry = Application.Log.CreateEntry(Nothing, "Windows fallback display drivers verified (BasicDisplay / BasicRender)")
            logEntry.Type = LogType.Event
            logEntry.Separator = " : "

            Dim corrected As Boolean = False

            ImpersonateUser.RunImpersonatedSystem(
                Sub()
                    ' Plain If/Then rather than Or-ing the calls: every check must run, and this
                    ' reads the same whether or not the reader knows VB's Or is non-short-circuit.
                    If CheckFallbackDevice("BASICDISPLAY", logEntry) Then corrected = True
                    If CheckFallbackService("BasicDisplay", logEntry) Then corrected = True
                    If CheckFallbackDevice("BASICRENDER", logEntry) Then corrected = True
                    If CheckFallbackService("BasicRender", logEntry) Then corrected = True
                End Sub)

            If corrected Then
                logEntry.Type = LogType.Warning
                logEntry.Message = "Windows fallback display drivers needed attention - with them disabled, removing the display driver leaves no display at all, Safe Mode included."
            End If

            Application.Log.Add(logEntry)
        End Sub

        ''' <summary>
        ''' Records the fallback device's state into logEntry and clears CONFIGFLAG_DISABLED when
        ''' it is set. Returns True when the entry deserves a warning (corrected, missing, or
        ''' could not be read).
        ''' </summary>
        Private Function CheckFallbackDevice(ByVal name As String, ByVal logEntry As LogEntry) As Boolean
            Const CONFIGFLAG_DISABLED As Integer = 1
            Dim label As String = "device ROOT\" & name & "\0000"

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Enum\ROOT\" & name & "\0000", True)
                    If regkey Is Nothing Then
                        logEntry.Add(label, "MISSING - DDU cannot recreate it")
                        Return True
                    End If

                    Dim raw As Object = regkey.GetValue("ConfigFlags", Nothing)

                    If raw Is Nothing Then
                        logEntry.Add(label, "enabled (no ConfigFlags value)")
                        Return False
                    End If

                    Dim flags As Integer = CInt(raw)

                    If (flags And CONFIGFLAG_DISABLED) = 0 Then
                        logEntry.Add(label, String.Format("enabled (ConfigFlags = 0x{0:X})", flags))
                        Return False
                    End If

                    Dim fixedFlags As Integer = flags And Not CONFIGFLAG_DISABLED
                    regkey.SetValue("ConfigFlags", fixedFlags, RegistryValueKind.DWord)

                    logEntry.Add(label, String.Format("DISABLED (ConfigFlags = 0x{0:X}) -> RE-ENABLED (0x{1:X})", flags, fixedFlags))
                    Return True
                End Using
            Catch ex As Exception
                ' Kept out of logEntry on purpose: LogEntry.AddException would force the whole
                ' entry to Error and clear its exception data. The stack gets its own entry.
                Application.Log.AddException(ex, "Fallback display: could not check " & label)
                logEntry.Add(label, "check FAILED : " & ex.Message)
                Return True
            End Try
        End Function

        ''' <summary>
        ''' Records the fallback service's state into logEntry and puts a disabled one back to
        ''' Start = 1 (System), its inbox value. Returns True when the entry deserves a warning.
        ''' </summary>
        Private Function CheckFallbackService(ByVal name As String, ByVal logEntry As LogEntry) As Boolean
            Const SERVICE_SYSTEM_START As Integer = 1
            Const SERVICE_DISABLED As Integer = 4
            Dim label As String = "service " & name

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & name, True)
                    If regkey Is Nothing Then
                        logEntry.Add(label, "MISSING - DDU cannot recreate it")
                        Return True
                    End If

                    Dim raw As Object = regkey.GetValue("Start", Nothing)

                    If raw Is Nothing Then
                        regkey.SetValue("Start", SERVICE_SYSTEM_START, RegistryValueKind.DWord)
                        logEntry.Add(label, String.Format("no Start value -> SET to {0} (System)", SERVICE_SYSTEM_START))
                        Return True
                    End If

                    Dim startValue As Integer = CInt(raw)

                    If startValue <> SERVICE_DISABLED Then
                        logEntry.Add(label, String.Format("enabled (Start = {0})", startValue))
                        Return False
                    End If

                    regkey.SetValue("Start", SERVICE_SYSTEM_START, RegistryValueKind.DWord)

                    logEntry.Add(label, String.Format("DISABLED (Start = {0}) -> RE-ENABLED (Start = {1}, System)", startValue, SERVICE_SYSTEM_START))
                    Return True
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Fallback display: could not check " & label)
                logEntry.Add(label, "check FAILED : " & ex.Message)
                Return True
            End Try
        End Function

        Private Sub OnCLSIDLeftoverRemoval(ByVal child As String)
            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\CLSID", True), child, False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        Private Sub OnCLSIDLeftoverRemovalWOW6432(ByVal child As String)
            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1), False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            Try
                Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\Wow6432Node\CLSID", True), child, False)
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        Public Sub Clsidleftover(ByVal clsidleftover As String(), config As ThreadSettings)

            Dim wantedvalue As String
            Dim wantedvalue2 As String
            Dim appid As String = Nothing
            Dim typelib As String = Nothing
            Dim childlist As New List(Of String)
            Dim typelibList As New List(Of String)

            Application.Log.AddMessage("Begin clsidleftover CleanUP")

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, clsidleftover) Then
                                Try
                                    appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                    If Not String.IsNullOrWhiteSpace(appid) Then
                                        Try
                                            Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid, False)
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
                                    End If

                                    Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                        If subregkey2 IsNot Nothing Then
                                            typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(typelib) Then
                                                Try
                                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib, False)
                                                    typelibList.Add(typelib)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
                                    End Using

                                    Deletesubregkey(regkey, child)
                                    OnCLSIDLeftoverRemoval(child)
                                    childlist.Add(child)
                                    Continue For
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If

                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\InProcServer32", False)
                                If subregkey IsNot Nothing Then
                                    wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                    If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                        If config.SelectedGPU = GPUVendor.Nvidia AndAlso config.NVIDIA_App_Installed AndAlso (Not config.RemoveGFE) AndAlso StrContainsAny(wantedvalue, True, "nvui") Then Continue For

                                        If StrContainsAny(wantedvalue, True, clsidleftover) Then

                                            appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(appid) Then
                                                Try
                                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If

                                            Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                                If subregkey2 IsNot Nothing Then
                                                    typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                                    If Not String.IsNullOrWhiteSpace(typelib) Then
                                                        Try
                                                            Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib, False)
                                                            typelibList.Add(typelib)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End If
                                            End Using

                                            'specific to the NVIDIA Broadcast. Could apply to more in the future.
                                            'Try
                                            '	Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}\Instance", True), child)
                                            'Catch exARG As ArgumentException
                                            '	'Do nothing, can happen (Not found)
                                            'Catch ex As Exception
                                            '	Application.Log.AddException(ex)
                                            'End Try

                                            'here I remove the mediafoundationkeys if present
                                            'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                            OnCLSIDLeftoverRemoval(child)

                                            Try
                                                Deletesubregkey(regkey, child)
                                                childlist.Add(child)
                                                Continue For
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                End If
                            End Using

                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                If subregkey IsNot Nothing Then
                                    wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                    If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                        If StrContainsAny(wantedvalue, True, clsidleftover) Then

                                            appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(appid) Then
                                                Try
                                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If

                                            Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                                If subregkey2 IsNot Nothing Then
                                                    typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                                    If Not String.IsNullOrWhiteSpace(typelib) Then
                                                        Try
                                                            Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib, False)
                                                            typelibList.Add(typelib)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End If
                                            End Using

                                            'here I remove the mediafoundationkeys if present
                                            'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                            OnCLSIDLeftoverRemoval(child)

                                            Try
                                                Deletesubregkey(regkey, child)
                                                childlist.Add(child)
                                                Continue For
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                End If

                                Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "LocalServer32", False)
                                    If subregkey2 IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey2.GetValue("", String.Empty), String)
                                        wantedvalue2 = TryCast(subregkey2.GetValue("ServerExecutable", String.Empty), String)  'Intel specific workaround "igfxext.exe"
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) OrElse Not String.IsNullOrWhiteSpace(wantedvalue2) Then

                                            If StrContainsAny(wantedvalue, True, clsidleftover) OrElse StrContainsAny(wantedvalue2, True, clsidleftover) Then

                                                appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                                If Not String.IsNullOrWhiteSpace(appid) Then
                                                    Try
                                                        Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If

                                                Using subregkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                                    If subregkey3 IsNot Nothing Then
                                                        typelib = TryCast(subregkey3.GetValue("", String.Empty), String)
                                                        If Not String.IsNullOrWhiteSpace(typelib) Then
                                                            Try
                                                                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
                                                                typelibList.Add(typelib)
                                                            Catch exARG As ArgumentException
                                                                'Do nothing, can happen (Not found)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End If
                                                End Using

                                                'here I remove the mediafoundationkeys if present
                                                'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                OnCLSIDLeftoverRemoval(child)

                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    childlist.Add(child)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try

                                            End If
                                        End If
                                    End If
                                End Using
                            End Using
                        Next
                        InterfaceRemovalWithValue(childlist, typelibList, False)
                        RemoveCLSIDInstance(childlist, False)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            childlist.Clear()
            'I do not clear the typelib because when the nomal typelib is removed, the WOW6432Node typelib also get removed by windows.

            If IntPtr.Size = 8 Then
                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For

                                If StrContainsAny(child, True, clsidleftover) Then
                                    Try
                                        appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                        If Not String.IsNullOrWhiteSpace(appid) Then
                                            Try
                                                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid, False)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If

                                        Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                            If subregkey2 IsNot Nothing Then
                                                typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                                If Not String.IsNullOrWhiteSpace(typelib) Then
                                                    Try
                                                        Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib, False)
                                                        typelibList.Add(typelib)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End If
                                        End Using

                                        Deletesubregkey(regkey, child)
                                        OnCLSIDLeftoverRemovalWOW6432(child)
                                        childlist.Add(child)
                                        Continue For
                                    Catch ex As Exception
                                        Application.Log.AddException(ex)
                                    End Try
                                End If

                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\InProcServer32", False)
                                    If subregkey IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                            If StrContainsAny(wantedvalue, True, clsidleftover) Then

                                                appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                                If Not String.IsNullOrWhiteSpace(appid) Then
                                                    Try
                                                        Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If

                                                Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                                    If subregkey2 IsNot Nothing Then
                                                        typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                                        If Not String.IsNullOrWhiteSpace(typelib) Then
                                                            Try
                                                                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib, False)
                                                                typelibList.Add(typelib)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End If
                                                End Using

                                                'specific to the NVIDIA Broadcast. Could apply to more in the future.
                                                'Try
                                                '	Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}\Instance", True), child)
                                                'Catch exARG As ArgumentException
                                                '	'Do nothing, can happen (Not found)
                                                'Catch ex As Exception
                                                '	Application.Log.AddException(ex)
                                                'End Try

                                                'here I remove the mediafoundationkeys if present
                                                'f79eac7d-e545-4387-bdee-d647d7bde42a is the Encoder section. Same on all windows version.

                                                OnCLSIDLeftoverRemovalWOW6432(child)

                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    childlist.Add(child)
                                                    Continue For
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try

                                            End If
                                        End If
                                    End If
                                End Using

                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child, False)
                                    If subregkey IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                            If StrContainsAny(wantedvalue, True, clsidleftover) Then

                                                appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                                If Not String.IsNullOrWhiteSpace(appid) Then
                                                    Try
                                                        Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If

                                                Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "TypeLib")
                                                    If subregkey2 IsNot Nothing Then
                                                        typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
                                                        If Not String.IsNullOrWhiteSpace(typelib) Then
                                                            Try
                                                                Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib, False)
                                                                typelibList.Add(typelib)
                                                            Catch ex As Exception
                                                                Application.Log.AddException(ex)
                                                            End Try
                                                        End If
                                                    End If
                                                End Using


                                                'here I remove the mediafoundationkeys if present
                                                'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

                                                OnCLSIDLeftoverRemovalWOW6432(child)

                                                Try
                                                    Deletesubregkey(regkey, child)
                                                    childlist.Add(child)
                                                    Continue For
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try

                                            End If
                                        End If
                                    End If

                                    Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "LocalServer32", False)
                                        If subregkey2 IsNot Nothing Then
                                            wantedvalue = TryCast(subregkey2.GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                                If StrContainsAny(wantedvalue, True, clsidleftover) Then
                                                    appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
                                                    If Not String.IsNullOrWhiteSpace(appid) Then
                                                        Try
                                                            Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid, False)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If

                                                    Using subregkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
                                                        If subregkey3 IsNot Nothing Then
                                                            typelib = TryCast(subregkey3.GetValue("", String.Empty), String)
                                                            If Not String.IsNullOrWhiteSpace(typelib) Then
                                                                Try
                                                                    Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib, False)
                                                                    typelibList.Add(typelib)
                                                                Catch ex As Exception
                                                                    Application.Log.AddException(ex)
                                                                End Try
                                                            End If
                                                        End If
                                                    End Using


                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

                                                    OnCLSIDLeftoverRemovalWOW6432(child)

                                                    Try
                                                        Deletesubregkey(regkey, child)
                                                        childlist.Add(child)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try

                                                End If
                                            End If
                                        End If
                                    End Using
                                End Using
                            Next
                            InterfaceRemovalWithValue(childlist, typelibList, True)
                            RemoveCLSIDInstance(childlist, True)
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
            End If

            childlist.Clear()
            typelibList.Clear()

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, clsidleftover) Then
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                    If subregkey IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey.GetValue("AppID", String.Empty), String)
                                        If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                            Try
                                                Deletesubregkey(regkey, wantedvalue, False)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        Else
                                            wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                Try
                                                    Deletesubregkey(regkey, wantedvalue, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
                                        Try
                                            Deletesubregkey(regkey, child)
                                            Continue For
                                        Catch ex As Exception
                                            Application.Log.AddException(ex)
                                        End Try
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
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrWhiteSpace(child) Then Continue For
                                If StrContainsAny(child, True, clsidleftover) Then
                                    Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                        If subregkey IsNot Nothing Then
                                            wantedvalue = TryCast(subregkey.GetValue("AppID", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then

                                                Try
                                                    Deletesubregkey(regkey, wantedvalue, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If

                                            wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                            If Not String.IsNullOrWhiteSpace(wantedvalue) Then
                                                Try
                                                    Deletesubregkey(regkey, wantedvalue, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                            Try
                                                Deletesubregkey(regkey, child)
                                                Continue For
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
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

            'clean orphan typelib.....
            Application.Log.AddMessage("Orphan cleanUp")
            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True)
                    If regkey IsNot Nothing Then
                        Parallel.ForEach(regkey.GetSubKeyNames(),
                        Sub(child)
                            If String.IsNullOrWhiteSpace(child) Then Return
                            Dim value As String = Nothing
                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                If regkey2 Is Nothing Then Return

                                For Each child2 As String In regkey2.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child2) Then Continue For

                                    Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
                                        If regkey3 Is Nothing Then Continue For

                                        For Each child3 As String In regkey3.GetSubKeyNames()
                                            If String.IsNullOrWhiteSpace(child3) Then Continue For

                                            Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey3, child3)
                                                If regkey4 Is Nothing Then Continue For

                                                For Each child4 As String In regkey4.GetSubKeyNames()
                                                    If String.IsNullOrWhiteSpace(child4) Then Continue For

                                                    Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkey4, child4)
                                                        If regkey5 Is Nothing Then Continue For

                                                        value = TryCast(regkey5.GetValue("", String.Empty), String)    'Can also be UInt32 btw! (Usualy abnormal from personal experience,but still should be managed in the future)

                                                        If String.IsNullOrWhiteSpace(value) Then Continue For

                                                        For Each clsIdle As String In clsidleftover
                                                            If String.IsNullOrWhiteSpace(clsIdle) Then Continue For

                                                            If StrContainsAny(value, True, clsIdle) Then
                                                                Try
                                                                    Deletesubregkey(regkey, child)
                                                                    SyncLock _listLock
                                                                        typelibList.Add(child)
                                                                    End SyncLock
                                                                    Application.Log.AddMessage(child + " for " + clsIdle)
                                                                    Exit For
                                                                Catch exARG As ArgumentException
                                                                    'Do nothing, can happen (Not found)
                                                                Catch ex As Exception
                                                                    Application.Log.AddException(ex)
                                                                End Try
                                                            End If
                                                        Next
                                                    End Using
                                                Next
                                            End Using
                                        Next
                                    End Using
                                Next
                            End Using
                        End Sub)
                        InterfaceRemovalWithValue(Nothing, typelibList, False)

                        If IntPtr.Size = 8 Then
                            InterfaceRemovalWithValue(Nothing, typelibList, True)
                        End If
                        typelibList.Clear()
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
            Application.Log.AddMessage("End Orphan cleanUp")
            Application.Log.AddMessage("End clsidleftover CleanUP")
        End Sub

        Private Sub RemoveCLSIDInstance(ByVal childlist As List(Of String), ByVal wow6432 As Boolean)
            If Not wow6432 Then
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Instance") Then
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                    If regkey2 Is Nothing Then Continue For
                                    If childlist IsNot Nothing AndAlso childlist.Count > 0 Then
                                        For Each clsid As String In childlist
                                            For Each child2 As String In regkey2.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(child2) Then Continue For

                                                If StrContainsAny(child2, True, clsid) Then
                                                    Try
                                                        Deletesubregkey(regkey2, child2)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            Next
                                        Next
                                    End If
                                    If regkey2.SubKeyCount = 0 Then
                                        Deletesubregkey(regkey, child)
                                        Exit For
                                    End If
                                End Using
                            End If
                        Next
                    End If
                End Using
            Else
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, "Instance") Then
                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child, True)
                                    If regkey2 Is Nothing Then Continue For
                                    If childlist IsNot Nothing AndAlso childlist.Count > 0 Then
                                        For Each clsid As String In childlist
                                            For Each child2 As String In regkey2.GetSubKeyNames()
                                                If String.IsNullOrWhiteSpace(child2) Then Continue For
                                                If StrContainsAny(child2, True, clsid) Then
                                                    Try
                                                        Deletesubregkey(regkey2, child2)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            Next
                                        Next
                                    End If
                                    If regkey2.SubKeyCount = 0 Then
                                        Deletesubregkey(regkey, child)
                                        Exit For
                                    End If
                                End Using
                            End If
                        Next
                    End If
                End Using
            End If
        End Sub

        Public Sub Interfaces(ByVal interfaces As String())

            Application.Log.AddMessage("Start Interface CleanUP")

            Try
                Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface", True)
                    If regkey IsNot Nothing Then
                        Parallel.ForEach(regkey.GetSubKeyNames(),
                        Sub(child)
                            If String.IsNullOrWhiteSpace(child) Then Return
                            Dim wantedvalue As String
                            Dim typelib As String = Nothing
                            Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface\" & child, False)

                                If subregkey IsNot Nothing Then
                                    wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                    If String.IsNullOrWhiteSpace(wantedvalue) Then Return

                                    For Each iface As String In interfaces
                                        If String.IsNullOrWhiteSpace(iface) Then Continue For

                                        If StrContainsAny(wantedvalue, True, iface) Then

                                            Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
                                                If regkey2 IsNot Nothing Then
                                                    typelib = TryCast(regkey2.GetValue("", String.Empty), String)
                                                    If String.IsNullOrWhiteSpace(typelib) Then Continue For

                                                    Try
                                                        Deletesubregkey(regkey2, typelib, False)
                                                    Catch ex As Exception
                                                        Application.Log.AddException(ex)
                                                    End Try
                                                End If
                                            End Using

                                            Try
                                                Deletesubregkey(regkey, child, False)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    Next
                                End If
                            End Using
                        End Sub)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try

            If IntPtr.Size = 8 Then

                Try
                    Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface", True)
                        If regkey IsNot Nothing Then
                            Parallel.ForEach(regkey.GetSubKeyNames(),
                            Sub(child)
                                If String.IsNullOrWhiteSpace(child) Then Return
                                Dim wantedvalue As String
                                Dim typelib As String = Nothing
                                Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface\" & child, False)

                                    If subregkey IsNot Nothing Then
                                        wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
                                        If String.IsNullOrWhiteSpace(wantedvalue) Then Return

                                        For Each iface As String In interfaces
                                            If String.IsNullOrWhiteSpace(iface) Then Continue For

                                            If StrContainsAny(wantedvalue, True, iface) Then

                                                Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
                                                    If regkey2 IsNot Nothing Then
                                                        typelib = TryCast(regkey2.GetValue("", String.Empty), String)
                                                        If String.IsNullOrWhiteSpace(typelib) Then Continue For

                                                        Try
                                                            Deletesubregkey(regkey2, typelib, False)
                                                        Catch ex As Exception
                                                            Application.Log.AddException(ex)
                                                        End Try
                                                    End If
                                                End Using

                                                Try
                                                    Deletesubregkey(regkey, child, False)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        Next
                                    End If
                                End Using
                            End Sub)
                        End If
                    End Using
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try

            End If

            Application.Log.AddMessage("END Interface CleanUP")
        End Sub

        Public Sub Folderscleanup(ByVal driverfiles As String())
            Dim winxp = FrmMain.IsWindowsXp

            Dim TaskList = New List(Of Task)()

            'tasks.Add(Threading.Task(Of Integer).Factory.StartNew(Sub() Threaddata1(Thread1Finished, Application.Paths.System32, driverfiles)))

            Dim thread1 As Task = Task.Run(Sub() Threaddata1(Application.Paths.System32, driverfiles))

            TaskList.Add(thread1)

            Dim thread2 As Task = Task.Run(Sub() Threaddata1(Application.Paths.System32 & "drivers\", driverfiles))

            TaskList.Add(thread2)

            If winxp Then

                Dim thread3 As Task = Task.Run(Sub() Threaddata1(Application.Paths.System32 & "drivers\dllcache\", driverfiles))
                TaskList.Add(thread3)

            End If

            Dim thread4 As Task = Task.Run(Sub() Threaddata1(Application.Paths.WinDir, driverfiles))
            TaskList.Add(thread4)

            If IntPtr.Size = 8 Then


                Dim thread8 As Task = Task.Run(Sub() Threaddata1(Application.Paths.SysWOW64, driverfiles))
                TaskList.Add(thread8)

                Dim thread5 As Task = Task.Run(Sub() Threaddata1(Application.Paths.SysWOW64 & "Drivers\", driverfiles))
                TaskList.Add(thread5)
            End If

            Dim thread7 As Task = Task.Run(Sub() Threaddata1Prefetch(Application.Paths.WinDir & "Prefetch\", driverfiles))
            TaskList.Add(thread7)

            Task.WaitAll(TaskList.ToArray())

            'While Thread1Finished <> True Or Thread2Finished <> True Or Thread3Finished <> True Or Thread4Finished <> True Or Thread5Finished <> True Or Thread7Finished <> True Or Thread8Finished <> True
            '	objAuto.WaitOne(500)
            'End While

        End Sub

        Private Sub Threaddata1(filepath As String, ByVal driverfiles As String())
            ImpersonateUser.RunImpersonatedSystem(
            Sub()
                Dim FileIO As New FileIO
                If filepath IsNot Nothing Then
                    If FileIO.ExistsDir(filepath) Then
                        For Each driverfile As String In driverfiles
                            If String.IsNullOrWhiteSpace(driverfile) Then Continue For
                            If FileIO.ExistsFile(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile)) Then
                                Delete(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile))
                                If Not FileIO.ExistsFile(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile)) Then
                                    RemoveSharedDlls(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile))
                                End If
                            End If
                        Next
                    End If
                End If
            End Sub)
        End Sub

        Private Sub Threaddata1Prefetch(filepath As String, ByVal driverfiles As String())
            ImpersonateUser.RunImpersonatedSystem(
            Sub()
                Dim FileIO As New FileIO
                If filepath IsNot Nothing Then
                    If FileIO.ExistsDir(filepath) Then
                        For Each child As String In FileIO.GetFiles(filepath)
                            If String.IsNullOrWhiteSpace(child) Then Continue For
                            If StrContainsAny(child, True, driverfiles) Then
                                Try
                                    Delete(child)
                                Catch ex As Exception
                                    Application.Log.AddException(ex)
                                End Try
                            End If
                        Next
                    End If
                    For Each driverfile As String In driverfiles
                        If String.IsNullOrWhiteSpace(driverfile) Then Continue For
                        If Not FileIO.ExistsFile(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile)) Then
                            RemoveSharedDlls(filepath & If(driverfile.StartsWith("\"), driverfile.Substring(1), driverfile))
                        End If
                    Next
                End If
            End Sub)
        End Sub

        Public Sub TestDelete(ByVal folder As String, config As ThreadSettings)
            ' UpdateTextMethod(UpdateTextMethodmessagefn("18"))
            'Application.Log.AddMessage("Deleting some specials folders, it could take some times...")
            'ensure that this folder can be accessed with current user ac.
            If Not Directory.Exists(folder) Then
                Exit Sub
            End If

            'Get an object repesenting the directory path below
            Dim di As New DirectoryInfo(folder)

            'Traverse all of the child directors in the root; get to the lowest child
            'and delete all files, working our way back up to the top.  All files
            'must be deleted in the directory, before the directory itself can be deleted.
            'also if there is hidden / readonly / system attribute..  change those attribute.
            Try


                For Each diChild As DirectoryInfo In di.GetDirectories()
                    diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
                    diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
                    diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
                    If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

                        Try
                            TraverseDirectory(diChild, config)
                        Catch ex As Exception
                            Application.Log.AddException(ex)
                        End Try
                    End If
                Next
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
            'Finally, clean all of the files directly in the root directory
            CleanAllFilesInDirectory(di)

            'The containing directory can only be deleted if the directory
            'is now completely empty and all files previously within
            'were deleted.
            Try
                If di.GetFiles().Length = 0 And Directory.GetDirectories(folder).Length = 0 Then
                    di.Delete()
                    Application.Log.AddMessage(di.ToString + " - " + "Folder removed via testdelete sub")
                End If
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
            RemoveSharedDlls(folder)
        End Sub
        Private Sub TraverseDirectory(ByVal di As DirectoryInfo, ByVal config As ThreadSettings)

            'If the current directory has more child directories, then continure
            'to traverse down until we are at the lowest level and remove
            'there hidden / readonly / system attribute..  At that point all of the
            'files will be deleted.
            For Each diChild As DirectoryInfo In di.GetDirectories()
                diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
                diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
                diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
                If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

                    Try
                        TraverseDirectory(diChild, config)
                    Catch ex As Exception
                        Application.Log.AddException(ex)
                    End Try
                End If
            Next

            'Now that we have no more child directories to traverse, delete all of the files
            'in the current directory, and then delete the directory itself.
            CleanAllFilesInDirectory(di)


            'The containing directory can only be deleted if the directory
            'is now completely empty and all files previously within
            'were deleted.
            If di.GetFiles().Length = 0 Then
                Try
                    di.Delete()
                Catch ex As Exception
                    Application.Log.AddException(ex)
                End Try
            End If

        End Sub

        ''' Iterates through all files in the directory passed into
        ''' method and deletes them.
        ''' It may be necessary to wrap this call in impersonation or ensure parent directory
        ''' permissions prior, because delete permissions are not guaranteed.

        Private Sub CleanAllFilesInDirectory(ByVal DirectoryToClean As DirectoryInfo)

            Try
                For Each fi As FileInfo In DirectoryToClean.GetFiles()
                    'The following code is NOT required, but shows how some logic can be wrapped
                    'around the deletion of files.  For example, only delete files with
                    'a creation date older than 1 hour from the current time.  If you
                    'always want to delete all of the files regardless, just remove
                    'the next 'If' statement.

                    'Read only files can not be deleted, so mark the attribute as 'IsReadOnly = False'

                    Try
                        fi.IsReadOnly = False
                    Catch ex As Exception
                    End Try

                    Try
                        fi.Delete()
                    Catch ex As Exception
                    End Try
                    'On a rare occasion, files being deleted might be slower than program execution, and upon returning
                    'from this call, attempting to delete the directory will throw an exception stating it is not yet
                    'empty, even though a fraction of a second later it actually is.  Therefore the 'Optional' code below
                    'can stall the process just long enough to ensure the file is deleted before proceeding. The value
                    'can be adjusted as needed from testing and running the process repeatedly.
                    'System.Threading.Thread.sleep(10)  '50 millisecond stall (0.025 Seconds)

                Next
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End Sub

        Private Sub Delete(ByVal filename As String)
            Dim FileIO As New FileIO
            If FileIO.ExistsFile(filename) OrElse FileIO.ExistsDir(filename) Then
                FileIO.Delete(filename)
            End If
        End Sub

        Public Sub Cleandriverstore(ByVal config As ThreadSettings)
            Dim FileIO As New FileIO
            Dim catalog As String = ""
            Dim driverfiles As String() = Nothing
            Dim CurrentProvider As String() = Nothing

            UpdateTextMethod("Executing Driver Store cleanUP(finding OEM step)...")
            Application.Log.AddMessage("Executing Driver Store cleanUP(Find OEM)...")
            'Check the driver from the driver store  ( oemxx.inf)

            UpdateTextMethod(UpdateTextTranslated(0))

            Select Case config.SelectedType
                Case CleanType.GPU
                    Select Case config.SelectedGPU
                        Case GPUVendor.Nvidia
                            CurrentProvider = {"NVIDIA"}
                            driverfiles = {"nvlddmkm.sys", "nvhda64v", "UcmCxUcsiNvppc.sys", "nvvad64v.sys", "NVSWCFilter64.sys", "nvrtxvad", "nvvad64v.sys", "nvvhci.sys", "nvrtxvad64v.sys", "nvpcf.sys"}
                        Case GPUVendor.AMD
                            CurrentProvider = {"Advanced Micro Devices", "atitech", "advancedmicrodevices", "ati tech", "amd"}
                            driverfiles = {"amdkmdag.sys", "amdxe.sys", "amdfendrmgr", "AtihdWT6.sys", "amdsafd.sys", "amdkmpfd", "amdocl32", "amdocl64", "AMDNoiseSuppression", "amdsdws.sys"}
                        Case GPUVendor.Intel
                            CurrentProvider = {"Intel"}
                            driverfiles = {"igdkmd64.sys", "igdkmdn64.sys", "igdkmdnd64.sys", "IntcDAud.sys", "intelaud.sys", "iwdbus.sys", "GSCAuxDriverx64.sys", "TeeDriverGSCW8x64.sys", "MiniCtaDriver.sys", "IntcDAudD.sys", "IntelGraphicsAGS.exe", "CtaChildDriver.sys", "Intel_NF_I2C.sys", "PmtChildDriver.sys"}
                        Case GPUVendor.Lisuan
                            CurrentProvider = {"Lisuan"}    'INF Provider = "Shanghai Lisuan Semiconductor Co.,Ltd."
                            driverfiles = {"LSGKMD.sys", "LSGDDM.sys"}
                        Case GPUVendor.None
                            CurrentProvider = {"None"}
                            driverfiles = Nothing
                    End Select
                Case CleanType.Audio
                    Select Case config.SelectedAUDIO
                        Case AudioVendor.Realtek
                            CurrentProvider = {"Realtek"}
                            driverfiles = {"RTKVHD64.sys"}
                        Case AudioVendor.SoundBlaster
                            CurrentProvider = {"Creative"} 'Not verified.
                            driverfiles = {"cthda.sys", "cthdb.sys"}
                        Case AudioVendor.None
                            CurrentProvider = {"None"}
                            driverfiles = Nothing
                    End Select
                Case CleanType.None
                    CurrentProvider = {"None"}
                    Application.Log.AddWarningMessage("CleanType is none, it is unexpected")
            End Select

            For Each oem As Inf In GetOemInfList(Application.Paths.WinDir & "inf\")
                If Not oem.IsValid Then
                    Continue For
                End If

                If StrContainsAny(oem.Provider, True, CurrentProvider) Then
                    'before removing the oem we try to get the original inf name (win8+)
                    If FrmMain.IsWindows8OrHigher Then
                        If MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName) IsNot Nothing Then
                            Try
                                catalog = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName).GetValue("Active").ToString
                                catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
                            Catch ex As Exception
                                catalog = ""
                            End Try
                        Else
                            catalog = ""
                        End If
                    End If

                    If StrContainsAny(oem.Class, True, "display", "media", "extension", "softwarecomponent", "CTA Driver Devices", "system", "SoftwareDevice", "USB") Then
                        If Not ((Not config.RemoveNVBROADCAST AndAlso StrContainsAny(oem.Catalog, True, "nvrtxvad")) Or (Not config.RemoveGFE AndAlso StrContainsAny(oem.Catalog, True, "nvvad")) Or (Not config.RemoveGFE AndAlso StrContainsAny(oem.Catalog, True, "nvswcfilter")) Or ((Not config.RemoveAMDKMPFD Or Not config.NotPresentAMDKMPFD) AndAlso StrContainsAny(oem.Catalog, True, "amdkmpfd"))) Then
                            If StrContainsAny(oem.Class, True, "Extension") AndAlso StrContainsAny(oem.Catalog, True, "extinf.cat", "amdpcibridgeextension.cat", "igdlh.cat") Then
                                SetupAPI.RemoveInf(oem, False)
                                Continue For
                            End If

                            If config.SelectedType = CleanType.GPU AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                If StrContainsAny(oem.Class, True, "softwarecomponent", "system") AndAlso StrContainsAny(oem.Catalog, True, "igdlh.cat", "Intel_NF_I2C.cat", "IntelGFXFwUpdate.cat", "HdBusExt.cat") Then
                                    SetupAPI.RemoveInf(oem, False)
                                    Continue For
                                End If
                            End If

                            If config.SelectedType = CleanType.GPU AndAlso config.SelectedGPU = GPUVendor.AMD Then
                                If StrContainsAny(oem.Class, True, "softwarecomponent", "system") AndAlso StrContainsAny(oem.Catalog, True, "amdvlk.cat", "amdogl.cat", "amdocl.cat", "amdwin-u", "amdfdans.cat") Then
                                    SetupAPI.RemoveInf(oem, False)
                                    Continue For
                                End If
                            End If

                            'When forcing the removal of an extension it is better to disable the devices that are associated with them before removing the inf.

                            If config.SelectedType = CleanType.GPU AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                If StrContainsAny(oem.Class, True, "Extension") AndAlso StrContainsAny(oem.Catalog, True, "HdBusExt.cat") Then
                                    Dim oemRemoved = False
                                    'For some special cases, we need to disable the device before removing the inf.
                                    Dim audiobusList As List(Of SetupAPI.Device) = SetupAPI.GetDevicesByCompatibleID("PCI\VEN_8086&CC_040", False, False, False, True)
                                    If audiobusList IsNot Nothing AndAlso audiobusList.Count > 0 Then
                                        Dim disabledAudiobusList As New List(Of SetupAPI.Device)
                                        For Each audiobus As SetupAPI.Device In audiobusList
                                            If audiobus IsNot Nothing AndAlso audiobus.ExtendedInfs IsNot Nothing AndAlso
                                                audiobus.ExtendedInfs.Length > 0 AndAlso Not String.IsNullOrWhiteSpace(audiobus.Service) Then
                                                If StrContainsAny(audiobus.Service, True, "HDAudBus", "IntcAudioBus") Then
                                                    If audiobus.IsPresent Then
                                                        SetupAPI.EnableDevice(audiobus, False) 'Removing the Audio bus.
                                                        disabledAudiobusList.Add(audiobus)
                                                    End If
                                                End If
                                            End If
                                        Next

                                        SetupAPI.RemoveInf(oem, True)
                                        oemRemoved = True
                                        If disabledAudiobusList IsNot Nothing AndAlso disabledAudiobusList.Count > 0 Then
                                            For Each disabledAudiobus As SetupAPI.Device In disabledAudiobusList
                                                If disabledAudiobus IsNot Nothing Then
                                                    SetupAPI.EnableDevice(disabledAudiobus, True)
                                                End If
                                            Next
                                        End If
                                    End If

                                    If Not oemRemoved Then
                                        SetupAPI.RemoveInf(oem, False)
                                    End If
                                    Continue For
                                End If
                            End If

                            If config.SelectedType = CleanType.GPU AndAlso config.SelectedGPU = GPUVendor.Intel Then
                                If StrContainsAny(oem.Class, True, "media") AndAlso StrContainsAny(oem.Catalog, True, "AcxDAC.cat") Then
                                    SetupAPI.RemoveInf(oem, False)
                                    Continue For
                                End If
                            End If

                            For Each SourceDisksName In oem.SourceDisksFiles
                                If String.IsNullOrWhiteSpace(SourceDisksName) Then Continue For
                                If SourceDisksName IsNot Nothing AndAlso StrContainsAny(SourceDisksName, True, driverfiles) Then
                                    SetupAPI.RemoveInf(oem, False)
                                    Exit For
                                End If
                            Next
                        End If
                        'Else
                        '	If Not StrContainsAny(oem.Class, True, "HDC") Then 'we dont want to ever remove an HDC class device or info.
                        '		SetupAPI.RemoveInf(oem, False)
                        '	End If
                    End If
                End If
                'check if the oem was removed to process to the pnplockdownfile if necessary
                If FrmMain.IsWindows8OrHigher AndAlso (Not FileIO.ExistsFile(oem.FileName)) AndAlso (Not String.IsNullOrWhiteSpace(catalog)) Then
                    If (config.RemoveAudioBus = False OrElse FrmMain.DoNotRemoveAmdHdAudioBusFiles) AndAlso StrContainsAny(catalog, True, "amdkmafd") Then Continue For
                    PrePnplockdownfiles(catalog)
                End If
            Next

            UpdateTextMethod("Driver Store cleanUP complete.")

            Application.Log.AddMessage("Driver Store CleanUP Complete.")
        End Sub

        Public Sub Fixregistrydriverstore(ByVal config As ThreadSettings)
            Dim win8higher As Boolean = FrmMain.IsWindows8OrHigher
            Dim FileIO As New FileIO

            ImpersonateUser.RunImpersonatedSystem(
            Sub()

                'Windows 8 + only
                'This should fix driver installation problem reporting that a file is not found.
                'It is usually caused by Windows somehow losing track of the driver store , This intend to help it a bit.
                If win8higher Then
                    Dim FilePath As String = Nothing
                    Application.Log.AddMessage("Fixing registry driverstore if necessary")
                    Try

                        Dim infslist As String = ""
                        For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", Microsoft.VisualBasic.FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")
                            If Not String.IsNullOrWhiteSpace(infs) Then
                                infslist += infs
                            End If
                        Next
                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverInfFiles", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    If child.ToLower.StartsWith("oem") AndAlso child.ToLower.EndsWith(".inf") Then
                                        If Not StrContainsAny(infslist, True, child) Then
                                            If Not String.IsNullOrWhiteSpace(MyRegistry.OpenSubKey(regkey, child).GetValue("", String.Empty).ToString) Then
                                                Try
                                                    Deletesubregkey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages\" & MyRegistry.OpenSubKey(regkey, child).GetValue("", String.Empty).ToString)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
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

                        Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrWhiteSpace(child) Then Continue For

                                    Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
                                        If regkey2 IsNot Nothing Then
                                            If (Not String.IsNullOrWhiteSpace(regkey2.GetValue("", String.Empty).ToString)) AndAlso
                                 regkey2.GetValue("", String.Empty).ToString.ToLower.StartsWith("oem") AndAlso
                                 regkey2.GetValue("", String.Empty).ToString.ToLower.EndsWith(".inf") AndAlso
                                 (Not StrContainsAny(infslist, True, regkey2.GetValue("", String.Empty).ToString)) Then
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

                    'Cleaning of possible left-overs %windir%\system32\driverstore\filerepository
                    Select Case config.SelectedGPU
                        Case GPUVendor.Lisuan
                            FilePath = System.Environment.SystemDirectory & "\DriverStore\FileRepository"
                            If String.IsNullOrWhiteSpace(FilePath) = False Then
                                For Each child As String In FileIO.GetDirectories(FilePath)
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        Dim dirinfo As New System.IO.DirectoryInfo(child)
                                        If StrContainsAny(dirinfo.Name, True, "ls_universial64.inf") Then
                                            Try
                                                Delete(child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                Next
                            End If
                        Case GPUVendor.AMD
                            FilePath = System.Environment.SystemDirectory & "\DriverStore\FileRepository"
                            If String.IsNullOrWhiteSpace(FilePath) = False Then
                                For Each child As String In FileIO.GetDirectories(FilePath)
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        Dim dirinfo As New System.IO.DirectoryInfo(child)
                                        If dirinfo.Name.ToLower.StartsWith("c030") Or
                                 StrContainsAny(dirinfo.Name, True, "atihdwt6.inf") Or
                                 (config.RemoveAudioBus AndAlso FrmMain.DoNotRemoveAmdHdAudioBusFiles = False) AndAlso StrContainsAny(dirinfo.Name, True, "amdkmafd.inf") Then
                                            Try
                                                Delete(child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                    End If
                                Next
                            End If
                        Case GPUVendor.Nvidia
                            FilePath = System.Environment.SystemDirectory & "\DriverStore\FileRepository"
                            If String.IsNullOrWhiteSpace(FilePath) = False Then
                                For Each child As String In FileIO.GetDirectories(FilePath)
                                    If String.IsNullOrWhiteSpace(child) = False Then
                                        Dim dirinfo As New System.IO.DirectoryInfo(child)
                                        If StrContainsAny(dirinfo.Name, True, "nvstusb.inf", "nvhda.inf", "nv_dispi.inf") Then
                                            Try
                                                Delete(child)
                                            Catch ex As Exception
                                                Application.Log.AddException(ex)
                                            End Try
                                        End If
                                        If config.RemoveGFE Then
                                            If StrContainsAny(dirinfo.Name, True, "nvvad.inf", "nvswcfilter.inf") Then
                                                Try
                                                    Delete(child)
                                                Catch ex As Exception
                                                    Application.Log.AddException(ex)
                                                End Try
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                    End Select
                End If

            End Sub)

        End Sub

        Private Sub UpdateTextMethod(ByVal strMessage As String)
            FrmMain.UpdateTextMethod(strMessage)
        End Sub

        Private Function UpdateTextTranslated(ByVal number As Integer) As String
            Return FrmMain.UpdateTextTranslated(number)
        End Function

    End Class
End Namespace
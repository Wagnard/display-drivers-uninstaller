﻿'    Display driver Uninstaller (DDU) a driver uninstaller / Cleaner for Windows
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

Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading
Imports System.Security.Principal
Imports System.Management
Imports System.Runtime.InteropServices


Public Class Form1

    Dim MyIdentity As WindowsIdentity = WindowsIdentity.GetCurrent()
    Dim checkvariables As New checkvariables
    Dim identity = WindowsIdentity.GetCurrent()
    Dim principal = New WindowsPrincipal(identity)
    Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)
    Dim processinfo As New ProcessStartInfo
    Dim process As New Process
    Dim vendid As String = ""
    Dim vendidexpected As String = ""
    Dim provider As String = ""
    Dim toolTip1 As New ToolTip()
    Dim reboot As Boolean = False
    Dim shutdown As Boolean = False
    Public win8higher As Boolean = False
    Public winxp As Boolean = False
    Dim stopme As Boolean = False
    Public removephysx As Boolean
    Public removeamdaudiobus As Boolean
    Dim remove3dtvplay As Boolean
    Dim time As String = DateAndTime.Now
    Dim locations As String = Application.StartupPath & "\DDU Logs\" & DateAndTime.Now.Year & " _" & DateAndTime.Now.Month & "_" & DateAndTime.Now.Day _
                              & "_" & DateAndTime.Now.Hour & "_" & DateAndTime.Now.Minute & "_" & DateAndTime.Now.Second & "_DDULog.log"
    Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive")
    Dim userpth As String = My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory") + "\"
    Dim checkupdatethread As Thread = Nothing
    Public updates As Integer = Nothing
    Dim reply As String = Nothing
    Dim reply2 As String = Nothing
    Dim version As String = Nothing
    Dim tos As String = Nothing
    Dim card1 As Integer = Nothing
    Dim position2 As Integer = Nothing
    Dim keyroot As String = Nothing
    Dim keychild As String = Nothing
    Dim typelib As String = Nothing
    Dim appid As String = Nothing
    Dim wantedvalue2 As String = Nothing
    Dim subregkey As RegistryKey = Nothing
    Dim subregkey2 As RegistryKey = Nothing
    Dim superkey As RegistryKey = Nothing
    Dim wantedvalue As String = Nothing
    Dim regkey As RegistryKey = Nothing
    Dim currentdriverversion As String = Nothing
    Dim packages As String()
    Dim safemode As Boolean = False
    Dim myExe As String
    Dim driverfiles() As String
    Dim checkupdates As New genericfunction
    Dim settings As New genericfunction
    Dim CleanupEngine As New CleanupEngine
    Dim enduro As Boolean = False
    Dim preventclose As Boolean = False
    Dim filePath As String
    Dim combobox1value As String = Nothing
    Dim combobox2value As String = Nothing
    Dim buttontext As String()
    Dim closeapp As String = False
    Public ddudrfolder As String
    Dim TextLines() As String
    Dim array() As String = Nothing
    Dim loadinitiated As Boolean = True
    Dim systemrestore As Boolean
    Public donotremoveamdhdaudiobusfiles As Boolean = True
    Public msgboxmessage As String()
    Public UpdateTextMethodmessage As String()

    Private Sub Checkupdates2()
        AccessUI()
    End Sub

    Private Sub AccessUI()
        Dim updates As Integer = checkupdates.checkupdates
        If Me.InvokeRequired Then
            Me.Invoke(New MethodInvoker(AddressOf AccessUI))
        Else
            If updates = 1 Then
                Try
                    buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\label11.txt") '// add each line as String Array.
                    Label11.Text = ""
                    Label11.Text = Label11.Text & buttontext("1")
                Catch ex As Exception
                    Label11.Text = ("No Updates found. Program is up to date.")
                End Try

            ElseIf updates = 2 Then

                Try
                    buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\label11.txt") '// add each line as String Array.
                    Label11.Text = ""
                    Label11.Text = Label11.Text & buttontext("2")
                Catch ex As Exception
                    Label11.Text = ("Updates found! Expect limited support on older versions than the most recent.")
                End Try

                If Not MyIdentity.IsSystem Then    'we dont want to open a webpage when the app is under "System" user.
                    Dim result = MsgBox(msgboxmessage("0"), MsgBoxStyle.YesNoCancel)

                    If result = MsgBoxResult.Yes Then
                        process.Start("http://www.wagnardmobile.com")
                        closeapp = True
                        preventclose = False
                        Me.Close()
                        Exit Sub
                    ElseIf result = MsgBoxResult.No Then
                        MsgBox(msgboxmessage("1"))
                    ElseIf result = MsgBoxResult.Cancel Then
                        closeapp = True
                        preventclose = False
                        Me.Close()
                    End If
                End If

                ElseIf updates = 3 Then
                    Try
                        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\label11.txt") '// add each line as String Array.
                        Label11.Text = ""
                        Label11.Text = Label11.Text & buttontext("3")
                    Catch ex As Exception
                        Label11.Text = ("Unable to Fetch updates!!")
                    End Try
                End If
            End If
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        reboot = True
        combobox1value = ComboBox1.Text
        BackgroundWorker1.RunWorkerAsync(ComboBox1.Text)

    End Sub

    Private Sub cleandriverstore()

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "-Executing Driver Store cleanUP(finding OEM step)..." + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Executing Driver Store cleanUP(Find OEM)...")
        'Check the driver from the driver store  ( oemxx.inf)
        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        processinfo.Arguments = "dp_enum"
        processinfo.UseShellExecute = False
        processinfo.CreateNoWindow = True
        processinfo.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        process.StartInfo = processinfo
        process.Start()
        reply = process.StandardOutput.ReadToEnd
        process.WaitForExit()

        ' log("ddudr DP_ENUM RESULT BELOW")
        ' log(reply)
        'Preparing to read output.
        Dim oem As Integer = Nothing
        Try
            oem = reply.IndexOf("oem")
        Catch ex As Exception
        End Try

        UpdateTextMethod(UpdateTextMethodmessage("0"))

        While oem > -1 And oem <> Nothing
            Dim position As Integer = reply.IndexOf("Provider:", oem)
            Dim classs As Integer = reply.IndexOf("Class:", oem)
            Dim inf As Integer = reply.IndexOf(".inf", oem)
            If classs > -1 Then 'I saw that sometimes, there could be no class on some oems (winxp)
                If reply.Substring(position, classs - position).Contains(provider) Or _
                   reply.Substring(position, classs - position).ToLower.Contains("ati tech") Or _
                    reply.Substring(position, classs - position).ToLower.Contains("amd") Then
                    Dim part As String = reply.Substring(oem, inf - oem)
                    log(part + " Found")
                    Dim deloem As New Diagnostics.ProcessStartInfo
                    deloem.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                    deloem.Arguments = "dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                    Try
                        For Each child As String In IO.File.ReadAllLines(Environment.GetEnvironmentVariable("windir") & "\inf\" & part & ".inf")
                            If child.ToLower.Trim.Replace(" ", "").Contains("class=display") Or _
                                child.ToLower.Trim.Replace(" ", "").Contains("class=media") Then
                                deloem.Arguments = "-f dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                            End If
                        Next
                    Catch ex As Exception
                    End Try

                    'Uninstall Driver from driver store  delete from (oemxx.inf)
                    log(deloem.Arguments)
                    deloem.UseShellExecute = False
                    deloem.CreateNoWindow = True
                    deloem.RedirectStandardOutput = True
                    'creation dun process fantome pour le wait on exit.
                    Dim proc3 As New Diagnostics.Process
                    Invoke(Sub() TextBox1.Text = TextBox1.Text + "Executing Driver Store cleanUP(Delete OEM)..." + vbNewLine)
                    Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
                    Invoke(Sub() TextBox1.ScrollToCaret())
                    log("Executing Driver Store CleanUP(delete OEM)...")
                    proc3.StartInfo = deloem
                    proc3.Start()
                    reply2 = proc3.StandardOutput.ReadToEnd
                    proc3.WaitForExit()


                    Invoke(Sub() TextBox1.Text = TextBox1.Text + reply2 + vbNewLine)
                    Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
                    Invoke(Sub() TextBox1.ScrollToCaret())
                    log(reply2)

                End If
            End If
            oem = reply.IndexOf("oem", oem + 1)
        End While


        Invoke(Sub() TextBox1.Text = TextBox1.Text + "-Driver Store cleanUP complete." + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Driver Store CleanUP Complete.")



        'Delete left over files.
    End Sub
    Private Sub cleanamdserviceprocess()


        CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\services.cfg")) '// add each line as String Array.

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

        Dim appproc = process.GetProcessesByName("MOM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("CLIStart")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("CLI")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("CCC")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("HydraDM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("HydraDM64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("HydraGrd")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("Grid64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("HydraMD64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("HydraMD")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("ThumbnailExtractionHost")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("jusched")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        System.Threading.Thread.Sleep(10)
    End Sub
    Private Sub cleanamdfolders()
        'Delete AMD data Folders
        UpdateTextMethod(UpdateTextMethodmessage("1"))

        log("Cleaning Directory (Please Wait...)")


        If CheckBox1.Checked = True Then
            filePath = "C:\AMD"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                log(ex.Message)
                TestDelete(filePath)
            End Try
        End If

        'Delete driver files
        'delete OpenCL

        CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\driverfiles.cfg")) '// add each line as String Array.



        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            My.Computer.FileSystem.DeleteFile(filePath + "\atiogl.xml")
        Catch ex As Exception
        End Try

        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            My.Computer.FileSystem.DeleteFile(filePath + "\ativpsrm.bin")
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.CommonProgramFiles) + "\ATI Technologies"
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("multimedia") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ati.ace") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\ATI"
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("cim") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then

            filePath = Environment.GetFolderPath _
                       (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                log(ex.Message)
                TestDelete(filePath)
            End Try

            Try
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ati.ace") Or _
                            child.ToLower.Contains("hydravision") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory _
                                (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try

                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True).DeleteValue(Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies\")
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            filePath = System.Environment.SystemDirectory
            Dim files() As String = IO.Directory.GetFiles(filePath + "\", "coinst_*.*")
            For i As Integer = 0 To files.Length - 1
                If Not checkvariables.isnullorwhitespace(files(i)) Then
                    Try
                        My.Computer.FileSystem.DeleteFile(files(i))
                    Catch ex As Exception
                    End Try
                End If
            Next

            filePath = Environment.GetFolderPath _
               (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                log(ex.Message + "AMD APP")
                TestDelete(filePath)
            End Try

            filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
            Try
                TestDelete(filePath)
            Catch ex As Exception
                log(ex.Message + "SteadyVideo testdelete")
            End Try

            Try
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies"
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("multimedia") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory _
                                (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            'on success, do this
                            My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True).DeleteValue(Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies\")
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try
        End If

        filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\ProgramData\Microsoft\Windows\Start Menu\Programs\Catalyst Control Center"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
            TestDelete(filePath)
        End Try

        filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\ProgramData\Microsoft\Windows\Start Menu\Programs\AMD Catalyst Control Center"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
            TestDelete(filePath)
        End Try


        filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\ProgramData\ATI"
        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ace") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
            log(ex.Message)
        End Try

        For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(userpth))
            filePath = filepaths + "\AppData\Roaming\ATI"
            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ace") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
                log(ex.Message)
            End Try


            filePath = filepaths + "\AppData\Local\ATI"
            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ace") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
                log(ex.Message)
            End Try
        Next
    End Sub
    Private Sub cleanamd()
        UpdateTextMethod(UpdateTextMethodmessage("2"))
        log("Cleaning known Regkeys")


        'Delete AMD regkey
        'Deleting DCOM object

        log("Starting dcom/clsid/appid/typelib cleanup")

        CleanupEngine.classroot(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\classroot.cfg")) '// add each line as String Array.


        '-----------------
        'interface cleanup
        '-----------------



        CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\interface.cfg")) '// add each line as String Array.

        '-------------
        'control/video
        '-------------
        Try


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Video", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = regkey.OpenSubKey(child & "\Video", False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("Service").ToString) = False Then
                                If subregkey.GetValue("Service").ToString.ToLower = "amdkmdap" Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\CurrentControlSet\Control\Video\" & child)
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '-----------------
        'end control/video
        '-----------------

        log("Instance lass cleanUP")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            Try
                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance", False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey2 IsNot Nothing Then
                                For Each child2 As String In subregkey2.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child2) = False Then
                                        Try
                                            superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance\" & child2)
                                        Catch ex As Exception
                                            Continue For
                                        End Try
                                        If superkey IsNot Nothing Then
                                            If checkvariables.isnullorwhitespace(superkey.GetValue("FriendlyName")) = False Then
                                                wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                If wantedvalue2.ToLower.Contains("ati mpeg") Or _
                                                    wantedvalue2.ToLower.Contains("amd mjpeg") Or _
                                                    wantedvalue2.ToLower.Contains("ati ticker") Or _
                                                    wantedvalue2.ToLower.Contains("mmace softemu") Or _
                                                    wantedvalue2.ToLower.Contains("mmace deinterlace") Or _
                                                    wantedvalue2.ToLower.Contains("amd video") Or _
                                                    wantedvalue2.ToLower.Contains("mmace procamp") Or _
                                                    wantedvalue2.ToLower.Contains("ati video") Then
                                                    Try
                                                        My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("CLSID\" & child & "\Instance\" & child2)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", False)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                Try
                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance", False)
                                Catch ex As Exception
                                    Continue For
                                End Try
                                If subregkey2 IsNot Nothing Then
                                    For Each child2 As String In subregkey2.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child2) = False Then
                                            Try
                                                superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance\" & child2)
                                            Catch ex As Exception
                                                Continue For
                                            End Try
                                            If superkey IsNot Nothing Then
                                                If checkvariables.isnullorwhitespace(superkey.GetValue("FriendlyName")) = False Then
                                                    wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                    If wantedvalue2.ToLower.Contains("ati mpeg") Or _
                                                    wantedvalue2.ToLower.Contains("amd mjpeg") Or _
                                                    wantedvalue2.ToLower.Contains("ati ticker") Or _
                                                    wantedvalue2.ToLower.Contains("mmace softemu") Or _
                                                    wantedvalue2.ToLower.Contains("mmace deinterlace") Or _
                                                    wantedvalue2.ToLower.Contains("mmace procamp") Or _
                                                    wantedvalue2.ToLower.Contains("amd video") Or _
                                                    wantedvalue2.ToLower.Contains("ati video") Then
                                                        Try
                                                            My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\CLSID\" & child & "\Instance\" & child2)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        log("AppID and clsidleftover cleanUP")
        'old dcom 
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("amdwdst") Then
                            If checkvariables.isnullorwhitespace(child) = False Then
                                Try
                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID\" + child)
                                Catch ex As Exception
                                    Continue For
                                End Try
                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(subregkey.GetValue("AppID")) = False Then
                                        wantedvalue = subregkey.GetValue("AppID").ToString
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
                                        Try
                                            regkey.DeleteSubKeyTree(wantedvalue)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.clsidleftover(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg")) '// add each line as String Array.

        log("Record CleanUP")

        '--------------
        'Record cleanup
        '--------------
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Record", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        Try
                            subregkey = regkey.OpenSubKey(child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            For Each childs As String In subregkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(childs) = False Then
                                    Try
                                        If checkvariables.isnullorwhitespace(subregkey.OpenSubKey(childs, False).GetValue("Assembly")) = False Then
                                            If subregkey.OpenSubKey(childs, False).GetValue("Assembly").ToString.ToLower.Contains("aticccom") Then
                                                regkey.DeleteSubKeyTree(child)
                                            End If
                                        End If
                                    Catch ex As Exception
                                        Continue For
                                    End Try
                                End If
                            Next
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try
        log("Assembly CleanUP")

        '------------------
        'Assemblies cleanUP
        '------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Installer\Assemblies", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ati.ace") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try

                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '----------------------
        'End Assemblies cleanUP
        '----------------------


        'end of decom?

        Try
            My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" & _
                                                         "Display\shellex\PropertySheetHandlers", True).DeleteSubKeyTree("ATIACE")
        Catch ex As Exception
        End Try


        'remove opencl registry Khronos

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Khronos\OpenCL\Vendors", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("amdocl") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.GetValueNames().Length = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("Software\Khronos")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("amdocl") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.GetValueNames().Length = 0 Then
                        Try
                            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("Software\Wow6432Node\Khronos")
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Catch ex As Exception
            End Try
        End If

        log("ngenservice Clean")

        '----------------------
        '.net ngenservice clean
        '----------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ati.ace") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '-----------------------------
        'End of .net ngenservice clean
        '-----------------------------

        '-----------------------------
        'Shell extensions\aprouved
        '-----------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or _
                            regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or _
                                regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If
        '-----------------------------
        'End Shell extensions\aprouved
        '-----------------------------

        log("Pnplockdownfiles region cleanUP")

        CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\driverfiles.cfg")) '// add each line as String Array.

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\amdkmdap")
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then
            Try

                My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
            Catch ex As Exception
            End Try

            Try

                My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
            Catch ex As Exception
            End Try
        End If

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
        Catch ex As Exception
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
        Catch ex As Exception
        End Try


        '---------------------------------------------
        'Cleaning of Legacy_AMDKMDAG on win7 and lower
        '---------------------------------------------

        Try
            If version < "6.2" And System.Windows.Forms.SystemInformation.BootMode <> BootMode.Normal Then 'win 7 and lower + safemode only
                log("Cleaning LEGACY_AMDKMDAG")
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("SYSTEM")
                If subregkey IsNot Nothing Then
                    For Each childs As String In subregkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(childs) = False Then
                            If childs.ToLower.Contains("controlset") Then
                                Try
                                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                     ("SYSTEM\" & childs & "\Enum\Root")
                                Catch ex As Exception
                                    Continue For
                                End Try

                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If child.ToLower.Contains("legacy_amdkmdag") Then

                                                Try
                                                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\" & childs & "\Enum\Root\" & child)
                                                Catch ex As Exception
                                                    log(ex.Message & " Legacy_AMDKMDAG   (error)")
                                                End Try
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '----------------------------------------------------
        'End of Cleaning of Legacy_AMDKMDAG on win7 and lower
        '----------------------------------------------------


        '--------------------------------
        'System environement path cleanup
        '--------------------------------
        log("System environement cleanUP")
        Try
            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
            If subregkey IsNot Nothing Then
                For Each child2 As String In subregkey.GetSubKeyNames()
                    If child2.ToLower.Contains("controlset") Then
                        Try
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If child.Contains("AMDAPPSDKROOT") Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    If child.Contains("Path") Then
                                        If checkvariables.isnullorwhitespace(regkey.GetValue(child)) = False Then
                                            wantedvalue = regkey.GetValue(child).ToString
                                            Try
                                                Select Case True
                                                    Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\AMD APP\bin\x86_64;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\AMD APP\bin\x86_64;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\AMD APP\bin\x86;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\AMD APP\bin\x86;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv.ToLower & "\Program Files (x86)\AMD APP\bin\x86_64;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv.ToLower & "\Program Files (x86)\AMD APP\bin\x86_64;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv.ToLower & "\Program Files (x86)\AMD APP\bin\x86;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv.ToLower & "\Program Files (x86)\AMD APP\bin\x86;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv.ToLower & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv.ToLower & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv.ToLower & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static")
                                                        wantedvalue = wantedvalue.Replace(sysdrv.ToLower & "\Program Files (x86)\ATI Technologies\ATI.ACE\Core-Static", "")
                                                        regkey.SetValue(child, wantedvalue)
                                                End Select
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        'end system environement patch cleanup

        '-----------------------
        'remove event view stuff
        '-----------------------
        log("Remove eventviewer stuff")
        Try
            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
            If subregkey IsNot Nothing Then
                For Each child2 As String In subregkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child2) = False Then
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.ToLower.Contains("aceeventlog") Then
                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                Next
                            End If

                            Try
                                regkey.OpenSubKey("Application", True).DeleteSubKeyTree("ATIeRecord")
                            Catch ex As Exception
                            End Try

                            Try
                                regkey.OpenSubKey("System", True).DeleteSubKeyTree("amdkmdag")
                            Catch ex As Exception
                            End Try

                            Try
                                regkey.OpenSubKey("System", True).DeleteSubKeyTree("amdkmdap")
                            Catch ex As Exception
                            End Try

                            Try
                                My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services", True).DeleteSubKeyTree("Atierecord")
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try


        '--------------------------------
        'end of eventviewer stuff removal
        '--------------------------------
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
          ("Directory\background\shellex\ContextMenuHandlers", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.Contains("ACE") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.StartsWith("ATI") Then
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try



        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ace") Or _
                           child.ToLower.Contains("install") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("ATI")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("cbt") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                        If child.ToLower.Contains("install") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If child2.ToLower.Contains("ati catalyst") Or _
                                    child2.ToLower.Contains("ati mcat") Or _
                                    child2.ToLower.Contains("avt") Or _
                                    child2.ToLower.Contains("ccc") Or _
                                    child2.ToLower.Contains("packages") Or _
                                    child2.ToLower.Contains("wirelessdisplay") Or _
                                    child2.ToLower.Contains("hydravision") Or _
                                    child2.ToLower.Contains("avivo") Or _
                                    child2.ToLower.Contains("steadyvideo") Then
                                    Try
                                        regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                    Catch ex As Exception
                                    End Try
                                End If
                            Next
                            If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("ATI Technologies")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\AMD", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("eeu") Or _
                           child.ToLower.Contains("fuel") Or _
                           child.ToLower.Contains("mftvdecoder") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("AMD")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\ATI", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("ace") Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True).DeleteSubKeyTree("ATI")
                        Catch ex As Exception
                        End Try
                    End If
                End If

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\AMD", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("eeu") Or
                               child.ToLower.Contains("mftvdecoder") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True).DeleteSubKeyTree("AMD")
                    End If
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\ATI Technologies", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("system wide settings") Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                            If child.ToLower.Contains("install") Then
                                For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                    If child2.ToLower.Contains("ati catalyst") Or _
                                        child2.ToLower.Contains("ati mcat") Or _
                                        child2.ToLower.Contains("avt") Or _
                                        child2.ToLower.Contains("ccc") Or _
                                        child2.ToLower.Contains("packages") Or _
                                        child2.ToLower.Contains("wirelessdisplay") Or _
                                        child2.ToLower.Contains("hydravision") Or _
                                        child2.ToLower.Contains("dndtranscoding64") Or _
                                        child2.ToLower.Contains("avivo") Or _
                                        child2.ToLower.Contains("steadyvideo") Then
                                        Try
                                            regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                Next
                                If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                    Try
                                        regkey.DeleteSubKeyTree(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True).DeleteSubKeyTree("ATI Technologies")
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Microsoft\Windows\CurrentVersion\Run", True)
                    If regkey IsNot Nothing Then
                        Try
                            regkey.DeleteValue("HydraVisionDesktopManager")
                        Catch ex As Exception

                            log(ex.Message + " HydraVisionDesktopManager")
                        End Try

                        Try
                            regkey.DeleteValue("Grid")
                        Catch ex As Exception

                            log(ex.Message + " GRID")
                        End Try

                        Try
                            regkey.DeleteValue("HydraVisionMDEngine")
                        Catch ex As Exception

                            log(ex.Message + " HydraVisionMDEngine")
                        End Try

                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("Removing known Packages")

        packages = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\packages.cfg") '// add each line as String Array.
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        Try
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("DisplayName")) = False Then
                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i)) Then
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            packages = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\packages.cfg") '// add each line as String Array.
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("DisplayName")) = False Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To packages.Length - 1
                                            If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                If wantedvalue.ToLower.Contains(packages(i)) Then
                                                    Try
                                                        regkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        CleanupEngine.installer(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\packages.cfg"))

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                If regkey IsNot Nothing Then
                    Try
                        regkey.DeleteValue("StartCCC")

                    Catch ex As Exception

                        log(ex.Message + " StartCCC")
                    End Try
                    Try

                        regkey.DeleteValue("AMD AVT")

                    Catch ex As Exception

                        log(ex.Message + " AMD AVT")
                    End Try
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        log("SharedDLLs CleanUP")
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.Contains("ATI\CIM\") Or _
                        child.Contains("SteadyVideo") Or _
                        child.Contains("ATI.ACE") Or _
                        child.Contains("ATI Technologies\Multimedia") Or _
                        child.Contains("OpenCL") Or _
                        child.Contains("OpenVideo") Or _
                        child.Contains("OVDecode") Or _
                        child.Contains("amdocl") Or _
                        child.Contains("kbdsdk64.dll") Or _
                        child.Contains("clinfo") Or _
                        child.Contains("SlotMaximizer") Or _
                        child.Contains("cccutil") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
             ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.Contains("ATI\CIM\") Or _
                        child.Contains("SteadyVideo") Or _
                        child.Contains("ATI.ACE") Or _
                        child.Contains("ATI Technologies\Multimedia") Or _
                        child.Contains("OpenCL") Or _
                        child.Contains("OpenVideo") Or _
                        child.Contains("OVDecode") Or _
                        child.Contains("amdocl") Or _
                        child.Contains("clinfo") Or _
                        child.Contains("kdbsdk32.dll") Or _
                        child.Contains("SlotMaximizer") Or _
                        child.Contains("cccutil") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If


        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.Contains("ATI\CIM\") Or child.Contains("AMD AVT") Or _
                           child.Contains("ATI\CIM\") Or _
                           child.Contains("AMD APP\") Or _
                           child.Contains("AMD\SteadyVideo\") Or _
                           child.Contains("ATI.ACE\") Or _
                           child.Contains("HydraVision\") Or _
                           child.Contains("ATI Technologies\Application Profiles\") Or _
                           child.Contains("ATI Technologies\Multimedia\") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                            ("CLSID\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    If wantedvalue.Contains("SteadyVideoBHO") Then
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try


        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                ("Wow6432Node\CLSID\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        If wantedvalue.Contains("AMDWDST") Or _
                                           wantedvalue.Contains("ATI Transcoder DB Enum") Or _
                                           wantedvalue.Contains("ATI Transcoder") Or _
                                           wantedvalue.Contains("ATI Transcoder DB") Or _
                                           wantedvalue.Contains("SteadyVideoBHO") Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If




        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\Interface", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                ("Wow6432Node\Interface\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        If wantedvalue.Contains("SteadyVideoBHO") Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If
    End Sub

    Private Sub cleannvidiaserviceprocess()

        CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\services.cfg"))

        'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
        'holding files in the NVIDIA folders sometimes.

        Dim appproc = process.GetProcessesByName("Lcore")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("nvstreamsvc")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("NvTmru")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i


        appproc = process.GetProcessesByName("nvxdsync")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("nvtray")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("dwm")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("WWAHost")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("nvspcaps64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("nvspcaps")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = process.GetProcessesByName("NvBackend")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

    End Sub

    Private Sub cleannvidiafolders()

        'Delete NVIDIA data Folders
        'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/

        UpdateTextMethod(UpdateTextMethodmessage("3"))
        log("Cleaning UpdatusUser users ac if present")

        Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName.ToString())
        Dim users As DirectoryEntries = AD.Children
        Dim newuser As DirectoryEntry = Nothing

        Try
            newuser = users.Find("UpdatusUser")
            users.Remove(newuser)
        Catch ex As Exception
        End Try

        UpdateTextMethod(UpdateTextMethodmessage("4"))

        log("Cleaning Directory")


        If CheckBox1.Checked = True Then
            filePath = "C:\NVIDIA"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message)
                TestDelete(filePath)
            End Try

        End If

        ' here I erase the folders / files of the nvidia GFE / update in users.
        filePath = IO.Path.GetDirectoryName(userpth)
        For Each child As String In Directory.GetDirectories(filePath)
            If checkvariables.isnullorwhitespace(child) = False Then
                If child.ToLower.Contains("updatususer") Then
                    Try
                        TestDelete(child)
                    Catch ex As Exception
                    End Try

                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception

                        log(ex.Message + " Updatus directory delete")
                    End Try

                    'Yes we do it 2 times. This will workaround a problem on junction/sybolic/hard link
                    Try
                        TestDelete(child)
                    Catch ex As Exception
                        log(ex.Message + " UpdatusUsers second pass")
                    End Try
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message + " Updatus directory delete")
                    End Try
                End If
            End If
        Next


        For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(userpth))

            filePath = filepaths + "\AppData\Local\NVIDIA"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvbackend") Or _
                            child.ToLower.Contains("gfexperience") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            filePath = filepaths + "\AppData\Roaming\NVIDIA"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("computecache") Or _
                            child.ToLower.Contains("glcache") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try


            filePath = filepaths + "\AppData\Local\NVIDIA Corporation"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ledvisualizer") Or _
                            child.ToLower.Contains("shadowplay") Or _
                            child.ToLower.Contains("gfexperience") Or _
                            child.ToLower.Contains("shield apps") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try


        Next

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"

        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("updatus") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"
        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("drs") Or _
                        child.ToLower.Contains("geforce experience") Or _
                        child.ToLower.Contains("netservice") Or _
                        child.ToLower.Contains("shadowplay") Or _
                        child.ToLower.Contains("nvstreamsvc") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation"
        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("3d vision") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetFolderPath _
      (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("control panel client") Or _
                       child.ToLower.Contains("display") Or _
                       child.ToLower.Contains("coprocmanager") Or _
                       child.ToLower.Contains("drs") Or _
                       child.ToLower.Contains("nvsmi") Or _
                       child.ToLower.Contains("opencl") Or _
                       child.ToLower.Contains("3d vision") Or _
                       child.ToLower.Contains("led visualizer") Or _
                       child.ToLower.Contains("netservice") Or _
                       child.ToLower.Contains("nvidia geforce experience") Or _
                       child.ToLower.Contains("nvstreamc") Or _
                       child.ToLower.Contains("nvstreamsrv") Or _
                       child.ToLower.Contains("physx") Or _
                       child.ToLower.Contains("nvstreamsrv") Or _
                       child.ToLower.Contains("shadowplay") Or _
                       child.ToLower.Contains("installer2") Or _
                       child.ToLower.Contains("update common") Or _
                       child.ToLower.Contains("update core") Then

                        If child.ToLower.Contains("installer2") Then
                            For Each child2 As String In Directory.GetDirectories(child)
                                If checkvariables.isnullorwhitespace(child2) = False Then
                                    If child2.ToLower.Contains("display.3dvision") Or _
                                       child2.ToLower.Contains("display.controlpanel") Or _
                                       child2.ToLower.Contains("display.driver") Or _
                                       child2.ToLower.Contains("display.gfexperience") Or _
                                       child2.ToLower.Contains("display.nvirusb") Or _
                                       child2.ToLower.Contains("display.physx") Or _
                                       child2.ToLower.Contains("display.update") Or _
                                       child2.ToLower.Contains("gfexperience") Or _
                                       child2.ToLower.Contains("nvidia.update") Or _
                                       child2.ToLower.Contains("installer2\installer") Or _
                                       child2.ToLower.Contains("network.service") Or _
                                       child2.ToLower.Contains("shadowplay") Or _
                                       child2.ToLower.Contains("update.core") Or _
                                       child2.ToLower.Contains("virtualaudio.driver") Or _
                                       child2.ToLower.Contains("hdaudio.driver") Then
                                        If removephysx Then
                                            Try
                                                My.Computer.FileSystem.DeleteDirectory _
                                                (child2, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                            Catch ex As Exception
                                                log(ex.Message)
                                                TestDelete(child2)
                                            End Try
                                        Else
                                            If child2.ToLower.Contains("physx") Then
                                                'do nothing
                                            Else
                                                Try
                                                    My.Computer.FileSystem.DeleteDirectory _
                                                    (child2, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                                Catch ex As Exception
                                                    log(ex.Message)
                                                    TestDelete(child2)
                                                End Try
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                            Try
                                If Directory.GetDirectories(child).Length = 0 Then
                                    Try
                                        My.Computer.FileSystem.DeleteDirectory _
                                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                    Catch ex As Exception
                                        log(ex.Message)
                                        TestDelete(child)
                                    End Try
                                End If
                            Catch ex As Exception
                            End Try
                        End If
                        If removephysx And Not child.ToLower.Contains("installer2") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory _
                                (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        Else
                            If child.ToLower.Contains("physx") Or child.ToLower.Contains("installer2") Then
                                'do nothing
                            Else
                                Try
                                    My.Computer.FileSystem.DeleteDirectory _
                                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                            End If
                        End If
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\AGEIA Technologies"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
            End Try

        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("3d vision") Or _
                           child.ToLower.Contains("coprocmanager") Or _
                           child.ToLower.Contains("led visualizer") Or _
                           child.ToLower.Contains("netservice") Or _
                           child.ToLower.Contains("nvidia geforce experience") Or _
                           child.ToLower.Contains("nvstreamc") Or _
                           child.ToLower.Contains("nvstreamsrv") Or _
                           child.ToLower.Contains("update common") Or _
                           child.ToLower.Contains("\physx") Or _
                           child.ToLower.Contains("update core") Then
                            If removephysx Then
                                Try
                                    My.Computer.FileSystem.DeleteDirectory _
                                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                            Else
                                If child.ToLower.Contains("physx") Then
                                    'do nothing
                                Else
                                    Try
                                        My.Computer.FileSystem.DeleteDirectory _
                                        (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                    Catch ex As Exception
                                        log(ex.Message)
                                        TestDelete(child)
                                    End Try
                                End If
                            End If
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            End If
        Catch ex As Exception
        End Try

        Try
            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AGEIA Technologies"
                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True).DeleteValue(Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AGEIA Technologies\")
                Catch ex As Exception
                End Try

            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.

        filePath = System.Environment.SystemDirectory
        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "nvdisp*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    My.Computer.FileSystem.DeleteFile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

        filePath = System.Environment.SystemDirectory
        files = IO.Directory.GetFiles(filePath + "\", "nvhdagenco*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    My.Computer.FileSystem.DeleteFile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            My.Computer.FileSystem.DeleteDirectory _
                    (filePath + "\Help\nvcpl", FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetEnvironmentVariable("windir") + "\Temp\NVIDIA Corporation"
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("nv_cache") Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try



        For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(userpth))

            filePath = filepaths + "\AppData\Local\Temp\NVIDIA Corporation"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nv_cache") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(filePath)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            filePath = filepaths + "\AppData\Local\Temp\NVIDIA"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("geforceexperienceselfupdate") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(filePath)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            filePath = filepaths + "\AppData\Local\Temp\Low\NVIDIA Corporation"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nv_cache") Then
                            Try
                                My.Computer.FileSystem.DeleteDirectory(child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(filePath)
                            End Try
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            My.Computer.FileSystem.DeleteDirectory _
                                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try
        Next

    End Sub

    Private Sub cleannvidia()

        '-----------------
        'Registry Cleaning
        '-----------------
        UpdateTextMethod(UpdateTextMethodmessage("5"))
        log("Starting reg cleanUP... May take a minute or two.")


        'Deleting DCOM object /classroot
        log("Starting dcom/clsid/appid/typelib cleanup")
        log("Step 1/2")


        CleanupEngine.classroot(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\classroot.cfg")) '// add each line as String Array.

        log("Step 2/2")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        If child.ToLower.Contains("comupdatus") Or _
                           child.ToLower.Contains("nv3d") Or _
                           child.ToLower.Contains("nvui") Or _
                           child.ToLower.Contains("nvvsvc") Or _
                           child.ToLower.Contains("nvxd") Or _
                           child.ToLower.Contains("gamesconfigserver") Or _
                           child.ToLower.Contains("nvidia.installer") Or _
                           child.ToLower.Contains("displayserver") Then
                            subregkey = regkey.OpenSubKey(child)
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("AppID")) = False Then
                                    wantedvalue = subregkey.GetValue("AppID").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        Try
                                            appid = wantedvalue
                                            If checkvariables.isnullorwhitespace(appid) = False Then
                                                Try
                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("AppID\" & appid)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        Catch ex As Exception
                                        End Try
                                        Try
                                            My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("AppID\" & child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.clsidleftover(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftover.cfg")) '// add each line as String Array.

        '-----------------
        'interface cleanup
        '-----------------

        CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\interface.cfg")) '// add each line as String Array.


        log("Finished dcom/clsid/appid/typelib/interface cleanup")

        'end of deleting dcom stuff
        log("Pnplockdownfiles region cleanUP")

        CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.

        'Cleaning PNPRessources.

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
        Catch ex As Exception
            log(ex.Message & "pnp resources khronos")
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\Global\CoprocManager\OptimusEnhancements")
        Catch ex As Exception
            log(ex.Message & "pnp resources khronos")
        End Try

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
        Catch ex As Exception
            log(ex.Message & "pnp resources cpl extension")
        End Try

        If IntPtr.Size = 8 Then

            Try

                My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
            Catch ex As Exception
                log(ex.Message & "pnpresources wow6432node khronos")
            End Try
        End If

        Try

            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
        Catch ex As Exception
            log(ex.Message & "pnp ressources nvidia corporation")
        End Try


        '----------------------
        'Firewall entry cleanup
        '----------------------
        log("Firewall entry cleanUP")
        Try
            If winxp = False Then
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\", False)
                If subregkey IsNot Nothing Then
                    For Each child2 As String In subregkey.GetSubKeyNames()
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If checkvariables.isnullorwhitespace(regkey.GetValue(child)) = False Then
                                            wantedvalue = regkey.GetValue(child).ToString()
                                        End If
                                        If wantedvalue.ToLower.ToString.Contains("nvstreamsrv") Or _
                                           wantedvalue.ToLower.ToString.Contains("nvidia network service") Or _
                                           wantedvalue.ToLower.ToString.Contains("nvidia update core") Then
                                            Try
                                                regkey.DeleteValue(child)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '--------------------------
        'End Firewall entry cleanup
        '--------------------------

        '--------------------------
        'Power Settings CleanUP
        '--------------------------
        log("Power Settings Cleanup")
        Try
            If winxp = False Then
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\", False)
                If subregkey IsNot Nothing Then
                    For Each child2 As String In subregkey.GetSubKeyNames()
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each childs As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(childs) = False Then
                                        For Each child As String In regkey.OpenSubKey(childs).GetValueNames()
                                            If checkvariables.isnullorwhitespace(child) = False And child.ToString.ToLower.Contains("description") Then
                                                If checkvariables.isnullorwhitespace(regkey.OpenSubKey(childs).GetValue(child)) = False Then
                                                    wantedvalue = regkey.OpenSubKey(childs).GetValue(child).ToString()
                                                End If
                                                If wantedvalue.ToString.ToLower.Contains("nvsvc") Then
                                                    regkey.DeleteSubKeyTree(childs)
                                                End If
                                                If wantedvalue.ToString.ToLower.Contains("video and display power management") Then
                                                    Try
                                                        subregkey2 = regkey.OpenSubKey(childs, True)
                                                    Catch ex As Exception
                                                        Continue For
                                                    End Try
                                                    For Each childinsubregkey2 As String In subregkey2.GetSubKeyNames()
                                                        If checkvariables.isnullorwhitespace(childinsubregkey2) = False Then
                                                            For Each childinsubregkey2value As String In subregkey2.OpenSubKey(childinsubregkey2).GetValueNames()
                                                                If checkvariables.isnullorwhitespace(childinsubregkey2value) = False And childinsubregkey2value.ToString.ToLower.Contains("description") Then
                                                                    If checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(childinsubregkey2).GetValue(childinsubregkey2value)) = False Then
                                                                        wantedvalue2 = subregkey2.OpenSubKey(childinsubregkey2).GetValue(childinsubregkey2value).ToString
                                                                    End If
                                                                    If wantedvalue2.ToString.ToLower.Contains("nvsvc") Then
                                                                        Try
                                                                            subregkey2.DeleteSubKeyTree(childinsubregkey2)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                End If
                                                            Next
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        Next
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '--------------------------
        'End Power Settings CleanUP
        '--------------------------


        '--------------------------------
        'System environement path cleanup
        '--------------------------------
        If removephysx Then
            log("System environement CleanUP")
            Try
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
                If subregkey IsNot Nothing Then
                    For Each child2 As String In subregkey.GetSubKeyNames()
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.Contains("Path") Then
                                            If Not checkvariables.isnullorwhitespace(regkey.GetValue(child).ToString()) Then
                                                wantedvalue = regkey.GetValue(child).ToString()
                                                Try
                                                    Select Case True
                                                        Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;")
                                                            wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;", "")
                                                            Try
                                                                regkey.SetValue(child, wantedvalue)
                                                            Catch ex As Exception
                                                            End Try
                                                        Case wantedvalue.Contains(sysdrv.ToLower & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;")
                                                            wantedvalue = wantedvalue.Replace(sysdrv.ToLower & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;", "")
                                                            Try
                                                                regkey.SetValue(child, wantedvalue)
                                                            Catch ex As Exception
                                                            End Try
                                                    End Select
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If
        '-------------------------------------
        'end system environement patch cleanup
        '-------------------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
            If regkey IsNot Nothing Then
                If checkvariables.isnullorwhitespace(regkey.GetValue("AppInit_DLLs")) = False Then
                    wantedvalue = regkey.GetValue("AppInit_DLLs")   'Will need to consider the comma in the future for multiple value
                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
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
                If regkey.GetValue("AppInit_DLLs") = "" Then
                    Try
                        regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)

                If regkey IsNot Nothing Then
                    If checkvariables.isnullorwhitespace(regkey.GetValue("AppInit_DLLs")) = False Then
                        wantedvalue = regkey.GetValue("AppInit_DLLs")
                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
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
                    If regkey.GetValue("AppInit_DLLs") = "" Then
                        Try
                            regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try


        Try
            If removephysx Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.Contains("NVIDIA Corporation\") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception

                                    log(ex.Message + " HKLM..CU\Installer\Folders")
                                End Try
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        'remove opencl registry Khronos
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Khronos\OpenCL\Vendors", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvopencl") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.GetValueNames().Length = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("Software\Khronos")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("nvopencl") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.GetValueNames().Length = 0 Then
                        Try
                            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("Software\Wow6432Node\Khronos")
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Catch ex As Exception
            End Try
        End If
        log("SharedDlls CleanUP")

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
         ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.Contains("NVIDIA Corporation") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                                log(ex.Message + " SharedDLLS")
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
             ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.Contains("NVIDIA Corporation") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                    log(ex.Message + " SharedDLLS")
                                End Try
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.ToLower.Contains("nvidia corporation") Then
                                    For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child2) = False Then
                                            If child2.ToLower.Contains("global") Or _
                                               child2.ToLower.Contains("nvbackend") Or _
                                               child2.ToLower.Contains("nvidia update core") Or _
                                                child2.ToLower.Contains("nvcontrolpanel2") Or _
                                                child2.ToLower.Contains("nvtray") Or _
                                                child2.ToLower.Contains("nvidia control panel") Then
                                                Try
                                                    regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    Next
                                    If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.Users.OpenSubKey(".DEFAULT\Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvidia corporation") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child2) = False Then
                                    If child2.ToLower.Contains("global") Or _
                                       child2.ToLower.Contains("nvbackend") Or _
                                       child2.ToLower.Contains("nvidia update core") Or _
                                        child2.ToLower.Contains("nvcontrolpanel2") Or _
                                        child2.ToLower.Contains("nvidia control panel") Then
                                        Try
                                            regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                            If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ageia technologies") Then
                            If removephysx Then
                                regkey.DeleteSubKeyTree(child)
                            End If
                        End If
                        If child.ToLower.Contains("nvidia corporation") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child2) = False Then
                                    If child2.ToLower.Contains("global") Or _
                                       child2.ToLower.Contains("installer") Or _
                                        child2.ToLower.Contains("installer2") Or _
                                        child2.ToLower.Contains("nvidia update core") Or _
                                        child2.ToLower.Contains("nvcontrolpanel") Or _
                                        child2.ToLower.Contains("nvcontrolpanel2") Or _
                                        child2.ToLower.Contains("nvstream") Or _
                                        child2.ToLower.Contains("nvstreamc") Or _
                                        child2.ToLower.Contains("nvstreamsrv") Or _
                                        child2.ToLower.Contains("uxd") Or _
                                        child2.ToLower.Contains("nvtray") Then
                                        Try
                                            regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                            If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("ageia technologies") Then
                                If removephysx Then
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                            If child.ToLower.Contains("nvidia corporation") Then
                                For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child2) = False Then
                                        If child2.ToLower.Contains("global") Or _
                                           child2.ToLower.Contains("installer2") Or _
                                           child2.ToLower.Contains("physx") Then
                                            If removephysx Then
                                                Try
                                                    regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                                Catch ex As Exception
                                                End Try
                                            Else
                                                If child2.ToLower.Contains("physx") Then
                                                    'do nothing
                                                Else
                                                    regkey.OpenSubKey(child, True).DeleteSubKeyTree(child2)
                                                End If
                                            End If
                                        End If
                                    End If
                                Next
                                If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                    Try
                                        regkey.DeleteSubKeyTree(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)

                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("display.3dvision") Or _
                            child.ToLower.Contains("3dtv") Or _
                            child.ToLower.Contains("_display.controlpanel") Or _
                            child.ToLower.Contains("_display.driver") Or _
                            child.ToLower.Contains("_display.gfexperience") Or _
                            child.ToLower.Contains("_display.nvirusb") Or _
                            child.ToLower.Contains("_display.physx") Or _
                            child.ToLower.Contains("_display.update") Or _
                            child.ToLower.Contains("_gfexperience.") Or _
                            child.ToLower.Contains("_hdaudio.driver") Or _
                            child.ToLower.Contains("_installer") Or _
                            child.ToLower.Contains("_network.service") Or _
                            child.ToLower.Contains("_shadowplay") Or _
                            child.ToLower.Contains("_update.core") Or _
                            child.ToLower.Contains("nvidiastereo") Or _
                            child.ToLower.Contains("miracast.virtualaudio") Or _
                            child.ToLower.Contains("_virtualaudio.driver") Then
                            If removephysx = False And child.ToLower.Contains("physx") Then
                                Continue For
                            End If
                            If remove3dtvplay = False And child.ToLower.Contains("3dtv") Then
                                Continue For
                            End If
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If removephysx Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If checkvariables.isnullorwhitespace(regkey.OpenSubKey(child).GetValue("DisplayName")) = False Then
                                If regkey.OpenSubKey(child).GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("display.3dvision") Or _
                        child.ToLower.Contains("3dtv") Or _
                        child.ToLower.Contains("_display.controlpanel") Or _
                        child.ToLower.Contains("_display.driver") Or _
                        child.ToLower.Contains("_display.gfexperience") Or _
                        child.ToLower.Contains("_display.nvirusb") Or _
                        child.ToLower.Contains("_display.physx") Or _
                        child.ToLower.Contains("_display.update") Or _
                        child.ToLower.Contains("_nvidia.update") Or _
                        child.ToLower.Contains("_gfexperience.") Or _
                        child.ToLower.Contains("_hdaudio.driver") Or _
                        child.ToLower.Contains("_installer") Or _
                        child.ToLower.Contains("_network.service") Or _
                        child.ToLower.Contains("_shadowplay") Or _
                        child.ToLower.Contains("_update.core") Or _
                        child.ToLower.Contains("nvidiastereo") Or _
                        child.ToLower.Contains("miracast.virtualaudio") Or _
                        child.ToLower.Contains("_virtualaudio.driver") Then
                        If removephysx = False And child.ToLower.Contains("physx") Then
                            Continue For
                        End If
                        If remove3dtvplay = False And child.ToLower.Contains("3dtv") Then
                            Continue For
                        End If
                        Try
                            regkey.DeleteSubKeyTree(child)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
            If removephysx Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If checkvariables.isnullorwhitespace(regkey.OpenSubKey(child).GetValue("DisplayName")) = False Then
                            If regkey.OpenSubKey(child).GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
                                regkey.DeleteSubKeyTree(child)
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    Try
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
                    Catch ex As Exception
                        Continue For
                    End Try
                    If subregkey IsNot Nothing Then
                        If checkvariables.isnullorwhitespace(subregkey.GetValue("ProfileImagePath")) = False Then
                            wantedvalue = subregkey.GetValue("ProfileImagePath").ToString
                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                If wantedvalue.Contains("UpdatusUser") Then
                                    Try
                                        regkey.DeleteSubKeyTree(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    If wantedvalue.ToLower.Contains("nvidia control panel") Then
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
                                        'special case only to nvidia afaik. there i a clsid for a control pannel that link from namespace.
                                        Try
                                            My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True).DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("ngenservice Clean")

        '----------------------
        '.net ngenservice clean
        '----------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("gfexperience.exe") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("gfexperience.exe") Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        '-----------------------------
        'End of .net ngenservice clean
        '-----------------------------

        '-----------------------------
        'Mozilla plugins
        '-----------------------------
        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\MozillaPlugins", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("nvidia.com/3dvision") Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If


        '-----------------------
        'remove event view stuff
        '-----------------------
        log("Remove eventviewer stuff")
        Try
            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
            If subregkey IsNot Nothing Then
                For Each child2 As String In subregkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child2) = False Then
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog\Application", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.ToLower.Contains("nvidia update") Then
                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        '---------------------------
        'end remove event view stuff
        '---------------------------

        '---------------------------
        'virtual store
        '---------------------------
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteSubKeyTree("Global")
                    If regkey.SubKeyCount = 0 Then
                        My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE", True).DeleteSubKeyTree("NVIDIA Corporation")
                    End If
                Catch ex As Exception
                End Try
            End If
        Catch ex As Exception
        End Try
        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
                    If regkey IsNot Nothing Then
                        Try
                            regkey.DeleteSubKeyTree("Global")
                            If regkey.SubKeyCount = 0 Then
                                My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True).DeleteSubKeyTree("NVIDIA Corporation")
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
        Try
            For Each child As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(child) Then
                    If child.ToLower.Contains("s-1-5") Then
                        Try
                            My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True).DeleteSubKeyTree("Global")
                            If My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", False).SubKeyCount = 0 Then
                                My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE", True).DeleteSubKeyTree("NVIDIA Corporation")
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try


        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteValue("Nvtmru")
                Catch ex As Exception
                    log(ex.Message + " Nvtmru")
                End Try

                Try
                    regkey.DeleteValue("NvBackend")
                Catch ex As Exception
                End Try

                Try
                    regkey.DeleteValue("ShadowPlay")
                Catch ex As Exception
                    log(ex.Message + " ShadowPlay")
                End Try
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                If regkey IsNot Nothing Then
                    Try
                        regkey.DeleteValue("StereoLinksInstall")
                    Catch ex As Exception
                        log(ex.Message + " StereoLinksInstall")
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.installer(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\packages.cfg"))



        '-------------
        'control/video
        '-------------
        Try


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Video", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = regkey.OpenSubKey(child & "\Video", False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("Service").ToString) = False Then
                                If subregkey.GetValue("Service").ToString.ToLower = "nvlddmkm" Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\CurrentControlSet\Control\Video\" & child)
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '-----------------
        'end control/video
        '-----------------

        If remove3dtvplay Then
            Try
                My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
            Try
                My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
            Try
                My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
        End If

        '-----------------------------
        'Shell extensions\aprouved
        '-----------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Or _
                           regkey.GetValue(child).ToString.ToLower.Contains("nvidia play on my tv context menu extension") Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Extended Properties", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        For Each childs As String In regkey.OpenSubKey(child).GetValueNames()
                            If Not checkvariables.isnullorwhitespace(childs) Then
                                If childs.ToLower.Contains("nvcpl.cpl") Then
                                    Try
                                        regkey.OpenSubKey(child, True).DeleteValue(childs)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If
        '-----------------------------
        'End Shell extensions\aprouved
        '-----------------------------

        'Shell ext
        Try
            My.Computer.Registry.ClassesRoot.OpenSubKey("Directory\background\shellex\ContextMenuHandlers", True).DeleteSubKeyTree("NvCplDesktopContext")
        Catch ex As Exception
        End Try
        Try
            My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Directory\background\shellex\ContextMenuHandlers", True).DeleteSubKeyTree("NvCplDesktopContext")
        Catch ex As Exception
        End Try

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "-End of Registry Cleaning" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("End of Registry Cleaning")
        System.Threading.Thread.Sleep(10)
    End Sub
    Private Sub cleanintelfolders()

        UpdateTextMethod(UpdateTextMethodmessage("4"))

        log("Cleaning Directory")

        CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\driverfiles.cfg")) '// add each line as String Array.

        filePath = System.Environment.SystemDirectory
        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "igfxcoin*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    My.Computer.FileSystem.DeleteFile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

    End Sub
    Private Sub cleanintelserviceprocess()

        CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\services.cfg")) '// add each line as String Array.

        Dim appproc = process.GetProcessesByName("IGFXEM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

    End Sub
    Private Sub cleanintel()

        UpdateTextMethod(UpdateTextMethodmessage("5"))

        log("Cleaning registry")

        CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\driverfiles.cfg")) '// add each line as String Array.

        CleanupEngine.classroot(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\classroot.cfg")) '// add each line as String Array.

        CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\interface.cfg")) '// add each line as String Array.

        CleanupEngine.clsidleftover(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\clsidleftover.cfg")) '// add each line as String Array.

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Intel", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("igfx") Or _
                           child.ToLower.Contains("mediasdk") Or _
                           child.ToLower.Contains("opencl") Or _
                           child.ToLower.Contains("intel wireless display") Then
                            Try
                                regkey.DeleteSubKeyTree(child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("Intel")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Intel", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.ToLower.Contains("display") Then
                                    Try
                                        regkey.DeleteSubKeyTree(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next
                        If regkey.SubKeyCount = 0 Then
                            Try
                                My.Computer.Registry.Users.OpenSubKey(users & "\Software", True).DeleteSubKeyTree("Intel")
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Intel", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("igfx") Or _
                               child.ToLower.Contains("mediasdk") Or _
                               child.ToLower.Contains("opencl") Or _
                               child.ToLower.Contains("intel wireless display") Then
                                Try
                                    regkey.DeleteSubKeyTree(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True).DeleteSubKeyTree("Intel")
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If


        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteValue("IgfxTray")
                Catch ex As Exception
                    log(ex.Message + " IgfxTray")
                End Try

                Try
                    regkey.DeleteValue("Persistence")
                Catch ex As Exception
                    log(ex.Message + " Persistence")
                End Try

                Try
                    regkey.DeleteValue("HotKeysCmds")
                Catch ex As Exception
                    log(ex.Message + " HotKeysCmds")
                End Try
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
          ("Directory\background\shellex\ContextMenuHandlers", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("igfxcui") Or _
                           child.ToLower.Contains("igfxosp") Or _
                            child.ToLower.Contains("igfxdtcm") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            packages = IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\packages.cfg") '// add each line as String Array.
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("DisplayName")) = False Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To packages.Length - 1
                                            If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                If wantedvalue.ToLower.Contains(packages(i)) Then
                                                    Try
                                                        regkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If
        UpdateTextMethod(UpdateTextMethodmessage("6"))
    End Sub
    Private Sub checkpcieroot()  'This is for Nvidia Optimus to prevent the yellow mark on the PCI-E controler. We must remove the UpperFilters.

        UpdateTextMethod(UpdateTextMethodmessage("7"))

        log("Starting the removal of nVidia Optimus UpperFilter if present.")
        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("SYSTEM\CurrentControlSet\Enum\PCI")
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ven_8086") Then
                        subregkey = regkey.OpenSubKey(child)
                        If subregkey IsNot Nothing Then
                            For Each childs As String In subregkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(childs) = False Then
                                    array = subregkey.OpenSubKey(childs).GetValue("UpperFilters")    'do a .tostring here?
                                    If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
                                        For i As Integer = 0 To array.Length - 1
                                            If (Not checkvariables.isnullorwhitespace(array(i))) AndAlso (array(i).ToLower.Contains("nvpciflt")) Then

                                                log("nVidia Optimus UpperFilter Found.")

                                                Try
                                                    subregkey.OpenSubKey(childs, True).DeleteValue("UpperFilters")
                                                Catch ex As Exception
                                                    log(ex.Message + ex.StackTrace)
                                                    log("Failed to fix Optimus. You will have to manually remove the device with yellow mark in device manager to fix the missing videocard")
                                                End Try
                                            End If
                                        Next
                                    End If
                                End If
                            Next
                        End If
                    End If
                End If
            Next
        End If
    End Sub
    Private Sub rescan()

        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        scan.Arguments = "rescan"
        scan.UseShellExecute = False
        scan.CreateNoWindow = True
        scan.RedirectStandardOutput = False

        If reboot Then
            preventclose = False
            log("Restarting Computer ")
            System.Diagnostics.Process.Start("shutdown", "/r /t 0")
            Invoke(Sub() Me.Close())
            Exit Sub
        End If
        If shutdown Then
            preventclose = False
            System.Diagnostics.Process.Start("shutdown", "/s /t 0")
            Invoke(Sub() Me.Close())
            Exit Sub
        End If
        If reboot = False And shutdown = False Then
            UpdateTextMethod(UpdateTextMethodmessage("8"))
            log("Scanning for new device...")
            Dim proc4 As New Process
            proc4.StartInfo = scan
            proc4.Start()
            proc4.WaitForExit()
            System.Threading.Thread.Sleep(2000)
            If Not safemode Then
                Dim appproc = process.GetProcessesByName("explorer")
                For i As Integer = 0 To appproc.Length - 1
                    appproc(i).Kill()
                Next i
            End If

        End If
        UpdateTextMethod(UpdateTextMethodmessage("9"))

        log("Clean uninstall completed!")

    End Sub
    Private Sub Form1_close(sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        If preventclose Then
            e.Cancel = True
            Exit Sub
        End If
        If MyIdentity.IsSystem Then
            Try
                My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True).DeleteSubKeyTree("PAexec")
            Catch ex As Exception
            End Try
            Try
                My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True).DeleteSubKeyTree("PAexec")
            Catch ex As Exception
            End Try
        End If
    End Sub

    Public Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'We try to create config.cfg if non existant.
        If Not (File.Exists(Application.StartupPath & "\settings\config.cfg")) Then
            myExe = Application.StartupPath & "\settings\config.cfg"
            System.IO.File.WriteAllBytes(myExe, My.Resources.config)
        End If

        ''checking if we must open the webpage for donation (when not using system priviledge)
        'If Not MyIdentity.IsSystem Then
        '    Try
        '        Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")

        '        For i As Integer = 0 To lines.Length - 1
        '            If Not String.IsNullOrEmpty(lines(i)) Then
        '                If lines(i).ToLower.Contains("donate") Then
        '                    Dim webAddress As String = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"
        '                    process.Start(webAddress)
        '                    System.Array.Resize(lines, lines.Length - 1)
        '                    System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
        '                    preventclose = False
        '                    Me.Close()
        '                    Exit Sub
        '                End If
        '            End If
        '        Next
        '    Catch ex As Exception
        '        MsgBox(ex.Message + ex.StackTrace)
        '    End Try
        'End If

        Try
            My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True).CreateSubKey("PAexec")
            My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\PAexec", True).SetValue("", "Service")
        Catch ex As Exception
        End Try

        Try
            My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True).CreateSubKey("PAexec")
            My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\SafeBoot\Network\PAexec", True).SetValue("", "Service")
        Catch ex As Exception
        End Try

        loadinitiated = False

        CheckForIllegalCrossThreadCalls = True
        If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\DDU Logs") Then
            My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\DDU Logs")
        End If


        'read config file

        If settings.getconfig("logbox") = "true" Then
            CheckBox2.Checked = True

        Else
            CheckBox2.Checked = False
        End If

        If settings.getconfig("remove3dtvplay") = "true" Then
            CheckBox4.Checked = True
            remove3dtvplay = True
        Else
            CheckBox4.Checked = False
            remove3dtvplay = False
        End If

        If settings.getconfig("systemrestore") = "true" Then
            CheckBox5.Checked = True
        Else
            CheckBox5.Checked = False
        End If

        If settings.getconfig("removephysx") = "true" Then
            CheckBox3.Checked = True
            removephysx = True
        Else
            CheckBox3.Checked = False
            removephysx = False
        End If

        If ComboBox1.Text = "AMD" Then
            If settings.getconfig("removeamdaudiobus") = "true" Then
                CheckBox3.Checked = True
                removeamdaudiobus = True
            Else
                CheckBox3.Checked = False
                removeamdaudiobus = False
            End If
        End If


        '----------------
        'language section
        '----------------
        combobox2value = ComboBox2.Text
        Dim diChild() As String = Directory.GetDirectories(Application.StartupPath & "\settings\Languages")
        Dim list(diChild.Length - 1) As String
        For i As Integer = 0 To diChild.Length - 1
            If Not checkvariables.isnullorwhitespace(diChild(i)) Then
                Dim split As String() = diChild(i).Split("\")
                Dim parentFolder As String = split(split.Length - 1)
                list(i) = parentFolder
            End If
        Next

        ComboBox2.Items.AddRange(list)
        If settings.getconfig("language") = "" Then
            If System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("fr") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("French")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("es") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Spanish")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("nl") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Dutch")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("pt") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Portuguese")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("zh") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Chinese (Simplified)")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("sk") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Slovak")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("cs") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Czech")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("pl") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Polish")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("de") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("German")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("hu") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Hungarian")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("it") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Italian")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("he") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Hebrew")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("iw") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Hebrew")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("ru") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Russian")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("sv") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Swedish")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("el") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Greek")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("sr") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Serbian (Cyrilic)")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("ko") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Korean")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("da") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Danish")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("ja") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Japanese")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("jv") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Japanese")

            ElseIf System.Globalization.CultureInfo.CurrentCulture.ToString.ToLower.StartsWith("uk") Then
                ComboBox2.SelectedIndex = ComboBox2.FindString("Ukraine")

            Else
                ComboBox2.SelectedIndex = ComboBox2.FindString("English")
            End If

        Else
            ComboBox2.SelectedIndex = ComboBox2.FindString(settings.getconfig("language"))

        End If

        'check for admin before trying to do things, as this could cause errors and message boxes for rebooting into startup without admin are useless because you can't bcdedit without admin rights, however the next messagebox still plays the sound effect, for msgboxstyle.information. Not sure if this can be fixed.
        If Not isElevated Then

            MsgBox(msgboxmessage("2"), MsgBoxStyle.Critical)
            preventclose = False
            Me.Close()
            Exit Sub
        End If

        '----------------------
        'check computer/os info
        '----------------------

        Dim arch As Boolean

        version = My.Computer.Info.OSVersion
        Me.ComboBox1.SelectedIndex = 0
        If IntPtr.Size = 8 Then

            arch = True
            Try
                If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\x64") Then
                    My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\x64")
                End If
            Catch ex As Exception
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try

        ElseIf IntPtr.Size = 4 Then

            arch = False
            Try
                If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\x86") Then
                    My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\x86")
                End If
            Catch ex As Exception
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try

        End If

        If version < "5.1" Then

            Label2.Text = "Unsupported OS"
            log("Unsupported OS.")
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False
            Button4.Enabled = False
        End If

        If version.StartsWith("5.1") Then
            Label2.Text = "Windows XP or Server 2003"
            winxp = True
        End If

        If version.StartsWith("5.2") Then
            Label2.Text = "Windows XP or Server 2003"
            winxp = True
        End If

        If version.StartsWith("6.0") Then
            Label2.Text = "Windows Vista or Server 2008"
        End If

        If version.StartsWith("6.1") Then
            Label2.Text = "Windows 7 or Server 2008r2"
        End If

        If version.StartsWith("6.2") Then
            Label2.Text = "Windows 8 or Server 2012"
            win8higher = True
        End If

        If version.StartsWith("6.3") Then
            Label2.Text = "Windows 8.1"
            win8higher = True
        End If
        If version >= "6.4" Then
            Label2.Text = "Unsupported O.S"
            win8higher = True
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False
            Button4.Enabled = False
        End If


        If arch = True Then
            Label3.Text = "x64"
        Else
            Label3.Text = "x86"
        End If
        Label3.Refresh()
        ddudrfolder = Label3.Text


        myExe = Application.StartupPath & "\settings\paypal.bat"
        System.IO.File.WriteAllBytes(myExe, My.Resources.paypal)
        If arch = True Then
            Try

                If winxp Then  'XP64
                    myExe = Application.StartupPath & "\x64\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp64)
                    myExe = Application.StartupPath & "\x64\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl64)
                    myExe = Application.StartupPath & "\x64\paexec.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
                Else

                    myExe = Application.StartupPath & "\x64\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr64)

                    myExe = Application.StartupPath & "\x64\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl64)

                    myExe = Application.StartupPath & "\x64\paexec.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
                End If
                If version.StartsWith("6.2") Or version.StartsWith("6.3") Then
                    myExe = Application.StartupPath & "\x64\pnpldf.bkp"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.pnpldfwin8)
                End If
                If version.StartsWith("6.0") Or version.StartsWith("6.1") Then
                    myExe = Application.StartupPath & "\x64\pnpldf.bkp"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.pnpldfwin7vista)
                End If
            Catch ex As Exception
            End Try
        Else
            Try
                If winxp Then  'XP32
                    myExe = Application.StartupPath & "\x86\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp32)

                    myExe = Application.StartupPath & "\x86\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl32)

                    myExe = Application.StartupPath & "\x86\paexec.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
                Else 'all other 32 bits
                    myExe = Application.StartupPath & "\x86\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr32)

                    myExe = Application.StartupPath & "\x86\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl32)

                    myExe = Application.StartupPath & "\x86\paexec.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)

                    If version.StartsWith("6.2") Or version.StartsWith("6.3") Then
                        myExe = Application.StartupPath & "\x86\pnpldf.bkp"
                        System.IO.File.WriteAllBytes(myExe, My.Resources.pnpldfwin8)
                    End If
                    If version.StartsWith("6.0") Or version.StartsWith("6.1") Then
                        myExe = Application.StartupPath & "\x86\pnpldf.bkp"
                        System.IO.File.WriteAllBytes(myExe, My.Resources.pnpldfwin7vista)
                    End If
                End If

            Catch ex As Exception
            End Try
        End If

        If arch = True Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x64\ddudr.exe") Then
                MsgBox(msgboxmessage("3"), MsgBoxStyle.Critical)
                Button1.Enabled = False
                Button2.Enabled = False
                Button3.Enabled = False
                Exit Sub
            End If
        ElseIf arch = False Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x86\ddudr.exe") Then
                MsgBox(msgboxmessage("3"), MsgBoxStyle.Critical)
                Button1.Enabled = False
                Button2.Enabled = False
                Button3.Enabled = False
                Exit Sub
            End If
        End If

        'here I check if the process is running on system user account.

        '------------
        'Check update
        '------------
        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\label11.txt") '// add each line as String Array.
            Label11.Text = ""
            Label11.Text = Label11.Text & buttontext("0")
        Catch ex As Exception
        End Try
        Checkupdates2()
        If closeapp Then
            Exit Sub
        End If


        If Not MyIdentity.IsSystem Then
            Dim stopservice As New ProcessStartInfo
            stopservice.FileName = "cmd.exe"
            stopservice.Arguments = " /Csc stop PAExec"
            stopservice.UseShellExecute = False
            stopservice.CreateNoWindow = True
            stopservice.RedirectStandardOutput = False

            Dim processstopservice As New Process
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(10)

            stopservice.Arguments = " /Csc delete PAExec"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /Csc interrogate PAExec"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\paexec.exe"
            processinfo.Arguments = "-noname -i -s " & Chr(34) & Application.StartupPath & "\" & System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" & Chr(34)
            processinfo.UseShellExecute = False
            processinfo.CreateNoWindow = True
            processinfo.RedirectStandardOutput = False

            process.StartInfo = processinfo
            process.Start()
            preventclose = False
            Me.Close()
            Exit Sub
        End If

        Me.TopMost = True

        MsgBox(msgboxmessage("4"), MsgBoxStyle.Information)

        Me.TopMost = False

        UpdateTextMethod(UpdateTextMethodmessage("10") + Application.ProductVersion)
        log("DDU Version: " + Application.ProductVersion)
        log("OS: " + Label2.Text)
        log("Architecture: " & ddudrfolder)

        ''Videocard type indentification
        'processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        'processinfo.Arguments = "findall =display"
        'processinfo.UseShellExecute = False
        'processinfo.CreateNoWindow = True
        'processinfo.RedirectStandardOutput = True

        ''creation dun process fantome pour le wait on exit.

        ''------------------------------------------------------------------
        ''Detection of the current and leftover videocard for the textboxlog
        ''------------------------------------------------------------------
        'process.StartInfo = processinfo
        'process.Start()
        'reply = process.StandardOutput.ReadToEnd
        'process.WaitForExit()
        'log(reply)
        'TextLines = reply.Split(Environment.NewLine.ToCharArray, System.StringSplitOptions.RemoveEmptyEntries)
        'For i As Integer = 0 To TextLines.Length - 2  'reason of -2 instead of -1 , we dont want the last line of ddudr.
        '    If Not checkvariables.isnullorwhitespace(TextLines(i)) Then
        '        TextBox1.Text = TextBox1.Text + "Detected GPU : " + TextLines(i).Substring(TextLines(i).IndexOf(":") + 1) + vbNewLine
        '    End If
        'Next

        ''Trying to autoselect the right GPU cleanup option. 
        'If reply.Contains("VEN_8086") Then
        '    ComboBox1.SelectedIndex = 2
        'End If
        'If reply.Contains("VEN_10DE") Then
        '    ComboBox1.SelectedIndex = 0
        'End If
        'If reply.Contains("VEN_1002") Then
        '    ComboBox1.SelectedIndex = 1
        'End If


        ' ----------------------------------------------------------------------------
        ' Trying to get the installed GPU info 
        ' (These list the one that are at least installed with minimal driver support)
        ' ----------------------------------------------------------------------------

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames
                    If Not child.ToLower.Contains("properties") Then

                        subregkey = regkey.OpenSubKey(child)
                        If subregkey IsNot Nothing Then

                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("DriverDesc").ToString) Then
                                currentdriverversion = subregkey.GetValue("DriverDesc").ToString
                                UpdateTextMethod(UpdateTextMethodmessage("11") + " " + child + " " + UpdateTextMethodmessage("12") + " " + currentdriverversion)
                                log("GPU #" + child + " Detected : " + currentdriverversion)
                            End If
                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("MatchingDeviceId").ToString) Then
                                currentdriverversion = subregkey.GetValue("MatchingDeviceId").ToString
                                UpdateTextMethod(UpdateTextMethodmessage("13") + " " + currentdriverversion)
                                log("GPU DeviceId : " + currentdriverversion)
                                If currentdriverversion.ToLower.Contains("ven_8086") Then
                                    ComboBox1.SelectedIndex = 2
                                    PictureBox2.Location = New Point(333, 92)
                                    PictureBox2.Size = New Size(158, 126)
                                End If
                                If currentdriverversion.ToLower.Contains("ven_10de") Then
                                    ComboBox1.SelectedIndex = 0
                                    PictureBox2.Location = New Point(286, 92)
                                    PictureBox2.Size = New Size(252, 123)
                                End If
                                If currentdriverversion.ToLower.Contains("ven_1002") Then
                                    ComboBox1.SelectedIndex = 1
                                    PictureBox2.Location = New Point(333, 92)
                                    PictureBox2.Size = New Size(158, 126)
                                End If
                            End If

                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("HardwareInformation.BiosString")) Then
                                currentdriverversion = subregkey.GetValue("HardwareInformation.BiosString")
                                UpdateTextMethod("Vbios :" + " " + currentdriverversion)
                                log("Vbios : " + currentdriverversion)
                            End If

                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("DriverVersion").ToString) Then
                                currentdriverversion = subregkey.GetValue("DriverVersion").ToString
                                UpdateTextMethod(UpdateTextMethodmessage("14") + " " + currentdriverversion)
                                log("Detected Driver(s) Version(s) : " + currentdriverversion)
                            End If
                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("InfPath").ToString) Then
                                currentdriverversion = subregkey.GetValue("InfPath").ToString
                                UpdateTextMethod(UpdateTextMethodmessage("15") + " " + currentdriverversion)
                                log("INF : " + currentdriverversion)
                            End If
                            If Not checkvariables.isnullorwhitespace(subregkey.GetValue("InfSection").ToString) Then
                                currentdriverversion = subregkey.GetValue("InfSection").ToString
                                UpdateTextMethod(UpdateTextMethodmessage("16") + " " + currentdriverversion)
                                log("INF Section : " + currentdriverversion)
                            End If
                        End If
                        UpdateTextMethod("--------------")
                        log("--------------")
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        ' ----------------------------------------------------------------------------
        ' Trying to get the installed GPU info 
        ' (These list the one that are at least installed with minimal driver support)
        ' ----------------------------------------------------------------------------

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")

            For Each child As String In regkey.GetSubKeyNames
                If child.ToLower.Contains("ven_10de") Or _
                    child.ToLower.Contains("ven_8086") Or _
                   child.ToLower.Contains("ven_1002") Then

                    subregkey = regkey.OpenSubKey(child)
                    For Each child2 As String In subregkey.GetSubKeyNames

                        If Not checkvariables.isnullorwhitespace(subregkey.OpenSubKey(child2).GetValue("ClassGUID")) Then
                            'if the device does not return null here, it mean it has a class associated with it
                            'so we skip it as we search for missing one.
                            Continue For
                        End If

                        array = subregkey.OpenSubKey(child2).GetValue("CompatibleIDs")

                        If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
                            For i As Integer = 0 To array.Length - 1
                                If array(i).ToLower.Contains("pci\cc_03") Then
                                    If Not checkvariables.isnullorwhitespace(subregkey.OpenSubKey(child2).GetValue("DeviceDesc").ToString) Then
                                        currentdriverversion = subregkey.OpenSubKey(child2).GetValue("DeviceDesc").ToString
                                        UpdateTextMethod(UpdateTextMethodmessage("17") + " " + currentdriverversion)
                                        log("Not Correctly Installed GPU : " + currentdriverversion)
                                        UpdateTextMethod("--------------")
                                        log("--------------")
                                        Exit For  'we exit as we have found what we were looking for
                                    End If
                                End If
                            Next
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
            MsgBox(msgboxmessage("5"))
            log(ex.Message + ex.StackTrace)
        End Try


        ' -------------------------------------
        ' Check if this is an AMD Enduro system
        ' -------------------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
               ("SYSTEM\CurrentControlSet\Enum\PCI")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ven_8086") Then
                            Try
                                subregkey = regkey.OpenSubKey(child)
                            Catch ex As Exception
                                Continue For
                            End Try
                            For Each childs As String In subregkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(childs) = False Then
                                    If Not checkvariables.isnullorwhitespace(subregkey.OpenSubKey(childs).GetValue("Service")) Then
                                        If subregkey.OpenSubKey(childs).GetValue("Service").ToString.ToLower.Contains("amdkmdap") Then
                                            enduro = True
                                            TextBox1.Text = TextBox1.Text + "System seems to be an AMD Enduro (Intel)" + vbNewLine
                                            log("System seems to be an AMD Enduro (Intel)")
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        If enduro Then
            MsgBox(msgboxmessage("6"), MsgBoxStyle.Critical)
        End If

        'This code checks to see which mode Windows has booted up in.
        Select Case System.Windows.Forms.SystemInformation.BootMode
            Case BootMode.FailSafe
                'The computer was booted using only the basic files and drivers.
                'This is the same as Safe Mode
                safemode = True
                If winxp = False Then
                    Dim setbcdedit As New ProcessStartInfo
                    setbcdedit.FileName = "cmd.exe"
                    setbcdedit.Arguments = " /CBCDEDIT /deletevalue safeboot"
                    setbcdedit.UseShellExecute = False
                    setbcdedit.CreateNoWindow = True
                    setbcdedit.RedirectStandardOutput = False
                    Dim processstopservice As New Process
                    processstopservice.StartInfo = setbcdedit
                    processstopservice.Start()
                    processstopservice.WaitForExit()
                End If
            Case BootMode.FailSafeWithNetwork
                'The computer was booted using the basic files, drivers, and services necessary to start networking.
                'This is the same as Safe Mode with Networking
                'I am also removing the auto go into safemode with bcdedit
                safemode = True
                If winxp = False Then
                    Dim setbcdedit As New ProcessStartInfo
                    setbcdedit.FileName = "cmd.exe"
                    setbcdedit.Arguments = " /CBCDEDIT /deletevalue safeboot"
                    setbcdedit.UseShellExecute = False
                    setbcdedit.CreateNoWindow = True
                    setbcdedit.RedirectStandardOutput = False
                    Dim processstopservice As New Process
                    processstopservice.StartInfo = setbcdedit
                    processstopservice.Start()
                    processstopservice.WaitForExit()
                End If

            Case BootMode.Normal
                If CheckBox5.Checked = True Then
                    Form2.Show()
                    Try
                        log("Trying to Create a System Restored Point")
                        Dim SysterRestoredPoint = GetObject("winmgmts:\\.\root\default:Systemrestore")
                        If SysterRestoredPoint IsNot Nothing Then
                            'Replace 'My System Restored Point' with your ststem restored point
                            If SysterRestoredPoint.CreateRestorePoint("DDU System Restored Point", 0, 100) = 0 Then
                                log("System Restored Point Created")
                            Else
                                log("System Restored Point Could not Created!")
                            End If
                        End If
                    Catch ex As Exception
                        log(ex.Message)
                    End Try
                    Form2.Close()
                End If
                safemode = False

                If winxp = False And isElevated Then 'added iselevated so this will not try to boot into safe mode/boot menu without admin rights, as even with the admin check on startup it was for some reason still trying to gain registry access and throwing an exception

                    Dim resultmsgbox As Integer = MessageBox.Show(msgboxmessage("11"), "Safe Mode?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information)
                    If resultmsgbox = DialogResult.Cancel Then

                        preventclose = False
                        Me.Close()
                        Exit Sub
                    ElseIf resultmsgbox = DialogResult.No Then
                        'do nothing and continue without safe mode
                    ElseIf resultmsgbox = DialogResult.Yes Then


                        Dim setbcdedit As New ProcessStartInfo
                        setbcdedit.FileName = "cmd.exe"
                        setbcdedit.Arguments = " /CBCDEDIT /set safeboot minimal"
                        setbcdedit.UseShellExecute = False
                        setbcdedit.CreateNoWindow = True
                        setbcdedit.RedirectStandardOutput = False
                        Dim processstopservice As New Process
                        processstopservice.StartInfo = setbcdedit
                        processstopservice.Start()
                        processstopservice.WaitForExit()
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
                        If regkey IsNot Nothing Then
                            Try
                                regkey.SetValue("*loadDDU", "cmd /c start " & Chr(34) & Chr(34) & " /d " & Chr(34) & Application.StartupPath & Chr(34) & " " & Chr(34) & IO.Path.GetFileName(Application.ExecutablePath) & Chr(34))
                                regkey.SetValue("*UndoSM", "bcdedit /deletevalue safeboot")
                            Catch ex As Exception
                                log(ex.Message & ex.StackTrace)
                            End Try

                        End If
                        preventclose = False
                        System.Diagnostics.Process.Start("shutdown", "/r /t 0")
                        Me.Close()
                        Exit Sub
                    End If
                Else
                    MsgBox(msgboxmessage("7"))
                End If

        End Select


        'Check and log the driver from the driver store  ( oemxx.inf)
        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        processinfo.Arguments = "dp_enum"
        processinfo.UseShellExecute = False
        processinfo.CreateNoWindow = True
        processinfo.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        process.StartInfo = processinfo
        process.Start()
        reply = process.StandardOutput.ReadToEnd
        process.WaitForExit()

        log("ddudr DP_ENUM RESULT BELOW")
        log(reply)
        loadinitiated = True

    End Sub

    Public Sub TestDelete(ByVal folder As String)
        UpdateTextMethod(UpdateTextMethodmessage("18"))
        log("Deleting some specials folders, it could take some times...")
        'ensure that this folder can be accessed with current user ac.
        If Not Directory.Exists(folder) Then
            log("Folder " + folder + " doesn't exist.")
            Exit Sub
        End If

        'Get an object repesenting the directory path below
        Dim di As New DirectoryInfo(folder)

        'Traverse all of the child directors in the root; get to the lowest child
        'and delete all files, working our way back up to the top.  All files
        'must be deleted in the directory, before the directory itself can be deleted.
        'also if there is hidden / readonly / system attribute..  change those attribute.
        For Each diChild As DirectoryInfo In di.GetDirectories()
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
            If removephysx Then
                Try
                    If diChild.ToString.ToLower.Contains("nvidia demos") Then
                        'do nothing
                    Else
                        Try
                            TraverseDirectory(diChild)
                        Catch ex As Exception
                        End Try
                    End If

                Catch ex As Exception
                End Try
            Else
                If diChild.ToString.ToLower.Contains("physx") Or diChild.ToString.ToLower.Contains("nvidia demos") Then
                    'do nothing
                Else
                    Try
                        TraverseDirectory(diChild)
                    Catch ex As Exception
                    End Try
                End If
            End If
        Next

        'Finally, clean all of the files directly in the root directory
        CleanAllFilesInDirectory(di)
    End Sub


    Private Sub TraverseDirectory(ByVal di As DirectoryInfo)

        'If the current directory has more child directories, then continure
        'to traverse down until we are at the lowest level and remove
        'there hidden / readonly / system attribute..  At that point all of the
        'files will be deleted.
        For Each diChild As DirectoryInfo In di.GetDirectories()
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
            diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
            If removephysx Then
                If diChild.ToString.ToLower.Contains("nvidia demos") Then
                    'do nothing
                Else
                    Try
                        TraverseDirectory(diChild)
                    Catch ex As Exception
                    End Try
                End If
            Else
                If diChild.ToString.ToLower.Contains("physx") Or diChild.ToString.ToLower.Contains("nvidia demos") Then
                    'do nothing
                Else
                    Try
                        TraverseDirectory(diChild)
                    Catch ex As Exception
                    End Try
                End If
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
            End Try
        End If

    End Sub


    ''' Iterates through all files in the directory passed into
    ''' method and deletes them.
    ''' It may be necessary to wrap this call in impersonation or ensure parent directory
    ''' permissions prior, because delete permissions are not guaranteed.

    Private Sub CleanAllFilesInDirectory(ByVal DirectoryToClean As DirectoryInfo)

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
    End Sub


    Private Sub Cleanup(ByVal directory As String, ByVal KeepDur As Integer)
        'Code taken from my CoDUO FoV Changer program, thus why it uses a keepdur, 
        'it's supposed to delete logs older than whatever days. I set it to 2 seconds instead of modifying the code. Lol
        Try
            Dim logdir As New System.IO.DirectoryInfo(directory)
            For Each file As System.IO.FileInfo In logdir.GetFiles
                If (Now - file.CreationTime).Seconds > KeepDur Then
                    file.Delete()
                End If

            Next
        Catch ex As Exception
            log("")
            log("!! ERROR: " & ex.Message)
            log("")
        End Try
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged

        If CheckBox2.Checked = True Then
            settings.setconfig("logbox", "true")
        Else
            settings.setconfig("logbox", "false")
        End If

    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        about.Show()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        reboot = False
        shutdown = False
        combobox1value = ComboBox1.Text
        BackgroundWorker1.RunWorkerAsync(ComboBox1.Text)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        reboot = False
        shutdown = True
        combobox1value = ComboBox1.Text
        BackgroundWorker1.RunWorkerAsync(ComboBox1.Text)
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

        If combobox1value = "NVIDIA" Then

            If CheckBox3.Checked = True Then
                settings.setconfig("removephysx", "true")
                removephysx = True
            Else
                settings.setconfig("removephysx", "false")
                removephysx = False
            End If

        End If


        If combobox1value = "AMD" Then
            If CheckBox3.Checked = True Then
                settings.setconfig("removeamdaudiobus", "true")
                removeamdaudiobus = True
            Else
                settings.setconfig("removeamdaudiobus", "false")
                removeamdaudiobus = False
            End If
        End If

    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        combobox1value = ComboBox1.Text
        If combobox1value = "NVIDIA" Then
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text & vbNewLine
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
            If settings.getconfig("removephysx") = "true" Then
                CheckBox3.Checked = True
                removephysx = True
            Else
                CheckBox3.Checked = False
                removephysx = False
            End If
            CheckBox3.Visible = True
            CheckBox4.Visible = True
            PictureBox2.Location = New Point(286, 92)
            PictureBox2.Size = New Size(252, 123)
            PictureBox2.Image = My.Resources.NV_GF_GTX_preferred_badge_FOR_WEB_ONLY
        End If

        If combobox1value = "AMD" Then
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3amd.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text & vbNewLine
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
            If settings.getconfig("removeamdaudiobus") = "true" Then
                CheckBox3.Checked = True
                removeamdaudiobus = True
            Else
                CheckBox3.Checked = False
                removeamdaudiobus = False
            End If
            CheckBox3.Visible = True
            CheckBox4.Visible = False
            PictureBox2.Location = New Point(333, 92)
            PictureBox2.Size = New Size(158, 126)
            PictureBox2.Image = My.Resources.RadeonLogo1
        End If

        If ComboBox1.Text = "INTEL" Then
            CheckBox3.Visible = False
            CheckBox4.Visible = False
            PictureBox2.Location = New Point(333, 92)
            PictureBox2.Size = New Size(158, 126)
            PictureBox2.Image = My.Resources.intel_logo
        End If

    End Sub

    Public Sub log(ByVal value As String)
        If Me.CheckBox2.Checked = True Then
            Dim wlog As New IO.StreamWriter(locations, True)
            wlog.WriteLine(DateTime.Now & " >> " & value)
            wlog.Flush()
            wlog.Dispose()
            System.Threading.Thread.Sleep(20)  '20 millisecond stall (0.02 Seconds) just to be sure its really released.
        Else

        End If
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        Dim webAddress As String = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"
        ' process.Start(webAddress)
        Dim UserTokenHandle As IntPtr = IntPtr.Zero
        WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
        Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
        Dim StartInfo As New WindowsApi.STARTUPINFOW
        StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))


        'Try
        '    Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")

        '    'if we endup here, it mean the value is not found on .cfg so we add it.
        '    System.Array.Resize(lines, lines.Length + 1)
        '    lines(lines.Length - 1) = "Donate"
        '    System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)

        'Catch ex As Exception
        '    MsgBox(ex.Message + ex.StackTrace)
        'End Try

        If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\settings\paypal.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
        Else
            MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
        End If

        If Not UserTokenHandle = IntPtr.Zero Then
            WindowsApi.CloseHandle(UserTokenHandle)
        End If
    End Sub

    Private Sub VisitGuru3dNVIDIAThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dNVIDIAThreadToolStripMenuItem.Click
        process.Start("http://forums.guru3d.com/showthread.php?t=379506")
    End Sub

    Private Sub VisitGuru3dAMDThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dAMDThreadToolStripMenuItem.Click
        process.Start("http://forums.guru3d.com/showthread.php?t=379505")
    End Sub

    Private Sub VisitGeforceThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGeforceThreadToolStripMenuItem.Click
        process.Start("https://forums.geforce.com/default/topic/550192/geforce-drivers/display-driver-uninstaller-ddu-v6-2/")
    End Sub

    Private Sub SVNToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SVNToolStripMenuItem.Click
        process.Start("https://code.google.com/p/display-drivers-uninstaller/source/list")
    End Sub

    Private Sub VisitDDUHomepageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUHomepageToolStripMenuItem.Click
        process.Start("http://www.wagnardmobile.com")
    End Sub


    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, _
                     ByVal e As System.ComponentModel.DoWorkEventArgs) _
                     Handles BackgroundWorker1.DoWork


        UpdateTextMethod(UpdateTextMethodmessage("19"))

        preventclose = True
        Invoke(Sub() Button1.Enabled = False)
        Invoke(Sub() Button2.Enabled = False)
        Invoke(Sub() Button3.Enabled = False)
        Invoke(Sub() ComboBox1.Enabled = False)
        Invoke(Sub() CheckBox1.Enabled = False)
        Invoke(Sub() CheckBox2.Enabled = False)
        Invoke(Sub() CheckBox3.Enabled = False)
        Invoke(Sub() CheckBox4.Enabled = False)
        Invoke(Sub() CheckBox5.Enabled = False)

        If version >= "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
                If regkey.GetValue("SearchOrderConfig").ToString <> 0 Then
                    regkey.SetValue("SearchOrderConfig", 0)
                    MsgBox(msgboxmessage("8"))
                End If
            Catch ex As Exception
            End Try
        End If
        If version >= "6.0" And version < "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
                If regkey.GetValue("DontSearchWindowsUpdate").ToString <> 1 Then
                    regkey.SetValue("DontSearchWindowsUpdate", 1)
                    MsgBox(msgboxmessage("8"))
                End If
            Catch ex As Exception
            End Try
        End If

        Try

            If combobox1value = "AMD" Then
                vendidexpected = "VEN_1002"
                provider = "Provider: Advanced Micro Devices"
            End If

            If combobox1value = "NVIDIA" Then
                vendidexpected = "VEN_10DE"
                provider = "Provider: NVIDIA"
            End If

            If combobox1value = "INTEL" Then
                vendidexpected = "VEN_8086"
                provider = "Provider: Intel"
            End If

            UpdateTextMethod(UpdateTextMethodmessage("20") + " " & combobox1value & " " + UpdateTextMethodmessage("21"))
            log("Uninstalling " + combobox1value + " driver ...")
            UpdateTextMethod(UpdateTextMethodmessage("22"))

            Try
                If combobox1value = "NVIDIA" Then
                    temporarynvidiaspeedup()   'we do this If and until nvidia speed up their installer that is impacting "ddudr remove" of the GPU from device manager.
                End If
            Catch ex As Exception
            End Try


            '----------------------------------------------
            'Here I remove AMD HD Audio bus (System device)
            '----------------------------------------------

            ' First , get the ParentIdPrefix

            If removeamdaudiobus And combobox1value = "AMD" Then

                Try
                    If combobox1value = "AMD" Then
                        log("Trying to remove the AMD HD Audio BUS")
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\HDAUDIO")
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If child.ToLower.Contains("ven_1002") Then
                                        For Each ParentIdPrefix As String In regkey.OpenSubKey(child).GetSubKeyNames
                                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
                                            If subregkey IsNot Nothing Then
                                                For Each child2 As String In subregkey.GetSubKeyNames()
                                                    If checkvariables.isnullorwhitespace(child2) = False Then
                                                        If child2.ToLower.Contains("ven_1002") Then
                                                            For Each child3 As String In subregkey.OpenSubKey(child2).GetSubKeyNames()
                                                                If checkvariables.isnullorwhitespace(child3) = False Then
                                                                    array = subregkey.OpenSubKey(child2 & "\" & child3).GetValue("LowerFilters")
                                                                    If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
                                                                        For i As Integer = 0 To array.Length - 1
                                                                            If Not checkvariables.isnullorwhitespace(array(i)) Then
                                                                                If array(i).ToLower.Contains("amdkmafd") AndAlso ParentIdPrefix.ToLower.Contains(subregkey.OpenSubKey(child2 & "\" & child3).GetValue("ParentIdPrefix").ToString.ToLower) Then
                                                                                    log("Found an AMD audio controller bus !")
                                                                                    Try
                                                                                        log("array result: " + array(i))
                                                                                    Catch ex As Exception
                                                                                    End Try
                                                                                    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                                                                                    processinfo.Arguments = "remove =system " & Chr(34) & "*" & child2 & Chr(34)
                                                                                    processinfo.UseShellExecute = False
                                                                                    processinfo.CreateNoWindow = True
                                                                                    processinfo.RedirectStandardOutput = True
                                                                                    process.StartInfo = processinfo

                                                                                    process.Start()
                                                                                    reply2 = process.StandardOutput.ReadToEnd
                                                                                    process.WaitForExit()
                                                                                    log(reply2)
                                                                                    log("AMD HD Audio Bus Removed !")
                                                                                End If
                                                                            End If
                                                                        Next
                                                                    End If
                                                                End If
                                                            Next
                                                        End If
                                                    End If
                                                Next
                                            End If
                                        Next
                                    End If
                                End If
                            Next
                        End If
                    End If
                Catch ex As Exception
                    log(ex.Message + ex.StackTrace)
                End Try

                'Verification is there is still an AMD HD Audio Bus device and set donotremoveamdhdaudiobusfiles to true if thats the case
                Try
                    donotremoveamdhdaudiobusfiles = False
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child2) = False Then
                                If child2.ToLower.Contains("ven_1002") Then
                                    For Each child3 As String In subregkey.OpenSubKey(child2).GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child3) = False Then
                                            array = subregkey.OpenSubKey(child2 & "\" & child3).GetValue("LowerFilters")
                                            If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
                                                For i As Integer = 0 To array.Length - 1
                                                    If Not checkvariables.isnullorwhitespace(array(i)) Then
                                                        If array(i).ToLower.Contains("amdkmafd") Then
                                                            log("Found a remaining AMD audio controller bus ! Preventing the removal of its driverfiles.")
                                                            donotremoveamdhdaudiobusfiles = True
                                                        End If
                                                    End If
                                                Next
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        Next
                    End If
                Catch ex As Exception
                    log(ex.Message + ex.StackTrace)
                End Try

            End If

            ' ----------------------
            ' Removing the videocard
            ' ----------------------

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames
                        If child.ToLower.Contains("ven_10de") Or _
                            child.ToLower.Contains("ven_8086") Or _
                           child.ToLower.Contains("ven_1002") Then

                            subregkey = regkey.OpenSubKey(child)
                            If subregkey IsNot Nothing Then

                                For Each child2 As String In subregkey.GetSubKeyNames

                                    If subregkey.OpenSubKey(child2) Is Nothing Then
                                        Continue For
                                    End If

                                    array = subregkey.OpenSubKey(child2).GetValue("CompatibleIDs")

                                    If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
                                        For i As Integer = 0 To array.Length - 1
                                            If array(i).ToLower.Contains("pci\cc_03") Then

                                                vendid = child & "\" & child2

                                                If vendid.ToLower.Contains(vendidexpected.ToLower) Then
                                                    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                                                    processinfo.Arguments = "remove " & Chr(34) & "@pci\" & vendid & "*" & Chr(34)
                                                    processinfo.UseShellExecute = False
                                                    processinfo.CreateNoWindow = True
                                                    processinfo.RedirectStandardOutput = True
                                                    process.StartInfo = processinfo

                                                    process.Start()
                                                    reply2 = process.StandardOutput.ReadToEnd
                                                    process.WaitForExit()
                                                    log(reply2)

                                                End If
                                                Exit For
                                            End If
                                        Next
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                MsgBox(msgboxmessage("5"))
                log(ex.Message + ex.StackTrace)
            End Try

            'If combobox1value= "AMD" & enduro Then
            '    ' ----------------------------------------------------------------------
            '    ' (Experimental) Removing the Intel card because of AMD Enduro videocard
            '    ' ----------------------------------------------------------------------

            '    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            '    processinfo.Arguments = "findall =display"
            '    processinfo.UseShellExecute = False
            '    processinfo.CreateNoWindow = True
            '    processinfo.RedirectStandardOutput = True

            '    process.StartInfo = processinfo
            '    process.Start()
            '    reply = process.StandardOutput.ReadToEnd
            '    process.WaitForExit()

            '    Try
            '        card1 = reply.IndexOf("PCI\")
            '    Catch ex As Exception

            '    End Try
            '    While card1 > -1
            '        position2 = reply.IndexOf(":", card1)
            '        vendid = reply.Substring(card1, position2 - card1).Trim
            '        If vendid.Contains("VEN_8086") Then
            '            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            '            processinfo.Arguments = "remove =display " & Chr(34) & "@" & vendid & Chr(34)
            '            processinfo.UseShellExecute = False
            '            processinfo.CreateNoWindow = True
            '            processinfo.RedirectStandardOutput = True
            '            process.StartInfo = processinfo

            '            process.Start()
            '            reply2 = process.StandardOutput.ReadToEnd
            '            process.WaitForExit()
            '            log(reply2)



            '        End If
            '        card1 = reply.IndexOf("PCI\", card1 + 1)
            '    End While
            'End If
            UpdateTextMethod(UpdateTextMethodmessage("23"))
            log("DDUDR Remove Display Driver: Complete.")

            cleandriverstore()

            UpdateTextMethod(UpdateTextMethodmessage("24"))
            log("Executing DDUDR Remove Audio controler.")
            'Next
            'For i As Integer = 0 To 1 'loop 2 time to check if there is a remaining videocard.
            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            processinfo.Arguments = "findall =media"
            processinfo.UseShellExecute = False
            processinfo.CreateNoWindow = True
            processinfo.RedirectStandardOutput = True

            'creation dun process fantome pour le wait on exit.

            process.StartInfo = processinfo
            process.Start()
            reply = process.StandardOutput.ReadToEnd
            process.WaitForExit()


            Try
                card1 = reply.IndexOf("HDAUDIO\")


                While card1 > -1

                    position2 = reply.IndexOf(":", card1)
                    vendid = reply.Substring(card1, position2 - card1).Trim
                    If vendid.Contains(vendidexpected) Then

                        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                        processinfo.Arguments = "remove =MEDIA " & Chr(34) & "@" & vendid & Chr(34)
                        processinfo.UseShellExecute = False
                        processinfo.CreateNoWindow = True
                        processinfo.RedirectStandardOutput = True
                        process.StartInfo = processinfo
                        Try
                            process.Start()
                            reply2 = process.StandardOutput.ReadToEnd
                            process.WaitForExit()
                            log(reply2)
                        Catch ex As Exception
                            preventclose = False
                            log(ex.Message)
                            Button1.Enabled = True
                            Button2.Enabled = True
                            Button3.Enabled = True
                        End Try


                    End If
                    card1 = reply.IndexOf("HDAUDIO\", card1 + 1)

                End While

            Catch ex As Exception
                MsgBox(msgboxmessage("5"))
                log(ex.Message + ex.StackTrace)
            End Try

            UpdateTextMethod(UpdateTextMethodmessage("25"))


        log("DDUDR Remove Audio controler Complete.")

        If Not combobox1value = "INTEL" Then
            cleandriverstore()
        End If

        'Here I remove 3dVision USB Adapter.

            If combobox1value = "NVIDIA" Then
                Try
                    'removing 3DVision USB driver
                    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                    processinfo.Arguments = "findall =USB"
                    processinfo.UseShellExecute = False
                    processinfo.CreateNoWindow = True
                    processinfo.RedirectStandardOutput = True

                    'creation dun process fantome pour le wait on exit.

                    process.StartInfo = processinfo
                    process.Start()
                    reply = process.StandardOutput.ReadToEnd
                    process.WaitForExit()

                    Try
                        card1 = reply.IndexOf("USB\")
                    Catch ex As Exception
                    End Try

                    While card1 > -1

                        position2 = reply.IndexOf(":", card1)
                        vendid = reply.Substring(card1, position2 - card1).Trim
                        If vendid.Contains("USB\VID_0955&PID_0007") Or _
                            vendid.Contains("USB\VID_0955&PID_7001") Or _
                            vendid.Contains("USB\VID_0955&PID_7002") Or _
                            vendid.Contains("USB\VID_0955&PID_7003") Or _
                            vendid.Contains("USB\VID_0955&PID_7004") Or _
                            vendid.Contains("USB\VID_0955&PID_7008") Or _
                            vendid.Contains("USB\VID_0955&PID_7009") Or _
                            vendid.Contains("USB\VID_0955&PID_700A") Or _
                            vendid.Contains("USB\VID_0955&PID_700C") Or _
                            vendid.Contains("USB\VID_0955&PID_700D&MI_00") Or _
                            vendid.Contains("USB\VID_0955&PID_700E&MI_00") Then
                            log("-" & vendid & "- 3D vision usb controler found")

                            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                            processinfo.Arguments = "remove =USB " & Chr(34) & vendid & Chr(34)
                            processinfo.UseShellExecute = False
                            processinfo.CreateNoWindow = True
                            processinfo.RedirectStandardOutput = True
                            process.StartInfo = processinfo

                            process.Start()
                            reply2 = process.StandardOutput.ReadToEnd
                            process.WaitForExit()
                            log(reply2)


                        End If
                        card1 = reply.IndexOf("USB\", card1 + 1)

                    End While

                Catch ex As Exception
                    MsgBox(msgboxmessage("5"))
                    log(ex.Message + ex.StackTrace)
                End Try

                UpdateTextMethod(UpdateTextMethodmessage("26"))

                Try
                    'Removing NVIDIA Virtual Audio Device (Wave Extensible) (WDM)

                    log("Trying to remove NVIDIA Virtual Audio Device (Wave Extensible) (WDM) if present!")
                    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                    processinfo.Arguments = "remove =media " & Chr(34) & "USB\VID_0955&PID_9000" & Chr(34)
                    processinfo.UseShellExecute = False
                    processinfo.CreateNoWindow = True
                    processinfo.RedirectStandardOutput = True
                    process.StartInfo = processinfo

                    process.Start()
                    reply2 = process.StandardOutput.ReadToEnd
                    process.WaitForExit()
                    log(reply2)

                    ' ------------------------------
                    ' Removing nVidia AudioEndpoints
                    ' ------------------------------
                    log("Removing nVidia Audio Endpoints")
                    processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                    processinfo.Arguments = "findall =audioendpoint"
                    processinfo.UseShellExecute = False
                    processinfo.CreateNoWindow = True
                    processinfo.RedirectStandardOutput = True

                    process.StartInfo = processinfo
                    process.Start()
                    reply = process.StandardOutput.ReadToEnd
                    process.WaitForExit()

                    Dim audioendpoints As String() = reply.Split(Environment.NewLine.ToCharArray, System.StringSplitOptions.RemoveEmptyEntries)

                    For Each child As String In audioendpoints
                        If Not checkvariables.isnullorwhitespace(child) Then
                            If child.ToLower.Contains("nvidia virtual audio device") Or _
                                child.ToLower.Contains("nvidia high definition audio") Then
                                child = child.Substring(0, child.IndexOf(":"))
                                processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                                processinfo.Arguments = "remove =audioendpoint " & Chr(34) & "@" & child & Chr(34)
                                processinfo.UseShellExecute = False
                                processinfo.CreateNoWindow = True
                                processinfo.RedirectStandardOutput = True
                                process.StartInfo = processinfo

                                process.Start()
                                reply2 = process.StandardOutput.ReadToEnd
                                process.WaitForExit()
                                log(reply2)
                            End If
                        End If
                    Next
                Catch ex As Exception
                    MsgBox(msgboxmessage("5"))
                    log(ex.Message + ex.StackTrace)
                End Try
            End If


        If combobox1value = "INTEL" Then
            'Removing Intel WIdI bus Enumerator
                log("Removing IWD Bus Enumerator")
            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            processinfo.Arguments = "remove =system " & Chr(34) & "root\iwdbus" & Chr(34)
            processinfo.UseShellExecute = False
            processinfo.CreateNoWindow = True
            processinfo.RedirectStandardOutput = True
            process.StartInfo = processinfo

            process.Start()
            reply2 = process.StandardOutput.ReadToEnd
            process.WaitForExit()
            log(reply2)


            ' ------------------------------
            ' Removing Intel AudioEndpoints
            ' ------------------------------
            log("Removing nVidia Audio Endpoints")
            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            processinfo.Arguments = "findall =audioendpoint"
            processinfo.UseShellExecute = False
            processinfo.CreateNoWindow = True
            processinfo.RedirectStandardOutput = True

            process.StartInfo = processinfo
            process.Start()
            reply = process.StandardOutput.ReadToEnd
            process.WaitForExit()

            Dim audioendpoints As String() = reply.Split(Environment.NewLine.ToCharArray, System.StringSplitOptions.RemoveEmptyEntries)

            For Each child As String In audioendpoints
                If Not checkvariables.isnullorwhitespace(child) Then
                    If child.ToLower.Contains("intel widi audio") Then
                        child = child.Substring(0, child.IndexOf(":"))
                        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                        processinfo.Arguments = "remove =audioendpoint " & Chr(34) & "@" & child & Chr(34)
                        processinfo.UseShellExecute = False
                        processinfo.CreateNoWindow = True
                        processinfo.RedirectStandardOutput = True
                        process.StartInfo = processinfo

                        process.Start()
                        reply2 = process.StandardOutput.ReadToEnd
                        process.WaitForExit()
                        log(reply2)
                    End If
                End If
            Next
        End If


        log("ddudr Remove Audio/HDMI Complete")
        'removing monitor and hidden monitor

        log("ddudr Remove Monitor started")

        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        processinfo.Arguments = "findall =monitor"
        processinfo.UseShellExecute = False
        processinfo.CreateNoWindow = True
        processinfo.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        process.StartInfo = processinfo
        process.Start()
        reply = process.StandardOutput.ReadToEnd
        process.WaitForExit()
        Try
            card1 = reply.IndexOf("DISPLAY\")
        Catch ex As Exception

        End Try
        While card1 > -1

            position2 = reply.IndexOf(":", card1)
            vendid = reply.Substring(card1, position2 - card1).Trim


            log("-" & vendid & "- Monitor id found")
            'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
            processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
            processinfo.Arguments = "remove =monitor " & Chr(34) & "@" & vendid & Chr(34)
            processinfo.UseShellExecute = False
            processinfo.CreateNoWindow = True
            processinfo.RedirectStandardOutput = True
            process.StartInfo = processinfo

                process.Start()
                reply2 = process.StandardOutput.ReadToEnd
                process.WaitForExit()
                log(reply2)


                card1 = reply.IndexOf("DISPLAY\", card1 + 1)

            End While
            UpdateTextMethod(UpdateTextMethodmessage("27"))
            UpdateTextMethod(UpdateTextMethodmessage("28"))


            If combobox1value = "AMD" Then
                cleanamdserviceprocess()
                cleanamd()

                log("Killing Explorer.exe")
                Dim appproc = process.GetProcessesByName("explorer")
                For i As Integer = 0 To appproc.Length - 1
                    appproc(i).Kill()
                Next i
                cleanamdfolders()
            End If

            If combobox1value = "NVIDIA" Then
                cleannvidiaserviceprocess()
                cleannvidia()

                log("Killing Explorer.exe")
                Dim appproc = process.GetProcessesByName("explorer")
                For i As Integer = 0 To appproc.Length - 1
                    appproc(i).Kill()
                Next i
                cleannvidiafolders()
                checkpcieroot()
            End If

            If combobox1value = "INTEL" Then
                cleanintelserviceprocess()
                cleanintel()
                Dim appproc = process.GetProcessesByName("explorer")
                For i As Integer = 0 To appproc.Length - 1
                    appproc(i).Kill()
                Next i
                cleanintelfolders()
            End If

            cleandriverstore()

            rescan()

        Catch ex As Exception
            log(ex.Message & ex.StackTrace)
            MsgBox(msgboxmessage("5"), MsgBoxStyle.Critical)
            stopme = True
        End Try

    End Sub
    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As System.Object, _
                             ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) _
                             Handles BackgroundWorker1.RunWorkerCompleted


        If stopme = True Then
            'Scan for new hardware to not let users into a non working state.


            Try
                Dim scan As New ProcessStartInfo
                scan.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
                scan.Arguments = "rescan"
                scan.UseShellExecute = False
                scan.CreateNoWindow = True
                scan.RedirectStandardOutput = False
                Dim proc4 As New Process
                proc4.StartInfo = scan
                proc4.Start()
                proc4.WaitForExit()
            Catch ex As Exception
            End Try
            'then quit
            preventclose = False
            Me.Close()
            Exit Sub
        End If
        Button1.Enabled = True
        Button2.Enabled = True
        Button3.Enabled = True
        ComboBox1.Enabled = True
        CheckBox1.Enabled = True
        CheckBox2.Enabled = True
        CheckBox3.Enabled = True
        CheckBox4.Enabled = True
        CheckBox5.Enabled = True

        If Not reboot And Not shutdown Then
            If MsgBox(msgboxmessage("9"), MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                'do nothing
            Else
                preventclose = False
                Me.Close()
            End If
        End If
        preventclose = False
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If version >= "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
                regkey.SetValue("SearchOrderConfig", 1)
                MsgBox(msgboxmessage("10"))
            Catch ex As Exception
            End Try
        End If
        If version >= "6.0" And version < "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
                regkey.SetValue("DontSearchWindowsUpdate", 0)
                MsgBox(msgboxmessage("10"))
            Catch ex As Exception
            End Try
        End If
    End Sub
    Private Sub initlanguage()

        Try
            toolTip1.AutoPopDelay = 3000
            toolTip1.InitialDelay = 1000
            toolTip1.ReshowDelay = 250
            toolTip1.ShowAlways = True


            toolTip1.SetToolTip(Me.Button1, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & combobox2value & "\tooltip1.txt")) '// add each line as String Array.)
            Button1.Text = ""
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\button1.txt") '// add each line as String Array.
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button1.Text = Button1.Text & vbNewLine
                End If
                Button1.Text = Button1.Text & buttontext(i)
            Next


            toolTip1.SetToolTip(Me.Button2, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & combobox2value & "\tooltip2.txt")) '// add each line as String Array.)
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\button2.txt") '// add each line as String Array.
            Button2.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button2.Text = Button2.Text & vbNewLine
                End If
                Button2.Text = Button2.Text & buttontext(i)
            Next


            toolTip1.SetToolTip(Me.Button3, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & combobox2value & "\tooltip3.txt")) '// add each line as String Array.)
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\button3.txt") '// add each line as String Array.
            Button3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button3.Text = Button3.Text & vbNewLine
                End If
                Button3.Text = Button3.Text & buttontext(i)
            Next


            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\button4.txt") '// add each line as String Array.
            Button4.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button4.Text = Button4.Text & vbNewLine
                End If
                Button4.Text = Button4.Text & buttontext(i)
            Next

            Label11.Text = ("")

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\label1.txt") '// add each line as String Array.
            Label1.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label1.Text = Label1.Text & vbNewLine
                End If
                Label1.Text = Label1.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\label4.txt") '// add each line as String Array.
            Label4.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label4.Text = Label4.Text & vbNewLine
                End If
                Label4.Text = Label4.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\label5.txt") '// add each line as String Array.
            Label5.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label5.Text = Label5.Text & vbNewLine
                End If
                Label5.Text = Label5.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\label7.txt") '// add each line as String Array.
            Label7.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label7.Text = Label7.Text & vbNewLine
                End If
                Label7.Text = Label7.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\label10.txt") '// add each line as String Array.
            Label10.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label10.Text = Label10.Text & vbNewLine
                End If
                Label10.Text = Label10.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox1.txt") '// add each line as String Array.
            CheckBox1.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox1.Text = CheckBox1.Text & vbNewLine
                End If
                CheckBox1.Text = CheckBox1.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox2.txt") '// add each line as String Array.
            CheckBox2.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox2.Text = CheckBox2.Text & vbNewLine
                End If
                CheckBox2.Text = CheckBox2.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox4.txt") '// add each line as String Array.
            CheckBox4.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox4.Text = CheckBox4.Text & vbNewLine
                End If
                CheckBox4.Text = CheckBox4.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text & vbNewLine
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next

            If ComboBox1.Text = "AMD" Then
                buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3amd.txt") '// add each line as String Array.
                CheckBox3.Text = ""
                For i As Integer = 0 To buttontext.Length - 1
                    If i <> 0 Then
                        CheckBox3.Text = CheckBox3.Text & vbNewLine
                    End If
                    CheckBox3.Text = CheckBox3.Text & buttontext(i)
                Next
            End If

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox5.txt") '// add each line as String Array.
            CheckBox5.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox5.Text = CheckBox5.Text & vbNewLine
                End If
                CheckBox5.Text = CheckBox5.Text & buttontext(i)
            Next

            msgboxmessage = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\msgbox.txt") '// add each line as String Array.
            UpdateTextMethodmessage = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & ComboBox2.Text & "\updatetextmethod.txt") '// add each line as String Array.
        Catch ex As Exception
            log(ex.Message)
        End Try
    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        combobox2value = ComboBox2.Text
        settings.setconfig("language", combobox2value)
        initlanguage()
    End Sub

    Private Sub ToSToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToSToolStripMenuItem.Click
        MessageBox.Show(IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & combobox2value & "\tos.txt"), "ToS")
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged

        If CheckBox4.Checked = True Then
            settings.setconfig("remove3dtvplay", "true")
            remove3dtvplay = True
        Else
            settings.setconfig("remove3dtvplay", "false")
            remove3dtvplay = False
        End If

    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged

        If CheckBox5.Checked = True Then
            settings.setconfig("systemrestore", "true")
            systemrestore = True

        Else
            settings.setconfig("systemrestore", "false")
            systemrestore = False
        End If

    End Sub

    Private Sub temporarynvidiaspeedup()   'we do this to speedup the removal of the nividia display driver because of the huge time the nvidia installer files take to do unknown stuff.
        Try
            filePath = Environment.GetFolderPath _
      (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("installer2") Then
                        For Each child2 As String In Directory.GetDirectories(child)
                            If checkvariables.isnullorwhitespace(child2) = False Then
                                If child2.ToLower.Contains("display.3dvision") Or _
                                   child2.ToLower.Contains("display.controlpanel") Or _
                                   child2.ToLower.Contains("display.driver") Or _
                                   child2.ToLower.Contains("display.gfexperience") Or _
                                   child2.ToLower.Contains("display.nvirusb") Or _
                                   child2.ToLower.Contains("display.physx") Or _
                                   child2.ToLower.Contains("display.update") Or _
                                   child2.ToLower.Contains("gfexperience") Or _
                                   child2.ToLower.Contains("nvidia.update") Or _
                                   child2.ToLower.Contains("installer2\installer") Or _
                                   child2.ToLower.Contains("network.service") Or _
                                   child2.ToLower.Contains("miracast.virtualaudio") Or _
                                   child2.ToLower.Contains("shadowplay") Or _
                                   child2.ToLower.Contains("update.core") Or _
                                   child2.ToLower.Contains("virtualaudio.driver") Or _
                                   child2.ToLower.Contains("hdaudio.driver") Then
                                    If (removephysx Or Not ((Not removephysx) And child2.ToLower.Contains("physx"))) Then
                                        Try
                                            My.Computer.FileSystem.DeleteDirectory _
                                            (child2, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                        Catch ex As Exception
                                            log(ex.Message)
                                            TestDelete(child2)
                                        End Try
                                    End If
                                End If
                            End If
                        Next
                        Try
                            If Directory.GetDirectories(child).Length = 0 Then
                                Try
                                    My.Computer.FileSystem.DeleteDirectory _
                                        (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
    End Sub
    Public Sub UpdateTextMethod(ByVal strMessage As String)

        If TextBox1.InvokeRequired Then
            Invoke(Sub() TextBox1.Text = TextBox1.Text + strMessage + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
        Else
            TextBox1.Text = TextBox1.Text + strMessage + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
        End If

    End Sub

End Class
Public Class checkvariables

    Public Function isnullorwhitespace(ByVal stringtocheck As String) As Boolean
        If String.IsNullOrEmpty(Trim(stringtocheck)) = True Then
            Return True
        Else
            Return False
        End If
    End Function
End Class

Public Class genericfunction
    Public Function checkupdates() As Integer

        Try
            Dim request2 As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create("http://www.wagnardmobile.com/DDU/currentversion2.txt")
            Dim response2 As System.Net.HttpWebResponse = Nothing
            request2.Timeout = 2500
            Try
                response2 = request2.GetResponse()
            Catch ex As Exception
                request2 = System.Net.HttpWebRequest.Create("http://archive.sunet.se/pub/games/PC/guru3d/ddu/currentversion2.txt")
            End Try
            request2.Timeout = 2500
            response2 = request2.GetResponse()
            Dim sr As System.IO.StreamReader = New System.IO.StreamReader(response2.GetResponseStream())

            Dim newestversion2 As String = sr.ReadToEnd()
            Dim newestversion2int As Integer = newestversion2.Replace(".", "")
            Dim applicationversion As Integer = Application.ProductVersion.Replace(".", "")

            If newestversion2int <= applicationversion Then
                Return 1
            Else
                Return 2
            End If

        Catch ex As Exception
            Return 3
        End Try
    End Function
    Public Function getconfig(ByVal options As String) As String

        Try
            Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
            For i As Integer = 0 To lines.Length - 1
                If Not String.IsNullOrEmpty(lines(i)) Then
                    If lines(i).ToLower.Contains(options.ToLower) Then
                        Return lines(i).ToLower.Replace(options + "=", "")
                    End If
                End If
            Next
            'if we endup here, it mean the value is not found on .cfg so we add it.
            Array.Resize(lines, lines.Length + 1)
            lines(lines.Length - 1) = options + "=false"
            System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)

            Return False
        Catch ex As Exception
            MsgBox(ex.Message + ex.StackTrace)
            Return False
        End Try

    End Function
    Public Sub setconfig(ByVal name As String, ByVal setvalue As String)
        Try
            Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
            For i As Integer = 0 To lines.Length - 1
                If Not String.IsNullOrEmpty(lines(i)) Then
                    If lines(i).ToLower.Contains(name) Then
                        lines(i) = name + "=" + setvalue
                        System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
    End Sub
End Class

Public Class CleanupEngine

    Dim checkvariables As New checkvariables

    Public Sub classroot(ByVal classroot As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String = Nothing
        Dim appid As String = Nothing
        Dim typelib As String = Nothing

        f.log("Begin classroot CleanUP")

        Try
            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        For i As Integer = 0 To classroot.Length - 1
                            If Not checkvariables.isnullorwhitespace(classroot(i)) Then
                                If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                    If subregkey IsNot Nothing Then
                                        If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                            wantedvalue = subregkey.GetValue("").ToString
                                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                                Try
                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID")) Then
                                                            appid = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("")) Then
                                                            typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True).DeleteSubKeyTree(wantedvalue)

                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    End If
                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree(child)
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            f.log(ex.Message + ex.StackTrace)
        End Try
        f.log("End classroot CleanUP")
    End Sub

    Public Sub installer(ByVal packages As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim regkey As RegistryKey
        Dim basekey As RegistryKey
        Dim superregkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim subsuperregkey As RegistryKey
        Dim wantedvalue As String = Nothing
        Dim removephysx As Boolean = f.removephysx
        Dim msgboxmessage As String() = f.msgboxmessage
        Dim updateTextMethodmessage As String() = f.UpdateTextMethodmessage
        f.UpdateTextMethod(UpdateTextMethodmessage("29"))

        Try
            f.log("-Starting S-1-5-xx region cleanUP")
            basekey = My.Computer.Registry.LocalMachine.OpenSubKey _
                  ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(super) = False Then
                        If super.ToLower.Contains("s-1-5") Then

                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)

                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then

                                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                            "\InstallProperties", False)

                                        If subregkey IsNot Nothing Then
                                            If checkvariables.isnullorwhitespace(subregkey.GetValue("DisplayName")) = False Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                                    For i As Integer = 0 To packages.Length - 1
                                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And _
                                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then

                                                                Try
                                                                    regkey.DeleteSubKeyTree(child)
                                                                Catch ex As Exception
                                                                End Try

                                                                superregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
    ("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes", True)
                                                                If superregkey IsNot Nothing Then
                                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                                        If checkvariables.isnullorwhitespace(child2) = False Then

                                                                            subsuperregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\" & child2, False)

                                                                            If subsuperregkey IsNot Nothing Then
                                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                                    If checkvariables.isnullorwhitespace(wantedstring) = False Then
                                                                                        If wantedstring.Contains(child) Then
                                                                                            Try
                                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                                            Catch ex As Exception
                                                                                            End Try
                                                                                        End If
                                                                                    End If
                                                                                Next
                                                                            End If
                                                                        End If
                                                                    Next
                                                                End If
                                                                superregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
    ("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)
                                                                If superregkey IsNot Nothing Then
                                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                                        If checkvariables.isnullorwhitespace(child2) = False Then

                                                                            subsuperregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child2, False)


                                                                            If subsuperregkey IsNot Nothing Then
                                                                                For Each wantedstring In subsuperregkey.GetValueNames()
                                                                                    If checkvariables.isnullorwhitespace(wantedstring) = False Then
                                                                                        If wantedstring.Contains(child) Then
                                                                                            Try
                                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                                            Catch ex As Exception
                                                                                            End Try
                                                                                        End If
                                                                                    End If
                                                                                Next
                                                                            End If
                                                                        End If
                                                                    Next
                                                                End If
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        End If
                    End If
                Next
            End If
            f.UpdateTextMethod(updateTextMethodmessage("30"))
            f.log("-End of S-1-5-xx region cleanUP")
        Catch ex As Exception
            MsgBox(msgboxmessage("5"))
            f.log(ex.Message + ex.StackTrace)
        End Try

        f.UpdateTextMethod(updateTextMethodmessage("31"))
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
("Installer\Products\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And _
                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("Installer\Features", True).DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                                superregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                     ("Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        If checkvariables.isnullorwhitespace(child2) = False Then

                                                            subsuperregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                        ("Installer\UpgradeCodes\" & child2, False)

                                                            If subsuperregkey IsNot Nothing Then
                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                    If checkvariables.isnullorwhitespace(wantedstring) = False Then
                                                                        If wantedstring.Contains(child) Then
                                                                            Try
                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                            Catch ex As Exception
                                                                            End Try
                                                                        End If
                                                                    End If
                                                                Next
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
            f.UpdateTextMethod(updateTextMethodmessage("32"))
        Catch ex As Exception
            MsgBox(msgboxmessage("5"))
            f.log(ex.Message + ex.StackTrace)
        End Try


        f.UpdateTextMethod(updateTextMethodmessage("33"))

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Classes\Installer\Products\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And _
                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Installer\Features", True).DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try

                                                superregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                     ("Software\Classes\Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        If checkvariables.isnullorwhitespace(child2) = False Then

                                                            subsuperregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                                        ("Software\Classes\Installer\UpgradeCodes\" & child2, False)

                                                            If subsuperregkey IsNot Nothing Then
                                                                For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                    If checkvariables.isnullorwhitespace(wantedstring) = False Then
                                                                        If wantedstring.Contains(child) Then
                                                                            Try
                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                            Catch ex As Exception
                                                                            End Try
                                                                        End If
                                                                    End If
                                                                Next
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
            f.UpdateTextMethod(updateTextMethodmessage("34"))
        Catch ex As Exception
            MsgBox(msgboxmessage("5"))
            f.log(ex.Message + ex.StackTrace)
        End Try

        f.UpdateTextMethod(updateTextMethodmessage("35"))
        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then

                    regkey = My.Computer.Registry.Users.OpenSubKey _
              (users & "\Software\Microsoft\Installer\Products", True)

                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then

                                subregkey = My.Computer.Registry.Users.OpenSubKey _
    (users & "\Software\Microsoft\Installer\Products" & child, False)

                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(subregkey.GetValue("ProductName")) = False Then
                                        wantedvalue = subregkey.GetValue("ProductName").ToString
                                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                            For i As Integer = 0 To packages.Length - 1
                                                If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                    If wantedvalue.ToLower.Contains(packages(i).ToLower) And _
                                                       (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then
                                                        Try
                                                            regkey.DeleteSubKeyTree(child)
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            My.Computer.Registry.Users.OpenSubKey(users & "\Software\Microsoft\Installer\Features", True).DeleteSubKeyTree(child)
                                                        Catch ex As Exception
                                                        End Try

                                                        superregkey = My.Computer.Registry.Users.OpenSubKey _
                             (users & "\Software\Microsoft\Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If checkvariables.isnullorwhitespace(child2) = False Then

                                                                    subsuperregkey = My.Computer.Registry.Users.OpenSubKey _
                                                                (users & "\Software\Microsoft\Installer\UpgradeCodes" & child2, False)

                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                            If checkvariables.isnullorwhitespace(wantedstring) = False Then
                                                                                If wantedstring.Contains(child) Then
                                                                                    Try
                                                                                        superregkey.DeleteSubKeyTree(child2)
                                                                                    Catch ex As Exception
                                                                                    End Try
                                                                                End If
                                                                            End If
                                                                        Next
                                                                    End If
                                                                End If
                                                            Next
                                                        End If
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    End If
                End If
            Next
            f.UpdateTextMethod(updateTextMethodmessage("36"))
        Catch ex As Exception
            MsgBox(msgboxmessage("5"))
            f.log(ex.Message + ex.StackTrace)
        End Try

    End Sub
    Public Sub cleanserviceprocess(ByVal services As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles
        Dim updateTextMethodmessage As String() = f.UpdateTextMethodmessage

        f.UpdateTextMethod(updateTextMethodmessage("37"))
        f.log("Cleaning Process/Services...")

        'STOP AMD service
        For i As Integer = 0 To services.Length - 1
            If Not (donotremoveamdhdaudiobusfiles AndAlso services(i).ToLower.Contains("amdkmafd")) Then
                If Not checkvariables.isnullorwhitespace(services(i)) Then
                    Dim stopservice As New ProcessStartInfo
                    stopservice.FileName = "cmd.exe"
                    stopservice.Arguments = " /Csc stop " & Chr(34) & services(i) & Chr(34)
                    stopservice.UseShellExecute = False
                    stopservice.CreateNoWindow = True
                    stopservice.RedirectStandardOutput = False

                    Dim processstopservice As New Process
                    processstopservice.StartInfo = stopservice
                    processstopservice.Start()
                    processstopservice.WaitForExit()

                    System.Threading.Thread.Sleep(10)

                    stopservice.Arguments = " /Csc delete " & Chr(34) & services(i) & Chr(34)

                    processstopservice.StartInfo = stopservice
                    processstopservice.Start()
                    processstopservice.WaitForExit()

                    stopservice.Arguments = " /Csc interrogate " & Chr(34) & services(i) & Chr(34)
                    processstopservice.StartInfo = stopservice
                    processstopservice.Start()
                    processstopservice.WaitForExit()
                End If
            End If
        Next
        System.Threading.Thread.Sleep(10)
        f.UpdateTextMethod(updateTextMethodmessage("38"))
        f.log("Process/Services CleanUP Complete")
    End Sub
    Public Sub Pnplockdownfiles(ByVal driverfiles As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim regkey As RegistryKey
        Dim winxp = f.winxp
        Dim win8higher = f.win8higher
        Dim processinfo As New ProcessStartInfo
        Dim process As New Process
        Dim ddudrfolder = f.ddudrfolder
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles

        Try
            If Not winxp Then
                If win8higher Then
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                                If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                    f.log(ex.Message & " @Pnplockdownfiles")
                                                End Try
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        Next
                    End If

                Else   'Older windows  (windows vista and 7 run here)

                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                                If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then
                                    For Each child As String In regkey.GetValueNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                                Try
                                                    regkey.DeleteValue(child)
                                                Catch ex As Exception
                                                    f.log(ex.Message & " @Pnplockdownfiles")
                                                End Try
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        Next
                    End If
                End If

            End If

        Catch ex As Exception
            f.log(ex.StackTrace)
        End Try

    End Sub

    Public Sub clsidleftover(ByVal clsidleftover As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String
        Dim subregkey2 As RegistryKey
        Dim appid As String = Nothing
        Dim typelib As String = Nothing

        f.log("Begin clsidleftover CleanUP")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InProcServer32", False)
                        Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        If subregkey2 IsNot Nothing Then

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                    appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                    typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            subregkey2.DeleteSubKeyTree(child)
                                                        End If
                                                    Catch ex As Exception
                                                        f.log(ex.Message + ex.StackTrace)
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If
        Catch ex As Exception
            f.log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        If subregkey2 IsNot Nothing Then

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                    appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                    typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            subregkey2.DeleteSubKeyTree(child)
                                                        End If
                                                    Catch ex As Exception
                                                        f.log(ex.Message + ex.StackTrace)
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If
        Catch ex As Exception
            f.log(ex.StackTrace)
        End Try


        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\InProcServer32", False)
                            Try
                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                            For i As Integer = 0 To clsidleftover.Length - 1
                                                If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                    If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            If subregkey2 IsNot Nothing Then

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                        appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True).DeleteSubKeyTree(appid)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                        typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                subregkey2.DeleteSubKeyTree(child)
                                                            End If
                                                        Catch ex As Exception
                                                            f.log(ex.Message + ex.StackTrace)
                                                        End Try
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                            End Try
                        End If
                    Next
                End If
            Catch ex As Exception
                f.log(ex.StackTrace)
            End Try

            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
                            Try
                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                            For i As Integer = 0 To clsidleftover.Length - 1
                                                If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                    If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            If subregkey2 IsNot Nothing Then

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                        appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True).DeleteSubKeyTree(appid)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                        typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                subregkey2.DeleteSubKeyTree(child)
                                                            End If
                                                        Catch ex As Exception
                                                            f.log(ex.Message + ex.StackTrace)
                                                        End Try
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                            End Try
                        End If
                    Next
                End If
            Catch ex As Exception
                f.log(ex.StackTrace)
            End Try
        End If

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\LocalServer32", False)
                        Try
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        If subregkey2 IsNot Nothing Then

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                    appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            Try
                                                                If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                    typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                    Try
                                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                            Catch ex As Exception
                                                            End Try

                                                            subregkey2.DeleteSubKeyTree(child)
                                                        End If
                                                    Catch ex As Exception
                                                        f.log(ex.Message + ex.StackTrace)
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If
        Catch ex As Exception
            f.log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\LocalServer32", False)
                            Try
                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                            For i As Integer = 0 To clsidleftover.Length - 1
                                                If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                    If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            If subregkey2 IsNot Nothing Then

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child).GetValue("AppID")) Then
                                                                        appid = subregkey2.OpenSubKey(child).GetValue("AppID").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True).DeleteSubKeyTree(appid)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                Try
                                                                    If Not checkvariables.isnullorwhitespace(subregkey2.OpenSubKey(child & "\TypeLib").GetValue("")) Then
                                                                        typelib = subregkey2.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                                        Try
                                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                                        Catch ex As Exception
                                                                        End Try
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try

                                                                subregkey2.DeleteSubKeyTree(child)
                                                            End If
                                                        Catch ex As Exception
                                                            f.log(ex.Message + ex.StackTrace)
                                                        End Try
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                            End Try
                        End If
                    Next
                End If
            Catch ex As Exception
                f.log(ex.StackTrace)
            End Try
        End If

        f.log("End clsidleftover CleanUP")
    End Sub

    Public Sub interfaces(ByVal interfaces As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String
        Dim appid As String = Nothing
        Dim typelib As String = Nothing

        f.log("Start Interface CleanUP")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To interfaces.Length - 1
                                        If Not checkvariables.isnullorwhitespace(interfaces(i)) Then
                                            If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                                If subregkey.OpenSubKey("Typelib", False) IsNot Nothing Then
                                                    If checkvariables.isnullorwhitespace(subregkey.OpenSubKey("TypeLib", False).GetValue("")) = False Then
                                                        typelib = subregkey.OpenSubKey("TypeLib", False).GetValue("")
                                                        If checkvariables.isnullorwhitespace(typelib) = False Then
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    End If
                                                End If
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            f.log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then

            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\Interface", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then

                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\Interface\" & child, False)

                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To interfaces.Length - 1
                                            If Not checkvariables.isnullorwhitespace(interfaces(i)) Then
                                                If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                                    If subregkey.OpenSubKey("Typelib", False) IsNot Nothing Then
                                                        If checkvariables.isnullorwhitespace(subregkey.OpenSubKey("TypeLib", False).GetValue("")) = False Then
                                                            typelib = subregkey.OpenSubKey("TypeLib", False).GetValue("")
                                                            If checkvariables.isnullorwhitespace(typelib) = False Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        End If
                                                    End If
                                                    Try
                                                        regkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                f.log(ex.StackTrace)
            End Try

        End If

        f.log("END Interface CleanUP")
    End Sub
    Public Sub folderscleanup(ByVal driverfiles As String())
        Dim f As Form1 = My.Application.OpenForms("Form1")
        Dim filePath As String
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles

        For i As Integer = 0 To driverfiles.Length - 1
            If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then

                    filePath = System.Environment.SystemDirectory

                    Try
                        My.Computer.FileSystem.DeleteFile(filePath & "\" & driverfiles(i))
                    Catch ex As Exception
                    End Try

                    Try
                        My.Computer.FileSystem.DeleteFile(filePath + "\Drivers\" + driverfiles(i))
                    Catch ex As Exception
                    End Try

                End If
            End If
        Next

        Try
            For i As Integer = 0 To driverfiles.Length - 1
                If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                    filePath = Environment.GetEnvironmentVariable("windir")
                    For Each child As String In My.Computer.FileSystem.GetFiles(filePath & "\Prefetch")
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                Try
                                    My.Computer.FileSystem.DeleteFile(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
            f.log("info: " + ex.Message)
        End Try

        If IntPtr.Size = 8 Then
            For i As Integer = 0 To driverfiles.Length - 1
                If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                    If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then
                        filePath = Environment.GetEnvironmentVariable("windir")
                        For Each child As String In My.Computer.FileSystem.GetFiles(filePath & "\SysWOW64", FileIO.SearchOption.SearchTopLevelOnly, "*.log")
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                    Try
                                        My.Computer.FileSystem.DeleteFile(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next

                        Try
                            My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\Drivers\" + driverfiles(i))
                        Catch ex As Exception
                        End Try

                        Try
                            My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\" + driverfiles(i))
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        End If
    End Sub
End Class
Public Class WindowsApi

    <DllImport("kernel32.dll", EntryPoint:="WTSGetActiveConsoleSessionId", SetLastError:=True)> _
    Public Shared Function WTSGetActiveConsoleSessionId() As UInteger
    End Function

    <DllImport("Wtsapi32.dll", EntryPoint:="WTSQueryUserToken", SetLastError:=True)> _
    Public Shared Function WTSQueryUserToken(ByVal SessionId As UInteger, ByRef phToken As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("kernel32.dll", EntryPoint:="CloseHandle", SetLastError:=True)> _
    Public Shared Function CloseHandle(<InAttribute()> ByVal hObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="CreateProcessAsUserW", SetLastError:=True)> _
    Public Shared Function CreateProcessAsUser(<InAttribute()> ByVal hToken As IntPtr, _
                                                    <InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpApplicationName As String, _
                                                    ByVal lpCommandLine As IntPtr, _
                                                    <InAttribute()> ByVal lpProcessAttributes As IntPtr, _
                                                    <InAttribute()> ByVal lpThreadAttributes As IntPtr, _
                                                    <MarshalAs(UnmanagedType.Bool)> ByVal bInheritHandles As Boolean, _
                                                    ByVal dwCreationFlags As UInteger, _
                                                    <InAttribute()> ByVal lpEnvironment As IntPtr, _
                                                    <InAttribute(), MarshalAsAttribute(UnmanagedType.LPWStr)> ByVal lpCurrentDirectory As String, _
                                                    <InAttribute()> ByRef lpStartupInfo As STARTUPINFOW, _
                                                    <OutAttribute()> ByRef lpProcessInformation As PROCESS_INFORMATION) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure SECURITY_ATTRIBUTES
        Public nLength As UInteger
        Public lpSecurityDescriptor As IntPtr
        <MarshalAs(UnmanagedType.Bool)> _
        Public bInheritHandle As Boolean
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure STARTUPINFOW
        Public cb As UInteger
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpReserved As String
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpDesktop As String
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpTitle As String
        Public dwX As UInteger
        Public dwY As UInteger
        Public dwXSize As UInteger
        Public dwYSize As UInteger
        Public dwXCountChars As UInteger
        Public dwYCountChars As UInteger
        Public dwFillAttribute As UInteger
        Public dwFlags As UInteger
        Public wShowWindow As UShort
        Public cbReserved2 As UShort
        Public lpReserved2 As IntPtr
        Public hStdInput As IntPtr
        Public hStdOutput As IntPtr
        Public hStdError As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure PROCESS_INFORMATION
        Public hProcess As IntPtr
        Public hThread As IntPtr
        Public dwProcessId As UInteger
        Public dwThreadId As UInteger
    End Structure

End Class
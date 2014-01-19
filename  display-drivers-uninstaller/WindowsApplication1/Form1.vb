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

Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading
Imports System.Security.Principal




Public Class Form1

    Dim identity = WindowsIdentity.GetCurrent()
    Dim principal = New WindowsPrincipal(identity)
    Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)
    Dim removedisplaydriver As New ProcessStartInfo
    Dim removehdmidriver As New ProcessStartInfo
    Dim checkoem As New ProcessStartInfo
    Dim vendid As String = ""
    Dim vendidexpected As String = ""
    Dim provider As String = ""
    Dim proc As New Process
    Dim proc2 As New Process
    Dim prochdmi As New Process
    Dim toolTip1 As New ToolTip()
    Dim reboot As Boolean = True
    Dim shutdown As Boolean = False
    Dim win8higher As Boolean = False
    Dim winxp As Boolean = False
    Dim stopme As Boolean = False
    Dim removephysx As Boolean = True
    Dim remove3dtvplay As Boolean = True
    Dim userpth As String = System.Environment.GetEnvironmentVariable("userprofile")
    Dim time As String = DateAndTime.Now
    Dim locations As String = Application.StartupPath & "\DDU Logs\" & DateAndTime.Now.Year & " _" & DateAndTime.Now.Month & "_" & DateAndTime.Now.Day _
                              & "_" & DateAndTime.Now.Hour & "_" & DateAndTime.Now.Minute & "_" & DateAndTime.Now.Second & "_DDULog.log"
    Dim UserAc As String = System.Environment.GetEnvironmentVariable("username")
    Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive")
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
    Dim wantedvalue As String = Nothing
    Dim superkey As RegistryKey = Nothing
    Dim regkey As RegistryKey = Nothing
    Dim subregkey2 As RegistryKey = Nothing
    Dim subsuperregkey As RegistryKey = Nothing
    Dim basekey As RegistryKey = Nothing
    Dim superregkey As RegistryKey = Nothing
    Dim currentdriverversion As String = Nothing
    Dim classroot() As String = Nothing
    Dim safemode As Boolean = False
    Dim myExe As String

    Private Function checkupdates() As Integer
        Try
            Dim request2 As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create("http://www.wagnardmobile.com/DDU/currentversion2.txt")
            Dim response2 As System.Net.HttpWebResponse = Nothing

            Try
                response2 = request2.GetResponse()
            Catch ex As Exception
                request2 = System.Net.HttpWebRequest.Create("http://archive.sunet.se/pub/games/PC/guru3d/ddu/currentversion2.txt")
            End Try

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
    Private Sub Checkupdates2()
        Dim result As Integer = checkupdates()

        If result = 2 Then
            updates = 2
        ElseIf result = 1 Then
            updates = 1
        ElseIf result = 3 Then
            updates = 3
        End If

        AccessUI()
    End Sub
    Private Sub regfullfordelete(ByVal key As String)
        removehdmidriver.FileName = application.startuppath & "\" & label3.Text & "\setacl.exe"
        removehdmidriver.Arguments = _
"-on " & Chr(34) & key & Chr(34) & " -ot reg -rec yes -actn setowner -ownr n:s-1-5-32-544"
        removehdmidriver.UseShellExecute = False
        removehdmidriver.CreateNoWindow = True
        removehdmidriver.RedirectStandardOutput = False
        prochdmi.StartInfo = removehdmidriver
        prochdmi.Start()
        prochdmi.WaitForExit()

        removehdmidriver.Arguments = _
"-on " & Chr(34) & key & Chr(34) & " -ot reg -rec yes -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
        prochdmi.StartInfo = removehdmidriver
        prochdmi.Start()
        prochdmi.WaitForExit()
    End Sub
    Private Sub AccessUI()
        If Me.InvokeRequired Then
            Me.Invoke(New MethodInvoker(AddressOf AccessUI))
        Else
            If updates = 1 Then
                Label11.Text = ("No Updates found. Program is up to date.")

            ElseIf updates = 2 Then

                Label11.Text = ("Updates found! Expect limited support on older versions than the most recent.")

                Dim result = MsgBox("Updates are available! Visit forum thread now?", MsgBoxStyle.YesNoCancel)

                If result = MsgBoxResult.Yes And ComboBox1.SelectedIndex = 0 Then
                    Process.Start("http://forums.guru3d.com/showthread.php?t=379506")
                ElseIf result = MsgBoxResult.Yes And ComboBox1.SelectedIndex = 1 Then
                    Process.Start("http://forums.guru3d.com/showthread.php?t=379505")
                ElseIf result = MsgBoxResult.No Then
                    MsgBox("Note: Most bugs you find have probably already been fixed in the most recent version, if not please report them." & _
                           "Do not report bugs from older versions unless they have not been fixed yet.")
                ElseIf result = MsgBoxResult.Cancel Then
                    MsgBox("Note: Most bugs you find have probably already been fixed in the most recent version, if not please report them." & _
                           "Do not report bugs from older versions unless they have not been fixed yet.")

                End If
            ElseIf updates = 3 Then
                Label11.Text = "Unable to Fetch updates!"
            End If
        End If
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        CheckForIllegalCrossThreadCalls = True
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        ComboBox1.Enabled = False
        CheckBox2.Enabled = False
        CheckBox1.Enabled = False
        CheckBox3.Enabled = False
        CheckBox4.Enabled = False
        If ComboBox1.Text = "AMD" Then
            vendidexpected = "VEN_1002"
            provider = "Provider: Advanced Micro Devices"
        End If

        If ComboBox1.Text = "NVIDIA" Then
            vendidexpected = "VEN_10DE"
            provider = "Provider: NVIDIA"
        End If

        If ComboBox1.Text = "INTEL" Then
            vendidexpected = "VEN_8086"
            provider = "Provider: Intel"
        End If

        TextBox1.Text = TextBox1.Text + "*****  Uninstalling " & ComboBox1.Text & " driver... *****" + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        log("Uninstalling " + ComboBox1.Text + " driver ...")
        TextBox1.Text = TextBox1.Text + "***** Executing ddudr Remove , Please wait(can take a few minutes *****) " + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        log("Executing ddudr Remove")

        BackgroundWorker1.RunWorkerAsync(ComboBox1.Text)

    End Sub

    Private Sub cleandriverstore()

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Executing Driver Store cleanUP(finding OEM step)... *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Executing Driver Store cleanUP(Find OEM)...")
        'Check the driver from the driver store  ( oemxx.inf)
        checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
        checkoem.Arguments = "dp_enum"
        checkoem.UseShellExecute = False
        checkoem.CreateNoWindow = True
        checkoem.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        proc2.StartInfo = checkoem
        proc2.Start()
        reply = proc2.StandardOutput.ReadToEnd
        proc2.WaitForExit()
        ' System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)
        log("ddudr DP_ENUM RESULT BELOW")
        log(reply)
        'Preparing to read output.
        Dim oem As Integer = Nothing
        Try
            oem = reply.IndexOf("oem")
        Catch ex As Exception
        End Try


        While oem > -1 And oem <> Nothing
            Dim position As Integer = reply.IndexOf("Provider:", oem)
            Dim classs As Integer = reply.IndexOf("Class:", oem)
            Dim inf As Integer = reply.IndexOf(".inf", oem)
            If classs > -1 Then 'I saw that sometimes, there could be no class on some oems (winxp)
                If reply.Substring(position, classs - position).Contains(provider) Then
                    Dim part As String = reply.Substring(oem, inf - oem)
                    log(part + " Found")
                    Dim deloem As New Diagnostics.ProcessStartInfo
                    deloem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                    deloem.Arguments = "dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                    Try
                        For Each child As String In IO.File.ReadAllLines(Environment.GetEnvironmentVariable("windir") & "\inf\" & part & ".inf")
                            If child.ToLower.Trim.Replace(" ", "").Contains("class=display") Or _
                                child.ToLower.Trim.Replace(" ", "").Contains("class=media") Or _
                                child.ToLower.Trim.Replace(" ", "").Contains("class=usb") Then
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
                    Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Executing Driver Store cleanUP(Delete OEM)... *****" + vbNewLine)
                    Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
                    Invoke(Sub() TextBox1.ScrollToCaret())
                    log("Executing Driver Store CleanUP(delete OEM)...")
                    proc3.StartInfo = deloem
                    proc3.Start()
                    Dim Reply2 As String = proc3.StandardOutput.ReadToEnd
                    proc3.WaitForExit()


                    Invoke(Sub() TextBox1.Text = TextBox1.Text + Reply2 + vbNewLine)
                    Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
                    Invoke(Sub() TextBox1.ScrollToCaret())
                    log(Reply2)

                End If
            End If
            oem = reply.IndexOf("oem", oem + 1)
        End While


        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Driver Store cleanUP complete. *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Driver Store CleanUP Complete.")


        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning process/services... *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Cleaning Process/Services...")
        'Delete left over files.
    End Sub
    Private Sub cleanamd()

        'STOP AMD service
        Dim services() As String
        services = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\services.cfg") '// add each line as String Array.
        For i As Integer = 0 To services.Length - 1
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

            System.Threading.Thread.Sleep(50)

            stopservice.Arguments = " /Csc delete " & Chr(34) & services(i) & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(50)

            stopservice.Arguments = " /Csc interrogate " & Chr(34) & services(i) & Chr(34)
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
        Next


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

        Dim appproc = Process.GetProcessesByName("MOM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("CLIStart")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("CLI")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("CCC")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("HydraDM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("HydraDM64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("HydraGrd")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("Grid64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("HydraMD64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("HydraMD")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i


        If Not safemode Then
            log("Killing Explorer.exe")
            appproc = Process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Length - 1
                appproc(i).Kill()
            Next i
        End If


        appproc = Process.GetProcessesByName("ThumbnailExtractionHost")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("jusched")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        System.Threading.Thread.Sleep(50)
        'Delete AMD data Folders
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning Directory *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Cleaning Directory")
        Dim filePath As String

        If CheckBox1.Checked = True Then
            filePath = "C:\AMD"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                log(ex.Message)
            End Try

        End If

        filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\ATI"

        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
            log(ex.Message)
        End Try

        filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\ATI"

        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
            log(ex.Message)
        End Try

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.ProgramFiles) + "\ATI"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception

            log(ex.Message)
        End Try

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.ProgramFiles) + "\AMD\SteadyVideo"
        Try
            TestDelete(filePath)
        Catch ex As Exception

            log(ex.Message)
        End Try


        filePath = Environment.GetFolderPath _
 (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception

            log(ex.Message)
        End Try

        'Not sure if this work on XP

        filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\ATI"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception

            log(ex.Message)
        End Try

        filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\AMD"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception

            log(ex.Message)
        End Try

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.CommonProgramFiles) + "\ATI Technologies\Multimedia"
        Try
            My.Computer.FileSystem.DeleteDirectory _
                (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
        End Try

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.CommonProgramFiles) + "\ATI Technologies"
        If Not Directory.Exists(filePath) Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
            End Try
        End If

        'Delete driver files
        'delete OpenCL
        Dim driverfiles() As String
        Dim tempStr As String = "" '// temp String for result.
        driverfiles = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\driverfiles.cfg") '// add each line as String Array.

        For i As Integer = 0 To driverfiles.Length - 1

            filePath = System.Environment.SystemDirectory
            Try
                My.Computer.FileSystem.DeleteFile(filePath + "\" + driverfiles(i))
            Catch ex As Exception
            End Try

            Try
                My.Computer.FileSystem.DeleteFile(filePath + "\Drivers\" + driverfiles(i))
            Catch ex As Exception
            End Try

            filePath = Environment.GetEnvironmentVariable("windir")
            For Each child As String In My.Computer.FileSystem.GetFiles(filePath & "\Prefetch")
                If String.IsNullOrEmpty(Trim(child)) = False Then
                    If child.ToLower.Contains(driverfiles(i)) Then
                        Try
                            My.Computer.FileSystem.DeleteFile(child)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next

            If IntPtr.Size = 8 Then

                filePath = Environment.GetEnvironmentVariable("windir")
                Try
                    My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\" + driverfiles(i))
                Catch ex As Exception
                End Try

                Try
                    My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\Drivers\" + driverfiles(i))
                Catch ex As Exception
                End Try

            End If
        Next

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

        If IntPtr.Size = 8 Then

            filePath = Environment.GetFolderPath _
                       (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message)
            End Try

            filePath = Environment.GetFolderPath _
               (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message)
            End Try

            filePath = System.Environment.SystemDirectory
            Dim files() As String = IO.Directory.GetFiles(filePath + "\", "coinst_*.*")
            For i As Integer = 0 To files.Length - 1
                Try
                    My.Computer.FileSystem.DeleteFile(files(i))
                Catch ex As Exception
                End Try
            Next

            filePath = Environment.GetFolderPath _
               (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message)
            End Try

            filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
            Try
                TestDelete(filePath)
            Catch ex As Exception

                log(ex.Message + "SteadyVideo testdelete")
            End Try

            filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\Program Files (x86)" + "\Common Files" + "\ATI Technologies\Multimedia"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
            End Try

            filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\Program Files (x86)" + "\Common Files" + "\ATI Technologies"
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                    End Try
                End If
            Catch ex As Exception
            End Try

            filePath = System.Environment.GetEnvironmentVariable("systemdrive") + "\ProgramData\Microsoft\Windows\Start Menu\Programs\Catalyst Control Center"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
            End Try
        End If

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning known Regkeys... May take a minute or two. *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Cleaning known Regkeys")
        'Delete AMD regkey


        'Deleting DCOM object
        Try
            log("Starting dcom/clsid/appid/typelib cleanup")

            classroot = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\classroot.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        For i As Integer = 0 To classroot.Length - 1
                            If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                Try
                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                Catch ex As Exception
                                    Continue For
                                End Try
                                If subregkey IsNot Nothing Then
                                    If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If String.IsNullOrEmpty(wantedvalue) = False Then


                                            If IntPtr.Size = 8 Then
                                                Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue)
                                                    Dim appid As String
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                        appid = Nothing
                                                    End Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib")

                                                    Dim typelib As String
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                        typelib = Nothing
                                                    End Try

                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            'special case for an unusual key configuration nv bug?
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If String.IsNullOrEmpty(typelib) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                            subregkey2.DeleteSubKeyTree(typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                    subregkey2.DeleteSubKeyTree(wantedvalue)

                                                Catch ex As Exception
                                                End Try
                                            End If
                                            Try
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue)

                                                Try
                                                    appid = subregkey2.GetValue("AppID").ToString
                                                Catch ex As Exception
                                                End Try
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib")

                                                Try
                                                    typelib = subregkey2.GetValue("").ToString
                                                Catch ex As Exception
                                                End Try

                                                If String.IsNullOrEmpty(appid) = False Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                        subregkey2.DeleteSubKeyTree(appid)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                If String.IsNullOrEmpty(typelib) = False Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                        subregkey2.DeleteSubKeyTree(typelib)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                subregkey2.DeleteSubKeyTree(wantedvalue)
                                            Catch ex As Exception
                                            End Try
                                            Try
                                                My.Computer.Registry.ClassesRoot.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        '-----------------
        'interface cleanup
        '-----------------
        log("Interface CleanUP")
        Try
            Dim interfaces() As String
            interfaces = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\interface.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    For i As Integer = 0 To interfaces.Length - 1
                                        If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                            If IntPtr.Size = 8 Then
                                                Try
                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\Interface\" & child)
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

        log("ActiveMovie Filter Class Manager cleanUP")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("ActiveMovie Filter Class Manager") Then
                                        Try
                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance", False)
                                        Catch ex As Exception
                                            Continue For
                                        End Try
                                        If subregkey2 IsNot Nothing Then
                                            For Each child2 As String In subregkey2.GetSubKeyNames()
                                                If String.IsNullOrEmpty(child2) = False Then
                                                    Try
                                                        superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance\" & child2)
                                                    Catch ex As Exception
                                                        Continue For
                                                    End Try
                                                    If superkey IsNot Nothing Then
                                                        If String.IsNullOrEmpty(superkey.GetValue("FriendlyName")) = False Then
                                                            wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                            If wantedvalue2.Contains("ATI MPEG") Or _
                                                                wantedvalue2.Contains("AMD MJPEG") Or _
                                                                wantedvalue2.Contains("ATI Ticker") Or _
                                                                wantedvalue2.Contains("MMACE SoftEmu") Or _
                                                                wantedvalue2.Contains("MMACE DeInterlace") Or _
                                                                wantedvalue2.Contains("ATI Video") Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
                                        If wantedvalue.Contains("ActiveMovie Filter Class Manager") Then
                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance", False)
                                            If subregkey2 IsNot Nothing Then
                                                For Each child2 As String In subregkey2.GetSubKeyNames()
                                                    If String.IsNullOrEmpty(child2) = False Then
                                                        Try
                                                            superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance\" & child2)
                                                        Catch ex As Exception
                                                            Continue For
                                                        End Try
                                                        If superkey IsNot Nothing Then
                                                            If String.IsNullOrEmpty(superkey.GetValue("FriendlyName")) = False Then
                                                                wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                                If wantedvalue2.Contains("ATI MPEG") Or _
                                                                   wantedvalue2.Contains("AMD MJPEG") Or _
                                                                   wantedvalue2.Contains("ATI Ticker") Or _
                                                                   wantedvalue2.Contains("MMACE SoftEmu") Or _
                                                                   wantedvalue2.Contains("MMACE DeInterlace") Or _
                                                                   wantedvalue2.Contains("ATI Video") Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("amdwdst") Then
                            If String.IsNullOrEmpty(Trim(child)) = False Then
                                Try
                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID\" + child)
                                Catch ex As Exception
                                    Continue For
                                End Try
                                If subregkey IsNot Nothing Then
                                    If String.IsNullOrEmpty(subregkey.GetValue("AppID")) = False Then
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

        Dim clsidleftover() As String
        Try
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                            regkey.DeleteSubKeyTree(child)
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

        Try
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InprocServer32", False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                            regkey.DeleteSubKeyTree(child)
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
            Try
                clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\InprocServer32", False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                regkey.DeleteSubKeyTree(child)
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


        log("Record CleanUP")

        '--------------
        'Record cleanup
        '--------------
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Record", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = regkey.OpenSubKey(child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            For Each childs As String In subregkey.GetSubKeyNames()
                                If String.IsNullOrEmpty(childs) = False Then
                                    Try
                                        If String.IsNullOrEmpty(subregkey.OpenSubKey(childs, False).GetValue("Assembly")) = False Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            regkey.DeleteSubKeyTree("Khronos")
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                regkey.DeleteSubKeyTree("Khronos")
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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

        log("Pnplockdownfiles region cleanUP")
        Try
            If winxp = False Then
                If win8higher Then
                    'setting permission to registry
                    removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & "s-1-5-32-544" & Chr(34) & ";p:full"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                        removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child & Chr(34) & " -ot reg -actn setowner -ownr n:" & Chr(34) & "s-1-5-32-544" & Chr(34)
                                        prochdmi.StartInfo = removehdmidriver
                                        prochdmi.Start()
                                        prochdmi.WaitForExit()
                                        System.Threading.Thread.Sleep(10)  '10 millisecond stall (0.01 Seconds)
                                        removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & "s-1-5-32-544" & Chr(34) & ";p:full"
                                        prochdmi.StartInfo = removehdmidriver
                                        prochdmi.Start()
                                        prochdmi.WaitForExit()
                                        System.Threading.Thread.Sleep(10)  '10 millisecond stall (0.01 Seconds)
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                            log(ex.Message & " @Pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If


                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                    Catch ex As Exception
                    End Try

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
                    Catch ex As Exception
                    End Try

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
                    Catch ex As Exception
                    End Try

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
                    Catch ex As Exception
                    End Try

                    If IntPtr.Size = 8 Then
                        Try
                            regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                        Catch ex As Exception
                        End Try
                    End If

                    If IntPtr.Size = 8 Then
                        Try
                            regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
                            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
                        Catch ex As Exception
                        End Try
                    End If

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
                    Catch ex As Exception
                    End Try

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
                    Catch ex As Exception
                    End Try

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
                    Catch ex As Exception
                    End Try

                    '--------------------------------
                    'Setting permission to normal
                    '--------------------------------

                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn restore -bckp .\" & Label3.Text & "\pnpldf.bkp"
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                    '--------------------------------
                    'End setting permission to normal
                    '--------------------------------

                Else   'Older windows  (windows vista and 7 run here)
                    'setting permission to registry
                    removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message & " @Pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If


                    '-----------------------------------------------
                    'setting back the registry permission to normal.
                    '-----------------------------------------------

                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn restore -bckp .\" & Label3.Text & "\pnpldf.bkp"

                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
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
                        If String.IsNullOrEmpty(childs) = False Then
                            If childs.ToLower.Contains("controlset") Then
                                Try
                                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                     ("SYSTEM\" & childs & "\Enum\Root")
                                Catch ex As Exception
                                    Continue For
                                End Try

                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If String.IsNullOrEmpty(Trim(child)) = False Then
                                            If child.ToLower.Contains("legacy_amdkmdag") Then

                                                '-------------------------------------
                                                'Setting permission to the key region
                                                '-------------------------------------


                                                removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root\" & child & Chr(34) & " -ot reg -rec no -actn setowner -ownr n:" & Chr(34) & UserAc & Chr(34)
                                                removehdmidriver.UseShellExecute = False
                                                removehdmidriver.CreateNoWindow = True
                                                removehdmidriver.RedirectStandardOutput = False
                                                prochdmi.StartInfo = removehdmidriver
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root\" & child & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root" & Chr(34) & " -ot reg -rec no -actn setowner -ownr n:" & Chr(34) & UserAc & Chr(34)
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root" & Chr(34) & " -ot reg -rec no -actn setowner -ownr n:s-1-5-32-544"
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)


                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                                                Try
                                                    My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & childs & "\Enum\Root", True).DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                    log(ex.Message & " Legacy_AMDKMDAG   (error)")
                                                End Try


                                                'seting permission back to normal

                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SYSTEM\" & childs & "\Enum\Root" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full;m:revoke"
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
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
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.Contains("AMDAPPSDKROOT") Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    If child.Contains("Path") Then
                                        If String.IsNullOrEmpty(regkey.GetValue(child)) = False Then
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
                    If String.IsNullOrEmpty(child2) = False Then
                        If child2.ToLower.Contains("controlset") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.StartsWith("ATI") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try



        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("ace") Or _
                           child.ToLower.Contains("install") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                Next
                If regkey.SubKeyCount.ToString = 0 Then
                    My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("ATI")
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("cbt") Or _
                           child.ToLower.Contains("install") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                Next
                If regkey.SubKeyCount.ToString = 0 Then
                    My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("ATI Technologies")
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies\Install", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.Contains("ATI Catalyst") Or child.Contains("ATI MCAT") Or _
                            child.Contains("AVT") Or child.Contains("ccc") Or _
                            child.Contains("Packages") Or child.Contains("WirelessDisplay") Or _
                            child.Contains("SteadyVideo") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\AMD", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("eeu") Or
                           child.ToLower.Contains("mftvdecoder") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                Next
                If regkey.SubKeyCount.ToString = 0 Then
                    My.Computer.Registry.LocalMachine.OpenSubKey("Software", True).DeleteSubKeyTree("AMD")
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            If child.Contains("ATI") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    Next
                End If

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\AMD", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            If child.ToLower.Contains("eeu") Or
                               child.ToLower.Contains("mftvdecoder") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    Next
                    If regkey.SubKeyCount.ToString = 0 Then
                        My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True).DeleteSubKeyTree("AMD")
                    End If
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        Try
            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Run", True)
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
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("Removing known Packages")

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("DisplayName")) = False Then
                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                If String.IsNullOrEmpty(Trim(wantedvalue)) = False Then
                                    If wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                        wantedvalue.Contains("ccc-utility") Or _
                                        wantedvalue.Contains("AMD Accelerated Video") Or _
                                        wantedvalue.Contains("AMD Wireless Display") Or _
                                            wantedvalue.Contains("AMD Media Foundation") Or _
                                            wantedvalue.Contains("HydraVision") Or _
                                            wantedvalue.Contains("AMD Drag and Drop") Or _
                                            wantedvalue.Contains("AMD APP SDK") Or _
                                            wantedvalue.Contains("AMD Steady") Or _
                                            wantedvalue.Contains("AMD Fuel") Or _
                                            wantedvalue.Contains("Application Profiles") Or _
                                            wantedvalue.Contains("ATI AVIVO") Then

                                        Try
                                            If String.IsNullOrEmpty(Trim(subregkey.GetValue("InstallLocation").ToString)) = False And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files (x86)") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files (x86)\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~1") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~1\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~2") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~2\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.Length < 4 Then
                                                My.Computer.FileSystem.DeleteDirectory _
                                                                      (subregkey.GetValue("InstallLocation").ToString, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                            End If
                                        Catch ex As Exception

                                        End Try
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

        Try
            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
      ("Software\Microsoft\Installer\Features", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                            ("Software\Microsoft\Installer\Features\" & child, True)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            For Each child2 As String In subregkey.GetValueNames()
                                If String.IsNullOrEmpty(child2) = False Then
                                    If child2.Contains("SteadyVideo") Then
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                        End Try
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

        Try
            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
      ("Software\Microsoft\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                               ("Software\Microsoft\Installer\Products\" & child, True)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("AMD Steady Video") Or _
                                    wantedvalue.Contains("ATI AVIVO") Then
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
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("DisplayName")) = False Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If String.IsNullOrEmpty(Trim(wantedvalue)) = False Then
                                        If wantedvalue.Contains("CCC Help") Or wantedvalue.Contains("AMD Accelerated") Or _
                                        wantedvalue.Contains("Catalyst Control Center") Or _
                                        wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                        wantedvalue.Contains("ccc-utility") Or _
                                            wantedvalue.Contains("AMD Wireless Display") Or _
                                            wantedvalue.Contains("AMD Media Foundation") Or _
                                            wantedvalue.Contains("HydraVision") Or _
                                            wantedvalue.Contains("AMD Drag and Drop") Or _
                                            wantedvalue.Contains("AMD APP SDK") Or _
                                            wantedvalue.Contains("AMD Steady") Or _
                                            wantedvalue.Contains("AMD Fuel") Or _
                                            wantedvalue.Contains("Application Profiles") Or _
                                            wantedvalue.Contains("ATI AVIVO") Then
                                            Try
                                                If String.IsNullOrEmpty(Trim(subregkey.GetValue("InstallLocation").ToString)) = False And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files (x86)") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("files (x86)\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~1") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~1\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~2") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.EndsWith("progra~2\") And _
                                                Not subregkey.GetValue("InstallLocation").ToString.ToLower.Length < 4 Then
                                                    My.Computer.FileSystem.DeleteDirectory _
                                                                          (subregkey.GetValue("InstallLocation").ToString, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                                End If
                                            Catch ex As Exception
                                            End Try
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


        log("Starting S-1-5-xx region cleanUP")

        Try
            basekey = My.Computer.Registry.LocalMachine.OpenSubKey _
        ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If String.IsNullOrEmpty(super) = False Then
                        If super.Contains("S-1-5") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                    ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrEmpty(Trim(child)) = False Then
                                        Try
                                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                                            "\InstallProperties", True)
                                        Catch ex As Exception
                                            Continue For
                                        End Try
                                        If subregkey IsNot Nothing Then
                                            If String.IsNullOrEmpty(subregkey.GetValue("DisplayName")) = False Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString

                                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                                    If wantedvalue.Contains("CCC Help") Or wantedvalue.Contains("AMD Accelerated") Or _
                                                       wantedvalue.Contains("Catalyst Control Center") Or _
                                                       wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                                       wantedvalue.Contains("ccc-utility") Or _
                                                       wantedvalue.Contains("AMD Wireless Display") Or _
                                                       wantedvalue.Contains("AMD Media Foundation") Or _
                                                       wantedvalue.Contains("HydraVision") Or _
                                                       wantedvalue.Contains("AMD Drag and Drop") Or _
                                                       wantedvalue.Contains("AMD APP SDK") Or _
                                                       wantedvalue.Contains("AMD Steady") Or _
                                                       wantedvalue.Contains("AMD Fuel") Or _
                                                       wantedvalue.Contains("Application Profiles") Or _
                                                       wantedvalue.Contains("ATI AVIVO") Then
                                                        Try
                                                            regkey.DeleteSubKeyTree(child)
                                                        Catch ex As Exception
                                                        End Try

                                                        'okay .. important part here to fixed the famous AMD yellow mark on their installer.
                                                        'The yellow mark in this case is really stupid imo and shouldn't even
                                                        'be thrown as a warning to the end user... it has not bad effect.


                                                        superregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                         ("Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If String.IsNullOrEmpty(child2) = False Then
                                                                    Try

                                                                        subsuperregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                        ("Installer\UpgradeCodes\" & child2, False)
                                                                    Catch ex As Exception
                                                                        Continue For
                                                                    End Try

                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring In subsuperregkey.GetValueNames()
                                                                            If String.IsNullOrEmpty(wantedstring) = False Then
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

                                                        superregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                         ("Software\Microsoft\Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child3 As String In superregkey.GetSubKeyNames()
                                                                If String.IsNullOrEmpty(child3) = False Then
                                                                    Try
                                                                        subsuperregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                        ("Software\Microsoft\Installer\UpgradeCodes\" & child3, False)
                                                                    Catch ex As Exception
                                                                        Continue For
                                                                    End Try
                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring In subsuperregkey.GetValueNames()
                                                                            If String.IsNullOrEmpty(wantedstring) = False Then
                                                                                If wantedstring.Contains(child) Then
                                                                                    Try
                                                                                        superregkey.DeleteSubKeyTree(child3)
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

        log("End S-1-5-xx region cleanUP")

        Try
            basekey = My.Computer.Registry.LocalMachine.OpenSubKey _
     ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If String.IsNullOrEmpty(super) = False Then
                        If super.Contains("S-1-5") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                    ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrEmpty(Trim(child)) = False Then
                                        Try
                                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child, False)
                                        Catch ex As Exception
                                            Continue For
                                        End Try
                                        If subregkey IsNot Nothing Then
                                            For Each wantedstring In subregkey.GetValueNames()
                                                If String.IsNullOrEmpty(wantedstring) = False Then
                                                    If String.IsNullOrEmpty(subregkey.GetValue(wantedstring)) = False Then
                                                        wantedvalue = subregkey.GetValue(wantedstring).ToString
                                                        If String.IsNullOrEmpty(wantedvalue) = False Then
                                                            If wantedvalue.Contains("ATI\CIM\") Or _
                                                                wantedvalue.Contains("ATI Technologies\Multimedia\") Or _
                                                                wantedvalue.Contains("AMD APP\") Or _
                                                                wantedvalue.Contains("ATI Technologies\cccutil") Or _
                                                                wantedvalue.Contains("ATI.ACE\") Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Installer\Features\" & wantedstring)
                                                                Catch ex As Exception
                                                                End Try
                                                                Try
                                                                    regkey.DeleteSubKeyTree(child)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        End If
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
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("SharedDLLs CleanUP")
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.Contains("ATI\CIM\") Or _
                        child.Contains("SteadyVideo") Or _
                        child.Contains("ATI.ACE") Or _
                        child.Contains("ATI Technologies\Multimedia") Or _
                        child.Contains("OpenCL") Or _
                        child.Contains("OpenVideo") Or _
                        child.Contains("OVDecode") Or _
                        child.Contains("amdocl") Or _
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            If child.Contains("ATI\CIM\") Or _
                        child.Contains("SteadyVideo") Or _
                        child.Contains("ATI.ACE") Or _
                        child.Contains("ATI Technologies\Multimedia") Or _
                        child.Contains("OpenCL") Or _
                        child.Contains("OpenVideo") Or _
                        child.Contains("OVDecode") Or _
                        child.Contains("amdocl") Or _
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
        End If


        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                            ("Installer\Products\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("CCC Help") Or wantedvalue.Contains("AMD Accelerated") Or _
                                                wantedvalue.Contains("Catalyst Control Center") Or _
                                                wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                                wantedvalue.Contains("ccc-utility") Or _
                                                wantedvalue.Contains("AMD Wireless Display") Or _
                                                wantedvalue.Contains("AMD Media Foundation") Or _
                                                wantedvalue.Contains("HydraVision") Or _
                                                wantedvalue.Contains("AMD Drag and Drop") Or _
                                                wantedvalue.Contains("AMD APP SDK") Or _
                                                wantedvalue.Contains("AMD Steady") Or _
                                                wantedvalue.Contains("ATI AVIVO") Or _
                                                wantedvalue.Contains("AMD Fuel") Then
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

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
    ("SOFTWARE\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("SOFTWARE\Classes\Installer\Products\" & child, True)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("CCC Help") Or _
                                                wantedvalue.Contains("AMD Accelerated") Or _
                                                wantedvalue.Contains("Catalyst Control Center") Or _
                                                wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                                wantedvalue.Contains("ccc-utility") Or _
                                                wantedvalue.Contains("AMD Wireless Display") Or _
                                                wantedvalue.Contains("AMD Media Foundation") Or _
                                                wantedvalue.Contains("HydraVision") Or _
                                                wantedvalue.Contains("AMD Drag and Drop") Or _
                                                wantedvalue.Contains("AMD APP SDK") Or _
                                                wantedvalue.Contains("AMD Steady") Or _
                                                wantedvalue.Contains("ATI AVIVO") Or _
                                                wantedvalue.Contains("AMD Fuel") Then
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

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                            ("CLSID\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                ("Wow6432Node\CLSID\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                ("Wow6432Node\Interface\" & child, False)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
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

    Private Sub Cleannvidia()

        'STOP / delete / interrogate NVIDIA service
        Dim services() As String
        services = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\services.cfg") '// add each line as String Array.
        For i As Integer = 0 To services.Length - 1
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

            System.Threading.Thread.Sleep(50)

            stopservice.Arguments = " /Csc delete " & Chr(34) & services(i) & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(50)

            stopservice.Arguments = " /Csc interrogate " & Chr(34) & services(i) & Chr(34)
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
        Next

        'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
        'holding files in the NVIDIA folders sometimes.

        Dim appproc = Process.GetProcessesByName("Lcore")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("NvTmru")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i


        appproc = Process.GetProcessesByName("nvxdsync")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("nvtray")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("dwm")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("WWAHost")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("nvspcaps64")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("nvspcaps")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("NvBackend")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        'Delete NVIDIA data Folders
        'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning UpdatusUser users ac if present *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Cleaning UpdatusUser users ac if present")

        Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName.ToString())
        Dim users As DirectoryEntries = AD.Children
        Dim newuser As DirectoryEntry = Nothing
        Try
            newuser = users.Find("UpdatusUser")
            users.Remove(newuser)
        Catch ex As Exception
        End Try

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning Directory *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Cleaning Directory")
        Dim filePath As String

        If CheckBox1.Checked = True Then
            filePath = "C:\NVIDIA"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message)
            End Try

        End If

        ' here I erase the folders / files of the nvidia GFE / update in users.
        filePath = Environment.GetEnvironmentVariable("UserProfile")
        Dim parentPath As String = IO.Path.GetDirectoryName(filePath)
        filePath = parentPath
        For Each child As String In Directory.GetDirectories(filePath)
            If String.IsNullOrEmpty(Trim(child)) = False Then
                If child.ToLower.Contains("updatususer") Then
                    Try
                        TestDelete(child)
                    Catch ex As Exception
                        log(ex.Message + ex.StackTrace + " UpdatusUser")
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



        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.LocalApplicationData) + "\NVIDIA"

        If removephysx Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message & "local application data \Ndidia")
            End Try
        Else
            Try
                TestDelete(filePath)
            Catch ex As Exception
            End Try

        End If

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.ApplicationData) + "\NVIDIA"

        If removephysx Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message & "application data \Nvidia")
            End Try
        Else
            Try
                TestDelete(filePath)
            Catch ex As Exception
            End Try

        End If

        Try
            filePath = Environment.GetFolderPath _
      (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

            For Each child As String In Directory.GetDirectories(filePath)
                If String.IsNullOrEmpty(Trim(child)) = False Then
                    If child.ToLower.Contains("control panel client") Or _
                       child.ToLower.Contains("display") Or _
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
                                If String.IsNullOrEmpty(Trim(child2)) = False Then
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
                                            End Try
                                        Else
                                            If child2.ToLower.Contains("physx") Then
                                                'do nothing
                                            Else
                                                Try
                                                    My.Computer.FileSystem.DeleteDirectory _
                                                    (child2, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                                Catch ex As Exception
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
                            End Try
                        Else
                            If child.ToLower.Contains("physx") Or child.ToLower.Contains("installer2") Then
                                'do nothing
                            Else
                                Try
                                    My.Computer.FileSystem.DeleteDirectory _
                                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                Catch ex As Exception
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
                    End Try
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.CommonProgramFiles) + "\NVIDIA Corporation"

        If removephysx Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message & "common programfiles\Nvidia corporation")
            End Try
        Else
            Try
                TestDelete(filePath)
            Catch ex As Exception
            End Try

        End If

        Try
            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
                For Each child As String In Directory.GetDirectories(filePath)
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("3d vision") Or _
                           child.ToLower.Contains("led visualizer") Or _
                           child.ToLower.Contains("netservice") Or _
                           child.ToLower.Contains("nvidia geforce experience") Or _
                           child.ToLower.Contains("nvstreamc") Or _
                           child.ToLower.Contains("nvstreamsrv") Or _
                           child.ToLower.Contains("update common") Or _
                           child.ToLower.Contains("physx") Or _
                           child.ToLower.Contains("update core") Then
                            If removephysx Then
                                Try
                                    My.Computer.FileSystem.DeleteDirectory _
                                    (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                Catch ex As Exception
                                End Try
                            Else
                                If child.ToLower.Contains("physx") Then
                                    'do nothing
                                Else
                                    Try
                                        My.Computer.FileSystem.DeleteDirectory _
                                        (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                                    Catch ex As Exception
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
                        End Try
                    End If
                Catch ex As Exception
                End Try
            End If
        Catch ex As Exception
        End Try
        'Not sure if this work on XP

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"

        If removephysx Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message & "common application data\nvidia")
            End Try
        Else
            Try
                TestDelete(filePath)
            Catch ex As Exception
            End Try

        End If

        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"

        If removephysx Then
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception

                log(ex.Message & "common application data\Nvidia corporation")
            End Try
        Else
            Try
                TestDelete(filePath)
            Catch ex As Exception
            End Try

        End If
        'Erase driver file from windows directory

        Dim driverfiles() As String
        driverfiles = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\driverfiles.cfg") '// add each line as String Array.

        For i As Integer = 0 To driverfiles.Length - 1

            filePath = System.Environment.SystemDirectory
            Try
                My.Computer.FileSystem.DeleteFile(filePath + "\" + driverfiles(i))
            Catch ex As Exception
            End Try

            Try
                My.Computer.FileSystem.DeleteFile(filePath + "\Drivers\" + driverfiles(i))
            Catch ex As Exception
            End Try

            filePath = Environment.GetEnvironmentVariable("windir")
            For Each child As String In My.Computer.FileSystem.GetFiles(filePath & "\Prefetch")
                If String.IsNullOrEmpty(Trim(child)) = False Then
                    If child.ToLower.Contains(driverfiles(i)) Then
                        Try
                            My.Computer.FileSystem.DeleteFile(child)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next


            If IntPtr.Size = 8 Then

                filePath = Environment.GetEnvironmentVariable("windir")
                Try
                    My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\" + driverfiles(i))
                Catch ex As Exception
                End Try

                Try
                    My.Computer.FileSystem.DeleteFile(filePath + "\SysWOW64\Drivers\" + driverfiles(i))
                Catch ex As Exception
                End Try

            End If
        Next

        filePath = System.Environment.SystemDirectory
        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "nvdisp*.*")
        For i As Integer = 0 To files.Length - 1
            Try
                My.Computer.FileSystem.DeleteFile(files(i))
            Catch ex As Exception
            End Try
        Next
        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            My.Computer.FileSystem.DeleteDirectory _
                    (filePath + "\Help\nvcpl", FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
        End Try
        'Delete NVIDIA regkey
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Starting reg cleanUP *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Starting reg cleanUP... May take a minute or two.")


        'Deleting DCOM object /classroot
        log("Starting dcom/clsid/appid/typelib cleanup")
        log("Step 1/2")

        Try
            classroot = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\classroot.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        For i As Integer = 0 To classroot.Length - 1
                            If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                Try
                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                Catch ex As Exception
                                    Continue For
                                End Try
                                If subregkey IsNot Nothing Then
                                    If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If String.IsNullOrEmpty(wantedvalue) = False Then
                                            If removephysx Then
                                                If IntPtr.Size = 8 Then
                                                    Try

                                                        Try
                                                            appid = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                            appid = Nothing
                                                        End Try

                                                        Try
                                                            typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                        Catch ex As Exception
                                                            typelib = Nothing
                                                        End Try

                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True).DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                            Try
                                                                'special case for an unusual key configuration nv bug?
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If

                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True).DeleteSubKeyTree(wantedvalue)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                Try

                                                    Try
                                                        appid = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                        appid = Nothing
                                                    End Try


                                                    Try
                                                        typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                    Catch ex As Exception
                                                        typelib = Nothing
                                                    End Try

                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        Try
                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If

                                                    If String.IsNullOrEmpty(typelib) = False Then
                                                        Try
                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If

                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True).DeleteSubKeyTree(wantedvalue)
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try

                                            Else
                                                If child.ToLower.Contains("gamesconfigserver") Then   'Physx related
                                                    'do nothing
                                                Else
                                                    If IntPtr.Size = 8 Then
                                                        Try

                                                            Try
                                                                appid = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                            Catch ex As Exception
                                                                appid = Nothing
                                                            End Try

                                                            Try
                                                                typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                            Catch ex As Exception
                                                                typelib = Nothing
                                                            End Try

                                                            If String.IsNullOrEmpty(appid) = False Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True).DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                                Try
                                                                    'special case for an unusual key configuration nv bug?
                                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If

                                                            If String.IsNullOrEmpty(typelib) = False Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True).DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            If String.IsNullOrEmpty(wantedvalue) = False Then
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True).DeleteSubKeyTree(wantedvalue)
                                                            End If
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    Try

                                                        Try
                                                            appid = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                            appid = Nothing
                                                        End Try


                                                        Try
                                                            typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                        Catch ex As Exception
                                                            typelib = Nothing
                                                        End Try

                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True).DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If

                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True).DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If String.IsNullOrEmpty(wantedvalue) = False Then
                                                            My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True).DeleteSubKeyTree(wantedvalue)
                                                        End If
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        My.Computer.Registry.ClassesRoot.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("Step 2/2")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then

                        If child.ToLower.Contains("comupdatus") Or _
                           child.ToLower.Contains("nv3d") Or _
                           child.ToLower.Contains("nvui") Or _
                           child.ToLower.Contains("nvvsvc") Or _
                           child.ToLower.Contains("nvxd") Or _
                           child.ToLower.Contains("gamesconfigserver") Or _
                           child.ToLower.Contains("nvidia.installer") Or _
                           child.ToLower.Contains("displayserver") Then
                            Try
                                subregkey = regkey.OpenSubKey(child)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("AppID")) = False Then
                                    wantedvalue = subregkey.GetValue("AppID").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
                                        If removephysx Then
                                            If IntPtr.Size = 8 Then
                                                Try
                                                    appid = wantedvalue
                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\AppID\" & appid)
                                                    End If
                                                Catch ex As Exception
                                                End Try
                                            End If
                                            Try
                                                appid = wantedvalue
                                                If String.IsNullOrEmpty(appid) = False Then
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

                                        Else
                                            If child.ToLower.Contains("gamesconfigserver") Then
                                                'do nothing
                                            Else
                                                If IntPtr.Size = 8 Then
                                                    Try
                                                        appid = wantedvalue
                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\AppID\" & appid)
                                                        End If
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                Try

                                                    appid = wantedvalue
                                                    If String.IsNullOrEmpty(appid) = False Then
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
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        log("Extra step")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\LocalServer32", False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False And wantedvalue.ToLower.Contains("\nvidia corporation\") Then

                                        If removephysx Then
                                            If IntPtr.Size = 8 Then
                                                Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                    Dim appid As String = Nothing
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                    End Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")

                                                    Dim typelib As String = Nothing
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                    End Try

                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            'special case for an unusual key configuration nv bug?
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If String.IsNullOrEmpty(typelib) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                            subregkey2.DeleteSubKeyTree(typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                    subregkey2.DeleteSubKeyTree(child)

                                                Catch ex As Exception
                                                End Try
                                            End If
                                            Try
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                Dim appid As String = Nothing
                                                Try
                                                    appid = subregkey2.GetValue("AppID").ToString
                                                Catch ex As Exception
                                                End Try
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                Dim typelib As String = Nothing
                                                Try
                                                    typelib = subregkey2.GetValue("").ToString
                                                Catch ex As Exception
                                                End Try

                                                If String.IsNullOrEmpty(appid) = False Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                        subregkey2.DeleteSubKeyTree(appid)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                If String.IsNullOrEmpty(typelib) = False Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                        subregkey2.DeleteSubKeyTree(typelib)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                subregkey2.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try

                                        Else
                                            If child.Contains("gamesconfigserver") Then   'Physx related
                                                'do nothing
                                            Else
                                                If IntPtr.Size = 8 Then
                                                    Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")
                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                                subregkey.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                            Try
                                                                'special case for an unusual key configuration nv bug?
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                subregkey.DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                        subregkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                Try
                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                    Dim appid As String = Nothing
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                    End Try
                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                    Dim typelib As String = Nothing
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                    End Try

                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If String.IsNullOrEmpty(typelib) = False Then
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                            subregkey.DeleteSubKeyTree(typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                    subregkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\LocalServer32", False)
                            Catch ex As Exception
                                Continue For
                            End Try

                            Try
                                If subregkey IsNot Nothing Then
                                    If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If String.IsNullOrEmpty(wantedvalue) = False And wantedvalue.ToLower.Contains("\nvidia corporation\") Then

                                            If removephysx Then
                                                If IntPtr.Size = 8 Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")

                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try

                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                                subregkey2.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                            Try
                                                                'special case for an unusual key configuration nv bug?
                                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey2.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                subregkey2.DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                        subregkey2.DeleteSubKeyTree(child)

                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                    Dim appid As String = Nothing
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                    End Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                    Dim typelib As String = Nothing
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                    End Try

                                                    If String.IsNullOrEmpty(appid) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If String.IsNullOrEmpty(typelib) = False Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                            subregkey2.DeleteSubKeyTree(typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                    subregkey2.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try

                                            Else
                                                If child.Contains("gamesconfigserver") Then   'Physx related
                                                    'do nothing
                                                Else
                                                    If IntPtr.Size = 8 Then
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                            Dim appid As String = Nothing
                                                            Try
                                                                appid = subregkey2.GetValue("AppID").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")
                                                            Dim typelib As String = Nothing
                                                            Try
                                                                typelib = subregkey2.GetValue("").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            If String.IsNullOrEmpty(appid) = False Then
                                                                Try
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                                    subregkey.DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                                Try
                                                                    'special case for an unusual key configuration nv bug?
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                    subregkey.DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            If String.IsNullOrEmpty(typelib) = False Then
                                                                Try
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                    subregkey.DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            subregkey.DeleteSubKeyTree(child)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try

                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                                subregkey.DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        subregkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                            End Try
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.StackTrace)
            End Try
        End If

        log("exta step #2")

        Dim clsidleftover() As String
        Try
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InProcServer32", False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        Try
                            If subregkey IsNot Nothing Then
                                If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If String.IsNullOrEmpty(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                If removephysx Then
                                                    If IntPtr.Size = 8 Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                            Dim appid As String = Nothing
                                                            Try
                                                                appid = subregkey2.GetValue("AppID").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")

                                                            Dim typelib As String = Nothing
                                                            Try
                                                                typelib = subregkey2.GetValue("").ToString
                                                            Catch ex As Exception
                                                            End Try

                                                            If String.IsNullOrEmpty(appid) = False Then
                                                                Try
                                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                                    subregkey2.DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                                Try
                                                                    'special case for an unusual key configuration nv bug?
                                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                    subregkey2.DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            If String.IsNullOrEmpty(typelib) = False Then
                                                                Try
                                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                    subregkey2.DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            subregkey2.DeleteSubKeyTree(child)

                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try

                                                        If String.IsNullOrEmpty(appid) = False Then
                                                            Try
                                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey2.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If String.IsNullOrEmpty(typelib) = False Then
                                                            Try
                                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                                subregkey2.DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        subregkey2.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try

                                                Else
                                                    If child.ToLower.Contains("gamesconfigserver") Then   'Physx related
                                                        'do nothing
                                                    Else
                                                        If IntPtr.Size = 8 Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child)
                                                                Dim appid As String = Nothing
                                                                Try
                                                                    appid = subregkey2.GetValue("AppID").ToString
                                                                Catch ex As Exception
                                                                End Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\TypeLib")
                                                                Dim typelib As String = Nothing
                                                                Try
                                                                    typelib = subregkey2.GetValue("").ToString
                                                                Catch ex As Exception
                                                                End Try
                                                                If String.IsNullOrEmpty(appid) = False Then
                                                                    Try
                                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                                                                        subregkey.DeleteSubKeyTree(appid)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                    Try
                                                                        'special case for an unusual key configuration nv bug?
                                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                        subregkey.DeleteSubKeyTree(appid)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                                If String.IsNullOrEmpty(typelib) = False Then
                                                                    Try
                                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                        subregkey.DeleteSubKeyTree(typelib)
                                                                    Catch ex As Exception
                                                                    End Try
                                                                End If
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                                subregkey.DeleteSubKeyTree(child)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child)
                                                            Dim appid As String = Nothing
                                                            Try
                                                                appid = subregkey2.GetValue("AppID").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\TypeLib")
                                                            Dim typelib As String = Nothing
                                                            Try
                                                                typelib = subregkey2.GetValue("").ToString
                                                            Catch ex As Exception
                                                            End Try

                                                            If String.IsNullOrEmpty(appid) = False Then
                                                                Try
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                    subregkey.DeleteSubKeyTree(appid)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            If String.IsNullOrEmpty(typelib) = False Then
                                                                Try
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                                    subregkey.DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                            subregkey.DeleteSubKeyTree(child)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
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
            log(ex.StackTrace)
        End Try

        Try
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" + child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
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

        log("Interface CleanUP")
        'interface cleanup
        Dim interfaces() As String
        Try
            interfaces = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\interface.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    For i As Integer = 0 To interfaces.Length - 1
                                        If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                            If subregkey.OpenSubKey("Typelib", False) IsNot Nothing Then
                                                If String.IsNullOrEmpty(Trim(subregkey.OpenSubKey("TypeLib", False).GetValue(""))) = False Then
                                                    typelib = subregkey.OpenSubKey("TypeLib", False).GetValue("")
                                                    If String.IsNullOrEmpty(Trim(typelib)) = False Then
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
                                            If IntPtr.Size = 8 Then
                                                Try
                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\Interface\" & child)
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

        log("Finished dcom/clsid/appid/typelib cleanup")

        'end of deleting dcom stuff
        log("Pnplockdownfiles region cleanUP")
        Try
            If winxp = False Then
                If win8higher Then
                    'setting permission to registry
                    removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & "s-1-5-32-544" & Chr(34) & ";p:full"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            For Each child As String In regkey.GetSubKeyNames()
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                        removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child & Chr(34) & " -ot reg -actn setowner -ownr n:" & Chr(34) & "s-1-5-32-544" & Chr(34)
                                        prochdmi.StartInfo = removehdmidriver
                                        prochdmi.Start()
                                        prochdmi.WaitForExit()
                                        System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                                        removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & "s-1-5-32-544" & Chr(34) & ";p:full"
                                        prochdmi.StartInfo = removehdmidriver
                                        prochdmi.Start()
                                        prochdmi.WaitForExit()
                                        System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                                        Try
                                            regkey.DeleteSubKeyTree(child)
                                        Catch ex As Exception
                                            log(ex.Message & " @Pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If

                    'Cleaning PNPRessources.

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                    Catch ex As Exception
                        log(ex.Message & "pnp resources khronos")
                    End Try

                    Try
                        regfullfordelete("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
                    Catch ex As Exception
                        log(ex.Message & "pnp resources cpl extension")
                    End Try

                    If IntPtr.Size = 8 Then

                        Try
                            regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                            My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                        Catch ex As Exception
                            log(ex.Message & "pnpresources wow6432node khronos")
                        End Try
                    End If

                    Try
                        regfullfordelete("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
                    Catch ex As Exception
                        log(ex.Message & "pnp ressources nvidia corporation")
                    End Try

                    '-----------------------------------------------
                    'setting back the registry permission to normal.                       
                    '-----------------------------------------------

                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn restore -bckp .\" & Label3.Text & "\pnpldf.bkp"
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                    '--------------------------------
                    'End setting permission to normal
                    '--------------------------------

                Else   'Older windows  (windows vista and 7 run here)
                    'setting permission to registry
                    removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            For Each child As String In regkey.GetValueNames()
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message & " @Pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If

                    '-----------------------------------------------
                    'setting back the registry permission to normal.
                    '-----------------------------------------------

                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
"-on " & Chr(34) & "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles" & Chr(34) & " -ot reg -actn restore -bckp .\" & Label3.Text & "\pnpldf.bkp"
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
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
                                    If String.IsNullOrEmpty(Trim(child)) = False Then
                                        If String.IsNullOrEmpty(regkey.GetValue(child)) = False Then
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
                                    If String.IsNullOrEmpty(childs) = False Then
                                        For Each child As String In regkey.OpenSubKey(childs).GetValueNames()
                                            If String.IsNullOrEmpty(Trim(child)) = False And child.ToString.ToLower.Contains("description") Then
                                                If String.IsNullOrEmpty(regkey.OpenSubKey(childs).GetValue(child)) = False Then
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
                                                        If String.IsNullOrEmpty(childinsubregkey2) = False Then
                                                            For Each childinsubregkey2value As String In subregkey2.OpenSubKey(childinsubregkey2).GetValueNames()
                                                                If String.IsNullOrEmpty(childinsubregkey2value) = False And childinsubregkey2value.ToString.ToLower.Contains("description") Then
                                                                    If String.IsNullOrEmpty(subregkey2.OpenSubKey(childinsubregkey2).GetValue(childinsubregkey2value)) = False Then
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
                                If String.IsNullOrEmpty(Trim(child)) = False Then
                                    If child.Contains("Path") Then
                                        wantedvalue = regkey.GetValue(child).ToString()
                                        Try
                                            Select Case True
                                                Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;")
                                                    wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;", "")
                                                    Try
                                                        regkey.SetValue(child, wantedvalue)
                                                    Catch ex As Exception
                                                    End Try
                                            End Select
                                        Catch ex As Exception
                                        End Try
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

        '-------------------------------------
        'end system environement patch cleanup
        '-------------------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
            If regkey IsNot Nothing Then
                If String.IsNullOrEmpty(regkey.GetValue("AppInit_DLLs")) = False Then
                    wantedvalue = regkey.GetValue("AppInit_DLLs")   'Will need to consider the comma in the future for multiple value
                    If String.IsNullOrEmpty(wantedvalue) = False Then
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
                    If String.IsNullOrEmpty(regkey.GetValue("AppInit_DLLs")) = False Then
                        wantedvalue = regkey.GetValue("AppInit_DLLs")
                        If String.IsNullOrEmpty(wantedvalue) = False Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
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
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)

            regkey.DeleteSubKeyTree("Khronos")

        Catch ex As Exception

            log(ex.Message + " Opencl Khronos")
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)

                regkey.DeleteSubKeyTree("Khronos")

            Catch ex As Exception

                log(ex.Message + " Opencl Khronos")
            End Try
        End If
        log("SharedDlls CleanUP")

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
         ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
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
            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(child) = False Then
                        If child.ToLower.Contains("nvidia corporation") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If String.IsNullOrEmpty(child2) = False Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If child.ToLower.Contains("ageia technologies") Then
                            If removephysx Then
                                regkey.DeleteSubKeyTree(child)
                            End If
                        End If
                        If child.ToLower.Contains("nvidia corporation") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If String.IsNullOrEmpty(child2) = False Then
                                    If child2.ToLower.Contains("global") Or _
                                       child2.ToLower.Contains("installer") Or _
                                        child2.ToLower.Contains("installer2") Or _
                                        child2.ToLower.Contains("nvidia update core") Or _
                                        child2.ToLower.Contains("nvcontrolpanel") Or _
                                        child2.ToLower.Contains("nvcontrolpanel2") Or _
                                        child2.ToLower.Contains("nvstream") Or _
                                        child2.ToLower.Contains("nvstreamc") Or _
                                        child2.ToLower.Contains("nvstreamsrv") Or _
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            If child.ToLower.Contains("ageia technologies") Then
                                If removephysx Then
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                            If child.ToLower.Contains("nvidia corporation") Then
                                For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                    If String.IsNullOrEmpty(child2) = False Then
                                        If child2.ToLower.Contains("global") Or _
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            If String.IsNullOrEmpty(Trim(regkey.OpenSubKey(child).GetValue("DisplayName"))) = False Then
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
                If String.IsNullOrEmpty(Trim(child)) = False Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        If String.IsNullOrEmpty(Trim(regkey.OpenSubKey(child).GetValue("DisplayName"))) = False Then
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
                If String.IsNullOrEmpty(Trim(child)) = False Then
                    Try
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
                    Catch ex As Exception
                        Continue For
                    End Try
                    If subregkey IsNot Nothing Then
                        If String.IsNullOrEmpty(subregkey.GetValue("ProfileImagePath")) = False Then
                            wantedvalue = subregkey.GetValue("ProfileImagePath").ToString
                            If String.IsNullOrEmpty(wantedvalue) = False Then
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
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("")) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("NVIDIA") Then
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

        log("ngenservice Clean")

        '----------------------
        '.net ngenservice clean
        '----------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
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

        '-----------------------------
        'End of .net ngenservice clean
        '-----------------------------

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

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Debug : Starting S-1-5-xx region cleanUP *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        Try
            log("Debug : Starting S-1-5-xx region cleanUP")
            Dim basekey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
                  ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If String.IsNullOrEmpty(super) = False Then
                        If super.Contains("S-1-5") Then
                            Try
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                    ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If String.IsNullOrEmpty(Trim(child)) = False Then
                                        Try
                                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                                "\InstallProperties")
                                        Catch ex As Exception
                                            Continue For
                                        End Try
                                        If subregkey IsNot Nothing Then
                                            If String.IsNullOrEmpty(subregkey.GetValue("DisplayName")) = False Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                                    If wantedvalue.ToLower.Contains("nvidia") Then
                                                        If removephysx Then
                                                            regkey.DeleteSubKeyTree(child)
                                                        Else
                                                            If wantedvalue.ToLower.Contains("physx") Then
                                                                'do nothing
                                                            Else
                                                                regkey.DeleteSubKeyTree(child)
                                                            End If
                                                        End If
                                                        Dim superregkey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                                                                         ("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If String.IsNullOrEmpty(Trim(child2)) = False Then
                                                                    Try
                                                                        subsuperregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                                                    ("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\" & child2)
                                                                    Catch ex As Exception
                                                                        Continue For
                                                                    End Try
                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring As String In subsuperregkey.GetValueNames()
                                                                            If String.IsNullOrEmpty(wantedstring) = False Then
                                                                                If wantedstring.Contains(child) Then
                                                                                    If removephysx Then
                                                                                        Try
                                                                                            superregkey.DeleteSubKeyTree(child2)
                                                                                        Catch ex As Exception
                                                                                        End Try
                                                                                    Else
                                                                                        If wantedvalue.ToLower.Contains("physx") Then
                                                                                            'do nothing
                                                                                        Else
                                                                                            Try
                                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                                            Catch ex As Exception
                                                                                            End Try
                                                                                        End If
                                                                                    End If
                                                                                End If
                                                                            End If
                                                                        Next
                                                                    End If
                                                                End If
                                                            Next
                                                        End If

                                                        Try
                                                            superregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                            ("Software\Microsoft\Installer\UpgradeCodes", True)
                                                            If superregkey IsNot Nothing Then
                                                                For Each child2 As String In superregkey.GetSubKeyNames()
                                                                    If String.IsNullOrEmpty(child2) = False Then
                                                                        Try
                                                                            subsuperregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                        ("Software\Microsoft\Installer\UpgradeCodes\" & child2)
                                                                        Catch ex As Exception
                                                                            Continue For
                                                                        End Try
                                                                        If subsuperregkey IsNot Nothing Then
                                                                            For Each wantedstring In subsuperregkey.GetValueNames()
                                                                                If String.IsNullOrEmpty(wantedstring) = False Then
                                                                                    If wantedstring.Contains(child) Then

                                                                                        If removephysx Then
                                                                                            superregkey.DeleteSubKeyTree(child2)
                                                                                        Else
                                                                                            If wantedvalue.ToLower.Contains("physx") Then
                                                                                                'do nothing
                                                                                            Else
                                                                                                superregkey.DeleteSubKeyTree(child2)
                                                                                            End If
                                                                                        End If
                                                                                    End If
                                                                                End If
                                                                            Next
                                                                        End If
                                                                    End If
                                                                Next
                                                            End If
                                                        Catch ex As Exception
                                                            log(ex.Message)
                                                        End Try
                                                    End If
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
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Debug : End of S-1-5-xx region cleanUP *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            log("Debug : End of S-1-5-xx region cleanUP")
        Catch ex As Exception
            log(ex.StackTrace)
        End Try



        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
("Installer\Products\" & child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.ToLower.Contains("nvidia") Then

                                        If removephysx Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                        Else
                                            If wantedvalue.ToLower.Contains("physx") Then
                                                'do nothing
                                            Else
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
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

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("SOFTWARE\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        Try
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("SOFTWARE\Classes\Installer\Products\" & child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(subregkey.GetValue("ProductName")) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If String.IsNullOrEmpty(wantedvalue) = False Then
                                    If wantedvalue.Contains("NVIDIA") Then

                                        If removephysx Then
                                            Try
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try
                                        Else
                                            If wantedvalue.Contains("PhysX") Then
                                                'do nothing
                                            Else
                                                Try
                                                    regkey.DeleteSubKeyTree(child)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
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

        Try
            If removephysx Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If String.IsNullOrEmpty(Trim(child)) = False Then
                            Try
                                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\" & child)
                            Catch ex As Exception
                                Continue For
                            End Try
                            If subregkey IsNot Nothing Then
                                For Each wantedstring In subregkey.GetValueNames()
                                    If String.IsNullOrEmpty(wantedstring) = False Then
                                        If String.IsNullOrEmpty(subregkey.GetValue(wantedstring)) = False Then
                                            wantedvalue = subregkey.GetValue(wantedstring).ToString
                                            If String.IsNullOrEmpty(wantedvalue) = False Then
                                                If wantedvalue.ToLower.Contains("physx") Or _
                                                    wantedvalue.ToLower.Contains("ageia") Then
                                                    Try
                                                        regkey.DeleteSubKeyTree(child)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
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

        '-------------
        'control/video
        '-------------
        Try


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Video", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames
                    If String.IsNullOrEmpty(Trim(child)) = False Then
                        subregkey = regkey.OpenSubKey(child & "\Video", False)
                        If subregkey IsNot Nothing Then
                            If String.IsNullOrEmpty(Trim(subregkey.GetValue("Service")).ToString) = False Then
                                If subregkey.GetValue("Service").ToString.ToLower = "nvlddmkm" Then
                                    regfullfordelete("HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\" & child)
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SYSTEM\CurrentControlSet\Control\Video\" & child)
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

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** End of Registry Cleaning *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("End of Registry Cleaning")
        System.Threading.Thread.Sleep(50)
    End Sub
    Private Sub checkpcieroot()  'This is for Nvidia Optimus to prevent the yellow mark on the PCI-E controler. We must remove the UpperFilters.
        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("SYSTEM\CurrentControlSet\Enum\PCI")
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If String.IsNullOrEmpty(Trim(child)) = False Then
                    If child.ToLower.Contains("ven_8086") Then
                        Try
                            subregkey = regkey.OpenSubKey(child)
                        Catch ex As Exception
                            Continue For
                        End Try
                        For Each childs As String In subregkey.GetSubKeyNames()
                            If String.IsNullOrEmpty(childs) = False Then
                                If subregkey.OpenSubKey(childs).GetValue("UpperFilters") IsNot Nothing Then
                                    Dim array() As String = subregkey.OpenSubKey(childs).GetValue("UpperFilters")    'do a .tostring here?
                                    For i As Integer = 0 To array.Length - 1

                                        If array(i).ToLower.Contains("nvpciflt") Then
                                            '-------------------------------------
                                            'Setting permission to the key region
                                            '-------------------------------------

                                            removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                                            removehdmidriver.Arguments = _
"-on " & Chr(34) & subregkey.OpenSubKey(childs).ToString & Chr(34) & " -ot reg -rec no -actn setowner -ownr n:" & Chr(34) & UserAc & Chr(34)
                                            removehdmidriver.UseShellExecute = False
                                            removehdmidriver.CreateNoWindow = True
                                            removehdmidriver.RedirectStandardOutput = False
                                            prochdmi.StartInfo = removehdmidriver
                                            prochdmi.Start()
                                            prochdmi.WaitForExit()
                                            System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)

                                            If Not winxp Then
                                                removehdmidriver.Arguments = _
"-on " & Chr(34) & subregkey.OpenSubKey(childs).ToString & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full"
                                                prochdmi.Start()
                                                prochdmi.WaitForExit()
                                                System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                                            End If

                                            Try
                                                subregkey.OpenSubKey(childs, True).DeleteValue("UpperFilters")
                                            Catch ex As Exception
                                                log("Failed to fix Optimus. You will have to manually remove the device with yellow mark in device manager to fix the missing vieocard")
                                            End Try


                                            '---------------------------------
                                            'Setting permission back to normal 
                                            '---------------------------------
                                            removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\setacl.exe"
                                            removehdmidriver.Arguments = _
"-on " & Chr(34) & subregkey.OpenSubKey(childs).ToString & Chr(34) & " -ot reg -actn ace -ace n:" & Chr(34) & UserAc & Chr(34) & ";p:full;m:revoke"
                                            removehdmidriver.UseShellExecute = False
                                            removehdmidriver.CreateNoWindow = True
                                            removehdmidriver.RedirectStandardOutput = False
                                            prochdmi.StartInfo = removehdmidriver
                                            prochdmi.Start()
                                            prochdmi.WaitForExit()
                                            System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                                            removehdmidriver.Arguments = _
"-on " & Chr(34) & subregkey.OpenSubKey(childs).ToString & Chr(34) & " -ot reg -rec no -actn setowner -ownr n:" & Chr(34) & "s-1-5-32-544" & Chr(34)
                                            prochdmi.StartInfo = removehdmidriver
                                            prochdmi.Start()
                                            prochdmi.WaitForExit()
                                            System.Threading.Thread.Sleep(10)  '25 millisecond stall (0.025 Seconds)
                                        End If
                                    Next
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        End If
    End Sub
    Private Sub rescan()

        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
        scan.Arguments = "rescan"
        scan.UseShellExecute = False
        scan.CreateNoWindow = True
        scan.RedirectStandardOutput = False

        If reboot Then
            log("Restarting Computer ")
            System.Diagnostics.Process.Start("shutdown", "/r /t 0 /f")
            Exit Sub
        End If
        If shutdown Then
            System.Diagnostics.Process.Start("shutdown", "/s /t 0 /f")
            Exit Sub
        End If
        If reboot = False And shutdown = False Then
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Scanning for new device... *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            log("Scanning for new device...")
            Dim proc4 As New Process
            proc4.StartInfo = scan
            proc4.Start()
            proc4.WaitForExit()
            System.Threading.Thread.Sleep(2000)
            If Not safemode Then
                Dim appproc = Process.GetProcessesByName("explorer")
                For i As Integer = 0 To appproc.Length - 1
                    appproc(i).Kill()
                Next i
            End If

            reboot = True
        End If
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Clean uninstall completed! *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Clean uninstall completed!")


    End Sub
    Private Sub Form1_close(sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        If Button1.Enabled = False Then
            e.Cancel = True
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\DDU Logs") Then
            My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\DDU Logs")
        End If

        If My.Settings.logbox = "dontlog" Then
            CheckBox2.Checked = False
        Else
            CheckBox2.Checked = True
        End If

        '----------------
        'language section
        '----------------

        Dim diChild() As String = Directory.GetDirectories(Application.StartupPath & "\settings\Languages")
        Dim list(diChild.Length - 1) As String
        For i As Integer = 0 To diChild.Length - 1
            Dim split As String() = diChild(i).Split("\")
            Dim parentFolder As String = split(split.Length - 1)
            list(i) = parentFolder
        Next

        ComboBox2.Items.AddRange(list)
        If My.Settings.language = "" Then
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

            Else
                ComboBox2.SelectedIndex = ComboBox2.FindString("English")
            End If

        Else
            ComboBox2.SelectedIndex = ComboBox2.FindString(My.Settings.language)

        End If

        initlanguage(ComboBox2.Text)

        '------------
        'Check update
        '------------

        checkupdatethread = New Thread(AddressOf Me.Checkupdates2)
        'checkthread.Priority = ThreadPriority.Highest
        checkupdatethread.Start()


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


        If arch = True Then
            Try

                If winxp Then  'XP64
                    myExe = Application.StartupPath & "\x64\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp64)
                    myExe = Application.StartupPath & "\x64\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl64)
                Else

                    myExe = Application.StartupPath & "\x64\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr64)

                    myExe = Application.StartupPath & "\x64\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl64)
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
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try
        Else
            Try
                If winxp Then  'XP32
                    myExe = Application.StartupPath & "\x86\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp32)

                    myExe = Application.StartupPath & "\x86\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl32)


                Else 'all other 32 bits
                    myExe = Application.StartupPath & "\x86\ddudr.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr32)

                    myExe = Application.StartupPath & "\x86\setacl.exe"
                    System.IO.File.WriteAllBytes(myExe, My.Resources.setacl32)

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
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try
        End If

        If arch = True Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x64\ddudr.exe") Then
                MsgBox("Unable to find ddudr. Please refer to the log.", MsgBoxStyle.Critical)
                Button1.Enabled = False
                Button2.Enabled = False
                Button3.Enabled = False
                Exit Sub
            End If
        ElseIf arch = False Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x86\ddudr.exe") Then
                MsgBox("Unable to find ddudr. Please refer to the log.", MsgBoxStyle.Critical)
                Button1.Enabled = False
                Button2.Enabled = False
                Button3.Enabled = False
                Exit Sub
            End If
        End If

        TextBox1.Text = TextBox1.Text + "DDU Version: " + Label6.Text.Replace("V", "") + vbNewLine
        log("DDU Version: " + Label6.Text.Replace("V", ""))
        log("OS: " + Label2.Text)
        log("Architecture: " & Label3.Text)

        'Videocard type indentification
        checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
        checkoem.Arguments = "findall =display"
        checkoem.UseShellExecute = False
        checkoem.CreateNoWindow = True
        checkoem.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        '------------------------------------------------------------------
        'Detection of the current and leftover videocard for the textboxlog
        '------------------------------------------------------------------
        proc2.StartInfo = checkoem
        proc2.Start()
        reply = proc2.StandardOutput.ReadToEnd
        proc2.WaitForExit()
        log(reply)
        Dim TextLines() As String = reply.Split(Environment.NewLine.ToCharArray, System.StringSplitOptions.RemoveEmptyEntries)
        For i As Integer = 0 To TextLines.Length - 2  'reason of -2 instead of -1 , we dont want the last line of ddudr.
            TextBox1.Text = TextBox1.Text + "Detected GPU : " + TextLines(i).Substring(TextLines(i).IndexOf(":") + 1) + vbNewLine
        Next

        'Trying to autoselect the right GPU cleanup option. 
        If reply.Contains("VEN_10DE") Then
            ComboBox1.SelectedIndex = 0
        End If
        If reply.Contains("VEN_1002") Then
            ComboBox1.SelectedIndex = 1
        End If
        If reply.Contains("VEN_8086") Then
            ComboBox1.SelectedIndex = 2
        End If

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")

            For Each child As String In regkey.GetSubKeyNames
                If Not child.ToLower.Contains("properties") Then
                    Try
                        subregkey = regkey.OpenSubKey(child)
                    Catch ex As Exception
                        Continue For
                    End Try
                    If subregkey IsNot Nothing Then
                        If Not String.IsNullOrEmpty(subregkey.GetValue("DriverVersion").ToString) Then
                            currentdriverversion = subregkey.GetValue("DriverVersion").ToString
                            TextBox1.Text = TextBox1.Text + "Detected Driver(s) Version(s) : " + currentdriverversion + vbNewLine
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
        log("Driver Version : " + currentdriverversion)

        'setting the driversearching parameter for win 7+
        If version >= "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
                If regkey.GetValue("SearchOrderConfig").ToString <> 0 Then
                    regkey.SetValue("SearchOrderConfig", 0)
                    MsgBox("DDU has changed a setting that prevent driver to be downloaded automatically with Windows Update. You can set this" _
                           & " back to default if you want AFTER your new driver installation.")
                End If
            Catch ex As Exception
            End Try
        End If
        If version >= "6.0" And version < "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
                If regkey.GetValue("DontSearchWindowsUpdate").ToString <> 1 Then
                    regkey.SetValue("DontSearchWindowsUpdate", 1)
                    MsgBox("DDU has changed a setting that prevent driver to be downloaded automatically with Windows Update. You can set this" _
                           & " back to default if you want AFTER your new driver installation.")
                End If
            Catch ex As Exception
            End Try
        End If


        'This code checks to see which mode Windows has booted up in.
        Select Case System.Windows.Forms.SystemInformation.BootMode
            Case BootMode.FailSafe
                'The computer was booted using only the basic files and drivers.
                'This is the same as Safe Mode
                safemode = True
            Case BootMode.FailSafeWithNetwork
                'The computer was booted using the basic files, drivers, and services necessary to start networking.
                'This is the same as Safe Mode with Networking
                safemode = True
            Case BootMode.Normal
                safemode = False
                If winxp = False Then
                    If MsgBox("DDU has detected that you are NOT in SafeMode... For a better CleanUP without issues, would you like to reboot the computer into SafeMode now?", MsgBoxStyle.YesNo, "Reboot into SafeMode?") = MsgBoxResult.Yes Then
                        Dim setbcdedit As New ProcessStartInfo
                        setbcdedit.FileName = "cmd.exe"
                        setbcdedit.Arguments = " /Cbcdedit /set {current} safeboot Minimal"
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
                                regkey.SetValue("*loadDDU", "cmd /c start " & Chr(34) & Chr(34) & " /d " & Chr(34) & Application.StartupPath & Chr(34) & " " & Chr(34) & "display driver uninstaller.exe" & Chr(34))
                                regkey.SetValue("*UndoSM", "bcdedit /deletevalue {current} safeboot")
                            Catch ex As Exception
                            End Try

                        End If
                        System.Diagnostics.Process.Start("shutdown", "/r /t 0 /f")
                        Application.Exit()
                        Exit Sub
                    End If
                Else
                    MsgBox("DDU has detected that you are NOT in SafeMode... For a better CleanUP without issues, it is recommended that you reboot into safemode")
                End If

                'The computer was booted in Normal mode.
        End Select

        log("User Account Name : " & UserAc)
        If Not isElevated Then
            MsgBox("You are not using DDU with Administrator priviledge. The application will exit.")
            Application.Exit()
        End If
        MsgBox("Please make a BACKUP or a System Restore point before using DDU. We take no responsibilities if something goes wrong.")

    End Sub

    Public Sub TestDelete(ByVal folder As String)
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "Deleting some specials folders, it may take some times..." + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Deleting some specials folders, it could take some times...")
        'ensure that this folder can be accessed with current user ac.
        Dim FolderInfo As IO.DirectoryInfo = New IO.DirectoryInfo(folder)
        Dim FolderAcl As New DirectorySecurity
        FolderAcl.AddAccessRule(New FileSystemAccessRule(UserAc, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit Or InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow))
        'FolderAcl.SetAccessRuleProtection(True, False) 'uncomment to remove existing permissions
        FolderInfo.SetAccessControl(FolderAcl)
        System.Threading.Thread.Sleep(10)
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
            My.Settings.logbox = "log"
        Else
            My.Settings.logbox = "dontlog"
        End If
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        about.Show()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        reboot = False
        shutdown = False
        Button1.PerformClick()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        reboot = False
        shutdown = True
        Button1.PerformClick()
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        If CheckBox3.Checked = True Then
            removephysx = True
        Else
            removephysx = False
        End If
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        If ComboBox1.Text = "NVIDIA" Then
            CheckBox3.Visible = True
            CheckBox4.Visible = True
            PictureBox2.Image = My.Resources.Nvidia_GeForce_Logo
        End If
        If ComboBox1.Text = "AMD" Then
            CheckBox3.Visible = False
            CheckBox4.Visible = False
            PictureBox2.Image = My.Resources.RadeonLogo1
        End If
        If ComboBox1.Text = "INTEL" Then
            CheckBox3.Visible = False
            CheckBox4.Visible = False
            PictureBox2.Image = My.Resources.intel_logo
        End If
    End Sub

    Public Sub log(ByVal value As String)
        If Me.CheckBox2.Checked = True Then
            Dim wlog As New IO.StreamWriter(locations, True)
            wlog.WriteLine(DateTime.Now & " >> " & value)
            wlog.Flush()
            wlog.Dispose()
            System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds) just to be sure its really released.
        Else

        End If
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        Dim webAddress As String = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"
        Process.Start(webAddress)
    End Sub

    Private Sub VisitGuru3dNVIDIAThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dNVIDIAThreadToolStripMenuItem.Click
        Process.Start("http://forums.guru3d.com/showthread.php?t=379506")
    End Sub

    Private Sub VisitGuru3dAMDThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dAMDThreadToolStripMenuItem.Click
        Process.Start("http://forums.guru3d.com/showthread.php?t=379505")
    End Sub

    Private Sub VisitGeforceThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGeforceThreadToolStripMenuItem.Click
        Process.Start("https://forums.geforce.com/default/topic/550192/geforce-drivers/display-driver-uninstaller-ddu-v6-2/")
    End Sub

    Private Sub SVNToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SVNToolStripMenuItem.Click
        Process.Start("https://code.google.com/p/display-drivers-uninstaller/source/list")
    End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, _
                     ByVal e As System.ComponentModel.DoWorkEventArgs) _
                     Handles BackgroundWorker1.DoWork

        Try

            checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
            checkoem.Arguments = "findall =display"
            checkoem.UseShellExecute = False
            checkoem.CreateNoWindow = True
            checkoem.RedirectStandardOutput = True

            'creation dun process fantome pour le wait on exit.

            proc2.StartInfo = checkoem
            proc2.Start()
            reply = proc2.StandardOutput.ReadToEnd
            proc2.WaitForExit()

            Try
                card1 = reply.IndexOf("PCI\")
            Catch ex As Exception

            End Try
            While card1 > -1
                position2 = reply.IndexOf(":", card1)
                vendid = reply.Substring(card1, position2 - card1).Trim
                If vendid.Contains(vendidexpected) Then
                    removedisplaydriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                    removedisplaydriver.Arguments = "remove =display " & Chr(34) & "@" & vendid & Chr(34)
                    removedisplaydriver.UseShellExecute = False
                    removedisplaydriver.CreateNoWindow = True
                    removedisplaydriver.RedirectStandardOutput = True
                    proc.StartInfo = removedisplaydriver

                    proc.Start()
                    reply2 = proc.StandardOutput.ReadToEnd
                    proc.WaitForExit()
                    log(reply2)



                End If
                card1 = reply.IndexOf("PCI\", card1 + 1)


            End While
            log("ddudr Remove Display Complete")
            'Next
            'For i As Integer = 0 To 1 'loop 2 time to check if there is a remaining videocard.
            checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
            checkoem.Arguments = "findall =media"
            checkoem.UseShellExecute = False
            checkoem.CreateNoWindow = True
            checkoem.RedirectStandardOutput = True

            'creation dun process fantome pour le wait on exit.

            proc2.StartInfo = checkoem
            proc2.Start()
            reply = proc2.StandardOutput.ReadToEnd
            proc2.WaitForExit()

            'System.Threading.Thread.Sleep(200) '200 ms sleep between removal of media.
            Try
                card1 = reply.IndexOf("HDAUDIO\")
            Catch ex As Exception

            End Try

            While card1 > -1

                position2 = reply.IndexOf(":", card1)
                vendid = reply.Substring(card1, position2 - card1).Trim
                If vendid.Contains(vendidexpected) Then

                    removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                    removehdmidriver.Arguments = "remove =MEDIA " & Chr(34) & "@" & vendid & Chr(34)
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = True
                    prochdmi.StartInfo = removehdmidriver
                    Try
                        prochdmi.Start()
                        reply2 = prochdmi.StandardOutput.ReadToEnd
                        prochdmi.WaitForExit()
                        log(reply2)
                    Catch ex As Exception

                        log(ex.Message)
                        MsgBox("Cannot find ddudr in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                        Button1.Enabled = True
                        Button2.Enabled = True
                        Button3.Enabled = True
                    End Try


                End If
                card1 = reply.IndexOf("HDAUDIO\", card1 + 1)
                ' System.Threading.Thread.Sleep(50) '100 ms sleep between removal of media.
            End While


            If DirectCast(e.Argument, String) = "NVIDIA" Then
                'removing 3DVision USB driver
                checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                checkoem.Arguments = "findall =USB"
                checkoem.UseShellExecute = False
                checkoem.CreateNoWindow = True
                checkoem.RedirectStandardOutput = True

                'creation dun process fantome pour le wait on exit.

                proc2.StartInfo = checkoem
                proc2.Start()
                reply = proc2.StandardOutput.ReadToEnd
                proc2.WaitForExit()
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

                        removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                        removehdmidriver.Arguments = "remove =USB " & Chr(34) & "@" & vendid & Chr(34)
                        removehdmidriver.UseShellExecute = False
                        removehdmidriver.CreateNoWindow = True
                        removehdmidriver.RedirectStandardOutput = True
                        prochdmi.StartInfo = removehdmidriver
                        Try
                            prochdmi.Start()
                            reply2 = prochdmi.StandardOutput.ReadToEnd
                            prochdmi.WaitForExit()
                            log(reply2)
                        Catch ex As Exception

                            log(ex.Message)
                            MsgBox("Cannot find ddudr in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                            Button1.Enabled = True
                            Button2.Enabled = True
                            Button3.Enabled = True
                        End Try


                        ' System.Threading.Thread.Sleep(50)
                    End If
                    card1 = reply.IndexOf("USB\", card1 + 1)
                    ' System.Threading.Thread.Sleep(50) '100 ms sleep between removal of media.
                End While

                'Removing NVIDIA Virtual Audio Device (Wave Extensible) (WDM)

                removehdmidriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                removehdmidriver.Arguments = "findall =media " & Chr(34) & "@*ROOT\*" & Chr(34)
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = True

                proc2.StartInfo = removehdmidriver
                proc2.Start()
                reply = proc2.StandardOutput.ReadToEnd
                proc2.WaitForExit()
                Try
                    card1 = reply.IndexOf("ROOT\")
                Catch ex As Exception

                End Try
                While card1 > -1

                    position2 = reply.IndexOf(":", card1)
                    vendid = reply.Substring(card1, position2 - card1).Trim
                    If reply.Substring(position2, reply.Length - position2).Contains("NVIDIA Virtual") Then

                        'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
                        removedisplaydriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                        removedisplaydriver.Arguments = "remove =media " & Chr(34) & "@" & vendid & Chr(34)
                        removedisplaydriver.UseShellExecute = False
                        removedisplaydriver.CreateNoWindow = True
                        removedisplaydriver.RedirectStandardOutput = True
                        proc.StartInfo = removedisplaydriver
                        Try
                            proc.Start()
                            reply2 = proc.StandardOutput.ReadToEnd
                            proc.WaitForExit()
                            log(reply2)
                        Catch ex As Exception
                            log(ex.Message)
                            MsgBox("Cannot find ddudr in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                            Button1.Enabled = True
                            Button2.Enabled = True
                            Button3.Enabled = True
                            Exit Sub
                        End Try
                    End If
                    card1 = reply.IndexOf("ROOT\", card1 + 1)
                End While
            End If

            log("ddudr Remove Audio/HDMI Complete")
            'removing monitor and hidden monitor

            log("ddudr Remove Monitor started")

            checkoem.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
            checkoem.Arguments = "findall =monitor"
            checkoem.UseShellExecute = False
            checkoem.CreateNoWindow = True
            checkoem.RedirectStandardOutput = True

            'creation dun process fantome pour le wait on exit.

            proc2.StartInfo = checkoem
            proc2.Start()
            reply = proc2.StandardOutput.ReadToEnd
            proc2.WaitForExit()
            Try
                card1 = reply.IndexOf("DISPLAY\")
            Catch ex As Exception

            End Try
            While card1 > -1

                position2 = reply.IndexOf(":", card1)
                vendid = reply.Substring(card1, position2 - card1).Trim


                log("-" & vendid & "- Monitor id found")
                'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
                removedisplaydriver.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
                removedisplaydriver.Arguments = "remove =monitor " & Chr(34) & "@" & vendid & Chr(34)
                removedisplaydriver.UseShellExecute = False
                removedisplaydriver.CreateNoWindow = True
                removedisplaydriver.RedirectStandardOutput = True
                proc.StartInfo = removedisplaydriver
                Try
                    proc.Start()
                    reply2 = proc.StandardOutput.ReadToEnd
                    proc.WaitForExit()
                    log(reply2)
                Catch ex As Exception
                    log(ex.Message)
                    MsgBox("Cannot find ddudr in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                    Button1.Enabled = True
                    Button2.Enabled = True
                    Button3.Enabled = True
                    Exit Sub
                End Try

                card1 = reply.IndexOf("DISPLAY\", card1 + 1)

            End While
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** ddudr Remove complete *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())

            If DirectCast(e.Argument, String) = "AMD" Then
                cleanamd()
            End If
            If DirectCast(e.Argument, String) = "NVIDIA" Then
                Cleannvidia()
            End If
            If DirectCast(e.Argument, String) = "INTEL" Then
                ' Cleanintel()
            End If
            cleandriverstore()
            checkpcieroot()
            rescan()

        Catch ex As Exception
            log(ex.Message & ex.StackTrace)
            MsgBox("An error occured. Send the .log to the devs, Application will exit", MsgBoxStyle.Critical)
            stopme = True
        End Try

    End Sub
    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As System.Object, _
                             ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) _
                             Handles BackgroundWorker1.RunWorkerCompleted

        If stopme = True Then
            'Scan for new hardware to not let users into a non working state.
            Button1.Enabled = True
            Try
                Dim scan As New ProcessStartInfo
                scan.FileName = Application.StartupPath & "\" & Label3.Text & "\ddudr.exe"
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
            Application.Exit()
            Exit Sub
        End If
        Button1.Enabled = True
        Button2.Enabled = True
        Button3.Enabled = True
        ComboBox1.Enabled = True
        CheckBox2.Enabled = True
        CheckBox1.Enabled = True
        CheckBox3.Enabled = True

    End Sub

    Private Sub CheckForUpdatesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CheckForUpdatesToolStripMenuItem.Click
        checkupdatethread = New Thread(AddressOf Me.Checkupdates2)
        'checkthread.Priority = ThreadPriority.Highest
        checkupdatethread.Start()
    End Sub

    Private Sub RestoreWindowsUpdateDefaultToolStripMenuItem_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If version >= "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
                regkey.SetValue("SearchOrderConfig", 1)
                MsgBox("Done")
            Catch ex As Exception
            End Try
        End If
        If version >= "6.0" And version < "6.1" Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
                regkey.SetValue("DontSearchWindowsUpdate", 0)
                MsgBox("Done")
            Catch ex As Exception
            End Try
        End If
    End Sub
    Private Sub initlanguage(e As String)

        Try

            Dim buttontext() As String


            toolTip1.AutoPopDelay = 5000
            toolTip1.InitialDelay = 1000
            toolTip1.ReshowDelay = 250
            toolTip1.ShowAlways = True


            toolTip1.SetToolTip(Me.Button1, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & e & "\tooltip1.txt")) '// add each line as String Array.)
            Button1.Text = ""
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button1.txt") '// add each line as String Array.
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button1.Text = Button1.Text & vbNewLine
                End If
                Button1.Text = Button1.Text & buttontext(i)
            Next


            toolTip1.SetToolTip(Me.Button2, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & e & "\tooltip2.txt")) '// add each line as String Array.)
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button2.txt") '// add each line as String Array.
            Button2.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button2.Text = Button2.Text & vbNewLine
                End If
                Button2.Text = Button2.Text & buttontext(i)
            Next


            toolTip1.SetToolTip(Me.Button3, IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & e & "\tooltip3.txt")) '// add each line as String Array.)
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button3.txt") '// add each line as String Array.
            Button3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button3.Text = Button3.Text & vbNewLine
                End If
                Button3.Text = Button3.Text & buttontext(i)
            Next


            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button4.txt") '// add each line as String Array.
            Button4.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button4.Text = Button4.Text & vbNewLine
                End If
                Button4.Text = Button4.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label1.txt") '// add each line as String Array.
            Label1.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label1.Text = Label1.Text & vbNewLine
                End If
                Label1.Text = Label1.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label4.txt") '// add each line as String Array.
            Label4.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label4.Text = Label4.Text & vbNewLine
                End If
                Label4.Text = Label4.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label5.txt") '// add each line as String Array.
            Label5.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label5.Text = Label5.Text & vbNewLine
                End If
                Label5.Text = Label5.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label7.txt") '// add each line as String Array.
            Label7.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label7.Text = Label7.Text & vbNewLine
                End If
                Label7.Text = Label7.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label10.txt") '// add each line as String Array.
            Label10.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label10.Text = Label10.Text & vbNewLine
                End If
                Label10.Text = Label10.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label11.txt") '// add each line as String Array.
            Label11.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label11.Text = Label11.Text & vbNewLine
                End If
                Label11.Text = Label11.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\checkbox1.txt") '// add each line as String Array.
            CheckBox1.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox1.Text = CheckBox1.Text & vbNewLine
                End If
                CheckBox1.Text = CheckBox1.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\checkbox2.txt") '// add each line as String Array.
            CheckBox2.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox2.Text = CheckBox2.Text & vbNewLine
                End If
                CheckBox2.Text = CheckBox2.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\checkbox3.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text & vbNewLine
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
            tos = IO.File.ReadAllText(Application.StartupPath & "\settings\Languages\" & e & "\tos.txt")
        Catch ex As Exception
            log(ex.Message)
        End Try
    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        My.Settings.language = ComboBox2.Text
        initlanguage(ComboBox2.Text)
    End Sub

    Private Sub ToSToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToSToolStripMenuItem.Click
        MessageBox.Show(tos, "ToS")
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        If CheckBox4.Checked = True Then
            remove3dtvplay = True
        Else
            remove3dtvplay = False
        End If
    End Sub
End Class
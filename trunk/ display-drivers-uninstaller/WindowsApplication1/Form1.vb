Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading


Public Class Form1

    Dim removedisplaydriver As New ProcessStartInfo
    Dim removehdmidriver As New ProcessStartInfo
    Dim checkoem As New ProcessStartInfo
    Dim vendid As String = ""
    Dim vendidexpected As String = ""
    Dim provider As String = ""
    Dim proc As New Process
    Dim proc2 As New Process
    Dim prochdmi As New Process
    Dim reboot As Boolean = True
    Dim shutdown As Boolean = False
    Dim card1 As Integer
    Dim position2 As Integer
    Dim removephysx As Boolean = True
    Dim t As Thread
    Dim checkupdatethread As Thread
    Public updates As Boolean
    Dim reply As String = Nothing
    Dim reply2 As String = Nothing
    Dim userpth As String = System.Environment.GetEnvironmentVariable("userprofile")
    Dim time As String = DateAndTime.Now
    Dim locations As String = Application.StartupPath & "\DDU Logs\" & DateAndTime.Now.Year & " _" & DateAndTime.Now.Month & "_" & DateAndTime.Now.Day _
                              & "_" & DateAndTime.Now.Hour & "_" & DateAndTime.Now.Minute & "_" & DateAndTime.Now.Second & "_DDULog.log"

    Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive")
    Dim win8higher As Boolean = False
    Dim wantedvalue2 As String = Nothing
    Dim subregkey As RegistryKey = Nothing
    Dim wantedvalue As String = Nothing
    Dim regkey As RegistryKey
    Dim subregkey2 As RegistryKey = Nothing
    Dim currentdriverversion As String = Nothing
    Dim stopme As Boolean = False
    Dim version As String
    Private Function checkupdates() As Boolean
        Try
            Dim request2 As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create("http://wagnardmobile.com/DDU/currentversion.txt")
            Dim response2 As System.Net.HttpWebResponse = request2.GetResponse()

            Dim sr As System.IO.StreamReader = New System.IO.StreamReader(response2.GetResponseStream())

            Dim newestversion2 As String = sr.ReadToEnd()
            If newestversion2 <= (Application.ProductVersion) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Label11.Text = "Unable to Fetch updates!"
            Return True
        End Try


    End Function
    Private Sub Checkupdates2()
        If checkupdates() = False Then
            updates = False
        Else
            updates = True
        End If
        AccessUI()
    End Sub
    Private Sub AccessUI()
        If Me.InvokeRequired Then
            Me.Invoke(New MethodInvoker(AddressOf AccessUI))
        Else
            If updates = True Then
                Label11.Text = ("No Updates found. Program is up to date.")

            Else
                Label11.Text = ("Updates found! Expect limited support on older versions than the most recent.")

                Dim result = MsgBox("Updates are available! Visit forum thread now?", MsgBoxStyle.YesNoCancel)

                If result = MsgBoxResult.Yes And ComboBox1.SelectedIndex = 0 Then
                    Process.Start("http://forums.guru3d.com/showthread.php?t=379506")
                ElseIf result = MsgBoxResult.Yes And ComboBox1.SelectedIndex = 1 Then
                    Process.Start("http://forums.guru3d.com/showthread.php?t=379505")
                ElseIf result = MsgBoxResult.No Then
                    MsgBox("Note: Most bugs you find have probably already been fixed in the most recent version, if not please report them. Do not report bugs from older versions unless they have not been fixed yet.")
                ElseIf result = MsgBoxResult.Cancel Then
                    MsgBox("Note: Most bugs you find have probably already been fixed in the most recent version, if not please report them. Do not report bugs from older versions unless they have not been fixed yet.")
                End If



                'MsgBox("Updates are available! Visit forum thread now?    Note: Most bugs you find have probably already been fixed in the most recent version, if not please report them. Do not report bugs from older versions unless they have not been fixed yet.", MsgBoxStyle.Information)
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
        If ComboBox1.Text = "AMD" Then
            vendidexpected = "VEN_1002"
            provider = "Provider: Advanced Micro Devices"
        End If

        If ComboBox1.Text = "NVIDIA" Then
            vendidexpected = "VEN_10DE"
            provider = "Provider: NVIDIA"
        End If

        TextBox1.Text = TextBox1.Text + "*****  Uninstalling " & ComboBox1.Text & " driver... *****" + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        log("Uninstalling " + ComboBox1.Text + " driver ...")
        TextBox1.Text = TextBox1.Text + "***** Executing DEVCON Remove , Please wait(can take a few minutes *****) " + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        log("Executing DEVCON Remove")

        BackgroundWorker1.RunWorkerAsync(ComboBox1.Text)

    End Sub

    Private Sub cleandriverstore(ByVal e As String)

        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Executing Driver Store cleanUP(finding OEM step)... *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Executing Driver Store cleanUP(Find OEM)...")
        'Check the driver from the driver store  ( oemxx.inf)
        checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
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
        log("DEVCON DP_ENUM RESULT BELOW")
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

            If reply.Substring(position, classs - position).Contains(provider) Then
                Dim part As String = reply.Substring(oem, inf - oem)
                log(part + " Found")
                Dim deloem As New Diagnostics.ProcessStartInfo
                deloem.FileName = ".\" & Label3.Text & "\devcon.exe"
                deloem.Arguments = "dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                For Each child As String In IO.File.ReadAllLines(Environment.GetEnvironmentVariable("windir") & "\inf\" & part & ".inf")
                    If child.ToLower.Trim.Replace(" ", "").Contains("class=display") Or _
                        child.ToLower.Trim.Replace(" ", "").Contains("class=media") Or _
                        child.ToLower.Trim.Replace(" ", "").Contains("class=usb") Then
                        deloem.Arguments = "-f dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                    End If
                Next
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
    Private Sub clean(ByVal e As String)
        If e = "AMD" Then

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
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("CLIStart")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("CLI")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("CCC")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("HydraDM")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("HydraDM64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("HydraGrd")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("Grid64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("HydraMD64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("HydraMD")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i



            log("Killing Explorer.exe")
            appproc = Process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("ThumbnailExtractionHost")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("jusched")
            For i As Integer = 0 To appproc.Count - 1
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

                log(ex.Message)
            End Try
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
                For i As Integer = 0 To files.Count - 1
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


            End If

            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning known Regkeys... *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            log("Cleaning known Regkeys")
            'Delete AMD regkey


            'Deleting DCOM object

            log("Starting dcom/clsid/appid/typelib cleanup")
            Dim classroot() As String
            classroot = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\classroot.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To classroot.Length - 1
                        If child IsNot Nothing Then
                            If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                If subregkey IsNot Nothing Then
                                    If subregkey.GetValue("") IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If wantedvalue IsNot Nothing Then


                                            If IntPtr.Size = 8 Then
                                                Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue)
                                                    Dim appid As String = Nothing
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                    End Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib")

                                                    Dim typelib As String = Nothing
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                    End Try

                                                    If appid IsNot Nothing Then
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
                                                    If typelib IsNot Nothing Then
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
                                                Dim appid As String = Nothing
                                                Try
                                                    appid = subregkey2.GetValue("AppID").ToString
                                                Catch ex As Exception
                                                End Try
                                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib")
                                                Dim typelib As String = Nothing
                                                Try
                                                    typelib = subregkey2.GetValue("").ToString
                                                Catch ex As Exception
                                                End Try

                                                If appid IsNot Nothing Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                        subregkey2.DeleteSubKeyTree(appid)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                If typelib IsNot Nothing Then
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
                        End If
                    Next
                Next
            End If

            '-----------------
            'interface cleanup
            '-----------------

            Dim interfaces() As String
            interfaces = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\interface.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To interfaces.Length - 1
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
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
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            End If


            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("ActiveMovie Filter Class Manager") Then
                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance", False)
                                        If subregkey2 IsNot Nothing Then
                                            For Each child2 As String In subregkey2.GetSubKeyNames()
                                                If child2 IsNot Nothing Then
                                                    Dim superkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance\" & child2)
                                                    If superkey IsNot Nothing Then
                                                        If superkey.GetValue("FriendlyName") IsNot Nothing Then
                                                            wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                            If wantedvalue2.Contains("ATI MPEG") Or _
                                                                wantedvalue2.Contains("AMD MJPEG") Or _
                                                                wantedvalue2.Contains("ATI Video") Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("CLSID\" & child & "\Instance\" & child2)
                                                                Catch ex As Exception
                                                                End Try
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("CLSID\" & child2)
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

            'old dcom 
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)

            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.ToLower.Contains("amdwdst") Then
                            If child IsNot Nothing Then
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID\" + child, True)
                                If subregkey IsNot Nothing Then
                                    If subregkey.GetValue("AppID") IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("AppID").ToString
                                        regkey.DeleteSubKeyTree(child)
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

            Dim clsidleftover() As String
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To clsidleftover.Length - 1
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" + child, False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
                                        If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            End If

            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To clsidleftover.Length - 1
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InprocServer32", False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
                                        If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            End If

            If IntPtr.Size = 8 Then
                clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\clsidleftover.cfg") '// add each line as String Array.
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        For i As Integer = 0 To clsidleftover.Length - 1
                            If child IsNot Nothing Then
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\InprocServer32", False)
                                If subregkey IsNot Nothing Then
                                    If subregkey.GetValue("") IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If wantedvalue IsNot Nothing Then
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                regkey.DeleteSubKeyTree(child)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    Next
                End If
            End If


            'end of decom?

            'remove opencl registry Khronos
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            Try
                regkey.DeleteSubKeyTree("Khronos")

            Catch ex As Exception
                log(ex.Message + " Opencl Khronos")
            End Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                Try
                    regkey.DeleteSubKeyTree("Khronos")

                Catch ex As Exception

                    log(ex.Message + " Opencl Khronos")
                End Try
            End If
            Dim UserAc As String = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString()
            If win8higher Then
                Dim reginfos As RegistryKey = Nothing
                Dim FolderAcl As New RegistrySecurity
                'setting permission to registry
                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles")
                If regkey IsNot Nothing Then

                    For Each child In regkey.GetSubKeyNames()
                        For i As Integer = 0 To driverfiles.Length - 1
                            If child IsNot Nothing Then
                                If child.ToLower.StartsWith(driverfiles(i).ToLower) Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree _
                                            ("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child)
                                    Catch ex As Exception
                                        log(ex.Message)
                                    End Try
                                End If
                            End If
                        Next
                    Next
                End If

                'setting back the registry permission to normal.
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=SYSTEM"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/keyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=Administrators"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /revoke=" & UserAc
                If UserAc <> "Administrator" Then
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                End If
                'cleaning pnpresources
                'setting permission to registry
                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                Catch ex As Exception
                    log(ex.Message)
                End Try

                If IntPtr.Size = 8 Then
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos /owner=Administrators"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos /grant=" & UserAc & "=f"
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    Try
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                    Catch ex As Exception
                        log(ex.Message)
                    End Try
                End If

                If IntPtr.Size = 8 Then
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE /owner=Administrators"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE /grant=" & UserAc & "=f"
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    Try
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
                    Catch ex As Exception
                        log(ex.Message)
                    End Try
                End If

                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
                Catch ex As Exception
                    log(ex.Message)
                End Try

                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
                Catch ex As Exception
                    log(ex.Message)
                End Try

                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
                Catch ex As Exception
                    log(ex.Message)
                End Try

            Else
                Dim reginfos As RegistryKey = Nothing
                Dim FolderAcl As New RegistrySecurity
                'setting permission to registry
                Try
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /grant=" & UserAc & "=f"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)

                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For Each child In regkey.GetValueNames()
                            For i As Integer = 0 To driverfiles.Length - 1
                                If child IsNot Nothing Then
                                    If child.ToLower.StartsWith(driverfiles(i).ToLower) Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message)
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If


                    '-----------------------------------------------
                    'setting back the registry permission to normal.
                    '-----------------------------------------------

                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /revoke=" & UserAc
                    If UserAc <> "Administrator" Then
                        prochdmi.Start()
                        prochdmi.WaitForExit()
                        System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    End If
                Catch ex As Exception
                    log(ex.Message & " We Got a security warning avoided on the subacl Part win7")
                End Try
            End If

            '--------------------------------
            'System environement path cleanup
            '--------------------------------

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Session Manager\Environment", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If child IsNot Nothing Then
                            If child.Contains("AMDAPPSDKROOT") Then
                                Try
                                    regkey.DeleteValue(child)
                                Catch ex As Exception
                                End Try
                            End If
                            If child.Contains("Path") Then
                                wantedvalue = regkey.GetValue(child).ToString()
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
                    Next
                End If
            Catch ex As Exception
                log("Path section " & ex.Message)
            End Try
            'end system environement patch cleanup

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
          ("Directory\background\shellex\ContextMenuHandlers", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("ACE") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If


            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("ATI") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If



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



            'Here im not deleting the ATI completly for safety until 100% sure


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("ACE") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("CBT") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If


            ' This may not be super safe to do.
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies\Install", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("ATI Catalyst") Or child.Contains("ATI MCAT") Or _
                            child.Contains("AVT") Or child.Contains("ccc") Or _
                            child.Contains("Packages") Or child.Contains("WirelessDisplay") Or _
                            child.Contains("SteadyVideo") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If

                Next
            End If


            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            If child.Contains("ATI") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If

                    Next
                End If
            End If



            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("DisplayName") IsNot Nothing Then

                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                If wantedvalue IsNot Nothing Then
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

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If


            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
      ("Software\Microsoft\Installer\Features", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                        ("Software\Microsoft\Installer\Features\" & child, True)
                        If subregkey IsNot Nothing Then
                            For Each child2 As String In subregkey.GetValueNames()
                                If child2 IsNot Nothing And subregkey IsNot Nothing Then
                                    If child2.Contains("SteadyVideo") Then

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If

                            Next
                        End If
                    End If
                Next
            End If


            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
      ("Software\Microsoft\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                        ("Software\Microsoft\Installer\Products\" & child, True)
                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("ProductName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("AMD Steady Video") Or _
                                    wantedvalue.Contains("ATI AVIVO") Then

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If

                            End If
                        End If
                    End If
                Next
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("DisplayName") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If wantedvalue IsNot Nothing Then
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
                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                End If
                            End If
                        End If

                    Next
                End If
            End If


            If IntPtr.Size = 8 Then
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
            End If


            log("Debug : Starting S-1-5-xx region cleanUP")
            Dim basekey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
        ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", True)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If super IsNot Nothing Then
                        If super.Contains("S-1-5") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If child IsNot Nothing Then
                                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                            "\InstallProperties", True)

                                        If subregkey IsNot Nothing Then
                                            If subregkey.GetValue("DisplayName") IsNot Nothing Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString

                                                If wantedvalue IsNot Nothing Then
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

                                                        regkey.DeleteSubKeyTree(child)
                                                        'okay .. important part here to fixed the famous AMD yellow mark.
                                                        'The yellow mark in this case is really stupid imo and shouldn't even
                                                        'be thrown as a warning to the end user... it has not bad effect.


                                                        Dim superregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                         ("Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If child2 IsNot Nothing Then
                                                                    Dim subsuperregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                             ("Installer\UpgradeCodes\" & child2, True)
                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring In subsuperregkey.GetValueNames()
                                                                            If wantedstring IsNot Nothing Then
                                                                                If wantedstring.Contains(child) Then
                                                                                    superregkey.DeleteSubKeyTree(child2)
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
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If child2 IsNot Nothing Then
                                                                    Dim subsuperregkey As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                             ("Software\Microsoft\Installer\UpgradeCodes\" & child2, True)
                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring In subsuperregkey.GetValueNames()
                                                                            If wantedstring IsNot Nothing Then
                                                                                If wantedstring.Contains(child) Then
                                                                                    superregkey.DeleteSubKeyTree(child2)

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

            log("Debug : End S-1-5-xx region cleanUP")
            basekey = My.Computer.Registry.LocalMachine.OpenSubKey _
     ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If super IsNot Nothing Then
                        If super.Contains("S-1-5") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)

                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If child IsNot Nothing Then
                                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child, False)
                                        If subregkey IsNot Nothing Then
                                            For Each wantedstring In subregkey.GetValueNames()
                                                If wantedstring IsNot Nothing Then
                                                    If subregkey.GetValue(wantedstring) IsNot Nothing Then
                                                        wantedvalue = subregkey.GetValue(wantedstring).ToString
                                                        If wantedvalue IsNot Nothing Then
                                                            If wantedvalue.Contains("ATI\CIM\") Or _
                                                                wantedvalue.Contains("ATI Technologies\Multimedia\") Or _
                                                                wantedvalue.Contains("AMD APP\") Or _
                                                                wantedvalue.Contains("ATI Technologies\cccutil") Or _
                                                                wantedvalue.Contains("ATI.ACE\") Then
                                                                Try
                                                                    My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Installer\Features\" & wantedstring)
                                                                Catch ex As Exception
                                                                End Try
                                                                regkey.DeleteSubKeyTree(child)
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


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If child IsNot Nothing Then
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

                                log(ex.Message + " SharedDLLS")
                            End Try
                        End If
                    End If
                Next
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
             ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If child IsNot Nothing Then
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

                                    log(ex.Message + " SharedDLLS")
                                End Try
                            End If
                        End If
                    Next
                End If
            End If

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If child IsNot Nothing Then
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

                                log(ex.Message + " HKLM..CU\Installer\Folders")
                            End Try
                        End If
                    End If
                Next
            End If


            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
            ("Installer\Products\" & child, True)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("ProductName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If wantedvalue IsNot Nothing Then
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

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
    ("SOFTWARE\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
            ("SOFTWARE\Classes\Installer\Products\" & child, True)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("ProductName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If wantedvalue IsNot Nothing Then
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

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If



            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
            ("CLSID\" & child, False)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("SteadyVideoBHO") Then

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\CLSID\" & child, False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
                                        If wantedvalue.Contains("AMDWDST") Or _
                                        wantedvalue.Contains("ATI Transcoder DB Enum") Or _
                                        wantedvalue.Contains("ATI Transcoder") Or _
                                       wantedvalue.Contains("ATI Transcoder DB") Or _
                                       wantedvalue.Contains("SteadyVideoBHO") Then

                                            regkey.DeleteSubKeyTree(child)

                                        End If
                                    End If
                                End If
                            End If
                        End If

                    Next
                End If
            End If




            If IntPtr.Size = 8 Then

                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\Interface", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\Interface\" & child, False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
                                        If wantedvalue.Contains("SteadyVideoBHO") Then

                                            regkey.DeleteSubKeyTree(child)

                                        End If
                                    End If
                                End If
                            End If
                        End If

                    Next
                End If
            End If


        End If

        If e = "NVIDIA" Then

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
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("NvTmru")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i


            appproc = Process.GetProcessesByName("nvxdsync")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("nvtray")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("dwm")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("WWAHost")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            'log("Killing Explorer")

            'appproc = Process.GetProcessesByName("explorer")
            'For i As Integer = 0 To appproc.Count - 1
            '    appproc(i).Kill()
            'Next i
            'Delete NVIDIA data Folders
            'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Cleaning UpdatusUser users ac if present *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            log("Cleaning UpdatusUser users ac if present")
            Try
                Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName + ",computer")
                Dim NewUser As DirectoryEntry = AD.Children.Find("UpdatusUser")

                AD.Children.Remove(NewUser)
            Catch ex As Exception

                log(ex.Message + " UpdatusUser")
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
                If child IsNot Nothing Then
                    If child.Contains("UpdatusUser") Then

                        Try
                            TestDelete(child)
                        Catch ex As Exception

                            log(ex.Message + " UpdatusUser")
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

            filePath = Environment.GetFolderPath _
      (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

            If removephysx Then
                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception

                    log(ex.Message & "program files\nvidia corporation")
                End Try
            Else
                Try
                    TestDelete(filePath)
                Catch ex As Exception
                End Try

            End If

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

            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"

                If removephysx Then
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception

                        log(ex.Message & "programfiles x86\nvidia corporation")
                    End Try
                Else
                    Try
                        TestDelete(filePath)
                    Catch ex As Exception
                    End Try

                End If

            End If

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
            For i As Integer = 0 To files.Count - 1
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
            log("Starting reg cleanUP")


            'Deleting DCOM object /classroot
            log("Starting dcom/clsid/appid/typelib cleanup")
            log("Step 1/2")
            Dim classroot() As String
            classroot = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\classroot.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To classroot.Length - 1
                        If child IsNot Nothing Then
                            If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                If subregkey IsNot Nothing Then
                                    If subregkey.GetValue("") IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("").ToString
                                        If wantedvalue IsNot Nothing Then

                                            If removephysx Then
                                                If IntPtr.Size = 8 Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib")

                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try

                                                        If appid IsNot Nothing Then
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
                                                        If typelib IsNot Nothing Then
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
                                                    Dim appid As String = Nothing
                                                    Try
                                                        appid = subregkey2.GetValue("AppID").ToString
                                                    Catch ex As Exception
                                                    End Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib")
                                                    Dim typelib As String = Nothing
                                                    Try
                                                        typelib = subregkey2.GetValue("").ToString
                                                    Catch ex As Exception
                                                    End Try

                                                    If appid IsNot Nothing Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If typelib IsNot Nothing Then
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

                                            Else
                                                If child.ToLower.Contains("gamesconfigserver") Then   'Physx related
                                                    'do nothing
                                                Else
                                                    If IntPtr.Size = 8 Then
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue)
                                                            Dim appid As String = Nothing
                                                            Try
                                                                appid = subregkey2.GetValue("AppID").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & wantedvalue & "\TypeLib")
                                                            Dim typelib As String = Nothing
                                                            Try
                                                                typelib = subregkey2.GetValue("").ToString
                                                            Catch ex As Exception
                                                            End Try
                                                            If appid IsNot Nothing Then
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
                                                            If typelib IsNot Nothing Then
                                                                Try
                                                                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True)
                                                                    subregkey.DeleteSubKeyTree(typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                                                            subregkey.DeleteSubKeyTree(wantedvalue)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue)
                                                        Dim appid As String = Nothing
                                                        Try
                                                            appid = subregkey2.GetValue("AppID").ToString
                                                        Catch ex As Exception
                                                        End Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib")
                                                        Dim typelib As String = Nothing
                                                        Try
                                                            typelib = subregkey2.GetValue("").ToString
                                                        Catch ex As Exception
                                                        End Try

                                                        If appid IsNot Nothing Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If typelib IsNot Nothing Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
                                                                subregkey.DeleteSubKeyTree(typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
                                                        subregkey.DeleteSubKeyTree(wantedvalue)
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
                        End If
                    Next
                Next
            End If

            log("Step 2/2")

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then

                        If child.ToLower.Contains("comupdatus") Or child.ToLower.Contains("nv3d") Or _
                            child.ToLower.Contains("nvui") Or child.ToLower.Contains("nvvsvc") Or child.ToLower.Contains("nvxd") Or _
                           child.ToLower.Contains("gamesconfigserver") Or _
                          child.ToLower.Contains("nvidia.installer") Or child.ToLower.Contains("displayserver") Then

                            subregkey = regkey.OpenSubKey(child)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("AppID") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("AppID").ToString
                                    If wantedvalue IsNot Nothing Then
                                        If removephysx Then
                                            If IntPtr.Size = 8 Then
                                                Try
                                                    Dim appid As String = wantedvalue
                                                    If appid IsNot Nothing Then
                                                        My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\AppID\" & appid)
                                                    End If
                                                Catch ex As Exception
                                                End Try
                                            End If
                                            Try

                                                Dim appid As String = wantedvalue
                                                If appid IsNot Nothing Then
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
                                                        Dim appid As String = wantedvalue
                                                        If appid IsNot Nothing Then
                                                            My.Computer.Registry.ClassesRoot.DeleteSubKeyTree("Wow6432Node\AppID\" & appid)
                                                        End If
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                Try

                                                    Dim appid As String = wantedvalue
                                                    If appid IsNot Nothing Then
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

            log("Extra step")

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\LocalServer32", False)
                            If subregkey.GetValue("") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If wantedvalue IsNot Nothing And wantedvalue.ToLower.Contains("\nvidia corporation\") Then

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

                                                If appid IsNot Nothing Then
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
                                                If typelib IsNot Nothing Then
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

                                            If appid IsNot Nothing Then
                                                Try
                                                    subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                    subregkey2.DeleteSubKeyTree(appid)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                            If typelib IsNot Nothing Then
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
                                                    If appid IsNot Nothing Then
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
                                                    If typelib IsNot Nothing Then
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

                                                If appid IsNot Nothing Then
                                                    Try
                                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                        subregkey.DeleteSubKeyTree(appid)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                If typelib IsNot Nothing Then
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
                        Catch ex As Exception
                        End Try
                    End If
                Next
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID")
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\LocalServer32", False)
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing And wantedvalue.ToLower.Contains("\nvidia corporation\") Then

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

                                                    If appid IsNot Nothing Then
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
                                                    If typelib IsNot Nothing Then
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

                                                If appid IsNot Nothing Then
                                                    Try
                                                        subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                        subregkey2.DeleteSubKeyTree(appid)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                                If typelib IsNot Nothing Then
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
                                                        If appid IsNot Nothing Then
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
                                                        If typelib IsNot Nothing Then
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

                                                    If appid IsNot Nothing Then
                                                        Try
                                                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If typelib IsNot Nothing Then
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
                            Catch ex As Exception
                            End Try
                        End If
                    Next
                End If
            End If

            log("exta step #2")
            Dim clsidleftover() As String
            clsidleftover = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftover.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To clsidleftover.Length - 1
                        If child IsNot Nothing Then
                            Try
                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InProcServer32", False)
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
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

                                                        If appid IsNot Nothing Then
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
                                                        If typelib IsNot Nothing Then
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

                                                    If appid IsNot Nothing Then
                                                        Try
                                                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                            subregkey2.DeleteSubKeyTree(appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                    If typelib IsNot Nothing Then
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
                                                            If appid IsNot Nothing Then
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
                                                            If typelib IsNot Nothing Then
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

                                                        If appid IsNot Nothing Then
                                                            Try
                                                                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
                                                                subregkey.DeleteSubKeyTree(appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                        If typelib IsNot Nothing Then
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
                Next
            End If

            log("Interface CleanUP")
            'interface cleanup
            Dim interfaces() As String
            interfaces = IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\interface.cfg") '// add each line as String Array.
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    For i As Integer = 0 To interfaces.Length - 1
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)
                            If subregkey IsNot Nothing Then
                                If subregkey.GetValue("") IsNot Nothing Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If wantedvalue IsNot Nothing Then
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
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            End If


            log("Finished dcom/clsid/appid/typelib cleanup")

            'end of deleting dcom stuff
            Dim UserAc As String = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString()
            If win8higher Then
                Dim reginfos As RegistryKey = Nothing
                Dim FolderAcl As New RegistrySecurity
                'setting permission to registry
                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles")
                If regkey IsNot Nothing Then
                    For Each child In regkey.GetSubKeyNames()
                        For i As Integer = 0 To driverfiles.Length - 1
                            If child IsNot Nothing Then
                                If child.ToLower.StartsWith(driverfiles(i).ToLower) Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree _
                                            ("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child)
                                    Catch ex As Exception
                                        log(ex.Message & "pnplockdownfiles")
                                    End Try
                                End If
                            End If
                        Next
                    Next
                End If

                'setting back the registry permission to normal.
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=SYSTEM"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/keyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /owner=Administrators"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /revoke=" & UserAc
                If UserAc <> "Administrator" Then
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                End If
                '----------------

                'cleaning pnpresources
                'setting permission to registry
                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
                Catch ex As Exception
                    log(ex.Message & "pnp resources khronos")
                End Try

                If IntPtr.Size = 8 Then
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos /owner=Administrators"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos /grant=" & UserAc & "=f"
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    Try
                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                    Catch ex As Exception
                        log(ex.Message & "pnpresources wow6432node khronos")
                    End Try
                End If

                removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation /owner=Administrators"
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = False
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                removehdmidriver.Arguments = _
                    "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation /grant=" & UserAc & "=f"
                prochdmi.Start()
                prochdmi.WaitForExit()
                System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                Try
                    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
                Catch ex As Exception
                    log(ex.Message & "pnp ressources")
                End Try
            Else
                Dim reginfos As RegistryKey = Nothing
                Dim FolderAcl As New RegistrySecurity
                'setting permission to registry
                Try
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /grant=" & UserAc & "=f"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = False
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For Each child In regkey.GetValueNames()
                            For i As Integer = 0 To driverfiles.Length - 1
                                If child IsNot Nothing Then

                                    If child.ToLower.StartsWith(driverfiles(i).ToLower) Then

                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message & "pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        Next
                    End If

                    'setting back the registry permission to normal.
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    removehdmidriver.Arguments = _
                        "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /revoke=" & UserAc
                    If UserAc <> "Administrator" Then
                        prochdmi.Start()
                        prochdmi.WaitForExit()
                        System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    End If
                Catch ex As Exception
                    log(ex.Message & " We Got a security warning avoided on the subacl Part win7")
                End Try
            End If

            '--------------------------------
            'System environement path cleanup
            '--------------------------------

            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Session Manager\Environment", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If child IsNot Nothing Then
                            If child.Contains("Path") Then
                                wantedvalue = regkey.GetValue(child).ToString()
                                Try
                                    Select Case True
                                        Case wantedvalue.Contains(sysdrv & "\Program Files (x86)\NVIDIA Corporation\PhysX\Common;")
                                            wantedvalue = wantedvalue.Replace(sysdrv & "\Program Files (x86)\AMD APP\bin\x86_64;", "")
                                            regkey.SetValue(child, wantedvalue)
                                    End Select
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log("Path section " & ex.Message)
            End Try

            '-------------------------------------
            'end system environement patch cleanup
            '-------------------------------------

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)

            If regkey IsNot Nothing Then
                If regkey.GetValue("AppInit_DLLs") IsNot Nothing Then
                    wantedvalue = regkey.GetValue("AppInit_DLLs")   'Will need to consider the comma in the future for multiple value
                    If wantedvalue IsNot Nothing Then
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
                    regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                End If

            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)

                If regkey IsNot Nothing Then
                    If regkey.GetValue("AppInit_DLLs") IsNot Nothing Then
                        wantedvalue = regkey.GetValue("AppInit_DLLs")
                        If wantedvalue IsNot Nothing Then
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
                        regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                    End If
                End If
            End If




            If removephysx Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If child IsNot Nothing Then
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


            'remove opencl registry Khronos
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            Try
                regkey.DeleteSubKeyTree("Khronos")

            Catch ex As Exception

                log(ex.Message + " Opencl Khronos")
            End Try

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                Try
                    regkey.DeleteSubKeyTree("Khronos")

                Catch ex As Exception

                    log(ex.Message + " Opencl Khronos")
                End Try
            End If

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
         ("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If child IsNot Nothing Then
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


            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
             ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If child IsNot Nothing Then
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

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                           Or child.Contains("NVXD") Or child.Contains("NvXD") Then

                            regkey.DeleteSubKeyTree(child)
                        End If
                    End If

                Next
            End If



            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                           Or child.Contains("NVXD") Or child.Contains("NvXD") Or child.Contains("AGEIA") Or _
                           child.Contains("Nv3DV") Then

                            If removephysx Then
                                regkey.DeleteSubKeyTree(child)
                            Else
                                If child.Contains("PhysX") Then
                                    'do nothing
                                Else
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                        End If
                    End If

                Next
            End If


            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing And removephysx Then
                            If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                               Or child.Contains("NVXD") Or child.Contains("NvXD") Or child.Contains("AGEIA") Then

                                regkey.DeleteSubKeyTree(child)
                            End If
                        Else
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\NVIDIA Corporation", True)
                            If regkey IsNot Nothing Then
                                Try
                                    regkey.DeleteSubKeyTree("Global")
                                Catch ex As Exception
                                End Try
                            End If
                        End If

                    Next
                End If
            End If




            If IntPtr.Size = 8 Then

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)

                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("DisplayName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("NVIDIA") And Not wantedvalue.Contains("3DTV") Or _
                                    wantedvalue.Contains("SHIELD Streaming") Or _
                                    wantedvalue.Contains("GeForce Experience") Then
                                        If removephysx Then
                                            regkey.DeleteSubKeyTree(child)
                                        Else
                                            If wantedvalue.Contains("PhysX") Then
                                                'do nothing
                                            Else
                                                regkey.DeleteSubKeyTree(child)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If



            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            For Each child As String In regkey.GetSubKeyNames()
                If child IsNot Nothing Then
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                    If subregkey IsNot Nothing Then
                        If subregkey.GetValue("DisplayName") IsNot Nothing Then
                            wantedvalue = subregkey.GetValue("DisplayName").ToString
                            If wantedvalue IsNot Nothing Then
                                If wantedvalue.Contains("NVIDIA") And Not wantedvalue.Contains("3DTV") Or _
                                    wantedvalue.Contains("SHIELD Streaming") Or _
                                    wantedvalue.Contains("GeForce Experience") Then
                                    If removephysx Then
                                        regkey.DeleteSubKeyTree(child)
                                    Else
                                        If wantedvalue.Contains("PhysX") Then
                                            'do nothing
                                        Else
                                            regkey.DeleteSubKeyTree(child)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If

            Next

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
            For Each child As String In regkey.GetSubKeyNames()
                If child IsNot Nothing Then
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, True)
                    If subregkey IsNot Nothing Then
                        If subregkey.GetValue("ProfileImagePath") IsNot Nothing Then
                            wantedvalue = subregkey.GetValue("ProfileImagePath").ToString
                            If wantedvalue IsNot Nothing Then
                                If wantedvalue.Contains("UpdatusUser") Then
                                    regkey.DeleteSubKeyTree(child)
                                End If
                            End If
                        End If
                    End If
                End If

            Next

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("NVIDIA") Then

                                        regkey.DeleteSubKeyTree(child)

                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If



            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteValue("Nvtmru")
                Catch ex As Exception

                    log(ex.Message + " Nvtmru")
                End Try
                Try
                    regkey.DeleteValue("ShadowPlay")
                Catch ex As Exception

                    log(ex.Message + " ShadowPlay")
                End Try
            End If

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


            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Debug : Starting S-1-5-xx region cleanUP *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            log("Debug : Starting S-1-5-xx region cleanUP")
            Dim basekey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
                  ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", True)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If super IsNot Nothing Then
                        If super.Contains("S-1-5") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If child IsNot Nothing Then
                                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                            "\InstallProperties", True)

                                        If subregkey IsNot Nothing Then
                                            If subregkey.GetValue("DisplayName") IsNot Nothing Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                                If wantedvalue IsNot Nothing Then
                                                    If wantedvalue.Contains("NVIDIA") Then

                                                        If removephysx Then
                                                            regkey.DeleteSubKeyTree(child)
                                                        Else
                                                            If wantedvalue.Contains("PhysX") Then
                                                                'do nothing
                                                            Else
                                                                regkey.DeleteSubKeyTree(child)
                                                            End If
                                                        End If

                                                        Dim superregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                         ("Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                Dim subsuperregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                         ("Installer\UpgradeCodes\" & child2, True)
                                                                If subsuperregkey IsNot Nothing Then
                                                                    For Each wantedstring In subsuperregkey.GetValueNames()
                                                                        If wantedstring IsNot Nothing Then
                                                                            If wantedstring.Contains(child) Then

                                                                                If removephysx Then
                                                                                    superregkey.DeleteSubKeyTree(child2)
                                                                                Else
                                                                                    If wantedvalue.Contains("PhysX") Then
                                                                                        'do nothing
                                                                                    Else
                                                                                        superregkey.DeleteSubKeyTree(child2)
                                                                                    End If
                                                                                End If
                                                                            End If
                                                                        End If
                                                                    Next
                                                                End If
                                                            Next
                                                        End If
                                                        superregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                         ("Software\Microsoft\Installer\UpgradeCodes", True)
                                                        If superregkey IsNot Nothing Then
                                                            For Each child2 As String In superregkey.GetSubKeyNames()
                                                                If child2 IsNot Nothing Then
                                                                    Dim subsuperregkey As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                             ("Software\Microsoft\Installer\UpgradeCodes\" & child2, True)
                                                                    If subsuperregkey IsNot Nothing Then
                                                                        For Each wantedstring In subsuperregkey.GetValueNames()
                                                                            If wantedstring IsNot Nothing Then
                                                                                If wantedstring.Contains(child) Then

                                                                                    If removephysx Then
                                                                                        superregkey.DeleteSubKeyTree(child2)
                                                                                    Else
                                                                                        If wantedvalue.Contains("PhysX") Then
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

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
            ("Installer\Products\" & child, True)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("ProductName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("NVIDIA") Then

                                        If removephysx Then
                                            regkey.DeleteSubKeyTree(child)
                                        Else
                                            If wantedvalue.Contains("PhysX") Then
                                                'do nothing
                                            Else
                                                regkey.DeleteSubKeyTree(child)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("SOFTWARE\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
            ("SOFTWARE\Classes\Installer\Products\" & child, True)

                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("ProductName") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("NVIDIA") Then

                                        If removephysx Then
                                            regkey.DeleteSubKeyTree(child)
                                        Else
                                            If wantedvalue.Contains("PhysX") Then
                                                'do nothing
                                            Else
                                                regkey.DeleteSubKeyTree(child)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
            End If

            If removephysx Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If child IsNot Nothing Then
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\" & child, True)
                            If subregkey IsNot Nothing Then
                                For Each wantedstring In subregkey.GetValueNames()
                                    If wantedstring IsNot Nothing Then
                                        If subregkey.GetValue(wantedstring) IsNot Nothing Then
                                            wantedvalue = subregkey.GetValue(wantedstring).ToString
                                            If wantedvalue IsNot Nothing Then
                                                If wantedvalue.Contains("PhysX") Then

                                                    regkey.DeleteSubKeyTree(child)

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
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** End of Registry Cleaning *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("End of Registry Cleaning")
        System.Threading.Thread.Sleep(50)
    End Sub
    Private Sub removepcieroot()
        If reboot Or shutdown Then
            log("remove PCI-E Root for sandy / ivy processor (fix laptop issues) if found")
            Dim removepcieroot As New ProcessStartInfo
            removepcieroot.FileName = ".\" & Label3.Text & "\devcon.exe"
            removepcieroot.Arguments = "findall =system " & Chr(34) & "*VEN_8086&DEV_0151*" & Chr(34)
            removepcieroot.UseShellExecute = False
            removepcieroot.CreateNoWindow = True
            removepcieroot.RedirectStandardOutput = True

            proc2.StartInfo = removepcieroot
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


                log("-" & vendid & "- PCI-E root id found")
                Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** System May Freeze for a couple of seconds. Please be Patient it is not. *****" + vbNewLine)
                Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
                Invoke(Sub() TextBox1.ScrollToCaret())
                'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
                removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
                removedisplaydriver.Arguments = "remove =system " & Chr(34) & "@" & vendid & Chr(34)
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
                    MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                    Button1.Enabled = True
                    Button2.Enabled = True
                    Button3.Enabled = True
                    Exit Sub
                End Try
                
                card1 = reply.IndexOf("PCI\", card1 + 1)
            End While
        End If
    End Sub
    Private Sub rescan()
        
        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = ".\" & Label3.Text & "\devcon.exe"
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
            Dim appproc = Process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
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
            ComboBox2.SelectedIndex = ComboBox2.FindString("English")
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

        If version >= "5.1" Then
            Label2.Text = "Windows XP or Server 2003"
        End If

        If version >= "6.0" Then
            Label2.Text = "Windows Vista or Server 2008"
        End If

        If version >= "6.1" Then
            Label2.Text = "Windows 7 or Server 2008r2"

        End If

        If version >= "6.2" Then
            Label2.Text = "Windows 8 or Server 2012"
            win8higher = True
        End If

        If version >= "6.3" Then
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
                Dim myExe As String = Application.StartupPath & "\x64\devcon.exe"
                System.IO.File.WriteAllBytes(myExe, My.Resources.devcon64)

                myExe = Application.StartupPath & "\x64\subinacl.exe"
                System.IO.File.WriteAllBytes(myExe, My.Resources.subinacl64)
            Catch ex As Exception
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try
        Else
            Try
                Dim myExe As String = Application.StartupPath & "\x86\devcon.exe"
                System.IO.File.WriteAllBytes(myExe, My.Resources.devcon32)

                myExe = Application.StartupPath & "\x86\subinacl.exe"
                System.IO.File.WriteAllBytes(myExe, My.Resources.subinacl32)

            Catch ex As Exception
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try
        End If

        If arch = True Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x64\devcon.exe") Then
                MsgBox("Unable to find Devcon. Please refer to the log.", MsgBoxStyle.Critical)
                Button1.Enabled = False
            End If
        ElseIf arch = False Then
            If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x86\devcon.exe") Then
                MsgBox("Unable to find Devcon. Please refer to the log.", MsgBoxStyle.Critical)
                Button1.Enabled = False
            End If
        End If

        TextBox1.Text = TextBox1.Text + "DDU Version: " + Label6.Text.Replace("V", "") + vbNewLine
        log("DDU Version: " + Label6.Text.Replace("V", ""))
        log("OS: " + Label2.Text)
        log("Architecture: " & Label3.Text)

        'Videocard type indentification
        checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
        checkoem.Arguments = "findall =display"
        checkoem.UseShellExecute = False
        checkoem.CreateNoWindow = True
        checkoem.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.

        proc2.StartInfo = checkoem
        proc2.Start()
        reply = proc2.StandardOutput.ReadToEnd
        proc2.WaitForExit()
        log(reply)

        Dim part As String
        Dim match As Boolean = False
        card1 = reply.IndexOf(":")
        position2 = reply.IndexOf("PCI", card1 + 1)
        If position2 < 0 Then
            position2 = reply.IndexOf("matching", card1 + 1)
            match = True
        End If

        While card1 > -1
            If Not match Then
                part = reply.Substring(card1 + 2, (position2 - card1 - 3))
                card1 = reply.IndexOf(":", card1 + 1)
                position2 = reply.IndexOf("PCI", card1 + 1)
                If position2 < 0 Then
                    position2 = reply.IndexOf("matching", card1 + 1)
                    match = True
                End If
                TextBox1.Text = TextBox1.Text + "Detected GPU : " + part + vbNewLine
                log("Detected GPU : " + part)
            End If
            If match Then
                part = reply.Substring(card1 + 2, (position2 - card1 - 5))
                card1 = reply.IndexOf(":", card1 + 1)
                TextBox1.Text = TextBox1.Text + "Detected GPU : " + part + vbNewLine
                log("Detected GPU : " + part)
            End If
        End While
        If reply.Contains("VEN_10DE") Then
            ComboBox1.SelectedIndex = 0
        Else
            ComboBox1.SelectedIndex = 1
        End If
        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000")
        Try
            currentdriverversion = regkey.GetValue("DriverVersion").ToString
        Catch ex As Exception
            currentdriverversion = "Unknown"
        End Try
        TextBox1.Text = TextBox1.Text + "Current driver version : " + currentdriverversion + vbNewLine
        log("Current driver version : " + currentdriverversion)

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
    End Sub

    Public Sub TestDelete(ByVal folder As String)
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "Deleting some specials folders, it may take some times..." + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Deleting some specials folders, it could take some times...")
        'ensure that this folder can be accessed with current user ac.
        Dim UserAc As String = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString()
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
                    TraverseDirectory(diChild)
                Catch ex As Exception
                End Try
            Else
                If diChild.ToString.ToLower.Contains("physx") Then
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
                Try
                    TraverseDirectory(diChild)
                Catch ex As Exception
                End Try
            Else
                If diChild.ToString.ToLower.Contains("physx") Then
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
        If di.GetFiles().Count = 0 Then
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
            'System.Threading.Thread.Sleep(25)  '50 millisecond stall (0.025 Seconds)

        Next
    End Sub


    Private Sub Cleanup(ByVal directory As String, ByVal KeepDur As Integer)
        'Code taken from my CoDUO FoV Changer program, thus why it uses a keepdur, it's supposed to delete logs older than whatever days. I set it to 2 seconds instead of modifying the code. Lol
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
            PictureBox2.Image = My.Resources.Nvidia_GeForce_Logo
        Else
            CheckBox3.Visible = False
            PictureBox2.Image = My.Resources.RadeonLogo1
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

            checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                    removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
            log("DEVCON Remove Display Complete")
            'Next
            'For i As Integer = 0 To 1 'loop 2 time to check if there is a remaining videocard.
            checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
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

                    removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                        MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
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
                checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
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

                        removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                            MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
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

                removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                        removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                            MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                            Button1.Enabled = True
                            Button2.Enabled = True
                            Button3.Enabled = True
                            Exit Sub
                        End Try
                    End If
                    card1 = reply.IndexOf("ROOT\", card1 + 1)
                End While
            End If

            log("DEVCON Remove Audio/HDMI Complete")
            'removing monitor and hidden monitor

            log("DEVCON Remove Monitor started")

            checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
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
                    MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                    Button1.Enabled = True
                    Button2.Enabled = True
                    Button3.Enabled = True
                    Exit Sub
                End Try

                card1 = reply.IndexOf("DISPLAY\", card1 + 1)

            End While
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** DEVCON Remove complete *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            clean(DirectCast(e.Argument, String))
            cleandriverstore(DirectCast(e.Argument, String))
            removepcieroot()
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
                scan.FileName = ".\" & Label3.Text & "\devcon.exe"
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
        If Version >= "6.1" Then
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
            Button1.Text = ""
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button1.txt") '// add each line as String Array.
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button1.Text = Button1.Text & vbNewLine
                End If
                Button1.Text = Button1.Text & buttontext(i)
            Next

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\button2.txt") '// add each line as String Array.
            Button2.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Button2.Text = Button2.Text & vbNewLine
                End If
                Button2.Text = Button2.Text & buttontext(i)
            Next

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
                Label4.Text = Label1.Text & buttontext(i)
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

            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & e & "\label9.txt") '// add each line as String Array.
            Label9.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    Label9.Text = Label9.Text & vbNewLine
                End If
                Label9.Text = Label9.Text & buttontext(i)
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
        Catch ex As Exception
            log(ex.Message)
        End Try
    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        My.Settings.language = ComboBox2.Text
        initlanguage(ComboBox2.Text)
    End Sub
End Class
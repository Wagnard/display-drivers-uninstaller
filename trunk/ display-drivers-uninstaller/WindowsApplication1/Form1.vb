Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        

        If Button1.Text = "Done." Then
            Close()
            Exit Sub
        Else
            MsgBox("For SLI users, it is recommended that you disable it before continuing or a black screen may occur.", MsgBoxStyle.Information)
            Button1.Enabled = False
            CheckBox2.Enabled = False
            Button1.Text = "Uninstalling..."

            Dim removedisplaydriver As New ProcessStartInfo
            Dim removehdmidriver As New ProcessStartInfo
            Dim vendid As String = ""
            Dim provider As String = ""
            Dim proc As New Process

            If ComboBox1.Text = "AMD" Then
                vendid = "@*ven_1002*"
                provider = "Provider: Advanced Micro Devices"
            End If

            If ComboBox1.Text = "NVIDIA" Then
                vendid = "@*ven_10de*"
                provider = "Provider: NVIDIA"
            End If
            TextBox1.Text = TextBox1.Text + "Uninstalling " & ComboBox1.Text & " driver ..." + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            TextBox1.Text = TextBox1.Text + "Executing DEVCON Remove" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
            removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
            removedisplaydriver.Arguments = "remove =display " & Chr(34) & vendid & Chr(34)
            removedisplaydriver.UseShellExecute = False
            removedisplaydriver.CreateNoWindow = True
            removedisplaydriver.RedirectStandardOutput = True

            removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
            removehdmidriver.Arguments = "remove =MEDIA " & Chr(34) & vendid & Chr(34)
            removehdmidriver.UseShellExecute = False
            removehdmidriver.CreateNoWindow = True
            removehdmidriver.RedirectStandardOutput = True


            'creation dun process fantome pour le wait on exit.

            Try

                proc.StartInfo = removedisplaydriver
                proc.Start()
                proc.WaitForExit()
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
                MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                Button1.Text = "Done."
                Button1.Enabled = True
                Exit Sub
            End Try
            '   System.Threading.Thread.Sleep(1000)
            TextBox1.Text = TextBox1.Text + "DEVCON Remove Display Complete" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Dim prochdmi As New Process
            prochdmi.StartInfo = removehdmidriver
            prochdmi.Start()
            prochdmi.WaitForExit()

            '      System.Threading.Thread.Sleep(1000)
            TextBox1.Text = TextBox1.Text + "DEVCON Remove Audio/hdmi Complete" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Dim checkoem As New Diagnostics.ProcessStartInfo


            TextBox1.Text = TextBox1.Text + "Executing Driver Store cleanUP(find OEM)..." + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            'Check the driver from the driver store  ( oemxx.inf)
            checkoem.FileName = ".\" & Label3.Text & "\devcon.exe"
            checkoem.Arguments = "dp_enum"
            checkoem.UseShellExecute = False
            checkoem.CreateNoWindow = True
            checkoem.RedirectStandardOutput = True

            'creation dun process fantome pour le wait on exit.
            Dim proc2 As New Diagnostics.Process
            proc2.StartInfo = checkoem
            proc2.Start()
            Dim Reply As String = proc2.StandardOutput.ReadToEnd
            proc2.WaitForExit()

            ' System.Threading.Thread.Sleep(1000)

            'Preparing to read output.

            Dim position As Integer

            position = Reply.IndexOf(provider)
5:

            If position < 0 Then

                GoTo 10

            Else
                'work around...
                Dim part As String = Reply.Substring(position - 14, 10).Replace("oem", "em")
                position = Reply.IndexOf(provider, position + 1)
                part = part.Replace("em", "m")
                part = part.Replace("m", "oem")
                part = part.Replace(vbNewLine, "")
                TextBox1.Text = TextBox1.Text + part + " found" + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
                'Uninstall Driver from driver store  delete from (oemxx.inf)
                Dim deloem As New Diagnostics.ProcessStartInfo

                deloem.FileName = ".\" & Label3.Text & "\devcon.exe"
                deloem.Arguments = ("dp_delete " & part)
                deloem.UseShellExecute = False
                deloem.CreateNoWindow = True
                deloem.RedirectStandardOutput = True
                'creation dun process fantome pour le wait on exit.
                Dim proc3 As New Diagnostics.Process
                TextBox1.Text = TextBox1.Text + "Executing Driver Store cleanUP(Delete OEM)..." + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
                proc3.StartInfo = deloem
                proc3.Start()
                Dim Reply2 As String = proc3.StandardOutput.ReadToEnd
                proc3.WaitForExit()

                '  System.Threading.Thread.Sleep(1000)



                TextBox1.Text = TextBox1.Text + Reply2 + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()


                GoTo 5
            End If

        End If
10:
        TextBox1.Text = TextBox1.Text + "Driver Store cleanUP complete." + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()



        TextBox1.Text = TextBox1.Text + "Cleaning process/services..." + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        'Delete left over files.

        If ComboBox1.Text = "AMD" Then

            'STOP AMD service
            Dim stopservice As New ProcessStartInfo
            stopservice.FileName = "cmd.exe"
            stopservice.Arguments = " /C" & "sc stop " & Chr(34) & "AMD External Events Utility" & Chr(34)
            stopservice.UseShellExecute = False
            stopservice.CreateNoWindow = True
            stopservice.RedirectStandardOutput = True

            Dim processstopservice As New Process
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            ' System.Threading.Thread.Sleep(1000)

            'Delete AMD service
            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "AMD External Events Utility" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            ' System.Threading.Thread.Sleep(1000)

            'kill process CCC.exe / MOM.exe /Clistart.exe HydraDM/HydraDM64(if it exist)

            Dim killpid As New ProcessStartInfo
            killpid.FileName = "cmd.exe"
            killpid.Arguments = " /C" & "taskkill /f /im CLIStart.exe"
            killpid.UseShellExecute = False
            killpid.CreateNoWindow = True
            killpid.RedirectStandardOutput = True

            Dim processkillpid As New Process
            processkillpid.StartInfo = killpid
            processkillpid.Start()
            processkillpid.WaitForExit()

            ' System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im MOM.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            Dim appproc = Process.GetProcessesByName("MOM")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '  System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im CLI.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("CLI")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '  System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im CCC.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("CCC")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '   System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im HydraDM.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("HydraDM")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '  System.Threading.Thread.Sleep(100)


            'killpid.Arguments = " /C" & "taskkill /f /im HydraDM64.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("HydraDM64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            '   System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im HydraGrd.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("HydraGrd")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '  System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im Grid64.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("Grid64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '   System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im HydraMD64.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("HydraMD64")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '   System.Threading.Thread.Sleep(100)

            'killpid.Arguments = " /C" & "taskkill /f /im HydraMD.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("HydraMD")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '   System.Threading.Thread.Sleep(100)

            TextBox1.Text = TextBox1.Text + "Killing Explorer.exe" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()

            'killpid.Arguments = " /C" & "taskkill /f /im explorer.exe"
            'processkillpid.StartInfo = killpid
            'processkillpid.Start()
            'processkillpid.WaitForExit()
            appproc = Process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '  System.Threading.Thread.Sleep(100)
            appproc = Process.GetProcessesByName("ThumbnailExtractionHost")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            '    System.Threading.Thread.Sleep(100)
            appproc = Process.GetProcessesByName("jusched")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            '   System.Threading.Thread.Sleep(1000)
            'Delete AMD data Folders
            TextBox1.Text = TextBox1.Text + "Cleaning Directory" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Dim filePath As String

            If CheckBox1.Checked = True Then
                Try
                    TestDelete("C:\AMD")
                Catch ex As Exception
                End Try
                System.Threading.Thread.Sleep(100)
                filePath = "C:\AMD"

                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

            End If

            filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\ATI"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\ATI"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\ATI"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\AMD\SteadyVideo\resources"
            Try
                Dim attribute As System.IO.FileAttributes = FileAttributes.Normal
                File.SetAttributes(filePath + "\AMD_SV_bar_middle.png", attribute)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\AMD\SteadyVideo"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try


            filePath = Environment.GetFolderPath _
               (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            'Not sure if this work on XP

            filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\ATI"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\AMD"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.CommonProgramFiles) + "\ATI Technologies\Multimedia"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"

                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"

                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo\resources"
                Try
                    Dim attribute As System.IO.FileAttributes = FileAttributes.Normal
                    File.SetAttributes(filePath + "\AMD_SV_bar_middle.png", attribute)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try


            End If

            TextBox1.Text = TextBox1.Text + "Cleaning known Regkeys..." + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            'Delete AMD regkey
            Dim count As Int32 = 0
            Dim subregkey As RegistryKey = Nothing
            Dim wantedvalue As String = Nothing

            Dim regkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Directory\background\shellex\ContextMenuHandlers", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("ACE") Then

                        regkey.DeleteSubKeyTree(child)

                    End If
                    count += 1
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("ATI") Then

                        regkey.DeleteSubKeyTree(child)

                    End If
                    count += 1
                Next
            End If

            count = 0

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteValue("HydraVisionDesktopManager")
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                Try
                    regkey.DeleteValue("Grid")
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

                Try
                    regkey.DeleteValue("HydraVisionMDEngine")
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

            End If
            count += 1


            'Here im not deleting the ATI completly for safety until 100% sure
            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("ACE") Then

                        regkey.DeleteSubKeyTree(child)

                    End If
                    count += 1
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("CBT") Then

                        regkey.DeleteSubKeyTree(child)

                    End If
                    count += 1
                Next
            End If
            count = 0

            ' This may not be super safe to do.
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\ATI Technologies\Install", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("ATI Catalyst") Or child.Contains("ATI MCAT") Or _
                        child.Contains("AVT") Or child.Contains("ccc") Or _
                        child.Contains("Packages") Or child.Contains("WirelessDisplay") Or _
                        child.Contains("SteadyVideo") Then

                        regkey.DeleteSubKeyTree(child)

                    End If
                    count += 1
                Next
            End If
            count = 0

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()

                        If child.Contains("ATI") Then

                            regkey.DeleteSubKeyTree(child)

                        End If

                        count += 1
                    Next
                End If
            End If
            count = 0


            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            For Each child As String In regkey.GetSubKeyNames()
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                If subregkey IsNot Nothing Then
                    wantedvalue = subregkey.GetValue("DisplayName")
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
                                wantedvalue.Contains("ATI AVIVO") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                End If
                count += 1
            Next

            count = 0

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                ("Software\Microsoft\Installer\Features", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                    ("Software\Microsoft\Installer\Features\" & child, True)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetValueNames()
                            If child2 IsNot Nothing And subregkey IsNot Nothing Then
                                If child2.Contains("SteadyVideo") Then

                                    regkey.DeleteSubKeyTree(child)

                                End If
                            End If
                            count += 1
                        Next
                    End If
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                ("Software\Microsoft\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    subregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                    ("Software\Microsoft\Installer\Products\" & child, True)
                    If subregkey IsNot Nothing Then
                        wantedvalue = subregkey.GetValue("ProductName")
                        If wantedvalue IsNot Nothing Then
                            If wantedvalue.Contains("AMD Steady Video") Or _
                            wantedvalue.Contains("ATI AVIVO") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                        count += 1

                    End If
                Next
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                        If subregkey IsNot Nothing Then
                            wantedvalue = subregkey.GetValue("DisplayName")
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
                                    wantedvalue.Contains("ATI AVIVO") Then
                                    regkey.DeleteSubKeyTree(child)

                                End If
                            End If
                        End If
                        count += 1
                    Next
                End If
            End If
            count = 0

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                If regkey IsNot Nothing Then
                    Try
                        regkey.DeleteValue("StartCCC")

                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                    Try

                        regkey.DeleteValue("AMD AVT")

                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                End If
            End If

            count = 0

            Dim basekey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", True)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If basekey IsNot Nothing Then
                        If super.Contains("S-1-5") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()

                                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                        "\InstallProperties", True)

                                    If subregkey IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("DisplayName")
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
                                                    wantedvalue.Contains("ATI AVIVO") Then

                                                regkey.DeleteSubKeyTree(child)
                                                'okay .. important part here to fixed the famous AMD yellow mark.
                                                'The yellow mark in this case is really stupid imo and shouldn't even
                                                'be thrown as a warning to the end user... it has not bad effect.
                                                'But im gona fix this b'cause im a 'PROFESSIONAL' :)

                                                Dim superregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                 ("Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        Dim subsuperregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                 ("Installer\UpgradeCodes\" & child2, True)
                                                        If subsuperregkey IsNot Nothing Then
                                                            For Each wantedstring In subsuperregkey.GetValueNames()
                                                                If wantedstring.Contains(child) Then
                                                                    superregkey.DeleteSubKeyTree(child2)
                                                                End If
                                                            Next
                                                        End If
                                                    Next
                                                End If
                                                superregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                 ("Software\Microsoft\Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        Dim subsuperregkey As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                 ("Software\Microsoft\Installer\UpgradeCodes\" & child2, True)
                                                        If subsuperregkey IsNot Nothing Then
                                                            For Each wantedstring In subsuperregkey.GetValueNames()
                                                                If wantedstring.Contains(child) Then
                                                                    superregkey.DeleteSubKeyTree(child2)

                                                                End If
                                                            Next
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End If
                                    End If
                                    count += 1
                                Next
                            End If
                        End If
                    End If
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
               ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
        ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\" & child, True)
                    For Each wantedstring In subregkey.GetValueNames()
                        If subregkey IsNot Nothing Then
                            wantedvalue = subregkey.GetValue(wantedstring)
                            If wantedvalue IsNot Nothing Then
                                If wantedvalue.Contains("ATI\CIM\") Or _
                                    wantedvalue.Contains("ATI.ACE\") Then

                                    regkey.DeleteSubKeyTree(child)

                                End If
                            End If
                        End If
                        count += 1
                    Next
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If child.Contains("ATI\CIM\") Or _
                        child.Contains("SteadyVideo") Or _
                        child.Contains("ATI Technologies\Multimedia") Or _
                        child.Contains("cccutil") Then
                        If regkey IsNot Nothing Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                                TextBox1.Select(TextBox1.Text.Length, 0)
                                TextBox1.ScrollToCaret()
                            End Try
                        End If

                    End If
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If child.Contains("ATI\CIM\") Or child.Contains("AMD AVT") Or _
                        child.Contains("ATI\CIM\") Or _
                        child.Contains("AMP APP\") Or _
                        child.Contains("AMD\SteadyVideo\") Or _
                        child.Contains("ATI.ACE\") Or _
                        child.Contains("HydraVision\") Or _
                        child.Contains("ATI Technologies\Multimedia\") Then
                        If regkey IsNot Nothing Then
                            Try
                                regkey.DeleteValue(child)
                            Catch ex As Exception
                                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                                TextBox1.Select(TextBox1.Text.Length, 0)
                                TextBox1.ScrollToCaret()
                            End Try
                        End If

                    End If
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
        ("Installer\Products\" & child, True)

                    If subregkey IsNot Nothing Then
                        wantedvalue = subregkey.GetValue("ProductName")
                        If wantedvalue IsNot Nothing Then
                            If wantedvalue.Contains("CCC Help") Or wantedvalue.Contains("AMD Accelerated") Or _
                                wantedvalue.Contains("Catalyst Control Center") Or _
                                wantedvalue.Contains("AMD Catalyst Install Manager") Or _
                                wantedvalue.Contains("ccc-utility") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    End If
                    count += 1
                Next
            End If
            count = 0


            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
        ("CLSID\" & child, False)

                    If subregkey IsNot Nothing Then
                        wantedvalue = subregkey.GetValue("")
                        If wantedvalue IsNot Nothing Then
                            If wantedvalue.Contains("SteadyVideoBHO") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    End If
                    count += 1
                Next
            End If

            count = 0

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
            ("Interface\" & child, False)

                    If subregkey IsNot Nothing Then
                        wantedvalue = subregkey.GetValue("")
                        If wantedvalue IsNot Nothing Then
                            If wantedvalue.Contains("SteadyVideoBHO") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    End If
                    count += 1
                Next
            End If
            count = 0


            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Wow6432Node\CLSID\" & child, False)
                        Catch ex As Exception

                        End Try
                        If subregkey IsNot Nothing Then
                            wantedvalue = subregkey.GetValue("")
                            If wantedvalue IsNot Nothing Then
                                If wantedvalue.Contains("SteadyVideoBHO") Then

                                    regkey.DeleteSubKeyTree(child)

                                End If
                            End If
                        End If
                        count += 1
                    Next
                End If
            End If
            count = 0



            If IntPtr.Size = 8 Then

                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                    ("Wow6432Node\Interface", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        Try
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Wow6432Node\Interface\" & child, False)
                        Catch ex As Exception

                        End Try
                        If subregkey IsNot Nothing Then
                            wantedvalue = subregkey.GetValue("")
                            If wantedvalue IsNot Nothing Then
                                If wantedvalue.Contains("SteadyVideoBHO") Then

                                    regkey.DeleteSubKeyTree(child)

                                End If
                            End If
                        End If
                        count += 1
                    Next
                End If
            End If
            count = 0


            'System.Threading.Thread.Sleep(2000)
            'Dim processInfo As New ProcessStartInfo("Explorer.exe")
            'Process.Start(processInfo)

        End If

        If ComboBox1.Text = "NVIDIA" Then

            'STOP NVIDIA service
            Dim stopservice As New ProcessStartInfo
            stopservice.FileName = "cmd.exe"
            stopservice.Arguments = " /C" & "sc stop nvsvc"
            stopservice.UseShellExecute = False
            stopservice.CreateNoWindow = True
            stopservice.RedirectStandardOutput = True

            Dim processstopservice As New Process
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)

            stopservice.Arguments = " /C" & "sc stop nvUpdatusService"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)

            stopservice.Arguments = " /C" & "sc stop " & Chr(34) & "Stereo Service" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)

            'Delete NVIDIA service

            stopservice.Arguments = " /C" & "sc delete nvsvc"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)

            stopservice.Arguments = " /C" & "sc delete nvUpdatusService"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)

            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "Stereo Service" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(100)
            'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
            'holding files in the NVIDIA folders sometimes.

            Dim appproc = Process.GetProcessesByName("Lcore")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            System.Threading.Thread.Sleep(100)
            

            System.Threading.Thread.Sleep(100)

            appproc = Process.GetProcessesByName("NvTmru")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            System.Threading.Thread.Sleep(100)

            appproc = Process.GetProcessesByName("nvxdsync")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            appproc = Process.GetProcessesByName("nvtray")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i

            System.Threading.Thread.Sleep(100)

            appproc = Process.GetProcessesByName("dwm")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            System.Threading.Thread.Sleep(1000)

            TextBox1.Text = TextBox1.Text + "Killing Explorer.exe" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()

            appproc = Process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Count - 1
                appproc(i).Kill()
            Next i
            'Delete NVIDIA data Folders
            'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/
            TextBox1.Text = TextBox1.Text + "Cleaning UpdatusUser users account if present" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Try
                Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName + ",computer")
                Dim NewUser As DirectoryEntry = AD.Children.Find("UpdatusUser")

                AD.Children.Remove(NewUser)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            TextBox1.Text = TextBox1.Text + "Cleaning Diectory" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()

            Dim filePath As String

            If CheckBox1.Checked = True Then
                If CheckBox1.Checked = True Then
                    Try
                        TestDelete("C:\NVIDIA")
                    Catch ex As Exception
                    End Try
                    System.Threading.Thread.Sleep(100)
                    filePath = "C:\NVIDIA"
                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try

                End If
            End If

            ' here I erase the folders / files of the nvidia GFE / update in users.
            filePath = Environment.GetEnvironmentVariable("UserProfile")
            Dim parentPath As String = IO.Path.GetDirectoryName(filePath)
            filePath = parentPath
            For Each child As String In Directory.GetDirectories(filePath)
                If child.Contains("UpdatusUser") Then

                    Try
                        TestDelete(child)
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                    System.Threading.Thread.Sleep(200) 'just to be sure files are not holded anymore.

                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                    'Yes we do it 2 time. This will workaround a problem on I think is (sybolic link)
                    Try
                        TestDelete(child)
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                    System.Threading.Thread.Sleep(200) 'just to be sure files are not holded anymore.

                    Try
                        My.Computer.FileSystem.DeleteDirectory _
                            (child, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                End If
            Next



            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.LocalApplicationData) + "\NVIDIA"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ApplicationData) + "\NVIDIA"

            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.CommonProgramFiles) + "\NVIDIA Corporation"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            If IntPtr.Size = 8 Then
                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
                Try
                    My.Computer.FileSystem.DeleteDirectory _
                        (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try

            End If

            'Not sure if this work on XP

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"
            Try
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)
            Catch ex As Exception
                TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End Try


            TextBox1.Text = TextBox1.Text + "Cleaning Regkeys" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            'Delete NVIDIA regkey
            TextBox1.Text = TextBox1.Text + "Starting reg cleanUP" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Dim count As Int32 = 0
            Dim regkey As RegistryKey
            Dim wantedvalue As String = Nothing
            Dim subregkey As RegistryKey

            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In My.Computer.Registry.ClassesRoot.GetSubKeyNames()

                    If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                       Or child.Contains("NVXD") Or child.Contains("NvXD") Then

                        regkey.DeleteSubKeyTree(child)
                    End If
                    count += 1
                Next
            End If
            count = 0

            regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                       Or child.Contains("NVXD") Or child.Contains("NvXD") Then

                        regkey.DeleteSubKeyTree(child)
                    End If
                    count += 1
                Next
            End If

            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                       Or child.Contains("NVXD") Or child.Contains("NvXD") Or child.Contains("AGEIA") Then

                        regkey.DeleteSubKeyTree(child)
                    End If
                    count += 1
                Next
            End If
            count = 0

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()

                        If child.Contains("NvCpl") Or child.Contains("NVIDIA") Or child.Contains("Nvvsvc") _
                           Or child.Contains("NVXD") Or child.Contains("NvXD") Or child.Contains("AGEIA") Then

                            regkey.DeleteSubKeyTree(child)
                        End If
                        count += 1
                    Next
                End If
            End If
            count = 0

            count = 0

            If IntPtr.Size = 8 Then

                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)

                For Each child As String In regkey.GetSubKeyNames()
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                    If subregkey IsNot Nothing Then
                        wantedvalue = subregkey.GetValue("DisplayName")
                        If wantedvalue IsNot Nothing Then
                            If wantedvalue.Contains("NVIDIA") Then

                                regkey.DeleteSubKeyTree(child)

                            End If
                        End If
                    End If
                    count += 1
                Next
            End If

            count = 0

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            For Each child As String In regkey.GetSubKeyNames()
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                If subregkey IsNot Nothing Then

                    wantedvalue = subregkey.GetValue("DisplayName")
                    If wantedvalue IsNot Nothing Then
                        If wantedvalue.Contains("NVIDIA") Then
                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                End If
                count += 1
            Next

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
            For Each child As String In regkey.GetSubKeyNames()
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, True)
                If subregkey IsNot Nothing Then

                    wantedvalue = subregkey.GetValue("ProfileImagePath")
                    If wantedvalue IsNot Nothing Then
                        If wantedvalue.Contains("UpdatusUser") Then
                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                End If
                count += 1
            Next

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()

                    If child.Contains("0bbca823-e77d-419e-9a44-5adec2c8eeb0") Then

                        regkey.DeleteSubKeyTree(child)
                    End If
                    count += 1
                Next
            End If

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    regkey.DeleteValue("Nvtmru")
                Catch ex As Exception
                    TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                    TextBox1.Select(TextBox1.Text.Length, 0)
                    TextBox1.ScrollToCaret()
                End Try
            End If

            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                If regkey IsNot Nothing Then
                    Try
                        regkey.DeleteValue("StereoLinksInstall")
                    Catch ex As Exception
                        TextBox1.Text = TextBox1.Text + ex.Message + vbNewLine
                        TextBox1.Select(TextBox1.Text.Length, 0)
                        TextBox1.ScrollToCaret()
                    End Try
                End If
            End If

            count = 0
            TextBox1.Text = TextBox1.Text + "Debug : Starting S-1-5-xx region cleanUP" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()
            Dim basekey As RegistryKey = My.Computer.Registry.LocalMachine.OpenSubKey _
                            ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData", True)
            If basekey IsNot Nothing Then
                For Each super As String In basekey.GetSubKeyNames()
                    If basekey IsNot Nothing Then
                        If super.Contains("S-1-5") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()

                                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                        ("Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
                        "\InstallProperties", True)

                                    If subregkey IsNot Nothing Then
                                        wantedvalue = subregkey.GetValue("DisplayName")
                                        If wantedvalue IsNot Nothing Then
                                            If wantedvalue.Contains("NVIDIA") Then

                                                regkey.DeleteSubKeyTree(child)

                                                Dim superregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                 ("Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        Dim subsuperregkey As RegistryKey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                                                                                 ("Installer\UpgradeCodes\" & child2, True)
                                                        If subsuperregkey IsNot Nothing Then
                                                            For Each wantedstring In subsuperregkey.GetValueNames()
                                                                If wantedstring.Contains(child) Then
                                                                    superregkey.DeleteSubKeyTree(child2)
                                                                End If
                                                            Next
                                                        End If
                                                    Next
                                                End If
                                                superregkey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                 ("Software\Microsoft\Installer\UpgradeCodes", True)
                                                If superregkey IsNot Nothing Then
                                                    For Each child2 As String In superregkey.GetSubKeyNames()
                                                        Dim subsuperregkey As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey _
                                                                                 ("Software\Microsoft\Installer\UpgradeCodes\" & child2, True)
                                                        If subsuperregkey IsNot Nothing Then
                                                            For Each wantedstring In subsuperregkey.GetValueNames()
                                                                If wantedstring.Contains(child) Then
                                                                    superregkey.DeleteSubKeyTree(child2)

                                                                End If
                                                            Next
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End If
                                    End If
                                    count += 1
                                Next
                            End If
                        End If
                    End If
                Next
            End If
            count = 0

            TextBox1.Text = TextBox1.Text + "Debug : End of S-1-5-xx region cleanUP" + vbNewLine
            TextBox1.Select(TextBox1.Text.Length, 0)
            TextBox1.ScrollToCaret()

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
                ("Installer\Products", True)

            For Each child As String In regkey.GetSubKeyNames()

                subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
    ("Installer\Products\" & child, True)

                If subregkey IsNot Nothing Then
                    wantedvalue = subregkey.GetValue("ProductName")
                    If wantedvalue IsNot Nothing Then
                        If wantedvalue.Contains("NVIDIA") Then

                            regkey.DeleteSubKeyTree(child)

                        End If
                    End If
                End If
                count += 1
            Next

        End If
        TextBox1.Text = TextBox1.Text + "End of Registry Cleaning" + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        System.Threading.Thread.Sleep(1000)
        TextBox1.Text = TextBox1.Text + "Scanning for new device..." + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()
        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = ".\" & Label3.Text & "\devcon.exe"
        scan.Arguments = "rescan"
        scan.UseShellExecute = False
        scan.CreateNoWindow = True
        scan.RedirectStandardOutput = True

        'creation dun process fantome pour le wait on exit.
        Dim proc4 As New Process
        proc4.StartInfo = scan
        proc4.Start()
        proc4.WaitForExit()

        TextBox1.Text = TextBox1.Text + "Clean uninstall completed!" + vbNewLine
        TextBox1.Select(TextBox1.Text.Length, 0)
        TextBox1.ScrollToCaret()


        Button1.Enabled = True
        Button1.Text = "Done."
        log(TextBox1.Text)
        log("Finished.")
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load


        If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\Logs") Then
            My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\Logs")
        Else
            log("Log directory already exists.")
        End If


        If My.Settings.logbox = "" Or My.Settings.logbox = "dontlog" Then
            CheckBox2.Checked = False
        Else
            CheckBox2.Checked = True
        End If


        Dim version As String
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
            Button1.Text = "Done."
            log("Unsupported OS.")
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

        End If
        log("OS : " + Label2.Text)



        If arch = True Then
            Label3.Text = "x64"
        Else
            Label3.Text = "x86"
        End If
        log("Architecture: " & Label3.Text)

        If arch = True Then
            Try
                Dim myExe As String = Application.StartupPath & "\x64\devcon.exe"
                If Not System.IO.File.Exists(myExe) Then
                    System.IO.File.WriteAllBytes(myExe, My.Resources.devcon64)
                End If
            Catch ex As Exception
                log(ex.Message)
                TextBox1.AppendText(ex.Message)
            End Try
        Else
            Try
                Dim myExe As String = Application.StartupPath & "\x86\devcon.exe"
                If Not System.IO.File.Exists(myExe) Then
                    System.IO.File.WriteAllBytes(myExe, My.Resources.devcon32)
                End If
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



    End Sub

    Public Sub TestDelete(ByVal folder As String)
        'ensure that this folder can be accessed with current user account.
        Dim UserAccount As String = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString()
        Dim FolderInfo As IO.DirectoryInfo = New IO.DirectoryInfo(folder)
        Dim FolderAcl As New DirectorySecurity
        FolderAcl.AddAccessRule(New FileSystemAccessRule(UserAccount, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit Or InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow))
        'FolderAcl.SetAccessRuleProtection(True, False) 'uncomment to remove existing permissions
        FolderInfo.SetAccessControl(FolderAcl)
        System.Threading.Thread.Sleep(500)
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
            TraverseDirectory(diChild)
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
            TraverseDirectory(diChild)
        Next

        'Now that we have no more child directories to traverse, delete all of the files
        'in the current directory, and then delete the directory itself.
        CleanAllFilesInDirectory(di)


        'The containing directory can only be deleted if the directory
        'is now completely empty and all files previously within
        'were deleted.
        If di.GetFiles().Count = 0 Then
            di.Delete()
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
            fi.IsReadOnly = False
            fi.Delete()

            'On a rare occasion, files being deleted might be slower than program execution, and upon returning
            'from this call, attempting to delete the directory will throw an exception stating it is not yet
            'empty, even though a fraction of a second later it actually is.  Therefore the 'Optional' code below
            'can stall the process just long enough to ensure the file is deleted before proceeding. The value
            'can be adjusted as needed from testing and running the process repeatedly.
            System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)

        Next
    End Sub

    Private Sub Form1_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        'If Button1.Text = "Done." Then
        '  Module1.wlog.Dispose()
        '   My.Computer.FileSystem.DeleteFile(Module1.location)
        'End If
        'I've commented this out because even if you have logging enabled and you want to see your logs, if the button was "Done." then it would delete it. Add it back if you want, but I think it needs to be worked on or left unused.
        If CheckBox2.Checked = False Then
            Module1.wlog.Dispose()
            My.Computer.FileSystem.DeleteFile(Module1.location)
            Cleanup(Application.StartupPath & "\Logs", 2) 'Deletes all older logs, instead only the most recent one.
        End If
        Try
            My.Computer.FileSystem.DeleteDirectory(Application.StartupPath & "\x64", FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
        End Try
        Try
            My.Computer.FileSystem.DeleteDirectory(Application.StartupPath & "\x86", FileIO.DeleteDirectoryOption.DeleteAllContents)
        Catch ex As Exception
        End Try
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
End Class
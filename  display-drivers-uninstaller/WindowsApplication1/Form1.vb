Imports System.DirectoryServices

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim removedisplaydriver As New ProcessStartInfo
        Dim removehdmidriver As New ProcessStartInfo
        Dim jobstatus As Boolean
        Dim vendid As String = ""
        Dim provider As String = ""


        If ComboBox1.Text = "AMD" Then
            vendid = "@*ven_1002*"
            provider = "Provider: Advanced Micro Devices"
        End If

        If ComboBox1.Text = "NVIDIA" Then
            vendid = "@*ven_10de*"
            provider = "Provider: NVIDIA"
        End If
        TextBox1.Text = TextBox1.Text + "Uninstalling " & ComboBox1.Text & " driver if found." + vbNewLine

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

        If Button1.Text = "Done." Then
            Close()

        Else
            Button1.Enabled = False
            Button1.Text = "Uninstalling..."

            'creation dun process fantome pour le wait on exit.
            Dim proc As New Process
            proc.StartInfo = removedisplaydriver
            proc.Start()
            proc.WaitForExit()

            System.Threading.Thread.Sleep(1000)

            Dim prochdmi As New Process
            prochdmi.StartInfo = removehdmidriver
            prochdmi.Start()
            prochdmi.WaitForExit()

            System.Threading.Thread.Sleep(1000)

            Dim checkoem As New Diagnostics.ProcessStartInfo



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
            proc2.WaitForExit()

            System.Threading.Thread.Sleep(1000)

            'Preparing to read output.
            Dim Reply As String = proc2.StandardOutput.ReadToEnd
            Dim position As Integer

            position = Reply.IndexOf(provider)
5:

            If position < 0 Then

                GoTo 10

            Else
                'work around...
                Dim part As String = Reply.Substring(position - 14, 10).Replace("oem", "em")
                position = Reply.IndexOf(provider, position + 1)
                part = part.Replace("em", "oem")
                part = part.Replace(vbNewLine, "")


                Button1.Enabled = True
                Button1.Text = "Done."

                'Uninstall Driver from driver store  delete from (oemxx.inf)
                Dim deloem As New Diagnostics.ProcessStartInfo

                deloem.FileName = ".\" & Label3.Text & "\devcon.exe"
                deloem.Arguments = ("dp_delete " & part)
                deloem.UseShellExecute = False
                deloem.CreateNoWindow = True
                deloem.RedirectStandardOutput = True
                'creation dun process fantome pour le wait on exit.
                Dim proc3 As New Diagnostics.Process

                proc3.StartInfo = deloem
                proc3.Start()
                proc3.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                Dim Reply2 As String = proc3.StandardOutput.ReadToEnd

                TextBox1.Text = TextBox1.Text + Reply2
                TextBox1.Text = TextBox1.Text + "Removed " & ComboBox1.Text & " from the driver store." + vbNewLine
                jobstatus = True
                GoTo 5
            End If

        End If
10:
        If jobstatus = True Then

           
            TextBox1.Text = TextBox1.Text + "Deleting " & ComboBox1.Text & " files/Folders/services." + vbNewLine
            'Delete left over files.
            On Error Resume Next
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

                System.Threading.Thread.Sleep(1000)

                'Delete AMD service
                stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "AMD External Events Utility" & Chr(34)

                processstopservice.StartInfo = stopservice
                processstopservice.Start()
                processstopservice.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                'kill process CCC.exe / MOM.exe /Clistart.exe (if it exist)

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

                System.Threading.Thread.Sleep(1000)

                killpid.Arguments = " /C" & "taskkill /f /im MOM.exe"
                processkillpid.StartInfo = killpid
                processkillpid.Start()
                processkillpid.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                killpid.Arguments = " /C" & "taskkill /f /im CCC.exe"
                processkillpid.StartInfo = killpid
                processkillpid.Start()
                processkillpid.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                'Delete AMD data Folders
                Dim filePath As String

                filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\ATI"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\ATI"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + "\ATI"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                'Not sure if this work on XP

                filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\ATI"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\AMD"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "AMD AVT"
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                   (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "ATI Technologies"
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)


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

                System.Threading.Thread.Sleep(1000)

                stopservice.Arguments = " /C" & "sc stop nvUpdatusService"

                processstopservice.StartInfo = stopservice
                processstopservice.Start()
                processstopservice.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                'Delete NVIDIA service
                
                stopservice.Arguments = " /C" & "sc delete nvsvc"
                
                processstopservice.StartInfo = stopservice
                processstopservice.Start()
                processstopservice.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                stopservice.Arguments = " /C" & "sc delete nvUpdatusService"

                processstopservice.StartInfo = stopservice
                processstopservice.Start()
                processstopservice.WaitForExit()

                System.Threading.Thread.Sleep(1000)
                'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
                'holding files in the NVIDIA folders sometimes.

                Dim killpid As New ProcessStartInfo
                killpid.FileName = "cmd.exe"
                killpid.Arguments = " /C" & "taskkill /f /im Lcore.exe"
                killpid.UseShellExecute = False
                killpid.CreateNoWindow = True
                killpid.RedirectStandardOutput = True

                Dim processkillpid As New Process
                processkillpid.StartInfo = killpid
                processkillpid.Start()
                processkillpid.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                killpid.Arguments = " /C" & "taskkill /f /im NvTmru.exe"
                processkillpid.StartInfo = killpid
                processkillpid.Start()
                processkillpid.WaitForExit()

                System.Threading.Thread.Sleep(1000)

                'Delete NVIDIA data Folders
                Dim filePath As String

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.LocalApplicationData) + "\NVIDIA"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ApplicationData) + "\NVIDIA"
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.CommonProgramFiles) + "\NVIDIA Corporation"
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
                My.Computer.FileSystem.DeleteDirectory _
                    (filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                'Not sure if this work on XP

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                filePath = Environment.GetFolderPath _
                    (Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA Corporation"
                My.Computer.FileSystem.DeleteDirectory(filePath, FileIO.DeleteDirectoryOption.DeleteAllContents)

                'Here we delete the Geforce experience / Nvidia update user it created.
                Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName + ",computer")
                Dim NewUser As DirectoryEntry = AD.Children.Find("UpdatusUser")
                AD.Children.Remove(NewUser)


            End If

            On Error GoTo 0
            TextBox1.Text = TextBox1.Text + "Scanning for new device..." + vbNewLine
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

            System.Threading.Thread.Sleep(1000)
            TextBox1.Text = TextBox1.Text + "Clean uninstall complete !" + vbNewLine
        End If

        Button1.Enabled = True
        Button1.Text = "Done."
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim version As String
        Dim arch As Boolean

        'Dim filepath As String
        'filepath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + " (x86)"
        'MsgBox(filepath)










        version = My.Computer.Info.OSVersion
        Me.ComboBox1.SelectedIndex = 0
        If IntPtr.Size = 8 Then

            arch = True

        ElseIf IntPtr.Size = 4 Then

            arch = False

        End If

        If version < "5.1" Then

            Label2.Text = "Unsupported OS"
            Button1.Text = "Done."

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



        If arch = True Then
            Label3.Text = "x64"
        Else
            Label3.Text = "x86"
        End If

    End Sub



End Class
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
        TextBox1.Text = TextBox1.Text + "***** Executing DEVCON Remove , Please wait(can take up to 1 minute *****) " + vbNewLine
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
            Dim searchposition As Integer
            Dim argument As String = Nothing
            If reply.IndexOf("oem", classs) < reply.Length And reply.IndexOf("oem", classs) > -1 Then
                searchposition = reply.IndexOf("oem", classs) - classs
            Else
                searchposition = reply.Length - classs
            End If

            If reply.Substring(position, classs - position).Contains(provider) Then




                Dim part As String = reply.Substring(oem, inf - oem)
                log(part + " Found")
                'Uninstall Driver from driver store  delete from (oemxx.inf)
                Dim deloem As New Diagnostics.ProcessStartInfo
                If reply.Substring(classs, searchposition).Contains("Display adapters") Or _
                  reply.Substring(classs, searchposition).Contains("Sound, video") Then
                    argument = "-f dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                    log(argument)
                Else
                    argument = "dp_delete " + Chr(34) + part + ".inf" + Chr(34)
                    log(argument)
                End If
                deloem.FileName = ".\" & Label3.Text & "\devcon.exe"
                deloem.Arguments = (argument)
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

            'System.Threading.Thread.Sleep(50)

            'Delete AMD service
            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "AMD External Events Utility" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /C" & "sc stop " & Chr(34) & "ATI External Events Utility" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            System.Threading.Thread.Sleep(50)
            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "ATI External Events Utility" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            'kill process CCC.exe / MOM.exe /Clistart.exe HydraDM/HydraDM64(if it exist)


            stopservice.Arguments = " /C" & "sc delete amdkmdag"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdagA"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdagB"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdagC"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdap"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdapA"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdapB"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete amdkmdapC"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete atikmdag"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete atikmpag"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete AtiHDAudioService"
            processstopservice.Start()
            processstopservice.WaitForExit()
            'interrogate somehow make windows check another time and remove the flag serviced for removal :D
            stopservice.Arguments = " /C" & "sc interrogate amdkmdag"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdagA"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdagB"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdagC"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdap"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdapA"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdapB"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate amdkmdapC"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate atikmdag"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate atikmpag"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate AtiHDAudioService"
            processstopservice.Start()
            processstopservice.WaitForExit()


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

            Dim appproc = Process.GetProcessesByName("MOM")
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
            Dim driverfiles(133) As String
            driverfiles(0) = "amdave32.dll"
            driverfiles(1) = "amdocl32.dll"
            driverfiles(2) = "amdh232enc32.dll"
            driverfiles(3) = "amdhcp32.dll"
            driverfiles(4) = "amdhdl32.dll"
            driverfiles(5) = "AMDhwDecoder_32.dll"
            driverfiles(6) = "amdmiracast.dll"
            driverfiles(7) = "amdpcom32.dll"
            driverfiles(8) = "ati2edxx.dll"
            driverfiles(9) = "ati2erec.dll"
            driverfiles(10) = "atiadlxx.dll"
            driverfiles(11) = "atiadlxy.dll"
            driverfiles(12) = "atiapfxx.blb"
            driverfiles(13) = "atiapfxx.exe"
            driverfiles(14) = "atibtmon.exe"
            driverfiles(15) = "aticalcl.dll"
            driverfiles(16) = "aticaldd.dll"
            driverfiles(17) = "aticalrt.dll"
            driverfiles(18) = "aticfx32.dll"
            driverfiles(19) = "aticfx32A.dll"
            driverfiles(20) = "atidxx32.dll"
            driverfiles(21) = "atidxx32A.dll"
            driverfiles(22) = "atiedu32.dll"
            driverfiles(23) = "atieslxx.exe"
            driverfiles(24) = "atiesrxx.exe"
            driverfiles(25) = "atigktxx.dll"
            driverfiles(26) = "atiglpxx.dll"
            driverfiles(27) = "atiicdxx.dat"
            driverfiles(28) = "atimpc32.dll"
            driverfiles(29) = "atimuixx.dll"
            driverfiles(30) = "atioglxx.dll"
            driverfiles(31) = "atioglxxA.dll"
            driverfiles(32) = "atio6axxB.dll"
            driverfiles(33) = "atiodcli.exe"
            driverfiles(34) = "atiode.exe"
            driverfiles(35) = "atiogl.xml"
            driverfiles(36) = "atipblag.dat"
            driverfiles(37) = "atipdlxx.dll"
            driverfiles(38) = "atipdl32.dll"
            driverfiles(39) = "atisamu32.dll"
            driverfiles(40) = "atitmm32.dll"
            driverfiles(41) = "atitmp32.dll"
            driverfiles(42) = "atiu9p32.dll"
            driverfiles(43) = "atiu9pag.dll"
            driverfiles(44) = "atiuldx6a.cap"
            driverfiles(45) = "atiuldx6a.dll"
            driverfiles(46) = "atiuldxva.cap"
            driverfiles(47) = "atiuldxva.dll"
            driverfiles(48) = "atiumd6a.cap"
            driverfiles(49) = "atiumd6a.dll"
            driverfiles(50) = "atiumd6v.dll"
            driverfiles(51) = "atiumd64.dll"
            driverfiles(52) = "atiumd64A.dll"
            driverfiles(53) = "atiumdag.dll"
            driverfiles(54) = "atiumdagA.dll"
            driverfiles(55) = "atiumdmv.dll"
            driverfiles(56) = "atiumdva.cap"
            driverfiles(57) = "atiumdva.dll"
            driverfiles(58) = "atiuxp32.dll"
            driverfiles(59) = "atiuxpag.dll"
            driverfiles(60) = "OVDecode32.dll"
            driverfiles(61) = "OVDecode.dll"
            driverfiles(62) = "OpenVideo32.dll"
            driverfiles(63) = "OpenVideo.dll"
            driverfiles(64) = "OpenCL.dll"
            driverfiles(65) = "amdave64.dll"
            driverfiles(66) = "amdocl64.dll"
            driverfiles(67) = "amdocl_ld.exe"
            driverfiles(68) = "amdocl_as.exe"
            driverfiles(69) = "amdocl_ld64.exe"
            driverfiles(70) = "amdocl_as64.exe"
            driverfiles(71) = "AMDh264Enc64.dll"
            driverfiles(72) = "amdhcp64.dll"
            driverfiles(73) = "amdhdl64.dll"
            driverfiles(74) = "AMDhwDecoder_64.dll"
            driverfiles(75) = "amdmiracast.dll"
            driverfiles(76) = "ati2edxx.dll"
            driverfiles(77) = "aticaldd64.dll"
            driverfiles(78) = "aticalrt64.dll"
            driverfiles(79) = "aticfx64.dll"
            driverfiles(80) = "aticfx64A.dll"
            driverfiles(81) = "atidemgx.dll"
            driverfiles(82) = "atidemgy.dll"
            driverfiles(83) = "atidxx64.dll"
            driverfiles(84) = "atidxx64A.dll"
            driverfiles(85) = "atiedu64.dll"
            driverfiles(86) = "atieslxx.exe"
            driverfiles(87) = "atig6txx.dll"
            driverfiles(88) = "atiicdxx.dat"
            driverfiles(89) = "atimuixx.dll"
            driverfiles(90) = "atio6axxA.dll"
            driverfiles(91) = "atio6axxB.dll"
            driverfiles(92) = "atisamu64.dll"
            driverfiles(93) = "atitmm64.dll"
            driverfiles(94) = "atitmp64.dll"
            driverfiles(95) = "atiu9p64.dll"
            driverfiles(96) = "atiu9pag.dll"
            driverfiles(97) = "atiumdag.dll"
            driverfiles(98) = "atiumdva.cap"
            driverfiles(99) = "atiuxpag.dll"
            driverfiles(100) = "ativvsvl.dat"
            driverfiles(101) = "ativce02.dat"
            driverfiles(102) = "ativvaxy_cik_nd.dat"
            driverfiles(103) = "ativvaxy_cik.dat"
            driverfiles(104) = "clinfo.exe"
            driverfiles(105) = "oemdspif.dll"
            driverfiles(106) = "SlotMaximizerAg.dll"
            driverfiles(107) = "SlotMaximizerBe.dll"
            driverfiles(108) = "OVDecode64.dll"
            driverfiles(109) = "OVDecode.dll"
            driverfiles(110) = "OpenVideo64.dll"
            driverfiles(111) = "atikmdag.sys"
            driverfiles(112) = "atiogl.xml"
            driverfiles(113) = "ativpsrm.bin"
            driverfiles(114) = "atig6pxx.dll"
            driverfiles(115) = "atio6axx.dll"
            driverfiles(116) = "aticalcl64.dll"
            driverfiles(117) = "atimpc64.dll"
            driverfiles(118) = "atieclxx.exe"
            driverfiles(119) = "atiuxp64.dll"
            driverfiles(120) = "amdpcom64.dll"
            driverfiles(121) = "atikmpag.sys"
            driverfiles(122) = "ativpsrm.dll"
            driverfiles(123) = "AtihdW86.sys"
            driverfiles(124) = "amdocl.dll"
            driverfiles(125) = "amdocl_ld32.exe"
            driverfiles(126) = "amdocl_as32.exe"
            driverfiles(127) = "ativvsva.dat"
            driverfiles(128) = "ATIODCLI.exe"
            driverfiles(129) = "ATIODE.exe"
            driverfiles(130) = "AMDh264Enc32.dll"
            driverfiles(131) = "coinst_"
            driverfiles(132) = "atiilhag"
            For i As Integer = 0 To 130

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


            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)

            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        If child.Contains("amdwdst") Then
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


            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" + child, False)
                        If subregkey IsNot Nothing Then
                            If subregkey.GetValue("") IsNot Nothing Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If wantedvalue IsNot Nothing Then
                                    If wantedvalue.Contains("AMDWDST") Then

                                        regkey.DeleteSubKeyTree(child)
                                    End If
                                End If
                            End If
                        End If
                    End If

                Next
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
                For i As Integer = 0 To 132
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles")
                    If regkey IsNot Nothing Then

                        For Each child In regkey.GetSubKeyNames()
                            If child IsNot Nothing Then
                                If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree _
                                            ("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child)
                                    Catch ex As Exception
                                        log(ex.Message)
                                    End Try
                                End If
                            End If
                        Next
                    End If
                Next
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

                'removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                'removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies\CBT /owner=Administrators"
                'removehdmidriver.UseShellExecute = False
                'removehdmidriver.CreateNoWindow = True
                'prochdmi.StartInfo = removehdmidriver
                'prochdmi.Start()
                'prochdmi.WaitForExit()
                'System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                'removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies\CBT /grant=" & UserAc & "=f"
                'prochdmi.Start()
                'prochdmi.WaitForExit()
                'System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                'Try
                '    My.Computer.Registry.LocalMachine.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies\CBT")
                'Catch ex As Exception
                '    log(ex.Message)
                'End Try
            Else
                Dim reginfos As RegistryKey = Nothing
                Dim FolderAcl As New RegistrySecurity
                'setting permission to registry
                Try
                    removehdmidriver.FileName = ".\" & Label3.Text & "\subinacl.exe"
                    removehdmidriver.Arguments = "/subkeyreg HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles /grant=" & UserAc & "=f"
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    For i As Integer = 0 To 132
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                        If regkey IsNot Nothing Then
                            For Each child In regkey.GetValueNames()
                                If child IsNot Nothing Then
                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message)
                                        End Try
                                    End If
                                End If
                            Next
                        End If
                    Next
                    'setting back the registry permission to normal.
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
                                        If wantedvalue.Contains("ATI\CIM\") Or _
                                            wantedvalue.Contains("ATI.ACE\") Then

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


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetValueNames()
                If child IsNot Nothing Then
                    If child.Contains("ATI\CIM\") Or _
                    child.Contains("SteadyVideo") Or _
                    child.Contains("ATI Technologies\Multimedia") Or _
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


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetValueNames()
                If child IsNot Nothing Then
                    If child.Contains("ATI\CIM\") Or child.Contains("AMD AVT") Or _
                    child.Contains("ATI\CIM\") Or _
                    child.Contains("AMP APP\") Or _
                    child.Contains("AMD\SteadyVideo\") Or _
                    child.Contains("ATI.ACE\") Or _
                    child.Contains("HydraVision\") Or _
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



        regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
  ("Interface", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If child IsNot Nothing Then
                    subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
        ("Interface\" & child, False)

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

            System.Threading.Thread.Sleep(50)

            stopservice.Arguments = " /C" & "sc stop nvUpdatusService"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /C" & "sc stop " & Chr(34) & "Stereo Service" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /C" & "sc stop " & Chr(34) & "NvStreamSvc" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            System.Threading.Thread.Sleep(50)

            'Delete NVIDIA service

            stopservice.Arguments = " /C" & "sc delete nvsvc"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()


            stopservice.Arguments = " /C" & "sc delete nvUpdatusService"

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()



            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "Stereo Service" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /C" & "sc delete " & Chr(34) & "NvStreamSvc" & Chr(34)

            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()

            stopservice.Arguments = " /C" & "sc delete nvlddmkm"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete NVHDA"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc delete nvvad_WaveExtensible"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate nvlddmkm"
            processstopservice.StartInfo = stopservice
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate NVHDA"
            processstopservice.Start()
            processstopservice.WaitForExit()
            stopservice.Arguments = " /C" & "sc interrogate nvvad_WaveExtensible"
            processstopservice.Start()
            processstopservice.WaitForExit()


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

            Dim driverfiles(79) As String
            driverfiles(0) = "nvapi.dll"
            driverfiles(1) = "nvapi64.dll"
            driverfiles(2) = "nvcompiler.dll"
            driverfiles(3) = "nvcompiler32.dll"
            driverfiles(4) = "nvcuda.dll"
            driverfiles(5) = "nvcuda32.dll"
            driverfiles(6) = "nvcuvenc.dll"
            driverfiles(7) = "nvcuvenc64.dll"
            driverfiles(8) = "nvcuvid.dll"
            driverfiles(9) = "nvcuvid32.dll"
            driverfiles(10) = "nvd3d9wrap.dll"
            driverfiles(11) = "nvd3d9wrapx.dll"
            driverfiles(12) = "nvd3dum.dll"
            driverfiles(13) = "nvd3dumx.dll"
            driverfiles(14) = "nvdet.dll"
            driverfiles(15) = "nvdetx.dll"
            driverfiles(16) = "nvdispco64.dll"
            driverfiles(17) = "nvdispgenco64.dll"
            driverfiles(18) = "nvdrsdb.bin"
            driverfiles(19) = "nvdxgiwrap.dll"
            driverfiles(20) = "nvdxgiwrapx.dll"
            driverfiles(21) = "nvEncodeAPI.dll"
            driverfiles(22) = "nvEncodeAPI64.dll"
            driverfiles(23) = "nvFBC.dll"
            driverfiles(24) = "nvFBC64.dll"
            driverfiles(25) = "nvIFR.dll"
            driverfiles(26) = "nvIFR64.dll"
            driverfiles(27) = "nvinfo.pb"
            driverfiles(28) = "nvinit.dll"
            driverfiles(29) = "nvinitx.dll"
            driverfiles(30) = "nvkflt.sys"
            driverfiles(31) = "nvlddmkm.sys"
            driverfiles(32) = "nvml.dll"
            driverfiles(33) = "nvoglshim32.dll"
            driverfiles(34) = "nvoglshim64.dll"
            driverfiles(35) = "nvoglv32.dll"
            driverfiles(36) = "nvoglv64.dll"
            driverfiles(37) = "nvopencl.dll"
            driverfiles(38) = "nvopencl32.dll"
            driverfiles(39) = "nvpciflt.sys"
            driverfiles(40) = "nvumdshim.dll"
            driverfiles(41) = "nvumdshimx.dll"
            driverfiles(42) = "nvwgf2um.dll"
            driverfiles(43) = "nvwgf2umx.dll"
            driverfiles(44) = "opencl.dll"
            driverfiles(45) = "opencl64.dll"
            driverfiles(46) = "nvaudcap32v.dll"
            driverfiles(47) = "nvaudcap64v.dll"
            driverfiles(48) = "nvvad32v.sys"
            driverfiles(49) = "nvvad64v.sys"
            driverfiles(50) = "nvstusb32.sys"
            driverfiles(51) = "nvstusb64.sys"
            driverfiles(52) = "nvhda32.sys"
            driverfiles(53) = "nvhda64.sys"
            driverfiles(54) = "nvhda32v.sys"
            driverfiles(55) = "nvhda64v.sys"
            driverfiles(56) = "nvhdap32.dll"
            driverfiles(57) = "nvhdap64.dll"
            driverfiles(58) = "nvcpl.dll"
            driverfiles(59) = "nvmctray.dll"
            driverfiles(60) = "nvsvc64.dll"
            driverfiles(61) = "nvsvcr.dll"
            driverfiles(62) = "nvvsvc.exe"
            driverfiles(63) = "nvshext.dll"
            driverfiles(64) = "dbInstaller.exe"
            driverfiles(65) = "nvidia-smi.exe"
            driverfiles(66) = "nvidia-smi.1.pdf"
            driverfiles(67) = "mcu.exe"
            driverfiles(68) = "license.txt"
            driverfiles(69) = "nvdebugdump.exe"
            driverfiles(70) = "openCL.dll"
            driverfiles(71) = "openCL64.dll"
            driverfiles(72) = "nvStreaming.exe"
            driverfiles(73) = "nv_disp.inf"
            driverfiles(74) = "nv_dispi.inf"
            driverfiles(75) = "nvdisp"
            driverfiles(76) = "nvhda"
            driverfiles(77) = "detoured.dll"
            driverfiles(78) = "nvcplsetupeng.exe"
            For i As Integer = 0 To 76

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


            regkey = My.Computer.Registry.ClassesRoot
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        Dim childl As String = child.ToLower
                        If childl.Contains("comupdatus") Or childl.Contains("nv3d") Or _
                            childl.Contains("nvui") Or childl.Contains("nvvsvc") Or childl.Contains("nvxd") Or _
                           childl.Contains("gamesconfigserver") Or _
                          childl.Contains("nvidia.installer") Or childl.Contains("displayserver.") Or childl.Contains("workstationserver.") Or _
                          childl.Contains("video_tvserver.") Or childl.Contains("stereovisionserver.") Or childl.Contains("mobileserver.") Or _
                          childl.Contains("nvcpl") Then

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
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try

                                        Else
                                            If child.Contains("gamesconfigserver") Then   'Physx related
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

            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If child IsNot Nothing Then
                        Dim childl As String = child.ToLower

                        If childl.Contains("comupdatus") Or childl.Contains("nv3d") Or _
                            childl.Contains("nvui") Or childl.Contains("nvvsvc") Or childl.Contains("nvxd") Or _
                           childl.Contains("gamesconfigserver") Or _
                          childl.Contains("nvidia.installer") Or childl.Contains("displayserver") Then

                            subregkey = regkey.OpenSubKey(child, True)
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
                                                regkey.DeleteSubKeyTree(child)
                                            Catch ex As Exception
                                            End Try

                                        Else
                                            If child.Contains("gamesconfigserver") Then
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
                For i As Integer = 0 To 78
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles")
                    If regkey IsNot Nothing Then

                        For Each child In regkey.GetSubKeyNames()
                            If child IsNot Nothing Then
                                If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                    Try
                                        My.Computer.Registry.LocalMachine.DeleteSubKeyTree _
                                            ("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles\" & child)
                                    Catch ex As Exception
                                        log(ex.Message & "pnplockdownfiles")
                                    End Try
                                End If
                            End If
                        Next
                    End If
                Next
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
                    prochdmi.StartInfo = removehdmidriver
                    prochdmi.Start()
                    prochdmi.WaitForExit()
                    System.Threading.Thread.Sleep(25)  '25 millisecond stall (0.025 Seconds)
                    For i As Integer = 0 To 78
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                        If regkey IsNot Nothing Then
                            For Each child In regkey.GetValueNames()
                                If child IsNot Nothing Then

                                    If child.ToLower.Contains(driverfiles(i).ToLower) Then

                                        Try
                                            regkey.DeleteValue(child)
                                        Catch ex As Exception
                                            log(ex.Message & "pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        End If
                    Next
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
    Private Sub rescan(ByVal e As String)
        Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** Scanning for new device... *****" + vbNewLine)
        Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
        Invoke(Sub() TextBox1.ScrollToCaret())
        log("Scanning for new device...")
        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = ".\" & Label3.Text & "\devcon.exe"
        scan.Arguments = "rescan"
        scan.UseShellExecute = False
        scan.CreateNoWindow = True
        scan.RedirectStandardOutput = True

        If reboot Then
            log("Restarting Computer ")
            System.Diagnostics.Process.Start("shutdown", "/r /t 0 /f")
        End If
        If shutdown Then
            System.Diagnostics.Process.Start("shutdown", "/s /t 0 /f")
        End If
        If reboot = False And shutdown = False Then
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
            win8higher = True
        End If

        If version >= "6.3" Then
            Label2.Text = "Windows 8.1"
            win8higher = True
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
                If Not System.IO.File.Exists(myExe) Then
                    System.IO.File.WriteAllBytes(myExe, My.Resources.devcon64)
                End If
                myExe = Application.StartupPath & "\x64\subinacl.exe"
                If Not System.IO.File.Exists(myExe) Then
                    System.IO.File.WriteAllBytes(myExe, My.Resources.subinacl64)
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
                myExe = Application.StartupPath & "\x86\subinacl.exe"
                If Not System.IO.File.Exists(myExe) Then
                    System.IO.File.WriteAllBytes(myExe, My.Resources.subinacl32)
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
            'For i As Integer = 0 To 1 'loop 2 time to check if there is a remaining videocard.
            'find the PCI.... of the videocards.
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
                card1 = reply.IndexOf("PCI")
            Catch ex As Exception

            End Try
            While card1 > -1

                position2 = reply.IndexOf(":", card1)
                vendid = reply.Substring(card1, position2 - card1).Trim

                If vendid.Contains(vendidexpected) Then
                    log("-" & vendid & "- GPU id found")
                    'Driver uninstallation procedure Display & Sound/HDMI used by some GPU
                    removedisplaydriver.FileName = ".\" & Label3.Text & "\devcon.exe"
                    removedisplaydriver.Arguments = "disable =display " & Chr(34) & "@" & vendid & Chr(34)
                    removedisplaydriver.UseShellExecute = False
                    removedisplaydriver.CreateNoWindow = True
                    removedisplaydriver.RedirectStandardOutput = True
                    proc.StartInfo = removedisplaydriver
                    Try
                        proc.Start()

                    Catch ex As Exception
                        log(ex.Message)
                        MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                        Button1.Enabled = True
                        Button2.Enabled = True
                        Button3.Enabled = True
                        Exit Sub
                    End Try
                    reply2 = proc.StandardOutput.ReadToEnd
                    Application.DoEvents()
                    System.Threading.Thread.Sleep(400)
                    proc.WaitForExit()
                    log(reply2)


                End If

                card1 = reply.IndexOf("PCI", card1 + 1)
            End While


            Try
                card1 = reply.IndexOf("PCI")
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
                    Try
                        proc.Start()
                    Catch ex As Exception
                    End Try
                    reply2 = proc.StandardOutput.ReadToEnd
                    proc.WaitForExit()
                    log(reply2)

                End If
                card1 = reply.IndexOf("PCI", card1 + 1)


            End While

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
            Try
                card1 = reply.IndexOf("HDAUDIO")
            Catch ex As Exception

            End Try

            While card1 > -1

                position2 = reply.IndexOf(":", card1)
                vendid = reply.Substring(card1, position2 - card1).Trim
                If vendid.Contains(vendidexpected) Then
                    log("-" & vendid & "- Audio device found")

                    removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
                    removehdmidriver.Arguments = "disable =MEDIA " & Chr(34) & "@" & vendid & Chr(34)
                    removehdmidriver.UseShellExecute = False
                    removehdmidriver.CreateNoWindow = True
                    removehdmidriver.RedirectStandardOutput = True
                    prochdmi.StartInfo = removehdmidriver
                    Try
                        prochdmi.Start()
                    Catch ex As Exception

                        log(ex.Message)
                        MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                        Button1.Enabled = True
                        Button2.Enabled = True
                        Button3.Enabled = True
                    End Try
                    reply2 = prochdmi.StandardOutput.ReadToEnd
                    prochdmi.WaitForExit()
                    log(reply2)

                    ' System.Threading.Thread.Sleep(50)
                End If
                card1 = reply.IndexOf("HDAUDIO", card1 + 1)
                ' System.Threading.Thread.Sleep(50) '100 ms sleep between removal of media.
            End While

            'System.Threading.Thread.Sleep(200) '200 ms sleep between removal of media.
            Try
                card1 = reply.IndexOf("HDAUDIO")
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
                    prochdmi.StartInfo = removehdmidriver
                    Try
                        prochdmi.Start()
                    Catch ex As Exception

                        log(ex.Message)
                        MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                        Button1.Enabled = True
                        Button2.Enabled = True
                        Button3.Enabled = True
                    End Try
                    reply2 = prochdmi.StandardOutput.ReadToEnd
                    prochdmi.WaitForExit()
                    log(reply2)

                End If
                card1 = reply.IndexOf("HDAUDIO", card1 + 1)
                ' System.Threading.Thread.Sleep(50) '100 ms sleep between removal of media.
            End While

            'creation dun process fantome pour le wait on exit.

            ' System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)
            'Next

            log("DEVCON Remove Display Complete")

            '  System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)
            'ugly code to remove the new NVIDIA Virtual Audio Device (Wave Extensible) (WDM) and 3d vision drivers


            If DirectCast(e.Argument, String) = "NVIDIA" Then

                removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
                removehdmidriver.Arguments = "disable =MEDIA " & Chr(34) & "usb\vid_0955&PID_700*" & Chr(34)
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                removehdmidriver.RedirectStandardOutput = True
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
                ' System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)
                removehdmidriver.Arguments = "disable =MEDIA " & Chr(34) & "usb\vid_0955&PID_0007" & Chr(34)
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
                removehdmidriver.Arguments = "disable =MEDIA " & Chr(34) & "usb\vid_0955&PID_9000" & Chr(34)
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
                System.Threading.Thread.Sleep(100)  '100 millisecond stall
                removehdmidriver.FileName = ".\" & Label3.Text & "\devcon.exe"
                removehdmidriver.Arguments = "remove =MEDIA " & Chr(34) & "usb\vid_0955&PID_700*" & Chr(34)
                removehdmidriver.UseShellExecute = False
                removehdmidriver.CreateNoWindow = True
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
                removehdmidriver.Arguments = "remove =MEDIA " & Chr(34) & "usb\vid_0955&PID_0007" & Chr(34)
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
                removehdmidriver.Arguments = "remove =MEDIA " & Chr(34) & "usb\vid_0955&PID_9000" & Chr(34)
                prochdmi.StartInfo = removehdmidriver
                prochdmi.Start()
                reply = prochdmi.StandardOutput.ReadToEnd
                prochdmi.WaitForExit()
                log(reply)
            End If
            ' System.Threading.Thread.Sleep(50)  '50 millisecond stall (0.05 Seconds)

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
                removedisplaydriver.Arguments = "disable =monitor " & Chr(34) & "@" & vendid & Chr(34)
                removedisplaydriver.UseShellExecute = False
                removedisplaydriver.CreateNoWindow = True
                removedisplaydriver.RedirectStandardOutput = True
                proc.StartInfo = removedisplaydriver
                Try
                    proc.Start()

                Catch ex As Exception
                    log(ex.Message)
                    MsgBox("Cannot find DEVCON in " & Label3.Text & " folder", MsgBoxStyle.Critical)
                    Button1.Enabled = True
                    Button2.Enabled = True
                    Button3.Enabled = True
                    Exit Sub
                End Try
                reply2 = proc.StandardOutput.ReadToEnd
                proc.WaitForExit()
                log(reply2)

                ' System.Threading.Thread.Sleep(20)
                removedisplaydriver.Arguments = "remove =monitor " & Chr(34) & "@" & vendid & Chr(34)
                Try
                    proc.Start()
                Catch ex As Exception
                End Try
                reply2 = proc.StandardOutput.ReadToEnd
                proc.WaitForExit()
                log(reply2)

                card1 = reply.IndexOf("DISPLAY\", card1 + 1)

            End While
            Invoke(Sub() TextBox1.Text = TextBox1.Text + "***** DEVCON Remove complete *****" + vbNewLine)
            Invoke(Sub() TextBox1.Select(TextBox1.Text.Length, 0))
            Invoke(Sub() TextBox1.ScrollToCaret())
            clean(DirectCast(e.Argument, String))
            cleandriverstore(DirectCast(e.Argument, String))
            rescan(DirectCast(e.Argument, String))
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
                scan.RedirectStandardOutput = True
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
End Class
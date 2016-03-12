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
Option Strict On

Imports System.DirectoryServices
Imports Microsoft.Win32
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading
Imports System.Security.Principal
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Text




Public Class frmMain
    Dim arg As String
    Dim trd As Thread
    Dim backgroundworkcomplete As Boolean = True
    Dim arguments As String() = Environment.GetCommandLineArgs()
    Dim silent As Boolean = False
    Dim argcleanamd As Boolean = False
    Dim argcleanintel As Boolean = False
    Dim argcleannvidia As Boolean = False
    Dim nbclean As Integer = 0
    Dim restart As Boolean = False
    Dim MyIdentity As WindowsIdentity = WindowsIdentity.GetCurrent()
    Dim checkvariables As New checkvariables
    Dim identity As WindowsIdentity = WindowsIdentity.GetCurrent()
    Dim principal As WindowsPrincipal = New WindowsPrincipal(identity)
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
    Public win10 As Boolean = False
    Public winxp As Boolean = False
    Dim stopme As Boolean = False
    Public Shared removemonitor As Boolean
    Public Shared removedxcache As Boolean
    Public Shared removecamd As Boolean
    Public Shared removecnvidia As Boolean
    Public Shared removephysx As Boolean
    Public Shared removeamdaudiobus As Boolean
    Public Shared remove3dtvplay As Boolean
    Public Shared removeamdkmpfd As Boolean
    Public Shared safemodemb As Boolean
    Public Shared roamingcfg As Boolean
    Public Shared donotcheckupdatestartup As Boolean
    Public Shared trysystemrestore As Boolean
    Public Shared removegfe As Boolean

    Dim f As New frmOptions
	Dim locations As String = Application.StartupPath & "\DDU Logs\" & DateAndTime.Now.Year & " _" & DateAndTime.Now.Month & "_" & DateAndTime.Now.Day _
							  & "_" & DateAndTime.Now.Hour & "_" & DateAndTime.Now.Minute & "_" & DateAndTime.Now.Second & "_DDULog.log"
    Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive").ToLower
    Dim windir As String = System.Environment.GetEnvironmentVariable("windir").ToLower
    Dim userpth As String = CStr(My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory")) & "\"
    Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
    Dim checkupdatethread As Thread = Nothing
    Public updates As Integer = Nothing
    Dim reply As String = Nothing
    Dim reply2 As String = Nothing
    Dim version As String = Nothing
    Dim card1 As Integer = Nothing
    Dim position2 As Integer = Nothing
    Dim currentdriverversion As String = Nothing
    Dim safemode As Boolean = False
    Dim myExe As String
    Dim checkupdates As New genericfunction
    Public Shared settings As New genericfunction
    Dim CleanupEngine As New CleanupEngine
    Dim enduro As Boolean = False
    Public Shared preventclose As Boolean = False
    Public Shared combobox1value As String = Nothing
    Public Shared combobox2value As String = Nothing
    Dim buttontext As String()
    Dim closeapp As Boolean = False
    Public ddudrfolder As String

    Public donotremoveamdhdaudiobusfiles As Boolean = True
    Public msgboxmessage As String()
    Public UpdateTextMethodmessage As String()
    Public picturebox2originalx As Integer
    Public picturebox2originaly As Integer

    Public Function getremovephysx() As Boolean
        Return removephysx
    End Function

	Private Sub Checkupdates2()
		If Me.InvokeRequired Then
			Me.Invoke(New MethodInvoker(AddressOf Checkupdates2))
		Else
			Label11.Text = Language.GetTranslation(Me.Name, "Label11", "Text")
			Dim updates As Integer = checkupdates.checkupdates

			If updates = 1 Then
				Label11.Text = Language.GetTranslation(Me.Name, "Label11", "Text2")

			ElseIf updates = 2 Then
				Label11.Text = Language.GetTranslation(Me.Name, "Label11", "Text3")

				If Not MyIdentity.IsSystem Then	 'we dont want to open a webpage when the app is under "System" user.
					Select Case MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text1"), Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information)
						Case Windows.Forms.DialogResult.Yes
							process.Start("http://www.wagnardmobile.com")
							closeapp = True
							closeddu()
							Exit Sub
						Case Windows.Forms.DialogResult.No
							MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text2"), Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information)
						Case Windows.Forms.DialogResult.Cancel
							closeapp = True
							closeddu()
					End Select

				End If

			ElseIf updates = 3 Then
				Label11.Text = Language.GetTranslation(Me.Name, "Label11", "Text4")
			End If
		End If
	End Sub

    Private Sub cleandriverstore()
        Dim catalog As String = ""

        UpdateTextMethod("-Executing Driver Store cleanUP(finding OEM step)...")
        log("Executing Driver Store cleanUP(Find OEM)...")
        'Check the driver from the driver store  ( oemxx.inf)

        Dim deloem As New Diagnostics.ProcessStartInfo
        deloem.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        Dim proc3 As New Diagnostics.Process
        processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        processinfo.Arguments = "dp_enum"

        UpdateTextMethod(UpdateTextMethodmessagefn(0))

        Try
            For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")

                If Not checkvariables.isnullorwhitespace(infs) Then
                    For Each child As String In IO.File.ReadAllLines(infs)
                        If Not checkvariables.isnullorwhitespace(child) Then

                            child = child.Replace(" ", "").Replace(vbTab, "")

                            If Not checkvariables.isnullorwhitespace(child) AndAlso child.ToLower.StartsWith("provider=") Then
                                If child.EndsWith("%") Then
                                    For Each providers As String In IO.File.ReadAllLines(infs)
                                        If Not checkvariables.isnullorwhitespace(providers) Then



                                            providers = providers.Replace(" ", "").Replace(vbTab, "")
                                            If Not checkvariables.isnullorwhitespace(providers) AndAlso providers.ToLower.StartsWith(child.ToLower.Replace("provider=", "").Replace("%", "") + "=") AndAlso
                                               Not providers.Contains("%") Then
                                                If providers.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.Contains(provider.ToLower) Or
                                                   providers.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.StartsWith("atitech") Or
                                                   providers.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.Contains("amd") Then

                                                    deloem.Arguments = "dp_delete " + Chr(34) + infs.Substring(infs.IndexOf("oem")) + Chr(34)
                                                    Try
                                                        For Each child3 As String In IO.File.ReadAllLines(infs)
                                                            If Not checkvariables.isnullorwhitespace(child3) Then
                                                                If child3.ToLower.Trim.Replace(" ", "").Contains("class=display") Or
                                                                    child3.ToLower.Trim.Replace(" ", "").Contains("class=media") Then
                                                                    deloem.Arguments = "-f dp_delete " + Chr(34) + infs.Substring(infs.IndexOf("oem")) + Chr(34)
                                                                    Exit For
                                                                End If
                                                            End If
                                                        Next
                                                    Catch ex As Exception
                                                    End Try

                                                    'before removing the oem we try to get the original inf name (win8+)
                                                    If win8higher Then
                                                        Try
                                                            catalog = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles\" & infs.Substring(infs.IndexOf("oem"))).GetValue("Active").ToString
                                                            catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
                                                        Catch ex As Exception
                                                            catalog = ""
                                                        End Try
                                                    End If
                                                    'Uninstall Driver from driver store  delete from (oemxx.inf)

                                                    log(deloem.Arguments)
                                                    deloem.UseShellExecute = False
                                                    deloem.CreateNoWindow = True
                                                    deloem.RedirectStandardOutput = True
                                                    'creation dun process fantome pour le wait on exit.

                                                    proc3.StartInfo = deloem
                                                    proc3.Start()
                                                    reply2 = proc3.StandardOutput.ReadToEnd
                                                    'proc3.WaitForExit()
                                                    proc3.StandardOutput.Close()
                                                    proc3.Close()
                                                    UpdateTextMethod(reply2)
                                                    log(reply2)
                                                    'check if the oem was remove to process to the pnplockdownfile if necessary
                                                    If win8higher AndAlso (Not System.IO.File.Exists(Environment.GetEnvironmentVariable("windir") & "\inf\" + infs.Substring(infs.IndexOf("oem")))) AndAlso (Not checkvariables.isnullorwhitespace(catalog)) Then
                                                        CleanupEngine.prePnplockdownfiles(catalog)
                                                    End If
                                                    Exit For
                                                End If
                                            End If
                                        End If
                                    Next

                                Else

                                    If child.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.Contains(provider.ToLower) Or
                                                   child.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.StartsWith("atitech") Or
                                                   child.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "").ToLower.Contains("amd") Then
                                        deloem.Arguments = "dp_delete " + Chr(34) + infs.Substring(infs.IndexOf("oem")) + Chr(34)
                                        Try
                                            For Each child3 As String In IO.File.ReadAllLines(infs)
                                                If Not checkvariables.isnullorwhitespace(child3) Then
                                                    If child3.ToLower.Trim.Replace(" ", "").Contains("class=display") Or
                                                        child3.ToLower.Trim.Replace(" ", "").Contains("class=media") Then
                                                        deloem.Arguments = "-f dp_delete " + Chr(34) + infs.Substring(infs.IndexOf("oem")) + Chr(34)
                                                        Exit For
                                                    End If
                                                End If
                                            Next
                                        Catch ex As Exception
                                        End Try
                                        'before removing the oem we try to get the original inf name (win8+)
                                        If win8higher Then
                                            Try
                                                catalog = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles\" & infs.Substring(infs.IndexOf("oem"))).GetValue("Active").ToString
                                                catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
                                            Catch ex As Exception
                                                catalog = ""
                                            End Try
                                        End If
                                        'Uninstall Driver from driver store  delete from (oemxx.inf)
                                        log(deloem.Arguments)
                                        deloem.UseShellExecute = False
                                        deloem.CreateNoWindow = True
                                        deloem.RedirectStandardOutput = True
                                        'creation dun process fantome pour le wait on exit.

                                        proc3.StartInfo = deloem
                                        proc3.Start()
                                        reply2 = proc3.StandardOutput.ReadToEnd
                                        'proc3.WaitForExit()
                                        proc3.StandardOutput.Close()
                                        proc3.Close()

                                        UpdateTextMethod(reply2)
                                        log(reply2)
                                        If win8higher AndAlso (Not System.IO.File.Exists(Environment.GetEnvironmentVariable("windir") & "\inf\" + infs.Substring(infs.IndexOf("oem")))) Then
                                            CleanupEngine.prePnplockdownfiles(catalog)
                                        End If
                                        Exit For
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try
        UpdateTextMethod("-Driver Store cleanUP complete.")

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
        processkillpid.Close()

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

        appproc = process.GetProcessesByName("Cnext")
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

        appproc = Process.GetProcessesByName("ThumbnailExtractionHost")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        appproc = Process.GetProcessesByName("jusched")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

        System.Threading.Thread.Sleep(10)
    End Sub

    Private Sub cleanamdfolders()
        Dim filePath As String = Nothing
        'Delete AMD data Folders
        UpdateTextMethod(UpdateTextMethodmessagefn(1))

        log("Cleaning Directory (Please Wait...)")


        If removecamd Then
            filePath = sysdrv + "\AMD"

            Try
                deletedirectory(filePath)
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
            deletefile(filePath + "\atiogl.xml")
        Catch ex As Exception
        End Try

        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            deletefile(filePath + "\ativpsrm.bin")
        Catch ex As Exception
        End Try


        filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + "\ATI Technologies"
        If Directory.Exists(filePath) Then

            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ati.ace") Or
                       child.ToLower.Contains("ati catalyst control center") Or
                       child.ToLower.Contains("application profiles") Or
                       child.ToLower.EndsWith("\px") Or
                       child.ToLower.Contains("hydravision") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If


        filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + "\ATI"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("cim") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If


        filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\Common Files" + "\ATI Technologies"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("multimedia") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                    'on success, do this

                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If

        filePath = Environment.GetFolderPath _
   (Environment.SpecialFolder.ProgramFiles) + "\AMD APP"
        If Directory.Exists(filePath) Then
            Try
                deletedirectory(filePath)
            Catch ex As Exception
                log(ex.Message + "AMD APP")
                TestDelete(filePath)
            End Try
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If

        If IntPtr.Size = 8 Then

            filePath = Environment.GetFolderPath _
                       (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD AVT"
            If Directory.Exists(filePath) Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\ATI Technologies"
            If Directory.Exists(filePath) Then
                Try
                    For Each child As String In Directory.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("ati.ace") Or
                                child.ToLower.Contains("ati catalyst control center") Or
                                child.ToLower.Contains("application profiles") Or
                                child.ToLower.EndsWith("\px") Or
                                child.ToLower.Contains("hydravision") Then
                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                                If Not Directory.Exists(child) Then
                                    CleanupEngine.shareddlls(child)
                                End If
                            End If
                        End If
                    Next
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = System.Environment.SystemDirectory
            Dim files() As String = IO.Directory.GetFiles(filePath + "\", "coinst_*.*")
            For i As Integer = 0 To files.Length - 1
                If Not checkvariables.isnullorwhitespace(files(i)) Then
                    Try
                        deletefile(files(i))
                    Catch ex As Exception
                    End Try
                End If
            Next

            filePath = Environment.GetFolderPath _
               (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD APP"
            If Directory.Exists(filePath) Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message + "AMD APP")
                    TestDelete(filePath)
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideo"
            If Directory.Exists(filePath) Then
                Try
                    TestDelete(filePath)
                Catch ex As Exception
                    log(ex.Message + "SteadyVideo testdelete")
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoFirefox"
            If Directory.Exists(filePath) Then
                Try
                    TestDelete(filePath)
                Catch ex As Exception
                    log(ex.Message + "SteadyVideo testdelete")
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD\SteadyVideoChrome"
            If Directory.Exists(filePath) Then
                Try
                    TestDelete(filePath)
                Catch ex As Exception
                    log(ex.Message + "SteadyVideo testdelete")
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\Common Files" + "\ATI Technologies"
            If Directory.Exists(filePath) Then
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("multimedia") Then
                            Try
                                deletedirectory(child)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                            If Not Directory.Exists(child) Then
                                CleanupEngine.shareddlls(child)
                            End If
                        End If
                    End If
                Next
                Try
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If
        End If


        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Catalyst Control Center"
        If Directory.Exists(filePath) Then
            Try
                deletedirectory(filePath)
            Catch ex As Exception
                TestDelete(filePath)
            End Try
        End If


        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\AMD Catalyst Control Center"
        If Directory.Exists(filePath) Then
            Try
                deletedirectory(filePath)
            Catch ex As Exception
                TestDelete(filePath)
            End Try
        End If

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\ATI"
        If Directory.Exists(filePath) Then
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ace") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\AMD"
        If Directory.Exists(filePath) Then
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("kdb") Or _
                       child.ToLower.Contains("fuel") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If

        For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(userpth))
            filePath = filepaths + "\AppData\Roaming\ATI"
            If winxp Then
                filePath = filepaths + "\Application Data\ATI"
            End If
            If Directory.Exists(filePath) Then
                Try
                    For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("ace") Then
                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                                If Not Directory.Exists(child) Then
                                    CleanupEngine.shareddlls(child)
                                End If
                            End If
                        End If
                    Next
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                    log("Possible permission issue detected on : " + filePath)
                End Try
            End If

            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If


            filePath = filepaths + "\AppData\Local\ATI"
            If winxp Then
                filePath = filepaths + "\Local Settings\Application Data\ATI"
            End If
            If Directory.Exists(filePath) Then
                Try
                    For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("ace") Then
                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                                If Not Directory.Exists(child) Then
                                    CleanupEngine.shareddlls(child)
                                End If
                            End If
                        End If
                    Next
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                    log("Possible permission issue detected on : " + filePath)
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

            filePath = filepaths + "\AppData\Local\AMD"
            If winxp Then
                filePath = filepaths + "\Local Settings\Application Data\AMD"
            End If
            If Directory.Exists(filePath) Then
                Try
                    For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If child.ToLower.Contains("cn") Or
                                child.ToLower.Contains("fuel") Or _
                                removedxcache AndAlso child.ToLower.Contains("dxcache") Or _
                                removedxcache AndAlso child.ToLower.Contains("glcache") Then
                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                                If Not Directory.Exists(child) Then
                                    CleanupEngine.shareddlls(child)
                                End If
                            End If
                        End If
                    Next
                    If Directory.GetDirectories(filePath).Length = 0 Then
                        Try
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                    log("Possible permission issue detected on : " + filePath)
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If

        Next

        'starting with AMD  14.12 Omega driver folders

        filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + "\AMD"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("amdkmpfd") Or
                        child.ToLower.Contains("cnext") Or
                        child.ToLower.Contains("steadyvideo") Or
                        child.ToLower.Contains("920dec42-4ca5-4d1d-9487-67be645cddfc") Or
                       child.ToLower.Contains("cim") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            Try
                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        deletedirectory(filePath)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                Else
                    For Each data As String In Directory.GetDirectories(filePath)
                        log("Remaining folders found " + " : " + data)
                    Next

                End If
            Catch ex As Exception
            End Try
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If

        filePath = Environment.GetFolderPath _
    (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AMD"
        If Directory.Exists(filePath) Then

            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ati.ace") Or _
                       child.ToLower.Contains("cnext") Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If
        'Cleaning the CCC assemblies.


        filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_64"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
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
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
        End If

        filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\GAC_MSIL"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
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
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
        End If

    End Sub

    Private Sub cleanamd()

        Dim regkey As RegistryKey = Nothing
        Dim subregkey As RegistryKey = Nothing
        Dim subregkey2 As RegistryKey = Nothing
        Dim wantedvalue As String = Nothing
        Dim wantedvalue2 As String = Nothing
        Dim superkey As RegistryKey = Nothing
        Dim filePath As String = Nothing
        Dim packages As String()

        UpdateTextMethod(UpdateTextMethodmessagefn(2))
        log("Cleaning known Regkeys")


        'Delete AMD regkey
        'Deleting DCOM object

        log("Starting dcom/clsid/appid/typelib cleanup")

        CleanupEngine.classroot(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\classroot.cfg")) '// add each line as String Array.


        '-----------------
        'interface cleanup
        '-----------------



        CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\AMD\interface.cfg")) '// add each line as String Array.

        log("Instance class cleanUP")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", False)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        If subregkey IsNot Nothing Then
                            subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance", False)
                            If subregkey2 IsNot Nothing Then
                                For Each child2 As String In subregkey2.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child2) = False Then
                                        superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\Instance\" & child2)
                                        If superkey IsNot Nothing Then
                                            If checkvariables.isnullorwhitespace(CStr(superkey.GetValue("FriendlyName"))) = False Then
                                                wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                If wantedvalue2.ToLower.Contains("ati mpeg") Or
                                                    wantedvalue2.ToLower.Contains("amd mjpeg") Or
                                                    wantedvalue2.ToLower.Contains("ati ticker") Or
                                                    wantedvalue2.ToLower.Contains("mmace softemu") Or
                                                    wantedvalue2.ToLower.Contains("mmace deinterlace") Or
                                                    wantedvalue2.ToLower.Contains("amd video") Or
                                                    wantedvalue2.ToLower.Contains("mmace procamp") Or
                                                    wantedvalue2.ToLower.Contains("ati video") Then
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot, "CLSID\" & child & "\Instance\" & child2)
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
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
                            If subregkey IsNot Nothing Then
                                subregkey2 = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance", False)
                                If subregkey2 IsNot Nothing Then
                                    For Each child2 As String In subregkey2.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child2) = False Then
                                            superkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\Instance\" & child2)
                                            If superkey IsNot Nothing Then
                                                If checkvariables.isnullorwhitespace(CStr(superkey.GetValue("FriendlyName"))) = False Then
                                                    wantedvalue2 = superkey.GetValue("FriendlyName").ToString
                                                    If wantedvalue2.ToLower.Contains("ati mpeg") Or
                                                    wantedvalue2.ToLower.Contains("amd mjpeg") Or
                                                    wantedvalue2.ToLower.Contains("ati ticker") Or
                                                    wantedvalue2.ToLower.Contains("mmace softemu") Or
                                                    wantedvalue2.ToLower.Contains("mmace deinterlace") Or
                                                    wantedvalue2.ToLower.Contains("mmace procamp") Or
                                                    wantedvalue2.ToLower.Contains("amd video") Or
                                                    wantedvalue2.ToLower.Contains("ati video") Then
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\Instance\" & child2)
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

        log("MediaFoundation cleanUP")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue(""))) Then
                            If regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd d3d11 hardware mft") Or
                                    regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd fast (dnd) decoder") Or
                                     regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd h.264 hardware mft encoder") Or
                                    regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd playback decoder mft") Then

                                For Each child2 As String In regkey.OpenSubKey("Categories", False).GetSubKeyNames
                                    Try
                                        deletesubregkey(regkey.OpenSubKey("Categories\" & child2, True), child)
                                    Catch ex As Exception
                                    End Try
                                Next

                                Try
                                    deletesubregkey(regkey, child)
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

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then

                            If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue(""))) Then
                                If regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd d3d11 hardware mft") Or
                                    regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd fast (dnd) decoder") Or
                                    regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd h.264 hardware mft encoder") Or
                                    regkey.OpenSubKey(child).GetValue("").ToString.ToLower.Contains("amd playback decoder mft") Then

                                    For Each child2 As String In regkey.OpenSubKey("Categories", False).GetSubKeyNames
                                        Try
                                            deletesubregkey(regkey.OpenSubKey("Categories\" & child2, True), child)
                                        Catch ex As Exception
                                        End Try
                                    Next

                                    Try
                                        deletesubregkey(regkey, child)
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
        End If

        log("AppID and clsidleftover cleanUP")
        'old dcom 

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
                        subregkey = regkey.OpenSubKey(child)
                        If subregkey IsNot Nothing Then
                            For Each childs As String In subregkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(childs) = False Then
                                    Try
                                        If checkvariables.isnullorwhitespace(CStr(subregkey.OpenSubKey(childs, False).GetValue("Assembly"))) = False Then
                                            If subregkey.OpenSubKey(childs, False).GetValue("Assembly").ToString.ToLower.Contains("aticccom") Then
                                                deletesubregkey(regkey, child)
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
                                deletesubregkey(regkey, child)
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
            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
                                                         "Display\shellex\PropertySheetHandlers", True), "ATIACE")
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
                                deletevalue(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.GetValueNames().Length = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos")
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
                                    deletevalue(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.GetValueNames().Length = 0 Then
                        Try
                            deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\Khronos")
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
                                deletesubregkey(regkey, child)
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
                        If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
                            regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                            Try
                                deletevalue(regkey, child)
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
                            If regkey.GetValue(child).ToString.ToLower.Contains("catalyst context menu extension") Or
                                regkey.GetValue(child).ToString.ToLower.Contains("display cpl extension") Then
                                Try
                                    deletevalue(regkey, child)
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

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\ATI Technologies")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\amdkmdap")
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then
            Try

                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
            Catch ex As Exception
            End Try

            Try

                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\ATI\ACE")
            Catch ex As Exception
            End Try
        End If

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\AMD\EEU")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnable")
        Catch ex As Exception
        End Try

        Try

            deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SYSTEM\CurrentControlSet\Services\Atierecord\eRecordEnablePopups")
        Catch ex As Exception
        End Try


        '---------------------------------------------
        'Cleaning of Legacy_AMDKMDAG+ on win7 and lower
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
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                     ("SYSTEM\" & childs & "\Enum\Root")
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If child.ToLower.Contains("legacy_amdkmdag") Or _
                                                (child.ToLower.Contains("legacy_amdkmdag") AndAlso removeamdkmpfd) Or _
                                                child.ToLower.Contains("legacy_amdacpksd") Then

                                                Try
                                                    deletesubregkey(My.Computer.Registry.LocalMachine, "SYSTEM\" & childs & "\Enum\Root\" & child)
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
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetValueNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If child.Contains("AMDAPPSDKROOT") Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    If child.Contains("Path") Then
                                        If checkvariables.isnullorwhitespace(CStr(regkey.GetValue(child))) = False Then
                                            wantedvalue = regkey.GetValue(child).ToString.ToLower
                                            Try
                                                Select Case True
                                                    Case wantedvalue.Contains(";" + sysdrv & "\program files (x86)\amd app\bin\x86_64")
                                                        wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\amd app\bin\x86_64", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\program files (x86)\amd app\bin\x86_64;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\amd app\bin\x86_64;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(";" + sysdrv & "\program files (x86)\amd app\bin\x86")
                                                        wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\amd app\bin\x86", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\program files (x86)\amd app\bin\x86;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\amd app\bin\x86;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static")
                                                        wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(";" + sysdrv & "\program Files (x86)\amd\ati.ace\core-static")
                                                        wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static", "")
                                                        regkey.SetValue(child, wantedvalue)

                                                    Case wantedvalue.Contains(sysdrv & "\program Files (x86)\amd\ati.ace\core-static;")
                                                        wantedvalue = wantedvalue.Replace(sysdrv & "\program Files (x86)\ati technologies\ati.ace\core-static;", "")
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
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.ToLower.Contains("aceeventlog") Then
                                            deletesubregkey(regkey, child)
                                        End If
                                    End If
                                Next


                                Try
                                    deletesubregkey(regkey.OpenSubKey("Application", True), "ATIeRecord")
                                Catch ex As Exception
                                End Try

                                Try
                                    deletesubregkey(regkey.OpenSubKey("System", True), "amdkmdag")
                                Catch ex As Exception
                                End Try

                                Try
                                    deletesubregkey(regkey.OpenSubKey("System", True), "amdkmdap")
                                Catch ex As Exception
                                End Try
                            End If
                            Try
                                deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services", True), "Atierecord")
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

                            deletesubregkey(regkey, child)

                        End If
                    End If

                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try


        ' to fix later, the range is too large and could lead to problems.
        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.StartsWith("ATI") Then
                                    deletesubregkey(regkey, child)
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        ' to fix later, the range is too large and could lead to problems.
        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.StartsWith("AMD") Then
                                    deletesubregkey(regkey, child)
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
                        If child.ToLower.Contains("ace") Or
                            child.ToLower.Contains("appprofiles") Or
                           child.ToLower.Contains("install") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software", True), "ATI")
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
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                        If child.ToLower.Contains("ati catalyst control center") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                        If child.ToLower.Contains("cds") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                        If child.ToLower.Contains("install") Then
                            'here we check the install path location in case CCC is not installed on the system drive.  A kill to explorer must be made
                            'to help cleaning in normal mode.
                            If System.Windows.Forms.SystemInformation.BootMode = BootMode.Normal Then
                                log("Killing Explorer.exe")
                                Dim appproc = Process.GetProcessesByName("explorer")
                                For i As Integer = 0 To appproc.Length - 1
                                    appproc(i).Kill()
                                Next i
                            End If

                            Try
                                If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("InstallDir"))) Then
                                    filePath = regkey.OpenSubKey(child).GetValue("InstallDir").ToString
                                    If Not checkvariables.isnullorwhitespace(filePath) AndAlso My.Computer.FileSystem.DirectoryExists(filePath) Then

                                        For Each childf As String In Directory.GetDirectories(filePath)
                                            If checkvariables.isnullorwhitespace(childf) = False Then
                                                If childf.ToLower.Contains("ati.ace") Or
                                                    childf.ToLower.Contains("cnext") Or
                                                    childf.ToLower.Contains("amdkmpfd") Or
                                                    childf.ToLower.Contains("cim") Then
                                                    Try
                                                        deletedirectory(childf)
                                                    Catch ex As Exception
                                                        log(ex.Message)
                                                        TestDelete(childf)
                                                    End Try
                                                    If Not Directory.Exists(childf) Then
                                                        CleanupEngine.shareddlls(childf)
                                                    End If
                                                End If
                                            End If
                                        Next

                                        If Directory.GetDirectories(filePath).Length = 0 Then
                                            Try
                                                deletedirectory(filePath)

                                            Catch ex As Exception
                                                log(ex.Message)
                                                TestDelete(filePath)
                                            End Try
                                        End If
                                        If Not Directory.Exists(filePath) Then
                                            CleanupEngine.shareddlls(filePath)
                                            'here we will do a special environement path cleanup as there is chances that the installation is
                                            'somewhere else.
                                            amdenvironementpath(filePath)
                                        End If
                                    End If
                                End If

                            Catch ex As Exception
                                log(ex.Message + ex.StackTrace)
                            End Try
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If Not checkvariables.isnullorwhitespace(child2) Then
                                    If child2.ToLower.Contains("ati catalyst") Or
                                        child2.ToLower.Contains("ati mcat") Or
                                        child2.ToLower.Contains("avt") Or
                                        child2.ToLower.Contains("ccc") Or
                                        child2.ToLower.Contains("cnext") Or
                                        child2.ToLower.Contains("amd app sdk") Or
                                        child2.ToLower.Contains("packages") Or
                                        child2.ToLower.Contains("wirelessdisplay") Or
                                        child2.ToLower.Contains("hydravision") Or
                                        child2.ToLower.Contains("avivo") Or
                                        child2.ToLower.Contains("ati display driver") Or
                                        child2.ToLower.Contains("installed drivers") Or
                                        child2.ToLower.Contains("steadyvideo") Then
                                        Try
                                            deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                            For Each values As String In regkey.OpenSubKey(child).GetValueNames()
                                Try
                                    deletevalue(regkey.OpenSubKey(child, True), values) 'This is for windows 7, it prevent removing the South Bridge and fix the Catalyst "Upgrade"
                                Catch ex As Exception
                                End Try
                            Next
                            If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                Try
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software", True), "ATI Technologies")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\AMD", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("eeu") Or
                           child.ToLower.Contains("fuel") Or
                           child.ToLower.Contains("cn") Or
                           child.ToLower.Contains("mftvdecoder") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software", True), "AMD")
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
                            If child.ToLower.Contains("ace") Or
                               child.ToLower.Contains("appprofiles") Then
                                Try
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True), "ATI")
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

                                deletesubregkey(regkey, child)

                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True), "AMD")
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
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                            If child.ToLower.Contains("install") Then
                                For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                    If child2.ToLower.Contains("ati catalyst") Or
                                        child2.ToLower.Contains("ati mcat") Or
                                        child2.ToLower.Contains("avt") Or
                                        child2.ToLower.Contains("ccc") Or
                                        child2.ToLower.Contains("cnext") Or
                                        child2.ToLower.Contains("packages") Or
                                        child2.ToLower.Contains("wirelessdisplay") Or
                                        child2.ToLower.Contains("hydravision") Or
                                        child2.ToLower.Contains("dndtranscoding64") Or
                                        child2.ToLower.Contains("avivo") Or
                                        child2.ToLower.Contains("steadyvideo") Then
                                        Try
                                            deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                Next
                                If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                    Try
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True), "ATI Technologies")
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
                            deletevalue(regkey, "HydraVisionDesktopManager")
                        Catch ex As Exception

                            log(ex.Message + " HydraVisionDesktopManager")
                        End Try

                        Try
                            deletevalue(regkey, "Grid")
                        Catch ex As Exception

                            log(ex.Message + " GRID")
                        End Try

                        Try
                            deletevalue(regkey, "HydraVisionMDEngine")
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

                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows\CurrentVersion\Uninstall\" & child)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
                                                Try
                                                    deletesubregkey(regkey, child)
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
                            subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                                ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" & child, True)
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To packages.Length - 1
                                            If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
                                                    Try
                                                        deletesubregkey(regkey, child)
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

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                ("Software\Microsoft\Windows\CurrentVersion\Run", True)
            If regkey IsNot Nothing Then
                Try
                    deletevalue(regkey, "StartCCC")

                Catch ex As Exception

                    log(ex.Message + " StartCCC")
                End Try
                Try
                    deletevalue(regkey, "StartCN")

                Catch ex As Exception

                    log(ex.Message + " StartCCC")
                End Try
                Try

                    deletevalue(regkey, "AMD AVT")

                Catch ex As Exception

                    log(ex.Message + " AMD AVT")
                End Try
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try


        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", True)
                If regkey IsNot Nothing Then
                    Try
                        deletevalue(regkey, "StartCCC")

                    Catch ex As Exception

                        log(ex.Message + " StartCCC")
                    End Try
                    Try
                        deletevalue(regkey, "StartCN")

                    Catch ex As Exception

                        log(ex.Message + " StartCCC")
                    End Try
                    Try

                        deletevalue(regkey, "AMD AVT")

                    Catch ex As Exception

                        log(ex.Message + " AMD AVT")
                    End Try
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
                        If child.Contains("ATI\CIM\") Or
                           child.Contains("AMD\CNext\") Or
                           child.Contains("AMD APP\") Or
                           child.Contains("AMD\SteadyVideo\") Or
                           child.Contains("HydraVision\") Then

                            Try
                                deletevalue(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        'prevent CCC reinstalltion (comes from drivers installed from windows updates)
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        If child.ToLower.Contains("launchwuapp") Then
                            deletevalue(regkey, child)
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetValueNames()
                        If Not checkvariables.isnullorwhitespace(child) Then
                            If child.ToLower.Contains("launchwuapp") Then
                                deletevalue(regkey, child)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.Message + ex.StackTrace)
            End Try
        End If

        'Saw on Win 10 cat 15.7
        log("AudioEngine CleanUP")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AudioEngine\AudioProcessingObjects", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        Try
                            If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) Then
                                If regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("cdelayapogfx") Then
                                    deletesubregkey(regkey, child)
                                End If
                            End If
                        Catch ex As Exception
                            log(ex.Message + ex.StackTrace)
                        End Try
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        'SteadyVideo stuff

        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    subregkey = regkey.OpenSubKey(child, False)
                    If subregkey IsNot Nothing Then
                        If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                            wantedvalue = subregkey.GetValue("").ToString
                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                If wantedvalue.ToLower.Contains("steadyvideo") Then
                                    Try
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If


        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("PROTOCOLS\Filter", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        subregkey = regkey.OpenSubKey(child, False)
                        If subregkey IsNot Nothing Then
                            If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) Then
                                wantedvalue = CStr(subregkey.GetValue(""))
                                If Not checkvariables.isnullorwhitespace(wantedvalue) Then
                                    If wantedvalue.ToLower.Contains("steadyvideo") Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            log(ex.Message + ex.StackTrace)
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            'SteadyVideo stuff

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = regkey.OpenSubKey(child, False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    If wantedvalue.ToLower.Contains("steadyvideo") Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If



            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\PROTOCOLS\Filter", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If Not checkvariables.isnullorwhitespace(child) Then
                            subregkey = regkey.OpenSubKey(child, False)
                            If subregkey IsNot Nothing Then
                                If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) Then
                                    wantedvalue = CStr(subregkey.GetValue(""))
                                    If Not checkvariables.isnullorwhitespace(wantedvalue) Then
                                        If wantedvalue.ToLower.Contains("steadyvideo") Then
                                            Try
                                                deletesubregkey(regkey, child)
                                            Catch ex As Exception
                                                log(ex.Message + ex.StackTrace)
                                            End Try
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.Message + ex.StackTrace)
            End Try

        End If

    End Sub

    Private Sub rebuildcountercache()
        log("Rebuilding the Perf.Counter cache X2")
        Try

            For i = 0 To 1
                processinfo.FileName = "lodctr"
                processinfo.Arguments = "/R"
                processinfo.WindowStyle = ProcessWindowStyle.Hidden
                processinfo.UseShellExecute = False
                processinfo.CreateNoWindow = True
                processinfo.RedirectStandardOutput = True

                process.StartInfo = processinfo
                process.Start()
                reply2 = process.StandardOutput.ReadToEnd
                process.StandardOutput.Close()
                process.Close()
                log(reply2)
            Next

        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try
    End Sub

    Private Sub fixregistrydriverstore()
        'Windows 8 + only
        'This should fix driver installation problem reporting that a file is not found.
        'It is usually caused by Windows somehow losing track of the driver store , This intend to help it a bit.
        If win8higher Then
            log("Fixing registry driverstore if necessary")
            Try
                Dim regkey As RegistryKey = Nothing
                Dim infslist As String = ""
                For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")
                    If Not checkvariables.isnullorwhitespace(infs) Then
                        infslist = infslist + infs
                    End If
                Next
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverInfFiles", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If Not checkvariables.isnullorwhitespace(child) Then
                            If child.ToLower.StartsWith("oem") AndAlso child.ToLower.EndsWith(".inf") Then
                                If Not infslist.ToLower.Contains(child) Then
                                    Try
                                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("DRIVERS\DriverDatabase\DriverPackages", True), CStr(regkey.OpenSubKey(child).GetValue("Active")))
                                    Catch ex As Exception
                                        log(ex.Message)
                                    End Try

                                    Try
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                        log(ex.Message)
                                    End Try
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.Message + ex.StackTrace)
            End Try
        End If

    End Sub

	Private Sub cleannvidiaserviceprocess()
		CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\services.cfg"))

		If removegfe Then
			CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\gfeservice.cfg"))
		End If

		'kill process NvTmru.exe and special kill for Logitech Keyboard(Lcore.exe) 
		'holding files in the NVIDIA folders sometimes.
		Try
			Dim processes As String() =
			 New String() {
			 "Lcore",
			 "nvgamemonitor",
			 "nvstreamsvc",
			 "NvTmru",
			 "nvxdsync",
			 "dwm",
			 "WWAHost",
			 "nvspcaps64",
			 "nvspcaps",
			 "NvBackend"}

			For Each pname As String In processes
				For Each p As Process In process.GetProcessesByName(pname)
					p.Kill()
				Next
			Next

			If removegfe Then
				Dim appproc = process.GetProcessesByName("nvtray")

				For i As Integer = 0 To appproc.Length - 1
					appproc(i).Kill()
				Next i
			End If

		Catch ex As Exception
		End Try
	End Sub

    Private Sub cleannvidiafolders()
        Dim regkey As RegistryKey = Nothing
        Dim subregkey As RegistryKey = Nothing
        Dim filePath As String = Nothing
        'Delete NVIDIA data Folders
        'Here we delete the Geforce experience / Nvidia update user it created. This fail sometime for no reason :/

        UpdateTextMethod(UpdateTextMethodmessagefn(3))
        log("Cleaning UpdatusUser users ac if present")

        Dim AD As DirectoryEntry = New DirectoryEntry("WinNT://" + Environment.MachineName.ToString())
        Dim users As DirectoryEntries = AD.Children
        Dim newuser As DirectoryEntry = Nothing

        Try
            newuser = users.Find("UpdatusUser")
            users.Remove(newuser)
        Catch ex As Exception
        End Try

        UpdateTextMethod(UpdateTextMethodmessagefn(4))

        log("Cleaning Directory")


        If removecnvidia = True Then
            filePath = sysdrv + "\NVIDIA"
            Try
                deletedirectory(filePath)
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
                        deletedirectory(child)
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
                        deletedirectory(child)
                    Catch ex As Exception
                        log(ex.Message + " Updatus directory delete")
                    End Try
                End If
            End If
        Next


        For Each filepaths As String In Directory.GetDirectories(IO.Path.GetDirectoryName(userpth))

            filePath = filepaths + "\AppData\Local\NVIDIA"

            If removegfe Then
                Try
                    For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If (child.ToLower.Contains("nvbackend") AndAlso removegfe) Or
                                (child.ToLower.Contains("nvosc.") AndAlso removegfe) Or
                                (child.ToLower.Contains("shareconnect") AndAlso removegfe) Or
                                (child.ToLower.Contains("gfexperience") AndAlso removegfe) Then
                                Try
                                    deletedirectory(child)
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
                                deletedirectory(filePath)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(filePath)
                            End Try
                        Else
                            For Each data As String In Directory.GetDirectories(filePath)
                                log("Remaining folders found " + " : " + data)
                            Next

                        End If
                    Catch ex As Exception
                    End Try
                Catch ex As Exception
                End Try
            End If

            filePath = filepaths + "\AppData\Roaming\NVIDIA"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("computecache") Or
                            child.ToLower.Contains("glcache") Then
                            Try
                                deletedirectory(child)
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
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try


            filePath = filepaths + "\AppData\Local\NVIDIA Corporation"
            If removegfe Then
                Try
                    For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                        If checkvariables.isnullorwhitespace(child) = False Then
                            If (child.ToLower.Contains("ledvisualizer") AndAlso removegfe) Or
                                (child.ToLower.Contains("shadowplay") AndAlso removegfe) Or
                                (child.ToLower.Contains("gfexperience") AndAlso removegfe) Or
                                (child.ToLower.Contains("nvstreamsrv") AndAlso removegfe) Or
                                (child.ToLower.EndsWith("\osc") AndAlso removegfe) Or
                                (child.ToLower.Contains("nvvad") AndAlso removegfe) Or
                                (child.ToLower.Contains("shield apps") AndAlso removegfe) Then

                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                End Try
                            End If
                        End If
                    Next
                    Try
                        If Directory.GetDirectories(filePath).Length = 0 Then
                            Try
                                deletedirectory(filePath)
                            Catch ex As Exception
                                log(ex.Message)
                            End Try
                        Else
                            For Each data As String In Directory.GetDirectories(filePath)
                                log("Remaining folders found " + " : " + data)
                            Next

                        End If
                    Catch ex As Exception
                    End Try
                Catch ex As Exception
                End Try
            End If

        Next

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\NVIDIA"

        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("updatus") Or _
                        (child.ToLower.Contains("grid") AndAlso removegfe) Then
                        Try
                            deletedirectory(child)
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
                        deletedirectory(filePath)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                Else
                    For Each data As String In Directory.GetDirectories(filePath)
                        log("Remaining folders found " + " : " + data)
                    Next
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
                    If child.ToLower.Contains("drs") Or
                        (child.ToLower.Contains("geforce experience") AndAlso removegfe) Or
                        (child.ToLower.Contains("gfexperience") AndAlso removegfe) Or
                        (child.ToLower.Contains("netservice") AndAlso removegfe) Or
                        (child.ToLower.Contains("crashdumps") AndAlso removegfe) Or
                        (child.ToLower.Contains("nvstream") AndAlso removegfe) Or
                        (child.ToLower.Contains("shadowplay") AndAlso removegfe) Or
                        (child.ToLower.Contains("ledvisualizer") AndAlso removegfe) Or
                        (child.ToLower.Contains("nview") AndAlso removegfe) Or
                        (child.ToLower.Contains("nvstreamsvc") AndAlso removegfe) Then
                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next

            End If
        Catch ex As Exception
        End Try

        filePath = Environment.GetFolderPath _
(Environment.SpecialFolder.CommonApplicationData) + "\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation"
        Try
            For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("3d vision") Then
                        Try
                            deletedirectory(child)
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
                        deletedirectory(filePath)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                Else
                    For Each data As String In Directory.GetDirectories(filePath)
                        log("Remaining folders found " + " : " + data)
                    Next
                End If
            Catch ex As Exception
            End Try
        Catch ex As Exception
        End Try


        filePath = Environment.GetFolderPath _
  (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"
        If Directory.Exists(filePath) Then
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("control panel client") Or
                       child.ToLower.Contains("display") Or
                       child.ToLower.Contains("coprocmanager") Or
                       child.ToLower.Contains("drs") Or
                       child.ToLower.Contains("nvsmi") Or
                       child.ToLower.Contains("opencl") Or
                       child.ToLower.Contains("3d vision") Or
                       child.ToLower.Contains("led visualizer") AndAlso removegfe Or
                       child.ToLower.Contains("netservice") AndAlso removegfe Or
                       child.ToLower.Contains("geforce experience") AndAlso removegfe Or
                       child.ToLower.Contains("nvstreamc") AndAlso removegfe Or
                       child.ToLower.Contains("nvstreamsrv") AndAlso removegfe Or
                       child.ToLower.EndsWith("\physx") Or
                       child.ToLower.Contains("nvstreamsrv") AndAlso removegfe Or
                       child.ToLower.Contains("shadowplay") AndAlso removegfe Or
                       child.ToLower.Contains("update common") AndAlso removegfe Or
                       child.ToLower.Contains("shield") AndAlso removegfe Or
                       child.ToLower.Contains("nview") Or
                       child.ToLower.Contains("nvidia wmi provider") Or
                       child.ToLower.Contains("gamemonitor") AndAlso removegfe Or
                       child.ToLower.Contains("nvgsync") Or
                       child.ToLower.Contains("update core") AndAlso removegfe Then


                        Try
                            deletedirectory(child)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(child)
                        End Try

                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                    If child.ToLower.Contains("installer2") Then
                        For Each child2 As String In Directory.GetDirectories(child)
                            If checkvariables.isnullorwhitespace(child2) = False Then
                                If child2.ToLower.Contains("display.3dvision") Or
                                   child2.ToLower.Contains("display.controlpanel") Or
                                   child2.ToLower.Contains("display.driver") Or
                                   child2.ToLower.Contains("msvcruntime") Or
                                   child2.ToLower.Contains("display.gfexperience") AndAlso removegfe Or
                                   child2.ToLower.Contains("osc.") AndAlso removegfe Or
                                   child2.ToLower.Contains("osclib.") AndAlso removegfe Or
                                   child2.ToLower.Contains("display.nvirusb") Or
                                   child2.ToLower.Contains("display.physx") Or
                                   child2.ToLower.Contains("display.update") AndAlso removegfe Or
                                   child2.ToLower.Contains("display.gamemonitor") AndAlso removegfe Or
                                   child2.ToLower.Contains("gfexperience") AndAlso removegfe Or
                                   child2.ToLower.Contains("nvidia.update") AndAlso removegfe Or
                                   child2.ToLower.Contains("installer2\installer") AndAlso removegfe Or
                                   child2.ToLower.Contains("network.service") AndAlso removegfe Or
                                   child2.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
                                   child2.ToLower.Contains("shadowplay") AndAlso removegfe Or
                                   child2.ToLower.Contains("update.core") AndAlso removegfe Or
                                   child2.ToLower.Contains("virtualaudio.driver") AndAlso removegfe Or
                                   child2.ToLower.Contains("coretemp") AndAlso removegfe Or
                                   child2.ToLower.Contains("shield") AndAlso removegfe Or
                                   child2.ToLower.Contains("hdaudio.driver") Then

                                    Try
                                        deletedirectory(child2)
                                    Catch ex As Exception
                                        log(ex.Message)
                                        TestDelete(child2)
                                    End Try

                                    If Not Directory.Exists(child2) Then
                                        CleanupEngine.shareddlls(child2)
                                    End If
                                End If
                            End If
                        Next

                        If Directory.GetDirectories(child).Length = 0 Then
                            Try
                                deletedirectory(child)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        Else
                            For Each data As String In Directory.GetDirectories(child)
                                log("Remaining folders found " + " : " + data)
                            Next

                        End If
                        If Not Directory.Exists(child) Then
                            CleanupEngine.shareddlls(child)
                        End If
                    End If
                End If
            Next
            If Directory.GetDirectories(filePath).Length = 0 Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                    log(ex.Message)
                    TestDelete(filePath)
                End Try
            Else
                For Each data As String In Directory.GetDirectories(filePath)
                    log("Remaining folders found " + " : " + data)
                Next
            End If
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If




        filePath = Environment.GetFolderPath _
            (Environment.SpecialFolder.ProgramFiles) + "\AGEIA Technologies"
        If Directory.Exists(filePath) Then
            Try
                deletedirectory(filePath)
            Catch ex As Exception
            End Try
        End If
        If Not Directory.Exists(filePath) Then
            CleanupEngine.shareddlls(filePath)
        End If


        If IntPtr.Size = 8 Then
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation"
            If Directory.Exists(filePath) Then
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("3d vision") Or
                           child.ToLower.Contains("coprocmanager") Or
                           child.ToLower.Contains("led visualizer") AndAlso removegfe Or
                           child.ToLower.Contains("osc") AndAlso removegfe Or
                           child.ToLower.Contains("netservice") AndAlso removegfe Or
                           child.ToLower.Contains("nvidia geforce experience") AndAlso removegfe Or
                           child.ToLower.Contains("nvstreamc") AndAlso removegfe Or
                           child.ToLower.Contains("nvstreamsrv") AndAlso removegfe Or
                           child.ToLower.Contains("update common") AndAlso removegfe Or
                           child.ToLower.Contains("nvgsync") Or
                           child.ToLower.EndsWith("\physx") Or
                           child.ToLower.Contains("update core") AndAlso removegfe Then
                            If removephysx Then
                                Try
                                    deletedirectory(child)
                                Catch ex As Exception
                                    log(ex.Message)
                                    TestDelete(child)
                                End Try
                            Else
                                If child.ToLower.Contains("physx") Then
                                    'do nothing
                                Else
                                    Try
                                        deletedirectory(child)
                                    Catch ex As Exception
                                        log(ex.Message)
                                        TestDelete(child)
                                    End Try
                                End If
                            End If
                            If Not Directory.Exists(child) Then
                                CleanupEngine.shareddlls(child)
                            End If
                        End If
                    End If
                Next

                If Directory.GetDirectories(filePath).Length = 0 Then
                    Try
                        deletedirectory(filePath)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                Else
                    For Each data As String In Directory.GetDirectories(filePath)
                        log("Remaining folders found " + " : " + data)
                    Next

                End If
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If
        End If



        If IntPtr.Size = 8 Then
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\AGEIA Technologies"
            If Directory.Exists(filePath) Then
                Try
                    deletedirectory(filePath)
                Catch ex As Exception
                End Try
            End If
            If Not Directory.Exists(filePath) Then
                CleanupEngine.shareddlls(filePath)
            End If
        End If


        CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.
        If removegfe Then
            CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\gfedriverfiles.cfg")) '// add each line as String Array.
        End If

        filePath = System.Environment.SystemDirectory
        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "nvdisp*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    deletefile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

        filePath = System.Environment.SystemDirectory
        files = IO.Directory.GetFiles(filePath + "\", "nvhdagenco*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    deletefile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

        filePath = Environment.GetEnvironmentVariable("windir")
        Try
            deletedirectory(filePath + "\Help\nvcpl")
        Catch ex As Exception
        End Try

        Try
            filePath = Environment.GetEnvironmentVariable("windir") + "\Temp\NVIDIA Corporation"
            For Each child As String In Directory.GetDirectories(filePath)
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("nv_cache") Then
                        Try
                            deletedirectory(child)
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
                        deletedirectory(filePath)
                    Catch ex As Exception
                        log(ex.Message)
                        TestDelete(filePath)
                    End Try
                Else
                    For Each data As String In Directory.GetDirectories(filePath)
                        log("Remaining folders found " + " : " + data)
                    Next

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
                        If child.ToLower.Contains("nv_cache") Or
                            child.ToLower.Contains("displaydriver") Then
                            Try
                                deletedirectory(child)
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
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            filePath = filepaths + "\AppData\Local\Temp\NVIDIA"

            Try
                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("geforceexperienceselfupdate") AndAlso removegfe Or _
                           child.ToLower.Contains("displaydriver") Then
                            Try
                                deletedirectory(child)
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
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

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
                                deletedirectory(child)
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
                            deletedirectory(filePath)
                        Catch ex As Exception
                            log(ex.Message)
                            TestDelete(filePath)
                        End Try
                    Else
                        For Each data As String In Directory.GetDirectories(filePath)
                            log("Remaining folders found " + " : " + data)
                        Next

                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try

            'windows 8+ only (store apps nv_cache cleanup)
            Try
                If win8higher Then
                    Dim prefilePath As String = filepaths + "\AppData\Local\Packages"
                    For Each childs As String In My.Computer.FileSystem.GetDirectories(prefilePath)
                        If Not checkvariables.isnullorwhitespace(childs) Then
                            filePath = childs + "\AC\Temp\NVIDIA Corporation"

                            If Directory.Exists(filePath) Then
                                For Each child As String In My.Computer.FileSystem.GetDirectories(filePath)
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.ToLower.Contains("nv_cache") Then
                                            Try
                                                deletedirectory(child)
                                            Catch ex As Exception
                                                log(ex.Message)
                                                TestDelete(child)
                                            End Try
                                        End If
                                    End If
                                Next

                                If Directory.GetDirectories(filePath).Length = 0 Then
                                    Try
                                        deletedirectory(filePath)
                                    Catch ex As Exception
                                        log(ex.Message)
                                        TestDelete(filePath)
                                    End Try
                                Else
                                    For Each data As String In Directory.GetDirectories(filePath)
                                        log("Remaining folders found " + " : " + data)
                                    Next

                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
            End Try

        Next

        'Cleaning the GFE 2.0.1 and earlier assemblies.
        If removegfe Then
            filePath = Environment.GetEnvironmentVariable("windir") + "\assembly\NativeImages_v4.0.30319_32"
            If Directory.Exists(filePath) Then
                For Each child As String In Directory.GetDirectories(filePath)
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("gfexperience") Or
                            child.ToLower.Contains("nvidia.sett") Or
                            child.ToLower.Contains("nvidia.updateservice") Or
                            child.ToLower.Contains("nvidia.win32api") Or
                            child.ToLower.Contains("installeruiextension") Or
                            child.ToLower.Contains("installerservice") Or
                            child.ToLower.Contains("gridservice") Or
                            child.ToLower.Contains("shadowplay") Or
                           child.ToLower.Contains("nvidia.gfe") Then
                            Try
                                deletedirectory(child)
                            Catch ex As Exception
                                log(ex.Message)
                                TestDelete(child)
                            End Try
                        End If
                    End If
                Next
            End If
        End If

        '-----------------
        'MUI cache cleanUP
        '-----------------
        'Note: this MUST be done after cleaning the folders.
        log("MuiCache CleanUP")
        Try
            For Each regusers As String In My.Computer.Registry.Users.GetSubKeyNames
                If Not checkvariables.isnullorwhitespace(regusers) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(regusers & "\software\classes\local settings\muicache", False)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                subregkey = regkey.OpenSubKey(child, False)
                                If subregkey IsNot Nothing Then
                                    For Each childs As String In subregkey.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(childs) = False Then
                                            For Each Keyname As String In subregkey.OpenSubKey(childs).GetValueNames
                                                If Not checkvariables.isnullorwhitespace(Keyname) Then

                                                    If Keyname.ToLower.Contains("nvstlink.exe") Or
                                                        Keyname.ToLower.Contains("nvstview.exe") Or
                                                       Keyname.ToLower.Contains("gfexperience.exe") AndAlso removegfe Or
                                                       Keyname.ToLower.Contains("nvcpluir.dll") Then
                                                        Try
                                                            deletevalue(subregkey.OpenSubKey(childs, True), Keyname)
                                                        Catch ex As Exception
                                                            log(ex.Message + ex.StackTrace)
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
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        Try
            For Each regusers As String In My.Computer.Registry.Users.GetSubKeyNames
                If Not checkvariables.isnullorwhitespace(regusers) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(regusers & "\software\classes\local settings\software\microsoft\windows\shell\muicache", True)
                    If regkey IsNot Nothing Then

                        For Each Keyname As String In regkey.GetValueNames
                            If Not checkvariables.isnullorwhitespace(Keyname) Then

                                If Keyname.ToLower.Contains("nvstlink.exe") Or
                                    Keyname.ToLower.Contains("nvstview.exe") Or
                                   Keyname.ToLower.Contains("gfexperience.exe") AndAlso removegfe Or
                                   Keyname.ToLower.Contains("nvcpluir.dll") Then
                                    Try
                                        deletevalue(regkey, Keyname)
                                    Catch ex As Exception
                                        log(ex.Message + ex.StackTrace)
                                    End Try
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        If removephysx Then
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + " (x86)" + "\NVIDIA Corporation\physx"
            CleanupEngine.shareddlls(filePath)
            filePath = Environment.GetFolderPath _
                (Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation\physx"
        End If

    End Sub

    Private Sub cleannvidia()
        Dim regkey As RegistryKey = Nothing
        Dim subregkey As RegistryKey = Nothing
        Dim subregkey2 As RegistryKey = Nothing
        Dim wantedvalue As String = Nothing
        Dim wantedvalue2 As String = Nothing
        '-----------------
        'Registry Cleaning
        '-----------------
        UpdateTextMethod(UpdateTextMethodmessagefn(5))
        log("Starting reg cleanUP... May take a minute or two.")


        'Deleting DCOM object /classroot
        log("Starting dcom/clsid/appid/typelib cleanup")

        CleanupEngine.classroot(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\classroot.cfg")) '// add each line as String Array.

        CleanupEngine.clsidleftover(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftover.cfg")) '// add each line as String Array.

        'for GFE removal only
        If removegfe Then
            CleanupEngine.clsidleftover(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\clsidleftoverGFE.cfg")) '// add each line as String Array.
        End If
        '------------------------------
        'Clean the rebootneeded message
        '------------------------------
        Try

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        If child.ToLower.Contains("nvidia_rebootneeded") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                                log(ex.Message + ex.StackTrace)
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        '-----------------
        'interface cleanup
        '-----------------

        CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\interface.cfg")) '// add each line as String Array.

        'When removing GFE only
        If removegfe Then
            CleanupEngine.interfaces(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\interfaceGFE.cfg")) '// add each line as String Array.
        End If

        log("Finished dcom/clsid/appid/typelib/interface cleanup")

        'end of deleting dcom stuff
        log("Pnplockdownfiles region cleanUP")

        CleanupEngine.Pnplockdownfiles(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\driverfiles.cfg")) '// add each line as String Array.

        'Cleaning PNPRessources.
        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos", False) IsNot Nothing Then
            Try
                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Khronos")
            Catch ex As Exception
                log(ex.Message & "pnp resources khronos")
            End Try
        End If

        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\Global", False) IsNot Nothing Then
            Try
                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation\global")
            Catch ex As Exception
                log(ex.Message & "pnp resources khronos")
            End Try
        End If

        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False) IsNot Nothing Then
            If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation", False).SubKeyCount = 0 Then
                Try
                    deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\NVIDIA Corporation")
                Catch ex As Exception
                    log(ex.Message & "pnp resources khronos")
                End Try
            End If
        End If

        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension", False) IsNot Nothing Then
            Try
                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\Display\shellex\PropertySheetHandlers\NVIDIA CPL Extension")
            Catch ex As Exception
                log(ex.Message & "pnp resources cpl extension")
            End Try
        End If

        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation", False) IsNot Nothing Then
            Try
                deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\NVIDIA Corporation")
            Catch ex As Exception
                log(ex.Message & "pnp ressources nvidia corporation")
            End Try
        End If

        If IntPtr.Size = 8 Then
            If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos", False) IsNot Nothing Then
                Try
                    deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKLM\SOFTWARE\Wow6432Node\Khronos")
                Catch ex As Exception
                    log(ex.Message & "pnpresources wow6432node khronos")
                End Try
            End If
        End If



        If removegfe Then
            '----------------------
            'Firewall entry cleanup
            '----------------------
            log("Firewall entry cleanUP")
            Try
                If winxp = False Then
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
                    If subregkey IsNot Nothing Then
                        For Each child2 As String In subregkey.GetSubKeyNames()
                            If child2.ToLower.Contains("controlset") Then
                                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules", True)
                                If regkey IsNot Nothing Then
                                    For Each child As String In regkey.GetValueNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If checkvariables.isnullorwhitespace(CStr(regkey.GetValue(child))) = False Then
                                                wantedvalue = regkey.GetValue(child).ToString()
                                            End If
                                            If wantedvalue.ToLower.ToString.Contains("nvstreamsrv") Or
                                               wantedvalue.ToLower.ToString.Contains("nvidia network service") Or
                                               wantedvalue.ToLower.ToString.Contains("nvidia update core") Then
                                                Try
                                                    deletevalue(regkey, child)
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
        End If
        '--------------------------
        'End Firewall entry cleanup
        '--------------------------
        log("End Firewall CleanUP")
        '--------------------------
        'Power Settings CleanUP
        '--------------------------
        log("Power Settings Cleanup")
        Try
            If winxp = False Then
                subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
                If subregkey IsNot Nothing Then
                    For Each child2 As String In subregkey.GetSubKeyNames()
                        If child2.ToLower.Contains("controlset") Then
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Power\PowerSettings", True)
                            If regkey IsNot Nothing Then
                                For Each childs As String In regkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(childs) = False Then
                                        For Each child As String In regkey.OpenSubKey(childs).GetValueNames()
                                            If checkvariables.isnullorwhitespace(child) = False And child.ToString.ToLower.Contains("description") Then
                                                If checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(childs).GetValue(child))) = False Then
                                                    wantedvalue = regkey.OpenSubKey(childs).GetValue(child).ToString()
                                                End If
                                                If wantedvalue.ToString.ToLower.Contains("nvsvc") Then
                                                    deletesubregkey(regkey, childs)
                                                End If
                                                If wantedvalue.ToString.ToLower.Contains("video and display power management") Then
                                                    subregkey2 = regkey.OpenSubKey(childs, True)
                                                    If subregkey2 IsNot Nothing Then
                                                        For Each childinsubregkey2 As String In subregkey2.GetSubKeyNames()
                                                            If checkvariables.isnullorwhitespace(childinsubregkey2) = False Then
                                                                For Each childinsubregkey2value As String In subregkey2.OpenSubKey(childinsubregkey2).GetValueNames()
                                                                    If checkvariables.isnullorwhitespace(childinsubregkey2value) = False And childinsubregkey2value.ToString.ToLower.Contains("description") Then
                                                                        If checkvariables.isnullorwhitespace(CStr(subregkey2.OpenSubKey(childinsubregkey2).GetValue(childinsubregkey2value))) = False Then
                                                                            wantedvalue2 = subregkey2.OpenSubKey(childinsubregkey2).GetValue(childinsubregkey2value).ToString
                                                                        End If
                                                                        If wantedvalue2.ToString.ToLower.Contains("nvsvc") Then
                                                                            Try
                                                                                deletesubregkey(subregkey2, childinsubregkey2)
                                                                            Catch ex As Exception
                                                                            End Try
                                                                        End If
                                                                    End If
                                                                Next
                                                            End If
                                                        Next
                                                    End If
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
        log("End Power Settings Cleanup")

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
                            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
                            If regkey IsNot Nothing Then
                                For Each child As String In regkey.GetValueNames()
                                    If checkvariables.isnullorwhitespace(child) = False Then
                                        If child.Contains("Path") Then
                                            If Not checkvariables.isnullorwhitespace(regkey.GetValue(child).ToString()) Then
                                                wantedvalue = regkey.GetValue(child).ToString.ToLower
                                                Try
                                                    Select Case True
                                                        Case wantedvalue.Contains(sysdrv & "\program files (x86)\nvidia corporation\physx\common;")
                                                            wantedvalue = wantedvalue.Replace(sysdrv & "\program files (x86)\nvidia corporation\physx\common;", "")
                                                            Try
                                                                regkey.SetValue(child, wantedvalue)
                                                            Catch ex As Exception
                                                            End Try
                                                        Case wantedvalue.Contains(";" + sysdrv & "\program files (x86)\nvidia corporation\physx\common")
                                                            wantedvalue = wantedvalue.Replace(";" + sysdrv & "\program files (x86)\nvidia corporation\physx\common", "")
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
        log("End System environement path cleanup")

        Try
            sysdrv = sysdrv.ToUpper
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", True)
            If regkey IsNot Nothing Then
                If checkvariables.isnullorwhitespace(CStr(regkey.GetValue("AppInit_DLLs"))) = False Then
                    wantedvalue = CStr(regkey.GetValue("AppInit_DLLs"))   'Will need to consider the comma in the future for multiple value
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
                If CStr(regkey.GetValue("AppInit_DLLs")) = "" Then
                    Try
                        regkey.SetValue("LoadAppInit_DLLs", "0", RegistryValueKind.DWord)
                    Catch ex As Exception
                    End Try
                End If
            End If
            sysdrv = sysdrv.ToLower
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        Try
            If IntPtr.Size = 8 Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                   ("SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows", True)

                If regkey IsNot Nothing Then
                    If checkvariables.isnullorwhitespace(CStr(regkey.GetValue("AppInit_DLLs"))) = False Then
                        wantedvalue = CStr(regkey.GetValue("AppInit_DLLs"))
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
                    If CStr(regkey.GetValue("AppInit_DLLs")) = "" Then
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

        'remove opencl registry Khronos
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Khronos\OpenCL\Vendors", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvopencl") Then
                            Try
                                deletevalue(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.GetValueNames().Length = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Khronos")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
        End Try

        If IntPtr.Size = 8 Then
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Khronos\OpenCL\Vendors", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvopencl") Then
                            Try
                                deletevalue(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.GetValueNames().Length = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine, "Software\Wow6432Node\Khronos")
                    Catch ex As Exception
                    End Try
                End If
            End If

        End If


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
                                            If child2.ToLower.Contains("global") Then
                                                If removegfe Then
                                                    Try
                                                        deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                                    Catch ex As Exception
                                                    End Try
                                                Else
                                                    For Each child3 As String In regkey.OpenSubKey(child + "\" + child2).GetSubKeyNames()
                                                        If checkvariables.isnullorwhitespace(child3) = False Then
                                                            If child3.ToLower.Contains("gfeclient") Or _
                                                                child3.ToLower.Contains("gfexperience") Or _
                                                                child3.ToLower.Contains("shadowplay") Or _
                                                                child3.ToLower.Contains("ledvisualizer") Then
                                                                'do nothing
                                                            Else
                                                                Try
                                                                    deletesubregkey(regkey.OpenSubKey(child + "\" + child2, True), child3)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If
                                            If child2.ToLower.Contains("logging") Or
                                                child2.ToLower.Contains("nvbackend") AndAlso removegfe Or
                                                child2.ToLower.Contains("nvidia update core") AndAlso removegfe Or
                                                child2.ToLower.Contains("nvcontrolpanel2") Or
                                                child2.ToLower.Contains("nvcontrolpanel") Or
                                                child2.ToLower.Contains("nvtray") AndAlso removegfe Or
                                                child2.ToLower.Contains("nvstream") AndAlso removegfe Or
                                                child2.ToLower.Contains("nvidia control panel") Then
                                                Try
                                                    deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    Next
                                    If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            End If
                        Next
                    End If

                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetValueNames()
                            If checkvariables.isnullorwhitespace(child) = False Then
                                Dim tArray() As String = CType(regkey.GetValue(child), String())
                                For i As Integer = 0 To tArray.Length - 1
                                    If checkvariables.isnullorwhitespace(tArray(i)) = False AndAlso Not tArray(i) = "" Then
                                        If tArray(i).ToLower.ToString.Contains("nvstview.exe") Or _
                                           tArray(i).ToLower.ToString.Contains("nvstlink.exe") Then
                                            Try
                                                deletevalue(regkey, child)
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
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\ARP", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetValueNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    Dim tArray() As String = CType(regkey.GetValue(child), String())
                    For i As Integer = 0 To tArray.Length - 1
                        If checkvariables.isnullorwhitespace(tArray(i)) = False AndAlso Not tArray(i) = "" Then
                            If tArray(i).ToLower.ToString.Contains("nvi2.dll") Or _
                               tArray(i).ToLower.ToString.Contains("nvstlink.exe") Then
                                Try
                                    deletevalue(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Next
        End If

        regkey = My.Computer.Registry.Users.OpenSubKey(".DEFAULT\Software", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("nvidia corporation") Then
                        For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child2) = False Then
                                If child2.ToLower.Contains("global") Or
                                   child2.ToLower.Contains("nvbackend") Or
                                   child2.ToLower.Contains("nvidia update core") AndAlso removegfe Or
                                    child2.ToLower.Contains("nvcontrolpanel2") Or
                                    child2.ToLower.Contains("nvidia control panel") Then
                                    Try
                                        deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next
                        If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                End If
            Next
        End If


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("ageia technologies") Then
                        If removephysx Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                    If child.ToLower.Contains("nvidia corporation") Then
                        For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child2) = False Then
                                If child2.ToLower.Contains("global") Then
                                    If removegfe Then
                                        Try
                                            deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                        Catch ex As Exception
                                        End Try
                                    Else
                                        For Each child3 As String In regkey.OpenSubKey(child + "\" + child2).GetSubKeyNames()
                                            If checkvariables.isnullorwhitespace(child3) = False Then
                                                If child3.ToLower.Contains("gfeclient") Or _
                                                    child3.ToLower.Contains("gfexperience") Or _
                                                    child3.ToLower.Contains("nvbackend") Or _
                                                    child3.ToLower.Contains("nvscaps") Or _
                                                    child3.ToLower.Contains("shadowplay") Or _
                                                    child3.ToLower.Contains("ledvisualizer") Then
                                                    'do nothing
                                                Else
                                                    Try
                                                        deletesubregkey(regkey.OpenSubKey(child + "\" + child2, True), child3)
                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                                If child2.ToLower.Contains("installer") Or
                                   child2.ToLower.Contains("logging") Or
                                    child2.ToLower.Contains("installer2") AndAlso removegfe Or
                                    child2.ToLower.Contains("nvidia update core") Or
                                    child2.ToLower.Contains("nvcontrolpanel") Or
                                    child2.ToLower.Contains("nvcontrolpanel2") Or
                                    child2.ToLower.Contains("nvstream") AndAlso removegfe Or
                                    child2.ToLower.Contains("nvstreamc") AndAlso removegfe Or
                                    child2.ToLower.Contains("nvstreamsrv") AndAlso removegfe Or
                                    child2.ToLower.Contains("physx_systemsoftware") Or
                                    child2.ToLower.Contains("physxupdateloader") Or
                                    child2.ToLower.Contains("uxd") Or
                                    child2.ToLower.Contains("nvtray") AndAlso removegfe Then
                                    If removephysx Then
                                        Try
                                            deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                        Catch ex As Exception
                                        End Try
                                    Else
                                        If child2.ToLower.Contains("physx") Then
                                            'do nothing
                                        Else
                                            Try
                                                deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                            Catch ex As Exception
                                            End Try
                                        End If
                                    End If
                                End If
                            End If
                        Next
                        If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                End If
            Next
        End If



        If IntPtr.Size = 8 Then
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("ageia technologies") Then
                            If removephysx Then
                                deletesubregkey(regkey, child)
                            End If
                        End If
                        If child.ToLower.Contains("nvidia corporation") Then
                            For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child2) = False Then
                                    If child2.ToLower.Contains("global") Then
                                        If removegfe Then
                                            Try
                                                deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                            Catch ex As Exception
                                            End Try
                                        Else
                                            For Each child3 As String In regkey.OpenSubKey(child + "\" + child2).GetSubKeyNames()
                                                If checkvariables.isnullorwhitespace(child3) = False Then
                                                    If child3.ToLower.Contains("gfeclient") Or _
                                                        child3.ToLower.Contains("gfexperience") Or _
                                                        child3.ToLower.Contains("nvbackend") Or _
                                                        child3.ToLower.Contains("nvscaps") Or _
                                                        child3.ToLower.Contains("shadowplay") Or _
                                                        child3.ToLower.Contains("ledvisualizer") Then
                                                        'do nothing
                                                    Else
                                                        Try
                                                            deletesubregkey(regkey.OpenSubKey(child + "\" + child2, True), child3)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                End If
                                            Next
                                        End If
                                    End If
                                    If child2.ToLower.Contains("logging") Or
                                        child2.ToLower.Contains("physx_systemsoftware") Or
                                        child2.ToLower.Contains("physxupdateloader") Or
                                       child2.ToLower.Contains("installer2") Or
                                       child2.ToLower.Contains("physx") Then
                                        If removephysx Then
                                            Try
                                                deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                            Catch ex As Exception
                                            End Try
                                        Else
                                            If child2.ToLower.Contains("physx") Then
                                                'do nothing
                                            Else
                                                Try
                                                    deletesubregkey(regkey.OpenSubKey(child, True), child2)
                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                            If regkey.OpenSubKey(child).SubKeyCount = 0 Then
                                Try
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
            End If
        End If



        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            Try
                                If removephysx Then
                                    If checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("DisplayName"))) = False Then
                                        If regkey.OpenSubKey(child).GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
                                            deletesubregkey(regkey, child)
                                            Continue For
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                                log(ex.Message + ex.StackTrace + "WoWChild = " + child)
                            End Try
                            If child.ToLower.Contains("display.3dvision") Or
                                child.ToLower.Contains("3dtv") Or
                                child.ToLower.Contains("_display.controlpanel") Or
                                child.ToLower.Contains("_display.driver") Or
                                child.ToLower.Contains("_display.gfexperience") AndAlso removegfe Or
                                child.ToLower.Contains("_display.nvirusb") Or
                                child.ToLower.Contains("_display.physx") Or
                                child.ToLower.Contains("_display.update") AndAlso removegfe Or
                                child.ToLower.Contains("_display.gamemonitor") AndAlso removegfe Or
                                child.ToLower.Contains("_gfexperience") AndAlso removegfe Or
                                child.ToLower.Contains("_hdaudio.driver") Or
                                child.ToLower.Contains("_installer") AndAlso removegfe Or
                                child.ToLower.Contains("_network.service") AndAlso removegfe Or
                                child.ToLower.Contains("_shadowplay") AndAlso removegfe Or
                                child.ToLower.Contains("_update.core") AndAlso removegfe Or
                                child.ToLower.Contains("nvidiastereo") Or
                                child.ToLower.Contains("_shieldwireless") AndAlso removegfe Or
                                child.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
                                child.ToLower.Contains("_virtualaudio.driver") AndAlso removegfe Then
                                If removephysx = False And child.ToLower.Contains("physx") Then
                                    Continue For
                                End If
                                If remove3dtvplay = False And child.ToLower.Contains("3dtv") Then
                                    Continue For
                                End If
                                Try
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.Message + ex.StackTrace)
            End Try
        End If


        Try

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
          ("Software\Microsoft\Windows\CurrentVersion\Uninstall", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        Try
                            If removephysx Then
                                If checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("DisplayName"))) = False Then
                                    If regkey.OpenSubKey(child).GetValue("DisplayName").ToString.ToLower.Contains("physx") Then
                                        deletesubregkey(regkey, child)
                                        Continue For
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                            log(ex.Message + ex.StackTrace + "Child = " + child)
                        End Try
                        If child.ToLower.Contains("display.3dvision") Or
                            child.ToLower.Contains("3dtv") Or
                            child.ToLower.Contains("_display.controlpanel") Or
                            child.ToLower.Contains("_display.driver") Or
                            child.ToLower.Contains("_display.optimus") Or
                            child.ToLower.Contains("_display.gfexperience") AndAlso removegfe Or
                            child.ToLower.Contains("_display.nvirusb") Or
                            child.ToLower.Contains("_display.physx") Or
                            child.ToLower.Contains("_display.update") AndAlso removegfe Or
                            child.ToLower.Contains("_osc") AndAlso removegfe Or
                            child.ToLower.Contains("_display.nview") Or
                            child.ToLower.Contains("_display.nvwmi") Or
                            child.ToLower.Contains("_display.gamemonitor") AndAlso removegfe Or
                            child.ToLower.Contains("_nvidia.update") AndAlso removegfe Or
                            child.ToLower.Contains("_gfexperience") AndAlso removegfe Or
                            child.ToLower.Contains("_hdaudio.driver") Or
                            child.ToLower.Contains("_installer") AndAlso removegfe Or
                            child.ToLower.Contains("_network.service") AndAlso removegfe Or
                            child.ToLower.Contains("_shadowplay") AndAlso removegfe Or
                            child.ToLower.Contains("_update.core") AndAlso removegfe Or
                            child.ToLower.Contains("nvidiastereo") Or
                            child.ToLower.Contains("_shieldwireless") AndAlso removegfe Or
                            child.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
                            child.ToLower.Contains("_virtualaudio.driver") Then
                            If removephysx = False And child.ToLower.Contains("physx") Then
                                Continue For
                            End If

                            If remove3dtvplay = False And child.ToLower.Contains("3dtv") Then
                                Continue For
                            End If
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
      ("Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetValueNames()
                If Not checkvariables.isnullorwhitespace(child) Then
                    If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                        deletevalue(regkey, child)
                    End If
                End If
            Next
        End If

        regkey = My.Computer.Registry.CurrentUser.OpenSubKey _
("Software\Microsoft\.NETFramework\SQM\Apps", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(child) Then
                    If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                        deletesubregkey(regkey, child)
                    End If
                End If
            Next
        End If

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey _
            (users + "\Software\Microsoft\.NETFramework\SQM\Apps", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If Not checkvariables.isnullorwhitespace(child) Then
                                If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                                    deletesubregkey(regkey, child)
                                End If
                            End If
                        Next
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try


        Try

            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey _
            (users + "\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store", True)
                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetValueNames()
                            If Not checkvariables.isnullorwhitespace(child) Then
                                If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                                    deletevalue(regkey, child)
                                End If
                            End If
                        Next
                    End If
                End If
            Next

        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                    ("Software\Microsoft\Windows NT\CurrentVersion\ProfileList\" & child, False)
                    If subregkey IsNot Nothing Then
                        If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("ProfileImagePath"))) = False Then
                            wantedvalue = subregkey.GetValue("ProfileImagePath").ToString
                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                If wantedvalue.Contains("UpdatusUser") Then
                                    Try
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\" & child, False)
                    If subregkey IsNot Nothing Then
                        If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                            wantedvalue = subregkey.GetValue("").ToString
                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                If wantedvalue.ToLower.Contains("nvidia control panel") Or
                                   wantedvalue.ToLower.Contains("nvidia nview desktop manager") Then
                                    Try
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                    End Try
                                    'special case only to nvidia afaik. there i a clsid for a control pannel that link from namespace.
                                    Try
                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True), child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        End If


        '----------------------
        '.net ngenservice clean
        '----------------------
        log("ngenservice Clean")

        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                        Try
                            deletesubregkey(regkey, child)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        End If

        If IntPtr.Size = 8 Then

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\NGenService\Roots", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("gfexperience.exe") AndAlso removegfe Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        End If
        log("End ngenservice Clean")
        '-----------------------------
        'End of .net ngenservice clean
        '-----------------------------

        '-----------------------------
        'Mozilla plugins
        '-----------------------------
        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\MozillaPlugins", True)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    If child.ToLower.Contains("nvidia.com/3dvision") Then
                        Try
                            deletesubregkey(regkey, child)
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        End If


        If IntPtr.Size = 8 Then
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\MozillaPlugins", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("nvidia.com/3dvision") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        End If


        '-----------------------
        'remove event view stuff
        '-----------------------
        log("Remove eventviewer stuff")

        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
        If subregkey IsNot Nothing Then
            For Each child2 As String In subregkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child2) = False Then
                    If child2.ToLower.Contains("controlset") Then
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog\Application", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If child.ToLower.StartsWith("nvidia update") Or
                                        (child.ToLower.StartsWith("nvstreamsvc") AndAlso removegfe) Or
                                        child.ToLower.StartsWith("nvidia opengl driver") Or
                                        child.ToLower.StartsWith("nvwmi") Or
                                        child.ToLower.StartsWith("nview") Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            log(ex.Message + ex.StackTrace)
                                        End Try
                                    End If
                                End If
                            Next
                        End If
                    End If
                End If
            Next
        End If

        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
        If subregkey IsNot Nothing Then
            For Each child2 As String In subregkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child2) = False Then
                    If child2.ToLower.Contains("controlset") Then
                        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Services\eventlog\System", True)
                        If regkey IsNot Nothing Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If child.ToLower.StartsWith("nvidia update") Or
                                        child.ToLower.StartsWith("nvidia opengl driver") Or
                                        child.ToLower.StartsWith("nvwmi") Or
                                        child.ToLower.StartsWith("nview") Then
                                        deletesubregkey(regkey, child)
                                    End If
                                End If
                            Next
                        End If
                    End If
                End If
            Next
        End If

        log("End Remove eventviewer stuff")
        '---------------------------
        'end remove event view stuff
        '---------------------------

        '---------------------------
        'virtual store
        '---------------------------

        regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
        If regkey IsNot Nothing Then
            Try
                deletesubregkey(regkey, "Global")
            Catch ex As Exception
            End Try
            If regkey.SubKeyCount = 0 Then
                Try
                    deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("VirtualStore\MACHINE\SOFTWARE", True), "NVIDIA Corporation")
                Catch ex As Exception
                End Try
            End If
        End If

        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then
                    regkey = My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True)
                    If regkey IsNot Nothing Then
                        Try
                            deletesubregkey(regkey, "Global")
                        Catch ex As Exception
                        End Try
                        If regkey.SubKeyCount = 0 Then
                            Try
                                deletesubregkey(My.Computer.Registry.Users.OpenSubKey(users & "\Software\Classes\VirtualStore\MACHINE\SOFTWARE", True), "NVIDIA Corporation")
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        Try
            For Each child As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(child) Then
                    If child.ToLower.Contains("s-1-5") Then
                        Try
                            deletesubregkey(My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", True), "Global")
                            If My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE\NVIDIA Corporation", False).SubKeyCount = 0 Then
                                deletesubregkey(My.Computer.Registry.Users.OpenSubKey(child & "Software\Classes\VirtualStore\MACHINE\SOFTWARE", True), "NVIDIA Corporation")
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
                    If removegfe Then
                        deletevalue(regkey, "Nvtmru")
                    End If
                Catch ex As Exception
                    log(ex.Message + " Nvtmru")
                End Try

                Try
                    deletevalue(regkey, "NvCplDaemon")
                Catch ex As Exception
                    log(ex.Message + " NvCplDaemon")
                End Try

                Try
                    deletevalue(regkey, "NvMediaCenter")
                Catch ex As Exception
                    log(ex.Message + " NvMediaCenter")
                End Try

                Try
                    If removegfe Then
                        deletevalue(regkey, "NvBackend")
                    End If
                Catch ex As Exception
                End Try

                Try
                    deletevalue(regkey, "nwiz")
                Catch ex As Exception
                End Try

                Try
                    If removegfe Then
                        deletevalue(regkey, "ShadowPlay")
                    End If
                Catch ex As Exception
                    log(ex.Message + " ShadowPlay")
                End Try

                Try
                    deletevalue(regkey, "StereoLinksInstall")
                Catch ex As Exception
                    log(ex.Message + " StereoLinksInstall")
                End Try
                Try
                    If removegfe Then
                        deletevalue(regkey, "NvGameMonitor")
                    End If
                Catch ex As Exception
                    log(ex.Message + " NvGameMonitor")
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
                        deletevalue(regkey, "StereoLinksInstall")
                    Catch ex As Exception
                        log(ex.Message + " StereoLinksInstall")
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.installer(IO.File.ReadAllLines(Application.StartupPath & "\settings\NVIDIA\packages.cfg"))


        If remove3dtvplay Then
            Try
                deletesubregkey(My.Computer.Registry.ClassesRoot, "mpegfile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
            Try
                deletesubregkey(My.Computer.Registry.ClassesRoot, "WMVFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
            Try
                deletesubregkey(My.Computer.Registry.ClassesRoot, "AVIFile\shellex\ContextMenuHandlers\NvPlayOnMyTV")
            Catch ex As Exception
            End Try
        End If

        '-----------------------------
        'Shell extensions\aproved
        '-----------------------------
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Or
                           regkey.GetValue(child).ToString.ToLower.Contains("nview desktop context menu") Or
                           regkey.GetValue(child).ToString.ToLower.Contains("nvappshext extension") Or
                           regkey.GetValue(child).ToString.ToLower.Contains("openglshext extension") Or
                           regkey.GetValue(child).ToString.ToLower.Contains("nvidia play on my tv context menu extension") Then
                            Try
                                deletevalue(regkey, child)
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
            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Controls Folder\" &
                                                         "Display\shellex\PropertySheetHandlers", True), "NVIDIA CPL Extension")
        Catch ex As Exception
        End Try

        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Extended Properties", False)
        If regkey IsNot Nothing Then
            For Each child As String In regkey.GetSubKeyNames()
                If checkvariables.isnullorwhitespace(child) = False Then
                    For Each childs As String In regkey.OpenSubKey(child).GetValueNames()
                        If Not checkvariables.isnullorwhitespace(childs) Then
                            If childs.ToLower.Contains("nvcpl.cpl") Then
                                Try
                                    deletevalue(regkey.OpenSubKey(child, True), childs)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Next
        End If


        If IntPtr.Size = 8 Then

            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetValueNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If regkey.GetValue(child).ToString.ToLower.Contains("nvcpl desktopcontext class") Then
                            Try
                                deletevalue(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If

        End If
        '-----------------------------
        'End Shell extensions\aprouved
        '-----------------------------

        'Shell ext
        Try
            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Directory\background\shellex\ContextMenuHandlers", True), "NvCplDesktopContext")
        Catch ex As Exception
        End Try

        Try
            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Directory\background\shellex\ContextMenuHandlers", True), "00nView")
        Catch ex As Exception
        End Try

        Try
            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Directory\background\shellex\ContextMenuHandlers", True), "NvCplDesktopContext")
        Catch ex As Exception
        End Try

        Try
            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Directory\background\shellex\ContextMenuHandlers", True), "00nView")
        Catch ex As Exception
        End Try

        'Cleaning of some "open with application" related to 3d vision
        regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("jpsfile\shell\open\command", True)
        If regkey IsNot Nothing Then
            If (Not checkvariables.isnullorwhitespace(CType(regkey.GetValue(""), String))) AndAlso regkey.GetValue("").ToString.ToLower.Contains _
                ("nvstview") Then
                Try
                    deletesubregkey(My.Computer.Registry.ClassesRoot, "jpsfile")
                Catch ex As Exception
                End Try
            End If
        End If
        regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("mpofile\shell\open\command", True)
        If regkey IsNot Nothing Then
            If (Not checkvariables.isnullorwhitespace(CStr(regkey.GetValue("")))) AndAlso regkey.GetValue("").ToString.ToLower.Contains _
                ("nvstview") Then
                Try
                    deletesubregkey(My.Computer.Registry.ClassesRoot, "mpofile")
                Catch ex As Exception
                End Try
            End If
        End If

        regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("pnsfile\shell\open\command", True)
        If regkey IsNot Nothing Then
            If (Not checkvariables.isnullorwhitespace(CStr(regkey.GetValue("")))) AndAlso regkey.GetValue("").ToString.ToLower.Contains _
                ("nvstview") Then
                Try
                    deletesubregkey(My.Computer.Registry.ClassesRoot, "pnsfile")
                Catch ex As Exception
                End Try
            End If
        End If

        Try
            deletesubregkey(My.Computer.Registry.ClassesRoot, ".tvp")  'CrazY_Milojko
        Catch ex As Exception
        End Try

        UpdateTextMethod("-End of Registry Cleaning")

        log("End of Registry Cleaning")

    End Sub

    Private Sub cleanintelfolders()

        Dim filePath As String = Nothing

        UpdateTextMethod(UpdateTextMethodmessagefn(4))

        log("Cleaning Directory")

        CleanupEngine.folderscleanup(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\driverfiles.cfg")) '// add each line as String Array.

        filePath = System.Environment.SystemDirectory
        Dim files() As String = IO.Directory.GetFiles(filePath + "\", "igfxcoin*.*")
        For i As Integer = 0 To files.Length - 1
            If Not checkvariables.isnullorwhitespace(files(i)) Then
                Try
                    deletefile(files(i))
                Catch ex As Exception
                End Try
            End If
        Next

    End Sub

    Private Sub cleanintelserviceprocess()

        CleanupEngine.cleanserviceprocess(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\services.cfg")) '// add each line as String Array.

        Dim appproc = Process.GetProcessesByName("IGFXEM")
        For i As Integer = 0 To appproc.Length - 1
            appproc(i).Kill()
        Next i

    End Sub

    Private Sub cleanintel()

        Dim regkey As RegistryKey = Nothing
        Dim subregkey As RegistryKey = Nothing
        Dim wantedvalue As String = Nothing
        Dim packages As String()

        UpdateTextMethod(UpdateTextMethodmessagefn(5))

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
                        If child.ToLower.Contains("igfx") Or
                           child.ToLower.Contains("mediasdk") Or
                           child.ToLower.Contains("opencl") Or
                           child.ToLower.Contains("intel wireless display") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software", True), "Intel")
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
                                        deletesubregkey(regkey, child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next
                        If regkey.SubKeyCount = 0 Then
                            Try
                                deletesubregkey(My.Computer.Registry.Users.OpenSubKey(users & "\Software", True), "Intel")
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
                            If child.ToLower.Contains("igfx") Or
                               child.ToLower.Contains("mediasdk") Or
                               child.ToLower.Contains("opencl") Or
                               child.ToLower.Contains("intel wireless display") Then
                                Try
                                    deletesubregkey(regkey, child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                    If regkey.SubKeyCount = 0 Then
                        Try
                            deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node", True), "Intel")
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
                    deletevalue(regkey, "IgfxTray")
                Catch ex As Exception
                    log(ex.Message + " IgfxTray")
                End Try

                Try
                    deletevalue(regkey, "Persistence")
                Catch ex As Exception
                    log(ex.Message + " Persistence")
                End Try

                Try
                    deletevalue(regkey, "HotKeysCmds")
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
                        If child.ToLower.Contains("igfxcui") Or
                           child.ToLower.Contains("igfxosp") Or
                            child.ToLower.Contains("igfxdtcm") Then

                            deletesubregkey(regkey, child)

                        End If
                    End If

                Next
            End If
        Catch ex As Exception
            log(ex.StackTrace)
        End Try

        CleanupEngine.installer(IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\packages.cfg"))

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
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
                                    wantedvalue = subregkey.GetValue("DisplayName").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To packages.Length - 1
                                            If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                If wantedvalue.ToLower.Contains(packages(i).ToLower) Then
                                                    Try
                                                        deletesubregkey(regkey, child)
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
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cpls", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        If child.ToLower.Contains("igfxcpl") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        'Special Cleanup For Intel PnpResources
        Try
            If win8higher Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR", True)
                If regkey IsNot Nothing Then
                    Dim classroot As String() = IO.File.ReadAllLines(Application.StartupPath & "\settings\INTEL\classroot.cfg")
                    For Each child As String In regkey.GetSubKeyNames()
                        If Not checkvariables.isnullorwhitespace(child) Then
                            For i As Integer = 0 To classroot.Length - 1
                                If Not checkvariables.isnullorwhitespace(classroot(i)) Then
                                    If child.ToLower.Contains(classroot(i).ToLower) Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        If child.ToLower.Contains("igfx") Then
                            Try
                                deletesubregkey(regkey, child)
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
                If regkey.SubKeyCount = 0 Then
                    Try
                        deletesubregkey(My.Computer.Registry.LocalMachine, "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify")
                    Catch ex As Exception
                    End Try
                End If
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        UpdateTextMethod(UpdateTextMethodmessagefn(6))
    End Sub

    Private Sub checkpcieroot()  'This is for Nvidia Optimus to prevent the yellow mark on the PCI-E controler. We must remove the UpperFilters.

        Dim regkey As RegistryKey = Nothing
        Dim subregkey As RegistryKey = Nothing
        Dim array() As String

        UpdateTextMethod(UpdateTextMethodmessagefn(7))

        log("Starting the removal of nVidia Optimus UpperFilter if present.")

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
                  ("SYSTEM\CurrentControlSet\Enum\PCI")
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If Not checkvariables.isnullorwhitespace(child) Then
                        If child.ToLower.Contains("ven_8086") Then
                            subregkey = regkey.OpenSubKey(child)
                            If subregkey IsNot Nothing Then
                                For Each childs As String In subregkey.GetSubKeyNames()
                                    If checkvariables.isnullorwhitespace(childs) = False Then
                                        array = CType(subregkey.OpenSubKey(childs).GetValue("UpperFilters"), String())
                                        If (array IsNot Nothing) AndAlso (Not array.Length < 1) Then
                                            For i As Integer = 0 To array.Length - 1
                                                If Not checkvariables.isnullorwhitespace(array(i)) Then
                                                    log("UpperFilter found : " + array(i))
                                                    If (array(i).ToLower.Contains("nvpciflt")) Then
                                                        Dim AList As ArrayList = New ArrayList(array)

                                                        AList.Remove("nvpciflt")
                                                        AList.Remove("nvkflt")

                                                        log("nVidia Optimus UpperFilter Found.")
                                                        Dim upfiler As String() = CType(AList.ToArray(GetType(String)), String())

                                                        Try

                                                            deletevalue(subregkey.OpenSubKey(childs, True), "UpperFilters")
                                                            If (upfiler IsNot Nothing) AndAlso (Not upfiler.Length < 1) Then
                                                                subregkey.OpenSubKey(childs, True).SetValue("UpperFilters", upfiler, RegistryValueKind.MultiString)
                                                            End If
                                                        Catch ex As Exception
                                                            log(ex.Message + ex.StackTrace)
                                                            log("Failed to fix Optimus. You will have to manually remove the device with yellow mark in device manager to fix the missing videocard")
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
        Catch ex As Exception
			MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            log(ex.Message + ex.StackTrace)
        End Try
    End Sub

    Private Sub restartcomputer()

        log("Restarting Computer ")
        processinfo.FileName = "shutdown"
        processinfo.Arguments = "/r /t 0"
        processinfo.WindowStyle = ProcessWindowStyle.Hidden
        processinfo.UseShellExecute = True
        processinfo.CreateNoWindow = True
        processinfo.RedirectStandardOutput = False

        process.StartInfo = processinfo
        process.Start()
        process.WaitForExit()
        process.Close()
        closeddu()

    End Sub

    Private Sub shutdowncomputer()
        preventclose = False
        processinfo.FileName = "shutdown"
        processinfo.Arguments = "/s /t 0"
        processinfo.WindowStyle = ProcessWindowStyle.Hidden
        processinfo.UseShellExecute = True
        processinfo.CreateNoWindow = True
        processinfo.RedirectStandardOutput = False

        process.StartInfo = processinfo
        process.Start()
        process.WaitForExit()
        process.Close()
        closeddu()

    End Sub

    Private Sub rescan()

        'Scan for new devices...
        Dim scan As New ProcessStartInfo
        scan.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
        scan.Arguments = "rescan"
        scan.UseShellExecute = False
        scan.CreateNoWindow = True
        scan.RedirectStandardOutput = False


        UpdateTextMethod(UpdateTextMethodmessagefn(8))
        log("Scanning for new device...")
        Dim proc4 As New Process
        proc4.StartInfo = scan
        proc4.Start()
        proc4.WaitForExit()
        proc4.Close()
        System.Threading.Thread.Sleep(2000)
        If Not safemode Then
            Dim appproc = process.GetProcessesByName("explorer")
            For i As Integer = 0 To appproc.Length - 1
                appproc(i).Kill()
            Next i
        End If


    End Sub

    Private Function winupdatepending() As Boolean
        Dim regkey As RegistryKey = Nothing
        regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
        If regkey IsNot Nothing Then
            Return True
        Else
            Return False
        End If
    End Function

	Private Sub gpuidentify(ByVal gpu As String)

		Dim regkey As RegistryKey = Nothing
		Dim subregkey As RegistryKey = Nothing
		Dim array() As String

		Try
			regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")

			For Each child As String In regkey.GetSubKeyNames
				If Not checkvariables.isnullorwhitespace(child) Then
					If child.ToLower.Contains(gpu) Then

						subregkey = regkey.OpenSubKey(child)
						For Each child2 As String In subregkey.GetSubKeyNames
							array = CType(subregkey.OpenSubKey(child2).GetValue("CompatibleIDs"), String())
							If (array IsNot Nothing) AndAlso (Not (array.Length < 1)) Then
								For i As Integer = 0 To array.Length - 1
									If array(i).ToLower.Contains("pci\cc_03") Then
										For j As Integer = 0 To array.Length - 1
											If array(j).ToLower.Contains("ven_8086") Then
												ComboBox1.SelectedIndex = 2
												PictureBox2.Location = New Point(picturebox2originalx, picturebox2originaly)
												PictureBox2.Size = New Size(158, 126)
											End If
											If array(j).ToLower.Contains("ven_1002") Then
												ComboBox1.SelectedIndex = 1
												PictureBox2.Location = New Point(picturebox2originalx, picturebox2originaly)
												PictureBox2.Size = New Size(158, 126)
											End If
											If array(j).ToLower.Contains("ven_10de") Then
												ComboBox1.SelectedIndex = 0
												PictureBox2.Location = New Point(CInt(286 * (picturebox2originalx / 333)), CInt(92 * (picturebox2originaly / 92)))
												PictureBox2.Size = New Size(252, 123)
											End If
										Next
									End If
								Next
							End If
						Next
					End If
				End If
			Next
		Catch ex As Exception
			MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			log(ex.Message + ex.StackTrace)
		End Try
	End Sub

	Private Sub restartinsafemode(Optional ByVal withNetwork As Boolean = False)

		Dim regkey As RegistryKey = Nothing

		systemrestore()	'we try to do a system restore if allowed before going into safemode.
		log("restarting in safemode")


		Me.TopMost = False

		Dim setbootconf As New ProcessStartInfo("bcdedit")

		If withNetwork Then
			setbootconf.Arguments = "/set safeboot network"
		Else
			setbootconf.Arguments = "/set safeboot minimal"
		End If

		setbootconf.UseShellExecute = False
		setbootconf.CreateNoWindow = True
		setbootconf.RedirectStandardOutput = False

		Dim processstopservice As New Process
		processstopservice.StartInfo = setbootconf
		processstopservice.Start()
		processstopservice.WaitForExit()
		processstopservice.Close()

		Try
			regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", True)

			If regkey IsNot Nothing Then
				'Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
				'sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34) + " " + arg)
				'sw.Flush()
				'sw.Close()
				settings.setconfig("arguments", arg)
				regkey.SetValue("*" + Application.ProductName, Application.ExecutablePath)
                regkey.SetValue("*UndoSM", "BCDEDIT /deletevalue safeboot")
			End If
		Catch ex As Exception
			log(ex.Message & ex.StackTrace)
		End Try


		processinfo.FileName = "shutdown"
		processinfo.Arguments = "/r /t 0"
		processinfo.WindowStyle = ProcessWindowStyle.Hidden
		processinfo.UseShellExecute = True
		processinfo.CreateNoWindow = True
		processinfo.RedirectStandardOutput = False

		process.StartInfo = processinfo
		process.Start()
		process.WaitForExit()
		process.Close()

		closeddu()
	End Sub

	Private Sub closeddu()

		If Me.InvokeRequired Then
			Me.Invoke(New MethodInvoker(AddressOf Me.closeddu))
		Else
			Try
				preventclose = False

				Me.Close()

			Catch ex As Exception
				log(ex.Message + ex.StackTrace)
			End Try
		End If
	End Sub

#Region "frmMain Controls"

	Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

		If Not CBool(settings.getconfig("goodsite")) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			settings.setconfig("goodsite", "True")
		End If

		disabledriversearch()
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = True
		combobox1value = ComboBox1.Text

		BackgroundWorker1.RunWorkerAsync()
	End Sub

	Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
		If Not CBool(settings.getconfig("goodsite")) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			settings.setconfig("goodsite", "True")
		End If
		disabledriversearch()
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = False
		shutdown = False
		combobox1value = ComboBox1.Text
		BackgroundWorker1.RunWorkerAsync()

	End Sub

	Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
		If Not CBool(settings.getconfig("goodsite")) Then
			MessageBox.Show("A simple 1 time message.... For helping DDU developpement, please always download DDU from its homepage http://www.wagnardmobile.com it really help and will encourage me to continue developping DDU. In the event there is a problem with the main page, feel free to use the Guru3d mirror.")
			settings.setconfig("goodsite", "True")
		End If
		disabledriversearch()
		'kill processes that read GPU stats, like RTSS, MSI Afterburner, EVGA Prec X to prevent invalid readings
		KillGPUStatsProcesses()
		'this shouldn't be slow, so it isn't on a thread/background worker

		reboot = False
		shutdown = True
		combobox1value = ComboBox1.Text
		BackgroundWorker1.RunWorkerAsync()
	End Sub

	Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

		Dim regkey As RegistryKey = Nothing

		If version >= "6.1" Then
			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
				regkey.SetValue("SearchOrderConfig", 1)
				MsgBox(Language.GetTranslation("frmMain", "Messages", "Text11"))
			Catch ex As Exception
				log(ex.Message + ex.StackTrace)
			End Try
		End If
		If version >= "6.0" And version < "6.1" Then
			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
				regkey.SetValue("DontSearchWindowsUpdate", 0)
				MsgBox(Language.GetTranslation("frmMain", "Messages", "Text11"))
			Catch ex As Exception
				log(ex.Message + ex.StackTrace)
			End Try
		End If
	End Sub

	Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
		Using frm As New frmLog
			frm.ShowDialog(Me)
		End Using
	End Sub



	Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
		combobox1value = ComboBox1.Text
		If combobox1value = "NVIDIA" Then

			PictureBox2.Location = New Point(CInt(286 * (picturebox2originalx / 333)), CInt(92 * (picturebox2originaly / 92)))
			PictureBox2.Size = New Size(252, 123)
			PictureBox2.Image = My.Resources.NV_GF_GTX_preferred_badge_FOR_WEB_ONLY
		End If

		If combobox1value = "AMD" Then


			PictureBox2.Location = New Point(picturebox2originalx, picturebox2originaly)
			PictureBox2.Size = New Size(158, 126)
			PictureBox2.Image = My.Resources.RadeonLogo1
		End If

		If combobox1value = "INTEL" Then

			PictureBox2.Location = New Point(picturebox2originalx, picturebox2originaly)
			PictureBox2.Size = New Size(158, 126)
			PictureBox2.Image = My.Resources.intel_logo
		End If

	End Sub

	Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
		If ComboBox2.SelectedItem IsNot Nothing Then
			InitLanguage(False, CType(ComboBox2.SelectedItem, Language.LanguageOption))
        End If
        Checkupdates2()
	End Sub

	Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
		settings.setconfig("donate", "true")

		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub



	Private Sub ToSToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToSToolStripMenuItem.Click
		MessageBox.Show(Language.GetTranslation("Misc", "Tos", "Text"))
	End Sub

	Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
		Using frm As New frmAbout
			frm.ShowDialog(Me)
		End Using
	End Sub

	Private Sub VisitGuru3dNVIDIAThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dNVIDIAThreadToolStripMenuItem.Click

		settings.setconfig("guru3dnvidia", "true")

		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub

	Private Sub VisitGuru3dAMDThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGuru3dAMDThreadToolStripMenuItem.Click

		settings.setconfig("guru3damd", "true")

		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub

	Private Sub VisitGeforceThreadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitGeforceThreadToolStripMenuItem.Click

		settings.setconfig("geforce", "true")

		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub

	Private Sub SVNToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SVNToolStripMenuItem.Click

		settings.setconfig("svn", "true")

		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub

	Private Sub VisitDDUHomepageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VisitDDUHomepageToolStripMenuItem.Click

		settings.setconfig("dduhome", "true")


		'Create the ddu.bat file
		Dim sw As StreamWriter = System.IO.File.CreateText(Application.StartupPath + "\DDU.bat")
		sw.WriteLine(Chr(34) + Application.StartupPath + "\" + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + Chr(34))
		sw.Flush()
		sw.Close()

		Dim UserTokenHandle As IntPtr = IntPtr.Zero
		WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId, UserTokenHandle)
		Dim ProcInfo As New WindowsApi.PROCESS_INFORMATION
		Dim StartInfo As New WindowsApi.STARTUPINFOW
		StartInfo.cb = CUInt(Runtime.InteropServices.Marshal.SizeOf(StartInfo))

		If WindowsApi.CreateProcessAsUser(UserTokenHandle, Application.StartupPath + "\DDU.bat", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, False, 0, IntPtr.Zero, Nothing, StartInfo, ProcInfo) Then
		Else
			MsgBox("Error ---" & System.Runtime.InteropServices.Marshal.GetLastWin32Error())
		End If

		If Not UserTokenHandle = IntPtr.Zero Then
			WindowsApi.CloseHandle(UserTokenHandle)
		End If
	End Sub

	Private Sub OptionsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OptionsToolStripMenuItem.Click
		frmOptions.Show()
		frmOptions.TopMost = True
	End Sub

	Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
		If Not preventclose Then
			Me.Close()
		End If
	End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem3.Click
        If Not preventclose Then
            Checkupdates2()
        End If
    End Sub

	Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
		If Not silent Then
			Me.WindowState = FormWindowState.Normal
		End If
	End Sub



	Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'We try to create config.cfg if non existant.
        If roamingcfg = True Then
            My.Computer.FileSystem.CreateDirectory(userpthn & "\Display Driver Uninstaller")
        End If

        If Not System.IO.File.Exists(Application.StartupPath & "\settings\config.cfg") Then
            If Not System.IO.File.Exists(userpthn & "\Display Driver Uninstaller\config.cfg") Then
                myExe = Application.StartupPath & "\settings\config.cfg"
                System.IO.File.WriteAllBytes(myExe, My.Resources.config)
            Else
                System.IO.File.Copy(userpthn & "\Display Driver Uninstaller\config.cfg", Application.StartupPath & "\settings\config.cfg") 'this is a really bad lazy fix, and will be improved upon later.
            End If
        End If

        InitLanguage(True)

		If Not donotcheckupdatestartup Then
			Me.TopMost = True
			Checkupdates2()

			Me.TopMost = False
			If closeapp Then
				Exit Sub
			End If
		End If

		Dim regkey As RegistryKey = Nothing
		Dim subregkey As RegistryKey = Nothing

		CheckForIllegalCrossThreadCalls = True



		Dim webAddress As String = ""





		'we check if the donate/guru3dnvidia/gugu3damd/geforce/dduhome is trigger here directly.
		If CBool(settings.getconfig("donate")) = True Then
			webAddress = "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=KAQAJ6TNR9GQE&lc=CA&item_name=Display%20Driver%20Uninstaller%20%28DDU%29&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted"
		End If

		If CBool(settings.getconfig("guru3dnvidia")) = True Then
			webAddress = "http://forums.guru3d.com/showthread.php?t=379506"
		End If

		If CBool(settings.getconfig("guru3damd")) = True Then

			webAddress = "http://forums.guru3d.com/showthread.php?t=379505"
		End If

		If CBool(settings.getconfig("geforce")) = True Then

			webAddress = "https://forums.geforce.com/default/topic/550192/geforce-drivers/wagnard-tools-ddu-gmp-tdr-manupulator-updated-01-22-2015-/"
		End If

		If CBool(settings.getconfig("dduhome")) = True Then
			webAddress = "http://www.wagnardmobile.com"
		End If

		If CBool(settings.getconfig("svn")) = True Then
			webAddress = "https://github.com/Wagnard/display-drivers-uninstaller"
		End If

		If CBool(settings.getconfig("donate")) = True Or
		   CBool(settings.getconfig("guru3dnvidia")) = True Or
		   CBool(settings.getconfig("guru3damd")) = True Or
		   CBool(settings.getconfig("geforce")) = True Or
		   CBool(settings.getconfig("svn")) = True Or
		   CBool(settings.getconfig("dduhome")) = True Then

			processinfo.FileName = webAddress
			processinfo.Arguments = Nothing
			processinfo.UseShellExecute = True
			processinfo.CreateNoWindow = True
			processinfo.RedirectStandardOutput = False

			process.StartInfo = processinfo
			process.Start()
			'Do not put WaitForExit here. It will cause error and prevent DDU to exit.
			process.Close()

			settings.setconfig("donate", "false")
			settings.setconfig("guru3dnvidia", "false")
			settings.setconfig("guru3damd", "false")
			settings.setconfig("geforce", "false")
			settings.setconfig("dduhome", "false")
			settings.setconfig("svn", "false")

			closeddu()
			Exit Sub
		End If


		If Not isElevated Then
			MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text3"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			closeddu()
			Exit Sub
		End If

		'moved this log code up higher so the directory is created sooner to avoid potential issues
		If Not My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\DDU Logs") Then
			My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\DDU Logs")
		End If

		'second, we check on what we are running and set variables accordingly (os, architecture)

		If Not checkvariables.isnullorwhitespace(CStr(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False).GetValue("CurrentVersion"))) Then
			version = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False).GetValue("CurrentVersion").ToString

		Else
			version = "5.0"
		End If

		Select Case version

			Case "5.1"
				Label2.Text = "Windows XP or Server 2003"
				winxp = True
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case "5.2"
				winxp = True
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True
			Case "6.0"
				Label2.Text = "Windows Vista or Server 2008"
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case "6.1"
				Label2.Text = "Windows 7 or Server 2008r2"
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case "6.2"
				Label2.Text = "Windows 8 or Server 2012"
				win8higher = True
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case "6.3"
				Label2.Text = "Windows 8.1"
				If Not checkvariables.isnullorwhitespace(CStr(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False).GetValue("CurrentMajorVersionNumber"))) Then
					If CStr(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False).GetValue("CurrentMajorVersionNumber")) = "10" Then
						Label2.Text = "Windows 10"
						win10 = True
					End If
				End If
				win8higher = True
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case "6.4", "10.0"
				Label2.Text = "Windows 10"
				win8higher = True
				win10 = True
				Button1.Enabled = True
				Button2.Enabled = True
				Button3.Enabled = True
				Button4.Enabled = True

			Case Else
				Label2.Text = "Unsupported OS"
				log("Unsupported OS.")
				Button1.Enabled = False
				Button2.Enabled = False
				Button3.Enabled = False
				Button4.Enabled = False
		End Select




		Try
			'We try to create config.cfg if non existant.
			If Not System.IO.File.Exists(Application.StartupPath & "\settings\config.cfg") Then
				myExe = Application.StartupPath & "\settings\config.cfg"
				System.IO.File.WriteAllBytes(myExe, My.Resources.config)
			End If

			picturebox2originalx = PictureBox2.Location.X
			picturebox2originaly = PictureBox2.Location.Y


			'allow Paexec to run in safemode

			'  If BootMode.FailSafe Or BootMode.FailSafeWithNetwork Then ' we do this in safemode because of some Antivirus....(Kaspersky)
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
			'End If

			'read config file

			If settings.getconfig("logbox") = "true" Then
				f.CheckBox2.Checked = True

			Else
				f.CheckBox2.Checked = False
			End If

			If settings.getconfig("roamingcfg") = "true" Then
				f.CheckBox11.Checked = True
				roamingcfg = True
			Else
				f.CheckBox11.Checked = False
				roamingcfg = False
			End If

			If settings.getconfig("remove3dtvplay") = "true" Then
				f.CheckBox4.Checked = True
				remove3dtvplay = True
			Else
				f.CheckBox4.Checked = False
				remove3dtvplay = False
			End If

			If settings.getconfig("systemrestore") = "true" Then
				f.CheckBox5.Checked = True
				trysystemrestore = True
			Else
				f.CheckBox5.Checked = False
				trysystemrestore = False
			End If

			If settings.getconfig("removephysx") = "true" Then
				f.CheckBox3.Checked = True
				removephysx = True
			Else
				f.CheckBox3.Checked = False
				removephysx = False
			End If


			If settings.getconfig("removeamdaudiobus") = "true" Then
				f.CheckBox7.Checked = True
				removeamdaudiobus = True
			Else
				f.CheckBox7.Checked = False
				removeamdaudiobus = False
			End If

			If settings.getconfig("removeamdkmpfd") = "true" Then
				f.CheckBox9.Checked = True
				removeamdkmpfd = True
			Else
				f.CheckBox9.Checked = False
				removeamdkmpfd = False
			End If


			If settings.getconfig("removemonitor") = "true" Then
				f.CheckBox6.Checked = True
				removemonitor = True
			Else
				f.CheckBox6.Checked = False
				removemonitor = False
			End If

			If settings.getconfig("removecnvidia") = "true" Then
				f.CheckBox1.Checked = True
				removecnvidia = True
			Else
				f.CheckBox1.Checked = False
				removecnvidia = False
			End If

			If settings.getconfig("removecamd") = "true" Then
				f.CheckBox8.Checked = True
				removecamd = True
			Else
				f.CheckBox8.Checked = False
				removecamd = False
			End If

			If settings.getconfig("removegfe") = "true" Then
				f.CheckBox13.Checked = True
				removegfe = True
			Else
				f.CheckBox13.Checked = False
				removegfe = False
			End If

			If settings.getconfig("donotcheckupdatestartup") = "true" Then
				f.CheckBox12.Checked = True
				donotcheckupdatestartup = True
			Else
				f.CheckBox12.Checked = False
				donotcheckupdatestartup = False
			End If

			If settings.getconfig("showsafemodebox") = "false" Then
				f.CheckBox10.Checked = False
				safemodemb = False
			Else
				f.CheckBox10.Checked = True
				safemodemb = True
			End If

			If settings.getconfig("removedxcache") = "true" Then
				f.CheckBox14.Checked = True
				removedxcache = True
			Else
				f.CheckBox14.Checked = False
				removedxcache = False
			End If

			If closeapp Then
				Exit Sub
			End If


			'----------------------
			'check computer/os info
			'----------------------

			Dim arch As Boolean


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

			'Verifying if we are on X86 or x64

			If arch = True Then
				Label3.Text = "x64"
			Else
				Label3.Text = "x86"
			End If
			Label3.Refresh()
			ddudrfolder = Label3.Text

			If arch = True Then
				Try

					If winxp Then  'XP64
						myExe = Application.StartupPath & "\x64\ddudr.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp64)
						myExe = Application.StartupPath & "\x64\paexec.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
					Else

						myExe = Application.StartupPath & "\x64\ddudr.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr64)

						myExe = Application.StartupPath & "\x64\paexec.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
					End If

				Catch ex As Exception
				End Try
			Else
				Try
					If winxp Then  'XP32
						myExe = Application.StartupPath & "\x86\ddudr.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.ddudrxp32)

						myExe = Application.StartupPath & "\x86\paexec.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)
					Else 'all other 32 bits
						myExe = Application.StartupPath & "\x86\ddudr.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.ddudr32)

						myExe = Application.StartupPath & "\x86\paexec.exe"
						System.IO.File.WriteAllBytes(myExe, My.Resources.paexec)

					End If

				Catch ex As Exception
				End Try
			End If

			If arch = True Then
				If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x64\ddudr.exe") Then
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text4"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					Button1.Enabled = False
					Button2.Enabled = False
					Button3.Enabled = False
					Exit Sub
				End If
			ElseIf arch = False Then
				If Not My.Computer.FileSystem.FileExists(Application.StartupPath & "\x86\ddudr.exe") Then
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text4"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					Button1.Enabled = False
					Button2.Enabled = False
					Button3.Enabled = False
					Exit Sub
				End If
			End If

			'processing arguments

			arg = String.Join(" ", arguments, 1, arguments.Length - 1)
			arg = arg.ToLower.Replace("  ", " ")

			If Not checkvariables.isnullorwhitespace(settings.getconfig("arguments")) Then
				arg = settings.getconfig("arguments")
			End If

			settings.setconfig("arguments", "")

			If Not checkvariables.isnullorwhitespace(arg) Then
				If Not arg = " " Then
					settings.setconfig("logbox", "false")
					settings.setconfig("systemrestore", "false")
					settings.setconfig("removemonitor", "false")
					settings.setconfig("showsafemodebox", "true")
					settings.setconfig("removeamdaudiobus", "false")
					settings.setconfig("removeamdkmpfd", "false")
					settings.setconfig("removegfe", "false")

					If arg.Contains("-silent") Then
						silent = True
						Me.WindowState = FormWindowState.Minimized
					Else
						Checkupdates2()
						If closeapp Then
							Exit Sub
						End If
					End If


					If arg.Contains("-logging") Then
						settings.setconfig("logbox", "true")
					End If
					If arg.Contains("-createsystemrestorepoint") Then
						settings.setconfig("systemrestore", "true")
					End If
					If arg.Contains("-removemonitors") Then
						settings.setconfig("removemonitor", "true")
					End If
					If arg.Contains("-nosafemode") Then
						settings.setconfig("showsafemodebox", "false")
					End If
					If arg.Contains("-restart") Then
						restart = True
					End If
					If arg.Contains("-removeamdaudiobus") Then
						settings.setconfig("removeamdaudiobus", "true")
					End If
					If arg.Contains("-removeamdkmpfd") Then
						settings.setconfig("removeamdkmpfd", "true")
					End If
					If arg.Contains("-removegfe") Then
						settings.setconfig("removegfe", "true")
					End If
					If arg.Contains("-cleanamd") Then
						argcleanamd = True
						nbclean = nbclean + 1
					End If
					If arg.Contains("-cleanintel") Then
						argcleanintel = True
						nbclean = nbclean + 1
					End If
					If arg.Contains("-cleannvidia") Then
						argcleannvidia = True
						nbclean = nbclean + 1
					End If
				End If
			End If


			'We check if there are any reboot from windows update pending. and if so we quit.
			If winupdatepending() Then
				MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text14"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
				closeddu()
				Exit Sub
			End If

			Me.TopMost = True
			NotifyIcon1.Visible = True



			'here I check if the process is running on system user account. if not, make it so.
			If Not MyIdentity.IsSystem Then
				'This code checks to see which mode Windows has booted up in.
				Dim processstopservice As New Process
				Select Case System.Windows.Forms.SystemInformation.BootMode
                    Case BootMode.FailSafeWithNetwork, BootMode.FailSafe 'Merged both boot options (same code 1:1) (WARNING, we cannot use "OR" with "Case", we must use a ",")
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

                            processstopservice.StartInfo = setbcdedit
                            processstopservice.Start()
                            processstopservice.WaitForExit()
                            processstopservice.Close()
                            MsgBox("Reached")
                        End If
					Case BootMode.Normal
						safemode = False

						If winxp = False And isElevated Then 'added iselevated so this will not try to boot into safe mode/boot menu without admin rights, as even with the admin check on startup it was for some reason still trying to gain registry access and throwing an exception --probably because there's no return
							If restart Then	 'restart command line argument
								restartinsafemode()
								Exit Sub
							Else
								If safemodemb = True Then
									If Not silent Then
										Dim bootOption As Integer = -1 '-1 = close, 0 = normal, 1 = SafeMode, 2 = SafeMode with network

										Using frmSafeBoot As New frmLaunch
											frmSafeBoot.TopMost = True

											If frmSafeBoot.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
												bootOption = frmSafeBoot.selection
											Else
												bootOption = -1
											End If
										End Using

										Select Case bootOption
											Case 0 'normal
												Exit Select
											Case 1 'SafeMode
												restartinsafemode(False)
												Exit Sub
											Case 2 'SafeMode with network
												restartinsafemode(True)
												Exit Sub
											Case Else '-1 = Close
												Me.TopMost = False
												closeddu()
												Exit Sub
										End Select
										' Dim resultmsgbox As Integer = MessageBox.Show(msgboxmessagefn(11), "Safe Mode?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information)

										'If resultmsgbox = DialogResult.Cancel Then
										'
										' Me.TopMost = False
										'closeddu()
										'Exit Sub
										' ElseIf resultmsgbox = DialogResult.No Then
										'do nothing and continue without safe mode
										'ElseIf resultmsgbox = DialogResult.Yes Then
										'restartinsafemode(False)
										' Exit Sub
										' End If
									End If
								End If
							End If
						Else
							MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text8"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
						End If

				End Select
				TopMost = False



				Dim stopservice As New ProcessStartInfo
				stopservice.FileName = "cmd.exe"
				stopservice.Arguments = " /Csc stop PAExec"
				stopservice.UseShellExecute = False
				stopservice.CreateNoWindow = True
				stopservice.RedirectStandardOutput = False
				processstopservice.StartInfo = stopservice
				processstopservice.Start()
				processstopservice.WaitForExit()
				processstopservice.Close()
				System.Threading.Thread.Sleep(10)

				stopservice.Arguments = " /Csc delete PAExec"

				processstopservice.StartInfo = stopservice
				processstopservice.Start()
				processstopservice.WaitForExit()
				processstopservice.Close()

				stopservice.Arguments = " /Csc interrogate PAExec"
				processstopservice.StartInfo = stopservice
				processstopservice.Start()
				processstopservice.WaitForExit()
				processstopservice.Close()

				processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\paexec.exe"
				processinfo.Arguments = "-noname -i -s " & Chr(34) & Application.StartupPath & "\" & System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" & Chr(34) + arg
				processinfo.UseShellExecute = False
				processinfo.CreateNoWindow = True
				processinfo.RedirectStandardOutput = False

				process.StartInfo = processinfo
				process.Start()
				'Do not add waitforexit here or DDU(current user)will not close
				process.Close()

				closeddu()
				Exit Sub
			End If

			UpdateTextMethod(UpdateTextMethodmessagefn(10) + Application.ProductVersion)
			log("DDU Version: " + Application.ProductVersion)
			log("OS: " + Label2.Text)
			log("Architecture: " & ddudrfolder)

			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If Not checkvariables.isnullorwhitespace(child) Then

							If Not child.ToLower.Contains("properties") Then

								subregkey = regkey.OpenSubKey(child)
								If subregkey IsNot Nothing Then

									If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("Device Description"))) Then
										currentdriverversion = subregkey.GetValue("Device Description").ToString
										UpdateTextMethod(UpdateTextMethodmessagefn(11) + " " + child + " " + UpdateTextMethodmessagefn(12) + " " + currentdriverversion)
										log("GPU #" + child + " Detected : " + currentdriverversion)
									Else
										If (subregkey.GetValue("DriverDesc") IsNot Nothing) AndAlso (subregkey.GetValueKind("DriverDesc") = RegistryValueKind.Binary) Then
											UpdateTextMethod(UpdateTextMethodmessagefn(11) + " " + child + " " + UpdateTextMethodmessagefn(12) + " " + HexToString(GetREG_BINARY(subregkey.ToString, "DriverDesc").Replace("00", "")))
											log("GPU #" + child + " Detected :  " + HexToString(GetREG_BINARY(subregkey.ToString, "DriverDesc").Replace("00", "")))
										Else
											If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DriverDesc"))) Then
												currentdriverversion = subregkey.GetValue("DriverDesc").ToString
												UpdateTextMethod(UpdateTextMethodmessagefn(11) + " " + child + " " + UpdateTextMethodmessagefn(12) + " " + currentdriverversion)
												log("GPU #" + child + " Detected : " + currentdriverversion)
											End If
										End If

									End If
									If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("MatchingDeviceId"))) Then
										currentdriverversion = subregkey.GetValue("MatchingDeviceId").ToString
										UpdateTextMethod(UpdateTextMethodmessagefn(13) + " " + currentdriverversion)
										log("GPU DeviceId : " + currentdriverversion)
									End If

									Try
										If (Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("HardwareInformation.BiosString")))) AndAlso (subregkey.GetValueKind("HardwareInformation.BiosString") = RegistryValueKind.Binary) Then
											UpdateTextMethod("Vbios :" + " " + HexToString(GetREG_BINARY(subregkey.ToString, "HardwareInformation.BiosString").Replace("00", "")))
											log("Vbios :" + HexToString(GetREG_BINARY(subregkey.ToString, "HardwareInformation.BiosString").Replace("00", "")))
										Else
											If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("HardwareInformation.BiosString"))) Then
												currentdriverversion = subregkey.GetValue("HardwareInformation.BiosString").ToString
												For i As Integer = 0 To 9
													'this is a little fix to correctly show the vbios version info
													currentdriverversion = currentdriverversion.Replace("." + i.ToString + ".", ".0" + i.ToString + ".")
												Next
												UpdateTextMethod("Vbios :" + " " + currentdriverversion)
												log("Vbios : " + currentdriverversion)
											End If
										End If
									Catch ex As Exception
									End Try


									If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DriverVersion"))) Then
										currentdriverversion = subregkey.GetValue("DriverVersion").ToString
										UpdateTextMethod(UpdateTextMethodmessagefn(14) + " " + currentdriverversion)
										log("Detected Driver(s) Version(s) : " + currentdriverversion)
									End If
									If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("InfPath"))) Then
										currentdriverversion = subregkey.GetValue("InfPath").ToString
										UpdateTextMethod(UpdateTextMethodmessagefn(15) + " " + currentdriverversion)
										log("INF : " + currentdriverversion)
									End If
									If Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("InfSection"))) Then
										currentdriverversion = subregkey.GetValue("InfSection").ToString
										UpdateTextMethod(UpdateTextMethodmessagefn(16) + " " + currentdriverversion)
										log("INF Section : " + currentdriverversion)
									End If
								End If
								UpdateTextMethod("--------------")
								log("--------------")
							End If
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

			gpuidentify("ven_8086")
			gpuidentify("ven_1002")
			gpuidentify("ven_10de")


			' -------------------------------------
			' Check if this is an AMD Enduro system
			' -------------------------------------
			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")

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
										If Not checkvariables.isnullorwhitespace(CStr(subregkey.OpenSubKey(childs).GetValue("Service"))) Then
											If subregkey.OpenSubKey(childs).GetValue("Service").ToString.ToLower.Contains("amdkmdap") Then
												enduro = True
												UpdateTextMethod("System seems to be an AMD Enduro (Intel)")
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


			If MyIdentity.IsSystem Then
				Select Case System.Windows.Forms.SystemInformation.BootMode
					Case BootMode.FailSafe
						log("We are in Safe Mode")
					Case BootMode.FailSafeWithNetwork
						log("We are in Safe Mode with Networking")
					Case BootMode.Normal
						log("We are not in Safe Mode")
				End Select
			End If


			getoeminfo()

		Catch ex As Exception
			MsgBox(ex.Message + ex.StackTrace)
			log(ex.Message + ex.StackTrace)
			closeddu()
			Exit Sub
		End Try

		NotifyIcon1.Text = "DDU Version: " + Application.ProductVersion
		TopMost = False

		If argcleanamd Or argcleannvidia Or argcleanintel Or restart Or silent Then
			trd = New Thread(AddressOf ThreadTask)
			trd.IsBackground = True
			trd.Start()
		End If
	End Sub

	Private Sub Form1_Shown(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Shown
		If silent Then
			Me.Hide()
		End If
	End Sub

	Private Sub Form1_Close(sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing

		If preventclose Then
			e.Cancel = True
			Exit Sub
		End If

		If MyIdentity.IsSystem AndAlso safemode Then
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



	Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object,
	 ByVal e As System.ComponentModel.DoWorkEventArgs) _
	 Handles BackgroundWorker1.DoWork

		Dim regkey As RegistryKey = Nothing
		Dim subregkey As RegistryKey = Nothing
		Dim array() As String

		UpdateTextMethod(UpdateTextMethodmessagefn(19))

		preventclose = True
		Invoke(Sub() Button1.Enabled = False)
		Invoke(Sub() Button2.Enabled = False)
		Invoke(Sub() Button3.Enabled = False)
		Invoke(Sub() ComboBox1.Enabled = False)
		Invoke(Sub() MenuStrip1.Enabled = False)


		Try
			systemrestore()

			If combobox1value = "AMD" Then
				vendidexpected = "VEN_1002"
				provider = "AdvancedMicroDevices"
			End If

			If combobox1value = "NVIDIA" Then
				vendidexpected = "VEN_10DE"
				provider = "NVIDIA"
			End If

			If combobox1value = "INTEL" Then
				vendidexpected = "VEN_8086"
				provider = "Intel"
			End If

			UpdateTextMethod(UpdateTextMethodmessagefn(20) + " " & combobox1value & " " + UpdateTextMethodmessagefn(21))
			log("Uninstalling " + combobox1value + " driver ...")
			UpdateTextMethod(UpdateTextMethodmessagefn(22))

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
						Dim removed As Boolean = False
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
													removed = False
													If checkvariables.isnullorwhitespace(child2) = False Then
														If child2.ToLower.Contains("ven_1002") Then
															For Each child3 As String In subregkey.OpenSubKey(child2).GetSubKeyNames()
																If checkvariables.isnullorwhitespace(child3) = False Then
																	array = CType(subregkey.OpenSubKey(child2 & "\" & child3).GetValue("LowerFilters"), String())
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
																					'process.WaitForExit()
																					process.StandardOutput.Close()
																					process.Close()
																					log(reply2)
																					log("AMD HD Audio Bus Removed !")
																					removed = True
																				End If
																			End If
																		Next
																	End If
																	If removed Then
																		Exit For
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
							If Not checkvariables.isnullorwhitespace(child2) AndAlso child2.ToLower.Contains("ven_1002") Then
								For Each child3 As String In subregkey.OpenSubKey(child2).GetSubKeyNames()
									If checkvariables.isnullorwhitespace(child3) = False Then
										array = CType(subregkey.OpenSubKey(child2 & "\" & child3).GetValue("LowerFilters"), String())
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
						Next
					End If
				Catch ex As Exception
					log(ex.Message + ex.StackTrace)
				End Try

			End If



			' ----------------------
			' Removing the videocard
			' ----------------------
			For a = 1 To 2	 'loop 2 time here for nVidia SLI pupose in normal mode.(4 may be necessary for quad SLI... need to check.)
				Try
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\PCI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not checkvariables.isnullorwhitespace(child) AndAlso
							 (child.ToLower.Contains("ven_10de") Or
							 child.ToLower.Contains("ven_8086") Or
							 child.ToLower.Contains("ven_1002")) Then

								subregkey = regkey.OpenSubKey(child)
								If subregkey IsNot Nothing Then

									For Each child2 As String In subregkey.GetSubKeyNames

										If subregkey.OpenSubKey(child2) Is Nothing Then
											Continue For
										End If

										array = CType(subregkey.OpenSubKey(child2).GetValue("CompatibleIDs"), String())

										If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
											For i As Integer = 0 To array.Length - 1

												If Not checkvariables.isnullorwhitespace(array(i)) AndAlso array(i).ToLower.Contains("pci\cc_03") Then

													vendid = child & "\" & child2

													If vendid.ToLower.Contains(vendidexpected.ToLower) Then
														processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
														processinfo.Arguments = "remove " & Chr(34) & "@pci\" & vendid & Chr(34)
														processinfo.UseShellExecute = False
														processinfo.CreateNoWindow = True
														processinfo.RedirectStandardOutput = True
														process.StartInfo = processinfo

														process.Start()
														reply2 = process.StandardOutput.ReadToEnd
														process.StandardOutput.Close()
														process.Close()
														'process.WaitForExit()
														log(reply2)
													End If
													Exit For   'the card is removed so we exit the loop from here.
												End If
											Next
										End If
									Next
								End If
							End If
						Next
					End If
				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try
			Next

			UpdateTextMethod(UpdateTextMethodmessagefn(23))
			log("DDUDR Remove Display Driver: Complete.")

			cleandriverstore()

			UpdateTextMethod(UpdateTextMethodmessagefn(24))
			log("Executing DDUDR Remove Audio controler.")

			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\HDAUDIO")
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames
						If Not checkvariables.isnullorwhitespace(child) AndAlso
						   (child.ToLower.Contains("ven_10de") Or
						   child.ToLower.Contains("ven_8086") Or
						   child.ToLower.Contains("ven_1002")) Then

							subregkey = regkey.OpenSubKey(child)
							If subregkey IsNot Nothing Then

								For Each child2 As String In subregkey.GetSubKeyNames

									If subregkey.OpenSubKey(child2) Is Nothing Then
										Continue For
									End If

									vendid = child & "\" & child2

									If vendid.ToLower.Contains(vendidexpected.ToLower) Then
										processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
										processinfo.Arguments = "remove " & Chr(34) & "@HDAUDIO\" & vendid & Chr(34)
										processinfo.UseShellExecute = False
										processinfo.CreateNoWindow = True
										processinfo.RedirectStandardOutput = True
										process.StartInfo = processinfo

										process.Start()
										reply2 = process.StandardOutput.ReadToEnd
										process.StandardOutput.Close()
										process.Close()
										'process.WaitForExit()
										log(reply2)

									End If
								Next
							End If
						End If
					Next
				End If
			Catch ex As Exception
				MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
				log(ex.Message + ex.StackTrace)
			End Try

			UpdateTextMethod(UpdateTextMethodmessagefn(25))


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
					process.StandardOutput.Close()
					process.Close()
					'process.WaitForExit()

					Try
						card1 = reply.IndexOf("USB\")
					Catch ex As Exception
					End Try

					While card1 > -1

						position2 = reply.IndexOf(":", card1)
						vendid = reply.Substring(card1, position2 - card1).Trim
						If vendid.Contains("USB\VID_0955&PID_0007") Or
						 vendid.Contains("USB\VID_0955&PID_7001") Or
						 vendid.Contains("USB\VID_0955&PID_7002") Or
						 vendid.Contains("USB\VID_0955&PID_7003") Or
						 vendid.Contains("USB\VID_0955&PID_7004") Or
						 vendid.Contains("USB\VID_0955&PID_7008") Or
						 vendid.Contains("USB\VID_0955&PID_7009") Or
						 vendid.Contains("USB\VID_0955&PID_700A") Or
						 vendid.Contains("USB\VID_0955&PID_700C") Or
						 vendid.Contains("USB\VID_0955&PID_700D&MI_00") Or
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
							process.StandardOutput.Close()
							process.Close()
							'process.WaitForExit()
							log(reply2)


						End If
						card1 = reply.IndexOf("USB\", card1 + 1)

					End While

				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try

				UpdateTextMethod(UpdateTextMethodmessagefn(26))

				Try
					'removing NVIDIA SHIELD Wireless Controller Trackpad
					processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
					processinfo.Arguments = "findall =MOUSE"
					processinfo.UseShellExecute = False
					processinfo.CreateNoWindow = True
					processinfo.RedirectStandardOutput = True

					'creation dun process fantome pour le wait on exit.

					process.StartInfo = processinfo
					process.Start()
					reply = process.StandardOutput.ReadToEnd
					process.StandardOutput.Close()
					process.Close()
					'process.WaitForExit()

					Try
						card1 = reply.IndexOf("HID\")
					Catch ex As Exception
					End Try

					While card1 > -1

						position2 = reply.IndexOf(":", card1)
						vendid = reply.Substring(card1, position2 - card1).Trim
						If vendid.ToLower.Contains("hid\vid_0955&pid_7210") Then
							log("-" & vendid & "- NVIDIA SHIELD Wireless Controller Trackpad found")

							processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
							processinfo.Arguments = "remove =MOUSE " & Chr(34) & vendid & Chr(34)
							processinfo.UseShellExecute = False
							processinfo.CreateNoWindow = True
							processinfo.RedirectStandardOutput = True
							process.StartInfo = processinfo

							process.Start()
							reply2 = process.StandardOutput.ReadToEnd
							process.StandardOutput.Close()
							process.Close()
							'process.WaitForExit()
							log(reply2)


						End If
						card1 = reply.IndexOf("HID\", card1 + 1)

					End While

				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try

				'Removing NVIDIA Virtual Audio Device (Wave Extensible) (WDM)
				If removegfe Then

					Try
						regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ROOT")
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetSubKeyNames
								If Not checkvariables.isnullorwhitespace(child) Then

									subregkey = regkey.OpenSubKey(child)
									If subregkey IsNot Nothing Then

										For Each child2 As String In subregkey.GetSubKeyNames
											If Not checkvariables.isnullorwhitespace(child2) Then
												If subregkey.OpenSubKey(child2) Is Nothing Then
													Continue For
												End If

												If Not checkvariables.isnullorwhitespace(CStr(subregkey.OpenSubKey(child2).GetValue("DeviceDesc"))) AndAlso
												   subregkey.OpenSubKey(child2).GetValue("DeviceDesc").ToString.ToLower.Contains("nvidia virtual audio device") Then

													vendid = child & "\" & child2

													processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
													processinfo.Arguments = "remove " & Chr(34) & "@ROOT\" & vendid & Chr(34)
													processinfo.UseShellExecute = False
													processinfo.CreateNoWindow = True
													processinfo.RedirectStandardOutput = True
													process.StartInfo = processinfo

													process.Start()
													reply2 = process.StandardOutput.ReadToEnd
													process.StandardOutput.Close()
													process.Close()
													'process.WaitForExit()
													log(reply2)

												End If
											End If
										Next
									End If
								End If
							Next
						End If
					Catch ex As Exception
						MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
						log(ex.Message + ex.StackTrace)
					End Try

				End If
				' ------------------------------
				' Removing nVidia AudioEndpoints
				' ------------------------------

				log("Removing nVidia Audio Endpoints")

				Try
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not checkvariables.isnullorwhitespace(child) Then

								If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
								   (regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("nvidia virtual audio device") AndAlso removegfe) Or
								   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("nvidia high definition audio") Then

									vendid = child

									processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
									processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
									processinfo.UseShellExecute = False
									processinfo.CreateNoWindow = True
									processinfo.RedirectStandardOutput = True
									process.StartInfo = processinfo

									process.Start()
									reply2 = process.StandardOutput.ReadToEnd
									process.StandardOutput.Close()
									process.Close()
									'process.WaitForExit()
									log(reply2)

								End If
							End If
						Next
					End If
				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try

			End If

			If combobox1value = "AMD" Then
				' ------------------------------
				' Removing some of AMD AudioEndpoints
				' ------------------------------

				log("Removing AMD Audio Endpoints")

				Try
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not checkvariables.isnullorwhitespace(child) Then

								If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
								   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("amd high definition audio device") Or
								   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("digital audio (hdmi) (high definition audio device)") Then

									vendid = child

									processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
									processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
									processinfo.UseShellExecute = False
									processinfo.CreateNoWindow = True
									processinfo.RedirectStandardOutput = True
									process.StartInfo = processinfo

									process.Start()
									reply2 = process.StandardOutput.ReadToEnd
									process.StandardOutput.Close()
									process.Close()
									'process.WaitForExit()
									log(reply2)

								End If
							End If
						Next
					End If
				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
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
				process.StandardOutput.Close()
				process.Close()
				'process.WaitForExit()
				log(reply2)


				' ------------------------------
				' Removing Intel AudioEndpoints
				' ------------------------------
				log("Removing Intel Audio Endpoints")
				Try
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\SWD\MMDEVAPI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not checkvariables.isnullorwhitespace(child) Then

								If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("FriendlyName"))) AndAlso
								   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("intel widi") Or
								   regkey.OpenSubKey(child).GetValue("FriendlyName").ToString.ToLower.Contains("intel(r)") Then

									vendid = child

									processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
									processinfo.Arguments = "remove " & Chr(34) & "@SWD\MMDEVAPI\" & vendid & Chr(34)
									processinfo.UseShellExecute = False
									processinfo.CreateNoWindow = True
									processinfo.RedirectStandardOutput = True
									process.StartInfo = processinfo

									process.Start()
									reply2 = process.StandardOutput.ReadToEnd
									process.StandardOutput.Close()
									process.Close()
									'process.WaitForExit()
									log(reply2)

								End If
							End If
						Next
					End If
				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try
			End If


			log("ddudr Remove Audio/HDMI Complete")
			'removing monitor and hidden monitor



			If removemonitor Then
				log("ddudr Remove Monitor started")
				Try
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\DISPLAY")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames
							If Not checkvariables.isnullorwhitespace(child) Then

								subregkey = regkey.OpenSubKey(child)
								If subregkey IsNot Nothing Then

									For Each child2 As String In subregkey.GetSubKeyNames
										If Not checkvariables.isnullorwhitespace(child2) Then

											If subregkey.OpenSubKey(child2) Is Nothing Then
												Continue For
											End If

											vendid = child & "\" & child2


											processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
											processinfo.Arguments = "remove " & Chr(34) & "@DISPLAY\" & vendid & Chr(34)
											processinfo.UseShellExecute = False
											processinfo.CreateNoWindow = True
											processinfo.RedirectStandardOutput = True
											process.StartInfo = processinfo

											process.Start()
											reply2 = process.StandardOutput.ReadToEnd
											process.StandardOutput.Close()
											process.Close()
											'process.WaitForExit()
										End If
									Next
								End If
							End If
						Next
					End If
				Catch ex As Exception
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
					log(ex.Message + ex.StackTrace)
				End Try

				UpdateTextMethod(UpdateTextMethodmessagefn(27))
			End If
			UpdateTextMethod(UpdateTextMethodmessagefn(28))

			'here we set back to default the changes made by the AMDKMPFD even if we are cleaning amd or intel. We dont what that
			'espcially if we are not using an AMD GPU

			If removeamdkmpfd Then
				Try
					log("Checking and Removing AMDKMPFD Filter if present")
					regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ACPI")
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If checkvariables.isnullorwhitespace(child) = False Then
								If child.ToLower.Contains("pnp0a08") Or
								   child.ToLower.Contains("pnp0a03") Then
									subregkey = regkey.OpenSubKey(child)
									If subregkey IsNot Nothing Then
										For Each child2 As String In subregkey.GetSubKeyNames()
											If Not checkvariables.isnullorwhitespace(child2) Then
												array = CType(subregkey.OpenSubKey(child2).GetValue("LowerFilters"), String())
												If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
													For i As Integer = 0 To array.Length - 1
														If Not checkvariables.isnullorwhitespace(array(i)) Then
															If array(i).ToLower.Contains("amdkmpfd") Then
																log("Found an AMDKMPFD! in " + child)
																Try
																	log("array result: " + array(i))
																Catch ex As Exception
																End Try
																processinfo.FileName = Application.StartupPath & "\" & ddudrfolder & "\ddudr.exe"
																If win10 Then
																	processinfo.Arguments = "update " & windir & "\inf\pci.inf " & Chr(34) & "*" & child & Chr(34)
																Else
																	processinfo.Arguments = "update " & windir & "\inf\machine.inf " & Chr(34) & "*" & child & Chr(34)
																End If
																processinfo.UseShellExecute = False
																processinfo.CreateNoWindow = True
																processinfo.RedirectStandardOutput = True
																process.StartInfo = processinfo

																process.Start()
																reply2 = process.StandardOutput.ReadToEnd
																'process.WaitForExit()
																process.StandardOutput.Close()
																process.Close()
																log(reply2)
																log(child + " Restored.")

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
					log(ex.Message + ex.StackTrace)
				End Try

				'We now try to remove the service AMDPMPFD if its lowerfilter is not found
				If reboot Or shutdown Then
					If Not checkamdkmapfd() Then
						CleanupEngine.cleanserviceprocess({"amdkmpfd"})
					End If
				End If
			End If

			If combobox1value = "AMD" Then
				cleanamdserviceprocess()
				cleanamd()

				If System.Windows.Forms.SystemInformation.BootMode = BootMode.Normal Then
					log("Killing Explorer.exe")
					Dim appproc = process.GetProcessesByName("explorer")
					For i As Integer = 0 To appproc.Length - 1
						appproc(i).Kill()
					Next i
				End If

				cleanamdfolders()
			End If

			If combobox1value = "NVIDIA" Then
				cleannvidiaserviceprocess()
				cleannvidia()

				If System.Windows.Forms.SystemInformation.BootMode = BootMode.Normal Then
					log("Killing Explorer.exe")
					Dim appproc = process.GetProcessesByName("explorer")
					For i As Integer = 0 To appproc.Length - 1
						appproc(i).Kill()
					Next i
				End If


				cleannvidiafolders()
				checkpcieroot()
			End If

			If combobox1value = "INTEL" Then
				cleanintelserviceprocess()
				cleanintel()

				If System.Windows.Forms.SystemInformation.BootMode = BootMode.Normal Then
					log("Killing Explorer.exe")
					Dim appproc = process.GetProcessesByName("explorer")
					For i As Integer = 0 To appproc.Length - 1
						appproc(i).Kill()
					Next i
				End If

				cleanintelfolders()
			End If

			cleandriverstore()
			fixregistrydriverstore()
			'rebuildcountercache()
		Catch ex As Exception
			log(ex.Message & ex.StackTrace)
			MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text6"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			stopme = True
		End Try

	End Sub

	Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As System.Object,
	ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) _
	Handles BackgroundWorker1.RunWorkerCompleted
		Try

			If stopme = True Then
				'Scan for new hardware to not let users into a non working state.

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
				proc4.Close()
				'then quit
				closeddu()
				Exit Sub
			End If


			'For command line arguement to know if there is more cleans to be done.

			preventclose = False
			backgroundworkcomplete = True

			UpdateTextMethod(UpdateTextMethodmessagefn(9))

			log("Clean uninstall completed!")

			If Not shutdown Then
				rescan()
			End If

			Invoke(Sub() Button1.Enabled = True)
			Invoke(Sub() Button2.Enabled = True)
			Invoke(Sub() Button3.Enabled = True)
			Invoke(Sub() ComboBox1.Enabled = True)
			Invoke(Sub() MenuStrip1.Enabled = True)

			If nbclean < 2 And Not silent And Not reboot And Not shutdown Then
				If MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text10"), Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Information) = Windows.Forms.DialogResult.Yes Then
					closeddu()
					Exit Sub
				End If
			End If

			If reboot Then
				restartcomputer()
			End If

			If shutdown Then
				shutdowncomputer()
			End If

		Catch ex As Exception
			preventclose = False
			log(ex.Message + ex.StackTrace)
		End Try
	End Sub

#End Region

	Private Sub ThreadTask()

		Try
			If argcleanamd Then
				backgroundworkcomplete = False
				cleananddonothing("AMD")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If argcleannvidia Then
				backgroundworkcomplete = False
				cleananddonothing("NVIDIA")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If argcleanintel Then
				backgroundworkcomplete = False
				cleananddonothing("INTEL")
			End If

			Do Until backgroundworkcomplete
				System.Threading.Thread.Sleep(10)
			Loop

			If restart Then
				log("Restarting Computer ")
				processinfo.FileName = "shutdown"
				processinfo.Arguments = "/r /t 0"
				processinfo.WindowStyle = ProcessWindowStyle.Hidden
				processinfo.UseShellExecute = True
				processinfo.CreateNoWindow = True
				processinfo.RedirectStandardOutput = False

				process.StartInfo = processinfo
				process.Start()
				process.WaitForExit()
				process.Close()

				closeddu()
				Exit Sub
			End If

			If silent And (Not restart) Then
				closeddu()
			End If
		Catch ex As Exception
			log(ex.Message + ex.StackTrace)
		End Try
	End Sub

	Sub systemrestore()
		'THIS NEEDS TO BE FIXED!!! DOES NOT WORK WITH OPTION STRICT ON. I WAS UNABLE TO FIGURE OUT MY SELF. BE SURE TO FIX BEFORE RELEASE.

		If trysystemrestore Then
			Try
				UpdateTextMethod("Creating System Restore point (If allowed by the system)")
				log("Trying to Create a System Restored Point")
				Dim oScope As New ManagementScope("\\localhost\root\default")
				Dim oPath As New ManagementPath("SystemRestore")
				Dim oGetOp As New ObjectGetOptions()
				Dim oProcess As New ManagementClass(oScope, oPath, oGetOp)

				Dim oInParams As ManagementBaseObject = oProcess.GetMethodParameters("CreateRestorePoint")
				oInParams("Description") = "DDU System Restored Point"
				oInParams("RestorePointType") = 12 ' MODIFY_SETTINGS
				oInParams("EventType") = 100

				Dim oOutParams As ManagementBaseObject = oProcess.InvokeMethod("CreateRestorePoint", oInParams, Nothing)

				log("System Restored Point Created. code: " + CStr(oOutParams("ReturnValue")))
			Catch ex As Exception
				log("System Restored Point Could not be Created! Err Code: 0x" & Hex(Err.Number))
			End Try

		End If
		'     If trysystemrestore Then
		'     Select Case System.Windows.Forms.SystemInformation.BootMode
		'     Case BootMode.Normal
		'     If f.CheckBox5.Checked = True Then
		'     UpdateTextMethod("Creating System Restore point (If allowed by the system)")
		'     Try
		'     log("Trying to Create a System Restored Point")
		'     Dim SysterRestoredPoint As Object = GetObject("winmgmts:\\.\root\default:Systemrestore")
		'     If SysterRestoredPoint IsNot Nothing Then
		'     If SysterRestoredPoint.CreateRestorePoint("DDU System Restored Point", 0, 100) = 0 Then
		'     log("System Restored Point Created")
		'     Else
		'     log("System Restored Point Could not Created!")
		'     End If
		'     End If
		'
		'        Catch ex As Exception
		'        log(ex.Message)
		'        End Try
		'        End If
		'        End Select
		'        End If
	End Sub

	Sub getoeminfo()

		log("The following third-party driver packages are installed on this computer: ")
		Dim infisvalid As Boolean = True
		Try
			For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")
				If Not checkvariables.isnullorwhitespace(infs) Then

					log("---")
					log(infs)
					infisvalid = False 'false unless we find either a provider or class 
					For Each child As String In IO.File.ReadAllLines(infs)
						If Not checkvariables.isnullorwhitespace(child) Then
							child = child.Replace(" ", "").Replace(vbTab, "")

							If Not checkvariables.isnullorwhitespace(child) AndAlso child.ToLower.StartsWith("provider=") Then
								infisvalid = True
								If child.EndsWith("%") Then
									For Each provider As String In IO.File.ReadAllLines(infs)
										If Not checkvariables.isnullorwhitespace(provider) Then
											provider = provider.Replace(" ", "").Replace(vbTab, "")
											If Not checkvariables.isnullorwhitespace(provider) AndAlso provider.ToLower.StartsWith(child.ToLower.Replace("provider=", "").Replace("%", "") + "=") AndAlso
											   Not provider.Contains("%") Then
												log(provider.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("provider=", "").Replace("%", "") + "=", "Provider="))
												Exit For
											End If
										End If
									Next
									Exit For
								End If
								log(child)
								Exit For
							End If
						End If
					Next

					For Each child As String In IO.File.ReadAllLines(infs)
						If Not checkvariables.isnullorwhitespace(child) Then

							child = child.Replace(" ", "").Replace(vbTab, "")

							If Not checkvariables.isnullorwhitespace(child) AndAlso child.ToLower.StartsWith("class=") Then
								infisvalid = True
								If child.EndsWith("%") Then
									For Each provider As String In IO.File.ReadAllLines(infs)
										If Not checkvariables.isnullorwhitespace(provider) Then
											provider = provider.Replace(" ", "").Replace(vbTab, "")
											If Not checkvariables.isnullorwhitespace(provider) AndAlso provider.ToLower.StartsWith(child.ToLower.Replace("class=", "").Replace("%", "") + "=") AndAlso
											   Not provider.Contains("%") Then
												log(provider.ToLower.Replace(Chr(34), "").Replace(child.ToLower.Replace("class=", "").Replace("%", "") + "=", "Class="))
												Exit For
											End If
										End If
									Next
									Exit For
								End If
								log(child)
								Exit For
							End If
						End If
					Next
					If Not infisvalid Then
						log("This inf entry is corrupted or invalid.")
						deletefile(infs)
					End If
				End If
			Next
		Catch ex As Exception
			log(ex.Message + ex.StackTrace)
		End Try

	End Sub

	Public Sub TestDelete(ByVal folder As String)
		' UpdateTextMethod(UpdateTextMethodmessagefn("18"))
		'log("Deleting some specials folders, it could take some times...")
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
				If (removephysx Or Not ((Not removephysx) And diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

					Try
						TraverseDirectory(diChild)
					Catch ex As Exception
						log(ex.Message + ex.StackTrace)
					End Try
				End If
			Next
		Catch ex As Exception
			log("test delete : " + ex.Message)
		End Try
		'Finally, clean all of the files directly in the root directory
		CleanAllFilesInDirectory(di)

		'The containing directory can only be deleted if the directory
		'is now completely empty and all files previously within
		'were deleted.
		Try
			If di.GetFiles().Length = 0 And Directory.GetDirectories(folder).Length = 0 Then
				di.Delete()
				log(di.ToString + " - " + "Folder removed via testdelete sub")
			End If
		Catch ex As Exception
			log("testdelete @ di.getfiles() : " + ex.Message)
		End Try
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
			If (removephysx Or Not ((Not removephysx) And diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

				Try
					TraverseDirectory(diChild)
				Catch ex As Exception
					log(ex.Message + ex.StackTrace)
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
				log(ex.Message + ex.StackTrace)
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
			log("cleanallfilesindi : " + ex.Message)
		End Try
	End Sub

	Private Sub KillP(processname As String)
		Dim processList() As Process
		processList = process.GetProcessesByName(processname)

		For Each proc As Process In processList
			Try
				proc.Kill()
			Catch ex As Exception
				log("!! ERROR !! Failed to kill process(es): " & ex.Message)
			End Try
		Next
	End Sub

	Private Sub KillGPUStatsProcesses()
		KillP("MSIAfterburner")
		KillP("PrecisionX_x64")	' Not sure for the x86 one...      Shady: probably the same but without _x64, and a few sites seem to confirm this, doesn't hurt to just add it anyway
		KillP("PrecisionXServer_x64")
		KillP("PrecisionXServer")
		KillP("PrecisionX")
		KillP("RTSS")
		KillP("RTSSHooksLoader64")
		KillP("EncoderServer64")
		KillP("RTSSHooksLoader")
		KillP("EncoderServer")
		KillP("nvidiaInspector")
	End Sub

	Private Sub cleananddonothing(ByVal gpu As String)
		reboot = False
		shutdown = False
		Invoke(Sub() ComboBox1.Text = gpu)
		BackgroundWorker1.RunWorkerAsync()

	End Sub

	Private Sub cleanandandreboot(ByVal gpu As String)
		reboot = True
		shutdown = False
		Invoke(Sub() ComboBox1.Text = gpu)
		BackgroundWorker1.RunWorkerAsync()

	End Sub

	Public Sub log(ByVal strmessage As String)
		Try
			If f.CheckBox2.Checked = True Then
				Using wlog As New IO.StreamWriter(locations, True)
					wlog.WriteLine(DateTime.Now & " >> " & strmessage)

					UpdateTextMethod2(strmessage)

					wlog.Flush()
					wlog.Close()
				End Using 'End using always calls .Dispose() 
				'  System.Threading.Thread.Sleep(10)  '20 millisecond stall (0.02 Seconds) just to be sure its really released.
			End If
		Catch ex As Exception

		End Try
	End Sub

	Private Sub disabledriversearch()
		Dim regkey As RegistryKey = Nothing
		log("Trying to disable search for Windows Updates :")
		log("Version " + version + " detected")

		If version >= "6.1" Then
			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", True)
				If CInt(regkey.GetValue("SearchOrderConfig").ToString) <> 0 Then
					regkey.SetValue("SearchOrderConfig", 0)
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text9"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
				End If
			Catch ex As Exception
				log(ex.Message + ex.StackTrace)
			End Try
		End If

		If version >= "6.0" And version < "6.1" Then
			Try
				regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Policies\Microsoft\Windows\DriverSearching", True)
				If CInt(regkey.GetValue("DontSearchWindowsUpdate").ToString) <> 1 Then
					regkey.SetValue("DontSearchWindowsUpdate", 1)
					MessageBox.Show(Language.GetTranslation(Me.Name, "Messages", "Text9"), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information)
				End If
			Catch ex As Exception
				log(ex.Message + ex.StackTrace)
			End Try
		End If
	End Sub

	Private Function checkamdkmapfd() As Boolean

		Dim regkey As RegistryKey = Nothing
		Dim subregkey As RegistryKey = Nothing
		Dim array As String() = Nothing
		Dim iskmpfdpresent As Boolean = False

		Try
			log("Checking if AMDKMPFD is present before Service removal")
			regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\ACPI")
			If regkey IsNot Nothing Then
				For Each child As String In regkey.GetSubKeyNames()
					If checkvariables.isnullorwhitespace(child) = False Then
						If child.ToLower.Contains("pnp0a08") Or
						   child.ToLower.Contains("pnp0a03") Then
							subregkey = regkey.OpenSubKey(child)
							If subregkey IsNot Nothing Then
								For Each child2 As String In subregkey.GetSubKeyNames()
									If Not checkvariables.isnullorwhitespace(child2) Then
										array = CType(subregkey.OpenSubKey(child2).GetValue("LowerFilters"), String())
										If (array IsNot Nothing) AndAlso Not (array.Length < 1) Then
											For i As Integer = 0 To array.Length - 1
												If Not checkvariables.isnullorwhitespace(array(i)) Then
													If array(i).ToLower.Contains("amdkmpfd") Then
														log("Found an AMDKMPFD! in " + child)
														log("We do not remove the AMDKMPFP service yet")
														iskmpfdpresent = True

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
			log(ex.Message + ex.StackTrace)
		End Try
		If iskmpfdpresent Then
			Return True
		Else
			Return False
		End If

	End Function

	Private Sub InitLanguage(ByVal firstLaunch As Boolean, Optional ByVal changeTo As Language.LanguageOption = Nothing)
		toolTip1.AutoPopDelay = 3000
		toolTip1.InitialDelay = 1000
		toolTip1.ReshowDelay = 250
		toolTip1.ShowAlways = True

		If firstLaunch Then
			ComboBox2.Items.Clear()

			If Directory.Exists(String.Concat(Application.StartupPath, "\settings\languages\")) Then
				ComboBox2.Items.AddRange(Language.ScanFolderForLang(Application.StartupPath + "\settings\languages\").ToArray())
			Else
				Directory.CreateDirectory(String.Concat(Application.StartupPath, "\settings\languages\"))
			End If

			Dim defaultLang As New Language.LanguageOption("en", "English", Application.StartupPath + "\settings\languages\English.xml")
			ComboBox2.Items.Add(defaultLang)

			Using sr As New StreamReader(Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("{0}.{1}", GetType(Language).Namespace, "English.xml")), Encoding.UTF8, True)
				Using sw As New StreamWriter(defaultLang.Filename, False, Encoding.UTF8)
					While (sr.Peek() <> -1)
						sw.WriteLine(sr.ReadLine())
					End While

					sw.Flush()
					sw.Close()
				End Using

				sr.Close()
			End Using

			Language.Load()	'default = english

			Dim systemLang As String = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName	'en, fr, sv etc.
			Dim lastUsedLang As String = settings.getconfig("language")

			Dim hasLastUsed As Boolean = False
			Dim hasNativeLang As Boolean = False

			Dim nativeLang As Language.LanguageOption = Nothing

			For Each item As Language.LanguageOption In ComboBox2.Items
				If item.ISOLanguage.Equals(lastUsedLang, StringComparison.OrdinalIgnoreCase) Then
					ComboBox2.SelectedItem = item
					hasLastUsed = True
					Exit For 'found last used lang, moving on
				End If

				If Not hasNativeLang AndAlso systemLang.Equals(item.ISOLanguage, StringComparison.OrdinalIgnoreCase) Then
					nativeLang = item 'take native on hold incase last used language not found (avoid multiple loops)
				End If
			Next

			If Not hasLastUsed Then
				If Not hasNativeLang Then
					ComboBox2.SelectedItem = defaultLang 'couldn't find last used nor native lang, using english.
				Else
					ComboBox2.SelectedItem = nativeLang	'couldn't find last used, using native lang
				End If
			End If

			Language.TranslateForm(Me, toolTip1)
		Else
			If changeTo IsNot Nothing AndAlso Not changeTo.ISOLanguage.Equals(Language.Current) Then
				Language.Load(changeTo)
				Language.TranslateForm(Me, toolTip1)
			End If
		End If

		settings.setconfig("language", Language.Current)
	End Sub

	Public Function UpdateTextMethodmessagefn(ByRef number As Integer) As String
		Return Language.GetTranslation(Me.Name, "UpdateLog", String.Format("Text{0}", number + 1))
	End Function

	Private Sub temporarynvidiaspeedup()   'we do this to speedup the removal of the nividia display driver because of the huge time the nvidia installer files take to do unknown stuff.

		Dim filePath As String = Nothing

		Try
			filePath = Environment.GetFolderPath _
			(Environment.SpecialFolder.ProgramFiles) + "\NVIDIA Corporation"

			For Each child As String In Directory.GetDirectories(filePath)
				If checkvariables.isnullorwhitespace(child) = False Then
					If child.ToLower.Contains("installer2") Then
						For Each child2 As String In Directory.GetDirectories(child)
							If checkvariables.isnullorwhitespace(child2) = False Then
								If child2.ToLower.Contains("display.3dvision") Or
								   child2.ToLower.Contains("display.controlpanel") Or
								   child2.ToLower.Contains("display.driver") Or
								   child2.ToLower.Contains("display.gfexperience") AndAlso removegfe Or
								   child2.ToLower.Contains("display.nvirusb") Or
								   child2.ToLower.Contains("display.optimus") Or
								   child2.ToLower.Contains("display.physx") Or
								   child2.ToLower.Contains("display.update") AndAlso removegfe Or
								   child2.ToLower.Contains("display.nview") Or
								   child2.ToLower.Contains("display.nvwmi") Or
								   child2.ToLower.Contains("gfexperience") AndAlso removegfe Or
								   child2.ToLower.Contains("nvidia.update") AndAlso removegfe Or
								   child2.ToLower.Contains("installer2\installer") AndAlso removegfe Or
								   child2.ToLower.Contains("network.service") AndAlso removegfe Or
								   child2.ToLower.Contains("miracast.virtualaudio") AndAlso removegfe Or
								   child2.ToLower.Contains("shadowplay") AndAlso removegfe Or
								   child2.ToLower.Contains("update.core") AndAlso removegfe Or
								   child2.ToLower.Contains("virtualaudio.driver") AndAlso removegfe Or
								   child2.ToLower.Contains("coretemp") AndAlso removegfe Or
								   child2.ToLower.Contains("shield") AndAlso removegfe Or
								   child2.ToLower.Contains("hdaudio.driver") Then
									Try
										deletedirectory(child2)
									Catch ex As Exception
									End Try

									If Not Directory.Exists(child2) Then
										CleanupEngine.shareddlls(child2)
									End If
								End If
							End If
						Next

						If Directory.GetDirectories(child).Length = 0 Then
							Try
								deletedirectory(child)
							Catch ex As Exception
							End Try
						End If
						If Not Directory.Exists(child) Then
							CleanupEngine.shareddlls(child)
						End If
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

	Public Sub UpdateTextMethod2(ByVal strMessage As String)

		If TextBox1.InvokeRequired Then
			Invoke(Sub() frmLog.TextBox1.Text = frmLog.TextBox1.Text + strMessage + vbNewLine)
			Invoke(Sub() frmLog.TextBox1.Select(frmLog.TextBox1.Text.Length, 0))
			Invoke(Sub() frmLog.TextBox1.ScrollToCaret())
		Else
			frmLog.TextBox1.Text = frmLog.TextBox1.Text + strMessage + vbNewLine
			frmLog.TextBox1.Select(frmLog.TextBox1.Text.Length, 0)
			frmLog.TextBox1.ScrollToCaret()
		End If

	End Sub

	Public Function GetREG_BINARY(ByVal Path As String, ByVal Value As String) As String
		Dim Data() As Byte = CType(Microsoft.Win32.Registry.GetValue(Path, Value, Nothing), Byte())
		If Data Is Nothing Then Return "N/A"
		Dim Result As String = String.Empty
		For j As Integer = 0 To Data.Length - 1
			Result &= Hex(Data(j)).PadLeft(2, "0"c) & ""
		Next
		Return Result
	End Function

	Public Function HexToString(ByVal Data As String) As String
		Dim com As String = ""
		For x = 0 To Data.Length - 1 Step 2
			com &= ChrW(CInt("&H" & Data.Substring(x, 2)))
		Next
		Return com
	End Function

	Public Sub deletesubregkey(ByVal value1 As RegistryKey, ByVal value2 As String)

		CleanupEngine.deletesubregkey(value1, value2)

	End Sub

	Private Sub deletedirectory(ByVal directory As String)
		CleanupEngine.deletedirectory(directory)
	End Sub

	Private Sub deletefile(ByVal file As String)
		CleanupEngine.deletefile(file)
	End Sub

	Public Sub deletevalue(ByVal value1 As RegistryKey, ByVal value2 As String)

		CleanupEngine.deletevalue(value1, value2)

	End Sub

	Private Sub amdenvironementpath(ByVal filepath As String)

		Dim regkey As RegistryKey
		Dim subregkey As RegistryKey
		Dim wantedvalue As String = Nothing

		'--------------------------------
		'System environement path cleanup
		'--------------------------------

		log("System environement cleanUP")
		filepath = filepath.ToLower
		Try
			subregkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM", False)
			If subregkey IsNot Nothing Then
				For Each child2 As String In subregkey.GetSubKeyNames()
					If child2.ToLower.Contains("controlset") Then
						regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\" & child2 & "\Control\Session Manager\Environment", True)
						If regkey IsNot Nothing Then
							For Each child As String In regkey.GetValueNames()
								If checkvariables.isnullorwhitespace(child) = False Then
									If child.Contains("Path") Then
										If checkvariables.isnullorwhitespace(CStr(regkey.GetValue(child))) = False Then
											wantedvalue = regkey.GetValue(child).ToString.ToLower
											Try
												Select Case True
													Case wantedvalue.Contains(";" + filepath & "\amd app\bin\x86_64")
														wantedvalue = wantedvalue.Replace(";" + filepath & "\amd app\bin\x86_64", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(filepath & "\amd app\bin\x86_64;")
														wantedvalue = wantedvalue.Replace(filepath & "\amd app\bin\x86_64;", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(";" + filepath & "\amd app\bin\x86")
														wantedvalue = wantedvalue.Replace(";" + filepath & "\amd app\bin\x86", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(filepath & "\amd app\bin\x86;")
														wantedvalue = wantedvalue.Replace(filepath & "\amd app\bin\x86;", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(";" + filepath & "\ati.ace\core-static")
														wantedvalue = wantedvalue.Replace(";" + filepath & "\ati.ace\core-static", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(filepath & "\ati.ace\core-static;")
														wantedvalue = wantedvalue.Replace(filepath & "\ati.ace\core-static;", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(";" + filepath & "\ati.ace\core-static")
														wantedvalue = wantedvalue.Replace(";" + filepath & "\ati.ace\core-static", "")
														regkey.SetValue(child, wantedvalue)

													Case wantedvalue.Contains(filepath & "\ati.ace\core-static;")
														wantedvalue = wantedvalue.Replace(filepath & "\ati.ace\core-static;", "")
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
			log(ex.Message + ex.StackTrace)
		End Try

		'end system environement patch cleanup
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
        If Not My.Computer.Network.IsAvailable Then
            Return 3
        End If

        Try
            Dim request2 As System.Net.HttpWebRequest = CType(System.Net.HttpWebRequest.Create("http://www.wagnardmobile.com/DDU/currentversion2.txt"), Net.HttpWebRequest)
            Dim response2 As System.Net.HttpWebResponse = Nothing
			request2.Timeout = 2500

            Try
                response2 = CType(request2.GetResponse(), Net.HttpWebResponse)
            Catch ex As Exception
                request2 = CType(System.Net.HttpWebRequest.Create("http://archive.sunet.se/pub/games/PC/guru3d/ddu/currentversion2.txt"), Net.HttpWebRequest)
			End Try

            request2.Timeout = 2500
            response2 = CType(request2.GetResponse(), Net.HttpWebResponse)

			Dim newestversion2 As String = ""

			Using sr As System.IO.StreamReader = New System.IO.StreamReader(response2.GetResponseStream())
				newestversion2 = sr.ReadToEnd()

				sr.Close()
			End Using


            Dim newestversion2int As Integer = CInt(newestversion2.Replace(".", ""))
            Dim applicationversion As Integer = CInt(Application.ProductVersion.Replace(".", ""))

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
            Dim userpth As String = CStr(My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory")) + "\"
            Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
			Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
			Dim isUsingRoaming As Boolean = False

            If My.Computer.FileSystem.FileExists(userpthn & "\Display Driver Uninstaller\config.cfg") Then
                '    Dim liness() As String = IO.File.ReadAllLines(userpth & "\AppData\Roaming\Display Driver Uninstaller\config.cfg")
                isUsingRoaming = True
                frmMain.roamingcfg = True
                lines = IO.File.ReadAllLines(userpthn & "\Display Driver Uninstaller\config.cfg")
                ' MessageBox.Show(userpth)
                '    MessageBox.Show("using roaming cfg")
            End If

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
            If isUsingRoaming = False Then
                If frmMain.roamingcfg = False Then
                    System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
                End If
            Else
                If frmMain.roamingcfg = True Then
                    System.IO.File.WriteAllLines(userpthn & "\Display Driver Uninstaller\config.cfg", lines)
                End If
            End If


            Return CType(False, String)
        Catch ex As Exception
            MsgBox(ex.Message + ex.StackTrace)
            Return CType(False, String)
        End Try

    End Function
    Public Sub setconfig(ByVal name As String, ByVal setvalue As String)
        Try
            '    Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
            Dim userpth As String = CStr(My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory")) + "\"
            Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
            Dim isUsingRoaming As Boolean = False
            If My.Computer.FileSystem.FileExists(userpthn & "\Display Driver Uninstaller\config.cfg") Then
                '    Dim liness() As String = IO.File.ReadAllLines(userpth & "\AppData\Roaming\Display Driver Uninstaller\config.cfg")
                isUsingRoaming = True
                frmMain.roamingcfg = True
                lines = IO.File.ReadAllLines(userpthn & "\Display Driver Uninstaller\config.cfg")
                '   MessageBox.Show(userpth)
                '  MessageBox.Show("using roaming cfg")
            End If
            For i As Integer = 0 To lines.Length - 1
                If Not String.IsNullOrEmpty(lines(i)) Then
                    If lines(i).ToLower.Contains(name) Then
                        lines(i) = name + "=" + setvalue
                        If isUsingRoaming = False Then
                            If frmMain.roamingcfg = False Then
                                System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
                            End If
                        Else
                            If frmMain.roamingcfg = True Then
                                System.IO.File.WriteAllLines(userpthn & "\Display Driver Uninstaller\config.cfg", lines)
                            End If
                        End If

                        End If
                End If
            Next
        Catch ex As Exception
        End Try
    End Sub

End Class

Public Class CleanupEngine

	Dim checkvariables As New checkvariables

    Private Function UpdateTextMethodmessagefn(ByRef number As Integer) As String
		Return Language.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
	End Function

    Private Sub updatetextmethod(strmessage As String)
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        f.UpdateTextMethod(strmessage)
	End Sub

    Private Sub log(strmessage As String)
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        f.log(strmessage)
	End Sub

    Public Sub TestDelete(ByVal folder As String)
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        f.TestDelete(folder)
	End Sub

    Public Sub deletesubregkey(ByVal regkeypath As RegistryKey, ByVal child As String)

        If (regkeypath IsNot Nothing) AndAlso (Not checkvariables.isnullorwhitespace(child)) Then
         
            regkeypath.DeleteSubKeyTree(child)
            log(regkeypath.ToString + "\" + child + " - " + UpdateTextMethodmessagefn(39))

        End If
    End Sub

    Public Sub deletedirectory(ByVal directorypath As String)
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim removephysx As Boolean = f.getremovephysx
        If Not checkvariables.isnullorwhitespace(directorypath) Then

            If (removephysx Or Not ((Not removephysx) And directorypath.ToLower.Contains("physx"))) Then
                My.Computer.FileSystem.DeleteDirectory _
                        (directorypath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                log(directorypath + " - " + UpdateTextMethodmessagefn(39))
            End If

        End If

    End Sub

    Public Sub deletefile(ByVal filepath As String)


        If Not checkvariables.isnullorwhitespace(filepath) Then

            My.Computer.FileSystem.DeleteFile(filepath) 'filepath here include the file too.

            log(filepath + " - " + UpdateTextMethodmessagefn(41))
        End If

    End Sub

    Public Sub deletevalue(ByVal regkeypath As RegistryKey, ByVal child As String)


        If (regkeypath IsNot Nothing) AndAlso (Not checkvariables.isnullorwhitespace(child)) Then
          
            regkeypath.DeleteValue(child)
            log(regkeypath.ToString + "\" + child + " - " + UpdateTextMethodmessagefn(40))


        End If

    End Sub

    Public Sub classroot(ByVal classroot As String())

        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String = Nothing
        Dim appid As String = Nothing
        Dim typelib As String = Nothing

        log("Begin classroot CleanUP")

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
                                        If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                            wantedvalue = subregkey.GetValue("").ToString
                                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                                Try
                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID"))) Then
                                                            appid = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                            Try

                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True), appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue(""))) Then
                                                            typelib = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True), typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True), wantedvalue)

                                                Catch ex As Exception
                                                End Try
                                            End If
                                        End If
                                    End If
                                    'here I remove the mediafoundationkeys if present
                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                    Try
                                        deletesubregkey(regkey.OpenSubKey("MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                    Catch ex As Exception
                                    End Try
                                    Try
                                        deletesubregkey(regkey.OpenSubKey("MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                    Catch ex As Exception
                                    End Try
                                    deletesubregkey(regkey, child)
                                End If
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            For i As Integer = 0 To classroot.Length - 1
                                If Not checkvariables.isnullorwhitespace(classroot(i)) Then
                                    If child.ToLower.StartsWith(classroot(i).ToLower) Then
                                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey(child & "\CLSID")
                                        If subregkey IsNot Nothing Then
                                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                                wantedvalue = subregkey.GetValue("").ToString
                                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                                    Try
                                                        Try
                                                            If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID"))) Then
                                                                appid = regkey.OpenSubKey("CLSID\" & wantedvalue).GetValue("AppID").ToString
                                                                Try
                                                                    deletesubregkey(regkey.OpenSubKey("AppID", True), appid)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        Catch ex As Exception
                                                        End Try

                                                        Try
                                                            If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue(""))) Then
                                                                typelib = regkey.OpenSubKey("CLSID\" & wantedvalue & "\TypeLib").GetValue("").ToString
                                                                Try
                                                                    deletesubregkey(regkey.OpenSubKey("TypeLib", True), typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        Catch ex As Exception
                                                        End Try

                                                        deletesubregkey(regkey.OpenSubKey("CLSID", True), wantedvalue)

                                                    Catch ex As Exception
                                                    End Try
                                                End If
                                            End If
                                        End If
                                        'here I remove the mediafoundationkeys if present
                                        'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                        Try
                                            deletesubregkey(regkey.OpenSubKey("MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                        Catch ex As Exception
                                        End Try
                                        Try
                                            deletesubregkey(regkey.OpenSubKey("MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                        Catch ex As Exception
                                        End Try
                                        deletesubregkey(regkey, child)
                                    End If
                                End If
                            Next
                        End If
                    Next
                End If
            Catch ex As Exception
                log(ex.Message + ex.StackTrace)
            End Try
        End If

        log("End classroot CleanUP")
    End Sub

    Public Sub installer(ByVal packages As String())
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim regkey As RegistryKey
        Dim basekey As RegistryKey
        Dim superregkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim subsuperregkey As RegistryKey
        Dim wantedvalue As String = Nothing
        Dim removephysx As Boolean = f.getremovephysx

        updatetextmethod(UpdateTextMethodmessagefn(29))

        Try
            log("-Starting S-1-5-xx region cleanUP")
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
                                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("DisplayName"))) = False Then
                                                wantedvalue = subregkey.GetValue("DisplayName").ToString
                                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                                    For i As Integer = 0 To packages.Length - 1
                                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And
                                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then



                                                                'Deleting here the c:\windows\installer entries.
                                                                Try
                                                                    If (Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("LocalPackage")))) AndAlso
                                                                      subregkey.GetValue("LocalPackage").ToString.ToLower.Contains(".msi") Then
                                                                        deletefile(subregkey.GetValue("LocalPackage").ToString)
                                                                    End If
                                                                Catch ex As Exception
                                                                End Try


                                                                Try
                                                                    If (Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("UninstallString")))) AndAlso
                                                                      subregkey.GetValue("UninstallString").ToString.ToLower.Contains("{") Then
                                                                        Dim folder As String = subregkey.GetValue("UninstallString").ToString
                                                                        folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
                                                                        TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder)
                                                                        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", False) IsNot Nothing Then
                                                                            For Each subkeyname As String In My.Computer.Registry.LocalMachine.OpenSubKey _
                      ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders").GetValueNames
                                                                                If Not checkvariables.isnullorwhitespace(subkeyname) Then
                                                                                    If subkeyname.ToLower.Contains(folder.ToLower) Then
                                                                                        deletevalue(My.Computer.Registry.LocalMachine.OpenSubKey _
                      ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True), subkeyname)
                                                                                    End If
                                                                                End If
                                                                            Next
                                                                        End If
                                                                    End If
                                                                Catch ex As Exception
                                                                    log(ex.Message + ex.StackTrace)
                                                                End Try

                                                                Try
                                                                    deletesubregkey(regkey, child)
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
                                                                                                deletesubregkey(superregkey, child2)
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
                                                                                                deletesubregkey(superregkey, child2)
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
            updatetextmethod(UpdateTextMethodmessagefn(30))
			log("-End of S-1-5-xx region cleanUP")
        Catch ex As Exception
			MsgBox(Language.GetTranslation("frmMain", "Messages", "Text6"))
            log(ex.Message + ex.StackTrace)
        End Try

        updatetextmethod(UpdateTextMethodmessagefn(31))
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
      ("Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey _
("Installer\Products\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("ProductName"))) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And
                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then

                                                Try
                                                    If (Not checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("ProductIcon")))) AndAlso
                                                      subregkey.GetValue("ProductIcon").ToString.ToLower.Contains("{") Then
                                                        Dim folder As String = subregkey.GetValue("ProductIcon").ToString
                                                        folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
                                                        TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder)
                                                        If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", False) IsNot Nothing Then
                                                            For Each subkeyname As String In My.Computer.Registry.LocalMachine.OpenSubKey _
    ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders").GetValueNames
                                                                If Not checkvariables.isnullorwhitespace(subkeyname) Then
                                                                    If subkeyname.ToLower.Contains(folder.ToLower) Then
                                                                        deletevalue(My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Microsoft\Windows\CurrentVersion\Installer\Folders", True), subkeyname)
                                                                    End If
                                                                End If
                                                            Next
                                                        End If
                                                    End If
                                                Catch ex As Exception
                                                    log(ex.Message + ex.StackTrace)
                                                End Try

                                                Try
                                                    deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Installer\Features", True), child)
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
                                                                                deletesubregkey(superregkey, child2)
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
            updatetextmethod(UpdateTextMethodmessagefn(32))
        Catch ex As Exception
			MsgBox(Language.GetTranslation("frmMain", "Messages", "Text6"))
			log(ex.Message + ex.StackTrace)
        End Try


        updatetextmethod(UpdateTextMethodmessagefn(33))

        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey _
      ("Software\Classes\Installer\Products", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.LocalMachine.OpenSubKey _
("Software\Classes\Installer\Products\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("ProductName"))) = False Then
                                wantedvalue = subregkey.GetValue("ProductName").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To packages.Length - 1
                                        If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                            If wantedvalue.ToLower.Contains(packages(i).ToLower) And
                                               (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then
                                                Try
                                                    deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    deletesubregkey(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Classes\Installer\Features", True), child)
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
                                                                                deletesubregkey(superregkey, child2)
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
            updatetextmethod(UpdateTextMethodmessagefn(34))
        Catch ex As Exception
			MsgBox(Language.GetTranslation("frmMain", "Messages", "Text6"))
			log(ex.Message + ex.StackTrace)
        End Try

        updatetextmethod(UpdateTextMethodmessagefn(35))
        Try
            For Each users As String In My.Computer.Registry.Users.GetSubKeyNames()
                If Not checkvariables.isnullorwhitespace(users) Then

                    regkey = My.Computer.Registry.Users.OpenSubKey _
              (users & "\Software\Microsoft\Installer\Products", True)

                    If regkey IsNot Nothing Then
                        For Each child As String In regkey.GetSubKeyNames()
                            If checkvariables.isnullorwhitespace(child) = False Then

                                subregkey = My.Computer.Registry.Users.OpenSubKey _
    (users & "\Software\Microsoft\Installer\Products\" & child, False)

                                If subregkey IsNot Nothing Then
                                    If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("ProductName"))) = False Then
                                        wantedvalue = subregkey.GetValue("ProductName").ToString
                                        If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                            For i As Integer = 0 To packages.Length - 1
                                                If Not checkvariables.isnullorwhitespace(packages(i)) Then
                                                    If wantedvalue.ToLower.Contains(packages(i).ToLower) And
                                                       (removephysx Or Not ((Not removephysx) And child.ToLower.Contains("physx"))) Then
                                                        Try
                                                            deletesubregkey(regkey, child)
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.Users.OpenSubKey(users & "\Software\Microsoft\Installer\Features", True), child)
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
                                                                                        deletesubregkey(superregkey, child2)
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
            updatetextmethod(UpdateTextMethodmessagefn(36))
        Catch ex As Exception
			MsgBox(Language.GetTranslation("frmMain", "Messages", "Text6"))
			log(ex.Message + ex.StackTrace)
        End Try

	End Sub

    Public Sub cleanserviceprocess(ByVal services As String())
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles
        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey

        updatetextmethod(UpdateTextMethodmessagefn(37))
        log("Cleaning Process/Services...")


        regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Services", False)
        If regkey IsNot Nothing Then
            For i As Integer = 0 To services.Length - 1
                If Not checkvariables.isnullorwhitespace(services(i)) Then
                    If regkey.OpenSubKey(services(i), False) IsNot Nothing Then

                        If Not (donotremoveamdhdaudiobusfiles AndAlso services(i).ToLower.Contains("amdkmafd")) Then

                            Dim stopservice As New ProcessStartInfo
                            stopservice.FileName = "cmd.exe"
                            stopservice.Arguments = " /Cnet stop " & Chr(34) & services(i) & Chr(34)
                            stopservice.UseShellExecute = False
                            stopservice.CreateNoWindow = True
                            stopservice.RedirectStandardOutput = False


                            Dim processstopservice As New Process
                            processstopservice.StartInfo = stopservice
                            updatetextmethod("Stopping service : " & services(i))
                            log("Stopping service : " & services(i))
                            processstopservice.Start()
                            processstopservice.WaitForExit()
                            processstopservice.Close()
							
                            stopservice.Arguments = " /Csc delete " & Chr(34) & services(i) & Chr(34)

                            processstopservice.StartInfo = stopservice
                            updatetextmethod("Trying to Deleting service : " & services(i))
                            log("Trying to Deleting service : " & services(i))
                            processstopservice.Start()
                            processstopservice.WaitForExit()
                            processstopservice.Close()
							
                            stopservice.Arguments = " /Csc interrogate " & Chr(34) & services(i) & Chr(34)
                            processstopservice.StartInfo = stopservice
                            processstopservice.Start()
                            processstopservice.WaitForExit()
                            processstopservice.Close()
							
                            'Verify that the service was indeed removed.
                            If regkey.OpenSubKey(services(i), False) IsNot Nothing Then
                                updatetextmethod("Failed to remove the service.")
                                log("Failed to remove the service.")
                            Else
                                updatetextmethod("Service removed.")
                                log("Service removed.")
                            End If

                        End If
                    End If
                End If

                System.Threading.Thread.Sleep(10)
            Next
        End If
        updatetextmethod(UpdateTextMethodmessagefn(38))
        log("Process/Services CleanUP Complete")

        '-------------
        'control/video
        '-------------
        'Reason I put this in service is that the removal of this is based from its service.
        log("Control/Video CleanUP")
        Try
            regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Video", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = regkey.OpenSubKey(child & "\Video", False)
                        If subregkey IsNot Nothing Then
                            For i As Integer = 0 To services.Length - 1
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("Service"))) = False Then
                                    If subregkey.GetValue("Service").ToString.ToLower = services(i).ToLower Then
                                        Try
                                            deletesubregkey(regkey, child)
                                            deletesubregkey(My.Computer.Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
                                            Exit For
                                        Catch ex As Exception
                                        End Try
                                    End If
                                End If
                            Next
                        Else
                            'Here, if subregkey is nothing, it mean \video doesnt exist and is no \0000, we can delete it.
                            'this is a general cleanUP we could say.
                            If regkey.OpenSubKey(child + "\0000") Is Nothing Then
                                Try
                                    deletesubregkey(regkey, child)
                                    deletesubregkey(My.Computer.Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try
	End Sub

    Public Sub prePnplockdownfiles(ByVal oeminf As String)
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim regkey As RegistryKey
        Dim winxp = f.winxp
        Dim win8higher = f.win8higher
        Dim processinfo As New ProcessStartInfo
        Dim process As New Process
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles

        Try
            If win8higher Then
                regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                If regkey IsNot Nothing Then
                    If Not checkvariables.isnullorwhitespace(oeminf) Then
                        If Not (donotremoveamdhdaudiobusfiles AndAlso oeminf.ToLower.Contains("amdkmafd.sys")) Then
                            For Each child As String In regkey.GetSubKeyNames()
                                If checkvariables.isnullorwhitespace(child) = False Then
                                    If (Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("Source")))) AndAlso regkey.OpenSubKey(child).GetValue("Source").ToString.ToLower.Contains(oeminf.ToLower) Then
                                        Try
                                            deletesubregkey(regkey, child)
                                        Catch ex As Exception
                                            log(ex.Message & " @Pnplockdownfiles")
                                        End Try
                                    End If
                                End If
                            Next
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

	End Sub

    Public Sub Pnplockdownfiles(ByVal driverfiles As String())
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim regkey As RegistryKey
        Dim winxp = f.winxp
        Dim win8higher = f.win8higher
        Dim processinfo As New ProcessStartInfo
        Dim process As New Process
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles

        Try
            If Not winxp Then  'this does not exist on winxp so we skip if winxp detected
                If win8higher Then
                    regkey = My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
                    If regkey IsNot Nothing Then
                        For i As Integer = 0 To driverfiles.Length - 1
                            If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                                If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd.sys")) Then
                                    For Each child As String In regkey.GetSubKeyNames()
                                        If checkvariables.isnullorwhitespace(child) = False Then
                                            If child.ToLower.Replace("/", "\").Contains("\" + driverfiles(i).ToLower) Then
                                                Try
                                                    deletesubregkey(regkey, child)
                                                Catch ex As Exception
                                                    log(ex.Message & " @Pnplockdownfiles")
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
                                                    deletevalue(regkey, child)
                                                Catch ex As Exception
                                                    log(ex.Message & " @Pnplockdownfiles")
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
            log(ex.StackTrace)
        End Try

    End Sub

    Public Sub clsidleftover(ByVal clsidleftover As String())

        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String
        Dim appid As String = Nothing
        Dim typelib As String = Nothing

        log("Begin clsidleftover CleanUP")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\InProcServer32", False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                        appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True), appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                        typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True), typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try

                                                Try
                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    deletesubregkey(regkey, child)
                                                    Exit For
                                                Catch ex As Exception
                                                    log(ex.Message + ex.StackTrace)
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
            log(ex.Message + ex.StackTrace)
        End Try

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child, False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                        appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True), appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                        typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True), typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    deletesubregkey(regkey, child)
                                                    Exit For
                                                Catch ex As Exception
                                                    log(ex.Message + ex.StackTrace)
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
            log(ex.Message + ex.StackTrace)
        End Try


        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\InProcServer32", False)

                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                            appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True), appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                            typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True), typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        'here I remove the mediafoundationkeys if present
                                                        'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))

                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                        Catch ex As Exception
                                                        End Try
                                                        deletesubregkey(regkey, child)
                                                        Exit For
                                                    Catch ex As Exception
                                                        log(ex.Message + ex.StackTrace)
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
                log(ex.Message + ex.StackTrace)
            End Try

            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child, False)
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                            appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True), appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                            typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True), typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        'here I remove the mediafoundationkeys if present
                                                        'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                        Catch ex As Exception
                                                        End Try
                                                        deletesubregkey(regkey, child)
                                                        Exit For
                                                    Catch ex As Exception
                                                        log(ex.Message + ex.StackTrace)
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
                log(ex.Message + ex.StackTrace)
            End Try
        End If

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("CLSID\" & child & "\LocalServer32", False)
                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To clsidleftover.Length - 1
                                        If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                            If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                        appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True), appid)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try

                                                Try
                                                    If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                        typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True), typelib)
                                                        Catch ex As Exception
                                                        End Try
                                                    End If
                                                Catch ex As Exception
                                                End Try
                                                Try
                                                    'here I remove the mediafoundationkeys if present
                                                    'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                    Catch ex As Exception
                                                    End Try
                                                    deletesubregkey(regkey, child)
                                                    Exit For
                                                Catch ex As Exception
                                                    log(ex.Message + ex.StackTrace)
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
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\CLSID\" & child & "\LocalServer32", False)
                            If subregkey IsNot Nothing Then
                                If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To clsidleftover.Length - 1
                                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then


                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).GetValue("AppID"))) Then
                                                            appid = regkey.OpenSubKey(child).GetValue("AppID").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True), appid)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        If Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child & "\TypeLib").GetValue(""))) Then
                                                            typelib = regkey.OpenSubKey(child & "\TypeLib").GetValue("").ToString
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True), typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    Catch ex As Exception
                                                    End Try
                                                    Try
                                                        'here I remove the mediafoundationkeys if present
                                                        'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
                                                        Catch ex As Exception
                                                        End Try
                                                        Try
                                                            deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
                                                        Catch ex As Exception
                                                        End Try
                                                        deletesubregkey(regkey, child)
                                                        Exit For
                                                    Catch ex As Exception
                                                        log(ex.Message + ex.StackTrace)
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
                log(ex.Message + ex.StackTrace)
            End Try
        End If

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("AppID", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then
                        For i As Integer = 0 To clsidleftover.Length - 1
                            If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                If child.ToLower.Contains(clsidleftover(i).ToLower) Then
                                    subregkey = regkey.OpenSubKey(child)
                                    If subregkey IsNot Nothing Then
                                        If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("AppID"))) = False Then
                                            wantedvalue = subregkey.GetValue("AppID").ToString
                                            If checkvariables.isnullorwhitespace(wantedvalue) = False Then

                                                Try
                                                    deletesubregkey(regkey, wantedvalue)
                                                Catch ex As Exception
                                                End Try

                                                Try
                                                    deletesubregkey(regkey, child)
                                                    Exit For
                                                Catch ex As Exception
                                                End Try
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
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then
            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\AppID", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then
                            For i As Integer = 0 To clsidleftover.Length - 1
                                If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                    If child.ToLower.Contains(clsidleftover(i).ToLower) Then
                                        subregkey = regkey.OpenSubKey(child)
                                        If subregkey IsNot Nothing Then
                                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue("AppID"))) = False Then
                                                wantedvalue = subregkey.GetValue("AppID").ToString
                                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then

                                                    Try
                                                        deletesubregkey(regkey, wantedvalue)
                                                    Catch ex As Exception
                                                    End Try

                                                    Try
                                                        deletesubregkey(regkey, child)
                                                        Exit For
                                                    Catch ex As Exception
                                                    End Try
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
                log(ex.Message + ex.StackTrace)
            End Try
        End If


        'clean orphan typelib.....
        log("Orphan cleanUp")
        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If (Not checkvariables.isnullorwhitespace(child)) AndAlso (regkey.OpenSubKey(child) IsNot Nothing) Then
                        For Each child2 As String In regkey.OpenSubKey(child).GetSubKeyNames()
                            If (regkey.OpenSubKey(child, False) IsNot Nothing) AndAlso (Not checkvariables.isnullorwhitespace(child2)) Then
                                For Each child3 As String In regkey.OpenSubKey(child).OpenSubKey(child2).GetSubKeyNames()
                                    If (regkey.OpenSubKey(child, False) IsNot Nothing) AndAlso (Not checkvariables.isnullorwhitespace(child3)) Then
                                        For Each child4 As String In regkey.OpenSubKey(child).OpenSubKey(child2).OpenSubKey(child3).GetSubKeyNames()
                                            If (Not checkvariables.isnullorwhitespace(child4)) AndAlso regkey.OpenSubKey(child, False) IsNot Nothing Then
                                                For i As Integer = 0 To clsidleftover.Length - 1
                                                    If Not checkvariables.isnullorwhitespace(clsidleftover(i)) Then
                                                        If (regkey.OpenSubKey(child, False) IsNot Nothing) AndAlso (Not checkvariables.isnullorwhitespace(CStr(regkey.OpenSubKey(child).OpenSubKey(child2).OpenSubKey(child3).OpenSubKey(child4).GetValue("")))) Then
                                                            If regkey.OpenSubKey(child).OpenSubKey(child2).OpenSubKey(child3).OpenSubKey(child4).GetValue("").ToString.ToLower.Contains(clsidleftover(i).ToLower) Then
                                                                Try
                                                                    deletesubregkey(regkey, child)
                                                                    log(child + " for " + clsidleftover(i))
                                                                    Exit For
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        End If
                                                    End If
                                                Next
                                            End If
                                        Next
                                    End If
                                Next
                            End If
                        Next
                    End If
                Next
            End If
        Catch ex As Exception
            log(ex.Message + ex.StackTrace)
        End Try

        log("End clsidleftover CleanUP")
    End Sub

    Public Sub interfaces(ByVal interfaces As String())

        Dim regkey As RegistryKey
        Dim subregkey As RegistryKey
        Dim wantedvalue As String
        Dim typelib As String = Nothing

        log("Start Interface CleanUP")

        Try
            regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface", True)
            If regkey IsNot Nothing Then
                For Each child As String In regkey.GetSubKeyNames()
                    If checkvariables.isnullorwhitespace(child) = False Then

                        subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Interface\" & child, False)

                        If subregkey IsNot Nothing Then
                            If checkvariables.isnullorwhitespace(CStr(subregkey.GetValue(""))) = False Then
                                wantedvalue = subregkey.GetValue("").ToString
                                If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                    For i As Integer = 0 To interfaces.Length - 1
                                        If Not checkvariables.isnullorwhitespace(interfaces(i)) Then
                                            If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                                If subregkey.OpenSubKey("Typelib", False) IsNot Nothing Then
                                                    If checkvariables.isnullorwhitespace(CStr(subregkey.OpenSubKey("TypeLib", False).GetValue(""))) = False Then
                                                        typelib = CStr(subregkey.OpenSubKey("TypeLib", False).GetValue(""))
                                                        If checkvariables.isnullorwhitespace(typelib) = False Then
                                                            Try
                                                                deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("TypeLib", True), typelib)
                                                            Catch ex As Exception
                                                            End Try
                                                        End If
                                                    End If
                                                End If
                                                Try
                                                    deletesubregkey(regkey, child)
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
            log(ex.Message + ex.StackTrace)
        End Try

        If IntPtr.Size = 8 Then

            Try
                regkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\Interface", True)
                If regkey IsNot Nothing Then
                    For Each child As String In regkey.GetSubKeyNames()
                        If checkvariables.isnullorwhitespace(child) = False Then

                            subregkey = My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\Interface\" & child, False)

                            If subregkey IsNot Nothing Then
                                'Hack for some weird registry state  "For user: Watcher"
                                Try
                                    If checkvariables.isnullorwhitespace(CStr((subregkey.GetValue("")))) = False Then
                                        'do nothing
                                    End If
                                Catch ex As Exception
                                    log("non standard keytype found : " + child)
                                    Continue For
                                End Try
                                If checkvariables.isnullorwhitespace(CStr((subregkey.GetValue("")))) = False Then
                                    wantedvalue = subregkey.GetValue("").ToString
                                    If checkvariables.isnullorwhitespace(wantedvalue) = False Then
                                        For i As Integer = 0 To interfaces.Length - 1
                                            If Not checkvariables.isnullorwhitespace(interfaces(i)) Then
                                                If wantedvalue.ToLower.StartsWith(interfaces(i).ToLower) Then
                                                    If subregkey.OpenSubKey("Typelib", False) IsNot Nothing Then
                                                        If checkvariables.isnullorwhitespace(CStr(subregkey.OpenSubKey("TypeLib", False).GetValue(""))) = False Then
                                                            typelib = CStr(subregkey.OpenSubKey("TypeLib", False).GetValue(""))
                                                            If checkvariables.isnullorwhitespace(typelib) = False Then
                                                                Try
                                                                    deletesubregkey(My.Computer.Registry.ClassesRoot.OpenSubKey("Wow6432Node\TypeLib", True), typelib)
                                                                Catch ex As Exception
                                                                End Try
                                                            End If
                                                        End If
                                                    End If
                                                    Try
                                                        deletesubregkey(regkey, child)
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
                log(ex.Message + ex.StackTrace)
            End Try

        End If

        log("END Interface CleanUP")
	End Sub

    Public Sub folderscleanup(ByVal driverfiles As String())
		Dim f As frmMain = CType(My.Application.OpenForms("frmMain"), frmMain)
        Dim winxp = f.winxp
        Dim filePath As String
        Dim donotremoveamdhdaudiobusfiles = f.donotremoveamdhdaudiobusfiles

        For i As Integer = 0 To driverfiles.Length - 1
            If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then

                    filePath = System.Environment.SystemDirectory

                    Try
                        deletefile(filePath & "\" & driverfiles(i))
                    Catch ex As Exception
                    End Try

                    Try
                        deletefile(filePath + "\Drivers\" + driverfiles(i))
                    Catch ex As Exception
                    End Try

                    If winxp Then
                        Try
                            deletefile(filePath + "\Drivers\dllcache\" + driverfiles(i))
                        Catch ex As Exception
                        End Try
                    End If
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
                                    deletefile(child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
            log("info: " + ex.Message)
        End Try

        Const CSIDL_WINDOWS As Integer = &H29
        Dim winPath As New StringBuilder(300)
        If WindowsApi.SHGetFolderPath(Nothing, CSIDL_WINDOWS, Nothing, 0, winPath) <> 0 Then
            Throw New ApplicationException("Can't get window's sysWOW64 directory")
            log("Can't get window's sysWOW64 directory")
        End If


        If IntPtr.Size = 8 Then
            For i As Integer = 0 To driverfiles.Length - 1
                If Not checkvariables.isnullorwhitespace(driverfiles(i)) Then
                    If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then

                        For Each child As String In My.Computer.FileSystem.GetFiles(winPath.ToString, FileIO.SearchOption.SearchTopLevelOnly, "*.log")
                            If checkvariables.isnullorwhitespace(child) = False Then
                                If child.ToLower.Contains(driverfiles(i).ToLower) Then
                                    Try
                                        deletefile(child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next

                        Try
                            deletefile(winPath.ToString + "\Drivers\" + driverfiles(i))
                        Catch ex As Exception
                        End Try

                        Try
                            deletefile(winPath.ToString + "\" + driverfiles(i))
                        Catch ex As Exception
                        End Try
                    End If
                End If
            Next
        End If
	End Sub

    Public Sub shareddlls(ByVal filepath As String)
        If Not checkvariables.isnullorwhitespace(filepath) Then
            If Not Directory.Exists(filepath) Then
                If My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", False) IsNot Nothing Then
                    For Each child As String In My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", False).GetValueNames
                        If Not checkvariables.isnullorwhitespace(child) Then
                            If child.ToLower.Contains(filepath.ToLower + "\") Then
                                Try
                                    deletevalue(My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True), child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
                If My.Computer.Registry.LocalMachine.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", False) IsNot Nothing Then
                    For Each child As String In My.Computer.Registry.LocalMachine.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", False).GetValueNames
                        If Not checkvariables.isnullorwhitespace(child) Then
                            If child.ToLower.Contains(filepath.ToLower + "\") Then
                                Try
                                    deletevalue(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True), child)
                                Catch ex As Exception
                                End Try
                            End If
                        End If
                    Next
                End If
                If IntPtr.Size = 8 Then
                    If My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", False) IsNot Nothing Then
                        For Each child As String In My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", False).GetValueNames
                            If Not checkvariables.isnullorwhitespace(child) Then
                                If child.ToLower.Contains(filepath.ToLower + "\") Then
                                    Try
                                        deletevalue(My.Computer.Registry.LocalMachine.OpenSubKey("Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True), child)
                                    Catch ex As Exception
                                    End Try
                                End If
                            End If
                        Next
                    End If
                End If
            End If
        End If
    End Sub
End Class

Public Class WindowsApi

    <DllImport("shell32.dll")> _
    Public Shared Function SHGetFolderPath(ByVal hwndOwner As IntPtr, ByVal nFolder As Int32, ByVal hToken As IntPtr, ByVal dwFlags As Int32, ByVal pszPath As StringBuilder) As Int32
    End Function

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
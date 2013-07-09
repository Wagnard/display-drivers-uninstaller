Public Class Form1
   
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim removedriver As New ProcessStartInfo
        Dim jobstatus As Boolean
        Dim vendid As String
        Dim provider As String

        If ComboBox1.Text = "AMD" Then
            vendid = "@*ven_1002*"
            provider = "Provider: Advanced Micro Devices"
        End If

        If ComboBox1.Text = "NVIDIA" Then
            vendid = "@*ven_10de*"
            provider = "Provider: NVIDIA"
        End If

        'Debut de la disinstallation du driver
        removedriver.FileName = ".\" & Label3.Text & "\devcon.exe"
        removedriver.Arguments = "remove =display " & Chr(34) & vendid & Chr(34)
        removedriver.UseShellExecute = False
        removedriver.CreateNoWindow = True
        removedriver.RedirectStandardOutput = True

        If Button1.Text = "Done." Then
            Close()

        Else
            Button1.Enabled = False
            Button1.Text = "Uninstalling..."

            'creation dun process fantome pour le wait on exit.
            Dim proc As New Process
            proc.StartInfo = removedriver
            proc.Start()
            proc.WaitForExit()

            

            Dim checkoem As New Diagnostics.ProcessStartInfo

            'Debut de la disinstallation du driver (du driver store) recherche oem
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
            'prepare a lire
            Dim Reply As String = proc2.StandardOutput.ReadToEnd
            Dim position As Integer

            position = Reply.IndexOf(provider)
5:
            '  On Error Resume Next
            If position < 0 Then

                GoTo 10

            Else

                Dim part As String = Reply.Substring(position - 14, 10).Replace("oem", "em")  'work around...
                position = Reply.IndexOf(provider, position + 1)
                part = part.Replace("em", "oem")
                part = part.Replace(vbNewLine, "")


                Button1.Enabled = True
                Button1.Text = "Done."

                'Debut de la disinstallation du driver (du driver store) delete oem
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

                Dim Reply2 As String = proc3.StandardOutput.ReadToEnd

                TextBox1.Text = TextBox1.Text + Reply2

                jobstatus = True
                GoTo 5
            End If

        End If
10:
        If jobstatus = True Then
            'Debut du scan de nouveau peripherique
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
        End If
        Button1.Enabled = True
        Button1.Text = "Done."
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim version As String
        Dim arch As Boolean
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
Imports WindowsApplication1.Form1
Public Class options
    Dim buttontext As String()
    'Dim userpthn As String = System.Environment.GetEnvironmentVariable("appdata")
    'Dim userpthn As String = My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("Public")
    Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If CheckBox2.Checked = True Then
            setconfig("logbox", "true")

        Else
            setconfig("logbox", "false")
        End If
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        If CheckBox4.Checked = True Then
            setconfig("remove3dtvplay", "true")
            remove3dtvplay = True
        Else
            setconfig("remove3dtvplay", "false")
            remove3dtvplay = False
        End If
    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged
        If CheckBox5.Checked = True Then
            setconfig("systemrestore", "true")

        Else
            setconfig("systemrestore", "false")

        End If
    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        If combobox1value = "NVIDIA" Then

            If CheckBox3.Checked = True Then
                setconfig("removephysx", "true")
                removephysx = True
            Else
                setconfig("removephysx", "false")
                removephysx = False
            End If

        End If

    End Sub

    Public Sub setconfig(ByVal name As String, ByVal setvalue As String)
        Try
            Dim lines() As String = IO.File.ReadAllLines(Application.StartupPath & "\settings\config.cfg")
            Dim isUsingRoaming As Boolean = False
            'Dim userpth As String = My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory") + "\"
            If My.Computer.FileSystem.FileExists(userpthn & "\Display Driver Uninstaller\config.cfg") Then
                '    Dim liness() As String = IO.File.ReadAllLines(userpth & "\AppData\Roaming\Display Driver Uninstaller\config.cfg")
                isUsingRoaming = True
                roamingcfg = True
                lines = IO.File.ReadAllLines(userpthn & "\Display Driver Uninstaller\config.cfg")
                ' MessageBox.Show(userpth)
                '    MessageBox.Show("using roaming cfg")
            End If

            For i As Integer = 0 To lines.Length - 1
                If Not String.IsNullOrEmpty(lines(i)) Then
                    If lines(i).ToLower.Contains(name) Then
                        lines(i) = name + "=" + setvalue
                        If isUsingRoaming = False Then
                            System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
                        Else
                            System.IO.File.WriteAllLines(userpthn & "\Display Driver Uninstaller\config.cfg", lines)
                        End If
                        '  System.IO.File.WriteAllLines(Application.StartupPath & "\settings\config.cfg", lines)
                    End If
                End If
            Next
        Catch ex As Exception
        End Try
    End Sub
    Private Sub options_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub
    Private Sub options_Load(sender As Object, e As EventArgs) Handles MyBase.Load





        '----------------
        'read config file
        '-----------------
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



        If settings.getconfig("removeamdaudiobus") = "true" Then
            CheckBox7.Checked = True
            removeamdaudiobus = True
        Else
            CheckBox7.Checked = False
            removeamdaudiobus = False
        End If

        If settings.getconfig("removeamdkmpfd") = "true" Then
            CheckBox9.Checked = True
            removeamdkmpfd = True
        Else
            CheckBox9.Checked = False
            removeamdkmpfd = False
        End If



        If settings.getconfig("removemonitor") = "true" Then
            CheckBox6.Checked = True
            removemonitor = True
        Else
            CheckBox6.Checked = False
            removemonitor = False
        End If


        If settings.getconfig("showsafemodebox") = "true" Then
            CheckBox10.Checked = True
            safemodemb = True
        Else
            CheckBox10.Checked = False
            safemodemb = False
        End If

        If settings.getconfig("roamingcfg") = "true" Then
            CheckBox11.Checked = True
            roamingcfg = True
        Else
            CheckBox11.Checked = False
            roamingcfg = False
        End If


        If settings.getconfig("removecnvidia") = "true" Then
            CheckBox1.Checked = True
            removecnvidia = True
        Else
            CheckBox1.Checked = False
            removecnvidia = False
        End If

        If settings.getconfig("removecamd") = "true" Then
            CheckBox8.Checked = True
            removecamd = True
        Else
            CheckBox8.Checked = False
            removecamd = False
        End If


        'Init language for checkboxes
        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox1.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\checkbox1.txt") '// add each line as String Array.
        End Try
        CheckBox1.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox1.Text = CheckBox1.Text
            End If
            CheckBox1.Text = CheckBox1.Text & buttontext(i)
            ' CheckBox1.Text = CheckBox1.Text.Replace("\n", vbNewLine) disable this until I find a way to not detect C:\nVidia as being a new line
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox2.txt") '// add each line as String Array.

        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\checkbox2.txt") '// add each line as String Array.
        End Try
        CheckBox2.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox2.Text = CheckBox2.Text
            End If
            CheckBox2.Text = CheckBox2.Text & buttontext(i)
            CheckBox2.Text = CheckBox2.Text.Replace("\n", vbNewLine)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox4.txt") '// add each line as String Array.
        CheckBox4.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox4.Text = CheckBox4.Text
            End If
            CheckBox4.Text = CheckBox4.Text & buttontext(i)
            CheckBox4.Text = CheckBox4.Text.Replace("\n", vbNewLine)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3.txt") '// add each line as String Array.
        CheckBox3.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox3.Text = CheckBox3.Text
            End If
            CheckBox3.Text = CheckBox3.Text & buttontext(i)
            CheckBox3.Text = CheckBox3.Text.Replace("\n", vbNewLine)
        Next


        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox7amd.txt") '// add each line as String Array.
        CheckBox7.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox7.Text = CheckBox7.Text
            End If
            CheckBox7.Text = CheckBox7.Text & buttontext(i)
            CheckBox7.Text = CheckBox7.Text.Replace("\n", vbNewLine)
        Next


        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox5.txt") '// add each line as String Array.
        CheckBox5.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox5.Text = CheckBox5.Text
            End If
            CheckBox5.Text = CheckBox5.Text & buttontext(i)
            CheckBox5.Text = CheckBox5.Text.Replace("\n", vbNewLine)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox6.txt") '// add each line as String Array.
        CheckBox6.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox6.Text = CheckBox6.Text
            End If
            CheckBox6.Text = CheckBox6.Text & buttontext(i)
            CheckBox6.Text = CheckBox6.Text.Replace("\n", vbNewLine)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox8.txt") '// add each line as String Array.
        CheckBox8.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox8.Text = CheckBox8.Text
            End If
            CheckBox8.Text = CheckBox8.Text & buttontext(i)
            CheckBox8.Text = CheckBox8.Text.Replace("\n", vbNewLine)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox9.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\checkbox9.txt") '// add each line as String Array.
        End Try
        CheckBox9.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox9.Text = CheckBox9.Text
            End If
            CheckBox9.Text = CheckBox9.Text & buttontext(i)
            CheckBox9.Text = CheckBox9.Text.Replace("\n", vbNewLine)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox10.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\checkbox10.txt") '// add each line as String Array.
        End Try
        CheckBox10.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox10.Text = CheckBox10.Text
            End If
            CheckBox10.Text = CheckBox10.Text & buttontext(i)
            CheckBox10.Text = CheckBox10.Text.Replace("\n", vbNewLine)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox11.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\checkbox11.txt") '// add each line as String Array.
        End Try
        CheckBox11.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox11.Text = CheckBox11.Text
            End If
            CheckBox11.Text = CheckBox11.Text & buttontext(i)
            CheckBox11.Text = CheckBox11.Text.Replace("\n", vbNewLine)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\options.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\options.txt") '// add each line as String Array.
        End Try
        Me.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                Me.Text = Me.Text & vbNewLine
            End If
            Me.Text = Me.Text & buttontext(i)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\olabel1.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\olabel1.txt") '// add each line as String Array.
        End Try
        Label1.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                Label1.Text = Label1.Text & vbNewLine
            End If
            Label1.Text = Label1.Text & buttontext(i)
        Next

        Try
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\olabel2.txt") '// add each line as String Array.
        Catch ex As Exception
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\English\olabel2.txt") '// add each line as String Array.
        End Try
        Label2.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                Label2.Text = Label2.Text & vbNewLine
            End If
            Label2.Text = Label2.Text & buttontext(i)
        Next

        '-------------------------------------
        'Resize the option window if necessary
        '-------------------------------------


        If CheckBox2.Width + CheckBox2.Location.X > CheckBox5.Width + CheckBox5.Location.X Then
            If CheckBox2.Width + CheckBox2.Location.X > CheckBox6.Width + CheckBox6.Location.X Then

                If CheckBox2.Width + CheckBox2.Location.X > CheckBox10.Width + CheckBox10.Location.X Then
                    Me.Size = New System.Drawing.Size(CheckBox2.Width + CheckBox2.Location.X + 10, 559)
                End If

            End If

        ElseIf CheckBox5.Width + CheckBox5.Location.X > CheckBox6.Width + CheckBox6.Location.X Then

            If CheckBox5.Width + CheckBox5.Location.X > CheckBox10.Width + CheckBox10.Location.X Then
                Me.Size = New System.Drawing.Size(CheckBox5.Width + CheckBox5.Location.X + 10, 559)
            End If

        ElseIf CheckBox6.Width + CheckBox6.Location.X > CheckBox2.Width + CheckBox2.Location.X Then
            If CheckBox6.Width + CheckBox6.Location.X > CheckBox10.Width + CheckBox10.Location.X Then
                Me.Size = New System.Drawing.Size(CheckBox6.Width + CheckBox6.Location.X + 10, 559)
            End If

        ElseIf CheckBox10.Width + CheckBox10.Location.X > CheckBox2.Width + CheckBox2.Location.X Then
            If CheckBox10.Width + CheckBox10.Location.X > CheckBox5.Width + CheckBox5.Location.X Then
                If CheckBox10.Width + CheckBox10.Location.X > CheckBox6.Width + CheckBox6.Location.X Then
                    Me.Size = New System.Drawing.Size(CheckBox10.Width + CheckBox10.Location.X + 10, 559)
                End If
            End If


        End If




    End Sub

    Private Sub options_close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
        Form1.Show()
    End Sub

    Private Sub CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox6.CheckedChanged
        If CheckBox6.Checked = True Then
            setconfig("removemonitor", "true")
            removemonitor = True

        Else
            setconfig("removemonitor", "false")
            removemonitor = False
        End If
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = True Then
            setconfig("removecnvidia", "true")
            removecnvidia = True

        Else
            setconfig("removecnvidia", "false")
            removecnvidia = False
        End If
    End Sub

    Private Sub CheckBox8_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox8.CheckedChanged
        If CheckBox8.Checked = True Then
            setconfig("removecamd", "true")
            removecamd = True

        Else
            setconfig("removecamd", "false")
            removecamd = False
        End If
    End Sub

    Private Sub CheckBox7_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox7.CheckedChanged

        If CheckBox7.Checked = True Then
            setconfig("removeamdaudiobus", "true")
            removeamdaudiobus = True
        Else
            setconfig("removeamdaudiobus", "false")
            removeamdaudiobus = False
        End If

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub


    Private Sub CheckBox9_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox9.CheckedChanged
        If CheckBox9.Checked = True Then
            setconfig("removeamdkmpfd", "true")
            removeamdkmpfd = True

        Else
            setconfig("removeamdkmpfd", "false")
            removeamdkmpfd = False
        End If
    End Sub

    Private Sub CheckBox10_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox10.CheckedChanged
        If CheckBox10.Checked = True Then
            setconfig("showsafemodebox", "true")
            safemodemb = True
        Else
            setconfig("showsafemodebox", "false")
            safemodemb = False
        End If
    End Sub

    Private Sub CheckBox11_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox11.CheckedChanged
        If CheckBox11.Checked = True Then
            setconfig("roamingcfg", "true")
            roamingcfg = True
        Else
            setconfig("roamingcfg", "false")
            roamingcfg = False
        End If
        Dim userpth As String = My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("ProfilesDirectory") + "\"
        Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        'MessageBox.Show(userpthn)
        If CheckBox11.Checked = True Then
            If Not My.Computer.FileSystem.DirectoryExists(userpthn & "\Display Driver Uninstaller") Then
                '  My.Computer.FileSystem.CreateDirectory(userpthn & "\Display Driver Uninstaller")
                System.IO.Directory.CreateDirectory(userpthn & "\Display Driver Uninstaller")
                System.Threading.Thread.Sleep(75)
                My.Computer.FileSystem.CopyFile(Application.StartupPath & "\settings\config.cfg", userpthn & "\Display Driver Uninstaller\config.cfg")
            End If
        End If
        If CheckBox11.Checked = False Then
            If My.Computer.FileSystem.DirectoryExists(userpthn & "\Display Driver Uninstaller") Then
                If My.Computer.FileSystem.FileExists(userpthn & "\Display Driver Uninstaller\config.cfg") Then
                    My.Computer.FileSystem.CopyFile(userpthn & "\Display Driver Uninstaller\config.cfg", Application.StartupPath & "\settings\config.cfg", True)
                    System.Threading.Thread.Sleep(75)
                    My.Computer.FileSystem.DeleteDirectory(userpthn & "\Display Driver Uninstaller", FileIO.DeleteDirectoryOption.DeleteAllContents)
                End If
            End If
        End If

    End Sub
End Class
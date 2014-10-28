Imports WindowsApplication1.Form1
Public Class options
    Dim buttontext As String()
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


        If combobox1value = "AMD" Then
            If CheckBox3.Checked = True Then
                setconfig("removeamdaudiobus", "true")
                removeamdaudiobus = True
            Else
                setconfig("removeamdaudiobus", "false")
                removeamdaudiobus = False
            End If
        End If
    End Sub

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

        If combobox1value = "NVIDIA" Then
            If settings.getconfig("removephysx") = "true" Then
                CheckBox3.Checked = True
                removephysx = True
            Else
                CheckBox3.Checked = False
                removephysx = False
            End If
        End If

        If combobox1value = "AMD" Then
            If settings.getconfig("removeamdaudiobus") = "true" Then
                CheckBox3.Checked = True
                removeamdaudiobus = True
            Else
                CheckBox3.Checked = False
                removeamdaudiobus = False
            End If
        End If

        If settings.getconfig("removemonitor") = "true" Then
            CheckBox6.Checked = True
        Else
            CheckBox6.Checked = False
        End If

        'Check some values.
        If combobox1value = "NVIDIA" Then
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
            CheckBox3.Visible = True
            CheckBox4.Visible = True
        End If
        If combobox1value = "AMD" Then
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3amd.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
            CheckBox3.Visible = True
            CheckBox4.Visible = False
        End If
        If combobox1value = "INTEL" Then
            CheckBox3.Visible = False
            CheckBox4.Visible = False
        End If

        'Init language for checkboxes
        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox1.txt") '// add each line as String Array.
        CheckBox1.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox1.Text = CheckBox1.Text
            End If
            CheckBox1.Text = CheckBox1.Text & buttontext(i)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox2.txt") '// add each line as String Array.
        CheckBox2.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox2.Text = CheckBox2.Text
            End If
            CheckBox2.Text = CheckBox2.Text & buttontext(i)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox4.txt") '// add each line as String Array.
        CheckBox4.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox4.Text = CheckBox4.Text
            End If
            CheckBox4.Text = CheckBox4.Text & buttontext(i)
        Next

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3.txt") '// add each line as String Array.
        CheckBox3.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox3.Text = CheckBox3.Text
            End If
            CheckBox3.Text = CheckBox3.Text & buttontext(i)
        Next

        If combobox1value = "AMD" Then
            buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox3amd.txt") '// add each line as String Array.
            CheckBox3.Text = ""
            For i As Integer = 0 To buttontext.Length - 1
                If i <> 0 Then
                    CheckBox3.Text = CheckBox3.Text
                End If
                CheckBox3.Text = CheckBox3.Text & buttontext(i)
            Next
        End If

        buttontext = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & combobox2value & "\checkbox5.txt") '// add each line as String Array.
        CheckBox5.Text = ""
        For i As Integer = 0 To buttontext.Length - 1
            If i <> 0 Then
                CheckBox5.Text = CheckBox5.Text
            End If
            CheckBox5.Text = CheckBox5.Text & buttontext(i)
        Next

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
End Class
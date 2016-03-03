Imports WindowsApplication1.frmMain
Public Class frmOptions
	Dim buttontext As String()
	Dim toolTip1 As New ToolTip()
	'Dim userpthn As String = System.Environment.GetEnvironmentVariable("appdata")
	'Dim userpthn As String = My.Computer.Registry.LocalMachine.OpenSubKey("software\microsoft\windows nt\currentversion\profilelist").GetValue("Public")
	Dim userpthn As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)

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

	Private Sub frmOptions_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
		If e.KeyCode = Keys.Escape Then Me.Close()
	End Sub

	Private Sub frmOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		toolTip1.AutoPopDelay = 3000
		toolTip1.InitialDelay = 1000
		toolTip1.ReshowDelay = 250
		toolTip1.ShowAlways = True
		frmMain.Enabled = False

		Language.TranslateForm(Me, toolTip1)
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

		If settings.getconfig("removegfe") = "true" Then
			CheckBox13.Checked = True
			removegfe = True
		Else
			CheckBox13.Checked = False
			removegfe = False
		End If

		If settings.getconfig("donotcheckupdatestartup") = "true" Then
			CheckBox12.Checked = True
			donotcheckupdatestartup = True
		Else
			CheckBox12.Checked = False
			donotcheckupdatestartup = False
		End If

		If settings.getconfig("removedxcache") = "true" Then
			CheckBox14.Checked = True
			removedxcache = True
		Else
			CheckBox14.Checked = False
			removedxcache = False
		End If


		'-------------------------------------
		'Resize the option window if necessary
		'-------------------------------------


		If CheckBox2.Width + CheckBox2.Location.X > CheckBox5.Width + CheckBox5.Location.X AndAlso
		 CheckBox2.Width + CheckBox2.Location.X > CheckBox6.Width + CheckBox6.Location.X AndAlso
		 CheckBox2.Width + CheckBox2.Location.X > CheckBox10.Width + CheckBox10.Location.X AndAlso
		 CheckBox2.Width + CheckBox2.Location.X > CheckBox11.Width + CheckBox11.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox2.Width + CheckBox2.Location.X + 10, Me.Height)


		ElseIf CheckBox5.Width + CheckBox5.Location.X > CheckBox6.Width + CheckBox6.Location.X AndAlso
		 CheckBox5.Width + CheckBox5.Location.X > CheckBox10.Width + CheckBox10.Location.X AndAlso
		 CheckBox5.Width + CheckBox5.Location.X > CheckBox11.Width + CheckBox11.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox5.Width + CheckBox5.Location.X + 10, Me.Height)


		ElseIf CheckBox6.Width + CheckBox6.Location.X > CheckBox2.Width + CheckBox2.Location.X AndAlso
		 CheckBox6.Width + CheckBox6.Location.X > CheckBox10.Width + CheckBox10.Location.X AndAlso
		 CheckBox6.Width + CheckBox6.Location.X > CheckBox11.Width + CheckBox11.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox6.Width + CheckBox6.Location.X + 10, Me.Height)

		ElseIf CheckBox10.Width + CheckBox10.Location.X > CheckBox2.Width + CheckBox2.Location.X AndAlso
			CheckBox10.Width + CheckBox10.Location.X > CheckBox5.Width + CheckBox5.Location.X AndAlso
			CheckBox10.Width + CheckBox10.Location.X > CheckBox6.Width + CheckBox6.Location.X AndAlso
			CheckBox10.Width + CheckBox10.Location.X > CheckBox11.Width + CheckBox11.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox10.Width + CheckBox10.Location.X + 10, Me.Height)

		ElseIf CheckBox11.Width + CheckBox11.Location.X > CheckBox2.Width + CheckBox2.Location.X AndAlso
			CheckBox11.Width + CheckBox11.Location.X > CheckBox5.Width + CheckBox5.Location.X AndAlso
			CheckBox11.Width + CheckBox11.Location.X > CheckBox6.Width + CheckBox6.Location.X AndAlso
			CheckBox11.Width + CheckBox11.Location.X > CheckBox10.Width + CheckBox10.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox11.Width + CheckBox11.Location.X + 10, Me.Height)

		End If
		If Me.Size.Width < CheckBox12.Width + CheckBox12.Location.X Then
			Me.Size = New System.Drawing.Size(CheckBox12.Width + CheckBox12.Location.X + 10, Me.Height)
		End If
	End Sub

	Private Sub frmOptions_Close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
		frmMain.Enabled = True
	End Sub

	Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
		Me.Close()
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

	Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
		If CheckBox2.Checked = True Then
			setconfig("logbox", "true")

		Else
			setconfig("logbox", "false")
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
			trysystemrestore = True
		Else
			setconfig("systemrestore", "false")
			trysystemrestore = False
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

	Private Sub CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox6.CheckedChanged
		If CheckBox6.Checked = True Then
			setconfig("removemonitor", "true")
			removemonitor = True

		Else
			setconfig("removemonitor", "false")
			removemonitor = False
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

	Private Sub CheckBox12_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox12.CheckedChanged
		If CheckBox12.Checked = True Then
			setconfig("donotcheckupdatestartup", "true")
			donotcheckupdatestartup = True
		Else
			setconfig("donotcheckupdatestartup", "false")
			donotcheckupdatestartup = False
		End If
	End Sub

	Private Sub CheckBox13_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox13.CheckedChanged
		If CheckBox13.Checked = True Then
			setconfig("removegfe", "true")
			removegfe = True
		Else
			setconfig("removegfe", "false")
			removegfe = False
		End If
	End Sub

	Private Sub CheckBox14_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox14.CheckedChanged
		If CheckBox14.Checked = True Then
			setconfig("removedxcache", "true")
			removedxcache = True
		Else
			setconfig("removedxcache", "false")
			removedxcache = False
		End If
	End Sub
End Class
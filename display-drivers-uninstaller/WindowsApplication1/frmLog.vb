Public Class frmLog


	Private Sub frmLog_Close(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
		e.Cancel = True
		Me.Hide()
	End Sub

    Private Sub frmLog_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub frmLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Language.TranslateForm(Me)

        If TextBox1.Text = "" Or TextBox1.Text = Nothing Then
            TextBox1.Text = frmMain.TextBox1.Text
        End If

        btnClose.Select() 'prevents the textbox from getting highlighted for some reason

        'This is a workaround to get to fix the window control on different DPI
        Me.AutoSize = True  'This will allow to set the windows according to the DPI
        Me.AutoSize = False 'This will allow the resize of the application.
        Me.Size = New Size(Me.Size.Width + 1, Me.Size.Height + 1)
    End Sub
End Class
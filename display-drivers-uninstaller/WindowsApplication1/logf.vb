Public Class logf


    Private Sub logf_Close(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        e.Cancel = True
        Me.Hide()
    End Sub

    Private Sub logf_Resize(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Resize
        TextBox1.Size = New Size(Me.Size.Width - 31, Me.Size.Height - 87)
        Button1.Location = New Point(Me.Size.Width - 103, Me.Size.Height - 74)
    End Sub
    Private Sub options_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub logf_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If TextBox1.Text = "" Or TextBox1.Text = Nothing Then
            TextBox1.Text = Form1.TextBox1.Text
        End If
        Button1.Select() 'prevents the textbox from getting highlighted for some reason
    End Sub
End Class
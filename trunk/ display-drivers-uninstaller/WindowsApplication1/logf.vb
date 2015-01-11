Public Class logf


    Private Sub logf_Close(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        e.Cancel = True
        Me.Hide()
    End Sub

    Private Sub logf_Resize(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Resize
        TextBox1.Size = New Size(Me.Size.Width - 31, Me.Size.Height - 53)
    End Sub
End Class
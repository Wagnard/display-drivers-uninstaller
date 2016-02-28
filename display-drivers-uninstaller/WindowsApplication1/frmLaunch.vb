Public Class frmLaunch
    Public selection As Integer = -1

    Private Sub btnAccept_Click(sender As System.Object, e As System.EventArgs) Handles btnAccept.Click
        Me.DialogResult = Windows.Forms.DialogResult.OK

        selection = cbBootOption.SelectedIndex
        Me.Close()
    End Sub

    Private Sub btnClose_Click(sender As System.Object, e As System.EventArgs)
        Me.DialogResult = Windows.Forms.DialogResult.Cancel

        selection = cbBootOption.SelectedIndex
        Me.Close()
    End Sub

    Private Sub frmLaunch_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        cbBootOption.SelectedIndex = 0
    End Sub

    Private Sub cbBootOption_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbBootOption.SelectedIndexChanged
        If cbBootOption.SelectedIndex = 0 Then
            btnAccept.Text = "Launch"
        Else
            btnAccept.Text = "Reboot to" + vbCrLf + "Safe Mode"
        End If
    End Sub

End Class

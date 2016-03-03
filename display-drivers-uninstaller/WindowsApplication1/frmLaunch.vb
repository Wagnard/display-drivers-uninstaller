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
        Language.TranslateForm(Me)
        cbBootOption.SelectedIndex = 0
    End Sub

    Private Sub cbBootOption_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbBootOption.SelectedIndexChanged
        If cbBootOption.SelectedIndex = 0 Then
            btnAccept.Text = Language.GetTranslation(Me.Name, btnAccept.Name, "Text")
        Else
            btnAccept.Text = Language.GetTranslation(Me.Name, btnAccept.Name, "Text2")
        End If
    End Sub

End Class

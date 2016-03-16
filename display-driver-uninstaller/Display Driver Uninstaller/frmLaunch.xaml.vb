Public Class frmLaunch
    Public selection As Integer = -1

    Private Sub btnAccept_Click(sender As Object, e As RoutedEventArgs) Handles btnAccept.Click
        Me.DialogResult = Windows.Forms.DialogResult.OK

        selection = cbBootOption.SelectedIndex
        Me.Close()
    End Sub

    Private Sub btnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
        Me.DialogResult = Windows.Forms.DialogResult.Cancel

        selection = cbBootOption.SelectedIndex
        Me.Close()
    End Sub

    Private Sub frmLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
        '   Language.TranslateForm(Me)
        cbBootOption.SelectedIndex = 0
    End Sub

    Private Sub cbBootOption_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbBootOption.SelectionChanged
        If cbBootOption.SelectedIndex = 0 Then
            '         btnAccept.Content = Language.GetTranslation(Me.Name, btnAccept.Name, "Text")
        Else
            '        btnAccept.Content = Language.GetTranslation(Me.Name, btnAccept.Name, "Text2")
        End If
    End Sub

End Class


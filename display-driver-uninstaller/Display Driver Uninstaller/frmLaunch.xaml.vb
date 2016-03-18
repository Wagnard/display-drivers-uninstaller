Public Class frmLaunch
	Public selection As Integer = -1

	Private Sub btnAccept_Click(sender As Object, e As RoutedEventArgs) Handles btnAccept.Click
		selection = cbBootOption.SelectedIndex

		Me.DialogResult = True
		Me.Close()
	End Sub

	Private Sub btnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
		selection = cbBootOption.SelectedIndex

		Me.DialogResult = False
		Me.Close()
	End Sub

	Private Sub frmLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
        Me.Visibility = Windows.Visibility.Visible
        Languages.TranslateForm(Me)
	End Sub

	Private Sub cbBootOption_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbBootOption.SelectionChanged
		If cbBootOption.SelectedIndex = 0 Then
			'         btnAccept.Content = Language.GetTranslation(Me.Name, btnAccept.Name, "Text")
		Else
			'        btnAccept.Content = Language.GetTranslation(Me.Name, btnAccept.Name, "Text2")
		End If
	End Sub

End Class


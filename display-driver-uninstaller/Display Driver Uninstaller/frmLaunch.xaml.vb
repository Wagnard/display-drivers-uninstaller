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
		Dim tb As TextBlock = TryCast(btnAccept.Content, TextBlock)

		If tb IsNot Nothing Then
			tb.Text = Languages.GetTranslation(Me.Name, btnAccept.Name, If(cbBootOption.SelectedIndex = 0, "Text", "Text2"))
		End If
	End Sub

	Private Sub frmLaunch_ContentRendered(sender As Object, e As EventArgs) Handles MyBase.ContentRendered
		Me.Topmost = False
	End Sub
End Class


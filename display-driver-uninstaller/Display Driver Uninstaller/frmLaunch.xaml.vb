Namespace Display_Driver_Uninstaller
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

		Private Sub frmLaunch_ContentRendered(sender As Object, e As System.EventArgs) Handles Me.ContentRendered
			Me.Topmost = False
			Dim Checkupdate As New CheckUpdate
			If Application.Settings.ProcessKilled Then
				'		MessageBox.Show(Languages.GetTranslation("frmLaunch", "Messages", "Text1"), Application.Settings.AppName, Nothing, MessageBoxImage.Information)
				'	Application.Settings.ProcessKilled = False
			End If

			Checkupdate.CheckUpdates()

		End Sub
		Private Sub btnWuRestore_Click(sender As Object, e As EventArgs) Handles btnWuRestore.Click
			frmMain.EnableDriverSearch(True)
			selection = cbBootOption.SelectedIndex

			Me.DialogResult = False
			Me.Close()
		End Sub
		Private Sub frmLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
			Languages.TranslateForm(Me)
			Debug.WriteLine(Application.Settings.PreventWinUpdate)
		End Sub

		Private Sub cbBootOption_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cbBootOption.SelectionChanged
			Dim tb As TextBlock = TryCast(btnAccept.Content, TextBlock)

			If tb IsNot Nothing Then
				tb.Text = Languages.GetTranslation(Me.Name, btnAccept.Name, If(cbBootOption.SelectedIndex = 0, "Text", "Text2"))
			End If
		End Sub

	End Class

End Namespace
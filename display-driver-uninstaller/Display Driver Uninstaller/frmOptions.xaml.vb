Namespace Display_Driver_Uninstaller
	Public Class FrmOptions

		Private Sub FrmOptions_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
			Languages.TranslateForm(Me)
			Application.Settings.PreventWinUpdate = FrmMain.InfoDriverSearch
			'AdjustWindow(Me)
		End Sub

		Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
			Me.Close()
		End Sub

		Private Sub Chk_lblPreventWinUpdate(sender As Object, e As RoutedEventArgs) Handles lblPreventWinUpdate.Click

			If lblPreventWinUpdate.IsChecked Then
				FrmMain.EnableDriverSearch(False)
			Else
				FrmMain.EnableDriverSearch(True)
			End If

		End Sub
	End Class
End Namespace
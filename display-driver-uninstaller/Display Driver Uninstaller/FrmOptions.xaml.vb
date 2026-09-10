Namespace Display_Driver_Uninstaller
	Public Class FrmOptions

		Public Sub New()
			InitializeComponent()
			Application.ApplyWindowTheme(Me)
		End Sub

		Private Sub FrmOptions_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
			Languages.TranslateForm(Me)
			Application.Settings.PreventWinUpdate = FrmMain.InfoDriverSearch
			'AdjustWindow(Me)

			'Open on the tab of the GPU selected in the main window.
			Select Case Application.Settings.SelectedGPU
				Case GPUVendor.Nvidia : tabOptions.SelectedIndex = 1
				Case GPUVendor.AMD : tabOptions.SelectedIndex = 2
				Case GPUVendor.Intel : tabOptions.SelectedIndex = 3
				Case GPUVendor.Qualcomm : tabOptions.SelectedIndex = 4
			End Select
		End Sub

		'Only the selected tab is in the visual tree, so TranslateForm misses the others : translate again
		'once the new tab's content is laid out.
		Private Sub TabOptions_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles tabOptions.SelectionChanged
			If Not Me.IsLoaded Then Return
			Dispatcher.BeginInvoke(Sub() Languages.TranslateForm(Me, False), Threading.DispatcherPriority.Loaded)
		End Sub

		Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
			Me.Close()
		End Sub

		Private Sub BtnResetDefaults_Click(sender As Object, e As RoutedEventArgs) Handles btnResetDefaults.Click
			Dim confirm As String = Languages.GetTranslation("frmOptions", "Messages", "Text1")
			If String.IsNullOrWhiteSpace(confirm) Then
				confirm = "Reset all options to their recommended default values?"
			End If

			If Application.ShowThemedNotice(confirm, Nothing, MessageBoxButton.YesNo, Me) <> MessageBoxResult.Yes Then
				Return
			End If

			Application.Settings.ResetToDefaults()

			' PreventWinUpdate mirrors the real registry state: re-enable Windows driver
			' search to match the default (False), then reflect the actual state back.
			FrmMain.EnableDriverSearch(True)
			Application.Settings.PreventWinUpdate = FrmMain.InfoDriverSearch()

			' Dark theme defaulted back to light: refresh every open window.
			Application.ApplyThemeToAllWindows()
		End Sub

        Private Sub lblUseDarkTheme_Click(sender As Object, e As RoutedEventArgs) Handles lblUseDarkTheme.Click
            Application.ApplyThemeToAllWindows()
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

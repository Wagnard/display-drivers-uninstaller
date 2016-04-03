Public Class frmLog

	Private Sub Close_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
		Me.Close()
	End Sub

	Private Sub frmLog_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		lbLog.Items.Refresh()
		Languages.TranslateForm(Me)
	End Sub

	Private Sub btnLoadLog_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnLoadLog.Click
		Using ofd As System.Windows.Forms.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
			ofd.Filter = "DDU Log (*.xml)|*.xml"
			ofd.FilterIndex = 0

			If ofd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Dim newLog As New AppLog
				newLog.OpenFromFile(ofd.FileName)

				Dim newLogWindow As New frmLog With
				 {
				   .Title = ofd.FileName,
				   .Owner = Me,
				   .ShowInTaskbar = False,
				   .Width = Me.ActualWidth,
				   .Height = Me.ActualHeight,
				   .WindowState = Me.WindowState,
				   .WindowStartupLocation = Windows.WindowStartupLocation.CenterOwner
				  }

				newLogWindow.btnLoadLog.Visibility = Windows.Visibility.Collapsed
				newLogWindow.lbLog.DataContext = newLog.LogEntries
				newLogWindow.tbOpenedLog.Visibility = Windows.Visibility.Visible
				newLogWindow.tbOpenedLog.Text = ofd.FileName

				Me.Visibility = Windows.Visibility.Hidden

				newLogWindow.ShowDialog()

				Me.WindowState = newLogWindow.WindowState
				Me.Width = newLogWindow.ActualWidth
				Me.Height = newLogWindow.ActualHeight
				Me.Top = newLogWindow.Top
				Me.Left = newLogWindow.Left

				Me.Visibility = Windows.Visibility.Visible


				newLog.Clear()
				newLog = Nothing
			End If
		End Using
	End Sub

End Class

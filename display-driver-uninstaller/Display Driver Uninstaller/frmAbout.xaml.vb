Public Class frmAbout

	Public Property Text As String

	Private Sub frmAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
		Me.DataContext = Me
	End Sub

End Class

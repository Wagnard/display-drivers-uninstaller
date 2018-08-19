Public Class AUDIOCleanup
	'todo
	Public Sub start(ByVal config As ThreadSettings)
		Dim vendidexpected As String = ""
		Select Case config.SelectedAUDIO
			Case AudioVendor.Realtek
				vendidexpected = "VEN_10EC"
			Case AudioVendor.SoundBlaster
				vendidexpected = "VEN_1102"
			Case AudioVendor.None
				vendidexpected = "NONE"
		End Select

		If vendidexpected = "NONE" Then
			Application.Log.AddWarningMessage("VendID is NONE, this is unexpected, cleaning aborted.")
			Exit Sub
		End If

	End Sub
End Class

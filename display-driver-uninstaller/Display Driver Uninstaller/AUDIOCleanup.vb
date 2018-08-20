Imports Display_Driver_Uninstaller.Win32

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

		'----------------------------------
		'Removing the Audio card-----------
		'----------------------------------

		Try
			Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("media", vendidexpected, False)
			If found.Count > 0 Then
				For Each d As SetupAPI.Device In found
					SetupAPI.UninstallDevice(d)
				Next
				found.Clear()
			End If
		Catch ex As Exception
			'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			Application.Log.AddException(ex)
		End Try

	End Sub
End Class

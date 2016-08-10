Imports Microsoft.VisualBasic
Imports Microsoft.Win32



Public Class MyRegistry


	Public Shared Function OpenSubKey(RootKey As RegistryKey, Key As String, Optional Writable As Boolean = False) As RegistryKey
		Dim FixPerm As Boolean = False
		Dim FullPath As String = (RootKey.ToString) + "\" + Key

		Try
			Return RootKey.OpenSubKey(Key, Writable)
		Catch ex As System.Security.SecurityException
			FixPerm = True

			Application.Log.AddWarningMessage("Access to : " + Chr(34) + FullPath + Chr(34) + " is denied! Will add permissions.")
		End Try

		If FixPerm Then
			Win32.ACL.Registry.FixRights(FullPath)
		End If

		Try
			RootKey.OpenSubKey(Key, Writable)
			Return RootKey.OpenSubKey(Key, Writable)
		Catch ex As Exception
			Return RootKey.OpenSubKey(Key, Writable)
		End Try
	End Function

End Class

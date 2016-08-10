Imports Microsoft.VisualBasic
Imports Microsoft.Win32


Public Class MyRegistry

	Private Shared Function GetRootKey(ByVal name As String) As String
		Select Case True
			Case name.StartsWith("CLASSESROOT", StringComparison.OrdinalIgnoreCase)
				Return "HKEY_CLASSES_ROOT\"
			Case name.StartsWith("CURRENTUSER", StringComparison.OrdinalIgnoreCase)
				Return "HKEY_CURRENT_USER\"
			Case name.StartsWith("CURRENTCONFIG", StringComparison.OrdinalIgnoreCase)
				Return "HKEY_CURRENT_CONFIG\"
			Case name.StartsWith("LOCALMACHINE", StringComparison.OrdinalIgnoreCase)
				Return "HKEY_LOCAL_MACHINE\"
			Case name.StartsWith("USERS", StringComparison.OrdinalIgnoreCase)
				Return "HKEY_USERS\"
			Case Else
				Throw New ArgumentException("name is unknown!" & Environment.NewLine & name, "name")
		End Select
	End Function

	Public Shared Function OpenSubKey(RootKey As RegistryHive, Key As String, Optional Writable As Boolean = False) As MyRegistry
		Dim FixPerm As Boolean = False
		Dim FullPath As String = GetRootKey(RootKey.ToString) + Key
		Try


			Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey(Key, Writable)
				If regkey IsNot Nothing Then
					Return regkey
				Else
					FixPerm = True
				End If
			End Using
		Catch ex As System.Security.SecurityException
			FixPerm = True
			MsgBox("Fixing Permission")
			MsgBox(FullPath)
		End Try

		If FixPerm Then
			Win32.ACL.Registry.FixRights(FullPath)
		End If

		Try
			Using regkey As RegistryKey = Registry.LocalMachine.OpenSubKey(Key, Writable)
				If regkey IsNot Nothing Then
					Return regkey
				Else
					FixPerm = True
				End If
			End Using
		Catch ex As Exception

		End Try
	End Function

End Class

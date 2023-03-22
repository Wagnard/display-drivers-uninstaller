Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32

Namespace Display_Driver_Uninstaller
	Public Class MyRegistry

		Public Shared Function OpenSubKey(ByVal RootKey As RegistryKey, ByVal Key As String, Optional ByVal Writable As Boolean = False) As RegistryKey
			If RootKey Is Nothing OrElse IsNullOrWhitespace(Key) Then
				Return Nothing
			End If

			Return OpenSubKeyInternal(RootKey, Key, Writable, False)
		End Function

		Private Shared Function OpenSubKeyInternal(ByVal RootKey As RegistryKey, ByVal Key As String, ByVal Writable As Boolean, ByVal fixedACL As Boolean) As RegistryKey
			Dim fullPath As String = RootKey.ToString() & IO.Path.DirectorySeparatorChar & Key

			Try
				Return RootKey.OpenSubKey(Key, Writable)
			Catch ex As Security.SecurityException
				If Not fixedACL Then
					Dim logEntry As New LogEntry()
					logEntry.Type = LogType.Warning
					logEntry.Message = "Access is denied! Attempting to fix path's permissions." & CRLF & ">> " & fullPath
					logEntry.Add("fullPath", fullPath)

					Dim success As Boolean = ACL.Registry.FixRights(fullPath, logEntry)

					logEntry.Add(KvP.Empty)
					logEntry.Add("fixed?", If(success, "Yes", "No"))

					Application.Log.Add(logEntry)

					If success Then
						Return OpenSubKeyInternal(RootKey, Key, Writable, True)
					Else
						logEntry.Type = LogType.Error
						Return Nothing
					End If
				Else
					Application.Log.AddException(ex)
					Return Nothing
				End If
			Catch ex As Exception
				Dim logEntry As LogEntry = New LogEntry(ex)
				logEntry.Type = LogType.Error
				logEntry.Message = String.Concat("Couldn't open registry key!", CRLF, ">> " & fullPath)
				logEntry.Add("fullPath", fullPath)
				logEntry.Add("fixedAcl", fixedACL.ToString())

				Application.Log.Add(logEntry)
				Return Nothing
			End Try

		End Function

	End Class
End Namespace
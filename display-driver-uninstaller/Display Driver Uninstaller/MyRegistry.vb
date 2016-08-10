Imports Microsoft.VisualBasic
Imports Microsoft.Win32



Public Class MyRegistry
	Implements IDisposable

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

#Region "IDisposable Support"
	Private disposedValue As Boolean ' To detect redundant calls

	' IDisposable
	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me.disposedValue Then
			If disposing Then
				' TODO: dispose managed state (managed objects).

			End If

			' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
			' TODO: set large fields to null.
		End If
		Me.disposedValue = True
	End Sub

	' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
	'Protected Overrides Sub Finalize()
	'    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
	'    Dispose(False)
	'    MyBase.Finalize()
	'End Sub

	' This code added by Visual Basic to correctly implement the disposable pattern.
	Public Sub Dispose() Implements IDisposable.Dispose
		' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub
#End Region

End Class

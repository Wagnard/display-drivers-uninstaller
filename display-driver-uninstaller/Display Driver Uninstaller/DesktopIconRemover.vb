Imports System.IO
Imports IWshRuntimeLibrary
Public Class DesktopIconRemover
    Public Shared Function GetShortcutTargetPath(shortcutPath As String) As String
        If System.IO.File.Exists(shortcutPath) Then
            Dim shell As WshShell = New WshShell()
            Dim shortcut As IWshShortcut = CType(shell.CreateShortcut(shortcutPath), IWshShortcut)
            Return shortcut.TargetPath
        Else
            Throw New FileNotFoundException("Shortcut file not found.", shortcutPath)
        End If
    End Function
End Class
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Win32

Public Class frmSystemRestore
	Implements IDisposable

	Private disposed As Boolean
    Private ReadOnly canClose2 As New EventWaitHandle(True, EventResetMode.ManualReset) ' Thread safe!

	Private Sub frmSystemRestore_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
		Languages.TranslateForm(Me)
	End Sub

    Private Sub CreateSystemRestore()
        canClose2.Reset()

        Try
            Try
                Using regKey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", RegistryKeyPermissionCheck.ReadWriteSubTree, Security.AccessControl.RegistryRights.SetValue)
                    If regKey IsNot Nothing Then
                        regKey.SetValue("SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord)
                    End If
                End Using
            Catch ex As Exception
                Application.Log.AddException(ex, "Settings value for RegistryKey 'SystemRestorePointCreationFrequency' failed!")
            End Try

            Dim result As Int64 = 0

            ' RESTORE_TYPE.CHECKPOINT is used be System  (which also overrides Description)
            Win32.SystemRestore.StartRestore("DDU Restore Point", Win32.SystemRestore.RESTORE_TYPE.MODIFY_SETTINGS, result)

            Win32.SystemRestore.EndRestore(result)

            Application.Log.AddMessage("Restore Point Created")

            'Application.Log.AddMessage("Trying to Create a System Restored Point")
            'Dim oScope As New ManagementScope("\\localhost\root\default")
            'Dim oPath As New ManagementPath("SystemRestore")
            'Dim oGetOp As New ObjectGetOptions()
            'Dim oProcess As New ManagementClass(oScope, oPath, oGetOp)

            'Dim oInParams As ManagementBaseObject = oProcess.GetMethodParameters("CreateRestorePoint")
            'oInParams("Description") = "DDU System Restored Point"
            'oInParams("RestorePointType") = 12UI ' MODIFY_SETTINGS
            'oInParams("EventType") = 100UI

            'Dim oOutParams As ManagementBaseObject = oProcess.InvokeMethod("CreateRestorePoint", oInParams, Nothing)

            'Dim errCode As UInt32 = CUInt(oOutParams("ReturnValue"))

            'If errCode <> 0UI Then
            '	Throw New COMException("System Restored Point Could not be Created!", Win32.GetInt32(errCode))
            'End If

            'Application.Log.AddMessage("System Restored Point Created. Code: " + errCode.ToString())

        Catch ex As Exception
            Application.Log.AddWarning(ex, "System Restored Point could not be Created!")
        Finally
            canClose2.Set()
            CloseDDU()
        End Try
    End Sub

	Private Sub frmSystemRestore_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles MyBase.Closing
		If Not canClose2.WaitOne(0) Then
			e.Cancel = True
			Exit Sub
		End If
	End Sub

	Private Sub frmSystemRestore_ContentRendered(sender As Object, e As EventArgs) Handles MyBase.ContentRendered

        Dim thread As New Thread(AddressOf CreateSystemRestore)
        thread.Start()

    End Sub

	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me.disposed Then
			If disposing Then
				Try
					canClose2.Close()
				Catch ex As Exception
				End Try
			End If
		End If
	End Sub

	Public Sub Dispose() Implements IDisposable.Dispose
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub

    Private Sub CloseDDU()
        If Not Dispatcher.CheckAccess() Then
            Dispatcher.BeginInvoke(Sub() CloseDDU())
        Else
            Try
                Me.Close()
            Catch ex As Exception
                Application.Log.AddException(ex)
            End Try
        End If
    End Sub

End Class

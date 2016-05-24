Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class frmSystemRestore
	Implements IDisposable

	Private disposed As Boolean
	Private ReadOnly canClose2 As New EventWaitHandle(True, EventResetMode.ManualReset)	' Thread safe!

	Private Sub frmSystemRestore_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
		Languages.TranslateForm(Me)
	End Sub

	Private Sub CreateSystemRestore()
		canClose2.Reset()

		Try
			Dim result As Int64 = 0

			' Devmltk;	NOTE!
			'
			' You must initialize COM security to allow NetworkService, LocalService and System to call back into any process that uses SRSetRestorePoint. 
			' This is necessary for SRSetRestorePoint to operate properly. 
			' For information on setting up the COM calls to CoInitializeEx and CoInitializeSecurity, see Using System Restore.

			Win32.SystemRestore.StartRestore("DDU Restore Point", Win32.SystemRestore.RESTORE_TYPE.CHECKPOINT, result)

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
			Application.Log.AddWarning(ex, "System Restored Point Could not be Created!")
		Finally
			canClose2.Set()
		End Try
	End Sub

	Private Sub frmSystemRestore_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles MyBase.Closing
		If Not canClose2.WaitOne(0) Then
			e.Cancel = True
			Exit Sub
		End If
	End Sub

	Private Sub frmSystemRestore_ContentRendered(sender As Object, e As EventArgs) Handles MyBase.ContentRendered
		CreateSystemRestore()

		Me.Close()
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

End Class

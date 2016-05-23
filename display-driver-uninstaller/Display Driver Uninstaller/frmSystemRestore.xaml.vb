Imports System.Management
Imports System.Runtime.InteropServices

Public Class frmSystemRestore
	Dim CanClose As Boolean = False
	Private Sub frmSystemRestore_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded

		Languages.TranslateForm(Me)

	End Sub

	Private Sub createsystemrestore()
		CanClose = False
		Try

			Application.Log.AddMessage("Trying to Create a System Restored Point")
			Dim oScope As New ManagementScope("\\localhost\root\default")
			Dim oPath As New ManagementPath("SystemRestore")
			Dim oGetOp As New ObjectGetOptions()
			Dim oProcess As New ManagementClass(oScope, oPath, oGetOp)

			Dim oInParams As ManagementBaseObject = oProcess.GetMethodParameters("CreateRestorePoint")
			oInParams("Description") = "DDU System Restored Point"
			oInParams("RestorePointType") = 12UI ' MODIFY_SETTINGS
			oInParams("EventType") = 100UI

			Dim oOutParams As ManagementBaseObject = oProcess.InvokeMethod("CreateRestorePoint", oInParams, Nothing)

			Dim errCode As UInt32 = CUInt(oOutParams("ReturnValue"))

			If errCode <> 0UI Then
				Throw New COMException("System Restored Point Could not be Created!", Win32.GetInt32(errCode))
			End If

			Application.Log.AddMessage("System Restored Point Created. Code: " + errCode.ToString())

		Catch ex As Exception
			Application.Log.AddWarning(ex, "System Restored Point Could not be Created!")
			CanClose = True
		End Try
		CanClose = True
	End Sub

	Private Sub frmSystemRestore_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles MyBase.Closing
		If Not CanClose Then
			e.Cancel = True
			Exit Sub
		End If
	End Sub

	Private Sub frmSystemRestore_ContentRendered(sender As Object, e As EventArgs) Handles MyBase.ContentRendered
		createsystemrestore()
		Me.Close()
	End Sub
End Class

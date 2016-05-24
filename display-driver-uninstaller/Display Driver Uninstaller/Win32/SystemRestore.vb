Imports System.Collections.Generic
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.ComponentModel

Namespace Win32
	Friend Class SystemRestore
		Private Const MAX_DESC As Int32 = 64		' Ansi
		Private Const MAX_DESC_W As Int32 = 256		' Unicode


		<DllImport("srclient.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
		Private Shared Function SRSetRestorePoint(
 <[In]()> ByVal pRestorePtSpec As IntPtr,
 <[In](), [Out]()> ByVal pSMgrStatus As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
		Private Shared Function SearchPath(
   ByVal lpPath As String,
   ByVal lpFileName As String,
   ByVal lpExtension As String,
   ByVal nBufferLength As Integer,
   <MarshalAs(UnmanagedType.LPWStr)> ByVal lpBuffer As StringBuilder,
   ByVal lpFilePart As String) As UInteger
		End Function

		''' <summary>Contains information used by the SRSetRestorePoint function</summary>
		<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
		Private Structure RESTOREPOINTINFO

			''' <summary> The type of event</summary>
			Public dwEventType As UInt32

			''' <summary> The type of restore point.</summary>
			Public dwRestorePtType As UInt32

			''' <summary> The sequence number of the restore point. 
			''' To end a system change, set this to the sequence number returned by the previous call to SRSetRestorePoint.</summary>
			Public llSequenceNumber As Int64

			''' <summary> The description to be displayed so the user can easily identify a restore point</summary>
			<MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_DESC_W)>
			Public szDescription As String
		End Structure

		''' <summary>Contains status information used by the SRSetRestorePoint function</summary>
		<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
		Private Structure STATEMGRSTATUS

			''' <summary> The status code</summary>
			Public nStatus As UInt32

			''' <summary> The sequence number of the restore point</summary>
			Public llSequenceNumber As Int64
		End Structure

		''' <summary> Type of restorations</summary>
		Friend Enum RESTORE_TYPE As UInt32
			''' <summary> Installing a new application</summary>
			APPLICATION_INSTALL = 0UI

			''' <summary> An application has been uninstalled</summary>
			APPLICATION_UNINSTALL = 1UI

			''' <summary> System Restore</summary>
			RESTORE = 6UI

			''' <summary> Checkpoint</summary>
			CHECKPOINT = 7UI

			''' <summary> Device driver has been installed</summary>
			DEVICE_DRIVER_INSTALL = 10UI

			''' <summary> Program used for 1st time </summary>
			FIRSTRUN = 11UI

			''' <summary> An application has had features added or removed</summary>
			MODIFY_SETTINGS = 12UI

			''' <summary> An application needs to delete the restore point it created</summary>
			CANCELLED_OPERATION = 13UI

			''' <summary> Restoring a backup</summary>
			BACKUP_RECOVERY = 14UI
		End Enum

		Friend Enum EVENT_TYPE As UInt32
			BEGINSYSTEMCHANGE = 100UI
			ENDSYSTEMCHANGE = 101UI
			BEGINNESTEDSYSTEMCHANGE = 102UI
			ENDNESTEDSYSTEMCHANGE = 103UI
			DESKTOPSETTING = 2UI
			ACCESSIBILITYSETTING = 3UI
			OESETTING = 4UI
			APPLICATIONRUN = 5UI
			WINDOWSSHUTDOWN = 8UI
			WINDOWSBOOT = 9UI
		End Enum

		''' <summary>
		''' Verifies that the OS can do system restores
		''' </summary>
		''' <returns>True if OS is either ME,XP,Vista,7</returns>
		Private Shared Function SysRestoreAvailable() As Boolean
			Dim majorVersion As Integer = Environment.OSVersion.Version.Major
			Dim minorVersion As Integer = Environment.OSVersion.Version.Minor

			Dim sbPath As New StringBuilder(260)

			' See if DLL exists
			If SearchPath(Nothing, "srclient.dll", Nothing, 260, sbPath, Nothing) <> 0 Then
				Return True
			End If

			' Windows ME
			If majorVersion = 4 AndAlso minorVersion = 90 Then
				Return True
			End If

			' Windows XP
			If majorVersion = 5 AndAlso minorVersion = 1 Then
				Return True
			End If

			' Windows Vista
			If majorVersion = 6 AndAlso minorVersion = 0 Then
				Return True
			End If

			' Windows Se7en
			If majorVersion = 6 AndAlso minorVersion = 1 Then
				Return True
			End If

			' All others : Win 95, 98, 2000, Server
			Return False
		End Function

		''' <summary>
		''' Starts system restore
		''' </summary>
		''' <param name="strDescription">The description of the restore</param>
		''' <returns>The status of call</returns>
		Friend Shared Function StartRestore(ByVal strDescription As String, ByVal restoreType As RESTORE_TYPE, ByRef result As Int64) As Boolean
			If Not SysRestoreAvailable() Then
				Return False
			End If

			Dim rpInfo As New RESTOREPOINTINFO()
			Dim rpStatus As New STATEMGRSTATUS()

			' Prepare Restore Point
			rpInfo.dwEventType = EVENT_TYPE.BeginSystemChange

			' By default we create a verification system
			rpInfo.dwRestorePtType = restoreType
			rpInfo.llSequenceNumber = 0L
			rpInfo.szDescription = strDescription

			Using ptrInfo As New StructPtr(rpInfo)
				Using ptrStatus As New StructPtr(rpStatus)
					If Not SRSetRestorePoint(ptrInfo.Ptr, ptrStatus.Ptr) Then
						Throw New Win32Exception()
					Else
						result = rpStatus.llSequenceNumber
						Return True
					End If
				End Using
			End Using
		End Function

		''' <summary>
		''' Ends system restore call
		''' </summary>
		''' <param name="lSeqNum">The restore sequence number</param>
		''' <returns>The status of call</returns>
		Friend Shared Function EndRestore(ByVal lSeqNum As Int64) As Boolean
			Dim rpInfo As New RESTOREPOINTINFO()
			Dim rpStatus As New STATEMGRSTATUS()

			rpInfo.dwEventType = EVENT_TYPE.EndSystemChange
			rpInfo.llSequenceNumber = lSeqNum

			Using ptrInfo As New StructPtr(rpInfo)
				Using ptrStatus As New StructPtr(rpStatus)
					If Not SRSetRestorePoint(ptrInfo.Ptr, ptrStatus.Ptr) Then
						Throw New Win32Exception()
					End If
				End Using
			End Using
			Return True
		End Function

		''' <summary>
		''' Cancels restore call
		''' </summary>
		''' <param name="lSeqNum">The restore sequence number</param>
		''' <returns>The status of call</returns>
		Friend Shared Function CancelRestore(ByVal lSeqNum As Int64) As Boolean
			Dim rpInfo As New RESTOREPOINTINFO()
			Dim rpStatus As New STATEMGRSTATUS()

			rpInfo.dwEventType = EVENT_TYPE.EndSystemChange
			rpInfo.dwRestorePtType = RESTORE_TYPE.CANCELLED_OPERATION
			rpInfo.llSequenceNumber = lSeqNum

			Using ptrInfo As New StructPtr(rpInfo)
				Using ptrStatus As New StructPtr(rpStatus)
					If Not SRSetRestorePoint(ptrInfo.Ptr, ptrStatus.Ptr) Then
						Throw New Win32Exception()
					End If
				End Using
			End Using

			Return True
		End Function
	End Class
End Namespace

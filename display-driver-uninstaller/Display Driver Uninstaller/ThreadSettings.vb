Imports System.Reflection

Public Class ThreadSettings

	Public Property Paths As New AppPaths(False)
    Public Property SelectedGPU As GPUVendor
    Public Property AppName As String

	Public Property DoShutdown As Boolean
	Public Property DoReboot As Boolean

	Public Property RemoveMonitors As Boolean
	Public Property RemoveCrimsonCache As Boolean
	Public Property RemoveAMDDirs As Boolean
	Public Property RemoveAMDAudioBus As Boolean
	Public Property RemoveAMDKMPFD As Boolean

	Public Property RemoveNvidiaDirs As Boolean
	Public Property RemovePhysX As Boolean
	Public Property Remove3DTVPlay As Boolean
	Public Property RemoveGFE As Boolean

	Public Property ShowSafeModeMsg As Boolean
	Public Property UseRoamingConfig As Boolean
	Public Property DontCheckUpdates As Boolean
	Public Property CreateRestorePoint As Boolean
	Public Property SaveLogs As Boolean

	Public Sub New()
		PropertyCopy(Application.Paths, Me.Paths)
		PropertyCopy(Application.Settings, Me)
	End Sub

	Private Sub PropertyCopy(ByVal fromObj As Object, toObj As Object)
		Dim type As Type = fromObj.GetType()
		Dim typeNew As Type = toObj.GetType()

		Dim properties As PropertyInfo() = type.GetProperties(BindingFlags.Public Or BindingFlags.Instance)
		Dim propertiesNew As PropertyInfo() = typeNew.GetProperties(BindingFlags.Public Or BindingFlags.Instance)

		For Each pNew As PropertyInfo In propertiesNew
			For Each p As PropertyInfo In properties
				If p.Name = pNew.Name Then
					pNew.SetValue(toObj, p.GetValue(fromObj, Nothing), Nothing)
				End If
			Next
		Next
	End Sub

End Class
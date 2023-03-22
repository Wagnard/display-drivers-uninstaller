Imports System.Reflection

Namespace Display_Driver_Uninstaller
	Public Class ThreadSettings
		Inherits AppLaunchOptions

		Public Property Paths As New AppPaths(False)
		Public Property Success As Boolean = False
		Public Property GPURemovedSuccess As Boolean = True
		Public Property PreventClose As Boolean = False
		Public Property NotPresentAMDKMPFD As Boolean = False
		Public Property SelectedGPU As GPUVendor
		Public Property SelectedAUDIO As AudioVendor
		Public Property SelectedType As CleanType
		Public Property AppName As String

		Public Property WinVersion As OSVersion
		Public Property WinIs64 As Boolean

		Public ReadOnly Property IsWinXp As Boolean
			Get
				Return (WinVersion = OSVersion.WinXP Or WinVersion = OSVersion.WinXPPro_Server2003)
			End Get
		End Property
		Public ReadOnly Property IsWin8Higher As Boolean
			Get
				Return WinVersion >= OSVersion.Win8
			End Get
		End Property
		Public ReadOnly Property IsWin10 As Boolean
			Get
				Return WinVersion = OSVersion.Win10
			End Get
		End Property

		Public Sub New(Optional ByVal fromCmdLineArgs As Boolean = False)
			PropertyCopy(Application.Paths, Me.Paths)
			PropertyCopy(Application.Settings, Me)

			If fromCmdLineArgs Then
				PropertyCopy(Application.LaunchOptions, Me)     ' Use cmdline args
			End If
		End Sub

		Private Sub PropertyCopy(ByVal fromObj As Object, toObj As Object)
			Dim type As Type = fromObj.GetType()
			Dim typeNew As Type = toObj.GetType()

			Dim properties As PropertyInfo() = type.GetProperties(BindingFlags.Public Or BindingFlags.Instance)
			Dim propertiesNew As PropertyInfo() = typeNew.GetProperties(BindingFlags.Public Or BindingFlags.Instance)

			For Each pNew As PropertyInfo In propertiesNew
				If Not pNew.CanWrite Then Continue For

				For Each p As PropertyInfo In properties
					If p.Name = pNew.Name Then
						pNew.SetValue(toObj, p.GetValue(fromObj, Nothing), Nothing)
						Exit For
					End If
				Next
			Next
		End Sub

	End Class
End Namespace
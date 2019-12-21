Imports System.ComponentModel
Imports System.IO
Imports System.Text
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography

Imports System.Windows.Forms

Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32

Public Module Tools
	Dim FileIO As New FileIO
	' 9 = vbTAB --- 10 = vbLF --- 11 = vbVerticalTab --- 12 = vbFormFeed --- 13 = vbCR --- 32 = SPACE
	Private ReadOnly whiteSpaceChars As Char() = New Char() {ChrW(9), ChrW(10), ChrW(11), ChrW(12), ChrW(13), ChrW(32)}

	''' <summary>Compares two streams equality by using MD5 checksums</summary>
	Public Function CompareStreams(ByVal stream1 As Stream, ByVal stream2 As Stream) As Boolean
		If stream1 Is Nothing Or stream2 Is Nothing Then
			Return False
		End If

		stream1.Position = 0L
		stream2.Position = 0L

		Using md5 As New MD5CryptoServiceProvider
			Dim bytes1 As Byte() = md5.ComputeHash(stream1)
			Dim bytes2 As Byte() = md5.ComputeHash(stream2)

			For i As Int32 = 0 To bytes1.Length - 1
				If bytes1(i) <> bytes2(i) Then
					Return False
				End If
			Next

			Return True
		End Using
	End Function

	Public Function IsNet45OrNewer() As Boolean
		Return Type.[GetType]("System.Reflection.ReflectionContext", False) IsNot Nothing
	End Function

	Public Function PreferredUILanguages() As String
		Try
			Using regkey As RegistryKey = Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", False)
				If regkey IsNot Nothing Then
					Dim wantedValue As String() = CType(regkey.GetValue("PreferredUILanguages"), String())

					If wantedValue IsNot Nothing AndAlso wantedValue.Length > 0 AndAlso Not IsNullOrWhitespace(wantedValue(0)) Then
						Return wantedValue(0)
					Else
						Return Globalization.CultureInfo.InstalledUICulture.Name    'Return en-US, en-GB, fr-FR etc.
					End If
				Else
					' DevMltk: Don't have PreferredUILanguages.. but have:
					' HKEY_CURRENT_USER\Control Panel\Desktop\MuiCached => MachinePreferredUILanguages (REG_MULTI_SZ)

					Using regkey2 As RegistryKey = Registry.CurrentUser.OpenSubKey("Control Panel\Desktop\MuiCached", False)
						If regkey2 IsNot Nothing Then
							Dim wantedValue As String() = CType(regkey.GetValue("MachinePreferredUILanguages"), String())

							If wantedValue IsNot Nothing AndAlso wantedValue.Length > 0 AndAlso Not IsNullOrWhitespace(wantedValue(0)) Then
								Return wantedValue(0)
							Else
								Return Globalization.CultureInfo.InstalledUICulture.Name    'Return en-US, en-GB, fr-FR etc.
							End If
						End If
					End Using
				End If
			End Using

			Return "en-US"    'Return en-US (English) by default if nothing found.
		Catch ex As Exception
			Return "en-US"    'Return en-US (English) by default if error
		End Try
	End Function

	Public Function IsNullOrWhitespace(ByVal str As String) As Boolean
		Return If(str IsNot Nothing, String.IsNullOrEmpty(str.Trim(whiteSpaceChars)), True)
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ByVal sb As StringBuilder, ParamArray str As String()) As StringBuilder
		If str IsNot Nothing Then
			For Each s As String In str
				sb.Append(s)
			Next
		End If

		Return sb
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ByVal ParamArray str As String()) As StringBuilder
		Return StrAppend(New StringBuilder(), str)
	End Function

	''' <summary>Replaces all given parameters from text (Case Sensitive!)</summary>
	Public Function StrReplace(ByVal sb As StringBuilder, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		If IsNullOrWhitespace(oldStr) Then
			Return sb
		End If

		Return sb.Replace(oldStr, newStr)
	End Function

	''' <summary>Replaces all given parameters from text (Case Sensitive!)</summary>
	Public Function StrReplace(ByVal text As String, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		Return StrReplace(New StringBuilder(text), oldStr, newStr)
	End Function

	''' <summary>Removes all given parameters from text (Case InSensitive!)</summary>
	Public Function StrRemoveAny(ByVal text As String, ByVal ignoreCase As Boolean, ParamArray Str As String()) As String
		If Str IsNot Nothing And Str.Length > 0 Then
			If ignoreCase Then
				For Each s As String In Str
					If Not IsNullOrWhitespace(s) Then
						text = Strings.Replace(text, s, String.Empty, 1, -1, CompareMethod.Text)
					End If
				Next
			Else
				If Str.Length = 1 Then
					Return text.Replace(Str(0), String.Empty)
				End If

				Dim sb As New StringBuilder(text)

				For Each s As String In Str
					If Not IsNullOrWhitespace(s) Then
						sb.Replace(s, String.Empty)
					End If
				Next

				Return sb.ToString()
			End If
		End If

		Return text
	End Function

	''' <summary>Check if text contains any of the given parameters</summary>
	Public Function StrContainsAny(ByVal text As String, ByVal ignoreCase As Boolean, ParamArray Str As String()) As Boolean
		If IsNullOrWhitespace(text) Then Return False

		If Str IsNot Nothing And Str.Length > 0 Then
			Dim comparison As StringComparison = If(ignoreCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)

			For Each s As String In Str
				If Not IsNullOrWhitespace(s) Then
					If text.IndexOf(s, comparison) <> -1 Then   ' -1 = NOT FOUND
						Return True
					End If
				End If
			Next
		End If

		Return False
	End Function

	''' <summary>Check if text contains Equal of the given parameters</summary>
	Public Function StrEqual(ByVal text As String, ByVal ignoreCase As Boolean, ParamArray Str As String()) As Boolean
		If IsNullOrWhitespace(text) Then Return False

		If Str IsNot Nothing And Str.Length > 0 Then

			For Each s As String In Str
				If Not IsNullOrWhitespace(s) Then
					If String.Equals(text, s, If(ignoreCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)) Then
						Return True
					End If
				End If
            Next
		End If

		Return False
	End Function


	' <Extension()>
	Public Function GetDescription(ByVal EnumConstant As [Enum]) As String
		Dim fi As FieldInfo = EnumConstant.GetType().GetField(EnumConstant.ToString())
		Dim attr() As DescriptionAttribute = DirectCast(fi.GetCustomAttributes(GetType(DescriptionAttribute), False), DescriptionAttribute())

		If attr.Length > 0 Then
			Return attr(0).Description
		Else
			Return EnumConstant.ToString()
		End If
	End Function

	Public Function GetOemInfList(ByVal directory As String) As List(Of Inf)
		Dim oemInfList As New List(Of Inf)

		For Each inf As String In FileIO.GetFiles(directory, "oem*.inf", False)
			oemInfList.Add(New Inf(inf))
		Next

		Return oemInfList
	End Function

	Public ReadOnly Property ProcessIs64 As Boolean
		Get
			Return WinAPI.Is64
		End Get
	End Property

	Public ReadOnly Property UserHasAdmin As Boolean
		Get
			Return WinAPI.IsAdmin
		End Get
	End Property




	Public Sub AdjustWindow(ByRef window As Window)
		Dim parent As Window = window.Owner

		If parent Is Nothing Then
			Return
		End If

		With window
			.SizeToContent = SizeToContent.Manual
			.WindowState = parent.WindowState

			.Width = parent.ActualWidth
			.Height = parent.ActualHeight

			.Top = parent.Top
			.Left = parent.Left

			AddHandler window.StateChanged, AddressOf StateChanged
		End With

	End Sub

	Private Sub StateChanged(ByVal sender As Object, ByVal e As System.EventArgs)
		Dim window As Window = TryCast(sender, Window)

		If window IsNot Nothing Then
			If window.WindowState <> WindowState.Maximized Then
				If window.Top < 0D Then window.Top = 0D
				If window.Left < 0D Then window.Left = 0D

				' Screen.FromHandle for Multimonitor setups. Otherwise it gets Primary screens size and not where application is (eg. second monitor)
				' Not sure what happens if maximized to over more than one screen thought..
				' WorkingArea Excludes Taskbars height if visible. Covers taskbar if set auto hide.
				Dim workArea As System.Drawing.Rectangle = System.Windows.Forms.Screen.FromHandle(New Interop.WindowInteropHelper(window).Handle).WorkingArea
				Dim newW As Double = CDbl(workArea.Width)
				Dim newH As Double = CDbl(workArea.Height)

				' ActualWidth goes over screen Width if maximized (Win7, ~16px)
				If window.ActualWidth > newW AndAlso window.Width > newW Then
					window.Width = newW
				End If

				' ActualHeight goes over screen Height if maximized (Win7, ~16px)
				If window.ActualHeight > newH AndAlso window.Height > newH Then
					window.Height = newH
				End If
			End If
		End If
	End Sub

	Friend Function RegDP(ByVal s As String, ByVal t As Type, ByVal c As Type, ByVal m As Object) As DependencyProperty
		If TypeOf (m) Is FrameworkPropertyMetadata Then
			Return DependencyProperty.Register(s, t, c, DirectCast(m, FrameworkPropertyMetadata))
		Else
			Return DependencyProperty.Register(s, t, c, New PropertyMetadata(m))
		End If
	End Function

	''' <summary>Alias for MessageBox.Show(message) as defaults settings: only 'OK' button + 'Information' image</summary>
	Public Function MsgBox(ByVal message As String, Optional ByVal buttons As MessageBoxButton = MessageBoxButton.OK, Optional ByVal image As MessageBoxImage = MessageBoxImage.Information) As MessageBoxResult
		Return System.Windows.MessageBox.Show(message, Application.Settings.AppName, MessageBoxButton.OK, MessageBoxImage.Information)
	End Function

	''' <summary>Alias for MessageBox.Show(message, title) as defaults settings: only 'OK' button + 'Information' image</summary>
	Public Function MsgBox(ByVal message As String, ByVal title As String, Optional ByVal buttons As MessageBoxButton = MessageBoxButton.OK, Optional ByVal image As MessageBoxImage = MessageBoxImage.Information) As MessageBoxResult
		Return System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information)
	End Function

End Module
Imports System.ComponentModel
Imports System.IO
Imports System.Text
Imports System.Reflection
Imports System.Security.Cryptography
Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32
Imports System.Linq
Imports System.Text.RegularExpressions

Namespace Display_Driver_Uninstaller

	Public Module Tools
		ReadOnly FileIO As New FileIO
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

		Public Function ReplaceIgnoreCase(input As String, oldValue As String, newValue As String) As String
			Return Regex.Replace(input, Regex.Escape(oldValue), newValue, RegexOptions.IgnoreCase)
		End Function

		Public Function IsNet45OrNewer() As Boolean
			Return Type.[GetType]("System.Reflection.ReflectionContext", False) IsNot Nothing
		End Function

		Public Function IsNet48OrNewer() As Boolean
			Using ndpKey As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\")
				Dim releaseKey As Integer = Convert.ToInt32(ndpKey.GetValue("Release"))


				If CheckFor45DotVersion(releaseKey) = "4.8 or later" Then
					Return True
				End If

				Return False
			End Using
		End Function

		Private Function CheckFor45DotVersion(ByVal releaseKey As Integer) As String
			If releaseKey >= 528040 Then
				Return "4.8 or later"
			End If

			If releaseKey >= 461808 Then
				Return "4.7.2 or later"
			End If

			If releaseKey >= 461308 Then
				Return "4.7.1 or later"
			End If

			If releaseKey >= 460798 Then
				Return "4.7 or later"
			End If

			If releaseKey >= 394802 Then
				Return "4.6.2 or later"
			End If

			If releaseKey >= 394254 Then
				Return "4.6.1 or later"
			End If

			If releaseKey >= 393295 Then
				Return "4.6 or later"
			End If

			If releaseKey >= 393273 Then
				Return "4.6 RC or later"
			End If

			If (releaseKey >= 379893) Then
				Return "4.5.2 or later"
			End If

			If (releaseKey >= 378675) Then
				Return "4.5.1 or later"
			End If

			If (releaseKey >= 378389) Then
				Return "4.5 or later"
			End If

			Return "No 4.5 or later version detected"
		End Function

		Public Function CanDeprovisionPackageForAllUsersAsync() As Boolean
			Dim packageManager As Windows.Management.Deployment.PackageManager = New Windows.Management.Deployment.PackageManager
			Dim type As Type = packageManager.GetType
			Return type.GetMethod("DeprovisionPackageForAllUsersAsync") IsNot Nothing
		End Function

		Public Function PreferredUILanguages() As String
			Try
				Using regkey As RegistryKey = Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", False)
					If regkey IsNot Nothing Then
						Dim wantedValue As String() = CType(regkey.GetValue("PreferredUILanguages"), String())

						If wantedValue IsNot Nothing AndAlso wantedValue.Length > 0 AndAlso Not String.IsNullOrWhiteSpace(wantedValue(0)) Then
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

								If wantedValue IsNot Nothing AndAlso wantedValue.Length > 0 AndAlso Not String.IsNullOrWhiteSpace(wantedValue(0)) Then
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

		'Public Function String.IsNullOrWhiteSpace(ByVal str As String) As Boolean
		'	Return If(str IsNot Nothing, String.IsNullOrEmpty(str.Trim(whiteSpaceChars)), True)
		'End Function

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
			If String.IsNullOrWhiteSpace(oldStr) Then
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
						If Not String.IsNullOrWhiteSpace(s) Then
							text = Strings.Replace(text, s, String.Empty, 1, -1, CompareMethod.Text)
						End If
					Next
				Else
					If Str.Length = 1 Then
						Return text.Replace(Str(0), String.Empty)
					End If

					Dim sb As New StringBuilder(text)

					For Each s As String In Str
						If Not String.IsNullOrWhiteSpace(s) Then
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
			If String.IsNullOrWhiteSpace(text) Then Return False

			If Str IsNot Nothing And Str.Length > 0 Then
				Dim comparison As StringComparison = If(ignoreCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)

				For Each s As String In Str
					If Not String.IsNullOrWhiteSpace(s) Then
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
			If String.IsNullOrWhiteSpace(text) Then Return False

			If Str IsNot Nothing And Str.Length > 0 Then

				For Each s As String In Str
					If Not String.IsNullOrWhiteSpace(s) Then
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

		Public Function GetOemInf(ByVal directory As String, oem As String) As Inf

			' Regular expression to match a string ending with .inf (with or without the colon)
			Dim pattern As String = "^[\w\d_-]+\.inf"
			Dim match As Match = Regex.Match(oem, pattern)

			If match.Success Then
				Dim infFile = directory + match.Value
				If FileIO.ExistsFile(infFile) Then
					Return New Inf(infFile)
				End If
			End If
			Return Nothing
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

		Public Sub FixBrokenPathIfNeeded()
			Const VALUE_NAME As String = "Path"

			' 1) Read actual system root path from registry
			Dim systemRootFromRegistry As String = String.Empty
			Using key As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion", False)
				If key IsNot Nothing Then
					systemRootFromRegistry = If(TryCast(key.GetValue("SystemRoot"), String), String.Empty)
				End If
			End Using

			If String.IsNullOrWhiteSpace(systemRootFromRegistry) Then
				' Could log warning or abort because we don't know system root
				Application.Log.AddMessage("SystemRoot not found in registry; skipping Path fix.")
				Return
			End If

			Using topLevelKey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM", Writable:=False)
				If topLevelKey Is Nothing Then Return

				For Each childName As String In topLevelKey.GetSubKeyNames()
					' ... your existing control set checks ...
					If String.IsNullOrWhiteSpace(childName) Then Continue For

					Using envKey As RegistryKey = MyRegistry.OpenSubKey(topLevelKey, $"{childName}\Control\Session Manager\Environment", Writable:=True)
						If envKey Is Nothing Then Continue For

						Dim allNames = envKey.GetValueNames()
						Dim hasPath = allNames.Any(Function(n) String.Equals(n, VALUE_NAME, StringComparison.OrdinalIgnoreCase))
						If Not hasPath Then Continue For

						Dim kind As RegistryValueKind = envKey.GetValueKind(VALUE_NAME)
						If kind = RegistryValueKind.ExpandString Then Continue For

						Dim rawObj = envKey.GetValue(VALUE_NAME, String.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames)
						Dim rawPath As String = If(rawObj?.ToString(), String.Empty)
						If String.IsNullOrWhiteSpace(rawPath) Then Continue For

						' 2) Only fix if rawPath contains the literal system root path (case-insensitive)
						If rawPath.IndexOf(systemRootFromRegistry, StringComparison.OrdinalIgnoreCase) < 0 Then
							Continue For ' Nothing to fix here
						End If

						' 3) Perform replacements with %SystemRoot%
						Dim fixedPath As String = rawPath

						' Replace "c:\windows\system32\wbem" → "%SystemRoot%\System32\Wbem"
						fixedPath = Regex.Replace(
					fixedPath,
					"(?i)\b" & Regex.Escape(systemRootFromRegistry) & "\\system32\\wbem",
					"%SystemRoot%\System32\Wbem",
					RegexOptions.IgnoreCase)

						' Replace "c:\windows\" everywhere with "%SystemRoot%\"
						fixedPath = Regex.Replace(
					fixedPath,
					"(?i)\b" & Regex.Escape(systemRootFromRegistry) & "\\",
					"%SystemRoot%\",
					RegexOptions.IgnoreCase)

						' Replace standalone "c:\windows" at segment boundary
						fixedPath = Regex.Replace(
					fixedPath,
					"(?i)\b" & Regex.Escape(systemRootFromRegistry) & "(?=(;|$))",
					"%SystemRoot%",
					RegexOptions.IgnoreCase)

						If String.Equals(rawPath, fixedPath, StringComparison.Ordinal) Then Continue For

						Try
							envKey.SetValue(VALUE_NAME, fixedPath, RegistryValueKind.ExpandString)
							Application.Log.AddMessage(
						$"Fixed broken Path in registry. Old value: {rawPath} New value: {fixedPath}")
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					End Using
				Next
			End Using
		End Sub

		Public Function IsIntelNpuPresent() As Boolean
			Try
				Dim found As List(Of SetupAPI.Device) = SetupAPI.GetDevices("ComputeAccelerator", "VEN_8086", False, driverDetails:=False, logging:=False)
				If found IsNot Nothing AndAlso found.Count > 0 Then
					Return True
				End If
				Return False
			Catch ex As Exception
				Application.Log.AddException(ex)
				Return False
				'MessageBox.Show(Languages.GetTranslation("frmMain", "Messages", "Text6"), config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
			End Try
		End Function

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
End Namespace
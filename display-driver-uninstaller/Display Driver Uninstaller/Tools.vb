Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography
Imports System.Runtime.InteropServices
Imports System.Collections.ObjectModel

Module Tools
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

	Public Function IsNullOrWhitespace(ByRef str As String) As Boolean
		Return If(str IsNot Nothing, String.IsNullOrEmpty(str.Trim(whiteSpaceChars)), True)
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ByRef sb As StringBuilder, ParamArray str As String()) As StringBuilder
		If str IsNot Nothing Then
			For Each s As String In str
				sb.Append(s)
			Next
		End If

		Return sb
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ParamArray str As String()) As StringBuilder
		Return StrAppend(New StringBuilder(), str)
	End Function

	''' <summary>Replaces all given parameters from text (Case Sensitive!)</summary>
	Public Function StrReplace(ByRef sb As StringBuilder, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		If IsNullOrWhitespace(oldStr) Then
			Return sb
		End If

		Return sb.Replace(oldStr, newStr)
	End Function

	''' <summary>Replaces all given parameters from text (Case Sensitive!)</summary>
	Public Function StrReplace(ByRef text As String, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		Return StrReplace(New StringBuilder(text), oldStr, newStr)
	End Function

	''' <summary>Removes all given parameters from text (Case InSensitive!)</summary>
	Public Function StrRemoveAny(ByRef text As String, ByVal ignoreCase As Boolean, ParamArray Str As String()) As String
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
	Public Function StrContainsAny(ByRef text As String, ByVal ignoreCase As Boolean, ParamArray Str As String()) As Boolean
		If Str IsNot Nothing And Str.Length > 0 Then
			Dim comparison As StringComparison = If(ignoreCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)

			For Each s As String In Str
				If Not IsNullOrWhitespace(s) Then
					If text.IndexOf(s, comparison) <> -1 Then	' -1 = NOT FOUND
						Return True
					End If
				End If
			Next
		End If

		Return False
	End Function

	''' <summary>Get files from directory using Windows API (FAST!)</summary>
	''' <param name="directory">Directory where to look for files</param>
	''' <param name="wildCard">Wilcard for files (default = *)</param>
	''' <param name="searchSubDirs">Search subdirectories of directory (recursive)</param>
	''' <returns>List(Of String) files</returns>
	Public Function GetFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		Dim fileNames As New List(Of String)(100)

		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		WINDOWS_API_FIND.GetFilenames(directory, fileNames, wildCard, searchSubDirs)

		Return fileNames
	End Function

	Public Function GetDirectories(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		Dim dirNames As New List(Of String)(100)

		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		WINDOWS_API_FIND.GetDirnames(directory, dirNames, wildCard, searchSubDirs)

		Return dirNames
	End Function

	Public Function GetOemInfList(ByVal directory As String, Optional ByVal wildCard As String = "oem*.inf", Optional ByVal searchSubDirs As Boolean = False) As List(Of OemINF)
		Dim oemInfList As New List(Of OemINF)
		Dim oemInf As OemINF

		For Each inf As String In GetFiles(directory, wildCard, searchSubDirs)
			oemInf = New OemINF(inf)

			Try
				oemInf.Provider = WINDOWS_API_INI.GetINIValue(inf, "Version", "Provider")
				oemInf.Class = WINDOWS_API_INI.GetINIValue(inf, "Version", "Class")

				If Not IsNullOrWhitespace(oemInf.Provider) Or Not IsNullOrWhitespace(oemInf.Class) Then
					oemInf.IsValid = True
				End If

			Catch ex As Exception
				oemInf.IsValid = False
				Application.Log.AddException(ex)
			End Try

			oemInfList.Add(oemInf)
		Next

		Return oemInfList
	End Function

End Module

Public Class OemINF
	Public Property FileName As String
	Public Property Provider As String
	Public Property [Class] As String
	Public Property IsValid As Boolean

	Public Sub New(ByVal fileName As String)
		Me.FileName = fileName
	End Sub

End Class

Public Class WINDOWS_API_INI
	Private Declare Auto Function GetPrivateProfileString Lib "kernel32" (ByVal lpAppName As String, ByVal lpKeyName As String, ByVal lpDefault As String, ByVal lpReturnedString As StringBuilder, ByVal nSize As Integer, ByVal lpFileName As String) As Integer

	''' <summary>Read Section->Key->Value from INI using Win API</summary>
	''' <param name="infFile">Fullpath to file</param>
	''' <param name="section">[Version]</param>
	''' <param name="key">Key under section. eg 'Provider'</param>
	''' <returns>Found value or Nothing</returns>
	Public Shared Function GetINIValue(ByRef infFile As String, ByRef section As String, ByRef key As String) As String
		Dim searchStrings As String = "Strings"
		Dim sb As New StringBuilder(256)
		Dim value As String

		GetPrivateProfileString(section, key, Nothing, sb, sb.Capacity, infFile)
		value = sb.ToString()

		If value.Contains("%") Then
			sb.Remove(0, sb.Length)

			If GetPrivateProfileString(searchStrings, value.Replace("%", String.Empty), Nothing, sb, sb.Capacity, infFile) = 0 Then
				GetPrivateProfileString(searchStrings, value, Nothing, sb, sb.Capacity, infFile)
			End If

			value = sb.ToString()
		End If

		Return value
	End Function
End Class

Public Class WINDOWS_API_FIND
	<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
	Structure WIN32_FIND_DATA
		Public dwFileAttributes As UInteger
		Public ftCreationTime As System.Runtime.InteropServices.ComTypes.FILETIME
		Public ftLastAccessTime As System.Runtime.InteropServices.ComTypes.FILETIME
		Public ftLastWriteTime As System.Runtime.InteropServices.ComTypes.FILETIME
		Public nFileSizeHigh As UInteger
		Public nFileSizeLow As UInteger
		Public dwReserved0 As UInteger
		Public dwReserved1 As UInteger
		<MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)> Public cFileName As String
		<MarshalAs(UnmanagedType.ByValTStr, SizeConst:=14)> Public cAlternateFileName As String
	End Structure

	<DllImport("kernel32", CharSet:=CharSet.Unicode)>
	Public Shared Function FindFirstFile(lpFileName As String, ByRef lpFindFileData As WIN32_FIND_DATA) As IntPtr
	End Function

	<DllImport("kernel32", CharSet:=CharSet.Unicode)>
	Public Shared Function FindNextFile(hFindFile As IntPtr, ByRef lpFindFileData As WIN32_FIND_DATA) As Boolean
	End Function

	<DllImport("kernel32.dll")>
	Public Shared Function FindClose(ByVal hFindFile As IntPtr) As Boolean
	End Function

	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, ExactSpelling:=False, SetLastError:=True)>
	Public Shared Function DeleteFile(ByVal path As String) As Boolean
	End Function

	Public Shared Sub GetFilenames(ByVal directory As String, ByRef fileNames As List(Of String), Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As New IntPtr
		Dim INVALID_HANDLE_VALUE As New IntPtr(-1)

		If Not directory.EndsWith(Path.DirectorySeparatorChar) Then directory &= Path.DirectorySeparatorChar

		Dim findDir As String = directory & wildCard

		Try
			findHandle = FindFirstFile(findDir, findData)

			If findHandle <> INVALID_HANDLE_VALUE Then
				Do
					If findData.cFileName <> "." AndAlso findData.cFileName <> ".." Then
						If (findData.dwFileAttributes And FileAttributes.Directory) <> FileAttributes.Directory Then
							fileNames.Add(directory & findData.cFileName)
						Else
							If searchSubDirs Then
								GetFilenames(directory & findData.cFileName, fileNames, wildCard, searchSubDirs)
							End If
						End If

					End If
				Loop While (FindNextFile(findHandle, findData))

				FindClose(findHandle)
			End If
		Catch ex As Exception
			If findHandle <> INVALID_HANDLE_VALUE Then
				FindClose(findHandle)
			End If

			Application.Log.AddException(ex)
		Finally

		End Try
	End Sub

	Public Shared Sub GetDirnames(ByVal directory As String, ByRef dirNames As List(Of String), Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As New IntPtr
		Dim INVALID_HANDLE_VALUE As New IntPtr(-1)

		If Not directory.EndsWith(Path.DirectorySeparatorChar) Then directory &= Path.DirectorySeparatorChar

		Dim findDir As String = directory & wildCard

		Try
			findHandle = FindFirstFile(findDir, findData)

			If findHandle <> INVALID_HANDLE_VALUE Then
				Do
					If findData.cFileName <> "." AndAlso findData.cFileName <> ".." Then
						If (findData.dwFileAttributes And FileAttributes.Directory) = FileAttributes.Directory Then
							dirNames.Add(directory & findData.cFileName)

							If searchSubDirs Then
								GetDirnames(directory & findData.cFileName, dirNames, wildCard, searchSubDirs)
							End If
						End If
					End If
				Loop While (FindNextFile(findHandle, findData))

				FindClose(findHandle)
			End If
		Catch ex As Exception
			If findHandle <> INVALID_HANDLE_VALUE Then
				FindClose(findHandle)
			End If

			Application.Log.AddException(ex)
		Finally

		End Try
	End Sub
End Class

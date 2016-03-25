Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography
Imports System.Runtime.InteropServices
Imports System.Collections.ObjectModel

Module Tools

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
		Return If(str IsNot Nothing, String.IsNullOrEmpty(str.Trim), True)
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ByRef sb As StringBuilder, ParamArray str As String()) As StringBuilder
		If str IsNot Nothing Then
			For i As Int32 = 0 To str.Length - 1
				sb.Append(str(i))
			Next
		End If

		Return sb
	End Function

	''' <summary>Concats all given parameters to single text</summary>
	Public Function StrAppend(ParamArray str As String()) As StringBuilder
		Return StrAppend(New StringBuilder(), str)
	End Function

	''' <summary>Replaces all given parameters from text</summary>
	Public Function StrReplace(ByRef sb As StringBuilder, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		If String.IsNullOrEmpty(oldStr) Then
			Return sb
		End If

		Return sb.Replace(oldStr, newStr)
	End Function

	''' <summary>Replaces all given parameters from text</summary>
	Public Function StrReplace(ByRef text As String, ByRef oldStr As String, ByRef newStr As String) As StringBuilder
		Return StrReplace(New StringBuilder(text), oldStr, newStr)
	End Function

	''' <summary>Removes all given parameters from text</summary>
	Public Function StrRemove(ByRef sb As StringBuilder, ParamArray Str As String()) As StringBuilder
		If Str IsNot Nothing And Str.Length > 0 Then
			For i As Int32 = 0 To Str.Length - 1
				If Not String.IsNullOrEmpty(Str(i)) Then
					sb.Replace(Str(i), String.Empty)
				End If
			Next
		End If

		Return sb
	End Function

	''' <summary>Removes all given parameters from text</summary>
	Public Function StrRemove(ByRef text As String, ParamArray Str As String()) As StringBuilder
		Return StrRemove(New StringBuilder(Str.Length), Str)
	End Function

	''' <summary>Check if text contains any of the give parameters</summary>
	Public Function StrContains(ByRef text As String, ParamArray Str As String()) As Boolean
		If Str IsNot Nothing And Str.Length > 0 Then
			For Each s As String In Str
				If Not String.IsNullOrEmpty(s) Then
					Return text.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1
				End If
			Next
		End If

		Return False
	End Function

	''' <summary>Get files from directory using Windows API (fast)</summary>
	''' <param name="directory">Directory where to look for files</param>
	''' <param name="wildCard">Wilcard for files (default = *)</param>
	''' <param name="searchSubDirs">Search subdirectories of directory (recursive)</param>
	''' <returns>List(Of String) files</returns>
	Public Function GetFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		Dim fileNames As New List(Of String)(100)

		If String.IsNullOrEmpty(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		WINDOWS_API.GetFilenames(directory, fileNames, wildCard, searchSubDirs)

		Return fileNames
	End Function

End Module

Public Class WINDOWS_API
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
End Class

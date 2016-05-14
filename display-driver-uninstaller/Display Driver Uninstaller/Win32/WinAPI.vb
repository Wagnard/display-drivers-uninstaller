Imports System.IO
Imports System.Reflection
Imports System.ComponentModel
Imports System.Security.Principal
Imports System.Runtime.InteropServices

Namespace Win32
	<ComVisible(False)>
	Friend Class WinAPI
		Private Shared _is64 As Boolean
		Private Shared _isAdmin As Boolean
		Friend Shared ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)

		Friend Shared Property Is64 As Boolean
			Get
				Return _is64
			End Get
			Private Set(value As Boolean)
				_is64 = value
			End Set
		End Property

		Friend Shared Property IsAdmin As Boolean
			Get
				Return _isAdmin
			End Get
			Private Set(value As Boolean)
				_isAdmin = value
			End Set
		End Property

		Shared Sub New()
			IsAdmin = GetIsAdmin()
			Is64 = GetIs64()
		End Sub

#Region "Enums"

		Private Enum BinaryType As UInteger
			<Description("A 32-bit Windows-based application")>
			SCS_32BIT_BINARY = 0UI

			<Description("A 64-bit Windows-based application.")>
			SCS_64BIT_BINARY = 6UI

			<Description("An MS-DOS – based application")>
			SCS_DOS_BINARY = 1UI

			<Description("A 16-bit OS/2-based application")>
			SCS_OS216_BINARY = 5UI

			<Description("A PIF file that executes an MS-DOS – based application")>
			SCS_PIF_BINARY = 3UI

			<Description("A POSIX – based application")>
			SCS_POSIX_BINARY = 4UI

			<Description("A 16-bit Windows-based application")>
			SCS_WOW_BINARY = 2UI
		End Enum

#End Region

#Region "Structures"

		<StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
		Private Structure WIN32_FIND_DATA
			Public dwFileAttributes As UInt32
			Public ftCreationTime As System.Runtime.InteropServices.ComTypes.FILETIME
			Public ftLastAccessTime As System.Runtime.InteropServices.ComTypes.FILETIME
			Public ftLastWriteTime As System.Runtime.InteropServices.ComTypes.FILETIME
			Public nFileSizeHigh As UInt32
			Public nFileSizeLow As UInt32
			Public dwReserved0 As UInt32
			Public dwReserved1 As UInt32
			<MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)>
			Public cFileName As String
			<MarshalAs(UnmanagedType.ByValTStr, SizeConst:=14)>
			Public cAlternateFileName As String
		End Structure

#End Region

#Region "P/Invokes"

		<DllImport("kernel32.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
		Private Shared Function GetBinaryType(
   <[In](), MarshalAs(UnmanagedType.LPStr)> ByVal lpApplicationName As String,
   <[Out]()> ByRef lpBinaryType As BinaryType) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		<DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function FindFirstFile(
   <[In]()> ByVal pFileName As String,
   <[Out]()> ByRef lpFindFileData As WIN32_FIND_DATA) As IntPtr
		End Function

		<DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
		Private Shared Function FindNextFile(
   <[In]()> ByVal hFindFile As IntPtr,
   <[Out]()> ByRef lpFindFileData As WIN32_FIND_DATA) As Boolean
		End Function

		<DllImport("kernel32.dll", SetLastError:=True)>
		Private Shared Function FindClose(
   <[In]()> ByVal hFindFile As IntPtr) As Boolean
		End Function

#End Region

#Region "Functions"

		Private Shared Function GetIs64() As Boolean
			Dim binaryType As BinaryType

			Try
				If GetBinaryType(Assembly.GetExecutingAssembly().Location, binaryType) Then
					Return binaryType = binaryType.SCS_64BIT_BINARY
				End If
			Catch ex As Exception
			End Try

			Return False
		End Function

		Private Shared Function GetIsAdmin() As Boolean
			Try
				Return New WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)
			Catch ex As UnauthorizedAccessException
			Catch ex As Exception
			End Try

			Return False
		End Function

		Public Shared Function GetFileNames(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
			Dim fileNames As New List(Of String)(100)
			Dim findData As New WIN32_FIND_DATA
			Dim findHandle As IntPtr

			If Not directory.EndsWith(System.IO.Path.DirectorySeparatorChar) Then directory &= System.IO.Path.DirectorySeparatorChar

			Dim findDir As String
			Dim dirs As Queue(Of String) = New Queue(Of String)(1000)
			dirs.Enqueue(directory)

			Try
				While dirs.Count > 0
					findDir = dirs.Dequeue()
					findHandle = FindFirstFile(findDir & wildCard, findData)

					If findHandle <> INVALID_HANDLE Then
						Do
							If findData.cFileName <> "." AndAlso findData.cFileName <> ".." Then
								If (findData.dwFileAttributes And FileAttributes.Directory) <> FileAttributes.Directory Then
									fileNames.Add(String.Concat(findDir, findData.cFileName))
								Else
									If searchSubDirs Then
										dirs.Enqueue(String.Concat(findDir, findData.cFileName, Path.DirectorySeparatorChar))
									End If
								End If
							End If
						Loop While FindNextFile(findHandle, findData)

						FindClose(findHandle)
					End If
				End While
			Catch ex As Exception
				Application.Log.AddException(ex)
				Return New List(Of String)(0)
			Finally
				If findHandle <> INVALID_HANDLE Then
					FindClose(findHandle)
				End If
			End Try

			Return fileNames
		End Function

		Public Shared Function GetDirNames(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
			Dim dirNames As New List(Of String)(100)
			Dim findData As New WIN32_FIND_DATA
			Dim findHandle As New IntPtr

			If Not directory.EndsWith(Path.DirectorySeparatorChar) Then directory &= Path.DirectorySeparatorChar

			Dim findDir As String
			Dim dirs As Queue(Of String) = New Queue(Of String)(1000)
			dirs.Enqueue(directory)

			Try
				While dirs.Count > 0
					findDir = dirs.Dequeue()
					findHandle = FindFirstFile(findDir & wildCard, findData)

					If findHandle <> INVALID_HANDLE Then
						Do
							If findData.cFileName <> "." AndAlso findData.cFileName <> ".." Then
								If (findData.dwFileAttributes And FileAttributes.Directory) = FileAttributes.Directory Then
									dirNames.Add(findDir & findData.cFileName)

									If searchSubDirs Then
										dirs.Enqueue(findDir & findData.cFileName & Path.DirectorySeparatorChar)
									End If
								End If
							End If
						Loop While (FindNextFile(findHandle, findData))

						FindClose(findHandle)
					End If
				End While
			Catch ex As Exception
				Application.Log.AddException(ex)
				Return New List(Of String)(0)
			Finally
				If findHandle <> INVALID_HANDLE Then
					FindClose(findHandle)
				End If
			End Try

			Return dirNames
		End Function

#End Region

	End Class
End Namespace
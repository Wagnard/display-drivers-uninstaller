Imports System.Text
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Security.AccessControl
Imports System.Security.Principal

Imports Display_Driver_Uninstaller.Win32

Public Class FileIO

#Region "Consts"
	Friend Const UNC_PREFIX As String = "\\?\"
	Private Shared ReadOnly DIR_CHAR As String = Path.DirectorySeparatorChar
	Private Shared ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)

#End Region

#Region "Enums"

	<Flags()>
	Private Enum FILE_ATTRIBUTES As UInt32
		''' <summary>A file that is read-only.
		''' Applications can read the file, but cannot write to it or delete it.
		''' This attribute is not honored on directories.
		''' For more information, see "You cannot view or change the Read-only or
		''' the System attributes of folders in Windows Server 2003, in Windows XP, or in Windows Vista.</summary>
		[READONLY] = &H1UI

		''' <summary>The file or directory is hidden.
		''' It is not included in an ordinary directory listing.</summary>
		HIDDEN = &H2UI

		''' <summary>A file or directory that the operating system uses a part of, or uses exclusively.</summary>
		SYSTEM = &H4UI

		''' <summary>Files cannot be converted into directories.
		''' To create a directory, use the CreateDirectory or CreateDirectoryEx function.</summary>
		DIRECTORY = &H10UI

		''' <summary>A file or directory that is an archive file or directory.
		''' Applications typically use this attribute to mark files for backup or removal.</summary>
		ARCHIVE = &H20UI

		''' <summary>Reserved; do not use.</summary>
		DEVICE = &H40UI

		''' <summary>A file that does not have other attributes set.
		''' This attribute is valid only when used alone.</summary>
		NORMAL = &H80UI

		''' <summary>A file that is being used for temporary storage.
		''' File systems avoid writing data back to mass storage if sufficient cache memory is available,
		''' because typically, an application deletes a temporary file after the handle is closed.
		''' In that scenario, the system can entirely avoid writing the data. Otherwise,
		''' the data is written after the handle is closed.</summary>
		TEMPORARY = &H100UI

		''' <summary>To set a file's sparse attribute, use the DeviceIoControl function with the FSCTL_SET_SPARSE operation.</summary>
		SPARSE_FILE = &H200UI

		''' <summary>To associate a reparse point with a file or directory, use the DeviceIoControl function with the FSCTL_SET_REPARSE_POINT operation.</summary>
		REPARSE_POINT = &H400UI

		''' <summary>To set a file's compression state, use the DeviceIoControl function with the FSCTL_SET_COMPRESSION operation.</summary>
		COMPRESSED = &H800UI

		''' <summary>The data of a file is not available immediately.
		''' This attribute indicates that the file data is physically moved to offline storage.
		''' This attribute is used by Remote Storage, which is the hierarchical storage management software.
		''' Applications should not arbitrarily change this attribute.</summary>
		OFFLINE = &H1000UI

		''' <summary>The file or directory is not to be indexed by the content indexing service.</summary>
		NOT_CONTENT_INDEXED = &H2000UI

		''' <summary>To create an encrypted file, use the CreateFile function with the FILE_ATTRIBUTE_ENCRYPTED attribute.
		''' To convert an existing file into an encrypted file, use the EncryptFile function.</summary>
		ENCRYPTED = &H4000UI

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

	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function DeleteFile(
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function



	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function RemoveDirectory(
 <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function CreateDirectory(
 <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String,
 <[In](), [Optional]()> ByVal lpSecurityAttributes As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function



	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function GetFileAttributes(
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String) As UInt32
	End Function

	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function SetFileAttributes(
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpFileName As String,
  <[In]()> ByVal dwFileAttributes As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function




	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function GetLongPathName(
  <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpszShortPath As String,
  <[Out](), MarshalAs(UnmanagedType.LPWStr)> ByVal lpszLongPath As StringBuilder,
  <[In]()> ByVal longPathLength As UInt32) As UInt32
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

	<DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
	Private Shared Function FindClose(
   <[In]()> ByVal hFindFile As IntPtr) As Boolean
	End Function

#End Region

	' WORKING TEST FUNCTION TO CREATE LONG PATHS OVER 248+ CHARS
	' NO NEED TO CREATE ALL CHILD DIRS "RECURSIVELY", IT CREATES WHOLE DIRECTORY STRUCTURE AT ONCE
	Public Shared Sub CreateDir(ByVal dirPath As String)
		Try
			Dim dirs() As String = dirPath.Split("\"c)
			Dim sb As New StringBuilder(dirPath.Length + 4)
			Dim fileAttr As UInt32 = 0UI

			sb.Append(UNC_PREFIX)

			For Each dir As String In dirs
				If dir.Contains(":") Then		' Drive
					sb.Append(dir)
					Continue For
				End If

				sb.Append("\" & dir)

				fileAttr = GetFileAttributes(sb.ToString())

				If fileAttr = Errors.INVALID_FILE_ATTRIBUTES Then	'Doesn't exists
					CreateDirectory(sb.ToString(), IntPtr.Zero)
				End If
			Next

		Catch ex As Exception
			MessageBox.Show(ex.Message)
		End Try
	End Sub

	' WORKING TEST FUNCTION TO REMOVE ABOVE LONG PATHS (IF EXPLORER CAN'T DELETE THEM)
	Public Shared Sub DeleteDir(ByVal dirPath As String)
		Try
			If RemoveDirectory(If(dirPath.StartsWith(UNC_PREFIX), dirPath, UNC_PREFIX & dirPath)) Then
				Return
			End If

			Dim errCode As UInt32 = GetLastWin32ErrorU()

			If errCode <> 0UI AndAlso errCode <> Errors.FILE_NOT_FOUND Then
				Throw New Win32Exception(GetInt32(errCode))
			End If

		Catch ex As Exception
			MessageBox.Show(ex.Message)
		End Try
	End Sub




	' DO NOT USE ON CLEANING. WORK IN PROGRESS. NEEDS TESTING!
	Public Shared Sub Delete(ByVal fileName As String)
		DeleteInternal(fileName, False)
	End Sub

	Public Shared Function CountFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = True) As Int32
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetFileCount(directory, wildCard, searchSubDirs)
	End Function

	Public Shared Function GetFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetFileNames(directory, wildCard, searchSubDirs, False)
	End Function

	Public Shared Function GetDirectories(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetDirNames(directory, wildCard, searchSubDirs, False)
	End Function



	Private Shared Sub DeleteInternal(ByVal fileName As String, ByVal fixAcl As Boolean)
		If IsNullOrWhitespace(fileName) Then
			Return
		End If

		Dim errCode As UInt32 = 0UI
		Dim isDir As Boolean = False
		Dim newFileName As String = If(fileName.StartsWith(UNC_PREFIX), fileName, UNC_PREFIX & fileName)

		Try
			errCode = SetAttributes(newFileName, FILE_ATTRIBUTES.NORMAL, isDir)		' restores attributes, checks does it exist, check is dir or file

			If errCode <> Errors.ACCESS_DENIED Then
				If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
					newFileName = GetLongPath(fileName)		' Check if was short path

					If newFileName Is Nothing Then
						Return			' Wasn't short path... Just doesn't exist
					End If

					If Not newFileName.StartsWith(UNC_PREFIX) Then
						newFileName = UNC_PREFIX & newFileName
					End If

					errCode = SetAttributes(newFileName, FILE_ATTRIBUTES.NORMAL, isDir)

					If errCode <> 0UI Then
						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If

				If isDir Then		' fileName is directory
					If RemoveDirectory(newFileName) Then
						Application.Log.AddMessage("Directory deleted!", "Path", fileName)
						Return
					End If

					errCode = GetLastWin32ErrorU()

					If errCode = Errors.DIR_NOT_EMPTY Then
						Dim files As List(Of String) = GetFiles(newFileName, "*", True)

						For i As Int32 = files.Count - 1 To 0 Step -1
							Delete(files(i))
						Next

						files = GetDirectories(newFileName, "*", True)

						For i As Int32 = files.Count - 1 To 0 Step -1
							Delete(files(i))
						Next

						System.Threading.Thread.Sleep(100)		' Windows is slow... takes bit time to files be deleted.

						Dim waits As Int32 = 0

						While True
							If GetFileCount(newFileName, "*", True) > 0 Then
								waits += 1
								System.Threading.Thread.Sleep(100)
							Else
								Exit While
							End If

							If waits >= 100 Then		'MAX 10 sec to remove all files.
								Exit While
							End If
						End While


						If RemoveDirectory(newFileName) Then
							Application.Log.AddMessage("Directory deleted!", "Path", fileName)
							Return
						Else
							Throw New Win32Exception(GetLastWin32Error)
						End If
					End If
				Else				' fileName is file
					If DeleteFile(newFileName) Then
						Application.Log.AddMessage("File deleted!", "Path", fileName)
						Return
					End If
				End If

				If errCode <> Errors.ACCESS_DENIED AndAlso errCode <> Errors.FILE_NOT_FOUND AndAlso errCode <> Errors.PATH_NOT_FOUND Then
					Throw New Win32Exception(GetInt32(errCode))
				End If
			End If

			' ACCESS_DENIED
			If Not fixAcl Then
				MessageBox.Show("Permission fix doesn't work yet!", "ACCESS_DENIED")
				Return

				If isDir Then
					ACL.AddDirSecurity(newFileName, FileSystemRights.DeleteSubdirectoriesAndFiles)
				Else
					ACL.AddFileSecurity(newFileName, FileSystemRights.Delete)
				End If

				DeleteInternal(fileName, True)	'Retry
				Return
			Else
				Throw New Win32Exception(GetInt32(errCode))
			End If
		Catch ex As Win32Exception
			errCode = GetUInt32(ex.ErrorCode)

			Select Case errCode
				Case Errors.FILE_NOT_FOUND, Errors.PATH_NOT_FOUND
					Return
				Case Else
					If Not fixAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						DeleteInternal(fileName, True)
					Else
						Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't delete file!")
						logEntry.Type = LogType.Error
						logEntry.Add("fileName", fileName)
						logEntry.Add("fixAcl", fixAcl.ToString())

						Application.Log.Add(logEntry)
					End If
			End Select
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't delete file!")
			logEntry.Type = LogType.Error
			logEntry.Add("fileName", fileName)

			Application.Log.Add(logEntry)
		End Try
	End Sub

	Private Shared Function GetFilesInternal(ByVal fileName As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal searchFiles As Boolean, ByVal fixAcl As Boolean) As List(Of String)
		GetFilesInternal = New List(Of String)(0)

		If IsNullOrWhitespace(fileName) Then
			Return GetFilesInternal
		End If

		Dim errCode As UInt32 = 0UI
		Dim isDir As Boolean = False
		Dim newFileName As String = If(fileName.StartsWith(UNC_PREFIX), fileName, UNC_PREFIX & fileName)

		Try
			errCode = GetAttributes(newFileName, isDir)		   ' checks does it exist, check is dir or file

			If errCode <> Errors.ACCESS_DENIED Then
				If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
					newFileName = GetLongPath(fileName)			' Check if was short path

					If newFileName Is Nothing Then
						Return GetFilesInternal					' Wasn't short path... Just doesn't exist
					End If

					errCode = GetAttributes(newFileName, isDir)

					If errCode <> 0UI Then
						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If

				If Not isDir Then
					Throw New ArgumentException("Search path isn't a directory!", "fileName")
				End If

				If searchFiles Then
					Return GetFileNames(newFileName, wildCard, searchSubDirs, True)
				Else
					Return GetDirNames(newFileName, wildCard, searchSubDirs, True)
				End If
			End If

			' ACCESS_DENIED
			If Not fixAcl Then
				ACL.AddDirSecurity(newFileName, FileSystemRights.Read And FileSystemRights.ListDirectory)

				Return GetFilesInternal(fileName, wildCard, searchSubDirs, searchFiles, True)	'Retry
			Else
				Throw New Win32Exception(GetInt32(errCode))
			End If
		Catch ex As Win32Exception
			errCode = GetUInt32(ex.ErrorCode)

			Select Case errCode
				Case Errors.FILE_NOT_FOUND, Errors.PATH_NOT_FOUND
					Return GetFilesInternal
				Case Else
					If Not fixAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						GetFilesInternal(fileName, wildCard, searchSubDirs, searchFiles, True)
					Else
						Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't delete file!")
						logEntry.Type = LogType.Error
						logEntry.Add("fileName", fileName)
						logEntry.Add("fixAcl", fixAcl.ToString())

						Application.Log.Add(logEntry)
					End If
			End Select
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't delete file!")
			logEntry.Type = LogType.Error
			logEntry.Add("fileName", fileName)

			Application.Log.Add(logEntry)
		End Try
	End Function

	Private Shared Function GetFileCount(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean) As Int32
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As IntPtr
		Dim count As Int32 = 0

		If Not directory.EndsWith(DIR_CHAR) Then directory &= DIR_CHAR
		If Not directory.StartsWith(UNC_PREFIX) Then directory = UNC_PREFIX & directory

		Dim findDir As String
		Dim dirs As Queue(Of String)

		If searchSubDirs Then
			dirs = New Queue(Of String)(GetDirNames(directory, "*", True, True))
			dirs.Enqueue(directory)
		Else
			dirs = New Queue(Of String)(New String() {directory})
		End If

		Try
			While dirs.Count > 0
				findDir = dirs.Dequeue()

				If Not findDir.EndsWith(DIR_CHAR) Then findDir &= DIR_CHAR

				findHandle = FindFirstFile(findDir & wildCard, findData)

				If findHandle <> INVALID_HANDLE Then
					Do
						If findData.cFileName <> "." AndAlso findData.cFileName <> ".." AndAlso (findData.dwFileAttributes And FileAttributes.Directory) <> FileAttributes.Directory Then
							count += 1
						End If
					Loop While FindNextFile(findHandle, findData)

					FindClose(findHandle)
				End If
			End While
		Catch ex As Exception
			Application.Log.AddException(ex)
		Finally
			If findHandle <> INVALID_HANDLE Then
				FindClose(findHandle)
			End If
		End Try

		Return count
	End Function

	Private Shared Function GetFileNames(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal unicodePaths As Boolean) As List(Of String)
		Dim fileNames As New List(Of String)(100)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As IntPtr

		If Not directory.EndsWith(DIR_CHAR) Then directory &= DIR_CHAR
		If Not directory.StartsWith(UNC_PREFIX) Then directory = UNC_PREFIX & directory

		Dim findDir As String
		Dim dirs As Queue(Of String)

		If searchSubDirs Then
			dirs = New Queue(Of String)(GetDirNames(directory, "*", True, True))
			dirs.Enqueue(directory)
		Else
			dirs = New Queue(Of String)(New String() {directory})
		End If

		Try
			While dirs.Count > 0
				findDir = dirs.Dequeue()

				If Not findDir.EndsWith(DIR_CHAR) Then findDir &= DIR_CHAR

				findHandle = FindFirstFile(findDir & wildCard, findData)

				If findHandle <> INVALID_HANDLE Then
					Do
						If findData.cFileName <> "." AndAlso findData.cFileName <> ".." AndAlso (findData.dwFileAttributes And FileAttributes.Directory) <> FileAttributes.Directory Then
							If unicodePaths Then
								fileNames.Add(String.Concat(findDir, findData.cFileName))
							Else
								fileNames.Add(String.Concat(findDir.Substring(UNC_PREFIX.Length), findData.cFileName))
							End If
						End If
					Loop While FindNextFile(findHandle, findData)

					FindClose(findHandle)
				End If
			End While
		Catch ex As Exception
			Application.Log.AddException(ex)
		Finally
			If findHandle <> INVALID_HANDLE Then
				FindClose(findHandle)
			End If
		End Try

		Return fileNames
	End Function

	Private Shared Function GetDirNames(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal unicodePaths As Boolean) As List(Of String)
		Dim dirNames As New List(Of String)(100)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As New IntPtr

		If Not directory.EndsWith(DIR_CHAR) Then directory &= DIR_CHAR
		If Not directory.StartsWith(UNC_PREFIX) Then directory = UNC_PREFIX & directory

		Dim findDir As String
		Dim dirs As Queue(Of String) = New Queue(Of String)(1000)
		dirs.Enqueue(directory)

		Try
			While dirs.Count > 0
				findDir = dirs.Dequeue()
				findHandle = FindFirstFile(findDir & wildCard, findData)

				If findHandle <> INVALID_HANDLE Then
					Do
						If findData.cFileName <> "." AndAlso findData.cFileName <> ".." AndAlso (findData.dwFileAttributes And FileAttributes.Directory) = FileAttributes.Directory Then

							If unicodePaths Then
								dirNames.Add(String.Concat(findDir, findData.cFileName))
							Else
								dirNames.Add(String.Concat(findDir.Substring(UNC_PREFIX.Length), findData.cFileName))
							End If

							If searchSubDirs Then
								dirs.Enqueue(findDir & findData.cFileName & Path.DirectorySeparatorChar)
							End If
						End If
					Loop While (FindNextFile(findHandle, findData))

					FindClose(findHandle)
				End If
			End While
		Catch ex As Exception
			Application.Log.AddException(ex)
		Finally
			If findHandle <> INVALID_HANDLE Then
				FindClose(findHandle)
			End If
		End Try

		Return dirNames
	End Function

	Private Shared Function GetLongPath(ByVal path As String) As String
		Dim sb As New StringBuilder
		Dim requiredSize As UInt32 = GetLongPathName(If(path.StartsWith(UNC_PREFIX), path, UNC_PREFIX & path), sb, 0UI)

		If requiredSize <> 0UI Then
			sb.EnsureCapacity(GetInt32(requiredSize) + 2)
		End If

		Dim errCode As UInt32 = GetLastWin32ErrorU()

		If errCode = Errors.INSUFFICIENT_BUFFER Then
			sb.EnsureCapacity(GetInt32(requiredSize + 1UI))
		ElseIf errCode = Errors.PATH_NOT_FOUND OrElse errCode = Errors.FILE_NOT_FOUND Then
			Return Nothing
		ElseIf errCode <> 0 Then
			Throw New Win32Exception(GetInt32(errCode))
		End If

		GetLongPathName(If(path.StartsWith(UNC_PREFIX), path, UNC_PREFIX & path), sb, GetUInt32(sb.Capacity))

		errCode = GetLastWin32ErrorU()

		If errCode <> 0 Then
			Throw New Win32Exception(GetInt32(errCode))
		End If

		Return sb.ToString()
	End Function

	Private Shared Function SetAttributes(ByVal filePath As String, ByVal fileAttributes As FILE_ATTRIBUTES, ByRef isDirectory As Boolean) As UInt32
		Dim fileAttr As UInt32 = GetFileAttributes(filePath)

		If fileAttr = Errors.INVALID_FILE_ATTRIBUTES Then	  'Doesn't exists
			Return Errors.FILE_NOT_FOUND
		End If

		If Not SetFileAttributes(filePath, fileAttributes) Then
			Return GetLastWin32ErrorU()
		End If

		isDirectory = ((fileAttr And FILE_ATTRIBUTES.DIRECTORY) = FILE_ATTRIBUTES.DIRECTORY)
		Return 0UI
	End Function

	Private Shared Function GetAttributes(ByVal filePath As String, ByRef isDirectory As Boolean) As UInt32
		Dim fileAttr As UInt32 = GetFileAttributes(filePath)

		If fileAttr = Errors.INVALID_FILE_ATTRIBUTES Then	  'Doesn't exists
			Return Errors.FILE_NOT_FOUND
		End If

		isDirectory = ((fileAttr And FILE_ATTRIBUTES.DIRECTORY) = FILE_ATTRIBUTES.DIRECTORY)
		Return 0UI
	End Function

End Class


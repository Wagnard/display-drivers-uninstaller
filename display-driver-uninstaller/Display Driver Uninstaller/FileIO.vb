Imports System.IO
Imports System.Text
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Security.AccessControl
Imports System.Security.Principal

Imports Display_Driver_Uninstaller.Win32

Public Class FileIO

#Region "Consts"
	Friend Const UNC_PREFIX As String = "\\?\"
	Friend Shared ReadOnly DIR_CHAR As String = Path.DirectorySeparatorChar
	Private Shared ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)

#End Region

#Region "Enums"

	<Flags()>
	Friend Enum FILE_ATTRIBUTES As UInt32
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


		''' <summary>The file is being opened or created for a backup or restore operation.
		''' The system ensures that the calling process overrides file security checks when the process
		''' has SE_BACKUP_NAME and SE_RESTORE_NAME privileges. For more information, see Changing Privileges in a Token.
		'''
		''' You must set this flag to obtain a handle to a directory. 
		''' A directory handle can be passed to some functions instead of a file handle. 
		''' For more information, see the Remarks section.</summary>
		FLAG_BACKUP_SEMANTICS = &H2000000UI

		''' <summary>The file is to be deleted immediately after all of its handles are closed,
		''' which includes the specified handle and any other open or duplicated handles.
		'''
		''' If there are existing open handles to a file,
		''' the call fails unless they were all opened with the FILE_SHARE_DELETE share mode.
		'''
		''' Subsequent open requests for the file fail, unless the FILE_SHARE_DELETE share mode is specified.</summary>
		FLAG_DELETE_ON_CLOSE = &H4000000UI

		''' <summary>The file or device is being opened with no system caching for data reads and writes.
		''' This flag does not affect hard disk caching or memory mapped files.
		'''
		''' There are strict requirements for successfully working with files opened with CreateFile
		''' using the FILE_FLAG_NO_BUFFERING flag, for details see File Buffering.</summary>
		FLAG_NO_BUFFERING = &H20000000UI

		''' <summary>The file data is requested, but it should continue to be located in remote storage.
		''' It should not be transported back to local storage. This flag is for use by remote storage systems.</summary>
		FLAG_OPEN_NO_RECALL = &H100000UI

		''' <summary>Normal reparse point processing will not occur;
		''' CreateFile will attempt to open the reparse point. When a file is opened,
		''' a file handle is returned, whether or not the filter that controls the reparse point is operational.
		'''
		''' This flag cannot be used with the CREATE_ALWAYS flag.
		'''
		''' If the file is not a reparse point, then this flag is ignored.
		'''
		''' For more information, see the Remarks section.</summary>
		FLAG_OPEN_REPARSE_POINT = &H200000UI

		''' <summary>The file or device is being opened or created for asynchronous I/O.
		''' 
		''' When subsequent I/O operations are completed on this handle,
		''' the event specified in the OVERLAPPED structure will be set to the signaled state.
		''' 
		''' If this flag is specified, the file can be used for simultaneous read and write operations.
		''' 
		''' If this flag is not specified, then I/O operations are serialized,
		''' even if the calls to the read and write functions specify an OVERLAPPED structure.
		''' 
		''' For information about considerations when using a file handle created with this flag,
		''' see the Synchronous and Asynchronous I/O Handles section of this topic.</summary>
		FLAG_OVERLAPPED = &H40000000UI

		''' <summary>Access will occur according to POSIX rules.
		''' This includes allowing multiple files with names, differing only in case,
		''' for file systems that support that naming. Use care when using this option,
		''' because files created with this flag may not be accessible by applications that are written for MS-DOS or 16-bit Windows.</summary>
		FLAG_POSIX_SEMANTICS = &H100000UI

		''' <summary>Access is intended to be random. The system can use this as a hint to optimize file caching.
		''' 
		''' This flag has no effect if the file system does not support cached I/O and FILE_FLAG_NO_BUFFERING.
		''' 
		''' For more information, see the Caching Behavior section of this topic.</summary>
		FLAG_RANDOM_ACCESS = &H10000000UI

		''' <summary>The file or device is being opened with session awareness.
		''' If this flag is not specified, then per-session devices (such as a device using RemoteFX USB Redirection)
		''' cannot be opened by processes running in session 0. This flag has no effect for callers not in session 0.
		''' This flag is supported only on server editions of Windows.
		'''
		''' Windows Server 2008 R2 and Windows Server 2008:  This flag is not supported before Windows Server 2012.</summary>
		FLAG_SESSION_AWARE = &H800000UI

		''' <summary>Access is intended to be sequential from beginning to end.
		''' The system can use this as a hint to optimize file caching.
		''' 
		''' This flag should not be used if read-behind (that is, reverse scans) will be used.
		''' 
		''' This flag has no effect if the file system does not support cached I/O and FILE_FLAG_NO_BUFFERING.
		''' 
		''' For more information, see the Caching Behavior section of this topic.</summary>
		FLAG_SEQUENTIAL_SCAN = &H8000000UI

		''' <summary>Write operations will not go through any intermediate cache, they will go directly to disk.
		''' For additional information, see the Caching Behavior section of this topic.</summary>
		FLAG_WRITE_THROUGH = &H80000000UI
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
   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pFileName As String,
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

#Region "TESTING SECTION"

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
		Catch ex As Exception When TypeOf ex Is Win32Exception
			Dim wEx As Win32Exception = TryCast(ex, Win32Exception)

			If wEx IsNot Nothing Then
				MessageBox.Show(wEx.Message & CRLF & "Win32_ErrorCode: " & wEx.NativeErrorCode.ToString(), "Win32Exception")
			Else
				MessageBox.Show(ex.Message)
			End If
		Catch ex As Exception
			MessageBox.Show(ex.Message)
		End Try
	End Sub

	' Removed Testing function... Use Delete()
	' I assume you found ERROR_DIR_NOT_EMPTY exception... ^^

#End Region

#Region "Functions"

	Public Shared Sub Delete(ByVal fileName As String)
		DeleteInternal(fileName, False)
	End Sub

	Public Shared Function ExistsFile(ByVal fileName As String) As Boolean
		Dim isDir As Boolean = True

		Return (Exists(fileName, isDir) AndAlso Not isDir)
	End Function

	Public Shared Function ExistsDir(ByVal dirName As String) As Boolean
		Dim isDir As Boolean = False

		Return (Exists(dirName, isDir) AndAlso isDir)
	End Function

	Public Shared Function CountFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = True) As Int32
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetFileCount(directory, wildCard, searchSubDirs)
	End Function

	Public Shared Function CountDirectories(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = True) As Int32
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetDirCount(directory, wildCard, searchSubDirs)
	End Function

	Public Shared Function GetFiles(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetFileNames(directory, wildCard, searchSubDirs, False, False, False)
	End Function

	Public Shared Function GetDirectories(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
		If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
			wildCard = "*"
		End If

		Return GetDirNames(directory, wildCard, searchSubDirs, False, False, False)
	End Function



	Private Shared Sub DeleteInternal(ByVal fileName As String, ByVal fixedAcl As Boolean)
		If IsNullOrWhitespace(fileName) Then
			Return
		End If

		Dim errCode As UInt32 = 0UI
		Dim isDir As Boolean = False
		Dim uncFileName As String = If(fileName.StartsWith(UNC_PREFIX), fileName, UNC_PREFIX & fileName)

		If fileName.StartsWith(UNC_PREFIX) Then
			fileName = fileName.Substring(UNC_PREFIX.Length)
		End If

		Try
			errCode = SetAttributes(uncFileName, FILE_ATTRIBUTES.NORMAL, isDir)		' restores attributes, checks does it exist, check is dir or file

			If errCode <> Errors.ACCESS_DENIED Then

				If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
					uncFileName = GetLongPath(fileName)		' Check if was short path

					If uncFileName Is Nothing Then
						Return			' Wasn't short path... Just doesn't exist
					End If

					If Not uncFileName.StartsWith(UNC_PREFIX) Then
						uncFileName = UNC_PREFIX & uncFileName
					End If

					errCode = SetAttributes(uncFileName, FILE_ATTRIBUTES.NORMAL, isDir)

					If errCode <> 0UI Then
						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If

				If isDir Then		' fileName is directory
					If RemoveDirectory(uncFileName) Then
						Application.Log.AddMessage(String.Concat("Deleted directory:", CRLF, fileName))
						Return
					End If

					errCode = GetLastWin32ErrorU()

					If errCode = Errors.DIR_NOT_EMPTY Then
						Dim files As List(Of String) = GetFilesToDeleteInternal(uncFileName, "*", True, True, False)

						For i As Int32 = files.Count - 1 To 0 Step -1
							Delete(files(i))
						Next

						'System.Threading.Thread.Sleep(10)		' Windows is slow... takes bit time to files be deleted.

						files = GetFilesToDeleteInternal(uncFileName, "*", True, False, False)

						For i As Int32 = files.Count - 1 To 0 Step -1
							Delete(files(i))
						Next

						'System.Threading.Thread.Sleep(10)		' Windows is slow... takes bit time to files be deleted.

						Dim waits As Int32 = 0

						While waits < 30						 'MAX 3 sec APROX to wait Windows remove all files. ( 30 * 100ms)
							If GetFileCount(uncFileName, "*", True) > 0 Then
								waits += 1
								System.Threading.Thread.Sleep(100)
							Else
								Exit While
							End If
							waits += 1
						End While

						If RemoveDirectory(uncFileName) Then
							Application.Log.AddMessage(String.Concat("Deleted directory:", CRLF, fileName))
							Return
						Else
							Throw New Win32Exception(GetLastWin32Error)
						End If
					ElseIf errCode <> 0UI Then
						Throw New Win32Exception(GetInt32(errCode))
					End If
				Else				' fileName is file
					If DeleteFile(uncFileName) Then
						Application.Log.AddMessage(String.Concat("Deleted file:", CRLF, fileName))
						Return
					Else
						errCode = GetLastWin32ErrorU()
					End If
				End If
				If errCode <> Errors.ACCESS_DENIED AndAlso errCode <> Errors.FILE_NOT_FOUND AndAlso errCode <> Errors.PATH_NOT_FOUND Then
					Throw New Win32Exception(GetInt32(errCode))
				End If
			End If

			' ACCESS_DENIED
			If Not fixedAcl Then
				Dim logEntry As LogEntry = Application.Log.CreateEntry()
				logEntry.Type = LogType.Warning
				logEntry.Message = String.Format("Couldn't delete path, access denied! Attempting to fix path's permissions.{0}{1}", CRLF, fileName)
				logEntry.Add("fileName", fileName)

				Dim success As Boolean
				Try
					success = ACL.FixFileSecurity(uncFileName, logEntry)
				Finally
					logEntry.Add("Fixed", If(success, "Yes", "No"))

					If Not success Then
						logEntry.Type = LogType.Error
					End If

					Application.Log.Add(logEntry)
				End Try

				DeleteInternal(fileName, True)	'Retry

				Return
			Else
				Throw New Win32Exception(GetInt32(errCode))
			End If
		Catch ex As Win32Exception
			errCode = GetUInt32(ex.NativeErrorCode)

			Select Case errCode
				Case Errors.FILE_NOT_FOUND, Errors.PATH_NOT_FOUND
					Return
				Case Else
					If Not fixedAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						DeleteInternal(fileName, True)
					Else
						Dim logEntry As LogEntry = Application.Log.CreateEntry(ex)

						If errCode = Errors.SHARING_VIOLATION Then
							logEntry.Message = String.Concat("Couldn't delete path! File is used by another process!", CRLF, fileName)
						Else
							logEntry.Message = String.Concat("Couldn't delete path!", CRLF, fileName)
						End If

						logEntry.Type = LogType.Error
						logEntry.Add("fileName", fileName)
						logEntry.Add("fixedAcl", fixedAcl.ToString())

						Application.Log.Add(logEntry)
					End If
			End Select
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, String.Concat("Couldn't delete path!", CRLF, fileName))
			logEntry.Type = LogType.Error
			logEntry.Add("fileName", fileName)
			logEntry.Add("fixedAcl", fixedAcl.ToString())

			Application.Log.Add(logEntry)
		End Try
	End Sub

	Private Shared Function Exists(ByVal fileName As String, ByRef isDirectory As Boolean) As Boolean
		If Not fileName.StartsWith(UNC_PREFIX) Then
			fileName = UNC_PREFIX & fileName
		End If

		Dim fileAttr As UInt32 = GetAttributes(fileName, isDirectory)

		If fileAttr = Errors.FILE_NOT_FOUND Then
			Return False
		End If

		Return True
	End Function

	Private Shared Function GetFilesToDeleteInternal(ByVal fileName As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal searchFiles As Boolean, ByVal fixedAcl As Boolean) As List(Of String)
		GetFilesToDeleteInternal = New List(Of String)(0)

		If IsNullOrWhitespace(fileName) Then
			Return GetFilesToDeleteInternal
		End If

		Dim errCode As UInt32 = 0UI
		Dim isDir As Boolean = False
		Dim uncFileName As String

		If fileName.StartsWith(UNC_PREFIX) Then
			uncFileName = fileName
			fileName = fileName.Substring(UNC_PREFIX.Length)
		Else
			uncFileName = UNC_PREFIX & fileName
		End If

		Try
			errCode = GetAttributes(uncFileName, isDir)		   ' checks does it exist, check is dir or file

			If errCode <> Errors.ACCESS_DENIED Then
				If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
					uncFileName = GetLongPath(fileName)			' Check if was short path

					If uncFileName Is Nothing Then
						Return GetFilesToDeleteInternal					' Wasn't short path... Just doesn't exist
					End If

					errCode = GetAttributes(uncFileName, isDir)

					If errCode <> 0UI Then
						If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
							Return GetFilesToDeleteInternal
						End If

						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If

				If Not isDir Then
					Throw New ArgumentException("Search path isn't a directory!", "fileName")
				End If

				If searchFiles Then
					Return GetFileNames(uncFileName, wildCard, searchSubDirs, True, True, False)
				Else
					Return GetDirNames(uncFileName, wildCard, searchSubDirs, True, True, False)
				End If
			End If

			' ACCESS_DENIED
			If Not fixedAcl Then
				Dim logEntry As LogEntry = Application.Log.CreateEntry()
				logEntry.Type = LogType.Warning
				logEntry.Message = String.Format("Couldn't find {0}, access denied! Attempting to fix path's permissions.{1}{2}", If(searchFiles, "files", "directories"), CRLF, fileName)
				logEntry.Add("fileName", fileName)

				Dim success As Boolean
				Try
					success = ACL.FixFileSecurity(uncFileName, logEntry)
				Finally
					logEntry.Add("Fixed", If(success, "Yes", "No"))

					If Not success Then
						logEntry.Type = LogType.Error
					End If

					Application.Log.Add(logEntry)
				End Try

				Return GetFilesToDeleteInternal(fileName, wildCard, searchSubDirs, searchFiles, True)	'Retry
			Else
				Throw New Win32Exception(GetInt32(errCode))
			End If
		Catch ex As Win32Exception
			errCode = GetUInt32(ex.ErrorCode)

			Select Case errCode
				Case Errors.FILE_NOT_FOUND, Errors.PATH_NOT_FOUND
					Return GetFilesToDeleteInternal
				Case Else
					If Not fixedAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						GetFilesToDeleteInternal(fileName, wildCard, searchSubDirs, searchFiles, True)
					Else
						Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, String.Format("Error! Couldn't find {0}!{1}{2}", If(searchFiles, "files", "directories"), CRLF, fileName))
						logEntry.Type = LogType.Error
						logEntry.Add("fileName", fileName)
						logEntry.Add("wildCard", wildCard)
						logEntry.Add("searchSubDirs", searchSubDirs.ToString())
						logEntry.Add("searchFiles", searchFiles.ToString())
						logEntry.Add("fixedAcl", fixedAcl.ToString())

						Application.Log.Add(logEntry)
					End If
			End Select
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, String.Format("Error! Couldn't find {0}!{1}{2}", If(searchFiles, "files", "directories"), CRLF, fileName))
			logEntry.Type = LogType.Error
			logEntry.Add("fileName", fileName)
			logEntry.Add("wildCard", wildCard)
			logEntry.Add("searchSubDirs", searchSubDirs.ToString())
			logEntry.Add("searchFiles", searchFiles.ToString())
			logEntry.Add("fixedAcl", fixedAcl.ToString())

			Application.Log.Add(logEntry)
		End Try
	End Function

	Private Shared Function GetFileCount(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean) As Int32
		Return GetFileNames(directory, wildCard, searchSubDirs, True, False, False).Count
	End Function

	Private Shared Function GetDirCount(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean) As Int32
		Return GetDirNames(directory, wildCard, searchSubDirs, True, False, False).Count
	End Function



	Private Shared Function GetFileNames(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal unicodePaths As Boolean, ByVal writeAccess As Boolean, ByVal fixedAcl As Boolean) As List(Of String)
		Dim fileNames As New List(Of String)(100)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As IntPtr
		Dim uncDirectory As String

		Try
			If Not directory.EndsWith(DIR_CHAR) Then directory &= DIR_CHAR

			If directory.StartsWith(UNC_PREFIX) Then
				uncDirectory = directory
				directory = directory.Substring(UNC_PREFIX.Length)
			Else
				uncDirectory = UNC_PREFIX & directory
			End If

			Dim findDir As String
			Dim dirs As Queue(Of String)

			If searchSubDirs Then
				dirs = New Queue(Of String)(GetDirNames(uncDirectory, "*", True, True, writeAccess, fixedAcl))
				dirs.Enqueue(uncDirectory)
			Else
				dirs = New Queue(Of String)(New String() {uncDirectory})
			End If

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
				Else
					Dim errCode As UInt32 = GetLastWin32ErrorU()

					If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
						Return fileNames
					End If

					If Not fixedAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						If writeAccess Then
							Dim logEntry As LogEntry = Application.Log.CreateEntry()
							logEntry.Type = LogType.Warning
							logEntry.Add("directory", directory)

							logEntry.Message = String.Format("Couldn't find files, access denied! Attempting to fix path's permissions.{0}{1}", CRLF, directory)

							Dim success As Boolean

							Try
								success = ACL.FixFileSecurity(uncDirectory, logEntry)
							Catch ex As Exception When TypeOf (ex) Is Win32Exception
								logEntry.AddException(ex, False)
							Finally
								logEntry.Add("Fixed", If(success, "Yes", "No"))

								If Not success Then
									logEntry.Type = LogType.Error
								End If

								Application.Log.Add(logEntry)
							End Try

							Return GetFileNames(directory, wildCard, searchSubDirs, unicodePaths, writeAccess, True)
						Else
							Application.Log.AddWarningMessage("Couldn't find files, access denied!{0}{1}", CRLF, directory)
						End If
					Else
						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If
			End While
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't find files!")
			logEntry.Type = LogType.Error
			logEntry.Add("directory", directory)
			logEntry.Add("wildCard", wildCard)
			logEntry.Add("searchSubDirs", searchSubDirs.ToString())
			logEntry.Add("unicodePaths", unicodePaths.ToString())
			logEntry.Add("fixedAcl", fixedAcl.ToString())

			Application.Log.Add(logEntry)
		Finally
			If findHandle <> INVALID_HANDLE Then
				FindClose(findHandle)
			End If
		End Try

		Return fileNames
	End Function

	Private Shared Function GetDirNames(ByVal directory As String, ByVal wildCard As String, ByVal searchSubDirs As Boolean, ByVal unicodePaths As Boolean, ByVal writeAccess As Boolean, ByVal fixedAcl As Boolean) As List(Of String)
		Dim dirNames As New List(Of String)(100)
		Dim findData As New WIN32_FIND_DATA
		Dim findHandle As New IntPtr
		Dim uncDirectory As String

		Try
			If Not directory.EndsWith(DIR_CHAR) Then directory &= DIR_CHAR

			If directory.StartsWith(UNC_PREFIX) Then
				uncDirectory = directory
				directory = directory.Substring(UNC_PREFIX.Length)
			Else
				uncDirectory = UNC_PREFIX & directory
			End If

			Dim findDir As String
			Dim dirs As Queue(Of String) = New Queue(Of String)(1000)
			dirs.Enqueue(uncDirectory)

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
								dirs.Enqueue(findDir & findData.cFileName & DIR_CHAR)
							End If
						End If
					Loop While (FindNextFile(findHandle, findData))

					FindClose(findHandle)
				Else
					Dim errCode As UInt32 = GetLastWin32ErrorU()

					If errCode = Errors.FILE_NOT_FOUND OrElse errCode = Errors.PATH_NOT_FOUND Then
						Return dirNames
					End If

					If Not fixedAcl AndAlso errCode = Errors.ACCESS_DENIED Then
						If writeAccess Then
							Dim logEntry As LogEntry = Application.Log.CreateEntry()
							logEntry.Type = LogType.Warning
							logEntry.Message = String.Format("Couldn't find directories, access denied! Attempting to fix path's permissions.{0}{1}", CRLF, directory)
							logEntry.Add("directory", directory)

							Dim success As Boolean

							Try
								success = ACL.FixFileSecurity(uncDirectory, logEntry)
							Finally
								logEntry.Add("Fixed", If(success, "Yes", "No"))

								If Not success Then
									logEntry.Type = LogType.Error
								End If

								Application.Log.Add(logEntry)
							End Try

							Return GetDirNames(directory, wildCard, searchSubDirs, unicodePaths, writeAccess, True)
						Else
							Application.Log.AddWarningMessage("Couldn't find directories, access denied!{0}{1}", CRLF, directory)
						End If
					Else
						Throw New Win32Exception(GetInt32(errCode))
					End If
				End If
			End While
		Catch ex As Exception
			Dim logEntry As LogEntry = Application.Log.CreateEntry(ex, "Couldn't find directories!")
			logEntry.Type = LogType.Error
			logEntry.Add("directory", directory)
			logEntry.Add("wildCard", wildCard)
			logEntry.Add("searchSubDirs", searchSubDirs.ToString())
			logEntry.Add("unicodePaths", unicodePaths.ToString())
			logEntry.Add("fixedAcl", fixedAcl.ToString())

			Application.Log.Add(logEntry)
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

#End Region



End Class


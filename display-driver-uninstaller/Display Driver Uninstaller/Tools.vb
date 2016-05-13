Imports System.IO
Imports System.Text
Imports System.Collections.ObjectModel
Imports System.Security.Cryptography
Imports System.Runtime.InteropServices
Imports System.ComponentModel
Imports System.Reflection
Imports System.Security.Principal

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

    Public Function PreferredUILanguages() As String
        Dim regkey As Microsoft.Win32.RegistryKey
        regkey = My.Computer.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", False)
        If regkey IsNot Nothing Then
            Dim wantedvalue As String() = CType(regkey.GetValue("PreferredUILanguages"), String())
            If wantedvalue IsNot Nothing Then
                If StrContainsAny(wantedvalue(0), True, "zh-tw") Then
                    Return "zh"  'Chinese Traditional
                ElseIf StrContainsAny(wantedvalue(0), True, "zh-cn") Then
                    Return "zh2"     'Chinese Simplified
                End If
                wantedvalue(0) = wantedvalue(0).Substring(0, If(wantedvalue(0).Length >= 2, 2, wantedvalue(0).Length))
                Return wantedvalue(0) 'Mutistring here, but only need the first value.
            Else
                Return Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName    'Return en, fr, sv etc. if preferedUILanguages is null (usually old OS)
            End If
        End If

        Return "en"   'Return en (English) by default if nothing found.
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
                    If text.IndexOf(s, comparison) <> -1 Then   ' -1 = NOT FOUND
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
        If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
            wildCard = "*"
        End If

        Return WinAPI.GetFileNames(directory, wildCard, searchSubDirs)
    End Function

    Public Function GetDirectories(ByVal directory As String, Optional ByVal wildCard As String = "*", Optional ByVal searchSubDirs As Boolean = False) As List(Of String)
        If IsNullOrWhitespace(wildCard) Or Not wildCard.Contains("*") Then
            wildCard = "*"
        End If

        Return WinAPI.GetDirNames(directory, wildCard, searchSubDirs)
    End Function

    Public Function GetOemInfList(ByVal directory As String, Optional ByVal wildCard As String = "oem*.inf", Optional ByVal searchSubDirs As Boolean = False) As List(Of OemINF)
        Dim oemInfList As New List(Of OemINF)
        Dim oemInf As OemINF

        For Each inf As String In GetFiles(directory, wildCard, searchSubDirs)
            oemInf = New OemINF(inf)

            Try
                Using infFile As SetupAPI.InfFile = New SetupAPI.InfFile(inf)
                    If infFile.Open() = 0UI Then
                        Dim lineClass As SetupAPI.InfLine = infFile.FindFirstKey("Version", "Class")
                        Dim lineProvider As SetupAPI.InfLine = infFile.FindFirstKey("Version", "Provider")

                        oemInf.Class = If(lineClass IsNot Nothing, lineClass.GetString(1), String.Empty)
                        oemInf.Provider = If(lineProvider IsNot Nothing, lineProvider.GetString(1), String.Empty)

                        If Not IsNullOrWhitespace(oemInf.Provider) Or Not IsNullOrWhitespace(oemInf.Class) Then
                            oemInf.IsValid = True
                        End If
                    Else
                        oemInf.IsValid = False

                        Dim logEntry As LogEntry = Application.Log.CreateEntry()

                        logEntry.Type = LogType.Warning
                        logEntry.Message = "Invalid inf file!"
                        logEntry.Add("infFile", inf)
                        logEntry.Add("Win32_ErrorCode", infFile.LastError.ToString())
                        logEntry.Add("Win32_Message", infFile.LastMessage)

                        Application.Log.Add(logEntry)
                    End If
                End Using
            Catch ex As Exception
                oemInf.IsValid = False
                Application.Log.AddException(ex)
            End Try

            oemInfList.Add(oemInf)
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


    <ComVisible(False)>
    Private Class WinAPI
        Private Shared _is64 As Boolean
        Private Shared _isAdmin As Boolean
        Private Shared ReadOnly INVALID_HANDLE_VALUE As IntPtr = New IntPtr(-1)

        Public Shared Property Is64 As Boolean
            Get
                Return _is64
            End Get
            Private Set(value As Boolean)
                _is64 = value
            End Set
        End Property

        Public Shared Property IsAdmin As Boolean
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
            SCS_32BIT_BINARY = 0

            <Description("A 64-bit Windows-based application.")>
            SCS_64BIT_BINARY = 6

            <Description("An MS-DOS – based application")>
            SCS_DOS_BINARY = 1

            <Description("A 16-bit OS/2-based application")>
            SCS_OS216_BINARY = 5

            <Description("A PIF file that executes an MS-DOS – based application")>
            SCS_PIF_BINARY = 3

            <Description("A POSIX – based application")>
            SCS_POSIX_BINARY = 4

            <Description("A 16-bit Windows-based application")>
            SCS_WOW_BINARY = 2
        End Enum

#End Region

#Region "Structures"

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Structure WIN32_FIND_DATA
            Public dwFileAttributes As UInt32
            Public ftCreationTime As System.Runtime.InteropServices.ComTypes.FILETIME
            Public ftLastAccessTime As System.Runtime.InteropServices.ComTypes.FILETIME
            Public ftLastWriteTime As System.Runtime.InteropServices.ComTypes.FILETIME
            Public nFileSizeHigh As UInt32
            Public nFileSizeLow As UInt32
            Public dwReserved0 As UInt32
            Public dwReserved1 As UInt32
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)> Public cFileName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=14)> Public cAlternateFileName As String
        End Structure

#End Region

#Region "P/Invokes"

        <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
        Private Shared Function GetBinaryType(
        <[In](), MarshalAs(UnmanagedType.LPStr)> ByVal lpApplicationName As String,
        <[Out]()> ByRef lpBinaryType As BinaryType) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Shared Function FindFirstFile(lpFileName As String, ByRef lpFindFileData As WIN32_FIND_DATA) As IntPtr
        End Function

        <DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Private Shared Function FindNextFile(hFindFile As IntPtr, ByRef lpFindFileData As WIN32_FIND_DATA) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function FindClose(ByVal hFindFile As IntPtr) As Boolean
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

                    If findHandle <> INVALID_HANDLE_VALUE Then
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
                If findHandle <> INVALID_HANDLE_VALUE Then
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

                    If findHandle <> INVALID_HANDLE_VALUE Then
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
                If findHandle <> INVALID_HANDLE_VALUE Then
                    FindClose(findHandle)
                End If
            End Try

            Return dirNames
        End Function

#End Region

    End Class
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

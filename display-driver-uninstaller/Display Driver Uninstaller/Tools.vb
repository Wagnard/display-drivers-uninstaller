Imports System.IO
Imports System.Text
Imports System.Security.Cryptography
Imports Display_Driver_Uninstaller.Win32

Public Module Tools
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
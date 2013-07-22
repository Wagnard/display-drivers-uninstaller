Module Module1
    Public userpth As String = System.Environment.GetEnvironmentVariable("userprofile")
    Dim time As String = DateAndTime.Now
    Public location As String = Application.StartupPath & "\Logs\" & DateAndTime.Now.Year & " _" & DateAndTime.Now.Month & "_" & DateAndTime.Now.Day & "_" & DateAndTime.Now.Hour & "_" & DateAndTime.Now.Minute & "_" & DateAndTime.Now.Second & "_DDULog.log"
    Dim sysdrv As String = System.Environment.GetEnvironmentVariable("systemdrive")
    Public wlog As New IO.StreamWriter(location, False)

    Public Sub log(ByVal value As String)
        If Form1.CheckBox2.Checked = True Then
            wlog.WriteLine(DateTime.Now & " >> " & value)
            wlog.Flush()
        Else

        End If
    End Sub
    'It may be possible to clean up the time code to be a simple PM/AM system, by getting system time.
End Module

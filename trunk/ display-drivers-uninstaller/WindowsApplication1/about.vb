Imports System.IO
Imports System.Text

Public Class about

    Private Sub about_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Dim oFS() As String
            oFS = IO.File.ReadAllLines(Application.StartupPath & "\settings\Languages\" & Form1.ComboBox2.Text & "\about.txt") '// add each line as String Array.
            Label1.Text = ""
            For i As Integer = 0 To oFS.Length - 1
                If i <> 0 Then
                    Label1.Text = Label1.Text & vbNewLine
                End If
                Label1.Text = Label1.Text & oFS(i)
            Next
        Catch ex As Exception
        End Try
    End Sub
End Class
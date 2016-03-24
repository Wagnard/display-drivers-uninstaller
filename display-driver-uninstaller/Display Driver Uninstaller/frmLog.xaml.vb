Public Class frmLog

	Private Sub Close_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
		Me.Close()
	End Sub

	Private Sub frmLog_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		lbLog.Items.Refresh()
		Languages.TranslateForm(Me)
	End Sub

	Private Sub Button1_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button1.Click
		For Each log As LogEntry In Application.Log.LogEntries
			log.AddException(New UnauthorizedAccessException("WHY???"))
			log.Message = "DID YOU JUST CHANGED ALL TO ERRORS!??"
			log.Type = LogType.Error
		Next
	End Sub

	Private threads As Int32 = 0
	Private Shared start As Boolean = False

	Private Sub Button2_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button2.Click
		Application.Log.AddMessage("LAUNCHING 100 THREADS!! WAIT for exit")

		start = False

		For i As Int32 = 1 To 100
			Dim t As New System.Threading.Thread(AddressOf Thread)
			t.Name = "logAdderThread"

			t.IsBackground = True

			t.CurrentCulture = Globalization.CultureInfo.CurrentCulture
			t.CurrentUICulture = Globalization.CultureInfo.CurrentUICulture

			t.Start()
			System.Threading.Interlocked.Increment(threads)

		Next
	
		Dim o As LogEntry = LogEntry.Create()
		o.Message = "logAdderThread Started!"
		o.Type = LogType.Warning
		o.Add("Runs 10seconds! There is ending message.")
		o.Add("When 'Items=x' stop updating = End")

		Application.Log.Add(o)

		start = True

	End Sub

	Private Sub Thread()
		While Not start
			System.Threading.Thread.Sleep(100)
		End While

		Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew

		While (True)
			Dim rnd As New Random
			Dim x As Int32 = rnd.Next(0, 5)

			Try
				Select Case x
					Case 0
						Dim l As LogEntry = Application.Log.CreateEntry()
						With l
							.Type = LogType.Warning
							.Message = "No access to folder ..."
						End With

						l.Add("Folder xxxxx")
						l.Add("UnauthorizedAccessException.")
						Application.Log.Add(l)
					Case 1
						Dim r As Integer
						Dim i As Integer = Math.DivRem(1, 0, r)
					Case 2
						Dim int As Integer = CType("wth", Int32)
					Case 3
						Dim int As Integer = Int32.MaxValue
						int += 500
					Case Else
						Dim l As LogEntry = LogEntry.Create()
						With l
							.Type = LogType.Event
							.Message = "Uninstalling Nvidia driver ..."
							.Add("some text")
							.Add("second line? yes?")
							.Add("Should be third...")
						End With

						Application.Log.Add(l)
				End Select
			Catch ex As Exception
				Dim log As LogEntry = LogEntry.Create
				log.Add("@Sub; Thread()")
				log.Add("x", x.ToString())
				log.AddException(ex)

				Application.Log.Add(log)
			End Try

			System.Threading.Thread.Sleep(rnd.Next(100, 1000))

			If sw.ElapsedMilliseconds > 10000 Then
				Exit While
			End If
		End While

		If System.Threading.Interlocked.Decrement(threads) <= 0 Then
			Application.Log.AddMessage("logAdderThread Completed!")
		End If
	End Sub

	Private Sub Button3_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button3.Click
		Application.Log.Clear()
		GC.Collect()
	End Sub

	Private Sub Button4_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button4.Click

		Using sfd As System.Windows.Forms.SaveFileDialog = New System.Windows.Forms.SaveFileDialog
			sfd.Filter = "DDU Log (*.xml)|*.xml"
			sfd.FilterIndex = 0
			sfd.AddExtension = True

			If sfd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Application.Log.SaveToFile(sfd.FileName)
			End If
		End Using
	End Sub

	Private Sub Button5_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button5.Click
		Using ofd As System.Windows.Forms.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
			ofd.Filter = "DDU Log (*.xml)|*.xml"
			ofd.FilterIndex = 0

			If ofd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Application.Log.OpenFromFile(ofd.FileName)
			End If
		End Using
	End Sub

	Private Sub Button6_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button6.Click
		Using sfd As System.Windows.Forms.SaveFileDialog = New System.Windows.Forms.SaveFileDialog
			sfd.Filter = "DDU settings (*.xml)|*.xml"
			sfd.FilterIndex = 0
			sfd.AddExtension = True

			If sfd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Application.Settings.Save(sfd.FileName)
			End If
		End Using
	End Sub

	Private Sub Button7_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button7.Click
		Using ofd As System.Windows.Forms.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
			ofd.Filter = "DDU settings (*.xml)|*.xml"
			ofd.FilterIndex = 0

			If ofd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Application.Settings.Load(ofd.FileName)
			End If
		End Using
	End Sub
End Class

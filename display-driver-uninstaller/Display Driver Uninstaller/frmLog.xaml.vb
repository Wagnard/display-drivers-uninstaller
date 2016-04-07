Imports System.ComponentModel

Public Class frmLog
	Implements INotifyPropertyChanged

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private _lastHeights() As GridLength = Nothing
	Private _selectedEntry As LogEntry = Nothing
	Public Property SelectedEntry As LogEntry
		Get
			Return _selectedEntry
		End Get
		Set(value As LogEntry)
			value.IsSelected = True
			_selectedEntry = value
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("SelectedEntry"))

			UpdateSelection()
		End Set
	End Property


	Private Sub Close_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
		Me.Close()
	End Sub

	Private Sub frmLog_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		lbLog.Items.Refresh()
		Languages.TranslateForm(Me)

		If lbLog.Items.Count > 0 Then
			SelectedEntry = DirectCast(lbLog.Items(0), LogEntry)
		End If
	End Sub

	Private Sub btnLoadLog_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnLoadLog.Click
		Using ofd As System.Windows.Forms.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
			ofd.Filter = "DDU Log (*.xml)|*.xml"
			ofd.FilterIndex = 0

			If ofd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
				Dim newLog As New AppLog
				newLog.OpenFromFile(ofd.FileName)

				Dim newLogWindow As New frmLog With
				 {
				   .Title = ofd.FileName,
				   .Owner = Me,
				   .ShowInTaskbar = False,
				   .Width = Me.ActualWidth,
				   .Height = Me.ActualHeight,
				   .WindowState = Me.WindowState,
				   .WindowStartupLocation = Windows.WindowStartupLocation.CenterOwner
				  }

				newLogWindow.btnLoadLog.Visibility = Windows.Visibility.Collapsed
				newLogWindow.lbLog.DataContext = newLog.LogEntries
				newLogWindow.tbOpenedLog.Visibility = Windows.Visibility.Visible
				newLogWindow.tbOpenedLog.Text = ofd.FileName

				Me.Visibility = Windows.Visibility.Hidden

				newLogWindow.ShowDialog()

				Me.WindowState = newLogWindow.WindowState
				Me.Width = newLogWindow.ActualWidth
				Me.Height = newLogWindow.ActualHeight
				Me.Top = newLogWindow.Top
				Me.Left = newLogWindow.Left

				Me.Visibility = Windows.Visibility.Visible


				newLog.Clear()
				newLog = Nothing
			End If
		End Using
	End Sub

	Public Sub New()

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.

		EventManager.RegisterClassHandler(GetType(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, New RoutedEventHandler(AddressOf ListBoxItem_MouseLeftButtonDown))
	End Sub

	Private Sub ListBoxItem_MouseLeftButtonDown(sender As System.Object, e As System.Windows.RoutedEventArgs)
		Dim lvi As ListBoxItem = TryCast(sender, ListBoxItem)
		Dim logEntry As LogEntry = TryCast(lvi.Content, LogEntry)

		If logEntry IsNot Nothing Then
			If SelectedEntry IsNot Nothing Then
				If Not SelectedEntry.Equals(logEntry) Then
					SelectedEntry.IsSelected = False
				End If
			End If

			SelectedEntry = logEntry
		End If
	End Sub

	Private Sub UpdateSelection()
		If SelectedEntry.HasException Then
			tabControl.SelectedIndex = 0
		ElseIf SelectedEntry.HasValues Then
			tabControl.SelectedIndex = 1
		Else
			tabControl.SelectedIndex = 2
		End If
	End Sub
End Class
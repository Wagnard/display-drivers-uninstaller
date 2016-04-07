Imports System.ComponentModel
Imports System.Reflection

Public Class frmLog
	Implements INotifyPropertyChanged

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private ReadOnly _nullLogEntry As LogEntry = LogEntry.Create()
	Private _selectedEntry As LogEntry = Nothing
	Private _listEvents As Boolean = True
	Private _listWarnings As Boolean = True
	Private _listErrors As Boolean = True

	Public Property SelectedEntry As LogEntry
		Get
			Return _selectedEntry
		End Get
		Set(value As LogEntry)
			If value IsNot Nothing Then
				value.IsSelected = True
			End If

			If _selectedEntry IsNot Nothing Then
				_selectedEntry.IsSelected = False
			End If
			_selectedEntry = value
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("SelectedEntry"))
			UpdateSelection()
		End Set
	End Property
	Public Property ListEvents As Boolean
		Get
			Return _listEvents
		End Get
		Set(value As Boolean)
			_listEvents = value
			FilterChanged()
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ListEvents"))
		End Set
	End Property
	Public Property ListWarnings As Boolean
		Get
			Return _listWarnings
		End Get
		Set(value As Boolean)
			_listWarnings = value
			FilterChanged()
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ListWarnings"))
		End Set
	End Property
	Public Property ListErrors As Boolean
		Get
			Return _listErrors
		End Get
		Set(value As Boolean)
			_listErrors = value
			FilterChanged()
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ListErrors"))
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

	Private Sub UpdateSelection()
		If SelectedEntry IsNot Nothing Then
			If SelectedEntry.HasException Then
				tabControl.SelectedIndex = 0
			ElseIf SelectedEntry.HasValues Then
				tabControl.SelectedIndex = 1
			Else
				tabControl.SelectedIndex = 2
			End If
		Else
			tabControl.SelectedIndex = 2
		End If
	End Sub

	Private Sub menuEvents_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
		ListEvents = (ListEvents = False)
	End Sub

	Private Sub menuWarnings_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
		ListWarnings = (ListWarnings = False)
	End Sub

	Private Sub menuErrors_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
		ListErrors = (ListErrors = False)
	End Sub

	Public Sub New()

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.

		EventManager.RegisterClassHandler(GetType(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, New RoutedEventHandler(AddressOf ListBoxItem_MouseLeftButtonDown))
	End Sub

	Private Sub FilterChanged()
		Dim view As ICollectionView = CollectionViewSource.GetDefaultView(lbLog.ItemsSource)

		view.Filter = New Predicate(Of Object)(AddressOf Filter)

		If lbLog.Items.Count <= 0 Then
			SelectedEntry = _nullLogEntry
		Else
			SelectEntry(DirectCast(lbLog.Items(0), LogEntry))
		End If
	End Sub

	Private Function Filter(ByVal obj As Object) As Boolean
		Dim logEntry As LogEntry = TryCast(obj, LogEntry)

		If logEntry Is Nothing Then
			Return False
		End If

		Select Case logEntry.Type
			Case LogType.Event
				If Not ListEvents Then
					logEntry.IsSelected = False
				End If
				Return ListEvents
			Case LogType.Warning
				If Not ListWarnings Then
					logEntry.IsSelected = False
				End If
				Return ListWarnings
			Case LogType.Error
				If Not ListErrors Then
					logEntry.IsSelected = False
				End If
				Return ListErrors
		End Select

		Return False
	End Function

	Private Sub ListBoxItem_MouseLeftButtonDown(sender As System.Object, e As System.Windows.RoutedEventArgs)
		Dim lvi As ListBoxItem = TryCast(sender, ListBoxItem)

		If lvi IsNot Nothing Then
			SelectEntry(TryCast(lvi.Content, LogEntry))
		End If
	End Sub

	Private Sub SelectEntry(ByRef logEntry As LogEntry)
		If logEntry IsNot Nothing Then
			If SelectedEntry IsNot Nothing Then
				If Not SelectedEntry.Equals(logEntry) Then
					SelectedEntry.IsSelected = False
					SelectedEntry = logEntry

					If lbLog.Items.Count > 0 Then
						Dim vsp As VirtualizingStackPanel =
						 TryCast(GetType(ItemsControl).InvokeMember("_itemsHost",
							   BindingFlags.Instance Or BindingFlags.GetField Or BindingFlags.NonPublic, Nothing, lbLog, Nothing), VirtualizingStackPanel)

						If vsp IsNot Nothing Then
							vsp.SetVerticalOffset(vsp.ScrollOwner.ScrollableHeight * 0.0 / lbLog.Items.Count)
						End If
					End If
				End If
			End If
		End If
	End Sub
End Class
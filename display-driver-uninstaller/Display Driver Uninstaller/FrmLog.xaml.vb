Imports System.ComponentModel
Imports System.Reflection

Namespace Display_Driver_Uninstaller

	Public Enum CopyOption
		CopyKey
		CopyValue
		CopyLine
	End Enum

	Public Class FrmLog
		Implements INotifyPropertyChanged

		Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

		Private ReadOnly _nullLogEntry As New LogEntry()
		Private _selectedEntry As LogEntry = Nothing
		Private _listEvents As Boolean = True
		Private _listWarnings As Boolean = True
		Private _listErrors As Boolean = True
		Private _enableCopy As Boolean = False

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
		Public Property EnableCopy As Boolean
			Get
				Return _enableCopy
			End Get
			Set(value As Boolean)
				_enableCopy = value

				If Not value Then
					lbValues.SelectedIndex = -1
					lbException.SelectedIndex = -1
				End If

				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("EnableCopy"))
			End Set
		End Property

		Private Sub Close_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
			Me.Close()
		End Sub

		Private Sub FrmLog_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
			lbLog.Items.Refresh()
			Languages.TranslateForm(Me)

			If lbLog.Items.Count > 0 Then
				SelectedEntry = DirectCast(lbLog.Items(0), LogEntry)
			End If
		End Sub

		Private Sub FrmLog_Closing(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
			If SelectedEntry IsNot Nothing Then
				SelectedEntry.IsSelected = False
			End If
		End Sub

		Private Sub BtnOpenLog_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnOpenLog.Click
			Using ofd As System.Windows.Forms.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
				ofd.Filter = "DDU Log (*.xml)|*.xml"
				ofd.FilterIndex = 0

				If ofd.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
					Dim newLog As New AppLog
					newLog.OpenFromFile(ofd.FileName)

					Dim newLogWindow As New FrmLog With
				 {
				   .Title = ofd.FileName,
				   .Owner = Me,
				   .ShowInTaskbar = False,
				   .Width = Me.ActualWidth,
				   .Height = Me.ActualHeight,
				   .WindowState = Me.WindowState,
				   .WindowStartupLocation = WindowStartupLocation.CenterOwner
				  }

					newLogWindow.btnOpenLog.Visibility = Visibility.Collapsed
					newLogWindow.lbLog.DataContext = newLog.LogEntries
					newLogWindow.tbOpenedLog.Visibility = Visibility.Visible
					newLogWindow.tbOpenedLog.Text = ofd.FileName

					Me.Visibility = Visibility.Hidden

					newLogWindow.ShowDialog()

					Me.WindowState = newLogWindow.WindowState
					Me.Width = newLogWindow.ActualWidth
					Me.Height = newLogWindow.ActualHeight
					Me.Top = newLogWindow.Top
					Me.Left = newLogWindow.Left

					Me.Visibility = Visibility.Visible


					newLog.Clear()
					newLog = Nothing

					If lbLog.Items.Count > 0 Then
						SelectEntry(DirectCast(lbLog.Items(0), LogEntry))
						UpdateScrollPosition(lbLog)
					Else
						SelectEntry(_nullLogEntry)
					End If
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

			UpdateScrollPosition(GetCurrentListBox())
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

			If lvi IsNot Nothing AndAlso TypeOf (lvi.Content) Is LogEntry Then
				SelectEntry(TryCast(lvi.Content, LogEntry))
			End If
		End Sub

		Private Sub SelectEntry(ByRef logEntry As LogEntry)
			If logEntry Is Nothing OrElse SelectedEntry Is Nothing Then
				Return
			End If

			If Not SelectedEntry.Equals(logEntry) Then
				SelectedEntry.IsSelected = False
				SelectedEntry = logEntry
			End If
		End Sub

		Private Sub CopyMessage()
			Dim logEntry As LogEntry
			Dim sb As New System.Text.StringBuilder(1000)

			For Each o As Object In lbLog.SelectedItems
				logEntry = DirectCast(o, LogEntry)

				If logEntry IsNot Nothing Then
					sb.AppendLine(logEntry.Message)
				End If
			Next

			Clipboard.SetText(sb.ToString())
		End Sub

		Private Sub CopyValues(ByVal copy As CopyOption)
			Dim kvp As KvP
			Dim sb As New System.Text.StringBuilder(1000)

			For Each o As Object In lbValues.SelectedItems
				kvp = TryCast(o, KvP)

				If kvp IsNot Nothing Then
					Select Case copy
						Case CopyOption.CopyKey
							If kvp.HasKey Then
								sb.AppendLine(kvp.Key)
							End If
						Case CopyOption.CopyValue
							If kvp.HasValue Then
								sb.AppendLine(kvp.Value)
							End If
						Case CopyOption.CopyLine
							If kvp.HasKey Then
								If kvp.HasValue Then
									sb.AppendFormat("{0}{1}{2}{3}", kvp.Key, SelectedEntry.Separator, kvp.Value, vbCrLf)
								Else : sb.AppendLine(kvp.Key)
								End If
							Else : sb.AppendLine(kvp.Value)
							End If
					End Select
				End If
			Next

			Clipboard.SetText(sb.ToString())
		End Sub

		Private Sub CopyException(ByVal copy As CopyOption)
			Dim kvp As KeyValuePair(Of String, String)
			Dim sb As New System.Text.StringBuilder(1000)

			For i As Int32 = 0 To lbException.SelectedItems.Count - 1
				kvp = DirectCast(lbException.SelectedItems(i), KeyValuePair(Of String, String))

				Select Case copy
					Case CopyOption.CopyKey
						sb.AppendLine(kvp.Key)
					Case CopyOption.CopyValue
						sb.AppendLine(kvp.Value)
					Case CopyOption.CopyLine
						sb.AppendFormat("{0}{1}{2}", kvp.Key, vbCrLf, kvp.Value)
				End Select

				If i < lbException.SelectedItems.Count - 1 Then
					sb.AppendLine(vbCrLf)
				End If
			Next

			Clipboard.SetText(sb.ToString())
		End Sub

		Private Sub CopyCommand(ByVal sender As Object, ByVal e As ExecutedRoutedEventArgs)
			If Not EnableCopy Then
				Return
			End If

			Dim lb As ListBox = TryCast(sender, ListBox)

			If lb Is Nothing Then
				Return
			End If

			Dim copyOption As CopyOption = CType(e.Parameter, CopyOption)

			If lb Is lbValues Then
				CopyValues(copyOption)
			ElseIf lb Is lbException Then
				CopyException(copyOption)
			ElseIf lb Is lbLog Then
				CopyMessage()
			End If
		End Sub

		Private Sub TabControl_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles tabControl.SelectionChanged
			UpdateScrollPosition(GetCurrentListBox())
		End Sub

		Private Function GetCurrentListBox() As ListBox
			Dim page As TabItem = TryCast(tabControl.SelectedItem, TabItem)
			If page Is Nothing Then
				Return Nothing
			End If

			Return TryCast(page.Content, ListBox)
		End Function

		Private Sub UpdateScrollPosition(ByRef lb As ListBox)
			If lb Is Nothing Then
				Return
			End If

			If lb.Items.Count > 0 Then
				Dim vsp As VirtualizingStackPanel =
			 TryCast(GetType(ItemsControl).InvokeMember("_itemsHost",
			 BindingFlags.Instance Or BindingFlags.GetField Or BindingFlags.NonPublic, Nothing, lb, Nothing), VirtualizingStackPanel)

				If vsp IsNot Nothing Then
					vsp.SetVerticalOffset(vsp.ScrollOwner.ScrollableHeight * 0.0 / lb.Items.Count)
				End If
			End If
		End Sub

	End Class
End Namespace
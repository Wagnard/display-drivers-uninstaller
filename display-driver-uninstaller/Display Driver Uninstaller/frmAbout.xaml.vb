Imports System.ComponentModel
Imports System.Text

Public Class frmAbout
	Implements INotifyPropertyChanged

	Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged

	Private _text As String = Nothing
	Public Property Text As String
		Get
			Return _text
		End Get
		Set(value As String)
			If _text <> value Then
				_text = value
				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Text"))
			End If
		End Set
	End Property

	Private Sub frmAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
		Languages.TranslateForm(Me)
	End Sub

	Private Sub btnClose_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
		Me.Close()
	End Sub

	' Testing  (works)
	' For language credits
	'Private Sub btnTranslators_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnTranslators.Click
	'	Dim langOption As Languages.LanguageOption = DirectCast(Me.DataContext, Data).Settings.SelectedLanguage

	'	Dim sb As New StringBuilder()
	'	sb.AppendLine(String.Format("Language: {0}  ({1})", langOption.DisplayText, langOption.ISOLanguage))
	'	sb.AppendLine("Translators: (in random order)")
	'	sb.AppendLine()

	'	Dim rnd As New System.Random

	'	For Each credit As Languages.LanguageCredits In DirectCast(Me.DataContext, Data).Settings.SelectedLanguage.Credits
	'		sb.AppendLine("User: " & credit.User)

	'		If Not IsNullOrWhitespace(credit.Details) Then
	'			sb.AppendLine("Details: " & credit.Details)
	'		End If

	'		If credit.LastUpdate IsNot Nothing AndAlso credit.LastUpdate.HasValue Then
	'			sb.AppendLine("Last update: " & credit.LastUpdate.Value.ToString("dd MMMM yyyy"))
	'		End If

	'		sb.AppendLine("")
	'	Next

	'	MessageBox.Show(sb.ToString())
	'End Sub

End Class

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

End Class

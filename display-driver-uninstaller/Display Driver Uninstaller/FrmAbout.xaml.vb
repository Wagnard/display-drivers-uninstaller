Imports System.ComponentModel

Namespace Display_Driver_Uninstaller

	Public Class FrmAbout
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

		Private _frmType As Int32 = 0
		Public Property FrmType As Int32
			Get
				Return _frmType
			End Get
			Set(value As Int32)
				If _frmType <> value Then
					_frmType = value

					Select Case _frmType
						Case 1 : Text = Languages.GetTranslation("Misc", "About", "Text")
						Case 2 : Text = Languages.GetTranslation("Misc", "Tos", "Text")
						Case 3 : Text = Languages.GetTranslation("frmAbout", "lblTranslators", "Text")
						Case 4 : Text = Languages.GetTranslation("frmAbout", "lblPatron", "Text", True)
						Case Else : Text = Nothing
					End Select

					RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("FrmType"))
				End If
			End Set
		End Property

		Private Sub FrmAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
			Languages.TranslateForm(Me, False)
			lblVersion.Content = Application.Settings.AppVersion.ToString
			FlowDirection = Application.Settings.FlowControl
		End Sub

		Private Sub BtnClose_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnClose.Click
			Me.Close()
		End Sub

		Private Sub Btn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnAbout.Click, btnTos.Click, btnTranslators.Click, btnPatron.Click
			Dim btn As Button = TryCast(sender, Button)

			If btn Is Nothing Then
				Return
			End If

			Select Case True
				Case StrContainsAny(btn.Name, True, "btnAbout") : FrmType = 1
				Case StrContainsAny(btn.Name, True, "btnTos") : FrmType = 2
				Case StrContainsAny(btn.Name, True, "btnTranslators") : FrmType = 3
				Case StrContainsAny(btn.Name, True, "btnPatron") : FrmType = 4
			End Select
		End Sub

	End Class
End Namespace
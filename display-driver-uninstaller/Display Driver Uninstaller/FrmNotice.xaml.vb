Namespace Display_Driver_Uninstaller
    Public Class FrmNotice

        Public Sub New()
            SeedThemeResources()
            InitializeComponent()
        End Sub

        Private Sub SeedThemeResources()
            If Application.UseDarkThemeSession Then
                Me.Background = New SolidColorBrush(Color.FromRgb(&H12, &H16, &H1D))
                Me.Foreground = New SolidColorBrush(Color.FromRgb(&HF4, &HF7, &HFA))
                Resources("NoticeTextBrush") = New SolidColorBrush(Color.FromRgb(&HF4, &HF7, &HFA))
                Resources("NoticeSurfaceBrush") = New SolidColorBrush(Color.FromRgb(&H12, &H16, &H1D))
                Resources("NoticePanelBrush") = New SolidColorBrush(Color.FromRgb(&H18, &H1E, &H27))
                Resources("NoticePanelHoverBrush") = New SolidColorBrush(Color.FromRgb(&H20, &H28, &H34))
                Resources("NoticeBorderBrush") = New SolidColorBrush(Color.FromRgb(&H54, &H61, &H73))
                Resources("NoticeAccentBrush") = New SolidColorBrush(Color.FromRgb(&H58, &HA6, &HFF))
            Else
                Me.Background = New SolidColorBrush(Colors.White)
                Me.Foreground = New SolidColorBrush(Colors.Black)
                Resources("NoticeTextBrush") = New SolidColorBrush(Colors.Black)
                Resources("NoticeSurfaceBrush") = New SolidColorBrush(Colors.White)
                Resources("NoticePanelBrush") = New SolidColorBrush(Color.FromRgb(&HF0, &HF0, &HF0))
                Resources("NoticePanelHoverBrush") = New SolidColorBrush(Color.FromRgb(&HE5, &HE5, &HE5))
                Resources("NoticeBorderBrush") = New SolidColorBrush(Colors.Black)
                Resources("NoticeAccentBrush") = New SolidColorBrush(Color.FromRgb(&H0, &H78, &HD4))
            End If
        End Sub

        Public Shared Function ShowNotice(owner As Window, title As String, message As String, Optional buttons As MessageBoxButton = MessageBoxButton.OK) As MessageBoxResult
            Dim notice As New FrmNotice With {
                .Title = If(String.IsNullOrWhiteSpace(title), Application.Settings.AppName, title),
                .Icon = If(owner IsNot Nothing, owner.Icon, Nothing),
                .MessageText = message
            }

            If owner IsNot Nothing AndAlso
               Not ReferenceEquals(owner, notice) AndAlso
               owner.IsLoaded AndAlso
               owner.IsVisible Then
                notice.Owner = owner
            End If

            notice.ConfigureButtons(buttons)
            System.Media.SystemSounds.Exclamation.Play()
            notice.ShowDialog()
            Return notice.Result
        End Function

        Public Property MessageText As String
            Get
                Return txtMessage.Text
            End Get
            Set(value As String)
                txtMessage.Text = value
            End Set
        End Property

        Public Property Result As MessageBoxResult = MessageBoxResult.None

        Private Sub ConfigureButtons(buttons As MessageBoxButton)
            btnOk.Visibility = Visibility.Collapsed
            btnYes.Visibility = Visibility.Collapsed
            btnNo.Visibility = Visibility.Collapsed
            btnCancel.Visibility = Visibility.Collapsed

            btnOk.IsDefault = False
            btnOk.IsCancel = False
            btnYes.IsDefault = False
            btnNo.IsCancel = False
            btnCancel.IsCancel = False

            Select Case buttons
                Case MessageBoxButton.OK
                    btnOk.Visibility = Visibility.Visible
                    btnOk.IsDefault = True
                    btnOk.IsCancel = True
                Case MessageBoxButton.YesNo
                    btnYes.Visibility = Visibility.Visible
                    btnNo.Visibility = Visibility.Visible
                    btnYes.IsDefault = True
                    btnNo.IsCancel = True
                Case MessageBoxButton.YesNoCancel
                    btnYes.Visibility = Visibility.Visible
                    btnNo.Visibility = Visibility.Visible
                    btnCancel.Visibility = Visibility.Visible
                    btnYes.IsDefault = True
                    btnCancel.IsCancel = True
            End Select
        End Sub

        Private Sub BtnOk_Click(sender As Object, e As RoutedEventArgs) Handles btnOk.Click
            Result = MessageBoxResult.OK
            DialogResult = True
            Close()
        End Sub

        Private Sub BtnYes_Click(sender As Object, e As RoutedEventArgs) Handles btnYes.Click
            Result = MessageBoxResult.Yes
            DialogResult = True
            Close()
        End Sub

        Private Sub BtnNo_Click(sender As Object, e As RoutedEventArgs) Handles btnNo.Click
            Result = MessageBoxResult.No
            DialogResult = False
            Close()
        End Sub

        Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs) Handles btnCancel.Click
            Result = MessageBoxResult.Cancel
            Close()
        End Sub

        Private Sub FrmNotice_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
            If Result = MessageBoxResult.None Then
                Result = If(btnOk.Visibility = Visibility.Visible, MessageBoxResult.OK, MessageBoxResult.Cancel)
            End If
        End Sub
    End Class
End Namespace
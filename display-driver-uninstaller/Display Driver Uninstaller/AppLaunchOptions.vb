'These values are not saved to Settings.xml (if need to save/load, use AppSettings)
' Add all commandline args you need or want here (with default setting)

Public Class AppLaunchOptions
	Public Property Arguments As String = Nothing
	Public Property ArgumentsArray As String() = Nothing

	' Remember to update HasCleanArg and LoadArgs() !
	Public Property Silent As Boolean = False
	Public Property Shutdown As Boolean = False
	Public Property Restart As Boolean = False
	Public Property NoSafeModeMsg As Boolean = False

	Public Property CleanNvidia As Boolean = False
	Public Property CleanAmd As Boolean = False
	Public Property CleanIntel As Boolean = False

	'All arguments which result launching separate cleaning thread!
	Public ReadOnly Property HasCleanArg As Boolean
		Get
			If CleanNvidia OrElse
			 CleanAmd OrElse
			 CleanIntel Then

				Return True
			End If

			Return False
		End Get
	End Property



	' Remember to update HasLinkArg and LoadArgs() !
	Public Property VisitDonate As Boolean = False
	Public Property VisitSVN As Boolean = False
	Public Property VisitGuru3DNvidia As Boolean = False
	Public Property VisitGuru3DAMD As Boolean = False
	Public Property VisitDDUHome As Boolean = False
	Public Property VisitGeforce As Boolean = False
	Public Property VisitOffer As Boolean = False

	Public ReadOnly Property HasLinkArg As Boolean
		Get
			If VisitDonate OrElse
			 VisitSVN OrElse
			 VisitGuru3DNvidia OrElse
			 VisitGuru3DAMD OrElse
			 VisitDDUHome OrElse
			 VisitGeforce OrElse
			 VisitOffer Then

				Return True
			End If

			Return False
		End Get
	End Property

	Public Sub LoadArgs(ByVal args() As String)
		If args IsNot Nothing AndAlso args.Length > 0 Then
			ArgumentsArray = args
			Arguments = String.Join(" ", args)

			For Each Argument As String In args
				Select Case True
					Case StrContainsAny(Argument, True, "-5648674614687") : Application.IsDebug = True
					Case StrContainsAny(Argument, True, "-donate") : VisitDonate = True
					Case StrContainsAny(Argument, True, "-svn") : VisitSVN = True
					Case StrContainsAny(Argument, True, "-guru3dnvidia") : VisitGuru3DNvidia = True
					Case StrContainsAny(Argument, True, "-guru3damd") : VisitGuru3DAMD = True
					Case StrContainsAny(Argument, True, "-dduhome") : VisitDDUHome = True
					Case StrContainsAny(Argument, True, "-geforce") : VisitGeforce = True
					Case StrContainsAny(Argument, True, "-visitoffer") : VisitOffer = True


					Case StrContainsAny(Argument, True, "-Silent") : Silent = True
					Case StrContainsAny(Argument, True, "-Shutdown") : Shutdown = True
					Case StrContainsAny(Argument, True, "-Restart") : Restart = True
					Case StrContainsAny(Argument, True, "-NoSafeModeMsg") : NoSafeModeMsg = True

					Case StrContainsAny(Argument, True, "-CleanNvidia") : CleanNvidia = True
					Case StrContainsAny(Argument, True, "-CleanAmd") : CleanAmd = True
					Case StrContainsAny(Argument, True, "-CleanIntel") : CleanIntel = True
				End Select
			Next
		Else
			Arguments = String.Empty
		End If
	End Sub

End Class

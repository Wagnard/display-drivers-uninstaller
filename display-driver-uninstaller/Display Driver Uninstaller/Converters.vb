Imports Display_Driver_Uninstaller.Win32

Namespace Converters

	Public Class NullableBooleanToBoolean
		Implements IValueConverter

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			Dim val As Boolean = False

			If TypeOf (value) Is Boolean AndAlso CType(value, Boolean?).HasValue Then
				val = CType(value, Boolean?).Value
			End If

			Return val
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Dim val As Boolean? = False

			If TypeOf (value) Is Boolean Then
				val = CBool(value)
			End If

			Return val
		End Function
	End Class

	Public Class LogTypeToBrush
		Implements IValueConverter

		Public Property Brush1 As Brush = New SolidColorBrush(Colors.Black)
		Public Property Brush2 As Brush = New SolidColorBrush(Colors.Black)
		Public Property Brush3 As Brush = New SolidColorBrush(Colors.Black)

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is LogType Then
				Dim type As LogType = DirectCast(value, LogType)

				Select Case type
					Case LogType.Event
						Return Brush1
					Case LogType.Warning
						Return Brush2
					Case LogType.Error
						Return Brush3
				End Select

			End If

			Return Brush1
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("LogTypeToBrush::ConvertBack")
		End Function
	End Class

	Public Class LogTypeIsType
		Implements IValueConverter

		Public Property Reversed As Boolean = False

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is LogType AndAlso TypeOf (parameter) Is String Then
				Dim type As LogType = DirectCast([Enum].Parse(GetType(LogType), CStr(parameter), True), LogType)

				Dim result As Boolean = DirectCast(value, LogType).Equals(type)

				Return If(Reversed, result = False, result = True)
			End If

			Return If(Reversed, True, False)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("LogTypeIsType::ConvertBack")
		End Function
	End Class

	Public Class ValueEquals
		Implements IValueConverter

		Public Property Reversed As Boolean = False

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If value IsNot Nothing AndAlso parameter IsNot Nothing AndAlso value.GetType() Is parameter.GetType() Then
				Return If(Reversed, Not value.Equals(parameter), value.Equals(parameter))
			End If

			Return If(Reversed, True, False)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("ValueEquals::ConvertBack")
		End Function
	End Class

	Public Class ValueEqualsOrGreater
		Implements IValueConverter

		Public Property Reversed As Boolean = False

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			Try
				If value IsNot Nothing AndAlso parameter IsNot Nothing AndAlso value.GetType() Is parameter.GetType() AndAlso TypeOf (value) Is [Enum] Then
					Dim v As Int32 = System.Convert.ToInt32(DirectCast(value, [Enum]))
					Dim param As Int32 = System.Convert.ToInt32(DirectCast(parameter, [Enum]))

					Return If(Reversed, v < param, v >= param)
				End If

				Return If(Reversed, True, False)
			Catch ex As Exception
				Return False
			End Try
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("ValueEqualsOrGreater::ConvertBack")
		End Function
	End Class

	Public Class LogTypeToColor
		Implements IValueConverter

		Public Property Color1 As Color = Colors.Black
		Public Property Color2 As Color = Colors.Black
		Public Property Color3 As Color = Colors.Black

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is LogType Then
				Dim type As LogType = DirectCast(value, LogType)

				Select Case type
					Case LogType.Event
						Return Color1
					Case LogType.Warning
						Return Color2
					Case LogType.Error
						Return Color3
				End Select

			End If

			Return Color1
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("LogTypeToColor::ConvertBack")
		End Function
	End Class

	Public Class StringIsNotNullOrEmpty
		Implements IValueConverter

		Public Property Reversed As Boolean = False

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is String Then
				Return If(Reversed, String.IsNullOrEmpty(DirectCast(value, String)), String.IsNullOrEmpty(DirectCast(value, String)) = False)
			End If

			Return If(Reversed, True, False)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("StringIsNotNullOrEmpty::ConvertBack")
		End Function
	End Class

	Public Class CountIsNotZero
		Implements IValueConverter

		Public Property Reversed As Boolean = False
		Public Property TargetValue As Int32 = 0

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is Int32 Then
				If parameter Is Nothing OrElse Not TypeOf (parameter) Is String OrElse Not Int32.TryParse(DirectCast(parameter, String), TargetValue) Then
					TargetValue = 0
				End If

				Return If(Reversed, DirectCast(value, Int32) = TargetValue, DirectCast(value, Int32) <> TargetValue)
			End If

			Return If(Reversed, True, False)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("CountIsZero::ConvertBack")
		End Function
	End Class

	Public Class IsNotNullConverter
		Implements IValueConverter

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			Return (value IsNot Nothing)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("IsNotNullConverter::ConvertBack")
		End Function
	End Class

	Public Class IsNullConverter
		Implements IValueConverter

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If value IsNot Nothing Then
				If TypeOf (value) Is List(Of SetupAPI.DriverInfo) Then
					Return DirectCast(value, List(Of SetupAPI.DriverInfo)).Count = 0
				ElseIf TypeOf (value) Is List(Of SetupAPI.Device) Then
					Return DirectCast(value, List(Of SetupAPI.Device)).Count = 0
				ElseIf TypeOf (value) Is SetupAPI.Device() Then
					Return DirectCast(value, SetupAPI.Device()).Length = 0
				ElseIf TypeOf (value) Is SetupAPI.DriverInfo() Then
					Return DirectCast(value, SetupAPI.DriverInfo()).Length = 0
				End If
			End If

			Return True
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("IsNullConverter::ConvertBack")
		End Function
	End Class


	Public Class EnumToTextConverter
		Implements IValueConverter

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is UpdateStatus Then
				If [Enum].IsDefined(GetType(UpdateStatus), value) Then
					Dim status As UpdateStatus = DirectCast(value, UpdateStatus)

					Return Languages.GetTranslation("frmMain", "lblUpdate", String.Format("Text{0}", If(status = UpdateStatus.NotChecked, String.Empty, CInt(status).ToString())))
				End If
			End If

			Return String.Empty
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("IsNotNullConverter::ConvertBack")
		End Function
	End Class

	Public Class BooleanToVisibilityConverter
		Inherits BooleanConverter(Of Visibility)
	End Class

	Public Class BooleanToColor
		Inherits BooleanConverter(Of Color)
		Implements IValueConverter

	End Class

	Public Class BooleanToStyle
		Inherits BooleanConverter(Of Style)
		Implements IValueConverter
	End Class

	Public Class BooleanToFontWeight
		Inherits BooleanConverter(Of FontWeight)
		Implements IValueConverter
	End Class

	Public Class BooleanToString
		Inherits BooleanConverter(Of String)
		Implements IValueConverter
	End Class

	Public Class BooleanConverter(Of T)
		Implements IValueConverter

		Public Property TrueValue As T
		Public Property FalseValue As T
		Public Property Reversed As Boolean

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			If TypeOf (value) Is Boolean Then
				Return If(Reversed, If(DirectCast(value, Boolean), FalseValue, TrueValue), If(DirectCast(value, Boolean), TrueValue, FalseValue))
			End If

			Return If(Reversed, TrueValue, FalseValue)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Return If(TypeOf (value) Is T, value.Equals(TrueValue), False)
		End Function
	End Class

	Public Class CombiningConverter
		Implements IValueConverter

		Public Property Converter1 As IValueConverter
		Public Property Converter2 As IValueConverter

		Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
			Dim convertedValue As Object = Converter1.Convert(value, targetType, parameter, culture)
			Return Converter2.Convert(convertedValue, targetType, parameter, culture)
		End Function

		Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
			Throw New NotImplementedException("CombiningConverter::ConvertBack")
		End Function
	End Class

End Namespace

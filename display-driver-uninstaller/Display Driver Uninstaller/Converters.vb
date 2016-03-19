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
End Namespace

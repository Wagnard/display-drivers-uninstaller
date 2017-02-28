Imports System.Globalization

Namespace Win32.TaskScheduler

	Friend Class SYSTEMTIME
		Public wYear As UInt16
		Public wMonth As UInt16
		Public wDayOfWeek As UInt16
		Public wDay As UInt16
		Public wHour As UInt16
		Public wMinute As UInt16
		Public wSecond As UInt16
		Public wMilliseconds As UInt16

		Public Overrides Function ToString() As String
			Throw New NotImplementedException("WIP: SYSTEMTIME")
		End Function

	End Class

End Namespace

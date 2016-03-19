Class Application
	' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
	' can be handled in this file.

	Private Shared m_settings As AppSettings
	Private Shared m_paths As AppPaths

	Public Shared ReadOnly Property Settings As AppSettings
		Get
			Return m_settings
		End Get
	End Property
	Public Shared ReadOnly Property Paths As AppPaths
		Get
			Return m_paths
		End Get
	End Property

	Public Sub New()
		m_paths = New AppPaths
		m_settings = New AppSettings
	End Sub
End Class

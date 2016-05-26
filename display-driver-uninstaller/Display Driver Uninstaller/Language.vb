Imports System.Xml
Imports System.IO
Imports System.Reflection
Imports System.Text

Public Class Languages
	Private Shared ReadOnly sysNewLine As String = Environment.NewLine
	'Private Shared newLineStr As String = "|"
	Private Shared ReadOnly threadLock As String = "You shall not pass!" 'lock access for one thread at time

	Private Shared isEngLoaded As Boolean = False
	Private Shared isUserLoaded As Boolean = False
	Private Shared useTranslated As Boolean = True
	Private Shared currentLang As LanguageOption = Nothing

	Public Shared ReadOnly Property Current As LanguageOption
		Get
			SyncLock threadLock
				Return currentLang
			End SyncLock
		End Get
	End Property
	Public Shared ReadOnly Property DefaultEng As LanguageOption
		Get
			SyncLock threadLock
				If Not isEngLoaded Then LoadDefault()
				Return englishDictionary.Details
			End SyncLock
		End Get
	End Property

	Private Shared englishDictionary As TranslatedFile = Nothing
	Private Shared translatedDictionary As TranslatedFile = Nothing

	''' <param name="langOption">Which language to load for use. Use 'Nothing' for defaul (English)</param>
	Public Shared Sub Load(Optional ByVal langOption As LanguageOption = Nothing)
		SyncLock (threadLock)
			If langOption Is Nothing OrElse langOption.ISOLanguage.Equals("en-US", StringComparison.OrdinalIgnoreCase) Then
				If Not isEngLoaded Or englishDictionary Is Nothing Then
					isEngLoaded = ReadFile("en-US", False, englishDictionary)
				End If

				useTranslated = False
				currentLang = englishDictionary.Details
			Else
				If isUserLoaded And translatedDictionary IsNot Nothing Then
					translatedDictionary.Parents.Clear()
				End If

				isUserLoaded = ReadFile(langOption.Filename, False, translatedDictionary)
				useTranslated = True
				currentLang = translatedDictionary.Details
			End If
		End SyncLock
	End Sub

	Private Shared Sub LoadDefault()
		isEngLoaded = ReadFile("en-US", False, englishDictionary)
		useTranslated = False
		currentLang = englishDictionary.Details
	End Sub

	''' <param name="parent">Which form (Form1, fmrLaunch)</param>
	''' <param name="type">Name of propery (Me.Text)</param>
	''' <returns>Translated text. If language not found, return English text.</returns> 
	Public Shared Function GetParentTranslation(ByVal parent As String, ByVal type As String, Optional ByVal forceEnglish As Boolean = False) As String
		SyncLock (threadLock)
			If Not isEngLoaded And Not useTranslated Then
				LoadDefault()
			End If

			Dim tc As TranslatedControl = GetParent(If(forceEnglish, False, useTranslated), parent)
			Dim noTranslation As Boolean = False

			If tc Is Nothing Then
				If forceEnglish Or Not useTranslated Then
					Return Nothing
				End If
notFound:
				tc = GetParent(False, parent)
				noTranslation = True

				If tc Is Nothing Then
					Return Nothing
				End If
			End If

			If Not tc Is Nothing Then
				For Each kvp As KeyValuePair(Of String, String) In tc.Attributes
					If (kvp.Key.Equals(type, StringComparison.OrdinalIgnoreCase)) Then
						If Not String.IsNullOrEmpty(kvp.Value) Then
							Return kvp.Value.Trim(sysNewLine.ToCharArray())
						Else
							If noTranslation Then
								Return String.Empty
							Else
								GoTo notFound
							End If
						End If
					End If
				Next
			End If

			Return Nothing
		End SyncLock
	End Function

	''' <param name="parent">In which form control is located (Form1, fmrLaunch)</param>
	''' <param name="control">Name of control, eg Button1</param>
	''' <param name="type">What attribute to return. Text, Tooltip etc.</param>
	''' <returns>Translated text. If language not found, return English text</returns> 
	Public Shared Function GetTranslation(ByVal parent As String, ByVal control As String, ByVal type As String, Optional ByVal forceEnglish As Boolean = False) As String
		SyncLock (threadLock)
			If IsNullOrWhitespace(parent) OrElse IsNullOrWhitespace(control) OrElse IsNullOrWhitespace(type) Then
				Return Nothing
			End If

			If Not isEngLoaded And Not useTranslated Then
				LoadDefault()
			End If

			Dim tc As TranslatedControl = GetControl(If(forceEnglish, False, useTranslated), parent, control)
			Dim noTranslation As Boolean = False

			If tc Is Nothing Then
				If forceEnglish Or Not useTranslated Then
					Return Nothing
				End If
notFound:
				tc = GetControl(False, parent, control)
				noTranslation = True

				If tc Is Nothing Then
					Return Nothing
				End If
			End If

			For Each kvp As KeyValuePair(Of String, String) In tc.Values
				If (kvp.Key.Equals(type, StringComparison.OrdinalIgnoreCase)) Then
					If Not String.IsNullOrEmpty(kvp.Value) Then
						Return kvp.Value.Trim(sysNewLine.ToCharArray())
					Else
						Exit For
					End If
				End If
			Next

			If noTranslation Then
				Return Nothing
			Else
				GoTo notFound
			End If
		End SyncLock
	End Function

	''' <param name="parent">In which form control is located (Form1, fmrLaunch)</param>
	''' <param name="control">Name of control, eg Button1</param>
	''' <param name="beginsWith">Begins with text. Useful for getting array of values (eg. ComboBox/ListBox)</param>
	''' <returns>Translated text array. If language not found, return as English</returns> 
	Public Shared Function GetTranslationList(ByVal parent As String, ByVal control As String, ByVal beginsWith As String, Optional ByVal forceEnglish As Boolean = False) As List(Of String)
		SyncLock (threadLock)
			If Not isEngLoaded And Not useTranslated Then
				LoadDefault()
			End If

			Dim tc As TranslatedControl = GetControl(If(forceEnglish, False, useTranslated), parent, control)

			If tc Is Nothing Then
				If forceEnglish Or Not useTranslated Then
					Return Nothing
				End If

				tc = GetControl(False, parent, control)
			End If


			If Not tc Is Nothing Then
				Dim items As New List(Of String)

				For Each kvp As KeyValuePair(Of String, String) In tc.Values
					If (kvp.Key.StartsWith(beginsWith, StringComparison.OrdinalIgnoreCase)) Then
						items.Add(kvp.Value.Replace(sysNewLine, String.Empty))
					End If
				Next
				Return items
			End If

			Return Nothing
		End SyncLock
	End Function

	''' <param name="window">Which window to translate (frmMain, frmLaunch etc)</param>
	Public Shared Sub TranslateForm(ByVal window As Window)
		SyncLock (threadLock)
			If Not isEngLoaded And Not useTranslated Then
				LoadDefault()
			End If

			Dim text As String = GetParentTranslation(window.Name, "Text")

			If Not String.IsNullOrEmpty(text) Then
				window.Title = text
			End If

			Dim controls As New List(Of DependencyObject)
			GetChildren(window, controls)

			For Each c As DependencyObject In controls
				TranslateControl(window.Name, c)
			Next
		End SyncLock
	End Sub

	Public Shared Function ScanFolderForLang(ByVal folder As String) As List(Of LanguageOption)
		SyncLock (threadLock)
			Dim tf As TranslatedFile = Nothing
			Dim ValidLangFiles As New List(Of LanguageOption)(30)

			For Each file As String In Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly)
				If file.EndsWith("\English.xml", StringComparison.OrdinalIgnoreCase) Then
					Continue For 'Skip english file
				End If

				If ReadFile(file, True, tf) AndAlso Not tf.Details.ISOLanguage.Equals("en-US") Then
					ValidLangFiles.Add(tf.Details)
				End If
			Next

			Return ValidLangFiles
		End SyncLock
	End Function

	Private Shared Sub GetChildren(ByVal parent As DependencyObject, ByRef controls As List(Of DependencyObject))
		If parent IsNot Nothing Then
			For i As Int32 = 0 To VisualTreeHelper.GetChildrenCount(parent) - 1
				Dim child As DependencyObject = VisualTreeHelper.GetChild(parent, i)

				If TypeOf (child) Is Control OrElse TypeOf (child) Is TextBlock Then
					controls.Add(child)
				End If

				If TypeOf (child) Is MenuItem Then
					Dim menuitem As MenuItem = CType(child, MenuItem)

					controls.Add(menuitem)
					GetMenuItems(menuitem, controls)
				Else
					If VisualTreeHelper.GetChildrenCount(child) > 0 Then
						GetChildren(child, controls)
					End If
				End If
			Next
		End If
	End Sub

	Private Shared Sub GetMenuItems(ByVal parent As MenuItem, ByRef controls As List(Of DependencyObject))
		If parent.HasItems Then
			For Each menuitem As MenuItem In parent.Items
				controls.Add(menuitem)

				If menuitem.HasItems Then
					GetMenuItems(menuitem, controls)
				End If
			Next
		End If
	End Sub

	Private Shared Sub TranslateControl(ByVal window As String, ByVal ctrl As DependencyObject)
		If TypeOf (ctrl) Is ComboBox Then				'ComboBox
			Dim cb As ComboBox = DirectCast(ctrl, ComboBox)
			Dim items As List(Of String) = GetTranslationList(window, cb.Name, "Item")

			If items IsNot Nothing AndAlso items.Count > 0 Then
				cb.Items.Clear()
				cb.ItemsSource = (GetTranslationList(window, cb.Name, "Item").ToArray())
				cb.SelectedIndex = 0
			End If

		ElseIf TypeOf (ctrl) Is ContentControl Then		'control has '.Content' property
			Dim contentCtrl As ContentControl = DirectCast(ctrl, ContentControl)
			Dim text = GetTranslation(window, contentCtrl.Name, "Text")

			If Not String.IsNullOrEmpty(text) Then
				If TypeOf (contentCtrl.Content) Is TextBlock Then
					Dim tbControl As TextBlock = DirectCast(contentCtrl.Content, TextBlock)

					tbControl.Text = text
				Else
					contentCtrl.Content = text
				End If
			End If

			Dim tooltipText As String = GetTranslation(window, contentCtrl.Name, "Tooltip")

			If Not String.IsNullOrEmpty(tooltipText) Then
				contentCtrl.ToolTip = tooltipText
			End If

		ElseIf TypeOf (ctrl) Is MenuItem Then			'MenuItem
			Dim menuCtrl As MenuItem = DirectCast(ctrl, MenuItem)

			Dim text = GetTranslation(window, menuCtrl.Name, "Text")

			If Not String.IsNullOrEmpty(text) Then
				menuCtrl.Header = text
			End If

			Dim tooltipText As String = GetTranslation(window, menuCtrl.Name, "Tooltip")

			If Not String.IsNullOrEmpty(tooltipText) Then
				menuCtrl.ToolTip = tooltipText
			End If
		ElseIf TypeOf (ctrl) Is TextBlock Then			'TextBlock
			Dim tb As TextBlock = DirectCast(ctrl, TextBlock)

			Dim text = GetTranslation(window, tb.Name, "Text")

			If Not String.IsNullOrEmpty(text) Then
				tb.Text = text
			End If

			Dim tooltipText As String = GetTranslation(window, tb.Name, "Tooltip")

			If Not String.IsNullOrEmpty(tooltipText) Then
				tb.ToolTip = tooltipText
			End If
		End If

	End Sub

	Private Shared Function LoadFile(ByVal langFile As String) As Stream
		If (langFile.Equals("en-US", StringComparison.OrdinalIgnoreCase)) Then
			Return Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("{0}.{1}", GetType(Languages).Namespace, "English.xml"))
		Else
			If (File.Exists(langFile)) Then
				Return New FileStream(langFile, FileMode.Open, FileAccess.Read, FileShare.Read)
			End If
		End If

		'gets caught on ReadLanguageFile()
		Throw New Exception(String.Format("Language file '{0}' not found!", langFile))
	End Function

	Private Shared Function ReadFile(ByVal langFile As String, ByVal onlyCheckValid As Boolean, ByRef output As TranslatedFile) As Boolean
		Try
			Using stream As Stream = LoadFile(langFile)
				Using sr As StreamReader = New StreamReader(stream, System.Text.Encoding.UTF8, True)  'use streamreader for correct encoding (for special chars)
					Dim settings As New XmlReaderSettings()
					settings.IgnoreComments = True
					settings.IgnoreWhitespace = True
					settings.ConformanceLevel = ConformanceLevel.Document

					Dim reader As XmlReader = XmlReader.Create(sr, settings)

					' Read until reach first line which should be
					' <DisplayDriverUninstaller ISO="en-US" Text="English">
					Do While reader.Read()
						If reader.NodeType = XmlNodeType.Element Then
							Exit Do
						End If
					Loop

					If reader.EOF Then 'End of File reached (empty translation file)
						Return False
					End If

					' Check reader nodetype (Element), element name (DDU), has attributes (ISO & Text)
					' Name = DisplayDriverUninstaller
					' Attributes = ISO="en-US" , Text="English"

					If reader.NodeType <> XmlNodeType.Element Or Not reader.Name.Equals(Application.Settings.AppName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) Or Not reader.HasAttributes Then
						Throw New InvalidDataException("Language file's format is invalid!" & sysNewLine & String.Format("Root node doesn't match '{0}'", Application.Settings.AppName.Replace(" ", "")) & sysNewLine & "Or missing attributes 'ISO' and 'Text'")
					End If

					Dim lang_iso As String = Nothing
					Dim lang_text As String = Nothing
					'	Dim newLineChr As String = "|"

					' <DisplayDriverUninstaller ISO="en-US" Text="English"> <-- read ISO & Text attribute's values
					Do While reader.MoveToNextAttribute()
						If Not String.IsNullOrEmpty(reader.Name) Then
							If reader.Name.Equals("ISO", StringComparison.OrdinalIgnoreCase) Then
								lang_iso = reader.Value
							ElseIf reader.Name.Equals("Text", StringComparison.OrdinalIgnoreCase) Then
								lang_text = reader.Value
								'ElseIf reader.Name.Equals("NewLineChar", StringComparison.OrdinalIgnoreCase) Then
								'	newLineChr = reader.Value
							End If
						End If
					Loop

					' ISO="en-US" and/or Text="English" attribute(s) not found 
					If String.IsNullOrEmpty(lang_iso) Or String.IsNullOrEmpty(lang_text) Then
						Throw New InvalidDataException("Language file's format is invalid!" & sysNewLine & "Missing required attributes 'ISO' and 'Text'" & sysNewLine & "(eg. 'ISO=""en-US""' and 'Text=""English""' )")
					End If

					'If Not IsNullOrWhitespace(newLineChr) Then
					'	newLineStr = newLineChr
					'End If

					Dim file As TranslatedFile = New TranslatedFile(lang_iso, lang_text, langFile)
					Dim controls As List(Of TranslatedControl)
					Dim ctrl As TranslatedControl
					Dim parent As TranslatedControl

					' File should be in correct format at this point
					Do While reader.Read() ' loop parents
						If reader.NodeType = XmlNodeType.Element Then ' found parent, <frmMain Text="...">, <frmLaunch Text="..."> etc.
							If reader.Name.Equals("LanguageCredits", StringComparison.OrdinalIgnoreCase) Then
								Dim credits As LanguageCredits

								Do
									reader.Read()

									If reader.NodeType = XmlNodeType.Element AndAlso reader.Name.StartsWith("Credits", StringComparison.OrdinalIgnoreCase) Then
										reader.Read()
										credits = New LanguageCredits()

										Do
											If reader.NodeType = XmlNodeType.Element Then ' child elements found  <User>, <LastUpdate> etc.
												If reader.Name.Equals("User", StringComparison.OrdinalIgnoreCase) Then
													credits.User = reader.ReadElementContentAsString().Replace(vbTab, "")
												ElseIf reader.Name.Equals("Details", StringComparison.OrdinalIgnoreCase) Then
													credits.Details = reader.ReadElementContentAsString().Replace(vbTab, "")
												ElseIf reader.Name.Equals("LastUpdate", StringComparison.OrdinalIgnoreCase) Then
													Dim dt As DateTime
													Dim dateStr As String = reader.ReadElementContentAsString().Replace(vbTab, "")

													If DateTime.TryParseExact(dateStr, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, dt) Then
														credits.LastUpdate = dt
													Else
														credits.LastUpdate = System.IO.File.GetLastWriteTime(langFile)
													End If
												End If

											Else
												reader.Read()
											End If
										Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.StartsWith("Credits", StringComparison.OrdinalIgnoreCase))

										If Not IsNullOrWhitespace(credits.User) Then
											file.Details.Credits.Insert(New Random().Next(0, file.Details.Credits.Count + 1), credits)

											'file.Details.Credits.Add(credits)
										End If
									End If
								Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals("LanguageCredits", StringComparison.OrdinalIgnoreCase))

								Continue Do
							End If

							parent = New TranslatedControl(reader.Name)
							controls = New List(Of TranslatedControl)

							If reader.HasAttributes Then 'parent has attributes. <frmMain Text="..."> <-- 'Text' attribute
								Do While reader.MoveToNextAttribute()
									parent.Attributes.Add(reader.Name, reader.Value.Trim(sysNewLine.ToCharArray()))
								Loop
							End If

							Do
								reader.Read()

								If reader.NodeType = XmlNodeType.Element Then ' found control, <Button1>, <Label1> etc.
									ctrl = New TranslatedControl(reader.Name)

									If reader.HasAttributes Then  ' has attributes? Shouldn't, but may used in future if needed
										Do While reader.MoveToNextAttribute()
											ctrl.Attributes.Add(reader.Name, reader.Value.Trim(sysNewLine.ToCharArray()))
										Loop
									End If

									reader.Read()

									Do
										If reader.NodeType = XmlNodeType.Element Then ' child elements found  <Text>, <Tooltip> etc.
											ctrl.Values.Add(reader.Name, reader.ReadElementContentAsString().Replace(vbTab, ""))
										Else
											reader.Read()
										End If
									Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals(ctrl.ControlName, StringComparison.OrdinalIgnoreCase))

									controls.Add(ctrl)
								End If
							Loop While Not (reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name.Equals(parent.ControlName, StringComparison.OrdinalIgnoreCase))

							file.Parents.Add(parent, controls)
						End If
					Loop

					output = file

					reader.Close()
					sr.Close()

					Return True
				End Using
			End Using
		Catch ex As Exception
			'if English translation is badly formatted/not readable (should never be)
			If langFile.EndsWith("\English.xml", StringComparison.OrdinalIgnoreCase) Or Not onlyCheckValid Then
				MsgBox(ex.Message & sysNewLine & ex.StackTrace)
			End If

			If Not onlyCheckValid Then
				Throw New InvalidDataException(String.Format("Language file is corrupted or badly formatted!{0}File: '{1}'", sysNewLine, langFile))
			Else
				If TypeOf (ex) Is InvalidDataException Then
					MessageBox.Show(ex.Message & String.Format("{0}{0}File: '{1}'", sysNewLine, langFile))
				Else
					MessageBox.Show(String.Format("Language file is corrupted or badly formatted!{0}File: '{1}'", sysNewLine, langFile))
				End If
			End If

			Return False
		End Try
	End Function

	Private Shared Function GetDictionary(ByVal translated As Boolean) As TranslatedFile
		If translated And isUserLoaded Then
			Return translatedDictionary
		Else
			Return englishDictionary
		End If
	End Function

	Private Shared Function GetControl(ByVal translated As Boolean, ByVal parent As String, ByVal control As String) As TranslatedControl
		Dim dict As TranslatedFile = GetDictionary(translated)

		For Each parent_kvp As KeyValuePair(Of TranslatedControl, List(Of TranslatedControl)) In dict.Parents 'forms
			If (String.Equals(parent_kvp.Key.ControlName, parent, StringComparison.OrdinalIgnoreCase)) Then	'form
				For Each tc As TranslatedControl In parent_kvp.Value 'control
					If (String.Equals(control, tc.ControlName, StringComparison.OrdinalIgnoreCase)) Then
						Return tc
					End If
				Next

				Exit For
			End If
		Next

		Return Nothing
	End Function

	Private Shared Function GetParent(ByVal translated As Boolean, ByVal parent As String) As TranslatedControl
		Dim dict As TranslatedFile = GetDictionary(translated)

		For Each parent_kvp As KeyValuePair(Of TranslatedControl, List(Of TranslatedControl)) In dict.Parents 'forms
			If (String.Equals(parent_kvp.Key.ControlName, parent, StringComparison.OrdinalIgnoreCase)) Then	'form
				Return parent_kvp.Key
			End If
		Next

		Return Nothing
	End Function

	Public Shared Sub CheckLanguageFileForErrors(ByVal logFile As String, ByVal skipSuccess As Boolean, ByVal langOption As LanguageOption)
		Using sw As StreamWriter = New StreamWriter(logFile, True, Encoding.UTF8)
			sw.WriteLine(">>>>>>>>> BEGINNING OF FILE <<<<<<<<<")
			sw.WriteLine("")

			Dim errors As New List(Of String)(100)
			Try

				Load(langOption)

				Dim foundParent As Boolean = False
				Dim foundTc As Boolean = False
				Dim foundTcValue As Boolean = False
				Dim foundAttr As Boolean = False
				Dim trims() As Char = sysNewLine.ToCharArray()
				Dim MaxLEN As Integer = 40

				For Each parent As KeyValuePair(Of TranslatedControl, List(Of TranslatedControl)) In englishDictionary.Parents
					foundParent = False

					For Each parent2 As KeyValuePair(Of TranslatedControl, List(Of TranslatedControl)) In translatedDictionary.Parents
						If parent.Key.ControlName = parent2.Key.ControlName Then
							foundParent = True
							If Not skipSuccess Then sw.WriteLine(parent.Key.ControlName & " -> " & parent2.Key.ControlName)

							For Each attr As KeyValuePair(Of String, String) In parent.Key.Attributes
								foundAttr = False

								For Each attr2 As KeyValuePair(Of String, String) In parent2.Key.Attributes
									If attr2.Key = attr.Key Then
										foundAttr = True

										If Not skipSuccess Then sw.WriteLine(vbTab & "Attribute: " & attr.Key & " -> " & attr2.Key)
										If Not skipSuccess Then sw.WriteLine(vbTab & vbTab & """" & attr.Value & """ -> """ & If(IsNullOrWhitespace(attr2.Value.Trim(trims)), "???", attr2.Value.Trim(trims)) & """")

										If IsNullOrWhitespace(attr2.Value) Then
											errors.Add("'" & parent2.Key.ControlName & "'s attribute '" & attr2.Key & "'s value is empty!")
										End If

										If Not skipSuccess Then sw.WriteLine("")
										Exit For
									End If
								Next

								If Not foundAttr Then
									errors.Add("'" & parent.Key.ControlName & "'s attribute '" & attr.Key & "' not found!")
								End If
							Next

							For Each tc As TranslatedControl In parent.Value
								foundTc = False

								For Each tc2 As TranslatedControl In parent2.Value
									If tc.ControlName = tc2.ControlName Then
										foundTc = True
										If Not skipSuccess Then sw.WriteLine(vbTab & tc.ControlName & " -> " & tc2.ControlName)


										For Each attr As KeyValuePair(Of String, String) In tc.Attributes
											foundAttr = False

											For Each attr2 As KeyValuePair(Of String, String) In tc2.Attributes
												If attr2.Key = attr.Key Then
													foundAttr = True

													If Not skipSuccess Then sw.WriteLine(vbTab & "Attribute: " & attr.Key & " -> " & attr2.Key)
													If Not skipSuccess Then sw.WriteLine(vbTab & vbTab & """" & If(attr.Value.Trim(trims).Length > MaxLEN, attr.Value.Trim(trims).Substring(0, MaxLEN) & " ...", attr.Value.Trim(trims)) & """ -> """ & If(IsNullOrWhitespace(attr2.Value.Trim(trims)), "???", If(attr2.Value.Trim(trims).Length > MaxLEN, attr2.Value.Trim(trims).Substring(0, MaxLEN) & " ...", attr2.Value.Trim(trims))) & """")

													If IsNullOrWhitespace(attr2.Value) Then
														errors.Add("'" & parent2.Key.ControlName & "'s control '" & tc2.ControlName & "'s attribute '" & attr2.Key & "'s value is empty!")
													End If

													Exit For
												End If
											Next

											If Not foundAttr Then
												errors.Add("'" & parent.Key.ControlName & "'s control '" & tc.ControlName & "'s attribute '" & attr.Key & "' not found!")
											End If
										Next

										For Each kvp As KeyValuePair(Of String, String) In tc.Values
											foundTcValue = False

											For Each kvp2 As KeyValuePair(Of String, String) In tc2.Values
												If kvp.Key = kvp2.Key Then
													foundTcValue = True

													If Not skipSuccess Then sw.WriteLine(vbTab & vbTab & kvp.Key & " -> " & kvp2.Key)
													If Not skipSuccess Then sw.WriteLine(vbTab & vbTab & vbTab & """" & If(kvp.Value.Trim(trims).Length > MaxLEN, kvp.Value.Trim(trims).Substring(0, MaxLEN) & " ...", kvp.Value.Trim(trims)) & """ -> """ & If(IsNullOrWhitespace(kvp2.Value.Trim(trims)), "???", If(kvp2.Value.Trim(trims).Length > MaxLEN, kvp2.Value.Trim(trims).Substring(0, MaxLEN) & " ...", kvp2.Value.Trim(trims))) & """")

													If IsNullOrWhitespace(kvp2.Value) Then
														errors.Add("'" & parent2.Key.ControlName & "'s control '" & tc2.ControlName & "' child elements '" & kvp2.Key & "'s value is empty!")
													End If

													Exit For
												End If
											Next

											If Not foundTcValue Then
												errors.Add("'" & parent.Key.ControlName & "'s control '" & tc.ControlName & "' child element '" & kvp.Key & "' not found!")
											End If
										Next

										If Not skipSuccess Then sw.WriteLine("")
										Exit For
									End If
								Next

								If Not foundTc Then
									errors.Add("'" & parent.Key.ControlName & "'s control '" & tc.ControlName & "' not found!")
								End If
							Next

							If Not skipSuccess Then sw.WriteLine("")
							Exit For
						End If
					Next

					If Not foundParent Then
						errors.Add("'" & parent.Key.ControlName & " not found!")
					End If
				Next

			Catch ex As XmlException
				errors.Add(">>>>>>>>> XML ERROR! " & ex.Message)
			End Try

			sw.WriteLine("")

			If errors.Count > 0 Then
				sw.WriteLine(">>>>>>>>> ERRORS <<<<<<<<<")
				sw.WriteLine("")
				sw.WriteLine(langOption.Filename.Substring(langOption.Filename.LastIndexOf("\")))
				sw.WriteLine("")

				For Each msg As String In errors
					sw.WriteLine(msg)
				Next

			Else
				sw.WriteLine(">>>>>>>>> NO ERRORS <<<<<<<<<")
				sw.WriteLine("")
				sw.WriteLine(langOption.Filename.Substring(langOption.Filename.LastIndexOf("\")))
			End If

			sw.WriteLine("")
			sw.WriteLine(">>>>>>>>> END OF FILE <<<<<<<<<")
			sw.WriteLine("")
			sw.WriteLine("")
			sw.WriteLine("")
			sw.WriteLine("")

			sw.Flush()
			sw.Close()
		End Using
	End Sub

	Private Class TranslatedFile
		Private m_details As LanguageOption
		Private m_parents As Dictionary(Of TranslatedControl, List(Of TranslatedControl))

		Public ReadOnly Property Details As LanguageOption
			Get
				Return m_details
			End Get
		End Property
		Public ReadOnly Property Parents As Dictionary(Of TranslatedControl, List(Of TranslatedControl))
			Get
				Return m_parents
			End Get
		End Property

		Public Sub New(ByVal langISO As String, ByVal langText As String, ByVal langFile As String)
			m_details = New LanguageOption(langISO, langText, langFile)
			m_parents = New Dictionary(Of TranslatedControl, List(Of TranslatedControl))(100)
		End Sub

		Public Overrides Function ToString() As String
			Return String.Format("{0} ({1})", m_details.DisplayText, m_details.ISOLanguage)
		End Function
	End Class

	Private Class TranslatedControl
		Private m_control As String
		Private m_attributes As Dictionary(Of String, String)
		Private m_values As Dictionary(Of String, String)

		Public ReadOnly Property ControlName As String
			Get
				Return m_control
			End Get
		End Property
		Public ReadOnly Property Attributes As Dictionary(Of String, String)
			Get
				Return m_attributes
			End Get
		End Property
		Public ReadOnly Property Values As Dictionary(Of String, String)
			Get
				Return m_values
			End Get
		End Property

		Public Sub New(ByVal control As String)
			m_control = control
			m_attributes = New Dictionary(Of String, String)
			m_values = New Dictionary(Of String, String)
		End Sub

		Public Overrides Function ToString() As String
			Return ControlName
		End Function
	End Class

	Public Class LanguageOption
		Implements IComparable(Of LanguageOption)
		Implements IEquatable(Of LanguageOption)

		Private m_isolang As String
		Private m_displaytext As String
		Private m_filename As String
		Private m_credits As List(Of LanguageCredits)

		Public ReadOnly Property ISOLanguage As String
			Get
				Return m_isolang
			End Get
		End Property
		Public ReadOnly Property DisplayText As String
			Get
				Return m_displaytext
			End Get
		End Property
		Public ReadOnly Property Filename As String
			Get
				Return m_filename
			End Get
		End Property
		Public ReadOnly Property Credits As List(Of LanguageCredits)
			Get
				Return m_credits
			End Get
		End Property

		Public Sub New(ByVal langISO As String, ByVal langText As String, ByVal langFile As String)
			m_isolang = langISO
			m_displaytext = langText
			m_filename = langFile
			m_credits = New List(Of LanguageCredits)(5)
		End Sub

		Public Overrides Function ToString() As String
			'Return String.Format("{0} - {1} - {2}", m_lang, m_text, m_filename)
			Return m_isolang
		End Function

		Public Overloads Function Equals(other As LanguageOption) As Boolean Implements System.IEquatable(Of LanguageOption).Equals
			If other IsNot Nothing Then
				Return Me.ISOLanguage.Equals(other.ISOLanguage, StringComparison.OrdinalIgnoreCase)
			End If

			Return False
		End Function

		Public Function CompareTo(other As LanguageOption) As Integer Implements System.IComparable(Of LanguageOption).CompareTo
			Return Me.DisplayText.CompareTo(other.DisplayText)
		End Function
	End Class

	Public Class LanguageCredits
		Public Property User As String
		Public Property Details As String
		Public Property LastUpdate As DateTime?
	End Class

	Shared Sub Compare(p1 As String, opt As LanguageOption)
		Throw New NotImplementedException
	End Sub

End Class
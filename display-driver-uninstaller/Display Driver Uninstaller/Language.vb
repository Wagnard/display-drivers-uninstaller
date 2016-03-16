Imports System.Xml
Imports System.IO
Imports System.Reflection
Imports System.Text

Public Class Languages
    Private Shared isEngLoaded As Boolean = False
    Private Shared isUserLoaded As Boolean = False
    Private Shared useTranslated As Boolean = True
    Private Shared currentLang As String = ""
    Private Shared ReadOnly threadLock As String = "You shall not pass!" 'lock access for one thread at time
    Private Const newlineStr As String = "|"

    Public Shared Property Current As String
        Get
            Return currentLang
        End Get
        Private Set(value As String)
            currentLang = value
        End Set
    End Property

    Private Shared englishDictionary As TranslatedFile = Nothing
    Private Shared translatedDictionary As TranslatedFile = Nothing

    ''' <param name="langOption">Which language to load for use. Use 'Nothing' for defaul (English)</param>
    Public Shared Sub Load(Optional ByVal langOption As LanguageOption = Nothing)
        SyncLock (threadLock)
            If langOption Is Nothing Then
                If Not isEngLoaded Or englishDictionary Is Nothing Then
                    isEngLoaded = ReadFile("en", False, englishDictionary)
                End If

                useTranslated = False
                currentLang = englishDictionary.ISOLanguage
            Else
                If langOption.ISOLanguage.Equals("en", StringComparison.OrdinalIgnoreCase) Then
                    If Not isEngLoaded Or englishDictionary Is Nothing Then
                        isEngLoaded = ReadFile("en", False, englishDictionary)
                    End If

                    useTranslated = False
                    currentLang = englishDictionary.ISOLanguage
                Else
                    If isUserLoaded And translatedDictionary IsNot Nothing Then
                        translatedDictionary.Parents.Clear()
                    End If

                    isUserLoaded = ReadFile(langOption.Filename, False, translatedDictionary)
                    useTranslated = True
                    currentLang = translatedDictionary.ISOLanguage
                End If
            End If
        End SyncLock
    End Sub

    Private Shared Sub LoadDefault()
        If Not isEngLoaded Or englishDictionary Is Nothing Then
            isEngLoaded = ReadFile("English", False, englishDictionary)
            useTranslated = False
            currentLang = englishDictionary.ISOLanguage
        End If
    End Sub

    ''' <param name="parent">Which form (Form1, fmrLaunch)</param>
    ''' <param name="type">Name of propery (Me.Text)</param>
    ''' <param name="returnValue">If no translation (english or current) found, return given value</param>
    ''' <returns>Translated text. If language not found, return English text.</returns> 
    Public Shared Function GetParentTranslation(ByVal parent As String, ByVal type As String, Optional ByVal returnValue As String = Nothing) As String
        SyncLock (threadLock)
            If Not isEngLoaded And Not useTranslated Then
                LoadDefault()
            End If

            Dim tc As TranslatedControl = GetParent(useTranslated, parent)
            Dim noTranslation As Boolean = False

            If tc Is Nothing Then
                If Not useTranslated Then
                    Return returnValue
                End If
notFound:
                tc = GetParent(False, parent)
                noTranslation = True

                If tc Is Nothing Then
                    Return returnValue
                End If
            End If

            If Not tc Is Nothing Then
                For Each kvp As KeyValuePair(Of String, String) In tc.Attributes
                    If (kvp.Key.Equals(type, StringComparison.OrdinalIgnoreCase)) Then
                        If Not String.IsNullOrEmpty(kvp.Value) Then
                            Return kvp.Value.Trim(New Char() {vbCr, vbLf, vbCrLf})
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

            Return returnValue
        End SyncLock
    End Function

    ''' <param name="parent">In which form control is located (Form1, fmrLaunch)</param>
    ''' <param name="control">Name of control, eg Button1</param>
    ''' <param name="type">What attribute to return. Text, Tooltip etc.</param>
    ''' <param name="returnValue">If no translation (english or current) found, return given value</param>
    ''' <returns>Translated text. If language not found, return English text</returns> 
    Public Shared Function GetTranslation(ByVal parent As String, ByVal control As String, ByVal type As String, Optional ByVal returnValue As String = Nothing) As String
        SyncLock (threadLock)
            If Not isEngLoaded And Not useTranslated Then
                LoadDefault()
            End If

            Dim tc As TranslatedControl = GetControl(useTranslated, parent, control)
            Dim noTranslation As Boolean = False

            If tc Is Nothing Then
                If Not useTranslated Then
                    Return returnValue
                End If
notFound:
                tc = GetControl(False, parent, control)
                noTranslation = True

                If tc Is Nothing Then
                    Return returnValue
                End If
            End If

            For Each kvp As KeyValuePair(Of String, String) In tc.Values
                If (kvp.Key.Equals(type, StringComparison.OrdinalIgnoreCase)) Then
                    If Not String.IsNullOrEmpty(kvp.Value) Then
                        Return kvp.Value.Trim(New Char() {vbCr, vbLf, vbCrLf}).Replace(newlineStr, vbCrLf)
                    Else
                        Exit For
                    End If
                End If
            Next

            If noTranslation Then
                Return returnValue
            Else
                GoTo notFound
            End If
        End SyncLock
    End Function

    ''' <param name="parent">In which form control is located (Form1, fmrLaunch)</param>
    ''' <param name="control">Name of control, eg Button1</param>
    ''' <param name="beginsWith">Begins with text. Useful for getting array of values (eg. ComboBox/ListBox)</param>
    ''' <returns>Translated text array. If language not found, return as English</returns> 
    Public Shared Function GetTranslationList(ByVal parent As String, ByVal control As String, ByVal beginsWith As String) As List(Of String)
        SyncLock (threadLock)
            If Not isEngLoaded And Not useTranslated Then
                LoadDefault()
            End If

            Dim tc As TranslatedControl = GetControl(useTranslated, parent, control)

            If tc Is Nothing Then
                If Not useTranslated Then
                    Return Nothing
                End If

                tc = GetControl(False, parent, control)
            End If


            If Not tc Is Nothing Then
                Dim items As New List(Of String)

                For Each kvp As KeyValuePair(Of String, String) In tc.Values
                    If (kvp.Key.StartsWith(beginsWith, StringComparison.OrdinalIgnoreCase)) Then
                        items.Add(kvp.Value)
                    End If
                Next
                Return items
            End If

            Return Nothing
        End SyncLock
    End Function

    ''' <param name="form">Which form to translate (frmMain, frmLaunch etc)</param>
    ''' <param name="tooltip">If form has tooltip, use it here (fills tooltip texts if has any)</param>
    Public Shared Sub TranslateForm(ByVal form As Forms.Form, Optional ByVal tooltip As ToolTip = Nothing)
        SyncLock (threadLock)
            If Not isEngLoaded And Not useTranslated Then
                LoadDefault()
            End If

            Dim text As String = GetParentTranslation(form.Name, "Text")

            If Not String.IsNullOrEmpty(text) Then
                form.Text = text
            End If

            For Each c As Control In form.Controls
                TranslateControl(form.Name, c, tooltip)
            Next

            'If form.MainMenuStrip IsNot Nothing AndAlso form.MainMenuStrip.Items.Count > 0 Then
            '    For Each menuitem As Forms.ToolStripMenuItem In form.MainMenuStrip.Items
            '        TranslateMenuItem(form.Name, menuitem, tooltip)
            '    Next
            'End If
        End SyncLock
    End Sub

    'Private Shared Sub TranslateMenuItem(ByVal form As String, ByVal menuitem As Forms.ToolStripMenuItem, Optional ByVal tp As ToolTip = Nothing)
    '    Dim text = GetTranslation(form, menuitem.Name, "Text")

    '    If Not String.IsNullOrEmpty(text) Then
    '        menuitem.Text = text
    '    End If

    '    If menuitem.HasDropDownItems Then
    '        For Each childmenuitem As ToolStripMenuItem In menuitem.DropDownItems
    '            TranslateMenuItem(form, childmenuitem, tp)
    '        Next
    '    End If
    'End Sub

    Private Shared Sub TranslateControl(ByVal form As String, ByVal ctrl As Control, Optional ByVal tp As ToolTip = Nothing)
        If TypeOf (ctrl) Is ComboBox Then
            Dim cb As ComboBox = ctrl
            Dim items As List(Of String) = GetTranslationList(form, ctrl.Name, "Item")

            If items IsNot Nothing AndAlso items.Count > 0 Then
                cb.Items.Clear()
                cb.ItemsSource=(GetTranslationList(form, ctrl.Name, "Item").ToArray())
            End If
        Else
            Dim text = GetTranslation(form, ctrl.Name, "Text")

            If Not String.IsNullOrEmpty(text) Then
                ' ctrl.text = text

            End If

            If tp IsNot Nothing Then
                Dim tooltip As String = GetTranslation(form, ctrl.Name, "Tooltip")

                If Not String.IsNullOrEmpty(tooltip) Then
                    '        tp.SetToolTip(ctrl, tooltip)
                End If
            End If
        End If

        'For Each c As Control In ctrl.controls
        '    If (c.HasChildren) Then
        '        TranslateControl(form, c, tp)
        '    End If
        'Next


    End Sub

    Private Shared Function LoadFile(ByVal langFile As String) As Stream
        If (langFile.Equals("en", StringComparison.OrdinalIgnoreCase)) Then
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
                    ' <DisplayDriverUninstaller ISO="en" Text="English">
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
                    ' Attributes = ISO="en" , Text="English"

                    If reader.NodeType <> XmlNodeType.Element Or Not reader.Name.Equals(Application.Current.MainWindow.GetType().Assembly.GetName().Name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) Or Not reader.HasAttributes Then
                        Throw New InvalidDataException("Language file's format is invalid!" & vbCrLf & String.Format("Root node doesn't match '{0}'", Application.Current.MainWindow.GetType().Assembly.GetName().Name.Replace(" ", "")) & vbCrLf & "Or missing attributes 'ISO' and 'Text'")
                    End If

                    Dim lang_iso As String = ""
                    Dim lang_text As String = ""

                    ' <DisplayDriverUninstaller ISO="en" Text="English"> <-- read ISO & Text attribute's values
                    Do While reader.MoveToNextAttribute()
                        If Not String.IsNullOrEmpty(reader.Name) Then
                            If reader.Name.Equals("ISO", StringComparison.OrdinalIgnoreCase) Then
                                lang_iso = reader.Value
                            ElseIf reader.Name.Equals("Text", StringComparison.OrdinalIgnoreCase) Then
                                lang_text = reader.Value
                            End If
                        End If
                    Loop

                    ' ISO="en" and/or Text="English" attribute(s) not found 
                    If String.IsNullOrEmpty(lang_iso) Or String.IsNullOrEmpty(lang_text) Then
                        Throw New InvalidDataException("Language file's format is invalid!" & vbCrLf & "Missing required attributes 'ISO' and 'Text'" & vbCrLf & "(eg. 'ISO=""en""' and 'Text=""English""' )")
                    End If

                    Dim file As TranslatedFile = New TranslatedFile(lang_iso, lang_text)
                    Dim controls As List(Of TranslatedControl)
                    Dim ctrl As TranslatedControl
                    Dim parent As TranslatedControl

                    ' File should be in correct format at this point
                    Do While reader.Read() ' loop parents
                        If reader.NodeType = XmlNodeType.Element Then ' found parent, <frmMain Text="...">, <frmLaunch Text="..."> etc.
                            parent = New TranslatedControl(reader.Name)
                            controls = New List(Of TranslatedControl)

                            If reader.HasAttributes Then 'parent has attributes. <frmMain Text="..."> <-- 'Text' attribute
                                Do While reader.MoveToNextAttribute()
                                    parent.Attributes.Add(reader.Name, reader.Value.Replace(newlineStr, vbCrLf))
                                Loop
                            End If

                            Do
                                reader.Read()

                                If reader.NodeType = XmlNodeType.Element Then ' found control, <Button1>, <Label1> etc.
                                    ctrl = New TranslatedControl(reader.Name)

                                    If reader.HasAttributes Then  ' has attributes? Shouldn't, but may used in future if needed
                                        Do While reader.MoveToNextAttribute()
                                            ctrl.Attributes.Add(reader.Name, reader.Value.Replace(newlineStr, vbCrLf))
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
            If langFile.Equals("en", StringComparison.OrdinalIgnoreCase) Or Not onlyCheckValid Then
                Throw ex
            End If

            If Not onlyCheckValid Then
                Throw New InvalidDataException(String.Format("Language file is corrupted or badly formatted!{0}File: '{1}'", vbCrLf, langFile))
            Else
                If TypeOf (ex) Is InvalidDataException Then
                    MessageBox.Show(ex.Message & vbCrLf & String.Format("{0}File: '{1}'", vbCrLf, langFile))
                Else
                    MessageBox.Show(String.Format("Language file is corrupted or badly formatted!{0}File: '{1}'", vbCrLf, langFile))
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
            If (String.Equals(parent_kvp.Key.ControlName, parent, StringComparison.OrdinalIgnoreCase)) Then 'form
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
            If (String.Equals(parent_kvp.Key.ControlName, parent, StringComparison.OrdinalIgnoreCase)) Then 'form
                Return parent_kvp.Key
            End If
        Next

        Return Nothing
    End Function

    Public Shared Function ScanFolderForLang(ByVal folder As String) As List(Of LanguageOption)
        SyncLock (threadLock)
            Dim tf As TranslatedFile = Nothing
            Dim ValidLangFiles As New List(Of LanguageOption)(30)

            For Each file As String In Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly)
                If file.EndsWith("\English.xml", StringComparison.OrdinalIgnoreCase) Then
                    Continue For 'Skip english file
                End If

                If ReadFile(file, True, tf) AndAlso Not tf.ISOLanguage.Equals("en") Then
                    ValidLangFiles.Add(New LanguageOption(tf.ISOLanguage, tf.LanguageText, file))
                End If
            Next

            Return ValidLangFiles
        End SyncLock
    End Function

    Private Class TranslatedFile
        Private m_langIso As String
        Private m_langText As String
        Private m_parents As Dictionary(Of TranslatedControl, List(Of TranslatedControl))

        Public ReadOnly Property ISOLanguage As String
            Get
                Return m_langIso
            End Get
        End Property
        Public ReadOnly Property LanguageText As String
            Get
                Return m_langText
            End Get
        End Property
        Public ReadOnly Property Parents As Dictionary(Of TranslatedControl, List(Of TranslatedControl))
            Get
                Return m_parents
            End Get
        End Property

        Public Sub New(ByVal lang As String, ByVal text As String)
            m_langIso = lang
            m_langText = text
            m_parents = New Dictionary(Of TranslatedControl, List(Of TranslatedControl))(100)
        End Sub

        Public Overrides Function ToString() As String
            Return String.Format("{0} ({1})", LanguageText, ISOLanguage)
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
        Private m_lang As String
        Private m_text As String
        Private m_filename As String

        Public ReadOnly Property ISOLanguage As String
            Get
                Return m_lang
            End Get
        End Property
        Public ReadOnly Property DisplayText As String
            Get
                Return m_text
            End Get
        End Property
        Public ReadOnly Property Filename As String
            Get
                Return m_filename
            End Get
        End Property

        Public Sub New(ByVal langISO As String, ByVal langText As String, ByVal langFile As String)
            m_lang = langISO
            m_text = langText
            m_filename = langFile
        End Sub

        Public Overrides Function ToString() As String
            'Return String.Format("{0} - {1} - {2}", m_lang, m_text, m_filename)
            Return m_text
        End Function
    End Class

End Class
	<!--
	
	For editting files, I recommended use 3rd party software.
	Good example, Notepad++
	
	Avoid Microsoft Office products and WordPad (can break file's structure)
	
	
	
	Following applies to adding new languages:

		<DisplayDriverUninstaller ISO="en-GB" Text="English">
		
			ISO = "en-GB"
			->	(.NET) CultureInfo.CurrentCulture.Name		(en-US, en-GB, fr-FR .. etc)
			->	For other languages: https://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx

			Text = "English"	
			->	Shown to user in language selection box on Main window (Write In English!)
	
		New languages are supported "out-of-box", just placed on \Settings\Languages\ directory.
		If language file isn't correctly formatted, it won't be shown on Main windows Language selection box!
		
		
	>>> For detailed information about XML; see
	>>> http://www.w3schools.com/xml/xml_whatis.asp
	
	
	
	Translateable texts are between <Text> or <ToolTip>
	
		<frmOptions Text="Place text here"/>	<-- Windows title
	
		<Text>Place text here</Text>   
		<ToolTip>Place ToolTip text here (shown when cursor hovering above control)</ToolTip>   

		
	Example:
		<btnCleanRestart>				<-- Controls name, Don't change this!
			<Text>
				Clean and restart				<-- Translated text		
				(Highly Recommended)			<-- for multiline, just place on next line. No need any special separators anymore			
			</Text>
			<!-- This is comment -->		<-- Comment
			<Tooltip>
				Uninstall the current and previous drivers, then restart the computer.
				(Highly Recommended)
			</Tooltip>
		</btnCleanRestart>
		
		

	If there is no <ToolTip> but there is <Text>,
	<ToolTip> can be added below <Text>, not all toolips are just added yet.			

	Tip! keep controls text simple and short, and give detailed text on tooltip. ( Just don't go over 1000 chars :) )
	
	
	xml doesn't allow some special chars on text like <>&"'
	See below for few example alternatives
	
	>> Text in xml 		Visible in UI  
		&lt; 				< 			less than
		&gt; 				> 			greater than
		&amp; 				& 			ampersand 
		&apos; 				' 			apostrophe
		&quot; 				" 			quotation mark
	
	
	For adding comments 
	just place following text above or below line (to new line)
		
	<!-- This is comment -->
	


	>>> Formatting Examples <<<
		ALL TEST 1 - 4 shows exactly the same (in two lines). Not so critical to have exactly positioned lines 
		You can always check how it looks on UI
		
		Note! Empty line between lines will be shown
		
		<TEST>
			<TEST1>																				
				
	Some text1	
	Some text on second line</TEST1>
				
			<TEST2>Some text2
			Some text on second line</TEST2>


			<TEST3>
	Some text3
														Some text on second line
			</TEST3>

			<TEST4>
								
				Some text4
				Some text on second line
						
				
			</TEST4>
				
			<Recommended>
				Some text5
				Some text on second line
			</Recommended>
		</TEST>
		
		
		
		
		
	>>> 	Optional	 <<<
	You can add credits to yourself at end of file	
	Shown at (Menu) Info -> Translators
	
	Note! 
		All text are directly readed from each language file from <LanguageTranslators> section (if exists)
		If your name won't show up there, <LanguageTranslators>...</LanguageTranslators> 'structure' may be invalid.
		Below example is in correct format, so you can always copy below structure and modify texts if current file doesn't seem to work.
		
	Note!
		If date won't show, it most likely is "incorrect" format. Check day and month are in correct positions (and not reversed)
		Each language has some difference in date formats, but application supports only few. All below formats (9) are valid.
		
		d = Day of month (1-31), M = Month (1-12), yyyy = Year in four numbers (2016-2100)
		d/M/yyyy		01/04/2016 	..	21/2/2016	..	09/12/2015
		d.M.yyyy		01.04.2016 	..	21.2.2016 	..	09.12.2015
		d-M-yyyy		01-04-2016 	..	21-2-2016 	..	09-12-2015
		
	
	
	See below for example:
	
		
	<LanguageTranslators>
		<Translator>							
			<User>Wagnard</User>					<!-- Required. name/username/nick/email/something as "ID"		( If not set, won't be shown )  -->
			<Details>Owner</Details>				<!-- Optional. Anything, contact, email, etc					( Note! its available to view for everyone. Supports multiline! )  -->
			<LastUpdate>23/05/2016</LastUpdate>		<!-- Optional. Last update Date as mentioned on above note		( d = Day [1-31];  M = Month [1-12];  yyyy = Year [2016 -> ] )  -->
		</Translator>

		<Translator2>								<!-- As long as 'element' starts with 'Translator', and has matching end element, its valid -->
			<User>Someone</User>
			<LastUpdate>01/01/2000</LastUpdate>
		</Translator2>

		<Translator3>
			<User>Example</User>					<!-- Optional lines can be deleted or just leave empty -->
		</Translator3>

		<Translator4>
			<User>Test</User>
			<Details>
				Something
				Need second line?
				Even More details
				This is 4th line	
			</Details>
		</Translator4>
				
		<TranslatorN>
			<User>Test2</User>
			<LastUpdate>31/12/1999</LastUpdate>
		</TranslatorN>
	</LanguageTranslators>

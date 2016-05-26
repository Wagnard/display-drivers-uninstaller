	<!--
	
	For editting files, I recommended use 3rd party software.
	Good example, Notepad++
	
	Avoid Microsoft Office products and WordPad
	
	
	
	Following applies to adding new languages:

		<DisplayDriverUninstaller ISO="en-GB" Text="English">
		
			ISO = "en-GB"
			->	(.NET) CultureInfo.CurrentCulture.Name		(en-US, en-GB, fr-FR .. etc)
			->	For other languages: https://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx

			Text = "English"	
			->	Shown to user in language selection box on Main window (Write In English!)
	
	New languages are supported "out-of-box", just placed on \Settings\Languages\ directory.
	If language file isn't correctly formatted, it won't be shown on Main windows Language selection box.
	
	

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
	See below for examples
		
		
	<LanguageCredits>
		<Credits>
			<User>Wagnard</User>					<-- Required. username/nick/email/something as "ID"	( If not set, won't be shown )
			<Details>Creator</Details>				<-- Optional. Anything, contact, email				( Note! its available to view for everyone )
			<LastUpdate>23/05/2016</LastUpdate>		<-- Optional. Last update Date as 'dd/MM/yyyy'		( dd = Day [1-31];  MM = Month [1-12];  yyyy = Year [2016 - 2050] )
		</Credits>

		<Credits2>
			<User>Someone</User>
			<LastUpdate>01/01/2000</LastUpdate>
		</Credits2>

		<!--
		<Credits3>
			<User>Example</User>					<-- Optional lines can be deleted or just leave empty
		</Credits3>

		<Credits4>
			<User>Test</User>
			<Details>Something</Details>
		</Credits4>
				
		<CreditsN>
			<User>Test2</User>
			<LastUpdate>31/12/1999</LastUpdate>
		</CreditsN>
		-->	
	</LanguageCredits>

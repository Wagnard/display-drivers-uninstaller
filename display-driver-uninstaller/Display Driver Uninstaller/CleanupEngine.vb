Imports System.IO
Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32
Imports System.Security.AccessControl

Public Class CleanupEngine
	Private Function UpdateTextMethodmessagefn(ByRef number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
	End Function

	Public Sub deletesubregkey(ByRef regkeypath As RegistryKey, ByVal child As String)
		Dim fixregacls As Boolean = False

		If regkeypath IsNot Nothing AndAlso Not IsNullOrWhitespace(child) Then
			Try
				regkeypath.DeleteSubKeyTree(child)
				Application.Log.AddMessage(regkeypath.ToString + "\" + child + " - " + UpdateTextMethodmessagefn(39))
			Catch ex As UnauthorizedAccessException
				Application.Log.AddWarningMessage("Failed to remove registry subkey " + child + " Will try to set ACLs permission and try again.")
				fixregacls = True
			End Try
			'If exists, it means we need to modify it's ACls.
			If fixregacls AndAlso regkeypath IsNot Nothing Then
				ACL.Addregistrysecurity(regkeypath, child, RegistryRights.FullControl, AccessControlType.Allow)
				regkeypath.DeleteSubKeyTree(child)
				Application.Log.AddMessage(child + " - " + UpdateTextMethodmessagefn(39))
			End If
		End If
	End Sub



	Public Sub RemoveSharedDlls(ByVal directorypath As String)
		If Not IsNullOrWhitespace(directorypath) AndAlso Not FileIO.ExistsDir(directorypath) Then
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, directorypath & "\") Then
							Try
								deletevalue(regkey, child)
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
						End If
					Next
				End If
			End Using

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetValueNames
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, directorypath) Then
							Try
								deletevalue(regkey, child)
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
						End If
					Next
				End If
			End Using

			If IntPtr.Size = 8 Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetValueNames
							If IsNullOrWhitespace(child) Then Continue For

							If StrContainsAny(child, True, directorypath) Then
								Try
									deletevalue(regkey, child)
								Catch ex As Exception
									Application.Log.AddException(ex)
								End Try
							End If
						Next
					End If
				End Using
			End If
		End If
	End Sub

	Public Sub deletevalue(ByVal regkeypath As RegistryKey, ByVal child As String)
		If regkeypath IsNot Nothing AndAlso Not IsNullOrWhitespace(child) Then
			regkeypath.DeleteValue(child)

			Application.Log.AddMessage(regkeypath.ToString & "\" & child & " - " & UpdateTextMethodmessagefn(40))
		End If
	End Sub

	Public Sub ClassRoot(ByVal classroots As String())

		Dim wantedvalue As String = Nothing
		Dim appid As String = Nothing
		Dim typelib As String = Nothing

		Application.Log.AddMessage("Begin ClassRoot CleanUP")

		Try
			Using regkeyRoot As RegistryKey = Registry.ClassesRoot
				If regkeyRoot IsNot Nothing Then
					For Each child As String In regkeyRoot.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						For Each croot As String In classroots
							If IsNullOrWhitespace(croot) Then Continue For

							If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
								Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, child & "\CLSID")
									If regkey2 IsNot Nothing Then
										wantedvalue = regkey2.GetValue("", String.Empty).ToString()

										If IsNullOrWhitespace(wantedvalue) Then Continue For

										Try
											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue)
												If regkey3 IsNot Nothing Then
													appid = regkey3.GetValue("AppID", String.Empty).ToString()

													If Not IsNullOrWhitespace(appid) Then
														Try
															Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "AppID", True)
																If regkey4 IsNot Nothing Then
																	deletesubregkey(regkey4, appid)
																End If
															End Using
														Catch ex As Exception
															'Application.Log.AddWarning(ex) 'Temporary not logging because its spamming unnecessary warnings
														End Try
													End If
												End If
											End Using
										Catch ex As Exception
										End Try

										Try
											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue & "\TypeLib")
												If regkey3 IsNot Nothing Then
													typelib = regkey3.GetValue("", String.Empty).ToString()

													If Not IsNullOrWhitespace(typelib) Then
														Try
															Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "TypeLib", True)
																If regkey4 IsNot Nothing Then
																	deletesubregkey(regkey4, typelib)
																End If
															End Using
														Catch ex As Exception
															'Application.Log.AddWarning(ex) 'Temporary not logging because its spamming unnecessary warnings
														End Try
													End If
												End If
											End Using
										Catch ex As Exception
										End Try

										Try
											Using crkey As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID", True)
												If crkey IsNot Nothing Then
													For Each wantedvaluekey In crkey.GetSubKeyNames
														If IsNullOrWhitespace(wantedvaluekey) Then Continue For

														If StrContainsAny(wantedvaluekey, True, wantedvalue) Then
															Try
																deletesubregkey(crkey, wantedvalue)

																For Each childfile As String In regkeyRoot.GetSubKeyNames()
																	If IsNullOrWhitespace(childfile) Then Continue For

																	If childfile.EndsWith("file", StringComparison.OrdinalIgnoreCase) Then

																		Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, childfile)
																			If regkey5 IsNot Nothing Then
																				For Each shellEX As String In regkey5.GetSubKeyNames
																					If IsNullOrWhitespace(shellEX) Then Continue For

																					If StrContainsAny(shellEX, True, "shellex") Then
																						Using regkey6 As RegistryKey = MyRegistry.OpenSubKey(regkey5, shellEX & "\ContextMenuHandlers", True)
																							If regkey6 IsNot Nothing Then
																								For Each ShExt As String In regkey6.GetSubKeyNames
																									If IsNullOrWhitespace(ShExt) Then Continue For

																									If StrContainsAny(ShExt, True, "openglshext", "nvappshext") Then
																										Using regkey7 As RegistryKey = MyRegistry.OpenSubKey(regkey6, ShExt)
																											If regkey7 IsNot Nothing Then
																												If StrContainsAny(regkey7.GetValue("", String.Empty).ToString, True, wantedvalue) Then
																													Try
																														deletesubregkey(regkey6, ShExt)
																													Catch ex As Exception
																														Application.Log.AddException(ex)
																													End Try
																												End If
																											End If
																										End Using
																									End If
																								Next
																							End If
																						End Using
																					End If
																				Next
																			End If
																		End Using
																	End If
																Next
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													Next
												End If
											End Using
										Catch ex As Exception
										End Try

									End If
								End Using

								'here I remove the mediafoundationkeys if present
								'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
								Try
									Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms", True)
										If regkeyM IsNot Nothing Then
											deletesubregkey(regkeyM, (child.Replace("{", "")).Replace("}", ""))
										End If
									End Using

								Catch ex As Exception
								End Try
								Try
									Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
										If regkeyM IsNot Nothing Then
											deletesubregkey(regkeyM, (child.Replace("{", "")).Replace("}", ""))
										End If
									End Using
								Catch ex As Exception
								End Try

								deletesubregkey(regkeyRoot, child)
							End If
						Next
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			' DevMltk: I think there was typo?    Yes look like so. nice catch. (Wagnard)
			'
			' Orginal code:
			'
			'Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,"Wow6432Node", True)		<--- regkey = 'HKEY_CLASSES_ROOT\Wow6432Node'
			'	If regkey IsNot Nothing Then
			'		
			'		For Each child As String In regkey.GetSubKeyNames()					<--- all subkeys of 'HKEY_CLASSES_ROOT\Wow6432Node'
			'			If IsNullOrWhitespace(child) = False Then
			'				For i As Integer = 0 To ClassRoot.Length - 1
			'					If Not IsNullOrWhitespace(ClassRoot(i)) Then
			'						If child.ToLower.StartsWith(ClassRoot(i).ToLower) Then
			'							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,child & "\CLSID")		 <-- ???
			'							
			'
			'	child is subkey of 'HKEY_CLASSES_ROOT\Wow6432Node'
			'		=> HKEY_CLASSES_ROOT\Wow6432Node\"child"
			'
			'	but ???
			'	Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,child & "\CLSID")
			'		=> HKEY_CLASSES_ROOT\"child"\CLSID		<--- shouldn't child be under \Wow6432Node ?
			'
			'
			'	Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey,child & "\CLSID")	<-- I Think this it should be, revert if I'm missing something there. Line 8311
			'		=> HKEY_CLASSES_ROOT\Wow6432Node\"child"\CLSID			

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							For Each croot As String In classroots
								If IsNullOrWhitespace(croot) Then Continue For

								If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
									Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\CLSID")
										If regkey2 IsNot Nothing Then
											wantedvalue = regkey2.GetValue("", String.Empty).ToString()

											If IsNullOrWhitespace(wantedvalue) Then Continue For

											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue)
												appid = regkey3.GetValue("AppID", String.Empty).ToString()

												If Not IsNullOrWhitespace(appid) Then
													Try
														Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "AppID", True)
															If regkey4 IsNot Nothing Then
																deletesubregkey(regkey4, appid)
															End If
														End Using
													Catch ex As Exception
													End Try
												End If
											End Using

											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue & "\TypeLib")
												typelib = regkey3.GetValue("", String.Empty).ToString

												If Not IsNullOrWhitespace(appid) Then
													Try
														Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "TypeLib", True)
															If regkey4 IsNot Nothing Then
																deletesubregkey(regkey4, typelib)
															End If
														End Using
													Catch ex As Exception
													End Try
												End If
											End Using

											Try
												Using regkeyC As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID", True)
													If regkeyC IsNot Nothing Then
														deletesubregkey(regkeyC, wantedvalue)
													End If
												End Using
											Catch ex As Exception
											End Try
										End If
									End Using

									'here I remove the mediafoundationkeys if present
									'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
									Try
										Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms", True)
											If regkeyM IsNot Nothing Then
												deletesubregkey(regkeyM, (child.Replace("{", "")).Replace("}", ""))
											End If
										End Using
									Catch ex As Exception
									End Try

									Try
										Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
											If regkeyM IsNot Nothing Then
												deletesubregkey(regkeyM, (child.Replace("{", "")).Replace("}", ""))
											End If
										End Using
									Catch ex As Exception
									End Try

									Try
										deletesubregkey(regkey, child)
									Catch ex As Exception
									End Try
								End If
							Next
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Application.Log.AddMessage("End ClassRoot CleanUP")
	End Sub

	Public Sub installer(ByVal packages As String(), config As ThreadSettings)

		Dim wantedvalue As String = Nothing
		Dim removephysx As Boolean = config.RemovePhysX

		Try
			Application.Log.AddMessage("-Starting S-1-5-xx region cleanUP")
			Dim file As String
			Dim folder As String
			Using basekey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			   "Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
				If basekey IsNot Nothing Then
					For Each super As String In basekey.GetSubKeyNames()
						If IsNullOrWhitespace(super) Then Continue For

						If StrContainsAny(super, True, "s-1-5") Then

							Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
							 "Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)

								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetSubKeyNames()
										If IsNullOrWhitespace(child) Then Continue For

										Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
										"Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child & _
										"\InstallProperties", False)

											If subregkey IsNot Nothing Then

												wantedvalue = subregkey.GetValue("DisplayName", String.Empty).ToString
												If IsNullOrWhitespace(wantedvalue) Then Continue For

												For Each package As String In packages
													If IsNullOrWhitespace(package) Then Continue For
													If (StrContainsAny(wantedvalue, True, package)) AndAlso
													  Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then


														Application.Log.AddMessage("Removing .msi")
														'Deleting here the c:\windows\installer entries.
														Try
															file = subregkey.GetValue("LocalPackage", String.Empty).ToString
															If IsNullOrWhitespace(file) Then Continue For

															If StrContainsAny(file, True, ".msi") Then
																delete(file)
															End If
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Try
															folder = subregkey.GetValue("UninstallString", String.Empty).ToString
															If Not IsNullOrWhitespace(folder) Then
																If StrContainsAny(folder, True, "{") AndAlso StrContainsAny(folder, True, "}") Then

																	folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
																	TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)

																	Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
																		If regkey2 IsNot Nothing Then

																			For Each subkeyname As String In regkey2.GetValueNames
																				If Not IsNullOrWhitespace(subkeyname) Then
																					If StrContainsAny(subkeyname, True, folder) Then
																						deletevalue(regkey2, subkeyname)
																					End If
																				End If
																			Next
																		End If
																	End Using
																End If
															End If
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Try
															deletesubregkey(regkey, child)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
													 "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes", True)
															If superregkey IsNot Nothing Then
																For Each child2 As String In superregkey.GetSubKeyNames()
																	If IsNullOrWhitespace(child2) Then Continue For

																	Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
																"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\" & child2, False)

																		If subsuperregkey IsNot Nothing Then
																			For Each wantedstring As String In subsuperregkey.GetValueNames()
																				If IsNullOrWhitespace(wantedstring) Then Continue For

																				If StrContainsAny(wantedstring, True, child) Then
																					Try
																						deletesubregkey(superregkey, child2)
																					Catch ex As Exception
																						Application.Log.AddException(ex)
																					End Try
																				End If
																			Next
																		End If
																	End Using
																Next
															End If
														End Using
														Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
													 "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)
															If superregkey IsNot Nothing Then
																For Each child2 As String In superregkey.GetSubKeyNames()
																	If IsNullOrWhitespace(child2) Then Continue For

																	Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
																"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child2, False)

																		If subsuperregkey IsNot Nothing Then
																			For Each wantedstring In subsuperregkey.GetValueNames()
																				If IsNullOrWhitespace(wantedstring) Then Continue For

																				If wantedstring.Contains(child) Then
																					Try
																						deletesubregkey(superregkey, child2)
																					Catch ex As Exception
																						Application.Log.AddException(ex)
																					End Try
																				End If
																			Next
																		End If
																	End Using
																Next
															End If
														End Using
													End If
												Next
											End If
										End Using
									Next
								End If
							End Using
						End If
					Next
				End If
			End Using

			Application.Log.AddMessage("-End of S-1-5-xx region cleanUP")
		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			Dim folder As String
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
			"Installer\Products", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
						"Installer\Products\" & child, False)

							If subregkey IsNot Nothing Then

								wantedvalue = subregkey.GetValue("ProductName", String.Empty).ToString
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each package As String In packages
									If IsNullOrWhitespace(package) Then Continue For

									If StrContainsAny(wantedvalue, True, package) AndAlso
									   Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then

										Try
											folder = subregkey.GetValue("ProductIcon", String.Empty).ToString

											If (IsNullOrWhitespace(folder)) Then Continue For
											If Not StrContainsAny(folder, True, "{") Then Continue For
											If Not StrContainsAny(folder, True, "}") Then Continue For

											folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
											TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)
											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
												If regkey2 IsNot Nothing Then
													For Each subkeyname As String In regkey2.GetValueNames
														If IsNullOrWhitespace(subkeyname) Then Continue For

														If StrContainsAny(subkeyname, True, folder) Then
															deletevalue(regkey2, subkeyname)
														End If
													Next
												End If
											End Using

										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try

										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try

										Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Features", True)

											Try
												deletesubregkey(regkey3, child)
											Catch ex As Exception
											End Try
										End Using

										Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
										"Installer\UpgradeCodes", True)
											If superregkey IsNot Nothing Then
												For Each child2 As String In superregkey.GetSubKeyNames()
													If IsNullOrWhitespace(child2) Then Continue For

													Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
													  "Installer\UpgradeCodes\" & child2, False)

														If subsuperregkey IsNot Nothing Then
															For Each wantedstring As String In subsuperregkey.GetValueNames()
																If IsNullOrWhitespace(wantedstring) Then Continue For
																If wantedstring.Contains(child) Then
																	Try
																		deletesubregkey(superregkey, child2)
																	Catch ex As Exception
																	End Try
																End If
															Next
														End If
													End Using
												Next
											End If
										End Using
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Classes\Installer\Products", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
						"Software\Classes\Installer\Products\" & child, False)

							If subregkey IsNot Nothing Then
								wantedvalue = subregkey.GetValue("ProductName", String.Empty).ToString
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each package As String In packages
									If IsNullOrWhitespace(package) Then Continue For

									If (StrContainsAny(wantedvalue, True, package)) AndAlso
									  Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then

										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try

										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Classes\Installer\Features", True)
											Try
												deletesubregkey(regkey2, child)
											Catch ex As Exception
											End Try
										End Using

										Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
										"Software\Classes\Installer\UpgradeCodes", True)
											If superregkey IsNot Nothing Then
												For Each child2 As String In superregkey.GetSubKeyNames()
													If IsNullOrWhitespace(child2) Then Continue For

													Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
													  "Software\Classes\Installer\UpgradeCodes\" & child2, False)

														If subsuperregkey IsNot Nothing Then
															For Each wantedstring As String In subsuperregkey.GetValueNames()
																If IsNullOrWhitespace(wantedstring) Then Continue For

																If StrContainsAny(wantedstring, True, child) Then
																	Try
																		deletesubregkey(superregkey, child2)
																	Catch ex As Exception
																	End Try
																End If
															Next
														End If
													End Using
												Next
											End If
										End Using
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If IsNullOrWhitespace(users) Then Continue For

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
				users & "\Software\Microsoft\Installer\Products", True)

					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
						   users & "\Software\Microsoft\Installer\Products\" & child, False)

								If subregkey IsNot Nothing Then
									wantedvalue = subregkey.GetValue("ProductName", String.Empty).ToString
									If IsNullOrWhitespace(wantedvalue) Then Continue For

									For Each package As String In packages
										If IsNullOrWhitespace(package) Then Continue For

										If (StrContainsAny(wantedvalue, True, package)) AndAlso
										   Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then

											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try

											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Installer\Features", True)
												Try
													deletesubregkey(regkey2, child)
												Catch ex As Exception
												End Try
											End Using

											Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
											users & "\Software\Microsoft\Installer\UpgradeCodes", True)
												If superregkey IsNot Nothing Then
													For Each child2 As String In superregkey.GetSubKeyNames()
														If IsNullOrWhitespace(child2) Then Continue For

														Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
														  users & "\Software\Microsoft\Installer\UpgradeCodes" & child2, False)

															If subsuperregkey IsNot Nothing Then
																For Each wantedstring As String In subsuperregkey.GetValueNames()
																	If IsNullOrWhitespace(wantedstring) Then Continue For

																	If wantedstring.Contains(child) Then
																		Try
																			deletesubregkey(superregkey, child2)
																		Catch ex As Exception
																		End Try
																	End If
																Next
															End If
														End Using
													Next
												End If
											End Using
										End If
									Next
								End If
							End Using
						Next
					End If
				End Using
			Next

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub cleanserviceprocess(ByVal services As String())
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles


		Application.Log.AddMessage("Cleaning Process/Services...")

		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services", False)
			If regkey IsNot Nothing Then
				For Each service As String In services
					If IsNullOrWhitespace(service) Then Continue For

					Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, service, False)
						If regkey2 IsNot Nothing Then
							If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(service, True, "amdkmafd")) Then

								Dim stopservice As New ProcessStartInfo
								stopservice.FileName = "cmd.exe"
								stopservice.Arguments = " /Cnet stop " & Chr(34) & service & Chr(34)
								stopservice.UseShellExecute = False
								stopservice.CreateNoWindow = True
								stopservice.RedirectStandardOutput = False


								Dim processstopservice As New Process
								processstopservice.StartInfo = stopservice

								Application.Log.AddMessage("Stopping service : " & service)
								processstopservice.Start()
								processstopservice.WaitForExit()
								processstopservice.Close()

								stopservice.Arguments = " /Csc delete " & Chr(34) & service & Chr(34)

								processstopservice.StartInfo = stopservice

								Application.Log.AddMessage("Trying to Deleting service : " & service)
								processstopservice.Start()
								processstopservice.WaitForExit()
								processstopservice.Close()

								stopservice.Arguments = " /Csc interrogate " & Chr(34) & service & Chr(34)
								processstopservice.StartInfo = stopservice
								processstopservice.Start()
								processstopservice.WaitForExit()
								processstopservice.Close()

								'Verify that the service was indeed removed.
								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, service, False)
									If regkey3 IsNot Nothing Then

										Application.Log.AddMessage("Failed to remove the service.")
									Else

										Application.Log.AddMessage("Service removed.")
									End If
								End Using
							End If
						End If
					End Using

					System.Threading.Thread.Sleep(10)
				Next
			End If
		End Using


		Application.Log.AddMessage("Process/Services CleanUP Complete")

		'-------------
		'control/video
		'-------------
		'Reason I put this in service is that the removal of this is based from its service.

		Application.Log.AddMessage("Control/Video CleanUP")

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\Video", True)
				If regkey IsNot Nothing Then
					Dim serviceValue As String

					For Each child As String In regkey.GetSubKeyNames
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\Video", False)
							If subregkey IsNot Nothing Then
								serviceValue = CStr(subregkey.GetValue("Service"))

								If IsNullOrWhitespace(serviceValue) Then Continue For

								For Each service As String In services
									If serviceValue.Equals(service, StringComparison.OrdinalIgnoreCase) Then
										Try
											deletesubregkey(regkey, child)
											deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
											Exit For
										Catch ex As Exception
										End Try
									End If

								Next
							Else
								'Here, if subregkey is nothing, it mean \video doesnt exist and is no \0000, we can delete it.
								'this is a general cleanUP we could say.
								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\0000")
									If regkey3 Is Nothing Then
										Try
											deletesubregkey(regkey, child)
											deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
										Catch ex As Exception
										End Try
									End If
								End Using
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Public Sub prePnplockdownfiles(ByVal oeminf As String)
		Dim win8higher = frmMain.win8higher
		Dim processinfo As New ProcessStartInfo
		Dim process As New Process
		Dim sourceValue As String
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		Try
			If win8higher Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
					If regkey IsNot Nothing Then
						If Not IsNullOrWhitespace(oeminf) Then
							If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(oeminf, True, "amdkmafd.sys")) Then
								For Each child As String In regkey.GetSubKeyNames()
									If IsNullOrWhitespace(child) Then Continue For

									sourceValue = CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("Source"))

									If Not IsNullOrWhitespace(sourceValue) AndAlso StrContainsAny(sourceValue, True, oeminf) Then
										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
								Next
							End If
						End If
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub Pnplockdownfiles(ByVal driverfiles As String())

		Dim winxp = frmMain.winxp
		Dim win8higher = frmMain.win8higher
		Dim processinfo As New ProcessStartInfo
		Dim process As New Process
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		Try
			If Not winxp Then  'this does not exist on winxp so we skip if winxp detected
				If win8higher Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
						If regkey IsNot Nothing Then
							For i As Integer = 0 To driverfiles.Length - 1
								If Not IsNullOrWhitespace(driverfiles(i)) Then
									If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd.sys")) Then
										For Each child As String In regkey.GetSubKeyNames()
											If IsNullOrWhitespace(child) = False Then
												If child.ToLower.Replace("/", "\").Contains("\" + driverfiles(i).ToLower) Then
													Try
														deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End If
										Next
									End If
								End If
							Next
						End If
					End Using

				Else   'Older windows  (windows vista and 7 run here)

					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
						If regkey IsNot Nothing Then
							For i As Integer = 0 To driverfiles.Length - 1
								If Not IsNullOrWhitespace(driverfiles(i)) Then
									If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then
										For Each child As String In regkey.GetValueNames()
											If IsNullOrWhitespace(child) = False Then
												If child.ToLower.Contains(driverfiles(i).ToLower) Then
													Try
														deletevalue(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End If
										Next
									End If
								End If
							Next
						End If
					End Using
				End If
			End If

		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub clsidleftover(ByVal clsidleftover As String())

		Dim wantedvalue As String
		Dim appid As String = Nothing
		Dim typelib As String = Nothing

		Application.Log.AddMessage("Begin clsidleftover CleanUP")

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\InProcServer32", False)
								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
										wantedvalue = subregkey.GetValue("").ToString
										If IsNullOrWhitespace(wantedvalue) = False Then
											For i As Integer = 0 To clsidleftover.Length - 1
												If Not IsNullOrWhitespace(clsidleftover(i)) Then
													If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try

														Try
															'here I remove the mediafoundationkeys if present
															'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															deletesubregkey(regkey, child)
															Exit For
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If
												End If
											Next
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child, False)
								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
										wantedvalue = subregkey.GetValue("").ToString
										If IsNullOrWhitespace(wantedvalue) = False Then
											For i As Integer = 0 To clsidleftover.Length - 1
												If Not IsNullOrWhitespace(clsidleftover(i)) Then
													If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try
														Try
															'here I remove the mediafoundationkeys if present
															'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															deletesubregkey(regkey, child)
															Exit For
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If
												End If
											Next
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\InProcServer32", False)
									Try

										If subregkey IsNot Nothing Then
											If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
												wantedvalue = subregkey.GetValue("").ToString
												If IsNullOrWhitespace(wantedvalue) = False Then
													For i As Integer = 0 To clsidleftover.Length - 1
														If Not IsNullOrWhitespace(clsidleftover(i)) Then
															If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

																Try
																	If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																		appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																		Try
																			deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
																		Catch ex As Exception
																		End Try
																	End If
																Catch ex As Exception
																End Try

																Try
																	If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																		typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																		Try
																			deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																		Catch ex As Exception
																		End Try
																	End If
																Catch ex As Exception
																End Try

																Try
																	'here I remove the mediafoundationkeys if present
																	'f79eac7d-e545-4387-bdee-d647d7bde42a is the Encoder section. Same on all windows version.
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))

																	Catch ex As Exception
																	End Try
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
																	Catch ex As Exception
																	End Try
																	deletesubregkey(regkey, child)
																	Exit For
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End If
													Next
												End If
											End If
										End If
									Catch ex As Exception
										Application.Log.AddException(ex, subregkey.ToString)	 ' for logging conversion error from a user(byte()---> String) Probably user fault. 
									End Try
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)

			End Try

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child, False)
									If subregkey IsNot Nothing Then
										If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
											wantedvalue = subregkey.GetValue("").ToString
											If IsNullOrWhitespace(wantedvalue) = False Then
												For i As Integer = 0 To clsidleftover.Length - 1
													If Not IsNullOrWhitespace(clsidleftover(i)) Then
														If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

															Try
																If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																	appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
																	Catch ex As Exception
																	End Try
																End If
															Catch ex As Exception
															End Try

															Try
																If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																	typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																	Catch ex As Exception
																	End Try
																End If
															Catch ex As Exception
															End Try
															Try
																'here I remove the mediafoundationkeys if present
																'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
																Catch ex As Exception
																End Try
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
																Catch ex As Exception
																End Try
																deletesubregkey(regkey, child)
																Exit For
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End If
												Next
											End If
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\LocalServer32", False)
								If subregkey IsNot Nothing Then
									If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
										wantedvalue = subregkey.GetValue("").ToString
										If IsNullOrWhitespace(wantedvalue) = False Then
											For i As Integer = 0 To clsidleftover.Length - 1
												If Not IsNullOrWhitespace(clsidleftover(i)) Then
													If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try

														Try
															If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
																Catch ex As Exception
																End Try
															End If
														Catch ex As Exception
														End Try
														Try
															'here I remove the mediafoundationkeys if present
															'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															Try
																deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
															Catch ex As Exception
															End Try
															deletesubregkey(regkey, child)
															Exit For
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If
												End If
											Next
										End If
									End If
								End If
							End Using
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\LocalServer32", False)
									If subregkey IsNot Nothing Then
										If IsNullOrWhitespace(CStr(subregkey.GetValue(""))) = False Then
											wantedvalue = subregkey.GetValue("").ToString
											If IsNullOrWhitespace(wantedvalue) = False Then
												For i As Integer = 0 To clsidleftover.Length - 1
													If Not IsNullOrWhitespace(clsidleftover(i)) Then
														If wantedvalue.ToLower.Contains(clsidleftover(i).ToLower) Then


															Try
																If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID"))) Then
																	appid = MyRegistry.OpenSubKey(regkey, child).GetValue("AppID").ToString
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
																	Catch ex As Exception
																	End Try
																End If
															Catch ex As Exception
															End Try

															Try
																If Not IsNullOrWhitespace(CStr(MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue(""))) Then
																	typelib = MyRegistry.OpenSubKey(regkey, child & "\TypeLib").GetValue("").ToString
																	Try
																		deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																	Catch ex As Exception
																	End Try
																End If
															Catch ex As Exception
															End Try
															Try
																'here I remove the mediafoundationkeys if present
																'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), (child.Replace("{", "")).Replace("}", ""))
																Catch ex As Exception
																End Try
																Try
																	deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), (child.Replace("{", "")).Replace("}", ""))
																Catch ex As Exception
																End Try
																deletesubregkey(regkey, child)
																Exit For
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End If
												Next
											End If
										End If
									End If
								End Using
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) = False Then
							For i As Integer = 0 To clsidleftover.Length - 1
								If Not IsNullOrWhitespace(clsidleftover(i)) Then
									If child.ToLower.Contains(clsidleftover(i).ToLower) Then
										Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
											If subregkey IsNot Nothing Then
												If IsNullOrWhitespace(CStr(subregkey.GetValue("AppID"))) = False Then
													wantedvalue = subregkey.GetValue("AppID").ToString
													If IsNullOrWhitespace(wantedvalue) = False Then

														Try
															deletesubregkey(regkey, wantedvalue)
														Catch ex As Exception
														End Try

														Try
															deletesubregkey(regkey, child)
															Exit For
														Catch ex As Exception
														End Try
													End If
												End If
											End If
										End Using
									End If
								End If
							Next
						End If
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) = False Then
								For i As Integer = 0 To clsidleftover.Length - 1
									If Not IsNullOrWhitespace(clsidleftover(i)) Then
										If child.ToLower.Contains(clsidleftover(i).ToLower) Then
											Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
												If subregkey IsNot Nothing Then
													If IsNullOrWhitespace(CStr(subregkey.GetValue("AppID"))) = False Then
														wantedvalue = subregkey.GetValue("AppID").ToString
														If IsNullOrWhitespace(wantedvalue) = False Then

															Try
																deletesubregkey(regkey, wantedvalue)
															Catch ex As Exception
															End Try

															Try
																deletesubregkey(regkey, child)
																Exit For
															Catch ex As Exception
															End Try
														End If
													End If
												End If
											End Using
										End If
									End If
								Next
							End If
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If


		'clean orphan typelib.....
		Application.Log.AddMessage("Orphan cleanUp")
		Try
			Dim value As String = Nothing

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
							If regkey2 Is Nothing Then Continue For

							For Each child2 As String In regkey2.GetSubKeyNames()
								If IsNullOrWhitespace(child2) Then Continue For

								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
									If regkey3 Is Nothing Then Continue For

									For Each child3 As String In regkey3.GetSubKeyNames()
										If IsNullOrWhitespace(child3) Then Continue For

										Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey3, child3)
											If regkey4 Is Nothing Then Continue For

											For Each child4 As String In regkey4.GetSubKeyNames()
												If IsNullOrWhitespace(child4) Then Continue For

												Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkey4, child4)
													If regkey5 Is Nothing Then Continue For

													value = regkey5.GetValue("", String.Empty).ToString()	 'Can also be UInt32 btw! (Usualy abnormal from personal experience,bit still should be managed in the future)

													If IsNullOrWhitespace(value) Then Continue For

													For Each clsIdle As String In clsidleftover
														If IsNullOrWhitespace(clsIdle) Then Continue For

														If StrContainsAny(value, True, clsIdle) Then
															Try
																deletesubregkey(regkey, child)
																Application.Log.AddMessage(child + " for " + clsIdle)
																Exit For
															Catch ex As Exception
															End Try
														End If
													Next
												End Using
											Next
										End Using
									Next
								End Using
							Next
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		Application.Log.AddMessage("End Orphan cleanUp")
		Application.Log.AddMessage("End clsidleftover CleanUP")
	End Sub

	Public Sub interfaces(ByVal interfaces As String())

		Application.Log.AddMessage("Start Interface CleanUP")

		Try
			Dim wantedvalue As String
			Dim typelib As String = Nothing
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface\" & child, False)

							If subregkey IsNot Nothing Then
								wantedvalue = subregkey.GetValue("", String.Empty).ToString
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each iface As String In interfaces
									If IsNullOrWhitespace(iface) Then Continue For

									If wantedvalue.ToLower.StartsWith(iface.ToLower) Then

										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
											If regkey2 IsNot Nothing Then
												typelib = regkey2.GetValue("", String.Empty).ToString
												If IsNullOrWhitespace(typelib) Then Continue For

												Try
													deletesubregkey(regkey2, typelib)
												Catch ex As Exception
												End Try
											End If
										End Using

										Try
											deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then

			Try
				Dim wantedvalue As String
				Dim typelib As String = Nothing
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface\" & child, False)

								If subregkey IsNot Nothing Then

									Try
										wantedvalue = subregkey.GetValue("", String.Empty).ToString
									Catch ex As Exception
										Application.Log.AddException(ex)
										wantedvalue = ""
									End Try

									If IsNullOrWhitespace(wantedvalue) Then Continue For

									For Each iface As String In interfaces
										If IsNullOrWhitespace(iface) Then Continue For

										If wantedvalue.ToLower.StartsWith(iface.ToLower) Then

											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
												If regkey2 IsNot Nothing Then
													typelib = regkey2.GetValue("", String.Empty).ToString
													If IsNullOrWhitespace(typelib) Then Continue For

													Try
														deletesubregkey(regkey2, typelib)
													Catch ex As Exception
													End Try
												End If
											End Using

											Try
												deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									Next
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

		End If

		Application.Log.AddMessage("END Interface CleanUP")
	End Sub

	Public Sub folderscleanup(ByVal driverfiles As String())

		Dim winxp = frmMain.winxp
		Dim filePath As String
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		For Each driverFile As String In driverfiles
			If IsNullOrWhitespace(driverFile) Then Continue For

			If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(driverFile, True, "amdkmafd")) Then

				filePath = System.Environment.SystemDirectory

				Try
					Delete(filePath & "\" & driverFile)
				Catch ex As Exception
				End Try

				Try
					Delete(filePath & "\Drivers\" & driverFile)
				Catch ex As Exception
				End Try

				If winxp Then
					Try
						Delete(filePath & "\Drivers\dllcache\" & driverFile)
					Catch ex As Exception
					End Try
				End If
			End If
		Next

		Try
			For Each driverFile As String In driverfiles
				If IsNullOrWhitespace(driverFile) Then Continue For

				filePath = Environment.GetEnvironmentVariable("windir")

				For Each child As String In My.Computer.FileSystem.GetFiles(filePath & "\Prefetch")
					If IsNullOrWhitespace(child) Then Continue For

					If StrContainsAny(child, True, driverFile) Then
						Try
							Delete(child)
						Catch ex As Exception
						End Try
					End If
				Next

			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		Dim winPath As String = Nothing

		Try
			'	Note  As of Windows Vista, these values have been replaced by KNOWNFOLDERID values. 
			'	See that topic for a list of the new constants and their corresponding CSIDL values. 
			'	For convenience, corresponding KNOWNFOLDERID values are also noted here for each CSIDL value.

			'	The CSIDL system is supported under Windows Vista for compatibility reasons.
			'	However, new development should use KNOWNFOLDERID values rather than CSIDL values.

			If Not WinAPI.GetFolderPath(WinAPI.CLSID.SYSTEMX86, winPath) Then
				Throw New ArgumentException("Can't get window's sysWOW64 directory")
			End If
		Catch ex As Exception
			Application.Log.AddException(ex, "Can't get window's sysWOW64 directory")
		End Try


		If IntPtr.Size = 8 Then
			For Each driverFile As String In driverfiles
				If IsNullOrWhitespace(driverFile) Then Continue For

				If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(driverFile, True, "amdkmafd")) Then

					For Each child As String In My.Computer.FileSystem.GetFiles(winPath, Microsoft.VisualBasic.FileIO.SearchOption.SearchTopLevelOnly, "*.log")
						If IsNullOrWhitespace(child) Then Continue For

						If StrContainsAny(child, True, driverFile) Then
							Try
								Delete(child)
							Catch ex As Exception
							End Try
						End If
					Next

					Try
						Delete(winPath & "\Drivers\" & driverFile)
					Catch ex As Exception
					End Try

					Try
						Delete(winPath & "\" & driverFile)
					Catch ex As Exception
					End Try
				End If
			Next
		End If
	End Sub
	Public Sub TestDelete(ByVal folder As String, config As ThreadSettings)
		' UpdateTextMethod(UpdateTextMethodmessagefn("18"))
		'Application.Log.AddMessage("Deleting some specials folders, it could take some times...")
		'ensure that this folder can be accessed with current user ac.
		If Not Directory.Exists(folder) Then
			Exit Sub
		End If

		'Get an object repesenting the directory path below
		Dim di As New DirectoryInfo(folder)

		'Traverse all of the child directors in the root; get to the lowest child
		'and delete all files, working our way back up to the top.  All files
		'must be deleted in the directory, before the directory itself can be deleted.
		'also if there is hidden / readonly / system attribute..  change those attribute.
		Try


			For Each diChild As DirectoryInfo In di.GetDirectories()
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
				If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

					Try
						TraverseDirectory(diChild, config)
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		'Finally, clean all of the files directly in the root directory
		CleanAllFilesInDirectory(di)

		'The containing directory can only be deleted if the directory
		'is now completely empty and all files previously within
		'were deleted.
		Try
			If di.GetFiles().Length = 0 And Directory.GetDirectories(folder).Length = 0 Then
				di.Delete()
				Application.Log.AddMessage(di.ToString + " - " + "Folder removed via testdelete sub")
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		RemoveSharedDlls(folder)
	End Sub
	Private Sub TraverseDirectory(ByVal di As DirectoryInfo, ByVal config As ThreadSettings)

		'If the current directory has more child directories, then continure
		'to traverse down until we are at the lowest level and remove
		'there hidden / readonly / system attribute..  At that point all of the
		'files will be deleted.
		For Each diChild As DirectoryInfo In di.GetDirectories()
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
			If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

				Try
					TraverseDirectory(diChild, config)
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		Next

		'Now that we have no more child directories to traverse, delete all of the files
		'in the current directory, and then delete the directory itself.
		CleanAllFilesInDirectory(di)


		'The containing directory can only be deleted if the directory
		'is now completely empty and all files previously within
		'were deleted.
		If di.GetFiles().Length = 0 Then
			Try
				di.Delete()
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

	End Sub

	''' Iterates through all files in the directory passed into
	''' method and deletes them.
	''' It may be necessary to wrap this call in impersonation or ensure parent directory
	''' permissions prior, because delete permissions are not guaranteed.

	Private Sub CleanAllFilesInDirectory(ByVal DirectoryToClean As DirectoryInfo)

		Try
			For Each fi As FileInfo In DirectoryToClean.GetFiles()
				'The following code is NOT required, but shows how some logic can be wrapped
				'around the deletion of files.  For example, only delete files with
				'a creation date older than 1 hour from the current time.  If you
				'always want to delete all of the files regardless, just remove
				'the next 'If' statement.

				'Read only files can not be deleted, so mark the attribute as 'IsReadOnly = False'

				Try
					fi.IsReadOnly = False
				Catch ex As Exception
				End Try

				Try
					fi.Delete()
				Catch ex As Exception
				End Try
				'On a rare occasion, files being deleted might be slower than program execution, and upon returning
				'from this call, attempting to delete the directory will throw an exception stating it is not yet
				'empty, even though a fraction of a second later it actually is.  Therefore the 'Optional' code below
				'can stall the process just long enough to ensure the file is deleted before proceeding. The value
				'can be adjusted as needed from testing and running the process repeatedly.
				'System.Threading.Thread.sleep(10)  '50 millisecond stall (0.025 Seconds)

			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub delete(ByVal filename As String)
		FileIO.Delete(filename)
		RemoveSharedDlls(filename)
	End Sub
End Class

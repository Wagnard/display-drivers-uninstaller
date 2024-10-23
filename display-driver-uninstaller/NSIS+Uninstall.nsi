﻿; Script generated by the HM NIS Edit Script Wizard.

; HM NIS Edit Wizard helper defines
!define PRODUCT_NAME            "Display Driver Uninstaller"
!define PRODUCT_VERSION         "18.0.8.3"

!define PRODUCT_PUBLISHER       "Wagnardsoft"
!define PRODUCT_WEB_SITE        "https://www.wagnardsoft.com"
!define PRODUCT_DIR_REGKEY      "Software\Microsoft\Windows\CurrentVersion\App Paths\Display Driver Uninstaller.exe"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"
!define PRODUCT_UNINST_KEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"

!define /date CURRENT_YEAR      "%Y"
!define PRODUCT_COPYRIGHT       "${PRODUCT_PUBLISHER} 2021-${CURRENT_YEAR}"
!define PRODUCT_DESCRIPTION     "${PRODUCT_NAME} setup"
!define SOURCE_PATH             "C:\Users\ghisl\Desktop\DDU\DDU v${PRODUCT_VERSION}"

; MUI 1.67 compatible ------
!include "MUI2.nsh"

; Define the welcome page title to support 3 lines
!define MUI_WELCOMEPAGE_TITLE_3LINES

; include new funcion (Getsize)
!include "FileFunc.nsh"
!include "TextFunc.nsh"
 
; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "Display Driver Uninstaller\Resources\DDU.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!insertmacro MUI_PAGE_WELCOME
; License page
!insertmacro MUI_PAGE_LICENSE "Licence.txt"
; Directory page
!insertmacro MUI_PAGE_DIRECTORY
; Instfiles page
!insertmacro MUI_PAGE_INSTFILES
; Finish page
!define MUI_FINISHPAGE_RUN "$INSTDIR\Display Driver Uninstaller.exe"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\Readme.txt"
!insertmacro MUI_PAGE_FINISH
; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language files
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Italian"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "TradChinese"
!insertmacro MUI_LANGUAGE "German"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "Japanese"
!insertmacro MUI_LANGUAGE "Polish"

; Show the language selection dialog in .onInit
Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

; English
LangString STR_01 ${LANG_ENGLISH}  "Web site ${PRODUCT_NAME}"
LangString STR_02 ${LANG_ENGLISH}  "Uninstall ${PRODUCT_NAME}"
LangString STR_03 ${LANG_ENGLISH}  "$(^Name) was successfully removed from your computer."
LangString STR_04 ${LANG_ENGLISH}  "Are you sure you want to completely remove $(^Name) and all of its components?"

; Italian
LangString STR_01 ${LANG_ITALIAN}  "Sito web ${PRODUCT_NAME}"
LangString STR_02 ${LANG_ITALIAN}  "Disinstalla ${PRODUCT_NAME}"
LangString STR_03 ${LANG_ITALIAN}  "$(^Name) è stato correttamente rimosso dal computer."
LangString STR_04 ${LANG_ITALIAN}  "Sei sicuro di voler rimuovere completamente $(^Name) e tutti i suoi componenti?"

; French
LangString STR_01 ${LANG_FRENCH} "Site web ${PRODUCT_NAME}"
LangString STR_02 ${LANG_FRENCH} "Désinstaller ${PRODUCT_NAME}"
LangString STR_03 ${LANG_FRENCH} "$(^Name) a été supprimé avec succès de votre ordinateur."
LangString STR_04 ${LANG_FRENCH} "Êtes-vous sûr de vouloir complètement supprimer $(^Name) et tous ses composants ?"

; Simplified Chinese
LangString STR_01 ${LANG_SIMPCHINESE} "网站 ${PRODUCT_NAME}"
LangString STR_02 ${LANG_SIMPCHINESE} "卸载 ${PRODUCT_NAME}"
LangString STR_03 ${LANG_SIMPCHINESE} "$(^Name) 已成功从您的电脑中移除。"
LangString STR_04 ${LANG_SIMPCHINESE} "您确定要完全删除 $(^Name) 及其所有组件吗？"

; Traditional Chinese
LangString STR_01 ${LANG_TRADCHINESE} "網站 ${PRODUCT_NAME}"
LangString STR_02 ${LANG_TRADCHINESE} "卸載 ${PRODUCT_NAME}"
LangString STR_03 ${LANG_TRADCHINESE} "$(^Name) 已成功從您的電腦中移除。"
LangString STR_04 ${LANG_TRADCHINESE} "您確定要完全刪除 $(^Name) 及其所有組件嗎？"

; German
LangString STR_01 ${LANG_GERMAN}  "Webseite ${PRODUCT_NAME}"
LangString STR_02 ${LANG_GERMAN}  "Deinstalliere ${PRODUCT_NAME}"
LangString STR_03 ${LANG_GERMAN}  "$(^Name) wurde erfolgreich von Ihrem Computer entfernt."
LangString STR_04 ${LANG_GERMAN}  "Sind Sie sicher, dass Sie $(^Name) und alle Komponenten vollständig entfernen möchten?"

; Spanish
LangString STR_01 ${LANG_SPANISH}  "Sitio web ${PRODUCT_NAME}"
LangString STR_02 ${LANG_SPANISH}  "Desinstalar ${PRODUCT_NAME}"
LangString STR_03 ${LANG_SPANISH}  "$(^Name) se ha eliminado correctamente de su ordenador."
LangString STR_04 ${LANG_SPANISH}  "¿Está seguro de que desea eliminar completamente $(^Name) y todos sus componentes?"

; Japanese
LangString STR_01 ${LANG_JAPANESE}  "ウェブサイト ${PRODUCT_NAME}"
LangString STR_02 ${LANG_JAPANESE}  "${PRODUCT_NAME} をアンインストールする"
LangString STR_03 ${LANG_JAPANESE}  "$(^Name) はコンピュータから正常に削除されました。"
LangString STR_04 ${LANG_JAPANESE}  "本当に $(^Name) とそのすべてのコンポーネントを完全に削除しますか？"

; Polski
LangString STR_01 ${LANG_POLISH}  "Strona ${PRODUCT_NAME}"
LangString STR_02 ${LANG_POLISH}  "Odinstaluj ${PRODUCT_NAME}"
LangString STR_03 ${LANG_POLISH}  "$(^Name) został pomyślnie usunięty z komputera."
LangString STR_04 ${LANG_POLISH}  "Czy na pewno chcesz usunąć $(^Name) i wszystkie jego składniki?"



; MUI end ------

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${SOURCE_PATH}_setup.exe"
InstallDir "$PROGRAMFILES\Display Driver Uninstaller"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

VIProductVersion "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName"        "${PRODUCT_NAME}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion"     "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName"        "${PRODUCT_PUBLISHER}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright"     "${PRODUCT_COPYRIGHT}"
;VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalTrademarks"    "${PRODUCT_COPYRIGHT}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion"        "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription"    "${PRODUCT_DESCRIPTION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "InternalName"       "${PRODUCT_NAME}"

Section "MainSection" SEC01
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  SetOverwrite try
  File "${SOURCE_PATH}\Display Driver Uninstaller.exe"
  CreateDirectory "$SMPROGRAMS\Display Driver Uninstaller"
  CreateShortCut "$SMPROGRAMS\Display Driver Uninstaller\Display Driver Uninstaller.lnk" "$INSTDIR\Display Driver Uninstaller.exe"
  CreateShortCut "$DESKTOP\Display Driver Uninstaller.lnk" "$INSTDIR\Display Driver Uninstaller.exe"
  File "${SOURCE_PATH}\Display Driver Uninstaller.pdb"
  File "${SOURCE_PATH}\Issues and solutions.txt"
  File "${SOURCE_PATH}\Licence.txt"
  File "${SOURCE_PATH}\Readme.txt"
  SetOutPath "$INSTDIR\Settings\AMD"
  File "${SOURCE_PATH}\Settings\AMD\classroot.cfg"
  File "${SOURCE_PATH}\Settings\AMD\clsidleftover.cfg"
  File "${SOURCE_PATH}\Settings\AMD\driverfiles.cfg"
  File "${SOURCE_PATH}\Settings\AMD\driverfilesKMAFD.cfg"
  File "${SOURCE_PATH}\Settings\AMD\driverfilesKMPFD.cfg"
  File "${SOURCE_PATH}\Settings\AMD\driverfilesKMPFD.cfg.bak"
  File "${SOURCE_PATH}\Settings\AMD\interface.cfg"
  File "${SOURCE_PATH}\Settings\AMD\packages.cfg"
  File "${SOURCE_PATH}\Settings\AMD\services.cfg"
  SetOutPath "$INSTDIR\Settings\INTEL"
  File "${SOURCE_PATH}\Settings\INTEL\classroot.cfg"
  File "${SOURCE_PATH}\Settings\INTEL\clsidleftover.cfg"
  File "${SOURCE_PATH}\Settings\INTEL\driverfiles.cfg"
  File "${SOURCE_PATH}\Settings\INTEL\interface.cfg"
  File "${SOURCE_PATH}\Settings\INTEL\packages.cfg"
  File "${SOURCE_PATH}\Settings\INTEL\services.cfg"
  SetOutPath "$INSTDIR\Settings\Languages"
  File "${SOURCE_PATH}\Settings\Languages\Arabic.xml"
  File "${SOURCE_PATH}\Settings\Languages\Bulgarian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Chinese (Simplified).xml"
  File "${SOURCE_PATH}\Settings\Languages\Chinese (Traditional).xml"
  File "${SOURCE_PATH}\Settings\Languages\Czech.xml"
  File "${SOURCE_PATH}\Settings\Languages\Danish.xml"
  File "${SOURCE_PATH}\Settings\Languages\Dutch.xml"
  File "${SOURCE_PATH}\Settings\Languages\English.xml"
  File "${SOURCE_PATH}\Settings\Languages\Finnish.xml"
  File "${SOURCE_PATH}\Settings\Languages\French.xml"
  File "${SOURCE_PATH}\Settings\Languages\German.xml"
  File "${SOURCE_PATH}\Settings\Languages\Greek.xml"
  File "${SOURCE_PATH}\Settings\Languages\Hebrew.xml"
  File "${SOURCE_PATH}\Settings\Languages\Hungarian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Italian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Japanese.xml"
  File "${SOURCE_PATH}\Settings\Languages\Korean.xml"
  File "${SOURCE_PATH}\Settings\Languages\Latvian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Macedonian (Latin).xml"
  File "${SOURCE_PATH}\Settings\Languages\Persian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Polish.xml"
  File "${SOURCE_PATH}\Settings\Languages\Portuguese.xml"
  File "${SOURCE_PATH}\Settings\Languages\PortugueseBrazil.xml"
  File "${SOURCE_PATH}\Settings\Languages\Russian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Serbian (Cyrilic).xml"
  File "${SOURCE_PATH}\Settings\Languages\Serbian (Latin).xml"
  File "${SOURCE_PATH}\Settings\Languages\Slovak.xml"
  File "${SOURCE_PATH}\Settings\Languages\Slovenian.xml"
  File "${SOURCE_PATH}\Settings\Languages\Spanish (Spain).xml"
  File "${SOURCE_PATH}\Settings\Languages\Spanish.xml"
  File "${SOURCE_PATH}\Settings\Languages\Swedish.xml"
  File "${SOURCE_PATH}\Settings\Languages\Thai.xml"
  File "${SOURCE_PATH}\Settings\Languages\Turkish.xml"
  File "${SOURCE_PATH}\Settings\Languages\Ukrainian.xml"
  File "${SOURCE_PATH}\Settings\Languages\_For translators - ReadMe.txt"
  SetOutPath "$INSTDIR\Settings\NVIDIA"
  File "${SOURCE_PATH}\Settings\NVIDIA\classroot.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\clsidleftover.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\clsidleftoverGFE.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\clsidleftoverNVB.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\driverfiles.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\gfedriverfiles.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\gfedriverfiles.cfg.bak"
  File "${SOURCE_PATH}\Settings\NVIDIA\gfeservice.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\interface.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\interfaceGFE.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\nvbdriverfiles.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\nvbservice.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\packages.cfg"
  File "${SOURCE_PATH}\Settings\NVIDIA\services.cfg"
  SetOutPath "$INSTDIR\Settings\REALTEK"
  File "${SOURCE_PATH}\Settings\REALTEK\classroot.cfg"
  File "${SOURCE_PATH}\Settings\REALTEK\clsidleftover.cfg"
  File "${SOURCE_PATH}\Settings\REALTEK\driverfiles.cfg"
  File "${SOURCE_PATH}\Settings\REALTEK\packages.cfg"
  File "${SOURCE_PATH}\Settings\REALTEK\services.cfg"

  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0 #< conv to DWORD
  ;WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "EstimatedSize" "$0"

SectionEnd

Section -AdditionalIcons
  SetOutPath $INSTDIR
    
  ; Resolve the language strings before creating shortcuts
  StrCpy $0 $(STR_01) ; "Web site"
  StrCpy $1 $(STR_02) ; "Uninstall"
  
  WriteIniStr "$INSTDIR\${PRODUCT_NAME}.url" "InternetShortcut" "URL" "${PRODUCT_WEB_SITE}"
  CreateShortCut "$SMPROGRAMS\Display Driver Uninstaller\$0.lnk" "$INSTDIR\${PRODUCT_NAME}.url"
  CreateShortCut "$SMPROGRAMS\Display Driver Uninstaller\$1.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\Display Driver Uninstaller.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName"     "${PRODUCT_NAME}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon"     "$INSTDIR\Display Driver Uninstaller.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion"  "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout"    "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher"       "${PRODUCT_PUBLISHER}"
  WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "EstimatedSize" "$0"
SectionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(STR_03)"
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "$(STR_04)" IDYES +2
  Abort

FunctionEnd

Section Uninstall
  ; Resolve the language strings before creating shortcuts
  StrCpy $0 $(STR_01) ; "Web site"
  StrCpy $1 $(STR_02) ; "Uninstall"
  
  Delete "$INSTDIR\${PRODUCT_NAME}.url"
  Delete "$INSTDIR\uninst.exe"
  Delete "$INSTDIR\Settings\REALTEK\services.cfg"
  Delete "$INSTDIR\Settings\REALTEK\packages.cfg"
  Delete "$INSTDIR\Settings\REALTEK\driverfiles.cfg"
  Delete "$INSTDIR\Settings\REALTEK\clsidleftover.cfg"
  Delete "$INSTDIR\Settings\REALTEK\classroot.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\services.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\packages.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\nvbservice.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\nvbdriverfiles.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\interfaceGFE.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\interface.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\gfeservice.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\gfedriverfiles.cfg.bak"
  Delete "$INSTDIR\Settings\NVIDIA\gfedriverfiles.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\driverfiles.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\clsidleftoverGFE.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\clsidleftoverNVB.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\clsidleftover.cfg"
  Delete "$INSTDIR\Settings\NVIDIA\classroot.cfg"
  Delete "$INSTDIR\Settings\Languages\_For translators - ReadMe.txt"
  Delete "$INSTDIR\Settings\Languages\Ukrainian.xml"
  Delete "$INSTDIR\Settings\Languages\Turkish.xml"
  Delete "$INSTDIR\Settings\Languages\Thai.xml"
  Delete "$INSTDIR\Settings\Languages\Swedish.xml"
  Delete "$INSTDIR\Settings\Languages\Spanish.xml"
  Delete "$INSTDIR\Settings\Languages\Spanish (Spain).xml"
  Delete "$INSTDIR\Settings\Languages\Slovenian.xml"
  Delete "$INSTDIR\Settings\Languages\Slovak.xml"
  Delete "$INSTDIR\Settings\Languages\Serbian (Latin).xml"
  Delete "$INSTDIR\Settings\Languages\Serbian (Cyrilic).xml"
  Delete "$INSTDIR\Settings\Languages\Russian.xml"
  Delete "$INSTDIR\Settings\Languages\PortugueseBrazil.xml"
  Delete "$INSTDIR\Settings\Languages\Portuguese.xml"
  Delete "$INSTDIR\Settings\Languages\Polish.xml"
  Delete "$INSTDIR\Settings\Languages\Persian.xml"
  Delete "$INSTDIR\Settings\Languages\Macedonian (Latin).xml"
  Delete "$INSTDIR\Settings\Languages\Latvian.xml"
  Delete "$INSTDIR\Settings\Languages\Korean.xml"
  Delete "$INSTDIR\Settings\Languages\Japanese.xml"
  Delete "$INSTDIR\Settings\Languages\Italian.xml"
  Delete "$INSTDIR\Settings\Languages\Hungarian.xml"
  Delete "$INSTDIR\Settings\Languages\Hebrew.xml"
  Delete "$INSTDIR\Settings\Languages\Greek.xml"
  Delete "$INSTDIR\Settings\Languages\German.xml"
  Delete "$INSTDIR\Settings\Languages\French.xml"
  Delete "$INSTDIR\Settings\Languages\Finnish.xml"
  Delete "$INSTDIR\Settings\Languages\English.xml"
  Delete "$INSTDIR\Settings\Languages\Dutch.xml"
  Delete "$INSTDIR\Settings\Languages\Danish.xml"
  Delete "$INSTDIR\Settings\Languages\Czech.xml"
  Delete "$INSTDIR\Settings\Languages\Chinese (Traditional).xml"
  Delete "$INSTDIR\Settings\Languages\Chinese (Simplified).xml"
  Delete "$INSTDIR\Settings\Languages\Bulgarian.xml"
  Delete "$INSTDIR\Settings\Languages\Arabic.xml"
  Delete "$INSTDIR\Settings\INTEL\services.cfg"
  Delete "$INSTDIR\Settings\INTEL\packages.cfg"
  Delete "$INSTDIR\Settings\INTEL\interface.cfg"
  Delete "$INSTDIR\Settings\INTEL\driverfiles.cfg"
  Delete "$INSTDIR\Settings\INTEL\clsidleftover.cfg"
  Delete "$INSTDIR\Settings\INTEL\classroot.cfg"
  Delete "$INSTDIR\Settings\AMD\services.cfg"
  Delete "$INSTDIR\Settings\AMD\packages.cfg"
  Delete "$INSTDIR\Settings\AMD\interface.cfg"
  Delete "$INSTDIR\Settings\AMD\driverfilesKMPFD.cfg.bak"
  Delete "$INSTDIR\Settings\AMD\driverfilesKMPFD.cfg"
  Delete "$INSTDIR\Settings\AMD\driverfilesKMAFD.cfg"
  Delete "$INSTDIR\Settings\AMD\driverfiles.cfg"
  Delete "$INSTDIR\Settings\AMD\clsidleftover.cfg"
  Delete "$INSTDIR\Settings\AMD\classroot.cfg"
  Delete "$INSTDIR\Readme.txt"
  Delete "$INSTDIR\Licence.txt"
  Delete "$INSTDIR\Issues and solutions.txt"
  Delete "$INSTDIR\Display Driver Uninstaller.pdb"
  Delete "$INSTDIR\Display Driver Uninstaller.exe"

  Delete "$SMPROGRAMS\Display Driver Uninstaller\$0.lnk"
  Delete "$SMPROGRAMS\Display Driver Uninstaller\$1.lnk"
  Delete "$DESKTOP\Display Driver Uninstaller.lnk"
  Delete "$SMPROGRAMS\Display Driver Uninstaller\Display Driver Uninstaller.lnk"

  RMDir /r "$SMPROGRAMS\Display Driver Uninstaller"
  RMDir "$INSTDIR\Settings\REALTEK"
  RMDir "$INSTDIR\Settings\NVIDIA"
  RMDir "$INSTDIR\Settings\Languages"
  RMDir "$INSTDIR\Settings\INTEL"
  RMDir "$INSTDIR\Settings\AMD"
  RMDir /r  "$INSTDIR\Settings"

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd

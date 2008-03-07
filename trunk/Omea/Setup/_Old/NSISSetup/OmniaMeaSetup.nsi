!verbose 2

;!define OBFUSC_LOCATION "C:\Bin"
;!define BINARIES_LOCATION "C:\Bin"

!include "defaults.nsi"
!include "dotNETInstalled.nsi"
!include "Registry.nsi"
!include "version.nsi"
!include "CmdLineParams.nsi"
;!include "AcrobatReaderInstalled.nsi"

SetCompressor lzma

;------------------------------------------------------------------------------
; include "Modern User Interface"
;------------------------------------------------------------------------------
!include "MUI.nsh"

ReserveFile "DeleteBase.ini"
ReserveFile "Desktop.ini"
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS

!define MUI_ICON Install.ico
!define MUI_UNICON Install.ico

Function .onInit
  Call GetParameters
  Pop $R0
  StrCmp $R0 "-silent" set_silent
  StrCmp $R0 "/silent" set_silent
  goto skip_silent

set_silent:
  SetSilent silent
skip_silent:

functionEnd

;------------------------------------------------------------------------------
; on GUI initialization installer checks whether Omnia Mea is already installed
; and whether .NET Framework is present on the computer
;------------------------------------------------------------------------------

!define MUI_CUSTOMFUNCTION_GUIINIT GUIInit

Var baseRegKey

Function GUIInit

  Push $0
  Push $1
  Push $2
  Push $3

; is OmniaMea installed?
  StrCpy $0 "HKCU"
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 ""
  Call OMReadRegStr
  StrCmp $3 "" "" check_existence
  StrCpy $0 "HKLM"
  Call OMReadRegStr
  StrCmp $3 "" check_Version
check_existence:
  StrCpy $INSTDIR $3
!ifdef READER
  IfFileExists $3\OmeaReader.exe check_Version check_IsLaunched
!else
  IfFileExists $3\OmeaPro.exe check_Version check_IsLaunched
!endif
check_Version:
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 "VersionMajor"
  Call OMReadRegStr
  StrCmp $3 "" check_IsLaunched
  IntCmpU $3 ${MUI_VERSION_MAJOR} check_version_minor check_IsLaunched ask_Install_Over
check_version_minor:
  StrCpy $2 "VersionMinor"
  Call OMReadRegStr
  StrCmp $3 "" check_IsLaunched
  IntCmpU $3 ${MUI_VERSION_MINOR} ask_Install_Over check_IsLaunched

ask_Install_Over:
  MessageBox MB_YESNO|MB_ICONQUESTION "Current or newer version of ${MUI_PRODUCT} is already installed. Do you wish to \
             continue?" IDYES check_IsLaunched IDNO exit_installer

; is OmniaMea launched?
check_IsLaunched:
  System::Call 'kernel32::CreateMutexA(i 0, i 0, t "OmniaMeaMutex") i .r1 ?e'
  Pop $R0
  StrCmp $R0 0 +3
    MessageBox MB_OK|MB_ICONEXCLAMATION "Omea (Reader) or its installation program is running. You need to close it."
    Abort

; check presence of .NET versions: 1.1 or 2.0
  StrCpy $0 "v1.1"
  Call IsDotNETVersionInstalled
  StrCmp $0 1 found.NETFramework
  StrCpy $0 "v2.0"
  Call IsDotNETVersionInstalled  
  StrCmp $0 1 found.NETFramework
  MessageBox MB_YESNO ".NET Framework version 1.1 or later is required, but not installed. \
             Do you wish to continue?" IDYES found.NETFramework IDNO exit_installer

exit_installer:
  Abort

found.NETFramework:

!ifdef READER
!else
  StrCpy $0 "HKCU"
  StrCpy $1 "Software\${MANUFACTURER}\Omea Reader"
  StrCpy $2 ""
  Call OMReadRegStr
  StrCmp $3 "" "" check_existence1
  StrCpy $0 "HKLM"
  Call OMReadRegStr
  StrCmp $3 "" _return
check_existence1:
  IfFileExists $3\OmeaReader.exe ask_replace _return
ask_replace:
  MessageBox MB_YESNO "You have Omea Reader installed. Would you like to uninstall it (the database will be preserved)?" IDYES _replace IDNO _return
_replace:
  StrCpy $2 "leavedb"
  Call OMWriteRegStr
  ExecWait "$3\Uninstall.exe"
!endif

_return:
  Pop $3
  Pop $2
  Pop $1
  Pop $0
  !insertmacro MUI_INSTALLOPTIONS_EXTRACT "Desktop.ini"

FunctionEnd

;------------------------------------------------------------------------------
; Variables
;------------------------------------------------------------------------------
  Var STARTMENU_FOLDER


;------------------------------------------------------------------------------
; configuration
;------------------------------------------------------------------------------

!insertmacro MUI_PAGE_WELCOME

!ifdef READER
  !insertmacro MUI_PAGE_LICENSE "${REFERENCES_LOCATION}\Omea Reader License.txt"
!else
  !insertmacro MUI_PAGE_LICENSE "${REFERENCES_LOCATION}\Omea License.txt"
!endif

!define MUI_COMPONENTSPAGE_SMALLDESC

!ifndef READER
  !insertmacro MUI_PAGE_COMPONENTS
!endif

!insertmacro MUI_PAGE_DIRECTORY
Page custom ConfirmDesktopShortcut
  !define MUI_STARTMENUPAGE_NODISABLE
  !define MUI_STARTMENUPAGE_DEFAULTFOLDER "JetBrains ${MUI_PRODUCT}"
!insertmacro MUI_PAGE_STARTMENU Application $STARTMENU_FOLDER
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_INSTFILES
;  !define MUI_FINISHPAGE_RUN_NOTCHECKED
!ifdef READER
  !define MUI_FINISHPAGE_RUN $INSTDIR\OmeaReaderLauncher.exe
!else
  !define MUI_FINISHPAGE_RUN $INSTDIR\OmeaLauncher.exe
!endif
!insertmacro MUI_PAGE_FINISH

!define MUI_UNINSTALLER
!insertmacro MUI_UNPAGE_CONFIRM
UninstPage custom un.ConfirmDatabaseDeletion
!insertmacro MUI_UNPAGE_INSTFILES

!ifdef READER
   OutFile "${BINARIES_LOCATION}\Setup\OmeaReaderSetup.exe"
!else
   OutFile "${BINARIES_LOCATION}\Setup\OmeaSetup.exe"
!endif

InstallDir "$PROGRAMFILES\${MANUFACTURER}\${MUI_PRODUCT}"
!define MUI_BRANDINGTEXT " "


;------------------------------------------------------------------------------
; languages
;------------------------------------------------------------------------------
!insertmacro MUI_LANGUAGE "English"


;------------------------------------------------------------------------------
; Installer sections
;------------------------------------------------------------------------------
Section "Runtime Files" CopyRuntime

  StrCpy $baseRegKey "HKCU"
  Call GetParameters
  Pop $R0
  StrCmp $R0 "-silent" continue_for_current_user
  StrCmp $R0 "/silent" continue_for_current_user

  !insertmacro MUI_INSTALLOPTIONS_READ $R2 "Desktop.ini" "Field 3" "State"
  StrCmp $R2 1 continue_for_current_user
  SetShellVarContext all
  StrCpy $baseRegKey "HKLM"

continue_for_current_user:

; create shortcuts

  !insertmacro MUI_INSTALLOPTIONS_READ $R2 "Desktop.ini" "Field 1" "State"
  StrCmp $R2 1 "" skip_desktop_shortcut
  !ifdef READER
    CreateShortCut "$DESKTOP\${MUI_PRODUCT}.lnk" \
                   "$INSTDIR\OmeaReaderLauncher.exe" "" "" "" SW_SHOWNORMAL
  !else
    CreateShortCut "$DESKTOP\${MUI_PRODUCT}.lnk" \
                   "$INSTDIR\OmeaLauncher.exe" "" "" "" SW_SHOWNORMAL
!endif  
skip_desktop_shortcut:
  !insertmacro MUI_INSTALLOPTIONS_READ $R2 "Desktop.ini" "Field 2" "State"
  StrCmp $R2 1 "" skip_quicklaunch_shortcut
  !ifdef READER
    CreateShortCut "$QUICKLAUNCH\${MUI_PRODUCT}.lnk" \
                   "$INSTDIR\OmeaReaderLauncher.exe" "" "" "" SW_SHOWNORMAL
  !else
    CreateShortCut "$QUICKLAUNCH\${MUI_PRODUCT}.lnk" \
                   "$INSTDIR\OmeaLauncher.exe" "" "" "" SW_SHOWNORMAL
  !endif
skip_quicklaunch_shortcut:

!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
; $STARTMENU_FOLDER stores name of Omnia Mea folder in Start Menu,
; save it name in the "MenuFolder" RegValue
  CreateDirectory "$SMPROGRAMS\$STARTMENU_FOLDER"
!ifdef READER
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\${MUI_PRODUCT}.lnk" \
                 "$INSTDIR\OmeaReaderLauncher.exe" "" "" "" SW_SHOWNORMAL
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\Help.lnk" \
                 "$INSTDIR\OmeaReaderHelp.chm" "" "" "" SW_SHOWNORMAL
!else
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\${MUI_PRODUCT}.lnk" \
                 "$INSTDIR\OmeaLauncher.exe" "" "" "" SW_SHOWNORMAL
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\Help.lnk" \
                 "$INSTDIR\Help.chm" "" "" "" SW_SHOWNORMAL
!endif
  CreateShortCut "$SMPROGRAMS\$STARTMENU_FOLDER\Uninstall.lnk" \
                 "$INSTDIR\Uninstall.exe"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 "MenuFolder"
  StrCpy $3 "$STARTMENU_FOLDER"
  Call OMWriteRegStr
!insertmacro MUI_STARTMENU_WRITE_END


; readonly section
  SectionIn RO

; dictionaries
  SetOutPath "$INSTDIR\Data"
  File "${DATA_LOCATION}\english.bin"
  File "${DATA_LOCATION}\oxford.lex"
  File "${DATA_LOCATION}\derivates.dat"
  File "${DATA_LOCATION}\unchangeables.dat"

  SetOutPath "$INSTDIR"

!ifdef READER
  File "${OBFUSC_LOCATION}\OmeaReader.exe"
  File "${REFERENCES_LOCATION}\OmeaReader.exe.manifest"
  File "${BINARIES_LOCATION}\OmeaReaderLauncher.exe"
!else
  File "${OBFUSC_LOCATION}\OmeaPro.exe"
  File "${REFERENCES_LOCATION}\OmeaPro.exe.manifest"
  File "${BINARIES_LOCATION}\OmeaLauncher.exe"
!endif

  File "${OBFUSC_LOCATION}\ResourceStore.dll"
  File "${OBFUSC_LOCATION}\DBUtils.dll"
  File "${BINARIES_LOCATION}\DBIndex.dll"
  File "${OBFUSC_LOCATION}\GUIControls.dll"
  File "${OBFUSC_LOCATION}\JetListView.dll"
  File "${OBFUSC_LOCATION}\TextIndex.dll"
  File "${OBFUSC_LOCATION}\OmniaMeaBase.dll"
  File "${BINARIES_LOCATION}\Org.Mentalis.Security.dll"
  File "${BINARIES_LOCATION}\OpenAPI.dll"
  File "${BINARIES_LOCATION}\PicoCore.dll"
  File "${OBFUSC_LOCATION}\ContactsPlugin.dll"
  File "${OBFUSC_LOCATION}\HTMLPlugin.dll"
  File "${OBFUSC_LOCATION}\Favorites.dll"
  File "${OBFUSC_LOCATION}\Pictures.dll"
  File "${OBFUSC_LOCATION}\ResourceTools.dll"

;  ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" CurrentVersion
;  IfErrors 0 lbl_iebrowser_winnt
;  File /oname=CIEBrowser.dll "${BINARIES_LOCATION}\CIEBrowserA.dll"
;  goto lbl_iebrowser_continue
;lbl_iebrowser_winnt:
;  File "${BINARIES_LOCATION}\CIEBrowser.dll"
;lbl_iebrowser_continue:

;  File "${BINARIES_LOCATION}\IEBrowser.dll"
;  File "${BINARIES_LOCATION}\AxIEBrowser.dll"
  File "${BINARIES_LOCATION}\MshtmlSiteW.dll"
  File "${BINARIES_LOCATION}\MshtmlBrowserControl.dll"
  File "${BINARIES_LOCATION}\SpHeader.dll"
  File "${OBFUSC_LOCATION}\DBRepair.exe"
  File "${OBFUSC_LOCATION}\JetBrainsShared.dll"
  File "${OBFUSC_LOCATION}\DebugPlugin.dll"
  File "${REFERENCES_LOCATION}\Interop.Shell32.dll"
  File "${REFERENCES_LOCATION}\Microsoft.Web.Services.dll"
  File "..\..\JetBrainsDotNet\Libraries\CookComputing.XmlRpc.dll"
  File "${REFERENCES_LOCATION}\ICSharpCode.SharpZipLib.dll"
  File "${REFERENCES_LOCATION}\msvcr71.dll"
  File "${REFERENCES_LOCATION}\System.Windows.Forms.Themes.dll"
  File "${REFERENCES_LOCATION}\PicoContainer.dll"
!ifdef READER
  File "..\..\Doc\Help\OmeaReaderHelp.chm"
  File "${REFERENCES_LOCATION}\Omea Reader License.txt"
!else
  File "..\..\Doc\Help\Help.chm"
  File "${REFERENCES_LOCATION}\Omea License.txt"
!endif
  File "..\..\Doc\Help\HHActiveX.dll"
  File "..\..\Doc\Help\*.swf"
  File "${REFERENCES_LOCATION}\Third-Party Software.txt"

!ifndef READER
  File "${OBFUSC_LOCATION}\FilePlugin.dll"
  File "${OBFUSC_LOCATION}\Tasks.dll"
  File "${SOUNDS_LOCATION}\reminder.wav"
  File "${REFERENCES_LOCATION}\office.dll"
!endif

; install HTTP extra config if either .NET SP1 tech preview or .NET SP1 final is installed
  ReadRegDWORD $R2 HKLM "Software\Microsoft\Updates\.NETFramework\1.1\S840129" "Installed"
  ReadRegDWORD $R3 HKLM "Software\Microsoft\Updates\.NETFramework\1.1\S867460" "Installed"
  IntOp $R4 $R2 + $R3
  IntCmp $R4 1 install_config skip_config install_config

install_config:
!ifdef READER
  File "${REFERENCES_LOCATION}\OmeaReader.exe.config"
!else
  File "${REFERENCES_LOCATION}\OmeaPro.exe.config"
!endif

skip_config:

  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 ""
  StrCpy $3 "$INSTDIR"
  Call OMWriteRegStr
  StrCpy $2 "VersionMajor"
  StrCpy $3 ${MUI_VERSION_MAJOR}
  Call OMWriteRegStr
  StrCpy $2 "VersionMinor"
  StrCpy $3 ${MUI_VERSION_MINOR}
  Call OMWriteRegStr

!ifndef READER
;  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\License"
;  StrCpy $2 "Username"
;  StrCpy $3 "Public Beta User"
;  Call OMWriteRegStr
;  StrCpy $2 "Company"
;  StrCpy $3 ""
;  Call OMWriteRegStr
;  StrCpy $2 "License"
;  StrCpy $3 "d+zW2UeE9+vyWCRUjsXgoQwdl9RoGs5d"
;  Call OMWriteRegStr
!endif

  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "Outlook"
  Call OMDeleteRegValue
  StrCpy $2 "ICQ"
  Call OMDeleteRegValue
  StrCpy $2 "News"
  Call OMDeleteRegValue
  StrCpy $2 "Miranda"
  Call OMDeleteRegValue
  StrCpy $2 "RSS"
  Call OMDeleteRegValue
  StrCpy $2 "MSWord"
  Call OMDeleteRegValue
  StrCpy $2 "MSPowerPoint"
  Call OMDeleteRegValue
  StrCpy $2 "PDF"
  Call OMDeleteRegValue

  StrCpy $2 "Contacts"
  StrCpy $3 "$INSTDIR\ContactsPlugin.dll"
  Call OMWriteRegstr
  StrCpy $2 "HTML"
  StrCpy $3 "$INSTDIR\HTMLPlugin.dll"
  Call OMWriteRegstr
  StrCpy $2 "Favorites"
  StrCpy $3 "$INSTDIR\Favorites.dll"
  Call OMWriteRegstr

!ifndef READER
  StrCpy $2 "Files"
  StrCpy $3 "$INSTDIR\FilePlugin.dll"
  Call OMWriteRegstr
  StrCpy $2 "Tasks"
  StrCpy $3 "$INSTDIR\Tasks.dll"
  Call OMWriteRegstr
!endif

  StrCpy $2 "Pictures"
  StrCpy $3 "$INSTDIR\Pictures.dll"
  Call OMWriteRegstr

; write uninstaller & add it to add/remove programs in control panel
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MUI_PRODUCT}" \
              "DisplayName" "${MANUFACTURER} ${MUI_PRODUCT}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MUI_PRODUCT}" \
              "UninstallString" "$INSTDIR\Uninstall.exe"

;  ExecWait "regsvr32 /s $\"$INSTDIR\CIEBrowser.dll$\"" $R2
  ExecWait "regsvr32 /s $\"$INSTDIR\HHActiveX.dll$\"" $R2
  ExecWait "regsvr32 /s $\"$INSTDIR\MshtmlSiteW.dll$\"" $R2

SectionEnd

;------------------------------------------------------------------------------
; Outlook plugin
;------------------------------------------------------------------------------

!ifndef READER

Section "Outlook Plugin" CopyOutlook
  SetOutPath "$INSTDIR"

  File "${OBFUSC_LOCATION}\OutlookPlugin.dll"
  File "${BINARIES_LOCATION}\EMAPILib.dll"
  File "${REFERENCES_LOCATION}\Interop.MAPI.dll"
  File "${REFERENCES_LOCATION}\Interop.Outlook.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "Outlook"
  StrCpy $3 "$INSTDIR\OutlookPlugin.dll"
  Call OMWriteRegStr
SectionEnd

!endif

;------------------------------------------------------------------------------
; NNTP plugin
;------------------------------------------------------------------------------
Section "NNTP Plugin" CopyNNTP
  SetOutPath "$INSTDIR"
  File "${OBFUSC_LOCATION}\NntpPlugin.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "News"
  StrCpy $3 "$INSTDIR\NntpPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; RSS plugin
;------------------------------------------------------------------------------
Section "RSS Plugin" CopyRSS
  SetOutPath "$INSTDIR"
  File "${REFERENCES_LOCATION}\blogExtension.dll"
  File "${OBFUSC_LOCATION}\RSSPlugin.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "RSS"
  StrCpy $3 "$INSTDIR\RSSPlugin.dll"
  Call OMWriteRegStr
SectionEnd

Section "Mozilla/Firefox Integration" CopyMozilla
  SetOutPath "$INSTDIR"
  File "${BINARIES_LOCATION}\omeaconnector.xpi"
  File "${BINARIES_LOCATION}\InstallOmeaConnector.html"
 
  Call GetParameters
  Pop $R0
  StrCmp $R0 "-silent" skip_mozilla_installconnector
  StrCmp $R0 "/silent" skip_mozilla_installconnector

  ExecShell open "firefox.exe" "$\"$INSTDIR\InstallOmeaConnector.html$\""
  IfErrors 0 skip_mozilla_installconnector
  ExecShell open "mozilla.exe" "$\"$INSTDIR\InstallOmeaConnector.html$\""
skip_mozilla_installconnector:
SectionEnd

Section "Internet Explorer Integration" CopyIE
  ExecWait "regsvr32 /s /u $\"$INSTDIR\IexploreOmeaW.dll$\"" $R2
  SetOutPath "$INSTDIR"
  GetTempFileName $0 $INSTDIR
  Delete $0
  Rename "$INSTDIR\IexploreOmeaW.dll" $0
  Delete /REBOOTOK $0
  File "${BINARIES_LOCATION}\IexploreOmeaW.dll"
  ExecWait "regsvr32 /s $\"$INSTDIR\IexploreOmeaW.dll$\"" $R2
SectionEnd

;------------------------------------------------------------------------------
; Notes plugin
;------------------------------------------------------------------------------
Section "Notes Plugin" CopyNotes
  SetOutPath "$INSTDIR"
  File "${BINARIES_LOCATION}\NotesPlugin.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "Notes"
  StrCpy $3 "$INSTDIR\NotesPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; ICQ plugin
;------------------------------------------------------------------------------

!ifndef READER

Section "ICQ Plugin" CopyICQ
  SetOutPath "$INSTDIR"
  File "${OBFUSC_LOCATION}\ICQPlugin.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "ICQ"
  StrCpy $3 "$INSTDIR\ICQPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; Miranda plugin
;--------- ---------------------------------------------------------------------
Section "Miranda Plugin" CopyMiranda
  SetOutPath "$INSTDIR"
  File "${OBFUSC_LOCATION}\MirandaPlugin.dll"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "Miranda"
  StrCpy $3 "$INSTDIR\MirandaPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; Word plugin
;------------------------------------------------------------------------------
Section "MS Word Plugin" CopyWord
  SetOutPath "$INSTDIR"
  File "${OBFUSC_LOCATION}\WordDocPlugin.dll"
  File "${REFERENCES_LOCATION}\wvWare.exe"
  File "${REFERENCES_LOCATION}\libpng13.dll"
  File "${REFERENCES_LOCATION}\zlib1.dll"
  File "${REFERENCES_LOCATION}\wvHtml.xml"
  File "${REFERENCES_LOCATION}\wvText.xml"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "MSWord"
  StrCpy $3 "$INSTDIR\WordDocPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; Excel plugin
;------------------------------------------------------------------------------
Section "MS Excel Plugin" CopyExcel
  SetOutPath "$INSTDIR"
  File "${OBFUSC_LOCATION}\ExcelDocPlugin.dll"
  File "${REFERENCES_LOCATION}\xlhtml-w32.exe"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "MSExcel"
  StrCpy $3 "$INSTDIR\ExcelDocPlugin.dll"
  Call OMWriteRegStr
SectionEnd

;------------------------------------------------------------------------------
; PDF plugin
;------------------------------------------------------------------------------
Section "PDF Plugin" CopyPDF
  SetOutPath "$INSTDIR"
; Install PDFPlugin only if acrobat reader is installed
;  Call AcrobatReaderInstalled
;  Pop $0
;  StrCmp $0 1 found.Acrobat no.Acrobat
;found.Acrobat:
  File "${OBFUSC_LOCATION}\PDFPlugin.dll"
  File "${REFERENCES_LOCATION}\Interop.PdfLib.dll"
  File "${REFERENCES_LOCATION}\AxInterop.PdfLib.dll"
  File "${REFERENCES_LOCATION}\AcroPDFLib.dll"
  File "${REFERENCES_LOCATION}\AxAcroPDFLib.dll"
  File "${REFERENCES_LOCATION}\pdftotext.exe"
  SetOutPath "$INSTDIR\xpdf-3.00-win32"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\pdffonts.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\pdfimages.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\pdfinfo.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\pdftops.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\pdftotext.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\xpdfrc.txt"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\ANNOUNCE"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\CHANGES"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\COPYING"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\INSTALL"
  File "${REFERENCES_LOCATION}\xpdf-3.00-win32\README"
  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "PDF"
  StrCpy $3 "$INSTDIR\PDFPlugin.dll"
  Call OMWriteRegStr
;no.Acrobat:
SectionEnd

!endif

;------------------------------------------------------------------------------
; Descriptions of sections
;------------------------------------------------------------------------------
LangString DESC_CopyRuntime ${LANG_ENGLISH} "${MUI_PRODUCT} core binary files"
LangString DESC_CopyOutlook ${LANG_ENGLISH} "Processing e-mail messages and contacts"
LangString DESC_CopyNNTP ${LANG_ENGLISH} "Processing News messages"
LangString DESC_CopyRSS ${LANG_ENGLISH} "Processing RSS news feeds"
LangString DESC_CopyNotes ${LANG_ENGLISH} "Create and edit notes"
LangString DESC_CopyICQ ${LANG_ENGLISH} "Processing ICQ messages and contacts"
LangString DESC_CopyMiranda ${LANG_ENGLISH} "Processing Miranda IM messages and contacts"
LangString DESC_CopyWord ${LANG_ENGLISH} "Processing MS Word and .rtf files"
LangString DESC_CopyExcel ${LANG_ENGLISH} "Processing MS Excel files"
LangString DESC_CopyPDF ${LANG_ENGLISH} "Processing PDF files"
;LangString DESC_CopyIE ${LANG_ENGLISH} "Working with Omea resources from Internet Explorer"
LangString DESC_CopyMozilla ${LANG_ENGLISH} "Working with Omea resources from Mozilla or Firefox"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyRuntime} $(DESC_CopyRuntime)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyOutlook} $(DESC_CopyOutlook)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyNNTP} $(DESC_CopyNNTP)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyRSS} $(DESC_CopyRSS)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyNotes} $(DESC_CopyNotes)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyICQ} $(DESC_CopyICQ)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyMiranda} $(DESC_CopyMiranda)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyWord} $(DESC_CopyWord)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyExcel} $(DESC_CopyExcel)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyPDF} $(DESC_CopyPDF)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyIE} $(DESC_CopyIE)
  !insertmacro MUI_DESCRIPTION_TEXT ${CopyMozilla} $(DESC_CopyMozilla)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


;------------------------------------------------------------------------------
; custom install pages
;------------------------------------------------------------------------------

Function ConfirmDesktopShortcut
  !insertmacro MUI_HEADER_TEXT "Installation Options" "Configure your ${MUI_PRODUCT} installation"
  !insertmacro MUI_INSTALLOPTIONS_DISPLAY "Desktop.ini"
FunctionEnd


;------------------------------------------------------------------------------
; Uninstaller
;------------------------------------------------------------------------------

;------------------------------------------------------------------------------
; extract InstallOptions ini files
;------------------------------------------------------------------------------
Function un.onInit
!ifdef READER
  StrCpy $0 "HKCU"
  StrCpy $1 "Software\${MANUFACTURER}\Omea Reader"
  StrCpy $2 "leavedb"
  Call un.OMReadRegStr
  StrCmp $3 "" "" _upgrade
  StrCpy $0 "HKLM"
  Call un.OMReadRegStr
  StrCmp $3 "" _continue
_upgrade:
  StrCpy $R5 "leavedb"
  Call un.OMDeleteRegValue
  goto _return
!endif
_continue:

  !insertmacro MUI_INSTALLOPTIONS_EXTRACT "DeleteBase.ini"

; is OmniaMea launched?
check_IsLaunched:
  System::Call 'kernel32::CreateMutexA(i 0, i 0, t "OmniaMeaMutex") i .r1 ?e'
  Pop $R0
  StrCmp $R0 0 +3
    MessageBox MB_OK|MB_ICONEXCLAMATION "Omea (Reader) or its installation program is running. You need to close it."
    Abort
_return:
FunctionEnd


;------------------------------------------------------------------------------
; custom uninstall functions
;------------------------------------------------------------------------------

Function un.OMReadRegStr
  StrCmp $0 "HKCU" hkcu
    ReadRegStr $3 HKLM $1 $2
    goto done
hkcu:
    ReadRegStr $3 HKCU $1 $2
done:
FunctionEnd

Function un.OMDeleteRegValue
  StrCmp $0 "HKCU" hkcu
    DeleteRegValue HKLM $1 $2
    goto done
hkcu:
    DeleteRegValue HKCU $1 $2
done:
FunctionEnd

Function un.OMDeleteRegKeyIfEmpty
  StrCmp $0 "HKCU" hkcu
    DeleteRegKey /ifempty HKLM $1
    goto done
hkcu:
    DeleteRegKey /ifempty HKCU $1
done:
FunctionEnd

Function un.OMDeleteRegKey
  StrCmp $0 "HKCU" hkcu
    DeleteRegKey /ifempty HKLM $1
    goto done
hkcu:
    DeleteRegKey /ifempty HKCU $1
done:
FunctionEnd

Function un.OMWriteRegStr
  StrCmp $0 "HKCU" hkcu
    WriteRegStr HKLM $1 $2 $3
    goto done
hkcu:
    WriteRegStr HKCU $1 $2 $3
done:
FunctionEnd


;------------------------------------------------------------------------------
; custom uninstall pages
;------------------------------------------------------------------------------

Function un.ConfirmDatabaseDeletion
  !insertmacro MUI_HEADER_TEXT "Uninstall ${MUI_PRODUCT}" "Remove ${MUI_PRODUCT} from your computer"
  !insertmacro MUI_INSTALLOPTIONS_DISPLAY "DeleteBase.ini"
FunctionEnd

Section "Uninstall"

!ifdef READER
  StrCmp $R5 "leavedb" skip_database_deletion
!endif

  StrCpy $baseRegKey "HKCU"

  !insertmacro MUI_INSTALLOPTIONS_READ $R2 "DeleteBase.ini" "Field 2" "State"
  StrCmp $R2 1 "" skip_database_deletion
  ExecWait '"$INSTDIR\DBRepair.exe" /deleteindex-ignoremutex' $R2
skip_database_deletion:

  ExecWait "regsvr32 /s /u $\"$INSTDIR\IexploreOmeaW.dll$\"" $R2
  ExecWait "regsvr32 /s /u $\"$INSTDIR\MshtmlSiteW.dll$\"" $R2
  RMDir /r "$INSTDIR\Data"
  RMDir /r "$INSTDIRxpdf-3.00-win32"

!ifdef READER
  Delete "$INSTDIR\OmeaReader.exe"
  Delete "$INSTDIR\OmeaReader.exe.manifest"
  Delete "$INSTDIR\OmeaReaderLauncher.exe"
!else
  Delete "$INSTDIR\OmeaPro.exe"
  Delete "$INSTDIR\OmeaPro.exe.manifest"
  Delete "$INSTDIR\OmeaLauncher.exe"
!endif
  Delete "$INSTDIR\ResourceStore.dll"
  Delete "$INSTDIR\DBUtils.dll"
  Delete "$INSTDIR\DBIndex.dll"
  Delete "$INSTDIR\GUIControls.dll"
  Delete "$INSTDIR\TextIndex.dll"
  Delete "$INSTDIR\OmniaMeaBase.dll"
  Delete "$INSTDIR\OpenAPI.dll"
  Delete "$INSTDIR\ContactsPlugin.dll"
  Delete "$INSTDIR\HTMLPlugin.dll"
  Delete "$INSTDIR\Favorites.dll"
  Delete "$INSTDIR\Pictures.dll"
  Delete "$INSTDIR\ResourceTools.dll"
;  Delete "$INSTDIR\CIEBrowser.dll"
;  Delete "$INSTDIR\IEBrowser.dll"
;  Delete "$INSTDIR\AxIEBrowser.dll"
  Delete "$INSTDIR\DBRepair.exe"
  Delete "$INSTDIR\JetBrainsShared.dll"
  Delete "$INSTDIR\DebugPlugin.dll"
  Delete "$INSTDIR\DeleteIndex.bat"
  Delete "$INSTDIR\DeleteTextIndex.bat"
  Delete "$INSTDIR\Interop.Shell32.dll"
  Delete "$INSTDIR\Microsoft.Web.Services.dll"
  Delete "$INSTDIR\CookComputing.XmlRpc.dll"
  Delete "$INSTDIR\ICSharpCode.SharpZipLib.dll"
  Delete "$INSTDIR\msvcr71.dll"
!ifdef READER
  Delete "$INSTDIR\OmeaReaderHelp.chm"
  Delete "$INSTDIR\Omea Reader License.txt"
!else
  Delete "$INSTDIR\Help.chm"
  Delete "$INSTDIR\Omea License.txt"
!endif
  Delete "$INSTDIR\HHActiveX.dll"
  Delete "$INSTDIR\*.swf"
  Delete "$INSTDIR\Third-Party Software.txt"
!ifndef READER
  Delete "$INSTDIR\FilePlugin.dll"
  Delete "$INSTDIR\Tasks.dll"
  Delete "$INSTDIR\reminder.wav"
  Delete "$INSTDIR\office.dll"
  Delete "$INSTDIR\OutlookPlugin.dll"
  Delete "$INSTDIR\EMAPILib.dll"
  Delete "$INSTDIR\Interop.MAPI.dll"
  Delete "$INSTDIR\Interop.Outlook.dll"
  Delete "$INSTDIR\ICQPlugin.dll"
  Delete "$INSTDIR\MirandaPlugin.dll"
  Delete "$INSTDIR\WordDocPlugin.dll"
  Delete "$INSTDIR\wvWare.exe"
  Delete "$INSTDIR\libpng13.dll"
  Delete "$INSTDIR\zlib1.dll"
  Delete "$INSTDIR\wvHtml.xml"
  Delete "$INSTDIR\wvText.xml"
  Delete "$INSTDIR\PDFPlugin.dll"
  Delete "$INSTDIR\pdftotext.exe"
!endif
!ifdef READER
  Delete "$INSTDIR\OmeaReader.exe.config"
!else
  Delete "$INSTDIR\OmeaPro.exe.config"
!endif
  Delete "$INSTDIR\NntpPlugin.dll"
  Delete "$INSTDIR\blogExtension.dll"
  Delete "$INSTDIR\RSSPlugin.dll"
  Delete "$INSTDIR\NotesPlugin.dll"
  Delete "$INSTDIR\System.Windows.Forms.Themes.dll"
  Delete "$INSTDIR\SpHeader.dll"
  Delete "$INSTDIR\PicoCore.dll"
  Delete "$INSTDIR\PicoContainer.dll"
  Delete "$INSTDIR\omeaconnector.xpi"
  Delete "$INSTDIR\IexploreOmeaW.dll"
  Delete "$INSTDIR\MshtmlSiteW.dll"
  Delete "$INSTDIR\MshtmlBrowserControl.dll"
  Delete "$INSTDIR\AcroPDFLib.dll"
  Delete "$INSTDIR\AxAcroPDFLib.dll"
  Delete "$INSTDIR\Interop.PdfLib.dll"
  Delete "$INSTDIR\AxInterop.PdfLib.dll"
  Delete "$INSTDIR\ExcelDocPlugin.dll"
  Delete "$INSTDIR\xlhtml-w32.exe"
  Delete "$INSTDIR\JetListView.dll"
  Delete "$INSTDIR\Org.Mentalis.Security.dll"
  Delete "$INSTDIR\xpdf-3.00-win32\pdffonts.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\pdfimages.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\pdfinfo.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\pdftops.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\pdftotext.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\xpdfrc.txt"
  Delete "$INSTDIR\xpdf-3.00-win32\ANNOUNCE"
  Delete "$INSTDIR\xpdf-3.00-win32\CHANGES"
  Delete "$INSTDIR\xpdf-3.00-win32\COPYING"
  Delete "$INSTDIR\xpdf-3.00-win32\INSTALL"
  Delete "$INSTDIR\xpdf-3.00-win32\README"

  RMDir "$INSTDIR\xpdf-3.00-win32"
  RMDir "$INSTDIR"

  ReadRegStr $R9 HKCU "Software\${MANUFACTURER}\${MUI_PRODUCT}" "MenuFolder"
  StrCmp $R9 "" "" clear_shortcuts
  ReadRegStr $R9 HKLM "Software\${MANUFACTURER}\${MUI_PRODUCT}" "MenuFolder"
  StrCmp $R9 "" clear_Registry
  StrCpy $baseRegKey "HKLM"
  SetShellVarContext all
clear_shortcuts:
  RMDir /r "$SMPROGRAMS\$R9"
  Delete "$DESKTOP\${MUI_PRODUCT}.lnk"
  Delete "$QUICKLAUNCH\${MUI_PRODUCT}.lnk"

clear_Registry:

  StrCpy $0 $baseRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 "MenuFolder"
  Call un.OMDeleteRegValue

  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  StrCpy $2 "Files"
  Call un.OMDeleteRegValue
  StrCpy $2 "Contacts"
  Call un.OMDeleteRegValue
  StrCpy $2 "HTML"
  Call un.OMDeleteRegValue
  StrCpy $2 "Favorites"
  Call un.OMDeleteRegValue
  StrCpy $2 "Tasks"
  Call un.OMDeleteRegValue
  StrCpy $2 "Pictures"
  Call un.OMDeleteRegValue
  StrCpy $2 "Outlook"
  Call un.OMDeleteRegValue
  StrCpy $2 "ICQ"
  Call un.OMDeleteRegValue
  StrCpy $2 "News"
  Call un.OMDeleteRegValue
  StrCpy $2 "Miranda"
  Call un.OMDeleteRegValue
  StrCpy $2 "RSS"
  Call un.OMDeleteRegValue
  StrCpy $2 "Notes"
  Call un.OMDeleteRegValue
  StrCpy $2 "MSWord"
  Call un.OMDeleteRegValue
  StrCpy $2 "MSPowerPoint"
  Call un.OMDeleteRegValue
  StrCpy $2 "PDF"
  Call un.OMDeleteRegValue

  EnumRegValue $R2 HKCU "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}" 0

  StrCmp $R2 "" "" finish_uninstall
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}\Config"
  Call un.OMDeleteRegKey
  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}\${PLUGINSSUBKEY}"
  Call un.OMDeleteRegKeyIfEmpty

finish_uninstall:

  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  StrCpy $2 "VersionMajor"
  Call un.OMDeleteRegValue
  StrCpy $2 "VersionMinor"
  Call un.OMDeleteRegValue
  StrCpy $2 ""
  Call un.OMDeleteRegValue

  StrCpy $1 "Software\${MANUFACTURER}\${MUI_PRODUCT}"
  Call un.OMDeleteRegKeyIfEmpty
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${MUI_PRODUCT}"

!ifdef READER
  StrCmp $R5 "leavedb" skip_feedback
  ExecShell "" "http://www.jetbrains.net/omea/uninstall/Start?product=reader&build=${BUILD}"
!else
  ExecShell "" "http://www.jetbrains.net/omea/uninstall/Start?product=pro&build=${BUILD}"
!endif
skip_feedback:

SectionEnd

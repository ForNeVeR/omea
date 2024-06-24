; SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
;
; SPDX-License-Identifier: GPL-2.0-only

 ; AcrobatReaderInstalled
 ;
 ; Usage:
 ;   Call AcrobatReaderInstalled
 ;   Pop $0
 ;   StrCmp $0 1 found.Acrobat no.Acrobat

Function AcrobatReaderInstalled
  Push $0
  Push $1

#read adobe acrobat reader installation path
  ReadRegStr $0 HKEY_LOCAL_MACHINE "Software\Adobe\Acrobat Reader\5.0\InstallPath" ""
# remove trailing back slash
  Push $0
  Exch $EXEDIR
  Exch $EXEDIR
  Pop $0

  ReadRegStr $1 HKEY_LOCAL_MACHINE "Software\Adobe\Acrobat Reader\5.0\Language\Current" ""

# if specified file doesn't exist acrobat reader is not installed
  IfFileExists $0\$1 0 noAcrobat
  StrCpy $0 1
  Goto done

noAcrobat:
  StrCpy $0 0

done:
  Pop $1
  Exch $0
FunctionEnd

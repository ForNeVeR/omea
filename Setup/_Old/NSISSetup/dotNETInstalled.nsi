; SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
;
; SPDX-License-Identifier: GPL-2.0-only

; IsDotNETVersionInstalled
;
; Usage:
;   StrCpy $0 "v1.1"
;   Call IsDotNETVersionInstalled
;   StrCmp $0 1 found.NETFramework no.NETFramework

Function IsDotNETVersionInstalled

  Push $1
  Push $2
  Push $3

  ReadRegStr $1 HKEY_LOCAL_MACHINE "Software\Microsoft\.NETFramework" "InstallRoot"
  # remove trailing back slash
  Push $1
  Exch $EXEDIR
  Exch $EXEDIR
  Pop $1
  # if the root directory doesn't exist .NET is not installed
  IfFileExists $1 0 noDotNET

  StrCpy $2 0

EnumPolicy:
    EnumRegValue $3 HKEY_LOCAL_MACHINE "Software\Microsoft\.NETFramework\Policy\$0" $2
    IntOp $2 $2 + 1
    StrCmp $3 "" noDotNET
    IfFileExists "$1\$0.$3" foundDotNET EnumPolicy

noDotNET:
  StrCpy $0 0
  Goto done
foundDotNET:
  StrCpy $0 1

done:
  Pop $3
  Pop $2
  Pop $1

FunctionEnd

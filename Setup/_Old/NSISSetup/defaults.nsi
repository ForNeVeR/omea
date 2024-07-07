; SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
;
; SPDX-License-Identifier: GPL-2.0-only

; manufacturer of Omnia Mea
!define MANUFACTURER                   "JetBrains"

; product name & version are necessary for the "Modern User Interface" scripting
!ifdef READER
  !define MUI_PRODUCT                 "Omea Reader"
  Name "Omea Reader"
!else
  !define MUI_PRODUCT                 "Omea"
  Name "Omea"
!endif

; here binary files should be located
!ifndef BINARIES_LOCATION
  !define BINARIES_LOCATION            "..\..\..\bin\Release"
!endif

!define REFERENCES_LOCATION            "..\..\References"

; here data files should be located
!define DATA_LOCATION                  "..\..\Data"

; where sound files should be located
!define SOUNDS_LOCATION                "..\..\Sounds"

; name of the 'Plugins' subkey in the Omnia Mea HKCU key
!define PLUGINSSUBKEY                  "PluginsTokaj"

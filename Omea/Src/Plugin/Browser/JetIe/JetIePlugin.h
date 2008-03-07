/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetIePlugin header file — contains some project-global declarations.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

/// The library UUID.
#ifdef JETIE_OMEA
struct __declspec(uuid("633820F7-C04E-4152-B64F-1147B881F998"))
/* LIBID */ LIBID_JetIePlugin;
#endif
#ifdef JETIE_BEELAXY
struct __declspec(uuid("633820F8-C04E-4152-B64F-1147B881F998"))
/* LIBID */ LIBID_JetIePlugin;
#endif

//#define USE_OLE_DISPATCH

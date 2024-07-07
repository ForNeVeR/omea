// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

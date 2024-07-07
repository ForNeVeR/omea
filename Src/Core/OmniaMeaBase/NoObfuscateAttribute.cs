// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.Base
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Field)]
    public class NoObfuscateAttribute: Attribute
	{
		public NoObfuscateAttribute()
		{
		}
	}
}

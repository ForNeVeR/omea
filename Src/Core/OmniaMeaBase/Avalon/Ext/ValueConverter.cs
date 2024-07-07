// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Annotations;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// A factory for <see cref="ValueConverter"/>, the universal Avalon Value Converter.
	/// </summary>
	public static class ValueConverter
	{
		#region Operations

		[NotNull]
		public static ValueConverter<TSource, TTarget> Create<TSource, TTarget>([NotNull] Func<TSource, TTarget> converter)
		{
			return new ValueConverter<TSource, TTarget>(converter);
		}

		#endregion
	}
}

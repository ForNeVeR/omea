// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Specifies the operation to be performed over the regions.
	/// </summary>
	public enum CombineRgnStyles
	{
		/// <summary>
		/// The new clipping region combines the overlapping areas of the current clipping region and the region identified by hrgn.
		/// </summary>
		RGN_AND = 1,
		/// <summary>
		/// The new clipping region is a copy of the region identified by hrgn. This is identical to SelectClipRgn. If the region identified by hrgn is NULL, the new clipping region is the default clipping region (the default clipping region is a null region).
		/// </summary>
		RGN_OR = 2,
		/// <summary>
		/// The new clipping region combines the areas of the current clipping region with those areas excluded from the region identified by hrgn.
		/// </summary>
		RGN_XOR = 3,
		/// <summary>
		/// The new clipping region combines the current clipping region and the region identified by hrgn.
		/// </summary>
		RGN_DIFF = 4,
		/// <summary>
		/// The new clipping region combines the current clipping region and the region identified by hrgn but excludes any overlapping areas.
		/// </summary>
		RGN_COPY = 5,
		/// <summary>
		/// Same as <see cref="RGN_AND"/>.
		/// </summary>
		RGN_MIN = RGN_AND,
		/// <summary>
		/// Same as <see cref="RGN_COPY"/>.
		/// </summary>
		RGN_MAX = RGN_COPY,
	}
}

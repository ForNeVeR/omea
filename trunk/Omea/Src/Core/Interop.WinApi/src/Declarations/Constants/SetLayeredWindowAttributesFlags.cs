using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum SetLayeredWindowAttributesFlags : uint
	{
		LWA_COLORKEY = 1,
		LWA_ALPHA = 2,
	}
}
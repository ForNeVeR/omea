// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Provides possibility to register common properties.
	/// </summary>
	public class CommonProps
	{
        private static bool _registered = false;
        public static int ContentId;

        public static void Register()
        {
            if ( !_registered )
            {
                ContentId = Core.ResourceStore.PropTypes.Register( "Content-Id", PropDataType.String, PropTypeFlags.Internal );
            }
            _registered = true;
        }
	}
}

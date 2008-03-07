/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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

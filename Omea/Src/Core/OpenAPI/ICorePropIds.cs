/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace JetBrains.Omea.OpenAPI
{
    public interface ICorePropIds
    {
        PropId<string> Name { get;  }
        PropId<IResource> Parent { get;  }
        PropId<string> LongBody { get; }
    }
}

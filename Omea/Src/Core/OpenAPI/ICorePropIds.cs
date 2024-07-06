// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

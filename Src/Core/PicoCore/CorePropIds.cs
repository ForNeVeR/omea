// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.PicoCore
{
    public class CorePropIds: ICorePropIds
    {
        private readonly PropId<string> _name;
        private readonly PropId<IResource> _parent;
        private readonly PropId<string> _longBody;

        public CorePropIds(ICoreProps props)
        {
            _name = new PropId<string>(props.Name);
            _parent = new PropId<IResource>(props.Parent);
            _longBody = new PropId<string>(props.LongBody);
        }

        public PropId<string> Name
        {
            get { return _name; }
        }

        public PropId<IResource> Parent
        {
            get { return _parent; }
        }

        public PropId<string> LongBody
        {
            get { return _longBody; }
        }
    }
}

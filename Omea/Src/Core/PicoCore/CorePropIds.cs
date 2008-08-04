/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
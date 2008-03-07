/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;

namespace JetBrains.Omea.Containers
{
    public class EmptyEnumerator : IEnumerator
    {
        public void Reset()
        {
        }

        public object Current
        {
            get { return null; }
        }

        public bool MoveNext()
        {
            return false;
        }
    }
}
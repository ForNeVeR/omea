// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

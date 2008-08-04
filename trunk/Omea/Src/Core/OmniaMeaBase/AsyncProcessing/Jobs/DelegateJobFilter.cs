/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
    internal class DelegateJobFilter
    {
        private Delegate _method;

        internal DelegateJobFilter( Delegate method )
        {
            _method = method;
        }

        public bool DoFilter( AbstractJob job )
        {
            return job is DelegateJob && ((DelegateJob) job).Method.Equals( _method );
        }
    }
}
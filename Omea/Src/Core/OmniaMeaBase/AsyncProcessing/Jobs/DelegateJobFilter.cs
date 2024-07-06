// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

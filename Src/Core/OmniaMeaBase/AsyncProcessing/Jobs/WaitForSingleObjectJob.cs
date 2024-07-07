// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Threading;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
    public class WaitForSingleObjectJob : AbstractJob
    {
        public WaitForSingleObjectJob( WaitHandle handle )
        {
            _handle = handle;
        }
        protected override void Execute()
        {
            if( _handle != null )
            {
                InvokeAfterWait( NextMethod, _handle );
                _handle = null;
            }
        }
        private WaitHandle _handle;
    }
}

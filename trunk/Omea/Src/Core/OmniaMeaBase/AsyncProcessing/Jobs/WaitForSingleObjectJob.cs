/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
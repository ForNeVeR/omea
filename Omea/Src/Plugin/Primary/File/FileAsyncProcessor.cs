/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Threading;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FilePlugin
{
    internal class FileAsyncProcessor: AsyncProcessor
    {
        public FileAsyncProcessor()
            : base( new AsyncExceptionHandler( HandleFileProcessorException ), false )
        {
        }

        private static void HandleFileProcessorException( Exception exception )
        {
            exception = Utils.GetMostInnerException( exception );
            Core.ReportException( exception, ExceptionReportFlags.AttachLog );
        }

        public static int MsgWaitForSingleObject( WaitHandle h, int timeout )
        {
            IntPtr[] handle = new IntPtr[ 1 ];
            handle[ 0 ] = h.Handle;
            return MsgWaitForMultipleObjects( handle, 1, (uint)timeout );
        }
    }
}
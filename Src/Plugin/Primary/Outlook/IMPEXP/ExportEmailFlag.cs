// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using EMAPILib;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ExportEmailFlag : AbstractNamedJob
    {
        private IResource _emailResource = null;
        private bool _completed = false;
        private bool _flagged = false;
        private int _color = 0;
        PairIDs _messageIDs;
        private ExportEmailFlag( IResource emailResource, PairIDs messageIDs )
        {
            _messageIDs = messageIDs;
            _emailResource = emailResource;
            IResource resFlag = _emailResource.GetLinkProp( ResourceFlag.PropFlag );
            _flagged = ( resFlag != null );
            if ( _flagged )
            {
                _completed = OutlookFlags.IsCompletedFlag( resFlag );
                _color = OutlookFlags.GetColorIndex( resFlag );
            }
        }
        public static void Do( JobPriority jobPriority, IResource mail )
        {
            PairIDs messageIDs;
            if ( IsDataCorrect( mail, out messageIDs ) )
            {
                OutlookSession.OutlookProcessor.QueueJob( jobPriority, new ExportEmailFlag( mail, messageIDs ) );
            }
        }
        private static bool IsDataCorrect( IResource mail, out PairIDs messageIDs )
        {
            messageIDs = null;
            if ( Mail.MailInIMAP( mail ) )
            {
                return false;
            }
            messageIDs = PairIDs.Get( mail );
            return messageIDs != null;
        }

        protected override void Execute()
        {
            IEMessage message = OutlookSession.OpenMessage( _messageIDs.EntryId, _messageIDs.StoreId );
            if ( message == null ) return;
            using ( message )
            {
                int flagStatus = 0;
                int flagColor = 0;
                GetCurrentFlag( message, out flagStatus, out flagColor );
                ExportFlag( message, flagStatus, flagColor );
            }
        }

        private static void GetCurrentFlag( IEMessage message, out int flagStatus, out int flagColor )
        {
            flagColor = 0;
            flagStatus = message.GetLongProp( MAPIConst.PR_FLAG_STATUS );
            if ( flagStatus == 2 && OutlookSession.Version >= 11 )
            {
                flagColor = message.GetLongProp( MAPIConst.PR_FLAG_COLOR, true );
                if ( flagColor == -9999 )
                {
                    flagColor = 6;
                }
            }
        }

        private void ExportFlag( IEMessage message, int flagStatus, int flagColor )
        {
            bool wasChanges = false;
            if ( _flagged )
            {
                if ( _completed )
                {
                    SetCompleteFlag( message, flagStatus, flagColor, ref wasChanges );
                }
                else
                {
                    SetColorFlag( message, flagStatus, flagColor, ref wasChanges );
                }
            }
            else
            {
                ClearFlag( message, flagStatus, flagColor, ref wasChanges );
            }
            if ( wasChanges )
            {
                OutlookSession.SaveChanges( "Export email flag for resource id = " + _emailResource.Id, message, message.GetBinProp( MAPIConst.PR_ENTRYID ) );
            }
        }

        private static void ClearFlag( IEMessage message, int flagStatus, int flagColor, ref bool wasChanges )
        {
            if ( flagStatus != 2 )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_STATUS, 0 );
                wasChanges = true;
            }
            if ( OutlookSession.Version >= 11 && flagColor != 0 )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_COLOR, 0 );
                wasChanges = true;
            }
        }

        private void SetColorFlag( IEMessage message, int flagStatus, int flagColor, ref bool wasChanges )
        {
            if ( flagStatus != 2 )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_STATUS, 2 );
                wasChanges = true;
            }
            if ( OutlookSession.Version >= 11 && flagColor != _color  )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_COLOR, _color );
                wasChanges = true;
            }
        }

        private static void SetCompleteFlag( IEMessage message, int flagStatus, int flagColor, ref bool wasChanges )
        {
            if ( flagStatus != 1 )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_STATUS, 1 );
                wasChanges = true;
            }
            if ( flagColor != 0 && OutlookSession.Version >= 11 )
            {
                message.SetLongProp( MAPIConst.PR_FLAG_COLOR, 0 );
                wasChanges = true;
            }
        }

        public override string Name
        {
            get { return "Export mail flag"; }
        }
    }
}

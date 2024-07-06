// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    /**
     * Class for working with attachments to Outlook messages.
     */

    public class OutlookAttachment
    {
        private readonly IResource _resAttach;

        private readonly int _attachmentIndex;
        private readonly int _sourceMailID;
        private readonly string _fileName;
        private readonly int _attachMethod = -1;
        private int _num = -1;

        public OutlookAttachment( IResource res )
        {
            _attachmentIndex = res.GetIntProp( PROP.AttachmentIndex );
            if ( res.HasProp( PROP.PR_ATTACH_NUM ) )
            {
                _num = res.GetIntProp( PROP.PR_ATTACH_NUM );
            }
            if ( res.HasProp( PROP.AttachMethod ) )
            {
                _attachMethod = res.GetIntProp( PROP.AttachMethod );
            }
            IResource srcMail = null;
            if ( res.HasProp( PROP.ResourceTransfer ) )
            {
                srcMail = res.GetLinkProp( PROP.InternalAttachment );
            }
            else
            {
                srcMail = res.GetLinkProp( PROP.Attachment );
            }
            _sourceMailID = srcMail.Id;
            _fileName = res.GetStringProp( Core.Props.Name );
            _resAttach = res;
        }
        public IEAttach OpenAttach()
        {
            try
            {
                IEMessage message = OpenMessage();
                if ( message == null ) return null;
                using ( message )
                {
                    if ( _num == -1 )
                    {
                        _num = GetAttachNum( message, _attachmentIndex );
                        ResourceProxy resAttach = new ResourceProxy( _resAttach );
                        resAttach.SetPropAsync( PROP.PR_ATTACH_NUM, _num );
                        resAttach.SetPropAsync( PROP.AttachMethod, _attachMethod );
                    }
                    return message.OpenAttach( _num );
                }
            }
            catch
            {
                return null;
            }
        }

        private static int GetAttachNum( IEMessage message, int index )
        {
            IETable table = message.GetAttachments();
            if ( table == null ) return 0;
            using ( table )
            {
                int count = table.GetRowCount();
                for ( int i = 0; i < count; i++ )
                {
                    IERowSet row = table.GetNextRow();
                    if ( row != null )
                    {
                        using ( row )
                        {
                            if ( index == i )
                            {
                                return row.FindLongProp( MAPIConst.PR_ATTACH_NUM );
                            }
                        }
                    }
                    if ( index < i ) break;
                }
            }
            return 0;
        }

        private static MemoryStream EmptyStream { get { return new MemoryStream( new byte[] {} ); } }

        public IEMessage OpenMessage()
        {
            IResource mail = Core.ResourceStore.LoadResource( _sourceMailID );
            if ( mail == null ) return null;
            PairIDs messageIDs = PairIDs.Get( mail );
            if ( messageIDs == null ) return null;
            return OutlookSession.OpenMessage( messageIDs.EntryId, messageIDs.StoreId );
        }

        public IEMessage OpenEmbeddedMessage()
        {
            IEAttach attach = OpenAttach();
            if ( attach == null ) return null;
            using ( attach )
            {
                return attach.OpenMessage();
            }
        }
        public MemoryStream Stream
        {
            get
            {
                IEAttach attach = OpenAttach();
                if ( attach == null ) return EmptyStream;
                using ( attach )
                {
                    return new MemoryStream( attach.ReadToEnd() );
                }
            }
        }

        public void SaveAs( string fileName )
        {
            FileStream fs = null;
            try
            {
                bool setRO = false;
                FileAttributes attr = FileAttributes.Normal;
                if ( File.Exists( fileName ) )
                {
                    attr = File.GetAttributes( fileName );
                    if ( ( attr & FileAttributes.ReadOnly ) == FileAttributes.ReadOnly )
                    {
                        File.SetAttributes( fileName, attr & ~FileAttributes.ReadOnly );
                        setRO = true;
                    }
                }
                fs = new FileStream( fileName, FileMode.OpenOrCreate, FileAccess.Write );
                Stream.WriteTo( fs );
                if ( setRO )
                {
                    File.SetAttributes( fileName, attr | FileAttributes.ReadOnly );
                }
            }
            catch ( Exception exception )
            {
                StandartJobs.MessageBox( exception.Message, "Operation failed", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            finally
            {
                if ( fs != null )
                {
                    fs.Close();
                }
            }
        }

        public string FileName { get { return _fileName; } }
    }

    internal class OutlookAttachmentException : Exception
    {
        internal OutlookAttachmentException( string msg ) : base( msg ) { }
    }
}

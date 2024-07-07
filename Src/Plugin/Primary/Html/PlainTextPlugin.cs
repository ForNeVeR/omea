// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.HTMLPlugin
{
    internal class PlainTextIconProvider : IResourceIconProvider
    {
        private Icon _textFileIcon;

        public Icon GetResourceIcon( IResource resource )
        {
            return FileIcons.GetFileSmallIcon( resource.GetPropText( "Name" ) );
        }

        public Icon GetDefaultIcon( string resType )
        {
            if ( _textFileIcon == null )
            {
                _textFileIcon = new Icon( Assembly.GetExecutingAssembly().GetManifestResourceStream( "HTMLPlugin.TextFile.ico" ) );
            }
            return _textFileIcon;
        }
    }

    internal class PlainTextDisplayer: IResourceDisplayer
    {
        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return new RTFDisplayPane( new LoadTextDelegate( LoadText ) );
        }

        private void LoadText( IResource res, RichTextBox richTextBox )
        {
            try
            {
                StreamReader reader = Core.FileResourceManager.GetStreamReader( res );
                if( reader != null )
                {
                    using( reader )
                    {
                        richTextBox.Text = Utils.StreamReaderReadToEnd( reader );
                    }
                }
            }
            catch( ObjectDisposedException ) {}
        }
    }

    internal class PlainTextTextProvider: IResourceTextProvider
    {
        bool IResourceTextProvider.ProcessResourceText( IResource resource, IResourceTextConsumer consumer )
        {
            try
            {
                StreamReader reader = Core.FileResourceManager.GetStreamReader( resource );
                if( reader != null )
                {
                    using( reader )
                    {
                        ProcessResourceStream( resource, reader, consumer );
                    }
                }
            }
            catch( ObjectDisposedException )
            {
                Core.TextIndexManager.QueryIndexing( resource.Id );
            }
            return true;
        }

        private void ProcessResourceStream( IResource resource, StreamReader reader, IResourceTextConsumer consumer )
        {
            StringBuilder builder = new StringBuilder();
            int aChar, lastChar = 0;
            char c;

            while( ( aChar = reader.Read() ) != -1 )
            {
                if( ( aChar != 0x0a && aChar != 0x0d ) || lastChar != 0x0d )
                {
                    c = (char) aChar;
                    builder.Append( c );
                    if( builder.Length > 4000 &&
                        ( Char.IsWhiteSpace( c ) || Char.IsPunctuation( c ) ) )
                    {
                        consumer.AddDocumentFragment( resource.Id, builder.ToString() );
                        builder.Length = 0;
                    }
                }
                lastChar = aChar;
            }
            if( builder.Length > 0 )
            {
                consumer.AddDocumentFragment( resource.Id, builder.ToString() );
            }
        }
    }
}

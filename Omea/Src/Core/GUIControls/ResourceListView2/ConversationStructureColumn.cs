// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A column which supports drawing a special "pink plus" icon for collapsed nodes
	/// which have unread replies.
	/// </summary>
	public class ConversationStructureColumn: TreeStructureColumn
	{
        private ConversationDataProvider _dataProvider;
        private Icon _boldPlusIcon;

		public ConversationStructureColumn( ConversationDataProvider dataProvider )
		{
		    _dataProvider = dataProvider;
		    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "GUIControls.Icons.boldplus.ico" );
            _boldPlusIcon = new Icon( stream );
            ShowLines = false;
		}

	    public ConversationDataProvider DataProvider
	    {
	        get { return _dataProvider; }
	        set { _dataProvider = value; }
	    }

	    protected override void DrawStructureIcon( Graphics g, Rectangle rc, Rectangle rcIcon, JetListViewNode node )
	    {
	        if ( node.CollapseState == CollapseState.Collapsed )
	        {
	            IResource res = (IResource) node.Data;
                if ( _dataProvider.ResourceHasUnreadReplies( res ) )
                {
                    rc.Offset( (rc.Width - 15) / 2, (rc.Height - 15) / 2 );
                    g.DrawIcon( _boldPlusIcon, new Rectangle( rc.Left, rc.Top, 16, 16 ) );
                    return;
                }
	        }
            base.DrawStructureIcon( g, rc, rcIcon, node );
	    }
	}
}

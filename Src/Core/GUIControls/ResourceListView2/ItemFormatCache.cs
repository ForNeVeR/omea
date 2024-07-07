// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Caches the results of applying formatting rules to ResourceListView2 items.
	/// </summary>
	internal class ItemFormatCache
	{
        private ItemFormat _defaultItemFormat;
        private ItemFormat _unreadItemFormat;
        private HashMap _unreadFormats = new HashMap();  // ItemFormat -> ItemFormat
        private HashMap _formatCache = new HashMap();    // IResource -> ItemFormat

        public ItemFormatCache()
		{
            _defaultItemFormat = new ItemFormat( FontStyle.Regular, SystemColors.WindowText, SystemColors.Window );
            _unreadItemFormat = new ItemFormat( FontStyle.Bold, SystemColors.WindowText, SystemColors.Window );
        }

        internal void Clear()
        {
            lock( _formatCache )
            {
                _formatCache.Clear();
            }
        }

        internal ItemFormat GetUnreadItemFormat( IResource res )
        {
            ItemFormat itemFormat = null;
            if ( Core.FormattingRuleManager != null )
            {
                itemFormat = Core.FormattingRuleManager.GetFormattingInfo( res );
            }

            if ( itemFormat == null )
                return _unreadItemFormat;

            ItemFormat unreadFormat = (ItemFormat) _unreadFormats [itemFormat];
            if ( unreadFormat == null )
            {
                unreadFormat = new ItemFormat( itemFormat.FontStyle | FontStyle.Bold,
                    itemFormat.ForeColor, itemFormat.BackColor );
                _unreadFormats [itemFormat] = unreadFormat;
            }
            return unreadFormat;
        }

	    public FontStyle GetItemFont( object item )
	    {
            ItemFormat format = GetItemFormat( item );
            if ( format != null )
            {
                return format.FontStyle;
            }
            return FontStyle.Regular;
	    }

        public Color GetItemForeColor( object item )
        {
            ItemFormat format = GetItemFormat( item );
            if ( format != null )
            {
                return format.ForeColor;
            }
            return SystemColors.WindowText;
        }

        public Color GetItemBackColor( object item )
        {
            ItemFormat format = GetItemFormat( item );
            if ( format != null )
            {
                return format.BackColor;
            }
            return SystemColors.Window;
        }

        public void InvalidateFormat( object item )
        {
            lock( _formatCache )
            {
                _formatCache.Remove( item );
            }
        }

        private ItemFormat GetItemFormat( object item )
	    {
            IResource res = (IResource) item;
            lock( _formatCache )
            {
                ItemFormat format = (ItemFormat) _formatCache [item];
                if ( format == null )
                {
                    if ( res.HasProp( Core.Props.IsUnread ) )
                    {
                        format = GetUnreadItemFormat( res );
                    }
                    else if ( Core.FormattingRuleManager != null )
                    {
                        format = Core.FormattingRuleManager.GetFormattingInfo( res );
                    }
                    if ( format == null )
                    {
                        format = _defaultItemFormat;
                    }
                    _formatCache [item] = format;
                }
                return format;
            }
	    }

	    public void HookFormattingRulesChange()
	    {
	        Core.FormattingRuleManager.FormattingRulesChanged += new EventHandler( HandleFormattingRulesChanged );
	    }

	    private void HandleFormattingRulesChanged( object sender, EventArgs e )
	    {
	        Clear();
	    }
	}
}

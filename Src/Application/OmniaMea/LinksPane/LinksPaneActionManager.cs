// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using System.Collections;
using System.Windows.Forms;

namespace JetBrains.Omea
{
	/**
     * Manages the actions registered for the links pane.
     */

    internal class LinksPaneActionManager
	{
        internal class LinksPaneAction
        {
            internal string _text;
            internal IAction _action;
            internal IActionStateFilter[] _filters;

            public LinksPaneAction( string text, IAction action, IActionStateFilter[] filters )
            {
                _text = text;
                _action = action;
                _filters = filters;
            }
        }

        private static LinksPaneActionManager _theManager;
        private Hashtable _typeToActions = new Hashtable();
        private ActionContext _lastContext;

        private LinksPaneActionManager()
		{
        }

        public static LinksPaneActionManager GetManager()
        {
            if ( _theManager == null )
            {
                _theManager = new LinksPaneActionManager();
            }
            return _theManager;
        }

        public void RegisterAction( IAction action, string text, string resourceType, IActionStateFilter[] filters )
        {
            if ( resourceType == null )
            {
                resourceType = "";
            }

            ArrayList actions = (ArrayList) _typeToActions [resourceType];
            if ( actions == null )
            {
                actions = new ArrayList();
                _typeToActions [resourceType] = actions;
            }
            actions.Add( new LinksPaneAction( text, action, filters ) );
        }

        public void UnregisterAction( IAction action )
        {
            foreach( DictionaryEntry de in _typeToActions )
            {
                ArrayList actions = (ArrayList) de.Value;
                foreach( LinksPaneAction lpAction in actions )
                {
                    if ( lpAction._action == action )
                    {
                        actions.Remove( lpAction );
                        break;
                    }
                }
            }
        }

        public LinksPaneActionItem[] CreateActionLinks( IResourceList resList, ILinksPaneFilter filter )
        {
            _lastContext = new ActionContext( ActionContextKind.LinksPane, null, resList );
            IResource res = (resList.Count == 1) ? resList [0] : null;

            ArrayList result = new ArrayList();
            CreateActionLinks( result, "", filter, res );
            if ( resList.Count > 0 && resList.AllResourcesOfType( resList [0].Type ) )
            {
                CreateActionLinks( result, resList [0].Type, filter, res );
            }
            return (LinksPaneActionItem[]) result.ToArray( typeof (LinksPaneActionItem) );
        }

        private void CreateActionLinks( ArrayList itemList, string resType, ILinksPaneFilter filter, IResource filterRes )
        {
            ArrayList actions = (ArrayList) _typeToActions [resType];
            if ( actions == null )
                return;

            ActionPresentation presentation = new ActionPresentation();
            foreach( LinksPaneAction action in actions )
            {
                if ( filter != null && filterRes != null && !filter.AcceptAction( filterRes, action._action ) )
                {
                    continue;
                }

                presentation.Reset();
                presentation.Text = action._text;
                UpdateAction( action, ref presentation );
                if ( !presentation.Visible )
                    continue;

                itemList.Add( new LinksPaneActionItem( action._action, presentation.Text, presentation.Enabled ) );
            }
        }

        private void UpdateAction( LinksPaneAction lpAction, ref ActionPresentation presentation )
        {
            if ( lpAction._filters != null )
            {
                foreach( IActionStateFilter filter in lpAction._filters )
                {
                    filter.Update( _lastContext, ref presentation );
                    if ( !presentation.Visible )
                        return;
                }
            }
            lpAction._action.Update( _lastContext, ref presentation );
        }

        internal void OnActionLabelClick( object sender, EventArgs e )
        {
            Control lbl = (Control) sender;
            IAction action = (IAction) lbl.Tag;
			action.Execute( _lastContext );
        }
	}

    internal class LinksPaneActionItem
    {
        private string _text;
        private bool _enabled;
        private IAction _action;

        public LinksPaneActionItem( IAction action, string text, bool enabled )
        {
            _action = action;
            _text = text;
            _enabled = enabled;
        }

        public string Text
        {
            get { return _text; }
        }

        public bool Enabled
        {
            get { return _enabled; }
        }

        public IAction Action
        {
            get { return _action; }
        }
    }
}

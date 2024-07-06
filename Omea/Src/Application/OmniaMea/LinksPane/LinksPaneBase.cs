// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    /// <summary>
    /// Common functionality of LinksBar and LinksPane.
    /// </summary>
    internal abstract class LinksPaneBase : UserControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        protected ILinksPaneFilter _filter;
        protected IResourceStore _store;
        protected IResourceList  _resourceList;

        private static readonly AnchoredList _linksPaneGroups = new AnchoredList();    // string -> IntArrayList

        protected ColorScheme _colorScheme;

        private IResourceList _customProperties;

		public LinksPaneBase()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
                if ( _customProperties != null )
                {
                    _customProperties.ResourceAdded -= HandleCustomPropertyListChanged;
                    _customProperties.ResourceDeleting -= HandleCustomPropertyListChanged;
                    _customProperties.Dispose();
                }
			}
			base.Dispose( disposing );
		}

        #region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

        public static void RegisterLinksGroup( string groupId, int[] propTypes, ListAnchor anchor )
        {
            IntArrayList existingList = (IntArrayList) _linksPaneGroups.FindByKey( groupId );
            if ( existingList != null )
            {
                foreach( int propType in propTypes )
                {
                    if ( existingList.IndexOf( propType ) < 0 )
                    {
                        existingList.Add( propType );
                    }
                }
            }
            else
            {
                _linksPaneGroups.Add( groupId, new IntArrayList( propTypes ), anchor );
            }
        }

        public ColorScheme ColorScheme
        {
            get { return _colorScheme; }
            set
            {
                if ( _colorScheme != value )
                {
                    _colorScheme = value;
                    OnColorSchemeChanged();
                    Invalidate();
                }
            }
        }

        protected virtual void OnColorSchemeChanged()
        {
        }

        /**
         * Sets the list of resources for which the links are displayed.
         */

        protected void SetResourceList( IResourceList resList, ILinksPaneFilter filter )
        {
            _filter = filter;
            if ( _store == null )
            {
                _store = Core.ResourceStore;
                _store.ResourceSaved += OnResourceSaved;
                _customProperties = Core.ResourceStore.FindResourcesLive( "PropType", "Custom", 1 );
                _customProperties.ResourceAdded += HandleCustomPropertyListChanged;
                _customProperties.ResourceDeleting += HandleCustomPropertyListChanged;
            }
            _resourceList = resList;
        }

        private void OnResourceSaved( object sender, ResourcePropEventArgs e )
        {
            IResourceList resList = _resourceList;
            if ( resList != null && resList.Count == 1 && e.Resource.Id == resList.ResourceIds [0] )
            {
                int[] changedPropIDs = e.ChangeSet.GetChangedProperties();
                for( int i=0; i<changedPropIDs.Length; i++ )
                {
                    int propId = changedPropIDs [i];
                    if ( Core.ResourceStore.PropTypes [propId].DataType == PropDataType.Link ||
                        ResourceTypeHelper.IsCustomPropType( propId ) )
                    {
                        Core.UIManager.QueueUIJob( new MethodInvoker( UpdateLinksPane ) );
                        break;
                    }
                }
            }
        }

        private void HandleCustomPropertyListChanged( object sender, ResourceIndexEventArgs e )
        {
            // see OM-3056
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateLinksPane ) );
        }

        protected abstract void UpdateLinksPane();

        protected LinkSection BuildLinksForResource( IResource res )
        {
            IntArrayList linkTypes = new IntArrayList( res.GetLinkTypeIds() );
            linkTypes.Sort();

            LinkSection lastSection = null;
            LinkSection groupStartSection;

            foreach( IntArrayList propIds in _linksPaneGroups )
            {
                groupStartSection = lastSection;
                foreach( int propId in propIds )
                {
                    int idx = linkTypes.IndexOf( propId );
                    if ( idx >= 0 )
                    {
                        lastSection = BuildLinksForType( lastSection, propId, res );
                        linkTypes.RemoveAt( idx );
                    }
                }
                if ( groupStartSection != lastSection )
                {
                    lastSection.Separator = true;
                }
            }

            groupStartSection = lastSection;
            foreach( int linkType in linkTypes )
            {
                if ( _store.PropTypes [linkType].HasFlag( PropTypeFlags.Internal ) )
                    continue;

                lastSection = BuildLinksForType( lastSection, linkType, res );
            }

            if ( lastSection != groupStartSection )
            {
                lastSection.Separator = true;
            }

            if ( lastSection != null )
            {
                while( lastSection.PrevSection != null )
                {
                    lastSection = lastSection.PrevSection;
                }
            }
            return lastSection;
        }

	    private LinkSection BuildLinksForType( LinkSection lastSection, int linkType, IResource res  )
	    {
            if ( _store.PropTypes [linkType].HasFlag( PropTypeFlags.DirectedLink ) )
	        {
	            lastSection = BuildLinkSection( lastSection, res, linkType, 1 );
	            lastSection = BuildLinkSection( lastSection, res, linkType, -1 );
	        }
	        else
	        {
	            lastSection = BuildLinkSection( lastSection, res, linkType, 0 );
	        }
            return lastSection;
	    }

	    /**
         * Shows the link type labels for the specified resource.
         */

        private LinkSection BuildLinkSection( LinkSection lastSection, IResource res, int linkType, int direction )
        {
            IResourceList resList;
            switch( direction )
            {
                case 0:  resList = res.GetLinksOfType( null, linkType );  break;

                case 1:  resList = res.GetLinksFrom( null, linkType );  break;

                case -1: resList = res.GetLinksTo( null, linkType );  break;

                default: throw new System.ArgumentException( "Invalid direction parameter" );
            }
            if ( resList.Count == 0 )
                return lastSection;

            int propId = (direction == -1 ) ? -linkType : linkType;
            string linkTypeName = Core.ResourceStore.PropTypes.GetPropDisplayName( propId );

            if ( _filter != null )
            {
                if ( !_filter.AcceptLinkType( res, propId, ref linkTypeName ) )
                {
                    return lastSection;
                }
            }

            foreach( IResource linkRes in resList.ValidResources )
            {
                string linkToolTip = null;
                if ( !CheckLinkVisible( res, propId, linkRes, ref linkToolTip ) )
                {
                    continue;
                }

                if ( lastSection == null || lastSection.Name != linkTypeName )
                {
                    lastSection = new LinkSection( linkTypeName, lastSection );
                }

                lastSection.AddResource( linkRes, propId, linkToolTip );
            }
            return lastSection;
        }

        private bool CheckLinkVisible( IResource baseResource, int propId, IResource linkRes, ref string linkTooltip )
        {
            if ( linkRes.HasProp( Core.Props.IsDeleted ) )
                return false;

            if ( !Core.ResourceStore.ResourceTypes [linkRes.Type].OwnerPluginLoaded )
            {
                return false;
            }
            foreach( IPropType propType in Core.ResourceStore.PropTypes )
            {
                if ( propType.HasFlag( PropTypeFlags.SourceLink ) && !propType.OwnerPluginLoaded &&
                    linkRes.HasProp( propType.Id ) )
                {
                    return false;
                }
            }

            if ( _filter != null && !_filter.AcceptLink( baseResource, propId, linkRes, ref linkTooltip ) )
            {
                return false;
            }
            return true;
        }

	    protected void ShowLinkContextMenu( ResourceLinkLabel linkLabel, ResourceLinkLabelEventArgs e )
        {
            ActionContext context = new ActionContext( ActionContextKind.ContextMenu, this,
                e.Resource.ToResourceList() );
            context.SetLinkTarget( linkLabel.LinkType, _resourceList [0] );
            Core.ActionManager.ShowResourceContextMenu( context, linkLabel, e.Point.X, e.Point.Y );
        }

        protected static string GetCustomPropText( IResource res, int propID )
        {
            string propText;
            PropDataType dataType = Core.ResourceStore.PropTypes [propID].DataType;
            if ( dataType == PropDataType.Bool )
            {
                propText = res.HasProp( propID ) ? "Yes" : "";
            }
            else if ( dataType == PropDataType.Date )
            {
                propText = res.GetDateProp( propID ).ToShortDateString();
            }
            else
            {
                propText = res.GetPropText( propID );
            }
            return propText;
        }

        internal class LinkItem
        {
            private readonly IResource _resource;
            private readonly int _propId;
            private readonly string _toolTip;

            public LinkItem( IResource resource, int propId, string toolTip )
            {
                _resource = resource;
                _propId = propId;
                _toolTip = toolTip;
            }

            public IResource Resource
            {
                get { return _resource; }
            }

            public int PropId
            {
                get { return _propId; }
            }

            public string ToolTip
            {
                get { return _toolTip; }
            }
        }

        internal class LinkSection
        {
            private readonly string _name;
            private readonly ArrayList _linkItems = new ArrayList() ;
            private bool _separator = false;   // whether a group separator is displayed after the group name
            private LinkSection _nextSection;
            private LinkSection _prevSection;

            public LinkSection( string name, LinkSection prevSection )
            {
                _name = name;
                if ( prevSection != null )
                {
                    prevSection._nextSection = this;
                    _prevSection = prevSection;
                }
            }

            public string Name
            {
                get { return _name; }
            }

            public bool Separator
            {
                get { return _separator; }
                set { _separator = value; }
            }

            public LinkSection NextSection
            {
                get { return _nextSection; }
            }

            public LinkSection PrevSection
            {
                get { return _prevSection; }
            }

            public ArrayList LinkItems
            {
                get { return _linkItems; }
            }

            internal void AddResource( IResource res, int propId, string toolTip )
            {
                _linkItems.Add( new LinkItem( res, propId, toolTip ) );
            }
        }
	}
}

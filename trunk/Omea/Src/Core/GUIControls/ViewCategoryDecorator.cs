/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
    public class CategoryTotalCountDecorator : IResourceNodeDecorator
    {
        private const string    _Sig = "TotalItems";

        private IResourceList   _allDecoCats, _allTyped, _allDeleted;
        private readonly TextStyle _textStyle;

        public event ResourceEventHandler DecorationChanged;
        public string DecorationKey { get{ return _Sig; } }

        public CategoryTotalCountDecorator()
        {
            _textStyle = new TextStyle( FontStyle.Regular, Color.Green, SystemColors.Window );

            _allDeleted = Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.IsDeleted );

            _allDecoCats = Core.ResourceStore.FindResourcesWithPropLive( "Category", "ShowTotalItems" );
            _allDecoCats.AddPropertyWatch( Core.ResourceStore.PropTypes[ "Category" ].Id );
            _allDecoCats.ResourceChanged += OnCategory;
        }

        public void AddContentTypes( string[] types )
        {
            if( types != null )
                _allTyped = Core.ResourceStore.GetAllResourcesLive( types[ 0 ] );
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.HasProp( Core.Props.ShowTotalCount ))
            {
                IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
                if( res.Type == FilterManagerProps.ViewResName )
                {
                    IResourceList total = Core.FilterEngine.ExecView( res );
                    if( !res.HasProp( "ShowDeletedItems" ))
                        total = total.Minus( _allDeleted );
                    total = total.Intersect( _allTyped );
                    if( wsp != null )
                        total = total.Intersect( wsp.GetLinksOfType( null, "WorkspaceVisible" ), true );

                    if( total.Count != 0 )
                    {
                        nodeText.Append( " " );
                        nodeText.Append( "[" + total.Count + "]", _textStyle );
                    }
                    return true;
                }
                else
                if( res.Type == "Category" )
                {
                    bool    leafCategory = (res.GetLinksTo( null, Core.Props.Parent ).Count == 0);
                    IResourceList inThis = Core.ResourceStore.EmptyResourceList, total;

                    if( leafCategory )
                        total = CollectResources( res, wsp );
                    else
                        CollectResources( res, wsp, out inThis, out total );

                    if( total.Count != 0 )
                    {
                        nodeText.Append( " " );
                        if( leafCategory )
                            nodeText.Append( "[" + total.Count + "]", _textStyle );
                        else
                            nodeText.Append( "[" + inThis.Count + "|" + total.Count + "]", _textStyle );
                    }
                    return true;
                }
            }
            return false;
        }

        private void CollectResources( IResource res, IResource wsp,
                                       out IResourceList inThis, out IResourceList total )
        {
            inThis = GetPureList( res, wsp );
            total = Core.ResourceStore.EmptyResourceList;
            IResourceList sub = res.GetLinksTo( "Category", Core.Props.Parent );
            foreach( IResource subCat in sub )
                total = total.Union( CollectResources( subCat, wsp ) );
            total = total.Union( inThis );
        }

        private IResourceList CollectResources( IResource res, IResource wsp )
        {
            IResourceList total = GetPureList( res, wsp );
            IResourceList sub = res.GetLinksTo( "Category", Core.Props.Parent );
            foreach( IResource subCat in sub )
                total = total.Union( CollectResources( subCat, wsp ) );

            return total;
        }

        private IResourceList GetPureList( IResource res, IResource wsp )
        {
            IResourceList total = res.GetLinksOfType( null, "Category" ).Minus( _allDeleted ).Intersect( _allTyped );
            if( wsp != null )
                total = total.Intersect( wsp.GetLinksOfType( null, "WorkspaceVisible" ), true );
            return total;
        }

        private void OnCategory( object sender, ResourcePropIndexEventArgs args )
        {
            DecorateResource( args.Resource );
            IResourceList parents = args.Resource.GetLinksFrom( "Category", Core.Props.Parent );
            if( parents.Count > 0 )
                DecorateResource( parents[ 0 ] );
        }

        private void DecorateResource( IResource res )
        {
            if( DecorationChanged != null )
            {
                DecorationChanged( this, new ResourceEventArgs( res ) );
            }
        }
    }

    public class TextQueryViewDecorator : IResourceNodeDecorator
    {
        private const string    _Sig = "TextIndexViews";

        private readonly TextStyle _notReadyStyle = new TextStyle( FontStyle.Regular, Color.Gray, SystemColors.Window );
        private readonly TextStyle _normalStyle = new TextStyle( FontStyle.Regular, Color.Black, SystemColors.Window );

        public event ResourceEventHandler DecorationChanged;
        public string DecorationKey { get{ return _Sig; } }

        public TextQueryViewDecorator()
        {
            //  Consume any event (what is first)
            Core.TextIndexManager.IndexLoaded += TextIndexLoaded;
        }

        private void TextIndexLoaded(object sender, System.EventArgs e)
        {
            Core.TextIndexManager.IndexLoaded -= TextIndexLoaded;

            IResourceList allViews = Core.FilterRegistry.GetViews();
            foreach( IResource view in allViews )
            {
                if( FilterRegistry.HasQueryCondition( view ) )
                    DecorateResource( view );
            }
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.Type == FilterManagerProps.ViewResName && FilterRegistry.HasQueryCondition( res ))
            {
                bool ready = Core.TextIndexManager.IsIndexPresent();
                nodeText.SetStyle( ready ? _normalStyle : _notReadyStyle, 0, nodeText.Length );
                return true;
            }
            return false;
        }

        private void DecorateResource( IResource res )
        {
            if( DecorationChanged != null )
            {
                DecorationChanged( this, new ResourceEventArgs( res ) );
            }
        }
    }
}
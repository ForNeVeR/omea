// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/**
     * Column for displaying resource flags.
     */

    public class FlagColumn: ICustomColumn
	{
        private readonly ImageList _imageList;
        private readonly IResourceIconProvider _flagIconProvider;
        private static ContextMenuStrip _flagContextMenu;
        private static ContextMenuActionManager _flagActionManager;
        private static int _lastFlagKey = 1;
        internal static bool _isCtrlPressed;

		public FlagColumn( IResourceIconProvider flagIconProvider )
		{
			_imageList = new ImageList();
			_imageList.ColorDepth = Core.ResourceIconManager.IconColorDepth;

			_imageList.Images.Add(Utils.TryGetEmbeddedResourceIconFromAssembly("OmniaMea", "OmniaMea.Icons.flag.ico"));
			_imageList.Images.Add(Utils.TryGetEmbeddedResourceIconFromAssembly("OmniaMea", "OmniaMea.Icons.FlagNoProp.ico"));

			_flagIconProvider = flagIconProvider;

			_flagContextMenu = new ContextMenuStrip();
			_flagActionManager = new ContextMenuActionManager(_flagContextMenu);
		}

        public static void FillImagesAndActions()
        {
            RegisterFlagActionGroup( "ItemFlagActions", new ListAnchor( AnchorType.After, "ItemAnnotateActions" ) );
            RegisterFlagActionGroup( "ItemFlagCompleteActions", new ListAnchor( AnchorType.After, "ItemFlagActions" ) );
            RegisterFlagActionGroup( "ItemClearFlagActions", new ListAnchor( AnchorType.After, "ItemFlagCompleteActions" ) );

            Core.ActionManager.SuppressContextMenuGroupSeparator( "ItemAnnotateActions", "ItemFlagActions" );

            //  Default flag must first in the list.
            RegisterFlagAction( ResourceFlag.DefaultFlag.Resource, "ItemFlagActions" );
            RegisterRestFlagActions();
            RegisterClearFlagAction();

            Core.ActionManager.RegisterLinkClickAction( new DisplayResourcesWithFlagAction(), "Flag", null );
        }

        private static void RegisterRestFlagActions()
        {
            IResourceList flags = Core.ResourceStore.GetAllResources( "Flag" ).Minus( ResourceFlag.DefaultFlag.Resource.ToResourceList() );
            flags.Sort( new SortSettings( Core.Props.Name, true ) );

            //  Register setters first and then cleaning flag.
            IResource completeFlag = null;
            foreach( IResource res in flags )
            {
                if( !res.HasProp( -ResourceFlag.PropNextStateFlag ) )
                    RegisterFlagAction( res,  "ItemFlagActions" );
                else
                    completeFlag = res;
            }
            RegisterFlagAction( completeFlag, "ItemFlagCompleteActions" );
        }

        private static void RegisterClearFlagAction()
        {
            IAction clearFlagAction = new ClearFlagAction();
            Core.ActionManager.RegisterContextMenuAction( clearFlagAction, "ItemClearFlagActions", ListAnchor.First, "Clear Flag", null, null, null );
            Core.ActionManager.RegisterMainMenuAction( clearFlagAction, "ItemClearFlagActions", ListAnchor.First, "Clear Flag", null, null, null );
            _flagActionManager.RegisterAction( clearFlagAction, "ItemClearFlagActions", ListAnchor.Last, "Clear Flag", null, null, null );

            Core.ActionManager.RegisterKeyboardAction( clearFlagAction, Keys.Control | Keys.D0, null, null );
            Core.ActionManager.RegisterKeyboardAction( clearFlagAction, Keys.Control | Keys.NumPad0, null, null );
        }


        private static void RegisterFlagActionGroup( string id, ListAnchor anchor )
        {
            Core.ActionManager.RegisterContextMenuActionGroup( id, "Flag With", anchor );
            _flagActionManager.RegisterGroup( id, null, ListAnchor.Last );

            if ( anchor.RefId == "ItemAnnotateActions" )  // HACK!
            {
                Core.ActionManager.RegisterMainMenuActionGroup( id, "Actions", "Flag With",
                    new ListAnchor( AnchorType.After, "ActionAnnotationsActions" ) );
            }
            else
            {
                Core.ActionManager.RegisterMainMenuActionGroup( id, "Actions", "Flag With", anchor );
            }
        }

        private static void RegisterFlagAction( IResource res, string actionGroup )
        {
            string flagID = res.GetStringProp( "FlagId" );
            string flagName = res.GetStringProp( "Name" );
            if( flagID != null && flagName != null )
            {
            	SetFlagAction setFlagAction = new SetFlagAction( flagID );
                Image icon = Utils.GetFlagResourceImage( res );
                Core.ActionManager.RegisterContextMenuAction( setFlagAction, actionGroup, ListAnchor.Last, flagName, icon, null, null );
                Core.ActionManager.RegisterMainMenuAction( setFlagAction, actionGroup, ListAnchor.Last, flagName, icon, null, null );
                _flagActionManager.RegisterAction( setFlagAction, actionGroup, ListAnchor.Last, flagName, icon, null, null );

                int keyDelta = _lastFlagKey++;
                Core.ActionManager.RegisterKeyboardAction( setFlagAction, Keys.Control | ( Keys.D0 + keyDelta ), null, null );
                Core.ActionManager.RegisterKeyboardAction( setFlagAction, Keys.Control | ( Keys.NumPad0 + keyDelta ), null, null );
            }
        }

        public void Draw( IResource res, Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;

            IResource flag = res.GetLinkProp( "Flag" );
            if ( flag != null )
            {
                Icon icon = _flagIconProvider.GetResourceIcon( flag );
                if ( icon != null )
                {
                    g.DrawIcon( icon, x, rc.Top );
                }
            }
            else if ( !Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal ) )
            {
                _imageList.Draw( g, x, rc.Top, 1 );   // FlagNoProp.ico
            }
        }

        public void DrawHeader( Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
            _imageList.Draw( g, x, rc.Top, 0 );
        }

        private delegate void AssignFlagsDelegate( ResourceFlag flag, IResourceList list );
        public void MouseClicked( IResource res, Point pt )
        {
            if ( !Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal ) )
            {
                ResourceFlag.ToggleFlag( res );

                //  If there is a command to propagate the flag over the whole
                //  thread (conversation) we need to set exactly the same flag
                //  on all resources, not just toggle their flags forward.
                if( (Control.ModifierKeys & Keys.Control) > 0 )
                {
                    PropagateFlag2Thread( res );
                }
            }
        }

        public static void  PropagateFlag2Thread( IResource res )
        {
            ResourceFlag currentFlag = ResourceFlag.GetResourceFlag( res );
            IResourceList subTree = ConversationBuilder.UnrollConversationFromCurrent( res );
            Core.ResourceAP.QueueJob( new AssignFlagsDelegate( AssignFlags2List ), currentFlag, subTree );
        }

        private static void AssignFlags2List( ResourceFlag flag, IResourceList list )
        {
            foreach( IResource resource in list )
            {
                if( flag != null )
                    flag.SetOnResource( resource );
                else
                    ResourceFlag.Clear( resource );
            }
        }

        public string GetTooltip( IResource res )
        {
            return null;
        }

        public static void ShowContextMenuSt( IActionContext context, Control ownerControl, Point pt )
        {
            _flagActionManager.ActionContext = context;
            _flagContextMenu.Show( ownerControl, pt );
            _isCtrlPressed = ((Control.ModifierKeys & Keys.Control) > 0 );
        }

        public bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt )
        {
            //  Workaround for OM-12972, do not show on the invisible contols.
            try
            {
                ShowContextMenuSt( context, ownerControl, pt );
                return true;
            }
            catch( System.ArgumentException )
            {
                return false;
            }
        }
    }

    /**
     * Icon provider for Flag resources.
     */

    public class FlagIconProvider: IResourceIconProvider
    {
        private static readonly HashMap _flagHashMap = new HashMap();    // flag ID -> Icon

        private static Icon LoadFlagIcon( IResource flag )
        {
            string flagID = flag.GetStringProp( "FlagId" );
            if ( flagID == null )
                return null;

            Icon icon = Utils.GetFlagResourceIcon( flag );
            if ( icon != null )
            {
                _flagHashMap [flagID] = icon;
            }

            return icon;
        }

        public Icon GetResourceIcon( IResource resource )
        {
            return DoGetResourceIcon( resource );
        }

        private static Icon DoGetResourceIcon( IResource resource )
        {
            string flagID = resource.GetStringProp( "FlagId" );
            if ( flagID != null )
            {
                if ( !_flagHashMap.Contains( flagID ) )
                {
                    return LoadFlagIcon( resource );
                }
                return (Icon) _flagHashMap [flagID];
            }
            return null;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return null;
        }

        public static string GetFlagIconFile( IResource flag )
        {
            string iconName = flag.GetStringProp( "IconName" );
            if ( iconName == null )
            {
                return null;
            }

            string tempDir = FileResourceManager.GetTrashDirectory();
            if ( !Directory.Exists( tempDir ) )
            {
                Directory.CreateDirectory( tempDir );
            }
            string path = Path.Combine( tempDir, iconName );
            if ( !File.Exists( path ) )
            {
                Icon icon = DoGetResourceIcon( flag );
                if ( icon == null )
                {
                    return null;
                }
                using( FileStream fs = new FileStream( path, FileMode.Create ) )
                {
                    icon.Save( fs );
                }
            }
            return path;
        }
    }

    internal class SetFlagAction: IAction
    {
        private readonly string _flagID;

        public SetFlagAction( string flagID )
        {
            _flagID = flagID;
        }

        public void Execute( IActionContext context )
        {
            IResourceList sel = context.SelectedResources;
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( SetFlags ), sel );

            //  Prepagate flag to the whole thread if the context menu was
            //  activated with the "Ctrl" key.
            if( FlagColumn._isCtrlPressed && ( context.Kind == ActionContextKind.ContextMenu ))
            {
                foreach( IResource res in sel )
                {
                    FlagColumn.PropagateFlag2Thread( res );
                }
            }
        }

        private void SetFlags( IResourceList resList )
        {
            ResourceFlag flag = new ResourceFlag( _flagID );
            foreach( IResource res in resList )
            {
                if ( !res.IsDeleted )
                {
                    flag.SetOnResource( res );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 ||
                ResourceTypeHelper.AnyResourcesInternal( context.SelectedResources ) )
            {
                if ( context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
            }
        }
    }

    internal class ClearFlagAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResourceList sel = context.SelectedResources;
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( ClearFlags ), sel );

            //  Prepagate flag clearing to the whole thread if the context
            //  menu was activated with the "Ctrl" key.
            if( FlagColumn._isCtrlPressed && ( context.Kind == ActionContextKind.ContextMenu ))
            {
                foreach( IResource res in sel )
                {
                    FlagColumn.PropagateFlag2Thread( res );
                }
            }
        }

        private static void ClearFlags( IResourceList resList )
        {
            foreach( IResource res in resList )
            {
                if ( !res.IsDeleted )
                {
                    res.DeleteLinks( "Flag" );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 ||
                ResourceTypeHelper.AnyResourcesInternal( context.SelectedResources ) )
            {
                if ( context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
                return;
            }

            bool anyHasFlag = false;
            for( int i = 0; i < context.SelectedResources.Count; i++ )
            {
                if ( context.SelectedResources[ i ].HasProp( "Flag" ) )
                {
                    anyHasFlag = true;
                    break;
                }
            }
            presentation.Enabled = anyHasFlag;
        }
    }

    internal class DisplayResourcesWithFlagAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource flag = context.SelectedResources [0];
            IResourceList resList = flag.GetLinksOfType( null, "Flag" );

            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.SelectResourceTypeTab( null );
            Core.UIManager.EndUpdateSidebar();

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Resources with " + flag.GetPropText( Core.Props.Name );
            options.SetTransientContainer( Core.ResourceTreeManager.ResourceTreeRoot,
                StandardViewPanes.ViewsCategories );
            Core.ResourceBrowser.DisplayResourceList( null, resList, options );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = (context.SelectedResources.Count == 1);
        }
    }

    public class FlagComparer: IResourceComparer
    {
        public int CompareResources( IResource r1, IResource r2 )
        {
            IResource flag1 = r1.GetLinkProp( "Flag" );
            IResource flag2 = r2.GetLinkProp( "Flag" );
            if ( flag1 == flag2 )
            {
                return 0;
            }
            if ( flag1 == null )
            {
                return 1;
            }
            if ( flag2 == null )
            {
                return -1;
            }
            if ( flag1 == ResourceFlag.DefaultFlag.Resource )
            {
                return -1;
            }
            if ( flag2 == ResourceFlag.DefaultFlag.Resource )
            {
                return 1;
            }
            if ( flag1.HasProp( -ResourceFlag.PropNextStateFlag ) )
            {
                return 1;
            }
            if ( flag2.HasProp( -ResourceFlag.PropNextStateFlag ) )
            {
                return -1;
            }
            return flag1.DisplayName.CompareTo( flag2.DisplayName );
        }
    }
}

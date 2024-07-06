// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Generic action for switching a display mode of a view.
    /// </summary>
    public class SwitchViewModeAction: IAction
    {
        private readonly string[] _resTypes;
        private readonly int _propToToggle;

        protected SwitchViewModeAction( string[] resTypes, int propToToggle )
        {
            _resTypes = resTypes;
            _propToToggle = propToToggle;
        }

        public virtual void Execute( IActionContext context )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate,
                new ToggleUnreadDelegate( ToggleViewMode ), GetResourcesFromContext( context ) );
        }

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList resources = GetResourcesFromContext( context );

            if( resources == null || resources.Count < 1 )
            {
                Trace.WriteLine( "Resources Counted: NULL or 0" );
                if ( context.Kind == ActionContextKind.Toolbar || context.Kind == ActionContextKind.MainMenu )
                {
                    presentation.Enabled = false;
                }
                else
                {
                    presentation.Visible = false;
                }
            }
            else
            {
                bool checked_ = GetCheckedState( resources );
                presentation.Checked = checked_;
            }
        }

        private bool GetCheckedState( IResourceList resources )
        {
            bool checked_ = false;
            foreach( IResource resource in resources.ValidResources )
            {
                if( !( checked_ = resource.HasProp( _propToToggle ) ) )
                {
                    break;
                }
            }
            return checked_;
        }

        private delegate void ToggleUnreadDelegate( IResourceList resources );

        private void ToggleViewMode( IResourceList resources )
        {
            bool checkedState = GetCheckedState( resources );
            foreach( IResource resource in resources.ValidResources )
            {
                resource.SetProp( _propToToggle, !checkedState );
                IResource owner = Core.ResourceBrowser.OwnerResource;
                if( resource == owner )
                {
                    Core.UIManager.QueueUIJob( new ResourceDelegate( RedisplayResource ), owner );
                }
            }
        }

        private IResourceList GetResourcesFromContext( IActionContext context )
        {
            IResourceList resources = context.SelectedResources;
            string[] resTypes = resources.GetAllTypes();
            if ( resTypes.Length > 0 )
            {
                bool haveWrongTypes = false;
                for( int i=0; i<resTypes.Length; i++ )
                {
                    if ( Array.IndexOf( _resTypes, resTypes [i] ) < 0 )
                    {
                        haveWrongTypes = true;
                        break;
                    }
                }

                if( !haveWrongTypes )
                {
                    return resources;
                }
            }

            if( context.ListOwnerResource != null &&
                Array.IndexOf( _resTypes, context.ListOwnerResource.Type ) >= 0 )
            {
                return context.ListOwnerResource.ToResourceList();
            }
            return null;
        }

        private static void RedisplayResource( IResource res )
        {
            AbstractViewPane viewPane = Core.LeftSidebar.GetPane( Core.LeftSidebar.ActivePaneId );
            if ( viewPane.SelectedResource == res )
            {
                // force redisplay of resource
                viewPane.SelectResource( res, false );
            }
        }
    }

    public class SwitchUnreadModeAction: SwitchViewModeAction
    {
        public SwitchUnreadModeAction( params string[] resTypes )
            : base( resTypes, Core.Props.DisplayUnread )
        {}
    }

    public class SwitchThreadedModeAction: SwitchViewModeAction
    {
        public SwitchThreadedModeAction( params string[] resTypes )
            : base( resTypes, Core.Props.DisplayThreaded )
        {}
    }

    public class SwitchNewspaperModeAction: SwitchViewModeAction
    {
        public SwitchNewspaperModeAction( params string[] resTypes )
            : base( resTypes, Core.Props.DisplayNewspaper )
        {}
    }
}

/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using System.Collections;

namespace JetBrains.Omea
{
    /**
     * An action which is composed of multiple actions contributed by different
     * plugins, and invokes the registered actions in order until one of them
     * matches the current context.
     */
	
    public class CompositeAction: IAction
	{
        private class ActionComponent
        {
        	public string               ResourceType;
            public IAction              Action;
            public IActionStateFilter[] Filters;

            internal ActionComponent( string resourceType, IAction action, IActionStateFilter[] filters )
            {
                ResourceType = resourceType;
                Action = action;
                Filters = filters;
            }
        }

        private string _id;
        private ArrayList _components = new ArrayList();

        public CompositeAction( string id )
        {
            _id = id;
            ActionManager actionManager = ICore.Instance.ActionManager as ActionManager;
            if ( actionManager != null )
            {
                actionManager.RegisterCompositeAction( id, this );
            }
        }

        /**
         * Adds a component to the composite action.
         */

        internal void AddComponent( string resType, IAction action, IActionStateFilter[] filters )
        {
        	_components.Add( new ActionComponent( resType, action, filters ) );
        }

        /**
         * Polls the components of the action until the enabled one is found and executes it.
         */
        
        public void Execute( IActionContext context )
        {
            if ( !ExecuteActionComponents( context ) )
            {
                SplitExecute( context );
            }
        }

        /**
         * Polls the components of the action and determines the aggregate state of the
         * action.
         */

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            string[] resTypes = null;

            if ( _components.Count == 0 )
            {
            	presentation.Visible = false;
                return;
            }

            bool isVisible = false;
            bool isDisabled = false;
            bool tabMatched = false;
            UpdateActionComponents( context, ref presentation, ref resTypes, ref isVisible, ref isDisabled,
                ref tabMatched );

            if ( isVisible )
                return;

            // if the resource types were never checked, none of the components are
            // resource type dependent
            if ( resTypes != null && resTypes.Length > 1 )
            {
                SplitUpdate( resTypes, context, ref presentation, ref tabMatched );
                if ( presentation.Visible && presentation.Enabled )
                    return;
            }

            if ( context.Kind == ActionContextKind.Toolbar && !tabMatched )
            {
            	presentation.Visible = false;
            }
            else if ( isDisabled || context.Kind == ActionContextKind.MainMenu ||
                context.Kind == ActionContextKind.Toolbar )
            {
                presentation.Visible = true;
            	presentation.Enabled = false;
            }
            else
            {
                presentation.Visible = false;
            }
        }

        private bool ExecuteActionComponents( IActionContext context )
        {
            string[] resTypes = null;

            ActionPresentation presentation = new ActionPresentation();
            foreach( ActionComponent component in _components )
            {
                bool tabMatched = false;
                UpdateComponentPresentation( component, context, ref presentation, ref resTypes, ref tabMatched );
                if ( presentation.Visible && presentation.Enabled )
                {
                    component.Action.Execute( context );
                    return true;
                }
            }
            return false;
        }

        private void UpdateActionComponents( IActionContext context, ref ActionPresentation presentation,
            ref string[] resTypes, ref bool isVisible, ref bool isDisabled, ref bool tabMatched )
        {
            foreach( ActionComponent component in _components )
            {
                UpdateComponentPresentation( component, context, ref presentation, ref resTypes, ref tabMatched );
                
                if ( presentation.Visible && presentation.Enabled )
                {
                    isVisible = true;
                    return;
                }

                if ( !presentation.Enabled )
                    isDisabled = true;
            }
        }

        /// <summary>
        /// Splits the resource list in multiple resource lists, each of which has only a
        /// single type, and tries to enable the action components with the split lists
        /// </summary>
        private void SplitUpdate( string[] resTypes, IActionContext context, ref ActionPresentation presentation,
            ref bool tabMatched )
        {
            IResourceList[] resLists = SplitResourceList( resTypes, context.SelectedResources );
            for( int i=0; i<resLists.Length; i++ )
            {
                tabMatched = false;
                ActionContext splitContext = new ActionContext( context, resLists [i] );
                string[] splitResTypes = new string[] { resTypes [i] };
                bool isVisible = false, isDisabled = false;
                UpdateActionComponents( splitContext, ref presentation, ref splitResTypes, 
                    ref isVisible, ref isDisabled, ref tabMatched );
                if ( presentation.Visible && presentation.Enabled )
                {
                    return;
                }
            }
        }

        private void SplitExecute( IActionContext context )
        {
            if ( context.SelectedResources != null )
            {
                string[] resTypes = context.SelectedResources.GetAllTypes();
                if ( resTypes.Length > 1 )
                {
                    IResourceList[] resLists = SplitResourceList( resTypes, context.SelectedResources );
                    for( int i=0; i<resLists.Length; i++ )
                    {
                        ActionContext splitContext = new ActionContext( context, resLists [i] );
                        ExecuteActionComponents( splitContext );
                    }
                }
            }
        }

        private static IResourceList[] SplitResourceList( string[] resTypes, IResourceList selectedResources )
        {
            IResourceList[] result = new IResourceList[ resTypes.Length ];
            for( int i=0; i<resTypes.Length; i++ )
            {
                result [i] = selectedResources.Intersect( Core.ResourceStore.GetAllResources( resTypes[ i ] ) );
            }
            return result;
        }

        private void UpdateComponentPresentation( ActionComponent component, IActionContext context,
            ref ActionPresentation presentation, ref string[] resTypes, ref bool tabMatched )
        {
            presentation.ResetState();

            if ( !CheckResourceTypes( component, context.SelectedResources, ref resTypes ) )
            {
                if ( resTypes != null && resTypes.Length == 0 )
                {
                    // we need to get tabMatched set correctly in this case
                    UpdateComponentFilters( component, context, ref presentation, ref tabMatched );
                }
                presentation.Visible = false;
                return;
            }

            UpdateComponentFilters( component, context, ref presentation, ref tabMatched );

            if ( presentation.Visible )
            {
                component.Action.Update( context, ref presentation );
            }
        }

        private static void UpdateComponentFilters( ActionComponent component, IActionContext context, 
            ref ActionPresentation presentation, ref bool tabMatched )
        {
            if ( component.Filters != null )
            {
                foreach( IActionStateFilter filter in component.Filters )
                {
                    filter.Update( context, ref presentation );
                    if ( !presentation.Visible )
                        break;
                    if ( filter is ActiveTabFilter )
                    {
                        tabMatched = true;
                    }
                }
            }
        }

        private bool CheckResourceTypes( ActionComponent component, IResourceList selectedResources,
            ref string[] resTypes )
        {
            if ( component.ResourceType != null )
            {
                if ( resTypes == null )
                {
                    resTypes = (selectedResources == null) 
                        ? new string[] {} 
                        : selectedResources.GetAllTypes();            		
                }
                if ( resTypes.Length != 1 || resTypes [0] != component.ResourceType )
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return "CompositeAction/" + _id;
        }
	}
}

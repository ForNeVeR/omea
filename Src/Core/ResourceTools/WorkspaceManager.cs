// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Specialized;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	#region Class WorkspaceManagerProps — Registers and provides the Workspace Manager properties.
	/// <summary>
	/// Registers and provides the Workspace Manager properties.
	/// </summary>
    public class WorkspaceManagerProps
    {
    	internal WorkspaceManagerProps( IResourceStore store )
    	{
    		store.ResourceTypes.Register( WorkspaceResourceType, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
    		store.ResourceTypes.Register( "WorkspaceOtherView", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

    		_propInWorkspace = ResourceTypeHelper.UpdatePropTypeRegistration( "InWorkspace", PropDataType.Link, PropTypeFlags.DirectedLink );
    		_propInWorkspaceRecursive = store.PropTypes.Register( "InWorkspaceRecursive", PropDataType.Link, PropTypeFlags.DirectedLink );
    		_propExcludeFromWorkspace = store.PropTypes.Register( "ExcludeFromWorkspace", PropDataType.Link, PropTypeFlags.DirectedLink );
    		store.PropTypes.RegisterDisplayName( _propInWorkspace, "In Workspace", "Resources" );
    		_propVisibleOrder = store.PropTypes.Register( "VisibleOrder", PropDataType.Int, PropTypeFlags.Internal );
    		_propWorkspaceHidden = store.PropTypes.Register( "WorkspaceHidden", PropDataType.Bool, PropTypeFlags.Internal );
			_propWorkspaceColor = store.PropTypes.Register( "WorkspaceColor", PropDataType.Int, PropTypeFlags.Internal );
			_propVisibleInAllWorkspaces = store.PropTypes.Register( "VisibleInAllWorkspaces", PropDataType.Bool, PropTypeFlags.Internal );

    		_propWorkspaceVisible = store.PropTypes.Register( "WorkspaceVisible", PropDataType.Link, PropTypeFlags.Internal );
    	}

    	private int _propInWorkspace;
    	private int _propInWorkspaceRecursive;
    	private int _propExcludeFromWorkspace;
    	private int _propVisibleOrder;
    	private int _propWorkspaceHidden;
    	private int _propWorkspaceVisible;
    	private int _propVisibleInAllWorkspaces;
    	private int _propWorkspaceColor;
		public int InWorkspace            { get { return _propInWorkspace; } }
    	public int InWorkspaceRecursive   { get { return _propInWorkspaceRecursive; } }
    	public int ExcludeFromWorkspace   { get { return _propExcludeFromWorkspace; } }

    	/// <summary>
    	/// A link between the workspace resource and those resources that should be included in this workspace.
    	/// </summary>
    	public int WorkspaceVisible       { get { return _propWorkspaceVisible; } }

    	public int VisibleInAllWorkspaces { get { return _propVisibleInAllWorkspaces; } }

    	/// <summary>
    	/// An <see cref="int"/> property that imposes sorting order on the workspaces
    	/// in the workspace buttons row and workspaces-editing dialog.
    	/// </summary>
    	public int VisibleOrder           { get { return _propVisibleOrder; } }

    	/// <summary>
    	/// A <see cref="bool"/> property that defines whether the workspace's button is hidden out from the workspace buttons row.
    	/// </summary>
    	public int WorkspaceHidden        { get { return _propWorkspaceHidden; } }

    	/// <summary>
    	/// The base workspace color.
    	/// This color is used
    	/// for identifying the workspace,
    	/// painting its border,
    	/// and deriving
    	/// the highlighted and darkened colors.
    	/// </summary>
    	public int WorkspaceColor         { get { return _propWorkspaceColor; } }

		/// <summary>
		/// Name of the default workpsace
		/// (one that contains all the resources and is represented by the <c>Null</c> <see cref="IResource"/> value).
		/// </summary>
		public string DefaultWorkspaceName { get { return "All"; } }

		/// <summary>
		/// Name of the resource type that represents the workspaces.
		/// </summary>
		public string WorkspaceResourceType { get { return "Workspace"; } }
    }

	#endregion

	#region Class WorkspaceManager — The class which manages workspace creation, editing and filtering.

	/// <summary>
	/// The class which manages workspace creation, editing and filtering.
	/// </summary>
	public class WorkspaceManager: IWorkspaceManager
	{
		private readonly IResourceStore _store;
		private readonly IResourceTreeManager _resourceTreeManager;
		private readonly WorkspaceManagerProps _props;

		private IResource _activeWorkspace;

		internal class WorkspaceTypeRec
		{
			internal string _resType;
			internal int[]  _linkPropIds;
			internal int    _recurseLinkPropId;
			internal WorkspaceResourceType _workspaceResType;

			internal WorkspaceTypeRec( string resType, int[] linkPropIDs,
			                           int recurseLinkPropId, WorkspaceResourceType wrType )
			{
				_resType     = resType;
				_linkPropIds = linkPropIDs;
				_recurseLinkPropId = recurseLinkPropId;
				_workspaceResType = wrType;
			}
		}

		private ArrayList _workspaceTypes = new ArrayList();
		private Hashtable _resTypeToWorkspaceRec = CollectionsUtil.CreateCaseInsensitiveHashtable();
		private Hashtable _availSelectorFilters = new Hashtable();
		private Hashtable _inWorkspaceSelectorFilters = new Hashtable();
		private bool _rebuildLinksNeeded;

        /// <summary>
        /// resource type -> name of tab in Workspaces dialog
        /// </summary>
        private Hashtable _workspaceTabNames = new Hashtable();

		public event EventHandler WorkspaceChanged;

		/// <summary>
		/// A live list of workspaces that is listened to for workspace deletions.
		/// </summary>
		protected IResourceList _workspaces;

		public WorkspaceManager( IResourceStore store, ResourceTreeManager resourceTreeManager, IPluginLoader pluginLoader )
		{
			_store = store;
			_resourceTreeManager = resourceTreeManager;
			_rebuildLinksNeeded = !_store.PropTypes.Exist( "WorkspaceVisible" );
			_props = new WorkspaceManagerProps( _store );
			UpdateOtherView();

			Core.ResourceStore.ResourceSaved += ResourceStore_OnResourceSaved;
			pluginLoader.RegisterResourceUIHandler( "WorkspaceOtherView",new WorkspaceOtherViewUIHandler( this ) );

			// Start listening for a possible deletion of the active workspace
			_workspaces = GetAllWorkspaces();
#pragma warning disable RedundantDelegateCreation
			_workspaces.ResourceDeleting += new ResourceIndexEventHandler(OnWorkspaceDeleting);
#pragma warning restore RedundantDelegateCreation
		}

		public WorkspaceManagerProps Props
		{
			get { return _props; }
		}

		private void UpdateOtherView()
		{
			IResourceList allWorkspaces = _store.GetAllResources( _props.WorkspaceResourceType );
			foreach( IResource res in allWorkspaces )
			{
				IResourceList otherLinks = res.GetLinksTo( "WorkspaceOtherView", _props.InWorkspace );
				if ( otherLinks.Count == 0 )
				{
					IResource otherView = _store.BeginNewResource( "WorkspaceOtherView" );
					otherView.SetProp( "Name", "Other" );
					otherView.AddLink( _props.InWorkspace, res );
					otherView.EndUpdate();
				}

				_resourceTreeManager.SetResourceNodeSort( res, "Type Name" );
			}
		}

		public void RegisterWorkspaceType( string resType, int[] linkPropIDs, WorkspaceResourceType wrType )
		{
			WorkspaceTypeRec rec = new WorkspaceTypeRec( resType, linkPropIDs, Core.Props.Parent, wrType );
			_workspaceTypes.Add( rec );
			_resTypeToWorkspaceRec [resType] = rec;
		}

		public void RegisterWorkspaceContainerType( string resType, int[] linkPropIds, int recurseLinkPropId )
		{
			WorkspaceTypeRec rec = new WorkspaceTypeRec( resType, linkPropIds,
			                                             recurseLinkPropId, WorkspaceResourceType.Container );
			_workspaceTypes.Add( rec );
			_resTypeToWorkspaceRec [resType] = rec;
		}

		public void RegisterWorkspaceFolderType( string resType, string contentType, int[] linkPropIDs )
		{
			WorkspaceTypeRec rec = new WorkspaceTypeRec( resType, linkPropIDs,
			                                             Core.Props.Parent, WorkspaceResourceType.Folder );
			_workspaceTypes.Add( rec );
			_resTypeToWorkspaceRec [resType] = rec;
		}

		public int WorkspaceTypeCount
		{
			get { return _workspaceTypes.Count; }
		}

		internal WorkspaceTypeRec GetWorkspaceTypeRec( string resType )
		{
			return (WorkspaceTypeRec) _resTypeToWorkspaceRec [resType];
		}

		public WorkspaceResourceType GetWorkspaceResourceType( string resType )
		{
			WorkspaceTypeRec rec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [resType];
			if ( rec == null )
			{
				return WorkspaceResourceType.None;
			}
			return rec._workspaceResType;
		}

		public int GetRecurseLinkPropId( string resType )
		{
			WorkspaceTypeRec rec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [resType];
			if ( rec == null )
			{
				return Core.Props.Parent;
			}
			return rec._recurseLinkPropId;
		}

		public void RegisterWorkspaceSelectorFilter( string resType, IResourceNodeFilter filter )
		{
			_availSelectorFilters [resType] = filter;
			_inWorkspaceSelectorFilters [resType] = filter;
		}

		public void RegisterWorkspaceSelectorFilter( string resType, IResourceNodeFilter availTreeFilter,
		                                             IResourceNodeFilter workspaceTreeFilter )
		{
			_availSelectorFilters [resType] = availTreeFilter;
			_inWorkspaceSelectorFilters [resType] = workspaceTreeFilter;
		}

		public IResourceNodeFilter GetAvailSelectorFilter( string resType )
		{
			return (IResourceNodeFilter) _availSelectorFilters [resType];
		}

		public IResourceNodeFilter GetInWorkspaceSelectorFilter( string resType )
		{
			return (IResourceNodeFilter) _inWorkspaceSelectorFilters [resType];
		}

		public IResource ActiveWorkspace
		{
			get { return _activeWorkspace; }
			set
			{
				if ( _activeWorkspace != value )
				{
					_activeWorkspace = value;
					if ( WorkspaceChanged != null )
					{
						WorkspaceChanged( this, EventArgs.Empty );
					}
				}
			}
		}

		public string GetWorkspaceType( int index )
		{
			return ((WorkspaceTypeRec) _workspaceTypes [index])._resType;
		}

		/// <summary>
		/// Creates a workspace with the specified name.
		/// </summary>
		public IResource CreateWorkspace( string name )
		{
			ResourceProxy proxy = ResourceProxy.BeginNewResource( _props.WorkspaceResourceType );
			proxy.SetProp( "Name", name );
			proxy.EndUpdate();

			ResourceProxy otherViewProxy = ResourceProxy.BeginNewResource( "WorkspaceOtherView" );
			otherViewProxy.SetProp( "Name", "Other" );
			otherViewProxy.AddLink( _props.InWorkspace, proxy.Resource );
			otherViewProxy.EndUpdate();

			_resourceTreeManager.SetResourceNodeSort( proxy.Resource, "Type Name" );
			return proxy.Resource;
		}

		/// <summary>
		/// Deletes the specified workspace.
		/// </summary>
		public void DeleteWorkspace( IResource workspace )
		{
			IResourceList otherViewList = workspace.GetLinksTo( "WorkspaceOtherView", _props.InWorkspace );
			if ( otherViewList.Count > 0 )
			{
				new ResourceProxy( otherViewList [0] ).Delete();
			}

			new ResourceProxy( workspace ).Delete();
		}


		/// <summary>
		/// Returns a live list of all the workspaces.
		/// </summary>
		public IResourceList GetAllWorkspaces()
		{
			return Core.ResourceStore.GetAllResourcesLive( _props.WorkspaceResourceType );
		}

		public void AddResourceToWorkspace( IResource workspace, IResource res )
		{
			if ( workspace == null )
				throw new ArgumentNullException( "workspace" );

			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceDelegate( AddResourceToWorkspace ),
				                              workspace, res );
				return;
			}

			if ( res.HasLink( Props.ExcludeFromWorkspace, workspace ) )
			{
				new ResourceProxy( res ).DeleteLink( Props.ExcludeFromWorkspace, workspace );
			}
			else
			{
				res.AddLink( Props.InWorkspace, workspace );
			}
			ProcessWorkspaceVisibleLink( workspace, res, LinkChangeType.Add, false, null );
		}

		public void AddResourcesToWorkspace( IResource workspace, IResourceList resList )
		{
			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceListDelegate( AddResourcesToWorkspace ),
				                              workspace, resList );
				return;
			}

			foreach( IResource res in resList )
			{
				AddResourceToWorkspace( workspace, res );
			}
		}

		public void AddResourceToWorkspaceRecursive( IResource workspace, IResource res )
		{
			if ( workspace == null )
				throw new ArgumentNullException( "workspace" );

			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceDelegate( AddResourceToWorkspaceRecursive ),
				                              workspace, res );
				return;
			}

			RemoveLinksRecursive( workspace, res, _props.InWorkspace );
			res.DeleteLink( _props.InWorkspace, workspace );
			// we need to delete WorkspaceVisibleLink in order to get the recursive links
			// built correctly by ProcessWorkspaceVisibleLink() (#5554)
			workspace.DeleteLink( Props.WorkspaceVisible, res );

			res.AddLink( Props.InWorkspaceRecursive, workspace );
			ProcessWorkspaceVisibleLink( workspace, res, LinkChangeType.Add, true, null );
		}

		public void AddResourcesToWorkspaceRecursive( IResource workspace, IResourceList resList )
		{
			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceListDelegate( AddResourcesToWorkspaceRecursive ),
				                              workspace, resList );
				return;
			}

			foreach( IResource res in resList )
			{
				AddResourceToWorkspaceRecursive( workspace, res );
			}
		}

		public void CheckRebuildWorkspaceLinks()
		{
			if ( _rebuildLinksNeeded )
			{
				foreach( IResource workspace in Core.ResourceStore.GetAllResources( _props.WorkspaceResourceType ) )
				{
					foreach( IResource wsRes in workspace.GetLinksTo( null, _props.InWorkspace ) )
					{
						ProcessWorkspaceVisibleLink( workspace, wsRes, LinkChangeType.Add, false, null );
					}

					foreach( IResource wsRes in workspace.GetLinksTo( null, Props.InWorkspaceRecursive ) )
					{
						ProcessWorkspaceVisibleLink( workspace, wsRes, LinkChangeType.Add, true, null );
					}
				}
			}
		}

		private void ProcessWorkspaceVisibleLink( IResource workspace, IResource res, LinkChangeType changeType,
		                                          bool recursive, IResource[] filterResources )
		{
			if ( ( changeType == LinkChangeType.Add && res.HasLink( Props.WorkspaceVisible, workspace ) ||
				( changeType == LinkChangeType.Delete && !res.HasLink( Props.WorkspaceVisible, workspace) ) ) )
			{
				return;
			}

			if ( changeType == LinkChangeType.Add )
			{
				res.AddLink( Props.WorkspaceVisible, workspace );
			}
			else
			{
				if ( HaveLinksToFilterResources( res, filterResources ) )
				{
					return;
				}
				res.DeleteLink( Props.WorkspaceVisible, workspace );
			}

			WorkspaceTypeRec wrType = (WorkspaceTypeRec) _resTypeToWorkspaceRec [res.Type];
			if ( wrType != null )
			{
				foreach( int linkPropId in wrType._linkPropIds )
				{
				    IResourceList linkList = GetWorkspaceLinks( res, linkPropId );
				    foreach( IResource linkRes in linkList )
					{
						ProcessWorkspaceVisibleLink( workspace, linkRes, changeType, false, filterResources );
					}
				}
			}
			if ( recursive )
			{
				int parentLinkType = GetRecurseLinkPropId( res.Type );
				foreach( IResource childRes in res.GetLinksTo( null, parentLinkType ) )
				{
					if ( !childRes.HasLink( Props.ExcludeFromWorkspace, workspace ) )
					{
						ProcessWorkspaceVisibleLink( workspace, childRes, changeType, true, filterResources );
					}
				}
			}
		}

	    private static IResourceList GetWorkspaceLinks( IResource res, int linkPropId )
	    {
	        IResourceList linkList;
	        if ( Core.ResourceStore.PropTypes [linkPropId].HasFlag( PropTypeFlags.DirectedLink ) )
	        {
	            if ( linkPropId < 0 )
	            {
	                linkList = res.GetLinksTo( null, -linkPropId );
	            }
	            else
	            {
	                linkList = res.GetLinksFrom( null, linkPropId );
	            }
	        }
	        else
	        {
	            linkList = res.GetLinksOfType( null, linkPropId );
	        }
	        return linkList;
	    }

	    private bool HaveLinksToFilterResources( IResource res, IResource[] filterResources )
		{
			if ( filterResources == null )
			{
				return false;
			}
			for( int i=0; i<filterResources.Length; i++ )
			{
				WorkspaceTypeRec rec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [filterResources [i].Type];
				for( int j=0; j<rec._linkPropIds.Length; j++ )
				{
					if ( res.HasLink( rec._linkPropIds [j], filterResources [i] ) )
					{
						return true;
					}
				}
			}
			return false;
		}

		private void ResourceStore_OnResourceSaved( object sender, ResourcePropEventArgs e )
		{
			if ( Props.WorkspaceVisible != 0 && e.Resource.HasProp( Props.WorkspaceVisible ) && e.Resource.Type != _props.WorkspaceResourceType )
			{
				IResourceList wsList = null;

				int recurseLinkProp = GetRecurseLinkPropId( e.Resource.Type );
				if ( e.ChangeSet.IsPropertyChanged( -recurseLinkProp ) )
				{
					if ( wsList == null )
					{
						wsList = e.Resource.GetLinksOfType( null, Props.WorkspaceVisible );
					}
					foreach( IResource ws in wsList )
					{
						if ( IsInWorkspaceRecursive( ws, e.Resource ) )
						{
							LinkChange[] linkChanges = e.ChangeSet.GetLinkChanges( -recurseLinkProp );
							ProcessWorkspaceLinkChanges( ws, linkChanges );
						}
					}
				}

				WorkspaceTypeRec wrType = (WorkspaceTypeRec) _resTypeToWorkspaceRec [e.Resource.Type];
				if ( wrType != null )
				{
					foreach( int linkPropId in wrType._linkPropIds )
					{
						if ( e.ChangeSet.IsPropertyChanged( linkPropId ) )
						{
							if ( wsList == null )
							{
								wsList = e.Resource.GetLinksOfType( null, Props.WorkspaceVisible );
							}
							LinkChange[] linkChanges = e.ChangeSet.GetLinkChanges( linkPropId );
							foreach( IResource workspace in wsList )
							{
								ProcessWorkspaceLinkChanges( workspace, linkChanges );
							}
						}
					}
				}
			}
		}

		public bool IsInWorkspaceRecursive( IResource workspace, IResource res )
		{
			int recurseLinkProp = GetRecurseLinkPropId( res.Type );
            while( res != null )
			{
				if ( res.HasLink( Props.InWorkspaceRecursive, workspace ) )
				{
					return true;
				}
				res = res.GetLinkProp( recurseLinkProp );
			}
			return false;

		}

		private void ProcessWorkspaceLinkChanges( IResource workspace, LinkChange[] linkChanges )
		{
			bool haveFilterResources = false;
			IResource[] filterResources = null;
			foreach( LinkChange chg in linkChanges )
			{
                try
                {
				    IResource target = Core.ResourceStore.LoadResource( chg.TargetId );
				    if ( chg.ChangeType == LinkChangeType.Delete && !haveFilterResources )
				    {
					    haveFilterResources = true;
					    filterResources = GetFilterResources( workspace );
				    }
				    ProcessWorkspaceVisibleLink( workspace, target, chg.ChangeType, true, filterResources );
                }
                catch( ResourceDeletedException )
                {
                    //  Fix for OM-13516, do not crash on already deleted resources.
                }
			}
		}

		public void AddToActiveWorkspace( IResource res )
		{
			AddToActiveWorkspace( res, false );
		}

		public void AddToActiveWorkspaceRecursive( IResource res )
		{
			AddToActiveWorkspace( res, true );
		}

		/// <summary>
		/// If a workspace is active and none of the parents of the specified
		/// resource belong to it, link the resource to the active workspace.
		/// </summary>
		private void AddToActiveWorkspace( IResource res, bool recursive )
		{
			// prevent changing _activeWorkspace under our feet (#2207)
			IResource activeWS = _activeWorkspace;
			if ( activeWS != null )
			{
				IResource parent = res;
				bool parentInWorkspace = false;
				while( parent != null )
				{
					if ( parent.HasLink( Props.InWorkspaceRecursive, activeWS ) )
					{
						parentInWorkspace = true;
						break;
					}
					parent = parent.GetLinkProp( GetRecurseLinkPropId( parent.Type ) );
				}
				if ( !parentInWorkspace )
				{
					if ( recursive )
					{
						AddResourceToWorkspaceRecursive( activeWS, res );
					}
					else
					{
						AddResourceToWorkspace( activeWS, res );
					}
				}
			}
		}

		public void RemoveResourceFromWorkspace( IResource workspace, IResource res )
		{
			if ( workspace == null )
				throw new ArgumentNullException( "workspace" );

			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceDelegate( RemoveResourceFromWorkspace ),
				                              workspace, res );
				return;
			}

			if ( res.HasLink( Props.InWorkspaceRecursive, workspace ) )
			{
				RemoveLinksRecursive( workspace, res, Props.ExcludeFromWorkspace );
				res.DeleteLink( Props.InWorkspaceRecursive, workspace );
			}
			else if ( res.HasLink( _props.InWorkspace, workspace ) )
			{
				res.DeleteLink( _props.InWorkspace, workspace );
			}
			else
			{
				IResource parent = res.GetLinkProp( GetRecurseLinkPropId( res.Type ) );
				while( parent != null )
				{
					if ( parent.HasLink( Props.InWorkspaceRecursive, workspace ) )
					{
						res.AddLink( Props.ExcludeFromWorkspace, workspace );
						break;
					}
					parent = parent.GetLinkProp( GetRecurseLinkPropId( parent.Type ) );
				}
			}
			IResource[] filterResources = GetFilterResources( workspace );
			ProcessWorkspaceVisibleLink( workspace, res, LinkChangeType.Delete, true, filterResources );
		}

		private IResource[] GetFilterResources( IResource workspace )
		{
			ArrayList result = ArrayListPool.Alloc();
            try
            {
                IResourceList resources = workspace.GetLinksTo( null, Props.InWorkspace ).Union(
                    workspace.GetLinksTo( null, Props.InWorkspaceRecursive ) );
                foreach( IResource res in resources )
                {
                    WorkspaceTypeRec rec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [res.Type];
                    if ( rec != null && rec._workspaceResType == WorkspaceResourceType.Filter )
                    {
                        result.Add( res );
                    }
                }
                if ( result.Count == 0 )
                {
                    return null;
                }
                return (IResource[]) result.ToArray( typeof (IResource) );
            }
            finally
            {
                ArrayListPool.Dispose( result );
            }
		}

		private void RemoveLinksRecursive( IResource workspace, IResource res, int propId )
		{
			foreach( IResource child in res.GetLinksTo( null, GetRecurseLinkPropId( res.Type ) ) )
			{
				if ( child.HasLink( propId, workspace ) )
				{
					child.DeleteLink( propId, workspace );
				}
				RemoveLinksRecursive( workspace, child, propId );
			}
		}

		private delegate void WorkspaceResourceDelegate( IResource workspace, IResource res );
		private delegate void WorkspaceResourceListDelegate( IResource workspace, IResourceList res );

		public void RemoveResourcesFromWorkspace( IResource workspace, IResourceList list )
		{
			if ( !Core.ResourceStore.IsOwnerThread() )
			{
				Core.ResourceAP.RunUniqueJob( new WorkspaceResourceListDelegate( RemoveResourcesFromWorkspace ),
				                              workspace, list );
				return;
			}

			foreach( IResource res in list )
			{
				RemoveResourceFromWorkspace( workspace, res );
			}
		}

		/// <summary>
		/// Returns the list of resources belonging to the workspace which have
		/// the specified type.
		/// </summary>
		public IResourceList GetWorkspaceResources( IResource workspace, string resType )
		{
			return workspace.GetLinksTo( resType, _props.InWorkspace );
		}

		/// <summary>
		/// Returns the list of all resources of the specified types which belong to the
		/// workspace.
		/// </summary>
		public IResourceList GetWorkspaceResources( IResource workspace, string[] resTypes )
		{
			IResourceList result = null;
			foreach( string resType in resTypes )
			{
				result = GetWorkspaceResources( workspace, resType ).Union( result );
			}
			if ( result == null )
			{
				return Core.ResourceStore.EmptyResourceList;
			}
			return result;
		}

		/// <summary>
		/// Returns the live list of resources belonging to the workspace which have
		/// the specified type (or all types, if resType is null).
		/// </summary>
		public IResourceList GetWorkspaceResourcesLive( IResource workspace, string resType )
		{
			if ( workspace == null )
				throw new ArgumentNullException( "workspace" );
			return workspace.GetLinksToLive( resType, _props.InWorkspace ).Union(
				workspace.GetLinksToLive( resType, Props.InWorkspaceRecursive ) );
		}

		/// <summary>
		/// Returns the list of resources filtered by the specified workspace.
		/// </summary>
		public IResourceList GetFilterList( IResource workspace )
		{
			if ( workspace == null )
				return null;

			return workspace.GetLinksOfTypeLive( null, Props.WorkspaceVisible );
		}

		/// <summary>
		/// Checks if the specified workspace can possibly have resources that belong to the
		/// workspace but not to any of the containers.
		/// </summary>
		public bool HasResourcesOutsideContainers( IResource workspace )
		{
			if ( workspace == null )
				return false;

			IResourceList resList = workspace.GetLinksTo( null, _props.InWorkspace );
			for( int i = 0; i < resList.Count; i++ )
			{
                //  Workaround of OM-13038 and related.
                try
                {
				    if ( resList [ i ].Type != "WorkspaceOtherView" )
				    {
					    WorkspaceTypeRec typeRec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [resList [ i ].Type];

					    if ( typeRec == null || typeRec._workspaceResType == WorkspaceResourceType.Filter ||
						    typeRec._workspaceResType == WorkspaceResourceType.None )
					    {
						    return true;
					    }
				    }
                }
                catch( OpenAPI.InvalidResourceIdException )
                {
                    //  Nothing to do, just ignore
                }
			}

			return false;
		}

		/// <summary>
		/// Returns the list of resources of the specified type that belong to the specified
		/// workspace but not to any containers in that workspace.
		/// </summary>
		public IResourceList GetResourcesOutsideContainers( IResource workspace )
		{
			if ( workspace == null )
				return Core.ResourceStore.EmptyResourceList;

            IResourceList resourcesInContainers = null;
            foreach( IResource res in workspace.GetLinksTo( null, Props.InWorkspace ).ValidResources )
            {
                FillResourcesInContainers( workspace, res, ref resourcesInContainers, false );
            }
            foreach( IResource res in workspace.GetLinksTo( null, Props.InWorkspaceRecursive ).ValidResources )
            {
                FillResourcesInContainers( workspace, res, ref resourcesInContainers, true );
            }

            if ( resourcesInContainers == null )
            {
                return GetFilterList( workspace );
            }
            return GetFilterList( workspace ).Minus( resourcesInContainers );
		}

	    private void FillResourcesInContainers( IResource workspace, IResource res,
            ref IResourceList resourcesInContainers, bool recursive )
	    {
            WorkspaceTypeRec rec = (WorkspaceTypeRec) _resTypeToWorkspaceRec [res.Type];
            if ( rec == null || rec._workspaceResType == WorkspaceResourceType.None ||
                rec._workspaceResType == WorkspaceResourceType.Filter )
            {
                return;
            }

            for( int i=0; i<rec._linkPropIds.Length; i++ )
            {
                resourcesInContainers = GetWorkspaceLinks( res, rec._linkPropIds [i] ).Union( resourcesInContainers );
            }

            if ( recursive )
            {
                foreach( IResource child in res.GetLinksTo( null, rec._recurseLinkPropId ) )
                {
                    if ( !child.HasLink( _props.ExcludeFromWorkspace, workspace ) )
                    {
                        FillResourcesInContainers( workspace, child, ref resourcesInContainers, true );
                    }
                }
            }
	    }

	    public IResourceList GetResourceWorkspaces( IResource resource )
		{
			return resource.GetLinksOfType( null, Props.WorkspaceVisible );
		}

		public bool IsResourceInWorkspace( IResource workspace, IResource res )
		{
			if ( workspace.Type != _props.WorkspaceResourceType )
				throw new ArgumentException( "Resource of wrong type passed as 'workspace'", "workspace" );

			return res.HasLink( Props.WorkspaceVisible, workspace );
		}

		/// <summary>
		/// A workspace is about to be deleted.
		/// </summary>
		private void OnWorkspaceDeleting(object sender, ResourceIndexEventArgs e)
		{
			// If the active workspace is being deleted, select the default one
			if(ActiveWorkspace == e.Resource)
				ActiveWorkspace = null;
		}

	    public void SetWorkspaceTabName( string resourceType, string tabName )
	    {
	        Guard.ValidResourceType( resourceType, "resourceType" );
            _workspaceTabNames [resourceType] = tabName;
	    }

        public string GetWorkspaceTabName( string resourceType )
        {
            return (string) _workspaceTabNames [resourceType];
        }

        /// <summary>
        /// If the parent of the specified resource is recursively added to a workspace,
        /// deletes the direct links from this resource to the workspace.
        /// </summary>
        /// <param name="res">The resource for which the links should be cleaned.</param>
        public void CleanWorkspaceLinks( IResource res )
        {
            int recurseLink = GetRecurseLinkPropId( res.Type );
            IResource parent =  res.GetLinkProp( recurseLink );
            if ( parent != null )
            {
                foreach( IResource wsp in Core.ResourceStore.GetAllResources( "Workspace" ) )
                {
                    if ( IsInWorkspaceRecursive( wsp, parent ) )
                    {
                        res.DeleteLink( Props.InWorkspace, wsp );
                        res.DeleteLink( Props.InWorkspaceRecursive, wsp );
                    }
                }
            }
        }
	}

	#endregion

	#region Class WorkspaceOtherViewUIHandler — The UI handler for WorkspaceOtherView.

	/// <summary>
	/// The UI handler for WorkspaceOtherView.
	/// </summary>
	internal class WorkspaceOtherViewUIHandler: IResourceUIHandler
	{
		private WorkspaceManager _workspaceManager;
		private IResource _lastWorkspace = null;
		private IResourceList _lastWorkspaceWatchList;
		private IResourceList _resList = null;
		private bool _workspaceChanged = false;

		internal WorkspaceOtherViewUIHandler( WorkspaceManager manager )
		{
			_workspaceManager = manager;
		}

		public void ResourceNodeSelected( IResource res )
		{
			IResource workspace = res.GetLinkProp( "InWorkspace" );
			if ( workspace != null )
			{
				if ( _lastWorkspace != workspace )
				{
					_resList = _workspaceManager.GetResourcesOutsideContainers( workspace );
					SetWorkspaceWatchList( workspace );
				}
				else if ( _workspaceChanged )
				{
					_resList = _workspaceManager.GetResourcesOutsideContainers( workspace );
				}

                string[] resTypesArray;
			    ArrayList nonInternalTypes = ArrayListPool.Alloc();
                try
                {
                    foreach( IResourceType rt in Core.ResourceStore.ResourceTypes )
                    {
                        if ( !rt.HasFlag( ResourceTypeFlags.Internal ) )
                        {
                            nonInternalTypes.Add( rt.Name );
                        }
                    }
                    resTypesArray = (string[]) nonInternalTypes.ToArray( typeof ( string ) );
                }
                finally
                {
                    ArrayListPool.Dispose( nonInternalTypes );
                }
			    _resList = _resList.Intersect( Core.ResourceStore.GetAllResources( resTypesArray ), true );
				Core.ResourceBrowser.DisplayResourceList( res, _resList,
				                                          "Resources in " + workspace.GetPropText( Core.Props.Name ),
				                                          null );
			}
		}

		private void SetWorkspaceWatchList( IResource workspace )
		{
			if ( _lastWorkspaceWatchList != null )
			{
				_lastWorkspaceWatchList.ResourceChanged -= OnWorkspaceChanged;
				_lastWorkspaceWatchList.Dispose();
			}

			_lastWorkspace = workspace;
			_lastWorkspaceWatchList = _lastWorkspace.ToResourceListLive();
			_lastWorkspaceWatchList.ResourceChanged += OnWorkspaceChanged;
			_workspaceChanged = false;
		}

		private void OnWorkspaceChanged( object sender, ResourcePropIndexEventArgs e )
		{
			_workspaceChanged = true;
		}

		public bool CanDropResources( IResource targetResource, IResourceList dragResources )
		{
			return false;
		}

		public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
		{
		}

		public bool CanRenameResource( IResource res )
		{
			return false;
		}

		public bool ResourceRenamed( IResource res, string newName )
		{
			return false;
		}
	}

	#endregion
}

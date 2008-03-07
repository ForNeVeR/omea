/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;

using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.CustomTreeView;

namespace JetBrains.Omea.OutlookPlugin
{
	internal class MAPIFolderTreeView : CustomTreeView
	{
		private IResource _mapiFolderRoot;
		private IResourceIconManager _resourceIconManager;
		private HashNode _rootNode = null;
		private string _folderType = "";
		private string _resourceTypeForIcons = "MAPIFolder";

		public void Init( string folderType, string resourceTypeForIcons )
		{
			_folderType = folderType;
			_resourceTypeForIcons = resourceTypeForIcons;
			ThreeStateCheckboxes = true;
			AfterThreeStateCheck +=new ThreeStateCheckEventHandler(_treeView_AfterThreeStateCheck);
			_resourceIconManager = ICore.Instance.ResourceIconManager;
			ImageList = ICore.Instance.ResourceIconManager.ImageList;
			_mapiFolderRoot = ICore.Instance.ResourceTreeManager.GetRootForType( STR.MAPIFolder );
			_rootNode = new HashNode( _mapiFolderRoot );
		}

		public void Save()
		{
			Save( Nodes );
		}
		private void Save( TreeNodeCollection nodes )
		{
			foreach ( TreeNode node in nodes )
			{
				IResource resource = (IResource)node.Tag;
				if ( resource != null )
				{
					bool ignore = ( GetNodeCheckState( node ) == NodeCheckState.Unchecked );
					Folder.SetIgnoreImportAsync( resource, _folderType, ignore );
				}
				Save( node.Nodes );
			}        
		}

		private HashMap _checkStates = new HashMap();

		public void CollectCheckStates( )
		{
			_checkStates.Clear();
			CollectCheckStates( Nodes );
		}

		private void CollectCheckStates( TreeNodeCollection nodes )
		{
			foreach ( TreeNode node in nodes )
			{
				IResource resource = (IResource)node.Tag;
				if ( resource != null )
				{
					NodeCheckState checkState = GetNodeCheckState( node );

					if ( resource.Id != -1 && ( checkState == NodeCheckState.Checked || checkState == NodeCheckState.Unchecked ) )
					{
						_checkStates.Add( resource.Id, checkState );
					}
				}
				CollectCheckStates( node.Nodes );
			}
		}
		public void ClearTree()
		{
			Nodes.Clear();
		}

		public void ShowTree()
		{
			CollectCheckStates();
			ClearTree();
			PopulateTree();
		}
		public void PopulateTree()
		{
			foreach ( IResource localAddressBook in Folder.GetFolders( _folderType ) )
			{
				AddLocalAddressBook( localAddressBook );
			}
			InsertTreeNodes( Nodes, _rootNode );
			ExpandAll();
		}

		private void InsertTreeNodes( TreeNodeCollection nodes, HashNode hashNode )
		{
			foreach ( HashMap.Entry entry in hashNode.HashNodes )
			{
				IResource resource = ((HashNode)entry.Value).Resource;
                
				if ( resource.Id == -1 || ( resource.Type == STR.MAPIFolder ) ) 
				{
					IResource store = Folder.GetMAPIStorage( resource );
					if ( store.HasProp( PROP.IgnoredFolder ) )
					{
						continue;
					}
				}
                
				int iconIndex = 0;
				IResource resourceTag = null;
				if ( Folder.IsFolderOfType( resource, _folderType ) )
				{
					iconIndex = _resourceIconManager.GetDefaultIconIndex( _resourceTypeForIcons );
					resourceTag = resource;
				}
				else
				{
					iconIndex = _resourceIconManager.GetDefaultIconIndex( "MAPIFolder" );
				}
				TreeNode treeNode = new TreeNode( resource.DisplayName, iconIndex, iconIndex );
				treeNode.Tag = resourceTag;
				nodes.Add( treeNode );
				SetNodeCheckStateFromCollection( treeNode );
				InsertTreeNodes( treeNode.Nodes, (HashNode)entry.Value );
			}
		}

		public void SetNodeCheckStateFromCollection( TreeNode node )
		{
			IResource resourceTag = (IResource)node.Tag;
			if ( resourceTag != null )
			{
				HashMap.Entry checkEntry = _checkStates.GetEntry( resourceTag.Id );
				NodeCheckState check = NodeCheckState.Checked;
				if ( checkEntry != null )
				{
					check = (NodeCheckState)checkEntry.Value;
				}
				else
				{
					if ( !Folder.IsIgnoreImport( resourceTag ) )
					{
						check =  NodeCheckState.Checked;
					}
					else
					{
						check = NodeCheckState.Unchecked;
					}
				}
				SetNodeCheckState( node, check );
			}
		}

		private void _treeView_AfterThreeStateCheck(object sender, ThreeStateCheckEventArgs e)
		{
			NodeCheckState checkState = GetNodeCheckState( e.Node );
			SetCheckStateRecursively( e.Node.Nodes, checkState );
		}
		private void SetCheckStateRecursively( TreeNodeCollection nodes, NodeCheckState parentCheckState )
		{
			foreach ( TreeNode node in nodes )
			{
				NodeCheckState checkState = GetNodeCheckState( node );
				if ( checkState == NodeCheckState.Unchecked || checkState == NodeCheckState.Checked )
				{
					SetNodeCheckState( node, parentCheckState );
				}
				SetCheckStateRecursively( node.Nodes, parentCheckState );
			}
		}
		private HashNode AddLocalAddressBook( IResource mapiFolder )
		{
			if ( Folder.IsDeletedItems( mapiFolder ) )
			{
				Folder.SetIgnoreImportAsync( mapiFolder, _folderType, true );
				return null;
			}
			IResource parentFolder = Folder.GetParent( mapiFolder );
			if ( parentFolder == null )
			{
				ResourceProxy proxy = new ResourceProxy( mapiFolder );
				proxy.DeleteAsync();
				return null;
			}
			if ( Folder.IsRoot( parentFolder ) )
			{
				return _rootNode.InsertResource( mapiFolder );
			}
			else
			{
				HashNode hasNode = 
					AddLocalAddressBook( parentFolder );
				if ( hasNode == null )
				{
					Folder.SetIgnoreImportAsync( mapiFolder, _folderType, true );
					return null;
				}
				return hasNode.InsertResource( mapiFolder );
			}
		}
	}
}
/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.CustomTreeView;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.Diagnostics;
using System.IO;

namespace JetBrains.Omea.GUIControls
{
	public class SendResourcesDialog : DialogBase
	{
        private IResourceList _selectedResources;
        private IResourceIconManager _resourceIconManager;
        private IResourceStore _resourceStore;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnSend;
        private CustomTreeView _resourceTreeView;
        private ResourceSerializer _resourceSerializer = new ResourceSerializer();
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _fileLength;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SendResourcesDialog( IResourceList selectedResources )
		{
            if ( selectedResources == null ) throw new System.ArgumentNullException( "selectedResources" );
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            _fileLength.ReadOnly = true;
            _resourceTreeView.ThreeStateCheckboxes = true;
            _resourceTreeView.AfterThreeStateCheck += new JetBrains.UI.Components.CustomTreeView.ThreeStateCheckEventHandler(AfterThreeStateCheck);
            _btnSend.Click += new EventHandler( OnSend );
            _selectedResources = selectedResources;
            _resourceIconManager = Core.ResourceIconManager;
            _resourceStore = Core.ResourceStore;
            _resourceTreeView.ImageList = Core.ResourceIconManager.ImageList;
            PopulateTreeView();
		}

        private int GetIcon( IResource resource )
        {
            return _resourceIconManager.GetIconIndex( resource );
        }
        private bool IsSerializerExists( IResource resource )
        {
            return Core.PluginLoader.GetResourceSerializer( resource.Type ) != null;
        }
        private string GetTypeDisplayName( string type )
        {
            return Core.ResourceStore.ResourceTypes[type].DisplayName;
        }
        private void AddSelectedResource( IResource selectedResource )
        {
            int iconIndex = GetIcon( selectedResource );
            TreeNode treeNode = new TreeNode( GetTypeDisplayName( selectedResource.Type ) + ": " + selectedResource.DisplayName, iconIndex, iconIndex );

            ResourceNode resourceNode = _resourceSerializer.AddResource( selectedResource );
            treeNode.Tag = new SerializableTag( resourceNode, SerializableTag.Type.SelectedResource );
            _resourceTreeView.Nodes.Add( treeNode );
            AddProperties( treeNode, resourceNode );
            treeNode.Expand();
        }

        private void AddLink( ResourceNode resourceNode, IResourceProperty property, TreeNode parentNode )
        {
            int iconIndex = Core.ResourceIconManager.GetPropTypeIconIndex( property.PropId );
            string linkDisplayName = _resourceStore.PropTypes.GetPropDisplayName( property.PropId );
            Tracer._Trace( "LINKNAME = " + linkDisplayName +  " LINKID = " + property.PropId.ToString() );

            TreeNode treeSubNode = 
                new TreeNode( linkDisplayName, iconIndex, iconIndex );
            LinkNode linkNode = resourceNode.AddLink( linkDisplayName, property.Name, property.PropId < 0 );

            treeSubNode.Tag = new SerializableTag( linkNode, SerializableTag.Type.Link );
            parentNode.Nodes.Add( treeSubNode );
            _resourceTreeView.SetNodeCheckState( treeSubNode, NodeCheckState.Checked );
            if ( AddLinkedResources( treeSubNode, resourceNode.Resource, property.PropId, linkNode ) == 0 )
            {
                parentNode.Nodes.Remove( treeSubNode );
            }
        }
        private void AddProperty( ResourceNode resourceNode, IResourceProperty property, TreeNode parentNode )
        {
            int iconIndex = Core.ResourceIconManager.GetPropTypeIconIndex( property.PropId );
            string linkDisplayName = _resourceStore.PropTypes.GetPropDisplayName( property.PropId );
            TreeNode treeSubNode = 
                new TreeNode( linkDisplayName, iconIndex, iconIndex );
            PropertyNode propertyNode = resourceNode.AddProperty( property );
            treeSubNode.Tag = new SerializableTag( propertyNode, SerializableTag.Type.Link );
            parentNode.Nodes.Add( treeSubNode );
            _resourceTreeView.SetNodeCheckState( treeSubNode, NodeCheckState.Checked );
        }
        private SerializationMode GetSerializationMode( IResourceSerializer serializer, IResource resource, IResourceProperty property )
        {
            SerializationMode serMode = serializer.GetSerializationMode( resource, property.Name );
            if ( serMode == SerializationMode.Default )
            {
                IPropType propType = Core.ResourceStore.PropTypes [property.Name];
                if ( propType.HasFlag( PropTypeFlags.AskSerialize ) )
                {
                    return SerializationMode.AskSerialize;
                }
                if ( propType.HasFlag( PropTypeFlags.NoSerialize ) )
                {
                    return SerializationMode.NoSerialize;
                }
                if ( property.DataType == PropDataType.Link && propType.HasFlag( PropTypeFlags.Internal ) )
                {
                    return SerializationMode.NoSerialize;
                }
                return SerializationMode.Serialize;
            }
            return serMode;
        }
        private void AddProperties( TreeNode treeNode, ResourceNode resourceNode )
        {
            IResourceSerializer serializer = Core.PluginLoader.GetResourceSerializer( resourceNode.Resource.Type );
            if ( serializer == null ) return;

            IPropertyCollection properties = resourceNode.Resource.Properties;
            foreach ( IResourceProperty property in properties )
            {
                SerializationMode serMode = GetSerializationMode( serializer, resourceNode.Resource, property );
                if ( serMode == SerializationMode.NoSerialize ) continue;

                if ( property.DataType == PropDataType.Link )
                {
                    if ( serMode == SerializationMode.Serialize || serMode == SerializationMode.AskSerialize )
                    {
                        AddLink( resourceNode, property, treeNode );
                    }
                }
                else
                {
                    if ( serMode == SerializationMode.AskSerialize )
                    {
                        AddProperty( resourceNode, property, treeNode );
                    }
                    else
                    {
                        resourceNode.AddProperty( property );
                    }
                }
            }
        }

        private int AddLinkedResources( TreeNode treeNode, IResource selectedResource, int linkID, LinkNode linkNode )
        {
            bool directed = _resourceStore.PropTypes [linkID].HasFlag( PropTypeFlags.DirectedLink );
            IResourceList resources = null;
            if ( directed )
            {
                if ( linkID < 0 )
                {
                    resources = selectedResource.GetLinksTo( null, -linkID );
                }
                else
                {
                    resources = selectedResource.GetLinksFrom( null, linkID );
                }
            }
            else
            {
                resources = selectedResource.GetLinksOfType( null, linkID );
            }

            int count = 0;
            foreach ( IResource resource in resources )
            {
                if ( IsSerializerExists( resource ) )
                {
                    int iconIndex = GetIcon( resource );
                    Tracer._Trace( resource.DisplayName );
                    TreeNode treeSubNode = new TreeNode( GetTypeDisplayName( resource.Type ) + ": " + resource.DisplayName, iconIndex, iconIndex );
                    ResourceNode resourceNode = linkNode.AddResource( resource );
                    IPropertyCollection properties = resourceNode.Resource.Properties;
                    foreach ( IResourceProperty property in properties )
                    {
                        if ( property.DataType != PropDataType.Link )
                        {
                            IResourceSerializer serializer = Core.PluginLoader.GetResourceSerializer( resourceNode.Resource.Type );
                            SerializationMode serMode = GetSerializationMode( serializer, resourceNode.Resource, property );
                            if ( serMode == SerializationMode.NoSerialize ) continue;
                            resourceNode.AddProperty( property );
                        }
                    }

                    treeSubNode.Tag = new SerializableTag( resourceNode, SerializableTag.Type.LinkedResource );
                    treeNode.Nodes.Add( treeSubNode );
                    _resourceTreeView.SetNodeCheckState( treeSubNode, NodeCheckState.Checked );
                    count++;
                }
            }
            return count;
        }

        private void PopulateTreeView()
        {
            foreach ( IResource resource in _selectedResources )
            {
                if ( IsSerializerExists( resource ) )
                {
                    AddSelectedResource( resource );
                }
            }
            _fileLength.Text = _resourceSerializer.GenerateXML( ResourceSerializer.ResourceTransferFileName ).ToString();
        }
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnSend = new System.Windows.Forms.Button();
            this._resourceTreeView = new JetBrains.UI.Components.CustomTreeView.CustomTreeView();
            this.label1 = new System.Windows.Forms.Label();
            this._fileLength = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(352, 304);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 27);
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "Cancel";
            // 
            // _btnSend
            // 
            this._btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSend.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnSend.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSend.Location = new System.Drawing.Point(268, 304);
            this._btnSend.Name = "_btnSend";
            this._btnSend.Size = new System.Drawing.Size(75, 27);
            this._btnSend.TabIndex = 2;
            this._btnSend.Text = "Create Email";
            // 
            // _resourceTreeView
            // 
            this._resourceTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceTreeView.ImageIndex = -1;
            this._resourceTreeView.Location = new System.Drawing.Point(8, 8);
            this._resourceTreeView.Name = "_resourceTreeView";
            this._resourceTreeView.NodePainter = null;
            this._resourceTreeView.SelectedImageIndex = -1;
            this._resourceTreeView.Size = new System.Drawing.Size(420, 288);
            this._resourceTreeView.TabIndex = 0;
            this._resourceTreeView.ThreeStateCheckboxes = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(12, 312);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Size in bytes:";
            // 
            // _fileLength
            // 
            this._fileLength.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._fileLength.Location = new System.Drawing.Point(96, 308);
            this._fileLength.Name = "_fileLength";
            this._fileLength.TabIndex = 1;
            this._fileLength.Text = "";
            // 
            // SendResourcesDialog
            // 
            this.AcceptButton = this._btnSend;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(440, 342);
            this.Controls.Add(this._resourceTreeView);
            this.Controls.Add(this._fileLength);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnSend);
            this.Name = "SendResourcesDialog";
            this.Text = "Send Resources";
            this.ResumeLayout(false);

        }
		#endregion

        private void SetNodeCheckState( TreeNode treeNode, NodeCheckState checkState )
        {
            SerializableTag linkedResourceNodeTag = (SerializableTag)treeNode.Tag;
            linkedResourceNodeTag.AcceptSending = ( checkState != NodeCheckState.Unchecked );
            _resourceTreeView.SetNodeCheckState( treeNode, checkState );
        }

        private void AfterThreeStateCheck(object sender, JetBrains.UI.Components.CustomTreeView.ThreeStateCheckEventArgs e)
        {
            SerializableTag nodeTag = (SerializableTag)e.Node.Tag;
            if ( nodeTag.TreeNodeType == SerializableTag.Type.Link )
            {
                NodeCheckState curState = _resourceTreeView.GetNodeCheckState( e.Node );
                nodeTag.AcceptSending = ( curState == NodeCheckState.Checked );
                foreach ( TreeNode treeNode in e.Node.Nodes )
                {
                    SetNodeCheckState( treeNode, curState );
                }
            } 
            else if ( nodeTag.TreeNodeType == SerializableTag.Type.LinkedResource )
            {
                nodeTag.AcceptSending = ( _resourceTreeView.GetNodeCheckState( e.Node ) ) == NodeCheckState.Checked;
                TreeNode parentNode = e.Node.Parent;
                bool thereChecked = false;
                bool thereUnchecked = false;
                foreach ( TreeNode treeNode in parentNode.Nodes )
                {
                    NodeCheckState curState = _resourceTreeView.GetNodeCheckState( treeNode );
                    if ( curState == NodeCheckState.Checked )
                    {
                        thereChecked = true;
                    }
                    else if ( curState == NodeCheckState.Unchecked )
                    {
                        thereUnchecked = true;
                    }
                    if ( thereChecked && thereUnchecked )
                    {
                        SetNodeCheckState( parentNode, NodeCheckState.Grayed );
                        break;
                    }
                }
                if ( thereChecked && !thereUnchecked )
                {
                    SetNodeCheckState( parentNode, NodeCheckState.Checked );
                }
                if ( !thereChecked && thereUnchecked )
                {
                    SetNodeCheckState( parentNode, NodeCheckState.Unchecked );
                }
            }
            _fileLength.Text = _resourceSerializer.GenerateXML( ResourceSerializer.ResourceTransferFileName ).ToString();
        }

        private void OnSend(object sender, System.EventArgs e)
        {
            
            IEmailService emailService = (IEmailService) Core.PluginLoader.GetPluginService( typeof(IEmailService) );
            if ( emailService != null )
            {
                string fullFileName = Path.Combine( Path.GetTempPath(), ResourceSerializer.ResourceTransferFileName );
                _resourceSerializer.GenerateXML( fullFileName );
                string[] attachments = new string[1];
                attachments[0] = fullFileName;
                emailService.CreateEmail( null, null, EmailBodyFormat.PlainText, (EmailRecipient[]) null, attachments, true );
                File.Delete( fullFileName );
            }
        }
        public class SerializableTag
        {
            public enum Type
            {
                SelectedResource,
                Link,
                LinkedResource
            }

            private SerializableNode _serNode;
            private SerializableTag.Type _type;

            public SerializableTag( SerializableNode serNode, SerializableTag.Type type )
            {
                _serNode = serNode;
                _type = type;
            }
            public SerializableTag.Type TreeNodeType { get { return _type; } }
            public bool AcceptSending
            { 
                get { return _serNode.AcceptSending; }
                set { _serNode.AcceptSending = value; }
            }
        }
	}
}

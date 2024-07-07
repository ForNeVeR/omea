// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    internal class UnknownFileResource : UserControl, IResourceIconProvider, IResourceDisplayer, IDisplayPane
    {
        private UnknownFileResource()
        {
            InitializeComponent();

            IResourceTypeCollection resTypes = Core.ResourceStore.ResourceTypes;
            if( !resTypes.Exist( _unknowFileResourceType ) )
            {
                resTypes.Register( _unknowFileResourceType, "Unknown File", "Name",
                    ResourceTypeFlags.NoIndex | ResourceTypeFlags.FileFormat );
            }
            else
            {
                resTypes[ _unknowFileResourceType ].ResourceDisplayNameTemplate = "Name";
            }
            Core.ResourceIconManager.RegisterResourceIconProvider( _unknowFileResourceType, this );
            Core.PluginLoader.RegisterResourceDisplayer( _unknowFileResourceType, this );
            (Core.FileResourceManager as FileResourceManager).RegisterFileTypeColumns( _unknowFileResourceType );
            Core.StateChanged += new EventHandler( Core_StateChanged );
        }

        public static void Register()
        {
            new UnknownFileResource();
        }

        private void Core_StateChanged( object sender, EventArgs e )
        {
            if( Core.State == CoreState.Running )
            {
                Core.StateChanged -= new EventHandler( Core_StateChanged );
                Core.ResourceAP.QueueJobAt(
                    DateTime.Now.AddSeconds( 20 ), new MethodInvoker( UnknownFileResource.UpdateResources ) );
            }
        }

        private static void UpdateResources()
        {
            string[] lastTypes = ObjectStore.ReadString( "UnknownFiles", "LastConf" ).Split( ';' );
            string[] types = (Core.FileResourceManager as FileResourceManager).GetResourceTypes();
            if( types.Length == lastTypes.Length )
            {
                bool needUpdate = false;
                Array.Sort( types );
                Array.Sort( lastTypes );
                for( int i = 0; i < types.Length; ++i )
                {
                    if( types[ i ] != lastTypes[ i ] )
                    {
                        needUpdate = true;
                        break;
                    }
                }
                // plugin configuration not changed
                if( !needUpdate )
                {
                    return;
                }
            }
            IResourceList unknownRcs = Core.ResourceStore.GetAllResources( _unknowFileResourceType );
            IProgressWindow progressWindow = Core.ProgressWindow;
            for( int i = 0, percents = 0; i < unknownRcs.Count && Core.State != CoreState.ShuttingDown; ++i )
            {
                if( progressWindow != null )
                {
                    progressWindow.UpdateProgress( percents / unknownRcs.Count, "Updating unknown resources", null );
                    percents += 100;
                }
                IResource unknown = unknownRcs[ i ];
                IResource source = FileResourceManager.GetSource( unknown );
                if( source != null )
                {
                    string resourceType = Core.FileResourceManager.GetResourceTypeByExtension(
                        IOTools.GetExtension( unknown.GetPropText( Core.Props.Name ) ) );
                    if( resourceType != null )
                    {
                        unknown.ChangeType( resourceType );
                    }
                }
            }
            foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
            {
                if( resType.HasFlag( ResourceTypeFlags.FileFormat ) && !resType.OwnerPluginLoaded )
                {
                    IResourceList formatFiles = Core.ResourceStore.GetAllResources( resType.Name );
                    foreach( IResource formatFile in formatFiles )
                    {
                        formatFile.ChangeType( _unknowFileResourceType );
                    }
                }
            }
            StringBuilder confString = new StringBuilder();
            foreach( string restype in types )
            {
                confString.Append( restype );
                confString.Append( ";" );
            }
            ObjectStore.WriteString( "UnknownFiles", "LastConf", confString.ToString().TrimEnd( ';' ) );
        }

        #region IResourceIconProvider members

        public Icon GetResourceIcon( IResource resource )
        {
            IResource source = FileResourceManager.GetSource( resource );
            Icon icon;
            if( source == null || source.Type != "Weblink" )
            {
                icon = FileIcons.GetFileSmallIcon( resource.GetPropText( Core.Props.Name ) );
            }
            else
            {
                string path = string.Empty;
                try
                {
                    Uri uri = new Uri( source.GetPropText( "URL" ) );
                    string query = uri.Query;
                    if( query.Length > 1 )
                    {
                        query = query.Substring( 1 ); // skip leading '?'
                        int i = query.IndexOf( '=' );
                        if( i >= 0 && i < query.Length )
                        {
                            query = query.Substring( i + 1 );
                        }
                        if( query.Length > 0 )
                        {
                            return FileIcons.GetFileSmallIcon( query );
                        }
                    }
                    path = uri.LocalPath;
                }
                catch {}
                icon = FileIcons.GetFileSmallIcon( path );
            }
            return icon;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return null;
        }

        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return this;
        }

        #endregion

        #region IDisplayPane Members

        public string GetSelectedText( ref JetBrains.Omea.OpenAPI.TextFormat format )
        {
            return null;
        }

        public string GetSelectedPlainText()
        {
            return null;
        }

        public Control GetControl()
        {
            return this;
        }

        public void EndDisplayResource( IResource resource )
        {
        }

        public void HighlightWords( WordPtr[] words )
        {
        }

        public void DisposePane()
        {
        }

        public void DisplayResource( IResource resource )
        {
            _displayedResource = resource;
            string path = null;
            try
            {
                path = Core.FileResourceManager.GetSourceFile( resource );
            }
            catch( Exception e )
            {
                Utils.DisplayException( e, "Error" );
            }
            if( path != null && path.Length > 0 )
            {
                string fileType = FileSystemTypes.GetFileType( Path.GetExtension( path ) );
                if( fileType == null || fileType.Length == 0 )
                {
                    fileType = Path.GetExtension( path );
                }
                _infoLabel.Text = Core.ProductName + " is unable to process files of type \"" +
                    fileType + "\". To open this resource in associated application, click the button below.";
                _openButton.Visible = true;
                return;
            }
            _infoLabel.Text = Core.ProductName + " failed to determine type of this resource, it cannot be displayed :(";
            _openButton.Visible = false;
        }

        #endregion

        #region ICommandProcessor Members

        public void ExecuteCommand( string action )
        {
        }

        public bool CanExecuteCommand( string action )
        {
            return false;
        }

        #endregion

        private void _openButton_Click(object sender, System.EventArgs e)
        {
            Core.FileResourceManager.OpenSourceFile( _displayedResource );
        }

        private void UnknownFileResource_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            e.Handled = Core.ActionManager.ExecuteKeyboardAction(
                new ActionContext( ActionContextKind.Keyboard, this, _displayedResource.ToResourceList() ), e.KeyData );
        }

        private void InitializeComponent()
        {
            this._infoLabel = new System.Windows.Forms.TextBox();
            this._openButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _infoLabel
            //
            this._infoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._infoLabel.BackColor = System.Drawing.SystemColors.Control;
            this._infoLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._infoLabel.Location = new System.Drawing.Point(8, 8);
            this._infoLabel.Multiline = true;
            this._infoLabel.Name = "_infoLabel";
            this._infoLabel.ReadOnly = true;
            this._infoLabel.Size = new System.Drawing.Size(432, 40);
            this._infoLabel.TabIndex = 0;
            this._infoLabel.Text = "";
            //
            // _openButton
            //
            this._openButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._openButton.Location = new System.Drawing.Point(8, 56);
            this._openButton.Name = "_openButton";
            this._openButton.Size = new System.Drawing.Size(220, 23);
            this._openButton.TabIndex = 1;
            this._openButton.Text = "Open in associated application";
            this._openButton.Click += new System.EventHandler(this._openButton_Click);
            //
            // UnknownFileResource
            //
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this._openButton);
            this.Controls.Add(this._infoLabel);
            this.Name = "UnknownFileResource";
            this.Size = new System.Drawing.Size(448, 352);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UnknownFileResource_KeyDown);
            this.ResumeLayout(false);

        }


        public const string _unknowFileResourceType = "UnknownFile";
        private IResource _displayedResource;
        private System.Windows.Forms.TextBox _infoLabel;
        private System.Windows.Forms.Button _openButton;
    }
}

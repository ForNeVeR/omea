// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	public class ResourceListLinkLabel : UserControl
	{
		private System.ComponentModel.Container components = null;
	    private IResourceList _resources;

		public ResourceListLinkLabel()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
                ListDispose();
			}
			base.Dispose( disposing );
		}

        private void ListDispose()
        {
            if( _resources != null )
            {
                _resources.ResourceAdded -= new ResourceIndexEventHandler( _resources_Updated );
                _resources.ResourceDeleting -= new ResourceIndexEventHandler( _resources_Updated );
                _resources.Dispose();
                _resources = null;
            }
        }

        public IResourceList ResourceList
        {
            get { return _resources; }
            set
            {
                ListDispose();
                if( ( _resources = value ) != null )
                {
                    _resources.ResourceAdded += new ResourceIndexEventHandler( _resources_Updated );
                    _resources.ResourceDeleting += new ResourceIndexEventHandler( _resources_Updated );
                }
                ReDraw();
            }
        }

	    private void _resources_Updated( object sender, ResourceIndexEventArgs e )
        {
            ReDraw();
        }

	    private void ReDraw()
	    {
            if( InvokeRequired )
            {
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( ReDraw ) );
                return;
            }
	        SuspendLayout();
	        try
	        {
	            Controls.Clear();
	            if( _resources != null )
	            {
                    lock( _resources )
                    {
                        ResourceLinkLabel lastLabel = null;
                        foreach( IResource res in _resources )
                        {
                            if( !res.IsDeleted )
                            {
                                ResourceLinkLabel label = new ResourceLinkLabel();
                                label.Resource = res;
                                int rightBound = 0;
                                int bottomBound = 0;
                                if( lastLabel != null )
                                {
                                    rightBound = lastLabel.Left + lastLabel.Width + 1;
                                    bottomBound = lastLabel.Top;
                                }
                                if( rightBound + label.Width > Width - SystemInformation.VerticalScrollBarWidth )
                                {
                                    rightBound = 0;
                                    bottomBound += label.Height + 1;
                                }
                                label.Left = rightBound;
                                label.Top = bottomBound;
                                Controls.Add( label );
                                lastLabel = label;
                            }
                        }
                    }
	            }
	        }
	        finally
	        {
	            ResumeLayout();
                Invalidate();
	        }
	    }

	    #region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            //
            // ResourceListLinkLabel
            //
            this.AutoScroll = true;
            this.Name = "ResourceListLinkLabel";
            this.Size = new System.Drawing.Size(412, 40);
            this.Resize += new System.EventHandler(this.ResourceListLinkLabel_Resize);

        }
		#endregion

        private void ResourceListLinkLabel_Resize(object sender, System.EventArgs e)
        {
            ReDraw();
        }
	}
}

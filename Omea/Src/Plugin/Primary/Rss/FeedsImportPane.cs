/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for FeedsImportPane.
	/// </summary>
	public class FeedsImportPane: AbstractOptionsPane
	{
		private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox _lstImportFrom;
        private System.Windows.Forms.CheckBox _chkPreview;

        private string[] _importersNames = null;
        private Hashtable _subPanes = null;
        private AbstractWizardPane _previewPane = null;
        private ImportManager _manager = null;


	    internal FeedsImportPane( ImportManager manager )
	    {
	        // This call is required by the Windows.Forms Form Designer.
	        InitializeComponent();

            _subPanes = new Hashtable();
            _manager = manager;
            _importersNames = new string[ _manager.Importers.Count ];
            int i = 0;
            foreach( string name in _manager.Importers.Keys )
            {
                _importersNames[ i++ ] = name;
            }

            //  Recall the sate of the checkbox from the last session.
            _chkPreview.Checked = Core.SettingStore.ReadBool( "RSS", "PreviewImport", false );
	    }

	    /// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

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
            this.label1 = new System.Windows.Forms.Label();
            this._lstImportFrom = new System.Windows.Forms.CheckedListBox();
            this._chkPreview = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(376, 23);
            this.label1.TabIndex = 1;
            this.label1.Text = "Import Feeds subscriptions from:";
            // 
            // _lstImportFrom
            // 
            this._lstImportFrom.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lstImportFrom.CheckOnClick = true;
            this._lstImportFrom.Location = new System.Drawing.Point(8, 32);
            this._lstImportFrom.Name = "_lstImportFrom";
            this._lstImportFrom.Size = new System.Drawing.Size(376, 260);
            this._lstImportFrom.TabIndex = 2;
            this._lstImportFrom.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this._importFrom_itemChecked);
            // 
            // _chkPreview
            // 
            this._chkPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._chkPreview.Enabled = false;
            this._chkPreview.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkPreview.Location = new System.Drawing.Point(8, 296);
            this._chkPreview.Name = "_chkPreview";
            this._chkPreview.Size = new System.Drawing.Size(376, 24);
            this._chkPreview.TabIndex = 3;
            this._chkPreview.Text = "Pre&view subscriptions before import";
            this._chkPreview.CheckedChanged += new System.EventHandler(this._chkPreview_CheckedChanged);
            // 
            // FeedsImportPane
            // 
            this.Controls.Add(this._chkPreview);
            this.Controls.Add(this._lstImportFrom);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "FeedsImportPane";
            this.Size = new System.Drawing.Size(392, 328);
            this.ResumeLayout(false);

        }
		#endregion

        public override void ShowPane()
        {
            if( _manager.Wizard == null )
            {
                _chkPreview.Hide();
            }
            foreach ( string importer in _importersNames )
            {
                _lstImportFrom.Items.Add( importer, false );
            }
        }

        private void _importFrom_itemChecked( object sender, ItemCheckEventArgs e )
        {
            string name = _importersNames[ e.Index ];
            IFeedImporter importer = (IFeedImporter) _manager.Importers[ name ];
            _manager.SelectImporter( _importersNames[ e.Index ], e.NewValue == CheckState.Checked );
            if( importer.HasSettings )
            {
                string paneName = ImportManager.ImportPaneName + "/" + name;
                if( e.CurrentValue != CheckState.Checked && e.NewValue == CheckState.Checked )
                {
                    if( _manager.Wizard != null )
                    {
                        _subPanes[ name ] = new OptionsPaneWizardAdapter( name, importer.GetSettingsPaneCreator() );
                        _manager.Wizard.CurrentPane.RegisterPane( e.Index + 1, (AbstractWizardPane) _subPanes[ name ] );
                    }
                    else
                    {
                        Core.UIManager.RegisterWizardPane( paneName, importer.GetSettingsPaneCreator(), e.Index );
                    }
                }
                else if( e.CurrentValue == CheckState.Checked && e.NewValue != CheckState.Checked )
                {
                    if( _manager.Wizard != null )
                    {
                        _manager.Wizard.CurrentPane.DeregisterPane( (AbstractWizardPane)_subPanes[ name ] );
                    }
                    else
                    {
                        Core.UIManager.DeRegisterWizardPane( paneName );
                    }
                }
            }
            if( _manager.ImportPossible != _chkPreview.Enabled )
            {
                _chkPreview.Enabled = _manager.ImportPossible;
                AddPreviewPane( _chkPreview.Enabled && _chkPreview.Checked );
            }
        }

	    private void AddPreviewPane( bool add )
	    {
            if( _manager.Wizard == null )
            {
                return;
            }
            if( add )
            {
                if( _previewPane == null )
                {
                    _previewPane = new PreviewSubscriptionsPaneAdapter( "Preview subscriptions", new OptionsPaneCreator( CreatePreviewPane ) );
                }
                _manager.Wizard.RegisterPane( 0xFFFF, _previewPane );
            }
            else
            {
                if( _previewPane != null )
                {
                    _manager.Wizard.DeregisterPane( _previewPane );
                }
            }
        }

	    private void _chkPreview_CheckedChanged(object sender, System.EventArgs e)
        {
            Core.SettingStore.WriteBool( "RSS", "PreviewImport", _chkPreview.Checked );
            if( _manager.Wizard == null )
            {
                return;
            }
            AddPreviewPane( _chkPreview.Enabled && _chkPreview.Checked );
        }

	    private AbstractOptionsPane CreatePreviewPane()
	    {
	        return new PreviewSubscriptionsPane( _manager );
	    }
	}
}

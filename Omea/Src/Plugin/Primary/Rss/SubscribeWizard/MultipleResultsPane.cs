// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
	/// <summary>
	/// Summary description for MultipleResultsPane.
	/// </summary>
	internal class MultipleResultsPane : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.Label label1;
        private JetListView _resultList;
        private System.Windows.Forms.Button btnSelAll;
        private System.Windows.Forms.Button btnUnselAll;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private JetListViewColumn _nameColumn;
        private JetListViewColumn _urlColumn;
        private CheckBoxColumn _checkColumn;
        private MultiLineColumnScheme _columnScheme;
        private bool _haveAvailableResults;

        public event EventHandler NextPage;

        //  Any step pane must be able to control the possibility to move
        //  further (via button Next) depending on the internal state.
        internal SubscribeForm.CanMoveNextDelegate  NextPredicate;

		public MultipleResultsPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            this.label1.Text = Core.ProductName + " has found multiple feeds for the site. Please select the feeds to which y" +
                "ou want to subscribe:";

            _checkColumn = new CheckBoxColumn();
            _checkColumn.AfterCheck += new CheckBoxEventHandler(_checkColumn_AfterCheck);
            _nameColumn = new JetListViewColumn();
            _nameColumn.ItemTextCallback = new ItemTextCallback( GetNameColumnText );
            _nameColumn.ForeColorCallback = new ItemColorCallback( GetNameColumnForeColor );
            _nameColumn.FontCallback = new ItemFontCallback( GetNameColumnFont );
            _nameColumn.CursorCallback = new ItemCursorCallback( GetNameColumnCursor );
            _nameColumn.MouseDown += new ItemMouseEventHandler( HandleNameColumnMouseDown );
            _urlColumn = new JetListViewColumn();
            _urlColumn.ItemTextCallback = new ItemTextCallback( GetUrlColumnText );
            _resultList.Columns.AddRange( new JetListViewColumn[] { _checkColumn, _nameColumn, _urlColumn } );
            _resultList.MultiLineView = true;
            _resultList.ControlPainter = new GdiControlPainter();
            _resultList.FullRowSelect = true;

            _columnScheme = new MultiLineColumnScheme();
            _resultList.ColumnScheme = _columnScheme;
            _columnScheme.AddColumn( _checkColumn, 0, 0, 0, 20, ColumnAnchor.Left, SystemColors.ControlText,
                HorizontalAlignment.Left );
            _columnScheme.AddColumn( _nameColumn, 0, 0, 20, 80, ColumnAnchor.Left | ColumnAnchor.Right, SystemColors.ControlText,
                HorizontalAlignment.Left );
            _columnScheme.AddColumn( _urlColumn, 1, 1, 20, 80, ColumnAnchor.Left | ColumnAnchor.Right, SystemColors.ControlText,
                HorizontalAlignment.Left );
        }

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
            this._resultList = new JetListView();
            this.btnSelAll = new System.Windows.Forms.Button();
            this.btnUnselAll = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(328, 32);
            this.label1.TabIndex = 0;
            this.label1.Text = "OmniaMea has found multiple feeds for the site. Please select the feed to which " +
                               "you want to subscribe:";
            this.label1.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            //
            // _resultPanel
            //
            this._resultList.Location = new System.Drawing.Point(8, 64);
            this._resultList.KeyDown += new KeyEventHandler( OnResultKeyDown );
            this._resultList.Name = "_resultList";
            this._resultList.Size = new System.Drawing.Size(280, 324);
            this._resultList.TabIndex = 1;
            this._resultList.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            //
            // btnSelAll
            //
            this.btnSelAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnSelAll.Location = new System.Drawing.Point(295, 64);
            this.btnSelAll.Text = "Select All";
            this.btnSelAll.Name = "btnSelAll";
            this.btnSelAll.Size = new System.Drawing.Size(75, 24);
            this.btnSelAll.Click += new EventHandler(btnSelAll_Click);
            this.btnSelAll.TabIndex = 2;
            this.btnSelAll.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            //
            // btnUnselAll
            //
            this.btnUnselAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnUnselAll.Location = new System.Drawing.Point(295, 94);
            this.btnUnselAll.Text = "Unselect All";
            this.btnUnselAll.Name = "btnUnselAll";
            this.btnUnselAll.Size = new System.Drawing.Size(75, 24);
            this.btnUnselAll.Click += new EventHandler(btnUnselAll_Click);
            this.btnUnselAll.TabIndex = 3;
            this.btnUnselAll.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            //
            // MultipleResultsPane
            //
            this.Controls.Add(this._resultList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSelAll);
            this.Controls.Add(this.btnUnselAll);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.Name = "MultipleResultsPane";
            this.Size = new System.Drawing.Size(370, 396);
            this.ResumeLayout(false);
        }

        #endregion

        public void ShowResults( RSSDiscover discover )
	    {
            _haveAvailableResults = false;
            _resultList.Nodes.Clear();
            foreach( RSSDiscover.RSSDiscoverResult result in discover.Results )
            {
                _resultList.Nodes.Add( result );
                if ( result.ExistingFeed != null )
                {
                    _checkColumn.SetItemCheckState( result, CheckBoxState.Grayed );
                }
                else
                {
                    if ( !_haveAvailableResults )
                    {
                        _checkColumn.SetItemCheckState( result, CheckBoxState.Checked );
                    }
                    _haveAvailableResults = true;
                }
            }
            NextPredicate( _haveAvailableResults );
	    }

        public bool HaveAvailableResults()
        {
            return _haveAvailableResults;
        }

        public RSSDiscover.RSSDiscoverResult[] GetSelectedResults()
        {
            ArrayList result = new ArrayList();
            foreach( JetListViewNode node in _resultList.Nodes )
            {
                if ( _checkColumn.GetItemCheckState( node.Data ) == CheckBoxState.Checked )
                {
                    result.Add( node.Data );
                }
            }
            return (RSSDiscover.RSSDiscoverResult[]) result.ToArray( typeof (RSSDiscover.RSSDiscoverResult) );
        }

        private void OnResultKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Enter && NextPage != null )
            {
                NextPage( this, EventArgs.Empty );
            }
        }

	    private string GetNameColumnText( object item )
	    {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) item;
            if ( result.ExistingFeed != null )
            {
                return result.Name + " (already subscribed)";
            }
            if ( result.HintText != null && result.HintText.Length > 0 )
            {
                return result.Name + " (" + result.HintText + ")";
            }
            return result.Name;
	    }

        private Color GetNameColumnForeColor( object item )
        {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) item;
            if ( result.ExistingFeed != null )
            {
                return Color.FromArgb( 70, 70, 211 );
            }
            return SystemColors.WindowText;
        }

        private FontStyle GetNameColumnFont( object item )
        {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) item;
            if ( result.ExistingFeed != null )
            {
                return FontStyle.Underline;
            }
            return FontStyle.Regular;
        }

        private Cursor GetNameColumnCursor( object item )
        {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) item;
            if ( result.ExistingFeed != null )
            {
                return Cursors.Hand;
            }
            return null;
        }

        private void HandleNameColumnMouseDown( object sender, ItemMouseEventArgs e )
        {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) e.Item;
            if ( result.ExistingFeed != null )
            {
                e.Handled = true;
                FindForm().Close();
                Core.UIManager.DisplayResourceInContext( result.ExistingFeed );
            }
        }

        private string GetUrlColumnText( object item )
	    {
            RSSDiscover.RSSDiscoverResult result = (RSSDiscover.RSSDiscoverResult) item;
            return result.URL;
        }

        private void btnSelAll_Click(object sender, EventArgs e)
        {
            foreach( JetListViewNode node in _resultList.Nodes )
            {
                if ( _checkColumn.GetItemCheckState( node.Data ) != CheckBoxState.Grayed )
                {
                    _checkColumn.SetItemCheckState( node.Data, CheckBoxState.Checked );
                }
            }
            NextPredicate( true );
        }

        private void btnUnselAll_Click(object sender, EventArgs e)
        {
            foreach( JetListViewNode node in _resultList.Nodes )
            {
                if ( _checkColumn.GetItemCheckState( node.Data ) != CheckBoxState.Grayed )
                {
                    _checkColumn.SetItemCheckState( node.Data, CheckBoxState.Unchecked );
                }
            }
            NextPredicate( false );
        }

        private void _checkColumn_AfterCheck(object sender, CheckBoxEventArgs e)
        {
            NextPredicate( AnyItemChecked() );
        }

        private bool AnyItemChecked()
        {
            foreach( JetListViewNode node in _resultList.Nodes )
            {
                if ( _checkColumn.GetItemCheckState( node.Data ) == CheckBoxState.Checked )
                    return true;
            }
            return false;
        }
    }
}

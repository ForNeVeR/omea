// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.UI.Components.TreeSearchWindow
{
    /// <summary>
    /// Represents a small search window which is a simple interface for different run-time searching features
    /// </summary>
    public class SearchWindow : System.Windows.Forms.Control
    {
        /// <summary>Stores the text to search</summary>
        private string mySearchText = "";

        /// <summary>The tree view to attach search window to</summary>
        private TreeView myTreeView;

        /// <summary>Contains node filter</summary>
        private INodeFilter myNodeFilter;

        /// <summary>Stores the parent form's "Cancel" button reference when a search window
        /// is visible to intercept "Escape" key pressing. Restores button reference back
        /// when the window is hidded.</summary>
        private IButtonControl _ownerCancelBtn;

        /// <summary>
        /// Gets the text being searched
        /// </summary>
        public string SearchText
        {
            get { return mySearchText; }
            set
            {
                mySearchText = value;

                if( Handle != (IntPtr) 0 )
                    Size = DefaultSize;
            }
        }

        /// <summary>
        /// Gets or sets node filter
        /// </summary>
        public INodeFilter NodeFilter
        {
            get { return myNodeFilter; }
            set
            {
                if( value == null )
                    throw new ArgumentNullException( "value" );
                myNodeFilter = value;
            }
        }

        /// <summary>
        /// Gets or sets an attached tree view
        /// </summary>
        public TreeView TreeView
        {
            get { return myTreeView; }
            set
            {
                if( myTreeView != null )
                {
                    myTreeView.KeyDown -= OnKeyDown;
                    myTreeView.KeyPress -= OnKeyPress;
                    VisibleChanged -= SearchWindow_VisibleChanged;
                }

                myTreeView = value;

                if( myTreeView != null )
                {
                    myTreeView.KeyDown += OnKeyDown;
                    myTreeView.KeyPress += OnKeyPress;

                    VisibleChanged += SearchWindow_VisibleChanged;
                }
            }
        }

        /// <summary>
        /// Creates a new search window for a tree view
        /// </summary>
        public SearchWindow()
        {
            if( Handle == (IntPtr) 0 )
                HandleCreated += OnHandleCreated;
            else
                Size = DefaultSize;

            BackColor = SystemColors.Info;
            ForeColor = SystemColors.InfoText;

            myNodeFilter = new StartingFilter();
            Visible = false;
        }

        /// <summary>
        /// When a SearchWindow is made visible (activated) it prohibits a standard
        /// processing of "Escape" key via submitting a "Cancel" button. This makes
        /// possible to process "Escape" key locally to hide the search window. When
        /// a SearchWindow is not visible, "Cancel" key is processed as usual.
        /// </summary>
        private void SearchWindow_VisibleChanged( object sender, EventArgs e )
        {
            Form owner = myTreeView.FindForm();
            if( owner != null )
            {
                if( Visible )
                {
                    _ownerCancelBtn = owner.CancelButton;
                    owner.CancelButton = null;
                }
                else
                {
                    owner.CancelButton = _ownerCancelBtn;
                }
            }
        }

        /// <summary>
        /// Gets default size for this control
        /// </summary>
        protected override Size DefaultSize
        {
            get
            {
                try
                {
                    Size defaultSize = Graphics.FromHwnd( Handle ).MeasureString( "Search for: " + mySearchText, Font ).ToSize();
                    defaultSize.Width += 4;
                    defaultSize.Height += 4;

                    return defaultSize;
                }
                catch( Exception ex )
                {
                    System.Diagnostics.Trace.WriteLine( "SearchWindow.DefaultSize failed : " + ex, "UI" );
                }
                return new Size();
            }
        }

        protected override void OnPaint( PaintEventArgs pe )
        {
            try
            {
                Graphics g = pe.Graphics;
                Rectangle rect = pe.ClipRectangle;

                if( rect.Width == 0 )
                    return;

                g.FillRectangle( new SolidBrush( BackColor ), rect );

                Rectangle textRect = pe.ClipRectangle;
                textRect.Height -= 4;
                textRect.Y += 2;

                g.DrawString( "Search for: " + mySearchText, Font, new SolidBrush( ForeColor ), textRect );

                rect.Width--;
                rect.Height--;

                g.DrawRectangle( new Pen( new SolidBrush( ForeColor ), 1 ), rect );
            }
            catch( Exception ex )
            {
                System.Diagnostics.Trace.WriteLine( "SearchWindow.OnPaint failed : " + ex, "UI" );
            }

            // Calling the base class OnPaint
            base.OnPaint( pe );
        }

        /// <summary>
        /// Handles the HandleCreated event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHandleCreated( object sender, EventArgs e )
        {
            Size = DefaultSize;
        }

        #region Keys processing logic

        /// <summary>
        /// Handles the key down event of the attached tree view
        /// </summary>
        private void OnKeyDown( object sender, KeyEventArgs e )
        {
            try
            {
                if( Visible && Enabled && myTreeView != null )
                {
                    TreeNode node = null;

                    switch( e.KeyCode )
                    {
                        case Keys.Back:
                            if( SearchText.Length > 0 )
                            {
                                if( e.Control )
                                {
                                    CancelSearch();
                                }
                                else
                                {
                                    SearchText = SearchText.Substring( 0, SearchText.Length - 1 );
                                    Invalidate();
                                    Update();

                                    node = GetFirstMatchingNode( SearchText );
                                }
                                e.Handled = true;
                            }
                            break;
                        case Keys.Enter:
                            CancelSearch();
                            e.Handled = true;
                            break;

                        case Keys.Up:
                            node = GetPrevMatchingNode( myTreeView.SelectedNode, SearchText );
                            e.Handled = true;
                            break;

                        case Keys.Down:
                            node = GetNextMatchingNode( myTreeView.SelectedNode, SearchText );
                            e.Handled = true;
                            break;
                    }

                    if( node != null )
                        myTreeView.SelectedNode = node;
                }
            }
            catch( Exception ex )
            {
                System.Diagnostics.Trace.WriteLine( "SearchWindow.OnKeyDown failed : " + ex, "UI" );
            }
        }

        /// <summary>
        /// Handles the key press event of the attached tree view
        /// </summary>
        private void OnKeyPress( object sender, KeyPressEventArgs e )
        {
            try
            {
                if( !Enabled || myTreeView == null )
                    return;

                if( Char.IsLetterOrDigit( e.KeyChar ) || Char.IsPunctuation( e.KeyChar ) )
                {
                    SearchText += e.KeyChar;

                    if( !Visible )
                        Show();

                    Invalidate();
                    Update();

                    TreeNode matchingNode = GetFirstMatchingNode( SearchText );

                    if( matchingNode != null )
                        myTreeView.SelectedNode = matchingNode;

                    e.Handled = true;
                }
                else
                if( e.KeyChar == 27 )
                {
                    if( Visible )
                    {
                        CancelSearch();
                        e.Handled = true;
                    }
                }
            }
            catch( Exception ex )
            {
                System.Diagnostics.Trace.WriteLine( "SearchWindow.OnKeyPress failed : " + ex, "UI" );
            }
        }

        /// <summary>
        /// Cancels the search
        /// </summary>
        public void CancelSearch()
        {
            if( Visible )
                Hide();

            SearchText = "";
        }
        #endregion

        #region Node tranversal methods

        /// <summary>
        /// Gets the first node that matches the given text
        /// </summary>
        /// <param name="searchText">The search text to match</param>
        private TreeNode GetFirstMatchingNode( string searchText )
        {
            TreeNode node = myTreeView.TopNode;

            while( node != null )
            {
                if( myNodeFilter.Matches( node, searchText ) )
                    break;
                node = GetNextNode( node );
            }

            return node;
        }

        /// <summary>
        /// Gets the first matching node which follows the given node
        /// </summary>
        private TreeNode GetNextMatchingNode( TreeNode node, string searchText )
        {
            node = GetNextNode( node );

            while( node != null )
            {
                if( myNodeFilter.Matches( node, searchText ) )
                    break;
                node = GetNextNode( node );
            }

            return node;
        }

        /// <summary>
        /// Gets the first matching node which goes before the given node
        /// </summary>
        private TreeNode GetPrevMatchingNode( TreeNode node, string searchText )
        {
            node = GetPrevNode( node );

            while( node != null )
            {
                if( myNodeFilter.Matches( node, searchText ) )
                    break;
                node = GetPrevNode( node );
            }

            return node;
        }

        /// <summary>
        /// Gets node which follows the given one
        /// </summary>
        private TreeNode GetNextNode( TreeNode node )
        {
            if( node.Nodes.Count > 0 )
                return node.Nodes[ 0 ];

            while( node.NextNode == null && node.Parent != null )
                node = node.Parent;

            return node.NextNode;
        }

        /// <summary>
        /// Gets node which is followed by the given one
        /// </summary>
        private TreeNode GetPrevNode( TreeNode node )
        {
            if( node.PrevNode != null )
            {
                node = node.PrevNode;

                while( node.Nodes.Count > 0 )
                    node = node.Nodes[ node.Nodes.Count - 1 ];

                return node;
            }

            return node.Parent;
        }

        #endregion
    }
}

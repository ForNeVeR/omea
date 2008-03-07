/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
	public interface IInPlaceEditor
	{
        void BeginEdit( JetListView jetListView, JetListViewColumn col, JetListViewNode node );
        void CloseEdit( bool save );
	}

    internal class InPlaceTextBox: TextBox
    {
        protected override bool IsInputKey( Keys keyData )
        {
            if ( keyData == Keys.Escape || keyData == Keys.Enter )
            {
                return true;
            }
            return base.IsInputKey( keyData );
        }
    }
    
    /// <summary>
	/// Control for in-place editing JetListView2 item values in a text box.
	/// </summary>
	public class InPlaceTextEditor: IInPlaceEditor
	{
        private TextBox _inPlaceEditBox;
        private Rectangle _inPlaceEditRect;
        private bool _closingInPlaceEdit = false;
        private JetListView _host;
        private JetListViewNode _editNode;
        private JetListViewColumn _editColumn;
        private string _startEditText;

		public InPlaceTextEditor()
		{
            _inPlaceEditBox = new InPlaceTextBox();
            _inPlaceEditBox.BorderStyle = BorderStyle.FixedSingle;
            _inPlaceEditBox.Visible = false;
            _inPlaceEditBox.KeyDown += new KeyEventHandler( HandleInPlaceKeyDown );
            _inPlaceEditBox.KeyPress += new KeyPressEventHandler( HandleInPlaceKeyPress );
            _inPlaceEditBox.LostFocus += new EventHandler( HandleInPlaceLostFocus );
            _inPlaceEditBox.TextChanged += new EventHandler( HandleInPlaceTextChanged );
        }

        /// <summary>
        /// Occurs when the user starts editing the text of an item.
        /// </summary>
        public event JetItemEditEventHandler BeforeItemEdit;

        /// <summary>
        /// Occurs when the text of an item is edited by the user.
        /// </summary>
        public event JetItemEditEventHandler AfterItemEdit;

        public void BeginEdit( JetListView jetListView, JetListViewColumn col, JetListViewNode node )
        {
            _host = jetListView;
            _editColumn = col;
            _editNode = node;

            if ( !jetListView.Controls.Contains( _inPlaceEditBox ) )
            {
                jetListView.Controls.Add( _inPlaceEditBox );
            }

            Rectangle rc = jetListView.GetItemBounds( node, col );
            if ( col.SizeToContent )
            {
                rc.Width = jetListView.InternalClientRect().Width - rc.Left;
            }
            JetItemEditEventArgs args = new JetItemEditEventArgs( col.GetItemText( node.Data, rc.Width ),
                node.Data, col );
            OnBeforeItemEdit( args );
            if ( args.CancelEdit )
                return;

            jetListView.ScrollInView( node );
            jetListView.SetEditedNode( node );

            _inPlaceEditRect = new Rectangle( rc.Left-2, rc.Top-1, rc.Width+4, rc.Height+2 );
            _inPlaceEditBox.Bounds = _inPlaceEditRect;
            _inPlaceEditBox.Text = args.Text;
            _startEditText = args.Text;
            AutosizeInPlaceEdit();
            _inPlaceEditBox.Visible = true;
            _inPlaceEditBox.Focus();
        }

        private void HandleInPlaceKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Escape )
            {
                CloseEdit( false );
            }
            else if ( e.KeyData == Keys.Enter )
            {
                CloseEdit( true );
            }
        }

        private void HandleInPlaceKeyPress( object sender, KeyPressEventArgs e )
        {
            if ( !_inPlaceEditBox.Visible || e.KeyChar == '\r' || e.KeyChar == '\n' )
            {
                e.Handled = true;
            }
        }

        private void HandleInPlaceLostFocus( object sender, EventArgs e )
        {
            CloseEdit( true );
        }

        private void HandleInPlaceTextChanged( object sender, EventArgs e )
        {
            AutosizeInPlaceEdit();
        }

        private void AutosizeInPlaceEdit()
        {
            using( Graphics g = _host.CreateGraphics() )
            {
                Size desiredSize = _host.ControlPainter.MeasureText( g, _inPlaceEditBox.Text, 
                    _inPlaceEditBox.Font );
                int desiredWidth = desiredSize.Width + 20;
                if ( desiredWidth > _inPlaceEditRect.Width )
                {
                    desiredWidth = _inPlaceEditRect.Width;
                }
                else if ( desiredWidth < 100 )
                {
                    desiredWidth = 100;
                }
                _inPlaceEditBox.Width = desiredWidth;
            }
        }

        protected void OnBeforeItemEdit( JetItemEditEventArgs args )
        {
            if ( BeforeItemEdit != null )
            {
                BeforeItemEdit( this, args );
            }
        }

        protected void OnAfterItemEdit( JetItemEditEventArgs args )
        {
            if ( AfterItemEdit != null )
            {
                AfterItemEdit( this, args );
            }
        }

        public void CloseEdit( bool save )
        {
            if ( _inPlaceEditBox.Visible && !_closingInPlaceEdit )
            {
                _closingInPlaceEdit = true;
                if ( _inPlaceEditBox.Text != _startEditText )
                {
                    JetItemEditEventArgs args = new JetItemEditEventArgs( save ? _inPlaceEditBox.Text : null,
                        _editNode.Data, _editColumn );
                    OnAfterItemEdit( args );
                }

                _host.SetEditedNode( null );
                _inPlaceEditBox.Visible = false;
                _host.Focus();
                _closingInPlaceEdit = false;
            }
        }
	}

    public class JetItemEditEventArgs
    {
        private string _text;
        private object _item;
        private JetListViewColumn _column;
        private bool _cancelEdit;

        public JetItemEditEventArgs( string text, object item, JetListViewColumn column )
        {
            _text = text;
            _item = item;
            _column = column;
            _cancelEdit = false;
        }

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public object Item
        {
            get { return _item; }
        }

        public JetListViewColumn Column
        {
            get { return _column; }
        }

        public bool CancelEdit
        {
            get { return _cancelEdit; }
            set { _cancelEdit = value; }
        }
    }

    public delegate void JetItemEditEventHandler( object sender, JetItemEditEventArgs e );
}

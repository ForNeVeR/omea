// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;

namespace JetBrains.JetListViewLibrary
{
    public enum CheckBoxState
    {
        Hidden, Unchecked, Checked, Grayed
    }

    public class CheckBoxEventArgs
    {
        private readonly object _item;
        private readonly CheckBoxState _oldState;
        private CheckBoxState _newState;

        public CheckBoxEventArgs( object item, CheckBoxState oldState, CheckBoxState newState )
        {
            _item = item;
            _oldState = oldState;
            _newState = newState;
        }

        public object Item
        {
            get { return _item; }
        }

        public CheckBoxState OldState
        {
            get { return _oldState; }
        }

        public CheckBoxState NewState
        {
            get { return _newState; }
            set { _newState = value; }
        }
    }

    public delegate void CheckBoxEventHandler( object sender, CheckBoxEventArgs e );

    /// <summary>
    /// A column in JetListView which supports drawing checkboxes.
    /// </summary>
    public class CheckBoxColumn: JetListViewColumn
	{
        private readonly HashMap _checkStates = new HashMap();
        private JetListViewNode _mouseDownNode;

        public event CheckBoxEventHandler BeforeCheck;
        public event CheckBoxEventHandler AfterCheck;

        public CheckBoxColumn()
        {
            Width = 18;
            FixedSize = true;
            _showHeader = false;
        }

        protected internal override void DrawItem( Graphics g, Rectangle rc, object item,
            RowState state, string highlightText )
        {
            int midPoint = (rc.Left + rc.Right) / 2;
            Rectangle rcCheck = new Rectangle( midPoint - 7, rc.Top, 15, 15 );

            CheckBoxState checkState = GetItemCheckState( item );
            if ( checkState != CheckBoxState.Hidden )
            {
                ButtonState buttonState;
                switch( checkState )
                {
                    case CheckBoxState.Checked: buttonState = ButtonState.Checked; break;
                    case CheckBoxState.Grayed: buttonState = ButtonState.Inactive; break;
                    default: buttonState = ButtonState.Normal; break;
                }
                if ( ( state & RowState.Disabled ) != 0 )
                {
                    buttonState |= ButtonState.Inactive;
                }
                OwnerControl.ControlPainter.DrawCheckBox( g, rcCheck, buttonState );
            }
        }

        protected internal override MouseHandleResult HandleMouseDown( JetListViewNode node, int x, int y )
        {
            _mouseDownNode = node;
            if ( x >= 0 && x < Width )
            {
                return MouseHandleResult.Handled;
            }
            return 0;
        }

        protected internal override bool HandleMouseUp( JetListViewNode node, int x, int y )
        {
            base.HandleMouseUp( node, x, y );
            bool toggleCheck = (_mouseDownNode == node);
            _mouseDownNode = null;
            if ( toggleCheck )
            {
                ToggleCheckState( node.Data );
            }
            return x >= 0 && x < Width;
        }

        protected internal override bool HandleKeyDown( JetListViewNode node, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Space )
            {
                ToggleSelectionCheckState( node.Data );
                return true;
            }
            return false;
        }

        private void ToggleSelectionCheckState( object item )
        {
            if ( OwnerControl.Selection.Contains( item ) )
            {
                CheckBoxState oldCheckState = GetItemCheckState( item );
                if ( oldCheckState != CheckBoxState.Hidden && oldCheckState != CheckBoxState.Grayed )
                {
                    CheckBoxState newCheckState = ( oldCheckState == CheckBoxState.Checked )
                        ? CheckBoxState.Unchecked
                        : CheckBoxState.Checked;
                    foreach( object selItem in OwnerControl.Selection )
                    {
                        SetItemCheckState( selItem, newCheckState );
                    }
                }
            }
            else
            {
                ToggleCheckState( item );
            }
        }

        private void ToggleCheckState( object item )
        {
            CheckBoxState oldCheckState = GetItemCheckState( item );
            if ( oldCheckState != CheckBoxState.Hidden && oldCheckState != CheckBoxState.Grayed )
            {
                CheckBoxState newCheckState = ( oldCheckState == CheckBoxState.Checked )
                    ? CheckBoxState.Unchecked
                    : CheckBoxState.Checked;
                SetItemCheckState( item, newCheckState );
            }
        }

        public void SetItemCheckState( object item, CheckBoxState newCheckState )
        {
            CheckBoxEventArgs args = new CheckBoxEventArgs( item, GetItemCheckState( item ), newCheckState );
            if ( BeforeCheck != null )
            {
                BeforeCheck( this, args );
            }

            _checkStates [item] = args.NewState;
            if ( args.OldState != args.NewState )
            {
                OnAfterCheck( args );
                OwnerControl.InvalidateItem( item );
            }
        }

        protected virtual void OnAfterCheck( CheckBoxEventArgs args )
        {
            if ( AfterCheck != null )
            {
                AfterCheck( this, args );
            }
        }

        public CheckBoxState GetItemCheckState( object item )
        {
            if ( !_checkStates.Contains( item ) )
            {
                return GetDefaultCheckState( item );
            }
            return (CheckBoxState) _checkStates [item];
        }

        protected virtual CheckBoxState GetDefaultCheckState( object item )
        {
            return CheckBoxState.Unchecked;
        }

        public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
        {
            return null;
        }

        protected internal override int GetWidthDelta( JetListViewNode node )
        {
            if ( GetItemCheckState( node.Data ) == CheckBoxState.Hidden )
            {
                return -Width;
            }
            return base.GetWidthDelta( node );
        }
	}
}

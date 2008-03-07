/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// A pool which allows to reuse the controls when updating forms, instead of
    /// deleting and recreating them.
    /// </summary>
    public class ControlPool
	{
        private Control _ownerControl;
        private ArrayList _controlList = new ArrayList();
        private ArrayList _controlPool = new ArrayList();
        private ControlPoolCreateDelegate _createDelegate;
        private ControlPoolDisposeDelegate _disposeDelegate;

		public ControlPool( Control ownerControl, ControlPoolCreateDelegate createDelegate )
		{
            _ownerControl = ownerControl;
            _createDelegate = createDelegate;
        }

        public ControlPoolCreateDelegate CreateDelegate
        {
            get { return _createDelegate; }
            set { _createDelegate = value; }
        }

        /// <summary>
        /// The delegate which is called before a control in the pool is disposed.
        /// </summary>
        public ControlPoolDisposeDelegate DisposeDelegate
        {
            get { return _disposeDelegate; }
            set { _disposeDelegate = value; }
        }

        /**
         * Moves the controls from the control list to the control pool. Called
         * before updating the form.
         */

	    public void MoveControlsToPool()
        {
            foreach( Control ctl in _controlList )
            {
                _controlPool.Add( ctl );
            }
            _controlList.Clear();
        }

        /**
         * Moves a single control from the control list to the control pool.
         * (Can be used to cancel GetControl()).
         */

        public void MoveControlToPool( Control ctl )
        {
            _controlPool.Insert( 0, ctl );
            _controlList.Remove( ctl );
        }

        /**
         * Removes controls that were pooled and not re-added from the form.
         */

        public void RemovePooledControls()
        {
            foreach( Control ctl in _controlPool )
            {
                _ownerControl.Controls.Remove( ctl );
                if ( _disposeDelegate != null )
                {
                    _disposeDelegate( ctl );
                }
                ctl.Dispose();
            }
            _controlPool.Clear();
        }

        /**
         * Returns a control from the pool, or creates a new one through the
         * delegate.
         */

        public Control GetControl()
        {
            if ( _controlPool.Count > 0 )
            {
                Control oldCtl = (Control) _controlPool [0];
                _controlPool.RemoveAt( 0 );
                _controlList.Add( oldCtl );
                return oldCtl;
            }
            
            Control ctl = _createDelegate();
            _ownerControl.Controls.Add( ctl );
            _controlList.Add( ctl );
            return ctl;
        }

        /**
         * Returns the list of currently visible controls.
         */

        public ArrayList VisibleControls
        {
            get { return (ArrayList) _controlList.Clone(); }
        }

        /**
         * Checks if the specified control is present in the pool.
         */

        public bool IsPooledControl( Control ctl )
        {
            return _controlPool.IndexOf( ctl ) >= 0;
        }
    }

    public delegate Control ControlPoolCreateDelegate();
    public delegate void ControlPoolDisposeDelegate( Control ctl );
}

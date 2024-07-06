// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;

namespace JetBrains.JetListViewLibrary
{
	public class JetListViewColumnCollection: IDisposable, IEnumerable
	{
        private ArrayList _items;
        private JetListView _ownerControl;
        private IControlPainter _controlPainter;
        private Font _font;
        private int _batchUpdateCount;

		internal JetListViewColumnCollection()
		{
            _items = new ArrayList();
		}

	    public IEnumerator GetEnumerator()
	    {
	        return _items.GetEnumerator();
	    }

	    public void Dispose()
	    {
            Clear();
	    }

	    internal JetListView OwnerControl
	    {
	        get { return _ownerControl; }
	        set { _ownerControl = value; }
	    }

	    public IControlPainter ControlPainter
	    {
	        get { return _controlPainter; }
	        set { _controlPainter = value; }
	    }

	    public Font Font
	    {
	        get { return _font; }
	        set { _font = value; }
	    }

        /// <summary>
        /// Occurs when a column is added to the collection.
        /// </summary>
        public event ColumnEventHandler ColumnAdded;

        /// <summary>
        /// Occurs when a column is removed from the collection.
        /// </summary>
        public event ColumnEventHandler ColumnRemoved;

        public event EventHandler BatchUpdateStarted;
        public event EventHandler BatchUpdated;

        public JetListViewColumn this[int index]
        {
            get
            {
                return (JetListViewColumn) _items [index];
            }
            set
            {
                _items [index] = value;
            }
        }

	    public int Count
	    {
	        get { return _items.Count; }
	    }

        public void Insert( int index, JetListViewColumn value )
        {
            value.Owner = this;
            _items.Insert( index, value );
            OnColumnAdded( value );
        }

        public void Remove( JetListViewColumn value )
        {
            _items.Remove( value );
            OnColumnRemoved( value );
            value.Dispose();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains( JetListViewColumn value )
        {
            return _items.Contains( value );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int IndexOf( JetListViewColumn value )
        {
            return _items.IndexOf( value );
        }

        /// <summary>
        /// Adds the specified column to the list.
        /// </summary>
        /// <param name="value">The column to add.</param>
        /// <returns>The index of the added column.</returns>
        public int Add( JetListViewColumn value )
        {
            int result = _items.Add( value );
            value.Owner = this;
            OnColumnAdded( value );
            return result;
        }

        public void AddRange( JetListViewColumn[] values )
        {
            foreach( JetListViewColumn col in values )
            {
                _items.Add( col );
                col.Owner = this;
                OnColumnAdded( col );
            }
        }

	    private void OnColumnAdded( JetListViewColumn value )
	    {
	        if ( ColumnAdded != null )
            {
                ColumnAdded( this, new ColumnEventArgs( value ) );
            }
	    }

	    /// <summary>
        /// Removes all columns from the collection.
        /// </summary>
        public void Clear()
        {
            for( int i=0; i<Count; i++ )
            {
                OnColumnRemoved( this [i] );
                this [i].Dispose();
            }
            _items.Clear();
        }

	    public void Move( JetListViewColumn col, int toIndex )
	    {
            BeginUpdate();
            _items.Remove( col );
            _items.Insert( toIndex, col );
            EndUpdate();
        }

	    private void OnColumnRemoved( JetListViewColumn column )
	    {
	        if ( ColumnRemoved != null )
	        {
	            ColumnRemoved( this, new ColumnEventArgs( column ) );
	        }
	    }

        public void BeginUpdate()
        {
            _batchUpdateCount++;
            if ( _batchUpdateCount == 1 )
            {
                if ( BatchUpdateStarted != null )
                {
                    BatchUpdateStarted( this, EventArgs.Empty );
                }
            }
        }

        public void EndUpdate()
        {
            _batchUpdateCount--;
            if ( _batchUpdateCount == 0 )
            {
                if ( BatchUpdated != null )
                {
                    BatchUpdated( this, EventArgs.Empty );
                }
            }
        }

        public bool BatchUpdating
        {
            get { return _batchUpdateCount > 0; }
        }
	}
}

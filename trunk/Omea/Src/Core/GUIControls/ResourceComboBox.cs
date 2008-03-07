/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Interop.WinApi;
using JetBrains.UI.Interop;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// A ComboBox with support for drawing resource icons and indented items.
    /// </summary>
    public class ResourceComboBox: ComboBox
	{
        private Hashtable _resourceIndents = new Hashtable();

		/// <summary>
		/// If the OnKeyDown handler processes a key, it raises this flag so that OnKeyPress handler could also mark this keystroke as processed.
		/// </summary>
		protected bool _isKeySuppressed = false;

		/// <summary>
		/// Constructs the instance.
		/// </summary>
		public ResourceComboBox()
		{
			// Initializes the base properties of this combobox
            DrawMode = DrawMode.OwnerDrawFixed;
			KeyDown += new KeyEventHandler(OnKeyDown);
			KeyPress += new KeyPressEventHandler(OnKeyPress);
		}

        /// <summary>
        /// Sets the indent in pixels for the specified item in the combo box.
        /// </summary>
        /// <param name="item">The item to be indented.</param>
        /// <param name="indent">The indent value.</param>
        public void SetItemIndent( object item, int indent )
        {
        	_resourceIndents [item] = indent;
        }

        /// <summary>
        /// Adds a hierarchy of resources with the specified root and parent link to the combo box.
        /// </summary>
        /// <param name="rootResource">The root of the resource hierarchy.</param>
        /// <param name="resType">The type of resources to add, or null if resources of all types
        /// are added.</param>
        /// <param name="propParent">The ID of a link property linking a resource in a hierarchy
        /// to its parent.</param>
        public void AddResourceHierarchy( IResource rootResource, string resType, int propParent )
        {
        	AddResourceRecursive( rootResource, resType, propParent, 0, null );
        }

        /// <summary>
        /// Adds a hierarchy of resources with the specified root and parent link to the combo box.
        /// </summary>
        /// <param name="rootResource">The root of the resource hierarchy.</param>
        /// <param name="resType">The type of resources to add, or null if resources of all types
        /// are added.</param>
        /// <param name="propParent">The ID of a link property linking a resource in a hierarchy
        /// to its parent.</param>
        /// <param name="indent">Initial indent of root resource.</param>
        public void AddResourceHierarchy( IResource rootResource, string resType, int propParent, int indent )
        {
            AddResourceRecursive( rootResource, resType, propParent, indent, null );
        }

        /// <summary>
        /// Adds a hierarchy of resources with the specified root and parent link to the combo box.
        /// </summary>
        /// <param name="rootResource">The root of the resource hierarchy.</param>
        /// <param name="resType">The type of resources to add, or null if resources of all types
        /// are added.</param>
        /// <param name="propParent">The ID of a link property linking a resource in a hierarchy
        /// to its parent.</param>
        /// <param name="acceptDelegate">The delegate which can be used to filter out parts of the
        /// hierarchy.</param>
        public void AddResourceHierarchy( IResource rootResource, string resType, int propParent,
            AcceptResourceDelegate acceptDelegate )
        {
            AddResourceRecursive( rootResource, resType, propParent, 0, acceptDelegate );
        }

		/// <summary>
		/// Adds a hierarchy of resources with the specified root and parent link to the combo box.
		/// </summary>
		/// <param name="rootResource">The root of the resource hierarchy.</param>
		/// <param name="resType">The type of resources to add, or null if resources of all types
		/// are added.</param>
		/// <param name="propParent">The ID of a link property linking a resource in a hierarchy
		/// to its parent.</param>
		/// <param name="intersect">A resource list that is intersected with the tree items when they are added.
		/// Only items that belong to this resource list get added into the tree.</param>
		public void AddResourceHierarchy( IResource rootResource, string resType, int propParent, IResourceList intersect)
		{
			AddResourceRecursive(rootResource, resType, propParent, 0, new AcceptResourceDelegate(new IntersectAcceptor(intersect).Intersect));
			
		}

    	private void AddResourceRecursive( IResource res, string resType, int propParent, int indent,
            AcceptResourceDelegate acceptDelegate )
        {
            Items.Add( res );
            SetItemIndent( res, indent );
            foreach( IResource child in res.GetLinksTo( resType, propParent ) )
            {
                if ( acceptDelegate == null || acceptDelegate( child ) )
                {
                    AddResourceRecursive( child, resType, propParent, indent + 16, acceptDelegate );
                }
            }
        }

    	/// <summary>
    	/// Adds a hierarchical list of resources to the combobox, simulating the tree structure with indents.
    	/// Also supports folders in the tree, which are the resources of some other type but the target type.
    	/// The main resources are filtered against an intersection list.
    	/// </summary>
    	/// <param name="resRoot">The root resource from which the tree enumeration starts. May be excluded from the tree (<paramref name="bAddRoot"/>).</param>
    	/// <param name="resItemType">Resource type of the main resources. Note that the root resource may have some other type.</param>
    	/// <param name="resFolderType">Type of the folder resources. They are not filtered against the intersection list. May be <c>Null</c> if no folders are expected.</param>
    	/// <param name="propParent">The child-parent link type.</param>
    	/// <param name="nStartIndent">Indent of the tree root (if included), or the first-level root's children (if the root is not included).
    	/// Each next level is indented by 16 pixels against the parent one.
    	/// The default value is <c>0</c>.</param>
    	/// <param name="resIntersect">A resource list that filters out the main resources (does not affect folders and root).
    	/// Only those resources that are present in the list are allowed into the tree.
    	/// Children of a dropped resource are also dropped.
    	/// The list is not instantiated, only its predicate is used.</param>
    	/// <param name="bAddRoot">Whether to add the root resource passed as <paramref name="resRoot"/>.
    	/// If the root is present (<c>True</c>), it has the <paramref name="nStartIndent"/> indent and its first-level children are indented by 16 pixels.
    	/// If the root is not present (<c>False</c>), the first-level children are added with the root indent (<paramref name="nStartIndent"/>).</param>
    	/// <param name="bSuppressEmptyFolders">If <c>True</c>, the empty folders (resources of folder type) are suppressed and not added
    	/// into the tree. Has no effect on main resources and tree root, or when <paramref name="resFolderType"/> is <c>Null</c>.</param>
    	public void AddFolderedResourceTree(IResource resRoot, string resItemType, string resFolderType, int propParent, int nStartIndent, IResourceList resIntersect, bool bAddRoot, bool bSuppressEmptyFolders)
    	{
    		if((resRoot == null) || (resItemType == null))
    			throw new ArgumentNullException();

    		ArrayList arItems = new ArrayList(); // Caches items before they get added to the list; some items may get removed from here
    		ArrayList arEnums = new ArrayList(); // Enumerations stack
    		HashSet hashEverAdded = new HashSet(); // A hash-set of all the items to avoid cyclic links

    		// Seed the enumeration
    		arEnums.Add(resRoot.ToResourceList().GetEnumerator());

    		// Collect the items
    		for(; arEnums.Count != 0; )
    		{
    			IEnumerator enumCurrent = (IEnumerator)arEnums[arEnums.Count - 1];
    			// Try to pick the next item, move to the upper level if unavailable
    			if(!enumCurrent.MoveNext())
    			{
    				// Pop to the upper level by removing the current enumerator from the stack
    				arEnums.RemoveAt(arEnums.Count - 1);

    				// Empty folder check: if the last-added item is the parent-level folder, drop it
    				if((bSuppressEmptyFolders) && (arItems.Count > 0))
    				{
    					TempListItem tli = ((TempListItem)arItems[arItems.Count - 1]);
    					if((tli.Resource.Type == resFolderType) && (tli.Indent == arEnums.Count - 1))
    						arItems.RemoveAt(arItems.Count - 1);
    				}
    				continue; // Go on dealing with the upper-level enumerator
    			}

    			// Add the current item
    			IResource resCurrent = (IResource)enumCurrent.Current;
    			if(hashEverAdded.Contains(resCurrent))
    				continue; // Skip the already-visited items (avoid cyclic links)
    			if(!(
    				((resCurrent == resRoot)
    					|| (resCurrent.Type == resFolderType)
    					|| ((resCurrent.Type == resItemType) && ((resIntersect == null) || (resIntersect.Contains(resCurrent)))))
    				))
    				continue; // Allow only root, folder type and item type; for item type, check the intersect list (if specified)

    			// Add the item (root will be suppressed later)
    			arItems.Add(new TempListItem(resCurrent, arEnums.Count - 1));
    			hashEverAdded.Add(resCurrent);

    			// Recurse to the item's children
    			IResourceList listChildren = resCurrent.GetLinksTo(resItemType, propParent);
    			if(resFolderType != null)
    				listChildren = listChildren.Union(resCurrent.GetLinksTo(resFolderType, propParent));
    			listChildren.Sort("RootSortOrder", true);

    			arEnums.Add(listChildren.GetEnumerator()); // Add a new enumerator to the stack
    		}

    		// Add the items
    		foreach(TempListItem tli in arItems)
    		{
    			if((!bAddRoot) && (tli.Resource == resRoot))
    				continue; // Do not add the root if prohibited
    			Items.Add(tli.Resource);
    			SetItemIndent(tli.Resource, nStartIndent + (tli.Indent - (bAddRoot ? 0 : 1)) * 16); // Decrease all the indents if there's no root
    		}
    	}

    	/// <summary>
    	/// A future item of the combo-list.
    	/// It's stored in a temporary array along with the indent value.
    	/// </summary>
    	protected struct TempListItem
    	{
    		public TempListItem(IResource resource, int indent)
    		{
    			Resource = resource;
    			Indent = indent;
    		}

    		/// <summary>
    		/// The item's resource.
    		/// </summary>
    		public IResource Resource;

    		/// <summary>
    		/// The item's indent.
    		/// Note that it's a logical value which adds 1 for each new level and does not account for the global start-indent.
    		/// </summary>
    		public int Indent;
    	}

    	protected override void OnDrawItem( DrawItemEventArgs e )
        {
            base.OnDrawItem( e );
            e.DrawBackground();

            object obj = (e.Index >= 0) ? Items [e.Index] : null;
            IResource res = obj as IResource;
            int textOffset = 0;

            // don't apply indent when drawing the item in the combo box itself -
            // only for drop-down items
            if ( e.Bounds.X != 3 && e.Bounds.Y != 3 )
            {
                if ( obj != null && _resourceIndents.Contains( obj ) )
                {
                    textOffset = (int) _resourceIndents [obj];
                }
            }
            string text = null;
            if ( res != null )
            {
                int imageIndex = Core.ResourceIconManager.GetIconIndex( res );
                if ( imageIndex >= 0 && imageIndex < Core.ResourceIconManager.ImageList.Images.Count )
                {
                	Core.ResourceIconManager.ImageList.Draw( e.Graphics, e.Bounds.X + textOffset, e.Bounds.Y, imageIndex );
                    textOffset += 18;
                }
                text = res.DisplayName;
            }
            else if ( obj != null )
            {
            	text = obj.ToString();
                textOffset += 2;
            }
            else
            {
                text = "";
            }

            IntPtr hdc = e.Graphics.GetHdc();
            try
            {
                RECT rc = new RECT( e.Bounds.Left + textOffset, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom );
                int oldColor = Win32Declarations.SetTextColor( hdc, Win32Declarations.ColorToRGB( e.ForeColor ) );
                BackgroundMode oldMode = Win32Declarations.SetBkMode( hdc, BackgroundMode.TRANSPARENT );
                Win32Declarations.DrawText( hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX );
                Win32Declarations.SetBkMode( hdc, oldMode );
                Win32Declarations.SetTextColor( hdc, oldColor );
            }
            finally
            {
            	e.Graphics.ReleaseHdc( hdc );
            }
        }

		/// <summary>
		/// Overrides the raw Windows procedure.
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			switch( m.Msg )
			{
			case Win32Declarations.WM_NOTIFY:
				switch( Win32Declarations.HIWORD( (UInt32) m.WParam ) )
				{
				case (UInt16) ComboBoxNotification.CBN_CLOSEUP:
					OnCloseUp();
					break;
				}
				break;
			}

			base.WndProc( ref m );
		}

    	/// <summary>
		/// Raises the CloseUp event.
		/// Is invoked when the list box of a combo box has been closed.
		/// </summary>
    	protected void OnCloseUp()
    	{
			if(CloseUp != null)
				CloseUp(this, EventArgs.Empty);
    	}

		/// <summary>
		/// Raises when the list box of a combo box has been closed.
		/// </summary>
		public event EventHandler CloseUp;

		/// <summary>
		/// A key has been pressed.
		/// </summary>
		protected void OnKeyDown(object sender, KeyEventArgs e)
		{
			switch(e.KeyData)
			{
				case Keys.Enter:
					if(EnterPressed != null)
						EnterPressed(this, e);	// Propagate Handled from the external event sink
					_isKeySuppressed = e.Handled;
					break;
				case Keys.Escape:
					if(EscapePressed != null)
						EscapePressed(this, e);	// Propagate Handled from the external event sink
					_isKeySuppressed = e.Handled;
					break;
			}
		}

		/// <summary>
		/// A key has been pressed.
		/// </summary>
		protected void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			// Suppress this key-press if this key has been caught by OnKeyDown
			if(_isKeySuppressed)
			{
				e.Handled = true;
				_isKeySuppressed = false;
			}
		}

		/// <summary>
		/// The Enter key has been pressed in the combobox.
		/// </summary>
		public event KeyEventHandler EnterPressed;

		/// <summary>
		/// The Escape key has been pressed in the combobox.
		/// </summary>
		public event KeyEventHandler EscapePressed;

		#region IntersectAcceptor Class

		/// <summary>
		/// A class that allows to filter the combobox items down to the content of some resource list, by providing a delegate compatible with the <see cref="AddResourceRecursive"/> function.
		/// </summary>
		protected class IntersectAcceptor
		{
			/// <summary>
			/// Resource list to be intersected.
			/// </summary>
			protected readonly IResourceList _list;

			/// <summary>
			/// Initializes the instance.
			/// </summary>
			/// <param name="list">The intersected list.</param>
			public IntersectAcceptor(IResourceList list)
			{
				_list = list;
			}

			/// <summary>
			/// A method that words as an <see cref="AcceptResourceDelegate"/>.
			/// </summary>
			public bool Intersect(IResource res)
			{
				return _list.Contains(res);
			}
		}

		#endregion
	}

    public delegate bool AcceptResourceDelegate( IResource res );
}

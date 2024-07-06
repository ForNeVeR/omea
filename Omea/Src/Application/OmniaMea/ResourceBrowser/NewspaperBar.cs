// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.ImageListButton;
using JetBrains.UI.Interop;

namespace JetBrains.Omea
{
	/// <summary>
	/// A composite control for a bar that is displayed above the newspaper and provides for narrowing the view and paging.
	/// </summary>
	public class NewspaperBar : UserControl, IBackgroundBrushProvider
	{
		#region Data

		/// <summary>
		/// "Show" text label.
		/// </summary>
		protected JetLinkLabel _labelShow;

		/// <summary>
		/// Combobox with the list of the filtering views available.
		/// </summary>
		protected ResourceComboBox _comboView;

		/// <summary>
		/// "with items" text label.
		/// </summary>
		protected JetLinkLabel _labelWithItems;

		/// <summary>
		/// An editable combobox for specifying number of items per page.
		/// </summary>
		protected ResourceComboBox _comboItemsPerPage;

		/// <summary>
		/// "Items per page" text label.7
		/// </summary>
		protected JetLinkLabel _labelItemsPerPage;

		/// <summary>
		/// Button that goes to the previous page.
		/// </summary>
		protected ImageListButton _btnPrevPage;

		/// <summary>
		/// Button that goes to the next page.
		/// </summary>
		protected ImageListButton _btnNextPage;

		/// <summary>
		/// An object that manages the newspaper state.
		/// </summary>
		protected NewspaperManager _man = null;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		protected Container components = null;

		/// <summary>
		/// A list of the available page button controls.
		/// </summary>
		protected ArrayList _btnPaging = new ArrayList();

		/// <summary>
		/// Number of page buttons that are currently visible, and the index of the first unused button in the <see cref="_btnPaging"/> array.
		/// </summary>
		protected int _nUsedPagingButtons = 0;

		/// <summary>
		/// Base tab-index for the paging buttons.
		/// The first paging button has this index, the following ones — base plus the internal (zero-based) number of its page.
		/// The prev-page-button has a tab index of base minus 1, and the next-page-button — base plus total number of pages.
		/// </summary>
		protected static readonly int c_nPagingButtonsTabIndexBase = 1000;

		/// <summary>
		/// A list of separators that indicate the places where there's a jump in numbering of the paging buttons.
		/// Populated by <see cref="OnLayout"/> -> <see cref="OnLayout_Paging"/>, used by <see cref="OnPaint"/> to implement it on the screen.
		/// </summary>
		protected Rectangle[] _separators = new Rectangle[0];

		/// <summary>
		/// Scale of the form. Affects sizing of the child controls.
		/// </summary>
		protected SizeF _sizeScale = new SizeF(1, 1);

		#endregion

		#region Types

		/// <summary>
		/// A delegate for a bool-param function.
		/// </summary>
		public delegate void BoolDelegate(bool param);

		#endregion

		#region Construction

		public NewspaperBar(NewspaperManager man)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();

			_man = man;

			// Wire up the events
			_man.FilteringViewAdded += new ResourceIndexEventHandler(OnViewAdded);
			_man.FilteringViewChanged += new ResourcePropIndexEventHandler(OnViewChanged);
			_man.FilteringViewDeleted += new ResourceIndexEventHandler(OnViewDeleted);
			_man.Initializing += new EventHandler(OnManInitializing);
			_man.Deinitializing += new EventHandler(OnManDeinitializing);
			_man.ItemsPerPageChanged += new EventHandler(OnManItemsPerPageChanged);
			_man.CurrentFilteringViewChanged += new EventHandler(OnManCurrentViewChanged);
			_man.PagingChanged += new EventHandler(OnManPagingChanged);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}

				// Unwire from the events
				_man.FilteringViewAdded -= new ResourceIndexEventHandler(OnViewAdded);
				_man.FilteringViewChanged -= new ResourcePropIndexEventHandler(OnViewChanged);
				_man.FilteringViewDeleted -= new ResourceIndexEventHandler(OnViewDeleted);
				_man.Initializing -= new EventHandler(OnManInitializing);
				_man.Deinitializing -= new EventHandler(OnManDeinitializing);
				_man.ItemsPerPageChanged -= new EventHandler(OnManItemsPerPageChanged);
				_man.CurrentFilteringViewChanged -= new EventHandler(OnManCurrentViewChanged);
				_man.PagingChanged -= new EventHandler(OnManPagingChanged);
				_man = null;
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Visual Init

		/// <summary>
		/// Visual Init.
		/// </summary>
		protected void InitializeComponentSelf()
		{
			SuspendLayout();
			//
			// _labelShow
			//
			_labelShow = new JetLinkLabel();
			_labelShow.BackColor = Color.Transparent;
			_labelShow.Name = "_labelShow";
			_labelShow.TabIndex = 0;
			_labelShow.Text = "Show";
			_labelShow.ClickableLink = false;
			_labelShow.Visible = false;
			_labelShow.AutoSize = true;
			//
			// _comboView
			//
			_comboView = new ResourceComboBox();
			_comboView.DropDownStyle = ComboBoxStyle.DropDownList;
			_comboView.Name = "_comboView";
			_comboView.TabIndex = 1;
			_comboView.SelectedIndexChanged += new EventHandler(OnViewsSelectionChange);
			_comboView.EnterPressed += new KeyEventHandler(OnViewsEnterPressed);
			_comboView.EscapePressed += new KeyEventHandler(OnViewsEscapePressed);
			_comboView.CloseUp += new EventHandler(OnViewsCloseUp);
			_comboView.Visible = false;
			_comboView.Width = 150;
			//
			// _labelWithItems
			//
			_labelWithItems = new JetLinkLabel();
			_labelWithItems.BackColor = Color.Transparent;
			_labelWithItems.Name = "_labelWithItems";
			_labelWithItems.TabIndex = 0;
			_labelWithItems.Text = "with";
			_labelWithItems.ClickableLink = false;
			_labelWithItems.Visible = false;
			_labelWithItems.AutoSize = true;
			//
			// _comboItemsPerPage
			//
			_comboItemsPerPage = new ResourceComboBox();
			_comboItemsPerPage.Name = "_comboItemsPerPage";
			_comboItemsPerPage.TabIndex = 2;
			_comboItemsPerPage.TextChanged += new EventHandler(OnItemsPerPageChange);
			_comboItemsPerPage.SelectedValueChanged += new EventHandler(OnItemsPerPageChange);
			_comboItemsPerPage.EnterPressed += new KeyEventHandler(OnItemsPerPageEnterPressed);
			_comboItemsPerPage.EscapePressed += new KeyEventHandler(OnItemsPerPageEscapePressed);
			_comboItemsPerPage.CloseUp += new EventHandler(OnItemsPerPageCloseUp);
			_comboItemsPerPage.Leave += new EventHandler(OnItemsPerPageLeave);
			_comboItemsPerPage.Visible = false;
			_comboItemsPerPage.Width = 75;
			//
			// _labelItemsPerPage
			//
			_labelItemsPerPage = new JetLinkLabel();
			_labelItemsPerPage.BackColor = Color.Transparent;
			_labelItemsPerPage.Name = "_labelItemsPerPage";
			_labelItemsPerPage.TabIndex = 0;
			_labelItemsPerPage.Text = "items per page";
			_labelItemsPerPage.ClickableLink = false;
			_labelItemsPerPage.Visible = false;
			_labelItemsPerPage.AutoSize = true;

			// Prev Button
			_btnPrevPage = new ImageListButton();
			_btnPrevPage.Text = "< Prev";
			_btnPrevPage.Click += new EventHandler(OnPrevPageClick);
			_btnPrevPage.Visible = false;
			_btnPrevPage.Cursor = Cursors.Hand;
			_btnPrevPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.PreviousPage.Normal.ico")), ImageListButton.ButtonState.Normal);
			_btnPrevPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.PreviousPage.Hot.ico")), ImageListButton.ButtonState.Hot);
			_btnPrevPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.PreviousPage.Disabled.ico")), ImageListButton.ButtonState.Disabled);

			// Next Button
			_btnNextPage = new ImageListButton();
			_btnNextPage.Text = "Next >";
			_btnNextPage.Click += new EventHandler(OnNextPageClick);
			_btnNextPage.Visible = false;
			_btnNextPage.Cursor = Cursors.Hand;
			_btnNextPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.NextPage.Normal.ico")), ImageListButton.ButtonState.Normal);
			_btnNextPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.NextPage.Hot.ico")), ImageListButton.ButtonState.Hot);
			_btnNextPage.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Newspaper.NextPage.Disabled.ico")), ImageListButton.ButtonState.Disabled);
			//
			// NewspaperBar
			//
			Controls.Add(_labelItemsPerPage);
			Controls.Add(_comboItemsPerPage);
			Controls.Add(_labelWithItems);
			Controls.Add(_comboView);
			Controls.Add(_labelShow);
			Controls.Add(_btnPrevPage);
			Controls.Add(_btnNextPage);

			SetStyle(ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ContainerControl
				| ControlStyles.Opaque
				| ControlStyles.ResizeRedraw
				| ControlStyles.UserPaint
				| ControlStyles.EnableNotifyMessage
			         , true);
			SetStyle(ControlStyles.StandardClick
				| ControlStyles.StandardDoubleClick
				| ControlStyles.Selectable
			         , false);
			UpdateStyles();

			Name = "NewspaperBar";
			Height = 24;
			Enabled = false;
			Dock = DockStyle.Bottom;	// Sets the newspaper bar location: either above the newspaper or below it
			Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((Byte)(204)));
			ResumeLayout(false);
		}

		#endregion

		#region Global Events

		/// <summary>
		/// Causes the newspaper bar to lose focus and passes it to the newspaper viewer itself.
		/// </summary>
		protected void LoseFocus()
		{
			Parent.SelectNextControl(this, true, true, true, true);
		}

		/// <summary>
		/// Processes the native Windows messages.
		/// </summary>
		/// <remarks>This is a hack which is needed in here because the combobox cannot catch its own notifications :(</remarks>
		protected override void WndProc(ref Message m)
		{
			switch(m.Msg)
			{
			case Win32Declarations.WM_COMMAND:
				switch(Win32Declarations.HIWORD((UInt32)m.WParam))
				{
				case (UInt16)ComboBoxNotification.CBN_CLOSEUP:
					OnCloseUp(Control.FromChildHandle(m.LParam));
					break;
				}
				break;
			}

			base.WndProc(ref m);
		}

		/// <summary>
		/// Invokes when either of the child comboboxes is dropped down and then closed back.
		/// </summary>
		/// <remarks>This is a hack which is needed in here because the combobox cannot catch its own notifications :(</remarks>
		protected void OnCloseUp(Control source)
		{
			if(source == _comboItemsPerPage)
				OnItemsPerPageCloseUp(source, EventArgs.Empty);
			else if(source == _comboView)
				OnViewsCloseUp(source, EventArgs.Empty);
		}

		/// <summary>
		/// Handles the Newspaper Manager's <see cref="NewspaperManager.Initializing"/> event and defers its processing to a bit later time.
		/// </summary>
		protected void OnManInitializing(object sender, EventArgs args)
		{
			Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddMilliseconds(100), new MethodInvoker(OnManInitializingDeferred), new object[0]);
		}

		/// <summary>
		/// Initializes the newspaper bar controls.
		/// </summary>
		protected void OnManInitializingDeferred()
		{
			if((_man != null) && (_man.IsInitialized))
			{
				using(new LayoutSuspender(this))
				{
					Enabled = true;
					PopulateViewsCombo();
					PopulateItemsPerPageCombo();
					_labelItemsPerPage.Visible = true;
					_labelShow.Visible = true;
					_labelWithItems.Visible = true;
				}
			}
		}

		protected void OnManDeinitializing(object sender, EventArgs e)
		{
			using(new LayoutSuspender(this))
			{
				_comboItemsPerPage.Items.Clear();
				_comboView.Items.Clear();
				Enabled = false;
			}
		}

		#endregion

		#region Views Combobox

		protected void OnViewAdded(object sender, ResourceIndexEventArgs e)
		{
			_comboView.Items.Add(e.Resource);
		}

		protected void OnViewChanged(object sender, ResourcePropIndexEventArgs e)
		{
			// If the changed item is being displayed, update its text rep
			_comboView.Update(); // This re-retrieves the view name
		}

		protected void OnViewDeleted(object sender, ResourceIndexEventArgs e)
		{
			// Find the item that is being deleted
			int nItem = _comboView.Items.IndexOf(e.Resource);
			if(nItem != -1)
			{
				// If this view is selected, select another one instead
				if((nItem == _comboView.SelectedIndex) && (_comboView.Items.Count > 1))
				{
					if(nItem < _comboView.Items.Count - 1)
						_comboView.SelectedIndex = nItem + 1; // Select the next view, if possible
					else if(nItem > 0)
						_comboView.SelectedIndex = nItem - 1; // Select the prev view otherwise
				}

				// Drop this item from the combobox
				_comboView.Items.RemoveAt(nItem);
			}
			else
				Debug.Assert(false, "The updated view is not present in the combo box.");
		}

		/// <summary>
		/// Populate the Views combobox.
		/// </summary>
		protected void PopulateViewsCombo()
		{
			// Remove the old views
			_comboView.Items.Clear();

			// Add the "All" view
			_comboView.Items.Add("All");

			// Add the "Unread" view explicitly (if it exists)
			IResourceList listUnreads = Core.ResourceStore.FindResources(SelectionType.Normal, "SearchView", "DeepName", "Unread");
			IResourceList listViewsButUnread = _man.FilteringViews;
			if(listUnreads.Count == 1)
			{
				_comboView.Items.Add(listUnreads[0]);
				listViewsButUnread = listViewsButUnread.Minus(listUnreads); // Exclude from the further adding
			}

			// Add all the other items, as a tree
			_comboView.AddFolderedResourceTree(Core.ResourceTreeManager.ResourceTreeRoot, "SearchView", "ViewFolder", Core.Props.Parent, 0, _man.FilteringViews, false, true);

			_comboView.Enabled = _comboView.Visible = true;

			// Assign selection
			if(_man.CurrentFilteringView == null)
				_comboView.SelectedIndex = 0;
			else
				_comboView.SelectedIndex = _comboView.Items.IndexOf(_man.CurrentFilteringView);
		}

		protected void OnManCurrentViewChanged(object sender, EventArgs e)
		{
			UpdateCurrentViewData(false);
		}

		protected void OnViewsCloseUp(object sender, EventArgs e)
		{
			OnViewsEnterPressed(sender, new KeyEventArgs(Keys.Enter));
		}

		protected void OnViewsEnterPressed(object sender, KeyEventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return; // Not quite ready

			// Check
			IResource resUI = (_comboView.Items.Count != 0 ? _comboView.SelectedItem as IResource : null); // A filtering view currently selected in the UI, or Null for the "All" view
			if((resUI != null) && (!_man.FilteringViews.Contains(resUI)))
			{
				MessageBox.Show(Parent, String.Format("\"{0}\" is not a valid filtering view for the newspaper.\n\nNote that you cannot select the view folders as filtering views.", resUI), "Newspaper View — " + Core.ProductFullName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Apply!
			UpdateCurrentViewData(true);

			LoseFocus();
		}

		protected void OnViewsEscapePressed(object sender, KeyEventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return; // Not quite ready

			// Update with the actual background data
			UpdateCurrentViewData(false);

			LoseFocus();
		}

		protected void OnViewsSelectionChange(object sender, EventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return; // Not quite ready

			// Defer application of the new value
			Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddMilliseconds(1000), "Apply User Input for Current View.", new BoolDelegate(UpdateCurrentViewData), true);
		}

		/// <summary>
		/// Sends the current view data from UI to Manager (<paramref name="bFromUI"/> is <c>True</c>), or vice versa.
		/// </summary>
		protected void UpdateCurrentViewData(bool bFromUI)
		{
			if((_man == null) || (!_man.IsInitialized))
				return;

			IResource resUI = (_comboView.Items.Count != 0 ? _comboView.SelectedItem as IResource : null); // A filtering view currently selected in the UI, or Null for the "All" view
			if(resUI != _man.CurrentFilteringView)
			{ // UI selection differs from the codebehind
				if(bFromUI) // UI -> Manager
				{
					if((resUI == null) || (_man.FilteringViews.Contains(resUI))) // Validate (there are also view folders in the list)
						_man.CurrentFilteringView = resUI;
				}
				else
				{ // Manager -> UI
					// Visualize the new setting (if necessary)
					int nNewIndex = 0; // Default — "All" selection for the Null value
					if(_man.CurrentFilteringView != null)
					{
						nNewIndex = _comboView.Items.IndexOf(_man.CurrentFilteringView); // Find this item in the combobox
						if(nNewIndex == -1)
							throw new InvalidOperationException("Inconsistency: a valid view is not present in the combobox.");
					}
					if(_comboView.SelectedIndex != nNewIndex) // Apply selection only if it's not the same; avoid re-switching
						_comboView.SelectedIndex = nNewIndex;
				}
			}
		}

		#endregion

		#region ItemsPerPage Combo

		/// <summary>
		/// Invokes when focus leaves the items-per-page combobox.
		/// </summary>
		protected void OnItemsPerPageLeave(object sender, EventArgs e)
		{
			UpdateItemsPerPageData(false); // Display the actual value
		}

		/// <summary>
		/// The Newspaper Manager's value for items on page has changed.
		/// </summary>
		protected void OnManItemsPerPageChanged(object sender, EventArgs e)
		{
			// Visualize the new value (if necessary)
			UpdateItemsPerPageData(false);
		}

		/// <summary>
		/// Fills in the items-per-page combobox.
		/// </summary>
		protected void PopulateItemsPerPageCombo()
		{
			// Clear
			_comboItemsPerPage.Items.Clear();

			// Load the new settings
			string sDefault = "10,25,50,100";
			string sValues = Core.SettingStore.ReadString(_man.GetSettingsKey(true), "ItemsPerPageValues", sDefault);

			// Fill in
			string[] arValues = sValues.Split(','); // String rep of individual proposed values
			foreach(string sValue in arValues)
			{
				try
				{
					// Convert to int and then back to string to canonicize and avoid illegal values in the default-values-combo
					int nValue = int.Parse(sValue); // This also checks if the string is a valid number
					if((nValue <= 0) || (nValue >= NewspaperManager.c_nMaxItemsOnPage)) // Check the constraints
					{
						Trace.WriteLine("Warning: the \"{0}\" value for the ItemsPerPage combobox does not fall into the allowed range.", sValue);
						continue;
					}
					_comboItemsPerPage.Items.Add(nValue.ToString());
				}
				catch(Exception)
				{
					Trace.WriteLine("Warning: the \"{0}\" value for the ItemsPerPage combobox is not coercible to an integer.", sValue);
				}
			}

			_comboItemsPerPage.Visible = _comboItemsPerPage.Enabled = true;

			// Set selection or explicit text to the proper item
			UpdateItemsPerPageData(false);

			// Reapply the new value after some time; this helps to prevent the combobox from setting the value whose prefix is currently typed in
			Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddMilliseconds(100), "Apply Initial Value for Items per Page.", new BoolDelegate(UpdateItemsPerPageData), false);
		}

		/// <summary>
		/// The combobox text has changed.
		/// </summary>
		protected void OnItemsPerPageChange(object sender, EventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return; // Not quite ready

			// Defer application of the new value
			Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddMilliseconds(1000), "Apply User Input for Items per Page.", new BoolDelegate(UpdateItemsPerPageData), true);
		}

		/// <summary>
		/// Applies a value from the combobox to the Newspaper Manager (if <paramref name="bFromUI"/> is <c>True</c>)
		/// or vice versa.
		/// Deferred-invoked when text in the combobox changes, or immediately-invoked when the Enter/ESC key is pressed.
		/// </summary>
		protected void UpdateItemsPerPageData(bool bFromUI)
		{
			if((_man == null) || (!_man.IsInitialized))
				return;

			Trace.WriteLine( String.Format("Updating items-per-page data in direction {0}.", bFromUI), "[NPB]" );

			if(_man.ItemsPerPage.ToString() != _comboItemsPerPage.Text)
			{ // UI value differs from the codebehind value
				if(bFromUI)
				{ // UI -> Manager
					try
					{
						_man.ItemsPerPage = int.Parse(_comboItemsPerPage.Text);
					}
					catch(Exception ex)
					{
						// Means that the value entered into the control is not a valid number, do nothing
						Trace.WriteLine("Failed to apply the Items per Page combobox value. " + ex.Message, "[NPB]");
					}
				}
				else
				{ // Manager -> UI
					int nIndex = _comboItemsPerPage.FindStringExact(_man.ItemsPerPage.ToString());
					_comboItemsPerPage.SelectedIndex = nIndex;
					if(nIndex < 0) // Select the existing item with such a value, or remove list item selection if there is none available
						_comboItemsPerPage.Text = _man.ItemsPerPage.ToString(); // There's no stock item with this value, type it by hand
				}
			}
		}

		protected void OnItemsPerPageEnterPressed(object sender, KeyEventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return; // Not quite ready

			e.Handled = true;

			/////////////
			// Validate

			// Check for an empty value
			if(_comboItemsPerPage.Text.Length == 0)
			{
				MessageBox.Show("The number of items per page cannot be empty.", Core.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Try coercing to a number
			int nItemsPerPage;
			try
			{
				nItemsPerPage = int.Parse(_comboItemsPerPage.Text);
			}
			catch(Exception)
			{
				MessageBox.Show(String.Format("\"{0}\" cannot be coerced to an integer value and is not a valid setting for number of items per page.", _comboItemsPerPage.Text), Core.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Check if non-positive
			if((nItemsPerPage <= 0) || (nItemsPerPage >= NewspaperManager.c_nMaxItemsOnPage))
			{
				MessageBox.Show(String.Format("The number of items per page must be a positive integer below {0}.", NewspaperManager.c_nMaxItemsOnPage), Core.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			///////////
			// Apply!

			// Apply the new setting
			UpdateItemsPerPageData(true);

			// Lose the focus
			LoseFocus();
		}

		protected void OnItemsPerPageEscapePressed(object sender, KeyEventArgs e)
		{
			UpdateItemsPerPageData(false); // In case the input is invalid, cause the previous valid value to appear in the editbox
			LoseFocus();
		}

		protected void OnItemsPerPageCloseUp(object sender, EventArgs e)
		{
			OnItemsPerPageEnterPressed(sender, new KeyEventArgs(Keys.Enter));
		}

		#endregion

		#region Paging Toolbar

		/// <summary>
		/// The set of pages or current page has changed.
		/// </summary>
		protected void OnManPagingChanged(object sender, EventArgs e)
		{
			PerformLayout();
		}

		/// <summary>
		/// The prev-page button has been clicked.
		/// </summary>
		private void OnPrevPageClick(object sender, EventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return;
			if(_man.CurrentPage > 0)
				_man.CurrentPage--;
			LoseFocus();
		}

		/// <summary>
		/// The next-page button has been clicked.
		/// </summary>
		private void OnNextPageClick(object sender, EventArgs e)
		{
			if((_man == null) || (!_man.IsInitialized))
				return;
			if(_man.CurrentPage < _man.PagesCount - 1)
				_man.CurrentPage++;
			LoseFocus();
		}

		/// <summary>
		/// Creates and returns a disposable brush for painting a background of a child control that resides on this one.
		/// If a brush is a gradient brush, it has to be specifically adjusted to fit the background.
		/// </summary>
		/// <param name="sender">Control for which the brush is being requested.
		/// This may be needed for calculating the rect for gradient brushes.</param>
		/// <returns>Background brush.</returns>
		public Brush GetBackgroundBrush(Control sender)
		{
			// Perform a safety check to ensure that the rectangle falls within the parent control
			Rectangle rectBrush = sender.RectangleToClient(RectangleToScreen(ClientRectangle));
			if((rectBrush.Width != 0) && (rectBrush.Height != 0))
				return new LinearGradientBrush(rectBrush, SystemColors.ControlLight, SystemColors.Control, LinearGradientMode.Vertical);
			else
				return new SolidBrush(SystemColors.Control);
		}

		#endregion

		#region Layouting

		#region Layouting Constants

		/// <summary>
		/// Layouting Constants Class.
		/// </summary>
		public class Const
		{
			/// <summary>
			/// Horizontal margin of the control.
			/// </summary>
			public static readonly int HorMargin = 5;

			/// <summary>
			/// Vertical margin of the control.
			/// </summary>
			public static readonly int VerMargin = 1;

			/// <summary>
			/// Gep between the adjacent controls.
			/// </summary>
			public static readonly int Gap = 5;

			/// <summary>
			/// Horizontal spacing between the last of the filtering controls and the first paging button (including the prev/next buttons).
			/// </summary>
			public static readonly int HorSpacingBeforePagingButtons = 20;

			/// <summary>
			/// Distance from the paging button text (displayed within) to the button edge.
			/// </summary>
			public static readonly int PagingButtonTextHorPadding = 7;

			/// <summary>
			/// Distance from the paging button text (displayed within) to the button edge.
			/// </summary>
			public static readonly int PagingButtonTextVerPadding = 2;

			/// <summary>
			/// Width of the separator between the paging buttons that is inserted in case there's a jump in numbers of the adjacent buttons (instead of the cut-out buttons).
			/// </summary>
			public static readonly int PagingJumpSeparatorHorSpacing = 4;

			/// <summary>
			/// Padding between the ends of the hover-underline of the paging button and the button edges.
			/// </summary>
			public static readonly int PagingButtonHoverUnderlinePadding = 2;
		}

		#endregion

		/// <summary>
		/// Layouts the controls.
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			// Calculate the client area
			Rectangle client = ClientArea;

			// If the newspaper manager has not been initialized, hide the controls and avoid layouting them
			if((_man == null) || ((_man == null) || (!_man.IsInitialized)))
			{
				foreach(Control control in Controls) // Hide all the controls available
					control.Visible = false;
				return;
			}

			// Layout the fixed controls
			int nPos = client.Left;

			// A list of the controls that belong to the fixed set and are located at the left of the bar
			Control[] controlsLeftFixed = new Control[] {_labelShow, _comboView, _labelWithItems, _comboItemsPerPage, _labelItemsPerPage};
			bool bFirst = true;
			foreach(Control control in controlsLeftFixed)
			{
				if(nPos + control.Width <= client.Right)
				{
					if(bFirst) // Do not add a gap before the first control
						bFirst = false;
					else
						nPos += Const.Gap;
					control.Location = new Point(nPos, client.Top + (client.Height - control.Height) / 2); // V-center
					control.Visible = true;
					nPos += control.Width;
				}
				else
					control.Visible = false;
			}

			// Add the horizontal spacing before the paging buttons
			nPos += Const.HorSpacingBeforePagingButtons;

			// Place the "Prev" button
			if(nPos + _btnPrevPage.Width <= client.Right)
			{
				_btnPrevPage.Location = new Point(nPos, client.Top + (client.Height - _btnPrevPage.Height) / 2); // V-center
				_btnPrevPage.Visible = true;
				nPos += _btnPrevPage.Width;
				nPos += Const.Gap;
			}
			else
				_btnPrevPage.Visible = false;

			// Calculate the space left for the paging controls (exclude the Next button that is about to appear on the right)
			Rectangle rectPaging = Rectangle.FromLTRB(nPos, client.Top, client.Right - _btnNextPage.Width - Const.Gap, client.Bottom);

			// Layout the paging controls
			OnLayout_Paging(ref rectPaging);

			// Place the "Next" button
			if(rectPaging.Right + Const.Gap + _btnNextPage.Width <= client.Right)
			{
				_btnNextPage.Location = new Point(rectPaging.Right + Const.Gap, client.Top + (client.Height - _btnNextPage.Height) / 2); // V-center
				_btnNextPage.Visible = true;
			}
			else
				_btnNextPage.Visible = false;

			// Repaint the separators
			Invalidate(false);
		}

		protected override void ScaleCore(float dx, float dy)
		{
			_sizeScale = new SizeF(dx, dy);

			// Apply scaling to the bar (the controls will be scaled automatically)
			Height = (int)(24 * dy);
		}

		/// <summary>
		/// Calculates layout for the paging buttons, and updates the rectangle to indicate the actually-consumed space.
		/// </summary>
		private void OnLayout_Paging(ref Rectangle paging)
		{
			// No place for the controls?
			_nUsedPagingButtons = 0; // Mark all the buttons as unused
			if((paging.Width <= 0) || (paging.Height <= 0))
			{ // Not enough place for layouing a single button
				foreach(Control control in _btnPaging)
					control.Visible = false;

				// Validate the rectangle to zero width
				paging = Rectangle.FromLTRB(paging.Left, paging.Top, paging.Left, paging.Bottom);
				return;
			}

			int nCurPage = _man.CurrentPage;
			int nNumPages = _man.PagesCount;

			int nAvailWidth = paging.Width; // Width available for all the buttons
			nAvailWidth -= Const.PagingJumpSeparatorHorSpacing * 2; // Reserve some place for the two possible cuttings

			RedBlackTree treePresent = new RedBlackTree();
			InsertPagingButton(treePresent, ref nAvailWidth, 0); // First page button that is never cut
			InsertPagingButton(treePresent, ref nAvailWidth, nNumPages - 1); // Last page button that is never cut
			InsertPagingButton(treePresent, ref nAvailWidth, nCurPage); // The current page button

			// Now go on inserting the buttons on both sides of the current page
			int nMaxOffs = Math.Max(nCurPage - 0 - 1, (nNumPages - 1) - nCurPage - 1); // Maximum distance from the current page to the ends
			for(int nOffs = 1; nOffs <= nMaxOffs; nOffs++)
			{
				// Left side: if within the scope, try inserting; break if space is thru
				if((nCurPage - nOffs > 0) && (InsertPagingButton(treePresent, ref nAvailWidth, nCurPage - nOffs) == InsertPagingButtonResult.NoRoom))
					break;

				// Right side: if within the scope, try inserting; break if space is thru
				if((nCurPage + nOffs < nNumPages - 1) && (InsertPagingButton(treePresent, ref nAvailWidth, nCurPage + nOffs) == InsertPagingButtonResult.NoRoom))
					break;
			}

			// Implement the results in the layouting
			RBNodeBase node = treePresent.GetMinimumNode(); // The first page button
			int nPrevPageNumber = -1; // Check for cuttings (jumps in numbering)
			int nCurPos = paging.Left;
			ArrayList separators = new ArrayList(); // Collect the separators that indicate the jumps in page numbers here
			while(node != null)
			{
				PagingButton button = (PagingButton)node.Key;

				// If there's a numbering jump, add a spacing (don't check for the very first button)
				if((nPrevPageNumber >= 0) && (button.PageNumber != nPrevPageNumber + 1))
				{
					// Add the separator-drawing info
					separators.Add(new Rectangle(nCurPos, paging.Top, Const.PagingJumpSeparatorHorSpacing, paging.Height));
					nCurPos += Const.PagingJumpSeparatorHorSpacing;
				}

				// Place the button
				button.Size = button.OptimalSize;
				button.Location = new Point(nCurPos, paging.Top + (paging.Height - button.Height) / 2);
				button.Visible = true;
				nCurPos += button.Width;
				nPrevPageNumber = button.PageNumber;

				// Advance to the next button
				node = treePresent.GetNext(node);
			}
			_separators = (Rectangle[])separators.ToArray(typeof(Rectangle));

			// Hide the buttons that have been created but are unused
			HideUnusedPagingButtons();

			// Update the prev-next page buttons state
			_btnNextPage.Enabled = nCurPage < nNumPages - 1;
			_btnNextPage.TabIndex = c_nPagingButtonsTabIndexBase + nNumPages;
			_btnPrevPage.Enabled = nCurPage > 0;
			_btnPrevPage.TabIndex = c_nPagingButtonsTabIndexBase - 1;

			// Return the actually-used rectangle
			paging = Rectangle.FromLTRB(paging.Left, paging.Top, nCurPos, paging.Bottom);
		}

		/// <summary>
		/// Inserts a paging button to the pending list
		/// </summary>
		/// <param name="tree">Tree that holds the buttons.</param>
		/// <param name="nAvailWidth">The available width that is decreased in case of a successful insertion.</param>
		/// <param name="nPageNumber">Page number for the button.</param>
		/// <returns>Whether the insertion had success.</returns>
		private InsertPagingButtonResult InsertPagingButton(RedBlackTree tree, ref int nAvailWidth, int nPageNumber)
		{
			// Get the button and measure its wannabe-size
			PagingButton button = GetPagingButton(nPageNumber, false);
			Size sizeButton = button.OptimalSize;

			// Is there room for it left?
			// Note: even if there's no room, we have to check if it's present already
			if(sizeButton.Width > nAvailWidth)
				return tree.Search(button) != null ? InsertPagingButtonResult.AlreadyThere : InsertPagingButtonResult.NoRoom;

			// Has it been inserted already?
			RBNodeBase foundOrNew;
			if(tree.SearchOrInsert(button, out foundOrNew)) // Try inserting a new button, true retval indicates a failute (the item is already there)
				return InsertPagingButtonResult.AlreadyThere;

			// Allow it to be inserted
			button = GetPagingButton(nPageNumber, true);
			nAvailWidth -= button.OptimalSize.Width;
			return InsertPagingButtonResult.OK; // Inserted a new one
		}

		/// <summary>
		/// Possible result of the <see cref="InsertPagingButton"/> function execution.
		/// </summary>
		private enum InsertPagingButtonResult
		{
			OK,
			NoRoom,
			AlreadyThere
		}

		/// <summary>
		/// Returns the next free paging button without marking it as used.
		/// </summary>
		/// <param name="nPageNumber">Number of the page that should be assigned to the button.</param>
		/// <param name="bMarkAsUsed">Either marks the button as used or not.</param>
		private PagingButton GetPagingButton(int nPageNumber, bool bMarkAsUsed)
		{
			PagingButton button;
			if(_nUsedPagingButtons == _btnPaging.Count)
			{ // If no more free buttons, create one
				button = new PagingButton(nPageNumber, this, _man);
				button.Visible = false;
				button.Font = Font;
				_btnPaging.Add(button);
				Controls.Add(button);
			}
			else // Reuse some free button
				button = (PagingButton)_btnPaging[_nUsedPagingButtons];

			button.PageNumber = nPageNumber;
			if(bMarkAsUsed)
				_nUsedPagingButtons++;
			return button;
		}

		/// <summary>
		/// Makes the unused buttons invisible.
		/// </summary>
		private void HideUnusedPagingButtons()
		{
			for(int a = _nUsedPagingButtons; a < _btnPaging.Count; a++)
				((PagingButton)_btnPaging[a]).Visible = false;
		}

		#endregion

		#region Painting

		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle rectNoBorder = ClientRectangle;
			rectNoBorder = new Rectangle(rectNoBorder.Left, rectNoBorder.Top + (Dock == DockStyle.Bottom ? 1 : 0), rectNoBorder.Width, rectNoBorder.Height - 1); // Leave some place for the border, either at the top (docked at bottom) or at the bottom (docked at top)

			// Background
			using(Brush brush = GetBackgroundBrush(this))
				e.Graphics.FillRectangle(brush, rectNoBorder);

			// Border
			using(Brush brush = new SolidBrush(NewspaperViewer.c_colorBorder))
			{
				Rectangle client = ClientRectangle;
				e.Graphics.FillRectangle(brush, new Rectangle(client.Left, (Dock == DockStyle.Bottom ? client.Top : client.Bottom - 1), client.Width, 1));
			}

			// Paint the control, if it's initialized and ready, otherwise, show the banner
			if((_man != null) && (_man.IsInitialized))
			{
				// Paint the separators at the places where the jumps in page numbering are located (collected by the layouting)
				foreach(Rectangle rectangle in _separators)
				{
					int nLeft = (rectangle.Left + rectangle.Right) / 2 - 1;
					e.Graphics.FillRectangle(SystemBrushes.ControlDark, new Rectangle(nLeft, rectangle.Top, 1, rectangle.Height));
					e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, new Rectangle(nLeft + 1, rectangle.Top, 1, rectangle.Height));
				}
			}
			else // The "initializing …" banner
				JetLinkLabel.DrawText(e.Graphics, "Newspaper is not ready, please wait …", rectNoBorder, Font, SystemColors.Control, DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_VCENTER | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_END_ELLIPSIS);
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Controls the docking of the Newspaper Bar: either at top or at bottom.
		/// Note that this does not actually affect the parent newspaper layouting, but controls the way Newspaper bar draws its borders, to avoid the double borders.
		/// </summary>
		public override DockStyle Dock
		{
			get { return base.Dock; }
			set
			{
				if(!((value == DockStyle.Top) || (value == DockStyle.Bottom)))
					throw new ArgumentException("The bar can be docked at either top or bottom. Other values are prohibited.");

				if(value != base.Dock)
					base.Dock = value;

				PerformLayout(); // Update the borders
				Invalidate(); // Repaint
			}
		}

		/// <summary>
		/// Gets the client are of the Newspaper bar, which is the <see cref="ClientRectangle"/> minus the border(s) and margins.
		/// </summary>
		public Rectangle ClientArea
		{
			get
			{
				Rectangle client = ClientRectangle;

				// Leave some place for the border, either at the top (docked at bottom) or at the bottom (docked at top)
				client = new Rectangle(client.Left, client.Top + (Dock == DockStyle.Bottom ? 1 : 0), client.Width, client.Height - 1);

				// Account for the margins
				client.Inflate(-Const.HorMargin, -Const.VerMargin);

				return client;
			}
		}

		#endregion

		#region Class PagingButton — A class that represents a paging button.

		/// <summary>
		/// A class that represents a paging button.
		/// </summary>
		internal class PagingButton : UserControl, IComparable, ICommandBar
		{
			#region Data

			/// <summary>
			/// Number of the page represented by this button.
			/// </summary>
			protected int _nPageNumber;

			/// <summary>
			/// Newspaper bar that owns the control.
			/// </summary>
			protected readonly NewspaperBar _bar;

			/// <summary>
			/// The newspaper manager.
			/// </summary>
			protected readonly NewspaperManager _man;

			/// <summary>
			/// Bounding rectangle of the button text.
			/// </summary>
			protected Rectangle _rectText = Rectangle.Empty;

			/// <summary>
			/// True if the button is currently hovered with mouse.
			/// </summary>
			protected bool _bHovered = false;

			#endregion

			#region Construction

			internal PagingButton(int nPageNumber, NewspaperBar bar, NewspaperManager man)
			{
				_nPageNumber = nPageNumber;
				_bar = bar;
				_man = man;

				// Set the control styles
				SetStyle(ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.FixedHeight
					| ControlStyles.FixedWidth
					| ControlStyles.Opaque
					| ControlStyles.ResizeRedraw
					| ControlStyles.StandardClick
					| ControlStyles.UserPaint
				         , true);

				// Set the back color (it applies to an active button only, otherwise, transparency is emulated)
				BackColor = ColorManagement.Mix(SystemColors.Control, SystemColors.ControlDark, 0.66);
			}

			#endregion

			#region IComparable Members

			/// <summary>
			/// Provides for comparing the page buttons by their page number.
			/// </summary>
			public int CompareTo(object obj)
			{
				return PageNumber.CompareTo(((PagingButton)obj).PageNumber);
			}

			#endregion

			#region Attributes

			/// <summary>
			/// Number of the page represented by this button.
			/// Internal rep, zero-based.
			/// </summary>
			public int PageNumber
			{
				get { return _nPageNumber; }
				set
				{
					// Store & update the related properties
					_nPageNumber = value;
					Text = DisplayPageNumber.ToString();
					Enabled = !Active;
					TabIndex = NewspaperBar.c_nPagingButtonsTabIndexBase + _nPageNumber;
					Cursor = Active ? Cursors.Default : Cursors.Hand;

					// The first, last, and current pages are painted in bold
					bool bBold = false;
					if((_man != null) && (_man.IsInitialized))
						bBold = (_nPageNumber == 0) || (_nPageNumber == _man.PagesCount - 1) || (_nPageNumber == _man.CurrentPage);
					Font = bBold ? new Font(_bar.Font, FontStyle.Bold) : _bar.Font;

					// Calculate the text bounds
					_rectText = new Rectangle(new Point(0, 0), JetLinkLabel.GetTextSize(this, Text, Font));

					PerformLayout(); // Adjust placement of the text rect
					Invalidate(); // Repaint the button
				}
			}

			/// <summary>
			/// Page number as it should be displayed to user (one-based).
			/// </summary>
			public int DisplayPageNumber
			{
				get { return _nPageNumber + 1; }
			}

			/// <summary>
			/// Gets whether this button represents an active page.
			/// </summary>
			public bool Active
			{
				get { return (_man.IsInitialized) && (_man.CurrentPage == _nPageNumber); }
			}

			#endregion

			#region ICommandBar Members

			public void SetSite(ICommandBarSite site)
			{
			}

			public Size MinSize
			{
				get { return new Size(10, 10); } // Just let it be
			}

			public Size MaxSize
			{
				get { return new Size(int.MaxValue, int.MaxValue); } // Just let it be
			}

			/// <summary>
			/// This is the destiation size of the button. The real size is not changed until it is shown in the layout.
			/// </summary>
			public Size OptimalSize
			{
				get { return _rectText.Size + new Size(Const.PagingButtonTextHorPadding * 2, Const.PagingButtonTextVerPadding * 2); }
			}

			public Size Integral
			{
				get { return new Size(1, 1); }
			}

			#endregion

			#region Overrides

			/// <summary>
			/// The paging button has been clicked. Try switching to the page.
			/// </summary>
			protected override void OnClick(EventArgs e)
			{
				if((_man == null) || (!_man.IsInitialized))
					return;

				if(PageNumber < _man.PagesCount)
					_man.CurrentPage = PageNumber;
				_bar.LoseFocus(); // Return focus to the reading pane
			}

			protected override void OnLayout(LayoutEventArgs levent)
			{
				if(_rectText == Rectangle.Empty) // The button has not been assigned the page number, no layouting possible
					return;

				// Update the text rectangle placement
				Rectangle client = ClientRectangle;
				_rectText = new Rectangle(new Point(client.Left + (client.Width - _rectText.Width) / 2, client.Top + (client.Height - _rectText.Height) / 2), _rectText.Size);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				Rectangle client = ClientRectangle;

				// Background
				using(Brush brush = Active ? new SolidBrush(BackColor) : _bar.GetBackgroundBrush(this))
					e.Graphics.FillRectangle(brush, client);

				// Button text
				JetLinkLabel.DrawText(e.Graphics, Text, _rectText, Font, (Active ? Color.Black : Color.Blue), DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE);

				// Underline the hovered button
				if((_bHovered) && (!Active))
				{
					using(Brush brush = new SolidBrush(Color.Blue))
						e.Graphics.FillRectangle(brush, Rectangle.FromLTRB(client.Left + Const.PagingButtonHoverUnderlinePadding, _rectText.Bottom - 1, client.Right - Const.PagingButtonHoverUnderlinePadding, _rectText.Bottom));
				}
			}

			protected override void OnMouseEnter(EventArgs e)
			{
				base.OnMouseEnter(e);
				_bHovered = true;
				Invalidate(new Rectangle(0, _rectText.Bottom - 1, ClientRectangle.Width, 1));
			}

			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				_bHovered = false;
				Invalidate(new Rectangle(0, _rectText.Bottom - 1, ClientRectangle.Width, 1));
			}

			#endregion
		}

		#endregion
	}
}

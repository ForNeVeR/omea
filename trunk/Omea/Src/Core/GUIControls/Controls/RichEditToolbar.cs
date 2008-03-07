/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A toolbar which contais buttons and other controls applicable to formatting the content of rich editors, such as HTML or RTF editors.
	/// The toolbar is bound to the control it rules over by giving it the proper command processor after creation.
	/// </summary>
	public class RichEditToolbar : GradientToolbar, IContextProvider
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		/// <summary>
		/// Manages the toolbar's actions by adding the controls, handling their events and binding them to the handlers that execute or query-state the actions.
		/// </summary>
		protected ToolbarActionManager _actionmanager;

		/// <summary>
		/// Someone who gives us the one who executes our commands, that's it!
		/// </summary>
		protected IContextProvider _contextprovider;

		public RichEditToolbar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Add the controls
			IntroduceActions();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
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
			// 
			// RichEditToolbar
			// 
			this.Name = "RichEditToolbar";

			DropDownArrows = true;
			ShowToolTips = true;
			Appearance = ToolBarAppearance.Flat;
			//AutoSize = false;
			Divider = true;
			GradientEndColor = SystemColors.Control;
			GradientStartColor = SystemColors.ControlLightLight;
			TextAlign = ToolBarTextAlign.Right;
		}

		#endregion

		/// <summary>
		/// Populates toolbar with the controls and registers actions for them.
		/// </summary>
		protected void IntroduceActions()
		{
			// Re-create the toolbar action manager
			_actionmanager = new ToolbarActionManager( this );
			_actionmanager.ContextProvider = this;
			string sGroup;

			// General Edit actions
			sGroup = "Edit";
			_actionmanager.RegisterActionGroup( sGroup, ListAnchor.Last );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Undo" ), sGroup, ListAnchor.Last, LoadImage( "Undo" ), "", "Undo", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Redo" ), sGroup, ListAnchor.Last, LoadImage( "Redo" ), "", "Redo", null, null );

			// Formatting actions
			sGroup = "Font";
			_actionmanager.RegisterActionGroup( sGroup, ListAnchor.Last );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Bold" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Bold" ), "", "Bold", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Italic" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Italic" ), "", "Italic", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Underline" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Underline" ), "", "Underline", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "StrikeThrough" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.StrikeThrough" ), "", "Strike-out", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Subscript" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Subscript" ), "", "Subscript", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Superscript" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Superscript" ), "", "Superscript", null, null );

			// Alignment
			sGroup = "Paragraph";
			_actionmanager.RegisterActionGroup( sGroup, ListAnchor.Last );
			_actionmanager.RegisterAction( new CommandProcessorAction( "JustifyLeft" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.JustifyLeft" ), "", "Align Left", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "JustifyCenter" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.JustifyCenter" ), "", "Center", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "JustifyRight" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.JustifyRight" ), "", "Align Right", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "JustifyFull" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.JustifyFull" ), "", "Justify", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "InsertOrderedList" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.InsertOrderedList" ), "", "Numbering", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "InsertUnorderedList" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.InsertUnorderedList" ), "", " Bullets", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Outdent" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Outdent" ), "", "Decrease Indent", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "Indent" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.Indent" ), "", "Increase Indent", null, null );

			// Insertions
			sGroup = "Insert";
			_actionmanager.RegisterActionGroup( sGroup, ListAnchor.Last );
			_actionmanager.RegisterAction( new CommandProcessorAction( "CreateLink" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.CreateLink" ), "", "Create Hyperlink", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "InsertHorizontalRule" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.InsertHorizontalRule" ), "", "Insert Horizontal Line", null, null );
			_actionmanager.RegisterAction( new CommandProcessorAction( "InsertImage" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.InsertImage" ), "", "Insert Picture", null, null );
		}

		/// <summary>
		/// Loads an icon for the RichEdit toolbar.
		/// </summary>
		/// <param name="name">A name of the command that forms the icon name, without the folder prefix or file extension.</param>
		/// <returns>The loaded icon resource.</returns>
		protected static Image LoadImage( string name )
		{
            Image icon = Utils.GetResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "GUIControls.Icons." + name + ".ico" );
            return icon;
		}

		/// <summary>
		/// An object that provides the execution context for the toolbar actions.
		/// </summary>
		public IContextProvider ContextProvider
		{
			get { return _contextprovider; }
			set { _contextprovider = value; }
		}

		/// <summary>
		/// A <see cref="ToolbarActionManager">toolbar action manager</see> that handles the binding of toolbar controls to the actions they represent.
		/// </summary>
		public ToolbarActionManager ActionManager
		{
			get { return _actionmanager; }
		}

		#region IContextProvider Members

		/// <summary>
		/// Provides the action execution context by quering the supplied context provider.
		/// </summary>
		public IActionContext GetContext( ActionContextKind kind )
		{
			// Relay context acquision to the supplied context provider, if specified
			if(_contextprovider != null)
				return _contextprovider.GetContext( kind );

			// Generate an empty context, if not set
			return new ActionContext(kind, this, null);
		}

		#endregion
	}
}
/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{

	#region DragDropLinkAdapter Class — adds default Link functionality to the drag-drop handlers

	/// <summary>
	/// Wrapper around a drag/drop handler which provides "Add Link" functionality when the
	/// drop cannot be handled by the base handler.
	/// </summary>
	public class DragDropLinkAdapter : IResourceDragDropHandler
	{
		/// <summary>
		/// The handler being wrapped.
		/// </summary>
		private IResourceDragDropHandler _baseHandler;

		/// <summary>
		/// The handler that implements link-by-dnd behavior, which is invoked 
		/// when the <see cref="_baseHandler"/> refuses to handle the operation.
		/// </summary>
		protected static ResourceDragDropLinkHandler _linkhandler;

		/// <summary>
		/// Wraps the <paramref name="baseHandler"/> handler.
		/// </summary>
		public DragDropLinkAdapter( IResourceDragDropHandler baseHandler )
		{
			_baseHandler = baseHandler;
			if( _linkhandler == null ) // Lazy-instantiate
				_linkhandler = new ResourceDragDropLinkHandler();
		}

		public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
		{
			_baseHandler.AddResourceDragData( dragResources, dataObject );
			_linkhandler.AddResourceDragData( dragResources, dataObject );
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data,
		                                 DragDropEffects allowedEffect, int keyState )
		{
			// Get the wrapped handler's opinion on the operation
			DragDropEffects baseEffects = _baseHandler.DragOver( targetResource, data, allowedEffect, keyState );
			// If base can handle, let it; otherwise, query the link-adapter
			return baseEffects != DragDropEffects.None ? baseEffects : _linkhandler.DragOver( targetResource, data, allowedEffect, keyState );
		}

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			// If the base handler can handle the dragover, let it do the drop
			if(_baseHandler.DragOver(targetResource, data, allowedEffect, keyState) != DragDropEffects.None)
			{
				_baseHandler.Drop(targetResource, data, allowedEffect, keyState);
				return;
			}

			// Now give the link-handler a try
			if(_linkhandler.DragOver(targetResource, data, allowedEffect, keyState) != DragDropEffects.None)
			{
				_linkhandler.Drop(targetResource, data, allowedEffect, keyState);
				return;
			}
		}
	}

	#endregion

	#region ResourceDragDropLinkHandler Clas — Implements the drag'n'drop handler that establishes a link between two arbitary resources

	public class ResourceDragDropLinkHandler : IResourceDragDropHandler
	{
		#region IResourceDragDropHandler Members

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( (data.GetDataPresent( typeof(IResourceList) ))
				&& (targetResource != null)
				&& (!Core.ResourceStore.ResourceTypes[ targetResource.Type ].HasFlag( ResourceTypeFlags.Internal )) )
			{
				IResourceList dropList = (IResourceList)data.GetData( typeof(IResourceList) );
				Core.UIManager.ShowAddLinkDialog( dropList, targetResource );
			}
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			return
				((data.GetDataPresent( typeof(IResourceList) ))
					&& (targetResource != null)
					&& (!Core.ResourceStore.ResourceTypes[ targetResource.Type ].HasFlag( ResourceTypeFlags.Internal )))
					? DragDropEffects.Link
					: DragDropEffects.None;
		}

		public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
		{
			// This handler has nothing to add
			return;
		}

		#endregion
	}

	#endregion

	#region ResourceDragDropCompositeHandler Class — Implements a drag-drop handler that encapsulates a number of other handlers

	public class ResourceDragDropCompositeHandler : IResourceDragDropHandler
	{
		#region Data

		/// <summary>
		/// A list of handlers this composite invokes.
		/// </summary>
		protected ArrayList _handlers = new ArrayList();

		/// <summary>
		/// Specifies whether the "Add Link" drop handler is suggested when none of the composed handlers fits.
		/// </summary>
		protected bool _bAddLink = true;

		/// <summary>
		/// The default link handler to fallback to if none of the composed handlers works out.
		/// </summary>
		protected static IResourceDragDropHandler _linkhandler = null;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a composite handler with no contained handlers, just with the default link handler.
		/// </summary>
		public ResourceDragDropCompositeHandler()
		{
			if( _linkhandler == null )
				_linkhandler = new ResourceDragDropLinkHandler();
		}

		/// <summary>
		/// Creates a composite handler wrapping one raw handler, plus the default link handler.
		/// <see cref="AddHandler"/> for more details.
		/// </summary>
		public ResourceDragDropCompositeHandler( IResourceDragDropHandler handlerBase )
		{
			if( _linkhandler == null )
				_linkhandler = new ResourceDragDropLinkHandler();

			AddHandler( handlerBase );
		}

		/// <summary>
		/// Creates a composite handler wrapping an arbitiary number of handlers, plus the default link handler.
		/// <see cref="AddHandler"/> for more details.
		/// </summary>
		public ResourceDragDropCompositeHandler( params object[] args )
		{
			if( _linkhandler == null )
				_linkhandler = new ResourceDragDropLinkHandler();

			foreach(object oHandler in args)
			{
				IResourceDragDropHandler handler = oHandler as IResourceDragDropHandler;
				if(handler == null)
					throw new ArgumentException("All the arguments must implement the IResourceDragDropHandler interface.");
				AddHandler( handler );
			}
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets a collection of the handlers wrapped by this composite handler; the default link hanlder is not included.
		/// </summary>
		[Browsable( false )]
		public ICollection Handlers
		{
			get { return _handlers; }
		}

		/// <summary>
		/// Gets the default link hanlder that is invoked when none of the wrapped handlers fits, 
		/// and <see cref="AddLink"/> is set to <c>True</c>.
		/// </summary>
		[Browsable( false )]
		public IResourceDragDropHandler LinkHandler
		{
			get { return _linkhandler; }
		}

		/// <summary>
		/// Gets or sets whether the default link handler (as provided by <see cref="LinkHandler"/>) gets executed when
		/// none of the wrapped handlers fits.
		/// </summary>
		[DefaultValue( true )]
		public bool AddLink
		{
			get { return _bAddLink; }
			set { _bAddLink = value; }
		}

		#endregion

		#region Operations

		/// <summary>
		/// Adds a new handler to the wrapped handlers list, as the last one in the list.
		/// The handlers will be executed in the same order as they were added.
		/// If the <paramref name="handler"/> is a composite handler of the same type as <c>this</c>,
		/// its members are added one by one instead of adding the composite handler itself.
		/// </summary>
		public void AddHandler( IResourceDragDropHandler handler )
		{
			// For a composite, add all of its members
			ResourceDragDropCompositeHandler composite = handler as ResourceDragDropCompositeHandler;
			if( composite != null )
			{
				foreach( IResourceDragDropHandler handlerInner in composite.Handlers )
					AddHandler( handlerInner );
			}
			else
				_handlers.Add( handler );
		}

		#endregion

		#region IResourceDragDropHandler Members

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			// Find who can handle the drop (including the link handler, if allowed)
			IResourceDragDropHandler handlerFit;
			GetDragOverHandler( targetResource, data, allowedEffect, keyState, out handlerFit );

			// Drop!
			if( handlerFit != null )
				handlerFit.Drop( targetResource, data, allowedEffect, keyState );
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			IResourceDragDropHandler handlerFit;
			return GetDragOverHandler( targetResource, data, allowedEffect, keyState, out handlerFit );
		}

		public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
		{
			// Call all the handlers so that they added their data
			foreach( IResourceDragDropHandler handler in _handlers )
				handler.AddResourceDragData( dragResources, dataObject );
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Looks for a handler within the composed ones and the link-handler, if enabled, and picks the first one that
		/// confirms it can handle the situation in response to the dragover event.
		/// That handler is then returned.
		/// Note that the drag-over operation is thus already executed for that handler.
		/// </summary>
		protected DragDropEffects GetDragOverHandler( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState, out IResourceDragDropHandler handlerFit )
		{
			DragDropEffects effect;

			// Try the composed handlers
			foreach( IResourceDragDropHandler handler in _handlers )
			{
				handlerFit = handler;
				if( (effect = handlerFit.DragOver( targetResource, data, allowedEffect, keyState )) != DragDropEffects.None )
					return effect;
			}

			// Try the link-handler
			if( _bAddLink )
			{
				handlerFit = _linkhandler;
				if( (effect = handlerFit.DragOver( targetResource, data, allowedEffect, keyState )) != DragDropEffects.None )
					return effect;
			}

			// None has fit
			handlerFit = null;
			return DragDropEffects.None;
		}

		#endregion
	}

	#endregion
}
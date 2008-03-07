/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Librarian
{
	/// <summary>
	/// Main class of the Librarian plugin.
	/// </summary>
	public class LibrarianPlugin: IPlugin, IResourceDisplayer
	{
        public void Register()
	    {
            PropTypes.Register();
            ResourceTypes.Register();
            
            Core.ResourceStore.RegisterLinkRestriction( ResourceTypes.Book, PropTypes.BookAuthor,
                "Contact", 0, Int32.MaxValue );
            Core.ResourceStore.RegisterUniqueRestriction( ResourceTypes.Book, PropTypes.Isbn );

            Core.ActionManager.RegisterMainMenuAction( new NewBookAction(),
                "FileNewActions", ListAnchor.Last, "Book...", null, null );
            Core.ActionManager.RegisterContextMenuAction( new EditBookAction(),
                ActionGroups.ITEM_OPEN_ACTIONS, ListAnchor.First, "Edit Book", ResourceTypes.Book, null );
            Core.ActionManager.RegisterDoubleClickAction( new EditBookAction(), ResourceTypes.Book, null );
            Core.ActionManager.RegisterActionComponent( new DeleteBookAction(), "Delete", 
                ResourceTypes.Book, null );

            Core.TabManager.RegisterResourceTypeTab( "Books", "Books", ResourceTypes.Book, 20 );

            Core.PluginLoader.RegisterViewsConstructor( new ViewsConstructor() );
            Core.PluginLoader.RegisterResourceDisplayer( ResourceTypes.Book, this );
            Core.PluginLoader.RegisterResourceTextProvider( ResourceTypes.Book, new TextProvider() );
	    }

	    public void Startup()
	    {
	    }

	    public void Shutdown()
	    {
	    }

        IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
        {
            return new BookDisplayPane();
        }

        private class ViewsConstructor: IViewsConstructor
        {
            public void RegisterViewsFirstRun()
            {
                IResource allBooksView = Core.FilterManager.RegisterView( "All Books", 
                    new string[] { ResourceTypes.Book }, new IResource[] {}, new IResource[] {} );
                Core.ResourceTreeManager.LinkToResourceRoot( allBooksView, 0 );
            }

            public void RegisterViewsEachRun()
            {
            }
        }

        private class TextProvider: IResourceTextProvider
        {
            public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
            {
                consumer.AddDocumentFragment( res.Id, res.GetPropText( "Name" ), 
                    DocumentSection.SubjectSection );
                
                foreach( IResource author in res.GetLinksOfType( null, PropTypes.BookAuthor ) )
                {
                    consumer.AddDocumentFragment( res.Id, author.DisplayName, 
                        DocumentSection.SourceSection );
                }

                consumer.AddDocumentFragment( res.Id, res.GetPropText( PropTypes.Isbn ) );
                
                return true;                
            }
        }
    }

    internal class ResourceTypes
    {
        private const string _resBook = "JetBrains.Librarian.Book";

        internal static string Book
        {
            get { return _resBook; }
        }

        internal static void Register()
        {
            Core.ResourceStore.ResourceTypes.Register( _resBook, "Book", "Name" );
        }
    }

    /// <summary>
    /// Identifiers of the properties used in the Librarian plugin.
    /// </summary>
    internal class PropTypes
    {
        private static int _propPubYear;
        private static int _propIsbn;
        private static int _propBookAuthor;

        internal static int BookAuthor
        {
            get { return _propBookAuthor; }
        }

        internal static int PubYear
        {
            get { return _propPubYear; }
        }

        internal static int Isbn
        {
            get { return _propIsbn; }
        }

        internal static void Register()
        {
            _propPubYear = Core.ResourceStore.PropTypes.Register( "JetBrains.Librarian.PubYear", 
                PropDataType.Int );
            Core.ResourceStore.PropTypes.RegisterDisplayName( PropTypes.PubYear, "Pub.Year" );

            _propIsbn = Core.ResourceStore.PropTypes.Register( "JetBrains.Librarian.ISBN",
                PropDataType.String );
            Core.ResourceStore.PropTypes.RegisterDisplayName( _propIsbn, "ISBN" );

            _propBookAuthor = Core.ResourceStore.PropTypes.Register( "JetBrains.Librarian.BookAuthor",
                PropDataType.Link );
            Core.ResourceStore.PropTypes.RegisterDisplayName( _propBookAuthor, "Author" );
        }
    }
}

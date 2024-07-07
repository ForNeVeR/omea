// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Reflection;
using System.Drawing;
using JetBrains.Omea.HTML;
using JetBrains.Omea.Notes;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Notes
{
    [PluginDescription("Notes", "Michael (LloiX) Gerasimov", "Support for short notes in html format. Allows to create and edit notes.<LineBreak/><Hyperlink NavigateUri=\"mailto:light.on.shadow@gmail.com\">light.on.shadow@gmail.com</Hyperlink>", PluginDescriptionFormat.XamlInline, "Icons/NotesPluginIcon.png")]
    public class NotesPlugin : IPlugin, IResourceDisplayer, IResourceTextProvider
    {
        #region IPlugin Members

        public void Register()
        {
            try
            {
                _plugin = this;
                RegisterTypes();
            }
            catch
            {
                Core.ActionManager.DisableXmlActionConfiguration( Assembly.GetExecutingAssembly() );
                return;
            }

            Core.TabManager.RegisterResourceTypeTab( "Notes", "Notes", new string[] { _Note, "Fragment" }, 8 );

            IPluginLoader pluginLoader = Core.PluginLoader;
            pluginLoader.RegisterResourceTextProvider( _Note, this );
            pluginLoader.RegisterResourceDisplayer( _Note, this );
            pluginLoader.RegisterViewsConstructor( new NotesViewsConstructor() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries: for restricting the resource
            //  type to Notes (two synonyms).
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "notes", "Note" );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "note", "Note" );

            NotesIconProvider iconProvider = new NotesIconProvider();
            Core.ResourceIconManager.RegisterResourceIconProvider( _Note, iconProvider );

            pluginLoader.RegisterResourceDeleter( _Note, new NoteDeleter() );
            Core.ResourceBrowser.SetDefaultViewSettings( "Notes", AutoPreviewMode.AllItems, true );
        }

        public void Startup()  {}
        public void Shutdown() {}
        #endregion

        #region IResourceTextProvider Members
        bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if( res != null )
            {
                int id = res.Id;
                if( res.Type == _Note )
                {
                    string longBody = res.GetPropText( Core.Props.LongBody );
                    StringReader reader = new StringReader( longBody );
                    using (HTMLParser parser = new HTMLParser( reader, true ) )
                    {
                        while( !parser.Finished )
                        {
                            string fragment = parser.ReadNextFragment();
                            if (fragment.Length > 0)
                            {
                                if (parser.InHeading)
                                {
                                    consumer.AddDocumentHeading(res.Id, fragment);
                                }
                                else
                                {
                                    consumer.AddDocumentFragment(res.Id, fragment);
                                }
                            }
                        }
                    }
                    consumer.RestartOffsetCounting();
                    consumer.AddDocumentHeading( id, res.GetPropText( Core.Props.Subject ) );
                }
            }
            return true;
        }
        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resType )
        {
            if( resType == _Note )
            {
                if( _previewPane == null )
                {
                    _previewPane = new NotePreviewPane();
                }
                return _previewPane;
            }
            return null;
        }

        #endregion

        #region implementation details
        private void RegisterTypes()
        {
            IResourceStore store = Core.ResourceStore;
            IResourceTypeCollection resTypes = store.ResourceTypes;
            resTypes.Register( _Note, "Note", "Subject", ResourceTypeFlags.Normal, this );

            IPropTypeCollection propTypes = store.PropTypes;
            _propLastUpdated = propTypes.Register( "LastUpdated", PropDataType.Date, PropTypeFlags.Internal );
        }

        internal static Icon LoadIcon( string iconName )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "NotesPlugin.Icons." + iconName );
            if( stream != null )
            {
                return new Icon( stream );
            }
            return null;
        }

        #region Icon Providers
        /**
         * icon provider for newsgroup resources
         */
        private class NotesIconProvider: IResourceIconProvider
        {
            private Icon _NoteIcon;

            public NotesIconProvider()
            {
                _NoteIcon = LoadIcon( "NoteSmall.ico" );
            }

            public Icon GetResourceIcon( IResource resource )
            {
                return (resource.Type == _Note) ? _NoteIcon : null;
            }

            public Icon GetDefaultIcon( string resType )
            {
                return (resType == _Note) ? _NoteIcon : null;
            }
        }
        #endregion Icon Providers

        private class NoteDeleter: DefaultResourceDeleter
        {
            public override bool CanIgnoreRecyclebin()
            {
                return true;
            }

            public override void DeleteResourcePermanent( IResource note )
            {
                note.Delete();
            }
        }

        internal const string               _Note = "Note";
        internal static NotesPlugin         _plugin;
        internal static int                 _propLastUpdated;
        internal static NotePreviewPane		_previewPane;
        #endregion
    }

    public class NotesViewsConstructor : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            IResource view = Core.FilterRegistry.RegisterView( "All Notes", new string[] { NotesPlugin._Note, "Fragment" }, (IResource[])null, null );
            Core.ResourceTreeManager.LinkToResourceRoot( view, 8 );
            IResourceList allNotes = Core.ResourceStore.GetAllResources( NotesPlugin._Note );
            foreach( IResource note in allNotes )
                note.SetProp( Core.Props.LongBodyIsHTML, true );
        }
        public void RegisterViewsEachRun()
        {}
    }
}

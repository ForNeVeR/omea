// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using System35;

using DBIndex;

using JetBrains.Annotations;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;
using JetBrains.Omea.Categories;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;
using Microsoft.Win32;

namespace JetBrains.Omea
{
    /// <summary>
    /// IUIManager implementation.
    /// </summary>
    internal class UIManager: IUIManager
    {
        internal class OptionsGroupDescriptor
        {
            public string _prompt;
            public HashMap _optionsPanes = new HashMap();
            public NameValueCollection _panePrompts = new NameValueCollection();
            public HashMap _optionsListeners = new HashMap();

            public string Prompt
            {
                get
                {
                    string result = _prompt ?? string.Empty;
                    string[] headers = _panePrompts.AllKeys;
                    Array.Sort( headers );
                    foreach( string header in headers )
                    {
                        if( result.Length > 0 )
                        {
                            result += "\r\n\r\n";
                        }
                        result += _panePrompts[ header ];
                    }
                    return result.Replace( "[product name]", Core.ProductName );
                }
            }
        }

        internal class LocationLinkData
        {
            internal string ResType;
            internal int PropId;

            internal LocationLinkData( string resType, int propId )
            {
                ResType = resType;
                PropId  = propId;
            }
        }

        private readonly HashMap _optionsGroups = new HashMap();

        private readonly Hashtable _resourceLocationLinks = new Hashtable();
        private readonly Hashtable _resourceDefaultLocations = new Hashtable();
        private readonly HashSet _locationResTypes = new HashSet();

        private readonly IntHashTable _editWindows = new IntHashTable();         // resource ID -> ResourceEditWindow
        private readonly Hashtable _resourceSelectPanes = new Hashtable();       // resource type -> typeof(IResourceSelectPane)
        private readonly Hashtable _displayInContextHandlers = new Hashtable();  // resource type -> IDisplayInContextHandler

        private int _sidebarUpdateCount;

        private MainFrame _mainFrame;
        private readonly Icon     _appIcon;

        public event EventHandler EnterIdle;
        public event EventHandler ExitMenuLoop;
        public event CancelEventHandler MainWindowClosing;

        private BalloonForm _balloonForm;

        private Font            _formattingFont;
        private const string    _cDefaultFont = "Verdana";
        private const int       _cDefaultFontSize = 10;

		/// <summary>
		/// The DDE client object.
		/// </summary>
		protected Dde _dde = null;

        public UIManager( Icon appIcon )
        {
            _appIcon = appIcon;
        }

        public Icon ApplicationIcon
        {
            get { return _appIcon; }
        }

		/// <summary>
		/// Registers a group of options panes with accompanying text
		/// </summary>
        public void RegisterOptionsGroup( string group, string prompt )
        {
            lock( _optionsGroups )
            {
                OptionsGroupDescriptor groupDescriptor;
                HashMap.Entry entry = _optionsGroups.GetEntry( group );
                if( entry != null )
                {
                    groupDescriptor = (OptionsGroupDescriptor) entry.Value;
                }
                else
                {
                    groupDescriptor = new OptionsGroupDescriptor();
                    _optionsGroups[ group ] = groupDescriptor;
                }
                if( groupDescriptor._prompt == null || groupDescriptor._prompt.Length > 0 )
                {
                    groupDescriptor._prompt = prompt;
                }
            }
        }

        public bool IsOptionsGroupRegistered( string group )
        {
            lock( _optionsGroups )
            {
                return _optionsGroups.GetEntry( group ) != null;
            }
        }

        /**
         * Registers an options pane that will be shown in the options dialog.
         */

        public void RegisterOptionsPane( string group, string header, OptionsPaneCreator creator, string prompt )
        {
            lock( _optionsGroups )
            {
                HashMap.Entry entry = _optionsGroups.GetEntry( group );
                if( entry != null )
                {
                    OptionsGroupDescriptor groupDescriptor = (OptionsGroupDescriptor) entry.Value;
                    groupDescriptor._optionsPanes[ header ] = creator;
                    if( prompt != null )
                    {
                        groupDescriptor._panePrompts[ header ] = prompt;
                    }
                }
                else
                {
                    throw new ArgumentException( "Options group " + group + " is not registered", "group" );
                }
            }
        }

        /**
         * for specified group & header, adds listener for options changes
         */

        public void AddOptionsChangesListener( string group, string header, EventHandler handler )
        {
            lock( _optionsGroups )
            {
                HashMap.Entry entry = _optionsGroups.GetEntry( group );
                if( entry != null )
                {
                    OptionsGroupDescriptor groupDescriptor = (OptionsGroupDescriptor) entry.Value;
                    ArrayList handlers;
                    HashMap.Entry E = groupDescriptor._optionsListeners.GetEntry( header );
                    if( E != null )
                    {
                        handlers = (ArrayList) E.Value;
                    }
                    else
                    {
                        handlers = new ArrayList( 1 );
                        groupDescriptor._optionsListeners[ header ] = handlers;
                    }
                    handlers.Add( handler );
                }
                else
                {
                    throw new ArgumentException( "Options group " + group + " is not registered", "group" );
                }
            }
        }

        public void RemoveOptionsChangesListener( string group, string header, EventHandler handler )
        {
            lock( _optionsGroups )
            {
                HashMap.Entry entry = _optionsGroups.GetEntry( group );
                if( entry != null )
                {
                    OptionsGroupDescriptor groupDescriptor = (OptionsGroupDescriptor) entry.Value;
                    ArrayList handlers;
                    HashMap.Entry E = groupDescriptor._optionsListeners.GetEntry( header );
                    if( E != null )
                    {
                        handlers = (ArrayList) E.Value;
                        handlers.Remove( handler );
                        if( handlers.Count == 0 )
                        {
                            groupDescriptor._optionsListeners.Remove( header );
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException( "Options group " + group + " is not registered" );
                }
            }
        }

        public void RegisterWizardPane( string header, OptionsPaneCreator creator, int order )
        {
            StartupWizard.RegisterWizardPane( header, creator, order );
        }

        public void DeRegisterWizardPane( string header )
        {
            StartupWizard.DeRegisterWizardPane( header );
        }

        public void ShowOptionsDialog()
        {
            using( OptionsForm optionsForm = new OptionsForm() )
            {
                optionsForm.EditOptions( _optionsGroups, Core.MainWindow );
            }
        }

        public void ShowOptionsDialog( string group, string paneName )
        {
            using( OptionsForm optionsForm = new OptionsForm() )
            {
                optionsForm.EditOptions( group, paneName, _optionsGroups, Core.MainWindow );
            }
        }

        internal static DialogResult RunWizard( bool forceWizard )
        {
            return StartupWizard.RunWizard( forceWizard );
        }

        #region IndicatorLights class def

        private class IndicatorLight : IDisposable
        {
            public IndicatorLight( string name, MainFrame frame, IAsyncProcessor processor, int timeout )
            {
                _name = name;
                _frame = frame;
                _processor = processor;
                processor.JobStarting += processor_JobStarting;
                processor.JobFinished += processor_JobFinished;
                _timeout = timeout;
                _lastStartedTicks = 0;
                _lastUpdatedTicks = -1;
                _stackDepth = 0;
                _box = new ImageListPictureBox();
                _box.ImageList = frame._statesImageList;
                _box.Dock = DockStyle.Right;
                _box.ImageLeftTopPoint = new Point( 2, 2 );
                Panel indicatorsPanel = frame._indicatorsPanel;
                _box.Width = indicatorsPanel.Height + 2;
                indicatorsPanel.Width += _box.Width;
                indicatorsPanel.Left -= _box.Width;
                indicatorsPanel.Controls.Add( _box );
                frame._toolTip.SetToolTip( _box, name + " is idle" );
                _toolTipBuilder = new StringBuilder();
            }
            public IndicatorLight( string name, MainFrame frame, IAsyncProcessor processor, int timeout, params Icon[] icons )
                : this( name, frame, processor, timeout )
            {
                ImageList list = new ImageList();
                list.ColorDepth = ColorDepth.Depth32Bit;
                foreach( Icon icon in icons )
                {
                    list.Images.Add( icon );
                }
                _box.ImageList = list;
            }

            public void Dispose()
            {
                _processor.JobStarting -= processor_JobStarting;
                _processor.JobFinished -= processor_JobFinished;
                _frame._indicatorsPanel.Width -= _box.Width;
                _frame._indicatorsPanel.Controls.Remove( _box );
                _box.Dispose();
            }

            public void UpdateLight()
            {
                /**
                 * if nothing changed and we are idle or already stuck don't invalidate the light
                 */
                if( _lastUpdatedTicks == _lastStartedTicks && _box.ImageIndex != 1 )
                {
                    return;
                }
                _toolTipBuilder.Length = 0;
                _toolTipBuilder.Append( _name );
                _toolTipBuilder.Append( " is " );
                if( _lastStartedTicks  == 0 )
                {
                    _box.ImageIndex = 0;
                    _toolTipBuilder.Append( "idle" );
                }
                else
                {
                    string currentJobName = _processor.CurrentJobName;
                    if( ( DateTime.Now.Ticks - _lastStartedTicks ) / 10000000 < _timeout )
                    {
                        _box.ImageIndex = 1;
                        _toolTipBuilder.Append( "busy." );
                    }
                    else
                    {
                        _toolTipBuilder.Append( "stuck since " );
                        _toolTipBuilder.Append( new DateTime( _lastStartedTicks ).ToLongTimeString() );
                        _toolTipBuilder.Append( '.' );
                        _box.ImageIndex = 2;
                    }
                    _toolTipBuilder.Append( "\r\nLast operation: " );
                    _toolTipBuilder.Append( ( currentJobName.Length > 0 ) ? currentJobName : "Indefinite" );
                }
                _frame._toolTip.SetToolTip( _box, _toolTipBuilder.ToString() );
                _box.Invalidate();
                _lastUpdatedTicks = _lastStartedTicks;
            }

            private void processor_JobStarting( object sender, EventArgs e )
            {
                _lastStartedTicks = DateTime.Now.Ticks;
                ++_stackDepth;
            }

            private void processor_JobFinished( object sender, EventArgs e )
            {
                if( --_stackDepth == 0 )
                {
                    _lastStartedTicks = 0;
                }
            }

            private readonly string              _name;
            private readonly MainFrame           _frame;
            private readonly IAsyncProcessor     _processor;
            private readonly int                 _timeout;
            private readonly ImageListPictureBox _box;
            private readonly StringBuilder       _toolTipBuilder;
            private long _lastStartedTicks;
            private long _lastUpdatedTicks;
            private int _stackDepth;
        }

        #endregion

        #region IndicatorLights Usage
        private HashMap _indicatorLights = new HashMap();

        void IUIManager.RegisterIndicatorLight( string name, IAsyncProcessor processor, int stuckTimeout )
        {
            _indicatorLights[ name ] = new IndicatorLight( name, MainFrame, processor, stuckTimeout );
        }
        void IUIManager.RegisterIndicatorLight( string name, IAsyncProcessor processor, int stuckTimeout, params Icon[] icons )
        {
            _indicatorLights[ name ] = new IndicatorLight( name, MainFrame, processor, stuckTimeout, icons );
        }
        void IUIManager.DeRegisterIndicatorLight( string name )
        {
            IndicatorLight iLight = (IndicatorLight) _indicatorLights[ name ];
            if( iLight != null )
            {
                _indicatorLights.Remove( name );
                iLight.Dispose();
            }
        }

        private int tickCounter = 0;
        private Label _memUsageLabel;

        internal Label MemUsageLabel
        {
            get { return _memUsageLabel; }
            set { _memUsageLabel = value; }
        }

        internal void UpdateLights()
        {
            foreach( HashMap.Entry entry in _indicatorLights )
            {
                IndicatorLight iLight = (IndicatorLight) entry.Value;
                iLight.UpdateLight();
            }
            if( _memUsageLabel != null && ( ++tickCounter & 3 ) == 0 )
            {
                long memUsage = GC.GetTotalMemory( false ) / ( 1024 * 100 );
                memUsage += OmniaMeaBTree.GetUsedMemory() / ( 1024 * 100 );
                _memUsageLabel.Text = "Mem Usage: " + memUsage / 10 + "." + memUsage % 10 + "M";
            }
        }
        #endregion IndicatorLights Usage

        #region Select Resource
        /**
         * Shows a modal dialog allowing to select a resource of a specific
         * type, and returns the selected resource or null if the dialog
         * was cancelled.
         */

        IResource IUIManager.SelectResource( string type, string caption )
        {
            return new ResourceSelector().SelectResource(Core.MainWindow, type, caption, null, null);
        }

        public IResource SelectResource( IWin32Window ownerWnd, string type, string caption )
        {
            return new ResourceSelector().SelectResource( ownerWnd, type, caption, null, null );
        }

        IResource IUIManager.SelectResource( string type, string caption, IResource initial )
        {
            return new ResourceSelector().SelectResource( Core.MainWindow, type, caption, initial, null );
        }

        public IResource SelectResource( IWin32Window ownerWnd, string type, string caption,
                                         IResource initialSelection )
        {
            return new ResourceSelector().SelectResource( ownerWnd, type, caption, initialSelection, null );
        }

        public IResource SelectResource( IWin32Window ownerWnd, string type, string caption,
                                         IResource initialSelection, string helpTopic )
        {
            return new ResourceSelector().SelectResource( ownerWnd, type, caption, initialSelection, helpTopic );
        }
        #endregion Select Resource

        #region Select Resources
        /**
         * Shows a modal dialog allowing to select multiple resources of a specific type.
         * Returns null if the dialog was cancelled.
         */

        public IResourceList SelectResources( string type, string caption )
        {
            return new ResourceSelector().SelectResources( Core.MainWindow, new[] { type }, caption, null, null );
        }

        public IResourceList SelectResources( IWin32Window ownerWnd, string type, string caption )
        {
            return new ResourceSelector().SelectResources( ownerWnd, new[] { type }, caption, null, null );
        }

        /**
         * Shows a modal dialog allowing to select multiple resources of a specific type,
         * and checks the specified resources initially.
         * Returns null if the dialog was cancelled.
         */

        public IResourceList SelectResources( string type, string caption, IResourceList initialSelection )
        {
            return new ResourceSelector().SelectResources( Core.MainWindow, new[] { type }, caption, initialSelection, null );
        }

        public IResourceList SelectResources( IWin32Window ownerWnd, string type, string caption,
                                              IResourceList initialSelection )
        {
            return new ResourceSelector().SelectResources( ownerWnd, new[] { type }, caption, initialSelection, null );
        }

        public IResourceList SelectResources( IWin32Window ownerWnd, string type, string caption,
                                              IResourceList initialSelection, string helpTopic )
        {
            return new ResourceSelector().SelectResources( ownerWnd, new[] { type }, caption, initialSelection, helpTopic );
        }

        /**
         * Shows a modal dialog allowing to select multiple resources of any of the specified types
         * and checks the specified resources initially. The type of the selector pane shown is determined
         * by the first type in the given list.
         * Returns null if the dialog was cancelled.
         */

        public IResourceList SelectResources( string[] types, string caption, IResourceList initialSelection )
        {
            return new ResourceSelector().SelectResources( Core.MainWindow, types, caption, initialSelection, null );
        }

        public IResourceList SelectResources( IWin32Window ownerWnd, string[] types,
                                              string caption, IResourceList initialSelection )
        {
            return new ResourceSelector().SelectResources( ownerWnd, types, caption, initialSelection, null );
        }

        public IResourceList SelectResources( IWin32Window ownerWnd, string[] types, string caption,
                                              IResourceList initialSelection, string helpTopic )
        {
            return new ResourceSelector().SelectResources( ownerWnd, types, caption, initialSelection, helpTopic );
        }
        #endregion Select Resources

        #region Select Resources From List
        /**
         * Shows a modal dialog allowing to select multiple resources from the specified list.
         * Returns null if the dialog was cancelled.
         */

        public IResourceList SelectResourcesFromList( IResourceList fromList, string caption )
        {
            return new ResourceSelector().SelectResourcesFromList(Core.MainWindow, fromList, caption, null, null);
        }

        public IResourceList SelectResourcesFromList( IWin32Window ownerWnd, IResourceList resList, string caption )
        {
            return new ResourceSelector().SelectResourcesFromList( ownerWnd, resList, caption, null, null );
        }

        public IResourceList SelectResourcesFromList( IWin32Window ownerWnd, IResourceList resList,
                                                      string caption, IResourceList initialSelection )
        {
            return new ResourceSelector().SelectResourcesFromList( ownerWnd, resList, caption, null, initialSelection );
        }

        public IResourceList SelectResourcesFromList( IWin32Window ownerWnd, IResourceList resList,
                                                      string caption, string helpTopic )
        {
            return new ResourceSelector().SelectResourcesFromList( ownerWnd, resList, caption, helpTopic, null );
        }
        #endregion Select Resources From List

        #region Show AddLink Dialog
        /**
         * Shows a dialog for adding a custom link between two resources.
         */

        public void ShowAddLinkDialog(IWin32Window ownerWindow, IResource res1, IResource res2)
        {
            using (AddLinkDlg dlg = new AddLinkDlg())
            {
                dlg.ShowAddLinkDialog(ownerWindow, res1.ToResourceList(), res2);
            }
        }

        public void ShowAddLinkDialog(IWin32Window ownerWindow, IResourceList sourceList, IResource target)
        {
            using (AddLinkDlg dlg = new AddLinkDlg())
            {
                dlg.ShowAddLinkDialog(ownerWindow, sourceList, target);
            }
        }

        public void ShowAddLinkDialog(IResource res1, IResource res2)
        {
            ShowAddLinkDialog(Core.MainWindow, res1, res2);
        }

        public void ShowAddLinkDialog(IResourceList sourceList, IResource target)
        {
            ShowAddLinkDialog(Core.MainWindow, sourceList, target);
        }
        #endregion Show AddLink Dialog

        #region Show Message Dialog
        private delegate void ShowMessageBoxDelegate( string header, string message );

        public void ShowSimpleMessageBox( string header, string message )
        {
            Core.UserInterfaceAP.QueueJob( new ShowMessageBoxDelegate( ShowMessageBoxImpl ), header, message );
        }
        private static void ShowMessageBoxImpl( string header, string message )
        {
            MessageBox.Show( Core.MainWindow, message, header, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
        }
        #endregion Show Message Dialog

        #region Show New/Assign Category Dialog
        /**
         * Shows the dialog for creating a new category.
         */

        public IResource ShowNewCategoryDialog( string defaultName, IResource defaultParent, string defaultContentType )
        {
            using( NewCategoryDlg dlg = new NewCategoryDlg() )
            {
                return dlg.ShowNewCategoryDialog( Core.MainWindow, defaultName, defaultParent, defaultContentType );
            }
        }

        public IResource ShowNewCategoryDialog( IWin32Window ownerWindow, string defaultName, IResource defaultParent, string defaultContentType )
        {
            using( NewCategoryDlg dlg = new NewCategoryDlg() )
            {
                return dlg.ShowNewCategoryDialog( ownerWindow, defaultName, defaultParent, defaultContentType );
            }
        }

        public DialogResult ShowAssignCategoriesDialog( IWin32Window ownerWindow, IResourceList resources )
        {
            using( CategoryEditorWithAssignment dlg = new CategoryEditorWithAssignment() )
            {
                return dlg.EditCategories( ownerWindow, resources );
            }
        }

        public DialogResult ShowAssignCategoriesDialog( IWin32Window ownerWindow, IResourceList resources,
                                                        IResourceList currCategories, out IResourceList resultCategories )
        {
            using( CategoryEditorOnList dlg = new CategoryEditorOnList() )
            {
                return dlg.EditCategories( ownerWindow, resources, currCategories, out resultCategories );
            }
        }
        #endregion Show New/Assign Category Dialog

        public void RegisterDisplayInContextHandler( string resType, IDisplayInContextHandler handler )
        {
            Guard.ValidResourceType( resType, "resType" );
            _displayInContextHandlers [resType] = handler;
        }

        /**
         * Registers the link type which connects a resource to its location (an Email to a
         * MAPIFolder, an Article to a Newsgroup, and so on).
         */

        public void RegisterResourceLocationLink( string resType, int propId, string locationResType )
        {
            if ( propId != 0 )
            {
                _resourceLocationLinks [resType] = new LocationLinkData( locationResType, propId );
            }

            _locationResTypes.Add( locationResType );
        }

        public void RegisterResourceDefaultLocation( string resType, IResource location )
        {
            _resourceDefaultLocations [resType] = location;
        }

        /**
         * Displays the specified resource in appropriate context (switches to
         * the tab showing resources of that type, opens the resource structure
         * pane if it's not opened, selects there the resource which contains the
         * specified resource, and highlights the resource in the appropriate resource
         * list).
         */

        public void DisplayResourceInContext( IResource res )
        {
            DisplayResourceInContext( res, false );
        }

        public void DisplayResourceInContext( IResource res, bool skipCurrentList )
        {
            // first, check if the resource is already visible in the list
            if ( !skipCurrentList && Core.ResourceBrowser.SelectResource( res ) )
                return;

            IDisplayInContextHandler handler = (IDisplayInContextHandler) _displayInContextHandlers [res.Type];
            if ( handler != null )
            {
                handler.DisplayResourceInContext( res );
                return;
            }

            IResource location = GetLocationForResource( res );
            if ( location == null )
            {
                location = GetLocationFromSource( res );
            }

            bool locationSelected = false, contextFound = false;
            if ( location != null )
            {
                if ( location == Core.ResourceBrowser.OwnerResource && !Core.ResourceBrowser.WebPageMode )
                {
                    if ( location != res )
                    {
                        contextFound = Core.ResourceBrowser.SelectResource( res );
                    }
                    else
                    {
                        contextFound = true;
                    }
                }
                else
                {
                    string tabId = Core.TabManager.GetResourceTab( location );
                    if ( tabId == null && _resourceDefaultLocations.Contains( res.Type ) )
                    {
                        tabId = Core.TabManager.GetResourceTab( res );
                    }
                    if ( tabId != null )
                    {
                        string viewPaneId = Core.LeftSidebar.GetResourceStructurePaneId( tabId );
                        if ( viewPaneId == null )
                        {
                            viewPaneId = StandardViewPanes.ViewsCategories;
                        }

                        BeginUpdateSidebar();
                        if ( !Core.TabManager.ActivateTab( tabId ) )
                        {
                            EndUpdateSidebar();
                            return;
                        }
                        AbstractViewPane viewPane = Core.LeftSidebar.ActivateViewPane( tabId, viewPaneId );
                        EndUpdateSidebar();

                        if ( viewPane == null )
                        {
                            // ActivateViewPane() may have caused event processing and change of active tab (OM-8213)
                            return;
                        }
                        if ( viewPane.SelectResource( res, false ) || viewPane.SelectResource( location, false ) )
                        {
                            locationSelected = true;
                            viewPane.Select();
                            if ( location != res )
                            {
                                contextFound = Core.ResourceBrowser.SelectResource( res );
                            }
                        }
                    }
                }
            }
            if ( !contextFound && ( Core.PluginLoader.GetResourceDisplayer( res.Type ) != null || !locationSelected ) )
            {
                if ( locationSelected )
                {
                    (Core.ResourceBrowser as ResourceBrowser).BrowseStack.DiscardTop();
                }
                Core.ResourceBrowser.DisplayResource( res );
            }
        }

        private IResource GetLocationFromSource( IResource res )
        {
            IResource location = null;
            int[] linkTypeIds = res.GetLinkTypeIds();
            foreach( int linkTypeId in linkTypeIds )
            {
                if ( Core.ResourceStore.PropTypes [linkTypeId].HasFlag( PropTypeFlags.SourceLink ) )
                {
                    IResource source = res.GetLinkProp( linkTypeId );
                    if ( source != null )
                    {
                        location = GetLocationForResource( source );
                        if ( location != null )
                        {
                            break;
                        }
                    }
                }
            }
            return location;
        }

        private IResource GetLocationForResource( IResource res )
        {
            IResource location = null;
            if ( _resourceLocationLinks.ContainsKey( res.Type ) )
            {
                LocationLinkData linkData = (LocationLinkData) _resourceLocationLinks [res.Type];
                IResourceList locationList;
                if ( Core.ResourceStore.PropTypes [linkData.PropId].HasFlag( PropTypeFlags.DirectedLink ) )
                {
                    locationList = linkData.PropId < 0 ? res.GetLinksTo( linkData.ResType, -linkData.PropId ) :
                                                         res.GetLinksFrom( linkData.ResType, linkData.PropId );
                }
                else
                {
                    locationList = res.GetLinksOfType( linkData.ResType, linkData.PropId );
                }

                if ( locationList.Count > 0 )
                {
                    location = locationList [0];
                }
            }
            else if ( _locationResTypes.Contains( res.Type ) )
            {
                location = res;
            }
            else if ( _resourceDefaultLocations.Contains( res.Type ) )
            {
                location = (IResource) _resourceDefaultLocations [res.Type];
            }
            return location;
        }

        public IResourceList GetResourcesInLocation( IResource location )
        {
            IResourceList result = null;
            foreach( DictionaryEntry de in _resourceLocationLinks )
            {
                string resType = (string) de.Key;
                LocationLinkData locationData = (LocationLinkData) de.Value;
                if ( locationData.ResType == location.Type )
                {
                    int propId = locationData.PropId;
                    IResourceList linkResult;
                    if ( Core.ResourceStore.PropTypes [propId].HasFlag( PropTypeFlags.DirectedLink ) )
                    {
                        // reverse direction of resource to location link
                        linkResult = propId < 0 ? location.GetLinksFrom( resType, -propId ) :
                                                  location.GetLinksTo( resType, propId );
                    }
                    else
                    {
                        linkResult = location.GetLinksOfType( resType, propId );
                    }
                    result = linkResult.Union( result );
                }
            }
            if ( result == null )
            {
                return Core.ResourceStore.EmptyResourceList;
            }
            return result;
        }

        /**
         * Checks if the specified list of target resources can be dropped on the
         * source resource.
         */

        public bool CanDropResource( IResource targetRes, IResourceList dropList )
        {
            return CanDropResource( targetRes, dropList, true );
        }

        private static bool CanDropResource( IResource targetRes, IResourceList dropList, bool sameView )
        {
            if ( dropList.Count == 0 || dropList [0].Id == targetRes.Id )
                return false;

            if ( sameView )
            {
                IResourceUIHandler uiHandler = Core.PluginLoader.GetResourceUIHandler( targetRes );
                if ( uiHandler != null )
                {
                    return uiHandler.CanDropResources( targetRes, dropList );
                }
            }

            return true;
        }

        public DragDropEffects ProcessDragOver( IResource targetRes, IDataObject data, DragDropEffects effect,
            int state, bool sameView )
        {
            IResourceDragDropHandler ddHandler = Core.PluginLoader.GetResourceDragDropHandler( targetRes );
            if ( ddHandler != null )
            {
                return ddHandler.DragOver( targetRes, data, effect, state );
            }

            IResourceList resList = (IResourceList) data.GetData( typeof(IResourceList) );
            if ( resList != null && CanDropResource( targetRes, resList, sameView ) )
            {
                return DragDropEffects.Link;
            }

            return DragDropEffects.None;
        }

        /**
         * Performs the action for dropping a list of resources on a resource.
         */

        public void ProcessResourceDrop( IResource targetRes, IResourceList dropList )
        {
            if ( dropList.Count == 0 )
                return;

            if ( dropList [0].Id != targetRes.Id )
            {
                IResourceUIHandler uiHandler = Core.PluginLoader.GetResourceUIHandler( targetRes );
                if ( uiHandler != null )
                {
                    if ( uiHandler.CanDropResources( targetRes, dropList ) )
                    {
                        uiHandler.ResourcesDropped( targetRes, dropList );
                    }
                    else
                    {
                        ShowAddLinkDialog( dropList, targetRes );
                    }
                }
                else if (dropList [0].Type == "Category" )
                {
                    Core.CategoryManager.AddResourceCategory( targetRes, dropList [0] );
                }
                else
                {
                    ShowAddLinkDialog( dropList, targetRes );
                }
            }
        }

        public void ProcessDragDrop( IResource targetRes, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            IResourceDragDropHandler ddHandler = Core.PluginLoader.GetResourceDragDropHandler( targetRes );
            if ( ddHandler != null )
            {
                ddHandler.Drop( targetRes, data, allowedEffect, keyState );
            }
            else
            {
                IResourceList dropList = (IResourceList) data.GetData( typeof(IResourceList) );
                if ( dropList != null && dropList.Count > 0 && dropList [0] != targetRes )
                {
                    ProcessResourceDrop( targetRes, dropList );
                }
            }
        }

        /**
         * Opens a window for editing the specified resource.
         * @param newResource If true, canceling the editing window deletes the resource
         */

        public void OpenResourceEditWindow( AbstractEditPane editPane, IResource res, bool newResource )
        {
            OpenResourceEditWindow( editPane, res, newResource, null, null );
        }

        public void OpenResourceEditWindow( AbstractEditPane editPane, IResource res, bool newResource,
                                            EditedResourceSavedDelegate savedDelegate, object savedDelegateTag )
        {
            if ( res == null )
            {
                throw new ArgumentNullException( "res" );
            }

            ResourceEditWindow oldWindow = (ResourceEditWindow) _editWindows [res.Id];
            if ( oldWindow != null )
            {
                oldWindow.BringToFront();
                return;
            }

            ResourceEditWindow wnd = new ResourceEditWindow();
            wnd.SetEditPane( editPane, res, newResource, savedDelegate, savedDelegateTag );
            wnd.Closed += OnEditWindowClosed;
            wnd.StartPosition = FormStartPosition.CenterParent;
            _editWindows [res.Id] = wnd;

            wnd.Show();
            wnd.Activate();
        }

        /**
         * When a resource edit window is closed, removes it from the _editWindows hashtable.
         */

        private void OnEditWindowClosed( object sender, EventArgs e )
        {
            ResourceEditWindow wnd = (ResourceEditWindow) sender;
            _editWindows.Remove( wnd.Resource.Id );
        }

        public void RegisterResourceSelectPane( string resType, Type resourceSelectPaneType )
        {
            _resourceSelectPanes [resType] = resourceSelectPaneType;
        }

        public Type GetResourceSelectPaneType( string resType )
        {
            return (Type) _resourceSelectPanes [resType];
        }

        public IResourceSelectPane CreateResourceSelectPane( string resType )
        {
            if ( resType != null )
            {
                Type paneType = (Type) _resourceSelectPanes [resType];
                if ( paneType != null )
                {
                    return (IResourceSelectPane) paneType.InvokeMember(null,
                        BindingFlags.DeclaredOnly |  BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.CreateInstance, null, null, new object[] {} );
                }
            }
            return new GenericResourceSelectPane();
        }

        public void BeginUpdateSidebar()
        {
            _sidebarUpdateCount++;
        }

        public void EndUpdateSidebar()
        {
            _sidebarUpdateCount--;
        }

        public bool IsSidebarUpdating()
        {
            return _sidebarUpdateCount > 0;
        }

        public void CreateShortcutToResource( IResource res )
        {
            ShortcutBar.GetInstance().AddShortcutToResource( res );
        }

        public string HelpFileName
        {
            get
            {
#if READER
                return Path.Combine( Application.StartupPath, "OmeaReaderHelp.chm" );
#else
                return Path.Combine( Application.StartupPath, "Help.chm" );
#endif
            }
        }

    	#region OpenInNewBrowserWindow + Its Satellites

		/// <summary>
		/// Opens the given URI in a new window of an external browser.
		/// </summary>
		/// <param name="uri">URI of the document to open in the new window. This may be a Web page address, a file pathname, etc.</param>
		/// <remarks>This function calls the extended version, <see cref="OpenInNewBrowserWindow(string, bool)"/>, and uses the Omea settings related to DDE use to either allow or prohibit the use of DDE in this method call.</remarks>
		public void OpenInNewBrowserWindow(string uri)
		{
			OpenInNewBrowserWindow( uri, Core.SettingStore.ReadBool( "General", "AllowBrowserDde", true ) );
		}

    	/// <summary>
    	/// Opens the given URI in a new window of an external browser.
    	/// </summary>
    	/// <param name="uri">URI of the document to open in the new window. This may be a Web page address, a file pathname, etc.</param>
    	/// <param name="bUseDde"><c>True</c> to force the use of DDE, <c>False</c> to prohibit the use of DDE.</param>
    	/// <remarks>To allow the use of DDE, but take the corresponding Omea setting into account, use the parameter-less function (<see cref="OpenInNewBrowserWindow(string)"/>).</remarks>
    	public void OpenInNewBrowserWindow( string uri, bool bUseDde )
    	{
    		if( uri == null )
    			throw new ArgumentNullException( "uri", "The URI cannot be Null." );
    		if( uri == "" )
    			return;

			// Marshal to another thread
			if(!Core.NetworkAP.IsOwnerThread)
			{
				Core.NetworkAP.QueueJob( "Open " + uri + " in a new browser window.", new OpenInNewBrowserWindowDelegate(OpenInNewBrowserWindow), uri, bUseDde );
				return;
			}

			///////////////////////
			// Check the Protocol

			// Try to determine the protocol scheme
			string sProto = Uri.UriSchemeHttp;
			try
			{
				Uri uriObject = new Uri(uri);
				if((uriObject.Scheme != null) && (uriObject.Scheme != ""))
					sProto = uriObject.Scheme;
			}
			catch(Exception ex)
			{
				Trace.WriteLine(String.Format("The \"{0}\" URI could not be parsed by the .NET Uri object. {1}", uri, ex.Message), "[UIM]");
			}

			// Rule out whether to use DDE with this type of a protocol
			if((sProto == Uri.UriSchemeMailto) || (sProto == Uri.UriSchemeNews) || (sProto == Uri.UriSchemeNntp))
				bUseDde = false;	// Do not use DDE (and do not open in the browser) for the uris that do not provide content, but trigger an action instead

    		/////////////////
    		// DDE Approach

			if(bUseDde)
			{
				try
				{
					////
					// Try to obtain the DDE parameters

					string sDdeServerName = Dde.InternetExplorer.Service; // Service name of the DDE server to be requested, IExplore is the default (due to the fact this is the most probable case; also, some other browsers reply to IExplore as well as to their own strings)
					string sDdeTopic = Dde.InternetExplorer.TopicOpenUrl; // DDE topic on which a conversation should be established with the DDE server, the default is WWW_OpenURL that has a parameter for opening a link in a new window
					string sDdeCommand = Dde.InternetExplorer.TopicOpenUrl; // Command that should be send during the conversation

					// First, try getting a DDE handler for the URI's protocol
					// If the URI's protocol has fails to provide the information, check for the HTTP protocol scheme
					if(!ReadDdeSettings(sProto, ref sDdeServerName, ref sDdeTopic, ref sDdeCommand))
						ReadDdeSettings("http", ref sDdeServerName, ref sDdeTopic, ref sDdeCommand);

					////
					// DDE connect

					// First, try using the given service name plus the standard topic/command
					if(TryCallingBrowserDde(sDdeServerName, Dde.InternetExplorer.TopicOpenUrlNewWindow, Dde.InternetExplorer.CommandOpenUrlInNewWindow, uri))
						return;
					// Second, try using the given service name plus the standard topic/command
					if(TryCallingBrowserDde(sDdeServerName, Dde.InternetExplorer.TopicOpenUrl, Dde.InternetExplorer.CommandOpenUrlInNewWindow, uri))
						return;
					// Third, try the obtained parameters (not guaranteed to open in the new window)
					if(TryCallingBrowserDde(sDdeServerName, sDdeCommand, sDdeTopic, uri))
						return;

					// Note: IE was temporarily disabled because if some buggy Mozilla won'y handle the DDE request, and an IE window is running, another IE window will appear, which is not quite nice, after all
					/*
					// Fourth, try the default IExplore settings
					if(TryCallingBrowserDde(Dde.InternetExplorer.Service, Dde.InternetExplorer.TopicOpenUrl, Dde.InternetExplorer.CommandOpenUrlInNewWindow, uri))
						return;
					*/
				}
				catch(Exception ex)
				{
					Trace.WriteLine( "An unhandled exception has occured while trying to talk to a browser via DDE. " + ex.Message, "[UIM]" );
					// Note: most DDE exceptions are handled, but some of them are considered fatal and are rethrown, for example, a timeout exception
					// In the latter case (timeout), it's important to stop retrying DDE connection another way and to fallback to ShellExecute method
				}
			}

    		/////////////////
    		// ShellExecute

    		// As DDE has failed, try doing ShellExecute on the URI
    		try
    		{
    			ProcessStartInfo psi = new ProcessStartInfo();
    			psi.FileName = uri;
    			psi.Verb = "open"; // Force opening the link in a new window
    			psi.UseShellExecute = true;
    			Process.Start(psi);
    		}
    		catch(SystemException ex)
    		{
    			// Trap exceptions from silly mozilla
    			Trace.WriteLine(String.Format("Error opening document \"{0}\". {1}", uri, ex.Message), "[UIM]");
    		}
    		catch(Exception ex)
    		{ // Intercept and report the exceptions (if invoked from the UI thread)
    			if(Core.UserInterfaceAP.IsOwnerThread)
    			{
    				Core.ReportBackgroundException(ex);
    				MessageBox.Show(String.Format("Error opening document \"{0}\".\n{1}", uri, ex.Message), "Open Link in New Window", MessageBoxButtons.OK, MessageBoxIcon.Error);
    			}
    			else
    				throw ex;
    		}
    	}

		/// <summary>
		/// A delegate for the <see cref="OpenInNewBrowserWindow"/> function.
		/// </summary>
		protected delegate void OpenInNewBrowserWindowDelegate( string uri, bool bUseDde );

    	/// <summary>
    	/// Attempts to read the DDE settings from the Registry for the given protocol scheme.
    	/// The referenced parameters provide the default values on entry and contain the captured data on exit, if the function fails (see retval), they're not modified.
    	/// Does not throw non-fatal exceptions.
    	/// </summary>
    	/// <param name="sProtocolScheme">Protocol scheme to read info on.</param>
    	/// <returns><c>True</c> if the data was fetched OK, <c>False</c> if some failure has occured.</returns>
    	protected bool ReadDdeSettings(string sProtocolScheme, ref string sDdeServerNameOut, ref string sDdeTopicOut, ref string sDdeCommandOut)
    	{
    		// Cache in the local vars to avoid changing the outers accidentally
    		string sDdeServerName = sDdeServerNameOut;
    		string sDdeTopic = sDdeTopicOut;
    		string sDdeCommand = sDdeCommandOut;

    		try
    		{
    			// Open the key under HKCR
    			RegistryKey keyDde = Registry.ClassesRoot.OpenSubKey(String.Format(@"{0}\shell\open\ddeexec", sProtocolScheme), false);
    			if(keyDde != null)
    			{ // Succeeded to open

    				// Get the application name (default value of the Application subkey)
    				RegistryKey keyApplication = keyDde.OpenSubKey("Application");
    				if(keyApplication != null)
    				{
    					sDdeServerName = keyApplication.GetValue(null).ToString();
    					keyApplication.Close();
    				}

    				// If a topic is specified, grab its name
    				// Look for the default value of the Topic subkey
    				RegistryKey keyTopic = keyDde.OpenSubKey("Topic");
    				if(keyTopic != null)
    				{
    					sDdeTopic = keyTopic.GetValue(null, sDdeTopicOut).ToString();
    					keyTopic.Close();

    					// Get the command text (default value of the current key)
    					// It's interesting only if the topic name is specified
    					sDdeCommand = keyDde.GetValue(null).ToString();
    				}

    				keyDde.Close();
    			}
    			else
    				return false; // Failed to open the key

    			// The information has been captured OK, pass it out
    			sDdeServerNameOut = sDdeServerName;
    			sDdeTopicOut = sDdeTopic;
    			sDdeCommandOut = sDdeCommand;
    			return true; // OK
    		}
    		catch(Exception ex)
    		{
    			Trace.WriteLine("Error obtaining the default browser's DDE service name and topic. " + ex.Message, "[UIM]");
    		}

    		return false; // An exception has occured -> failed to retrieve the info for the given proto
    	}

    	/// <summary>
    	/// Attempts to connect to the Browser via DDE using the information provided in order to open the given URI.///
    	/// </summary>
    	/// <param name="sDdeServerName">DDE service name.</param>
    	/// <param name="sDdeTopic">DDE topic name to open the conversation on.</param>
    	/// <param name="sDdeCommandTemplate">Template for the command to be sent within the conversation. The "<c>%1</c>" char marks the place to insert an URI into.</param>
    	/// <param name="uri">URI to be sent within the command.</param>
    	/// <returns><c>True</c> if the interaction has succeeded, or <c>False</c> if the Browser has reported a failure or DDE connection could not be established.</returns>
    	protected bool TryCallingBrowserDde(string sDdeServerName, string sDdeTopic, string sDdeCommandTemplate, string uri)
    	{
    		// Safety checks
    		if( (sDdeServerName == null) || (sDdeTopic == null) || (sDdeCommandTemplate == null) || (uri == null) )
    		{
    			Trace.WriteLine( "Refused talking to the Browser via DDE because one of the parameters is Null.", "[UIM]" );
    			return false;
    		}
    		if( (sDdeServerName.Length == 0) || (sDdeTopic.Length == 0) || (sDdeCommandTemplate.Length == 0) || (uri.Length == 0) )
    		{
    			Trace.WriteLine( "Refused talking to the Browser via DDE because one of the string parameters is zero-length.", "[UIM]" );
    			return false;
    		}

    		// Substitute the template
    		string sDdeCommand = sDdeCommandTemplate.Replace( "%1", uri );
    		// TODO: escape the possible double quotes within the URI

    		try
    		{
    			// Attempt sending the command
    			using( DdeConversation conv = GetDde().CreateConversation( sDdeServerName, sDdeTopic ) )
    				conv.StartAsyncTransaction( null, sDdeCommand );

    			// If succeeded, then the duty is completed
    			return true;
    		}
    		catch( DdeException ex )
    		{
    			Trace.WriteLine( String.Format( "Error making a DDE conversation to the Browser at \"{0}\" on topic \"{1}\" with command {3}. {2}", sDdeServerName, sDdeTopic, ex.Message, sDdeCommand ), "[UIM]" );
    			if( ex.Timeout )
    			{
    				Trace.WriteLine( "The DDE timeout exception will be rethrown to avoid retrying.", "[UIM]" );
    				throw ex;
    			}
    		}
    		catch( Exception ex )
    		{
    			Trace.WriteLine( String.Format( "Error making a DDE conversation to the Browser at \"{0}\" on topic \"{1}\" with command {3}. {2}", sDdeServerName, sDdeTopic, ex.Message, sDdeCommand ), "[UIM]" );
    		}

    		return false; // An exception has occured
    	}

    	#endregion

    	public IStatusWriter GetStatusWriter( object owner, StatusPane pane )
        {
            return MainFrame.GetStatusWriter( owner, pane );
        }

        public string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow )
        {
            return InputString( title, prompt, initialValue, validateDelegate, ownerWindow, 0, null );
        }

        public string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow, InputStringFlags flags )
        {
            return InputString( title, prompt, initialValue, validateDelegate, ownerWindow, flags, null );
        }

        public string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow, InputStringFlags flags,
            string helpTopic )
        {
            using( InputStringDlg dlg = new InputStringDlg() )
            {
                dlg.Text = title;
                dlg.PromptText = prompt;
                if ( initialValue != null )
                {
                    dlg.StringText = initialValue;
                }
                dlg.HelpTopic = helpTopic;
                dlg.ValidateStringDelegate = validateDelegate;
                if ( ( flags & InputStringFlags.AllowEmpty ) != 0 )
                {
                    dlg.AllowEmptyString = true;
                }
                if ( dlg.ShowDialog( ownerWindow ?? Core.MainWindow ) == DialogResult.OK )
                {
                    return dlg.StringText;
                }
                return null;
            }
        }


        public void QueueUIJob( Delegate method, params object[] args )
        {
            Core.UserInterfaceAP.QueueJob( method, args );
        }

        public void QueueUIJob(Action action)
        {
            Core.UserInterfaceAP.QueueJob("", action);
        }


        public void RunWithProgressWindow( [NotNull] string progressTitle, [NotNull] Action action )
        {
            MainFrame.RunWithProgressWindow( progressTitle, action );
        }

    	internal void OnEnterIdle()
        {
            if ( EnterIdle != null )
            {
                EnterIdle( this, EventArgs.Empty );
            }
        }

        internal void OnExitMenuLoop()
        {
            if ( ExitMenuLoop != null )
            {
                ExitMenuLoop( this, EventArgs.Empty );
            }
        }

        internal bool IsCancelMainWindowClosing()
        {
            if ( MainWindowClosing != null )
            {
                CancelEventArgs e = new CancelEventArgs( false );
                MainWindowClosing( this, e );
                return e.Cancel;
            }
            return false;
        }

        public void WriteToUsageLog( string text )
        {
            LogManager.WriteToUsageLog( text );
        }

        public bool UsageLogEnabled
        {
            get { return LogManager.UsageLogEnabled; }
        }

        public bool LeftSidebarExpanded
        {
            get { return MainFrame.LeftSidebarExpanded; }
            set { MainFrame.LeftSidebarExpanded = value; }
        }

        public bool RightSidebarExpanded
        {
            get { return MainFrame.RightSidebarExpanded; }
            set { MainFrame.RightSidebarExpanded = value; }
        }

        public bool ShortcutBarVisible
        {
            get { return MainFrame.ShortcutBarVisible; }
            set { MainFrame.ShortcutBarVisible = value; }
        }

        public bool WorkspaceBarVisible
        {
            get { return MainFrame.WorkspaceButtonsVisible;}
            set { MainFrame.WorkspaceButtonsVisible = value; }
        }

        private MainFrame MainFrame
        {
            get
            {
                if ( _mainFrame == null )
                {
                    _mainFrame = Core.MainWindow as MainFrame;
                }
                return _mainFrame;
            }
        }

        public void RestoreMainWindow()
        {
            MainFrame.RestoreFromTray();
        }

        public void CloseMainWindow()
        {
            if ( Core.UserInterfaceAP.IsOwnerThread )
            {
                MainFrame.ForceClose();
            }
            else
            {
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( MainFrame.ForceClose ) );
            }
        }

        public void ShowDesktopAlert( IResource res )
        {
            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                QueueUIJob( new ResourceDelegate( ShowDesktopAlert ), res );
                return;
            }
            PrepareBalloonForm();
            _balloonForm.ShowResource( res );
        }

        public void ShowDesktopAlert( ImageList imageList, int imageIndex, string from, string subject,
                                      string body, EventHandler clickHandler )
        {
            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                QueueUIJob( new ShowDesktopAlertDelegate( ShowDesktopAlert ),
                    imageList, imageIndex, from, subject, body, clickHandler);
                return;
            }
            PrepareBalloonForm();
            _balloonForm.ShowAlert( imageList, imageIndex, from, subject, body, clickHandler );
        }

        private delegate void ShowDesktopAlertDelegate( ImageList imageList, int imageIndex, string from,
                                                        string subject, string body, EventHandler clickHandler );

        private void PrepareBalloonForm()
        {
            if ( _balloonForm == null || _balloonForm.IsDisposed )
            {
                _balloonForm = new BalloonForm();
                _balloonForm.Show();
                _balloonForm.Hide();
            }
            if ( !_balloonForm.Visible )
            {
                _balloonForm.SetDefaultLocation();
                Win32Declarations.ShowWindow( _balloonForm.Handle, Win32Declarations.SW_SHOWNOACTIVATE );
            }
        }

        public string  DefaultFontFace  { get { return DefaultFormattingFont.Name; } }
        public float   DefaultFontSize  { get { return DefaultFormattingFont.Size; } }

        public Font DefaultFormattingFont
        {
            get
            {
                if( _formattingFont == null )
                {
                    string currFont = Core.SettingStore.ReadString( "MainFrame", "DefaultFont", _cDefaultFont );
                    float  currSize = (float)Core.SettingStore.ReadInt( "MainFrame", "DefaultFontSize", _cDefaultFontSize );
                    _formattingFont = new Font( currFont, currSize );
                }
                return _formattingFont;
            }
            set
            {
                _formattingFont = value;
                Core.SettingStore.WriteString( "MainFrame", "DefaultFont", _formattingFont.Name );
                Core.SettingStore.WriteInt( "MainFrame", "DefaultFontSize", (int)_formattingFont.Size );
            }
        }

    	/// <summary>
    	/// Gets the DDE object.
    	/// Note that it's lazy-created on first call and attaches to the calling thread, preventing from use from any other thread.
    	/// </summary>
    	internal Dde GetDde()
    	{
    		if(_dde == null)
    			_dde = new Dde();
    		return _dde;
    	}
    }
}

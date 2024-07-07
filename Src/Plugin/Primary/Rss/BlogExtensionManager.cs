// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;
using RSSPlugin;
using Syndication.Extensibility;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Loads and manages the plugins implementing the IBlogExtension interface.
	/// </summary>
	internal class BlogExtensionManager
	{
        private readonly ArrayList _blogExtensions = new ArrayList();
        private const string _extensionKey = @"Software\JetBrains\Omea\BlogExtensions";

        public void LoadExtensions()
        {
            Core.ActionManager.RegisterContextMenuActionGroup( "BlogExtensions", "Blog This", ListAnchor.Last );
            Core.ActionManager.RegisterContextMenuActionGroup( "BlogExtensionsConfigure", "Blog This", ListAnchor.Last );
            Core.ActionManager.RegisterContextMenuAction( new ConfigureExtensionsAction(), "BlogExtensionsConfigure",
                                                          ListAnchor.Last, "Configure Extensions...", null, "RSSItem", null );

            string pluginPath = Path.Combine( Application.StartupPath, "plugins" );
            if ( Directory.Exists( pluginPath ) )
            {
                foreach( string fileName in Directory.GetFiles( pluginPath, "*.dll" ) )
                {
                    string pluginFileName = Path.Combine( pluginPath, fileName );
                    LoadExtension( pluginFileName );
                }
            }

            RegistryKey regKey = Registry.CurrentUser.CreateSubKey( _extensionKey );
            foreach( string valueName in regKey.GetValueNames() )
            {
                string path = (string) regKey.GetValue( valueName );
                LoadExtension( path );
            }
            regKey.Close();
        }

	    private BlogExtensionData LoadExtension( string fileName )
        {
            Type[] pluginTypes;
            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom( fileName );
                pluginTypes = pluginAssembly.GetExportedTypes();
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString() );
                return null;
            }

            // search for IPlugin instances
            foreach( Type aType in pluginTypes )
            {
                foreach( Type intfType in aType.GetInterfaces() )
                {
                    if ( intfType == typeof(IBlogExtension) )
                    {
                        IBlogExtension extension;
                        try
                        {
                            extension = (IBlogExtension) Activator.CreateInstance( aType );
                        }
                        catch( Exception ex )
                        {
                            if ( ex is TargetInvocationException )
                            {
                                ex = (ex as TargetInvocationException).InnerException;
                            }
                            DialogResult dr = MessageBox.Show( Core.MainWindow,
                                "Exception when initializing weblog posting extension " + fileName +
                                    ":\r\n" + ex.Message + "\r\nWould you like to uninstall the extension?",
                                Core.ProductFullName, MessageBoxButtons.YesNo );
                            if ( dr == DialogResult.Yes )
                            {
                                UninstallExtensionFile( fileName );
                            }
                            return null;
                        }
                        IAction extAction = RegisterBlogExtensionAction( extension );
                        BlogExtensionData extData = new BlogExtensionData( fileName, extension, extAction );
                        _blogExtensions.Add( extData );
                        return extData;
                    }
                }
            }
            return null;
        }

        internal BlogExtensionData InstallExtension( string fileName )
        {
            BlogExtensionData extData = LoadExtension( fileName );
            if ( extData == null )
            {
                return null;
            }

            RegUtil.CreateSubKey( Registry.CurrentUser, _extensionKey );
            RegUtil.SetValue( Registry.CurrentUser, _extensionKey, Path.GetFileNameWithoutExtension( fileName ), fileName );
            return extData;
        }

        internal void UninstallExtension( BlogExtensionData extData )
        {
            UninstallExtensionFile( extData.FileName );
            Core.ActionManager.UnregisterContextMenuAction( extData.Action );
            _blogExtensions.Remove( extData );
        }

	    private static void UninstallExtensionFile( string fileName )
	    {
	        RegistryKey regKey = Registry.CurrentUser.OpenSubKey( _extensionKey, true );
	        foreach( string valueName in regKey.GetValueNames() )
	        {
	            string path = (string) regKey.GetValue( valueName );
	            if ( path == fileName )
	            {
	                regKey.DeleteValue( valueName );
	                break;
	            }
	        }
	    }

	    private static IAction RegisterBlogExtensionAction( IBlogExtension extension )
	    {
            BlogExtensionAction extAction = new BlogExtensionAction( extension );
            Core.ActionManager.RegisterContextMenuAction( extAction, "BlogExtensions", ListAnchor.Last,
                                                          extension.DisplayName, null, "RSSItem", null );
            return extAction;
	    }

        internal IEnumerable BlogExtensions
        {
            get { return _blogExtensions; }
        }
	}

    internal class BlogExtensionData
    {
        private readonly string _fileName;
        private readonly IBlogExtension _blogExtension;
        private readonly IAction _action;

        public BlogExtensionData( string fileName, IBlogExtension blogExtension, IAction extAction )
        {
            _fileName = fileName;
            _blogExtension = blogExtension;
            _action = extAction;
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public IBlogExtension BlogExtension
        {
            get { return _blogExtension; }
        }

        public IAction Action
        {
            get { return _action; }
        }
    }

    internal class BlogExtensionAction: ActionOnSingleResource
    {
        private readonly IBlogExtension _extension;

        public BlogExtensionAction( IBlogExtension extension )
        {
            _extension = extension;
        }

        public override void Execute( IActionContext context )
        {
            IResource item = context.SelectedResources [0];
            IResource feed = item.GetLinkProp( -Props.RSSItem );

			// Submit the item, show the editing UI if needed
			BlogExtensionComposer.Compose( _extension, item, feed );
        }
    }

    internal class ConfigureExtensionsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.UIManager.ShowOptionsDialog( "Internet", "Feeds" );
        }
    }
}

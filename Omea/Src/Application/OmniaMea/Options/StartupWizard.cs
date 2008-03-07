/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea
{
    internal class StartupWizard
    {
        private const string                _wizardType = "WizardPane";
        private static HashMap              _registeredPanes = new HashMap();
        private static HashMap              _activePanes = new HashMap();
        private static StartupWizardForm    _activeWizard;

        private class StartupWizardForm : WizardForm
        {
            public StartupWizardForm( string explanation )
                : base( Core.ProductName + " Startup Wizard",
                "Welcome\nto the " + Core.ProductName + " Startup Wizard", explanation )
            {}
            public override bool ConfirmCancel()
            {
                DialogResult dr = MessageBox.Show( this, 
                    "Do you really wish to cancel the Startup Wizard and close " + Core.ProductName + "?",
                    "Startup Wizard", MessageBoxButtons.YesNo );
                return dr == DialogResult.Yes;
            }
        }

        static public void RegisterTypes()
        {
            Core.ResourceStore.ResourceTypes.Register(
                _wizardType, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
        }

        static public void RegisterWizardPane( string header, OptionsPaneCreator creator, int order )
        {
            if( _registeredPanes.Contains( header ) )
            {
                throw new InvalidOperationException(
                    "Startup Wizard Pane '" + header + "' is already registered." );
            }
            int index = header.LastIndexOf( '/' );
            if( index > 0 )
            {
                string parentHeader = header.Substring( 0, index );
                if( !_registeredPanes.Contains( parentHeader ) )
                {
                    throw new InvalidOperationException(
                        "Can't register Startup Wizard Pane '" + header + "' because parent pane '" +
                        parentHeader + "' was not registered." );
                }
            }
            _registeredPanes[ header ] = new Pair( order, creator );
            if( _activeWizard != null )
            {
                CreatePane( header );
            }
        }

        static public void DeRegisterWizardPane( string header )
        {
            if( !_registeredPanes.Contains( header ) )
            {
                throw new InvalidOperationException(
                    "Startup Wizard Pane '" + header + "' was not registered." );
            }
            if( _activeWizard != null )
            {
                OptionsPaneWizardAdapter pane = (OptionsPaneWizardAdapter) _activePanes[ header ];
                if( pane.Parent != null )
                {
                    pane.Parent.DeregisterPane( pane );
                }
                else
                {
                    _activeWizard.DeregisterPane( pane );
                }
                _activePanes.Remove( header );
            }
            _registeredPanes.Remove( header );
        }

        static public DialogResult RunWizard( bool forceWizard )
        {
            HashSet submittedPanes = new HashSet();
            IResourceList wizardsResources = Core.ResourceStore.GetAllResources( _wizardType );
            foreach( IResource wizard in wizardsResources )
            {
                submittedPanes.Add( wizard.GetPropText( Core.Props.Name ) );
            }
            forceWizard = forceWizard || ( submittedPanes.Count == 0 );

            string explanation = ( forceWizard ) ?
                "This wizard helps you to configure " + Core.ProductName + "." :
                "This wizard helps you to configure the new plugins you have installed for " +
                Core.ProductName + ".";

            _activeWizard = new StartupWizardForm( explanation );
            _activeWizard.StartPosition = FormStartPosition.CenterScreen;
            DialogResult result = DialogResult.None;
            try
            {

                foreach( HashMap.Entry e in _registeredPanes )
                {
                    string header = (string) e.Key;
                    if( forceWizard || !submittedPanes.Contains( header ) )
                    {
                        CreatePane( header );
                    }
                }
                if( _activePanes.Count > 0 )
                {
                    Form progressWindow = (Form) Core.ProgressWindow;
                    progressWindow.Visible = false;
                    try
                    {
                        result = _activeWizard.ShowDialog( null );
                    }
                    finally
                    {
                        progressWindow.Visible = true;                        
                    }
                    if( result == DialogResult.OK )
                    {
                        foreach( HashMap.Entry e in _activePanes )
                        {
                            string header = (string) e.Key;
                            IResource wizardPane = Core.ResourceStore.NewResourceTransient( _wizardType );
                            wizardPane.SetProp( Core.Props.Name, header );
                            Core.ResourceAP.QueueJob(
                                JobPriority.Immediate, new MethodInvoker( wizardPane.EndUpdate ) );
                        }
                    }
                }
            }
            finally
            {
                _activeWizard = null;
                _activePanes.Clear();
            }
            return result;
        }

        static private OptionsPaneWizardAdapter CreatePane( string fullHeader )
        {
            Pair pair = (Pair) _registeredPanes[ fullHeader ];
            int order = (int) pair.First;
            OptionsPaneCreator creator = (OptionsPaneCreator) pair.Second;
            string parentHeader = string.Empty;
            string header = fullHeader;
            int index = fullHeader.LastIndexOf( '/' );
            if( index > 0 )
            {
                parentHeader = fullHeader.Substring( 0, index );
                header = fullHeader.Substring( index + 1 );
            }
            OptionsPaneWizardAdapter pane = new OptionsPaneWizardAdapter( header, creator );
            pane.IsStartupPane = true;
            if( parentHeader.Length == 0 )
            {
                _activeWizard.RegisterPane( order, pane );
            }
            else
            {
                OptionsPaneWizardAdapter parentPane = (OptionsPaneWizardAdapter) _activePanes[ parentHeader ];
                if( parentPane == null )
                {
                    parentPane = CreatePane( parentHeader );
                }
                parentPane.RegisterPane( order, pane );
            }
            _activePanes[ fullHeader ] = pane;
            return pane;
        }
    }
}

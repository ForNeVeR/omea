// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Manage the rules which control switching of tray icon (in the taskbar
    /// area) upon some conditions.
    /// </summary>
    /// <since>2.0</since>
    public interface ITrayIconManager
    {
        /// <summary>
        /// Register a rule for an tray icon, which changes an icon whenever
        /// its conditions match the context of the application.
        /// </summary>
        /// <param name="name">Name of a watcher.</param>
        /// <param name="resTypes">A list of resource types valid for a watcher.</param>
        /// <param name="conditions">Conditions necessary to be matched for an icon.</param>
        /// <param name="exceptions">Exceptions necessary to be matched for an icon.</param>
        /// <param name="icon">The Icon graphical object.</param>
        IResource RegisterTrayIconRule( string name, string[] resTypes, IResource[] conditions, IResource[] exceptions, Icon icon );

        IResource RegisterTrayIconRule( string name, string[] resTypes, IResource[][] conditionGroups, IResource[] exceptions, Icon icon );

        /// <summary>
        /// Reregister a rule for an tray icon - do not create new rule resource but rather
        /// reset the parameters of the existing one. Required by IFilteringForms API behavior.
        /// </summary>
        /// <param name="existingRule">Resource of an existing rule.</param>
        /// <param name="name">Name of a watcher.</param>
        /// <param name="resTypes">A list of resource types valid for a watcher.</param>
        /// <param name="conditions">Conditions necessary to be matched for an icon.</param>
        /// <param name="exceptions">Exceptions necessary to be matched for an icon.</param>
        /// <param name="icon">The Icon graphical object.</param>
        /// <since>539</since>
        IResource ReregisterTrayIconRule( IResource existingRule, string name, string[] resTypes,
                                          IResource[] conditions, IResource[] exceptions, Icon icon );

        /// <summary>
        /// Unregister a tray icon watcher.
        /// </summary>
        /// <param name="name">Name of a watcher.</param>
        void    UnregisterTrayIconRule( string name );

        /// <summary>
        /// Check whether there exists (registered) a tray icon watcher with the given name.
        /// </summary>
        /// <param name="name">Name of a watcher.</param>
        /// <returns>True if the watcher with the given name is registered already.</returns>
        bool    IsTrayIconRuleRegistered( string name );

        /// <summary>
        /// Find a resource corresponding to the tray icon watcher with the given name.
        /// </summary>
        /// <param name="name">Name of a watcher.</param>
        /// <returns>A resource corresponding to the watcher name. Null if there is no such watcher.</returns>
        IResource FindRule( string name );

        /// <summary>
        /// Rename a rule.
        /// </summary>
        /// <param name="rule">A resource representing a rule (returned by RegisterRule method).</param>
        /// <param name="newName">New name for a resource.</param>
        /// <throws>Throws ArgumentException object if the rule with the new name already exists.</throws>
        void        RenameRule( IResource rule, string newName );

        /// <summary>
        /// Creates new TrayIcon rule and clones all necessary information into
        /// the new destination.
        /// </summary>
        /// <param name="sourceRule">Resource from which the information will be cloned.</param>
        /// <param name="newName">Name of a new TrayIcon rule.</param>
        /// <since>501</since>
        /// <returns>A resource for a new rule.</returns>
        IResource  CloneRule( IResource sourceRule, string newName );

        /// <summary>
        /// Switches Tray Icon Rule Manager into mode when a rule's icon is
        /// removed when there is no resource matching the rule's condition.
        /// </summary>
        void  SetStrictMode();

        /// <summary>
        /// Switches Tray Icon Rule Manager into mode when a rule's icon is
        /// removed when at least one resource matching the rule's condition
        /// becomes invalid (e.g. one item becomes read).
        /// </summary>
        void  SetOutlookMode();

        /// <summary>
        /// Gets a value indicating whether the tray icon manager is currently in a mode
        /// in which a rule's icon isremoved when at least one resource matching the rule's condition
        /// becomes invalid.
        /// </summary>
        bool  IsOutlookMode { get; }
    }
}

// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Manage rules which control automatic removal of resources given the special
    /// conditions and exceptions from newsgroups, feeds, or deleted resources view.
    /// </summary>
    /// <since>530</since>
    public interface IExpirationRuleManager
    {
        /// <summary>
        /// Create and register an expiration rule for specified folders.
        /// </summary>
        /// <param name="folders">A list of folder resources (e.g. Newsgroups or RSSFeeds) which
        /// will be monitored for resources deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRule( IResourceList folders, int count, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Create and register an expiration rule for specified folder resource type. This default rule will
        /// be applied by default to all such folders.
        /// </summary>
        /// <param name="baseType">A resource representing "resource type" for a folder objects.
        /// Folders of this type will be monitored for resources deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRule( IResource baseType, int count, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Create and register an expiration rule for deleted items of specified resource type. This default rule will
        /// be applied by default to all such resources.
        /// </summary>
        /// <param name="baseType">A resource representing "resource type". Deleted resources of this type
        /// will be monitored for deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRuleForDeletedItems( IResource baseType, int count, IResource[] exceptions, IResource[] actions );


        /// <summary>
        /// Create and register an expiration rule for specified folders.
        /// </summary>
        /// <param name="folders">A list of folder resources (e.g. Newsgroups or RSSFeeds) which
        /// will be monitored for resources deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRule( IResourceList folders, IResource[] conditions, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Create and register an expiration rule for specified folder resource type. This default rule will
        /// be applied by default to all such folders.
        /// </summary>
        /// <param name="baseType">A resource representing "resource type" for a folder objects.
        /// Folders of this type will be monitored for resources deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRule( IResource baseType, IResource[] conditions, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Create and register an expiration rule for deleted items of specified resource type. This default rule will
        /// be applied by default to all such resources.
        /// </summary>
        /// <param name="baseType">A resource representing "resource type". Deleted resources of this type
        /// will be monitored for deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        /// <returns>A resource for the registered rule.</returns>
        IResource RegisterRuleForDeletedItems( IResource baseType, IResource[] conditions, IResource[] exceptions, IResource[] actions );


        /// <summary>
        /// Register an existing expiration rule for specified folders.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="folders">A list of folder resources (e.g. Newsgroups or RSSFeeds) which
        /// will be monitored for resources deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRule( IResource baseRes, IResourceList folders, IResource[] conditions, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Register an existing expiration rule for specified folder resource type. This default rule will
        /// be applied by default to all such folders.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="baseType">A resource representing "resource type" for a folder objects.
        /// Folders of this type will be monitored for resources deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRule( IResource baseRes, IResource baseType, IResource[] conditions, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Register an existing expiration rule for deleted items of specified resource type. This default rule will
        /// be applied by default to all such resources.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="baseType">A resource representing "resource type". Deleted resources of this type
        /// will be monitored for deletion.</param>
        /// <param name="conditions">A list of conditions which must be satisfied for resources to be deleted.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRuleForDeletedItems( IResource baseRes, IResource baseType, IResource[] conditions, IResource[] exceptions, IResource[] actions );


        /// <summary>
        /// Register an existing expiration rule for specified folders.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="folders">A list of folder resources (e.g. Newsgroups or RSSFeeds) which
        /// will be monitored for resources deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRule( IResource baseRes, IResourceList folders, int count, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Register an existing expiration rule for specified folder resource type. This default rule will
        /// be applied by default to all such folders.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="baseType">A resource representing "resource type" for a folder objects.
        /// Folders of this type will be monitored for resources deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRule( IResource baseRes, IResource baseType, int count, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Register an existing expiration rule for deleted items of specified resource type. This default rule will
        /// be applied by default to all such resources.
        /// No new resource is created; this method is required by RulesManager operational contract.
        /// </summary>
        /// <param name="baseRes">A resource for an existing expiration rule.</param>
        /// <param name="baseType">A resource representing "resource type". Deleted resources of this type
        /// will be monitored for deletion.</param>
        /// <param name="count">Control the number of resources in the folder under monitoring. If set to meaningful
        /// value (not -1) then time period exceptions are not used.</param>
        /// <param name="exceptions">A list of exceptions which must be satisfied for resources to be deleted.</param>
        /// <param name="actions">A list of actions to be performed over the matched resources.</param>
        void ReregisterRuleForDeletedItems( IResource baseRes, IResource baseType, int count, IResource[] exceptions, IResource[] actions );


        /// <summary>
        /// Delete an expiration rule.
        /// </summary>
        /// <param name="name">Name of the expiration rule.</param>
        void UnregisterRule( string name );

        /// <summary>
        /// Rename a rule.
        /// </summary>
        /// <param name="rule">A resource representing a rule (returned by RegisterRule method).</param>
        /// <param name="newName">New name for a resource.</param>
        /// <throws>Throws ArgumentException object if the rule with the new name already exists.</throws>
        /// <since>548</since>
        void      RenameRule( IResource rule, string newName );

        /// <summary>
        /// Register a resource type for which the deletion monitoring will be performed.
        /// </summary>
        /// <param name="linkId">Id of link which connects base resources and their containers (e.g. feed posts with feeds).</param>
        /// <param name="containerType">A resource type for container resources (e.g. Feeds for feed posts).</param>
        /// <param name="itemType">A resource type for base resources.</param>
        void      RegisterResourceType( int linkId, string containerType, string itemType );
    }
}

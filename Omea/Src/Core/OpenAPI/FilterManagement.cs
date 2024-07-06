// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Plugins should implement this interface if they need to register
    /// task-specific conditions, views or rules.
    /// </summary>
    public interface IViewsConstructor
    {
        /// <summary>
        /// Method is called when a plugin that implements this interface is loaded first time.
        /// </summary>
        void  RegisterViewsFirstRun();
        /// <summary>
        /// Method is called when a plugin that implements this interface is loaded.
        /// Usually this method contains code which creates rule actions and performs
        /// corrections to the resources created during the first start of the plugin.
        /// </summary>
        void  RegisterViewsEachRun();
    }

    /// <summary>
    /// Enumeration lists all possible operations supported by the FilterRegistry.
    /// Possible operations are descibed below.
    /// </summary>
    public enum ConditionOp
    {
        /// <summary>
        /// A property value (of value type, not a resource link) must be equal to the value given in the values list;
        /// </summary>
        Eq = 10,
        /// <summary>
        /// A property value (of value type, not a resource link) must be less than the value given in the values list;
        /// </summary>
        Lt = 20,
        /// <summary>
        /// A property value (of value type, not a resource link) must be greater than the value given in the values list;
        /// </summary>
        Gt = 30,
        /// <summary>
        /// A link value must refer to the resource object given in the values list;
        /// </summary>
        In = 40,
        /// <summary>
        /// Reserved;
        /// </summary>
        Has = 50,
        /// <summary>
        /// A property value (of value type, not a resource link) must be in the interval of the lower and upper margin given in the values list
        /// (as first and last value correspondingly);
        /// </summary>
        InRange = 60,
        /// <summary>
        /// A resource must have a link given in the propName parameter;
        /// </summary>
        HasLink = 70,
        /// <summary>
        /// A resource must have a property given in the propName parameter;
        /// </summary>
        HasProp = 80,
        /// <summary>
        /// A resource must not have a property given in the propName parameter;
        /// </summary>
        HasNoProp = 85,
        /// <summary>
        /// A resource must match query condition given in the propName parameter.
        /// </summary>
        QueryMatch = 90
    };

    /// <summary>
    /// Specifies the names of standard events in the system.
    /// </summary>
    /// <since>556</since>
    public class  StandardEvents
    {
        public const string  ResourceReceived = "A resource is received";
        public const string  CategoryAssigned = "A category is assigned";
    }

    /// <summary>
    /// Provides services related to custom views, rules, conditions and rule actions.
    /// </summary>
    public interface IFilterRegistry
    {
        #region Conditions
        /// <summary>
        /// Create standard condition anew, old condition with such name is removed, all links to
        /// the old condition are invalidated.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="propName">Name of a property which participates in condition evaluation.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="RecreateStandardCondition( string, string, string[], string, ConditionOp, IResourceList)">Overload which uses an array of resource
        /// objects as operation parameters (for In operation).</see>
        IResource   RecreateStandardCondition( string name, string deepName, string[] resTypes, string propName, ConditionOp op, params string[] values );

        /// <summary>
        /// Create standard condition anew, old condition with such name is removed, all links to
        /// the old condition are invalidated.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="propName">Name of a property which participates in condition evaluation.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="RecreateStandardCondition( string, string, string[], string, ConditionOp, string[])">Overload which uses an array of strings
        /// as operation parameters.</see>
        IResource   RecreateStandardCondition( string name, string deepName, string[] resTypes, string propName, ConditionOp op, IResourceList values );

        /// <summary>
        /// Create standard condition if it does not exist otherwise return the existing condition.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource types for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="propName">Name of a property which participates in condition evaluation.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="CreateStandardCondition( string, string, string[], string, ConditionOp, IResourceList)">Overload which uses an array of resource
        /// objects as operation parameters (for In operation).</see>
        IResource   CreateStandardCondition( string name, string deepName, string[] resTypes, string propName, ConditionOp op, params string[] values );

        /// <summary>
        /// Create standard condition if it does not exist otherwise return the existing condition.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource types for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="propName">Name of a property which participates in condition evaluation.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of resource parameters (operation-dependent).</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="CreateStandardCondition( string, string, string[], string, ConditionOp, string[])">Overload which uses an array of
        /// strings as operation parameters.</see>
        IResource   CreateStandardCondition( string name, string deepName, string[] resTypes, string propName, ConditionOp op, IResourceList values );

        /// <summary>
        /// Create query condition if it does not exist otherwise return the existing condition. This method is a shortcut of the more
        /// general method <see cref="CreateStandardCondition( string, string, string[], string, ConditionOp, string[])"></see>.
        /// </summary>
        /// <param name="name">Name of a query condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource types for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="query">Search query to the text index.</param>
        /// <param name="sectionName">[Optional] Name of a document section in which the search must be performed.</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="RecreateQueryCondition( string, string, string[], string, string)">Method which always creates new query condition and
        /// removes the existing condition with such name.</see>
        IResource   CreateQueryCondition( string name, string deepName, string[] resTypes, string query, string sectionName );

        /// <summary>
        /// Create query condition, remove old query condition with the same name. This method is a shortcut of the more
        /// general method <see cref="RecreateStandardCondition( string, string, string[], string, ConditionOp, string[])"></see>.
        /// </summary>
        /// <param name="name">Name of a query condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource types for which condition is applicable (null if the condition can be applied to all resource types).</param>
        /// <param name="query">Search query to the text index.</param>
        /// <param name="sectionName">[Optional] Name of a document section in which the search must be performed.</param>
        /// <returns>Resource representing the created condition.</returns>
        /// <see cref="RecreateQueryCondition( string, string, string[], string, string)">Method which always creates new query condition and
        /// removes the existing condition with such name.</see>
        IResource   RecreateQueryCondition( string name, string deepName, string[] resTypes, string query, string sectionName );

        /// <summary>
        /// Register custom condition object.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a condition - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which template is applicable (null if the template can be applied to all resource types).</param>
        /// <param name="filter">An object implementing ICustomCondition interface.</param>
        /// <returns>Resource representing the created condition.</returns>
        IResource   RegisterCustomCondition( string name, string deepName, string[] resTypes, ICustomCondition filter );
        #endregion Conditions

        #region Conditions Templates
        IResource   CreateCustomConditionTemplate( string name, string deepName, string[] resTypes,
                                                   ICustomConditionTemplate filter, ConditionOp op, params string[] values );

        /// <summary>
        /// Create standard condition template anew, old template with such name is removed, all links to
        /// the old template are invalidated.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a template - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which template is applicable (null if the template can be applied to all resource types).</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created template.</returns>
        IResource   RecreateConditionTemplate( string name, string deepName, string[] resTypes, ConditionOp op, params string[] values );

        /// <summary>
        /// Create standard condition template if there is no with such name, return existing template otherwise.
        /// </summary>
        /// <param name="name">Name of a condition template.</param>
        /// <param name="deepName">Deep name of a template - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which template is applicable (null if the template can be applied to all resource types).</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created template.</returns>
        IResource   CreateConditionTemplate( string name, string deepName, string[] resTypes, ConditionOp op, params string[] values );

        /// <summary>
        /// Create a condition template with UI handler anew, old template with such name is removed, all links to
        /// the old template are invalidated. UI handlers are the means to visually represent and accept condition
        /// template parameters, when e.g. they have a special semantics which differs from the parameter's original type.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a template - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which template is applicable (null if the template can be applied to all resource types).</param>
        /// <param name="handler">An instance of ITemplateParamUIHandler interface, implementing visual
        /// representation of the parameter's value and semantics.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created template.</returns>
        IResource   RecreateConditionTemplateWithUIHandler( string name, string deepName, string[] resTypes,
                                                            ITemplateParamUIHandler handler,
                                                            ConditionOp op, params string[] values );
        /// <summary>
        /// Create a condition template with UI handler if there is no with such name, return existing template otherwise.
        /// UI handlers are the means to visually represent and accept condition template parameters,
        /// when e.g. they have a special semantics which differs from the parameter's original type.
        /// </summary>
        /// <param name="name">Name of a condition.</param>
        /// <param name="deepName">Deep name of a template - a name which is permanent independently of
        /// potential condition renamings and translations. All conditions are identified using deep names,
        /// not display ones.</param>
        /// <param name="resTypes">A set of resource type names for which template is applicable (null if the template can be applied to all resource types).</param>
        /// <param name="handler">An instance of ITemplateParamUIHandler interface, implementing visual
        /// representation of the parameter's value and semantics.</param>
        /// <param name="op">Condition operation.</param>
        /// <param name="values">Set of string parameters (operation-dependent).</param>
        /// <returns>Resource representing the created template.</returns>
        IResource   CreateConditionTemplateWithUIHandler( string name, string deepName, string[] resTypes,
                                                          ITemplateParamUIHandler handler,
                                                          ConditionOp op, params string[] values );
        /// <summary>
        /// Create a proxy rule condition (exception) from a condition template
        /// by instantiating of template with necessary typed parameter. Link
        /// condition and template for subsequent modifications.
        /// </summary>
        /// <param name="template">A resource representing a condition template.</param>
        /// <param name="param">Parameter for instantiation.</param>
        /// <param name="resTypes">An array of resource types for which the condition is valid.</param>
        /// <returns>A resource representing an proxy condition.</returns>
        /// <since>428</since>
        IResource   InstantiateConditionTemplate( IResource template, object param, string[] resTypes );
        #endregion Conditions Templates

        #region Operations over Conditions
        /// <summary>
        /// Makes a condition usable only for rules matching. This prohibits it
        /// from using in views.
        /// </summary>
        /// <param name="condition">Resource representing a condition.</param>
        void        MarkConditionOnlyForRule( IResource condition );

        /// <summary>
        /// Ascribes a condition to a group. These groups divide conditions into
        /// meaningful groups, which are represented as folders in
        /// Choose Condition/Exception dialog.
        /// </summary>
        /// <param name="condition">Resource representing a condition.</param>
        /// <param name="groupName">Name of a group.</param>
        void        AssociateConditionWithGroup( IResource condition, string groupName );
        #endregion Operations over Conditions

        #region Folders
        /// <summary>
        /// Create new view folder and link it to the base view folder. Method does
        /// not create new view folder if there is already exists one with such name,
        /// but always performs linking with bas view folder. If base folder does not
        /// exist, methods creates new one with the given name and links it to the
        /// root folder with the order 0.
        /// </summary>
        /// <param name="name">Name of folder.</param>
        /// <param name="baseFolderName">Name of base folder. If parameter is null, root folder is used.</param>
        /// <param name="order">Order of a view under the root.</param>
        /// <returns>Resource representing view folder.</returns>
        /// <since>379</since>
        IResource  CreateViewFolder( string name, string baseFolderName, int order );

        /// <summary>
        /// Associate a view with a folder, that is a view will be placed into that folder in
        /// the tree of views and categories resources.
        /// </summary>
        /// <param name="view">A resource representing a view.</param>
        /// <param name="folderName">Name of a view folder. Set to NULL if ResourceTreeRoot is a target.
        /// If folder is not the ResourceTreeRoot, it must already exist.</param>
        /// <param name="order">Order of a view within a sequence of views under the same view folder.</param>
        /// <since>410</since>
        void    AssociateViewWithFolder( IResource view, string folderName, int order );
        #endregion Folders

        #region Views
        /// <summary>
        /// Register a view with the given set of conditions and exceptions.
        /// It returns the existing view with such name and creates new view otherwise.
        /// </summary>
        /// <param name="name">Name of a view.</param>
        /// <param name="conditions">Set of conditions created by means of CreateStandardView family of methods.</param>
        /// <param name="exceptions">Set of exceptions (negative conditions) created by means of CreateStandardView family of methods.</param>
        /// <returns>Resource representing the registered view.</returns>
        /// <see cref="RegisterView( string, string[], IResource[], IResource[])">Overload method which additionally specifies a set of resource types for which
        /// the view is valid</see>
        IResource   RegisterView( string name, IResource[] conditions, IResource[] exceptions );

        /// <summary>
        /// Register a view with the given set of conditions and exceptions. It creates new view each call.
        /// </summary>
        /// <param name="name">Name of a view.</param>
        /// <param name="resTypes">A set of resource type names for which view is applicable (null if the view can be applied to all resource types).</param>
        /// <param name="conditions">Set of conditions created by means of CreateStandardView family of methods.</param>
        /// <param name="exceptions">Set of exceptions (negative conditions) created by means of CreateStandardView family of methods.</param>
        /// <returns>Resource representing the registered view.</returns>
        /// <see cref="RegisterView( string, IResource[], IResource[])">Overload method which does not specify a set of resource types for which
        /// the view is valid</see>
        IResource   RegisterView( string name, string[] resTypes, IResource[] conditions, IResource[] exceptions );

        IResource   RegisterView( string name, string[] resTypes, IResource[][] conditionGroups, IResource[] exceptions );

        /// <summary>
        /// Register a view with the given set of conditions and exceptions. Method modifies the existing
        /// view resource with the given parameters.
        /// </summary>
        /// <param name="view">A basic resource which is modified.</param>
        /// <param name="name">Name of a view.</param>
        /// <param name="resTypes">A set of resource type names for which view is applicable (null if the view can be applied to all resource types).</param>
        /// <param name="conditions">Set of conditions created by means of CreateStandardView family of methods.</param>
        /// <param name="exceptions">Set of exceptions (negative conditions) created by means of CreateStandardView family of methods.</param>
        /// <see cref="RegisterView( string, IResource[], IResource[])">Overload method which does not modify existing view resource if it exist.</see>
        void  ReregisterView ( IResource view, string name, string[] resTypes, IResource[] conditions, IResource[] exceptions );

        void  ReregisterView ( IResource view, string name, string[] resTypes, IResource[][] conditionGroup, IResource[] exceptions );

        /// <summary>
        /// Delete a view.
        /// </summary>
        /// <param name="viewName">Name of a view.</param>
        void    DeleteView( string viewName );
        /// <summary>
        /// Delete a view.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        void    DeleteView( IResource view );

        IResource CloneView( IResource from, string newName );

        /// <summary>
        /// Makes possible a view to be visible in all tabs including those
        /// containing "exclusive" resource types.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <since>403</since>
        void    SetVisibleInAllTabs( IResource view );

        /// <summary>
        /// Returns true if a view is visible in all tabs including those
        /// containing "exclusive" resource types.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <since>403</since>
        bool    IsVisibleInAllTabs( IResource view );
        #endregion Views

        #region Rule Actions
        /// <summary>
        /// Methods registers a rule action - an object of a class which implements
        /// IRuleAction interface.
        /// </summary>
        /// <param name="actionName">Name of an action. This string is visible when user selects an action
        /// when constructing a rule.</param>
        /// <param name="deepName">Deep name of an action - a name which is permanent independently of
        /// potential action renamings and translations. All action are identified using deep names,
        /// not display ones.</param>
        /// <param name="executor">An object implementing the particular action.</param>
        /// <returns>Resource representing the registered action.</returns>
        /// <see cref="RegisterRuleAction( string, string, IRuleAction, string[])">Overload method which additionally specifies a set of resource types for which
        /// the action is valid</see>
        IResource   RegisterRuleAction( string actionName, string deepName, IRuleAction executor );

        /// <summary>
        /// Methods registers a rule action - an object of a class which implements
        /// IRuleAction interface.
        /// </summary>
        /// <param name="actionName">Name of an action. This string is visible when user selects an action
        /// when constructing a rule.</param>
        /// <param name="deepName">Deep name of an action - a name which is permanent independently of
        /// potential action renamings and translations. All action are identified using deep names,
        /// not display ones.</param>
        /// <param name="executor">An object implementing the particular action.</param>
        /// <param name="types">A set of resource type names for which the action is valid.</param>
        /// <returns>Resource representing the registered action.</returns>
        /// <see cref="RegisterRuleAction( string, string, IRuleAction)">Overload method which does not specify a set of resource types for which
        /// the action is valid</see>
        IResource   RegisterRuleAction( string actionName, string deepName, IRuleAction executor, string[] types );

        /// <summary>
        /// Methods registers a rule action template - an object of a class which
        /// implements IRuleAction interface and template parameters by means of which
        /// selected values can be passed to the object in runtime.
        /// </summary>
        /// <param name="name">Name of an action template. This string is visible when
        /// user selects an action when constructing a rule.</param>
        /// <param name="deepName">Deep name of an action template - a name which is permanent
        /// independently of potential action template renamings and translations. All
        /// action template are identified using deep names, not display ones.</param>
        /// <param name="executor">An object implementing the particular action.</param>
        /// <param name="op">Action parameters operation.</param>
        /// <param name="values">Operation arguments.</param>
        /// <returns>Resource representing the registered action template.</returns>
        /// <see cref="RegisterRuleActionTemplate( string, string, IRuleAction, string[], ConditionOp, string[])">Overload method which does not specify a set of resource types for which
        /// the action is valid</see>
        IResource   RegisterRuleActionTemplate( string name, string deepName, IRuleAction executor,
                                                ConditionOp op, params string[] values );
        /// <summary>
        /// Methods registers a rule action template - an object of a class which
        /// implements IRuleAction interface and template parameters by means of which
        /// selected values can be passed to the object in runtime.
        /// </summary>
        /// <param name="name">Name of an action template. This string is visible when
        /// user selects an action when constructing a rule.</param>
        /// <param name="deepName">Deep name of an action template - a name which is permanent
        /// independently of potential action template renamings and translations. All
        /// action template are identified using deep names, not display ones.</param>
        /// <param name="executor">An object implementing the particular action.</param>
        /// <param name="types">A set of resource type names for which the action is valid.</param>
        /// <param name="op">Action parameters operation.</param>
        /// <param name="values">Operation arguments.</param>
        /// <returns>Resource representing the registered action template.</returns>
        /// <see cref="RegisterRuleActionTemplate( string, string, IRuleAction, ConditionOp, string[])">Overload method which does not specify a set of resource types for which
        /// the action is valid</see>
        IResource   RegisterRuleActionTemplate( string name, string deepName, IRuleAction executor,
                                                string[] types, ConditionOp op, params string[] values );

        IResource   RegisterRuleActionTemplateWithUIHandler( string name, string deepName, IRuleAction executor, string[] types,
                                                             ITemplateParamUIHandler handler, ConditionOp op, params string[] values );

        /// <summary>
        /// Sets a single selection mode of parameter values for condition/action template.
        /// </summary>
        /// <param name="actionTemplate">Resource representing a template.</param>
        void        MarkActionTemplateAsSingleSelection( IResource actionTemplate );

        /// <summary>
        /// Check whether an rule action is instantiated, that is IRuleAction object
        /// is created and registered for this action.
        /// </summary>
        /// <param name="action">Resource representing a rule action.</param>
        /// <returns>True if IRuleAction object is registered for this rule action.</returns>
        bool        IsActionInstantiated( IResource action );

        /// <summary>
        /// Create a proxy rule action from an action template by instantiating of
        /// template with necessary typed parameter. Link action and template for
        /// subsequent modifications.
        /// </summary>
        /// <param name="template">A resource representing a filter template.</param>
        /// <param name="param">Parameter for instantiation.</param>
        /// <param name="representation">For action templates with UI handlers this string
        /// contains external representation of the parameter value. Pass null if not necessary.</param>
        /// <returns>A resource representing an proxy rule action.</returns>
        /// <since>556</since>
        IResource   InstantiateRuleActionTemplate( IResource template, object param, string representation );
        #endregion Rule Actions

        #region Rules
        /// <summary>
        /// Register rule. If a rule with the given name already exists, it is deleted and
        /// new rule resource is created.
        /// </summary>
        /// <param name="eventName">The name of an event when the rule is activated.</param>
        /// <param name="ruleName">Name of a rule.</param>
        /// <param name="resTypes">A set of resource type names for which the rule is valid.</param>
        /// <param name="conditions">An array of rule conditions.</param>
        /// <param name="exceptions">An array of rule exceptions (negative conditions).</param>
        /// <param name="actions">An array of actions.</param>
        /// <returns>Resource representing the registered rule.</returns>
        /// <since>556</since>
        IResource   RegisterRule( string eventName, string ruleName, string[] resTypes,
                                  IResource[] conditions, IResource[] exceptions, IResource[] actions );
        /// <since>993</since>
        IResource   RegisterRule( string eventName, string ruleName, string[] resTypes,
                                  IResource[][] conditionGroups, IResource[] exceptions, IResource[] actions );
        /// <summary>
        /// Reregister rule - overwrite the data for the existing rule resource.
        /// </summary>
        /// <param name="eventName">The name of an event when the rule is activated.</param>
        /// <param name="existingRule">A resource which parameters will be overwritten.</param>
        /// <param name="ruleName">Name of a rule.</param>
        /// <param name="resTypes">A set of resource type names for which the rule is valid.</param>
        /// <param name="conditions">An array of rule conditions.</param>
        /// <param name="exceptions">An array of rule exceptions (negative conditions).</param>
        /// <param name="actions">An array of actions.</param>
        /// <since>556</since>
        void  ReregisterRule( string eventName, IResource existingRule, string ruleName, string[] resTypes,
                              IResource[] conditions, IResource[] exceptions, IResource[] actions );
        /// <since>993</since>
        void  ReregisterRule( string eventName, IResource existingRule, string ruleName, string[] resTypes,
                              IResource[][] conditionGroups, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Delete a rule.
        /// </summary>
        /// <param name="viewName">Name of a rule, by which it was registered in the RegisterRule method.</param>
        void    DeleteRule( string viewName );
        /// <summary>
        /// Delete a rule.
        /// </summary>
        /// <param name="rule">A resource representing a rule (returned by RegisterRule method).</param>
        void    DeleteRule( IResource rule );

        /// <summary>
        /// Rename a rule.
        /// </summary>
        /// <param name="rule">A resource representing a rule (returned by RegisterRule method).</param>
        /// <param name="newName">New name for a resource.</param>
        /// <throws>Throws ArgumentException object if the rule with the new name already exists.</throws>
        /// <since>548</since>
        void    RenameRule( IResource rule, string newName );

        /// <summary>
        /// Activate a rule if it was deactivated.
        /// </summary>
        /// <param name="rule">Resource of a rule, returned by the RegisterRule method.</param>
        void    ActivateRule( IResource rule );
        /// <summary>
        /// Deactivate a rule - exclude it from the list of rules which process incoming resource.
        /// </summary>
        /// <param name="rule">Resource of a rule, returned by the RegisterRule method.</param>
        void    DeactivateRule( IResource rule );
        /// <summary>
        /// Perform actions for marking rule as invalid so that it is not used in the normal
        /// application run.
        /// </summary>
        /// <param name="rule">A rule resource.</param>
        /// <param name="reason">String message describing the reason of rule deactivation.</param>
        /// <since>997</since>
        void    MarkRuleAsInvalid( IResource rule, string reason );

        /// <summary>
        /// Assign an order number for a rule - determine the order in which rules are processed for any given resource.
        /// </summary>
        /// <param name="rule">Resource of a rule, returned by the RegisterRule method.</param>
        /// <param name="num">Order of a rule in the rules list.</param>
        void    AssignOrderNumber( IResource rule, int num );

        /// <summary>
        /// Find an action rule resource given its name.
        /// </summary>
        /// <param name="name">Name of a rule.</param>
        /// <returns>A resource representing the action rule.</returns>
        IResource   FindRule( string name );

        /// <summary>
        /// Check whether a rule is active.
        /// </summary>
        /// <param name="rule">A resource representing a rule.</param>
        /// <returns>True if a rule is active.</returns>
        bool        IsRuleActive( IResource rule );
        #endregion Rules

        #region Rename
        /// <summary>
        /// Rename created condition. This method must be called right after the
        /// condition with new name is [re]created.
        /// </summary>
        /// <param name="oldName">Old name of a condition (under which is was created).</param>
        /// <param name="newName">New name of a condition.</param>
        void        RenameCondition ( string oldName, string newName );

        /// <summary>
        /// Rename created condition template. This method must be called right after the
        /// condition with new name is [re]created.
        /// </summary>
        /// <param name="oldName">Old name of a condition template (under which is was created).</param>
        /// <param name="newName">New name of a condition template.</param>
        void        RenameConditionTemplate ( string oldName, string newName );

        /// <summary>
        /// Rename created rule action. This method must be called right after the
        /// rule action with new name is [re]created.
        /// </summary>
        /// <param name="oldName">Old name of a rule action (under which is was created).</param>
        /// <param name="newName">New name of a rule action.</param>
        void        RenameRuleAction( string oldName, string newName );
        #endregion Rename

        #region Getters/Setters
        /// <summary>
        /// Return a list of registered views.
        /// </summary>
        /// <returns>List of registered views.</returns>
        IResourceList   GetViews();

        /// <summary>
        /// Return a list of registered ormatting rules.
        /// </summary>
        /// <param name="visible">Indicates whether visible rules are requested (that is such rules which are
        /// shown and editable in the Rules Manager).</param>
        /// <returns>List of registered views.</returns>
        IResourceList   GetFormattingRules( bool visible );

        /// <summary>
        /// Return a list of linked conditions of a rule or view.
        /// </summary>
        /// <param name="viewOrRule">Resource representing the rule or view.</param>
        /// <returns>List of linked conditions.</returns>
        IResourceList   GetConditions( IResource viewOrRule );

        /// <summary>
        /// Return a list of linked conditions of a rule or view. It iterates over
        /// all conjunction groups and returns a plain list.
        /// </summary>
        /// <param name="viewOrRule">Resource representing the rule or view.</param>
        /// <returns>List of linked conditions.</returns>
        IResourceList   GetConditionsPlain( IResource viewOrRule );

        /// <summary>
        /// Return a list of associated exceptions of a rule or view.
        /// </summary>
        /// <param name="viewOrRule">Resource representing the rule or view.</param>
        /// <returns>List of associated exceptions.</returns>
        IResourceList   GetExceptions( IResource viewOrRule );
        /// <summary>
        /// Return a list of associated actions of a rule.
        /// </summary>
        /// <param name="rule">Resource representing the rule.</param>
        /// <returns>List of associated actions.</returns>
        IResourceList   GetActions   ( IResource rule );

        /// <summary>
        /// Creates a new condition with all the parameters equal to the input one.
        /// </summary>
        /// <param name="condition">Resource representing a condition to clone.</param>
        /// <returns>Resource representing new condition.</returns>
        IResource  CloneCondition( IResource condition );

        /// <summary>
        /// Creates a new action with all the parameters equal to the input one.
        /// </summary>
        /// <param name="action">Resource representing an action to clone.</param>
        /// <returns>Resource representing new action.</returns>
        IResource  CloneAction ( IResource action );

        /// <summary>
        /// Extract all conditions that are derived from the given template and linked to
        /// it with the <c>Core.FilterRegistry.Props.TemplateLink</c> link.
        /// </summary>
        /// <param name="conditionTemplate">Condition template resource.</param>
        /// <returns>List of conditions derived from the given template.</returns>
        /// <since>2.5</since>
        IResourceList GetLinkedConditions( IResource conditionTemplate );

        #endregion Getters/Setters

        /// <summary>
        /// Access standard conditions and templates.
        /// </summary>
        /// <since>445</since>
        IStandardConditions Std { get; }

        /// <summary>
        /// Get the standard name of a view under which search results are shown.
        /// </summary>
        string      ViewNameForSearchResults { get; }

        /// <summary>
        /// </summary>
        IFilterManagerProps Props { get; }
    }

    public interface IFilterEngine
    {
        #region Views Execution
        /// <summary>
        /// Activates a view, that is extracts a set of resource matching the
        /// view conditions/exceptions and passes the result to ResourceList Browser.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <param name="viewName">Activated view name.</param>
        /// <since>2.1</since>
        IResourceList ExecView( IResource view, string viewName );

        /// <summary>
        /// Extract a set of resources matching the view's conditions/exceptions.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <returns>Result list of resources.</returns>
        IResourceList ExecView( IResource view );

        /// <summary>
        /// Extract a set of resources matching the view's conditions/exceptions from
        /// a given initial set of resources.
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <param name="initialSet">List of resources from which matched resources must be selected.</param>
        /// <returns>Result list of resources.</returns>
        /// <since>544</since>
        IResourceList ExecView( IResource view, IResourceList initialSet );

        /// <param name="view">Resource representing a view.</param>
        /// <param name="initialSet">List of resources from which matched resources must be selected.</param>
        /// <param name="mode">Required mode of the resulting resource list (default is Snapshot).</param>
        /// <since>1180</since>
	    IResourceList ExecView( IResource view, IResourceList initialSet, SelectionType mode );

        /// <summary>
        /// </summary>
        /// <param name="view">Resource representing a view.</param>
        /// <param name="res">A resource which is checked against the view's conditions and exceptions.</param>
        /// <param name="checkWorkspace">Perform match only for those resources which belong to the same
        /// workspace as the view.</param>
        /// <returns>True if a resource matches the conditions/exceptions of the view.</returns>
        /// <since>438</since>
        bool  MatchView( IResource view, IResource res, bool checkWorkspace );
        #endregion Views Execution

        #region Rules Execution
        /// <summary>
        /// Activate execution of rules associated with the given event for the
        /// given resource in the particular order (order can be changed by means
        /// of method "AssignOrderNumber").
        /// </summary>
        /// <param name="eventName">Specifies the name of an event (resource received, resource is sent, ect)</param>
        /// <param name="res">Resource for which rules are activated.</param>
        /// <returns>True if any rule matched its condition against the resource
        /// and was activated</returns>
        bool        ExecRules( string eventName, IResource res );

        /// <summary>
        /// Activate the particular rule for the given list of resources.
        /// </summary>
        /// <param name="rule">Resource representing a rule.</param>
        /// <param name="list">List of resources for which a rule is activated.</param>
        /// <since>419</since>
        void        ExecRule( IResource rule, IResourceList list );

        /// <summary>
        /// Apply rule's actions to a given resource.
        /// </summary>
        /// <param name="rule">A resource representing a rule.</param>
        /// <param name="res">A resource which actions must be applied to.</param>
        /// <since>544</since>
        void        ApplyActions( IResource rule, IResource res );
        #endregion Rules Execution

        #region Events Registration
        /// <summary>
        /// Register a string identifier of a logical event in the system. Using this
        /// id core and plugins can mark that some event has occured and call
        /// execution of rules for such an event over the needed resources.
        /// </summary>
        /// <param name="eventName">Name of an event.</param>
        /// <param name="displayName">String which is shown in the combobox of the Edit Rule form.</param>
        /// <since>556</since>
        void    RegisterActivationEvent( string eventName, string displayName );

        /// <summary>
        /// Retrieves a collection of registered events and returns them in the
        /// hashtable of pairs (eventName, displayName);
        /// </summary>
        /// <returns></returns>
        /// <since>556</since>
        Hashtable  GetRegisteredEvents();

        /// <summary>
        /// If a plugin registers task-specific actions and rules for some particular
        /// resource type, this type must be registered in the system by means of this
        /// method.
        /// </summary>
        /// <param name="resType">A string representing the name of a resource type.</param>
        void    RegisterRuleApplicableResourceType( string resType );
        #endregion Events Registration
    }

    public interface IFilterManagerProps
    {
        int OpProp           { get; }
        int TemplateLink     { get; }
        int Invisible        { get; }
        int SetValueLink     { get; }

        int LinkedConditions { get; }
        int LinkedExceptions { get; }
        int LinkedActions    { get; }
    }

    /// <summary>
    /// Interface for classes which implement custom conditions.
    /// </summary>
    public interface ICustomCondition
    {
        /// <summary>
        /// Predicate which matches a resource against the condition implemented
        /// in the custom condition class. This method is called when a custom condition
        /// is used in rules.
        /// </summary>
        /// <param name="resource">Resource for matching.</param>
        /// <returns>True, if a resource matches the condition implemented by the class.</returns>
        bool  MatchResource( IResource resource );

        /// <summary>
        /// Produce a list of resources matching the condition and given restriction on resource types.
        /// This method is calles when a custom condition is used in views.
        /// </summary>
        /// <param name="resType">Resource type(s) description.</param>
        /// <returns>Result list of resources matching the custom conditon.</returns>
        IResourceList   Filter( string resType );
    }

    public interface ICustomConditionTemplate
    {
        /// <summary>
        /// Predicate which matches a resource against the condition implemented
        /// in the custom condition class. This method is called when a custom condition
        /// is used in rules.
        /// </summary>
        /// <param name="resource">Resource for matching.</param>
        /// <param name="actionStore">Container for parameters specified when instantiating the condition template.</param>
        /// <returns>True, if a resource matches the condition implemented by the class.</returns>
        bool  MatchResource( IResource resource, IActionParameterStore actionStore );

        /// <summary>
        /// Produce a list of resources matching the condition and given restriction on resource types.
        /// This method is calles when a custom condition is used in views.
        /// </summary>
        /// <param name="resType">Resource type(s) description.</param>
        /// <param name="actionStore">Container for parameters specified when instantiating the condition template.</param>
        /// <returns>Result list of resources matching the custom conditon.</returns>
        IResourceList   Filter( string resType, IActionParameterStore actionStore );
    }

    /// <summary>
    /// Interface for classes implementing rule actions. Rule actions are called
    /// when a resource matches against the rule's conditions. Each action is called
    /// in turn for the same resource.
    /// </summary>
    public interface IRuleAction
    {
        /// <summary>
        /// Exec an implemented action over the input resource.
        /// </summary>
        /// <param name="res">Resource for which action is called.</param>
        /// <param name="actionStore">Class which incapsulates the parameters chosen when
        /// action was parameterized through action template.</param>
        void  Exec( IResource res, IActionParameterStore actionStore );
    }

    /// <summary>
    /// Interface for classes implementing wrapper around the set of parameters choosen
    /// when the action was parameterized through action template.
    /// </summary>
    public interface IActionParameterStore
    {
        /// <summary>
        /// Return set of parameters as resource list. If parameters were of
        /// integral types or represented of set of string, exception is raised.
        /// </summary>
        /// <returns>Set of parameters as resources list.</returns>
        IResourceList   ParametersAsResList();
        /// <summary>
        /// Return set of parameters as strings list. If parameters were of
        /// integral types or represented of set of resource, exception is raised.
        /// </summary>
        /// <returns>Set of parameters as strings list.</returns>
        string          ParameterAsString();
    }

    /// <summary>
    /// This interface is used to construct small graphical elements - forms which are shown
    /// when user edits parameters of condition templates. These forms are used to represent
    /// semantically parameters for which editing in their ordinary format is not user-friendly.
    /// For example, task importance is specified by int value (-1, 0, 1), but UI handler for
    /// displaying a group of radio buttons is much more convenient.
    /// </summary>
    public interface ITemplateParamUIHandler
    {
        /// <summary>
        /// Show the form, accept the result.
        /// </summary>
        /// <param name="parent">Parent form handle.</param>
        /// <returns>Result of the dialog run.</returns>
        DialogResult    ShowUI( IWin32Window parent );

        /// <summary>
        /// Set the resource of the condition template which parameter will be edited.
        /// </summary>
        IResource       Template    { set; }
    }

    /// <summary>
    /// UI handler interface for visually editing list of resources.
    /// </summary>
    public interface IResourceListTemplateParamUIHandler : ITemplateParamUIHandler
    {
        IResourceList   Result      { get; }
        IResourceList   CurrentValue{ set; }
    }

    /// <summary>
    /// UI handler interface for visually editing a value represented as string.
    /// </summary>
    public interface IStringTemplateParamUIHandler : ITemplateParamUIHandler
    {
        /// <summary>
        /// Sets the current internal value referenced by the template.
        /// </summary>
        string  CurrentValue    { set; }

        /// <summary>
        /// Get the handler result in the internal form.
        /// </summary>
        string  Result          { get; }

        /// <summary>
        /// Get the handler's result in the form which will be shown in the View/Rule dialog.
        /// </summary>
        string  DisplayString   { get; }
    }
}

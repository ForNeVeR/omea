// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Provide the uniform access to all View-related Editing forms. Most predicted use of this API is
    /// from Plugins or other external components which have no complete access to internals of
    /// EditView or EditRule form modules.
    /// </summary>
    /// <since>341</since>
    public interface  IFilteringFormsManager
    {
        /// <summary>
        /// Show editing form for a view/rule-based resource given its type and flags.
        /// </summary>
        /// <param name="res">A resource to be viewed/edited.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditResourceForm( IResource res );

        /// <summary>
        /// Show empty EditView form.
        /// </summary>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditViewForm(); //  alias
        /// <summary>
        /// Edit a view with the predefined list of parameters.
        /// </summary>
        /// <param name="name">Name of a view.</param>
        /// <param name="resTypes">A list of resource type names for which a view is active.</param>
        /// <param name="conditions">List of condition groups.</param>
        /// <param name="exceptions">List of exception resources.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditViewForm( string name, string[] resTypes,
                                      IResource[][] conditions, IResource[] exceptions );

        /// <summary>
        /// Show empty EditRule form.
        /// </summary>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditActionRuleForm();
        /// <summary>
        /// Edit a rule with the predefined list of parameters.
        /// </summary>
        /// <param name="name">Name of a rule.</param>
        /// <param name="resTypes">A list of resource type names for which a rule is active.</param>
        /// <param name="conditions">List of condition groups.</param>
        /// <param name="exceptions">List of exception resources.</param>
        /// <param name="actions">List of action resources.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditActionRuleForm( string name, string[] resTypes,
                                            IResource[][] conditions, IResource[] exceptions, IResource[] actions );

        /// <summary>
        /// Show empty EditFormattingRule form.
        /// </summary>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditFormattingRuleForm();
        /// <summary>
        /// Edit a rule with the predefined list of parameters.
        /// </summary>
        /// <param name="name">Name of a rule.</param>
        /// <param name="resTypes">A list of resource type names for which a rule is active.</param>
        /// <param name="conditions">List of condition groups.</param>
        /// <param name="exceptions">List of exception resources.</param>
        /// <param name="isBold">Whether to apply "bold" attribute to a result formatting.</param>
        /// <param name="isItalic">Whether to apply "italic" attribute to a result formatting.</param>
        /// <param name="isUnderline">Whether to apply "underline" attribute to a result formatting.</param>
        /// <param name="isStrikeout">Whether to apply "strikeout" attribute to a result formatting.</param>
        /// <param name="foreColor">Color of the foreground.</param>
        /// <param name="backColor">Color of the backgroung.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditFormattingRuleForm( string name, string[] resTypes,
                                                IResource[][] conditions, IResource[] exceptions,
                                                bool isBold, bool isItalic, bool isUnderline, bool isStrikeout,
                                                string foreColor, string backColor );

        /// <summary>
        /// Show empty EditTrayIconRule form.
        /// </summary>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditTrayIconRuleForm();
        /// <summary>
        /// Edit a rule with the predefined list of parameters.
        /// </summary>
        /// <param name="name">Name of a rule.</param>
        /// <param name="resTypes">A list of resource type names for which a rule is active.</param>
        /// <param name="conditions">List of condition groups.</param>
        /// <param name="exceptions">List of exception resources.</param>
        /// <param name="iconFileName">Name of a file containing an icon resource.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowEditTrayIconRuleForm( string name, string[] resTypes,
                                              IResource[][] conditions, IResource[] exceptions,
                                              string iconFileName );

        /// <summary>
        /// Show AdvancedSearch dialog with no parameters set.
        /// </summary>
        void        ShowAdvancedSearchForm();
        /// <summary>
        /// Show AdvancedSearch dialog with predefined parameters.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="resTypes">A list of resource type names for which search is active.</param>
        /// <param name="conditions">List of condition groups.</param>
        /// <param name="exceptions">List of exception resources.</param>
        void        ShowAdvancedSearchForm( string query, string[] resTypes,
                                            IResource[][] conditions, IResource[] exceptions );
        void        ShowAdvancedSearchForm( string query, string[] resTypes,
                                            IResource[] conditions, IResource[] exceptions );

        /// <summary>
        /// Show empty EditExpirationRule form.
        /// </summary>
        /// <param name="folders">A list of folder resources for which a rule is monitored.</param>
        /// <param name="defRule">A resource for an existing rule.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowExpirationRuleForm( IResourceList folders, IResource defRule );

        /// <summary>
        /// Edit a rule with the predefined list of parameters.
        /// </summary>
        /// <param name="resType">A resource type for which a default rule is monitored.</param>
        /// <param name="defRule">A resource for an existing rule.</param>
        /// <param name="forDeletedItems">Whether the rule is applied to existing or deleted resources.</param>
        /// <returns>A resource which was returned as a result from the form.</returns>
        IResource   ShowExpirationRuleForm( IResource resType, IResource defRule, bool forDeletedItems );
    }
}

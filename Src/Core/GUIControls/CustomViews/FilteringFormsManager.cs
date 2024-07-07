// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using GUIControls.CustomViews;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CustomViews;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// </summary>
    /// <since>341</since>
    public class FilteringFormsManager : IFilteringFormsManager
    {
        #region Edit Resource
        public IResource   ShowEditResourceForm( IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "FilteringFormsManager -- Input resource is NULL" );

            if( res.Type != FilterManagerProps.ViewResName &&
                res.Type != FilterManagerProps.RuleResName &&
                res.Type != FilterManagerProps.ViewCompositeResName )
                throw new ArgumentException( "FilteringFormsManager -- Input resource has inappropriate type [" + res.Type + "]" );
            #endregion Preconditions

            Form form;
            IResource result = null;
            String name = res.GetStringProp( Core.Props.Name );
            if( res.HasProp( "IsActionFilter") )
                form = new EditRuleForm( name );
            else
            if( res.HasProp( "IsFormattingFilter") )
                form = new EditFormattingRuleForm( name );
            else
            if( res.HasProp( "IsTrayIconFilter") )
                form = new EditTrayIconRuleForm( name );
            else
            if( res.HasProp( "IsExpirationFilter") )
            {
                if( IsSimpleExpirationRule( res ) )
                    form = new EditExpirationRuleSimpleForm( res );
                else
                    form = new EditExpirationRuleForm( name );
            }
            else
                form = new EditViewForm( name );

            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                if( form is EditExpirationRuleSimpleForm )
                    result = (form as EditExpirationRuleSimpleForm).ResultResource;
                else
                    result = (form as ViewCommonDialogBase).ResultResource;
            }

            form.Dispose();

            return result;
        }
        #endregion Edit Resource

        #region Edit View
        public IResource   ShowEditViewForm()
        {
            return ShowEditViewForm( null, null, null, null );
        }
        public IResource   ShowEditViewForm( string name, string[] resTypes,
                                             IResource[][] conditions, IResource[] exceptions )
        {
            IResource   result = null;
            EditViewForm form = new EditViewForm( name, resTypes, conditions, exceptions );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                result = form.ResultResource;
            return result;
        }
        #endregion Edit View

        #region Edit Action Rule
        public IResource   ShowEditActionRuleForm()
        {
            return ShowEditActionRuleForm( null, null, null, null, null );
        }
        public IResource   ShowEditActionRuleForm( string name, string[] resTypes,
                                                   IResource[][] conditions, IResource[] exceptions, IResource[] actions )
        {
            IResource   result = null;
            EditRuleForm form = new EditRuleForm( name, resTypes, conditions, exceptions, actions );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                result = form.ResultResource;
            return result;
        }
        #endregion Edit Action Rule

        #region Edit Formatting Rule
        public IResource   ShowEditFormattingRuleForm()
        {
            return ShowEditFormattingRuleForm( null, null, null, null, false, false, false, false, null, null );
        }
        public IResource   ShowEditFormattingRuleForm( string name, string[] resTypes,
                                                       IResource[][] conditions, IResource[] exceptions,
                                                       bool isBold, bool isItalic, bool isUnderline, bool isStrikeout,
                                                       string foreColor, string backColor )
        {
            IResource   result = null;
            EditFormattingRuleForm form = new EditFormattingRuleForm( name, resTypes, conditions, exceptions,
                                                                      isBold, isItalic, isUnderline, isStrikeout,
                                                                      foreColor, backColor );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                result = form.ResultResource;
            return result;
        }
        #endregion Edit Formatting Rule

        #region Edit Tray Icon Rule
        public IResource   ShowEditTrayIconRuleForm()
        {
            return ShowEditTrayIconRuleForm( null, null, null, null, null );
        }
        public IResource   ShowEditTrayIconRuleForm( string name, string[] types,
                                                     IResource[][] conditions, IResource[] exceptions,
                                                     string  iconFileName )
        {
            IResource   result = null;
            EditTrayIconRuleForm form = new EditTrayIconRuleForm( name, types, conditions, exceptions, iconFileName );
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
                result = form.ResultResource;
            return result;
        }
        #endregion Edit Tray Icon Rule

        #region Advanced Search
        public void  ShowAdvancedSearchForm()
        {
            AdvancedSearchForm form = new AdvancedSearchForm( string.Empty );
            form.Show();
        }
        public void  ShowAdvancedSearchForm( string query, string[] resTypes,
                                             IResource[] conditions, IResource[] exceptions )
        {
            IResource[][] group = FilterRegistry.Convert2Group( conditions );
            ShowAdvancedSearchForm( query, resTypes, group, exceptions );
        }
        public void  ShowAdvancedSearchForm( string query, string[] resTypes,
                                             IResource[][] conditions, IResource[] exceptions )
        {
            AdvancedSearchForm form = new AdvancedSearchForm( query, resTypes, conditions, exceptions );
            form.Show();
        }
        #endregion Advanced Search

        #region Edit Expiration Rule
        public IResource   ShowExpirationRuleForm( IResourceList folders, IResource defRule )
        {
            if( defRule == null || IsSimpleExpirationRule( defRule ) )
            {
                EditExpirationRuleSimpleForm form = new EditExpirationRuleSimpleForm( folders, defRule );
                form.ShowDialog( Core.MainWindow );
                form.Dispose();
            }
            else
            {
                EditExpirationRuleForm form = new EditExpirationRuleForm( folders, defRule );
                form.ShowDialog( Core.MainWindow );
                form.Dispose();
            }
            return null;
        }

        public IResource   ShowExpirationRuleForm( IResource resType, IResource defRule, bool forDeletedItems )
        {
            if( defRule == null || IsSimpleExpirationRule( defRule ))
            {
                EditExpirationRuleSimpleForm form = new EditExpirationRuleSimpleForm( resType, defRule, forDeletedItems );
                form.ShowDialog( Core.MainWindow );
                form.Dispose();
            }
            else
            {
                EditExpirationRuleForm form = new EditExpirationRuleForm( resType, defRule, forDeletedItems );
                form.ShowDialog( Core.MainWindow );
                form.Dispose();
            }
            return null;
        }

        public static bool  IsSimpleExpirationRule( IResource rule )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilteringFormsManager -- Input rule resource is NULL." );

            if( !rule.HasProp( "IsExpirationFilter" ) )
                throw new ArgumentException( "FilteringFormsManager -- Input rule resource is not an Expiration rule." );
            #endregion Preconditions

            bool isStandard = true;
            IResourceList conditions = Core.FilterRegistry.GetConditionsPlain( rule ),
                          exceptions = Core.FilterRegistry.GetExceptions( rule ),
                          actions = Core.FilterRegistry.GetActions( rule );

            isStandard =               CheckExpirationConditions( conditions );
            isStandard = isStandard && CheckExpirationExceptions( exceptions );
            isStandard = isStandard && CheckExpirationActions( actions );
            isStandard = isStandard || rule.HasProp( "CountRestriction" );
            return isStandard;
        }

        private static bool  CheckExpirationConditions( IResourceList conditions )
        {
            return conditions.Count == 1 &&
                   conditions[ 0 ].GetStringProp( "Name" ) == FilterManagerStandards.DummyConditionName;
        }

        private static bool  CheckExpirationExceptions( IResourceList exceptions )
        {
            if( exceptions.IndexOf( Core.FilterRegistry.Std.ResourceIsFlagged ) != -1 )
            {
                exceptions = exceptions.Minus( Core.FilterRegistry.Std.ResourceIsFlagged.ToResourceList() );
                exceptions = exceptions.Minus( Core.FilterRegistry.Std.ResourceIsAnnotated.ToResourceList() );
            }
            exceptions = exceptions.Minus( Core.FilterRegistry.Std.ResourceIsCategorized.ToResourceList() );
            exceptions = exceptions.Minus( Core.FilterRegistry.Std.ResourceIsUnread.ToResourceList() );

            if( exceptions.Count == 1 )
            {
                IResource cond = exceptions[ 0 ];
                IResource template = cond.GetLinkProp( "TemplateLink" );
                if( template != null && template.Id == Core.FilterRegistry.Std.ReceivedInTheTimeSpanX.Id )
                {
                    string text = EditTimeSpanConditionForm.Condition2Text( cond );
                    string[] fields = text.Split( ' ' );
                    return (fields[ 0 ].ToLower() == "last");
                }
            }

            return false;
        }

        private static bool  CheckExpirationActions( IResourceList actions )
        {
            if( actions.Count == 0 )
                return false;

            actions = actions.Minus( Core.FilterRegistry.Std.DeleteResourceAction.ToResourceList() );
            actions = actions.Minus( Core.FilterRegistry.Std.MarkResourceAsReadAction.ToResourceList() );

            return actions.Count == 0;
        }
        #endregion Edit Expiration Rule
    }
}

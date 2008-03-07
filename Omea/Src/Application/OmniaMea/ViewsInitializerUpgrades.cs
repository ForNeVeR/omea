/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    public class Upgrade0ViewsInitializer : IViewsConstructor
    {
        public void  RegisterViewsFirstRun()
        {
            AscribeDeepNames();
            SetDefaultContactConfirmation();
            CheckMyselfLinkageWithAccounts();
        }        
        public void  RegisterViewsEachRun()
        {
            CheckUniquenessOfFromContact();
            RemoveObsoleteUndeletedRules();
        }

        private void  AscribeDeepNames()
        {
            IStandardConditions std = Core.FilterManager.Std;
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsUnreadName, std.ResourceIsUnreadNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsFlaggedName, std.ResourceIsFlaggedNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsAnnotatedName, std.ResourceIsAnnotatedNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsCategorizedName, std.ResourceIsCategorizedNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsAClippingName, std.ResourceIsAClippingNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, std.ResourceIsDeletedName, std.ResourceIsDeletedNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionResName, FilterManagerStandards.DummyConditionName, FilterManagerStandards.DummyConditionName );

            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.ResourceIsFlaggedWithFlagXName, std.ResourceIsFlaggedWithFlagXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SizeIsInTheIntervalXName, std.SizeIsInTheIntervalXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.ResourceBelongsToWorkspaceXName, std.ResourceBelongsToWorkspaceXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.FromContactXName, std.FromContactXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.ToContactXName, std.ToContactXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.CCContactXName, std.CCContactXNameDeep );

            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.BodyMatchesSearchQueryXName, std.BodyMatchesSearchQueryXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SubjectMatchSearchQueryXName, std.SubjectMatchSearchQueryXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SourceMatchSearchQueryXName, std.SourceMatchSearchQueryXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SubjectIsTextXName, std.SubjectIsTextXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SubjectContainsTextXName, std.SubjectContainsTextXNameDeep );

            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.InTheCategoryXName, std.InTheCategoryXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.SenderIsInTheCategoryXName, std.SenderIsInTheCategoryXNameDeep );
            AscribeDeepName( FilterManagerProps.ConditionTemplateResName, std.ReceivedInTheTimeSpanXName, std.ReceivedInTheTimeSpanXNameDeep );

            AscribeDeepName( FilterManagerProps.RuleActionResName, std.DeleteResourceActionName, std.DeleteResourceActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.DeleteResourcePermActionName, std.DeleteResourcePermActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.MarkResourceAsReadActionName, std.MarkResourceAsReadActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.MarkResourceAsUnreadActionName, std.MarkResourceAsUnreadActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.MarkResourceAsImportantActionName, std.MarkResourceAsImportantActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.ShowDesktopAlertActionName, std.ShowDesktopAlertActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionResName, std.ShowAsPlainTextActionName, std.ShowAsPlainTextActionNameDeep );

            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.AssignCategoryActionName, std.AssignCategoryActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.AssignCategoryToAuthorActionName, std.AssignCategoryToAuthorActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.PlaySoundFromFileActionName, std.PlaySoundFromFileActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.DisplayMessageBoxActionName, std.DisplayMessageBoxActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.RunApplicationActionName, std.RunApplicationActionNameDeep );
            AscribeDeepName( FilterManagerProps.RuleActionTemplateResName, std.MarkMessageWithFlagActionName, std.MarkMessageWithFlagActionNameDeep );
        }
        private static void AscribeDeepName( string type, string name, string deepName )
        {
            IResource res = Core.ResourceStore.FindUniqueResource( type, "Name", name );
            if( res != null )
                res.SetProp( "DeepName", deepName );
        }

        //---------------------------------------------------------------------
        //  Check whether there is more than one "From Contact" template resource
        //  (this is possible due to incorrect interversion transitions) and
        //  delete those which have no links to rules/views.
        //  NB: delete extra resources one by one.
        //---------------------------------------------------------------------
        private static void  CheckUniquenessOfFromContact()
        {
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ConditionTemplateResName, "DeepName", Core.FilterManager.Std.FromContactXNameDeep );
            if( list.Count > 1 )
            {
                if( list[ 0 ].GetLinksOfType( null, Core.FilterManager.Props.TemplateLink ).Count <
                    list[ 1 ].GetLinksOfType( null, Core.FilterManager.Props.TemplateLink ).Count )
                {
                    list[ 0 ].Delete();
                }
                else
                {
                    list[ 1 ].Delete();
                }
            }
        }

        private static void CheckMyselfLinkageWithAccounts()
        {
            IResource res = Core.ContactManager.MySelf.Resource;
            IResourceList accounts = res.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
            IResourceList cNames = res.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            IResourceList namesAccounts = Core.ResourceStore.EmptyResourceList;
            foreach( IResource cName in cNames )
            {
                namesAccounts = namesAccounts.Union( cName.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct ), true );
            }

            Trace.WriteLine( "ContactsUpgrade - " + accounts.Count + " normal accounts found: " );
            foreach( IResource acc in accounts )
                Trace.WriteLine( "              " + acc.DisplayName );

            Trace.WriteLine( "ContactsUpgrade - " + namesAccounts.Count + " ContactName accounts found: " );
            foreach( IResource acc in namesAccounts )
                Trace.WriteLine( "              " + acc.DisplayName );

            accounts = namesAccounts.Minus( accounts );

            Trace.WriteLine( "ContactsUpgrade - found " + accounts.Count + " hanged accounts." );
            foreach( IResource acc in accounts )
            {
                Trace.WriteLine( "          " + acc.DisplayName );
                res.AddLink( Core.ContactManager.Props.LinkEmailAcct, acc );
            }
        }

        #region RemoveObsoleteUndeletedRules

        //  From previous builds there may be several rules (of different type)
        //  which were not removed properly. All of them has a known name prefix
        //  and suffix. Remove them.

        private void  RemoveObsoleteUndeletedRules()
        {
            DeleteByName( "IsFormattingFilter" );
            DeleteByName( "IsTrayIconFilter" );
            DeleteByName( "IsActionFilter" );
        }
        private void  DeleteByName( string prop )
        {
            int  count = 0;
            IResourceList list;
            list = Core.ResourceStore.FindResourcesWithProp( null, prop );
            for( int i = 0; i < list.Count; i++ )
            {
                string name = list[ i ].GetStringProp( "Name" );
                if( name.StartsWith( "###-" ) && name.EndsWith( "-### " ) )
                {
                    list[ i ].Delete();
                    count++;
                }
            }
            Trace.WriteLine( "ViewsInitializer.1 -- " + count + " rules were removed of type [" + prop + "]" );
        }
        #endregion RemoveObsoleteUndeletedRules

        private static void  SetDefaultContactConfirmation()
        {
            ResourceDeleterOptions.SetConfirmDeleteToRecycleBin( "Contact", true );
        }
    }

    public class Upgrade1ViewsInitializer : IViewsConstructor
    {
        public void  RegisterViewsFirstRun()
        {
            UpgradeConditions();

            FixTemplateProducerLinks();
            FixQueryContainingViews();

            //  Fix ContactNames linkage only if all relevant resource types are
            //  already registered and have supporting plugins loaded.
            if( Core.ResourceStore.ResourceTypes.Exist( "Contact" ) &&
                Core.ResourceStore.ResourceTypes.Exist( "ContactName" ) &&
                Core.ResourceStore.ResourceTypes.Exist( "EmailAccount" ))
            {
                FixContactNames();
            }

            FixContentType();
            FixFlagsAndNamingForRules();
            FixHangedConditions();
            FixRuleEventNames();
            FixContactLinkageWithResourceTypes();

            SetDefaultPinning();
        }        
        public void  RegisterViewsEachRun()
        {
            CloneRulesToCompositeResType();
            FixValidButIgnoredContacts();
        }

        #region Version Upgrade Code
        private void  UpgradeConditions()
        {
            IResource res;
            IFilterManager fMgr = Core.FilterManager;  // alias
            IResourceStore Store = Core.ResourceStore; // alias

            #region Condition Renaming
            #region From Correspondent
            IResource newRes = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", Core.FilterManager.Std.FromContactXName );
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", "From %Correspondent(s)%" );
            if( res != null )
            {
                if ( newRes != null )
                {
                    newRes.Delete();
                }
                res.SetProp( "Name", Core.FilterManager.Std.FromContactXName );
                res.SetProp( "DeepName", Core.FilterManager.Std.FromContactXNameDeep );
                res.SetProp( "_DisplayName", Core.FilterManager.Std.FromContactXName.Replace( "%", "" ) );
            }
            #endregion From Correspondent

            #region To Correspondent
            newRes = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", Core.FilterManager.Std.ToContactXName );
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", "Sent to %Correspondent(s)%" );
            if( res != null )
            {
                Trace.WriteLine( "ViewsInitializer -- [Sent to Correspondent] has been found." );
                if ( newRes != null )
                {
                    Trace.WriteLine( "ViewsInitializer -- Occasional [To Contact] has been found." );
                    newRes.Delete();
                }
                res.SetProp( "Name", Core.FilterManager.Std.ToContactXName );
                res.SetProp( "DeepName", Core.FilterManager.Std.ToContactXNameDeep );
                res.SetProp( "_DisplayName", Core.FilterManager.Std.ToContactXName.Replace( "%", "" ) );
            }
            else
            if( newRes == null )
            {
                res = fMgr.CreateConditionTemplate( fMgr.Std.ToContactXName, fMgr.Std.ToContactXNameDeep, null, ConditionOp.In, "Contact", "To" );
                fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            }
            #endregion To Correspondent

            #region CC Correspondent
            newRes = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", Core.FilterManager.Std.CCContactXName );
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", "Copied (CC) to %Correspondent(s)%" );
            if( res != null )
            {
                if ( newRes != null )
                {
                    newRes.Delete();
                }
                res.SetProp( "Name", Core.FilterManager.Std.CCContactXName );
                res.SetProp( "DeepName", Core.FilterManager.Std.CCContactXNameDeep );
                res.SetProp( "_DisplayName", Core.FilterManager.Std.CCContactXName.Replace( "%", "" ) );
            }
            if( newRes == null )
            {
                res = fMgr.CreateConditionTemplate( fMgr.Std.CCContactXName, fMgr.Std.CCContactXNameDeep, null, ConditionOp.In, "Contact", "CC" );
                fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            }
            #endregion CC Correspondent

            #region From %Contact%
            fMgr.Std.FromContactX.SetProp( "_DisplayName", fMgr.Std.FromContactXName.Replace("%", "") );
            #endregion From %Contact%

            #region Mark Message as Y
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", "Mark message as read" );
            if( res != null )
            {
                IResource occ = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", fMgr.Std.MarkResourceAsReadActionName );
                if( occ != null )
                    occ.Delete();
                res.SetProp( "Name", fMgr.Std.MarkResourceAsReadActionName );
                res.SetProp( "DeepName", fMgr.Std.MarkResourceAsReadActionNameDeep );
            }
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", "Mark message as unread" );
            if( res != null )
            {
                IResource occ = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", fMgr.Std.MarkResourceAsUnreadActionName );
                if( occ != null )
                    occ.Delete();
                res.SetProp( "Name", fMgr.Std.MarkResourceAsUnreadActionName );
                res.SetProp( "DeepName", fMgr.Std.MarkResourceAsUnreadActionNameDeep );
            }
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", "Mark message as important" );
            if( res != null )
            {
                IResource occ = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", fMgr.Std.MarkResourceAsImportantActionName );
                if( occ != null )
                    occ.Delete();
                res.SetProp( "Name", fMgr.Std.MarkResourceAsImportantActionName );
                res.SetProp( "DeepName", fMgr.Std.MarkResourceAsImportantActionNameDeep );
            }
            #endregion Mark Message as Y
            #endregion Condition Renaming

            #region Views/Conditions which occasionally have been rotten in previous versions
            res = fMgr.CreateConditionTemplate( fMgr.Std.FromContactXName, fMgr.Std.FromContactXNameDeep, null, ConditionOp.In, "Contact", "From" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateConditionTemplate( fMgr.Std.SubjectContainsTextXName, fMgr.Std.SubjectContainsTextXNameDeep, null, ConditionOp.Has, "Subject" );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            #endregion

            #region Remove obsolete conditions and templates
            IResourceList list = Store.FindResources( FilterManagerProps.ConditionTemplateResName, "Name", "Contact has posted a message of type %Type%" );
            list.DeleteAll();

            list = Store.FindResources( FilterManagerProps.ConditionResName, "Name", "Reply in my thread" );
            list.DeleteAll();
            #endregion Remove obsolete conditions and templates

            #region Relink conditions to new group
            fMgr.AssociateConditionWithGroup( fMgr.Std.ToContactX, "Address and Contact Conditions" );
            fMgr.AssociateConditionWithGroup( fMgr.Std.CCContactX, "Address and Contact Conditions" );
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionGroupResName, "Name", "Address Conditions" );
            if( res != null )
                res.Delete();
            #endregion Relink conditions to new group
        }

        #region ContactNames Upgrade
        //---------------------------------------------------------------------
        //  Some previous build contained a bug which lead to ContactName
        //  resources explosion.
        //---------------------------------------------------------------------
        private void  FixContactNames()
        {
            //-- Part 1 -------------------------------------------------------
            Core.ResourceStore.ResourceTypes.Register( "ContactNameUpgradeFlag", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            IResource flag = Core.ResourceStore.FindUniqueResource( "ContactNameUpgradeFlag", "Name", "Done" );
            if( flag == null )
            {
                UpgradeContactNames();

                //  And remember the work is done.
                flag = Core.ResourceStore.NewResource( "ContactNameUpgradeFlag" );
                flag.SetProp( "Name", "Done" );
            }

            //-- Part 2 -------------------------------------------------------
            Core.ResourceStore.ResourceTypes.Register( "ContactNameUpgrade3Flag", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            flag = Core.ResourceStore.FindUniqueResource( "ContactNameUpgrade3Flag", "Name", "Done" );
            if( flag == null )
            {
                DeleteAllCNamesNotLinkedToAnyCorrespondence();

                //  And remember the work is done.
                flag = Core.ResourceStore.NewResource( "ContactNameUpgrade3Flag" );
                flag.SetProp( "Name", "Done" );
            }

            //-- Part 3 -------------------------------------------------------
            Core.ResourceStore.ResourceTypes.Register( "ContactNameUpgrade6Flag", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            flag = Core.ResourceStore.FindUniqueResource( "ContactNameUpgrade6Flag", "Name", "Done" );
            if( flag == null )
            {
                MergeContactNamesWithoutAccount();

                //  And remember the work is done.
                flag = Core.ResourceStore.NewResource( "ContactNameUpgrade6Flag" );
                flag.SetProp( "Name", "Done" );
            }
        }

        private static void UpgradeContactNames()
        {
            int  percent = 0;
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            for( int i = 0; i < contacts.Count; i++ )
            {
                Trace.WriteLine( "ViewsInitializer -- Upgrading contact names for contact: " + contacts[ i ].DisplayName );
                IResourceList accounts = contacts[ i ].GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
                foreach( IResource accnt in accounts )
                {
                    IResourceList cNames = contacts[ i ].GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
                    cNames = cNames.Intersect( accnt.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkEmailAcct ), true );

                    ProcessContactNamesList( cNames );
                }
                int newPercent = (int)( i * 100.0 / contacts.Count);
                if( newPercent != percent )
                {
                    percent = newPercent;
                    if( Core.ProgressWindow != null )
                        Core.ProgressWindow.UpdateProgress( percent, "Updating Contact Names pool from older builds...", null );
                }
            }
        }

        private static void ProcessContactNamesList( IResourceList cNames )
        {
            if( cNames.Count <= 1 )
                return;

            cNames.Sort( new SortSettings( Core.Props.Name, true ));

            IResource currentName = cNames[ 0 ];
            for( int i = 1; i < cNames.Count; i++ )
            {
                //  If we met the contact name with the same name - just remove
                //  it, and relink its corresponding item to the one single
                //  ContactName resource.
                if( cNames[ i ].GetStringProp( "Name" ) == currentName.GetStringProp( "Name" ) )
                {
                    IResource mail = null;
                    try
                    {
                        int  nameLinkId = ContactManager.GetLinkedIdFromContactName( cNames[ i ] );
                        mail = cNames[ i ].GetLinksOfType( null, nameLinkId )[ 0 ];
                        cNames[ i ].Delete();
                        mail.SetProp( nameLinkId, currentName );
                    }
                    catch( Exception )
                    {
                        Trace.WriteLine( "ViewsInitializer -- upgrading contact names - found a name, not linked to a primary resource." );
                    }
                }
                else
                    currentName = cNames[ i ];
            }
        }

        //---------------------------------------------------------------------
        //  Iterate over all ContactNames, count all "NameX" links (NameFrom,
        //  NameTo, NameCC). If cName is not linked to any correspondence
        //  resource, delete it.
        //---------------------------------------------------------------------
        private void  DeleteAllCNamesNotLinkedToAnyCorrespondence()
        {
            int removedCount = 0;
            IResourceList allCNames = Core.ResourceStore.GetAllResources( "ContactName" );
            for( int i = 0; i < allCNames.Count; i++ )
            {
                IResource name = allCNames[ i ];
                int linksCount = name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo ).Count + 
                                 name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom ).Count + 
                                 name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC ).Count;
                if( linksCount == 0 )
                {
                    removedCount++;
                    name.Delete();
                }
            }
            Trace.WriteLine( "ContactUpgrader -- DeleteAllCNamesNotLinkedToAnyCorrespondence found " + removedCount + " contact names not linked to any correspondence." );
        }

        //---------------------------------------------------------------------
        //  1. Collect all ContactNames from Myself which are NOT linked to
        //     any email account.
        //  2. Iterate over all such cNames, if a cName with such name string
        //     was already met, relink all dependant links to correspondence
        //     to that first cName.
        //---------------------------------------------------------------------
        private void  MergeContactNamesWithoutAccount()
        {
            int removedCount = 0;
            IContact myself = Core.ContactManager.MySelf;
            IResourceList linkedCNames = myself.Resource.GetLinksOfType( null, "BaseContact" );
            IResourceList properCNames = Core.ResourceStore.FindResourcesWithProp( "ContactName", Core.ContactManager.Props.LinkEmailAcct );
            linkedCNames = linkedCNames.Minus( properCNames );

            Hashtable cnames = new Hashtable();
            for( int i = 0; i < linkedCNames.Count; i++ )
            {
                IResource cName = linkedCNames[ i ];
                Debug.Assert( cName.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct ).Count == 0 );

                string name = cName.GetStringProp("Name");

                //  Empty names are removed unconditionally.
                if( name == null )
                {
                    linkedCNames[ i ].Delete();
                }
                else
                if( cnames.ContainsKey( name ))
                {
                    IResource existingCName = (IResource) cnames[ name ];

                    IResourceList linkedMails = cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo );
                    foreach( IResource mail in linkedMails )
                        mail.AddLink( Core.ContactManager.Props.LinkNameTo, existingCName );

                    linkedMails = cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom );
                    foreach( IResource mail in linkedMails )
                        mail.AddLink( Core.ContactManager.Props.LinkNameFrom, existingCName );

                    linkedMails = cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC );
                    foreach( IResource mail in linkedMails )
                        mail.AddLink( Core.ContactManager.Props.LinkNameCC, existingCName );

                    linkedCNames[ i ].Delete();
                }
                else
                    cnames[ cName.GetStringProp("Name") ] = linkedCNames[ i ];
            }
            Trace.WriteLine( "ContactUpgrader -- MergeContactNamesWithoutAccount removed " + removedCount + " contact names with equal name." );
        }
        #endregion ContactNames Upgrade

        #region TemplateProducerLinks
        //---------------------------------------------------------------------
        //  Link conditions and condition templates not by setting the name of
        //  the template as property but rather by direct link from condition
        //  to template.
        //---------------------------------------------------------------------
        private void  FixTemplateProducerLinks()
        {
            IResourceList list;
            if ( Core.ResourceStore.PropTypes.Exist( "TemplateProducer" ) )
            {
                list = Core.ResourceStore.GetAllResources( FilterManagerProps.ConditionResName );
                for( int i = 0; i < list.Count; i++ )
                {
                    string templateName = list[ i ].GetStringProp( "TemplateProducer" );
                    if( templateName != null )
                    {
                        IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", templateName );
                        list[ i ].SetProp( "TemplateLink", template );
                        list[ i ].DeleteProp( "TemplateProducer" );
                    }
                }

                list = Core.ResourceStore.GetAllResources( FilterManagerProps.RuleActionResName );
                for( int i = 0; i < list.Count; i++ )
                {
                    string templateName = list[ i ].GetStringProp( "TemplateProducer" );
                    if( templateName != null )
                    {
                        IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName, "Name", templateName );
                        list[ i ].SetProp( "TemplateLink", template );
                        list[ i ].DeleteProp( "TemplateProducer" );
                    }
                }
            }
        }
        #endregion TemplateProducerLinks

        #region IsQueryContained Prop
        //---------------------------------------------------------------------
        //  Set special property for Rules and Views which have conditions with
        //  Query operation.
        //---------------------------------------------------------------------
        private void  FixQueryContainingViews()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( FilterManagerProps.ViewResName ).Union(
                                 Core.ResourceStore.GetAllResources( FilterManagerProps.RuleResName ));
            foreach( IResource res in list )
            {
                IResourceList conds = Core.FilterManager.GetConditions( res ).Union( Core.FilterManager.GetExceptions( res ) );
                foreach( IResource cond in conds )
                {
                    if( cond.GetIntProp( "ConditionOp" ) == (int) ConditionOp.QueryMatch )
                    {
                        res.SetProp( "IsQueryContained", true );
                        break;
                    }
                }
            }
        }
        #endregion IsQueryContained Prop

        #region ContentType
        //---------------------------------------------------------------------
        //  ContentType "*" does not exist any more
        //  No more prop "ApplicableToType" - subst it with ContentType
        //---------------------------------------------------------------------
        private void  FixContentType()
        {
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "ContentType", "*" );
            foreach( IResource res in list )
            {
                res.DeleteProp( "ContentType" );
                Core.FilterManager.SetVisibleInAllTabs( res );
            }

            if( Core.ResourceStore.PropTypes.Exist( "ApplicableToType" ) )
            {
                list = Core.ResourceStore.FindResourcesWithProp( null, "ApplicableToType" );
                foreach( IResource res in list )
                {
                    res.SetProp( "ContentType", res.GetStringProp( "ApplicableToType") );
                    res.DeleteProp( "ApplicableToType" );
                }
            }
        }
        #endregion ContentType

        #region FlagsAndNamingForRules
        //---------------------------------------------------------------------
        //  Unify flags which define different type of rules.
        //---------------------------------------------------------------------
        private void  FixFlagsAndNamingForRules()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( FilterManagerProps.RuleResName );
            foreach( IResource rule in list )
            {
                if( !rule.HasProp( "IsExpirationFilter" ))
                    rule.SetProp( "IsActionFilter", true );
            }
            foreach( IResource rule in list )
            {
                if( rule.HasProp( "IsExpirationFilter" ))
                    rule.DeleteProp( "IsActionFilter" );
            }

            list = Core.ResourceStore.GetAllResources( FilterManagerProps.ViewResName );
            for( int i = 0; i < list.Count; i++ )
            {
                IResource rule = list[ i ];
                string    deepName = rule.GetStringProp( "DeepName" );
                if(( rule.HasProp( "IsFormattingFilter" ) || rule.HasProp( "IsTrayIconFilter" )) &&
                    rule.GetStringProp( "Name").EndsWith( "###") )
                {
                    int existCount = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "Name", deepName ).Count;
                    if( existCount == 0 )
                        rule.SetProp( "Name", deepName );
                    else
                        rule.Delete();
                }
            }
        }
        #endregion FlagsAndNamingForRules

        #region ContactLinkageWithResourceTypes
        private static void  FixContactLinkageWithResourceTypes()
        {
            Core.ResourceStore.ResourceTypes.Register( "FixContactLinkageFlag1", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            IResource flag = Core.ResourceStore.FindUniqueResource( "FixContactLinkageFlag1", "Name", "Done" );
            if( flag == null )
            {
                int percent = 0;
                IResource  mailRT = null, newsRT = null;
                if( Core.ResourceStore.ResourceTypes.Exist( "Email" ) )
                    mailRT = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "Email" );
                if( Core.ResourceStore.ResourceTypes.Exist( "Article" ) )
                    newsRT = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "Article" );

                if( mailRT != null || newsRT != null )
                {
                    IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
                    for( int i = 0; i < contacts.Count; i++ )
                    {
                        if( mailRT != null &&
                            contacts[ i ].GetLinksOfType( "Email", Core.ContactManager.Props.LinkFrom ).Count > 0 )
                        {
                            contacts[ i ].SetProp( Core.ContactManager.Props.LinkLinkedOfType, mailRT );
                        }
                        if( newsRT != null &&
                            contacts[ i ].GetLinksOfType( "Article", Core.ContactManager.Props.LinkFrom ).Count > 0 )
                        {
                            contacts[ i ].SetProp( Core.ContactManager.Props.LinkLinkedOfType, newsRT );
                        }

                        if( i * 100 / contacts.Count != percent )
                        {
                            percent = i * 100 / contacts.Count;
                            if( Core.ProgressWindow != null )
                                Core.ProgressWindow.UpdateProgress( percent, "Upgrading Contact information...", null );
                        }
                    }
                }
                //  And remember the work is done.
                flag = Core.ResourceStore.NewResource( "FixContactLinkageFlag1" );
                flag.SetProp( "Name", "Done" );
            }
        }
        #endregion ContactLinkageWithResourceTypes

        #region Hanged Conditions
        private static void  FixHangedConditions()
        {
            IResourceList list = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.ConditionResName, "InternalView" );
            for( int i = 0; i < list.Count; i++ )
            {
                if( list[ i ].GetLinksOfType( null, "LinkedCondition" ).Count == 0 &&
                    list[ i ].GetLinksOfType( null, "LinkedNegativeCondition" ).Count == 0 )
                    list[ i ].Delete();
            }
        }
        #endregion Hanged Conditions

        #region Rule Event Names
        private static void  FixRuleEventNames()
        {
            IResourceList rules = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.RuleResName, "ActionActivationTime" );
            for( int i = 0; i < rules.Count; i++ )
            {
                rules[ i ].SetProp( "EventName", StandardEvents.ResourceReceived );
                rules[ i ].DeleteProp( "ActionActivationTime" );
            }
        }
        #endregion Rule Event Names

        #region Clone Tray and Formatting rules to ViewComposite type
        private static void  CloneRulesToCompositeResType()
        {
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "IsTrayIconFilter", true );
            foreach( IResource res in list )
            {
                string name = res.GetStringProp( Core.Props.Name );
                if( Core.TrayIconManager.FindRule( name ) == null )
                {
                    try
                    {
                        Core.TrayIconManager.CloneRule( res, name );
                    }
                    catch( Exception )
                    {
                        //  We may fail to convert old or broken rules for which
                        //  proper transition has not been made. Just ignore them.
                    }
                }
            }
            list.DeleteAll();

            list = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "IsFormattingFilter", true );
            foreach( IResource res in list )
            {
                string name = res.GetStringProp( Core.Props.Name );
                if( Core.FormattingRuleManager.FindRule( name ) == null )
                {
                    try
                    {
                        Core.FormattingRuleManager.CloneRule( res, name );
                    }
                    catch( Exception )
                    {
                        //  We may fail to convert old or broken rules for which
                        //  proper transition has not been made. Just ignore them.
                    }
                }
            }
            list.DeleteAll();
        }
        #endregion Clone Tray and Formatting rules to ViewComposite type

        #region SetDefaultPinning
        //  Set some conditions/templates as default for particular view/rules:
        //  - Advanced Search dialog: "From Correspondent" and "Received within time span"
        //  - TrayIcon rules: "Resource is unread"

        private static void  SetDefaultPinning()
        {
            Core.FilterManager.Std.ReceivedInTheTimeSpanX.SetProp( "IsAdvSearchLinked", true );
            Core.FilterManager.Std.FromContactX.SetProp( "IsAdvSearchLinked", true );
            Core.FilterManager.Std.ResourceIsUnread.SetProp( "IsTrayRuleLinked", true );
        }
        #endregion SetDefaultPinning

        private static void FixValidButIgnoredContacts()
        {
            IResourceStore store = Core.ResourceStore;
            IResourceList ignored = store.FindResourcesWithProp( "Contact", "IsIgnored" );
            ignored = ignored.Minus( store.FindResourcesWithProp( "Contact", Core.Props.IsDeleted ) );
            Trace.WriteLine( "Upgrade1Initializer -- Found " + ignored.Count + " ignored but non-deleted contacts" );
            for( int i = 0; i < ignored.Count; i++ )
            {
                ignored[ i ].DeleteProp( "IsIgnored" );
            }
        }
        #endregion Version Upgrade Code
    }

    public class Upgrade2ViewsInitializer : IViewsConstructor
    {
        public void  RegisterViewsFirstRun()
        {
            IFilterManager fMgr = Core.FilterManager;
            IStandardConditions std = fMgr.Std;

            string  replyName = Core.ResourceStore.PropTypes[ Core.Props.Reply ].Name;

            //  We check for a link of reversed direction
            Core.FilterManager.CreateStandardCondition( std.MessageHasReplyName, std.MessageHasReplyDeep,
                                                        null, "-" + replyName, ConditionOp.HasLink );

            replyName = Core.ResourceStore.PropTypes[ Core.Props.Reply ].Name;
            Core.FilterManager.CreateStandardCondition( std.MessageIsAReplyName, std.MessageIsAReplyDeep,
                                                        null, replyName, ConditionOp.HasLink );
        }
        public void  RegisterViewsEachRun()
        {}
    }

    public class Upgrade3ViewsInitializer : IViewsConstructor
    {
        public void  RegisterViewsFirstRun()
        {
            RedirectLinks( FilterManagerProps.RuleResName );
            RedirectLinks( FilterManagerProps.ViewResName );
            RedirectLinks( FilterManagerProps.ViewCompositeResName );
        }
        public void  RegisterViewsEachRun()
        {}

        private static void RedirectLinks( string resName )
        {
            IResourceList list = Core.ResourceStore.GetAllResources( resName );
            foreach( IResource rule in list )
            {
                IResourceList conds = Core.FilterManager.GetConditions( rule );
                if( conds.Count > 0 )
                {
                    IResource group = Core.ResourceStore.NewResource( FilterManagerProps.ConjunctionGroup );
                    rule.DeleteLinks( Core.FilterManager.Props.LinkedConditions );

                    rule.SetProp( Core.FilterManager.Props.LinkedConditions, group );
                    foreach( IResource cond in conds )
                        group.AddLink( Core.FilterManager.Props.LinkedConditions, cond );
                }
            }
        }
    }
}
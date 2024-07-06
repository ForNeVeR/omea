// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Diagnostics;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ContactsPlugin
{
    public class ContactsViewsConstructor : IViewsConstructor
    {
        public const string  ContactNotInABName = "Contact is not in any address book";
        public const string  ContactNotInABNameDeep = "Contactnotinab";
        public const string  ContactInABName = "Contact is in %address book%";
        public const string  ContactInABNameDeep = "Contactinab";
        public const string  LastCorrespondenceName = "Last correspondence is received during %period%";
        public const string  LastCorrespondenceNameDeep = "lastdated";
        public const string  ContactHasCorrespondenceName = "Contact has correspondence";
        public const string  ContactHasCorrespondenceNameDeep = "hascorrespondence";

        public const string  AddContact2ABName = "Add a message's contact to an %Address Book%";
        public const string  AddContact2ABNameDeep = "contact2ab";

        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            string[]        applType = new string[ 1 ] { "Contact" };
            IResource       res;
            IFilterRegistry  fMgr = Core.FilterRegistry;

            //  Conditions/Templates
            res = fMgr.CreateConditionTemplate( ContactInABName, ContactInABNameDeep, applType, ConditionOp.In, "AddressBook", "InAddressBook" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            res = fMgr.CreateStandardCondition( ContactNotInABName, ContactNotInABNameDeep, applType, "InAddressBook", ConditionOp.HasNoProp );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateConditionTemplate( LastCorrespondenceName, LastCorrespondenceNameDeep, applType, ConditionOp.Eq, "LastCorrespondDate" );
            fMgr.AssociateConditionWithGroup( res, "Temporal Conditions" );
            IResource condActive = FilterConvertors.InstantiateTemplate( res, "last 30 days", null );

            //  Views
            IResource viewAll = fMgr.RegisterView( "All", applType, (IResource[]) null, null);
            IResource viewActive = fMgr.RegisterView( "Active", applType, new IResource[ 1 ] { condActive }, null);
            viewAll.SetProp( "DefaultSort", "LastName FirstName" );
            viewActive.SetProp( "DefaultSort", "LastName FirstName" );
            Core.ResourceTreeManager.LinkToResourceRoot( viewAll, 10 );
            Core.ResourceTreeManager.LinkToResourceRoot( viewActive, 11 );

            viewAll.SetProp( "DisableDefaultGroupping", true );
            viewActive.SetProp( "DisableDefaultGroupping", true );
        }

        public void RegisterViewsEachRun()
        {
            Core.NotificationManager.RegisterNotifyMeResourceType( "Contact", null );
            Core.NotificationManager.RegisterNotifyMeCondition( "Contact", Core.FilterRegistry.Std.FromContactX, 0 );

            Core.FilterRegistry.RegisterRuleActionTemplate( AddContact2ABName, AddContact2ABNameDeep,
                                                           new ContactsPlugin.AddContactToABAction(), ConditionOp.In, "AddressBook" );

            IResource res = Core.FilterRegistry.RegisterCustomCondition( ContactHasCorrespondenceName, ContactHasCorrespondenceNameDeep,
                                                                        new string[] { "Contact" }, new ContactHasCorrespondenceCondition() );
            Core.FilterRegistry.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            IResource defltView = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, "Active" );
            Core.TabManager.SetDefaultSelectedResource( "Contacts", defltView );
        }
        #endregion
    }

    public class ContactsUpgrade1ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            IResource res;

            //  Correct view "Active". LastCorrespondDate must fall into
            //  the range ["Tomorrow", -30].
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Active" );
            if( res != null )
            {
                IResourceList conditions = res.GetLinksOfType( null, "LinkedCondition" );
                if( conditions.Count == 1 )
                {
                    conditions[ 0 ].SetProp( "ConditionValLower", "Tomorrow" );
                }
            }

            //-----------------------------------------------------------------
            //  All conditions, templates and actions must have their deep names
            //-----------------------------------------------------------------
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", ContactsViewsConstructor.ContactNotInABName );
            if( res != null )
                res.SetProp( "DeepName", ContactsViewsConstructor.ContactNotInABNameDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", ContactsViewsConstructor.ContactInABName );
            if( res != null )
                res.SetProp( "DeepName", ContactsViewsConstructor.ContactInABNameDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", ContactsViewsConstructor.LastCorrespondenceName );
            if( res != null )
                res.SetProp( "DeepName", ContactsViewsConstructor.LastCorrespondenceNameDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName, "Name", ContactsViewsConstructor.AddContact2ABName );
            if( res != null )
                res.SetProp( "DeepName", ContactsViewsConstructor.AddContact2ABNameDeep );
        }

        public void RegisterViewsEachRun()
        {}
        #endregion
    }

    //-------------------------------------------------------------------------
    //  On upgrade, all accounts are considered personal by default. It is up to
    //  user how to define the actual personality of each of the accounts lately.
    //-------------------------------------------------------------------------
    public class ContactsUpgrade2ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            IResourceList accounts = Core.ResourceStore.GetAllResources( "EmailAccount" );
            for( int i = 0; i < accounts.Count; i++ )
            {
                accounts[ i ].SetProp( Core.ContactManager.Props.PersonalAccount, true );
            }
        }

        public void RegisterViewsEachRun()
        {
            IResource view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "All" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );

            view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", "Active" );
            if( view != null )
                view.SetProp( "DisableDefaultGroupping", true );
        }
        #endregion
    }

    public class ContactsUpgrade3ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            int  count = 0, illegallyNamedCount = 0;
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            IProgressWindow wnd = Core.ProgressWindow;
            if( wnd != null )
            {
                wnd.UpdateProgress( 0, "Upgrading Contact Names information.", null );
            }
            ContactManager.UnlinkIdenticalContactNames( contacts, wnd, ref count, ref illegallyNamedCount );
            Trace.WriteLine( "ContactsUpgrade3ViewsConstructor (RegisterViewsFirstRun) -- " + count + " completely unnecessary contact names removed, of that - " +
                             illegallyNamedCount + " illegally named Conact Names" );
        }

        public void RegisterViewsEachRun()
        {}
        #endregion
    }

    #region Condition and Actions
    internal class ContactHasCorrespondenceCondition : ICustomCondition
    {
        public bool  MatchResource( IResource res )
        {
            //  Do not use obvious solution -
            //  IResourceList linked = ContactManager.LinkedCorrespondence( res );
            //  Instead, check linkage in a more directed manner.

            IResourceList linked = res.GetLinksTo( null, Core.ContactManager.Props.LinkFrom );
            if( linked.Count == 0 )
            {
                linked = res.GetLinksTo( null, Core.ContactManager.Props.LinkTo );
                if( linked.Count == 0 )
                {
                    linked = res.GetLinksTo( null, Core.ContactManager.Props.LinkCC );
                }
            }
            return (linked.Count > 0);
        }

        public IResourceList  Filter( string resType )
        {
            IResourceStore  store = Core.ResourceStore;
            IResourceList   contacts = store.FindResourcesWithProp( "Contact", -Core.ContactManager.Props.LinkFrom );
            contacts = contacts.Union( store.FindResourcesWithProp( "Contact", -Core.ContactManager.Props.LinkTo ), true );
            contacts = contacts.Union( store.FindResourcesWithProp( "Contact", -Core.ContactManager.Props.LinkCC ), true );

            return contacts;
        }
    }
    #endregion Condition and Actions
}

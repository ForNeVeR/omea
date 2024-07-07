// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Interface which allows to disable the merge operation for groups of contacts
    /// which match a certain condition.
    /// </summary>
    /// <since>1.0.3</since>
    public interface IContactMergeFilter
    {
        /// <summary>
        /// Checks if the specified list of contacts can be merged.
        /// </summary>
        /// <param name="contacts">The list of contacts selected by the user
        /// for merging.</param>
        /// <returns>null if the merge is allowed, or an error message string if the
        /// merge is not allowed.</returns>
        string CheckMergeAllowed( IResourceList contacts );
    }

	/// <summary>
	/// Manages various core operations with contacts.
	/// </summary>
	public interface IContactManager
	{
        /// <summary>
        /// Creates a new contact with the specified name fields.
        /// </summary>
        /// <returns>The wrapper for the contact resource.</returns>
        /// <remarks>Method does not check the existance of the contact with
        /// the same values of the name fields and creates other resource</remarks>
        IContact CreateContact( string title, string firstName, string midName,
                                string lastName, string suffix );

        /// <summary>
        /// Creates a new contact with the specified name fields.
        /// </summary>
        /// <returns>The wrapper for the contact resource, or null if given full name is null, empty or
        /// consists of delimiters only.</returns>
        /// <remarks>1. Method does not check the existance of the contact with
        /// the same values of the name fields and creates other resource.
        /// 2. The sender name is automatically split into the title, first name, middle name, last name and suffix.
        /// The heuristics to do so may not always be correct, so if you know exactly
        /// name fields of a contact, it is strongly recommended to use the
        /// <see cref="CreateContact(string, string, string, string, string)">five-parameter overload</see>.</remarks>
        IContact CreateContact( string fullName );

        /// <summary>
        /// Method tries to find an empty contact linked to the given account and update its fields.
        /// Otherwise new contact (possibly empty) is created with the specified name fields.
        /// </summary>
        /// <returns>The wrapper for the contact resource.</returns>
        /// <remarks>1. Method does not link email account resource with the newly created contact.
        /// 2. Email account must be not NULL</remarks>
        IContact CreateOrUpdateContact( IResource emailAcc, string title, string firstName, string midName,
                                                            string lastName, string suffix );

        /// <summary>
        /// Method tries to find an empty contact linked to the given account and update its fields.
        /// Otherwise new contact (possibly empty) is created with the specified name fields.
        /// </summary>
        /// <returns>The wrapper for the contact resource.</returns>
        /// <remarks>1. Method does not link email account resource with the newly created contact.
        /// 2. Email account must be not NULL.
        /// 3. The sender name is automatically split into the title, first name, middle name, last name and suffix.
        /// The heuristics to do so may not always be correct, so if you know exactly
        /// name fields of a contact, it is strongly recommended to use the
        /// <see cref="CreateOrUpdateContact(IResource, string, string, string, string, string)">six-parameter overload</see>.</remarks>
        /// <since>1.0.3</since>
        IContact CreateOrUpdateContact( IResource emailAcc, string fullName );

        /// <summary>
        /// Locates an existing contact with the specified name.
        /// </summary>
        /// <param name="senderName">Full name of a contact.</param>
        /// <returns>The wrapper for the contact resource, null if no contact is found.</returns>
        /// <remarks>The sender name is automatically split into the first name and last name.
        /// The heuristics to do so may not always be correct, so if you know exactly
        /// name fields of a contact, it is strongly recommended to use the
        /// <see cref="FindContact(string, string, string, string, string)">six-parameter overload</see>.</remarks>
        IContact FindContact( string senderName );

        /// <summary>
        /// Locates an existing contact with the specified name.
        /// </summary>
        /// <returns>The wrapper for the contact resource, null if no contact is found.</returns>
        IContact FindContact( string title, string firstName, string midName,
                              string lastName, string suffix );

        /// <summary>
        /// Find all contacts matching the given full name.
        /// </summary>
        /// <returns>List of matching contact resources.</returns>
        IResourceList FindContactList( string fullName );

        /// <summary>
        /// Find all contacts matching the given the set of name fields.
        /// </summary>
        /// <returns>List of matching contact resources.</returns>
        IResourceList FindContactList( string title, string firstName, string midName,
                                       string lastName, string suffix );
        /// <summary>
        /// Locates an existing contact or creates a new contact with the specified name
        /// and e-mail address.
        /// </summary>
        /// <param name="email">The e-mail address of the contact to find or create.</param>
        /// <param name="senderName">The name of the contact to find or create.</param>
        /// <returns>The wrapper for the contact resource.</returns>
        /// <remarks>The sender name is automatically split into the first name and last name.
        /// The heuristics to do so may not always be correct, so if you know exactly
        /// the first and last names of a contact, it is strongly recommended to use the
        /// <see cref="FindOrCreateContact(string, string, string)">three-parameter overload.</see></remarks>
        IContact FindOrCreateContact( string email, string senderName );

        /// <summary>
        /// Locates an existing contact or creates a new contact with the specified first
        /// name, last name and e-mail address.
        /// </summary>
        /// <param name="email">The e-mail address of the contact to find or create.</param>
        /// <param name="firstName">The first name of the contact to find or create.</param>
        /// <param name="lastName">The last name of the contact to find or create.</param>
        /// <returns>The wrapper for the contact resource.</returns>
        /// <see cref="FindOrCreateContact(string, string, string, string, string, string)">Six-parameter
        /// overload (with full set of name fields).</see>
        IContact FindOrCreateContact( string email, string firstName, string lastName );

        /// <summary>
        /// Locates an existing contact or creates a new contact with the specified set
        /// of name fields and e-mail address.
        /// </summary>
        /// <param name="email">The e-mail address of the contact to find or create.</param>
        /// <param name="title">The title of the contact to find or create.</param>
        /// <param name="firstName">The first name of the contact to find or create.</param>
        /// <param name="midName">The middle name of the contact to find or create.</param>
        /// <param name="lastName">The last name of the contact to find or create.</param>
        /// <param name="suffix">The suffix of the contact to find or create.</param>
        /// <returns>The wrapper for the contact resource.</returns>
        IContact FindOrCreateContact( string email, string title, string firstName,
                                      string midName, string lastName, string suffix );

        /// <summary>
        /// Adds the specified name and e-mail address to the MySelf contact (the contact
        /// representing the current user of Omea), and returns that contact.
        /// </summary>
        /// <param name="email">The e-mail address to use for the MySelf contact.</param>
        /// <param name="senderName">The name to use for the MySelf contact.</param>
        /// <returns>The wrapper for the MySelf contact.</returns>
        /// <see cref="FindOrCreateMySelfContact(string, string, string, string, string, string )">seven-parameter overload</see>
        IContact FindOrCreateMySelfContact( string email, string senderName );

        /// <summary>
        /// Adds the specified name and e-mail address to the MySelf contact (the contact
        /// representing the current user of Omea), and returns that contact.
        /// </summary>
        /// <returns>The wrapper for the MySelf contact.</returns>
        /// <since>1.0.3</since>
        IContact FindOrCreateMySelfContact( string email, string title, string firstName,
                                            string midName, string lastName, string suffix );

        /// <summary>
        /// Parse name of a contact and split it into several structural parts.
        /// </summary>
        /// <param name="fullName">Name of a contact (correspondent).</param>
        /// <param name="emailAccount">Email account of a contact (may be null).</param>
        /// <param name="title">Title of a contact.</param>
        /// <param name="firstName">First name of a contact.</param>
        /// <param name="midName">Middle name of a contact.</param>
        /// <param name="lastName">Last name of a contact.</param>
        /// <param name="suffix">Suffix of a contact.</param>
        /// <returns>Whether method managed to parse a name.</returns>
        bool    ResolveName( string fullName, string emailAccount,
                             out string title, out string firstName,
                             out string midName, out string lastName, string suffix );

        /// <summary>
        /// Locates an existing e-mail account resource or creates a new one for the specified
        /// e-mail address.
        /// </summary>
        /// <param name="email">The e-mail address for which the account is created.</param>
        /// <returns>The resource of type EmailAccount.</returns>
        IResource FindOrCreateEmailAccount( string email );

        /// <summary>
        /// Locates an existing mailing list resource or creates a new one with the specified
        /// e-mail address.
        /// </summary>
        /// <param name="email">The e-mail address of the mailing list.</param>
        /// <returns>The resource of type MailingList.</returns>
        IResource FindOrCreateMailingList( string email );

        /// <summary>
        /// Merges contacts in the input list with the resulting fullName name.
        /// All links from all input contacts are retargeted to the result contact.
        /// </summary>
        /// <param name="fullName">New name for the resulting contact</param>
        /// <param name="contacts">Source contacts for merging.</param>
        /// <returns>Resource of the resulting contact.</returns>
        IResource Merge( string fullName, IResourceList contacts );

        /// <summary>
        /// Splits previously merged contact into original ones.
        /// </summary>
        /// <param name="contact">The contact to be split.</param>
        /// <returns>A list of contact resources from which merged contact was created.</returns>
        IResourceList  Split( IResource contact );

        /// <summary>
        /// Splits previously merged contact into several original ones, given in
        /// the second parameter contactToExtract. If not all subcontacts are to
        /// be extracted from the base one, others are still kept under the the base
        /// contact.
        /// </summary>
        /// <param name="contact">The contact to be split.</param>
        /// <param name="contactToExtract">The list of contacts (represented by the corresponding
        /// wrappers) to be extracted and represented as standalone resources.</param>
        /// <returns>A list of contact resources from which merged contact was created.
        ///  Source contact is also included into the final list.</returns>
        IResourceList  Split( IResource contact, IResourceList contactToExtract );

        /// <summary>
        /// Performs basic operations on linking between e.g. mail and a contact:
        /// bind a mail and a contact with "From", "To" or "CC" link; create a
        /// new object "ContactName" and bind a mail and CN with "NameFrom",
        /// "NameTo" or "NameCC" link, and finally link CN and its base contact
        /// object
        /// </summary>
        /// <param name="propId">Id of link between a mail and a contact</param>
        /// <param name="contact">Base contact</param>
        /// <param name="mail">A resource to be linked (mail, news article etc.)</param>
        /// <param name="account">Mail account of a mail.</param>
        /// <param name="senderName">Name which will be shown to the user.</param>
        void LinkContactToResource( int propId, IResource contact, IResource mail,
                                    IResource account, string senderName );

        /// <summary>
        /// Performs basic operations on linking between e.g. mail and a contact:
        /// bind a mail and a contact with "From", "To" or "CC" link; create a
        /// new object "ContactName" and bind a mail and CN with "NameFrom",
        /// "NameTo" or "NameCC" link, and finally link CN and its base contact
        /// object
        /// </summary>
        /// <param name="propId">Id of link between a mail and a contact</param>
        /// <param name="contact">Base contact</param>
        /// <param name="mail">A resource to be linked (mail, news article etc.)</param>
        /// <param name="account">String representation of a mail account.</param>
        /// <param name="senderName">Name which will be shown to the user.</param>
        void LinkContactToResource( int propId, IResource contact, IResource mail,
                                    string account, string senderName );

        /// <summary>
        /// Delete a contact given its resource.
        /// </summary>
        /// <param name="contact">Resource representing a contact.</param>
        /// <param name="ignoreContactLater">If contact is to be deleted non-permanently
        /// this parameter prevents from "recreating" this contact again if
        /// new correspondence comes from it.</param>
        /// <param name="errMessage">If method fails to delete a contact, this parameter
        /// contains an error message describing the reason.</param>
        /// <since>1.0.3</since>
        void  DeleteContact( IResource contact, bool ignoreContactLater, out string errMessage );

        /// <summary>
        /// Check whether the input resource (e.g. mail, article or rss post) is linked
        /// to a contact information (Contact, ContactName and EmailAccount) and removes
        /// these links for it (e.g. before a resource deletion).
        /// </summary>
        /// <param name="res">A resource for cleanup.</param>
        /// <since>2.0</since>
        void  UnlinkContactInformation( IResource res );

        /// <summary>
        /// Return the complete (full) name of the contact represented by the given resource.
        /// </summary>
        /// <param name="res">A resource from properties of which the full name is to be constructed.</param>
        /// <returns>Full name of a contact.</returns>
        string  GetFullName( IResource res );

        /// <summary>
        /// Create IContact wrapper around IResource object.
        /// </summary>
        /// <param name="res">A resource representing Contact.</param>
        /// <returns>IContact wrapper.</returns>
        IContact GetContact( IResource res );

        /// <summary>
        /// Scans the specified list for the contacts which were not created by the user
        /// and which don't have any message links and deletes those contacts.
        /// </summary>
        /// <param name="contacts">The list of contacts to scan for unused ones.</param>
        void DeleteUnusedContacts( IResourceList contacts );

        /// <summary>
        /// Registers the filter which will be used for checking if specified
        /// lists of contacts can be merged.
        /// </summary>
        /// <param name="filter">The filter which is registered.</param>
        /// <since>1.0.3</since>
        void RegisterContactMergeFilter( IContactMergeFilter filter );

        /// <summary>
        /// Returns the list of registered contact merge filters.
        /// </summary>
        /// <returns>The array of merge filters.</returns>
        /// <since>1.0.3</since>
        IContactMergeFilter[] GetContactMergeFilters();

        /// <summary>
        /// Returns the contact representing the owner user of the program.
        /// </summary>
        IContact MySelf { get; }

        /// <summary>
        /// IDs of standard property types related to contacts.
        /// </summary>
        /// <since>2.0</since>
        IContactManagerProps Props { get; }
	}

    /// <summary>
    /// Defines the IDs of standard property types related to contacts.
    /// </summary>
    /// <since>2.0</since>
    public interface IContactManagerProps
    {
        /// <summary>
        /// ID of the "From" property type.
        /// </summary>
        int LinkFrom { get; }

        /// <summary>
        /// ID of the "To" property type.
        /// </summary>
        int LinkTo { get; }

        /// <summary>
        /// ID of the "CC" property type.
        /// </summary>
        int LinkCC { get; }

        /// <summary>
        /// ID of the "EmailAcct" property type, links a Contact (ContactName)
        /// and EmailAccount resources.
        /// </summary>
        int LinkEmailAcct { get; }

        /// <summary>
        /// ID of the "EmailAccountFrom" property type, links a mail
        /// and EmailAccount resources.
        /// </summary>
        int LinkEmailAcctFrom{  get; }

        /// <summary>
        /// ID of the "EmailAccountTo" property type, links a mail
        /// and EmailAccount resources.
        /// </summary>
        int LinkEmailAcctTo  {  get; }

        /// <summary>
        /// ID of the "EmailAccountCC" property type, links a mail
        /// and EmailAccount resources.
        /// </summary>
        int LinkEmailAcctCC  {  get; }

        /// <summary>
        /// ID of the "EmailAddress" property type.
        /// </summary>
        int EmailAddress { get; }

        /// <summary>
        /// ID of the "UserAccount" property type.
        /// </summary>
        int UserName     { get; }

        /// <summary>
        /// ID of the "Domain" property type.
        /// </summary>
        int Domain       { get; }

        /// <summary>
        /// ID of the "IsGroupAccout" property type. If property is set
        /// then the account is possibly a group account and must be resolved
        /// strictly.
        /// </summary>
        int PersonalAccount {  get; }

        /// <summary>
        /// ID of the "ShowOriginalNames" property type. If property is set
        /// then user will see not the name of a conact but one of its contact name
        /// (ContactName resource) in resource list, links pane and links bar.
        /// </summary>
        int ShowOriginalNames {  get; }

        /// <summary>
        /// ID of the "NameFrom" property type. Connects a resource and a ContactName.
        /// </summary>
        int LinkNameFrom    { get; }

        /// <summary>
        /// ID of the "NameTo" property type. Connects a resource and a ContactName.
        /// </summary>
        int LinkNameTo      { get; }

        /// <summary>
        /// ID of the "NameCC" property type. Connects a resource and a ContactName.
        /// </summary>
        int LinkNameCC      { get; }

        /// <summary>
        /// ID of the "BaseContact" property type. Connects a ContactName and a Contact.
        /// </summary>
        int LinkBaseContact { get; }

        /// <summary>
        /// ID of the "LinkedResourcesOfType" property type. Connects a Contact and
        /// Resource Type resources if there was at least one link between a Contact
        /// and a correspondence of that resource type.
        /// </summary>
        int LinkLinkedOfType {  get; }

        /// <summary>
        /// ID of the IsIgnored property.
        /// </summary>
        int Ignored { get; }

        /// <summary>
        /// ID of the Myself property. Indicates that a contact represents a
        /// contact of the Omea owner.
        /// </summary>
        int Myself { get; }

        /// <summary>
        /// ID of the LastCorrespondDate property. Property belongs to the Contact resource and indicates
        /// a date of the last correspondence item from that contact.
        /// </summary>
        int LastCorrespondenceDate { get; }

        /// <summary>
        /// ID of the Picture property. Keeps the image thumbnail aligned inside the
        /// box 48x48.
        /// </summary>
        int Picture { get; }

        /// <summary>
        /// ID of the ContactOriginalPicture property. Keeps the original image.
        /// </summary>
        int PictureOriginal { get; }
    }

    /// <summary>
    /// Provides convenience wrappers for working with a Contact resource.
    /// </summary>
    public interface IContact
    {
        /// <summary>
        /// Gets the resource representing the contact.
        /// </summary>
        IResource Resource { get; }

        /// <summary>
        /// Gets or sets the title of the contact.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Gets or sets the first name of the contact.
        /// </summary>
        string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the middle name of the contact.
        /// </summary>
        string MiddleName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the contact.
        /// </summary>
        string LastName { get; set; }

        /// <summary>
        /// Gets or sets the suffix of the contact.
        /// </summary>
        string Suffix { get; set; }

        /// <summary>
        /// Gets or sets the homepage of the contact.
        /// </summary>
        string HomePage { get; set; }

        /// <summary>
        /// Gets or sets the address of the contact.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        /// Gets or sets the company of the contact.
        /// </summary>
        string Company { get; set; }

        /// <summary>
        /// Gets or sets the job title of the contact.
        /// </summary>
        string JobTitle { get; set; }

        /// <summary>
        /// Gets or sets the birthday of the contact.
        /// </summary>
        DateTime Birthday { get; set; }

        /// <summary>
        /// Gets the default e-mail address of the contact.
        /// </summary>
        string DefaultEmailAddress { get; }

        /// <summary>
        /// Gets or sets a string of a description of the contact.
        /// Description string is in the rich-text format.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the date of the last correspondence with a contact.
        /// </summary>
        /// <remarks>The date is used to determine which contacts should be shown in the
        /// Active view.</remarks>
        DateTime LastCorrespondDate { get; set; }

        /// <summary>
        /// Sets the name fields of the contact to specified values.
        /// </summary>
        /// <param name="title">The title of the contact.</param>
        /// <param name="firstName">The first name of the contact.</param>
        /// <param name="midName">The middle name of the contact.</param>
        /// <param name="lastName">The last name of the contact.</param>
        /// <param name="suffix">The suffix of the contact.</param>
        void UpdateNameFields( string title, string firstName, string midName, string lastName, string suffix );

        /// <summary>
        /// Breaks the specified full name into components and sets the name fields of the contact to the
        /// resulting components.
        /// </summary>
        /// <param name="fullName">The full name of the contact.</param>
        void UpdateNameFields( string fullName );

        /// <summary>
        /// Link a contact with the account.
        /// </summary>
        /// <param name="emailAccount">A resource representing an email account.</param>
        void AddAccount( IResource emailAccount );

        /// <summary>
        /// Link a contact with the account.
        /// </summary>
        /// <param name="emailAccount">String representing an email account.</param>
        /// <see cref="AddAccount(IResource)"> for base method.</see>
        void AddAccount( string emailAccount );

        /// <summary>
        /// Returns true if the contact represents the user of Omea.
        /// </summary>
        bool IsMyself { get; }

        /// <summary>
        /// Returns true if the contact is imported from any Address or Contact book.
        /// </summary>
        bool IsImported { get; }

        /// <summary>
        /// Begins a batch update of the contact. During a batch update, changes to the contact
        /// do not cause immediate sending of notifications; after the batch update, if the contact
        /// indexed text is changed, the contact is automatically queued for reindexing.
        /// </summary>
        /// <since>2.0</since>
        void BeginUpdate();

        /// <summary>
        /// Ends a batch update of the contact and queues it for indexing if the indexed text is changed.
        /// </summary>
        /// <since>2.0</since>
        void EndUpdate();

        /// <summary>
        /// Request a contact text to be indexed.
        /// </summary>
        /// <since>2.0</since>
        void QueueIndexing();

        /// <summary>
        /// Returns the list of phone names defined for the contact.
        /// </summary>
        /// <returns>The array of phone name strings, or an empty array if no phones are defined.</returns>
        string[] GetPhoneNames();

        /// <summary>
        /// Checks if the contact has a phone with the specified number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to check.</param>
        /// <returns>true if the phone number exists, false otherwise.</returns>
        bool PhoneNumberExists( string phoneNumber );

        /// <summary>
        /// Adds or changes a phone with the specified name and number.
        /// </summary>
        /// <param name="phoneName">Name of the phone to set.</param>
        /// <param name="phoneNumber">Number of the phone to set.</param>
        void SetPhoneNumber( string phoneName, string phoneNumber );

        /// <summary>
        /// Returns a phone number with the specified name.
        /// </summary>
        /// <param name="phoneName">Name of the phone to get.</param>
        /// <returns>Phone number if a phone wuth the specified name exist, null otherwise.</returns>
        string GetPhoneNumber( string phoneName );

        /// <summary>
        /// Returns a resource desribing a phone given the phone number. Phone numbers
        /// are compared by their normalized representations.
        /// </summary>
        /// <param name="phoneNumber">Phone number.</param>
        /// <returns>A resource desribing a phone, null otherwise.</returns>
        IResource GetPhoneByNumber( string phoneNumber );
    }
}

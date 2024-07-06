// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// IDs of the resource properties which are registered by the core.
    /// </summary>
    /// <since>2.0</since>
    public interface ICoreProps
    {
        /// <summary>
        /// String property which stores the name of a resource
        /// </summary>
        int Name { get; }

        /// <summary>
        /// Date property which stores the date of a resource
        /// </summary>
        int Date { get; }

        /// <summary>
        /// Integer property which stores the size of a resource
        /// </summary>
        int Size { get; }

        /// <summary>
        /// String property which stores the subject of a resource
        /// </summary>
        int Subject { get; }

        /// <summary>
        /// Long string property which stores the contents of a resource
        /// </summary>
        int LongBody { get; }

        /// <summary>
        /// Boolean property which is set to true if the contents of the LongBody property is formatted as HTML
        /// </summary>
        int LongBodyIsHTML { get; }

        /// <summary>
        /// Boolean property which is set to true if the contents of the LongBody property is formatted as RTF
        /// </summary>
        int LongBodyIsRTF { get; }

        /// <summary>
        /// Directed link property which links a resource to its parent in a hierarchy
        /// </summary>
        int Parent { get; }

        /// <summary>
        /// Directed link property which links a resource to its parent in a conversation thread
        /// </summary>
        int Reply { get; }

        /// <summary>
        /// Boolean property which is set to true on resources which have been non-permanently deleted
        /// </summary>
        int IsDeleted { get; }

        /// <summary>
        /// Order in some ortogonal sorting (that is if the list is not sorted by property-based way).
        /// Usually used when user is allowed to sort a list manually (using D'n'D e.g.).
        /// </summary>
        /// <since>2.3</since>
        int Order { get; }

        /// <summary>
        /// Boolean property which is set to true on container resources for which the deleted items are not hidden when displaying their content
        /// </summary>
        int ShowDeletedItems { get; }

        /// <summary>
        /// Boolean property which is set to true on container resources for which the total count of items is to be shown.
        /// </summary>
        int ShowTotalCount { get; }

        /// <summary>
        /// Boolean property which is set to true on unread resources
        /// </summary>
        int IsUnread { get; }

        /// <summary>
        /// Boolean property which is set to true on container resources for which only unread resources are shown when displaying their content
        /// </summary>
        int DisplayUnread { get; }

        /// <summary>
        /// Boolean property which is set to true on container resources whose contents is displayed in threaded mode.
        /// </summary>
        int DisplayThreaded { get; }

        /// <summary>
        /// Boolean property which is set to true on container resources whose contents is displayed in newspaper mode.
        /// </summary>
        int DisplayNewspaper { get; }

        /// <summary>
        /// Integer property which is set to 1 on folders which are expanded and 0 on folders which are not.
        /// </summary>
        int Open { get; }

        /// <summary>
        /// String property which stores the text of the resource annotation.
        /// </summary>
        int Annotation { get; }

        /// <summary>
        /// String property which stores the basic resource type for an entity (e.g. Tab, View, etc).
        /// </summary>
        int ContentType { get; }

        /// <summary>
        /// A double property that defines an order in which the resources should be arranged in lists and under one parent in trees in those controls that support rearranging by the user, either with order buttons or by drag'n'drop.
        /// </summary>
        int ResourceVisibleOrder { get; }

        /// <summary>
        /// A string property that is set on the resource container and defines the user sorting order on its children by listing the resource IDs in a descending order.
        /// </summary>
        int UserResourceOrder { get; }

        /// <summary>
        /// A string property that is set on the resource if any subsystem detected an error on it or failed to process it.
        /// The content of the property describes the error reason (and origin).
        /// </summary>
        int LastError { get; }

        /// <summary>
        /// A string property that is set on the resource when it is deleted (non-permanently). This facilitates
        /// to construct flexible views on the deleted resources.
        /// </summary>
        int DeleteDate { get; }

        /// <summary>
        /// An boolean property which is set on the resource if the fragment of the text fed to the
        /// <see cref="JetBrains.Omea.TextIndex.FullTextIndexer">FullTextIndexer</see> should be stored in the
        /// resource's Preview Property.
        /// </summary>
        int NeedPreview { get; }

        /// <summary>
        /// A string property which is set to the beginning fragment of the resource's text content.
        /// Usually takes 128 bytes.
        /// </summary>
        int PreviewText { get; }
    }
}

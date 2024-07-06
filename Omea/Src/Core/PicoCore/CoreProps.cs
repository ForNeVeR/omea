// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.PicoCore
{
    /// <summary>
    /// Implementation of the ICoreProps interface
    /// </summary>
    public class CoreProps: ICoreProps
    {
        private readonly int _propName;
        private readonly int _propDate;
        private readonly int _propSize;
        private readonly int _propSubject;
        private readonly int _propLongBody;
        private readonly int _propLongBodyIsHTML;
        private readonly int _propLongBodyIsRTF;
        private readonly int _propParent;
        private readonly int _propReply;
        private readonly int _propIsDeleted;
        private readonly int _propOrder;
        private readonly int _propShowDeletedItems;
        private readonly int _propShowTotalCount;
        private readonly int _propIsUnread;
        private readonly int _propDisplayUnread;
        private readonly int _propDisplayThreaded;
        private readonly int _propDisplayNewspaper;
        private readonly int _propOpen;
        private readonly int _propAnnotation;
        private readonly int _propContentType;
        private readonly int _propResourceVisibleOrder;
        private readonly int _propUserResourceOrder;
        private readonly int _propLastError;
        private readonly int _propDelDate;
        private readonly int _propNeedPreview;
        private readonly int _propPreviewText;

        /// <summary>
        /// String property which stores the name of a resource
        /// </summary>
        public int Name { get { return _propName; } }

        /// <summary>
        /// Date property which stores the date of a resource
        /// </summary>
        public int Date { get { return _propDate; } }

        /// <summary>
        /// Integer property which stores the size of a resource
        /// </summary>
        public int Size { get { return _propSize; } }

        /// <summary>
        /// String property which stores the subject of a resource
        /// </summary>
        public int Subject { get { return _propSubject; } }

        /// <summary>
        /// Long string property which stores the contents of a resource
        /// </summary>
        public int LongBody { get { return _propLongBody; } }

        /// <summary>
        /// Boolean property which is set to true if the contents of the LongBody property is formatted as HTML
        /// </summary>
        public int LongBodyIsHTML { get { return _propLongBodyIsHTML; } }

        /// <summary>
        /// Boolean property which is set to true if the contents of the LongBody property is formatted as RTF
        /// </summary>
        public int LongBodyIsRTF { get { return _propLongBodyIsRTF; } }

        /// <summary>
        /// Directed link property which links a resource to its parent in a hierarchy
        /// </summary>
        public int Parent { get { return _propParent; } }

        /// <summary>
        /// Directed link property which links a resource to its parent in a conversation thread
        /// </summary>
        public int Reply { get { return _propReply; } }

        /// <summary>
        /// Order in some ortogonal sorting (that is if the list is not sorted by property-based way).
        /// Usually used when user is allowed to sort a list manually (using D'n'D e.g.).
        /// </summary>
        public int Order { get { return _propOrder; } }

        /// <summary>
        /// Boolean property which is set to true on resources which have been non-permanently deleted
        /// </summary>
        public int IsDeleted { get { return _propIsDeleted; } }

        /// <summary>
        /// Boolean property which is set to true on container resources for which the deleted items are not hidden when displaying their content
        /// </summary>
        public int ShowDeletedItems { get { return _propShowDeletedItems; } }

        /// <summary>
        /// Boolean property which is set to true on container resources for which the total count of items is to be shown.
        /// </summary>
        public int ShowTotalCount { get { return _propShowTotalCount; } }

        /// <summary>
        /// Boolean property which is set to true on unread resources
        /// </summary>
        public int IsUnread { get { return _propIsUnread; } }

        /// <summary>
        /// Boolean property which is set to true on container resources for which only unread resources are shown when displaying their content
        /// </summary>
        public int DisplayUnread { get { return _propDisplayUnread; } }

        /// <summary>
        /// Boolean property which is set to true on container resources whose contents is displayed in threaded mode.
        /// </summary>
        public int DisplayThreaded { get { return _propDisplayThreaded; } }

        /// <summary>
        /// Boolean property which is set to true on container resources whose contents is displayed in newspaper mode.
        /// </summary>
        public int DisplayNewspaper { get { return _propDisplayNewspaper; } }

        /// <summary>
        /// Integer property which is set to 1 on folders which are expanded and 0 on folders which are not.
        /// </summary>
        public int Open { get { return _propOpen; } }

        /// <summary>
        /// String property which stores the text of the resource annotation.
        /// </summary>
        public int Annotation { get { return _propAnnotation; } }

        /// <summary>
        /// String property which stores the basic resource type for an entity (e.g. Tab, View, etc).
        /// </summary>
        public int ContentType { get { return _propContentType; } }

        /// <summary>
        /// A <see cref="double"/> property that defines an order in which the resources should be arranged in lists and under one parent in trees in those controls that support rearranging by the user, either with order buttons or by drag'n'drop.
        /// </summary>
        public int ResourceVisibleOrder { get { return _propResourceVisibleOrder; } }

        /// <summary>
        /// A <see cref="string"/> property that is set on the resource container and defines the user sorting order on its children by listing the resource IDs in a descending order.
        /// </summary>
        public int UserResourceOrder { get { return _propUserResourceOrder; } }

        /// <summary>
        /// A <see cref="string"/> property that is set on the resource if any subsystem detected an error on it or failed to process it.
        /// The content of the property describes the error reason (and origin).
        /// </summary>
        public int LastError { get { return _propLastError; } }

        /// <summary>
        /// A string property that is set on the resource when it is deleted (non-permanently). This facilitates
        /// to construct flexible views on the deleted resources.
        /// </summary>
        public int DeleteDate { get { return _propDelDate;  } }

        public int NeedPreview { get { return _propNeedPreview; } }

        /// <summary>
        /// A string property which is set to the beginning fragment of the resource's text content.
        /// Usually takes 128 bytes.
        /// </summary>
        public int PreviewText { get { return _propPreviewText;  } }

        public CoreProps( IResourceStore store )
        {
            _propName = store.PropTypes.Register( "Name", PropDataType.String );
            _propDate = store.PropTypes.Register( "Date", PropDataType.Date );
            _propSize = store.PropTypes.Register( "Size", PropDataType.Int );
            _propSubject = store.PropTypes.Register( "Subject", PropDataType.String );
            _propLongBody = store.PropTypes.Register( "BodyContent", PropDataType.Blob, PropTypeFlags.Internal );
            _propLongBodyIsHTML = store.PropTypes.Register( "LongBodyIsHTML", PropDataType.Bool, PropTypeFlags.Internal );
            _propLongBodyIsRTF = store.PropTypes.Register( "LongBodyIsRTF", PropDataType.Bool, PropTypeFlags.Internal );
            _propParent = store.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( _propParent, "Parent", "Children" );

            _propReply = store.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( _propReply, "Reply To", "Replies" );

            _propIsDeleted = store.PropTypes.Register( "IsDeleted", PropDataType.Bool, PropTypeFlags.Internal );
            _propOrder = store.PropTypes.Register( "Order", PropDataType.Int, PropTypeFlags.Internal );
            _propShowDeletedItems = store.PropTypes.Register( "ShowDeletedItems", PropDataType.Bool, PropTypeFlags.Internal );
            _propShowTotalCount = store.PropTypes.Register( "ShowTotalItems", PropDataType.Bool, PropTypeFlags.Internal );
            _propIsUnread = store.PropTypes.Register( "IsUnread", PropDataType.Bool, PropTypeFlags.Internal );
            _propDisplayUnread = store.PropTypes.Register( "DisplayUnread", PropDataType.Bool, PropTypeFlags.Internal );
            _propDisplayThreaded = store.PropTypes.Register( "DisplayThreaded", PropDataType.Bool, PropTypeFlags.Internal );
            _propDisplayNewspaper = store.PropTypes.Register( "DisplayNewspaper", PropDataType.Bool, PropTypeFlags.Internal );
            _propOpen = store.PropTypes.Register( "Open", PropDataType.Int, PropTypeFlags.Internal );
            _propAnnotation = store.PropTypes.Register( "Annotation", PropDataType.String, PropTypeFlags.AskSerialize );

            _propContentType = store.PropTypes.Register( "ContentType", PropDataType.String, PropTypeFlags.Internal );
            _propResourceVisibleOrder = store.PropTypes.Register( "ResourceVisibleOrder", PropDataType.Double, PropTypeFlags.Internal );
            _propUserResourceOrder = store.PropTypes.Register( "UserResourceOrder", PropDataType.LongString, PropTypeFlags.Internal );
            _propLastError = store.PropTypes.Register( "LastError", PropDataType.String, PropTypeFlags.Internal );
            _propDelDate = store.PropTypes.Register( "DelDate", PropDataType.Date, PropTypeFlags.Internal );

            _propNeedPreview = store.PropTypes.Register( "NeedPreview", PropDataType.Bool, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
            _propPreviewText = store.PropTypes.Register( "PreviewText", PropDataType.String, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
        }
    }
}

// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Conversations
{
    /// <summary>
    /// Utility functions for working with conversation threads.
    /// </summary>
    public class ConversationBuilder
    {
        /// <summary>
        /// Returns the root resource of the specified conversation.
        /// </summary>
        /// <param name="res">The resource in a conversation.</param>
        /// <returns>The root resource of the conversation, or <paramref name="res"/> if no
        /// threading handler has been registered for the resource type.</returns>
        public static IResource GetConversationRoot( IResource res )
        {
            IResourceThreadingHandler handler = Core.PluginLoader.GetResourceThreadingHandler( res.Type );
            if ( handler == null )
            {
                return res;
            }
            IResource rootRes = res;
            while( true )
            {
                IResource parent = handler.GetThreadParent( rootRes );
                if ( parent == null )
                {
                    break;
                }

                rootRes = parent;
            }
            return rootRes;
        }

        /// <summary>
        /// Return <c>true</c> if <c>head</c> of the conversation is reachable from the
        /// <c>intern</c> resource.
        /// </summary>
        public static bool AreLinked( IResource head, IResource intern )
        {
            IResourceThreadingHandler handler = Core.PluginLoader.GetResourceThreadingHandler( head.Type );
            if ( handler == null )
            {
                return false;
            }
            IResource rootRes = intern;
            while( true )
            {
                IResource parent = handler.GetThreadParent( rootRes );
                if ( parent == null )
                    break;

                if( parent.Id == head.Id )
                    return true;

                rootRes = parent;
            }
            return false;
        }

        /// <summary>
        /// Checks the presence of the property upwards the thread parents.
        /// </summary>
        /// <param name="res">The resource in a conversation.</param>
        /// <param name="propId">Property to be checked.</param>
        /// <returns>true if any of the parent of the resource contains the specified property.</returns>
        public static bool CheckPropOnParents( IResource res, int propId )
        {
            IResource foo;
            return CheckPropOnParents( res, propId, out foo );
        }

        /// <summary>
        /// Checks the presence of the property upwards the thread parents.
        /// </summary>
        /// <param name="res">The resource in a conversation.</param>
        /// <param name="propId">Property to be checked.</param>
        /// <param name="propId">Resource which has such property, null if none of the parents has it.</param>
        /// <returns>true if any of the parent of the resource contains the specified property.</returns>
        public static bool CheckPropOnParents( IResource res, int propId, out IResource propRes )
        {
            bool result = res.HasProp( propId );

            IResourceThreadingHandler handler = Core.PluginLoader.GetResourceThreadingHandler( res.Type );
            if ( handler != null )
            {
                while( !result && res != null )
                {
                    res = handler.GetThreadParent( res );
                    if ( res != null )
                        result = res.HasProp( propId );
                }
            }
            propRes = result ? res : null;

            return result;
        }

        /**
         * Marks all conversation as read/unread
         */

        public static void MarkConversationRead( IResource res, bool read )
        {
            using( IResourceList conv = UnrollConversation( res ) )
            {
                foreach( IResource convres in conv )
                {
                    convres.SetProp( Core.Props.IsUnread, !read );
                }
            }
        }

        /**
         * Returns a list of all resources in the specified conversation.
         */

        public static IResourceList UnrollConversation( IResource res )
        {
            IResourceThreadingHandler handler = Core.PluginLoader.GetResourceThreadingHandler( res.Type );
            IResource root = GetConversationRoot( res );
            IResourceList conv = root.ToResourceListLive();
            if ( handler == null )
            {
                return conv;
            }
            return UnrollConversationRecursive( conv, root, handler );
        }

        private static IResourceList UnrollConversationRecursive( IResourceList conv, IResource res,
            IResourceThreadingHandler handler )
        {
            IResourceList replies = handler.GetThreadChildren( res );
            conv = replies.Union( conv );
            foreach( IResource replyRes in replies )
            {
                conv = UnrollConversationRecursive( conv, replyRes, handler );
            }
            return conv;
        }

        public static IResourceList UnrollConversationFromCurrent( IResource root )
        {
            IResourceThreadingHandler handler = Core.PluginLoader.GetResourceThreadingHandler( root.Type );
            IResourceList conv = root.ToResourceListLive();
            if ( handler == null )
            {
                return conv;
            }
            return UnrollConversationRecursive( conv, root, handler );
        }
    }

    public class ShowConversationAction: IAction
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count == 0 ||
                context.SelectedResources [0].GetLinksOfType( null, "Reply" ).Count == 0 )
            {
                if ( context.Kind == ActionContextKind.ContextMenu )
                {
                    presentation.Visible = false;
                }
                else
                {
                    presentation.Enabled = false;
                }
            }
        }

        public void Execute( IActionContext context )
        {
            if ( context.SelectedResources.Count > 0 )
                Core.ResourceBrowser.DisplayConversation( context.SelectedResources[ 0 ] );
        }
    }
}

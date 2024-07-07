// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    internal sealed class NntpClientHelper
    {
        public static void DeliverNewsFromServer( IResource server )
        {
            DeliverNewsFromServer( server, null, false, null );
        }

        public static void DeliverNewsFromServer( IResource server,
            IResource preferableGroup,
            bool invokedByUser,
            AsciiProtocolUnitDelegate finishedMethod )
        {
            Guard.NullArgument( server, "server" );

            ServerResource serverResource = new ServerResource( server );
            if( Utils.IsNetworkConnectedLight() )
            {
                NntpConnection connection = NntpConnectionPool.GetConnection( server, "background" );
                IResourceList groups = serverResource.Groups;

                if( groups.Count > 0 )
                {
                    /**
                     * at first deliver news from prefered group
                     */
                    if( preferableGroup != null && groups.Contains( preferableGroup ) )
                    {
                        connection.StartUnit( Int32.MaxValue - 2,
                            new NntpDownloadHeadersUnit( new NewsgroupResource( preferableGroup ), JobPriority.AboveNormal ) );
                        if( serverResource.DownloadBodiesOnDeliver )
                        {
                            connection.StartUnit( Int32.MaxValue - 3,
                                new NntpDeliverEmptyArticlesFromGroupsUnit( preferableGroup.ToResourceList(), null ) );
                        }
                    }
                    /**
                     * then deliver headers of other groups
                     */
                    NntpDeliverHeadersFromGroupsUnit deliverHeadersFromGroupsUnit =
                        new NntpDeliverHeadersFromGroupsUnit( groups, preferableGroup );
                    if( finishedMethod != null )
                    {
                        deliverHeadersFromGroupsUnit.Finished += finishedMethod;
                    }
                    int clientPriority = 0;
                    if( invokedByUser && preferableGroup != null && groups.Contains( preferableGroup ) )
                    {
                        clientPriority = Int32.MaxValue - 3;
                    }
                    connection.StartUnit( clientPriority, deliverHeadersFromGroupsUnit );
                    /**
                     * then download empty bodies
                     */
                    if( serverResource.DownloadBodiesOnDeliver )
                    {
                        NntpDeliverEmptyArticlesFromGroupsUnit deliverEmptyArticlesUnit =
                            new NntpDeliverEmptyArticlesFromGroupsUnit( groups, preferableGroup );
                        if( finishedMethod != null )
                        {
                            deliverEmptyArticlesUnit.Finished += finishedMethod;
                        }
                        connection.StartUnit( clientPriority, deliverEmptyArticlesUnit  );
                    }
                }
                /**
                 * finally replicate new groups
                 */
                NntpDownloadGroupsUnit groupsUnit = new NntpDownloadGroupsUnit( server, false, JobPriority.Lowest );
                if( finishedMethod != null )
                {
                    groupsUnit.Finished += finishedMethod;
                }
                connection.StartUnit( 0, groupsUnit );
            }
            /**
             * queue timed news delivering is necessary
             */
            int freq = serverResource.DeliverFreq;
            if( freq > 0 )
            {
                Core.NetworkAP.QueueJobAt(
                    DateTime.Now.AddMinutes( freq ), new ResourceDelegate( DeliverNewsFromServer ), server );
            }
        }

        public static void DownloadHeadersFromGroup( IResource group )
        {
            Guard.NullArgument( group, "group" );

            if( !Utils.IsNetworkConnectedLight() ) return;

            NewsgroupResource groupResource = new NewsgroupResource( group );
            IResource server = groupResource.Server;
            if( server != null )
            {
                NntpConnectionPool.GetConnection( server, "background" ).StartUnit(
                    Int32.MaxValue - 3, new NntpDownloadHeadersUnit( groupResource, JobPriority.AboveNormal ) );

                /**
                 * also download bodies if necessary
                 */
                if( new ServerResource( server ).DownloadBodiesOnDeliver )
                {
                    NntpConnectionPool.GetConnection( server, "background" ).StartUnit(
                        0, new NntpDeliverEmptyArticlesFromGroupsUnit( group.ToResourceList(), null ) );
                }
            }
        }

        public static void DownloadNextHeadersFromGroup( IResource group )
        {
            Guard.NullArgument( group, "group" );

            if( !Utils.IsNetworkConnectedLight() ) return;

            NewsgroupResource groupResource = new NewsgroupResource( group );
            IResource server = groupResource.Server;
            if( server != null )
            {
                NntpConnection connection = NntpConnectionPool.GetConnection( server, "background" );
                connection.StartUnit( Int32.MaxValue - 3,
                    new NntpDownloadNextHeadersUnit( groupResource, JobPriority.AboveNormal ) );
            }
        }

        public static void DownloadAllHeadersFromGroup( IResource group )
        {
            Guard.NullArgument( group, "group" );

            if( !Utils.IsNetworkConnectedLight() ) return;

            NewsgroupResource groupResource = new NewsgroupResource( group );
            IResource server = groupResource.Server;
            if( server != null )
            {
                NntpConnection connection = NntpConnectionPool.GetConnection( server, "background" );
                connection.StartUnit( Int32.MaxValue - 5,
                    new NntpDownloadAllHeadersUnit( groupResource, JobPriority.AboveNormal ) );
            }
        }

        public static void PostArticle( IResource draftArticle, AsciiProtocolUnitDelegate finishedMethod, bool invokedByUser )
        {
            Guard.NullArgument( draftArticle, "draftArticle" );

            if( !Utils.IsNetworkConnectedLight() ) return;

            lock( _articlesBeenPosted )
            {
                if( _articlesBeenPosted.Contains( draftArticle ) )
                {
                    return;
                }
                _articlesBeenPosted.Add( draftArticle );
            }
            IResourceList groups = draftArticle.GetLinksFrom( NntpPlugin._newsGroup, NntpPlugin._propTo );
            if( groups.Count > 0 )
            {
                IResource server = new NewsgroupResource( groups[ 0 ] ).Server;
                if( server != null )
                {
                    NntpConnection postConnection = NntpConnectionPool.GetConnection( server, "foreground" );
                    NntpPostArticleUnit postUnit =
                        new NntpPostArticleUnit( draftArticle, server, finishedMethod, invokedByUser );
                    postUnit.Finished += postUnit_Finished;
                    postConnection.StartUnit( invokedByUser ? Int32.MaxValue - 2 : 0, postUnit );
                    return;
                }
            }
            ArticlePostedOrFailed( draftArticle );
            if( finishedMethod != null )
            {
                finishedMethod( null );
            }
        }

        private static void postUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpPostArticleUnit postUnit = (NntpPostArticleUnit) unit;
            ArticlePostedOrFailed( postUnit.DraftArticle );
        }

        private static void ArticlePostedOrFailed( IResource article )
        {
            lock( _articlesBeenPosted )
            {
                _articlesBeenPosted.Remove( article );
            }
        }

        private static readonly HashSet _articlesBeenPosted = new HashSet();
    }
}

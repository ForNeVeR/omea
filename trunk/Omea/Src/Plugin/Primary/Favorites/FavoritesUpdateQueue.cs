/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Net;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class FavoritesUpdateQueue : ReenteringGroupJob
    {
        internal FavoritesUpdateQueue( IResourceList weblinks )
            : base()
        {
            _weblinks = weblinks;
            // if we're using a proxy, all the connections will go through one ServicePoint,
            // so it does not make sense to set a queue size larger than the maximum connection
            // limit on the proxy
            int maxCount = 10;
            WebProxy defaultProxy = GlobalProxySelection.Select as WebProxy;
            if ( defaultProxy != null && defaultProxy.Address != null )
            {
                maxCount = ServicePointManager.DefaultConnectionLimit;                
            }
            SimultaneousJobs = maxCount;
            _count = 0;
        }

        public override AbstractJob GetNextJob()
        {
            if( _count >= _weblinks.Count )
            {
                return null;
            }
            IResource weblink = Core.ResourceStore.TryLoadResource( _weblinks.ResourceIds[ _count++ ] );
            if( weblink == null )
            {
                return GetNextJob();
            }
            string url = weblink.GetPropText( "URL" );
            if( url.Length == 0 )
            {
                return GetNextJob();
            }
            return new FavoriteJob( weblink, url );
        }

        public override void GroupStarting()
        {
            FavoritesTools.TraceIfAllowed( "Starting download a group of " + _weblinks.Count + " weblinks" );
        }

        public override void GroupFinished()
        {
            FavoritesTools.TraceIfAllowed( "Finished download a group of " + _weblinks.Count + " weblinks" );
        }

        public override string Name
        {
            get { return "Downloading bookmarks"; }
            set {}
        }

        private IResourceList   _weblinks;
        private int             _count;
    }
}
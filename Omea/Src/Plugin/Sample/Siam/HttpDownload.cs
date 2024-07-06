// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Siam
{
	/// <summary>
	/// Summary description for HttpDownload.
	/// </summary>
	public class HttpDownload : AbstractNamedJob
	{
		public HttpDownload(string sUrl)
		{
            _sUrl = sUrl;
		}

		/*
		/// <summary>
		/// Possible object state values
		/// </summary>
		protected enum DownloadState { Idle, Busy }

		/// <summary>
		/// Object state
		/// </summary>
		protected DownloadState	_state = DownloadState.Idle;
		*/

        /// <summary>
        /// The request to the server
        /// </summary>
	    protected WebRequest  _request;

        /// <summary>
        /// Represents the particular request being made and identifies it when information is retrieved from the request
        /// </summary>
        protected IAsyncResult  _asyncRequestResult;

        /// <summary>
        /// URL being downloaded, as submitted to the constructor.
        /// </summary>
	    public string SUrl
	    {
	        get { return _sUrl; }
	    }

	    /// <summary>
        /// URL being downloaded
        /// </summary>
	    protected string    _sUrl;

        /// <summary>
        /// Delegate for the ContentDownloaded event.
        /// </summary>
        public delegate void ContentDownloadedEventHandler(Stream streamData, HttpDownload session);

        /// <summary>
        /// Fires when the content is downloaded successfully
        /// </summary>
        public event ContentDownloadedEventHandler ContentDownloaded;

	    /// <summary>
		/// Executes the asynchronous job
		/// </summary>
		protected override void Execute()
		{
            _request = WebRequest.Create( _sUrl );
			_asyncRequestResult = _request.BeginGetResponse( null, null );  // Start the download

            InvokeAfterWait( new MethodInvoker(OnHttpDownloaded), _asyncRequestResult.AsyncWaitHandle ); // Wait until it completes and call the handler
		}

        /// <summary>
        /// Download has completed
        /// </summary>
	    private void OnHttpDownloaded()
	    {
            WebResponse response = _request.EndGetResponse( _asyncRequestResult );

            if(response.GetType() == typeof(HttpWebResponse))
            {
                HttpWebResponse responseHttp = response as HttpWebResponse;
                if(responseHttp.StatusCode != HttpStatusCode.OK)
                    throw new Exception("Failed to retrieve the sync data from HTTP.\n" + responseHttp.StatusDescription);
            }

            // Obtain the stream
            Stream  stream = response.GetResponseStream();

            // Raise the event
            ContentDownloaded(stream, this);
        }

        /// <summary>
        /// Job name.
        /// </summary>
	    public override string Name
		{
			get { return "Downloading SIAM sync data"; }
			set {  }
		}
	}
}

// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace ResourceToolsTests
{
	[TestFixture]
	public class HtmlIndexerTests : IResourceTextConsumer
	{
		private HtmlIndexer _indexer = null;

		private int _nOffset = 0;

		private ArrayList _sFragments = null;

		private ArrayList _nOffsets = null;

	    [SetUp]
		public void Setup()
		{
			_indexer = new HtmlIndexer();
			_nOffset = 0;
			_sFragments = new ArrayList();
			_nOffsets = new ArrayList();
		}

		[TearDown]
		public void Teardown()
		{
			_indexer = null;
			_nOffset = 0;
			_sFragments = null;
			_nOffsets = null;
		}

		[Test]
		public void CheckOffsetsSimple()
		{
			string sBody = "Here comes the &lt;nite&gt;!";
			HtmlIndexer.IndexHtml( 0xCD, sBody, this, null );

			Assert.AreEqual( _sFragments.Count, 4 );

			Assert.AreEqual( _sFragments[ 0 ], "Here " );
			Assert.AreEqual( _sFragments[ 1 ], "comes " );
			Assert.AreEqual( _sFragments[ 2 ], "the <" );
			Assert.AreEqual( _sFragments[ 3 ], "nite>!" );

			Assert.AreEqual( _nOffsets[ 0 ], sBody.IndexOf( "Here" ) );
			Assert.AreEqual( _nOffsets[ 1 ], sBody.IndexOf( "comes" ) );
			Assert.AreEqual( _nOffsets[ 2 ], sBody.IndexOf( "the" ) );
			Assert.AreEqual( _nOffsets[ 3 ], sBody.IndexOf( "nite" ) );
		}

		[Test]
			public void CheckOffsetsTags()
		{
			string sBody = "<html>Welcome</br>to <b>hell</b>&nbsp;</html>";
			HtmlIndexer.IndexHtml( 0xCD, sBody, this, null );

			Assert.AreEqual( _sFragments.Count, 4 );

			Assert.AreEqual( _sFragments[ 0 ], "Welcome" );
			Assert.AreEqual( _sFragments[ 1 ], "to " );
			Assert.AreEqual( _sFragments[ 2 ], "hell" );
			Assert.AreEqual( _sFragments[ 3 ], " " );

			Assert.AreEqual( _nOffsets[ 0 ], sBody.IndexOf( "Welcome" ) );
			Assert.AreEqual( _nOffsets[ 1 ], sBody.IndexOf( "to" ) );
			Assert.AreEqual( _nOffsets[ 2 ], sBody.IndexOf( "hell" ) );
			Assert.AreEqual( _nOffsets[ 3 ], sBody.IndexOf( "&nbsp;" ) );
		}

		public void AddDocumentFragment( int resourceId, string text )
		{
			Assert.AreEqual( resourceId, 0xCD );
            if( text != null )
            {
    			_nOffsets.Add( _nOffset );
	    		_sFragments.Add( text );
		    	_nOffset += text.Length;
            }
		}

		public void AddDocumentFragment( int resourceId, string text, string sectionName )
		{
			AddDocumentFragment( resourceId, text );
		}

		public void IncrementOffset( int spacesAmount )
		{
			if( spacesAmount < 0 )
				throw new ArgumentException( "Negative offset." );
			_nOffset += spacesAmount;
		}

		public TextRequestPurpose Purpose
		{
			get { return TextRequestPurpose.Indexing; }
		}

		#region Mock resource text consumer members.

		public void RestartOffsetCounting()
		{
			throw new NotImplementedException();
		}

		public void RejectResult()
		{
			throw new NotImplementedException();
		}

		public void AddDocumentHeading( int resourceId, string text )
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}

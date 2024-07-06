// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using  System;
using  JetBrains.Omea.PicoCore;
using  NUnit.Framework;
using  JetBrains.Omea.OpenAPI;

namespace FilterManagerTests
{
    [TestFixture]
    public class FilterManagerResourceTypesIntersectionTests
    {
        private TestCore _core;
        private IResourceStore _storage;

        private IResourceList oneResourceTypeList;

        //---------------------------------------------------------------------
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            CreateNecessaryResources();
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        //---------------------------------------------------------------------
        //  No conditions are used, resource type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, for which there are resources
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewNoConditionAllResourceTypes1()
        {
            Core.FilterRegistry.RegisterView( "View", null, null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewNoConditionAllResourceTypes2()
        {
            Core.FilterRegistry.RegisterView( "View", null,(IResource[]) null, null );
        }

        [Test]
        public void ViewNoConditionOneConformingType()
        {
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count,  result.Count );
        }

        [Test]
        public void ViewNoConditionSeveralExistingTypesWithOneConforming()
        {
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewNoConditionSeveralTypesWithNonExistingOne()
        {
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, (IResource[])null, null );
        }

        //---------------------------------------------------------------------
        //  One valid condition is used with All Res Types filter, view resource
        //  type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, for which there are resources;
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionNullTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", null, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneConditionNullTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneConditionNullTypeSeveralExistingTypesWithOneConforming()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneConditionNullTypeSeveralTypesWithNonExistingOne()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, new[]{ condition }, null );
        }

        //---------------------------------------------------------------------
        //  One valid condition is used with sample Res Types filter, view resource
        //  type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, conforming to the condition,
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionOneTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", null, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneTypeOneNonConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsArticle" }, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0, result.Count );
        }

        [Test]
        public void ViewOneConditionOneTypeSeveralExistingTypesWithOneConforming()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, new[]{ condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneConditionOneTypeSeveralTypesWithNonExistingOne()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, new[]{ condition }, null );
        }

        //---------------------------------------------------------------------
        //  One valid exception is used with All Res Types filter, view resource
        //  type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, for which there are resources;
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneExceptionNullTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", null, (IResource[])null, new[]{ condition } );
        }

        [Test]
        public void ViewOneExceptionNullTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, (IResource[])null, new[]{ condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneExceptionNullTypeSeveralExistingTypesWithOneConforming()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, (IResource[])null, new[]{ condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneExceptionNullTypeSeveralTypesWithNonExistingOne()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, (IResource[])null, new[]{ condition } );
        }

        //---------------------------------------------------------------------
        //  One valid exception is used with sample Res Types filter, view resource
        //  type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, conforming to the condition,
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneExceptionOneTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", null, (IResource[])null, new[]{ condition } );
        }

        [Test]
        public void ViewOneExceptionOneTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, (IResource[])null, new[]{ condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test]
        public void ViewOneExceptionOneTypeOneNonConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsArticle" }, (IResource[])null, new[]{ condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0, result.Count );
        }

        [Test]
        public void ViewOneExceptionOneTypeSeveralExistingTypesWithOneConforming()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, (IResource[])null, new[]{ condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( oneResourceTypeList.Count / 2,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneExceptionOneTypeSeveralTypesWithNonExistingOne()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, (IResource[])null, new[]{ condition } );
        }

        //---------------------------------------------------------------------
        //  One valid condition + one valid exception; result non empty;
        //  Sample Res Types filter;
        //  View resource type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, conforming to the condition,
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionOneExceptionOneTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "7" );
            IResource view = Core.FilterRegistry.RegisterView( "View", null, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "7" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeOneNonConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "7" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0, result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeSeveralExistingTypesWithOneConforming()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "7" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void ViewOneConditionOneExceptionOneTypeSeveralTypesWithNonExistingOne()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "7" );
            Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle", "FakeResType" }, new[]{ condition }, new[]{ exception } );
        }

        //---------------------------------------------------------------------
        //  One valid condition + one valid exception; result is empty;
        //  Sample Res Types filter;
        //  View resource type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, conforming to the condition,
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionOneExceptionOneTypeNullTypeResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "3" );
            IResource view = Core.FilterRegistry.RegisterView( "View", null, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeOneConformingTypeResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "3" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeOneNonConformingTypeResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "3" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0, result.Count );
        }

        [Test]
        public void ViewOneConditionOneExceptionOneTypeSeveralExistingTypesWithOneConformingResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Gt, "3" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        //---------------------------------------------------------------------
        //  One valid condition + one valid exception with empty result;
        //  Result is not empty;
        //  Sample Res Types filter;
        //  View resource type is set to:
        //  - NULL (All Resource Types),
        //  - One existing resource type, conforming to the condition,
        //  - several existing res types,
        //  - several existing res types and one fake.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionOneEmptyResultExceptionOneTypeNullType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Lt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", null, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 5,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneEmptyResultExceptionOneTypeOneConformingType()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Lt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 5,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneEmptyResultExceptionOneTypeOneNonConformingTypeResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Lt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0, result.Count );
        }

        [Test]
        public void ViewOneConditionOneEmptyResultExceptionOneTypeSeveralExistingTypesWithOneConformingResultEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", new[]{ "Email" }, "Counter", ConditionOp.Gt, "4" );
            IResource exception = Core.FilterRegistry.CreateStandardCondition( "Exception", "DeepName1", new[]{ "Email" }, "Counter", ConditionOp.Lt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Email", "NewsArticle" }, new[]{ condition }, new[]{ exception } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 5,  result.Count );
        }

        //---------------------------------------------------------------------
        //  Link types block.
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------
        //  No conditions, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewNoConditionsOneLinkType1()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test]
        public void ViewNoConditionsOneLinkType2()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        [Test]
        public void ViewNoConditionsAllLinkTypes()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 5,  result.Count );
        }

        //---------------------------------------------------------------------
        //  One condition, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionsOneLinkType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 1,  result.Count );
        }

        [Test]
        public void ViewOneConditionsOneLinkType2()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 1,  result.Count );
        }

        [Test]
        public void ViewOneConditionsAllLinkTypes()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        //---------------------------------------------------------------------
        //  One exception, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneExceptionOneLinkType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        [Test]
        public void ViewOneExceptionOneLinkType2()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 1,  result.Count );
        }

        [Test]
        public void ViewOneExceptionAllLinkTypes()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        //---------------------------------------------------------------------
        //  Link types with format restriction block.
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------
        //  No conditions, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewNoConditionsOneLinkTypeOneFormatType1()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "HtmlFile" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        [Test]
        public void ViewNoConditionsOneLinkTypeOneFormatType2()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment", "HtmlFile" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 1,  result.Count );
        }

        [Test]
        public void ViewNoConditionsAllLinkTypesOneFormatType1()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test]
        public void ViewNoConditionsAllLinkTypesAllFormatTypes()
        {
            CreateLinkResources();

            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile", "TextFile" }, (IResource[])null, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 5,  result.Count );
        }

        //---------------------------------------------------------------------
        //  One condition, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneConditionOneLinkTypeOneFormatType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "HtmlFile" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        [Test]
        public void ViewOneConditionOneLinkTypeOneFormatType2()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment", "HtmlFile" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        [Test]
        public void ViewOneConditionAllLinkTypesOneFormatType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 0,  result.Count );
        }

        [Test]
        public void ViewOneConditionAllLinkTypesAllFormatTypes()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile", "TextFile" }, new[] { condition }, null );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        //---------------------------------------------------------------------
        //  One exception, res type is set to LinkType, get all resources.
        //---------------------------------------------------------------------
        [Test]
        public void ViewOneExceptionOneLinkTypeOneFormatType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "HtmlFile" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 2,  result.Count );
        }

        [Test]
        public void ViewOneExceptionOneLinkTypeOneFormatType2()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "NewsAttachment", "HtmlFile" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 1,  result.Count );
        }

        [Test]
        public void ViewOneExceptionAllLinkTypesOneFormatType1()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        [Test]
        public void ViewOneExceptionAllLinkTypesAllFormatTypes()
        {
            CreateLinkResources();

            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Counter", ConditionOp.Gt, "4" );
            IResource view = Core.FilterRegistry.RegisterView( "View", new[]{ "Attachment", "NewsAttachment", "HtmlFile", "TextFile" }, (IResource[])null, new[] { condition } );
            IResourceList result = Core.FilterEngine.ExecView( view );
            Assert.AreEqual( 3,  result.Count );
        }

        //---------------------------------------------------------------------
        private void CreateNecessaryResources()
        {
            _storage.ResourceTypes.Register( "Email", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "NewsArticle", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "RSSFeed", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "Category", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "HtmlFile", "", ResourceTypeFlags.Normal | ResourceTypeFlags.FileFormat );
            _storage.ResourceTypes.Register( "TextFile", "", ResourceTypeFlags.Normal | ResourceTypeFlags.FileFormat );

            _storage.PropTypes.Register( "ContentType", PropDataType.String, PropTypeFlags.Internal );
            _storage.PropTypes.Register( "Subject", PropDataType.String, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "IsUnread", PropDataType.Bool, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Date", PropDataType.Date, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "UnreadCount", PropDataType.Int, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Category", PropDataType.Link, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Name", PropDataType.String, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "DeepName", PropDataType.String, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Counter", PropDataType.Int, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Attachment", PropDataType.Link, PropTypeFlags.Normal | PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink );
            _storage.PropTypes.Register( "NewsAttachment", PropDataType.Link, PropTypeFlags.Normal | PropTypeFlags.SourceLink | PropTypeFlags.DirectedLink );

            //  Prepare a list of abstract resources for CreateStandardConditions pool parameters.
            oneResourceTypeList = Core.ResourceStore.EmptyResourceList;
            for( int i = 0; i < 10; i++ )
            {
                IResource emailRes = _storage.BeginNewResource( "Email" );
                emailRes.SetProp( "IsUnread", true );
                emailRes.SetProp( "Date", DateTime.Now );
                emailRes.SetProp( "Counter", i );
                emailRes.EndUpdate();

                oneResourceTypeList = oneResourceTypeList.Union( emailRes.ToResourceList() );
            }
        }

        private void CreateLinkResources()
        {
            //  Resources with processable links - Attachment and NewsAttachment.
            IResource email1 = _storage.BeginNewResource( "Email" );
            email1.SetProp( "Subject", "Resource without attachment" );
            email1.EndUpdate();

            IResource email2 = _storage.BeginNewResource( "Email" );
            email2.SetProp( "Subject", "Resource with one attachment" );
            email2.EndUpdate();

            IResource email3 = _storage.BeginNewResource( "Email" );
            email3.SetProp( "Subject", "Resource with two attachments" );
            email3.EndUpdate();

            IResource article = _storage.BeginNewResource( "NewsArticle" );
            article.SetProp( "Subject", "Resource with two attachments" );
            article.EndUpdate();

            IResource attach1 = _storage.BeginNewResource( "HtmlFile" );
            attach1.SetProp( "Name", "Attachment1");
            attach1.SetProp( "Counter", 1 );
            attach1.SetProp( "Attachment", email2 );
            attach1.EndUpdate();

            IResource attach2 = _storage.BeginNewResource( "HtmlFile" );
            attach2.SetProp( "Name", "Attachment2");
            attach2.SetProp( "Counter", 3 );
            attach2.SetProp( "Attachment", email3 );
            attach2.EndUpdate();

            IResource attach3 = _storage.BeginNewResource( "TextFile" );
            attach3.SetProp( "Name", "Attachment3");
            attach3.SetProp( "Counter", 5 );
            attach3.SetProp( "Attachment", email3 );
            attach3.EndUpdate();

            IResource attach4 = _storage.BeginNewResource( "HtmlFile" );
            attach4.SetProp( "Name", "Attachment4 for News");
            attach4.SetProp( "Counter", 2 );
            attach4.SetProp( "NewsAttachment", article );
            attach4.EndUpdate();

            IResource attach5 = _storage.BeginNewResource( "TextFile" );
            attach5.SetProp( "Name", "Attachment5 for News");
            attach5.SetProp( "Counter", 9 );
            attach5.SetProp( "NewsAttachment", article );
            attach5.EndUpdate();
        }
    }
}

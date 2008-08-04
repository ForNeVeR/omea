/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using   System;
using   JetBrains.Omea.PicoCore;
using   NUnit.Framework;
using   JetBrains.Omea.OpenAPI;

namespace FilterManagerTests
{
    [TestFixture]
    public class FilterManagerOpenAPITest
    {
        private TestCore _core;
        private IResourceStore _storage;

        private IResourceList paramsList, emptyParamsList;
        private ICustomCondition filterObject1;

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
        //  RecreateStandardCondition( string name, string[] resTypes, string propName, ConditionOp op, params string[] values );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1NameNull()
        {
            Core.FilterRegistry.RecreateStandardCondition( null, null, null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1NameEmpty()
        {
            Core.FilterRegistry.RecreateStandardCondition( "", "", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1PropNameNull()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, null, ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1PropNameEmpty()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqNoArguments()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqArgumentEmptyVariant1()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqArgumentEmptyVariant2()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "string1", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationGtExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Gt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationLtExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Lt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Has, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationGtOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Gt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationLtOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Lt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Has );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasPropNoArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasNoPropNoArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasNoProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasLinkNoArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasLink, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected1()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected2()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange, "string1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected3()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange, "string1", "string2", "string3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeNoOrOneArgmentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.QueryMatch, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInOneOreMoreArgumentExpected()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.In );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnEq()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.Eq, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnGt()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.Gt, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnInRange()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.InRange, "x", "y" );
        }

        [Test]
        public void ReCreateStandardConditionsMethod1CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "x", "y" );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "x", "y" );
            int Id2 = res2.Id;
            Assert.IsTrue( Id1 != Id2 );
            Assert.IsTrue( res1.IsDeleting || res1.IsDeleted );
        }

        //---------------------------------------------------------------------
        //  RecreateStandardCondition( string name, string[] resTypes, string propName, ConditionOp op, IResourceList values );
        //  Only "Eq" or "In" operations can be passed to this overload.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2NameNull()
        {
            Core.FilterRegistry.RecreateStandardCondition( null, null, null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2NameEmpty()
        {
            Core.FilterRegistry.RecreateStandardCondition( "", "", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2PropNameNull()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2PropNameEmpty()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp1()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp2()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp3()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp4()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp5()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2NullResourceList()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, (IResourceList)null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2EmptyResourceList()
        {
            Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, emptyParamsList );
        }

        [Test]
        public void ReCreateStandardConditionsMethod2CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            int Id2 = res2.Id;
            Assert.IsTrue( Id1 != Id2 );
            Assert.IsTrue( res1.IsDeleting || res1.IsDeleted );
        }

        //---------------------------------------------------------------------
        //  CreateStandardCondition( string name, string[] resTypes, string propName, ConditionOp op, params string[] values );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3NameNull()
        {
            Core.FilterRegistry.CreateStandardCondition( null, "DeepName", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3NameEmpty()
        {
            Core.FilterRegistry.CreateStandardCondition( "", "DeepName", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3PropNameNull()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3PropNameEmpty()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqNoArguments()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqArgumentEmptyVariant1()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqArgumentEmptyVariant2()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "string1", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationGtExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationLtExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasExactlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationGtOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationLtOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasOnlyOneArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasPropNoArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasNoPropNoArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasNoProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasLinkNoArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasLink, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected1()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected2()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, "string1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected3()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, "string1", "string2", "string3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeNoOrOneArgmentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInOneOreMoreArgumentExpected()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnEq()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.Eq, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnGt()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.Gt, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnInRange()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.InRange, "x", "y" );
        }

        [Test]
        public void CreateStandardConditionsMethod3CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "x", "y" );
            IResource res2 = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "x", "y" );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  RecreateStandardCondition( string name, string[] resTypes, string propName, ConditionOp op, IResourceList values );
        //  Only "Eq" or "In" operations can be passed to this overload.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NameNull()
        {
            Core.FilterRegistry.CreateStandardCondition( null, "DeepName", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NameEmpty()
        {
            Core.FilterRegistry.CreateStandardCondition( "", "DeepName", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4PropNameNull()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4PropNameEmpty()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp1()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp2()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp3()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp4()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp5()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NullResourceList()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, (IResourceList)null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4EmptyResourceList()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, emptyParamsList );
        }

        [Test]
        public void CreateStandardConditionsMethod4CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterRegistry.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            IResource res2 = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  CreateQueryCondition( string name, string[] resTypes, string query, string sectionName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5NameNull()
        {
            Core.FilterRegistry.CreateQueryCondition( null, "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5NameEmpty()
        {
            Core.FilterRegistry.CreateQueryCondition( "", "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5QueryNull()
        {
            Core.FilterRegistry.CreateQueryCondition( "Name", "DeepName", null, null, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5QueryEmpty()
        {
            Core.FilterRegistry.CreateQueryCondition( "Name", "DeepName", null, "", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod5SectionNameEmpty()
        {
            Core.FilterRegistry.CreateQueryCondition( "Name", "DeepName", null, "Query", "" );
        }

        [Test]
        public void CreateQueryConditionsMethod5CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterRegistry.CreateQueryCondition( "Name", "DeepName", null, "Query", null );
            IResource res2 = Core.FilterRegistry.CreateQueryCondition( "Name", "DeepName", null, "Query", null );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  RecreateQueryCondition( string name, string[] resTypes, string query, string sectionName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6NameNull()
        {
            Core.FilterRegistry.RecreateQueryCondition( null, "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6NameEmpty()
        {
            Core.FilterRegistry.RecreateQueryCondition( "", "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6QueryNull()
        {
            Core.FilterRegistry.RecreateQueryCondition( "Name", "DeepName", null, null, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6QueryEmpty()
        {
            Core.FilterRegistry.RecreateQueryCondition( "Name", "DeepName", null, "", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod6SectionNameEmpty()
        {
            Core.FilterRegistry.RecreateQueryCondition( "Name", "DeepName", null, "Query", "" );
        }

        [Test]
        public void ReCreateQueryConditionsMethod6CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterRegistry.RecreateQueryCondition( "Name", "DeepName", null, "Query", null );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterRegistry.RecreateQueryCondition( "Name", "DeepName", null, "Query", null );
            int Id2 = res2.Id;
            Assert.IsTrue( Id1 != Id2 );
            Assert.IsTrue( res1.IsDeleting || res1.IsDeleted );
        }

        //---------------------------------------------------------------------
        //  IResource RegisterCustomCondition( string name, string[] resTypes, ICustomCondition filter )
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod7NameNull()
        {
            Core.FilterRegistry.RegisterCustomCondition( null, "DeepName", null, filterObject1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod7NameEmpty()
        {
            Core.FilterRegistry.RegisterCustomCondition( "", "DeepName", null, filterObject1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod7ObjectNull()
        {
            Core.FilterRegistry.RegisterCustomCondition( "Name", "DeepName", null, null );
        }

        //---------------------------------------------------------------------
        //  IResource RecreateConditionTemplate( string name, string[] resTypes, ConditionOp op, params string[] values );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8NameNull()
        {
            Core.FilterRegistry.RecreateConditionTemplate( null, "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8NameEmpty()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "", "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamsRequired()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamNull()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, null );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamEmpty()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_TooManyParams()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name", "Param" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_ParamsRequired1()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_ParamsRequired2()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_TooManyParams()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name", "IsUnread", "Other" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_ParamsRequired1()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_ParamsRequired2()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_TooManyParams()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1", "2", "3" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_IllegalPropertyType()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "Name", "1", "2", "3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationQuerySemantics_TooManyParams()
        {
            Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.QueryMatch, "Name", "1" );
        }

        [Test]
        public void ReCreateConditionTemplate8CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterRegistry.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            int Id2 = res2.Id;
            Assert.IsTrue( Id1 != Id2 );
            Assert.IsTrue( res1.IsDeleting || res1.IsDeleted );
        }

        //---------------------------------------------------------------------
        //  IResource CreateConditionTemplate( string name, string[] resTypes, ConditionOp op, params string[] values );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9NameNull()
        {
            Core.FilterRegistry.CreateConditionTemplate( null, "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9NameEmpty()
        {
            Core.FilterRegistry.CreateConditionTemplate( "", "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamsRequired()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamNull()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, null );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamEmpty()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_TooManyParams()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name", "Param" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_ParamsRequired1()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_ParamsRequired2()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_TooManyParams()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name", "IsUnread", "Other" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_ParamsRequired1()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_ParamsRequired2()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_TooManyParams()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1", "2", "3" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_IllegalPropertyType()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "Name", "1", "2", "3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationQuerySemantics_TooManyParams()
        {
            Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.QueryMatch, "Name", "1" );
        }

        [Test]
        public void CreateConditionTemplateMethod9CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            IResource res2 = Core.FilterRegistry.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  void  AssociateConditionWithGroup( IResource condition, string groupName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_NullResource()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterRegistry.AssociateConditionWithGroup( null, "Group" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void AssociateConditionWithGroup_IllegalInputResource()
        {
            Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            IResource res = Core.ResourceStore.NewResource( "Email" );
            Core.FilterRegistry.AssociateConditionWithGroup( res, "Group" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_IllegalGroupNull()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterRegistry.AssociateConditionWithGroup( condition, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_IllegalGroupEmpty()
        {
            IResource condition = Core.FilterRegistry.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterRegistry.AssociateConditionWithGroup( condition, "" );
        }

        //---------------------------------------------------------------------
        //  IResource CreateViewFolder( string name, string baseFolderName, int order );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateViewFolder_NullName()
        {
            Core.FilterRegistry.CreateViewFolder( null, "a", 1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateViewFolder_EmptyName()
        {
            Core.FilterRegistry.CreateViewFolder( "", "a", 1 );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateViewFolder_EmptyBaseFolder()
        {
            Core.FilterRegistry.CreateViewFolder( "a", "", 1 );
        }

        //---------------------------------------------------------------------
        private void CreateNecessaryResources()
        {
            _storage.ResourceTypes.Register( "Email", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "ViewFolder", "", ResourceTypeFlags.Normal );

            _storage.PropTypes.Register( "IsUnread", PropDataType.Bool, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Date", PropDataType.Date, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "UnreadCount", PropDataType.Int, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Category", PropDataType.Link, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Name", PropDataType.String, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "ContentType", PropDataType.String, PropTypeFlags.Internal );
            _storage.PropTypes.Register( "DeepName", PropDataType.String, PropTypeFlags.Normal );

            //  Prepare a list of abstract resources for CreateStandardConditions pool parameters.
            emptyParamsList = Core.ResourceStore.EmptyResourceList;
            paramsList = Core.ResourceStore.EmptyResourceList;
            for( int i = 0; i < 5; i++ )
            {
                IResource emailRes = _storage.BeginNewResource( "Email" );
                emailRes.SetProp( "IsUnread", true );
                emailRes.SetProp( "Date", DateTime.Now );
                emailRes.EndUpdate();

                paramsList = paramsList.Union( emailRes.ToResourceList() );
            }

            //-----------------------------------------------------------------
            filterObject1 = new SentOnly2MeCondition();
        }
    }

    #region Supplementary Classes
    internal class SentOnly2MeCondition : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            IResourceList toContacts = res.GetLinksOfType( "Contact", "To" );
            return( res.GetLinksOfType( "Contact", "CC" ).Count == 0 &&
                toContacts.Count == 1 && 
                toContacts[ 0 ].Id == Core.ContactManager.MySelf.Resource.Id );
        }

        public IResourceList   Filter( string resType )
        {
            throw new System.ApplicationException( "Can not call this condition in List context" );
        }

        public IResourceList   Filter( IResourceList input )
        {
            return Core.ResourceStore.EmptyResourceList;
        }
    }
    #endregion Supplementary Classes
}
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
            Core.FilterManager.RecreateStandardCondition( null, null, null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1NameEmpty()
        {
            Core.FilterManager.RecreateStandardCondition( "", "", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1PropNameNull()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, null, ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1PropNameEmpty()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqNoArguments()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqArgumentEmptyVariant1()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationEqArgumentEmptyVariant2()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "string1", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationGtExactlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Gt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationLtExactlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Lt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasExactlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Has, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationGtOnlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Gt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationLtOnlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Lt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasOnlyOneArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Has );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasPropNoArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasNoPropNoArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasNoProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationHasLinkNoArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.HasLink, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected1()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected2()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange, "string1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeTwoArgumentsExpected3()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.InRange, "string1", "string2", "string3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInRangeNoOrOneArgmentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.QueryMatch, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1OperationInOneOreMoreArgumentExpected()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.In );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnEq()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.Eq, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnGt()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.Gt, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod1CyclicProperiesOnInRange()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property*", ConditionOp.InRange, "x", "y" );
        }

        [Test]
        public void ReCreateStandardConditionsMethod1CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "x", "y" );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterManager.RecreateStandardCondition( "Condition", "Condition", null, "Property", ConditionOp.Eq, "x", "y" );
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
            Core.FilterManager.RecreateStandardCondition( null, null, null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2NameEmpty()
        {
            Core.FilterManager.RecreateStandardCondition( "", "", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2PropNameNull()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2PropNameEmpty()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp1()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp2()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp3()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp4()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2IllegalOp5()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod2NullResourceList()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, (IResourceList)null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod2EmptyResourceList()
        {
            Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, emptyParamsList );
        }

        [Test]
        public void ReCreateStandardConditionsMethod2CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
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
            Core.FilterManager.CreateStandardCondition( null, "DeepName", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3NameEmpty()
        {
            Core.FilterManager.CreateStandardCondition( "", "DeepName", null, "Name", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3PropNameNull()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3PropNameEmpty()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, "1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqNoArguments()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqArgumentEmptyVariant1()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationEqArgumentEmptyVariant2()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "string1", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationGtExactlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationLtExactlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasExactlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationGtOnlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationLtOnlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasOnlyOneArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasPropNoArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasNoPropNoArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasNoProp, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationHasLinkNoArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.HasLink, "string" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected1()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected2()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, "string1" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeTwoArgumentsExpected3()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, "string1", "string2", "string3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInRangeNoOrOneArgmentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, "string1", "string2" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3OperationInOneOreMoreArgumentExpected()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnEq()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.Eq, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnGt()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.Gt, "x" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod3CyclicProperiesOnInRange()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property*", ConditionOp.InRange, "x", "y" );
        }

        [Test]
        public void CreateStandardConditionsMethod3CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "x", "y" );
            IResource res2 = Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "x", "y" );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  RecreateStandardCondition( string name, string[] resTypes, string propName, ConditionOp op, IResourceList values );
        //  Only "Eq" or "In" operations can be passed to this overload.
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NameNull()
        {
            Core.FilterManager.CreateStandardCondition( null, "DeepName", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NameEmpty()
        {
            Core.FilterManager.CreateStandardCondition( "", "DeepName", null, "Name", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4PropNameNull()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, null, ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4PropNameEmpty()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "", ConditionOp.Eq, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp1()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Gt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp2()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Lt, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp3()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Has, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp4()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.InRange, paramsList );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4IllegalOp5()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.QueryMatch, paramsList );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod4NullResourceList()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, (IResourceList)null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod4EmptyResourceList()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, emptyParamsList );
        }

        [Test]
        public void CreateStandardConditionsMethod4CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterManager.RecreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            IResource res2 = Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.In, paramsList );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  CreateQueryCondition( string name, string[] resTypes, string query, string sectionName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5NameNull()
        {
            Core.FilterManager.CreateQueryCondition( null, "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5NameEmpty()
        {
            Core.FilterManager.CreateQueryCondition( "", "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5QueryNull()
        {
            Core.FilterManager.CreateQueryCondition( "Name", "DeepName", null, null, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod5QueryEmpty()
        {
            Core.FilterManager.CreateQueryCondition( "Name", "DeepName", null, "", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod5SectionNameEmpty()
        {
            Core.FilterManager.CreateQueryCondition( "Name", "DeepName", null, "Query", "" );
        }

        [Test]
        public void CreateQueryConditionsMethod5CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterManager.CreateQueryCondition( "Name", "DeepName", null, "Query", null );
            IResource res2 = Core.FilterManager.CreateQueryCondition( "Name", "DeepName", null, "Query", null );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  RecreateQueryCondition( string name, string[] resTypes, string query, string sectionName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6NameNull()
        {
            Core.FilterManager.RecreateQueryCondition( null, "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6NameEmpty()
        {
            Core.FilterManager.RecreateQueryCondition( "", "DeepName", null, "Query", null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6QueryNull()
        {
            Core.FilterManager.RecreateQueryCondition( "Name", "DeepName", null, null, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod6QueryEmpty()
        {
            Core.FilterManager.RecreateQueryCondition( "Name", "DeepName", null, "", null );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod6SectionNameEmpty()
        {
            Core.FilterManager.RecreateQueryCondition( "Name", "DeepName", null, "Query", "" );
        }

        [Test]
        public void ReCreateQueryConditionsMethod6CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterManager.RecreateQueryCondition( "Name", "DeepName", null, "Query", null );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterManager.RecreateQueryCondition( "Name", "DeepName", null, "Query", null );
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
            Core.FilterManager.RegisterCustomCondition( null, "DeepName", null, filterObject1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod7NameEmpty()
        {
            Core.FilterManager.RegisterCustomCondition( "", "DeepName", null, filterObject1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod7ObjectNull()
        {
            Core.FilterManager.RegisterCustomCondition( "Name", "DeepName", null, null );
        }

        //---------------------------------------------------------------------
        //  IResource RecreateConditionTemplate( string name, string[] resTypes, ConditionOp op, params string[] values );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8NameNull()
        {
            Core.FilterManager.RecreateConditionTemplate( null, "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8NameEmpty()
        {
            Core.FilterManager.RecreateConditionTemplate( "", "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamsRequired()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamNull()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, null );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_ParamEmpty()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationEqSemantics_TooManyParams()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name", "Param" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_ParamsRequired1()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_ParamsRequired2()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInSemantics_TooManyParams()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name", "IsUnread", "Other" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_ParamsRequired1()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_ParamsRequired2()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_TooManyParams()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1", "2", "3" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationInRangeSemantics_IllegalPropertyType()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "Name", "1", "2", "3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod8OperationQuerySemantics_TooManyParams()
        {
            Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.QueryMatch, "Name", "1" );
        }

        [Test]
        public void ReCreateConditionTemplate8CheckNewResourceCreated()
        {
            IResource res1 = Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            int Id1 = res1.Id;
            IResource res2 = Core.FilterManager.RecreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
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
            Core.FilterManager.CreateConditionTemplate( null, "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9NameEmpty()
        {
            Core.FilterManager.CreateConditionTemplate( "", "DeepName", null, ConditionOp.Eq );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamsRequired()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamNull()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, null );
        }
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_ParamEmpty()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationEqSemantics_TooManyParams()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name", "Param" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_ParamsRequired1()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_ParamsRequired2()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInSemantics_TooManyParams()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.In, "Name", "IsUnread", "Other" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_ParamsRequired1()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_ParamsRequired2()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_TooManyParams()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "UnreadCount", "1", "2", "3" );
        }
        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationInRangeSemantics_IllegalPropertyType()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.InRange, "Name", "1", "2", "3" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateConditionsWithIllegalParametersMethod9OperationQuerySemantics_TooManyParams()
        {
            Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.QueryMatch, "Name", "1" );
        }

        [Test]
        public void CreateConditionTemplateMethod9CheckOldResourceReturned()
        {
            IResource res1 = Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            IResource res2 = Core.FilterManager.CreateConditionTemplate( "Name", "DeepName", null, ConditionOp.Eq, "Name" );
            Assert.AreEqual( res1.Id, res2.Id );
        }

        //---------------------------------------------------------------------
        //  void  AssociateConditionWithGroup( IResource condition, string groupName );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_NullResource()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterManager.AssociateConditionWithGroup( null, "Group" );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void AssociateConditionWithGroup_IllegalInputResource()
        {
            Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            IResource res = Core.ResourceStore.NewResource( "Email" );
            Core.FilterManager.AssociateConditionWithGroup( res, "Group" );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_IllegalGroupNull()
        {
            IResource condition = Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterManager.AssociateConditionWithGroup( condition, null );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void AssociateConditionWithGroup_IllegalGroupEmpty()
        {
            IResource condition = Core.FilterManager.CreateStandardCondition( "Condition", "DeepName", null, "Property", ConditionOp.Eq, "1" );
            Core.FilterManager.AssociateConditionWithGroup( condition, "" );
        }

        //---------------------------------------------------------------------
        //  IResource CreateViewFolder( string name, string baseFolderName, int order );
        //---------------------------------------------------------------------
        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateViewFolder_NullName()
        {
            Core.FilterManager.CreateViewFolder( null, "a", 1 );
        }

        [Test][ExpectedException(typeof(ArgumentNullException))]
        public void CreateViewFolder_EmptyName()
        {
            Core.FilterManager.CreateViewFolder( "", "a", 1 );
        }

        [Test][ExpectedException(typeof(ArgumentException))]
        public void CreateViewFolder_EmptyBaseFolder()
        {
            Core.FilterManager.CreateViewFolder( "a", "", 1 );
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
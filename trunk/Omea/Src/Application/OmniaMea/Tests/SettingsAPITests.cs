/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace OmniaMea.Tests
{
    /// <summary>
    /// Summary description for ContactNamesTests.
    /// </summary>
    [TestFixture]
    public class SettingsAPITests
    {
        private TestCore _core;
        private IResourceStore _storage;
        private int _SIZE = 0;
        private int _NUM = 0;
        private int _BOOL = 0;
        //private int _STRING = 0;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _storage.ResourceTypes.Register( "Setting", string.Empty );
            _SIZE = _storage.PropTypes.Register( "Size", PropDataType.Int );
            _NUM = _storage.PropTypes.Register( "Num", PropDataType.Int );
            _BOOL = _storage.PropTypes.Register( "Bool", PropDataType.Bool );
            //_STRING = _storage.PropTypes.Register( "String", PropDataType.String );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        class ChangeListener
        {
            private int _count = 0;
            public void list_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
            {
                ++_count;
            }
            public int Count{ get { return _count; } }
        }
        [Test] public void SetStringTest()
        {
            StringSetting setting = new StringSetting( "Test", "String", "Default" );
            setting.Load();
            Assert.AreEqual( "Default", (string)setting.Value );
            setting = new StringSetting( "Test", "String", "" );
            setting.Load();
            Assert.AreEqual( "", (string)setting.Value );
            setting = new StringSetting( "Test", "String" );
            setting.Load();
            Assert.AreEqual( null, (string)setting.Value );
        }

        [Test] public void OneEventTest()
        {
            IResourceList list = Core.ResourceStore.GetAllResourcesLive( "Setting" );
            ChangeListener listener = new ChangeListener();
            list.ResourceChanged += new ResourcePropIndexEventHandler( listener.list_ResourceChanged );
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();
            setting.BeginUpdate();
            IntResourceSetting setting1 = new IntResourceSetting( setting, _SIZE, 30 );
            IntResourceSetting setting2 = new IntResourceSetting( setting, _NUM, 5 );
            setting1.Save( 12 );
            setting2.Save( 13 );
            setting.EndUpdate();
            Assert.AreEqual( 1, listener.Count );
        }
        [Test] public void CompositeSettingTest()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            IntResourceSetting size = new IntResourceSetting( setting, _SIZE, 30 );
            setting.EndUpdate();
            IResource defSetting = Core.ResourceStore.BeginNewResource( "Setting" );
            IntResourceSetting defSize = new IntResourceSetting( defSetting, _SIZE, 100 );
            defSetting.EndUpdate();
            CompositeSetting composite = new CompositeSetting( size, defSize );
            composite.Load();
            Assert.AreEqual( 100, composite.Value );
            composite.Save( 110 );
            Assert.AreEqual( 110, composite.Value );
        }
        [Test] public void SettingArrayTest()
        {
            for ( int i = 0; i < 10; ++i )
            {
                IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
                setting.EndUpdate();
            }

            IResourceList resources = Core.ResourceStore.GetAllResources( "Setting" );
            SettingArray settings = SettingArray.FromResourceList( resources, _SIZE, 100 );
            Assert.AreEqual( false, settings.Defined );
            Assert.AreEqual( 100, settings.Value );
            Assert.AreEqual( false, settings.Different );
            for ( int i = 0; i < 10; ++i )
            {
                IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
                setting.SetProp( _SIZE, i );
                setting.EndUpdate();
            }
            resources = Core.ResourceStore.GetAllResources( "Setting" );
            settings = SettingArray.FromResourceList( resources, _SIZE, 100 );
            Assert.AreEqual( false, settings.Defined );
            Assert.AreEqual( true, settings.Different );
            settings.Save( 333 );
            IResourceList list = Core.ResourceStore.FindResources( "Setting", _SIZE, 333 );
            Assert.AreEqual( 20, list.Count );
            resources = Core.ResourceStore.GetAllResources( "Setting" );
            settings = SettingArray.FromResourceList( resources, _SIZE, 100 );
            Assert.AreEqual( true, settings.Defined );
            Assert.AreEqual( 333, settings.Value );
            Assert.AreEqual( false, settings.Different );
        }
        [Test] public void BoolSettingArrayTest()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();
            setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.SetProp( _BOOL, true );
            setting.EndUpdate();

            IResourceList resources = Core.ResourceStore.GetAllResources( "Setting" );
            SettingArray settings = SettingArray.FromResourceList( resources, _BOOL, true );
            Assert.AreEqual( true, settings.Defined );
            Assert.AreEqual( true, settings.Different );
            settings.Save( true );

            resources = Core.ResourceStore.GetAllResources( "Setting" );
            settings = SettingArray.FromResourceList( resources, _BOOL, true );
            settings.Load();
            Assert.AreEqual( true, settings.Defined );
            Assert.AreEqual( true, settings.Value );
            Assert.AreEqual( false, settings.Different );
        }
        [Test] public void IntAsBoolResourceSettingTest_def_TRUE()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", true ) );
            intSetting.Load();
            Assert.AreEqual( true, intSetting.Value );
        }
        [Test] public void IntAsBoolResourceSettingTest_def_FALSE()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", false ) );
            intSetting.Load();
            Assert.AreEqual( false, intSetting.Value );
        }
        [Test] public void IntAsBoolResourceSettingTest_set_1()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.SetProp( _NUM, 1 );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", false ) );
            intSetting.Load();
            Assert.AreEqual( true, intSetting.Value );
        }
        [Test] public void IntAsBoolResourceSettingTest_set_minus_1()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.SetProp( _NUM, -1 );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", true ) );
            intSetting.Load();
            Assert.AreEqual( false, intSetting.Value );
        }
        [Test] public void IntAsBoolResourceSettingTest_set_true_and_check()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.SetProp( _NUM, -1 );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", true ) );
            intSetting.Load();
            Assert.AreEqual( false, intSetting.Value );
            intSetting.Save( true );
            IResourceList list = Core.ResourceStore.GetAllResources( "Setting" );
            Assert.AreEqual( 1, list.Count );
            Assert.AreEqual( 1, list[0].GetIntProp( _NUM ) );
        }
        [Test] public void IntAsBoolResourceSettingTest_set_false_and_check()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();

            IntAsBoolResourceSetting intSetting = 
                new IntAsBoolResourceSetting( setting, _NUM, new BoolSetting( "Test", "Test", true ) );
            intSetting.Load();
            Assert.AreEqual( true, intSetting.Value );
            intSetting.Save( false );
            IResourceList list = Core.ResourceStore.GetAllResources( "Setting" );
            Assert.AreEqual( 1, list.Count );
            Assert.AreEqual( -1, list[0].GetIntProp( _NUM ) );
        }
        [Test] public void IntAsBoolSettingArrayTest()
        {
            IResource setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.EndUpdate();
            setting = Core.ResourceStore.BeginNewResource( "Setting" );
            setting.SetProp( _NUM, -1 );
            setting.EndUpdate();

            IResourceList resources = Core.ResourceStore.GetAllResources( "Setting" );
            SettingArray settings = SettingArray.IntAsBoolFromResourceList( resources, _NUM, true );
            Assert.AreEqual( false, settings.Defined );
            Assert.AreEqual( true, settings.Different );
            settings.Save( true );

            resources = Core.ResourceStore.GetAllResources( "Setting" );
            settings = SettingArray.IntAsBoolFromResourceList( resources, _NUM, true );
            settings.Load();
            Assert.AreEqual( true, settings.Defined );
            Assert.AreEqual( true, settings.Value );
            Assert.AreEqual( false, settings.Different );
        }
    }
}

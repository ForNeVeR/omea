/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class OutlookFlags
    {
        static private readonly ResourceFlag _defaultFlag = ResourceFlag.DefaultFlag;
        static private readonly ResourceFlag _completedFlag;
        static private readonly ArrayList _flags = new ArrayList();
        static private readonly HashMap _resId2ColorIndex = new HashMap();
        private OutlookFlags(){}
        static OutlookFlags()
        {
            _completedFlag = new ResourceFlag( "CompletedFlag"  );

            AddFlag( "PurpleFlag" );
            AddFlag( "OrangeFlag" );
            AddFlag( "GreenFlag" );
            AddFlag( "YellowFlag" );
            AddFlag( "BlueFlag" );
            AddFlag( ResourceFlag.DefaultFlag.FlagId );
        }
        static private void AddFlag( string flagID )
        {
            Guard.NullArgument( flagID, "flagID" );
            ResourceFlag resourceFlag = new ResourceFlag( flagID );
            int index = _flags.Add( resourceFlag );
            _resId2ColorIndex.Add( resourceFlag.FlagId, index+1 );
        }
        static public void SetCompletedFlag( IResource resource )
        {
            Guard.NullArgument( resource, "resource" );
            _completedFlag.SetOnResource( resource );
        }
        static public void SetDefaultFlag( IResource resource )
        {
            Guard.NullArgument( resource, "resource" );
            _defaultFlag.SetOnResource( resource );
        }
        static public void ClearFlag( IResource resource )
        {
            Guard.NullArgument( resource, "resource" );
            ResourceFlag.Clear( resource );
        }
        static public int GetColorIndex( IResource resource )
        {
            Guard.NullArgument( resource, "resource" );
            HashMap.Entry entry = _resId2ColorIndex.GetEntry( resource.GetPropText( ResourceFlag._propFlagId ) );
            if ( entry != null )
            {
                return (int)entry.Value;
            }
            return 6;
        }
        static public bool IsCustomFlagSet( IResource resource )
        {
            Guard.NullArgument( resource, "resource" );
            ResourceFlag flag = ResourceFlag.GetResourceFlag( resource );
            return ( flag != null && !IsCompletedFlag( flag ) );
        }
        static public bool IsCompletedFlag( IResource resFlag )
        {
            Guard.NullArgument( resFlag, "resFlag" );
            return ( resFlag.GetPropText( ResourceFlag._propFlagId ).CompareTo( _completedFlag.FlagId ) == 0 );
        }
        static public bool IsCompletedFlag( ResourceFlag flag )
        {
            return ( flag.FlagId.CompareTo( _completedFlag.FlagId ) == 0 );
        }
        static public void SetOnResource( IResource resource, int index )
        {
            Guard.NullArgument( resource, "resource" );
            index--;

            if ( index >= 0 && index < _flags.Count )
            {
                ((ResourceFlag)_flags[index]).SetOnResource( resource );
            }
        }
    }
}

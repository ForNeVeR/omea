// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	public class DocSectionHelper
	{
        private const string _cResType = "DocumentSection";
        public static bool IsShortNameExist( string shortName )
        {
            IResource section = Core.ResourceStore.FindUniqueResource( _cResType, "SectionShortName", shortName );
            return section != null;
        }
        public static bool IsFullNameExist( string fullName )
        {
            IResource section = Core.ResourceStore.FindUniqueResource( _cResType, Core.Props.Name, fullName );
            return section != null;
        }
        public static uint OrderByFullName( string fullName )
        {
            uint sectionId;
            IResource section = Core.ResourceStore.FindUniqueResource( _cResType, Core.Props.Name, fullName );
            if( section != null )
                sectionId = (uint)section.GetIntProp( "SectionOrder" );
            else
                throw new ArgumentException( "DocSection Conversion - no such section with full name: [" + fullName + "]" );
            return sectionId;
        }
		public static uint OrderByShortName( string shortName )
        {
            uint sectionId;
            IResource section = Core.ResourceStore.FindUniqueResource( _cResType, "SectionShortName", shortName );
            if( section != null )
                sectionId = (uint)section.GetIntProp( "SectionOrder" );
            else
                throw new ArgumentException( "DocSection Conversion - no such section with short name: [" + shortName + "]" );
            return sectionId;
        }
		public static string FullNameByOrder( uint sectionId )
        {
            string name;
            IResource section = Core.ResourceStore.FindUniqueResource( _cResType, "SectionOrder", (int)sectionId );
            if( section != null )
                name = section.GetStringProp( Core.Props.Name );
            else
                throw new ArgumentException( "DocSection Conversion - no such section with order number: [" + sectionId + "]" );
            return name;
        }
	}
}

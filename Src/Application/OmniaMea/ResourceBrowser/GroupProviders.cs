// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    internal abstract class PropGroupProvider: IGroupProvider
    {
        protected int _propId;

        protected PropGroupProvider( int propId )
        {
            _propId = propId;
        }

        string IGroupProvider.GetGroupName( object item )
        {
            IResource res = (IResource) item;
            if ( !res.HasProp( _propId ) )
            {
                return "No " + Core.ResourceStore.PropTypes.GetPropDisplayName( _propId );
            }
            return GetGroupName( res );
        }

        protected abstract string GetGroupName( IResource res );

        public override bool Equals( object obj )
        {
            if ( GetType() != obj.GetType() )
            {
                return false;
            }
            PropGroupProvider pgp = obj as PropGroupProvider;
            return pgp._propId == _propId;
        }

        public override int GetHashCode()
        {
            return _propId;
        }
    }


    internal class DateGroupProvider: PropGroupProvider
    {
        public DateGroupProvider( int propId ) : base( propId )
        {
        }

        protected override string GetGroupName( IResource res )
        {
            DateTime dt = res.GetDateProp( _propId );
            DateTime today = DateTime.Today;
            if ( dt.Date > today )
            {
                return "Future";
            }
            if ( dt.Date == today )
            {
                return "Today";
            }
            if ( dt.Date == today.AddDays( -1 ) )
            {
                return "Yesterday";
            }
            if ( dt.Year == today.Year )
            {
                int dateWeek = GetWeekOfYear( dt );
                int thisWeek = GetWeekOfYear( DateTime.Today );
                if ( dateWeek == thisWeek )
                {
                    return dt.ToString( "dddd", CultureInfo.InvariantCulture );
                }
                if ( dateWeek == thisWeek-1 )
                {
                    return "Last Week";
                }
                if ( dt.Month == DateTime.Today.Month )
                {
                    return "This Month";
                }
            }

            return "Older";
        }

        private int GetWeekOfYear( DateTime dt )
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return ci.Calendar.GetWeekOfYear( dt, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek );
        }
    }

    internal class PropTextGroupProvider: PropGroupProvider
    {
        public PropTextGroupProvider( int propId ) : base( propId )
        {
        }

        protected override string GetGroupName( IResource res )
        {
            return res.GetPropText( _propId );
        }
    }

    internal class ResourceTypeGroupProvider: IGroupProvider
    {
        public string GetGroupName( object item )
        {
            IResource res = (IResource) item;
            return Core.ResourceStore.ResourceTypes [res.Type].DisplayName;
        }
    }

    internal class DisplayNameGroupProvider: IGroupProvider
    {
        public string GetGroupName( object item )
        {
            IResource res = (IResource) item;
            return res.DisplayName;
        }
    }
}

/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Interop;

namespace JetBrains.Omea
{
    /// <summary>
    /// Custom column formatting logic.
    /// </summary>
    internal class ColumnFormatter
	{
        private const String _GroupName = "Omea";
        private const String _HeaderName = "General";
        private const int _LongDateMarginForCompaction = 16;
        private const int _ShortDateMarginForCompaction = 10;

		private static ColumnFormatter _theFormatter;

        private readonly IDisplayColumnManager _columnManager;
        private static readonly StringBuilder _timeStringBuilder = new StringBuilder(255);
        private IResourceList _customProperties;
        private static bool     _useShortDate;
        
        private ColumnFormatter()
		{
            _columnManager = Core.DisplayColumnManager;
            RereadDateFormatOption( null, null );
            Core.UIManager.AddOptionsChangesListener( _GroupName, _HeaderName, RereadDateFormatOption );
		}

        private static void RereadDateFormatOption(object sender, EventArgs e)
        {
            _useShortDate = Core.SettingStore.ReadBool("Resources", "UseShortDateFormat", false);
        }

        internal static ColumnFormatter GetInstance()
        {
            if ( _theFormatter == null )
            {
                _theFormatter = new ColumnFormatter();
            }
            return _theFormatter;
        }

        internal void RegisterFormatters()
        {
            _columnManager.RegisterPropertyToTextCallback( Core.Props.Date, DateToString );
            _columnManager.RegisterPropertyToTextCallback( Core.Props.Size, SizeString );

            _customProperties = ResourceTypeHelper.GetCustomProperties();
            foreach( IResource res in _customProperties )
            {
                int propTypeID = res.GetIntProp( "ID" );
                IPropType propType = Core.ResourceStore.PropTypes [propTypeID];
                if ( propType.DataType == PropDataType.Bool )
                {
                    _columnManager.RegisterPropertyToTextCallback( propTypeID, BoolPropToString );
                }
            }
            _customProperties.ResourceAdded += OnCustomPropertyAdded;
        }

	    private void OnCustomPropertyAdded( object sender, ResourceIndexEventArgs e )
	    {
            int propTypeID = e.Resource.GetIntProp( "ID" );
            IPropType propType = ICore.Instance.ResourceStore.PropTypes [propTypeID];
            if ( propType.DataType == PropDataType.Bool )
            {
                _columnManager.RegisterPropertyToTextCallback( propTypeID, BoolPropToString );
            }
	    }

	    private static string DateToString( IResource res, int propID, int widthInChars )
        {
            DateTime dt = res.GetDateProp( propID );
            if( dt == DateTime.MinValue )
            {
                return string.Empty;
            }

            if ( dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0 )
            {
            	return dt.ToShortDateString();
            }

        	DateTime today = DateTime.Today;
        	TimeSpan ts = today - dt.Date;
            
            string dateStr;
            
            if ( ts.Days == 0 )
            {
                dateStr = "Today";
            }
            else
            if ( dt.Date.Year == today.Year && haveToShortenDateString( widthInChars ) )
            {
                byte iDate, iDayLZero;
                Win32Declarations.GetLocaleInfo( Win32Declarations.LOCALE_USER_DEFAULT,
                    Win32Declarations.LOCALE_IDATE, out iDate, 1 );
                Win32Declarations.GetLocaleInfo( Win32Declarations.LOCALE_USER_DEFAULT,
                    Win32Declarations.LOCALE_IDAYLZERO, out iDayLZero, 1 );
                
                string dayFormat = (iDayLZero == '1') ? "dd" : "d";
                string monthFormat = "MMM";
                
                if ( iDate == '0' || iDate == '2' )
                {
                    dateStr = dt.ToString( monthFormat + /*"/"*/" " + dayFormat );
                }
                else
                {
                    dateStr = dt.ToString( dayFormat + /*"/"*/" " + monthFormat );
                }
            }
            else
            {
                dateStr = dt.ToShortDateString();
            }

            if (!_useShortDate )
            {
                dateStr += " " + TimeToStringWin32(dt);
            }

            return dateStr;
        }

        private static bool haveToShortenDateString( int widthInChars )
        {
            return widthInChars < ((_useShortDate) ? _ShortDateMarginForCompaction : _LongDateMarginForCompaction);
        }
                
        ///<Summary>
        /// Converts a time to a string using the Win32 API which correctly handles 
        /// locale customization. (The default .NET APIs take only the base user locale
        /// and ignore the customizations.)
        ///</Summary>
        private static string TimeToStringWin32( DateTime dt )
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.Hour = (short) dt.Hour;
            st.Minute = (short) dt.Minute;
            lock( _timeStringBuilder )
            {
                _timeStringBuilder.Length = 0;
                Win32Declarations.GetTimeFormat( Win32Declarations.LOCALE_USER_DEFAULT,
                    Win32Declarations.TIME_NOSECONDS, ref st, null, _timeStringBuilder, 255 );
                return _timeStringBuilder.ToString();
            }
        }

        private static string SizeString( IResource res, int propID )
        {
            if( !res.HasProp( propID ) )
            {
                return string.Empty;
            }
            int size = res.GetIntProp( propID );
            return Utils.SizeToString( size );
        }

        private static string BoolPropToString( IResource res, int propID )
        {
            return res.HasProp( propID ) ? "Yes" : "";
        }
    }
}

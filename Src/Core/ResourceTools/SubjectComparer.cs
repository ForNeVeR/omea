// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Comparer for the values of "Subject" column which ignores prefixes like
	/// "Re" and "Fw". User has the option to configure its own (localized) mail
	/// prefixes like "SV" in Swedish.
	/// </summary>
	public class SubjectComparer: IResourceComparer, IResourceGroupProvider
	{
	    public const String csIniSection = "General";
	    public const String csIniKey = "SubjectPrefixes";
        public const String csDefaultPrefixes = "re; fw;";
	    public const String csNoSubjectGroup = "<none>";

        private static HashSet _prefixes = new HashSet();

	    public int CompareResources( IResource r1, IResource r2 )
	    {
	        bool hasPrefix1, hasPrefix2;
	        string normSubj1 = NormalizeSubject( r1, out hasPrefix1 );
	        string normSubj2 = NormalizeSubject( r2, out hasPrefix2 );
	        int rc = String.Compare( normSubj1, normSubj2, true );
            if ( rc == 0 )
            {
                if ( !hasPrefix1 && hasPrefix2 )
                    return -1;

                if ( hasPrefix1 && !hasPrefix2 )
                    return 1;
            }
            return rc;
	    }

	    public string GetGroupName( IResource res )
	    {
	        bool   hasPrefix;
	        string groupName = NormalizeSubject( res, out hasPrefix );

	        return String.IsNullOrEmpty( groupName ) ? csNoSubjectGroup : groupName;
	    }

	    private static string NormalizeSubject( IResource resource, out bool hasPrefix )
	    {
	        CheckSubjectPrefixes();

            hasPrefix = false;
            string subj = resource.GetStringProp( Core.Props.Subject );
            if( subj != null )
            {
                string prefix = FindPrefix( subj );
                if( prefix != null )
                {
                    //  Skip prefixes with enumeration like "Re[3]"
                    int pos = prefix.Length;
                    while( pos < subj.Length && IsDelimiter( subj, pos ) )
                    {
                        pos++;
                    }
                    hasPrefix = true;
                    subj = subj.Substring( pos );
                }
            }
            return subj;
        }

        //---------------------------------------------------------------------
        //  If the hash is empty then we did not initialize the prefixes yet.
        //  Perform lazy initialization of the has with the string values stored
        //  in the ini file.
        //---------------------------------------------------------------------
        private static void CheckSubjectPrefixes()
        {
            if( _prefixes.Count == 0 )
            {
                SubjectPrefixes = Core.SettingStore.ReadString(csIniSection, csIniKey, csDefaultPrefixes);
            }
        }

        public static String SubjectPrefixes
	    {
	        set
	        {
                _prefixes.Clear();
                string[] prefixes = value.Split( ';' );
                foreach( string prefix in prefixes )
                {
                    string pref = prefix.Trim();
                    if( !String.IsNullOrEmpty( pref ) )
                    {
                        _prefixes.Add( pref );
                    }
                }
	        }
	    }

        private static bool IsDelimiter( string subj, int pos )
        {
            return Char.IsWhiteSpace( subj, pos ) ||
                   Char.IsPunctuation( subj, pos ) ||
                   Char.IsDigit( subj, pos );
        }

	    private static string FindPrefix( string str )
        {
	        CompareInfo ci = CultureInfo.CurrentCulture.CompareInfo;
            foreach( HashSet.Entry e in _prefixes )
            {
                string prefix = (string)e.Key;
                if (ci.IsPrefix(str, prefix, CompareOptions.IgnoreCase))
                    return prefix;
            }
            return null;
        }
    }
}

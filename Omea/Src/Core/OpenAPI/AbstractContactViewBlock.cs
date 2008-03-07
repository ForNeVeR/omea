/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Base class for blocks displayed in contact edit/view panes.
    /// </summary>
   
    public class AbstractContactViewBlock : AbstractEditPane
	{
        /// <summary>
        /// Queries the contact block whether the user changed any data in it.
        /// </summary>
        /// <returns>true if the data was changed by the user, false otherwise.</returns>
        public virtual bool IsChanged() { return true; }

        /// <summary>
        /// Check whether this block owns (processes and displays) the
        /// partucular property of a contact resource.
        /// </summary>
        /// <param name="propId">Id of a property.</param>
        /// <returns>Return true if contact block is responsible for the given property.</returns>
        /// <since>2.0</since>
        public virtual bool OwnsProperty( int propId ) { throw new NotImplementedException(); }

        /// <summary>
        /// Get an html representation of the block of information on the
        /// united Contact View pane.
        /// </summary>
        /// <since>2.1.5</since>
        public virtual string  HtmlContent( IResource contact ){  throw new NotImplementedException(); }

        /// <summary>
        /// Construct simple pair of paragraphs Name/Value placed in one line
        /// one after another. If requested property is not set show a standard
        /// placeholder.
        /// </summary>
        /// <param name="res">A resource which property to show.</param>
        /// <param name="head">Pair heading title.</param>
        /// <param name="prop">Property id.</param>
        /// <returns>Html representation of the Name/Value property value.</returns>
        /// <since>2.1.5</since>
        protected static string  ObligatoryTag( IResource res, string head, int prop )
        {
            string result = "\t<tr><td>" + head + "</td>";
            string text = res.GetPropText( prop );
            result += (text.Length > 0) ? "<td class=\"name\">" + text + "</td>" : ContactViewStandardTags.NotSpecifiedHtmlText;
            result += "</tr>";
            return result;
        }

        /// <summary>
        /// Construct simple pair of paragraphs Name/Value placed in one line
        /// one after another. If requested property is not set nothing constructed.
        /// </summary>
        /// <param name="res">A resource which property to show.</param>
        /// <param name="head">Pair heading title.</param>
        /// <param name="prop">Property id.</param>
        /// <returns>Html representation of the Name/Value property value.</returns>
        /// <since>2.1.5</since>
        protected static string OptionalTag( IResource res, string head, int prop )
        {
            string result = string.Empty;
            string text = res.GetPropText( prop );
            if( text.Length > 0 )
            {
                result += "\t<tr><td>" + head + "</td><td class=\"name\">" + text + "</td></tr>";
            }
            return result;
        }
    }

    public class ContactViewStandardTags
    {
        /// <summary>
        /// Predefined formatting of the property value when it is not defined.
        /// </summary>
        public const string NotSpecifiedHtmlText = "<td class=\"name not\">Not specified</td>";
    }
}

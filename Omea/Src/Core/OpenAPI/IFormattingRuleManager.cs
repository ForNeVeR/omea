/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Manage the rules which control switching of tray icon (in the taskbar
    /// area) upon some conditions.
    /// </summary>
    /// <since>437</since>
    public interface IFormattingRuleManager
    {
        /// <summary>
        /// Register a formatting rule which changes the appearance of a resource in the 
        /// list whenever it matches the condition(s) of the rule.
        /// </summary>
        /// <param name="name">Name of a formatting rule.</param>
        /// <param name="resTypes">A list of resource types valid for a watcher.</param>
        /// <param name="conditions">Conditions necessary to be matched for a resource.</param>
        /// <param name="exceptions">Exceptions necessary to be matched for a resource.</param>
        /// <param name="isBold"></param>
        /// <param name="isItalic"></param>
        /// <param name="isUnderlined"></param>
        /// <param name="isStrikeout"></param>
        /// <param name="foreColor"></param>
        /// <param name="backColor"></param>
        IResource  RegisterRule( string name, string[] resTypes, IResource[] conditions, IResource[] exceptions,
                                 bool isBold, bool isItalic, bool isUnderlined, bool isStrikeout,
                                 string foreColor, string backColor );

        /// <summary>
        /// Reregister a formatting rule - do not create new resource but rather
        /// reset the parameters of the existing one. Required by IFilteringForms API behavior.
        /// </summary>
        /// <param name="baseRes">Resource of an existing rule.</param>
        /// <param name="name">Name of a formatting rule.</param>
        /// <param name="resTypes">A list of resource types valid for a watcher.</param>
        /// <param name="conditions">Conditions necessary to be matched for a resource.</param>
        /// <param name="exceptions">Exceptions necessary to be matched for a resource.</param>
        /// <param name="isBold"></param>
        /// <param name="isItalic"></param>
        /// <param name="isUnderlined"></param>
        /// <param name="isStrikeout"></param>
        /// <param name="foreColor"></param>
        /// <param name="backColor"></param>
        /// <since>539</since>
        IResource  ReregisterRule( IResource baseRes, string name, string[] resTypes, IResource[] conditions, IResource[] exceptions,
                                   bool isBold, bool isItalic, bool isUnderlined, bool isStrikeout, string foreColor, string backColor );

        /// <summary>
        /// Unregister a formatting rule.
        /// </summary>
        /// <param name="name">Name of a formatting rule.</param>
        void    UnregisterRule( string name );

        /// <summary>
        /// Check whether there exists (registered) a formatting rule with the given name.
        /// </summary>
        /// <param name="name">Name of a formatting rule.</param>
        /// <returns>True if the a rule with the given name is registered already.</returns>
        bool    IsRuleRegistered( string name );

        /// <summary>
        /// Find a resource corresponding to the formatting rule with the given name.
        /// </summary>
        /// <param name="name">Name of a formatting rule.</param>
        /// <returns>A resource corresponding to the formatting rule name.
        /// Null if there is no such formatting rule.</returns>
        IResource FindRule( string name );

        /// <summary>
        /// Rename a rule.
        /// </summary>
        /// <param name="rule">A resource representing a rule (returned by RegisterRule method).</param>
        /// <param name="newName">New name for a resource.</param>
        /// <throws>Throws ArgumentException object if the rule with the new name already exists.</throws>
        /// <since>548</since>
        void        RenameRule( IResource rule, string newName );

        /// <summary>
        /// Creates new Formatting rule and clones all necessary information into
        /// the new destination.
        /// </summary>
        /// <param name="sourceRule">Resource from which the information will be cloned.</param>
        /// <param name="newName">Name of a new Formatting rule.</param>
        /// <since>501</since>
        /// <returns>A resource for a new rule.</returns>
        IResource  CloneRule( IResource sourceRule, string newName );

        /// <summary>
        /// Copies formatting information from one formatting rule to the other.
        /// </summary>
        /// <param name="fromRule">Source rule from which the formatting information is taken.</param>
        /// <param name="toRule">Destination rule to which the formatting information is written.</param>
        void  CloneFormatting( IResource fromRule, IResource toRule );

        /// <summary>
        /// Applies the formatting rules to the specified resource and returns the
        /// item format.
        /// </summary>
        /// <param name="res">The resource for which the formatting rules are applied.</param>
        /// <returns>The format, or null if the default format should be used.</returns>
        ItemFormat  GetFormattingInfo( IResource res );

        /// <summary>
        /// Occurs when the set of formatting rules in the system has changed.
        /// </summary>
        event EventHandler FormattingRulesChanged;
    }

    /// <summary>
    /// Describes an item format set by a formatting rule.
    /// </summary>
    public class ItemFormat
    {
        private FontStyle _fontStyle;
        private Color     _foreColor, _backColor;

        /// <summary>
        /// Create a new formatting item with default font and colour characteristics.
        /// </summary>
        public ItemFormat()
        {
            _fontStyle = FontStyle.Regular;
            _foreColor = SystemColors.WindowText;
            _backColor = SystemColors.Window;
        }

        /// <summary>
        /// Create a new formatting item with defined font and colour characteristics.
        /// </summary>
        public ItemFormat( FontStyle fontStyle, Color foreColor, Color backColor )
        {
            _fontStyle = fontStyle;
            _foreColor = foreColor;
            _backColor = backColor;
        }

        /// <summary>
        /// Set or get the FontStyle attribute for the formatting.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return _fontStyle; }
            set { _fontStyle = value; }
        }

        /// <summary>
        /// Set or get the foreground color for the formatting.
        /// </summary>
        public Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; }
        }

        /// <summary>
        /// Set or get the background color for the formatting.
        /// </summary>
        public Color BackColor
        {
            get { return _backColor; }
            set { _backColor = value; }
        }
    }
}

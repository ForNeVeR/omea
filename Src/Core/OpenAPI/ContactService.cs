// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Set of predefined named for tabs in ContactEdit form.
    /// </summary>
    public class ContactTabNames
    {
        public const string GeneralTab = "Contact";
        public const string PersonalTab = "Personal Information";
        public const string MailingTab = "Mailing Address";
    }

    /// <summary>
    /// Allows to register custom blocks shown in contact display and edit panes.
    /// </summary>
    public interface IContactService
    {
        /// <summary>
        /// Registers a custom block shown in contact display pane.
        /// </summary>
        /// <param name="column">The index of the column where the block is displayed.</param>
        /// <param name="anchor">The position of the block relative to other blocks.</param>
        /// <param name="blockId">The ID of the block.</param>
        /// <param name="blockCreator">The delegate for creating instances of the block.</param>
        void RegisterContactEditBlock( int column, ListAnchor anchor, string blockId, ContactBlockCreator blockCreator );

        /// <summary>
        /// Registers a custom block shown in contact edit pane.
        /// </summary>
        /// <param name="tabName">The name of the tab where the block is displayed.</param>
        /// <param name="anchor">The position of the block relative to other blocks.</param>
        /// <param name="blockId">The ID of the block.</param>
        /// <param name="blockCreator">The delegate for creating instances of the block.</param>
        void RegisterContactEditBlock(string tabName, ListAnchor anchor, string blockId, ContactBlockCreator blockCreator);
    }

    /// <summary>
    /// Represents the method which creates an instance of <see cref="AbstractContactViewBlock"/>
    /// for showing in contact display and edit panes.
    /// </summary>
    public delegate AbstractContactViewBlock ContactBlockCreator();
}

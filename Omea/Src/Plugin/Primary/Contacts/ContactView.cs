// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ContactsPlugin
{
    public class ContactView: AbstractEditPane, IContactTabBlockContainer
    {
        private System.ComponentModel.Container components = null;

        private TabControl _contentPages;
        private readonly Dictionary<string, TabPage> _pages = new Dictionary<string, TabPage>();

        private readonly ArrayList  _contactBlocks = new ArrayList();
        private readonly HashMap    _blockValidationErrors = new HashMap();

        private ContactBO           _contact;

        public ContactView()
        {
            InitializeComponent();

            ContactService.GetInstance().CreateContactBlocks( this );
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                foreach( AbstractContactViewBlock block in _contactBlocks )
                {
                    block.ValidStateChanged -= block_ValidStateChanged;
                    block.SizeChanged -= OnBlockSizeChanged;
                }
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _contentPages = new TabControl();
            this.SuspendLayout();
            //
            // _contentPages
            //
            _contentPages.Dock = DockStyle.Fill;
            _contentPages.Name = "_contentPages";
            //
            // ContactView
            //
            this.Controls.Add(this._contentPages);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "ContactView";
            this.Size = new System.Drawing.Size(600, 336);
            this.ResumeLayout(false);

        }
        #endregion

        void IContactTabBlockContainer.AddContactBlock( string tabName, string caption, AbstractContactViewBlock block )
        {
            TabPage page;
            if( _pages.ContainsKey( tabName ))
                page = _pages[ tabName ];
            else
            {
                page = new TabPage( tabName );
                page.AutoScroll = true;
                _contentPages.TabPages.Add( page );
                _pages[ tabName ] = page;
            }

            block.Location = new Point(4, 12);
            block.Dock = DockStyle.Fill;

            //  Decorative panes delimit groupboxes from each other so that
            //  the bottom of the upper box is not visually merged with the
            //  top of the lower one.
            Panel decorativeShift = new Panel();
            decorativeShift.Dock = DockStyle.Top;
            decorativeShift.Size = new Size( block.Width + 8, 10 );
            page.Controls.Add( decorativeShift );
            page.Controls.SetChildIndex( decorativeShift, 0 );

            GroupBox blockGroupBox = new GroupBox();
            blockGroupBox.Dock = DockStyle.Top;
            blockGroupBox.Text = caption;
            blockGroupBox.Size = new Size(block.Width + 8, block.Height + (int)(20 * Core.ScaleFactor.Height));
            blockGroupBox.FlatStyle = FlatStyle.System;
            blockGroupBox.Controls.Add(block);

            page.Controls.Add( blockGroupBox );
            page.Controls.SetChildIndex( blockGroupBox, 0 );
            _contactBlocks.Add( block );

            block.ValidStateChanged += block_ValidStateChanged;
            block.SizeChanged += OnBlockSizeChanged;
        }

        /**
         * Propagates a valid state change notification from one of the contact blocks
         * to the entire pane.
         */

        private void block_ValidStateChanged( object sender, ValidStateEventArgs e )
        {
            if ( e.IsValid )
            {
                _blockValidationErrors.Remove( sender );
                if ( _blockValidationErrors.Count > 0 )
                {
                    IEnumerator errEnumerator = _blockValidationErrors.GetEnumerator();
                    errEnumerator.MoveNext();
                    HashMap.Entry entry = ( HashMap.Entry ) errEnumerator.Current;
                    OnValidStateChanged( (ValidStateEventArgs)entry.Value );
                }
                else
                {
                    OnValidStateChanged( new ValidStateEventArgs( true ) );
                }
            }
            else
            {
                _blockValidationErrors[ sender ] = e;
                OnValidStateChanged( e );
            }
        }

        /**
         * When the size of a block changes, shifts blocks below it up or down.
         */

        private void OnBlockSizeChanged( object sender, EventArgs e )
        {
            if( Width != 0 && Height != 0 )
            {
                AbstractContactViewBlock senderBlock = (AbstractContactViewBlock) sender;
                GroupBox blockGroupBox = (GroupBox) senderBlock.Parent;
                blockGroupBox.Height = senderBlock.Height + (int)(20 * Core.ScaleFactor.Height);
            }
        }

        public override void EditResource( IResource res )
        {
            _contact = new ContactBO( res );
            foreach( AbstractContactViewBlock block in _contactBlocks )
            {
                block.EditResource( _contact.Resource );
            }
        }

        public override void Save()
        {
            //  _contact.Changed ALWAYS returns false in the current
            //  implementation because although we create a ContacBO for
            //  a resource here, we pass its resource object to all blocks,
            //  thus all changes are missed from change-control mechanism of
            //  ContactBO (IContact) interface. Thus we have to rely on
            //  IsChanged method of each block and artificially call text
            //  indexing.
            bool contentChanged = _contact.Changed;
            foreach( AbstractContactViewBlock block in _contactBlocks )
                contentChanged = contentChanged || block.IsChanged();

            Core.ResourceAP.RunJob( new MethodInvoker( SaveContact ) );
        }

        public override void Cancel()
        {
            Core.ResourceAP.RunJob( new MethodInvoker( CancelContactBlocks ) );
        }

        private void SaveContact()
        {
            //  Special attention is payed to the asynchronous nature
            //  of this Form per se. In some contexts, saving of the
            //  resource can be activated (not queued) after the actual
            //  resource is deleted. We consider that simple check whether
            //  the resource to be saved is ALIVE is enough
            if( !_contact.Resource.IsDeleted && !_contact.Resource.IsDeleting )
            {
                _contact.BeginUpdate();
                foreach( AbstractContactViewBlock block in _contactBlocks )
                {
                    block.Save();
                }
                _contact.EndUpdate();

                //  NB: IContact.EndUpdate checks whether the body has
                //      changed and automatically requests TI.
                //      Core.TextIndexManager.QueryIndexing( _contact.ID );
            }
        }

        private void CancelContactBlocks()
        {
            if( !_contact.Resource.IsDeleted && !_contact.Resource.IsDeleting )
            {
                _contact.Resource.BeginUpdate();
                foreach( AbstractContactViewBlock block in _contactBlocks )
                {
                    block.Cancel();
                }
                _contact.Resource.EndUpdate();
            }
        }
    }
}

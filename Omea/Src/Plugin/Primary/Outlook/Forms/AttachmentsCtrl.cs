// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

using SystemMetricsCodes=JetBrains.Interop.WinApi.SystemMetricsCodes;

namespace JetBrains.Omea.OutlookPlugin
{
    /// <summary>
    /// Summary description for AttachmentsCtrl.
    /// </summary>
    public class AttachmentsCtrl: AbstractViewPane
    {
        private class AttachmentType
        {
            private string _name;
            private string[] _exts;
            private ArrayList _foundExts = new ArrayList();

            public AttachmentType( ISettingStore settings, int index )
            {
                _name = settings.ReadString( "Attachments", "Attachment" + index + "Name" );

                string exts = settings.ReadString( "Attachments", "Attachment" + index + "Exts" );
                _exts = exts.Split(',');
            }

            public AttachmentType( string name )
            {
                _name = name;
                _exts = null;
            }

            /**
             * Checks if the specified extension falls into the group specified in the
             * INI. If it does, adds it to the found extensions list and returns true.
             */

            public bool AddResource( IResource res )
            {
                // this branch is used by the "Other" attachment type
                if ( _exts == null )
                {
                    _foundExts.Add( res );
                    return true;
                }
                string ext = res.GetStringProp( "Name" );
                if ( ext == null )
                    return false;

                foreach( string myExt in _exts )
                {
                    if ( string.Compare( myExt, ext, true ) == 0 )
                    {
                        _foundExts.Add( res );
                        return true;
                    }
                }
                return false;
            }

            /**
             * Returns the list of mails that have attachments of one of the types
             * listed in the group.
             */

            public IResourceList GetMails()
            {
                IResourceList mails = null;
                foreach( IResource res in _foundExts )
                {
                    IResourceList rlist = res.GetLinksOfTypeLive( null, STR.AttachmentType );
                    mails = rlist.Union( mails, true );
                }
                return mails;
            }

            public string Name
            {
                get { return _name; }
            }

            public int FoundExtCount
            {
                get { return _foundExts.Count; }
            }

        }

        private ArrayList _attachmentTypes = new ArrayList();
        private AttachmentType _lastAttachmentType = null;
        private IResourceList _lastResourceList = null;
        private string _lastCaption;
        private ListView _listQueries;
        private System.Windows.Forms.ColumnHeader _columnHeader;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public AttachmentsCtrl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
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
            this._listQueries = new System.Windows.Forms.ListView();
            this._columnHeader = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            //
            // _listQueries
            //
            this._listQueries.BorderStyle = BorderStyle.None;
            this._listQueries.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                           this._columnHeader});
            this._listQueries.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listQueries.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._listQueries.FullRowSelect = true;
            this._listQueries.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._listQueries.Location = new System.Drawing.Point(0, 0);
            this._listQueries.Name = "_listQueries";
            this._listQueries.Size = new System.Drawing.Size(150, 150);
            this._listQueries.TabIndex = 0;
            this._listQueries.View = System.Windows.Forms.View.Details;
            this._listQueries.KeyDown += new System.Windows.Forms.KeyEventHandler(this._listQueries_KeyDown);
            this._listQueries.Layout += new System.Windows.Forms.LayoutEventHandler(this._listQueries_Layout);
            //
            // AttachmentsCtrl
            //
            this.Controls.Add(this._listQueries);
            this.Name = "AttachmentsCtrl";
            this.Enter += new System.EventHandler(this.AttachmentsCtrl_Enter);
            this.ResumeLayout(false);

        }
        #endregion

        public override void Populate()
        {
            if ( Core.SettingStore.ReadInt( "Attachments", "TypeCount", 0 ) == 0 )
            {
                WriteDefaultAttachmentTypes();
            }

            // begin 2.0 -> 2.1 correction
            string str = Core.SettingStore.ReadString( "Attachments", "Attachment2Exts", string.Empty );
            if( str.IndexOf( ".ico" ) == -1 )
            {
                str += ".ico," + str;
                WriteAttachmentType( Core.SettingStore, 2, "Images", str );
            }
            // end 2.0 -> 2.1 correction

            ReadAttachmentTypes();
            GroupExistingAttachmentTypes();

            foreach( AttachmentType attType in _attachmentTypes )
            {
                if ( attType.FoundExtCount > 0 )
                {
                    AddAttachmentType( attType.Name, attType );
                }
            }

            _listQueries.SmallImageList = Core.ResourceIconManager.ImageList;

            _listQueries.SelectedIndexChanged += new EventHandler( OnSelectedIndexChanged );
        }

        private ListViewItem AddAttachmentType( string itemText, AttachmentType obj )
        {
            ListViewItem lvItem = new ListViewItem();
            lvItem.Text = itemText;
            lvItem.Tag = obj;
            lvItem.ImageIndex = Core.ResourceIconManager.GetPropTypeIconIndex(
                Core.ResourceStore.GetPropId( "Attachment") );
            _listQueries.Items.Add( lvItem );
            if ( _listQueries.Items.Count == 1 )
            {
                lvItem.Selected = true;
            }
            return lvItem;
        }

        private void WriteDefaultAttachmentTypes()
        {
            ISettingStore ini = ICore.Instance.SettingStore;
            ini.WriteInt( "Attachments", "TypeCount", 8 );

            WriteAttachmentType( ini, 0, "Documents", ".txt,.doc,.xls,.ppt,.pps,.pp,.pdf,.rtf,.ps,.htm,.html,.vsd,.mpp,.mdb" );
            WriteAttachmentType( ini, 1, "E-mails",   ".eml,.msg");
            WriteAttachmentType( ini, 2, "Images",    ".bmp,.gif,.jpg,.png,.tif,.psd,.ico" );
            WriteAttachmentType( ini, 3, "Music",     ".mid,.mp3,.ogg,.wav,.ra" );
            WriteAttachmentType( ini, 4, "Video",     ".avi,.mpg,.mpeg,.wmv,.asx,.mpe,.asf" );
            WriteAttachmentType( ini, 5, "Programs",  ".exe,.dll,.sh" );
            WriteAttachmentType( ini, 6, "Archives",  ".zip,.rar,.sit,.tgz,.gz,.tar" );
            WriteAttachmentType( ini, 7, "Sources",   ".c,.cpp,.cc,.h,.hpp,.cs,.java,.pas,.pl,.awk" );

            ini.WriteBool( "MailIndexing", "ProcessDeletedItems", false );
            ini.WriteBool( "MailIndexing", "SkipBodyForIndex", false );
        }

        /**
         * Writes a single attachment type to the INI file.
         */

        private static void WriteAttachmentType( ISettingStore ini, int index, string name, string exts )
        {
            ini.WriteString( "Attachments", "Attachment" + index + "Name", name );
            ini.WriteString( "Attachments", "Attachment" + index + "Exts", exts );
        }

        /**
         * Loads the attachment types from the INI file.
         */

        private void ReadAttachmentTypes()
        {
            int typeCount = Core.SettingStore.ReadInt( "Attachments", "TypeCount", 0 );
            for( int i = 0; i < typeCount; i++ )
            {
                AttachmentType attType = new AttachmentType( Core.SettingStore, i );
                _attachmentTypes.Add( attType );
            }
            _attachmentTypes.Add( new AttachmentType( "Other" ) );
        }

        /**
         * Parses the attachment types found in the index into groups specified
         * in the INI.
         */

        private void GroupExistingAttachmentTypes()
        {
            IResourceList attTypes = Core.ResourceStore.GetAllResources( STR.AttachmentType );
            foreach( IResource res in attTypes )
            {
                foreach( AttachmentType attType in _attachmentTypes )
                {
                    if ( attType.AddResource( res ) )
                        break;
                }
            }
        }

        private void OnSelectedIndexChanged( object sender, EventArgs e )
        {
            if ( !_listQueries.ContainsFocus )
                return;

            UpdateSelection();
        }

        public override void UpdateSelection()
        {
            if ( _listQueries.SelectedItems.Count == 0 )
                return;

            AttachmentType attType = ( AttachmentType ) _listQueries.SelectedItems [0].Tag;
            if ( attType != _lastAttachmentType )
            {
                IResourceList mails = attType.GetMails();
                mails.Sort( new SortSettings( Core.Props.Date, false ) );

                _lastAttachmentType = attType;
                _lastResourceList = mails;
                _lastCaption = "Attachments of type " + attType.Name;
            }
            Debug.WriteLine( "Displaying attachment resource list" );
            Core.ResourceBrowser.DisplayThreadedResourceList( null, _lastResourceList, _lastCaption, "", PROP.Attachment, null, null );
        }

        private void AttachmentsCtrl_Enter(object sender, System.EventArgs e)
        {
            if ( _listQueries.SelectedItems.Count > 0 )
            {
                UpdateSelection();
            }
        }

        private void _listQueries_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if( ICore.Instance != null )
            {
                Core.ActionManager.ExecuteKeyboardAction( null, e.KeyData );
            }
        }

        private void _listQueries_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
        {
            _columnHeader.Width = _listQueries.Width - 6;
            if ( _listQueries.Items.Count > 0 )
            {
                int itemHeight = _listQueries.Items [0].Bounds.Height;
                if ( itemHeight * _listQueries.Items.Count > _listQueries.ClientSize.Height )
                {
                    _columnHeader.Width -= User32Dll.GetSystemMetrics( (int)SystemMetricsCodes.SM_CXVSCROLL );
                }
            }
        }

        public override bool ShowSelection
        {
            get { return !_listQueries.HideSelection; }
            set { _listQueries.HideSelection = !value; }
        }
    }
}

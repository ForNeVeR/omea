/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ContactsPlugin.ContactBlocks;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ContactsPlugin
{
    /// <summary>
    /// Block for editing contact names.
    /// </summary>
    internal class NameBlock : AbstractContactViewBlock
	{
        private const int _MaxContactNameList = 7;
        private const int _cThumbnailDim = 40;
        private const string _cDefaultPictureIcon = "ContactsPlugin.Icons.contact48.png";

        private Button _btnPicture;
        private Button _btnClearPicture;
        private Button _btnFullName;
        private PropertyEditor  _boxFullName;

        private Button _btnShowAllNames;
        private CheckBox _checkShowOrigNames;
        private JetLinkLabel _lblSeeAll;
        private CategoriesSelector _selector;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        private IResource   _resource;
        private bool        _isNewContact;
        private bool        _hasChanged;
        private string      _originalName;

		public NameBlock()
		{
			InitializeComponent();
		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new NameBlock();            
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
            _btnPicture = new Button();
		    _btnClearPicture = new Button();
		    _btnFullName = new Button();
            _boxFullName = new PropertyEditor();
            _btnShowAllNames = new System.Windows.Forms.Button();
            _lblSeeAll = new JetBrains.Omea.GUIControls.JetLinkLabel();
            _checkShowOrigNames = new CheckBox();
		    _selector = new CategoriesSelector();
            this.SuspendLayout();
            //
            // _btnPicture
            //
            _btnPicture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnPicture.Location = new System.Drawing.Point(2, 4);
            _btnPicture.Name = "_btnPicture";
            _btnPicture.Size = new System.Drawing.Size(64, 64);
            _btnPicture.TabIndex = 1;
            //
            // _btnClearPicture
            //
		    _btnClearPicture.FlatStyle = FlatStyle.System;
            _btnClearPicture.Location = new System.Drawing.Point(9, 74);
            _btnClearPicture.Name = "_btnClearPicture";
            _btnClearPicture.Size = new System.Drawing.Size(50, 16);
            _btnClearPicture.Text = "Clear";
            _btnClearPicture.TabIndex = 2;
            _btnClearPicture.Click += new EventHandler(_btnClearPicture_Click);
            //
            // _btnFullName
            //
            _btnFullName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            _btnFullName.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            _btnFullName.Location = new System.Drawing.Point(70, 5);
            _btnFullName.Name = "_btnFullName";
            _btnFullName.Size = new System.Drawing.Size(92, 26);
            _btnFullName.TabIndex = 3;
            _btnFullName.Text = "Full &Name...";
            _btnFullName.Click += new EventHandler(_btnFullName_Click);
            //
            // _boxFullName
            //
            _boxFullName.Anchor = (AnchorStyles.Top | AnchorStyles.Left| AnchorStyles.Right);
            _boxFullName.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            _boxFullName.Location = new System.Drawing.Point(170, 7);
            _boxFullName.Multiline = false;
            _boxFullName.Name = "_boxFullName";
            _boxFullName.ReadOnly = false;
            _boxFullName.Size = new System.Drawing.Size(114, 24);
            _boxFullName.TabIndex = 4;
            // 
            // _btnShowAllNames
            // 
            this._btnShowAllNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnShowAllNames.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnShowAllNames.Location = new System.Drawing.Point(180, 75);
            this._btnShowAllNames.Name = "_btnShowAllNames";
            this._btnShowAllNames.Size = new System.Drawing.Size(84, 23);
            this._btnShowAllNames.TabIndex = 5;
            this._btnShowAllNames.Text = "Show All Names";
            this._btnShowAllNames.Click += new System.EventHandler(this.buttonShowAllNames_Click);
            // 
            // _lblSeeAll
            // 
            this._lblSeeAll.ClickableLink = true;
            this._lblSeeAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._lblSeeAll.Cursor = System.Windows.Forms.Cursors.Default;
            this._lblSeeAll.Enabled = false;
            this._lblSeeAll.Location = new System.Drawing.Point(140, 75);
            this._lblSeeAll.Name = "_lblSeeAll";
            this._lblSeeAll.Size = new System.Drawing.Size(0, 0);
            this._lblSeeAll.TabIndex = 6;
            this._lblSeeAll.Visible = false;
            this._lblSeeAll.Click += new EventHandler(labelSeeAll_Click);
            //
            // _checkShowOrigNames
            //
            this._checkShowOrigNames.Location = new Point( 70, 75 );
            this._checkShowOrigNames.Size = new Size( 140, 20 );
            this._checkShowOrigNames.Name = "_checkShowOrigNames";
            this._checkShowOrigNames.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._checkShowOrigNames.TabIndex = 7;
            this._checkShowOrigNames.Text = "Show Original Names";
            this._checkShowOrigNames.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            this._checkShowOrigNames.FlatStyle = System.Windows.Forms.FlatStyle.System;
            //
            // __selector
            //
            _selector.Name = "_selector";
            _selector.TabIndex = 8;
            _selector.Size = new Size(214, 40);
            _selector.Location = new Point(70, 32);
		    _selector.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            // 
            // NameBlock
            // 
            Controls.Add( _selector );
            Controls.Add( _checkShowOrigNames );
            Controls.Add( _lblSeeAll );
            Controls.Add( _btnShowAllNames );
            Controls.Add( _boxFullName );
            Controls.Add( _btnFullName );
		    Controls.Add( _btnClearPicture );
		    Controls.Add( _btnPicture );

            Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            Name = "NameBlock";
            Size = new System.Drawing.Size(288, 120);
            ResumeLayout(false);
        }
		#endregion

        public override void EditResource( IResource res )
        {
            EditResource( res, false );
        }
        private void  EditResource( IResource res, bool showAllNames )
        {
            _resource = res;
            _isNewContact = res.IsTransient;

            InitializeContactControls( res );
            InitializeContactNamesControls( res, showAllNames );
            InitializeContactPicture( res );

            int currentSize = CurrentBlockLowerBorder;
            int heightDiff = Size.Height - currentSize;
            if( heightDiff != 0 )
            {
                Size = new Size( Width, currentSize );
            }
            UpdateValidState();
        }

        private void  InitializeContactControls( IResource res )
        {
            _boxFullName.Text = Core.ContactManager.GetFullName( res );
            _boxFullName.TextChanged += OnNameTextChanged;

            _selector.Resource = res;

            _hasChanged = false;
            _originalName = Core.ContactManager.GetFullName( res );
        }

        private void InitializeContactNamesControls( IResource res, bool showAllNames )
        {
            _checkShowOrigNames.Checked = res.HasProp( Core.ContactManager.Props.ShowOriginalNames );
            _checkShowOrigNames.Visible = _checkShowOrigNames.Enabled =
                (_checkShowOrigNames.Checked || (res.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact ).Count > 1));

            ArrayList uniqueNames = CollectUniqueContactNames( res, showAllNames );
            _btnShowAllNames.Visible = _btnShowAllNames.Enabled = !_isNewContact && (uniqueNames.Count > 1);
        }

        private void InitializeContactPicture( IResource res )
        {
            Image image;
            if (res.HasProp( Core.ContactManager.Props.Picture ))
            {
                Stream stream = res.GetBlobProp( Core.ContactManager.Props.Picture );
                image = Image.FromStream( stream );
            }
            else
            {
                image = Utils.GetResourceImageFromAssembly( "Contacts", _cDefaultPictureIcon );
            }
            _btnPicture.Image = image;
            _btnPicture.Click += _btnPicture_Click;
        }

        private static ArrayList  CollectUniqueContactNames( IResource res, bool showAllNames )
        {
            ArrayList uniqueNames = new ArrayList();
            IResourceList names = res.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );

            int maxNames = names.Count;
            if( !showAllNames )
                maxNames = Math.Min( maxNames, _MaxContactNameList );
            for( int i = 0; i < maxNames; i++ )
            {
                string name = names[ i ].GetStringProp( Core.Props.Name );
                if( uniqueNames.IndexOf( name ) == -1 )
                    uniqueNames.Add( name );
            }

            return uniqueNames;
        }

        public override void Save()
        {
            if( _hasChanged )
            {
                string title, fn, mn, ln, suffix, addspec;
                ContactResolver.ResolveName( _boxFullName.Text, null, out title, out fn, out mn, out ln, out suffix, out addspec);

                SaveProp( ContactManager._propTitle, title );
                SaveProp( ContactManager._propFirstName, fn );
                SaveProp( ContactManager._propMiddleName, mn );
                SaveProp( ContactManager._propLastName, ln );
                SaveProp( ContactManager._propSuffix, suffix );
            }

            if( IsOptionDiffer() )
            {
                if( _checkShowOrigNames.Checked )
                    _resource.SetProp( Core.ContactManager.Props.ShowOriginalNames, true );
                else
                    _resource.DeleteProp( Core.ContactManager.Props.ShowOriginalNames );
            }
        }

        private void SaveProp( int propId, string valueText )
        {
            string currVal = _resource.GetStringProp( propId );

            //  perform assignment only if values differ.
            if(( isValid( valueText ) && isValid( currVal ) && valueText != currVal ) ||
               ( isValid( valueText ) && !isValid( currVal ) ))
                _resource.SetProp( propId, valueText );
            else
            if( !isValid( valueText ) && isValid( currVal ) )
                _resource.DeleteProp( propId );
        }

        private static bool isValid( string text )
        {
            return !String.IsNullOrEmpty( text );
        }

        public override bool IsChanged()
        {
            return _hasChanged || IsOptionDiffer();
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propTitle ||
                   propId == ContactManager._propFirstName ||
                   propId == ContactManager._propMiddleName ||
                   propId == ContactManager._propLastName ||
                   propId == ContactManager._propSuffix ||
                   propId == ContactManager._propSpecificator ||
                   propId == Core.ContactManager.Props.ShowOriginalNames;
        }

        private void UpdateValidState()
        {
            bool isWarning = false;
            string message = null;
            if( String.IsNullOrEmpty( _boxFullName.Text ))
            {
                message = "Please enter a name for the contact";
            }
            else
            if( _isNewContact && Core.ContactManager.FindContact( _boxFullName.Text ) != null )
            {
                isWarning = true;
                message = "A contact with such name already exists";
            }
            OnValidStateChanged(new ValidStateEventArgs( message == null, isWarning, message ));
        }

        private bool IsOptionDiffer()
        {
            return _checkShowOrigNames.Checked != _resource.HasProp( Core.ContactManager.Props.ShowOriginalNames );
        }

        #region Move/Resize
        private int CurrentBlockLowerBorder
        {  
            get
            {  
                int lowerBound = 0;
                foreach( Control ctrl in Controls )
                {
                    if( ctrl.Enabled )
                        lowerBound = Math.Max( ctrl.Location.Y + ctrl.Size.Height, lowerBound );
                }
                    lowerBound += 8;
                return lowerBound;
            }
        }
        #endregion Move/Resize

        #region Name
        private void OnNameTextChanged( object sender, EventArgs e )
        {
            UpdateValidState();
            _hasChanged = _boxFullName.Text.Equals( _originalName );
        }

        private void buttonShowAllNames_Click(object sender, EventArgs e)
        {
            AllFullNamesForm form = new AllFullNamesForm( _resource );
            form.ShowDialog();
        }

        private void labelSeeAll_Click(object sender, EventArgs e)
        {
            EditResource( _resource, true );
        }

        void _btnFullName_Click(object sender, EventArgs e)
        {
            FullNameEditForm form = new FullNameEditForm(_resource, _boxFullName.Text);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _boxFullName.Text = form.FullName;
                _hasChanged = false;
            }
        }
        #endregion Name

        #region Picture Handling
        private delegate void SetPropDelegate( int prop, Stream stream );

        void _btnPicture_Click( object sender, EventArgs e )
        {
            bool ctrlPressed = (ModifierKeys == Keys.Control);
            if( !ctrlPressed )
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = false;
                    dlg.Title = "Select Image File";
                    dlg.Filter = "Image files|*.ico;*.png;*.bmp;*jpg|All files|*.*";

                    if( dlg.ShowDialog( this ) == DialogResult.OK )
                    {
                        String file = dlg.FileName;
                        JetMemoryStream origStream = null, thumbStream = null;
                        try
                        {
                            Image image = Image.FromFile( file );
                            origStream = new JetMemoryStream();
                            image.Save( origStream, ImageFormat.Png );

                            Image thumb = GraphicalUtils.GenerateImageThumbnail( image, _cThumbnailDim, _cThumbnailDim );
                            thumbStream = new JetMemoryStream();
                            ImageCodecInfo[] iciInfo = ImageCodecInfo.GetImageEncoders();
                            EncoderParameters encParams = new EncoderParameters( 1 );
                            encParams.Param[ 0 ] = new EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 100L );
                            thumb.Save( thumbStream, iciInfo[ 1 ], encParams );

                            Core.ResourceAP.RunJob( new SetPropDelegate( SetStreamProp ), Core.ContactManager.Props.PictureOriginal, origStream );
                            Core.ResourceAP.RunJob( new SetPropDelegate( SetStreamProp ), Core.ContactManager.Props.Picture, thumbStream );

                            InitializeContactPicture( _resource );
                            Invalidate( true );
                        }
                        catch( Exception ex )
                        {
                            Core.UIManager.ShowSimpleMessageBox( "Error Format", ex.Message );

                            Core.ResourceAP.RunJob( new SetPropDelegate(SetStreamProp), Core.ContactManager.Props.PictureOriginal,  null );
                            Core.ResourceAP.RunJob( new SetPropDelegate(SetStreamProp), Core.ContactManager.Props.Picture,  null );
                        }
                        finally
                        {
                            if( origStream != null ) origStream.Dispose();
                            if( thumbStream != null)  thumbStream.Dispose();
                        }
                    }
                }
            }
            else
            {
                //  If there is a reference to a full-scaled picture, show it
                //  in the associated application.
                Stream stream = _resource.GetBlobProp( Core.ContactManager.Props.PictureOriginal );
                if( stream != null )
                {
                    string fileName = Path.GetTempFileName() + ".png";
                    using (Image image = Image.FromStream(stream))
                    {
                        image.Save( fileName, ImageFormat.Png );
                        Utils.RunAssociatedApplicationOnFile( fileName );
                    }
                }
            }
        }

        void _btnClearPicture_Click(object sender, EventArgs e)
        {
            Core.ResourceAP.RunJob( new SetPropDelegate( SetStreamProp ), Core.ContactManager.Props.PictureOriginal, null );
            Core.ResourceAP.RunJob( new SetPropDelegate( SetStreamProp ), Core.ContactManager.Props.Picture, null );

            InitializeContactPicture(_resource);
            Invalidate(true);
        }

        private void SetStreamProp(int prop, Stream stream)
        {
            if( stream != null )
                _resource.SetProp( prop, stream );
            else
                _resource.DeleteProp( prop );
        }
        #endregion Picture Handling

        #region Html Content
        public override string HtmlContent( IResource contact )
        {
            StringBuilder result = StringBuilderPool.Alloc();
            try
            {
                if (contact.GetPropText(ContactManager._propTitle).Length > 0)
                {
                    result.Append( OptionalTag(contact, "Title:", ContactManager._propTitle) );
                }
                result.Append( ObligatoryTag(contact, "First Name:", ContactManager._propFirstName) );
                result.Append( ObligatoryTag(contact, "Mid Name:", ContactManager._propMiddleName) );
                result.Append( ObligatoryTag(contact, "Last Name:", ContactManager._propLastName) );
                if (contact.GetPropText(ContactManager._propSuffix).Length > 0)
                {
                    result.Append( OptionalTag(contact, "Suffix:", ContactManager._propSuffix) );
                }

                bool showAllNames = contact.HasProp( Core.ContactManager.Props.ShowOriginalNames );
                if( showAllNames )
                {
                    ArrayList uniqueNames = CollectUniqueContactNames( contact, showAllNames );

                    if (((uniqueNames.Count > 1) ||
                        (uniqueNames.Count == 1 && (string)uniqueNames[ 0 ] != contact.DisplayName)))
                    {
                        result.Append("\t<tr><td>Available Names:</td><td>");
                        foreach( string name in uniqueNames )
                        {
                            result.Append( name ).Append("<br/>");
                        }
                        result.Append("</td></tr>");
                    }
                }
                return result.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( result );
            }
        }
        #endregion Html Content
    }
}

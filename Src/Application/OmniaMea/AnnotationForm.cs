// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for AnnotationForm.
	/// </summary>
	public class AnnotationForm : Form
	{
        private const double        cDefaultOpacity = 0.75;

        private RichTextBox     richText;
        private ISettingStore   Settings;

        private IResource       _resource;
        private IResourceList   _ResourceChangeWatcher;

        private bool     IsPersistentMode = false;
        private bool     IsModified;
        private double   SavedOpacity = cDefaultOpacity;
        private string   SourceText;
        private bool     _defaultPosition;
        private bool     _disposing;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AnnotationForm()
		{
		    Settings = Core.SettingStore;
			InitializeComponent();

            //-----------------------------------------------------------------
            int     xPos = Settings.ReadInt( "Annotations", "LocationX", -1 ),
                    yPos = Settings.ReadInt( "Annotations", "LocationY", -1 );
            int     width = Settings.ReadInt( "Annotations", "Width", -1 ),
                    height = Settings.ReadInt( "Annotations", "Height", -1 );
            if( xPos != -1 && yPos != -1 )
            {
                Location = new Point( xPos, yPos );
                _defaultPosition = false;
            }
            else
            {
                _defaultPosition = true;
            }
            if( width != -1 && height != -1 )
                this.Size = new Size( width, height );

            if( IsOutsideScreen( Location ))
                this.Location = new Point( Screen.PrimaryScreen.WorkingArea.Right / 2,
                                           Screen.PrimaryScreen.WorkingArea.Bottom / 2 );

            //-----------------------------------------------------------------
            int opacity = Settings.ReadInt( "Annotations", "Opacity", -1 );
            if( opacity != -1 )
            {
                SavedOpacity = Opacity;
                try
                {
                    Opacity = ((double)opacity) / 100.0;
                }
                catch( Exception )
                {
                    // ignore
                }
            }

            string color = Settings.ReadString( "Annotations", "BackColor" );
            if( !String.IsNullOrEmpty( color ) )
            {
                if( IsValidColor( color ) )
                    richText.BackColor = Color.FromName( color );
                else
                {
                    MessageBox.Show( this, "Color name for the Annotation background is illegal", "Illegal Color Name", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    Settings.WriteString( "Annotations", "BackColor", "Info" );
                }
            }
            color = Settings.ReadString( "Annotations", "ForeColor" );
            if( !String.IsNullOrEmpty( color ) )
            {
                if( IsValidColor( color ) )
                    richText.ForeColor = Color.FromName( color );
                else
                {
                    MessageBox.Show( this, "Color name for the Annotation foreground is illegal", "Illegal Color Name", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    Settings.WriteString( "Annotations", "ForeColor", "Black" );
                }
            }

            Hide();
		}
        public  void    ShowAnnotation( IResource res, bool isPersistentMode )
        {
            ShowAnnotation( res, isPersistentMode, false );
        }

        public  void    ShowAnnotation( IResource res, bool isPersistentMode, bool focus )
        {
            _resource = res;
            IsPersistentMode = isPersistentMode;
            SourceText = res.GetPropText( Core.Props.Annotation );

            // [Clear and] Set a watcher on the annotation text which
            // can be changed outside the Omea.
            if( _ResourceChangeWatcher != null )
            {
                _ResourceChangeWatcher.ResourceChanged -= _ResourceChangeWatcher_ResourceChanged;
                _ResourceChangeWatcher.Dispose();
            }
            _ResourceChangeWatcher = _resource.ToResourceListLive();
            _ResourceChangeWatcher.AddPropertyWatch( Core.Props.Annotation );
            _ResourceChangeWatcher.ResourceChanged += _ResourceChangeWatcher_ResourceChanged;

            WriteText( SourceText );

            if ( _defaultPosition )
            {
                Bounds = GetDefaultPosition();
                _defaultPosition = false;
            }

            Show();
            if( !focus )
                Core.ResourceBrowser.FocusResourceList();
            else
                Focus();
        }

	    private static Rectangle GetDefaultPosition()
	    {
	        Rectangle rc = (Core.ResourceBrowser as ResourceBrowser).DisplayPanePosition;
            return new Rectangle( rc.Right - 310, rc.Bottom - 110, 300, 100 );
	    }

	    public string AnnotationText
        {
            get{  return( richText.Text );  }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
        {
            _disposing = true;
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.richText = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // richText
            //
            this.richText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.richText.BackColor = System.Drawing.SystemColors.Info;
            this.richText.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.richText.Location = new System.Drawing.Point(0, 0);
            this.richText.Name = "richText";
            this.richText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richText.Size = new System.Drawing.Size(440, 224);
            this.richText.TabIndex = 1;
            this.richText.Text = "";
            this.richText.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richText_LinkClicked);
            //
            // AnnotationForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(440, 224);
            this.Controls.Add(this.richText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AnnotationForm";
            this.Opacity = 0.9;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Edit Annotation";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyPressed);
            this.Resize += new System.EventHandler(this.AnnotationForm_Resize);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.OnToBeClosed);
            this.Move += new System.EventHandler(this.AnnotationForm_Move);
            this.ResumeLayout(false);

        }
		#endregion


        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if(( e.KeyCode == Keys.Enter && e.Control ) || ( e.KeyCode == Keys.Escape && !e.Control ))
            {
                e.Handled = true;
                /*
                if( e.KeyCode == Keys.Enter && AnnotationText.Length > 0 )
                    SaveAnnotation( AnnotationText, Res );
                else
                if(( e.KeyCode == Keys.Enter && AnnotationText.Length == 0 ) ||
                   ( e.KeyCode == Keys.Escape && SourceText.Length == 0 ))
                    DeleteAnnotation( Res );

                if( e.KeyCode == Keys.Escape && SourceText.Length > 0 )
                    WriteText( SourceText );
                */
                if ( AnnotationText.Length > 0 )
                {
                    SaveAnnotation( AnnotationText, _resource );
                    if( IsPersistentMode )
                    {
                        if( Core.ResourceBrowser.ResourceListVisible )
                            Core.ResourceBrowser.FocusResourceList();
                        else
                            this.OnDeactivate( null );
                    }
                    else
                        Hide();
                }
                else
                {
                    DeleteAnnotation( _resource );
                    Hide();
                }
            }
            else if ( !JetTextBox.IsEditorKey( e.KeyData ) )
            {
                Core.ActionManager.ExecuteKeyboardAction( null, e.KeyCode | e.Modifiers );
                IsModified = true;
            }
        }

        protected override void OnActivated( EventArgs e )
        {
            base.OnGotFocus( e );
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            SavedOpacity = Opacity;
            //  Crazy workaround - smtimes Win32Exception is raised that 1.0 is
            //  not a valid argument for a Opacity setter.
            try
            {
                Opacity = 1.0;
            }
            catch( Exception )
            {
                // ignore
            }

            Text = CaptionText();
        }

        protected override void OnDeactivate( System.EventArgs e )
        {
            if ( !_disposing )
            {
                if( AnnotationText.Length > 0 )
                    SaveAnnotation( AnnotationText, _resource );
                else
                    DeleteAnnotation( _resource );
            }

            base.OnLostFocus( e );
            try
            {
                Opacity = SavedOpacity;
            }
            catch( Exception )
            {
                // ignore
            }

            Text = CaptionText();
        }

        private void AnnotationForm_Move(object sender, System.EventArgs e)
        {
            SaveConfiguration();
        }

        private void AnnotationForm_Resize(object sender, System.EventArgs e)
        {
            SaveConfiguration();
        }

        private void OnToBeClosed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if( AnnotationText.Length > 0 )
                SaveAnnotation( AnnotationText, _resource );
            else
                DeleteAnnotation( _resource );
        }

        //---------------------------------------------------------------------
        //  Aux methods
        //---------------------------------------------------------------------
        private void WriteText( string text )
        {
            Debug.Assert( text != null );
            if( text.Length > 0 )
            {
                string[] lines = text.Split( '\n' );
                richText.Lines = lines;
            }
            else
                richText.Clear();
        }

        private delegate void SaveAnnotationDelegate( IResource res, string text );

        private void SaveAnnotation( string ann, IResource res )
        {
            if( IsModified )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new SaveAnnotationDelegate( DoSaveAnnotation ),
                    res, ann );
                IsModified = false;
            }
        }

        private static void DeleteAnnotation( IResource res )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new SaveAnnotationDelegate( DoSaveAnnotation ), res, null );
        }

        private static void DoSaveAnnotation( IResource res, string annotation )
        {
            if ( res.IsDeleted )
                return;

            if ( annotation != null )
            {
                res.SetProp( Core.Props.Annotation, annotation );
                res.SetProp( "AnnotationLastModifiedDate", DateTime.Now );
            }
            else
            {
                res.DeleteProp( Core.Props.Annotation );
            }

            if ( res.IsTransient )
            {
                res.EndUpdate();
            }

            //  finally call environment to reindex the resource so that the
            //  text index reflect the changes in annotation text.
            Core.TextIndexManager.QueryIndexing( res.Id );
        }

        private void SaveConfiguration()
        {
            Settings.WriteInt( "Annotations", "LocationX", Location.X );
            Settings.WriteInt( "Annotations", "LocationY", Location.Y );
            Settings.WriteInt( "Annotations", "Width", Size.Width );
            Settings.WriteInt( "Annotations", "Height", Size.Height );
        }

        private string CaptionText()
        {
            string text = "Edit Annotation";
            if( _resource != null )
            {
                text += ": " + _resource.DisplayName;
                if( _resource.HasProp( "AnnotationLastModifiedDate" ))
                    text += " | Last touched at " + _resource.GetDateProp( "AnnotationLastModifiedDate" ).ToShortDateString();
            }
            return text;
        }

        private static bool IsOutsideScreen( Point location )
        {
            Rectangle rect = Screen.PrimaryScreen.WorkingArea;
            return( location.X > rect.Right || location.Y > rect.Bottom );
        }

        //  If name does not belong to the KnownColor enumeration,
        //  FromName return a structure with all ARGB components equal to 0.
        private static bool IsValidColor( string name )
        {
            Color color = Color.FromName( name );
            return (color.A != 0) || (color.R != 0) || (color.G != 0) || (color.B != 0);
        }

        private void richText_LinkClicked( object sender, LinkClickedEventArgs e )
        {
			Core.UIManager.OpenInNewBrowserWindow(e.LinkText);
        }

        private void _ResourceChangeWatcher_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateAnnotation ) );
        }

	    private void UpdateAnnotation()
	    {
            if ( !IsDisposed && _resource != null && !_resource.IsDeleted )
            {
                SourceText = _resource.GetPropText( Core.Props.Annotation );
                WriteText( SourceText );
            }
	    }

	    protected override void OnClosed(EventArgs e)
        {
            base.OnClosed (e);
            if( _ResourceChangeWatcher != null )
            {
                _ResourceChangeWatcher.ResourceChanged -= _ResourceChangeWatcher_ResourceChanged;
                _ResourceChangeWatcher.Dispose();
            }
        }
    }
}

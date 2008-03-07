/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.OutlookPlugin
{
    public class MailBodyHeaderCtrl : System.Windows.Forms.UserControl
    {
        private JetRichTextBox _editSubject;
        private System.Windows.Forms.TextBox _editDate;
        private GradientBar _gradientBar;
        private IResource _resource;

        public MailBodyHeaderCtrl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _editSubject.BackColor = SystemColors.Window;
            _editSubject.ShowContextMenu = false;
            _editDate.BackColor = SystemColors.Window;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._editSubject = new JetRichTextBox();
            this._editDate = new System.Windows.Forms.TextBox();
            this._gradientBar = new GUIControls.GradientBar();
            this.SuspendLayout();
            // 
            // _editSubject
            // 
            this._editSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._editSubject.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._editSubject.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._editSubject.ForeColor = System.Drawing.SystemColors.WindowText;
            this._editSubject.HideSelection = false;
            this._editSubject.Location = new System.Drawing.Point(4, 4);
            this._editSubject.Name = "_editSubject";
            this._editSubject.ReadOnly = true;
            this._editSubject.Size = new System.Drawing.Size(336, 16);
            this._editSubject.TabIndex = 5;
            this._editSubject.Text = "";
            this._editSubject.KeyDown += new System.Windows.Forms.KeyEventHandler(this._editSubject_KeyDown);
            // 
            // _editDate
            // 
            this._editDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._editDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._editDate.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._editDate.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this._editDate.Location = new System.Drawing.Point(384, 4);
            this._editDate.Name = "_editDate";
            this._editDate.ReadOnly = true;
            this._editDate.Size = new System.Drawing.Size(120, 14);
            this._editDate.TabIndex = 7;
            this._editDate.Text = "";
            // 
            // _gradientBar
            // 
            this._gradientBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._gradientBar.EndColor = System.Drawing.Color.White;
            this._gradientBar.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            this._gradientBar.Location = new System.Drawing.Point(4, 22);
            this._gradientBar.Name = "_gradientBar";
            this._gradientBar.Size = new System.Drawing.Size(504, 1);
            this._gradientBar.StartColor = System.Drawing.SystemColors.ControlDarkDark;
            this._gradientBar.TabIndex = 10;
            // 
            // MailBodyHeaderCtrl
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this._gradientBar);
            this.Controls.Add(this._editDate);
            this.Controls.Add(this._editSubject);
            this.Name = "MailBodyHeaderCtrl";
            this.Size = new System.Drawing.Size(512, 24);
            this.ResumeLayout(false);

        }
        #endregion

        internal void SetMailResource( IResource res )
        {
            Guard.NullArgument( res, "res" );
            _resource = res;
            _editSubject.Clear();
            _editSubject.Text = res.GetStringProp( "Subject" );
            if ( res.HasProp( "Date" ) )
            {
                DateTime dt = res.GetDateProp( "Date" );
                _editDate.Text = dt.ToShortDateString() + " " + dt.ToShortTimeString();
            }
        }
        internal void SetSubject( string subject )
        {
            Guard.NullArgument( subject, "subject" );
            _editSubject.Clear();
            _editSubject.Text = subject;
        }

        internal void HighlightWords( WordPtr[] words  )
        {
            _editSubject.HighlightWords( DocumentSection.RestrictResults(words, DocumentSection.SubjectSection ));
        }

        private void _editSubject_KeyDown( object sender, System.Windows.Forms.KeyEventArgs e )
        {
            ActionContext context = new ActionContext( ActionContextKind.Keyboard, null,
                _resource.ToResourceList() );
            if ( Core.ActionManager.ExecuteKeyboardAction( context, e.KeyData ) )
            {
                e.Handled = true;
            }
        }
    }
}

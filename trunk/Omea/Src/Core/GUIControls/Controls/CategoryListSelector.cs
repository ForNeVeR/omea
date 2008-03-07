/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Class represents a list of categories as the simple comma-delimited
    /// enumeration of categories names with the ability to choose them
    /// through "AssignCategories" dialog.
    /// </summary>
    public class CategoriesSelector : UserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private Button      _btnCategories;
        private TextBox     _boxCategories;
        private IResource   _res;

        public CategoriesSelector()
        {
            InitializeComponent();
        }
        public IResource Resource
        {
            set
            {
                _res = value;
                ShowCategories( _res );
            }
        }

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

        private void InitializeComponent()
        {
            _btnCategories = new Button();
            _boxCategories = new TextBox();

            SuspendLayout();
            // 
            // _categoriesButton
            // 
            _btnCategories.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            _btnCategories.FlatStyle = FlatStyle.System;
            _btnCategories.Location = new System.Drawing.Point(0, 7);
            _btnCategories.Name = "_btnCategories";
            _btnCategories.Size = new System.Drawing.Size(92, 24);
            _btnCategories.TabIndex = 4;
            _btnCategories.Text = "&Categories...";
            _btnCategories.Click += _btnCategories_Click;
            // 
            // _boxCategories
            // 
            _boxCategories.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            _boxCategories.Location = new System.Drawing.Point(100, 8);
            _boxCategories.Name = "_boxCategories";
            _boxCategories.Size = new System.Drawing.Size(Width - 100, 20);
            _boxCategories.TabStop = false;
            _boxCategories.Text = "";
            _boxCategories.ReadOnly = true;
            // 
            // CategoriesSelector
            // 
            Controls.Add(_btnCategories);
            Controls.Add(_boxCategories);
            Name = "Categories Selector";
            ResumeLayout(false);
        }

        private void _btnCategories_Click(object sender, EventArgs e)
        {
            Core.UIManager.ShowAssignCategoriesDialog( this, _res.ToResourceList() );
            ShowCategories( _res );
        }

        private void ShowCategories( IResource res )
        {
            string  presentation = string.Empty;
            IResourceList categories = res.GetLinksOfType( "Category", "Category" );
            foreach( IResource cat in categories )
            {
                presentation += cat.DisplayName + ", ";
            }
            if( presentation.Length > 0 )
                presentation = presentation.Substring( 0, presentation.Length - 2 );

            _boxCategories.Text = presentation;
        }
    }
}
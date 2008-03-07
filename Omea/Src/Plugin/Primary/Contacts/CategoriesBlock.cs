/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Categories;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ContactsPlugin
{
	/**
     * Contact view block for editing work information.
     */
    
    internal class CategoriesBlock: AbstractContactViewBlock
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private ListBox listCategories;
        private Button buttonAssign;

        private IResource       _contact;
        private IResourceList   _originalCats, _resultCats;
        private readonly int    _propCategory;

		public CategoriesBlock()
		{
			InitializeComponent();
            _propCategory = (Core.CategoryManager as CategoryManager).PropCategory;
		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new CategoriesBlock();
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
            this.listCategories = new System.Windows.Forms.ListBox();
            this.buttonAssign = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listCategories
            // 
            this.listCategories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listCategories.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.listCategories.Location = new System.Drawing.Point(4, 4);
            this.listCategories.Name = "listCategories";
            this.listCategories.Size = new System.Drawing.Size(120, 82);
            this.listCategories.TabIndex = 1;
            // 
            // buttonAssign
            // 
            this.buttonAssign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAssign.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAssign.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonAssign.Location = new System.Drawing.Point(132, 4);
            this.buttonAssign.Name = "buttonAssign";
            this.buttonAssign.TabIndex = 2;
            this.buttonAssign.Text = "Assign...";
            this.buttonAssign.Click += new System.EventHandler(this.buttonAssign_Click);
            // 
            // CategoriesBlock
            // 
            this.Controls.Add(this.buttonAssign);
            this.Controls.Add(this.listCategories);
            this.Name = "CategoriesBlock";
            this.Size = new System.Drawing.Size(216, 88);
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
        {
            _contact = res;
            _resultCats = _originalCats = res.GetLinksOfType( "Category", _propCategory );
            SuspendLayout();

            foreach( IResource category in _originalCats )
                listCategories.Items.Add( category.DisplayName );
            ResumeLayout( true );

            int currentSize = listCategories.Location.Y + listCategories.Height;

            currentSize += 12;
            if( this.Size.Height != currentSize )
                this.Size = new Size( this.Width, currentSize );
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == _propCategory;
        }

        public override void Save()
        {
            //  Keep track of whether the original and final sets of categories
            //  differ and run particular rules if they differ.
            if( IsChanged() )
            {
                foreach (IResource res in _resultCats)
                {
                    if (_originalCats.IndexOf(res) == -1)
                        Core.CategoryManager.AddResourceCategory( _contact, res );
                    _originalCats = _originalCats.Minus(res.ToResourceList());
                }

                //  ... and remove those which are not now set.
                foreach (IResource res in _originalCats)
                {
                    Core.CategoryManager.RemoveResourceCategory( _contact, res );
                }


                Core.FilterManager.ExecRules( StandardEvents.CategoryAssigned, _contact );
            }
        }

        public override bool IsChanged()
        {
            if( _originalCats.Count == _resultCats.Count )
            {
                foreach( IResource res in _resultCats )
                {
                    if( _originalCats.IndexOf( res.Id ) == -1 )
                        return true;
                }

                return false;
            }
            return true;
        }

        private void buttonAssign_Click(object sender, System.EventArgs e)
        {
            if( Core.UIManager.ShowAssignCategoriesDialog( FindForm(), _contact.ToResourceList(), _resultCats, out _resultCats ) == DialogResult.OK )
            {
                listCategories.Items.Clear();
                foreach( IResource category in _resultCats )
                    listCategories.Items.Add( category.DisplayName );
            }
        }

        public override string  HtmlContent( IResource contact )
        {
            StringBuilder result = new StringBuilder( "\t<tr><td>Categories:</td>" );
            IResourceList cats = contact.GetLinksOfType( "Category", _propCategory );
            if( cats.Count > 0 )
            {
                result.Append("<td>");
                foreach (IResource res in cats)
                    result.Append( res.DisplayName + "<br/>" );
                result.Append("</td>");
            }
            else
                result.Append( ContactViewStandardTags.NotSpecifiedHtmlText );
            result.Append("</tr>");
            return result.ToString();
        }
    }
}

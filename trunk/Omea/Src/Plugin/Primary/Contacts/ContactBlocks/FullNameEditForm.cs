/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;

namespace ContactsPlugin.ContactBlocks
{
    public partial class FullNameEditForm : Form
    {
        private readonly string _fullName;
        private readonly IResource _contact;

        public FullNameEditForm( IResource res, string fullName )
        {
            _contact = res;
            _fullName = fullName;

            InitializeComponent();
            InitializeFields();
        }

        internal string FullName
        {
            get
            {
                StringBuilder name = StringBuilderPool.Alloc();
                name.Append( _contact.GetPropText(ContactManager._propTitle) );
                name.Append(' ').Append( _contact.GetPropText( ContactManager._propFirstName ) );
                name.Append(' ').Append(_contact.GetPropText(ContactManager._propMiddleName));
                name.Append(' ').Append(_contact.GetPropText(ContactManager._propLastName));
                name.Append(' ').Append(_contact.GetPropText(ContactManager._propSuffix));

                return ContactResolver.CompressBlanks( name.ToString() );
            }
        }

        private void InitializeFields()
        {
            string title, fn, mn, ln, suffix, addspec;
            ContactResolver.ResolveName(_fullName, null, out title, out fn, out mn, out ln, out suffix, out addspec);

            _boxTitle.Text = title;
            _boxFirstName.Text = fn;
            _boxMidName.Text = mn;
            _boxLastName.Text = ln;
            _boxSuffix.Text = suffix;

            _boxTitle.TextChanged += OnNameTextChanged;
            _boxFirstName.TextChanged += OnNameTextChanged;
            _boxMidName.TextChanged += OnNameTextChanged;
            _boxLastName.TextChanged += OnNameTextChanged;
            _boxSuffix.TextChanged += OnNameTextChanged;
        }

        private void OnNameTextChanged(object sender, EventArgs e)
        {
            UpdateValidState();
        }

        private void UpdateValidState()
        {
            _btnOK.Enabled = !( String.IsNullOrEmpty( _boxFirstName.Text ) &&
                                String.IsNullOrEmpty( _boxMidName.Text ) &&
                                String.IsNullOrEmpty( _boxLastName.Text ) );
        }

        private void _btnOK_Click(object sender, EventArgs e)
        {
            Core.ResourceAP.RunJob( new MethodInvoker( SaveNameParts ) );
        }

        private void SaveNameParts()
        {
            _contact.SetProp(ContactManager._propTitle, _boxTitle.Text);
            _contact.SetProp(ContactManager._propFirstName, _boxFirstName.Text);
            _contact.SetProp(ContactManager._propMiddleName, _boxMidName.Text);
            _contact.SetProp(ContactManager._propLastName, _boxLastName.Text);
            _contact.SetProp(ContactManager._propSuffix, _boxSuffix.Text);
        }
    }
}
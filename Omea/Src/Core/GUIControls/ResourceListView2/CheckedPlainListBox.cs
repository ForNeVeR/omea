// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Represents simple model of the checked listbox over the custom data provider
    /// (DiscreteListDataProvider in this case) with the strighforward API for
    /// checking/unchecking items and getting selected resource.
    /// </summary>
    public class CheckedPlainListBox : ResourceListView2
    {
        private CheckBoxColumn _chboxColumn;
        private RichTextColumn _nameColumn;
        private DiscreteListDataProvider _convDataProvider;

        public CheckedPlainListBox()
        {
            Initialize();
        }

        private void Initialize()
        {
            AllowColumnReorder = false;
            AllowDrop = true;
            ExecuteDoubleClickAction = false;
            HeaderStyle = ColumnHeaderStyle.None;
            HideSelection = false;
            Location = new System.Drawing.Point(0, 0);
            ShowContextMenu = false;
            MultiSelect = false;

            _chboxColumn = AddCheckBoxColumn();
            _nameColumn = new RichTextColumn();
            _nameColumn.AutoSize = true;
            Columns.Add( _nameColumn );
        }

        public IResourceList Resources
        {
            set
            {
                _convDataProvider = new DiscreteListDataProvider(value);
                DataProvider = _convDataProvider;
            }
        }

        public DiscreteListDataProvider Nodes
        {
            get { return _convDataProvider; }
        }

        public void AddDecorator( IResourceNodeDecorator decorator )
        {
            _nameColumn.AddNodeDecorator( decorator );
        }

        public bool Contains(IResource res)
        {
            #region Preconditions
            if (_convDataProvider == null)
                throw new InvalidOperationException("CheckedPlainListBox -- Internal exception: DataProvider must be set before IndexOf calls.");
            #endregion Preconditions

            return DataProvider.FindResourceNode(res);
        }

        public void SetCheckState(IResource res, CheckBoxState state)
        {
            _chboxColumn.SetItemCheckState(res, state);
        }

        public void SetCheckState(int index, CheckBoxState state)
        {
            _chboxColumn.SetItemCheckState(_convDataProvider[index], state);
        }

        public CheckBoxState GetCheckState(IResource res)
        {
            return _chboxColumn.GetItemCheckState(res);
        }

        public int SelectedIndex
        {
            get
            {
                IResourceList sel = GetSelectedResources();
                return (sel != null && sel.Count == 1) ? Nodes.IndexOf(sel[0]) : -1;
            }
        }

        public IResource SelectedResource
        {
            get
            {
                IResourceList sel = GetSelectedResources();
                return (sel != null && sel.Count == 1) ? sel[0] : null;
            }
        }
    }
}

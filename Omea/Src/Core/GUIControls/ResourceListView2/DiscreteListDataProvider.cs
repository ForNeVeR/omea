/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Manages a static list of resources for which a custom add/delete behavior is
    /// required. "Custom" means without facilities within IResourceList behavior.
    /// </summary>
    public class DiscreteListDataProvider : IResourceDataProvider
    {
        protected bool _disposed;
        protected JetListView _listView;
        private readonly List<IResource> _store = new List<IResource>();

        public event EventHandler ResourceCountChanged;

        public DiscreteListDataProvider(IResourceList resList)
        {
            Guard.NullArgument(resList, "resList");

            foreach (IResource res in resList)
                _store.Add(res);
        }

        public void FillResources( ResourceListView2 listView )
        {
            #region Preconditions
            if (_listView != null)
            {
                throw new InvalidOperationException("Attempt to attach a ResourceListDataProvider which is already attached");
            }
            #endregion Preconditions

            _listView = listView.JetListView;
            for (int i = 0; i < _store.Count; i++)
            {
                _listView.Nodes.Add(_store[i]);
            }
        }

        public virtual bool FindResourceNode(IResource res)
        {
            return _store.Contains(res);
        }

        public void AddResource(IResource res)
        {
            if (!_disposed)
            {
                _store.Add(res);
                _listView.Nodes.Add(res);
                OnResourceCountChanged();
            }
        }

        public void AddResourceAt(IResource res, int index)
        {
            if (!_disposed)
            {
                _store.Insert(index, res);
                _listView.Nodes.Add(res);
                JetListViewNode nodeOld = (index > 0)
                                              ? _listView.NodeCollection.NodeFromItem(_store[index - 1])
                                              : null;
                JetListViewNode nodeNew = _listView.NodeCollection.NodeFromItem(res);
                _listView.Nodes.Move(nodeNew, nodeOld);
                OnResourceCountChanged();
            }
        }

        public void RemoveResource(IResource res)
        {
            if (!_disposed && res != null)
            {
                _store.Remove(res);
                if (_listView.NodeCollection.NodeFromItem(res) != null)
                {
                    _listView.Nodes.Remove(res);
                    OnResourceCountChanged();
                }
            }
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public int IndexOf(IResource res)
        {
            return _store.IndexOf(res);
        }

        public IResource this[int index]
        {
            get { return _store[index]; }
            set { _store[index] = value; }
        }

        protected virtual void HandleResourceChanged(object sender, ResourcePropIndexEventArgs e)
        {
            if (!_disposed)
            {
                _listView.UpdateItemSafe(e.Resource);
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _listView = null;
            }
        }

        protected void OnResourceCountChanged()
        {
            if (ResourceCountChanged != null)
            {
                ResourceCountChanged(this, EventArgs.Empty);
            }
        }
    }
}

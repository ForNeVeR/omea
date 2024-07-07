// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
    /**
     * Arguments for a resource drag event.
     */

    public class ResourceDragEventArgs
    {
        private IResource _target;
        private IResourceList _droppedResources;
        private DragDropEffects _effect;

        internal ResourceDragEventArgs( IResource target, IResourceList droppedResources )
        {
            _target = target;
            _droppedResources = droppedResources;
            _effect = DragDropEffects.None;
        }

        public IResource Target
        {
            get { return _target; }
        }

        public IResourceList DroppedResources { get { return _droppedResources; } }
        public DragDropEffects Effect
        {
            get { return _effect; }
            set { _effect = value; }
        }
    }

    public delegate void ResourceDragEventHandler( object sender, ResourceDragEventArgs e );
}

﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea
{
    /**
     * The object that holds the state of the program (active tab and selected workspace).
     */

    internal class WorkspaceTabState
    {
        private string _tabID;
        private int _workspaceID;

        internal WorkspaceTabState( string tabID, int workspaceID )
        {
            _tabID = tabID;
            _workspaceID = workspaceID;
        }

        public override bool Equals( object obj )
        {
            WorkspaceTabState rhs = obj as WorkspaceTabState;
            if ( rhs == null )
                return false;

            return _tabID == rhs._tabID && _workspaceID == rhs._workspaceID;
        }

        public override int GetHashCode()
        {
            return _tabID.GetHashCode() ^ _workspaceID;
        }

        public string GetIniString()
        {
            string workspaceText = (_workspaceID == 0) ? "" : _workspaceID.ToString() + ".";
            return "TabState." + workspaceText + _tabID;
        }

        public override string ToString()
        {
            return GetIniString();
        }
    }
}

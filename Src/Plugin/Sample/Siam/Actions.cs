// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Siam
{
	public class SyncAction : IAction
	{
		/// <summary>
		/// Creates an either Sync action.
		/// </summary>
		/// <param name="pluginSiam">Plugin.</param>
		/// <param name="synctype">Type of the synchronization to be invoked by this action.</param>
		/// <param name="bStart">True if the action should start Sync, False if should abort Sync.</param>
		internal SyncAction(Plugin pluginSiam, Plugin.SyncType synctype, bool bStart)
		{
			_pluginSiam = pluginSiam;
			_synctype = synctype;
			_bStart = bStart;
		}

		/// <summary>
		/// Plugin.
		/// </summary>
		protected Plugin _pluginSiam;

		/// <summary>
		/// Synchronization type to be initiated by this action.
		/// </summary>
		protected Plugin.SyncType	_synctype;

		/// <summary>
		/// True if the action should start Sync, False if should abort Sync.
		/// </summary>
		protected bool	_bStart;

		#region IAction Members

		public void Execute( IActionContext context )
		{
			if(_bStart)
			{
				if(_pluginSiam.Running)
					_pluginSiam.AbortSync();
				_pluginSiam.StartSync( _synctype, true );
			}
			else
				_pluginSiam.AbortSync();
		}

		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			if(_pluginSiam.Running)	// Some sync is running
			{
				if(!_bStart)	// Stop action?
					presentation.Enabled = !_pluginSiam.MustStop;
				else if((_pluginSiam.RunningSyncType == Plugin.SyncType.DeferredSyncIn) && (_synctype != Plugin.SyncType.SyncIn))	// Start action, and a deferred sync is currently running — allow to start sync-out or restart the deferred sync
					presentation.Enabled = true;
				else
					presentation.Enabled = false;
			}
			else	// No sync is running
				presentation.Enabled = _bStart;	// Enable all the start actions
		}

		#endregion
	}
}

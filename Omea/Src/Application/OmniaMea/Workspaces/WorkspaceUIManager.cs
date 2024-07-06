// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for WorkspaceUIManager.
	/// </summary>
	public class WorkspaceUIManager : IDisposable
	{
		/*
		/// <summary>
		/// Gets the Hue value of the color associated with this workspace.
		/// Does not work for the default workspace because it's coloring model differs.
		/// </summary>
		/// <param name="workspace">The workspace to get the color of, must not be <c>Null</c>.</param>
		/// <returns>Hue value, in the range from <c>0</c> up to <see cref=""/></returns>
		public static byte GetWorkspaceColorHue(IResource workspace)
		{
			if(workspace == null)
				throw new ArgumentNullException();

			byte	hueBorder;
			WorkspaceManager	wm = (WorkspaceManager) Core.WorkspaceManager;	// Provides IDs of workspace resource properties

			workspace.Lock();
            try
			{
				if(workspace.HasProp( wm.Props.WorkspaceColorHue ) )	// Hue is available, take it (and constrain safely)
					hueBorder = (byte)( Math.Abs(workspace.GetIntProp( wm.Props.WorkspaceColorHue )) % ColorManagement.MaxHLS );
				else	// Color not available, produce it out of the workspace's hash code
					hueBorder = (byte)new Random(workspace.DisplayName.GetHashCode()).Next( ColorManagement.MaxHLS );
			}
            finally { workspace.UnLock(); }

			return hueBorder;
		}*/

		/// <summary>
		/// The workspace being managed. May be <c>Null</c> in case of the Default workspace.
		/// </summary>
		private IResource _workspace;

		/// <summary>
		/// Creates an instance attached to the given workspace.
		/// </summary>
		/// <param name="workspace"></param>
		public WorkspaceUIManager( IResource workspace )
		{
			_workspace = workspace;
		}

		/// <summary>
		/// Checks whether the given workspace has a color assigned to it, and, if no, generates a random one.
		/// For the <c>Null</c> Default workspace, does nothing.
		/// </summary>
		public void EnsureWorkspaceHasColor( )
		{
			if( _workspace == null )
				return; // No checks for the default workspace

			WorkspaceManager wm = (WorkspaceManager) Core.WorkspaceManager; // Provides IDs of workspace resource properties

			if( _workspace.HasProp( wm.Props.WorkspaceColor ) )
				return; // Assigned already, do nothing

			// No, the workspace does not have a color yet
			// Choose one based on its title
			byte hue = (byte) new Random( _workspace.DisplayName.GetHashCode() ).Next( ColorManagement.MaxHLS );
			Color color = ColorManagement.HLStoRGB( hue, DefaultWorkspaceLuminocity, DefaultWorkspaceSaturation );

			// Apply the property synchronously so that it would be valid as we exit the method
			new ResourceProxy( _workspace ).SetProp( wm.Props.WorkspaceColor, color.ToArgb() );
		}

		/// <summary>
		/// The default <c>Luminocity</c> value for the workspace colors.
		/// The range corresponds to <see cref="ColorManagement.MaxHLS"/>.
		/// </summary>
		public static readonly byte DefaultWorkspaceLuminocity = 93;

		/// <summary>
		/// The default <c>Saturation</c> value for the workspace colors.
		/// The range corresponds to <see cref="ColorManagement.MaxHLS"/>.
		/// </summary>
		public static readonly byte DefaultWorkspaceSaturation = 199;

		/// <summary>
		/// Available workspace colors.
		/// </summary>
		public enum Colors
		{
			/// <summary>
			/// The base (native, as-is) workspace color.
			/// </summary>
			Base,

			/// <summary>
			/// The lightened workspace color.
			/// </summary>
			Light,

			/// <summary>
			/// The darkened workspace color.
			/// </summary>
			Dark,

			/// <summary>
			/// The shadow casted by the workspace on the underlying controls.
			/// </summary>
			Shadow
		}

		/// <summary>
		/// Gets a specific color related to the current workspace.
		/// </summary>
		/// <param name="what">Type of the color to be returned.</param>
		/// <returns>The desired color.</returns>
		public Color GetWorkspaceColor( Colors what )
		{
			EnsureWorkspaceHasColor( ); // Assing the color, if needeed

			// Read the base color (or take the default for Default workspace)
			Color colorBase = _workspace != null ? Color.FromArgb( _workspace.GetIntProp( ((WorkspaceManager) Core.WorkspaceManager).Props.WorkspaceColor ) ) : SystemColors.Control;
			if( what == Colors.Base )
				return colorBase; // The base workspace color, as is

			// Get the HLS components
			byte bH, bL, bS;
			ColorManagement.RGBtoHLS( ColorManagement.RGB( colorBase ), out bH, out bL, out bS );
			double H = bH;
			double L = bL;
			double S = bS;

			// Produce the color appropriate
			switch( what )
			{
			case Colors.Light:
				L += 27.0 * 1.5;
				S -= 43.0 * 1.5;
				break;
			case Colors.Dark:
				L -= 16.0 * 2.0;
				S -= 8.0 * 2.0;
				break;
			case Colors.Shadow:
				L -= 8.0 * 2.0;
				S -= 75.0 * 2.0;
				break;
			default:
				throw new InvalidOperationException( "Unsupported workspace color type." );
			}

			// Constraint
			bH = (byte) (H >= 0 ? (H <= ColorManagement.MaxHLS - 1 ? H : ColorManagement.MaxHLS - 1) : 0);
			bL = (byte) (L >= 0 ? (L <= ColorManagement.MaxHLS - 1 ? L : ColorManagement.MaxHLS - 1) : 0);
			bS = (byte) (S >= 0 ? (S <= ColorManagement.MaxHLS - 1 ? S : ColorManagement.MaxHLS - 1) : 0);

			// Produce the color
			return ColorManagement.HLStoRGB( bH, bL, bS );
		}

		/// <summary>
		/// Gets or sets the workspace color.
		/// </summary>
		/// <remarks>In terms of the <see cref="GetWorkspaceColor"/> function, it's the <see cref="Colors.Base"/> color.</remarks>
		public Color WorkspaceColor
		{
			get
			{
				return GetWorkspaceColor( Colors.Base );
			}
			set
			{
				if(_workspace == null)
					throw new ArgumentNullException("value", "Cannot change color of the Default workspace.");
				new ResourceProxy( _workspace ).SetProp( ((WorkspaceManager)Core.WorkspaceManager).Props.WorkspaceColor, value.ToArgb() );
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			_workspace = null;
		}

		#endregion
	}
}

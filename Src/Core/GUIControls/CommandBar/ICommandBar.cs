// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;

namespace JetBrains.Omea.GUIControls.CommandBar
{
	/// <summary>
	/// The child command bar interface that provides for docking a command bar within a parent container, adjusting its size, dragging, etc.
	/// </summary>
	public interface ICommandBar
	{
		/// <summary>
		/// Assigns the (parent) command bar site that hosts this command bar and that can be requested of position or size change.
		/// </summary>
		/// <param name="site">Command bar site object. Must not be <c>Null</c>.</param>
		/// <remarks>This member is mandatory.</remarks>
		void SetSite( ICommandBarSite site );

		/// <summary>
		/// Provides the minimum size of the control.
		/// </summary>
		/// <remarks>
		/// <para>Tells the callee that the control can not be resized to the size below this value.</para>
		/// <para>Both components must be defined and be non-negative.</para>
		/// <para>Note that this value dictates the layouter just a SHOULD, not a MUST, and the control must be prepared to being set to an unfit size.</para>
		/// <para>This member is mandatory.</para>
		/// </remarks>
		Size MinSize { get; }

		/// <summary>
		/// Provides the maximum size of the control.
		/// </summary>
		/// <remarks>
		/// <para>Tells the callee that the control can not be resized to the size above this value, for example, there's no meaningful info to fill in the excessive space.</para>
		/// <para>Use <see cref="int.MaxValue"/> for the appropriate component if you do not wish to limit its size.</para>
		/// <para>Both components must be defined and be non-negative.</para>
		/// <para>Note that this value dictates the layouter just a SHOULD, not a MUST, and the control must be prepared to being set to an unfit size.</para>
		/// <para>This member is mandatory.</para>
		/// </remarks>
		Size MaxSize { get; }

		/// <summary>
		/// Provides the optimal size of the control.
		/// </summary>
		/// <remarks>
		/// <para>Tells the callee that the control should be granted this size if there is such a possibility and are no constraints.</para>
		/// <para>Both components must be defined and be non-negative.</para>
		/// <para>Note that this value dictates the layouter just a SHOULD, not a MUST, and the control must be prepared to being set to an unfit size.</para>
		/// <para>This member is mandatory.</para>
		/// </remarks>
		Size OptimalSize { get; }

		/// <summary>
		/// Step for changing the size in either direction.
		/// </summary>
		/// <remarks><para>This value means that the horizontal and vertical size of the control can be changed by the <see cref="Size.Width"/> and <see cref="Size.Height"/> steps only.</para></remarks>
		Size Integral { get; }
	}

	/// <summary>
	/// Interface for the site that hosts one or more command bars.
	/// </summary>
	/// <remarks>Interface for the layouter which controls the command bars placement.</remarks>
	public interface ICommandBarSite
	{
		/// <summary>
		/// A command bar requests its site to move it (for example, due to being dragged by a grip).
		/// </summary>
		/// <param name="sender">The command bar that is sending the request.</param>
		/// <param name="offset">The desired horizontal and vertical offset from the current position.</param>
		/// <returns>Whether the move request was fulfilled or not.</returns>
		/// <remarks>Note that the move request may be fulfilled partially only, for example, just horizontal not vertical move, or only a partial move up to some limit.</remarks>
		bool RequestMove( ICommandBar sender, Size offset );

		/// <summary>
		/// A command bar requests its site to resize it (for example, due to being resized by user).
		/// </summary>
		/// <param name="sender">The command bar that is sending the request.</param>
		/// <param name="difference">The desired horizontal and vertical change in size, relative to the current value.</param>
		/// <returns>Whether the sizing request was fulfilled or not.</returns>
		/// <remarks>Note that the sizing request may be fulfilled partially only, for example, just horizontal not vertical sizing, or only a partial resizing up to some limit.</remarks>
		bool RequestSize( ICommandBar sender, Size difference );

		/// <summary>
		/// Requests the layout to be performed on the command bar site when either
		/// <see cref="ICommandBar.MinSize"/>, <see cref="ICommandBar.MaxSize"/>, or <see cref="ICommandBar.OptimalSize"/>
		/// changes and the control should be resized or repositioned accordingly.
		/// </summary>
		/// <param name="sender">A <see cref="ICommandBar">command bar</see> that requests the layouting.</param>
		/// <returns>Whether the layouting request was accepted (<c>True</c>) or rejected (<c>False</c>).</returns>
		bool PerformLayout(ICommandBar sender);
	}
}

// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// An action that executes a specific command on the command processor.
	/// </summary>
	/// <remarks>
	/// <p>This action is populated upon creation with a string that represents a command name.</p>
	/// <p>Upon execution, the command is applied to a <see cref="ICommandProcessor">command processor</see> of the <see cref="IActionContext">action context</see> provided along with the execution request.</p>
	/// <p>The same command is used to retrieve the visual state and style information for the action.</p>
	/// </remarks>
	public class CommandProcessorAction : IAction, IComparable
	{
		/// <summary>
		/// Initializes the instance by populating it with a particular command.
		/// </summary>
		/// <param name="command">Name of the command to execute and query for visual state.</param>
		/// <remarks>
		/// <p>This action is populated upon creation with a string that represents a command name.</p>
		/// <p>Upon execution, the command is applied to a <see cref="ICommandProcessor">command processor</see> of the <see cref="IActionContext">action context</see> provided along with the execution request.</p>
		/// <p>The same command is used to retrieve the visual state and style information for the action.</p>
		/// </remarks>
		public CommandProcessorAction( string command )
		{
			_command = command;
		}

		/// <summary>
		/// Name of a command that this action executes or queries for the state on an active command processor.
		/// </summary>
		protected string _command;

		/// <summary>
		/// Name of a command that this action executes or queries for the state on an active command processor.
		/// </summary>
		public string Command
		{
			get { return _command; }
		}

		#region IAction Members

		/// <summary>
		/// Executes an action by applying the command specified on creation to the <see cref="ICommandProcessor">command processor</see> supplied to this method thru the <paramref name="context"/> parameter.
		/// </summary>
		/// <param name="context">Context for this action.</param>
		public void Execute( IActionContext context )
		{
			context.CommandProcessor.ExecuteCommand( _command );
		}

		/// <summary>
		/// Updates the action's state based on the <see cref="ICommandProcessor">command processor</see>'s (supplied to this method thru the <paramref name="context"/> parameter) ability to execute this command, as specified in the constructor.
		/// </summary>
		/// <param name="context">Context for this action.</param>
		/// <param name="presentation">An object that allows to control the state and style of a visual representation of this action.</param>
		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			bool avail = context.CommandProcessor.CanExecuteCommand( Command );

			if( context.Kind == ActionContextKind.ContextMenu )
				presentation.Visible = avail; // Hide disabled items from the context menu
			else
				presentation.Enabled = avail; // Let disabled items at other places
		}

		#endregion

		#region Object Overrides

		public override string ToString()
		{
			return "CommandProcessorAction/" + _command;
		}

		public override bool Equals( object obj )
		{
			return _command.Equals( ((CommandProcessorAction)obj)._command );
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		#endregion

		#region IComparable Members

		public int CompareTo( object obj )
		{
			return _command.CompareTo( ((CommandProcessorAction)obj)._command );
		}

		#endregion
	}
}

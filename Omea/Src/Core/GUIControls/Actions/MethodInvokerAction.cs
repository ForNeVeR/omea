// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// An action that relies on methods of some class represented with delegates (as specified upon creation of the action) for executing and updating the action state.
	/// </summary>
	/// <remarks>
	/// <para>You may use this action type if you would like to support more than one action within the same class by just defining a pair of methods for each of the actions.</para>
	/// </remarks>
	public class MethodInvokerAction : IAction, IComparable
	{
		/// <summary>
		/// Initializes the action by populating it with delegates representing the methods that will handle Execute and Update methods of the action.
		/// </summary>
		/// <param name="delegateExecute">A delegate for the method that will handle the action execution.</param>
		/// <param name="delegateUpdate">A delegate for the method that will handle the action update.</param>
		/// <remarks>
		/// <para>You may supply <c>Null</c> in place of one or more delegates if you wish the action to do nothing in response to the corresponding event.</para>
		/// </remarks>
		public MethodInvokerAction( ActionExecuteDelegate delegateExecute, ActionUpdateDelegate delegateUpdate )
		{
			_delegateExecute = delegateExecute;
			_delegateUpdate = delegateUpdate;
		}

		/// <summary>
		/// Stores a delegate that executes the action.
		/// </summary>
		protected ActionExecuteDelegate _delegateExecute;

		/// <summary>
		/// Stores a delegate that updates the action.
		/// </summary>
		protected ActionUpdateDelegate _delegateUpdate;

		#region Implementation

		/// <summary>
		/// String representation of the Execute delegate used for comparison and uniqueness identification.
		/// </summary>
		protected string DelegateExecuteString
		{
			get { return _delegateExecute != null ? _delegateExecute.Method.Name + "#" + _delegateExecute.GetHashCode().ToString() : "Null"; }
		}

		/// <summary>
		/// String representation of the Update delegate used for comparison and uniqueness identification.
		/// </summary>
		protected string DelegateUpdateString
		{
			get { return _delegateUpdate != null ? _delegateUpdate.Method.Name + "#" + _delegateUpdate.GetHashCode().ToString() : "Null"; }
		}

		#endregion

		#region IAction Members

		/// <summary>
		/// Executes the delegate.
		/// </summary>
		public void Execute( IActionContext context )
		{
			if( _delegateExecute != null )
				_delegateExecute( context );
		}

		/// <summary>
		/// Executes the delegate.
		/// </summary>
		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			if( _delegateUpdate != null )
				_delegateUpdate( context, ref presentation );
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Provides a string rep that provides for disambiguating between actions for different methods.
		/// </summary>
		public override string ToString()
		{
			return String.Format( "MethodInvokerAction({0},{1})", DelegateExecuteString, DelegateUpdateString );
		}

		public override int GetHashCode()
		{
			// Derive a hash code from this instance and delegates passed inside
			return (((base.GetHashCode() * 29)
				^ (_delegateExecute != null ? _delegateExecute.GetHashCode() : 0)) * 29)
				^ (_delegateUpdate != null ? _delegateUpdate.GetHashCode() : 0);
		}

		public override bool Equals( object obj )
		{
			MethodInvokerAction other = (MethodInvokerAction)obj;
			return ((_delegateExecute.Equals( other._delegateExecute )) && (_delegateUpdate.Equals( other._delegateUpdate )));
		}

		#endregion

		#region IComparable Members

		public int CompareTo( object obj )
		{
			MethodInvokerAction other = (MethodInvokerAction)obj;
			int nResult = 0;

			// First, the Execute delegate
			nResult = DelegateExecuteString.CompareTo( other.DelegateExecuteString );
			if( nResult != 0 )
				return nResult;

			// Second, the Update delegate
			nResult = DelegateUpdateString.CompareTo( other.DelegateUpdateString );
			if( nResult != 0 )
				return nResult;

			// Equal, then
			return 0;
		}

		#endregion
	}

	/// <summary>
	/// A delegate for the method that executes an action (for <see cref="IAction.Execute"/>).
	/// </summary>
	public delegate void ActionExecuteDelegate( IActionContext context );

	/// <summary>
	/// A delegate for the method that updates an action (for <see cref="IAction.Update"/>).
	/// </summary>
	public delegate void ActionUpdateDelegate( IActionContext context, ref ActionPresentation presentation );
}

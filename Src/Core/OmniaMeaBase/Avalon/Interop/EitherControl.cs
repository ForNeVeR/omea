// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using JetBrains.Annotations;

namespace JetBrains.UI.Avalon.Interop
{
	/// <summary>
	/// Adapts the WinForms and Avalon controls to one another.
	/// </summary>
	public class EitherControl : IDisposable
	{
		#region Data

		/// <summary>
		/// Stores the WinForms control, if explicitly inited.
		/// <see cref="myControl"/> and <see cref="myElement"/> are mutually exclusive.
		/// </summary>
		private readonly Control myControl;

		/// <summary>
		/// Stores the Avalon element, if explicitly inited.
		/// <see cref="myControl"/> and <see cref="myElement"/> are mutually exclusive.
		/// </summary>
		private readonly UIElement myElement;

		/// <summary>
		/// Stores the element host that adapts an Avalon control to the WinForms host, in case we were created with an Avalon control. Lazy-created.
		/// </summary>
		private ElementHost myElementHost;

		private bool myIsDisposed;

		/// <summary>
		/// Stores the winforms host that adapts a WinForms control to the Avalon host, in case we were created with a WinForms control. Lazy-created.
		/// </summary>
		private WindowsFormsHost myWindowsFormsHost;

		#endregion

		#region Init

		/// <summary>
		/// Initializes from a WinForms control.
		/// </summary>
		public EitherControl([NotNull] Control control)
		{
			if(control == null)
				throw new ArgumentNullException("control");

			// Try unwrapping
			var host = control as ElementHost;
			if(host != null)
			{
				myElement = host.Child;
				myElementHost = host;
			}
			else
				myControl = control;
		}

		/// <summary>
		/// Initializes from an Avalon element.
		/// </summary>
		public EitherControl([NotNull] UIElement element)
		{
			if(element == null)
				throw new ArgumentNullException("element");

			// Try unwrapping
			var host = element as WindowsFormsHost;
			if(host != null)
			{
				myControl = host.Child;
				myWindowsFormsHost = host;
			}
			else
				myElement = element;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the contained either-control as a Windows Forms control, creating a wrapper as needed.
		/// </summary>
		public Control Control
		{
			get
			{
				if(myControl != null)
					return myControl;
				if(myElementHost == null)
				{
					myElementHost = new ElementHost();
					myElementHost.Child = myElement;
				}
				return myElementHost;
			}
		}

		/// <summary>
		/// Gets the contained either-control as an Avalon UI element, creating a wrapper as needed.
		/// </summary>
		public UIElement Element
		{
			get
			{
				if(myElement != null)
					return myElement;
				if(myWindowsFormsHost == null)
				{
					myWindowsFormsHost = new WindowsFormsHost();
					myWindowsFormsHost.Child = myControl;
				}
				return myWindowsFormsHost;
			}
		}

		/// <summary>
		/// Gets whether the WinForms controls have been disposed of.
		/// </summary>
		public bool IsDisposed
		{
			get
			{
				return myIsDisposed;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Detects the control type, throws if neither. Calls the appropriate ctor.
		/// </summary>
		public static EitherControl FromObject([NotNull] object either)
		{
			if(either == null)
				throw new ArgumentNullException("either");

			var control = either as Control;
			if(control != null)
				return new EitherControl(control);

			var element = either as UIElement;
			if(element != null)
				return new EitherControl(element);

			throw new InvalidOperationException(string.Format("The control type “{0}” is not supported.", either.GetType().AssemblyQualifiedName));
		}

		#endregion

		#region ERROR

		public static implicit operator Control([NotNull] EitherControl either)
		{
			if(either == null)
				throw new ArgumentNullException("either");
			return either.Control;
		}

		public static implicit operator UIElement([NotNull] EitherControl either)
		{
			if(either == null)
				throw new ArgumentNullException("either");
			return either.Element;
		}

		public static implicit operator EitherControl([NotNull] Control control)
		{
			if(control == null)
				throw new ArgumentNullException("control");
			return new EitherControl(control);
		}

		public static implicit operator EitherControl([NotNull] UIElement element)
		{
			if(element == null)
				throw new ArgumentNullException("element");
			return new EitherControl(element);
		}

		#endregion

		#region IDisposable Members

		///<summary>
		///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		///</summary>
		///<filterpriority>2</filterpriority>
		public void Dispose()
		{
			myIsDisposed = true;
			if(myControl != null)
				myControl.Dispose();
			if(myElementHost != null)
				myElementHost.Dispose();
		}

		#endregion
	}
}

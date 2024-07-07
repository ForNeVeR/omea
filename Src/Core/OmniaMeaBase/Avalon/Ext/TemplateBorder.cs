// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows;
using System.Windows.Controls;

using System35;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// A border-derived control that supports runtime delegate-based data templates.
	/// On creation, exeutes the supplied code to build the nested objects.
	/// </summary>
	public class TemplateBorder : Decorator
	{
		#region Data

		public static readonly DependencyProperty ObjectGraphCreatorProperty = DependencyProperty.Register("ObjectGraphCreator", typeof(Func<UIElement>), typeof(TemplateBorder), new PropertyMetadata(OnObjectGraphCreatorPropertyChanged));

		#endregion

		#region Implementation

		private static void OnObjectGraphCreatorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			((TemplateBorder)d).ApplyObjectGraphCreatorProperty((Func<UIElement>)args.NewValue);
		}

		/// <summary>
		/// Creates the new object graph whenever <see cref="ObjectGraphCreatorProperty"/> changes its value.
		/// </summary>
		private void ApplyObjectGraphCreatorProperty(Func<UIElement> value)
		{
			try
			{
				Child = value != null ? value() : null;
			}
			catch(Exception ex)
			{
				Core.ReportException(new InvalidOperationException(string.Format("Could not create the object graph. {0}", ex.Message), ex));
			}
		}

		#endregion
	}
}

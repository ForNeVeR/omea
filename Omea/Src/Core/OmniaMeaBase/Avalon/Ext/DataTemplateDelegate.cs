// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows;

using System35;

using JetBrains.Annotations;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// Supports a data template that executes some code to create the object graph.
	/// </summary>
	public class DataTemplateDelegate : DataTemplate
	{
		#region Init

		/// <summary>
		/// Creates a Data Template that produces the object graph at runtime.
		/// </summary>
		/// <param name="funcObjectGraphCreator">The function to create the object graph.</param>
		public DataTemplateDelegate([NotNull] Func<UIElement> funcObjectGraphCreator)
		{
			if(funcObjectGraphCreator == null)
				throw new ArgumentNullException("funcObjectGraphCreator");

			var factory = new FrameworkElementFactory(typeof(TemplateBorder));
			factory.SetValue(TemplateBorder.ObjectGraphCreatorProperty, funcObjectGraphCreator);

			VisualTree = factory;
		}

		#endregion
	}
}

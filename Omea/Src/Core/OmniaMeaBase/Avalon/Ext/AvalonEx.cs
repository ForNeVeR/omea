// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using JetBrains.Annotations;
using JetBrains.Util;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// Extension methods for Avalon.
	/// </summary>
	public static class AvalonEx
	{
		#region Operations

		/// <summary>
		/// Adds a new child to the panel control.
		/// </summary>
		[NotNull]
		public static TPanel AddChild<TPanel>([NotNull] this TPanel panel, UIElement child) where TPanel : Panel
		{
			if(panel == null)
				throw new ArgumentNullException("panel");
			if(child == null)
				throw new ArgumentNullException("child");

			panel.Children.Add(child);
			return panel;
		}

		/// <summary>
		/// Adds a new column to the grid, placing a child in the newly-added column.
		/// </summary>
		[NotNull]
		public static Grid AddColumnChild([NotNull] this Grid grid, [NotNull] string size, [NotNull] UIElement child)
		{
			if(grid == null)
				throw new ArgumentNullException("grid");
			if(size.IsEmpty())
				throw new ArgumentNullException("size");
			if(child == null)
				throw new ArgumentNullException("child");

			// Create a new row for the child
			grid.ColumnDefinitions.Add(new ColumnDefinition {Width = (GridLength)TypeDescriptor.GetConverter(typeof(GridLength)).ConvertFromInvariantString(size)});

			// Specify child location
			Grid.SetColumn(child, grid.ColumnDefinitions.Count - 1);
			Grid.SetRow(child, 0);

			// Add child
			grid.Children.Add(child);

			return grid;
		}

		[NotNull]
		public static Paragraph AddPara([NotNull] this FlowDocument document)
		{
			if(document == null)
				throw new ArgumentNullException("document");

			var para = new Paragraph();
			document.Blocks.Add(para);
			return para;
		}

		/// <summary>
		/// Adds a new row to the grid, placing a child in the newly-added row.
		/// </summary>
		[NotNull]
		public static Grid AddRowChild([NotNull] this Grid grid, [NotNull] string size, [NotNull] UIElement child)
		{
			if(grid == null)
				throw new ArgumentNullException("grid");
			if(size.IsEmpty())
				throw new ArgumentNullException("size");
			if(child == null)
				throw new ArgumentNullException("child");

			// Create a new row for the child
			grid.RowDefinitions.Add(new RowDefinition {Height = (GridLength)TypeDescriptor.GetConverter(typeof(GridLength)).ConvertFromInvariantString(size)});

			// Specify child location
			Grid.SetColumn(child, 0);
			Grid.SetRow(child, grid.RowDefinitions.Count - 1);

			// Add child
			grid.Children.Add(child);

			return grid;
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] string text)
		{
			return Append(block, text, FontStyles.Normal, FontWeights.Normal);
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] string text, FontStyle style)
		{
			return Append(block, text, style, FontWeights.Normal);
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] string text, FontWeight weight)
		{
			return Append(block, text, FontStyles.Normal, weight);
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] Run run)
		{
			if(block == null)
				throw new ArgumentNullException("block");
			if(run == null)
				throw new ArgumentNullException("run");

			block.Inlines.Add(run);
			return block;
		}

		/// <summary>
		/// Adds one more <see cref="Inline"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] Inline run)
		{
			if(block == null)
				throw new ArgumentNullException("block");
			if(run == null)
				throw new ArgumentNullException("run");

			block.Inlines.Add(run);
			return block;
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="TextBlock"/>.
		/// </summary>
		public static TextBlock Append([NotNull] this TextBlock block, [NotNull] string text, FontStyle style, FontWeight weight)
		{
			if(block == null)
				throw new ArgumentNullException("block");
			if(text == null)
				throw new ArgumentNullException("text");

			block.Inlines.Add(new Run(text) {FontStyle = style, FontWeight = weight});
			return block;
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="Paragraph"/>.
		/// </summary>
		[NotNull]
		public static Paragraph Append([NotNull] this Paragraph para, [NotNull] string text)
		{
			return Append(para, text, FontStyles.Normal, FontWeights.Normal);
		}

		/// <summary>
		/// Adds one more <see cref="Run"/> to the <see cref="Paragraph"/>.
		/// </summary>
		[NotNull]
		public static Paragraph Append([NotNull] this Paragraph para, [NotNull] string text, FontStyle style, FontWeight weight)
		{
			if(para == null)
				throw new ArgumentNullException("para");
			if(text == null)
				throw new ArgumentNullException("text");

			para.Inlines.Add(new Run(text) {FontStyle = style, FontWeight = weight});
			return para;
		}

		/// <summary>
		/// Establishes a two-way property binding on the element.
		/// The “Update Source” is triggered according to the default scenario (ie on focus loss for an edit box).
		/// </summary>
		[NotNull]
		public static TElement Bind<TElement>([NotNull] this TElement element, [NotNull] DependencyProperty property, [NotNull] string path) where TElement : FrameworkElement
		{
			element.SetBinding(property, path);
			return element;
		}

		/// <summary>
		/// Establishes a two-way property binding on the element.
		/// The “Update Source” is triggered according to the default scenario (ie on focus loss for an edit box).
		/// </summary>
		[NotNull]
		public static TElement Bind<TElement>([NotNull] this TElement element, [NotNull] DependencyProperty property, [NotNull] BindingBase binding) where TElement : FrameworkElement
		{
			element.SetBinding(property, binding);
			return element;
		}

		/// <summary>
		/// Establishes a two-way property binding on the element.
		/// The “Update Source” is triggeret immediately when the property changes (ie on typing for an edit box).
		/// </summary>
		public static TElement BindOnChange<TElement>(this TElement element, DependencyProperty property, string path) where TElement : FrameworkElement
		{
			element.SetBinding(property, new Binding(path) {UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged});
			return element;
		}

		/// <summary>
		/// Adds columns to the grid. Each sizes element is a Star grid-length value.
		/// </summary>
		public static Grid Cols([NotNull] this Grid grid, [NotNull] params string[] sizes)
		{
			if(grid == null)
				throw new ArgumentNullException("grid");
			if(sizes == null)
				throw new ArgumentNullException("sizes");

			foreach(string size in sizes)
				grid.ColumnDefinitions.Add(new ColumnDefinition {Width = (GridLength)TypeDescriptor.GetConverter(typeof(GridLength)).ConvertFromInvariantString(size)});

			return grid;
		}

		/// <summary>
		/// Constraints the <paramref name="size"/> to be no more than <paramref name="constraint"/> on each of the dimensions independently.
		/// </summary>
		public static Size Constrain(this Size size, Size constraint)
		{
			if(constraint.Width == double.NaN)
				constraint.Width = double.PositiveInfinity;
			if(constraint.Height == double.NaN)
				constraint.Height = double.PositiveInfinity;
			return new Size(size.Width <= constraint.Width ? size.Width : constraint.Width, size.Height <= constraint.Height ? size.Height : constraint.Height);
		}

		/// <summary>
		/// A converter.
		/// </summary>
		public static Visibility ConvertBoolToVisibility(bool visible)
		{
			return visible ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>
		/// Sets the <see cref="DockPanel.DockProperty"/> extension property on the specified UI element.
		/// </summary>
		public static TElement Dock<TElement>(this TElement element, Dock dock) where TElement : UIElement
		{
			return element.Set(DockPanel.DockProperty, dock);
		}

		/// <summary>
		/// Sets the <see cref="Grid.ColumnProperty"/> and <see cref="Grid.RowProperty"/> extension properties on the specified UI element.
		/// </summary>
		public static TElement InGrid<TElement>(this TElement element, int col, int row) where TElement : UIElement
		{
			return element.Set(Grid.ColumnProperty, col).Set(Grid.RowProperty, row);
		}

		/// <summary>
		/// Sets the <see cref="Grid.ColumnProperty"/> and <see cref="Grid.RowProperty"/> extension properties on the specified UI element.
		/// In addition, specifies the <see cref="Grid.ColumnSpanProperty"/> and <see cref="Grid.RowSpanProperty"/> extension properties.
		/// </summary>
		public static TElement InGrid<TElement>(this TElement element, int col, int row, int colspan, int rowspan) where TElement : UIElement
		{
			return element.Set(Grid.ColumnProperty, col).Set(Grid.RowProperty, row).Set(Grid.ColumnSpanProperty, colspan).Set(Grid.RowSpanProperty, rowspan);
		}

		/// <summary>
		/// Mixes two colors together.
		/// </summary>
		public static Color MixWith(this Color first, Color second, float firstpercentage)
		{
			return Color.Add(Color.Multiply(first, firstpercentage), Color.Multiply(second, 1 - firstpercentage));
		}

		/// <summary>
		/// Registers a name for the object in the host's name scope.
		/// The name scope must first be registered for the host or one of its parents.
		/// </summary>
		public static TObject Name<TObject>(this TObject @object, FrameworkElement host, string name) where TObject : DependencyObject
		{
			host.RegisterName(name, @object);
			return @object;
		}

		/// <summary>
		/// Sinks the specified <paramref name="@event"/> on the <paramref name="element"/>.
		/// </summary>
		public static TElement OnEvent<TElement>(this TElement element, RoutedEvent @event, RoutedEventHandler handler) where TElement : UIElement
		{
			element.AddHandler(@event, handler);
			return element;
		}

		/// <summary>
		/// Sinks the specified <paramref name="@event"/> on the <paramref name="element"/>.
		/// </summary>
		public static TElement OnEventC<TElement>(this TElement element, RoutedEvent @event, RoutedEventHandler handler) where TElement : ContentElement
		{
			element.AddHandler(@event, handler);
			return element;
		}

		/// <summary>
		/// Adds rows to the grid. Each sizes element is a Star grid-length value.
		/// </summary>
		public static Grid Rows([NotNull] this Grid grid, [NotNull] params string[] sizes)
		{
			if(grid == null)
				throw new ArgumentNullException("grid");
			if(sizes == null)
				throw new ArgumentNullException("sizes");

			foreach(string size in sizes)
				grid.RowDefinitions.Add(new RowDefinition {Height = (GridLength)TypeDescriptor.GetConverter(typeof(GridLength)).ConvertFromInvariantString(size)});

			return grid;
		}

		/// <summary>
		/// Applies a scale layout transformation to the element.
		/// </summary>
		public static TElement Scale<TElement>(this TElement element, double factor) where TElement : FrameworkElement
		{
			var transform = new ScaleTransform(factor, factor);

			// Combine with existing?
			if(element.LayoutTransform != null)
			{
				var group = element.LayoutTransform as TransformGroup;
				if(group == null)
				{
					group = new TransformGroup();
					group.Children.Add(element.LayoutTransform);
					element.LayoutTransform = group;
				}
				group.Children.Add(transform);
			}
			else
				element.LayoutTransform = transform;
			return element;
		}

		/// <summary>
		/// Sets a dependency property on a dependency object, allows to pipe such settings.
		/// Especially useful for setting extension properties.
		/// </summary>
		public static TDependencyObject Set<TDependencyObject>([NotNull] this TDependencyObject dependencyobject, [NotNull] DependencyProperty dependencyproperty, object value) where TDependencyObject : DependencyObject
		{
			if(dependencyobject == null)
				throw new ArgumentNullException("dependencyobject");
			if(dependencyproperty == null)
				throw new ArgumentNullException("dependencyproperty");

			dependencyobject.SetValue(dependencyproperty, value);

			return dependencyobject;
		}

		/// <summary>
		/// Applies dialog font to a control.
		/// </summary>
		public static TElement SetDialogFont<TElement>(this TElement element) where TElement : Control
		{
			element.FontFamily = new FontFamily("Corbel");
			element.FontSize = 13;
			element.FontWeight = FontWeights.Normal;
			element.FontStyle = FontStyles.Normal;
			return element;
		}

		/// <summary>
		/// Applies editor font to a control.
		/// </summary>
		public static TElement SetEditorFont<TElement>(this TElement element) where TElement : Control
		{
			element.FontFamily = new FontFamily("Consolas");
			element.FontSize = 14;
			element.FontWeight = FontWeights.Normal;
			element.FontStyle = FontStyles.Normal;
			return element;
		}

		/// <summary>
		/// By default, a document has some large serif font applied.
		/// Makes the document follow the system font family and sizes.
		/// </summary>
		[NotNull]
		public static FlowDocument SetSystemFont([NotNull] this FlowDocument document)
		{
			document.FontFamily = SystemFonts.MessageFontFamily;
			document.FontSize = SystemFonts.MessageFontSize;
			document.FontStyle = SystemFonts.MessageFontStyle;
			document.FontWeight = SystemFonts.MessageFontWeight;

			return document;
		}

		#endregion
	}
}

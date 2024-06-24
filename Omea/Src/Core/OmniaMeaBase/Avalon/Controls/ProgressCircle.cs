// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JetBrains.UI.Avalon.Controls
{
	/// <summary>
	/// Implements a progress indication control in a circle.
	/// </summary>
	public class ProgressCircle : RangeBase
	{
		#region Data

		private readonly Grid myGridRoot = new Grid();

		#endregion

		#region Init

		public ProgressCircle()
		{
			InitView();
		}

		#endregion

		#region Implementation

		private void InitView()
		{
			AddLogicalChild(myGridRoot);
			AddVisualChild(myGridRoot);

			// Background
			myGridRoot.Children.Add(new Ellipse {Fill = SystemColors.ControlBrush});

			// Outer rim
			myGridRoot.Children.Add(new Ellipse {Stroke = Brushes.Green});

			// Placeholder for the progress elements
			Grid gridProgress;
			myGridRoot.Children.Add(gridProgress = new Grid());

			// Inner rim
			Grid grid;
			myGridRoot.Children.Add(grid = new Grid().Cols("*", "*", "*").Rows("*", "*", "*"));
			grid.Children.Add(AvalonEx.InGrid(new Ellipse {Fill = SystemColors.ControlBrush, Stroke = Brushes.Green}, 1, 1));

			// Temp: Progress Elements
			Image image;
			gridProgress.Children.Add(image = new Image());
			Geometry geometry = new EllipseGeometry(new Point(50, 50), 50, 50);
			Drawing drawing = new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0x00, 0xC0, 0x00)), null, geometry);
			image.Source = new DrawingImage(drawing);
		}

		private void UpdatePosition()
		{
			// TODO
		}

		#endregion

		#region Overrides

		///<summary>
		///Called to arrange and size the content of a <see cref="T:System.Windows.Controls.Control"></see> object.
		///</summary>
		///
		///<returns>
		///The size of the control.
		///</returns>
		///
		///<param name="arrangeBounds">The computed size that is used to arrange the content.</param>
		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			myGridRoot.Arrange(new Rect(new Point(), arrangeBounds));
			return arrangeBounds;
		}

		///<summary>
		///Called to remeasure a control.
		///</summary>
		///
		///<returns>
		///The size of the control.
		///</returns>
		///
		///<param name="constraint">Measurement constraints, a control cannot return a size larger than the constraint.</param>
		protected override Size MeasureOverride(Size constraint)
		{
			return new Size(24, 24).Constrain(constraint);
		}

		///<summary>
		///Called when the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Maximum"></see> property changes.
		///</summary>
		///
		///<param name="newMaximum">New value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Maximum"></see> property.</param>
		///<param name="oldMaximum">Old value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Maximum"></see> property.</param>
		protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
		{
			base.OnMaximumChanged(oldMaximum, newMaximum);
			UpdatePosition();
		}

		///<summary>
		/// Called when the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Minimum"></see> property changes.
		///</summary>
		///
		///<param name="oldMinimum">Old value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Minimum"></see> property.</param>
		///<param name="newMinimum">New value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Minimum"></see> property.</param>
		protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
		{
			base.OnMinimumChanged(oldMinimum, newMinimum);
			UpdatePosition();
		}

		///<summary>
		/// Called when the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Value"></see> property changes.
		///</summary>
		///
		///<param name="oldValue">Old value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Value"></see> property</param>
		///<param name="newValue">New value of the <see cref="P:System.Windows.Controls.Primitives.RangeBase.Value"></see> property</param>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);
			UpdatePosition();
		}

		#endregion
	}
}

/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using JetBrains.Omea.PresentationEx.Scheduller;

namespace Runner
{
	/// <summary>
	/// Interaction logic for RunnerWindow.xaml
	/// </summary>

	public partial class RunnerWindow : System.Windows.Window
	{

		public RunnerWindow()
		{
			InitializeComponent();
		}

		private void OnTaskPicker(object sender, RoutedEventArgs e)
		{
			new TaskPickerWindow().Show();
		}
	}
}
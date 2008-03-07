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

using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.PresentationEx.Scheduller
{
	/// <summary>
	/// Displays a picker for the scheduller tasks.
	/// </summary>
	public partial class TaskPickerWindow : System.Windows.Window
	{
		protected IResourceObjectsListByName<ISchedullerTask> myResult = null;

		public TaskPickerWindow()
		{
			InitializeComponent();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(this, "Clicked", "Cancel", MessageBoxButton.OK, MessageBoxImage.Information);
			Close();
		}

		private void OnOk(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(this, "Clicked", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
			myResult = null;
			//myPickerControl.
			Close();
		}

		/// <summary>
		/// After the dialog is executed, gets the result.
		/// <c>Null</c> if not executed yet.
		/// </summary>
		IResourceObjectsListByName<ISchedullerTask> Result { get
		{
			return myResult;
		}
		}

		
	}
}
// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using JetBrains.Omea.OpenApiEx;

using Timer=System.Threading.Timer;

namespace JetBrains.Omea.PresentationEx.Scheduller
{
	/// <summary>
	/// Interaction logic for TaskPicker.xaml
	/// </summary>

	public partial class TaskPicker : System.Windows.Controls.UserControl
	{
		protected DispatcherTimer myTimer;

		/// <summary>
		/// The list of all the tasks available for picking.
		/// Must not be <c>Null</c>.
		/// </summary>
		protected IResourceObjectsList<ISchedullerTask> myAllTasks = CoreEx.Scheduller.Tasks;

		/// <summary>
		/// The list of tasks that meet the filter currently in effect.
		/// Must not be <c>Null</c>.
		/// </summary>
		private IResourceObjectsList<ISchedullerTask> myFilteredTasks = CoreEx.Scheduller.Tasks;

		public TaskPicker()
		{
			myTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Normal, OnTimer, Dispatcher.CurrentDispatcher);
			InitializeComponent();

			//myData = "Insofar as I may be heard by anything, which may or may not care what I say, I ask, if it matters, that you be forgiven for anything you may have done or failed to do which requires forgiveness. Conversely, if not forgiveness but something else may be required to insure any possible benefit for which you may be eligible after the destruction of your body, I ask that this, whatever it may be, be granted or withheld, as the case may be, in such a manner as to insure your receiving said benefit. I ask this in my capacity as your elected intermediary between yourself and that which may not be yourself, but which may have an interest in the matter of your receiving as much as it is possible for you to receive of this thing, and which may in some way be influenced by this ceremony. Amen.".Split(' ');

			Refilter("");
		}

		/// <summary>
		/// Gets or sets the list of all the tasks available for picking.
		/// By default, it's <see cref="IScheduller.Tasks"/>.
		/// </summary>
		public IResourceObjectsList<ISchedullerTask> AllTasks
		{
			get
			{
				return myAllTasks;
			}
			set
			{
				myAllTasks = value;
			}
		}

		/// <summary>
		/// Gets the list of tasks that meet the filter currently in effect.
		/// </summary>
		public IResourceObjectsList<ISchedullerTask> FilteredTasks
		{
			get { return myFilteredTasks; }
		}

		private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
		{
			myTimer.Stop();
			myTimer.Start();
		}

		private void OnTimer(object sender, EventArgs e)
		{
			myTimer.Stop();
			Refilter(SearchText.Text);
		}

		private void Refilter(string text)
		{
			if(text == null)
				throw new ArgumentNullException("text");

			Regex regex = new Regex(text.Length > 0 ? Regex.Escape(text) : ".*");

			TaskList.Items.Clear();

			/*
			foreach(string dataitem in myData)
			{
				if(!regex.IsMatch(dataitem))
					continue;

				TaskList.Items.Add(dataitem);
			}
			 * */
		}
	}
}

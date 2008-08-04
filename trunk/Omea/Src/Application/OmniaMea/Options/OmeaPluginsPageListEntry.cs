using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Media;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// An entry in the plugins list, tracks the enabled state and contains basic info about the plugin.
	/// Queries the plugin for its info in a deferred fashion.
	/// </summary>
	internal abstract class OmeaPluginsPageListEntry : INotifyPropertyChanged
	{
		#region Data

		/// <summary>
		/// The initial value of the Enabled state, before we edit the item.
		/// </summary>
		protected bool _bIsEnabledInitially;

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the item description to display when the item is selected.
		/// </summary>
		public abstract FlowDocument Description { get; }

		/// <summary>
		/// Gets the item icon.
		/// </summary>
		public abstract ImageSource Icon { get; }

		/// <summary>
		/// Gets or sets the current enabled state.
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// The initial value of the Enabled state, before we edit the item.
		/// </summary>
		public bool IsEnabledInitially
		{
			get
			{
				return _bIsEnabledInitially;
			}
		}

		/// <summary>
		/// Gets whether this is a primary plugin (<c>True</c>), or not (<c>False</c>), or that is not applicable, eg on a non-plugin (<c>Null</c>).
		/// </summary>
		public abstract bool? IsPrimary { get; }

		/// <summary>
		/// Gets whether the <see cref="IsEnabled"/> checkbox should be available for the item.
		/// </summary>
		public abstract bool SupportsIsEnabled { get; }

		/// <summary>
		/// Gets the item title.
		/// </summary>
		public abstract string Title { get; }

		#endregion

		#region Implementation

		protected void FirePropertyChanged(string name)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion
	}
}
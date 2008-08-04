using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using JetBrains.Annotations;

namespace JetBrains.UI.Avalon
{
	/// <summary>
	/// Chooses a data template based on the type of the item.
	/// </summary>
	public class DataTemplateSwitchSelector : DataTemplateSelector, IEnumerable<KeyValuePair<Type, DataTemplate>>
	{
		#region Data

		private readonly Dictionary<Type, DataTemplate> _map = new Dictionary<Type, DataTemplate>();

		#endregion

		#region Operations

		/// <summary>
		/// Registers a <paramref name="template">data template</paramref> to be returned for the objects of the given <typeparamref name="TType"/>.
		/// </summary>
		public void Add<TType>([NotNull] DataTemplate template)
		{
			if(template == null)
				throw new ArgumentNullException("template");

			Add(typeof(TType), template);
		}

		/// <summary>
		/// Registers a <paramref name="template">data template</paramref> to be returned for the objects of the given <paramref name="type"/>.
		/// </summary>
		public void Add([NotNull] Type type, [NotNull] DataTemplate template)
		{
			if(type == null)
				throw new ArgumentNullException("type");
			if(template == null)
				throw new ArgumentNullException("template");

			_map.Add(type, template);
		}

		#endregion

		#region Overrides

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if(item == null)
				return null;

			// Take 1: exact type
			DataTemplate template;
			if(_map.TryGetValue(item.GetType(), out template))
				return template;

			// Take 2: base type
			foreach(var pair in _map)
			{
				if(pair.Key.GetType().IsAssignableFrom(item.GetType()))
					return pair.Value;
			}

			return null;
		}

		#endregion

		#region IEnumerable<KeyValuePair<Type,DataTemplate>> Members

		public IEnumerator<KeyValuePair<Type, DataTemplate>> GetEnumerator()
		{
			return _map.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
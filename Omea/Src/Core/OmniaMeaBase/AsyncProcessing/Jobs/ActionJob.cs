// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using System35;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
	/// <summary>
	/// An <see cref="IAsyncProcessor"/> job that executes a single delegate.
	/// </summary>
	public class ActionJob : AbstractNamedJob, IEquatable<ActionJob>
	{
		#region Data

		[NotNull]
		private readonly Action _action;

		[CanBeNull]
		private readonly object _identity;

		[NotNull]
		private readonly string _name;

		#endregion

		#region Init

		public ActionJob([NotNull] string name, [NotNull] Action action)
			: this(name, action, null)
		{
		}

		/// <summary>
		/// Creates the job.
		/// </summary>
		/// <param name="name">Name of the activity. Does not take part in merging the jobs.</param>
		/// <param name="action">The activity to be executed by the job.  Does not take part in merging the jobs.</param>
		/// <param name="identity">An optional identity for the job by which the async jobs will be merged together. If the <paramref name="identity"/> is specified, it's used to jo's equality checks. If it's <c>Null</c>, the job is considered to be unique.</param>
		public ActionJob([NotNull] string name, [NotNull] Action action, [CanBeNull] object identity)
		{
			if(name == null)
				throw new ArgumentNullException("name");
			if(action == null)
				throw new ArgumentNullException("action");

			_name = name;
			_action = action;
			_identity = identity;
		}

		#endregion

		#region Overrides

		public override bool Equals(object obj)
		{
			if(ReferenceEquals(this, obj))
				return true;
			return Equals(obj as ActionJob);
		}

		public override int GetHashCode()
		{
			return _identity != null ? _identity.GetHashCode() : base.GetHashCode();
		}

		///<summary>
		///Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		///</returns>
		///<filterpriority>2</filterpriority>
		public override string ToString()
		{
			return string.Format("Job �{0}�", Name);
		}

		/// <summary>
		/// Override this method in order to perform an one-step job or to do
		/// initialization work for a many-steps job.
		/// </summary>
		protected override void Execute()
		{
			_action();
		}

		/// <summary>
		/// Gets or sets the name of the job.
		/// </summary>
		/// <remarks>The name of the last executing job is displayed in the tooltip for
		/// the async processor status indicator in the status bar.</remarks>
		[NotNull]
		public override string Name
		{
			get
			{
				return _name;
			}
		}

		#endregion

		#region IEquatable<ActionJob> Members

		public bool Equals(ActionJob other)
		{
			if(other == null)
				return false;
			if(_identity == null)
				return ReferenceEquals(this, other);
			return Equals(_identity, other._identity);
		}

		#endregion
	}
}

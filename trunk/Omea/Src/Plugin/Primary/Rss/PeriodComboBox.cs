/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.RSSPlugin
{
	internal class PeriodComboBox : ComboBoxSettingEditor
	{
		public override void SetSetting( Setting setting )
		{
			string[] values     = new string[]{ "minutely", "hourly", "daily", "weekly", "monthly", "yearly" };
			string[] toStrings  = new string[]{ "minutes", "hours", "days", "weeks", "months", "years" };
			SetData( values, toStrings  );
			base.SetSetting( setting );
		}
	}
}
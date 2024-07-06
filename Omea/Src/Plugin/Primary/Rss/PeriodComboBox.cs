// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

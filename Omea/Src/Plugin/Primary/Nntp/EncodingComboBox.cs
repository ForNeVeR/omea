// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;

using JetBrains.Omea.Base;
using JetBrains.Omea.Charsets;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Nntp
{
	internal class EncodingComboBox : ComboBoxSettingEditor
	{
		public void Init( Setting setting )
		{
			string curSystemCharset = null;
			ArrayList arCharsetNames = new ArrayList();
			ArrayList arCharsetDescriptions = new ArrayList();

			foreach( CharsetsEnum.Charset charset in new CharsetsEnum( CharsetFlags.NntpCharset ) )
			{
				arCharsetNames.Add( charset.Name );
				arCharsetDescriptions.Add( charset.Description );
				if(charset.IsDefaultBodyCharset)
					curSystemCharset = charset.Name;
			}

			SetData( arCharsetNames.ToArray(), arCharsetDescriptions.ToArray() );
			SetSetting( setting );

			if ( !setting.Different && SelectedIndex == -1 && setting.Default != null )
			{
				SetValue( setting.Default );
			}
			if ( !setting.Different && SelectedIndex == -1 && curSystemCharset != null )
			{
				SetValue( curSystemCharset );
			}
		}
	}
}

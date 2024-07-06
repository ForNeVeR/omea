// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace GUIControls.CustomViews
{
    #region Decorator
    /// <summary>
    /// RichText node decorator used to corourify the rules and views which caused
    /// errors during their execution and which need reediting.
    /// </summary>
    public class RuleDecorator : IResourceNodeDecorator
    {
        private const String _DecoratorKey = "RuleAndViewDecorator";

        public event ResourceEventHandler DecorationChanged;

        public string DecorationKey { get { return _DecoratorKey; } }

        public bool DecorateNode(IResource res, JetBrains.UI.RichText.RichText nodeText)
        {
            bool need2Decor = res.HasProp(Core.Props.LastError);
            if (need2Decor)
            {
                nodeText.SetStyle(TextStyle.EffectStyle.WeavyUnderline, Color.Red, 0, nodeText.Length);
                nodeText.SetColors(Color.Red, SystemColors.Window, 0, nodeText.Length);
            }
            return need2Decor;
        }

        public void OnErrorRuleChanged(object sender, ResourceIndexEventArgs e)
        {
            if (DecorationChanged != null)
            {
                DecorationChanged(this, new ResourceEventArgs(e.Resource));
            }
        }
    }
    #endregion Decorator
}

// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;

namespace JetBrains.UI.RichText
{
	/// <summary>
	/// Represents style of text block
	/// </summary>
	public struct TextStyle
	{
    /// <summary>
    /// Enumerates different text drawing effects
    /// </summary>
    public enum EffectStyle
    {
      /// <summary>
      /// No effects
      /// </summary>
      None,

      /// <summary>
      /// Underline with straight line
      /// </summary>
      StraightUnderline,

      /// <summary>
      /// Underline with weavy line
      /// </summary>
      WeavyUnderline,

      /// <summary>
      /// Strike out with straight line
      /// </summary>
      StrikeOut
    }

    #region Fields
    /// <summary>
    /// Font style
    /// </summary>
		private FontStyle myFontStyle;

    /// <summary>
    /// Foreground color
    /// </summary>
    private Color myForegroundColor;

    /// <summary>
    /// Background color
    /// </summary>
    private Color myBackgroundColor;

    /// <summary>
    /// Effect to use
    /// </summary>
    private TextStyle.EffectStyle myEffect;

    /// <summary>
    /// Effect color
    /// </summary>
    private Color myEffectColor;
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets font style
    /// </summary>
    public FontStyle FontStyle
    {
      get { return myFontStyle; }
      set { myFontStyle = value; }
    }

    /// <summary>
    /// Gets or sets foreground color
    /// </summary>
    public Color ForegroundColor
    {
      get { return myForegroundColor; }
      set { myForegroundColor = value; }
    }

    /// <summary>
    /// Gets or sets background color
    /// </summary>
    public Color BackgroundColor
    {
      get { return myBackgroundColor; }
      set { myBackgroundColor = value; }
    }

    /// <summary>
    /// Gets or sets used effect
    /// </summary>
    public TextStyle.EffectStyle Effect
    {
      get { return myEffect; }
      set { myEffect = value; }
    }

    /// <summary>
    /// Gets or sets effect color
    /// </summary>
    public Color EffectColor
    {
      get { return myEffectColor; }
      set { myEffectColor = value; }
    }
    #endregion

    #region Statics
    /// <summary>
    /// Default text style
    /// </summary>
    private static TextStyle myDefaultStyle = new TextStyle(FontStyle.Regular, SystemColors.WindowText, SystemColors.Window);

    /// <summary>
    /// Gets default text style
    /// </summary>
    public static TextStyle DefaultStyle
    {
      get { return myDefaultStyle; }
    }
    #endregion

    /// <summary>
    /// Defines a text style.
    /// </summary>
    /// <remarks>
    /// This contructor defines style with no effects.
    /// </remarks>
    /// <param name="fontStyle">The font style to use</param>
    /// <param name="foregroundColor">Foreground color</param>
    /// <param name="backgroundColor">Background clor</param>
    public TextStyle( FontStyle fontStyle, Color foregroundColor, Color backgroundColor )
    {
      myFontStyle = fontStyle;
      myForegroundColor = foregroundColor;
      myBackgroundColor = backgroundColor;
      myEffect = TextStyle.EffectStyle.None;
      myEffectColor = Color.Transparent;
    }

    /// <summary>
    /// Defines a text style.
    /// </summary>
    /// <param name="fontStyle">The font style to use</param>
    /// <param name="foregroundColor">Foreground color</param>
    /// <param name="backgroundColor">Background clor</param>
    /// <param name="effect">Effect to use</param>
    /// <param name="effectColor">Effect color to use</param>
    public TextStyle( FontStyle fontStyle, Color foregroundColor, Color backgroundColor, EffectStyle effect, Color effectColor )
    {
      myFontStyle = fontStyle;
      myForegroundColor = foregroundColor;
      myBackgroundColor = backgroundColor;
      myEffect = effect;
      myEffectColor = effectColor;
    }
  }
}

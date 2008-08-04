using System;
using System.Drawing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using JetBrains.UI.RichText;

using Color=System.Windows.Media.Color;
using FontStyle=System.Drawing.FontStyle;
using Pen=System.Windows.Media.Pen;

namespace JetBrains.UI.Avalon.Interop
{
	/// <summary>
	/// Extension methods for <see cref="RichText"/>.
	/// </summary>
	public static class RichTextEx
	{
		#region Operations

		public static Span ToSpan(this RichText.RichText richtext)
		{
			var span = new Span();
			string plaintext = richtext.Text;
			foreach(RichString part in richtext.GetFormattedParts())
			{
				var run = new Run(plaintext.Substring(part.Offset, part.Length));
				span.Inlines.Add(run);

				if((part.Style.FontStyle & FontStyle.Bold) != 0)
					run.FontWeight = FontWeights.Bold;
				if((part.Style.FontStyle & FontStyle.Italic) != 0)
					run.FontStyle = FontStyles.Italic;
				if((part.Style.FontStyle & FontStyle.Underline) != 0)
					run.TextDecorations.Add(TextDecorations.Underline);
				if((part.Style.FontStyle & FontStyle.Strikeout) != 0)
					run.TextDecorations.Add(TextDecorations.Strikethrough);

				System.Drawing.Color color = part.Style.ForegroundColor;
				if(!color.IsEmpty)
					run.Foreground = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
				color = part.Style.BackgroundColor;
				if(!color.IsEmpty)
					run.Background = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

				color = part.Style.EffectColor;
				Pen pen = null;
				if(!color.IsEmpty)
					pen = new Pen {Brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))};

				switch(part.Style.Effect)
				{
				case TextStyle.EffectStyle.None:
					break;
				case TextStyle.EffectStyle.StraightUnderline:
					run.TextDecorations.Add(new TextDecoration {Location = TextDecorationLocation.Underline, Pen = pen});
					break;
				case TextStyle.EffectStyle.WeavyUnderline:
					run.TextDecorations.Add(new TextDecoration {Location = TextDecorationLocation.Underline, Pen = pen});
					break;
				case TextStyle.EffectStyle.StrikeOut:
					run.TextDecorations.Add(new TextDecoration {Location = TextDecorationLocation.Strikethrough, Pen = pen});
					break;
				default:
					throw new InvalidOperationException("Effect.");
				}
			}
			return span;
		}

		#endregion
	}
}
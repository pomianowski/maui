#nullable enable
using System;
using Microsoft.Maui.Controls.Internals;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using WSize = Windows.Foundation.Size;

namespace Microsoft.Maui.Controls.Platform
{
	internal static class TextBlockExtensions
	{
		public static void UpdateLineBreakMode(this TextBlock textBlock, LineBreakMode lineBreakMode)
		{
			if (textBlock == null)
				return;

			switch (lineBreakMode)
			{
				case LineBreakMode.NoWrap:
					textBlock.TextTrimming = TextTrimming.Clip;
					textBlock.TextWrapping = TextWrapping.NoWrap;
					break;
				case LineBreakMode.WordWrap:
					textBlock.TextTrimming = TextTrimming.None;
					textBlock.TextWrapping = TextWrapping.Wrap;
					break;
				case LineBreakMode.CharacterWrap:
					textBlock.TextTrimming = TextTrimming.WordEllipsis;
					textBlock.TextWrapping = TextWrapping.Wrap;
					break;
				case LineBreakMode.HeadTruncation:
					// TODO: This truncates at the end.
					textBlock.TextTrimming = TextTrimming.WordEllipsis;
					DetermineTruncatedTextWrapping(textBlock);
					break;
				case LineBreakMode.TailTruncation:
					textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
					DetermineTruncatedTextWrapping(textBlock);
					break;
				case LineBreakMode.MiddleTruncation:
					// TODO: This truncates at the end.
					textBlock.TextTrimming = TextTrimming.WordEllipsis;
					DetermineTruncatedTextWrapping(textBlock);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		static void DetermineTruncatedTextWrapping(TextBlock textBlock) =>
			textBlock.TextWrapping = textBlock.MaxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap;

		public static void UpdateText(this TextBlock nativeControl, Label label)
		{
			switch (label.TextType)
			{
				case TextType.Html:
					nativeControl.UpdateTextHtml(label);
					break;

				default:
					if (label.FormattedText != null)
						nativeControl.UpdateInlines(label);
					else
						nativeControl.Text = TextTransformUtilites.GetTransformedText(label.Text, label.TextTransform);
					break;
			}
		}

		public static double FindDefaultLineHeight(this TextBlock control, Inline inline)
		{
			control.Inlines.Add(inline);

			control.Measure(new WSize(double.PositiveInfinity, double.PositiveInfinity));

			var height = control.DesiredSize.Height;

			control.Inlines.Remove(inline);

			return height;
		}
	}
}

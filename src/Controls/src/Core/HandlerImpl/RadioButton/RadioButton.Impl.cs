﻿using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Microsoft.Maui.Controls
{
	/// <include file="../../../../docs/Microsoft.Maui.Controls/RadioButton.xml" path="Type[@FullName='Microsoft.Maui.Controls.RadioButton']/Docs" />
	public partial class RadioButton : IRadioButton
	{
		Font ITextStyle.Font => (Font)GetValue(FontElement.FontProperty);

#if ANDROID
		object IContentView.Content => ContentAsString();
#endif

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			DesiredSize = this.ComputeDesiredSize(widthConstraint, heightConstraint);
			return DesiredSize;
		}

		Size IContentView.CrossPlatformMeasure(double widthConstraint, double heightConstraint)
		{
			return this.MeasureContent(widthConstraint, heightConstraint);
		}

		protected override Size ArrangeOverride(Rectangle bounds)
		{
			Frame = this.ComputeFrame(bounds);
			Handler?.NativeArrange(Frame);
			return Frame.Size;
		}

		Size IContentView.CrossPlatformArrange(Rectangle bounds)
		{
			this.ArrangeContent(bounds);
			return bounds.Size;
		}

		IView IContentView.PresentedContent => ((this as IControlTemplated).TemplateRoot as IView) ?? (Content as IView);
	}
}
﻿#nullable enable
using System;
using Microsoft.Maui.Essentials;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	internal class MockDeviceDisplay : IDeviceDisplay
	{
		DisplayInfo _mainDisplayInfo = new(
			100, 200, 2, DisplayOrientation.Portrait, DisplayRotation.Rotation0);

		public bool KeepScreenOn { get; set; }

		public event EventHandler<DisplayInfoChangedEventArgs>? MainDisplayInfoChanged;

		public DisplayInfo GetMainDisplayInfo() => _mainDisplayInfo;

		public void UpdateMainDisplayInfo(DisplayInfo displayInfo)
		{
			_mainDisplayInfo = displayInfo;
			MainDisplayInfoChanged?.Invoke(this, new DisplayInfoChangedEventArgs(displayInfo));
		}

		public void StartScreenMetricsListeners()
		{
		}

		public void StopScreenMetricsListeners()
		{
		}

		public void SetMainDisplayOrientation(DisplayOrientation portrait)
		{
			var info = new DisplayInfo(
				_mainDisplayInfo.Width,
				_mainDisplayInfo.Height,
				_mainDisplayInfo.Density,
				portrait,
				_mainDisplayInfo.Rotation);

			UpdateMainDisplayInfo(info);
		}
	}
}
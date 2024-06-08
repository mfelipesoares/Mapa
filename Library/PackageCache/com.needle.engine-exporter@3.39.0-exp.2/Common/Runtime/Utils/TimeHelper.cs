using System;

namespace Needle.Engine.Utils
{
	public static class TimeHelper
	{
		public static TimeSpan CalculateTimeRemaining(DateTime processStarted, float total, float current)
		{
			var itemsPerSecond = current / (float)(processStarted - DateTime.Now).TotalSeconds;
			if (itemsPerSecond <= 0) return TimeSpan.FromSeconds(60);
			var secondsRemaining = (total - current) / itemsPerSecond;
			return TimeSpan.FromSeconds(secondsRemaining);
		}

	}
}
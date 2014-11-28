using System;

namespace Bmbsqd.Caching
{
	internal class Clock
	{
		public static long Current()
		{
			return DateTime.UtcNow.Ticks;
		}
	}
}
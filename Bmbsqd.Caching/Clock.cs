using System;

namespace Bmbsqd.Caching;

internal static class Clock
{
	public static long Current() => DateTime.UtcNow.Ticks;
}

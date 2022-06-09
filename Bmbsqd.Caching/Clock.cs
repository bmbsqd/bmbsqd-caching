using System;

namespace Bmbsqd.Caching;

internal class Clock
{
	public static long Current() => DateTime.UtcNow.Ticks;
}
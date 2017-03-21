using System;

namespace Bmbsqd.Caching {
	public static class CacheLog {
		public static void Error( Exception exception, string format, params object[] args ) { }
		public static void Error( Exception exception, string message ) { }
	}
}
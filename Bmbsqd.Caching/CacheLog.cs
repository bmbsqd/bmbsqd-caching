using System;
using JetBrains.Annotations;

namespace Bmbsqd.Caching {
	public static class CacheLog {
		[StringFormatMethod( "format" )]
		public static void Error( Exception exception, string format, params object[] args )
		{ }

		public static void Error( Exception exception, string message )
		{ }
	}
}
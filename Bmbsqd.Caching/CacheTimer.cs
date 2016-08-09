using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bmbsqd.Caching
{
	internal static class CacheTimer
	{
		private static readonly ISet<WeakReference<ICacheExpire>> _caches;
		private static readonly TimeSpan _period = TimeSpan.FromSeconds( 10 );
		private static Timer _timer;

		static CacheTimer()
		{
			_caches = new HashSet<WeakReference<ICacheExpire>>();
		}

		private static void Sweep( object state ) => InvalidateExpiredCacheItems();

		public static void InvalidateExpiredCacheItems()
		{
			var list = GetSnapshot();
			foreach( var r in list ) {
				ICacheExpire cache;
				if( r.TryGetTarget( out cache ) ) {
					cache.InvalidateExpiredItems();
				}
				else {
					Remove( r );
				}
			}
		}

		private static IEnumerable<WeakReference<ICacheExpire>> GetSnapshot()
		{
			if( _caches.Count == 0 )
				return Enumerable.Empty<WeakReference<ICacheExpire>>();

			lock( _caches ) {
				return _caches.ToList();
			}
		}

		private class CacheDisposed : IDisposable
		{
			private readonly WeakReference<ICacheExpire> _reference;

			public CacheDisposed( WeakReference<ICacheExpire> reference )
			{
				_reference = reference;
			}

			public void Dispose() => Remove( _reference );
		}

		private static void Remove( WeakReference<ICacheExpire> reference )
		{
			lock( _caches ) {
				_caches.Remove( reference );
			}
		}

		public static IDisposable Register( ICacheExpire cache )
		{
			InitializeTimer();
			var r = new WeakReference<ICacheExpire>( cache );
			lock( _caches ) {
				_caches.Add( r );
			}
			return new CacheDisposed( r );
		}

		private static void InitializeTimer()
		{
			lock( typeof(CacheTimer) ) {
				if( _timer == null ) {
					_timer = new Timer( Sweep, null, _period, _period );
				}
			}
		}
	}
}

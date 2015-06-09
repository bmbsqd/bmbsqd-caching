using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bmbsqd.Caching
{
	public interface ISingleAsyncCache<T> : IDisposable
	{
		Task<T> GetOrAddAsync( Func<Task<T>> factory );
		void Invalidate();
	}

	public class SingleAsyncCache<T> : 
		CacheBase<T>,
		ICacheExpire, 
		ISingleAsyncCache<T>
	{
		private readonly long _ttl;
		private long _expires;
		private Task<T> _task;
		private readonly IDisposable _cacheTimer;

		public SingleAsyncCache( TimeSpan ttl, bool removeExpired = true )
		{
			_ttl = ttl.Ticks;
			if( removeExpired ) {
				_cacheTimer = CacheTimer.Register( this );
			}
		}

		public Task<T> GetOrAddAsync( Func<Task<T>> factory )
		{
			var t = _task;

			if( t == null ) {
				lock( this ) {
					t = _task;
					if( t == null ) {
						_task = t = factory();
						_expires = Clock.Current() + _ttl;
					}
				}
			}
			else if( IsExpired ) {
				_expires = Clock.Current() + _ttl;
				TryRemove( Interlocked.Exchange( ref _task, factory() ) );
			}

			return t;
		}

		public void Invalidate()
		{
			TryRemove( Interlocked.Exchange( ref _task, null ) );
		}

		private void TryRemove( Task<T> oldItem )
		{
			if( oldItem != null ) {
				NotifyRemoved( oldItem );
			}
		}

		protected virtual void NotifyRemoved( Task<T> item )
		{
			if( item.IsCompleted ) {
				TryDispose( item.Result );
			}
		}

		public void InvalidateExpiredItems()
		{
			if( _task != null && IsExpired ) {
				Invalidate();
			}
		}

		private bool IsExpired => Clock.Current() > _expires;

		public void Dispose()
		{
			Invalidate();
			_cacheTimer?.Dispose();
		}
	}
}

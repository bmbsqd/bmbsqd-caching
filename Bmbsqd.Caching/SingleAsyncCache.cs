using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bmbsqd.Caching;

public interface ISingleAsyncCache<T> : ICache {
	Task<T> GetOrAddAsync( Func<Task<T>> factory );
	void Invalidate();
}

public class SingleAsyncCache<T> : CacheBase<T>,
	ICacheExpire,
	ISingleAsyncCache<T> {
	private readonly TimeSpan _ttl;
	private long _expires;
	private Task<T> _task;
	private readonly IDisposable _cacheTimer;

	public TimeSpan Ttl => _ttl;

	public SingleAsyncCache( TimeSpan ttl, bool removeExpired = true )
	{
		_ttl = ttl;
		if( removeExpired ) {
			_cacheTimer = CacheTimer.Register( this );
		}
	}

	public Task<T> GetOrAddAsync( Func<Task<T>> factory )
	{
		var t = _task;

		if( t is null ) {
			lock( this ) {
				t = _task;
				if( t is null ) {
					_task = t = factory();
					_expires = Clock.Current() + _ttl.Ticks;
				}
			}
		} else if( IsExpired ) {
			_expires = Clock.Current() + _ttl.Ticks;
			NotifyIfRemoved( Interlocked.Exchange( ref _task, factory() ) );
		}

		return t;
	}

	public void Invalidate() => NotifyIfRemoved( Interlocked.Exchange( ref _task, null ) );

	private void NotifyIfRemoved( Task<T> oldItem )
	{
		if( oldItem != null ) {
			NotifyRemoved( oldItem );
		}
	}

	protected virtual void NotifyRemoved( Task<T> item ) => item.ContinueWith( r => TryDispose( r.Result ), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously );

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

	void ICacheInvalidate.InvalidateAll() => Invalidate();
	long ICache.Count => _task is null ? 0 : 1;
}
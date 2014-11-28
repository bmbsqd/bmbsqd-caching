using System;
using System.Threading.Tasks;

namespace Bmbsqd.Caching
{
	public interface IAsyncCache<TKey, TValue> : ICacheInvalidate<TKey>, ICacheUpdate<TKey, TValue>, ICacheUpdate<TKey, Task<TValue>>
	{
		Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory );
	}

	public class AsyncCache<TKey, TValue> : CacheBase<TKey, TValue, AsyncCache<TKey, TValue>.Entry>, IAsyncCache<TKey, TValue>
	{
		public class Entry : EntryBase
		{
			private Func<TKey, Task<TValue>> _factory;
			private Task<TValue> _task;

			public Entry( TKey key, Func<TKey, Task<TValue>> factory, long validUntil )
				: base( key, validUntil )
			{
				_factory = factory;
			}

			public Task<TValue> GetTask()
			{
				if( _factory != null ) {
					lock( this ) {
						if( _factory != null ) {
							_task = _factory( _key );
							_factory = null;
						}
					}
				}
				return _task;
			}

			public bool IsMaterialized
			{
				get { return _task.IsCompleted; }
			}

			public bool TryUpdateValue( Task<TValue> value )
			{
				_task = value;
				return true;
			}

			public override bool TryUpdateValue( TValue value )
			{
				return TryUpdateValue( Task.FromResult( value ) );
			}
		}

		public AsyncCache( TimeSpan ttl )
			: base( ttl )
		{
		}

		public Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory )
		{
			var created = new Entry( key, factory, GetNewExpiration() );
			var entry = _items.GetOrAdd( key, created );
			if( entry == created ) {
				NotifyAdded( key );
			}

			return entry.GetTask();
		}

		public bool TryUpdate( TKey key, Task<TValue> value )
		{
			Entry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}
	}
}
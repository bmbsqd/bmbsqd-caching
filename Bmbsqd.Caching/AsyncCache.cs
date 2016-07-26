using System;
using System.Threading.Tasks;

namespace Bmbsqd.Caching
{
	public interface IAsyncCache<TKey, TValue> : ICache, ICacheInvalidate<TKey>, ICacheUpdate<TKey, TValue>, ICacheUpdate<TKey, Task<TValue>>
	{
		Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory, Func<TKey, Task<TValue>> fastFactory );
		Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory );
		Task<TValue> AddOrUpdateAsync( TKey key, Func<TKey, Task<TValue>> factory );
	}

	public class AsyncCache<TKey, TValue> : CacheBase<TKey, TValue, AsyncCache<TKey, TValue>.Entry>, IAsyncCache<TKey, TValue>
	{
		private readonly bool _returnExpiredItems;

		public class Entry : EntryBase
		{
			private Func<TKey, Task<TValue>> _factory;
			private Func<TKey, Task<TValue>> _fastFactory;
			private Task<TValue> _task;
			private Task<TValue> _fastTask;

			public Entry( TKey key, Func<TKey, Task<TValue>> factory, Func<TKey, Task<TValue>> fastFactory, long validUntil )
				: base( key, validUntil )
			{
				_factory = factory;
				_fastFactory = fastFactory;
			}

			public Task<TValue> GetTask()
			{
				if( _factory != null ) {
					lock( this ) {
						if( _factory != null ) {
							_task = _factory( _key );
							_factory = null;
						}

						if( _fastFactory != null ) {
							_fastTask = _fastFactory( _key );
							_fastFactory = null;
						}
					}
				}

				var ft = _fastTask;
				if( ft == null || _task.IsCompleted ) {
					_fastTask = null;
					return _task;
				}
				return ft;
			}

			public bool IsMaterialized => _task.IsCompleted;

			public bool TryUpdateValue( Task<TValue> value )
			{
				_task = value;
				return true;
			}

			public override bool TryUpdateValue( TValue value )
			{
				return TryUpdateValue( Task.FromResult( value ) );
			}

			public Task<TValue> UnsafeTask => _task;

			public void SetFactory( Func<TKey, Task<TValue>> factory )
			{
				_factory = factory;
			}

			public void UpdateFrom( Entry entry )
			{
				lock( this ) {
					// this order is important
					_fastFactory = entry._fastFactory;
					_factory = entry._factory;
					_validUntil = entry._validUntil;
				}
			}
		}

		public AsyncCache( TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true )
			: base( ttl, removeExpiredItems )
		{
			_returnExpiredItems = returnExpiredItems;
		}

		public Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory ) => GetOrAddAsync( key, factory, null );
		public Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory, Func<TKey, Task<TValue>> fastFactory )
		{
			var created = new Entry( key, factory, fastFactory, GetNewExpiration() );
			var entry = _items.GetOrAdd( key, created );

			var result = entry.GetTask();

			if( ReferenceEquals( entry, created ) ) {
				NotifyAdded( key );
			} else if( entry.IsExpired( Clock.Current() ) ) {
				entry.UpdateFrom( created );
				var createdTask = entry.GetTask();
				if( !_returnExpiredItems || createdTask.IsCompleted ) {
					result = createdTask;
				}
			}

			return result;
		}

		public Task<TValue> AddOrUpdateAsync( TKey key, Func<TKey, Task<TValue>> factory )
		{
			var result = _items.AddOrUpdate( key,
				k => new Entry( k, factory, null, GetNewExpiration() ),
				( k, entry ) => {
					entry.SetFactory( factory );
					entry.UpdateTtl( GetNewExpiration() );
					return entry;
				} );

			return result.GetTask();
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

		protected override void NotifyRemoved( TKey key, Entry entry )
		{
			var task = entry.UnsafeTask;
			if( task != null && task.IsCompleted ) {
				TryDispose( task.Result );
			}
			base.NotifyRemoved( key, entry );
		}
	}
}

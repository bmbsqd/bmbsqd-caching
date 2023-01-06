using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bmbsqd.Caching;

public interface IAsyncCache<TKey, TValue> :
	ICache,
	ICacheInvalidate<TKey>,
	ICacheUpdate<TKey, TValue>,
	ICacheUpdate<TKey, Task<TValue>>
{
	Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> factory);
	Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, Task<TValue>> factory);
	bool TryGetValue(TKey key, out Task<TValue> value);
}

public class AsyncCache<TKey, TValue> : CacheBase<TKey, TValue, AsyncCache<TKey, TValue>.Entry>, IAsyncCache<TKey, TValue>
{
	private readonly bool _returnExpiredItems;

	public class Entry : EntryBase
	{
		private Func<TKey, Task<TValue>> _factory;
		private Task<TValue> _task;

		public Entry(TKey key, Func<TKey, Task<TValue>> factory, long validUntil)
			: base(key, validUntil) {
			_factory = factory;
		}

		public Task<TValue> GetTask() {
			var task = _task;
			if( Volatile.Read(ref _factory) is not null ) {
				lock(this) {
					// Still have to retain the lock here,
					// because the _task needs to be set too, and we're locking for `_task`
					if( Interlocked.Exchange(ref _factory, null) is { } factory ) {
						_task = task = factory(_key);
					}
				}
			}

			return task;
		}

		public Task<TValue> UnsafeGetTask() {
			if( Interlocked.Exchange(ref _factory, null) is { } factory ) {
				return _task = factory(_key);
			}

			return _task;
		}

		private bool IsFaulted => _task is { IsFaulted: true };

		public override bool IsExpired(long time) {
			return IsFaulted || base.IsExpired(time);
		}

		public bool TryUpdateValue(Task<TValue> value) {
			_task = value;
			return true;
		}

		public override bool TryUpdateValue(TValue value) {
			return TryUpdateValue(Task.FromResult(value));
		}

		public Task<TValue> UnsafeTask => _task;

		public void SetFactory(Func<TKey, Task<TValue>> factory) {
			_factory = factory;
		}

		public void UpdateFrom(Entry entry) {
			lock(this) {
				// this order is important
				// when a new `_factory` exists, this will always be used for new tasks
				_factory = entry._factory;
				_validUntil = entry._validUntil;
			}
		}
	}

	public AsyncCache(TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true)
		: base(ttl, removeExpiredItems) {
		_returnExpiredItems = returnExpiredItems;
	}

	public Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> factory) {
		var now = Clock.Current();
		
		if( _items.TryGetValue(key, out var fastEntry) ) {
			// Optimistic read
			if( !fastEntry.IsExpired(now) ) {
				return fastEntry.GetTask();
			}
		}

		var createdEntry = new Entry(key, factory, GetNewExpiration());
		var entry = _items.GetOrAdd(key, createdEntry);
		var result = entry.GetTask();

		if( ReferenceEquals(entry, createdEntry) ) {
			NotifyAdded(key);
		}
		else if( entry.IsExpired(now) ) {
			entry.UpdateFrom(createdEntry);
			var createdTask = entry.GetTask();
			if( !_returnExpiredItems || createdTask.IsCompleted ) {
				result = createdTask;
			}
		}

		return result;
	}

	private readonly record struct FactoryWithExpiration(Func<TKey, Task<TValue>> ValueFactory, long Expiration);

	public Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, Task<TValue>> factory) {
		var result = _items.AddOrUpdate(key,
			static (k, x) => new Entry(k, x.ValueFactory, x.Expiration),
			static (_, entry, x) => {
				entry.SetFactory(x.ValueFactory);
				entry.UpdateTtl(x.Expiration);
				return entry;
			},
			new FactoryWithExpiration(factory, GetNewExpiration())
		);

		return result.GetTask();
	}

	public bool TryGetValue(TKey key, out Task<TValue> value) {
		if( _items.TryGetValue(key, out var entry) ) {
			value = entry.GetTask();
			return true;
		}

		value = null;
		return false;
	}

	public bool TryUpdate(TKey key, Task<TValue> value) {
		if( _items.TryGetValue(key, out var entry) && entry.TryUpdateValue(value) ) {
			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}

	protected override void NotifyRemoved(TKey key, Entry entry) {
		var task = entry.UnsafeTask;
		if( task is { IsCompleted: true, IsFaulted: false, IsCanceled: false } ) {
			TryDispose(task.Result);
		}

		base.NotifyRemoved(key, entry);
	}

	public bool TryUpdate(TKey key, Func<TKey, Task<TValue>, Task<TValue>> updater) {
		if( _items.TryGetValue(key, out var entry) ) {
			lock(entry) {
				var existingTask = entry.UnsafeGetTask();
				// Note, the existing task MUST be obtained before setting the new
				// factory, or this will end up as an un-ending loop.
				entry.SetFactory(k => updater(k, existingTask));
			}

			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}

	public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updater) {
		if( _items.TryGetValue(key, out var entry) ) {
			lock(entry) {
				var existingTask = entry.UnsafeGetTask();
				// Note, the existing task MUST be obtained before setting the new
				// factory, or this will end up as an un-ending loop.
				entry.SetFactory(async k => updater(k, await existingTask));
			}

			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}
}

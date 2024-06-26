using System;
using System.Diagnostics;
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

	public class Entry(TKey key, Func<TKey, Task<TValue>> factory, long validUntil) : EntryBase(key, validUntil)
	{
		private Func<TKey, Task<TValue>>? _factory = factory;
		private Task<TValue>? _task;

		public Task<TValue> GetTask() {
			top:
			var task = _task;
			if( Volatile.Read(ref _factory) is not null ) {
				lock(this) {
					// Re-read the factory. It may have been reset by another thread at this point.
					var factory = Volatile.Read(ref _factory);
					if( factory is null ) {
						// Some other thread owns the factory, and the task is about to be set.
						// There's a good chance `_task` will be set, and so we can just return it.
						// Go back to top and try again.
						goto top;
					}

					_task = task = factory(_key);

					// Be sure to clear the factory after the task is set so other thread can
					// short-circuit to `_task` instead.
					Volatile.Write(ref _factory, null);
				}
			}

			if( task is null ) {
				// This is a bit of a hack, but it's the only way to ensure that the task is always returned
				Thread.SpinWait(1000);
				Thread.Yield();
				goto top;
			}

			return task;
		}

		private bool IsFaulted => _task is { IsFaulted: true };

		public override bool IsExpired(long time) => IsFaulted || base.IsExpired(time);

		public bool TryUpdateValue(Task<TValue> value) {
			_task = value;
			return true;
		}

		public override bool TryUpdateValue(TValue value) => TryUpdateValue(Task.FromResult(value));

		public Task<TValue>? UnsafeTask => _task;

		public void SetFactory(Func<TKey, Task<TValue>> factory) {
			Volatile.Write(ref _factory, factory);
		}

		public void UpdateFrom(Entry entry) {
			lock(this) {
				// this order is important
				// when a new `_factory` exists, this will always be used for new tasks
				Volatile.Write(ref _factory, entry._factory);
				_validUntil = entry._validUntil;
				_task = null;
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
				var existingTask = entry.GetTask();
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
				var existingTask = entry.GetTask();
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

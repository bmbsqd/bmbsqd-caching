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
					if( Volatile.Read(ref _factory) is { } factory ) {
						_task = task = factory(_key);
						Volatile.Write(ref _factory, null);
					}
				}
			}

			return task;
		}

		private bool IsFaulted => _task.IsFaulted;

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
		var created = new Entry(key, factory, GetNewExpiration());
		var entry = _items.GetOrAdd(key, created);

		var result = entry.GetTask();

		if( ReferenceEquals(entry, created) ) {
			NotifyAdded(key);
		}
		else if( entry.IsExpired(Clock.Current()) ) {
			entry.UpdateFrom(created);
			var createdTask = entry.GetTask();
			if( !_returnExpiredItems || createdTask.IsCompleted ) {
				result = createdTask;
			}
		}

		return result;
	}

	public Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, Task<TValue>> factory) {
		var result = _items.AddOrUpdate(key,
			k => new Entry(k, factory, GetNewExpiration()),
			(k, entry) => {
				entry.SetFactory(factory);
				entry.UpdateTtl(GetNewExpiration());
				return entry;
			});

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
			var existingTask = entry.GetTask();
			entry.SetFactory(async k => await updater(k, existingTask));
			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}

	public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updater) {
		if( _items.TryGetValue(key, out var entry) ) {
			var existingTask = entry.GetTask();
			entry.SetFactory(async k => updater(k, await existingTask));
			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}
}

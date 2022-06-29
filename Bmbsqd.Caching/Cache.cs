using System;
using System.Threading;

namespace Bmbsqd.Caching;

public interface ICache<TKey, TValue> : ICacheInvalidate<TKey>, ICacheUpdate<TKey, TValue>
{
	TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);
	TValue AddOrUpdate(TKey key, Func<TKey, TValue> factory);
}

public class Cache<TKey, TValue> : CacheBase<TKey, TValue, Cache<TKey, TValue>.Entry>, ICache<TKey, TValue>
{
	public class Entry : EntryBase
	{
		private Func<TKey, TValue> _factory;
		private TValue _value;

		public Entry(TKey key, Func<TKey, TValue> factory, long validUntil)
			: base(key, validUntil) {
			_factory = factory;
		}

		public TValue GetValue() {
			if( Volatile.Read(ref _factory) is not null ) {
				lock(this) {
					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if( Volatile.Read(ref _factory) is { } factory ) {
						var result = _value = factory(_key);
						Volatile.Write(ref _factory, null);
						return result;
					}
				}
			}

			return _value;
		}

		public override bool TryUpdateValue(TValue value) {
			_value = value;
			return true;
		}

		public void SetFactory(Func<TKey, TValue> factory) {
			_factory = factory;
		}

		public TValue UnsafeValue => _value;
	}

	public Cache(TimeSpan ttl)
		: base(ttl) { }

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) {
		var created = new Entry(key, factory, GetNewExpiration());
		var entry = _items.GetOrAdd(key, created);
		if( ReferenceEquals(entry, created) ) {
			NotifyAdded(key);
		}

		return entry.GetValue();
	}

	public TValue AddOrUpdate(TKey key, Func<TKey, TValue> factory) {
		var result = _items.AddOrUpdate(key,
			k => new Entry(k, factory, GetNewExpiration()),
			(k, entry) => {
				entry.SetFactory(factory);
				entry.UpdateTtl(GetNewExpiration());
				return entry;
			});

		return result.GetValue();
	}

	protected override void NotifyRemoved(TKey key, Entry entry) {
		var value = entry.UnsafeValue;
		TryDispose(value);
		base.NotifyRemoved(key, entry);
	}

	public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updater) {
		if( _items.TryGetValue(key, out var entry) ) {
			var existingValue = entry.GetValue();
			entry.SetFactory(k => updater(k, existingValue));
			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}
}

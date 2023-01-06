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
		var expiration = GetNewExpiration();
		var result = _items.AddOrUpdate(key,
			static (k, f) => new Entry(k, f.factory, f.expiration),
			static (k, entry, f) => {
				entry.SetFactory(f.factory);
				entry.UpdateTtl(f.expiration);
				return entry;
			}, (factory, expiration));

		return result.GetValue();
	}

	protected override void NotifyRemoved(TKey key, Entry entry) {
		var value = entry.UnsafeValue;
		TryDispose(value);
		base.NotifyRemoved(key, entry);
	}

	public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updater) {
		if( _items.TryGetValue(key, out var entry) ) {
			lock(entry) {
				var existingValue = entry.GetValue();
				// Note, the existing value MUST be obtained before setting the new
				// factory, or this will end up as an un-ending loop.
				entry.SetFactory(k => updater(k, existingValue));
			}

			entry.UpdateTtl(GetNewExpiration());
			return true;
		}

		return false;
	}
}

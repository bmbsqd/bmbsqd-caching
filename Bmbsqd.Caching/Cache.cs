using System;

namespace Bmbsqd.Caching
{
	public interface ICache<TKey, TValue> : ICacheInvalidate<TKey>, ICacheUpdate<TKey, TValue>
	{
		TValue GetOrAdd( TKey key, Func<TKey, TValue> factory );
	}

	public class Cache<TKey, TValue> : CacheBase<TKey, TValue, Cache<TKey, TValue>.Entry>, ICache<TKey, TValue>
	{
		public class Entry : EntryBase
		{
			private Func<TKey, TValue> _factory;
			private TValue _value;

			public Entry( TKey key, Func<TKey, TValue> factory, long validUntil )
				: base( key, validUntil )
			{
				_factory = factory;
			}

			public TValue GetValue()
			{
				if( _factory != null ) {
					lock( this ) {
						if( _factory != null ) {
							_value = _factory( _key );
							_factory = null;
						}
					}
				}
				return _value;
			}

			public override bool TryUpdateValue( TValue value )
			{
				_value = value;
				return true;
			}

			public TValue UnsafeValue => _value;
		}

		public Cache( TimeSpan ttl )
			: base( ttl )
		{
		}

		public TValue GetOrAdd( TKey key, Func<TKey, TValue> factory )
		{
			var created = new Entry( key, factory, GetNewExpiration() );
			var entry = _items.GetOrAdd( key, created );
			if( entry == created ) {
				NotifyAdded( key );
			}

			return entry.GetValue();
		}

		protected override void NotifyRemoved( TKey key, Entry entry )
		{
			var value = entry.UnsafeValue;
			TryDispose( value );
			base.NotifyRemoved( key, entry );
		}
	}
}

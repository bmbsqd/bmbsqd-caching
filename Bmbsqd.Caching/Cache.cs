<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
||||||| parent of 047f255... ...
<<<<<<< HEAD
=======
>>>>>>> 047f255... ...
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
<<<<<<< HEAD
||||||| (empty tree)
=======
using System;

namespace Bmbsqd.Caching
{
	public interface ICache<TKey, TValue> : ICacheInvalidate<TKey>, ICacheUpdate<TKey,TValue>
	{
		TValue GetOrAdd( TKey key, Func<TKey, TValue> factory );
	}

	public class Cache<TKey, TValue> : CacheBase<TKey,TValue,Cache<TKey,TValue>.Entry>, ICache<TKey, TValue> 
	{
		public class Entry : EntryBase
		{
			private Func<TKey, TValue> _factory;
			private TValue _value;

			public Entry( TKey key, Func<TKey, TValue> factory, long validUntil ) : base( key, validUntil )
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
		}

		public Cache( TimeSpan ttl ) : base( ttl )
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
	}
}
>>>>>>> 46949d2... initial
||||||| parent of cebcaf3... updated
using System;

namespace Bmbsqd.Caching
{
	public interface ICache<TKey, TValue> : ICacheInvalidate<TKey>, ICacheUpdate<TKey,TValue>
	{
		TValue GetOrAdd( TKey key, Func<TKey, TValue> factory );
	}

	public class Cache<TKey, TValue> : CacheBase<TKey,TValue,Cache<TKey,TValue>.Entry>, ICache<TKey, TValue> 
	{
		public class Entry : EntryBase
		{
			private Func<TKey, TValue> _factory;
			private TValue _value;

			public Entry( TKey key, Func<TKey, TValue> factory, long validUntil ) : base( key, validUntil )
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
		}

		public Cache( TimeSpan ttl ) : base( ttl )
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
	}
}
=======
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

			public TValue UnsafeValue { get { return _value; } }
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
>>>>>>> cebcaf3... updated
||||||| parent of 047f255... ...
||||||| merged common ancestors
=======
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

			public TValue UnsafeValue { get { return _value; } }
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
>>>>>>> a5971016e9d3fbe96ed1d1847e84b5d19c5d1f8a
=======
>>>>>>> 047f255... ...

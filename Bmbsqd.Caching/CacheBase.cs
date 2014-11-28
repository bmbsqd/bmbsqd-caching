<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
﻿using System;
||||||| parent of 047f255... ...
<<<<<<< HEAD
﻿using System;
=======
using System;
>>>>>>> 047f255... ...
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bmbsqd.Caching
{
	public class CacheBase<TKey, TValue, TEntry> :
		ICacheExpire,
		ICacheInvalidate<TKey>
		where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase
	{
		protected readonly ConcurrentDictionary<TKey, TEntry> _items;
		protected readonly TimeSpan _ttl;
		private readonly IDisposable _cacheTimer;

		public abstract class EntryBase
		{
			protected TKey _key;
			protected long _validUntil;

			protected EntryBase( TKey key, long validUntil )
			{
				_key = key;
				_validUntil = validUntil;
			}

			public TKey Key => _key;

			public bool IsExpired( long time )
			{
				return _validUntil < time;
			}

			public void UpdateTtl( long validUntil )
			{
				_validUntil = validUntil;
			}

			public abstract bool TryUpdateValue( TValue value );

		}

		public CacheBase( TimeSpan ttl, bool removeExpiredItems = true )
		{
			_ttl = ttl;
			_items = new ConcurrentDictionary<TKey, TEntry>();
			if( removeExpiredItems ) {
				_cacheTimer = CacheTimer.Register( this );
			}
		}

		public bool Invalidate( TKey key )
		{
			return RemoveByKey( key );
		}

		protected long GetNewExpiration()
		{
			return Clock.Current() + _ttl.Ticks;
		}

		public bool TryUpdate( TKey key, TValue value )
		{
			TEntry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}

		public void InvalidateAll()
		{
			NotifyAllRemoved();
			_items.Clear();
		}

		private bool RemoveByKey( TKey key )
		{
			TEntry entry;
			if( _items.TryRemove( key, out entry ) ) {
				NotifyRemoved( key, entry );
				return true;
			}
			return false;
		}

		private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems()
		{
			var time = Clock.Current();
			return _items.Where( item => item.Value.IsExpired( time ) );
		}

		public void InvalidateExpiredItems()
		{
			foreach( var item in EnumerateExpiredItems() ) {
				TEntry entry;
				if( _items.TryRemove( item.Key, out entry ) ) {
					NotifyRemoved( item.Key, entry );
				}
			}
		}

		protected virtual void NotifyAllRemoved()
		{
			foreach( var item in _items ) {
				NotifyRemoved( item.Key, item.Value );
			}
		}

		protected virtual void NotifyRemoved( TKey key, TEntry entry )
		{

		}

		protected virtual void NotifyAdded( TKey key )
		{
		}

		protected static void TryDispose( object value )
		{
			if( !typeof( TValue ).IsValueType ) {
				var disposable = value as IDisposable;
				try {
					disposable?.Dispose();
				}
				catch( Exception e ) {
					Trace.TraceError( "{1} when disposing {0}: {2}", disposable, e.GetType().Name, e.Message );
				}
			}
		}

		public void Dispose()
		{
			InvalidateAll();
			_cacheTimer?.Dispose();
		}
	}
}
<<<<<<< HEAD
||||||| (empty tree)
=======
﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bmbsqd.Caching
{
	public class CacheBase<TKey, TValue, TEntry> : ICacheExpire where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase
	{
		protected readonly ConcurrentDictionary<TKey, TEntry> _items;
		protected readonly TimeSpan _ttl;

		public abstract class EntryBase
		{
			protected readonly TKey _key;
			protected long _validUntil;

			protected EntryBase( TKey key, long validUntil )
			{
				_key = key;
				_validUntil = validUntil;
			}

			public TKey Key
			{
				get { return _key; }
			}

			public bool IsExpired( long time )
			{
				return _validUntil < time;
			}

			public void UpdateTtl( long validUntil )
			{
				_validUntil = validUntil;
			}

			public abstract bool TryUpdateValue( TValue value );

		}

		public CacheBase( TimeSpan ttl )
		{
			_ttl = ttl;
			_items = new ConcurrentDictionary<TKey, TEntry>();
			CacheTimer.Register( this );
		}

		protected static long Clock()
		{
			return DateTime.UtcNow.Ticks;
		}

		public bool Invalidate( TKey key )
		{
			return RemoveByKey( key );
		}

		protected long GetNewExpiration()
		{
			return Clock() + _ttl.Ticks;
		}

		public bool TryUpdate( TKey key, TValue value )
		{
			TEntry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}

		public void InvalidateAll()
		{
			NotifyAllRemoved();
			_items.Clear();
		}

		private bool RemoveByKey( TKey key )
		{
			TEntry entry;
			if( _items.TryRemove( key, out entry ) ) {
				NotifyRemoved( key, entry );
				return true;
			}
			return false;
		}

		private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems()
		{
			var time = Clock();
			return _items.Where( item => item.Value.IsExpired( time ) );
		}

		public void InvalidateExpiredItems()
		{
			foreach( var item in EnumerateExpiredItems() ) {
				TEntry entry;
				_items.TryRemove( item.Key, out entry );
			}
		}

		protected virtual void NotifyAllRemoved()
		{
			foreach( var item in _items ) {
				NotifyRemoved( item.Key, item.Value );
			}
		}

		protected virtual void NotifyRemoved( TKey key, TEntry entry )
		{

		}

		protected virtual void NotifyAdded( TKey key )
		{
		}

	}
}
>>>>>>> 46949d2... initial
||||||| parent of cebcaf3... updated
﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bmbsqd.Caching
{
	public class CacheBase<TKey, TValue, TEntry> : ICacheExpire where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase
	{
		protected readonly ConcurrentDictionary<TKey, TEntry> _items;
		protected readonly TimeSpan _ttl;

		public abstract class EntryBase
		{
			protected readonly TKey _key;
			protected long _validUntil;

			protected EntryBase( TKey key, long validUntil )
			{
				_key = key;
				_validUntil = validUntil;
			}

			public TKey Key
			{
				get { return _key; }
			}

			public bool IsExpired( long time )
			{
				return _validUntil < time;
			}

			public void UpdateTtl( long validUntil )
			{
				_validUntil = validUntil;
			}

			public abstract bool TryUpdateValue( TValue value );

		}

		public CacheBase( TimeSpan ttl )
		{
			_ttl = ttl;
			_items = new ConcurrentDictionary<TKey, TEntry>();
			CacheTimer.Register( this );
		}

		protected static long Clock()
		{
			return DateTime.UtcNow.Ticks;
		}

		public bool Invalidate( TKey key )
		{
			return RemoveByKey( key );
		}

		protected long GetNewExpiration()
		{
			return Clock() + _ttl.Ticks;
		}

		public bool TryUpdate( TKey key, TValue value )
		{
			TEntry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}

		public void InvalidateAll()
		{
			NotifyAllRemoved();
			_items.Clear();
		}

		private bool RemoveByKey( TKey key )
		{
			TEntry entry;
			if( _items.TryRemove( key, out entry ) ) {
				NotifyRemoved( key, entry );
				return true;
			}
			return false;
		}

		private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems()
		{
			var time = Clock();
			return _items.Where( item => item.Value.IsExpired( time ) );
		}

		public void InvalidateExpiredItems()
		{
			foreach( var item in EnumerateExpiredItems() ) {
				TEntry entry;
				_items.TryRemove( item.Key, out entry );
			}
		}

		protected virtual void NotifyAllRemoved()
		{
			foreach( var item in _items ) {
				NotifyRemoved( item.Key, item.Value );
			}
		}

		protected virtual void NotifyRemoved( TKey key, TEntry entry )
		{

		}

		protected virtual void NotifyAdded( TKey key )
		{
		}

	}
}
=======
﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bmbsqd.Caching
{
	public class CacheBase<TKey, TValue, TEntry> :
		ICacheExpire,
		ICacheInvalidate<TKey>
		where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase
	{
		protected readonly ConcurrentDictionary<TKey, TEntry> _items;
		protected readonly TimeSpan _ttl;
		private readonly IDisposable _cacheTimer;

		public abstract class EntryBase
		{
			protected TKey _key;
			protected long _validUntil;

			protected EntryBase( TKey key, long validUntil )
			{
				_key = key;
				_validUntil = validUntil;
			}

			public TKey Key
			{
				get { return _key; }
			}

			public bool IsExpired( long time )
			{
				return _validUntil < time;
			}

			public void UpdateTtl( long validUntil )
			{
				_validUntil = validUntil;
			}

			public abstract bool TryUpdateValue( TValue value );

		}

		public CacheBase( TimeSpan ttl, bool removeExpiredItems = true )
		{
			_ttl = ttl;
			_items = new ConcurrentDictionary<TKey, TEntry>();
			if( removeExpiredItems ) {
				_cacheTimer = CacheTimer.Register( this );
			}
		}

		public bool Invalidate( TKey key )
		{
			return RemoveByKey( key );
		}

		protected long GetNewExpiration()
		{
			return Clock.Current() + _ttl.Ticks;
		}

		public bool TryUpdate( TKey key, TValue value )
		{
			TEntry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}

		public void InvalidateAll()
		{
			NotifyAllRemoved();
			_items.Clear();
		}

		private bool RemoveByKey( TKey key )
		{
			TEntry entry;
			if( _items.TryRemove( key, out entry ) ) {
				NotifyRemoved( key, entry );
				return true;
			}
			return false;
		}

		private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems()
		{
			var time = Clock.Current();
			return _items.Where( item => item.Value.IsExpired( time ) );
		}

		public void InvalidateExpiredItems()
		{
			foreach( var item in EnumerateExpiredItems() ) {
				TEntry entry;
				if( _items.TryRemove( item.Key, out entry ) ) {
					NotifyRemoved( item.Key, entry );
				}
			}
		}

		protected virtual void NotifyAllRemoved()
		{
			foreach( var item in _items ) {
				NotifyRemoved( item.Key, item.Value );
			}
		}

		protected virtual void NotifyRemoved( TKey key, TEntry entry )
		{

		}

		protected virtual void NotifyAdded( TKey key )
		{
		}

		protected static void TryDispose( object value )
		{
			if( !typeof( TValue ).IsValueType ) {
				var disposable = value as IDisposable;
				try {
					if( disposable != null )
						disposable.Dispose();
				}
				catch( Exception e ) {
					Trace.TraceError( "{1} when disposing {0}: {2}", disposable, e.GetType().Name, e.Message );
				}
			}
		}

		public void Dispose()
		{
			InvalidateAll();
			if( _cacheTimer != null ) {
				_cacheTimer.Dispose();
			}
		}
	}
}
>>>>>>> cebcaf3... updated
||||||| parent of 047f255... ...
||||||| merged common ancestors
=======
﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bmbsqd.Caching
{
	public class CacheBase<TKey, TValue, TEntry> :
		ICacheExpire,
		ICacheInvalidate<TKey>
		where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase
	{
		protected readonly ConcurrentDictionary<TKey, TEntry> _items;
		protected readonly TimeSpan _ttl;
		private readonly IDisposable _cacheTimer;

		public abstract class EntryBase
		{
			protected TKey _key;
			protected long _validUntil;

			protected EntryBase( TKey key, long validUntil )
			{
				_key = key;
				_validUntil = validUntil;
			}

			public TKey Key
			{
				get { return _key; }
			}

			public bool IsExpired( long time )
			{
				return _validUntil < time;
			}

			public void UpdateTtl( long validUntil )
			{
				_validUntil = validUntil;
			}

			public abstract bool TryUpdateValue( TValue value );

		}

		public CacheBase( TimeSpan ttl, bool removeExpiredItems = true )
		{
			_ttl = ttl;
			_items = new ConcurrentDictionary<TKey, TEntry>();
			if( removeExpiredItems ) {
				_cacheTimer = CacheTimer.Register( this );
			}
		}

		public bool Invalidate( TKey key )
		{
			return RemoveByKey( key );
		}

		protected long GetNewExpiration()
		{
			return Clock.Current() + _ttl.Ticks;
		}

		public bool TryUpdate( TKey key, TValue value )
		{
			TEntry entry;
			if( _items.TryGetValue( key, out entry ) ) {
				if( entry.TryUpdateValue( value ) ) {
					entry.UpdateTtl( GetNewExpiration() );
					return true;
				}
			}
			return false;
		}

		public void InvalidateAll()
		{
			NotifyAllRemoved();
			_items.Clear();
		}

		private bool RemoveByKey( TKey key )
		{
			TEntry entry;
			if( _items.TryRemove( key, out entry ) ) {
				NotifyRemoved( key, entry );
				return true;
			}
			return false;
		}

		private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems()
		{
			var time = Clock.Current();
			return _items.Where( item => item.Value.IsExpired( time ) );
		}

		public void InvalidateExpiredItems()
		{
			foreach( var item in EnumerateExpiredItems() ) {
				TEntry entry;
				if( _items.TryRemove( item.Key, out entry ) ) {
					NotifyRemoved( item.Key, entry );
				}
			}
		}

		protected virtual void NotifyAllRemoved()
		{
			foreach( var item in _items ) {
				NotifyRemoved( item.Key, item.Value );
			}
		}

		protected virtual void NotifyRemoved( TKey key, TEntry entry )
		{

		}

		protected virtual void NotifyAdded( TKey key )
		{
		}

		protected static void TryDispose( object value )
		{
			if( !typeof( TValue ).IsValueType ) {
				var disposable = value as IDisposable;
				try {
					if( disposable != null )
						disposable.Dispose();
				}
				catch( Exception e ) {
					Trace.TraceError( "{1} when disposing {0}: {2}", disposable, e.GetType().Name, e.Message );
				}
			}
		}

		public void Dispose()
		{
			InvalidateAll();
			if( _cacheTimer != null ) {
				_cacheTimer.Dispose();
			}
		}
	}
}
>>>>>>> a5971016e9d3fbe96ed1d1847e84b5d19c5d1f8a
=======
>>>>>>> 047f255... ...

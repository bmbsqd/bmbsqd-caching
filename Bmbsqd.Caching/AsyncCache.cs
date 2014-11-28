<<<<<<< HEAD
<<<<<<< HEAD
﻿using System;
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

			public Task<TValue> UnsafeTask
			{
				get { return _task; }
			}

			public void SetFactory( Func<TKey, Task<TValue>> factory )
			{
				_factory = factory;
			}

			public void UpdateFrom( Entry entry )
			{
				_factory = entry._factory;
				_validUntil = entry._validUntil;
			}
		}

		public AsyncCache( TimeSpan ttl, bool removeExpiredItems = true )
			: base( ttl, removeExpiredItems )
		{
		}

		public Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory )
		{
			var created = new Entry( key, factory, GetNewExpiration() );
			var entry = _items.GetOrAdd( key, created );

			var result = entry.GetTask();

			if( entry == created ) {
				NotifyAdded( key );
			}
			else if( entry.IsExpired( Clock.Current() ) ) {
				entry.UpdateFrom( created );
			}

			return result;
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
||||||| (empty tree)
=======
﻿using System;
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
>>>>>>> 46949d2... initial
||||||| parent of cebcaf3... updated
﻿using System;
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
=======
﻿using System;
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

			public Task<TValue> UnsafeTask
			{
				get { return _task; }
			}

			public void SetFactory( Func<TKey, Task<TValue>> factory )
			{
				_factory = factory;
			}

			public void UpdateFrom( Entry entry )
			{
				_factory = entry._factory;
				_validUntil = entry._validUntil;
			}
		}

		public AsyncCache( TimeSpan ttl, bool removeExpiredItems = true )
			: base( ttl, removeExpiredItems )
		{
		}

		public Task<TValue> GetOrAddAsync( TKey key, Func<TKey, Task<TValue>> factory )
		{
			var created = new Entry( key, factory, GetNewExpiration() );
			var entry = _items.GetOrAdd( key, created );

			var result = entry.GetTask();

			if( entry == created ) {
				NotifyAdded( key );
			}
			else if( entry.IsExpired( Clock.Current() ) ) {
				entry.UpdateFrom( created );
			}

			return result;
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
>>>>>>> cebcaf3... updated

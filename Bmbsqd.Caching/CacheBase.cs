using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bmbsqd.Caching;

public abstract class CacheBase<TValue> {
	protected static void TryDispose( TValue value )
	{
		var disposable = value as IDisposable;
		if( disposable != null ) {
			try {
				disposable.Dispose();
			}
			catch( Exception e ) {
				CacheLog.Error( e, "Error disposing {0}", disposable );
			}
		}
	}
}

public abstract class CacheBase<TKey, TValue, TEntry> :
	CacheBase<TValue>,
	ICacheExpire,
	ICacheInvalidate<TKey>
	where TEntry : CacheBase<TKey, TValue, TEntry>.EntryBase {
	protected readonly ConcurrentDictionary<TKey, TEntry> _items;
	protected readonly TimeSpan _ttl;
	private readonly IDisposable _cacheTimer;

	public TimeSpan Ttl => _ttl;
	public long Count => _items.Count;

	public abstract class EntryBase {
		protected readonly TKey _key;
		protected long _validUntil;

		protected EntryBase( TKey key, long validUntil )
		{
			_key = key;
			_validUntil = validUntil;
		}

		public TKey Key => _key;

		public virtual bool IsExpired( long time )
		{
			return _validUntil < time;
		}

		public void UpdateTtl( long validUntil )
		{
			_validUntil = validUntil;
		}

		public abstract bool TryUpdateValue( TValue value );

	}

	protected CacheBase( TimeSpan ttl, bool removeExpiredItems = true )
	{
		_ttl = ttl;
		_items = new ConcurrentDictionary<TKey, TEntry>();
		if( removeExpiredItems ) {
			_cacheTimer = CacheTimer.Register( this );
		}
	}

	public bool Invalidate( TKey key ) => RemoveByKey( key );

	protected long GetNewExpiration() => Clock.Current() + _ttl.Ticks;

	public bool TryUpdate( TKey key, TValue value )
	{
		if( _items.TryGetValue( key, out var entry ) ) {
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
		if( _items.TryRemove( key, out var entry ) ) {
			NotifyRemoved( key, entry );
			return true;
		}
		return false;
	}

	private IEnumerable<KeyValuePair<TKey, TEntry>> EnumerateExpiredItems( long time ) => _items.Where( item => item.Value.IsExpired( time ) );

	public void InvalidateExpiredItems()
	{
		foreach( var item in EnumerateExpiredItems( Clock.Current() ) ) {
			if( _items.TryRemove( item.Key, out var entry ) ) {
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

	protected virtual void NotifyRemoved( TKey key, TEntry entry ) { }
	protected virtual void NotifyAdded( TKey key ) { }

	public void Dispose()
	{
		InvalidateAll();
		_cacheTimer?.Dispose();
	}
}
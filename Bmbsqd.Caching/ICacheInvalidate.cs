using System;

namespace Bmbsqd.Caching;

public interface ICache : IDisposable, ICacheInvalidate {
	TimeSpan Ttl { get; }
	long Count { get; }
}

public interface ICacheInvalidate {
	void InvalidateAll();
}

public interface ICacheInvalidate<in TKey> : ICacheInvalidate {
	bool Invalidate( TKey key );
}

public interface ICacheUpdate<in TKey, in TValue> {
	bool TryUpdate( TKey key, TValue value );
}
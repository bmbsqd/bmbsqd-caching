using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Bmbsqd.Caching;

public interface ICacheRepository : ICacheInvalidate
{
	T Add<T>(string name, T cache) where T : ICache;
	IReadOnlyDictionary<string, ICache> Caches { get; }
	bool TryInvalidateByName(string name);
}

public class CacheRepository : ICacheRepository
{
	private readonly ConcurrentDictionary<string, ICache> _caches;

	public CacheRepository() {
		_caches = new ConcurrentDictionary<string, ICache>();
	}

	public T Add<T>(string name, T cache) where T : ICache {
		_caches[name] = cache;
		return cache;
	}

	public IReadOnlyDictionary<string, ICache> Caches => _caches;

	public void InvalidateAll() {
		foreach( var cache in _caches.Values ) {
			cache.InvalidateAll();
		}
	}

	public bool TryInvalidateByName(string name) {
		if( _caches.TryGetValue(name, out var cache) ) {
			cache.InvalidateAll();
			return true;
		}

		return false;
	}
}
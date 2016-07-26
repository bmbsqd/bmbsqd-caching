using System;

namespace Bmbsqd.Caching {
	public interface ICacheFactory {
		IAsyncCache<TKey, TValue> CreateAsyncCache<TKey, TValue>( string name, TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true );
		ISingleAsyncCache<T> CreateSingleAsyncCache<T>( string name, TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true );
	}

	public class CacheFactory : ICacheFactory {
		private readonly ICacheRepository _cacheRepository;
		public CacheFactory( ICacheRepository cacheRepository)
		{
			if( cacheRepository == null ) throw new ArgumentNullException( nameof( cacheRepository ) );
			_cacheRepository = cacheRepository;
		}


		public IAsyncCache<TKey, TValue> CreateAsyncCache<TKey, TValue>( string name, TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true ) 
			=> _cacheRepository.Add( name, new AsyncCache<TKey, TValue>( ttl, removeExpiredItems, returnExpiredItems ) );

		public ISingleAsyncCache<T> CreateSingleAsyncCache<T>( string name, TimeSpan ttl, bool removeExpiredItems = true, bool returnExpiredItems = true ) 
			=> _cacheRepository.Add( name, new SingleAsyncCache<T>( ttl, removeExpiredItems ) );
	}
}
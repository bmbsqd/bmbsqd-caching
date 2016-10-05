using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bmbsqd.Caching {
	public static class AsyncCacheExtensions {

		private static async Task<KeyValuePair<TKey, TValue>> One<TKey, TValue>( IAsyncCache<TKey, TValue> cache, List<KeyValuePair<TKey, TaskCompletionSource<TValue>>> pending, TKey key )
		{
			var value = await cache.GetOrAddAsync( key, k => {
				var tcs = new TaskCompletionSource<TValue>();
				lock( pending ) {
					pending.Add( new KeyValuePair<TKey, TaskCompletionSource<TValue>>( k, tcs ) );
				}
				return tcs.Task;
			} );
			return new KeyValuePair<TKey, TValue>( key, value );
		}

		public static async Task<IDictionary<TKey, TValue>> GetOrAddAsync<TKey, TValue>( this IAsyncCache<TKey, TValue> cache, IEnumerable<TKey> keys, Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> factory )
		{
			var result = new List<Task<KeyValuePair<TKey, TValue>>>();
			var pending = new List<KeyValuePair<TKey, TaskCompletionSource<TValue>>>();

			foreach( var x in keys ) {
				result.Add( One( cache, pending, x ) );
			}

			if( pending.Any() ) {
				var remaining = await factory( pending.Select( x => x.Key ) );
				foreach( var p in pending ) {
					TValue value;
					if( remaining.TryGetValue( p.Key, out value ) ) {
						p.Value.TrySetResult( value );
					} else {
						p.Value.TrySetCanceled();
					}
				}
			}

			while( true ) {
				// loop until there's no more canceled tasks
				try {
					var results = await Task.WhenAll( result );
					return results.ToDictionary( x => x.Key, x => x.Value );
				}
				catch( TaskCanceledException e ) {
					result.Remove( (Task<KeyValuePair<TKey, TValue>>)e.Task );
				}
			}
		}
	}
}
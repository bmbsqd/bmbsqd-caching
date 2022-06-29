using System;
using System.Threading.Tasks;

namespace Bmbsqd.Caching;

//internal static class AsyncTaskDisposableUtils
//{
//	private static readonly Task _done = Task.FromResult( true );

//	public static Task TryDisposeTask<T>( this Task<T> task )
//	{
//		if( !typeof( T ).IsValueType ) {
//			return TryDisposeTaskInternal( task );
//		}
//		return _done;
//	}

//	private static async Task TryDisposeTaskInternal<T>( Task<T> task )
//	{
//		var value = await task;
//		var asyncDisposable = value as IDisposableAsync;
//		if( asyncDisposable != null ) {
//			await asyncDisposable.DisposeAsync();
//			return;
//		}

//		var disposable = value as IDisposable;
//		if( disposable != null ) {
//			disposable.Dispose();
//		}
//	}
//}
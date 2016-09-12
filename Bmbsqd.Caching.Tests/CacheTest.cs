using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bmbsqd.Caching.Tests {
	public class CacheTest {
		[Fact]
		public void SyncCache()
		{
			var c = new Cache<string, string>( TimeSpan.FromSeconds( 1 ) );

			Assert.Equal( "world", c.GetOrAdd( "hello", k => "world" ) );
			Assert.Equal( "world", c.GetOrAdd( "hello", k => "no-no-no" ) ); // should still be "world"

			Thread.Sleep( TimeSpan.FromSeconds( 1.1 ) );
			c.InvalidateExpiredItems();

			Assert.Equal( "universe", c.GetOrAdd( "hello", k => "universe" ) );
		}

		[Fact]
		public async Task AsyncCache()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 1 ) );

			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ) );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "no-no-no" ) ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			c.InvalidateExpiredItems();

			Assert.Equal( "universe", await c.GetOrAddAsync( "hello", k => Task.FromResult( "universe" ) ) );
		}

		[Fact]
		public async Task ReturnExpiredItems()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 0.1 ), false, true );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ) );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "no-no-no" ) ) ); // should still be "world"
			await Task.Delay( TimeSpan.FromSeconds( 0.5 ) );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ) );
		}

		[Fact]
		public async Task DontReturnExpiredItems()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 0.1 ), returnExpiredItems: false );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ) );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "no-no-no" ) ) ); // should still be "world"
			await Task.Delay( TimeSpan.FromSeconds( 0.5 ) );
			Assert.Equal( "universe", await c.GetOrAddAsync( "hello", k => Task.FromResult( "universe" ) ) );
		}


		[Fact]
		public async Task AsyncCache2()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 100 ), false, true );

			Assert.Equal( "world", await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ) );

			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ) );

			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.Equal( "universe", await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ) );
		}


		[Fact]
		public async Task FastTaskFirstThenSlowTask()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 2000 ), false, true );

			// fast task returns world
			// slow task returns universe
			Assert.Equal( "world", await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			}, k => Task.FromResult( "world" ) ) );


			// Wait for slow task to materialize
			await Task.Delay( 200 );

			// Make sure that the cache slow task is now returning the expected 'universe'
			Assert.Equal( "universe", await c.GetOrAddAsync( "hello", k => Task.FromResult( "this-should-never-be-materialized" ) ) );
		}

		[Fact]
		public async Task NullFastTaskShouldBeIgnored()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 2000 ), false, true );

			Assert.Equal( "universe", await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			}, k => null ) );
		}


		[Fact]
		public async Task SingleAsyncCache()
		{
			var c = new SingleAsyncCache<string>( TimeSpan.FromSeconds( 1 ) );
			Assert.Equal( "world", await c.GetOrAddAsync( () => Task.FromResult( "world" ) ) );
			Assert.Equal( "world", await c.GetOrAddAsync( () => Task.FromResult( "no-no-no" ) ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			CacheTimer.InvalidateExpiredCacheItems();
			//c.InvalidateExpiredItems();

			Assert.Equal( "universe", await c.GetOrAddAsync( () => Task.FromResult( "universe" ) ) );
		}

		public class D : IDisposable {
			public void Dispose()
			{
				IsDisposed = true;
			}

			public bool IsDisposed { get; set; }
		}

		[Fact]
		public async Task IsDisposingDisposables()
		{
			var c = new AsyncCache<string, D>( TimeSpan.FromMinutes( 1 ), true );

			var d = await c.GetOrAddAsync( "abc", k => Task.FromResult( new D() ) );
			Assert.False( d.IsDisposed );
			c.InvalidateAll();
			Assert.True( d.IsDisposed );
		}
	}
}

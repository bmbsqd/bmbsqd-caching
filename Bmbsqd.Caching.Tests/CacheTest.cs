using System;
using System.Collections.Generic;
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


		[Fact]
		public async Task MultiGetOrAdd()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMinutes( 1 ), true );

			var r = await c.GetOrAddAsync( "a", k => Task.FromResult( "foo" ) );
			Assert.Equal( "foo", r );


			var m = await c.GetOrAddAsync( new[] { "a", "b", "c" }, k => Task.FromResult<IDictionary<string, string>>( new Dictionary<string, string> {
				{"a", null },
				{"b", "bar" },
				{"c", "baz" },
			} ) );

			Assert.Equal( 3, m.Count );
			Assert.Contains( "a", m.Keys );
			Assert.Contains( "b", m.Keys );
			Assert.Contains( "c", m.Keys );

			Assert.Contains( "foo", m["a"] );
		}
	}
}

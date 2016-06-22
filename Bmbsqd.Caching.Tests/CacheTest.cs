using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Caching.Tests
{
	[TestFixture]
	public class CacheTest
	{
		[Test]
		public void SyncCache()
		{
			var c = new Cache<string, string>( TimeSpan.FromSeconds( 1 ) );

			Assert.That( c.GetOrAdd( "hello", k => "world" ), Is.EqualTo( "world" ) );
			Assert.That( c.GetOrAdd( "hello", k => "no-no-no" ), Is.EqualTo( "world" ) ); // should still be "world"

			Thread.Sleep( TimeSpan.FromSeconds( 1.1 ) );
			c.InvalidateExpiredItems();

			Assert.That( c.GetOrAdd( "hello", k => "universe" ), Is.EqualTo( "universe" ) );
		}

		[Test]
		public async Task AsyncCache()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 1 ) );

			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult( "no-no-no" ) ), Is.EqualTo( "world" ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			c.InvalidateExpiredItems();

			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult( "universe" ) ), Is.EqualTo( "universe" ) );
		}

		[Test]
		public async Task ReturnExpiredItems()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 0.1 ), false, true );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult("world") ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult("no-no-no") ), Is.EqualTo( "world" ) ); // should still be "world"
			await Task.Delay( TimeSpan.FromSeconds( 0.5 ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ), Is.EqualTo( "world" ) );
		}

		[Test]
		public async Task DontReturnExpiredItems()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromSeconds( 0.1 ), returnExpiredItems: false );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult("world") ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult("no-no-no") ), Is.EqualTo( "world" ) ); // should still be "world"
			await Task.Delay( TimeSpan.FromSeconds( 0.5 ) );
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult("universe") ), Is.EqualTo( "universe" ) );
		}


		[Test]
		public async Task AsyncCache2()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 100 ), false, true );

			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult( "world" ) ), Is.EqualTo( "world" ) );

			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ), Is.EqualTo( "world" ) );

			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			} ), Is.EqualTo( "universe" ) );
		}


		[Test]
		public async Task FastTaskFirstThenSlowTask()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 2000 ), false, true );

			// fast task returns world
			// slow task returns universe
			Assert.That( await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			}, k => Task.FromResult( "world" ) ), Is.EqualTo( "world" ) );


			// Wait for slow task to materialize
			await Task.Delay( 200 );

			// Make sure that the cache slow task is now returning the expected 'universe'
			Assert.That( await c.GetOrAddAsync( "hello", k => Task.FromResult( "this-should-never-be-materialized" ) ), Is.EqualTo( "universe" ) );
		}

		[Test]
		public async Task NullFastTaskShouldBeIgnored()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 2000 ), false, true );

			Assert.That( await c.GetOrAddAsync( "hello", async k => {
				// A completed task is always returned
				// So to prevent that, we have to delay here :-)
				await Task.Delay( 25 );
				return "universe";
			}, k => null ), Is.EqualTo( "universe" ) );
		}


		[Test]
		public async Task SingleAsyncCache()
		{
			var c = new SingleAsyncCache<string>( TimeSpan.FromSeconds( 1 ) );
			Assert.That( await c.GetOrAddAsync( () => Task.FromResult( "world" ) ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( () => Task.FromResult( "no-no-no" ) ), Is.EqualTo( "world" ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			CacheTimer.InvalidateExpiredCacheItems();
			//c.InvalidateExpiredItems();

			Assert.That( await c.GetOrAddAsync( () => Task.FromResult( "universe" ) ), Is.EqualTo( "universe" ) );
		}

		public class D : IDisposable
		{
			public void Dispose()
			{
				IsDisposed = true;
			}

			public bool IsDisposed { get; set; }
		}

		[Test]
		public async Task IsDisposingDisposables()
		{
			var c = new AsyncCache<string, D>( TimeSpan.FromMinutes( 1 ), true );

			var d = await c.GetOrAddAsync( "abc", k => Task.FromResult( new D() ) );
			Assert.That( d.IsDisposed, Is.False );
			c.InvalidateAll();
			Assert.That( d.IsDisposed, Is.True );
		}
	}
}

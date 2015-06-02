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

			Assert.That( await c.GetOrAddAsync( "hello", async k => "world" ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => "no-no-no" ), Is.EqualTo( "world" ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			c.InvalidateExpiredItems();

			Assert.That( await c.GetOrAddAsync( "hello", async k => "universe" ), Is.EqualTo( "universe" ) );
		}


		[Test]
		public async Task AsyncCache2()
		{
			var c = new AsyncCache<string, string>( TimeSpan.FromMilliseconds( 100 ), false );

			Assert.That( await c.GetOrAddAsync( "hello", async k => "world" ), Is.EqualTo( "world" ) );
			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => "universe" ), Is.EqualTo( "world" ) );
			await Task.Delay( TimeSpan.FromMilliseconds( 200 ) );
			Assert.That( await c.GetOrAddAsync( "hello", async k => "universe" ), Is.EqualTo( "universe" ) );
		}

		[Test]
		public async Task SingleAsyncCache()
		{
			var c = new SingleAsyncCache<string>( TimeSpan.FromSeconds( 1 ) );
			Assert.That( await c.GetOrAddAsync( async () => "world" ), Is.EqualTo( "world" ) );
			Assert.That( await c.GetOrAddAsync( async () => "no-no-no" ), Is.EqualTo( "world" ) ); // should still be "world"

			await Task.Delay( TimeSpan.FromSeconds( 1.5 ) );
			CacheTimer.InvalidateExpiredCacheItems();
			//c.InvalidateExpiredItems();

			Assert.That( await c.GetOrAddAsync( async () => "universe" ), Is.EqualTo( "universe" ) );
		}
	}
}

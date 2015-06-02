using System;

namespace Bmbsqd.Caching
{
	public interface ICacheInvalidate : IDisposable
	{
		void InvalidateAll();
	}

	public interface ICacheInvalidate<in TKey> : ICacheInvalidate
	{
		bool Invalidate( TKey key );
	}

	public interface ICacheUpdate<in TKey, in TValue>
	{
		bool TryUpdate( TKey key, TValue value );
	}
}

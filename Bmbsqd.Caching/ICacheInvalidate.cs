<<<<<<< HEAD
<<<<<<< HEAD
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
||||||| (empty tree)
=======
namespace Bmbsqd.Caching
{
	public interface ICacheInvalidate
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
>>>>>>> 46949d2... initial
||||||| parent of cebcaf3... updated
namespace Bmbsqd.Caching
{
	public interface ICacheInvalidate
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
=======
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
>>>>>>> cebcaf3... updated

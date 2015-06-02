namespace Bmbsqd.Caching
{
	internal interface ICacheExpire
	{
		void InvalidateExpiredItems();
	}
}

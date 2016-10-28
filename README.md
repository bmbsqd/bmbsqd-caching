# Caching done right, now with extra Async() #
----------

Use what you want as long as, you know, http://opensource.org/licenses/MIT



Usage Examples:

### 1. Simple

```csharp
var cache = new AsyncCache<string,string>( TimeSpan.FromMinutes(1) );
var item = await cache.GetOrAdd( "something", key => _slowService.SomeAsyncMethod( key ) );
```

### 3. Single Item Cache as a Decorator

```csharp
public class SomeServiceCachingDecorator : ISomeService {
    private readonly ISingleCache<IReadOnlyList<Something>> _cache;
    SomeServiceCachingDecorator( ISomeService inner, ICacheFactory cacheFactory ) {
      _cache = cacheFactory.CreateSingleAsyncCache<IReadOnlyList<Something>>( "some-service-cache", TimeSpan.FromMinutes( 15 ));
    }
  
	public Task<IReadOnlyList<Something>> ListSomethingsAsync() {
      // cached
      return _cache.GetOrAdd( ()=> _inner.ListSomethingsAsync() );
	}
}
```

### 4. Clear all caches in an MVC Controller

```csharp
public class SomeCacheController : Controller{
  private readonly ICacheRepository _cacheRepository;
  public SomeCacheController( ICacheRepository cacheRepository ){
    _cacheRepository = cacheRepository;
  }
  
  [Route("all"), HttpPost]
  public ActionResult ClearAll(){
    _cacheRepository.InvalidateAll();
  }
}
```


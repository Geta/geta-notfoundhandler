using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectCache : IRegexRedirectCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IRegexRedirectLoader _redirectLoader;
    private const string GetAllCacheKey = "RegexRedirects_GetAll";

    public RegexRedirectCache(IMemoryCache memoryCache, IRegexRedirectLoader redirectLoader)
    {
        _memoryCache = memoryCache;
        _redirectLoader = redirectLoader;
    }
    
    public IEnumerable<RegexRedirect> GetOrCreate()
    {
        return _memoryCache.GetOrCreate(GetAllCacheKey,
                                        cacheEntry =>
                                        {
                                            cacheEntry.SlidingExpiration = TimeSpan.FromHours(1);
                                            return _redirectLoader.GetAll();
                                        });
    }

    public void Remove()
    {
        _memoryCache.Remove(GetAllCacheKey);
    }
}

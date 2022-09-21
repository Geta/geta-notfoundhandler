using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class MemoryCacheRegexRedirectRepository : IRepository<RegexRedirect>, IRegexRedirectLoader, IRegexRedirectOrderUpdater
{
    private readonly IRepository<RegexRedirect> _repository;
    private readonly IRegexRedirectLoader _redirectLoader;
    private readonly IRegexRedirectOrderUpdater _orderUpdater;
    private readonly IMemoryCache _cache;

    private const string GetAllCacheKey = "RegexRedirects_GetAll";

    public MemoryCacheRegexRedirectRepository(
        IRepository<RegexRedirect> repository,
        IRegexRedirectLoader redirectLoader,
        IRegexRedirectOrderUpdater orderUpdater,
        IMemoryCache cache)
    {
        _repository = repository;
        _redirectLoader = redirectLoader;
        _orderUpdater = orderUpdater;
        _cache = cache;
    }

    public IEnumerable<RegexRedirect> GetAll()
    {
        return _cache.GetOrCreate(GetAllCacheKey,
                                  cacheEntry =>
                                  {
                                      cacheEntry.SlidingExpiration = TimeSpan.FromHours(1);
                                      return _redirectLoader.GetAll();
                                  });
    }

    public RegexRedirect Get(Guid id)
    {
        return _redirectLoader.Get(id);
    }

    public void Save(RegexRedirect entity)
    {
        _repository.Save(entity);
        _cache.Remove(GetAllCacheKey);
    }

    public void Delete(RegexRedirect entity)
    {
        _repository.Delete(entity);
        _cache.Remove(GetAllCacheKey);
    }

    public void UpdateOrder(bool isIncrease = false)
    {
        _orderUpdater.UpdateOrder(isIncrease);
        _cache.Remove(GetAllCacheKey);
    }
}

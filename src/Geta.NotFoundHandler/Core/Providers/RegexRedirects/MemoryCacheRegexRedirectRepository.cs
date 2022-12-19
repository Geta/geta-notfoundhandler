// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class MemoryCacheRegexRedirectRepository : IRepository<RegexRedirect>, IRegexRedirectLoader, IRegexRedirectOrderUpdater
{
    private readonly IRepository<RegexRedirect> _repository;
    private readonly IRegexRedirectLoader _redirectLoader;
    private readonly IRegexRedirectOrderUpdater _orderUpdater;
    private readonly IRegexRedirectCache _cache;

    public MemoryCacheRegexRedirectRepository(
        IRepository<RegexRedirect> repository,
        IRegexRedirectLoader redirectLoader,
        IRegexRedirectOrderUpdater orderUpdater,
        IRegexRedirectCache cache)
    {
        _repository = repository;
        _redirectLoader = redirectLoader;
        _orderUpdater = orderUpdater;
        _cache = cache;
    }

    public IEnumerable<RegexRedirect> GetAll()
    {
        return _cache.GetOrCreate();
    }

    public RegexRedirect Get(Guid id)
    {
        return _redirectLoader.Get(id);
    }

    public void Save(RegexRedirect entity)
    {
        _repository.Save(entity);
        _cache.Remove();
    }

    public void Delete(RegexRedirect entity)
    {
        _repository.Delete(entity);
        _cache.Remove();
    }

    public void UpdateOrder(bool isIncrease = false)
    {
        _orderUpdater.UpdateOrder(isIncrease);
        _cache.Remove();
    }
}

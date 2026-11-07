// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class DefaultRegexRedirectsService : IRegexRedirectsService
{
    private readonly RegexRedirectFactory _regexRedirectFactory;
    private readonly IRepository<RegexRedirect> _repository;
    private readonly IRegexRedirectLoader _redirectLoader;
    private readonly IRegexRedirectOrderUpdater _orderUpdater;
    private readonly RedirectsEvents _redirectsEvents;

    public DefaultRegexRedirectsService(
        RegexRedirectFactory regexRedirectFactory,
        IRepository<RegexRedirect> repository,
        IRegexRedirectLoader redirectLoader,
        IRegexRedirectOrderUpdater orderUpdater,
        RedirectsEvents redirectsEvents)
    {
        _regexRedirectFactory = regexRedirectFactory;
        _repository = repository;
        _redirectLoader = redirectLoader;
        _orderUpdater = orderUpdater;
        _redirectsEvents = redirectsEvents;
    }

    public void Create(string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        Create(oldUrlRegex, newUrlFormat, orderNumber, null);
    }

    public void Create(string oldUrlRegex, string newUrlFormat, int orderNumber, RedirectType? redirectType)
    {
        var regexRedirect = _regexRedirectFactory.CreateNew(oldUrlRegex, newUrlFormat, orderNumber, redirectType);
        _repository.Save(regexRedirect);
        _orderUpdater.UpdateOrder();
        _redirectsEvents.RegexRedirectsUpdated();
    }

    public void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        var original = _redirectLoader.Get(id);
        Update(original, oldUrlRegex, newUrlFormat, orderNumber, original.RedirectType);
    }

    public void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber, RedirectType? redirectType)
    {
        Update(_redirectLoader.Get(id), oldUrlRegex, newUrlFormat, orderNumber, redirectType);
    }

    private void Update(RegexRedirect original, string oldUrlRegex, string newUrlFormat, int orderNumber, RedirectType? redirectType)
    {
        var isIncrease = original.OrderNumber < orderNumber;
        var regexRedirect = _regexRedirectFactory.Create(original.Id.Value,
                                                         oldUrlRegex,
                                                         newUrlFormat,
                                                         orderNumber,
                                                         redirectType: redirectType);
        _repository.Save(regexRedirect);
        _orderUpdater.UpdateOrder(isIncrease);
        _redirectsEvents.RegexRedirectsUpdated();
    }

    public void Delete(Guid id)
    {
        var regexRedirect = _regexRedirectFactory.CreateForDeletion(id);
        _repository.Delete(regexRedirect);
        _orderUpdater.UpdateOrder();
        _redirectsEvents.RegexRedirectsUpdated();
    }
}

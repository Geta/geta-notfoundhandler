using System;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class DefaultRegexRedirectsService : IRegexRedirectsService
{
    private readonly RegexRedirectFactory _regexRedirectFactory;
    private readonly IRepository<RegexRedirect> _repository;
    private readonly IRegexRedirectOrderUpdater _orderUpdater;

    public DefaultRegexRedirectsService(
        RegexRedirectFactory regexRedirectFactory,
        IRepository<RegexRedirect> repository,
        IRegexRedirectOrderUpdater orderUpdater)
    {
        _regexRedirectFactory = regexRedirectFactory;
        _repository = repository;
        _orderUpdater = orderUpdater;
    }

    public void Create(string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        var regexRedirect = _regexRedirectFactory.CreateNew(oldUrlRegex, newUrlFormat, orderNumber);
        _repository.Save(regexRedirect);
        _orderUpdater.Update();
    }

    public void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        var regexRedirect = _regexRedirectFactory.Create(id, oldUrlRegex, newUrlFormat, orderNumber);
        _repository.Save(regexRedirect);
        _orderUpdater.Update();
    }

    public void Delete(Guid id)
    {
        var regexRedirect = _regexRedirectFactory.CreateForDeletion(id);
        _repository.Delete(regexRedirect);
        _orderUpdater.Update();
    }
}

using System;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public interface IRegexRedirectsService
{
    void Create(string oldUrlRegex, string newUrlFormat, int orderNumber);
    void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber);
    void Delete(Guid id);
}

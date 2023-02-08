using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

public abstract class BaseCustomRedirectPageModel : BaseRedirectPageModel
{
    private readonly RedirectState _redirectState;
    private readonly string _messageFormat;

    protected BaseCustomRedirectPageModel(IRedirectsService redirectsService, RedirectState redirectState, string messageFormat)
    {
        RedirectsService = redirectsService;
        _redirectState = redirectState;
        _messageFormat = messageFormat;
    }

    protected IRedirectsService RedirectsService { get; }

    public CustomRedirectsResult Results { get; set; }

    protected override void Load()
    {
        Params.SortBy ??= nameof(CustomRedirect.OldUrl);
        Params.QueryState = _redirectState;
        Params.PageSize ??= 5;
        var results = RedirectsService.GetRedirects(Params);
        Message = string.Format(_messageFormat, results.UnfilteredCount);
        if (results.TotalCount < results.UnfilteredCount)
        {
            Message += $"Current filter gives {results.TotalCount}.";
        }
        Results = results;
    }
}

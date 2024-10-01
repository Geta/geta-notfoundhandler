using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class SuggestionsModel : BaseRedirectPageModel
{
    private readonly ISuggestionService _suggestionService;

    public SuggestionsModel(ISuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    public SuggestionRedirectsResult Results { get; set; }

    public IList<SuggestionRedirectModel> Items { get; set; } = Enumerable.Empty<SuggestionRedirectModel>().ToList();

    public IActionResult OnPostCreate(Dictionary<int, SuggestionRedirectModel> items, int index)
    {
        ModelState.Clear();
        if (items.ContainsKey(index))
        {
            var item = items[index];
            if (TryValidateModel(item, $"{nameof(items)}[{index}]"))
            {
                _suggestionService.AddRedirect(new SuggestionRedirect(item.OldUrl, item.NewUrl));
                OperationMessage = $"Added redirect from {item.OldUrl} to {item.NewUrl}";
            }
        }

        return LoadPage();
    }

    public IActionResult OnPostIgnore(string oldUrl)
    {
        _suggestionService.IgnoreSuggestion(oldUrl);
        OperationMessage = $"Added {oldUrl} to ignore list";
        return LoadPage(true);
    }

    protected override void Load()
    {
        Params.SortBy ??= nameof(SuggestionRedirectModel.OldUrl);
        Params.PageSize ??= 50;
        var results = _suggestionService.GetSummaries(Params);
        var redirectModels = results.Suggestions.Select(x => new SuggestionRedirectModel
        {
            OldUrl = x.OldUrl,
            Count = x.Count,
            Referers = x.Referers
        });

        Message = $"Based on the logged 404 errors, there are {results.TotalCount} custom redirect suggestions.";
        Results = results;
        Items = redirectModels.ToList();
    }
}

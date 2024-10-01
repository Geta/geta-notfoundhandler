using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CsvHelper;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Infrastructure;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class AdministerModel : PageModel
{
    private readonly IRedirectsService _redirectsService;
    private readonly ISuggestionService _suggestionService;
    private readonly RedirectsXmlParser _redirectsXmlParser;
    private readonly RedirectsCsvParser _redirectsCsvParser;
    private readonly RedirectsTxtParser _redirectsTxtParser;

    public AdministerModel(
        IRedirectsService redirectsService,
        ISuggestionService suggestionService,
        RedirectsXmlParser redirectsXmlParser,
        RedirectsCsvParser redirectsCsvParser,
        RedirectsTxtParser redirectsTxtParser)
    {
        _redirectsService = redirectsService;
        _suggestionService = suggestionService;
        _redirectsXmlParser = redirectsXmlParser;
        _redirectsCsvParser = redirectsCsvParser;
        _redirectsTxtParser = redirectsTxtParser;
    }

    [BindProperty(SupportsGet = true)]
    public string Message { get; set; }

    [BindProperty(SupportsGet = true)]
    public CardType CardType { get; set; }

    [BindProperty]
    public DeleteSuggestionsModel DeleteSuggestions { get; set; } = new DeleteSuggestionsModel();

    [BindProperty]
    public IFormFile ImportFile { get; set; }

    public IActionResult OnPostDeleteAllIgnoredSuggestions()
    {
        var count = _redirectsService.DeleteAllIgnored();

        return BuildResponse($"All {count} ignored suggestions permanently removed", CardType.Success);
    }

    public IActionResult OnPostDeleteAllSuggestions()
    {
        _suggestionService.DeleteAll();

        return BuildResponse("Suggestions successfully deleted", CardType.Success);
    }

    public IActionResult OnPostDeleteAllRedirects()
    {
        _redirectsService.DeleteAll();

        return BuildResponse("Redirects successfully deleted", CardType.Success);
    }

    public IActionResult OnPostDeleteSuggestions()
    {
        if (!ModelState.IsValid) return Page();

        _suggestionService.Delete(DeleteSuggestions.MaxErrors, DeleteSuggestions.MinimumDays);

        return BuildResponse("Suggestions successfully deleted", CardType.Success);
    }

    public IActionResult OnPostImportRedirects()
    {
        if (ImportFile == null)
        {
            return BuildResponse("The uploaded file is not a valid XML or CSV file. Please upload a valid XML/CSV file.",
                                 CardType.Warning);
        }

        if (ImportFile.IsXml())
        {
            return ProcessFile(_redirectsXmlParser);
        }

        if (ImportFile.IsCsv())
        {
            return ProcessFile(_redirectsCsvParser);
        }

        return BuildResponse("The uploaded file is not a valid. Please upload a file.",
                             CardType.Warning);
    }

    public IActionResult OnPostImportDeletedRedirects()
    {
        if (ImportFile == null || !ImportFile.IsTxt())
        {
            return BuildResponse("The uploaded file is not a valid TXT file. Please upload a valid TXT file.", CardType.Warning);
        }

        return ProcessFile(_redirectsTxtParser);
    }

    public IActionResult OnPostExportRedirects(string typeId)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return BuildResponse("Failed to Export", CardType.Warning);
        }

        var redirects = _redirectsService.GetSaved().ToList();

        if (!redirects.Any())
        {
            return BuildResponse("Nothing to Export", CardType.Warning);
        }

        return typeId.ToLower() switch
        {
            "xml" => ExportAsXml(redirects),
            "csv" => ExportAsCsv(redirects),
            _ => BuildResponse("Failed to export. Unsupported export type", CardType.Warning)
        };
    }

    private IActionResult ExportAsXml(List<CustomRedirect> redirects)
    {
        var document = _redirectsXmlParser.Export(redirects);

        using var memoryStream = new MemoryStream();
        using var writer = new XmlTextWriter(memoryStream, Encoding.UTF8) { Formatting = Formatting.Indented };
        document.WriteTo(writer);
        writer.Flush();
        memoryStream.Seek(0, SeekOrigin.Begin);

        return File(memoryStream.ToArray(), "text/xml", "customRedirects.xml");
    }

    private IActionResult ExportAsCsv(List<CustomRedirect> redirects)
    {
        var documents = _redirectsCsvParser.Export(redirects);

        using var memoryStream = new MemoryStream();
        using (var tw = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(tw, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(documents);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return File(memoryStream.ToArray(), "text/csv", "customRedirects.csv");
    }

    private IActionResult BuildResponse(string message, CardType cardType)
    {
        Message = message;
        CardType = cardType;

        return RedirectToPage(new { Message, CardType });
    }

    private IActionResult ProcessFile(IRedirectsParser parser)
    {
        var redirects = parser.LoadFromStream(ImportFile.OpenReadStream());

        if (redirects.Any())
        {
            _redirectsService.AddOrUpdate(redirects);

            return BuildResponse($"{redirects.Count()} urls successfully imported.", CardType.Success);
        }

        return BuildResponse("No redirects could be imported", CardType.Warning);
    }
}

public class DeleteSuggestionsModel
{
    public int MaxErrors { get; set; } = 5;
    public int MinimumDays { get; set; } = 30;
}

using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Infrastructure;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin
{
    public class AdministerModel : PageModel
    {
        private readonly IRedirectsService _redirectsService;
        private readonly ISuggestionService _suggestionService;
        private readonly RedirectsXmlParser _redirectsXmlParser;

        public AdministerModel(
            IRedirectsService redirectsService,
            ISuggestionService suggestionService,
            RedirectsXmlParser redirectsXmlParser)
        {
            _redirectsService = redirectsService;
            _suggestionService = suggestionService;
            _redirectsXmlParser = redirectsXmlParser;
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
            Message = $"All {count} ignored suggestions permanently removed";
            CardType = CardType.Success;

            return RedirectToPage(new {
                Message,
                CardType
            });
        }

        public IActionResult OnPostDeleteAllSuggestions()
        {
            _suggestionService.DeleteAll();
            Message = "Suggestions successfully deleted";
            CardType = CardType.Success;

            return RedirectToPage(new
            {
                Message,
                CardType
            });
        }

        public IActionResult OnPostDeleteAllRedirects()
        {
            _redirectsService.DeleteAll();
            Message = "Redirects successfully deleted";
            CardType = CardType.Success;

            return RedirectToPage(new
            {
                Message,
                CardType
            });
        }

        public IActionResult OnPostDeleteSuggestions()
        {
            if (!ModelState.IsValid) return Page();

            _suggestionService.Delete(DeleteSuggestions.MaxErrors, DeleteSuggestions.MinimumDays);
            Message = "Suggestions successfully deleted";
            CardType = CardType.Success;

            return RedirectToPage(new
            {
                Message,
                CardType
            });
        }

        public IActionResult OnPostImportRedirects()
        {
            if (ImportFile == null || !ImportFile.IsXml())
            {
                Message = "The uploaded file is not a valid XML file. Please upload a valid XML file.";
                CardType = CardType.Warning;

                return RedirectToPage(new
                {
                    Message,
                    CardType
                });
            }

            var redirects = _redirectsXmlParser.LoadFromStream(ImportFile.OpenReadStream());

            if (redirects.Any())
            {
                _redirectsService.AddOrUpdate(redirects);
                Message = $"{redirects.Count()} urls successfully imported.";
                CardType = CardType.Success;
            }
            else
            {
                Message = "No redirects could be imported";
                CardType = CardType.Warning;
            }

            return RedirectToPage(new
            {
                Message,
                CardType
            });
        }

        public IActionResult OnPostImportDeletedRedirects()
        {
            if (ImportFile == null || !ImportFile.IsTxt())
            {
                Message = "The uploaded file is not a valid TXT file. Please upload a valid TXT file.";
                CardType = CardType.Warning;

                return RedirectToPage(new
                {
                    Message,
                    CardType
                });
            }

            var redirects = ReadDeletedRedirectsFromImportFile();

            if (redirects.Any())
            {
                _redirectsService.AddOrUpdate(redirects);
                Message = $"{redirects.Count()} urls successfully imported.";
                CardType = CardType.Success;
            }
            else
            {
                Message = "No redirects could be imported";
                CardType = CardType.Warning;
            }

            return RedirectToPage(new
            {
                Message,
                CardType
            });
        }

        public IActionResult OnPostExportRedirects()
        {
            var redirects = _redirectsService.GetSaved().ToList();
            var document = _redirectsXmlParser.Export(redirects);

            var memoryStream = new MemoryStream();
            var writer = new XmlTextWriter(memoryStream, Encoding.UTF8)
            {
                Formatting = Formatting.Indented
            };
            document.WriteTo(writer);
            writer.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);

            return File(memoryStream, "text/xml", "customRedirects.xml");
        }

        private CustomRedirectCollection ReadDeletedRedirectsFromImportFile()
        {
            var redirects = new CustomRedirectCollection();
            using var streamReader = new StreamReader(ImportFile.OpenReadStream());
            while (streamReader.Peek() >= 0)
            {
                var url = streamReader.ReadLine();
                if (!string.IsNullOrEmpty(url))
                {
                    redirects.Add(new CustomRedirect
                    {
                        OldUrl = url,
                        NewUrl = string.Empty,
                        State = (int)RedirectState.Deleted
                    });
                }
            }

            return redirects;
        }
    }

    public class DeleteSuggestionsModel
    {
        public int MaxErrors { get; set; } = 5;
        public int MinimumDays { get; set; } = 30;
    }
}

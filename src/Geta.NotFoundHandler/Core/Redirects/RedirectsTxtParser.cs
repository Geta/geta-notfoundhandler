using System.IO;

namespace Geta.NotFoundHandler.Core.Redirects;

public class RedirectsTxtParser : IRedirectsParser
{
    public CustomRedirectCollection LoadFromStream(Stream txtContent)
    {
        var redirects = new CustomRedirectCollection();
        using var streamReader = new StreamReader(txtContent);
        while (streamReader.Peek() >= 0)
        {
            var url = streamReader.ReadLine();
            if (!string.IsNullOrEmpty(url))
            {
                redirects.Add(new CustomRedirect { OldUrl = url, NewUrl = string.Empty, State = (int)RedirectState.Deleted });
            }
        }

        return redirects;
    }
}

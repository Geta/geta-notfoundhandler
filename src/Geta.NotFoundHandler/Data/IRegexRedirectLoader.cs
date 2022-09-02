using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Data;

public interface IRegexRedirectLoader
{
    IEnumerable<RegexRedirect> GetAll();
}

public class RegexRedirect
{
    public Regex OldUrlRegex { get; set; }
    public string NewUrlFormat { get; set; }
}

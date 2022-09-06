using System;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;

public class RegexRedirectModel
{
    public Guid? Id { get; set; }
    public string OldUrlRegex { get; set; }
    public string NewUrlFormat { get; set; }
    public int OrderNumber { get; set; }
}

namespace Geta.NotFoundHandler.Models;

using CsvHelper.Configuration.Attributes;

public class CsvImportModel
{
    [Name("OldUrl")]
    public string OldUrl { get; set; }
    [Name("NewUrl")]
    public string NewUrl { get; set; }
    [Name("WildcardSkippedAppend")]
    public string WildcardSkippedAppend { get; set; }
    [Name("RedirectType")]
    public string RedirectType { get; set; }
}

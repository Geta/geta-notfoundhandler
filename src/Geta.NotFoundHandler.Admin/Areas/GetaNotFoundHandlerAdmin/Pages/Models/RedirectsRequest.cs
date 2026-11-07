using System;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages;

public class RedirectsRequest
{
    public string Query { get; set; }
    public int? PageNumber { get; set; }
    public Guid? Id { get; set; }
    public string SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }
}

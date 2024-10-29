using System;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages;

public class RedirectsRequest
{
    public string Query { get; set; }
    public int? PageNumber { get; set; }
    public Guid Id { get; set; }
}

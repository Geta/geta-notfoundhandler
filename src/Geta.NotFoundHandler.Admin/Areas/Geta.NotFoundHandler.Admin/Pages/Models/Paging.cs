using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class Paging
    {
        public const int DefaultPageSize = 50;

        [FromQuery(Name = "page")]
        public int PageNumber { get; set; } = 1;

        [FromQuery(Name = "page-size")]
        public int PageSize { get; set; } = DefaultPageSize;
    }
}

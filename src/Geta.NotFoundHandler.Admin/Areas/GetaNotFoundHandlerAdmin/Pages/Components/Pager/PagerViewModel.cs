namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Pager
{
    public class PagerViewModel
    {
        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }

        public int PageNumber { get; set; }

        public int PageCount { get; set; }

        public string QueryString { get; set; }
    }
}

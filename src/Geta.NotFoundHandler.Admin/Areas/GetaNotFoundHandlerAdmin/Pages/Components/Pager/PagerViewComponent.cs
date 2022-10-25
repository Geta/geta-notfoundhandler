using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Pager
{
    public class PagerViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public PagerViewComponent(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public IViewComponentResult Invoke(IPagedList items)
        {
            var context = _contextAccessor.HttpContext;
            return View(new PagerViewModel
            {
                HasPreviousPage = items.HasPreviousPage,
                HasNextPage = items.HasNextPage,
                PageNumber = items.PageNumber,
                PageCount = items.PageCount,
                QueryString = context.Request.QueryString.ToString()
            });
        }
    }
}

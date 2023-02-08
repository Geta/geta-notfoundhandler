using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Pager
{
    public class PagerViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public PagerViewComponent(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public IViewComponentResult Invoke(int page, int pageSize, int totalCount)
        {
            var context = _contextAccessor.HttpContext;
            var pageCount = pageSize is int ps && ps > 0 ? (int)Math.Ceiling((decimal)totalCount / ps) : 1;
            return View(new PagerViewModel
            {
                HasPreviousPage = page > 1,
                HasNextPage = pageCount > page,
                PageNumber = page,
                PageCount = pageCount,
                QueryString = context.Request.QueryString.ToString()
            });
        }
    }
}

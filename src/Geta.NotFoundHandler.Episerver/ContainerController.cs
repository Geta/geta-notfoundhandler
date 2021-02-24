using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Episerver
{
    public class ContainerController : Controller
    {
        [Authorize(Policy = Infrastructure.Constants.PolicyName)]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

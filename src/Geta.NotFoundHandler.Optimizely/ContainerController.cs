using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Optimizely
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

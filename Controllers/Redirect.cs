using Microsoft.AspNetCore.Mvc;

namespace MediaSync.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RedirectController : Controller
    {
        [Route("/")]
        [Route("/docs")]
        [Route("/swagger")]
        public IActionResult Index()
        {
            return new RedirectResult("/swagger");
        }
    }    
}
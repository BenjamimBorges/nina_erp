using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "healthy" });
    }
}

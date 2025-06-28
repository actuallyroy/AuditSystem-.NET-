using Microsoft.AspNetCore.Mvc;

namespace AuditSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
} 
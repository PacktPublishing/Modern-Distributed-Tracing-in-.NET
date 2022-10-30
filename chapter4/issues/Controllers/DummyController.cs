using Microsoft.AspNetCore.Mvc;

namespace issues.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DummyController : ControllerBase
    {
        [HttpGet]
        public async Task Dummy([FromQuery] int delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay);
        }
    }
}

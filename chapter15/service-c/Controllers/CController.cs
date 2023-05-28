using Microsoft.AspNetCore.Mvc;

namespace c.Controllers;

[ApiController]
[Route("[controller]")]
public class CController : ControllerBase
{
    
    public CController()
    {
    }

    [HttpGet]
    public string Get()
    {
        return "hello from service c";
    }
}

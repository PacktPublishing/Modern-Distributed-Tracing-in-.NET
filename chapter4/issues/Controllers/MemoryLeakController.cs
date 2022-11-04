using Microsoft.AspNetCore.Mvc;

namespace issues.Controllers
{
    public readonly record struct User(string name, string email);

    [ApiController]
    [Route("[controller]")]
    public class MemoryLeakController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ProcessingQueue _queue;

        public MemoryLeakController(ILogger<MemoryLeakController> logger, ProcessingQueue queue)
        {
            _logger = logger;
            _queue = queue;
        }

        [HttpGet]
        public string LeakMemory()
        {
            _queue.Enqueue(() => _logger.LogInformation("notification for {user}", 
                new User("Foo", "leak@memory.net")));

            return "all done";
        }
    }
}
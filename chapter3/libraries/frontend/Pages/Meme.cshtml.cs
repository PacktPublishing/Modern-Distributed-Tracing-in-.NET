using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

namespace frontend.Pages
{
    public class Meme : PageModel
    {
        private readonly StorageService _storage;
        public Meme(StorageService storage)
        {
            _storage = storage;
        }

        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? ImageBase64 { get; set; }

        [BindProperty]
        public CancellationToken cancellationToken { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Name = Request.Query["name"];

            try 
            {
                using var stream = await _storage.ReadAsync(Name, cancellationToken);
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy, cancellationToken);
                copy.Position = 0;
                ImageBase64 = Convert.ToBase64String(copy.ToArray());
                return new PageResult();
            } 
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
        }
    }
}

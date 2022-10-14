using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace frontend.Pages
{
    public class Upload : PageModel
    {
        private readonly StorageService _storage;
        public Upload(StorageService storage)
        {
            _storage = storage;
        }

        [BindProperty]
        public CancellationToken cancellationToken {get; set;}

        [BindProperty]
        public IFormFile MemeFile { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (Path.GetExtension(MemeFile.FileName).ToLowerInvariant() != ".png" || MemeFile.Length > 1024 * 1024)
            {
                throw new ArgumentException("Unsupported format, only PNG images up to 1MB are supported");
            }

            string name = Path.GetFileNameWithoutExtension(MemeFile.FileName);
            using var stream = MemeFile.OpenReadStream();
            await _storage.WriteAsync(name, stream, cancellationToken);

            return new RedirectToPageResult("/meme", new { name = name });
        }
    }
}

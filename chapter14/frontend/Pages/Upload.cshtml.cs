using Memes.OpenTelemetry.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenTelemetry;
using System.Diagnostics;

namespace frontend.Pages;

public class Upload : PageModel
{
    private readonly StorageService _storage;
    public Upload(StorageService storage)
    {
        _storage = storage;
    }

    [BindProperty]
    public CancellationToken CancellationToken {get; set;}

    [BindProperty]
    public IFormFile MemeFile { get; set; } = default!;

    public async Task<IActionResult> OnPostAsync()
    {
        string name = Path.GetFileNameWithoutExtension(MemeFile.FileName);

        Baggage.SetBaggage(SemanticConventions.MemeNameKey, name);
        Activity.Current?.AddMemeName(name);

        if (MemeFile.Length > 1024 * 1024) throw new ArgumentException("Image is too big, images up to 1 MB are supported");

        using var stream = MemeFile.OpenReadStream();
        await _storage.WriteAsync(name, stream, CancellationToken);

        return new RedirectToPageResult("/meme", new { name });
    }
}

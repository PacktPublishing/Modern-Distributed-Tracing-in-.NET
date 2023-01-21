using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenTelemetry;
using System.Diagnostics;
using System.Xml.Linq;

namespace frontend.Pages;

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
        string name = Path.GetFileNameWithoutExtension(MemeFile.FileName);

        var currentActivity = Activity.Current;
        if (currentActivity?.IsAllDataRequested == true)
        {
            currentActivity.SetTag("meme_name", name);
            currentActivity.SetTag("meme_extension", Path.GetExtension(MemeFile.FileName.AsSpan()).Slice(1).ToString());
        }
        Baggage.SetBaggage("meme_name", name);

        if (MemeFile.Length > 1024 * 1024) throw new ArgumentException("Image is too big, images up to 1MB are supported");

        using var stream = MemeFile.OpenReadStream();
        await _storage.WriteAsync(name, stream, cancellationToken);

        return new RedirectToPageResult("/meme", new { name });
    }
}

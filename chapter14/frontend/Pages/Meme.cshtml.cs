using Memes.OpenTelemetry.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenTelemetry;
using System.Diagnostics;
using System.Net;

namespace frontend.Pages;

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
    public CancellationToken CancellationToken { get; set; }

    public async Task<IActionResult> OnGet([FromQuery] string name)
    {
        Activity.Current?.AddMemeName(name);
        Baggage.SetBaggage(SemanticConventions.MemeNameKey, name);

        Name = name;
        try 
        {
            using var stream = await _storage.ReadAsync(Name, CancellationToken);
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, CancellationToken);
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

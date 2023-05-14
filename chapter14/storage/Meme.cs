using Microsoft.EntityFrameworkCore;

namespace storage;

[Index(nameof(Name), IsUnique = true, Name = "Name")]
public class Meme
{
    public Meme(string name, byte[] data)
    {
        Name = name;
        Data = data;
    }

    public string Name { get; set; }
    public byte[] Data { get; set; }
}
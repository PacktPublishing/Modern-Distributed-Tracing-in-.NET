using System.Text.Json.Serialization;

namespace consumer;

public class Message
{
    public Dictionary<string, string> Headers { get; set; } = new ();

    public string? Text { get; set; }
}

using System.Text.Json;

namespace producer;

public class Message
{
    public Message()
    { 
    }

    public Message(object payload)
    {
        Text = JsonSerializer.Serialize(payload);
    }

    public Dictionary<string, string> Headers { get; set; } = new ();

    public string? Text { get; set; }
}

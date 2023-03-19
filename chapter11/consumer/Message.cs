namespace producer;

public class Message
{
    public Dictionary<string, string> Headers { get; set; } = new ();

    public string? Text { get; set; }
}

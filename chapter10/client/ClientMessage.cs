namespace client;

public record ClientMessage(string text, IDictionary<string, string>? attributes);

public static class ClientMessageExtensions
{
    public static Message ToGrpcMessage(this ClientMessage clientMessage)
    {
        var grpcMessage = new Message()
        {
            Text = clientMessage.text
        };
        if (clientMessage.attributes != null)
        {
            foreach (var kvp in clientMessage.attributes)
            {
                grpcMessage.Attributes.Add(kvp.Key, kvp.Value);
            }
        }

        return grpcMessage;
    }
}
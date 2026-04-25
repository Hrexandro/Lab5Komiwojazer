using System.Text.Json;

namespace Shared.Communication;

public static class JsonLineWriter
{
    private static readonly object LockObject = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Write(object message)
    {
        lock (LockObject)
        {
            Console.WriteLine(JsonSerializer.Serialize(message, Options));
        }
    }
}
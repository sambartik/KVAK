using KVAK.Client;

namespace TestingPlayground;

internal static class Program
{
    static void Main(string[] args)
    {
        Run().GetAwaiter().GetResult();
    }
    
    private static async Task Run()
    {
        var client = new KVAKClient("127.0.0.1", "TEST_API_KEY", 3000);
        await client.Connect().ConfigureAwait(false);
        Console.WriteLine("Connected!");

        Console.WriteLine("Adding a kv data...");
        await client.Add("my-key", "my-value").ConfigureAwait(false);
        
        Console.WriteLine("Finding a kv data...");
        var response = await client.Find("my-key").ConfigureAwait(false);
        Console.WriteLine($"..found: {response}");

        Console.WriteLine("Deleting a kv data...");
        await client.Remove("my-key").ConfigureAwait(false);
        Console.WriteLine($"Querying for the deleted kv data: {await client.Find("my-key").ConfigureAwait(false)}");
    }
}
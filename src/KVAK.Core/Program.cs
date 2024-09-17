using KVAK.Core.Store;
using KVAK.Networking.Protocol.KVAKProtocol;

namespace KVAK.Core;

struct Configuration
{
    public readonly string ApiKey;
    public readonly int Port;
    public readonly int A;
    public readonly int B;

    public Configuration(string apiKey, int port, int a, int b)
    {
        ApiKey = apiKey;
        Port = port;
        A = a;
        B = b;
    }
}

internal static class Program
{
    public static void Main(string[] args)
    {
        Run().GetAwaiter().GetResult();
        Console.WriteLine("Server did \"KVAK\" one last time.");
    }

    private static Configuration GetEnvironmentalConfiguration()
    {
        string? apiKey = Environment.GetEnvironmentVariable("KVAK_API_KEY");
        int a = int.Parse(Environment.GetEnvironmentVariable("KVAK_A") ?? "2");
        int b = int.Parse(System.Environment.GetEnvironmentVariable("A") ?? "3");
        int port = int.Parse(Environment.GetEnvironmentVariable("KVAK_PORT") ?? "3000");

        if (apiKey == null)
        {
            throw new InvalidOperationException("KVAK_API_KEY environment variable is not set.");
        }
        if (port < 0 || port > 65535)
        {
            throw new InvalidOperationException("KVAK_PORT environment variable is invalid.");
        }

        if (a < 2 || b < (2 * a - 1))
        {
            throw new InvalidOperationException("KVAK_A and KVAK_B environment variable combination is invalid.");
        }
        
        return new Configuration(apiKey, port, a, b);
    }
    
    private async static Task Run()
    {
        try
        {
            var env = GetEnvironmentalConfiguration();
            var abTree = new AbTree(env.A, env.B);
            var concurrentStore = new ConcurrentStore(abTree);
            var listener = new KVAKTcpListener();
            var server = new KVAKServer(listener, env.ApiKey, concurrentStore, env.Port);

            await server.Run().ConfigureAwait(false);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"There was an error with environmental configuration: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error has just occured: {e.Message}");
        }
    }
}
using KVAK.Client;

namespace KVAK.Interactive;

class Program
{
    private static KVAKClient? _client = null;

    static void PrintHelp()
    {
        Console.WriteLine("KVAK Interactive: Help menu\n");

        Console.WriteLine("0. HELP");
        Console.WriteLine("   Displays this help message.\n");

        Console.WriteLine("1. EXIT");
        Console.WriteLine("   Exits the interactive mode.\n");

        Console.WriteLine("2. CONNECT [IP Address] [Port] [API Key]");
        Console.WriteLine("   Connects to the KVAK Server using the provided IP address, port, and API key.");
        Console.WriteLine("   Ensure the server is turned on before attempting to connect.\n");

        Console.WriteLine("3. ADD [Key] [Value]");
        Console.WriteLine("   Adds a key-value pair to the server.");
        Console.WriteLine("   Requires authentication.\n");

        Console.WriteLine("4. FIND [Key]");
        Console.WriteLine("   Finds and returns the value associated with the given key.");
        Console.WriteLine("   Requires authentication.\n");

        Console.WriteLine("5. REMOVE [Key]");
        Console.WriteLine("   Removes the specified key-value pair from the store.");
        Console.WriteLine("   Requires authentication.\n");
    }
    
    static async Task ConnectCommand(string ip, string portStr, string apiKey)
    {
        if (_client == null)
        {
            int port;
            try
            {
                port = Convert.ToInt32(portStr);
            }
            catch (Exception)
            {
                Console.WriteLine("There has been an error parsing the port");
                return;
            }

            try
            {
                _client = new KVAKClient(ip, apiKey, port);
                await _client.Connect().ConfigureAwait(false);
                Console.WriteLine("Connected!");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"There has been an error while connecting: {e.Message}");
                _client = null;
                return;
            }
        }
        else
        {
            Console.WriteLine("Already connected to a server, sorry.");
        }
    }
    
    static async Task FindCommand(string key)
    {
        try
        {
            dynamic? result = await _client!.Find(key).ConfigureAwait(false);
            if (result == null)
            {
                Console.WriteLine("The key was not found in the database.");
            }
            else
            {
                Console.WriteLine($"{key}={result}");
            }
        }
        catch (Exception e)
        {
              Console.WriteLine($"An error occured while finding the key: {e.Message}");
        }
    }
    
    static async Task RemoveCommand(string key)
    {
        try
        { 
            await _client!.Remove(key).ConfigureAwait(false);
            Console.WriteLine("The key was removed from the database.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occured while removing the key: {e.Message}");
        }
    }
    
    static async Task AddCommand(string key, string value)
    {
        try
        { 
            await _client!.Add(key, value).ConfigureAwait(false);
            Console.WriteLine("The key was added to the database.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occured while adding the key: {e.Message}");
        }
    }
    
    private static async Task Run()
    {
        Console.WriteLine("Welcome to the KVAK Interactive Mode! Type 'help' to see available commands. Type 'exit' to exit.");
        
        _client = null;
        string line;
        while (((line = Console.ReadLine() ?? "").ToLower()) != "exit" && line != "")
        {
            var explodedLine = line.Split(" ").ToList();
            
            // Unauthenticated commands:
            if (explodedLine[0].ToLower() == "help")
            {
                PrintHelp();
                continue;
            } else if (explodedLine[0].ToLower() != "connect" && _client == null)
            {
                // Make sure we are connected & authenticated if user asks anything different than to connect
                Console.WriteLine("Please connect to a server.");
                continue;
            } else if (explodedLine[0].ToLower() == "connect")
            {
                // Connect and authenticate if user asks to
                if (explodedLine.Count < 4)
                {
                    Console.WriteLine("Invalid number of arguments.");
                    continue;
                }
                
                await ConnectCommand(explodedLine[1], explodedLine[2], explodedLine[3]).ConfigureAwait(false);
                continue;
            }
            
            // Authenticated commands:
            switch (explodedLine[0].ToLower())
            {
                case "find":
                    if (explodedLine.Count >= 2)
                    {
                        await FindCommand(explodedLine[1]).ConfigureAwait(false);
                    }
                    else
                    {
                        Console.WriteLine("Not enough arguments.");
                    }
                    break;
                case "add":
                    if (explodedLine.Count >= 3)
                    {
                        await AddCommand(explodedLine[1], explodedLine[2]).ConfigureAwait(false);
                    }
                    else
                    {
                        Console.WriteLine("Not enough arguments.");
                    }
                    break;
                case "remove":
                    if (explodedLine.Count >= 2)
                    {
                        await RemoveCommand(explodedLine[1]).ConfigureAwait(false);
                    }
                    else
                    {
                        Console.WriteLine("Not enough arguments.");
                    }
                    break;
                default:
                    Console.WriteLine("Please enter a valid command");
                    continue;
            }
        }
        
        Console.WriteLine("KVAK. Goodbye.");
    }
    
    static void Main(string[] args)
    {
        Run().GetAwaiter().GetResult();
    }
}
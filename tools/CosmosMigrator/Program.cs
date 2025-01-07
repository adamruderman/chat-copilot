// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Container = Microsoft.Azure.Cosmos.Container;

namespace CosmosMigrator;

internal sealed class Program
{
    private static CosmosClient? _cosmosClient;
    private static Container? _chatSessionContainer;
    private static Container? _chatMessageContainer;

    private static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the CosmosDB Migration Tool!");

        // Load configuration
        IConfiguration configuration = LoadConfiguration();
        InitializeCosmosClient(configuration);

        bool running = true;
        while (running)
        {
            Console.WriteLine("\nMain Menu:");
            Console.WriteLine("1. Update Chat Session Titles");
            Console.WriteLine("2. Exit");
            Console.Write("Choose an option: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("Updating Chat Session Titles...");
                    await UpdateChatSessionTitlesAsync();
                    break;
                case "2":
                    Console.WriteLine("Exiting the application. Goodbye!");
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }

    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Load placeholder settings
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true); // Override with Development settings if present

        var config = builder.Build();


        return config;
    }



    private static void InitializeCosmosClient(IConfiguration configuration)
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string connectionString = configuration["CosmosDB:ConnectionString"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string databaseName = configuration["CosmosDB:DatabaseName"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string chatSessionContainerName = configuration["CosmosDB:ChatSessionContainerName"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string chatMessageContainerName = configuration["CosmosDB:ChatMessageContainerName"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.


        CosmosClientOptions options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway
        };
        _cosmosClient = new CosmosClient(connectionString, options);
        _chatSessionContainer = _cosmosClient.GetContainer(databaseName, chatSessionContainerName);
        _chatMessageContainer = _cosmosClient.GetContainer(databaseName, chatMessageContainerName);

        Console.WriteLine("CosmosDB client initialized.");
    }

    private static async Task UpdateChatSessionTitlesAsync()
    {
        if (_chatSessionContainer == null || _chatMessageContainer == null)
        {
            Console.WriteLine("CosmosDB containers are not initialized.");
            return;
        }

        var query = new QueryDefinition("SELECT * FROM c WHERE STARTSWITH(c.title, 'Copilot @')");
        using var iterator = _chatSessionContainer.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var session in response)
            {
                string sessionId = session.id;
                string originalTitle = session.title;

                // Fetch the first message for this session
                var messageQuery = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.chatId = @chatId and c.authorRole=0 ORDER BY c._ts ASC")
                    .WithParameter("@chatId", sessionId);

                var messageIterator = _chatMessageContainer.GetItemQueryIterator<dynamic>(messageQuery);

                if (messageIterator.HasMoreResults)
                {
                    var messageResponse = await messageIterator.ReadNextAsync();
                    var firstMessage = messageResponse.FirstOrDefault();

                    if (firstMessage != null)
                    {
                        // Update the title with the first message content
                        session.title = firstMessage.content;

                        // Upsert the updated session back to the container
                        await _chatSessionContainer.UpsertItemAsync(session);
                        Console.WriteLine($"Updated ChatSession {sessionId}: Title changed from '{originalTitle}' to '{firstMessage.content}'");
                    }
                }
            }
        }

        Console.WriteLine("Chat session title updates complete.");
    }
}

// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat sessions.
/// </summary>
public class ChatSessionRepository : Repository<ChatSession>
{
    /// <summary>
    /// Initializes a new instance of the ChatSessionRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatSessionRepository(IStorageContext<ChatSession> storageContext)
        : base(storageContext)
    {
    }

    /// <summary>
    /// Retrieves all chat sessions.
    /// </summary>
    /// <returns>A list of ChatSessions.</returns>
    public Task<IEnumerable<ChatSession>> GetAllChatsAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }

    /// <summary>
    /// Retrieves a chat session by its ID with the partition key.
    /// </summary>
    /// <param name="id">The ID of the chat session.</param>
    /// <param name="partitionKey">The partition key.</param>
    /// <returns>The chat session, or null if not found.</returns>
    public async Task<ChatSession?> GetByIdAsync(string id, string partitionKey)
    {
        try
        {
            var chatSession = await base.FindByIdAsync(id, partitionKey);
            return chatSession;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves a chat session by its ID without the partition key (for backward compatibility).
    /// </summary>
    /// <param name="id">The ID of the chat session.</param>
    /// <returns>The chat session, or null if not found.</returns>
    public async Task<ChatSession?> GetByIdAsync(string id)
    {
        var results = await base.StorageContext.QueryEntitiesAsync(e => e.Id == id);
        return results.SingleOrDefault();
    }
}

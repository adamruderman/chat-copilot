// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat messages.
/// </summary>
public class ChatMessageRepository : CopilotChatMessageRepository
{
    /// <summary>
    /// Initializes a new instance of the ChatMessageRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatMessageRepository(ICopilotChatMessageStorageContext storageContext)
        : base(storageContext)
    {
    }

    /// <summary>
    /// Finds chat messages by chat id with continuation token support.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <param name="count">The number of messages to return. Default is 10.</param>
    /// <param name="continuationToken">The continuation token for paging. Default is null.</param>
    /// <returns>
    /// A tuple containing a list of chat messages and the next continuation token.
    /// If the continuation token is null, there are no more messages.
    /// </returns>
    public async Task<(IEnumerable<CopilotChatMessage>, string)> FindByChatIdAsync(
        string chatId,
        int count = 10,
        string? continuationToken = null)
    {
        // Use the base class method to query with continuation token
        return await base.QueryEntitiesWithContinuationAsync(
            e => e.ChatId == chatId, // Predicate to match chatId
            chatId,                 // PartitionKey
            count,                  // Number of records to fetch
            continuationToken       // Continuation token for paging
        );
    }

    /// <summary>
    /// Finds all chat messages by chat id, fetching all pages of data.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <returns>A list of all ChatMessages matching the given chatId sorted from most recent to oldest.</returns>
    public async Task<IEnumerable<CopilotChatMessage>> FindByChatIdHistoryAsync(string chatId, int limit = -1)
    {
        var allMessages = new List<CopilotChatMessage>();
        string? continuationToken = null;

        do
        {
            var (messages, nextContinuationToken) = await FindByChatIdAsync(chatId, count: limit > 0 ? limit - allMessages.Count : 10, continuationToken);

            allMessages.AddRange(messages);
            continuationToken = nextContinuationToken;

            // Stop if limit is reached
            if (limit > 0 && allMessages.Count >= limit)
            {
                break;
            }
        } while (!string.IsNullOrEmpty(continuationToken));

        return allMessages;
    }
}

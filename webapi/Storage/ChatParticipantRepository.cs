﻿// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat sessions.
/// </summary>
public class ChatParticipantRepository : CopilotParticipantsRepository
{
    /// <summary>
    /// Initializes a new instance of the ChatParticipantRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public ChatParticipantRepository(IChatParticipantStorageContext storageContext)
        : base(storageContext)
    {
    }

    /// <summary>
    /// Finds chat participants by user id.
    /// A user can be part of multiple chats, thus a user can have multiple chat participants.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A list of chat participants of the same user id in different chat sessions.</returns>
    public Task<IEnumerable<ChatParticipant>> FindByUserIdAsync(string userId)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId);
    }

    public async Task<(IEnumerable<ChatParticipant>, string)> FindByChatIdWithContinuationAsync(
        string userid,
        int count = 10,
        string? continuationToken = null)
    {
        return await base.QueryEntitiesWithContinuationAsync(
            predicate: e => e.UserId == userid,
            partitionKey: userid, // userid is the partition key
            count: count,
            continuationToken: continuationToken
        );
    }

    /// <summary>
    /// Finds chat participants by chat id.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <returns>A list of chat participants in the same chat sessions.</returns>
    public Task<IEnumerable<ChatParticipant>> FindByChatIdAsync(string chatId)
    {
        return base.StorageContext.QueryEntitiesAsync(e => e.ChatId == chatId);
    }

    /// <summary>
    /// Checks if a user is in a chat session.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="chatId">The chat id.</param>
    /// <returns>True if the user is in the chat session, false otherwise.</returns>
    public async Task<bool> IsUserInChatAsync(string userId, string chatId)
    {
        var users = await base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId && e.ChatId == chatId);
        return users.Any();
    }
}

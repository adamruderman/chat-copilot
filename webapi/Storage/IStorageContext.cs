// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// Defines the basic CRUD operations for a storage context.
/// </summary>
public interface IStorageContext<T> where T : IStorageEntity
{
    /// <summary>
    /// Query entities in the storage context.
    /// <param name="predicate">Predicate that needs to evaluate to true for a particular entryto be returned.</param>
    /// </summary>
    Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate);

    /// <summary>
    /// Read an entity from the storage context by id.
    /// </summary>
    /// <param name="entityId">The entity id.</param>
    /// <param name="partitionKey">The entity partition</param>
    /// <returns>The entity.</returns>
    Task<T> ReadAsync(string entityId, string partitionKey);

    /// <summary>
    /// Create an entity in the storage context.
    /// </summary>
    /// <param name="entity">The entity to be created in the context.</param>
    Task CreateAsync(T entity);

    /// <summary>
    /// Upsert an entity in the storage context.
    /// </summary>
    /// <param name="entity">The entity to be upserted in the context.</param>
    Task UpsertAsync(T entity);

    /// <summary>
    /// Delete an entity from the storage context.
    /// </summary>
    /// <param name="entity">The entity to be deleted from the context.</param>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Query entities in the storage context with a partition key.
    /// <param name="predicate">Predicate that needs to evaluate to true for a particular entry to be returned.</param>
    /// <param name="partitionKey">The partition key for scoping the query.</param>
    /// <param name="orderBy">Optional function to order the entities.</param>
    /// <param name="isDescending">Whether to order entities in descending order.</param>
    /// </summary>
    Task<IEnumerable<T>> QueryEntitiesAsync(
            Func<T, bool> predicate,
            string partitionKey,
            Func<T, object>? orderBy = null,
            bool isDescending = false
        );
    // New method for paginated queries with continuation tokens
    Task<(IEnumerable<T>, string)> QueryEntitiesWithContinuationAsync(
        Func<T, bool> predicate,
        string? partitionKey = null,
        int count = 10,
        string? continuationToken = null);
}

/// <summary>
/// Specialization of IStorageContext<T> for CopilotChatMessage.
/// </summary>
public interface ICopilotChatMessageStorageContext : IStorageContext<CopilotChatMessage>
{
}

public interface IChatParticipantStorageContext : IStorageContext<ChatParticipant>
{
    public interface IChatParticipantStorageContext : IStorageContext<ChatParticipant>
    {
        new Task<(IEnumerable<ChatParticipant>, string)> QueryEntitiesWithContinuationAsync(
            Func<ChatParticipant, bool> predicate,
            string? partitionKey = null,
            int count = 10,
            string? continuationToken = null);
    }
}

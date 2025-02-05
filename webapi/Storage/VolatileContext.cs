// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using CopilotChat.WebApi.Models.Storage;
using System.Globalization;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in memory.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class VolatileContext<T> : IStorageContext<T> where T : IStorageEntity
{
    /// <summary>
    /// Using a concurrent dictionary to store entities in memory.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly ConcurrentDictionary<string, T> Entities;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the VolatileContext class.
    /// </summary>
    public VolatileContext()
    {
        this.Entities = new ConcurrentDictionary<string, T>();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return Task.FromResult(this.Entities.Values.Where(predicate));
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate, string partitionKey)
    {
        // Filter the entities based on the partitionKey and the predicate
        var filteredEntities = this.Entities.Values
            .Where(entity => entity.Partition == partitionKey && predicate(entity));

        return Task.FromResult(filteredEntities);
    }

    /// <inheritdoc/>
    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        this.Entities.TryAdd(entity.Id, entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        this.Entities.TryRemove(entity.Id, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        if (this.Entities.TryGetValue(entityId, out T? entity))
        {
            return Task.FromResult(entity);
        }

        throw new KeyNotFoundException($"Entity with id {entityId} not found.");
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        this.Entities.AddOrUpdate(entity.Id, entity, (key, oldValue) => entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> CountEntitiesAsync(string partitionKey, Func<T, bool>? predicate = null)
    {
        // Filter entities by partitionKey
        var filteredEntities = this.Entities.Values
            .Where(entity => entity.Partition == partitionKey);

        // Apply predicate if provided
        if (predicate != null)
        {
            filteredEntities = filteredEntities.Where(predicate);
        }

        return Task.FromResult(filteredEntities.Count());
    }

    private string GetDebuggerDisplay()
    {
        return this.ToString() ?? string.Empty;
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(
        Func<T, bool> predicate,
        int skip = 0,
        int count = -1,
        Func<T, object>? orderBy = null,
        bool isDescending = false)
    {
        var query = this.Entities.Values.Where(predicate);

        // Apply ordering if provided
        if (orderBy != null)
        {
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        // Apply pagination
        if (skip > 0 || count > 0)
        {
            query = query.Skip(skip).Take(count);
        }

        return Task.FromResult(query);
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(
        Func<T, bool> predicate,
        string partitionKey,
        Func<T, object>? orderBy = null,
        bool isDescending = false)
    {
        var query = this.Entities.Values
            .Where(entity => entity.Partition == partitionKey && predicate(entity));

        // Apply ordering if provided
        if (orderBy != null)
        {
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        return Task.FromResult(query);
    }

    public Task<(IEnumerable<T>, string)> QueryEntitiesWithContinuationAsync(
    Func<T, bool> predicate,
    string? partitionKey = null,
    int count = 10,
    string? continuationToken = null)
    {
        var filtered = this.Entities.Values.Where(predicate);
        if (partitionKey != null)
        {
            filtered = filtered.Where(e => e.Partition == partitionKey);
        }
        var pagedResults = filtered.Skip(int.Parse(continuationToken ?? "0", CultureInfo.InvariantCulture)).Take(count);
        var nextToken = (int.Parse(continuationToken ?? "0", CultureInfo.InvariantCulture) + count).ToString(CultureInfo.InvariantCulture);

        return Task.FromResult((pagedResults, nextToken));
    }
}
/// <summary>
/// Specialization of VolatileContext<T> for CopilotChatMessage.
/// </summary>
public class VolatileCopilotChatMessageContext : VolatileContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    /// <inheritdoc/>
    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip, int count)
    {
        return Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this.Entities.Values
                .Where(predicate).OrderByDescending(m => m.Timestamp).Skip(skip).Take(count));
    }
    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, string partitionKey, int skip, int count)
    {
        var filteredEntities = this.Entities.Values
            .Where(m => m.Partition == partitionKey && predicate(m))
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(count);

        return Task.FromResult(filteredEntities);
    }
}
/// <summary>
/// Specialization of VolatileContext<T> for CopilotChatMessage.
/// </summary>
public class VolatileCopilotParticipantContext : VolatileContext<ChatParticipant>, IChatParticipantStorageContext
{
}

﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using CopilotChat.WebApi.Models.Storage;

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
    /// Initializes a new instance of the InMemoryContext class.
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

    private string GetDebuggerDisplay()
    {
        return this.ToString() ?? string.Empty;
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
    /// <inheritdoc/>
    public Task<IEnumerable<ChatParticipant>> QueryEntitiesAsync(Func<ChatParticipant, bool> predicate, int skip = 0, int count = -1, Func<ChatParticipant, object> orderBy = null, bool isDescending = false)
    {
        var filteredEntities = this.Entities.Values
            .Where(predicate)
            .Skip(skip)
            .Take(count);

        return Task.FromResult(filteredEntities);
    }

    public Task<IEnumerable<ChatParticipant>> QueryEntitiesAsync(Func<ChatParticipant, bool> predicate, string partitionKey, int skip = 0, int count = -1, Func<ChatParticipant, object> orderBy = null, bool isDescending = false)
    {
        var filteredEntities = this.Entities.Values
            .Where(m => m.Partition == partitionKey && predicate(m))
            .Skip(skip)
            .Take(count);

        return Task.FromResult(filteredEntities);
    }
}

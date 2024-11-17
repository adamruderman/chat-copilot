﻿// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using CopilotChat.WebApi.Models.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in a CosmosDB container.
/// </summary>
public class CosmosDbContext<T> : IStorageContext<T>, IDisposable where T : IStorageEntity
{
    /// <summary>
    /// The CosmosDB client.
    /// </summary>
    private readonly CosmosClient _client;

    /// <summary>
    /// CosmosDB container.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly Container Container;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the CosmosDbContext class.
    /// </summary>
    /// <param name="connectionString">The CosmosDB connection string.</param>
    /// <param name="database">The CosmosDB database name.</param>
    /// <param name="container">The CosmosDB container name.</param>
    public CosmosDbContext(string connectionString, string database, string container)
    {
        // Configure JsonSerializerOptions
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
        };
        this._client = new CosmosClient(connectionString, options);
        this.Container = this._client.GetContainer(database, container);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return await Task.Run<IEnumerable<T>>(
            () => this.Container.GetItemLinqQueryable<T>(true).Where(predicate).AsEnumerable());
    }

    /// <inheritdoc/>
    public async Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        await this.Container.CreateItemAsync(entity, new PartitionKey(entity.Partition));
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        await this.Container.DeleteItemAsync<T>(entity.Id, new PartitionKey(entity.Partition));
    }

    /// <inheritdoc/>
    public async Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        try
        {
            var response = await this.Container.ReadItemAsync<T>(entityId, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Entity with id {entityId} not found.");
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        await this.Container.UpsertItemAsync(entity, new PartitionKey(entity.Partition));
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._client.Dispose();
        }
    }
}

/// <summary>
/// Specialization of CosmosDbContext<T> for CopilotChatMessage.
/// </summary>
public class CosmosDbCopilotChatMessageContext : CosmosDbContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    /// <summary>
    /// Initializes a new instance of the CosmosDbCopilotChatMessageContext class.
    /// </summary>
    /// <param name="connectionString">The CosmosDB connection string.</param>
    /// <param name="database">The CosmosDB database name.</param>
    /// <param name="container">The CosmosDB container name.</param>
    public CosmosDbCopilotChatMessageContext(string connectionString, string database, string container) :
        base(connectionString, database, container)
    {
    }

    /// <inheritdoc/>
    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip, int count)
    {
        return Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this.Container.GetItemLinqQueryable<CopilotChatMessage>(true)
                .Where(predicate).OrderByDescending(m => m.Timestamp).Skip(skip).Take(count).AsEnumerable());
    }

    public Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, string partitionKey, int skip, int count)
    {
        return Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this.Container.GetItemLinqQueryable<CopilotChatMessage>(
                    true,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) })
                .Where(predicate)
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(count)
                .AsEnumerable());
    }
}

public class CosmosDbChatParticipantContext : CosmosDbContext<ChatParticipant>, IChatParticipantStorageContext
{
    public CosmosDbChatParticipantContext(string connectionString, string database, string container)
        : base(connectionString, database, container)
    {
    }

    public Task<IEnumerable<ChatParticipant>> QueryEntitiesAsync(
     Func<ChatParticipant, bool> predicate,
     string partitionKey,
     int skip,
     int count,
     Func<ChatParticipant, object> orderBy = null,
     bool isDescending = false)
    {
        return Task.Run(() =>
        {
            // Get the queryable collection from Cosmos DB
            var query = this.Container.GetItemLinqQueryable<ChatParticipant>(
                true,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) })
                .Where(predicate);

            // Apply ordering if provided
            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }

            // Apply pagination
            query = query.Skip(skip).Take(count);

            return query.AsEnumerable();
        });
    }

    public Task<IEnumerable<ChatParticipant>> QueryEntitiesAsync(Func<ChatParticipant, bool> predicate, int skip = 0, int count = -1, Func<ChatParticipant, object> orderBy = null, bool isDescending = false)
    {
        return Task.Run(() =>
        this.Container.GetItemLinqQueryable<ChatParticipant>(true)
        .Where(predicate)
        .Skip(skip)
        .Take(count)
        .AsEnumerable());
    }
}

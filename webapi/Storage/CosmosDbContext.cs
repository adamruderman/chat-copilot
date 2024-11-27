// Copyright (c) Microsoft. All rights reserved.

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
    public async Task<int> CountEntitiesAsync(Func<T, bool>? predicate = null)
    {
        var query = this.Container.GetItemLinqQueryable<T>(true);

        if (predicate != null)
        {
            query = (IOrderedQueryable<T>)query.Where(predicate);
        }

        var iterator = query.ToFeedIterator();

        int totalCount = 0;
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            totalCount += response.Count;
        }

        return totalCount;
    }
    public async Task<int> CountEntitiesAsync(string partitionKey, Func<T, bool>? predicate = null)
    {
        var query = this.Container.GetItemLinqQueryable<T>(
            true,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) });

        if (predicate != null)
        {
            query = (IOrderedQueryable<T>)query.Where(predicate);
        }

        var iterator = query.ToFeedIterator();

        int totalCount = 0;
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            totalCount += response.Count;
        }

        return totalCount;
    }

    public async Task<(IEnumerable<T>, string)> QueryEntitiesWithContinuationAsync(
        Func<T, bool> predicate,
        string? partitionKey = null,
        int count = 10,
        string? continuationToken = null)
    {
        var queryDefinition = new QueryDefinition($"SELECT * FROM c WHERE c.chatId = '{partitionKey}' ORDER BY c.Timestamp DESC"); new QueryDefinition("SELECT * FROM c WHERE c.chatId = @chatId ORDER BY c.Timestamp DESC")
                                    .WithParameter("@chatId", partitionKey);

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = partitionKey != null ? new PartitionKey(partitionKey) : null,
            MaxItemCount = count
        };

        var queryIterator = this.Container.GetItemQueryIterator<T>(queryDefinition, continuationToken, requestOptions);

        if (queryIterator.HasMoreResults)
        {
            var response = await queryIterator.ReadNextAsync();
            return (response.Resource, response.ContinuationToken);
        }

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return (Enumerable.Empty<T>(), null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(
        Func<T, bool> predicate,
        Func<T, object>? orderBy = null,
        bool isDescending = false)
    {
        return Task.Run<IEnumerable<T>>(() =>
        {
            // Get the queryable collection from Cosmos DB
            var query = this.Container.GetItemLinqQueryable<T>(true)
                .Where(predicate);

            // Apply ordering if provided
            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }

            return query.AsEnumerable();
        });
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(
            Func<T, bool> predicate,
            string partitionKey,
            Func<T, object>? orderBy = null,
            bool isDescending = false)
    {
        return Task.Run<IEnumerable<T>>(() =>
        {
            // Get the queryable collection from Cosmos DB scoped by partition key
            var query = this.Container.GetItemLinqQueryable<T>(
                true,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) })
                .Where(predicate);

            // Apply ordering if provided
            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            return query.AsEnumerable();
        });
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
    public new async Task<(IEnumerable<CopilotChatMessage>, string)> QueryEntitiesWithContinuationAsync(
           Func<CopilotChatMessage, bool> predicate,
           string? partitionKey = null,
           int count = 10,
           string? continuationToken = null)
    {
        return await base.QueryEntitiesWithContinuationAsync(predicate, partitionKey, count, continuationToken);
    }
}

public class CosmosDbChatParticipantContext : CosmosDbContext<ChatParticipant>, IChatParticipantStorageContext
{
    public CosmosDbChatParticipantContext(string connectionString, string database, string container)
        : base(connectionString, database, container)
    {
    }

    public new async Task<(IEnumerable<ChatParticipant>, string)> QueryEntitiesWithContinuationAsync(
        Func<ChatParticipant, bool> predicate,
        string? partitionKey = null,
        int count = 10,
        string? continuationToken = null)
    {
        var queryDefinition = new QueryDefinition($"SELECT * FROM c WHERE c.userId = '{partitionKey}'  ORDER BY c.Timestamp DESC"); new QueryDefinition("SELECT * FROM c WHERE c.chatId = @chatId ORDER BY c.Timestamp DESC")
                                    .WithParameter("@userId", partitionKey);

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = partitionKey != null ? new PartitionKey(partitionKey) : null,
            MaxItemCount = count
        };

        var queryIterator = this.Container.GetItemQueryIterator<ChatParticipant>(queryDefinition, continuationToken, requestOptions);

        if (queryIterator.HasMoreResults)
        {
            var response = await queryIterator.ReadNextAsync();
            return (response.Resource, response.ContinuationToken);
        }

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return (Enumerable.Empty<ChatParticipant>(), null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}

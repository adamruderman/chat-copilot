// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// Defines the basic CRUD operations for a repository.
/// </summary>
/// <summary>
/// Defines the basic CRUD operations for a repository.
/// </summary>
public class Repository<T> : IRepository<T> where T : IStorageEntity
{
    /// <summary>
    /// The storage context.
    /// </summary>
    protected IStorageContext<T> StorageContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    public Repository(IStorageContext<T> storageContext)
    {
        this.StorageContext = storageContext;
    }

    /// <inheritdoc/>
    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity ID cannot be null or empty.");
        }

        return this.StorageContext.CreateAsync(entity);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        return this.StorageContext.DeleteAsync(entity);
    }

    /// <inheritdoc/>
    public Task<T> FindByIdAsync(string id, string? partition = null)
    {
        return this.StorageContext.ReadAsync(id, partition ?? id);
    }

    /// <inheritdoc/>
    public async Task<bool> TryFindByIdAsync(string id, string? partition = null, Action<T?>? callback = null)
    {
        try
        {
            T? found = await this.FindByIdAsync(id, partition ?? id);

            callback?.Invoke(found);

            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is KeyNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        return this.StorageContext.UpsertAsync(entity);
    }

    /// <summary>
    /// Queries entities with sorting.
    /// </summary>
    /// <param name="predicate">Predicate to filter the results.</param>
    /// <param name="partitionKey">The partition key to scope the query.</param>
    /// <param name="orderBy">Optional function to order the entities.</param>
    /// <param name="isDescending">Whether to order entities in descending order.</param>
    public async Task<IEnumerable<T>> QueryEntitiesAsync(
        Func<T, bool> predicate,
        string? partitionKey = null,
        Func<T, object>? orderBy = null,
        bool isDescending = false
    )
    {
        return partitionKey == null
            ? await this.StorageContext.QueryEntitiesAsync(predicate)
            : await this.StorageContext.QueryEntitiesAsync(predicate, partitionKey, orderBy, isDescending);
    }

    // New method for paginated queries with continuation tokens
    public Task<(IEnumerable<T>, string)> QueryEntitiesWithContinuationAsync(
        Func<T, bool> predicate,
        string? partitionKey = null,
        int count = 10,
        string? continuationToken = null) =>
        this.StorageContext.QueryEntitiesWithContinuationAsync(predicate, partitionKey, count, continuationToken);
}
/// <summary>
/// Specialization of Repository<T> for CopilotChatMessage.
/// </summary>
/// <summary>
/// Specialization of Repository<T> for CopilotChatMessage.
/// </summary>
public class CopilotChatMessageRepository : Repository<CopilotChatMessage>
{
    private readonly ICopilotChatMessageStorageContext _messageStorageContext;

    public CopilotChatMessageRepository(ICopilotChatMessageStorageContext storageContext)
        : base(storageContext)
    {
        this._messageStorageContext = storageContext;
    }
}

/// <summary>
/// Specialization of Repository<T> for ChatParticpants.
/// </summary>
public class CopilotParticpantsRepository : Repository<ChatParticipant>
{
    private readonly IChatParticipantStorageContext _particpantStorageContext;

    public CopilotParticpantsRepository(IChatParticipantStorageContext storageContext)
        : base(storageContext)
    {
        this._particpantStorageContext = storageContext;
    }

    /// <summary>
    /// Queries entities with continuation token support.
    /// </summary>
    /// <param name="predicate">Predicate to filter the results.</param>
    /// <param name="partitionKey">The partition key to scope the query.</param>
    /// <param name="count">The number of entities to return.</param>
    /// <param name="continuationToken">The continuation token for paging.</param>
    /// <returns>A tuple containing the results, the continuation token, and whether there are more results.</returns>
    public new async Task<(IEnumerable<ChatParticipant>, string)> QueryEntitiesWithContinuationAsync(
        Func<ChatParticipant, bool> predicate,
        string? partitionKey = null,
        int count = 10,
        string? continuationToken = null
    )
    {
        return await this._particpantStorageContext.QueryEntitiesWithContinuationAsync(predicate, partitionKey, count, continuationToken);
    }
}

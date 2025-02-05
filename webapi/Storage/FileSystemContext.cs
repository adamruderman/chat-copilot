// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities on disk.
/// </summary>
public class FileSystemContext<T> : IStorageContext<T> where T : IStorageEntity
{
    /// <summary>
    /// Initializes a new instance of the FileSystemContext class and loads the entities from disk.
    /// </summary>
    /// <param name="filePath">The file path to store and read entities on disk.</param>
    public FileSystemContext(FileInfo filePath)
    {
        this._fileStorage = filePath;

        this.Entities = this.Load(this._fileStorage);
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

        if (this.Entities.TryAdd(entity.Id, entity))
        {
            this.Save(this.Entities, this._fileStorage);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        if (this.Entities.TryRemove(entity.Id, out _))
        {
            this.Save(this.Entities, this._fileStorage);
        }

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

        return Task.FromException<T>(new KeyNotFoundException($"Entity with id {entityId} not found."));
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity Id cannot be null or empty.");
        }

        if (this.Entities.AddOrUpdate(entity.Id, entity, (key, oldValue) => entity) != null)
        {
            this.Save(this.Entities, this._fileStorage);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> CountEntitiesAsync(string partitionKey, Func<T, bool>? predicate = null)
    {
        // Filter the entities by partitionKey
        var filteredEntities = this.Entities.Values
            .Where(entity => entity.Partition == partitionKey);

        // Apply predicate if provided
        if (predicate != null)
        {
            filteredEntities = filteredEntities.Where(predicate);
        }

        return Task.FromResult(filteredEntities.Count());
    }

    /// <summary>
    /// A concurrent dictionary to store entities in memory.
    /// </summary>
    protected sealed class EntityDictionary : ConcurrentDictionary<string, T>
    {
    }

    /// <summary>
    /// Using a concurrent dictionary to store entities in memory.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly EntityDictionary Entities;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// The file path to store entities on disk.
    /// </summary>
    private readonly FileInfo _fileStorage;

    /// <summary>
    /// A lock object to prevent concurrent access to the file storage.
    /// </summary>
    private readonly object _fileStorageLock = new();

    /// <summary>
    /// Save the state of the entities to disk.
    /// </summary>
    private void Save(EntityDictionary entities, FileInfo fileInfo)
    {
        lock (this._fileStorageLock)
        {
            if (!fileInfo.Exists)
            {
                fileInfo.Directory!.Create();
                File.WriteAllText(fileInfo.FullName, "{}");
            }

            using FileStream fileStream = File.Open(
                path: fileInfo.FullName,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Write,
                share: FileShare.Read);

            JsonSerializer.Serialize(fileStream, entities);
        }
    }

    /// <summary>
    /// Load the state of entities from disk.
    /// </summary>
    private EntityDictionary Load(FileInfo fileInfo)
    {
        lock (this._fileStorageLock)
        {
            if (!fileInfo.Exists)
            {
                fileInfo.Directory!.Create();
                File.WriteAllText(fileInfo.FullName, "{}");
            }

            using FileStream fileStream = File.Open(
                path: fileInfo.FullName,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Read,
                share: FileShare.Read);

            return JsonSerializer.Deserialize<EntityDictionary>(fileStream) ?? new EntityDictionary();
        }
    }

    public Task<IEnumerable<T>> QueryEntitiesAsync(
        Func<T, bool> predicate,
        string partitionKey,
        Func<T, object>? orderBy = null,
        bool isDescending = false)
    {
        var query = this.Entities.Values.Where(predicate);

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

        var pagedResults = filtered.Skip(int.Parse(continuationToken ?? "0", System.Globalization.CultureInfo.InvariantCulture)).Take(count);
        var nextToken = (int.Parse(continuationToken ?? "0", System.Globalization.CultureInfo.InvariantCulture) + count).ToString(System.Globalization.CultureInfo.InvariantCulture);

        return Task.FromResult((pagedResults, nextToken));
    }
}

/// <summary>
/// Specialization of FileSystemContext<T> for CopilotChatMessage.
/// </summary>
/// <summary>
/// Specialization of FileSystemContext<T> for CopilotChatMessage.
/// </summary>
public class FileSystemCopilotChatMessageContext : FileSystemContext<CopilotChatMessage>, ICopilotChatMessageStorageContext
{
    /// <summary>
    /// Initializes a new instance of the FileSystemCopilotChatMessageContext class.
    /// </summary>
    public FileSystemCopilotChatMessageContext(FileInfo filePath) : base(filePath)
    {
    }

    /// <inheritdoc/>
    public new Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate)
    {
        return Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this.Entities.Values
                .Where(predicate)
                .OrderByDescending(m => m.Timestamp) // Ensure sorting by Timestamp descending
);
    }

    public new Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, string partitionKey)
    {
        return Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this.Entities.Values
                .Where(m => m.Partition == partitionKey && predicate(m))
                .OrderByDescending(m => m.Timestamp) // Ensure sorting by Timestamp descending
        );
    }
}

/// <summary>
/// Specialization of VolatileContext<T> for CopilotChatMessage.
/// </summary>
public class FileSystemCopilotParticipantContext : FileSystemContext<ChatParticipant>, IChatParticipantStorageContext
{
    public FileSystemCopilotParticipantContext(FileInfo filePath) : base(filePath)
    {
    }
}

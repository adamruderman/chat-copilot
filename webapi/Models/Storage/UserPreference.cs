// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A chat participant is a user that is part of a chat.
/// A user can be part of multiple chats, thus a user can have multiple chat participants.
/// </summary>
public class UserPreference : IStorageEntity
{
    /// <summary>
    /// Participant ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// User ID that is persistent and unique.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Dark Mode preference.
    /// </summary>
    public bool DarkMode { get; set; }
    /// <summary>
    /// Persona preference.
    /// </summary>
    public bool Persona { get; set; }
    /// <summary>
    /// Chat View preference.
    /// </summary>
    public bool SimplifiedChat { get; set; }
    /// <summary>
    /// Export Chat option preference.
    /// </summary>
    public bool ExportChat { get; set; }
    /// <summary>
    /// The partition key for the source.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.UserId;

    public UserPreference(string userId, bool DarkMode, bool Persona, bool SimplifiedChat, bool ExportChat)
    {
        this.Id = userId;
        this.UserId = userId;
        this.DarkMode = DarkMode;
        this.Persona = Persona;
        this.SimplifiedChat = SimplifiedChat;
        this.ExportChat = ExportChat;
    }

}

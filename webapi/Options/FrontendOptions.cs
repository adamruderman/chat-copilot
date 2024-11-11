// Copyright (c) Microsoft. All rights reserved.

using System;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options to be relayed to the frontend.
/// </summary>
public sealed class FrontendOptions
{
    public const string PropertyName = "Frontend";

    /// <summary>
    /// Client ID for the frontend
    /// </summary>
    public string AadClientId { get; set; } = string.Empty;

    public string HeaderTitle { get; set; } = string.Empty;
    public string HeaderTitleColor { get; set; } = string.Empty;
    public string HeaderBackgroundColor { get; set; } = string.Empty;
    public string HeaderIcon { get; set; } = string.Empty;
    public Boolean HeaderSettingsEnabled { get; set; } = false;
    public Boolean HeaderPluginsEnabled { get; set; } = false;
    public Boolean DocumentLocalUploadEnabled { get; set; } = false;
    public Boolean DocumentGlobalUploadEnabled { get; set; } = false;
    public Boolean CreateNewChat { get; set; } = false;
    public string DisclaimerMsg { get; set; } = string.Empty;
}

// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration;

public class SlideContentEventArgs : EventArgs
{
    public Dictionary<object, object> Values { get; set; }
    public SlideContentEventArgs(Dictionary<object, object> values)
    {
        Values = values;
    }
}

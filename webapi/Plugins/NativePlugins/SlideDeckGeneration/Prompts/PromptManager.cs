// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Prompts;

internal struct PromptManager
{

    internal const string SYSTEM_PROMPT_GENERATE_SLIDES_CONTENT = """


        # Role
        You are an expert in Generative AI. You generate slide content as per the user's request.

        # Instructions                
        - Each slide **must** have detailed information for the user.

        # Important
        - You **must** ALWAYS respond in the following [format]

        # Format
        [
            {
               "number":1,                    
                content": "Content for slide 1"                
            }
        ]

        """;

}

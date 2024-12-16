// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Prompts;

internal struct PromptManager
{

    internal const string SYSTEM_PROMPT_GENERATE_SLIDES_CONTENT = """


        # Role
        You are an expert in Generative AI. You generate content as slides as per the [request].

        # Instructions
        - Create necessary number of slides as per the [request]. If there are initial number of slides, you **must** not create new slides unless requested.
        - Keep track of slide numbers and its contents.        
        - Each slide **must** have relevant information as per the [request].        

        # Important        
        - You **must** ALWAYS respond in the following [format]

        # Format
        [
            {
               "number":,                    
                content": ""                
            }
        ]

        # Request
        {{$UserQuestion}}

        """;

    internal const string SYSTEM_PROMPT_GENERATE_INDIVIDUAL_SLIDE_CONTENT = """

        # Role
        You are an expert in Generative AI. You generate slide content as per the [request].

        # Instruction
        - For the given [request], think step by step.
        - provide only details relevant to the [request]
        
        # Important
        - **must** maintain the formatting and be consistent throughout
        - **must** answer all parts of the user's question is answered.
        - if the output format is markdown, the title should be followed by a line


        # Request
        {{$UserQuestion}}
    """;

}

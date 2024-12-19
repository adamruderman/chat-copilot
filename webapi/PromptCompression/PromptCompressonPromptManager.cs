// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.PromptCompression;

internal struct PromptCompressonPromptManager
{

    internal const string PROMPT_COMPRESSION = """

        # Instructions
        - Remove paragraphs and new lines.
        - Then aggressively compress the text in such way the semantic context is retained
        - **must** ensure the total token count is less than or equal to HALF of the given text.
        - Keep content separators like "Slide 1" or "page 1".
        - Numerical values are important, **don't** remove them. 
        - Output in a single text format with NO formatting.
        """;
}

// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CopilotChat.WebApi.PromptCompression;

internal static class PromptCompressor
{

    internal static async Task<string> CompressPrompt(Kernel kernel, string prompt)
    {
        OpenAIPromptExecutionSettings chatSettings = new OpenAIPromptExecutionSettings()
        {
            MaxTokens = 1000,
            Temperature = 0,
            TopP = 1,


        };
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        string systemMessage = PromptCompressonPromptManager.PROMPT_COMPRESSION;

        var chatHistory = new ChatHistory(systemMessage);
        chatHistory.AddUserMessage(prompt);

        ChatMessageContent answer = await chatCompletion.GetChatMessageContentAsync(chatHistory, chatSettings, kernel).ConfigureAwait(false);


        // Compress the prompt
        return answer.Content;
    }

}

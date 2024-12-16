// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Net;
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

        int retryCount = 0;
        bool success = false;
        string compressedPrompt = string.Empty;
        while (retryCount < 3 && !success)
        {
            try
            {
                // Compress the prompt
                ChatMessageContent answer = await chatCompletion.GetChatMessageContentAsync(chatHistory, chatSettings, kernel).ConfigureAwait(false);
                success = true;
                compressedPrompt = answer.Content;
            }
            catch (Exception ex) when (ex is HttpOperationException httpEx && httpEx.StatusCode == (HttpStatusCode)429)
            {
                int retryAfter = int.Parse(httpEx.ResponseContent?.ToLower(CultureInfo.CurrentCulture).Split("retry after ")[1].Split(" seconds")[0], CultureInfo.CurrentCulture);
                await Task.Delay(retryAfter * 1000);
                retryCount++;
                if (retryCount >= 3)
                {
                    throw;
                }
            }
        }
        return compressedPrompt;

    }

}

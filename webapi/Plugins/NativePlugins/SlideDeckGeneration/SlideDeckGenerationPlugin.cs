// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text;
using CopilotChat.WebApi.Attributes;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Models;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Pipeline;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Prompts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration;



[NativePlugin]
public class SlideDeckGenerationPlugin(Kernel kernel, ILogger logger)
{
    private Kernel _kernel = kernel;
    private readonly object _lock = new object();
    private readonly ILogger _logger = logger;



    [KernelFunction("GetSlidesContent"), Description("Generate slide content for the user question")]
    public async Task<string> GetContent(string userQuestion, CancellationToken cancellationToken = default)
    {

        await this.UpdateUIWithMessage("Generating slide content for the user question...");

        KernelFunction[] kernelFunctionsToPipe = await this.BuildKernelFunctions().ConfigureAwait(false);
        KernelFunction pipeline = SKPipeLineManager.Pipe(kernelFunctionsToPipe, "pipeline");

        KernelArguments pipeArgs = new()
        {
            ["userQuestion"] = userQuestion

        };

        FunctionResult result = await pipeline.InvokeAsync(kernel, pipeArgs).ConfigureAwait(false);
        string trimmedResult = result.ToString().Replace("\n", Environment.NewLine);

        await this.UpdateUIWithMessage("Generating content.");

        return trimmedResult;
    }



    private OpenAIPromptExecutionSettings GetChatSettings()
    {
        var settings = new OpenAIPromptExecutionSettings()
        {
            MaxTokens = 3000,
            Temperature = 0,
            TopP = 1,

        };


        return settings;
    }


    private async Task<KernelFunction[]> BuildKernelFunctions()
    {
        //Get the slides structure
        KernelFunction kernelFunctionGetSlideStructure = KernelFunctionFactory.CreateFromMethod(async (string userQuestion) =>
        {
            return await this.GenerateIndividualSlideContent(userQuestion).ConfigureAwait(false);
        });


        KernelFunction kernelFunctionSlideDetails = KernelFunctionFactory.CreateFromMethod(async (IEnumerable<IndividualSlideContent> slides) =>
        {
            return await this.GenerateContentForEachSlide(slides).ConfigureAwait(false);
        });


        //Build the Pipeline

        IList<KernelFunction> functions = new List<KernelFunction>();
        functions.Add(kernelFunctionGetSlideStructure);
        functions.Add(kernelFunctionSlideDetails);


        return functions.ToArray();

    }


    private async Task<IEnumerable<IndividualSlideContent>> GenerateIndividualSlideContent(string userQuestion, CancellationToken cancellationToken = default)
    {

        await this.UpdateUIWithMessage("Generating slide structure for the user question...");

        _logger.LogInformation("Generating the slide structure for the user question...");
        //1. Get the basic content for each slide in a json format
        //Add system prompt


        KernelArguments arguments = new()
            {
                { "UserQuestion", userQuestion }
            };

        string prompt = PromptManager.SYSTEM_PROMPT_GENERATE_SLIDES_CONTENT;
        string systemMessage = await new KernelPromptTemplateFactory().Create(new PromptTemplateConfig(prompt)).RenderAsync(kernel, arguments);

        OpenAIPromptExecutionSettings chatSettings = this.GetChatSettings();

        var chatCompletion = this._kernel.GetRequiredService<IChatCompletionService>();
        ChatMessageContent answer = null;


        int retryCount = 0;
        bool success = false;

        while (retryCount < 3 && !success)
        {
            try
            {
                answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel).ConfigureAwait(false);
                success = true;
            }
            catch (Exception ex) when (ex is HttpOperationException httpEx && httpEx.StatusCode == (HttpStatusCode)429)
            {

                int retryAfter = int.Parse(httpEx.ResponseContent?.ToLower(CultureInfo.CurrentCulture).Split("retry after ")[1].Split(" second")[0], CultureInfo.CurrentCulture);

                _logger.LogError($"Rate limit exceed. Wail wait for {retryAfter}seconds before retryng.");
                await this.UpdateUIWithMessage($"Rate limited. Will retry after {retryAfter} seconds");

                await Task.Delay(retryAfter * 1000);
                retryCount++;
                if (retryCount >= 3)
                {
                    throw;
                }
            }

        }
        //answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel).ConfigureAwait(false);


        var resultArray = JArray.Parse(answer.Content);
        var slides = JsonConvert.DeserializeObject<IEnumerable<IndividualSlideContent>>(resultArray.ToString());

        _logger.LogInformation($"Finished generating the slide structure for the user question. Total Slides: {slides.Count()}");

        await this.UpdateUIWithMessage($"Generating the slides. Total Slides: {slides.Count()}");
        return slides;


    }


    private async Task<string> GenerateContentForEachSlide(IEnumerable<IndividualSlideContent> slides)
    {
        _logger.LogInformation($"Generating content for each slides...");
        OpenAIPromptExecutionSettings chatSettings = this.GetChatSettings();

        string prompt = PromptManager.SYSTEM_PROMPT_GENERATE_INDIVIDUAL_SLIDE_CONTENT;


        var chatCompletion = this._kernel.GetRequiredService<IChatCompletionService>();
        ChatMessageContent answer = null;


        StringBuilder contents = new StringBuilder();
        SortedDictionary<int, string> slideContents = new SortedDictionary<int, string>();



        foreach (var slide in slides)
        {
            _logger.LogInformation($"Generating content for slide {slide.Number}");
            await this.UpdateUIWithMessage($"Generating content for slide {slide.Number}");

            int retryCount = 0;
            bool success = false;
            KernelArguments arguments = new()
            {
                { "UserQuestion", slide.Content }
            };
            string systemMessage = await new KernelPromptTemplateFactory().Create(new PromptTemplateConfig(prompt)).RenderAsync(kernel, arguments);

            while (retryCount < 3 && !success)
            {
                try
                {
                    _logger.LogInformation($"- Generating content for slide {slide.Number}, Retry effort: {retryCount + 1}");
                    answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel).ConfigureAwait(false);
                    lock (this._lock)
                    {

                        if (slideContents.TryGetValue(slide.Number, out string? value))
                        {
                            slideContents[slide.Number] = $"{value}{Environment.NewLine}{answer.Content}";
                        }
                        else
                        {

                            slideContents.Add(slide.Number, answer.Content);
                        }
                    }
                    success = true;
                }
                catch (Exception ex) when (ex is HttpOperationException httpEx && httpEx.StatusCode == (HttpStatusCode)429)
                {

                    int retryAfter = int.Parse(httpEx.ResponseContent?.ToLower(CultureInfo.CurrentCulture).Split("retry after ")[1].Split(" second")[0], CultureInfo.CurrentCulture);

                    _logger.LogError($"Rate limit exceed. Wail wait for {retryAfter}seconds before retryng.");
                    await this.UpdateUIWithMessage($"Rate limited. Will retry after {retryAfter} seconds");

                    await Task.Delay(retryAfter * 1000);
                    retryCount++;
                    if (retryCount >= 3)
                    {
                        throw;
                    }
                }
            }

            _logger.LogInformation($"- Finished generating content for slide {slide.Number}");
        };
        foreach (var kvp in slideContents)
        {
            contents.Append($"{Environment.NewLine}# Slide {kvp.Key}{Environment.NewLine}{kvp.Value}{Environment.NewLine}");
        }

        _logger.LogInformation($"Finished generating content for {slides.Count()} slides.");

        return contents.ToString();
    }


    private async Task UpdateUIWithMessage(string message)
    {

        IHubContext<MessageRelayHub> hub = (IHubContext<MessageRelayHub>)_kernel.Data["messageUpdateRelayHubContext"];
        string chatId = _kernel.Data["ChatId"].ToString();
        await hub.Clients.Group(chatId).SendAsync("ReceiveBotResponseStatus", chatId, message, CancellationToken.None);
    }
}

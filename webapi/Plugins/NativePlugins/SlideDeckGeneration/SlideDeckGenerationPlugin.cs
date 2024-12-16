// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text;
using CopilotChat.WebApi.Attributes;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Models;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Pipeline;
using CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace CopilotChat.WebApi.Plugins.NativePlugins.SlideDeckGeneration;

[NativePlugin]
public class SlideDeckGenerationPlugin(Kernel kernel)
{
    private Kernel _kernel = kernel;
    private readonly object _lock = new object();

    [KernelFunction("GetSlidesContent"), Description("Generate slide content for the user question")]
    public async Task<string> GetContent(string userQuestion, CancellationToken cancellationToken = default)
    {
        KernelFunction[] kernelFunctionsToPipe = await this.BuildKernelFunctions().ConfigureAwait(false);
        KernelFunction pipeline = SKPipeLineManager.Pipe(kernelFunctionsToPipe, "pipeline");

        KernelArguments pipeArgs = new()
        {
            ["userQuestion"] = userQuestion

        };
        FunctionResult result = await pipeline.InvokeAsync(kernel, pipeArgs).ConfigureAwait(false);

        string trimmedResult = result.ToString().Replace("\n", Environment.NewLine);

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

    //private async Task<ChatHistory> BuildChatHistory(string userQuestion, string systemMessage)
    //{



    //    var chatHistory = new ChatHistory(systemMessage);


    //    // Add user question
    //    chatHistory.AddUserMessage(userQuestion);
    //    return chatHistory;
    //}


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

        answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel).ConfigureAwait(false);


        var resultArray = JArray.Parse(answer.Content);
        var slides = JsonConvert.DeserializeObject<IEnumerable<IndividualSlideContent>>(resultArray.ToString());

        return slides;


    }


    private async Task<string> GenerateContentForEachSlide(IEnumerable<IndividualSlideContent> slides)
    {
        OpenAIPromptExecutionSettings chatSettings = this.GetChatSettings();

        var chatCompletion = this._kernel.GetRequiredService<IChatCompletionService>();
        ChatMessageContent answer = null;


        StringBuilder contents = new StringBuilder();
        SortedDictionary<int, string> slideContents = new SortedDictionary<int, string>();


        string prompt = PromptManager.SYSTEM_PROMPT_GENERATE_INDIVIDUAL_SLIDE_CONTENT;
        await Parallel.ForEachAsync(slides, async (slide, token) =>
        {

            KernelArguments arguments = new()
            {
                { "UserQuestion", slide.Content }
            };

            string systemMessage = await new KernelPromptTemplateFactory().Create(new PromptTemplateConfig(prompt)).RenderAsync(kernel, arguments);

            try
            {
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
            }
            catch (Exception ex) when (ex is HttpOperationException httpEx && httpEx.StatusCode == (HttpStatusCode)429)
            {

                //try
                //{

                //    int retryAfter = int.Parse(httpEx.ResponseContent.ToLower(CultureInfo.CurrentCulture).Split("retry after ")[1].Split(" seconds")[0], CultureInfo.CurrentCulture);
                //    await Task.Delay(retryAfter * 1000, token);
                //    answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel, cancellationToken: token).ConfigureAwait(false);

                //}
                //catch (Exception)
                //{
                //    Console.WriteLine("Rate limit exceeded. Please try again later.");
                //}


                int retryCount = 0;
                bool success = false;

                while (retryCount < 3 && !success)
                {
                    try
                    {
                        int retryAfter = int.Parse(httpEx.ResponseContent?.ToLower(CultureInfo.CurrentCulture).Split("retry after ")[1].Split(" seconds")[0], CultureInfo.CurrentCulture);
                        await Task.Delay(retryAfter * 1000, token);
                        answer = await chatCompletion.GetChatMessageContentAsync(systemMessage, chatSettings, this._kernel, cancellationToken: token).ConfigureAwait(false);
                        success = true;
                    }
                    catch (Exception ex1) when (ex1 is HttpOperationException httpEx1 && httpEx.StatusCode == (HttpStatusCode)429)
                    {
                        retryCount++;
                        if (retryCount >= 3)
                        {
                            Console.WriteLine("Rate limit exceeded. Please try again later.");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }



        });
        foreach (var kvp in slideContents)
        {
            contents.Append($"{Environment.NewLine}# Slide {kvp.Key}{Environment.NewLine}{kvp.Value}{Environment.NewLine}");
        }



        return contents.ToString();
    }

}

// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private readonly Kernel _kernel;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        this._kernel = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory);
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public Kernel GetCompletionKernel() => this._kernel.Clone();

    private static Kernel InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddLogging();

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory

                // Define a retry policy with exponential backoff
                AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt =>
                    {
                        var jitter = new Random().NextDouble();
                        return TimeSpan.FromSeconds(60 + jitter);
                        //Math.Pow(2, retryAttempt)
                    });

                HttpClientHandler httpClientHandler = new HttpClientHandler();
                HttpClient httpClient = new(new PolicyHttpMessageHandler(retryPolicy) { InnerHandler = httpClientHandler })
                {
                    Timeout = TimeSpan.FromMinutes(60)
                };

                //builder.AddAzureOpenAIChatCompletion(
                //    azureAIOptions.Deployment,
                //    azureAIOptions.Endpoint,
                //    azureAIOptions.APIKey,
                //    httpClient: httpClientFactory.CreateClient());

                builder.AddAzureOpenAIChatCompletion(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClient);

                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                builder.AddOpenAIChatCompletion(
                    openAIOptions.TextModel,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            default:
                throw new ArgumentException($"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings.");
        }

        return builder.Build();
    }
}
